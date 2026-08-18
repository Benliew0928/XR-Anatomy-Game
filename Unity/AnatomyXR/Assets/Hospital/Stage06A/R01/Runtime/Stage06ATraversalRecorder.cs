using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CutMyBodyPlease.Hospital.Stage06A
{
    public sealed class Stage06ATraversalRecorder : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Stage06AWaypoint[] waypoints;

        private readonly Stage06ARuntimeSession session = new Stage06ARuntimeSession();
        private InputAction headTracked;
        private InputAction leftTracked;
        private InputAction rightTracked;
        private InputAction move;
        private InputAction snap;
        private string sessionDirectory;
        private int expectedWaypoint;
        private bool environmentReady;
        private Vector3 lastSafeFloor = Stage06AContract.CombinedSpawn;
        private float nextFallRecoveryTime;

        public string SessionDirectory => sessionDirectory;
        public int CompletedWaypoints => expectedWaypoint;

        public void Configure(XROrigin origin, Camera camera, InputActionAsset inputActions, Stage06AWaypoint[] routeWaypoints)
        {
            xrOrigin = origin;
            xrCamera = camera;
            actions = inputActions;
            waypoints = routeWaypoints;
            ResolveActions();
        }

        private void Awake()
        {
            ResolveActions();
            session.schema = Stage06AContract.SessionSchema;
            session.revision = Stage06AContract.Revision;
            session.started_utc = DateTime.UtcNow.ToString("O");
            session.status = "INITIALIZING";
            session.unity_version = Application.unityVersion;
            session.platform = Application.platform.ToString();
            session.device_model = SystemInfo.deviceModel;
            session.graphics_device = SystemInfo.graphicsDeviceName;
            session.processor = SystemInfo.processorType;
            session.system_memory_mb = SystemInfo.systemMemorySize;
            session.graphics_memory_mb = SystemInfo.graphicsMemorySize;
            session.eye_texture_width = XRSettings.eyeTextureWidth;
            session.eye_texture_height = XRSettings.eyeTextureHeight;
            session.connection_mode = "RECORD_MANUALLY_IN_REVIEW_CHECKLIST";
            session.route_events = new List<Stage06ARouteEvent>();
            session.runtime_errors = new List<string>();
            sessionDirectory = Path.Combine(Application.persistentDataPath, "Stage06A_R01", "Sessions",
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(sessionDirectory);
        }

        private void ResolveActions()
        {
            if (actions == null)
                return;
            headTracked = actions.FindAction("Head/IsTracked", false);
            leftTracked = actions.FindAction("LeftController/IsTracked", false);
            rightTracked = actions.FindAction("RightController/IsTracked", false);
            move = actions.FindAction("Locomotion/Move", false);
            snap = actions.FindAction("Locomotion/SnapTurn", false);
        }

        public void OnEnvironmentReady(Stage06ALoadProfile profile, string[] scenes, int routeSurfaceCount)
        {
            environmentReady = true;
            session.load_profile = profile.ToString();
            session.loaded_scenes = scenes;
            session.route_surface_count = routeSurfaceCount;
            session.status = "HEADSET_REVIEW_REQUIRED";
            lastSafeFloor = profile == Stage06ALoadProfile.HospitalOnly
                ? Stage06AContract.HospitalOnlySpawn
                : Stage06AContract.CombinedSpawn;
            CaptureDisplayRefreshRate();
            WriteSession();
        }

        private void Update()
        {
            if (!environmentReady)
                return;

            session.hmd_tracking_observed |= IsTracked(headTracked);
            session.left_controller_tracking_observed |= IsTracked(leftTracked);
            session.right_controller_tracking_observed |= IsTracked(rightTracked);
            session.continuous_move_observed |= move != null && move.ReadValue<Vector2>().magnitude > 0.2f;
            session.snap_turn_observed |= snap != null && Mathf.Abs(snap.ReadValue<Vector2>().x) > 0.65f;

            if (xrOrigin != null && xrOrigin.Origin.transform.position.y < -2f && Time.unscaledTime >= nextFallRecoveryTime)
            {
                session.fall_recovery_count++;
                session.status = "TECHNICAL_FAILURE_FALL_RECOVERED";
                nextFallRecoveryTime = Time.unscaledTime + 1f;
                float eyeHeight = Mathf.Max(1.2f, xrOrigin.Origin.transform.InverseTransformPoint(xrCamera.transform.position).y);
                xrOrigin.MoveCameraToWorldLocation(lastSafeFloor + Vector3.up * eyeHeight);
                WriteSession();
            }
        }

        public void EnterWaypoint(int index, string waypointName, Vector3 position)
        {
            if (!environmentReady || index != expectedWaypoint)
                return;
            session.route_events.Add(new Stage06ARouteEvent
            {
                index = index,
                name = waypointName,
                utc = DateTime.UtcNow.ToString("O"),
                position = position,
            });
            lastSafeFloor = position;
            expectedWaypoint++;
            string screenshot = Path.Combine(sessionDirectory, $"Stage06A_Route_{index:00}_{Sanitize(waypointName)}.png");
            ScreenCapture.CaptureScreenshot(screenshot, 1);
            session.completed_waypoints = expectedWaypoint;
            if (waypoints != null && expectedWaypoint == waypoints.Length && session.fall_recovery_count == 0 && session.runtime_errors.Count == 0)
            {
                session.status = session.hmd_tracking_observed && session.left_controller_tracking_observed &&
                                 session.right_controller_tracking_observed
                    ? "ROUTE_COMPLETE_AWAITING_USER_DECISION"
                    : "TECHNICAL_FAILURE_MISSING_TRACKED_DEVICE";
            }
            WriteSession();
        }

        public void RecordTeleportAttempt(bool accepted, string reason, Vector3 position)
        {
            session.teleport_attempts++;
            if (accepted)
                session.teleport_successes++;
            else
                session.teleport_rejections++;
            session.last_teleport_reason = reason;
            session.last_teleport_position = position;
            WriteSession();
        }

        public void RecordRuntimeError(string message)
        {
            if (!session.runtime_errors.Contains(message))
                session.runtime_errors.Add(message);
            session.status = "TECHNICAL_FAILURE_RUNTIME_ERROR";
            WriteSession();
        }

        private void CaptureDisplayRefreshRate()
        {
            var displays = new List<XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            foreach (XRDisplaySubsystem display in displays)
            {
                if (display != null && display.running && display.TryGetDisplayRefreshRate(out float refresh))
                {
                    session.display_refresh_hz = refresh;
                    break;
                }
            }
        }

        private void OnApplicationQuit()
        {
            session.ended_utc = DateTime.UtcNow.ToString("O");
            WriteSession();
        }

        private void WriteSession()
        {
            if (string.IsNullOrEmpty(sessionDirectory))
                return;
            try
            {
                File.WriteAllText(Path.Combine(sessionDirectory, "Stage06A_RuntimeSession.json"),
                    JsonUtility.ToJson(session, true) + Environment.NewLine);
            }
            catch (Exception exception)
            {
                Debug.LogError("Could not write Stage 6A session: " + exception.Message, this);
            }
        }

        private static bool IsTracked(InputAction action) => action != null && action.ReadValue<float>() > 0.5f;
        private static string Sanitize(string value) => new string(value.Select(character =>
            char.IsLetterOrDigit(character) ? character : '_').ToArray());
    }

    [Serializable]
    internal sealed class Stage06ARuntimeSession
    {
        public string schema;
        public string revision;
        public string status;
        public string started_utc;
        public string ended_utc;
        public string unity_version;
        public string platform;
        public string load_profile;
        public string device_model;
        public string graphics_device;
        public string processor;
        public int system_memory_mb;
        public int graphics_memory_mb;
        public string connection_mode;
        public string[] loaded_scenes;
        public int eye_texture_width;
        public int eye_texture_height;
        public float display_refresh_hz;
        public int route_surface_count;
        public int completed_waypoints;
        public bool hmd_tracking_observed;
        public bool left_controller_tracking_observed;
        public bool right_controller_tracking_observed;
        public bool continuous_move_observed;
        public bool snap_turn_observed;
        public int teleport_attempts;
        public int teleport_successes;
        public int teleport_rejections;
        public string last_teleport_reason;
        public Vector3 last_teleport_position;
        public int fall_recovery_count;
        public List<Stage06ARouteEvent> route_events;
        public List<string> runtime_errors;
    }

    [Serializable]
    internal sealed class Stage06ARouteEvent
    {
        public int index;
        public string name;
        public string utc;
        public Vector3 position;
    }
}
