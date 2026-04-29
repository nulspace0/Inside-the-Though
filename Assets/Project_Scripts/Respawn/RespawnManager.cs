using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Настройки")]
    [Tooltip("Высота над поверхностью платформы куда ставим игрока")]
    public float playerHeightOffset = 0.1f;

    [Tooltip("Слой платформ — должен совпадать с Layer платформ в сцене")]
    public LayerMask platformLayerMask = 1;

    [Tooltip("На сколько выше точки респауна начинать рейкаст вниз")]
    public float raycastStartHeight = 5f;

    [Tooltip("Максимальная дистанция рейкаста")]
    public float raycastMaxDistance = 10f;

    private List<RespawnPoint> _activePoints = new List<RespawnPoint>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        FindRespawnPoints();
        SceneManager.sceneLoaded += (s, m) => FindRespawnPoints();
    }

    private void FindRespawnPoints()
    {
        _activePoints.Clear();
        foreach (var p in FindObjectsByType<RespawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (p.isActive && p.gameObject.activeInHierarchy)
                _activePoints.Add(p);

        Debug.Log($"[RespawnManager] Точек респауна: {_activePoints.Count}");
    }

    /// <summary>Возвращает позицию ближайшей точки с платформой под ней.</summary>
    public Vector3 GetBestRespawnPosition()
    {
        if (_activePoints.Count == 0)
        {
            Debug.LogError("[RespawnManager] Нет активных точек респауна!");
            return Vector3.zero;
        }

        // Берём первую точку у которой под ней есть платформа
        foreach (RespawnPoint point in _activePoints)
        {
            Vector3 pos = FindPlatformBelow(point.transform.position);
            if (pos != Vector3.zero) return pos;
        }

        // Запасной вариант — просто позиция первой точки + смещение
        Debug.LogWarning("[RespawnManager] Платформа не найдена ни под одной точкой, используем fallback");
        return _activePoints[0].transform.position + Vector3.up * playerHeightOffset;
    }

    /// <summary>Ищет платформу под точкой. Возвращает Vector3.zero если не нашёл.</summary>
    private Vector3 FindPlatformBelow(Vector3 pointPos)
    {
        // Стартуем выше точки чтобы гарантированно не промахнуться
        Vector3 rayOrigin = pointPos + Vector3.up * raycastStartHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
            raycastStartHeight + raycastMaxDistance, platformLayerMask))
        {
            Vector3 result = new Vector3(pointPos.x, hit.point.y + playerHeightOffset, pointPos.z);
            Debug.Log($"[RespawnManager] Платформа: {hit.collider.name} Y={hit.point.y:F2} → игрок Y={result.y:F2}");
            return result;
        }

        Debug.LogWarning($"[RespawnManager] Платформа не найдена под точкой {pointPos} (layer mask={platformLayerMask.value})");
        return Vector3.zero;
    }

    // Оставляем для совместимости со старым кодом
    public Vector3 GetAdjustedRespawnPosition(Vector3 _) => GetBestRespawnPosition();
    public List<RespawnPoint> GetActivePoints() => _activePoints;
    public LayerMask GetPlatformLayerMask() => platformLayerMask;

    private void OnDrawGizmosSelected()
    {
        // Визуализация рейкастов в Editor
        foreach (var point in FindObjectsByType<RespawnPoint>(FindObjectsSortMode.None))
        {
            Vector3 from = point.transform.position + Vector3.up * raycastStartHeight;
            Vector3 to   = point.transform.position - Vector3.up * raycastMaxDistance;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireSphere(from, 0.1f);
        }
    }
}
