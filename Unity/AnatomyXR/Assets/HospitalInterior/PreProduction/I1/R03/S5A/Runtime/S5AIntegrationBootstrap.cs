using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S5ARuntimeCheck
    {
        public string id;
        public string status;
        public string detail;
    }

    [Serializable]
    public sealed class S5ARuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S5A.RuntimeGate.v1";
        public string status;
        public string unityVersion;
        public int passed;
        public int failed;
        public int total;
        public string activeFloor;
        public string cabinFloor;
        public int maximumSimultaneousFloors;
        public string[] evidenceFiles;
        public S5ARuntimeCheck[] checks;
    }

    [DisallowMultipleComponent]
    public sealed class S5AIntegrationBootstrap : MonoBehaviour
    {
        public const string ExteriorBaseBlockerName = "UE_EXT_GlobalStructure_OPAQUE_ALL_02";

        [SerializeField] private S3DIntegrationBootstrap approvedS3DBootstrap;
        [SerializeField] private S5AStairContract stairContract;
        [SerializeField] private S5ADualAperture dualAperture;
        [SerializeField] private S5AFloorSceneCoordinator floorCoordinator;
        [SerializeField] private S5AVerticalCirculationController circulation;
        [SerializeField] private S5AStairDoorController[] stairDoors = Array.Empty<S5AStairDoorController>();
        [SerializeField] private S5AAdaptiveInteractionRouter interactionRouter;
        [SerializeField] private S5ARecoveryCoordinator recoveryCoordinator;
        [SerializeField] private Transform stairRoot;

        public S3DIntegrationBootstrap ApprovedS3DBootstrap => approvedS3DBootstrap;
        public S5AStairContract StairContract => stairContract;
        public S5ADualAperture DualAperture => dualAperture;
        public S5AFloorSceneCoordinator FloorCoordinator => floorCoordinator;
        public S5AVerticalCirculationController Circulation => circulation;
        public S5AStairDoorController[] StairDoors => stairDoors;
        public Transform StairRoot => stairRoot;
        public bool EnvironmentReady { get; private set; }
        public bool ExteriorDualApertureApplied { get; private set; }
        public string FatalError { get; private set; } = string.Empty;

        public void Configure(S3DIntegrationBootstrap legacyBootstrap,
            S5AStairContract contract, S5ADualAperture aperture,
            S5AFloorSceneCoordinator coordinator,
            S5AVerticalCirculationController verticalCirculation,
            S5AStairDoorController[] doors, S5AAdaptiveInteractionRouter router,
            S5ARecoveryCoordinator recovery, Transform persistentStairRoot)
        {
            approvedS3DBootstrap = legacyBootstrap;
            stairContract = contract;
            dualAperture = aperture;
            floorCoordinator = coordinator;
            circulation = verticalCirculation;
            stairDoors = doors ?? Array.Empty<S5AStairDoorController>();
            interactionRouter = router;
            recoveryCoordinator = recovery;
            stairRoot = persistentStairRoot;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            if (!ReferencesComplete())
            {
                Fail("S5A bootstrap references are incomplete.");
                yield break;
            }
            float timeout = Time.realtimeSinceStartup + 90f;
            while (!approvedS3DBootstrap.EnvironmentReady
                && string.IsNullOrEmpty(approvedS3DBootstrap.FatalError)
                && Time.realtimeSinceStartup < timeout)
                yield return null;
            if (!approvedS3DBootstrap.EnvironmentReady)
            {
                Fail("Approved S3D startup failed before S5A takeover: "
                    + approvedS3DBootstrap.FatalError);
                yield break;
            }
            bool contractValid = stairContract.Validate(out string contractDetail);
            bool apertureValid = dualAperture.ValidateAssets(out string apertureDetail);
            if (!contractValid || !apertureValid)
            {
                Fail("S5A measured authority failed: " + contractDetail + "; " + apertureDetail);
                yield break;
            }

            string initial = stairContract.ApprovedFloorContract.InitialFloorId;
            stairContract.TryGetFloor(initial, out S2FloorRecord initialFloor);
            Scene floorScene = SceneManager.GetSceneByPath(initialFloor.scenePath);
            if (!floorCoordinator.InitializeFromLoadedFloor(initial, floorScene,
                    out string floorDetail))
            {
                Fail("S5A floor coordinator takeover failed: " + floorDetail);
                yield break;
            }
            if (!ApplyExteriorDualAperture(out string exteriorDetail))
            {
                Fail("S5A exterior dual aperture failed: " + exteriorDetail);
                yield break;
            }
            if (!circulation.InitializeAtFloor(initial, out string circulationDetail))
            {
                Fail("S5A circulation initialization failed: " + circulationDetail);
                yield break;
            }

            S3ElevatorController oldElevator = approvedS3DBootstrap.ElevatorController;
            if (oldElevator != null)
                oldElevator.enabled = false;
            if (approvedS3DBootstrap.TeleportCoordinator != null)
                approvedS3DBootstrap.TeleportCoordinator.enabled = false;
            if (approvedS3DBootstrap.RecoveryCoordinator != null)
                approvedS3DBootstrap.RecoveryCoordinator.enabled = false;
            if (approvedS3DBootstrap.FacadeFloorEdgeCoordinator != null)
                approvedS3DBootstrap.FacadeFloorEdgeCoordinator.enabled = false;
            interactionRouter.NotifyEnvironmentReady();
            recoveryCoordinator.NotifyEnvironmentReady();
            Physics.SyncTransforms();
            EnvironmentReady = true;
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_RUNTIME_READY; active="
                + floorCoordinator.ActiveFloorId + "; cabin=" + circulation.CabinFloorId);

            if (HasArgument("-s5aAutoGate"))
                yield return RunAutoGate();
        }

        private IEnumerator RunAutoGate()
        {
            const float operationTimeout = 12f;
            var checks = new List<S5ARuntimeCheck>();
            var evidence = new List<string>();
            string evidenceDirectory = ArgumentValue("-s5aEvidenceDir");
            string reportPath = ArgumentValue("-s5aReport");
            circulation.SetGatePassengerOverride(true);
            circulation.SetGateDurationMultiplier(0.04f);

            Add(checks, "bootstrap_references", ReferencesComplete(),
                "S5A authority, coordinator, circulation, seven doors, router, recovery, and persistent stair assigned");
            Add(checks, "measured_contract", stairContract.Validate(out string contractDetail), contractDetail);
            Add(checks, "dual_aperture_assets", dualAperture.ValidateAssets(out string apertureDetail), apertureDetail);
            Add(checks, "s3d_environment_retained", approvedS3DBootstrap.EnvironmentReady
                && approvedS3DBootstrap.LoadedPersistentScenes.Count == 7,
                "persistent=" + string.Join(",", approvedS3DBootstrap.LoadedPersistentScenes));
            Add(checks, "single_floor_initial_steady_state",
                floorCoordinator.ValidateSteadyState(out string steadyDetail), steadyDetail);
            Add(checks, "active_floor_separate_from_cabin_floor",
                circulation.ActiveFloorId == "F00" && circulation.CabinFloorId == "F00",
                "active=" + circulation.ActiveFloorId + "; cabin=" + circulation.CabinFloorId);
            Add(checks, "exterior_dual_aperture_applied", ExteriorDualApertureApplied,
                ExteriorBaseBlockerName);

            MeshFilter[] stairMeshes = stairRoot.GetComponentsInChildren<MeshFilter>(true);
            int stepCount = stairMeshes.Count(item => item.name.Contains("_Step_"));
            int floorLandingCount = stairMeshes.Count(item => item.name.Contains("_FloorLanding_"));
            int midLandingCount = stairMeshes.Count(item => item.name.Contains("_MidLanding_"));
            Add(checks, "exact_stair_geometry_counts",
                stepCount == 164 && floorLandingCount == 7 && midLandingCount == 6,
                $"steps={stepCount}; floorLandings={floorLandingCount}; intermediateLandings={midLandingCount}");
            Add(checks, "seven_automatic_sliding_doors", stairDoors.Length == 7
                && stairDoors.Select(item => item.FloorId).Distinct().Count() == 7,
                string.Join(",", stairDoors.Select(item => item.FloorId)));
            Add(checks, "tread_aligned_collision_no_hidden_ramp",
                stairMeshes.Count(item => item.name.Contains("_Step_")
                    && item.GetComponent<Collider>() != null) == 164
                && !stairMeshes.Any(item => item.name.IndexOf("ramp",
                    StringComparison.OrdinalIgnoreCase) >= 0),
                "visible step colliders=164; ramp meshes=0");
            Add(checks, "stair_teleport_excluded",
                !stairRoot.GetComponentsInChildren<MonoBehaviour>(true)
                    .Any(item => item.GetType().Name.IndexOf("TeleportSurface",
                        StringComparison.OrdinalIgnoreCase) >= 0),
                "no teleport-surface component below persistent Stair A root");
            Add(checks, "s5a_sole_enabled_floor_owner",
                FindObjectsByType<S5AFloorSceneCoordinator>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Count(item => item.enabled) == 1
                && FindObjectsByType<S3ElevatorController>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).All(item => !item.enabled),
                "one S5A coordinator; legacy elevator controller disabled after takeover");
            Add(checks, "shared_interaction_interface",
                FindObjectsByType<S5AElevatorInteraction>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length >= 11
                && interactionRouter.InteractionDistanceM >= 5.5f
                && interactionRouter.AimAssistRadiusM >= 0.18f,
                "elevator controls rewired; desktop/PCVR reach retained");

            if (!string.IsNullOrEmpty(evidenceDirectory))
            {
                yield return Capture(evidenceDirectory, "01_S5A_F00_Entrance.png",
                    new Vector3(-18.5f, 0f, -3.76f), -90f, evidence);
                yield return Capture(evidenceDirectory, "02_S5A_Tall_F00_F01_Flight.png",
                    new Vector3(-23.15f, 0f, -4.00f), -15f, evidence);
            }

            S5AStairDoorController f00Door = Door("F00");
            approvedS3DBootstrap.AdaptiveRig.SetPositionSafely(
                new Vector3(-18.4f, 0f, stairContract.DoorCentreZ), Quaternion.identity);
            Physics.SyncTransforms();
            f00Door.SetGateOccupancy(false);
            f00Door.RequestClose();
            yield return WaitDoor(f00Door, false, 5f);
            approvedS3DBootstrap.AdaptiveRig.SetPositionSafely(
                new Vector3(-20.5f, 0f, stairContract.DoorCentreZ), Quaternion.identity);
            Physics.SyncTransforms();
            yield return WaitDoor(f00Door, true, 4f);
            Add(checks, "automatic_f00_external_approach_opens",
                f00Door.FullyOpen && f00Door.IsOccupied,
                $"state={f00Door.State}; occupied={f00Door.IsOccupied}; openness={f00Door.LinearOpenness:F3}");
            approvedS3DBootstrap.AdaptiveRig.SetPositionSafely(
                new Vector3(-18.4f, 0f, stairContract.DoorCentreZ), Quaternion.identity);
            Physics.SyncTransforms();
            f00Door.SetGateOccupancy(false);
            f00Door.RequestClose();
            yield return WaitDoor(f00Door, false, 5f);

            f00Door.SetGateOccupancy(true);
            f00Door.RequestAccess();
            yield return WaitDoor(f00Door, true, 4f);
            f00Door.SetGateOccupancy(false);
            f00Door.RequestClose();
            yield return new WaitForSecondsRealtime(0.25f);
            bool closing = f00Door.State == S5AStairDoorState.Closing;
            int reversals = f00Door.ReversalCount;
            f00Door.SetGateOccupancy(true);
            yield return WaitDoor(f00Door, true, 4f);
            Add(checks, "door_obstruction_reversal", closing && f00Door.FullyOpen
                && f00Door.ReversalCount > reversals,
                $"closingObserved={closing}; reversals={f00Door.ReversalCount}");
            f00Door.SetGateOccupancy(false);
            f00Door.RequestClose();
            yield return WaitDoor(f00Door, false, 4f);

            bool unloadedOpenRejected = !Door("F06").BeginOpenIfSafe()
                && Door("F06").FullyClosed;
            Add(checks, "unloaded_floor_door_stays_closed", unloadedOpenRejected,
                Door("F06").LastResult);

            bool ascent = true;
            for (int index = 1; index <= 6; index++)
            {
                string floorId = "F" + index.ToString("00");
                ascent &= circulation.RequestStairAccess(Door(floorId));
                yield return WaitCirculation(operationTimeout);
                ascent &= circulation.ActiveFloorId == floorId
                    && circulation.CabinFloorId == "F00"
                    && floorCoordinator.ValidateSteadyState(out _);
                Door(floorId).SetGateOccupancy(false);
            }
            Add(checks, "stair_ascent_f00_f06_all_landings", ascent,
                "active=" + circulation.ActiveFloorId + "; cabin=" + circulation.CabinFloorId);
            Add(checks, "stair_does_not_move_elevator", circulation.CabinFloorId == "F00",
                "cabin retained F00 after six stair transitions");
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=STAIR_ASCENT_COMPLETE");
            if (!string.IsNullOrEmpty(evidenceDirectory))
            {
                yield return Capture(evidenceDirectory, "03_S5A_Typical_Upper_Interval.png",
                    new Vector3(-23.15f, 9.7f, -4.00f), -15f, evidence);
                yield return Capture(evidenceDirectory, "04_S5A_F06_Closure.png",
                    new Vector3(-23.15f, 25.3f, -4.00f), -15f, evidence);
                yield return Capture(evidenceDirectory, "05_S5A_Ascent_Complete.png",
                    new Vector3(-24.8f, 25.3f, -3.76f), 90f, evidence);
            }

            bool landingCallAccepted = circulation.RequestLandingCall("F06");
            yield return WaitCirculation(operationTimeout);
            Add(checks, "landing_call_summons_actual_cabin", landingCallAccepted
                && circulation.CabinFloorId == "F06" && circulation.ActiveFloorId == "F06",
                "active=" + circulation.ActiveFloorId + "; cabin=" + circulation.CabinFloorId);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=LANDING_CALL_COMPLETE");

            bool elevatorReturn = circulation.RequestElevatorFloor("F00");
            yield return WaitCirculation(operationTimeout);
            bool elevatorToF06 = circulation.RequestElevatorFloor("F06");
            yield return WaitCirculation(operationTimeout);
            Add(checks, "elevator_f00_f06_shared_coordinator", elevatorReturn && elevatorToF06
                && circulation.ActiveFloorId == "F06" && circulation.CabinFloorId == "F06",
                circulation.LastResult);

            bool descent = true;
            for (int index = 5; index >= 0; index--)
            {
                string floorId = "F" + index.ToString("00");
                descent &= circulation.RequestStairAccess(Door(floorId));
                yield return WaitCirculation(operationTimeout);
                descent &= circulation.ActiveFloorId == floorId
                    && circulation.CabinFloorId == "F06";
            }
            Add(checks, "mixed_elevator_then_stair_descent", descent
                && circulation.ActiveFloorId == "F00" && circulation.CabinFloorId == "F06",
                "active=" + circulation.ActiveFloorId + "; cabin=" + circulation.CabinFloorId);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=MIXED_DESCENT_COMPLETE");
            if (!string.IsNullOrEmpty(evidenceDirectory))
                yield return Capture(evidenceDirectory, "06_S5A_Mixed_Return_To_Lobby.png",
                    new Vector3(-24.8f, 0f, -3.76f), 90f, evidence);

            bool callF00 = circulation.RequestLandingCall("F00");
            yield return WaitCirculation(operationTimeout);
            Add(checks, "post_stair_landing_call_no_cabin_teleport", callF00
                && circulation.CabinFloorId == "F00", circulation.LastResult);

            floorCoordinator.InjectNextDestinationLoadFailureForGate();
            bool injectedLoadAccepted = circulation.RequestStairAccess(Door("F01"));
            yield return WaitCirculation(operationTimeout);
            Add(checks, "destination_load_failure_rolls_back_closed", injectedLoadAccepted
                && circulation.ActiveFloorId == "F00" && Door("F01").FullyClosed
                && !floorCoordinator.LastTransitionSucceeded,
                floorCoordinator.LastResult);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=LOAD_FAILURE_COMPLETE");

            floorCoordinator.InjectNextOriginUnloadFailureForGate();
            bool injectedUnloadAccepted = circulation.RequestStairAccess(Door("F01"));
            yield return WaitCirculation(operationTimeout);
            Add(checks, "origin_unload_failure_rolls_back", injectedUnloadAccepted
                && circulation.ActiveFloorId == "F00" && Door("F01").FullyClosed,
                floorCoordinator.LastResult);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=UNLOAD_FAILURE_COMPLETE");

            floorCoordinator.InjectNextMissingApertureFailureForGate();
            bool missingApertureAccepted = circulation.RequestStairAccess(Door("F01"));
            yield return WaitCirculation(operationTimeout);
            Add(checks, "missing_aperture_rejected_before_door_open", missingApertureAccepted
                && circulation.ActiveFloorId == "F00" && Door("F01").FullyClosed,
                floorCoordinator.LastResult);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=MISSING_APERTURE_COMPLETE");

            bool firstConcurrent = circulation.RequestStairAccess(Door("F01"));
            bool conflictingRejected = !circulation.RequestElevatorFloor("F02");
            yield return WaitCirculation(operationTimeout);
            Add(checks, "concurrent_requests_serialized", firstConcurrent && conflictingRejected
                && circulation.ActiveFloorId == "F01", circulation.LastResult);
            circulation.RequestStairAccess(Door("F00"));
            yield return WaitCirculation(operationTimeout);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_GATE_PHASE=FAILURE_MATRIX_COMPLETE");

            Vector3 safe = new Vector3(-25.5f, 0f, -3.76f);
            recoveryCoordinator.SetSafePointForGate(safe, "F00 landing");
            approvedS3DBootstrap.AdaptiveRig.SetPositionSafely(new Vector3(-25.5f, -5f, -3.76f),
                Quaternion.identity);
            bool recovered = recoveryCoordinator.RecoverNow();
            Add(checks, "fall_recovery_latest_safe_landing", recovered
                && Vector3.Distance(approvedS3DBootstrap.AdaptiveRig.transform.position, safe) <= 0.02f,
                "safe=" + recoveryCoordinator.LatestSafeLabel);

            Add(checks, "transition_scene_cardinality", floorCoordinator.MaximumSimultaneousFloorCount == 2,
                "maximum=" + floorCoordinator.MaximumSimultaneousFloorCount);
            Add(checks, "final_floor_steady_f00", floorCoordinator.ValidateSteadyState(out string finalDetail)
                && circulation.ActiveFloorId == "F00", finalDetail);
            Add(checks, "s3d_entrance_regression", approvedS3DBootstrap.EntranceDoors
                .ValidateMetadata(out string entranceDetail), entranceDetail);
            Add(checks, "s3d_lawn_collision_regression", approvedS3DBootstrap.SiteCollisionCoordinator
                .ValidateGrassSupport(out string lawnDetail), lawnDetail);

            int failed = checks.Count(item => item.status == "FAIL");
            var report = new S5ARuntimeGateReport
            {
                status = failed == 0 ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passed = checks.Count - failed,
                failed = failed,
                total = checks.Count,
                activeFloor = circulation.ActiveFloorId,
                cabinFloor = circulation.CabinFloorId,
                maximumSimultaneousFloors = floorCoordinator.MaximumSimultaneousFloorCount,
                evidenceFiles = evidence.ToArray(),
                checks = checks.ToArray(),
            };
            if (!string.IsNullOrEmpty(reportPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_RUNTIME_GATE=" + report.status
                + $"; {report.passed}/{report.total}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(failed == 0 ? 0 : 1);
        }

        private bool ApplyExteriorDualAperture(out string detail)
        {
            MeshFilter[] candidates = FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,
                FindObjectsSortMode.None).Where(item => item.name == ExteriorBaseBlockerName).ToArray();
            if (candidates.Length != 1 || stairContract.ExteriorDualApertureStructure == null)
            {
                detail = "blockers=" + candidates.Length + "; asset="
                    + (stairContract.ExteriorDualApertureStructure != null);
                return false;
            }
            MeshFilter filter = candidates[0];
            filter.sharedMesh = stairContract.ExteriorDualApertureStructure;
            MeshCollider collider = filter.GetComponent<MeshCollider>();
            if (collider != null)
            {
                collider.sharedMesh = null;
                collider.sharedMesh = stairContract.ExteriorDualApertureStructure;
            }
            ExteriorDualApertureApplied = true;
            Physics.SyncTransforms();
            detail = "applied " + stairContract.ExteriorDualApertureStructure.name;
            return true;
        }

        private bool ReferencesComplete()
            => approvedS3DBootstrap != null && stairContract != null && dualAperture != null
                && floorCoordinator != null && circulation != null && stairDoors.Length == 7
                && stairDoors.All(item => item != null) && interactionRouter != null
                && recoveryCoordinator != null && stairRoot != null;

        private S5AStairDoorController Door(string floorId)
            => stairDoors.Single(item => item.FloorId == floorId);

        private IEnumerator WaitCirculation(float timeoutSeconds)
        {
            float timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (circulation.IsBusy && Time.realtimeSinceStartup < timeout)
                yield return null;
            if (circulation.IsBusy)
                Debug.LogError("HOSPITAL_INTERIOR_R03_S5A_GATE_OPERATION_TIMEOUT: "
                    + circulation.ActiveOperation + "; " + circulation.LastResult
                    + "; coordinator=" + floorCoordinator.State + "/"
                    + floorCoordinator.DestinationFloorId + "; " + floorCoordinator.LastResult);
        }

        private static IEnumerator WaitDoor(S5AStairDoorController door, bool open,
            float timeoutSeconds)
        {
            float timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while ((open ? !door.FullyOpen : !door.FullyClosed)
                && Time.realtimeSinceStartup < timeout)
                yield return null;
        }

        private IEnumerator Capture(string directory, string filename, Vector3 position,
            float yaw, ICollection<string> evidence)
        {
            Directory.CreateDirectory(directory);
            approvedS3DBootstrap.AdaptiveRig.SetPositionSafely(position,
                Quaternion.Euler(0f, yaw, 0f));
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(directory, filename);
            Camera camera = approvedS3DBootstrap.IntegrationCamera;
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = target;
            RenderTexture.active = target;
            camera.Render();
            image.ReadPixels(new Rect(0f, 0f, 1280f, 720f), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(target);
            Destroy(image);
            if (File.Exists(path) && new FileInfo(path).Length > 20000)
                evidence.Add(filename);
        }

        private static void Add(ICollection<S5ARuntimeCheck> checks, string id,
            bool passed, string detail)
            => checks.Add(new S5ARuntimeCheck
            {
                id = id,
                status = passed ? "PASS" : "FAIL",
                detail = detail,
            });

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S5A_FATAL: " + message, this);
        }

        private static bool HasArgument(string argument)
            => Environment.GetCommandLineArgs().Any(item => item == argument);

        private static string ArgumentValue(string argument)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length - 1; index++)
                if (arguments[index] == argument)
                    return arguments[index + 1];
            return string.Empty;
        }
    }
}
