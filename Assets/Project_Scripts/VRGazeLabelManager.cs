using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// VR Gaze Label Manager — показывает подпись при взгляде на объект с GazeTargetData.
///
/// Улучшения:
///   1. Плавное появление/исчезновение (CanvasGroup fade)
///   2. Задержка перед показом (Dwell Time) — нужно смотреть X секунд
///   3. Индикатор взгляда — кольцо заполняется пока смотришь
///   4. Плавное движение надписи (SmoothDamp)
///   5. Подсветка объекта при взгляде (через MaterialPropertyBlock)
/// </summary>
public class VRGazeLabelManager : MonoBehaviour
{
    [Header("Камера")]
    public Camera gazeCamera;

    [Header("UI объекты (назначь в Inspector)")]
    public GameObject globalLabelCanvasGO;
    public GameObject globalLabelTextGO;

    [Header("Размер и шрифт")]
    [Tooltip("Масштаб Canvas. Попробуй 0.001 – 0.003")]
    public float labelScale  = 0.002f;
    public int   fontSize    = 24;

    [Header("Расстояние от камеры")]
    public float fixedLabelDistance = 2.0f;

    [Header("1. Fade — плавное появление")]
    public float fadeInSpeed  = 4f;
    public float fadeOutSpeed = 6f;

    [Header("2. Dwell Time — задержка перед показом")]
    [Tooltip("Сколько секунд нужно смотреть на объект чтобы появилась надпись")]
    public float dwellTime = 0.8f;

    [Header("3. Индикатор взгляда (кольцо)")]
    public Color  ringColor       = new Color(0.3f, 0.85f, 1f, 0.9f);
    public Color  ringBgColor     = new Color(1f,   1f,    1f, 0.15f);
    [Tooltip("Размер кольца в метрах")]
    public float  ringSize        = 0.04f;
    [Tooltip("Расстояние кольца от камеры")]
    public float  ringDistance    = 1.8f;

    [Header("4. Smooth Follow — плавное движение надписи")]
    [Tooltip("Скорость следования надписи. Чем больше — тем быстрее")]
    public float followSpeed = 6f;

    [Header("5. Highlight — подсветка объекта")]
    public Color  highlightColor     = new Color(0.4f, 0.9f, 1.0f, 1f);
    [Range(0f, 1f)]
    public float  highlightIntensity = 0.5f;

    // ── внутренние переменные ────────────────────────────────────
    private CanvasGroup     _canvasGroup;
    private Text            _labelText;
    private TMP_Text        _labelTextTMP;

    private GameObject      _currentGazedObject;
    private GazeTargetData  _currentGazeTargetData;

    private float           _gazeTimer    = 0f;
    private bool            _labelVisible = false;
    private float           _targetAlpha  = 0f;

    // Smooth follow
    private Vector3         _targetLabelPos;
    private Vector3         _smoothVelocity;

    // Progress ring
    private GameObject      _ringRoot;
    private Image           _ringFill;
    private Image           _ringBg;

    // Highlight
    private Renderer            _highlightedRenderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int _emissionColorID  = Shader.PropertyToID("_EmissionColor");
    private static readonly int _pulseIntensityID = Shader.PropertyToID("_PulseIntensity");

