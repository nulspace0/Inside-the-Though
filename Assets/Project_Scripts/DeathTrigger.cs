using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRDeathRespawn : MonoBehaviour
{
    [Header("XR-SPECIFIC SETTINGS")]
    [Tooltip("Перетащите сюда ваш XR Origin (обычно 'XR Origin' в сцене)")]
    public XROrigin xrOrigin; // КРИТИЧЕСКИ ВАЖНО!

    [Tooltip("Точка спавна (пустой объект с правильными координатами)")]
    public Transform spawnPoint;

    [Header("Отладка")]
    public bool visualizeSpawnPoint = true;

    void Start()
    {
        // Автоматический поиск XR Origin если не назначен
        if (xrOrigin == null)
        {
            xrOrigin = Object.FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null)
                Debug.Log("✅ Найден XR Origin автоматически");
            else
                Debug.LogError("❌ XR Origin не найден! Перетащите его в инспекторе.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем: попал ли в триггер ЛЮБОЙ дочерний объект XR Rig
        if (!IsPartOfXRRig(other.transform)) return;

        if (spawnPoint == null)
        {
            Debug.LogError("❌ Не указана точка спавна!");
            return;
        }

        RespawnPlayer();
    }

    bool IsPartOfXRRig(Transform obj)
    {
        // Проверяем всю иерархию на наличие XR Origin
        Transform current = obj;
        while (current != null)
        {
            if (current == xrOrigin.transform) return true;
            current = current.parent;
        }
        return false;
    }

    void RespawnPlayer()
    {
        // ===== ГЛАВНЫЙ СЕКРЕТ XR =====
        // Всегда перемещаем CAMERA FLOOR OFFSET, а не сам XR Origin!
        Transform cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;

        // Фиксация всех возможных физических артефактов
        xrOrigin.Camera.transform.position = spawnPoint.position;
        xrOrigin.Camera.transform.rotation = spawnPoint.rotation;

        cameraOffset.position = spawnPoint.position;
        cameraOffset.rotation = spawnPoint.rotation;

        // Сбрасываем встроенные системы XR
        if (xrOrigin.GetComponent<CharacterController>())
            xrOrigin.GetComponent<CharacterController>().enabled = false;

        // Принудительно обновляем позицию
        xrOrigin.RequestedTrackingOriginMode = xrOrigin.RequestedTrackingOriginMode;

        if (xrOrigin.GetComponent<CharacterController>())
            xrOrigin.GetComponent<CharacterController>().enabled = true;

        // Дополнительная страховка для VR
        if (xrOrigin.Camera != null)
        {
            xrOrigin.Camera.transform.localPosition = Vector3.zero;
            xrOrigin.Camera.transform.localRotation = Quaternion.identity;
        }

        Debug.Log($"✨ XR Player возрожден в: {spawnPoint.position}");
    }

    // Визуализация точки в редакторе
    void OnDrawGizmos()
    {
        if (!visualizeSpawnPoint || spawnPoint == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + Vector3.up * 2f);
    }
}