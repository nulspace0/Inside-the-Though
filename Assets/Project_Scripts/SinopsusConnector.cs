using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SinopsisConnector : MonoBehaviour
{
    public Transform connectionPoint; // Точка, куда должен прикрепиться другой синопсис
    public GameObject hiddenObjectsToActivate; // Объекты, которые нужно активировать после соединения

    private XRGrabInteractable grabInteractable;
    private bool isConnected = false;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable == null)
        {
            Debug.LogError("XRGrabInteractable не найден на объекте: " + name);
            return;
        }

        // Добавляем обработчики событий
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        // Проверяем, не соединены ли уже объекты
        if (isConnected) return;

        // Проверяем, является ли захватываемый объект другим синопсисом
        if (args.interactorObject is XRDirectInteractor directInteractor)
        {
            var grabbedObject = directInteractor.gameObject;
            if (grabbedObject.CompareTag("Sinopsis"))
            {
                // Находим компонент SinopsisConnector у захваченного объекта
                var otherConnector = grabbedObject.GetComponent<SinopsisConnector>();
                if (otherConnector != null && !otherConnector.isConnected)
                {
                    // Прикрепляем объект к точке соединения
                    grabbedObject.transform.SetParent(connectionPoint);
                    grabbedObject.transform.localPosition = Vector3.zero;
                    grabbedObject.transform.localRotation = Quaternion.identity;

                    // Отключаем гравитацию и физику, чтобы объекты не разлетались
                    var rb = grabbedObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                    }

                    // Устанавливаем флаг соединения
                    isConnected = true;
                    otherConnector.isConnected = true;

                    // Активируем скрытые объекты
                    if (hiddenObjectsToActivate != null)
                    {
                        hiddenObjectsToActivate.SetActive(true);
                    }

                    Debug.Log("Синопсисы соединены!");
                }
            }
        }
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        // Опционально: можно добавить логику отсоединения, если нужно
    }
}