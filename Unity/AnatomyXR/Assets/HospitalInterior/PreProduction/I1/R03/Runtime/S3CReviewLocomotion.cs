using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class S3CReviewLocomotion : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S3CReviewBootstrap bootstrap;
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
        public S3CReviewBootstrap Bootstrap => bootstrap;

        public void Configure(XROrigin origin, Camera camera, S3CReviewBootstrap reviewBootstrap,
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
                int requested = RequestedFloorIndex(keyboard);
                if (requested >= 0)
                {
                    bootstrap.ElevatorController.RequestFloor("F" + requested.ToString("00"));
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.oKey.wasPressedThisFrame)
                {
                    bootstrap.ElevatorController.RequestDoorOpen();
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.cKey.wasPressedThisFrame)
                {
                    bootstrap.ElevatorController.RequestDoorClose();
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.lKey.wasPressedThisFrame)
                {
                    bootstrap.ElevatorController.RequestLandingCall(bootstrap.ElevatorController.ActiveFloorId);
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.xKey.wasPressedThisFrame)
                {
                    bootstrap.ElevatorController.CancelActiveTravel();
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    bootstrap.ElevatorController.RequestReviewRecovery();
                    lastInteraction = bootstrap.ElevatorController.LastResult;
                }
                if (keyboard.kKey.wasPressedThisFrame)
                {
                    bootstrap.ToggleReviewObstruction();
                    lastInteraction = "Threshold obstruction: "
                        + (bootstrap.ReviewObstructionEnabled ? "ON" : "OFF");
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
            if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistanceM, ~0,
                QueryTriggerInteraction.Collide))
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

        private void OnGUI()
        {
            if (!showHelp)
                return;
            const int width = 650;
            GUI.Box(new Rect(16, 16, width, 244), "Hospital Interior R03 - Stage S3C E01 Travel Review");
            string environment = bootstrap == null ? "Bootstrap missing"
                : bootstrap.EnvironmentReady ? "Approved exterior + one active floor loaded"
                : string.IsNullOrEmpty(bootstrap.FatalError) ? "Loading approved hospital..."
                : "ERROR: " + bootstrap.FatalError;
            GUI.Label(new Rect(30, 44, width - 24, 22), environment);
            if (bootstrap != null && bootstrap.ElevatorController != null)
            {
                S3ElevatorController elevator = bootstrap.ElevatorController;
                GUI.Label(new Rect(30, 68, width - 24, 22),
                    $"E01: {elevator.State} | Cabin {elevator.CommittedFloorId} | Active {elevator.ActiveFloorId} | Target {elevator.DestinationFloorId}");
                S3DoorController door = elevator.CurrentDoorController;
                if (door != null)
                    GUI.Label(new Rect(30, 92, width - 24, 22),
                        $"Doors: {door.State} | Open {door.EasedOpenness * 100f:F0}% | Interlocked {door.MovementInterlockSafe} | Obstructed {door.IsObstructed}");
                GUI.Label(new Rect(30, 116, width - 24, 22),
                    "Passenger inside cabin: " + elevator.IsPassengerInsideCabin());
            }
            GUI.Label(new Rect(30, 140, width - 24, 22), "0-6 request floors | O open | C close | L landing call | K obstruction");
            GUI.Label(new Rect(30, 164, width - 24, 22), "X cancel/rollback | R review recovery from FaultedSafe | Y hide help");
            GUI.Label(new Rect(30, 188, width - 24, 22), "WASD walk | Left Shift sprint | Hold RMB look | Left click a physical control");
            GUI.Label(new Rect(30, 212, width - 24, 22), string.IsNullOrEmpty(lastInteraction)
                ? "Walk through the open F00 doors, enter the cabin, then select a floor."
                : lastInteraction);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            GUI.Box(new Rect(cx - 1f, cy - 7f, 2f, 14f), GUIContent.none);
            GUI.Box(new Rect(cx - 7f, cy - 1f, 14f, 2f), GUIContent.none);
        }

        private static int RequestedFloorIndex(Keyboard keyboard)
        {
            if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) return 0;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 1;
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 2;
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 3;
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 4;
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 5;
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) return 6;
            return -1;
        }
    }
}
