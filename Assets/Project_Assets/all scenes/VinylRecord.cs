using UnityEngine;

public class VinylRecord : MonoBehaviour
{
    [Header("Вращение")]
    public float rpm = 33f;

    [Header("Ось — выбери нужную")]
    public bool rotateX = false;
    public bool rotateY = true;
    public bool rotateZ = false;

    void Update()
    {
        float speed = rpm * 6f * Time.deltaTime;

        if (rotateX) transform.Rotate(speed, 0f, 0f);
        if (rotateY) transform.Rotate(0f, speed, 0f);
        if (rotateZ) transform.Rotate(0f, 0f, speed);
    }
}