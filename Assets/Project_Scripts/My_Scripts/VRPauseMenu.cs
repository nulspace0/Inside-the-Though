using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
/// <summary>
/// VR Pause Menu — Meta Quest 3 и HTC Vive Cosmos Pro.
/// Открытие: левый стик / кнопка Menu / кнопка Y.
/// Кнопки нажимаются лучом контроллера + триггер.
/// </summary>
public class VRPauseMenu : MonoBehaviour
{
    [Header("Внешний вид")]
    public float menuDistance = 1.5f;
    public float menuWidth    = 0.6f;
    public float menuHeight   = 0.84f;   // высота с запасом под 4 кнопки + слайдер

    [Header("Скорость игрока")]
    public float minSpeed = 1f;
    public float maxSpeed = 12f;

    [Header("Цвета")]
    public Color backgroundColor = new Color(0.05f, 0.08f, 0.2f, 0.92f);
    public Color buttonColor     = new Color(0.1f,  0.4f,  0.8f, 1f);
    public Color buttonHover     = new Color(0.2f,  0.6f,  1.0f, 1f);
    public Color sliderFill      = new Color(0.2f,  0.7f,  1.0f, 1f);

    // ── внутренние ───────────────────────────────────────────────
    private static VRPauseMenu _instance;
    public  static bool IsPaused => _instance != null && _instance._isOpen;

    private GameObject          _menuRoot;
    private bool                _isOpen  = false;
    private XROrigin            _xrOrigin;
    private Camera              _xrCamera;
    private float               _cooldown = 0f;
    private InputAction         _menuAction;

    private ContinuousMoveProvider         _moveProvider;
    private Slider                         _speedSlider;
    private TextMeshProUGUI                _speedLabel;

    // ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _instance = this;
        _xrOrigin = FindAnyObjectByType<XROrigin>();
        if (_xrOrigin != null) _xrCamera = _xrOrigin.Camera;

