using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class GearRotator : MonoBehaviour
{
    [Header("Objects to Rotate")]
    public Transform targetObject; // Объект, который будет вращаться по оси Y

    [Header("Rotation Settings")]
    public float rotationMultiplier = 1f; // Множитель вращения (1 - вращается синхронно, >1 - быстрее, <1 - медленнее)
    public bool lockOtherAxes = true; // Блокировать вращение по другим осям у целевого объекта

    private Rigidbody rb;
    private Vector3 previousPosition;
    private float targetRotationY;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        previousPosition = GetGripPosition();
    }

    private void Update()
    {
        if (IsBeingInteracted())
        {
            UpdateTargetRotation();
        }
    }

    private Vector3 GetGripPosition()
    {
        // Получаем позицию центра масс ригидбоди, чтобы отслеживать вращение
        return rb.worldCenterOfMass;
    }

    private bool IsBeingInteracted()
    {
        // Проверяем, взаимодействует ли с нами XR Grab Interactable
        var grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            return grabInteractable.isSelected;
        }
        return false;
    }

    private void UpdateTargetRotation()
    {
        Vector3 currentPosition = GetGripPosition();
        Vector3 delta = currentPosition - previousPosition;

        // Вычисляем, насколько повернулся текущий объект
        // Предполагаем, что вращение происходит в плоскости, где изменение X и Z даёт вращение вокруг Y
        Vector3 currentForward = transform.forward;
        Vector3 projectedForward = new Vector3(currentForward.x, 0, currentForward.z).normalized;
        if (projectedForward.sqrMagnitude == 0) projectedForward = Vector3.forward;

        float angleDelta = Vector3.SignedAngle(transform.forward, projectedForward, transform.up);
        float additionalRotation = angleDelta * rotationMultiplier;

        targetRotationY += additionalRotation;

        // Применяем вращение к целевому объекту
        if (targetObject != null)
        {
            Vector3 newRotation = targetObject.localEulerAngles;
            newRotation.y = targetRotationY;

            if (lockOtherAxes)
            {
                newRotation.x = 0f;
                newRotation.z = 0f;
            }

            targetObject.localEulerAngles = newRotation;
        }

        previousPosition = currentPosition;
    }
}