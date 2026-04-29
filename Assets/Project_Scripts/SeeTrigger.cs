using UnityEngine;

[RequireComponent(typeof(Collider))] // Убедимся, что на объекте есть коллайдер
public class GazeLabelTrigger : MonoBehaviour
{
    [Tooltip("Камера, из которой исходит луч взгляда. Обычно MainCamera или камера VR-головы.")]
    public Camera mainCamera;

    // Ссылка на GameObject с компонентом Canvas (например, 'LabelCanvas').
    private GameObject labelCanvasGO;

    // Интервал между проверками взгляда (в секундах)
    private const float CheckInterval = 0.2f;

    void Start()
    {
        if (mainCamera == null)
        {
            // Пытаемся найти основную камеру, если она не установлена вручную
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("GazeLabelTrigger: Не найдена основная камера! Установите её вручную в поле mainCamera.");
                enabled = false; // Отключаем скрипт, если нет камеры
                return;
            }
        }

        // --- ИСПРАВЛЕННЫЙ БЛОК ПОИСКА CANVAS (GameObject) ---
        // Ищем дочерний GameObject с компонентом Canvas рекурсивно, включая неактивные
        Transform canvasTransform = FindCanvasGameObject(transform);
        if (canvasTransform == null)
        {
            Debug.LogError($"GazeLabelTrigger на {gameObject.name}: Не найден GameObject с компонентом Canvas среди дочерних элементов (включая неактивные)!");
            enabled = false; // Отключаем скрипт, если нет Canvas GameObject
            return;
        }

        labelCanvasGO = canvasTransform.gameObject; // Сохраняем ссылку на GameObject, а не на компонент
        // --- КОНЕЦ ИСПРАВЛЕННОГО БЛОКА ---

        // Убедимся, что надпись изначально скрыта (через активность родительского Canvas GO)
        labelCanvasGO.SetActive(false);

        // Запускаем корутину для периодической проверки
        InvokeRepeating(nameof(CheckGaze), 0f, CheckInterval);
    }

    // Вспомогательный метод для поиска GameObject, содержащего Canvas, рекурсивно, включая неактивные объекты
    private Transform FindCanvasGameObject(Transform parent)
    {
        // Проверяем самого родителя
        if (parent.GetComponent<Canvas>() != null)
        {
            return parent; // Нашли Canvas на текущем уровне
        }

        // Перебираем детей
        foreach (Transform child in parent)
        {
            Transform result = FindCanvasGameObject(child); // Рекурсивный вызов
            if (result != null)
            {
                return result; // Нашли в одном из поддеревьев
            }
        }

        return null; // Не нашли
    }


    /// <summary>
    /// Метод, вызываемый по таймеру для проверки, смотрит ли камера на этот объект.
    /// </summary>
    private void CheckGaze()
    {
        if (mainCamera == null || labelCanvasGO == null) return; // Проверяем, всё ли ещё валидно

        // Выпускаем луч из центра камеры вперёд
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        RaycastHit hit;

        // Проверяем, попал ли луч в этот объект (или его коллайдер)
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Если луч попал в этот объект (его коллайдер)
            if (hit.collider.gameObject == gameObject)
            {
                // Показать надпись (активируем Canvas GO)
                ShowLabel();
            }
            else
            {
                // Если луч не попал в этот объект (или попал в другой)
                // Скрыть надпись (деактивируем Canvas GO)
                HideLabel();
            }
        }
        else
        {
            // Если луч никуда не попал
            HideLabel();
        }
    }

    private void ShowLabel()
    {
        if (labelCanvasGO != null && !labelCanvasGO.activeSelf)
        {
            labelCanvasGO.SetActive(true);
            //Debug.Log("Надпись показана для " + gameObject.name);
        }
    }

    private void HideLabel()
    {
        if (labelCanvasGO != null && labelCanvasGO.activeSelf)
        {
            labelCanvasGO.SetActive(false);
            //Debug.Log("Надпись скрыта для " + gameObject.name);
        }
    }

    // Опционально: Отменить вызов корутины при деактивации объекта или уничтожении скрипта
    void OnDisable()
    {
        CancelInvoke(nameof(CheckGaze));
    }
}