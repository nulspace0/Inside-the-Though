// SceneVisualSettings.cs
// Динамически настраивает пост-процессинг под атмосферу каждой сцены
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SceneVisualSettings : MonoBehaviour
{
    public enum SceneMood { Brain, Cerebellum, Brainstem, Frontal, Parietal, Temporal }

    [Header("Атмосфера сцены")]
    public SceneMood mood = SceneMood.Brain;

    [Header("Ссылки")]
    public Volume globalVolume;

    // Компоненты Volume
    private Bloom            _bloom;
    private ColorAdjustments _color;
    private Vignette         _vignette;
    private SplitToning      _split;
    private FilmGrain        _grain;

    // Настройки для каждой сцены
    struct MoodPreset
    {
        public float bloomIntensity, bloomThreshold, bloomScatter;
        public float exposure, contrast, saturation;
        public float vignetteIntensity;
        public Color splitShadows, splitHighlights;
        public float splitBalance;
        public Color bloomTint;
    }

    static readonly MoodPreset[] Presets = new[]
    {
        // Brain — холодно-синий, загадочный
        new MoodPreset {
            bloomIntensity=2.2f, bloomThreshold=0.85f, bloomScatter=0.72f,
            exposure=0.3f, contrast=20f, saturation=22f, vignetteIntensity=0.30f,
            splitShadows=new Color(0.47f,0.49f,0.56f), splitHighlights=new Color(0.52f,0.51f,0.47f),
            splitBalance=15f, bloomTint=new Color(0.8f,0.9f,1f)
        },
        // Cerebellum — фиолетово-синий, органичный
        new MoodPreset {
            bloomIntensity=2.8f, bloomThreshold=0.80f, bloomScatter=0.75f,
            exposure=0.25f, contrast=22f, saturation=28f, vignetteIntensity=0.35f,
            splitShadows=new Color(0.45f,0.47f,0.58f), splitHighlights=new Color(0.54f,0.50f,0.48f),
            splitBalance=20f, bloomTint=new Color(0.7f,0.8f,1f)
        },
        // Brainstem — тёплый, оранжевый, энергичный
        new MoodPreset {
            bloomIntensity=2.0f, bloomThreshold=0.88f, bloomScatter=0.65f,
            exposure=0.2f, contrast=18f, saturation=25f, vignetteIntensity=0.28f,
            splitShadows=new Color(0.48f,0.46f,0.54f), splitHighlights=new Color(0.55f,0.52f,0.46f),
            splitBalance=10f, bloomTint=new Color(1f,0.9f,0.8f)
        },
        // Frontal — яркий, синий, активный
        new MoodPreset {
            bloomIntensity=3.0f, bloomThreshold=0.78f, bloomScatter=0.78f,
            exposure=0.35f, contrast=24f, saturation=30f, vignetteIntensity=0.25f,
            splitShadows=new Color(0.46f,0.48f,0.58f), splitHighlights=new Color(0.53f,0.51f,0.46f),
            splitBalance=18f, bloomTint=new Color(0.75f,0.85f,1f)
        },
        // Parietal — нейтральный, белый, чистый
        new MoodPreset {
            bloomIntensity=1.8f, bloomThreshold=0.92f, bloomScatter=0.68f,
            exposure=0.15f, contrast=16f, saturation=18f, vignetteIntensity=0.22f,
            splitShadows=new Color(0.49f,0.49f,0.53f), splitHighlights=new Color(0.52f,0.51f,0.49f),
            splitBalance=8f, bloomTint=new Color(0.9f,0.92f,1f)
        },
        // Temporal — тёплый фиолетово-розовый
        new MoodPreset {
            bloomIntensity=2.5f, bloomThreshold=0.82f, bloomScatter=0.73f,
            exposure=0.28f, contrast=21f, saturation=26f, vignetteIntensity=0.32f,
            splitShadows=new Color(0.46f,0.46f,0.56f), splitHighlights=new Color(0.55f,0.50f,0.50f),
            splitBalance=12f, bloomTint=new Color(0.85f,0.78f,1f)
        },
    };

    void Start()
    {
        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume == null || !globalVolume.profile) return;

        var p = globalVolume.profile;
        p.TryGet(out _bloom);
        p.TryGet(out _color);
        p.TryGet(out _vignette);
        p.TryGet(out _split);
        p.TryGet(out _grain);

        ApplyPreset(Presets[(int)mood]);
    }

    void ApplyPreset(MoodPreset m)
    {
        if (_bloom != null)
        {
            _bloom.intensity.Override(m.bloomIntensity);
            _bloom.threshold.Override(m.bloomThreshold);
            _bloom.scatter.Override(m.bloomScatter);
            _bloom.tint.Override(m.bloomTint);
            _bloom.highQualityFiltering.Override(true);
        }
        if (_color != null)
        {
            _color.postExposure.Override(m.exposure);
            _color.contrast.Override(m.contrast);
            _color.saturation.Override(m.saturation);
        }
        if (_vignette != null)
        {
            _vignette.intensity.Override(m.vignetteIntensity);
            _vignette.smoothness.Override(0.4f);
        }
        if (_split != null)
        {
            _split.shadows.Override(m.splitShadows);
            _split.highlights.Override(m.splitHighlights);
            _split.balance.Override(m.splitBalance);
        }
        if (_grain != null)
        {
            _grain.intensity.Override(0.03f);
            _grain.response.Override(0.75f);
        }
    }
}
