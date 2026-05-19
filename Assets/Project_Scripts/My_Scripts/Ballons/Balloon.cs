using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Balloon : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatAmplitude = 0.15f;
    public float floatSpeed = 1.2f;
    public float driftSpeed = 0.3f;

    [Header("Fact")]
    [TextArea(2, 4)]
    public string fact = "Мозг потребляет 20% всей энергии тела!";

    [Header("Visual")]
    public Gradient colorOverLifetime;
    public float lifetime = 30f;

    private Vector3 startPos;
    private float randomOffset;
    private Renderer balloonRenderer;
    private bool isPopped = false;

    void Start()
    {
        startPos = transform.position;
        randomOffset = Random.Range(0f, Mathf.PI * 2f);
        balloonRenderer = GetComponent<Renderer>();

        // Случайный цвет из градиента
        if (balloonRenderer != null)
            balloonRenderer.material.color = colorOverLifetime.Evaluate(Random.value);

        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (isPopped) return;

        // Плавное покачивание вверх-вниз
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
        // Лёгкий дрейф по X/Z
        float driftX = Mathf.Sin(Time.time * driftSpeed * 0.7f + randomOffset) * 0.1f;
        float driftZ = Mathf.Cos(Time.time * driftSpeed * 0.5f + randomOffset) * 0.1f;

        transform.position = new Vector3(
            startPos.x + driftX,
            newY,
            startPos.z + driftZ
        );
    }

    public void Pop()
    {
        if (isPopped) return;
        isPopped = true;

        // Вызываем эффект
        BalloonPopEffect.Instance?.PlayPop(transform.position, fact, balloonRenderer?.material.color ?? Color.white);

        Destroy(gameObject);
    }

    // Поддержка VR-контроллеров (XR Toolkit)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") || other.CompareTag("Finger"))
            Pop();
    }
}