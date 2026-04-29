using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Добавь этот компонент на объект с XRGrabInteractable.
/// Объект будет хвататься по ТРИГГЕРУ (а не грипу) — работает на любом шлеме.
///
/// Установка:
///   1. Добавь GrabByTrigger на объект рядом с XRGrabInteractable
///   2. Убедись что на XRDirectInteractor / XRRayInteractor есть Collider
/// </summary>
[RequireComponent(typeof(XRGrabInteractable))]
public class GrabByTrigger : MonoBehaviour
{
    [Tooltip("Порог нажатия триггера (0–1)")]
    [Range(0.1f, 0.9f)]
    public float triggerThreshold = 0.5f;

    private XRGrabInteractable _grab;
    private XRInteractionManager _interactionManager;

    // Триггеры — создаём в коде, без Inspector
    private InputAction _leftTrigger;
    private InputAction _rightTrigger;

    // Interactor который сейчас наводится на объект
    private IXRHoverInteractor _hoveringInteractor;

    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();

        // Отключаем стандартный select по грипу —
        // вместо этого мы сами вызываем Select по триггеру
        _grab.selectMode = InteractableSelectMode.Single;

        // Слушаем hover чтобы знать каким контроллером навели
        _grab.hoverEntered.AddListener(OnHoverEnter);
        _grab.hoverExited.AddListener(OnHoverExit);

        _interactionManager = FindAnyObjectByType<XRInteractionManager>();

        CreateTriggerActions();
    }

    private void CreateTriggerActions()
    {
        // Левый триггер — Meta Quest / HTC Vive / любой OpenXR контроллер
        _leftTrigger = new InputAction("LeftTrigger", InputActionType.Value);
        _leftTrigger.AddBinding("<XRController>{LeftHand}/trigger");
        _leftTrigger.Enable();

        // Правый триггер
        _rightTrigger = new InputAction("RightTrigger", InputActionType.Value);
        _rightTrigger.AddBinding("<XRController>{RightHand}/trigger");
        _rightTrigger.Enable();
    }

    private void Update()
    {
        if (_hoveringInteractor == null) return;

        float leftVal  = _leftTrigger.ReadValue<float>();
        float rightVal = _rightTrigger.ReadValue<float>();

        bool leftPressed  = leftVal  >= triggerThreshold;
        bool rightPressed = rightVal >= triggerThreshold;

        bool triggerPressed = leftPressed || rightPressed;

        bool alreadySelected = _grab.isSelected;

        if (triggerPressed && !alreadySelected)
        {
            if (_interactionManager != null && _hoveringInteractor is IXRSelectInteractor selectInteractor)
                _interactionManager.SelectEnter(selectInteractor, (IXRSelectInteractable)_grab);
        }
        else if (!triggerPressed && alreadySelected)
        {
            if (_interactionManager != null && _grab.interactorsSelecting.Count > 0)
            {
                var interactor = _grab.interactorsSelecting[0];
                _interactionManager.SelectExit(interactor, (IXRSelectInteractable)_grab);
            }
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args) => _hoveringInteractor = args.interactorObject;
    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (args.interactorObject == _hoveringInteractor)
            _hoveringInteractor = null;
    }

    private void OnDestroy()
    {
        _leftTrigger?.Disable();  _leftTrigger?.Dispose();
        _rightTrigger?.Disable(); _rightTrigger?.Dispose();

        if (_grab != null)
        {
            _grab.hoverEntered.RemoveListener(OnHoverEnter);
            _grab.hoverExited.RemoveListener(OnHoverExit);
        }
    }
}
