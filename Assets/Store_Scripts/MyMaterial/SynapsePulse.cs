using System.Collections;
using UnityEngine;

/// <summary>
/// SynapsePulse — управляет пульсацией нейронного материала и вспышками синапса.
///
/// КАК ПОДКЛЮЧИТЬ:
///   1. Повесь этот скрипт на GameObject с MeshRenderer (нейрон/дендрит)
///   2. В Inspector заполни поля (см. комментарии ниже)
///   3. Запусти сцену — нейрон начнёт пульсировать
/// </summary>
public class SynapsePulse : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════
    //  НАСТРОЙКИ В INSPECTOR
    // ═══════════════════════════════════════════════════════════════

    [Header("── Базовая пульсация ──────────────────────")]

    [Tooltip("Скорость пульсации (удары в секунду). 0.5 = медленно, 2.0 = быстро")]
    [Range(0.1f, 3f)]
    public float pulseSpeed = 0.8f;

    [Tooltip("Сила пульсации. 0 = нет эффекта, 1 = максимум")]
    [Range(0f, 1f)]
    public float pulseStrength = 0.6f;

    [Tooltip("Случайный сдвиг фазы — чтобы соседние нейроны не мигали синхронно")]
    [Range(0f, 6.28f)]
    public float phaseOffset = 0f;

    [Space]
    [Header("── Вспышка синапса ──────────────────────────")]

    [Tooltip("Включить случайные вспышки (имитация передачи сигнала)")]
    public bool enableSynapseFlash = true;

    [Tooltip("Минимальное время между вспышками (секунды)")]
    [Range(0.5f, 10f)]
    public float flashIntervalMin = 1.5f;

    [Tooltip("Максимальное время между вспышками (секунды)")]
    [Range(0.5f, 15f)]
    public float flashIntervalMax = 5f;

    [Tooltip("Длительность одной вспышки (секунды). Рекомендую 0.1–0.3")]
    [Range(0.05f, 0.5f)]
    public float flashDuration = 0.15f;

    [Tooltip("Яркость вспышки. ОСТОРОЖНО в VR — не больше 2.5!")]
    [Range(0f, 2.5f)]
    public float flashIntensity = 2.0f;

    [Space]
    [Header("── Частицы (необязательно) ─────────────────")]

    [Tooltip("Particle System в точке синапса. Можно оставить пустым.")]
    public ParticleSystem synapseParticles;

    [Tooltip("Сколько частиц выпустить при вспышке")]
    [Range(1, 30)]
    public int flashParticleCount = 8;

    [Space]
    [Header("── Цвет вспышки ──────────────────────────────")]

    [Tooltip("Цвет во время вспышки синапса")]
    public Color flashColor = new Color(1f, 0.95f, 0.7f);   // тёплый белый

    [Tooltip("Обычный цвет emission (должен совпадать с материалом)")]
    public Color baseEmissionColor = new Color(0f, 0.8f, 1f); // cyan

    // ═══════════════════════════════════════════════════════════════
    //  ПРИВАТНЫЕ ПЕРЕМЕННЫЕ (не трогать)
    // ═══════════════════════════════════════════════════════════════

    private Renderer       _renderer;
    private MaterialPropertyBlock _mpb;          // Быстрее чем material.SetFloat!
    private bool           _isFlashing = false;
    private float          _currentEmission = 0f;

    // ID свойств шейдера (кэшируем для производительности)
    private static readonly int _PulseIntensityID   = Shader.PropertyToID("_PulseIntensity");
    private static readonly int _EmissionColorID    = Shader.PropertyToID("_EmissionColor");

    // ═══════════════════════════════════════════════════════════════
    //  UNITY LIFECYCLE
    // ═══════════════════════════════════════════════════════════════

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();

        // Случайный сдвиг фазы если не задан вручную
        if (phaseOffset == 0f)
            phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Start()
    {
        if (enableSynapseFlash)
            StartCoroutine(SynapseFlashRoutine());
    }

    void Update()
    {
        if (!_isFlashing)
            UpdateBasePulse();
    }

    // ═══════════════════════════════════════════════════════════════
    //  БАЗОВАЯ ПУЛЬСАЦИЯ (каждый кадр)
    // ═══════════════════════════════════════════════════════════════

    void UpdateBasePulse()
    {
        // Синусоида: плавно колеблется от 0 до pulseStrength
        float sineValue = Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f + phaseOffset);
        _currentEmission = Mathf.InverseLerp(-1f, 1f, sineValue) * pulseStrength;

        // Применяем через MaterialPropertyBlock (без создания копии материала!)
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(_PulseIntensityID,     _currentEmission);
        _mpb.SetColor(_EmissionColorID,      baseEmissionColor);
        _renderer.SetPropertyBlock(_mpb);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ВСПЫШКА СИНАПСА (корутина)
    // ═══════════════════════════════════════════════════════════════

    IEnumerator SynapseFlashRoutine()
    {
        // Небольшая случайная задержка при старте
        yield return new WaitForSeconds(Random.Range(0f, flashIntervalMax));

        while (true)
        {
            // ── Ждём случайный интервал ─────────────────────────
            float waitTime = Random.Range(flashIntervalMin, flashIntervalMax);
            yield return new WaitForSeconds(waitTime);

            // ── Вспышка ─────────────────────────────────────────
            yield return StartCoroutine(DoFlash());
        }
    }

    IEnumerator DoFlash()
    {
        _isFlashing = true;

        // Выпускаем частицы
        if (synapseParticles != null)
        {
            var emit = new ParticleSystem.EmitParams();
            synapseParticles.Emit(emit, flashParticleCount);
        }

        // ── Нарастание (attack) ──────────────────────────────────
        float attackTime = flashDuration * 0.2f;   // 20% времени — нарастание
        float decayTime  = flashDuration * 0.8f;   // 80% времени — спад

        // Attack
        float t = 0f;
        while (t < attackTime)
        {
            t += Time.deltaTime;
            float progress = t / attackTime;
            ApplyFlashColor(progress);
            yield return null;
        }

        // Decay (плавный спад)
        t = 0f;
        while (t < decayTime)
        {
            t += Time.deltaTime;
            float progress = 1f - (t / decayTime);   // от 1 до 0
            ApplyFlashColor(progress);
            yield return null;
        }

        // Возвращаем базовые настройки
        _isFlashing = false;
    }

    void ApplyFlashColor(float progress)
    {
        // Интерполируем между базовым цветом и цветом вспышки
        Color currentColor = Color.Lerp(baseEmissionColor, flashColor, progress);
        float intensity     = Mathf.Lerp(_currentEmission, flashIntensity, progress);

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_EmissionColorID,   currentColor);
        _mpb.SetFloat(_PulseIntensityID,  intensity);
        _renderer.SetPropertyBlock(_mpb);
    }

    // ═══════════════════════════════════════════════════════════════
    //  ПУБЛИЧНЫЕ МЕТОДЫ (вызывай из других скриптов)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Вызови из другого скрипта чтобы принудительно вспыхнуть.
    /// Например: synapsePulse.TriggerFlash();
    /// </summary>
    public void TriggerFlash()
    {
        if (!_isFlashing)
            StartCoroutine(DoFlash());
    }

    /// <summary>
    /// Поменять скорость пульсации во время игры.
    /// Например при приближении игрока.
    /// </summary>
    public void SetPulseSpeed(float speed)
    {
        pulseSpeed = Mathf.Clamp(speed, 0.1f, 5f);
    }
}
