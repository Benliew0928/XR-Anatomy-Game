using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class S3BReviewLocomotion : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S3BReviewBootstrap bootstrap;
        [SerializeField] private float walkSpeedMps = 2f;
        [SerializeField] private float sprintSpeedMps = 4f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float interactionDistanceM = 3.5f;
        [SerializeField] private bool showHelp = true;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;
        private string lastInteraction = string.Empty;

        public float WalkSpeedMps => walkSpeedMps;
        public float SprintSpeedMps => sprintSpeedMps;
        public Camera ReviewCamera => reviewCamera;
        public S3BReviewBootstrap Bootstrap => bootstrap;

        public void Configure(XROrigin origin, Camera camera, S3BReviewBootstrap reviewBootstrap,
            float walkSpeed, float sprintSpeed, float sensitivity)
        {
            xrOrigin = origin;
            reviewCamera = camera;
            bootstrap = reviewBootstrap;
            walkSpeedMps = walkSpeed;
            sprintSpeedMps = sprintSpeed;
            mouseSensitivity = sensitivity;
        }

        private void Awake() => controller = GetComponent<CharacterController>();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || reviewCamera == null || controller == null || !controller.enabled)
                return;

            if (keyboard.yKey.wasPressedThisFrame)
                showHelp = !showHelp;
            if (bootstrap != null && bootstrap.EnvironmentReady)
            {
                if (keyboard.oKey.wasPressedThisFrame)
                    lastInteraction = bootstrap.DoorController.RequestOpen() ? "Door-open accepted." : "Door-open rejected safely.";
                if (keyboard.cKey.wasPressedThisFrame)
                    lastInteraction = bootstrap.DoorController.RequestClose() ? "Door-close accepted." : "Door-close blocked/rejected safely.";
                if (keyboard.lKey.wasPressedThisFrame)
                    lastInteraction = ActivateLandingCall();
                if (keyboard.kKey.wasPressedThisFrame)
                {
                    bootstrap.ToggleReviewObstruction();
                    lastInteraction = "Threshold obstruction: " + (bootstrap.ReviewObstructionEnabled ? "ON" : "OFF");
                }
            }

            bool xrActive = XRSettings.enabled && XRSettings.isDeviceActive;
            if (!xrActive && mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                reviewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                Interact(mouse.position.ReadValue());

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = reviewCamera.transform.forward;
            Vector3 right = reviewCamera.transform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            float speed = keyboard.leftShiftKey.isPressed ? sprintSpeedMps : walkSpeedMps;
            verticalVelocity = controller.isGrounded && verticalVelocity < 0f
                ? -1f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            Vector3 motion = (forward * input.y + right * input.x) * speed;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void Interact(Vector2 screenPosition)
        {
            Ray ray = reviewCamera.ScreenPointToRay(screenPosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistanceM, ~0, QueryTriggerInteraction.Collide))
            {
                lastInteraction = "No elevator control in reach.";
                return;
            }
            S3ElevatorButton button = hit.collider.GetComponentInParent<S3ElevatorButton>();
            if (button == null)
            {
                lastInteraction = "Target is not an elevator control.";
                return;
            }
            button.Activate();
            lastInteraction = button.LastResult;
        }

        private string ActivateLandingCall()
        {
            S3ElevatorButton[] buttons = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (S3ElevatorButton button in buttons)
                if (button.Command == S3ElevatorButtonCommand.LandingCall && button.FloorId == "F00")
                {
                    button.Activate();
                    return button.LastResult;
                }
            return "F00 landing-call control unavailable.";
        }

        private void OnGUI()
        {
            if (!showHelp)
                return;
            const int width = 570;
            GUI.Box(new Rect(16, 16, width, 210), "Hospital Interior R03 - Stage S3B Normal Door Review");
            string environment = bootstrap == null ? "Bootstrap missing"
                : bootstrap.EnvironmentReady ? "Approved exterior + F00 loaded"
                : string.IsNullOrEmpty(bootstrap.FatalError) ? "Loading approved hospital..." : "ERROR: " + bootstrap.FatalError;
            GUI.Label(new Rect(30, 44, width - 24, 22), environment);
            if (bootstrap != null && bootstrap.DoorController != null)
            {
                S3DoorController door = bootstrap.DoorController;
                GUI.Label(new Rect(30, 68, width - 24, 22),
                    $"Door state: {door.State} | Open {door.EasedOpenness * 100f:F0}% | Clear {door.MeasuredClearWidthM:F3} m");
                GUI.Label(new Rect(30, 92, width - 24, 22),
                    $"Obstructed: {door.IsObstructed} | Cabin lock: {door.CabinInterlock} | Landing lock: {door.LandingInterlock}");
            }
            GUI.Label(new Rect(30, 116, width - 24, 22), "O open | C close | L landing call | K toggle threshold obstruction | Y hide help");
            GUI.Label(new Rect(30, 140, width - 24, 22), "WASD walk | Left Shift sprint | Hold RMB look | Left click a physical control");
            GUI.Label(new Rect(30, 164, width - 24, 22), "F00-F06 buttons remain safely disabled until S3C travel is approved.");
            GUI.Label(new Rect(30, 188, width - 24, 22), string.IsNullOrEmpty(lastInteraction) ? "Walk through the open 1.20 m doorway to test collision." : lastInteraction);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 1f, cy - 7f, 2f, 14f), GUIContent.none);
            GUI.Box(new Rect(cx - 7f, cy - 1f, 14f, 2f), GUIContent.none);
        }
    }
}
