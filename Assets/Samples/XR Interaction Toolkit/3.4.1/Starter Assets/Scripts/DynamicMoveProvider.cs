using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    public class DynamicMoveProvider : ContinuousMoveProvider
    {
        public enum MovementDirection
        {
            HeadRelative,
            HandRelative,
        }

        [Space, Header("Movement Direction")]
        [SerializeField]
        [Tooltip("Directs the XR Origin's movement when using the head-relative mode.")]
        Transform m_HeadTransform;
        public Transform headTransform
        {
            get => m_HeadTransform;
            set => m_HeadTransform = value;
        }

        [SerializeField]
        Transform m_LeftControllerTransform;
        public Transform leftControllerTransform
        {
            get => m_LeftControllerTransform;
            set => m_LeftControllerTransform = value;
        }

        [SerializeField]
        Transform m_RightControllerTransform;
        public Transform rightControllerTransform
        {
            get => m_RightControllerTransform;
            set => m_RightControllerTransform = value;
        }

        [SerializeField]
        MovementDirection m_LeftHandMovementDirection;
        public MovementDirection leftHandMovementDirection
        {
            get => m_LeftHandMovementDirection;
            set => m_LeftHandMovementDirection = value;
        }

        [SerializeField]
        MovementDirection m_RightHandMovementDirection;
        public MovementDirection rightHandMovementDirection
        {
            get => m_RightHandMovementDirection;
            set => m_RightHandMovementDirection = value;
        }

        [Space, Header("Input Smoothing")]
        [SerializeField, Tooltip("Время разгона/торможения стика (сек). 0 = без сглаживания)")]
        float m_InputSmoothTime = 0.12f;

        Transform m_CombinedTransform;
        Pose m_LeftMovementPose = Pose.identity;
        Pose m_RightMovementPose = Pose.identity;

        Vector2 m_SmoothedInput;
        Vector2 m_InputVelocity;

        // --- Анти-скольжение ---
        CharacterController _cc;
        Vector3 _lockedPosition;
        bool _positionInitialized = false;

        protected override void Awake()
        {
            base.Awake();

            m_CombinedTransform = new GameObject("[Dynamic Move Provider] Combined Forward Source").transform;
            m_CombinedTransform.SetParent(transform, false);
            m_CombinedTransform.localPosition = Vector3.zero;
            m_CombinedTransform.localRotation = Quaternion.identity;

            forwardSource = m_CombinedTransform;
        }

        void Start()
        {
            // Ищем CharacterController на родительском XR Origin
            _cc = GetComponentInParent<CharacterController>();
            if (_cc == null)
                _cc = GetComponent<CharacterController>();
        }

        void LateUpdate()
        {
            if (_cc == null || !_cc.isGrounded) return;

            var left = leftHandMoveInput.ReadValue();
            var right = rightHandMoveInput.ReadValue();
            bool isMoving = left.sqrMagnitude > 0.01f || right.sqrMagnitude > 0.01f;

            if (isMoving || !_positionInitialized)
            {
                _lockedPosition = _cc.transform.position;
                _positionInitialized = true;
            }
            else
            {
                // Фиксируем X и Z, Y не трогаем чтобы гравитация работала
                _cc.enabled = false;
                _cc.transform.position = new Vector3(
                    _lockedPosition.x,
                    _cc.transform.position.y,
                    _lockedPosition.z
                );
                _cc.enabled = true;
            }
        }

        protected override Vector3 ComputeDesiredMove(Vector2 input)
        {
            if (input == Vector2.zero)
                return Vector3.zero;

            if (m_HeadTransform == null)
            {
                var xrOrigin = mediator.xrOrigin;
                if (xrOrigin != null)
                {
                    var xrCamera = xrOrigin.Camera;
                    if (xrCamera != null)
                        m_HeadTransform = xrCamera.transform;
                }
            }

            switch (m_LeftHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (m_HeadTransform != null)
                        m_LeftMovementPose = m_HeadTransform.GetWorldPose();
                    break;
                case MovementDirection.HandRelative:
                    if (m_LeftControllerTransform != null)
                        m_LeftMovementPose = m_LeftControllerTransform.GetWorldPose();
                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_LeftHandMovementDirection}");
                    break;
            }

            switch (m_RightHandMovementDirection)
            {
                case MovementDirection.HeadRelative:
                    if (m_HeadTransform != null)
                        m_RightMovementPose = m_HeadTransform.GetWorldPose();
                    break;
                case MovementDirection.HandRelative:
                    if (m_RightControllerTransform != null)
                        m_RightMovementPose = m_RightControllerTransform.GetWorldPose();
                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(MovementDirection)}={m_RightHandMovementDirection}");
                    break;
            }

            var leftHandValue = leftHandMoveInput.ReadValue();
            var rightHandValue = rightHandMoveInput.ReadValue();

            var totalSqrMagnitude = leftHandValue.sqrMagnitude + rightHandValue.sqrMagnitude;
            var leftHandBlend = 0.5f;
            if (totalSqrMagnitude > Mathf.Epsilon)
                leftHandBlend = leftHandValue.sqrMagnitude / totalSqrMagnitude;

            var combinedPosition = Vector3.Lerp(m_RightMovementPose.position, m_LeftMovementPose.position, leftHandBlend);
            var combinedRotation = Quaternion.Slerp(m_RightMovementPose.rotation, m_LeftMovementPose.rotation, leftHandBlend);
            m_CombinedTransform.SetPositionAndRotation(combinedPosition, combinedRotation);

            // Плавный разгон/торможение — убирает "рывок" при нажатии стика
            if (m_InputSmoothTime > 0f)
            {
                m_SmoothedInput = Vector2.SmoothDamp(
                    m_SmoothedInput, input, ref m_InputVelocity, m_InputSmoothTime);
                input = m_SmoothedInput;
            }

            return base.ComputeDesiredMove(input);
        }
    }
}