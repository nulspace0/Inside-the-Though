using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    [Tooltip("Активна ли точка для респауна")]
    public bool isActive = true;

    [Range(0.1f, 2f)]
    [Tooltip("Размер отображаемой сферы в редакторе")]
    public float sphereSize = 0.5f;

    [Tooltip("Слой платформы, на которой находится точка")]
    public LayerMask platformLayer = 1; // По умолчанию все слои

    [Tooltip("Максимальное расстояние до платформы")]
    public float maxPlatformDistance = 1f;

    private void OnDrawGizmos()
    {
        if (!isActive) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, sphereSize);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * maxPlatformDistance);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * maxPlatformDistance);
    }
}