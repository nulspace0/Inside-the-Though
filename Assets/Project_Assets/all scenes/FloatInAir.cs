using UnityEngine;

public class FloatInAir : MonoBehaviour
{
    [Header("Покачивание вверх-вниз")]
    public float floatHeight = 0.15f;
    public float floatSpeed = 1f;

    [Header("Покачивание туда-сюда")]
    public float swingAngle = 100f;   // градусов в каждую сторону
    public float swingSpeed = 0.8f;   // скорость покачивания

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // Плавное покачивание вверх-вниз
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Покачивание туда-сюда по Y
        float angle = Mathf.Sin(Time.time * swingSpeed) * swingAngle;
        transform.rotation = startRotation * Quaternion.Euler(0f, angle, 0f);
    }
}