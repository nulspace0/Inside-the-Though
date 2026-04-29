using UnityEngine;

public class LockRotationToZ : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // Ѕлокируем вращение по X и Y, оставл€€ только Z
        Quaternion currentRotation = rb.rotation;
        Vector3 eulerAngles = currentRotation.eulerAngles;

        // ќставл€ем только Z-угол, остальные обнул€ем
        eulerAngles.x = 0f;
        eulerAngles.y = 0f;

        rb.MoveRotation(Quaternion.Euler(eulerAngles));
    }
}