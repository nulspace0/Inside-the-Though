using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Вешай на Bone.007 рядом с XRGrabInteractable.
/// Убирает рывок вращения при захвате.
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabWithoutSnap : MonoBehaviour
{
    private XRGrabInteractable grab;
    private Transform dynamicAttach;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // Создаём пустой дочерний объект как точку захвата
        dynamicAttach = new GameObject("_DynamicAttach").transform;
        dynamicAttach.SetParent(transform);
        grab.attachTransform = dynamicAttach;

        // VelocityTracking — объект плавно следует за рукой,
        // без телепортации в позицию контроллера
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        grab.selectEntered.AddListener(OnGrabbed);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrabbed);
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        // В момент захвата — ставим точку захвата ТУДА ГДЕ НАХОДИТСЯ КОНТРОЛЛЕР
        // Объект остаётся на месте, рывка нет
        Transform controllerAttach = args.interactorObject.GetAttachTransform(grab);
        dynamicAttach.SetPositionAndRotation(
            controllerAttach.position,
            controllerAttach.rotation
        );
    }
}