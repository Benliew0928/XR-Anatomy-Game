using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace CutMyBodyPlease.Hospital.Stage06A
{
    /// <summary>
    /// Minimal action-driven Stage 6A locomotion. It queues movement and yaw through
    /// XRI's LocomotionMediator/XRBodyTransformer rather than moving the rig directly.
    /// </summary>
    public sealed class Stage06AActionLocomotionProvider : LocomotionProvider
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform forwardSource;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float snapTurnDegrees = 45f;
        [SerializeField] private float snapDebounceSeconds = 0.35f;
        [SerializeField] private float stickDeadzone = 0.2f;

        private readonly XROriginMovement movement = new XROriginMovement();
        private readonly XRBodyYawRotation yaw = new XRBodyYawRotation();
        private InputAction moveAction;
        private InputAction turnAction;
        private float nextTurnTime;
        private bool turnLatched;

        public float MoveSpeed => moveSpeed;
        public float SnapTurnDegrees => snapTurnDegrees;
        public InputActionAsset Actions => actions;

        public void Configure(InputActionAsset inputActions, Transform directionSource, float speed, float snapDegrees)
        {
            actions = inputActions;
            forwardSource = directionSource;
            moveSpeed = speed;
            snapTurnDegrees = snapDegrees;
            ResolveActions();
        }

        protected override void Awake()
        {
            base.Awake();
            ResolveActions();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveActions();
        }

        private void ResolveActions()
        {
            if (actions == null)
                return;
            moveAction = actions.FindAction("Locomotion/Move", false);
            turnAction = actions.FindAction("Locomotion/SnapTurn", false);
        }

        private void Update()
        {
            if (mediator == null || mediator.xrOrigin == null || actions == null)
                return;

            Vector2 stick = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector3 translation = ComputeTranslation(stick);
            float turn = ComputeSnapTurn(turnAction?.ReadValue<Vector2>() ?? Vector2.zero);

            if (translation.sqrMagnitude <= 0f && Mathf.Approximately(turn, 0f))
            {
                TryEndLocomotion();
                return;
            }

            TryStartLocomotionImmediately();
            if (locomotionState != LocomotionState.Moving)
                return;

            if (translation.sqrMagnitude > 0f)
            {
                movement.motion = translation;
                TryQueueTransformation(movement);
            }

            if (!Mathf.Approximately(turn, 0f))
            {
                yaw.angleDelta = turn;
                TryQueueTransformation(yaw);
            }
        }

        private Vector3 ComputeTranslation(Vector2 stick)
        {
            if (stick.magnitude < stickDeadzone)
                return Vector3.zero;

            stick = Vector2.ClampMagnitude(stick, 1f);
            Transform direction = forwardSource != null ? forwardSource : mediator.xrOrigin.Camera.transform;
            Vector3 up = mediator.xrOrigin.Origin.transform.up;
            Vector3 forward = Vector3.ProjectOnPlane(direction.forward, up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(direction.right, up).normalized;
            return (right * stick.x + forward * stick.y) * (moveSpeed * Time.deltaTime);
        }

        private float ComputeSnapTurn(Vector2 stick)
        {
            if (Mathf.Abs(stick.x) < 0.65f)
            {
                turnLatched = false;
                return 0f;
            }

            if (turnLatched || Time.unscaledTime < nextTurnTime)
                return 0f;

            turnLatched = true;
            nextTurnTime = Time.unscaledTime + snapDebounceSeconds;
            return Mathf.Sign(stick.x) * snapTurnDegrees;
        }
    }
}
