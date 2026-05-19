using UnityEngine;

public class RotateY : MonoBehaviour
{
    [SerializeField] private float speed = 30f; // градусов в секунду

    private void Update()
    {
        transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }
}