        EnsureXRUIInputModule();
        CreateInputActions();
        BuildMenu();
        _menuRoot.SetActive(false);
    }

    private void Start()
    {
        // FindMoveProvider в Start — к этому моменту все компоненты гарантированно инициализированы
        FindMoveProvider();
    }

    private void EnsureXRUIInputModule()
    {
        // ── EventSystem ───────────────────────────────────────────
        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            var esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
        }

        // Убираем ВСЕ старые InputModule — они блокируют XR-лучи
        // DestroyImmediate нужен здесь: Destroy() асинхронный, модуль остаётся живым
        // до конца кадра и блокирует EnableUIOnInteractors() ниже
        foreach (var old in es.GetComponents<BaseInputModule>())
        {
            if (old is XRUIInputModule) continue;
            Debug.Log($"[VRPauseMenu] Удаляем InputModule: {old.GetType().Name}");
#if UNITY_EDITOR
            DestroyImmediate(old);
#else
            Destroy(old);
#endif
        }

        // Добавляем XRUIInputModule если нет
        if (es.GetComponent<XRUIInputModule>() == null)
        {
            es.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("[VRPauseMenu] XRUIInputModule добавлен на EventSystem.");
        }

        // ── Перерегистрируем интеракторы в новом XRUIInputModule ──
        // (toggle нужен т.к. интеракторы могли инициализироваться до XRUIInputModule)
        EnableUIOnInteractors();
    }

    private void EnableUIOnInteractors()
    {
        // XRRayInteractor (Teleport и пр.)
        foreach (var ri in FindObjectsByType<XRRayInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ri.enableUIInteraction = false;
            ri.enableUIInteraction = true;
            Debug.Log($"[VRPauseMenu] XRRayInteractor перерегистрирован: {ri.gameObject.name}");
        }

        // NearFarInteractor (XRI 3.x — основной луч контроллера)
        foreach (var ri in FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            ri.enableUIInteraction = false;
            ri.enableUIInteraction = true;
            Debug.Log($"[VRPauseMenu] NearFarInteractor перерегистрирован: {ri.gameObject.name}");
        }
    }

    private void CreateInputActions()
    {
        _menuAction = new InputAction("PauseMenu", InputActionType.Button);

        // ── Meta Quest 2/3 — левый контроллер ──────────────────────
        _menuAction.AddBinding("<XRController>{LeftHand}/secondaryButton");      // Y
        _menuAction.AddBinding("<XRController>{LeftHand}/thumbstickClicked");    // стик
        _menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
        _menuAction.AddBinding("<OculusTouchController>{LeftHand}/secondaryButton");
        _menuAction.AddBinding("<OculusTouchController>{LeftHand}/start");

        // ── HTC Vive Cosmos Pro 2 — оба контроллера ─────────────
        // (Cosmos не всегда чётко делит left/right, добавляем оба)
        _menuAction.AddBinding("<XRController>{LeftHand}/menu");
        _menuAction.AddBinding("<XRController>{RightHand}/menu");
        _menuAction.AddBinding("<XRController>{LeftHand}/menuButton");
        _menuAction.AddBinding("<XRController>{RightHand}/menuButton");
        _menuAction.AddBinding("<ViveController>{LeftHand}/menu");
        _menuAction.AddBinding("<ViveController>{RightHand}/menu");
        // Bumper / System кнопки Cosmos
        _menuAction.AddBinding("<XRController>{LeftHand}/systemButton");
        _menuAction.AddBinding("<XRController>{RightHand}/systemButton");
        _menuAction.AddBinding("<XRController>{LeftHand}/secondaryButton");
        _menuAction.AddBinding("<XRController>{RightHand}/secondaryButton");

        // ── Клавиатура (Editor) ───────────────────────────────────
        _menuAction.AddBinding("<Keyboard>/escape");
        _menuAction.AddBinding("<Keyboard>/m");

        _menuAction.performed += _ =>
        {
            if (_cooldown <= 0f) { _cooldown = 0.4f; ToggleMenu(); }
        };
        _menuAction.Enable();

        Debug.Log("[VRPauseMenu] InputAction создан. Нажми M или Escape для теста.");
    }

    private void Update()
    {
        if (_cooldown > 0f) _cooldown -= Time.unscaledDeltaTime;
    }

    // Меню следует за камерой каждый кадр — работает и при падении, и при движении
    private void LateUpdate()
    {
        if (_isOpen && _xrCamera != null)
            RepositionMenu();
    }

    private void RepositionMenu()
    {
        Vector3 fwd = _xrCamera.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
        fwd.Normalize();

        _menuRoot.transform.position = _xrCamera.transform.position
                                     + fwd * menuDistance
                                     + Vector3.down * 0.15f;
        _menuRoot.transform.rotation = Quaternion.LookRotation(fwd);
    }

    private void OnDestroy()
    {
        _menuAction?.Disable();
        _menuAction?.Dispose();
    }

    // ─────────────────────────────────────────────────────────────
    public void ToggleMenu()
    {
        if (_isOpen) CloseMenu();
        else         OpenMenu();
    }

    private void OpenMenu()
    {
        _isOpen = true;
        RepositionMenu();

        // Синхронизируем слайдер с текущей скоростью
        if (_speedSlider != null)
        {
            float spd = GetCurrentSpeed();
            _speedSlider.value = spd;
            UpdateSpeedLabel(spd);
        }

        _menuRoot.SetActive(true);
    }

    private void CloseMenu()
    {
        _isOpen = false;
        _menuRoot.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────
    //  Кнопки и слайдер
    // ─────────────────────────────────────────────────────────────
    public void OnResume()  => CloseMenu();

    public void OnRestart()
    {
        _isOpen = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnGoToHead()
    {
        _isOpen = false;
        SceneManager.LoadScene("Head");
    }

    public void OnExit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void FindMoveProvider()
    {
        _moveProvider = FindAnyObjectByType<ContinuousMoveProvider>(FindObjectsInactive.Include);
        if (_moveProvider != null)
            Debug.Log("[VRPauseMenu] ContinuousMoveProvider найден.");
        else
            Debug.LogWarning("[VRPauseMenu] ContinuousMoveProvider не найден — слайдер скорости неактивен");
    }

    private float GetCurrentSpeed()
    {
        return _moveProvider != null ? _moveProvider.moveSpeed : 6f;
    }

    private void OnSpeedChanged(float value)
    {
        if (_moveProvider != null)
            _moveProvider.moveSpeed = value;
        UpdateSpeedLabel(value);
    }

    private void UpdateSpeedLabel(float value) =>
        _speedLabel.text = $"Скорость: {value:F1}";

    // ─────────────────────────────────────────────────────────────
    //  Построение UI
    // ─────────────────────────────────────────────────────────────
    private void BuildMenu()
    {
        _menuRoot = new GameObject("VR_PauseMenu");
        _menuRoot.transform.SetParent(transform, false);

        Canvas canvas = _menuRoot.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.WorldSpace;
        canvas.worldCamera = _xrCamera;

        _menuRoot.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 300;

        var raycaster = _menuRoot.AddComponent<TrackedDeviceGraphicRaycaster>();
        // Отключаем блокировку 3D-объектами — иначе геометрия сцены мешает лучу дотянуться до UI
        raycaster.checkFor3DOcclusion = false;

        RectTransform cr = _menuRoot.GetComponent<RectTransform>();
        cr.sizeDelta  = new Vector2(menuWidth, menuHeight);
        cr.localScale = Vector3.one * 0.001f;

        GameObject bg = CreatePanel(_menuRoot.transform, "BG",
            Vector2.zero, new Vector2(menuWidth * 1000, menuHeight * 1000), backgroundColor);

        // ── Заголовок ─────────────────────────────────────────────
        CreateLabel(bg.transform, "МЕНЮ", 36, new Vector2(0, 320), Color.white);
        CreateDivider(bg.transform,           new Vector2(0, 280));

        // ── Кнопки ────────────────────────────────────────────────
        CreateMenuButton(bg.transform, "Продолжить",    new Vector2(0, 225), OnResume);
        CreateMenuButton(bg.transform, "Перезапустить", new Vector2(0, 160), OnRestart);
        CreateMenuButton(bg.transform, "Главная сцена", new Vector2(0,  95), OnGoToHead);
        CreateMenuButton(bg.transform, "Выход",         new Vector2(0,  30), OnExit);

        // ── Разделитель ───────────────────────────────────────────
        CreateDivider(bg.transform, new Vector2(0, -20));

        // ── Слайдер скорости ──────────────────────────────────────
        float initSpeed = GetCurrentSpeed();

        _speedLabel = CreateLabel(bg.transform, $"Скорость: {initSpeed:F1}", 26,
            new Vector2(0, -55), Color.white);

        _speedSlider = CreateSlider(bg.transform, new Vector2(0, -115),
            new Vector2(460, 50), minSpeed, maxSpeed, initSpeed);

        _speedSlider.onValueChanged.AddListener(OnSpeedChanged);

        // Метки мин/макс
        CreateLabel(bg.transform, minSpeed.ToString("F0"), 20,
            new Vector2(-220, -115), new Color(0.6f, 0.8f, 1f, 0.8f));
        CreateLabel(bg.transform, maxSpeed.ToString("F0"), 20,
            new Vector2( 220, -115), new Color(0.6f, 0.8f, 1f, 0.8f));

        // Подсказка внизу
        CreateLabel(bg.transform, "Луч контроллера + триггер", 18,
            new Vector2(0, -175), new Color(0.6f, 0.8f, 1f, 0.5f));
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────
    private GameObject CreatePanel(Transform parent, string name,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string text,
        int size, Vector2 pos, Color color)
    {
        var go = new GameObject("Lbl");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(500, 55);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    private void CreateDivider(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Div");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(480, 2);
        go.AddComponent<Image>().color = new Color(0.3f, 0.6f, 1f, 0.35f);
    }

    private void CreateMenuButton(Transform parent, string label,
        Vector2 pos, System.Action onClick)
    {
        var go = new GameObject("Btn_" + label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(420, 52);

        var img = go.AddComponent<Image>();
        img.color = buttonColor;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = buttonColor; cb.highlightedColor = buttonHover;
        cb.pressedColor = Color.white; btn.colors = cb;
        btn.onClick.AddListener(() => onClick());

        var textGo = new GameObject("T");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero; trt.anchoredPosition = Vector2.zero;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 26;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;
    }

    private Slider CreateSlider(Transform parent, Vector2 pos,
        Vector2 size, float min, float max, float value)
    {
        // Корень слайдера
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos; rt.sizeDelta = size;

        // Прозрачный Image на корне — нужен чтобы TrackedDeviceGraphicRaycaster
        // мог попасть в слайдер в любой точке его площади, не только на ручке
        var hitArea = go.AddComponent<Image>();
        hitArea.color = Color.clear;

        var slider = go.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = value;

        // Фон трека — raycastTarget=false, чтобы не перехватывать события раньше ручки
        var bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
        var bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.15f, 0.3f, 1f);
        bgImg.raycastTarget = false;

        // Fill area — raycastTarget=false по той же причине
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f);
        faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.sizeDelta = new Vector2(-20, 0);
        faRt.anchoredPosition = new Vector2(-5, 0);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0, 1);
        fillRt.sizeDelta = new Vector2(10, 0);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = sliderFill;
        fillImg.raycastTarget = false;

        // Handle — увеличен до 60×60 для удобного захвата в VR
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haRt = handleArea.AddComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero; haRt.anchorMax = Vector2.one;
        haRt.sizeDelta = new Vector2(-20, 0);
        haRt.anchoredPosition = Vector2.zero;

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var hRt = handle.AddComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(120, 120); // зона попадания луча — большая
        // Прозрачный Image на самом handle — нужен для raycast
        var hHitImg = handle.AddComponent<Image>();
        hHitImg.color = Color.clear;

        // Визуальная ручка — дочерний объект, маленькая белая точка
        var hVisual = new GameObject("Handle_Visual");
        hVisual.transform.SetParent(handle.transform, false);
        var hVisRt = hVisual.AddComponent<RectTransform>();
        hVisRt.sizeDelta = new Vector2(44, 44);
        hVisRt.anchoredPosition = Vector2.zero;
        var hImg = hVisual.AddComponent<Image>();
        hImg.color = Color.white;

        slider.fillRect   = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = hHitImg; // цветовые переходы на зоне попадания
        slider.direction  = Slider.Direction.LeftToRight;

        var hcb = slider.colors;
        hcb.normalColor      = Color.white;
        hcb.highlightedColor = new Color(0.8f, 0.95f, 1f);
        hcb.pressedColor     = sliderFill;
        slider.colors = hcb;

        return slider;
    }
}
