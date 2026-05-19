using UnityEngine;

// Управляет параметрами LazyFog материала через MaterialPropertyBlock
// (не изменяет shared material — безопасно для нескольких объектов)
public class exampleSceneScript : MonoBehaviour
{
    [Header("LazyFog параметры")]
    public float scale     = 0.8f;
    public float intensity = 0.5f;   // было 5.25 — слишком ярко!
    public float alpha     = 0.45f;
    public float alphasub  = 0.05f;
    public float pow       = 1.2f;
    public Color color     = new Color(0.55f, 0.75f, 1f, 1f); // голубоватый

    private Renderer          _renderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat("_Scale",     scale);
        _mpb.SetFloat("_Intensity", intensity);
        _mpb.SetFloat("_Alpha",     alpha);
        _mpb.SetFloat("_AlphaSub",  alphasub);
        _mpb.SetFloat("_Pow",       pow);
        _mpb.SetColor("_Color",     color);
        _renderer.SetPropertyBlock(_mpb);
    }

    // OnGUI() удалён — больше нет слайдеров поверх экрана
}
