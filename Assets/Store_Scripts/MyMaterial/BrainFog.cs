using UnityEngine;

public class BrainFog : MonoBehaviour
{
    void Awake()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.03f;
        RenderSettings.fogColor = new Color(0.05f, 0.02f, 0.12f);
    }
}