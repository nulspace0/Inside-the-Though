using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class LookAtCamera : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;

    private void Update()
    {
        if (cameraTransform != null)
        {
            transform.LookAt(cameraTransform);
            transform.Rotate(0, 180, 0); // Коррекция ориентации
        }
    }
}