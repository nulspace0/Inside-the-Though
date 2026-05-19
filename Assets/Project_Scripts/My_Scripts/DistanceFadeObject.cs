using UnityEngine;
using TMPro;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(BoxCollider))]
public class DistanceFadeTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private float fadeSpeed = 2f;

    private BoxCollider _box;
    private Transform   _cam;
    private float       _alpha;

    private void Awake()
    {
        _box           = GetComponent<BoxCollider>();
        _box.isTrigger = true;

        if (textComponent == null)
            Debug.LogWarning($"[DistanceFadeTextUI] {name}: не назначен Text Component");

        SetAlpha(0f);
        if (textComponent != null) textComponent.enabled = false;
    }

    private void Start()
    {
        var xrOrigin = FindAnyObjectByType<XROrigin>();
        if (xrOrigin != null)
            _cam = xrOrigin.Camera.transform;
        else if (Camera.main != null)
            _cam = Camera.main.transform;
        else
            Debug.LogWarning($"[DistanceFadeTextUI] {name}: камера не найдена");
    }

    private void Update()
    {
        if (textComponent == null || _cam == null) return;

        // Проверяем позицию камеры против границ бокса — надёжнее OnTriggerExit в VR
        bool inside = _box.bounds.Contains(_cam.position);
        float target = inside ? 1f : 0f;
        _alpha = Mathf.MoveTowards(_alpha, target, fadeSpeed * Time.deltaTime);

        if (_alpha > 0f)
        {
            textComponent.enabled = true;
            SetAlpha(_alpha);
        }
        else
        {
            SetAlpha(0f);
            textComponent.enabled = false;
        }
    }

    private void SetAlpha(float a)
    {
        if (textComponent == null) return;
        var c = textComponent.color;
        c.a = a;
        textComponent.color = c;
    }
}
