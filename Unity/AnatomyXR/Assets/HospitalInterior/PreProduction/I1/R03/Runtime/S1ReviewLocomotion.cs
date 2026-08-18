using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class S1ReviewLocomotion : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S1Bootstrap bootstrap;
        [SerializeField] private float walkSpeedMps = 2f;
        [SerializeField] private float sprintSpeedMps = 4f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private bool showHelp = true;

        private CharacterController controller;
        private float pitch;
        private float verticalVelocity;

        public float WalkSpeedMps => walkSpeedMps;
        public float SprintSpeedMps => sprintSpeedMps;
        public Camera ReviewCamera => reviewCamera;

        public void Configure(XROrigin origin, Camera camera, S1Bootstrap sceneBootstrap,
            float walkSpeed, float sprintSpeed, float sensitivity)
        {
            xrOrigin = origin;
            reviewCamera = camera;
            bootstrap = sceneBootstrap;
            walkSpeedMps = walkSpeed;
            sprintSpeedMps = sprintSpeed;
            mouseSensitivity = sensitivity;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard == null || reviewCamera == null || controller == null)
                return;

            if (keyboard.yKey.wasPressedThisFrame)
                showHelp = !showHelp;

            bool xrActive = XRSettings.enabled && XRSettings.isDeviceActive;
            if (!xrActive && mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity;
                transform.Rotate(0f, delta.x, 0f, Space.World);
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
                reviewCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

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
                ? -1f
                : verticalVelocity + Physics.gravity.y * Time.deltaTime;
            Vector3 motion = (forward * input.y + right * input.x) * speed;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private void OnGUI()
        {
            if (!showHelp)
                return;

            const int width = 440;
            GUI.Box(new Rect(16, 16, width, 142), "Hospital Interior R03 — Stage S1 Datum Review");
            string state = bootstrap == null
                ? "Bootstrap missing"
                : bootstrap.EnvironmentReady ? "Environment ready" : string.IsNullOrEmpty(bootstrap.FatalError)
                    ? "Loading approved exterior…" : "ERROR: " + bootstrap.FatalError;
            GUI.Label(new Rect(30, 44, width - 24, 22), state);
            GUI.Label(new Rect(30, 68, width - 24, 22), "WASD 2 m/s · Left Shift 4 m/s · RMB look · Y help");
            GUI.Label(new Rect(30, 92, width - 24, 22), "Diagnostic only: floor datums and E01 reference cage");
            GUI.Label(new Rect(30, 116, width - 24, 30), "No rooms, stairs, elevator travel, floor scenes, or design authority.");
        }
    }
}