    // ─────────────────────────────────────────────────────────────
    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
    }

    void Start()
    {
        // Камера
        if (gazeCamera == null) gazeCamera = Camera.main;
        if (gazeCamera == null) { Debug.LogError("[GazeLabelManager] Камера не найдена!"); enabled = false; return; }

        if (globalLabelCanvasGO == null) { Debug.LogError("[GazeLabelManager] globalLabelCanvasGO не назначен!"); enabled = false; return; }
        if (globalLabelTextGO  == null) { Debug.LogError("[GazeLabelManager] globalLabelTextGO не назначен!");  enabled = false; return; }

        // Ищем TMP_Text (приоритет), потом Legacy Text
        _labelTextTMP = globalLabelTextGO.GetComponent<TMP_Text>()
                     ?? globalLabelTextGO.GetComponentInChildren<TMP_Text>();
        if (_labelTextTMP != null)
        {
            _labelTextTMP.fontSize = fontSize;
        }
        else
        {
            _labelText = globalLabelTextGO.GetComponent<Text>()
                      ?? globalLabelTextGO.GetComponentInChildren<Text>();
            if (_labelText != null)
                _labelText.fontSize = fontSize;
            else
                Debug.LogWarning("[GazeLabelManager] Не найден ни TMP_Text, ни Legacy Text на " + globalLabelTextGO.name);
        }

        // Назначаем worldCamera на Canvas (без этого не рендерится в VR!)
        Canvas canvas = globalLabelCanvasGO.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.worldCamera = gazeCamera;
        }

        // CanvasGroup для fade (не используем ??, т.к. Unity перегружает == но не ??)
        _canvasGroup = globalLabelCanvasGO.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = globalLabelCanvasGO.AddComponent<CanvasGroup>();
        _canvasGroup.alpha          = 0f;
        _canvasGroup.blocksRaycasts = false;

        globalLabelCanvasGO.SetActive(true);
        globalLabelCanvasGO.transform.localScale = Vector3.one * labelScale;

        // Стартовая позиция надписи
        _targetLabelPos = gazeCamera.transform.position
                        + gazeCamera.transform.forward * fixedLabelDistance;
        globalLabelCanvasGO.transform.position = _targetLabelPos;

        BuildProgressRing();

        Debug.Log("[GazeLabelManager] Инициализирован. TMP: " + (_labelTextTMP != null) + ", LegacyText: " + (_labelText != null));
    }

    void Update()
    {
        PerformGazeRaycast();
        UpdateFade();
        UpdateLabelPosition();
        UpdateProgressRing();
    }

    // ─────────────────────────────────────────────────────────────
    //  1+2. Рейкаст + Dwell Time
    // ─────────────────────────────────────────────────────────────
    private void PerformGazeRaycast()
    {
        Ray ray = new Ray(gazeCamera.transform.position, gazeCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            GazeTargetData data = hit.collider.GetComponent<GazeTargetData>();

            if (data != null)
            {
                // Переключились на новый объект
                if (_currentGazedObject != hit.collider.gameObject)
                {
                    ClearGaze();
                    _currentGazedObject   = hit.collider.gameObject;
                    _currentGazeTargetData = data;
                    _gazeTimer   = 0f;
                    _labelVisible = false;
                    _targetAlpha  = 0f;
                    if (_labelTextTMP != null)  _labelTextTMP.text = data.displayText;
                    else if (_labelText != null) _labelText.text = data.displayText;
                    ApplyHighlight(_currentGazedObject);
                }

                // Dwell: накапливаем время взгляда
                _gazeTimer += Time.deltaTime;
                if (!_labelVisible && _gazeTimer >= dwellTime)
                {
                    _labelVisible = true;
                    _targetAlpha  = 1f;
                    Debug.Log("[GazeLabelManager] Показываем надпись: " + data.displayText);
                }

                // Цель надписи — перед камерой
                _targetLabelPos = gazeCamera.transform.position
                                + gazeCamera.transform.forward * fixedLabelDistance;
            }
            else
            {
                ClearGaze();
            }
        }
        else
        {
            ClearGaze();
        }
    }

    private void ClearGaze()
    {
        if (_currentGazedObject == null) return;
        ClearHighlight();
        _currentGazedObject    = null;
        _currentGazeTargetData = null;
        _gazeTimer    = 0f;
        _labelVisible = false;
        _targetAlpha  = 0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  1. Fade
    // ─────────────────────────────────────────────────────────────
    private void UpdateFade()
    {
        if (_canvasGroup == null) return;
        float speed = _targetAlpha > _canvasGroup.alpha ? fadeInSpeed : fadeOutSpeed;
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, _targetAlpha, speed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────────
    //  4. Smooth Follow
    // ─────────────────────────────────────────────────────────────
    private void UpdateLabelPosition()
    {
        if (globalLabelCanvasGO == null || gazeCamera == null) return;

        // Плавно двигаемся к целевой позиции
        globalLabelCanvasGO.transform.position = Vector3.SmoothDamp(
            globalLabelCanvasGO.transform.position,
            _targetLabelPos,
            ref _smoothVelocity,
            1f / followSpeed
        );

        // Всегда смотрит на камеру
        Vector3 dir = gazeCamera.transform.position - globalLabelCanvasGO.transform.position;
        if (dir.sqrMagnitude > 0.001f)
            globalLabelCanvasGO.transform.rotation = Quaternion.LookRotation(-dir, Vector3.up);
    }

    // ─────────────────────────────────────────────────────────────
    //  3. Progress Ring
    // ─────────────────────────────────────────────────────────────
    private void BuildProgressRing()
    {
        // Отдельный маленький World Space Canvas для кольца
        _ringRoot = new GameObject("GazeProgressRing");
        _ringRoot.transform.SetParent(transform, false);

        Canvas c = _ringRoot.AddComponent<Canvas>();
        c.renderMode  = RenderMode.WorldSpace;
        c.worldCamera = gazeCamera;

        float px = ringSize * 1000f;
        var crt = _ringRoot.GetComponent<RectTransform>();
        crt.sizeDelta  = new Vector2(px, px);
        crt.localScale = Vector3.one * 0.001f;

        // Фон кольца (серый круг)
        _ringBg = CreateRingImage("RingBg", ringBgColor, 1f);

        // Заполняемое кольцо
        _ringFill = CreateRingImage("RingFill", ringColor, 0f);
        _ringFill.fillMethod = Image.FillMethod.Radial360;
        _ringFill.fillOrigin = (int)Image.Origin360.Top;

        _ringRoot.SetActive(false);
    }

    private Image CreateRingImage(string name, Color color, float fillAmt)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_ringRoot.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color      = color;
        img.type       = Image.Type.Filled;
        img.fillAmount = fillAmt;
        return img;
    }

    private void UpdateProgressRing()
    {
        if (_ringRoot == null) return;

        bool showRing = _currentGazedObject != null && !_labelVisible;
        _ringRoot.SetActive(showRing);

        if (!showRing) return;

        // Позиция: перед камерой (чуть ближе надписи)
        _ringRoot.transform.position = gazeCamera.transform.position
                                     + gazeCamera.transform.forward * ringDistance;
        _ringRoot.transform.rotation = Quaternion.LookRotation(
            _ringRoot.transform.position - gazeCamera.transform.position);

        // Заполнение кольца = прогресс dwell
        float progress = Mathf.Clamp01(_gazeTimer / dwellTime);
        _ringFill.fillAmount = progress;
    }

    // ─────────────────────────────────────────────────────────────
    //  5. Highlight через MaterialPropertyBlock
    // ─────────────────────────────────────────────────────────────
    private void ApplyHighlight(GameObject obj)
    {
        if (_mpb == null) return;
        _highlightedRenderer = obj.GetComponent<Renderer>();
        if (_highlightedRenderer == null) return;

        _highlightedRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_emissionColorID,  highlightColor * highlightIntensity * 2f);
        _mpb.SetFloat(_pulseIntensityID, highlightIntensity);
        _highlightedRenderer.SetPropertyBlock(_mpb);
    }

    private void ClearHighlight()
    {
        if (_highlightedRenderer == null || _mpb == null) return;
        _highlightedRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_emissionColorID,  Color.black);
        _mpb.SetFloat(_pulseIntensityID, 0f);
        _highlightedRenderer.SetPropertyBlock(_mpb);
        _highlightedRenderer = null;
    }

    void OnDisable()
    {
        ClearGaze();
        if (_ringRoot != null) _ringRoot.SetActive(false);
    }
}
