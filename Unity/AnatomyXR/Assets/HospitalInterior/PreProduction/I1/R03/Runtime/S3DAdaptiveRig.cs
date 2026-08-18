using System;
using System.Collections;
using System.Linq;
using CutMyBodyPlease.Hospital.Stage06A;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController), typeof(XROrigin))]
    public sealed class S3DAdaptiveRig : MonoBehaviour
    {
        [SerializeField] private S3DIntegrationMode requestedMode = S3DIntegrationMode.Auto;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform rightController;
        [SerializeField] private TrackedPoseDriver[] trackedPoseDrivers = Array.Empty<TrackedPoseDriver>();
        [SerializeField] private Behaviour[] pcvrLocomotionBehaviours = Array.Empty<Behaviour>();
        [SerializeField] private GameObject[] pcvrVisualRoots = Array.Empty<GameObject>();
        [SerializeField] private LineRenderer interactionLine;
        [SerializeField] private float eyeHeightM = 1.7f;
        [SerializeField] private float walkSpeedMps = 6f;
        [SerializeField] private float sprintSpeedMps = 12f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float interactionDistanceM = 5.5f;
        [SerializeField] private float interactionAimAssistRadiusM = 0.18f;
        [SerializeField] private bool showHelp = true;

        [SerializeField] private S3DIntegrationBootstrap bootstrap;
        private InputAction activateAction;
        private float pitch;
        private float verticalVelocity;
        private bool environmentReady;
        private bool gateSimulationSuspended;
        private string lastInteraction = string.Empty;

        public S3DIntegrationMode ActiveMode { get; private set; } = S3DIntegrationMode.Auto;
        public bool ModeReady { get; private set; }
        public bool EnvironmentReady => environmentReady;
        public float WalkSpeedMps => walkSpeedMps;
        public float SprintSpeedMps => sprintSpeedMps;
        public float InteractionDistanceM => interactionDistanceM;
        public float InteractionAimAssistRadiusM => interactionAimAssistRadiusM;
        public XROrigin XrOrigin => xrOrigin;
        public Camera ReviewCamera => reviewCamera;
        public CharacterController CharacterController => characterController;
        public Transform RightController => rightController;
        public InputActionAsset Actions => actions;

        public void Configure(S3DIntegrationMode mode, InputActionAsset inputActions,
            XROrigin origin, Camera camera, CharacterController controller,
            Transform pcvrRightController, TrackedPoseDriver[] poseDrivers,
            Behaviour[] pcvrBehaviours, GameObject[] visualRoots, LineRenderer rayLine,
            float eyeHeight, float walkSpeed, float sprintSpeed)
        {
            requestedMode = mode;
            actions = inputActions;
            xrOrigin = origin;
            reviewCamera = camera;
            characterController = controller;
            rightController = pcvrRightController;
            trackedPoseDrivers = poseDrivers ?? Array.Empty<TrackedPoseDriver>();
            pcvrLocomotionBehaviours = pcvrBehaviours ?? Array.Empty<Behaviour>();
            pcvrVisualRoots = visualRoots ?? Array.Empty<GameObject>();
            interactionLine = rayLine;
            eyeHeightM = eyeHeight;
            walkSpeedMps = walkSpeed;
            sprintSpeedMps = sprintSpeed;
            ResolveActions();
            SetPcvrStackEnabled(false);
        }

        public void ConfigureBootstrap(S3DIntegrationBootstrap integrationBootstrap)
            => bootstrap = integrationBootstrap;

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (xrOrigin == null)
                xrOrigin = GetComponent<XROrigin>();
            ResolveActions();
            if (interactionLine != null)
                interactionLine.enabled = false;
        }

        private IEnumerator Start()
        {
            S3DIntegrationMode commandLine = ParseCommandLineMode();
            if (commandLine != S3DIntegrationMode.Auto)
                ActiveMode = commandLine;
            else if (requestedMode != S3DIntegrationMode.Auto)
                ActiveMode = requestedMode;
            else
            {
                float elapsed = 0f;
                while (elapsed < 2f && !(XRSettings.enabled && XRSettings.isDeviceActive))
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
                ActiveMode = XRSettings.enabled && XRSettings.isDeviceActive
                    ? S3DIntegrationMode.PCVR : S3DIntegrationMode.Desktop;
            }

            ApplyMode();
            ModeReady = true;
        }

        public void NotifyEnvironmentReady()
        {
            environmentReady = true;
            ApplyMode();
        }

        public void SetGateSimulationSuspended(bool suspended)
        {
            gateSimulationSuspended = suspended;
            verticalVelocity = 0f;
        }

        private void ApplyMode()
        {
            bool pcvr = ActiveMode == S3DIntegrationMode.PCVR;
            foreach (TrackedPoseDriver driver in trackedPoseDrivers)
                if (driver != null)
                    driver.enabled = pcvr;
            foreach (GameObject visual in pcvrVisualRoots)
                if (visual != null)
                    visual.SetActive(pcvr);

            if (reviewCamera != null && !pcvr)
            {
                reviewCamera.transform.localPosition = new Vector3(0f, eyeHeightM, 0f);
                reviewCamera.transform.localRotation = Quaternion.identity;
            }
            SetPcvrStackEnabled(pcvr && environmentReady);
        }

        private void SetPcvrStackEnabled(bool enabled)
        {
            foreach (Behaviour behaviour in pcvrLocomotionBehaviours)
                if (behaviour != null)
                    behaviour.enabled = enabled;
        }

        private void Update()
        {
            if (!ModeReady || !environmentReady || reviewCamera == null
                || characterController == null || gateSimulationSuspended)
                return;

            if (ActiveMode == S3DIntegrationMode.Desktop)
                UpdateDesktop();
            else
                UpdatePcvrInteraction();

            if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
                showHelp = !showHelp;
        }

        private void UpdateDesktop()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null)
                return;

            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                reviewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                ActivateFromRay(reviewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)));

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = Vector3.ProjectOnPlane(reviewCamera.transform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(reviewCamera.transform.right, Vector3.up).normalized;
            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeedMps : walkSpeedMps;
            verticalVelocity = characterController.isGrounded && verticalVelocity < 0f
                ? -1f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            Vector3 motion = (forward * input.y + right * input.x) * speed;
            motion.y = verticalVelocity;
            characterController.Move(motion * Time.deltaTime);
        }

        private void UpdatePcvrInteraction()
        {
            ResolveActions();
            if (rightController == null || activateAction == null)
                return;
            Ray ray = new Ray(rightController.position, rightController.forward);
            bool hasButton = TryFindButton(ray, out _, out RaycastHit hit);
            if (interactionLine != null)
            {
                interactionLine.enabled = hasButton;
                if (hasButton)
                {
                    interactionLine.positionCount = 2;
                    interactionLine.SetPosition(0, ray.origin);
                    interactionLine.SetPosition(1, hit.point);
                }
            }
            if (activateAction.WasPressedThisFrame())
                ActivateFromRay(ray);
        }

        private void ActivateFromRay(Ray ray)
        {
            if (!TryFindButton(ray, out S3ElevatorButton button, out _))
            {
                lastInteraction = "No elevator control in reach.";
                return;
            }
            button.Activate();
            lastInteraction = button.LastResult;
        }

        private bool TryFindButton(Ray ray, out S3ElevatorButton button, out RaycastHit hit)
        {
            button = null;
            hit = default;
            RaycastHit[] assistedHits = Physics.SphereCastAll(ray,
                interactionAimAssistRadiusM, interactionDistanceM, ~0,
                QueryTriggerInteraction.Collide);
            if (assistedHits.Length == 0)
                return false;

            RaycastHit[] blockers = Physics.RaycastAll(ray, interactionDistanceM, ~0,
                QueryTriggerInteraction.Ignore);
            float nearestBlocker = blockers.Length == 0 ? float.PositiveInfinity
                : blockers.Min(item => item.distance);
            var candidates = assistedHits
                .Select(item => new
                {
                    Hit = item,
                    Button = item.collider == null ? null
                        : item.collider.GetComponentInParent<S3ElevatorButton>(),
                    AimOffset = item.collider == null ? float.PositiveInfinity
                        : Vector3.Cross(ray.direction.normalized,
                            item.collider.bounds.center - ray.origin).magnitude,
                })
                .Where(item => item.Button != null
                    && item.Hit.distance <= nearestBlocker + interactionAimAssistRadiusM + 0.12f)
                .OrderBy(item => item.AimOffset)
                .ThenBy(item => item.Hit.distance)
                .ToArray();
            if (candidates.Length == 0)
                return false;

            button = candidates[0].Button;
            hit = candidates[0].Hit;
            return true;
        }

        private void ResolveActions()
            => activateAction = actions?.FindAction("Interaction/Activate", false);

        public void SetPositionSafely(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = characterController != null && characterController.enabled;
            if (wasEnabled)
                characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            if (ActiveMode == S3DIntegrationMode.Desktop && reviewCamera != null)
                reviewCamera.transform.localRotation = Quaternion.identity;
            pitch = 0f;
            verticalVelocity = 0f;
            if (wasEnabled)
                characterController.enabled = true;
            Physics.SyncTransforms();
        }

        private static S3DIntegrationMode ParseCommandLineMode()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (!string.Equals(arguments[i], "-s3dMode", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Enum.TryParse(arguments[i + 1], true, out S3DIntegrationMode result))
                    return result;
            }
            return S3DIntegrationMode.Auto;
        }

        private void OnGUI()
        {
            if (!showHelp)
                return;
            const int width = 680;
            GUI.Box(new Rect(16, 16, width, 208), "Hospital Interior R03 - S3D Full Technical Integration");
            string state = bootstrap == null ? "Bootstrap missing"
                : bootstrap.EnvironmentReady ? "Site + hospital + active empty floor ready"
                : string.IsNullOrEmpty(bootstrap.FatalError) ? "Loading approved integration..."
                : "ERROR: " + bootstrap.FatalError;
            GUI.Label(new Rect(30, 44, width - 24, 22), state);
            GUI.Label(new Rect(30, 68, width - 24, 22), "Mode: " + ActiveMode);
            if (bootstrap?.ElevatorController != null)
            {
                S3ElevatorController elevator = bootstrap.ElevatorController;
                GUI.Label(new Rect(30, 92, width - 24, 22),
                    $"E01: {elevator.State} | Cabin {elevator.CommittedFloorId} | Active {elevator.ActiveFloorId}");
            }
            GUI.Label(new Rect(30, 116, width - 24, 22), ActiveMode == S3DIntegrationMode.PCVR
                ? "Left stick move | Right stick snap turn | A teleport | B cancel | Right trigger interact"
                : "WASD walk | Shift sprint | Hold RMB look | Left click elevator controls");
            GUI.Label(new Rect(30, 140, width - 24, 22), "Main entrance opens automatically. Y hides this panel.");
            GUI.Label(new Rect(30, 164, width - 24, 22), string.IsNullOrEmpty(lastInteraction)
                ? "Start at the gate, enter the hospital, call E01, and visit F00-F06."
                : lastInteraction);

            if (ActiveMode == S3DIntegrationMode.Desktop)
            {
                float cx = Screen.width * 0.5f;
                float cy = Screen.height * 0.5f;
                GUI.Box(new Rect(cx - 1f, cy - 7f, 2f, 14f), GUIContent.none);
                GUI.Box(new Rect(cx - 7f, cy - 1f, 14f, 2f), GUIContent.none);
            }
        }
    }
}
