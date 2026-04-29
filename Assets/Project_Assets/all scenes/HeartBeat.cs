using UnityEngine;

public class HeartBeat : MonoBehaviour
{
    [Header("Настройки")]
    public float bpm = 70f;
    public float beatScale = 1.2f;
    public float restScale = 1.0f;

    private float beatDuration;
    private float timer;
    private bool isBeating;

    void Start()
    {
        beatDuration = 60f / bpm;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Два удара подряд как настоящее сердце — ТУК-ТУК ... ТУК-ТУК
        float t = timer % beatDuration;

        float scale;

        if (t < 0.1f)
            scale = Mathf.Lerp(restScale, beatScale, t / 0.1f);       // первый удар вверх
        else if (t < 0.2f)
            scale = Mathf.Lerp(beatScale, restScale, (t - 0.1f) / 0.1f); // первый удар вниз
        else if (t < 0.3f)
            scale = Mathf.Lerp(restScale, beatScale * 0.9f, (t - 0.2f) / 0.1f); // второй удар вверх
        else if (t < 0.4f)
            scale = Mathf.Lerp(beatScale * 0.9f, restScale, (t - 0.3f) / 0.1f); // второй удар вниз
        else
            scale = restScale;                                          // пауза до следующего удара

        transform.localScale = new Vector3(scale, scale, scale);
    }
}