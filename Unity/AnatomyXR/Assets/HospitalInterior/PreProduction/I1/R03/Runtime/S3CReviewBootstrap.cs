using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S3CRuntimeCheck
    {
        public string name;
        public bool pass;
        public string detail;
    }

    [Serializable]
    public sealed class S3CTravelRecord
    {
        public string origin;
        public string destination;
        public bool accepted;
        public bool exactDatum;
        public bool oneActiveFloor;
        public bool passengerStayedInCabin;
        public float finalCabinElevationM;
        public float passengerDriftM;
    }

    [Serializable]
    public sealed class S3CRuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S3C.RuntimeGate.v1";
        public string status;
        public string unityVersion;
        public int passCount;
        public int totalCount;
        public string[] route;
        public string[] loadedProductionScenes;
        public S3CTravelRecord[] travelRecords;
        public S3CRuntimeCheck[] checks;
    }

    [DisallowMultipleComponent]
    public sealed class S3CReviewBootstrap : MonoBehaviour
    {
        public const string ExteriorBaseSceneName = "Exterior_Base";
        public const string ExteriorBaseBlockerName = "UE_EXT_GlobalStructure_OPAQUE_ALL_02";

        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private S3ElevatorController elevatorController;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S3CReviewLocomotion locomotion;
        [SerializeField] private S3ShaftStructure shaftStructure;
        [SerializeField] private S3ShaftFloorAperture floorAperture;
        [SerializeField] private S3CabinObservationDesign cabinObservationDesign;
        [SerializeField] private GameObject reviewObstructionProxy;
        [SerializeField] private Mesh exteriorBaseShaftCutMesh;

        private readonly List<string> loadedProductionScenes = new List<string>();

        public bool EnvironmentReady { get; private set; }
        public string FatalError { get; private set; } = string.Empty;
        public int HiddenExteriorBackerCount { get; private set; }
        public int HiddenExteriorBalconyFloorSkinCount { get; private set; }
        public S3ElevatorContract ElevatorContract => elevatorContract;
        public S3ElevatorController ElevatorController => elevatorController;
        public XROrigin XrOrigin => xrOrigin;
        public Camera ReviewCamera => reviewCamera;
        public S3CReviewLocomotion Locomotion => locomotion;
        public S3ShaftStructure ShaftStructure => shaftStructure;
        public S3ShaftFloorAperture FloorAperture => floorAperture;
        public S3CabinObservationDesign CabinObservationDesign => cabinObservationDesign;
        public Mesh ExteriorBaseShaftCutMesh => exteriorBaseShaftCutMesh;
        public bool ExteriorBaseShaftApertureApplied { get; private set; }
        public string ExteriorBaseShaftApertureDetail { get; private set; } = "not applied";
        public bool ReviewObstructionEnabled => reviewObstructionProxy != null
            && reviewObstructionProxy.activeSelf;

        public void Configure(S3ElevatorContract contract, S3ElevatorController controller,
            XROrigin origin, Camera camera, S3CReviewLocomotion reviewLocomotion,
            S3ShaftStructure shaft, S3ShaftFloorAperture aperture,
            S3CabinObservationDesign observationDesign, GameObject obstructionProxy,
            Mesh exteriorShaftCutMesh)
        {
            elevatorContract = contract;
            elevatorController = controller;
            xrOrigin = origin;
            reviewCamera = camera;
            locomotion = reviewLocomotion;
            shaftStructure = shaft;
            floorAperture = aperture;
            cabinObservationDesign = observationDesign;
            reviewObstructionProxy = obstructionProxy;
            exteriorBaseShaftCutMesh = exteriorShaftCutMesh;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            if (!ReferencesComplete())
            {
                Fail("S3C bootstrap references are incomplete.");
                yield break;
            }
            bool spatialValid = elevatorContract.ValidateSpatialFit(out string spatialDetail);
            bool shaftContractValid = elevatorContract.ValidateShaftFit(out string shaftContractDetail);
            bool shaftValid = shaftStructure.ValidateStructure(out string shaftDetail);
            bool sweepValid = shaftStructure.ValidateCabinSweep(out string sweepDetail);
            bool glassValid = shaftStructure.ValidateGlassDesign(out string glassDetail);
            bool observationValid = cabinObservationDesign.ValidateObservationDesign(out string observationDetail);
            bool nestingValid = cabinObservationDesign.ValidateNestedInShaft(shaftStructure, out string nestingDetail);
            bool apertureValid = floorAperture.ValidateAssets(out string apertureDetail);
            if (!spatialValid || !shaftContractValid || !shaftValid || !sweepValid
                || !glassValid || !observationValid || !nestingValid || !apertureValid)
            {
                Fail("Locked S3B inputs are invalid: " + spatialDetail + "; " + shaftContractDetail
                    + "; " + shaftDetail + "; " + sweepDetail + "; " + glassDetail + "; "
                    + observationDetail + "; " + nestingDetail + "; " + apertureDetail);
                yield break;
            }

            foreach (string path in elevatorContract.ApprovedS2FloorContract.ProductionScenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    Fail("Duplicate/preloaded production scene: " + sceneName);
                    yield break;
                }
                AsyncOperation operation = LoadSceneAdditive(path);
                if (operation == null)
                {
                    Fail("Could not begin production scene load: " + path);
                    yield break;
                }
                while (!operation.isDone)
                    yield return null;
                loadedProductionScenes.Add(sceneName);
            }

            if (!ApplyExteriorBaseShaftAperture(out string exteriorApertureDetail))
            {
                Fail("Could not clear the F00 exterior structure from the E01 hoistway: "
                    + exteriorApertureDetail);
                yield break;
            }

            string initialFloorId = elevatorContract.ApprovedS2FloorContract.InitialFloorId;
            if (!elevatorContract.ApprovedS2FloorContract.TryGetFloor(initialFloorId,
                out S2FloorRecord initialFloor))
            {
                Fail("Approved initial floor is unavailable.");
                yield break;
            }
            AsyncOperation floorLoad = LoadSceneAdditive(initialFloor.scenePath);
            if (floorLoad == null)
            {
                Fail("Could not begin approved F00 floor load.");
                yield break;
            }
            while (!floorLoad.isDone)
                yield return null;
            Scene floorScene = SceneManager.GetSceneByPath(initialFloor.scenePath);
            if (!floorScene.isLoaded)
            {
                Fail("Approved F00 floor did not become loaded.");
                yield break;
            }
            SceneManager.SetActiveScene(floorScene);

            HiddenExteriorBackerCount = S2ManualFloorLoader.HideExteriorOnlyBalconyBackers();
            HiddenExteriorBalconyFloorSkinCount = S2ManualFloorLoader.HideExteriorOverlappingBalconyFloorSkins();
            if (HiddenExteriorBackerCount != S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length
                || HiddenExteriorBalconyFloorSkinCount != S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length)
            {
                Fail("Approved exterior suppression contract did not match S2.");
                yield break;
            }
            if (!elevatorController.InitializeAtFloor(initialFloorId, floorScene, out string initializeDetail))
            {
                Fail("E01 initialization failed: " + initializeDetail);
                yield break;
            }

            Physics.SyncTransforms();
            EnvironmentReady = true;
            if (HasArgument("-s3cAutoGate"))
                yield return RunAutoGate();
            else
                elevatorController.RequestDoorOpen();
        }

        public void ToggleReviewObstruction()
            => SetReviewObstruction(!ReviewObstructionEnabled);

        public void SetReviewObstruction(bool obstructed)
        {
            if (reviewObstructionProxy != null)
                reviewObstructionProxy.SetActive(obstructed);
            if (elevatorController?.CurrentDoorController?.ObstructionSensor != null)
                elevatorController.CurrentDoorController.ObstructionSensor.SetReviewForcedObstruction(obstructed);
            Physics.SyncTransforms();
        }

        private IEnumerator RunAutoGate()
        {
            var checks = new List<S3CRuntimeCheck>();
            var travelRecords = new List<S3CTravelRecord>();
            string[] route = { "F00", "F01", "F02", "F03", "F04", "F05", "F06", "F00" };
            elevatorController.SetGateManualTick(true);
            SetReviewObstruction(false);
            elevatorController.CurrentDoorController.SetImmediateClosedForGate();

            Add(checks, "bootstrap_references", ReferencesComplete(),
                "Contract, travel controller, rig, shaft, aperture, observation cabin, and obstruction proxy are assigned.");
            Add(checks, "exact_production_scene_set", loadedProductionScenes.SequenceEqual(
                    elevatorContract.ApprovedS2FloorContract.ProductionScenePaths.Select(Path.GetFileNameWithoutExtension)),
                string.Join(",", loadedProductionScenes));
            Add(checks, "no_s2_or_s3b_review_controller",
                FindObjectsByType<S2ManualFloorLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0
                && FindObjectsByType<S3BReviewBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0
                && FindObjectsByType<S3BReviewLocomotion>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0,
                "S3C is the only active floor-travel review system.");
            bool shaftContract = elevatorContract.ValidateShaftFit(out string shaftContractDetail);
            bool shaftGeometry = shaftStructure.ValidateStructure(out string shaftStructureDetail);
            bool shaftSweep = shaftStructure.ValidateCabinSweep(out string sweepDetail);
            bool shaft = shaftContract && shaftGeometry && shaftSweep;
            Add(checks, "locked_s3b_shaft_and_sweep", shaft,
                shaftContractDetail + "; " + shaftStructureDetail + "; " + sweepDetail);
            bool shaftGlass = shaftStructure.ValidateGlassDesign(out string glassDetail);
            bool cabinObservation = cabinObservationDesign.ValidateObservationDesign(out string observationDetail);
            bool cabinNested = cabinObservationDesign.ValidateNestedInShaft(shaftStructure, out string nestingDetail);
            bool observation = shaftGlass && cabinObservation && cabinNested;
            Add(checks, "locked_glass_observation_design", observation,
                glassDetail + "; " + observationDetail + "; " + nestingDetail);
            Add(checks, "seven_floor_door_controllers", elevatorController.FloorDoorControllers.Length == 7
                && elevatorController.FloorDoorControllers.All(item => item != null)
                && elevatorController.FloorDoorControllers.Count(item => item.enabled) == 1,
                "controllers=" + elevatorController.FloorDoorControllers.Length);
            string initialApertureDetail = "initial floor marker mismatch";
            Add(checks, "single_initial_floor_and_aperture", LoadedFloorMarkers().Length == 1
                && LoadedFloorMarkers()[0].FloorId == "F00"
                && floorAperture.ValidateAppliedToFloorScene(SceneManager.GetSceneByPath(
                    elevatorContract.ApprovedS2FloorContract.Floors[0].scenePath), "F00", out initialApertureDetail),
                initialApertureDetail);
            Add(checks, "exterior_runtime_suppression_preserved",
                HiddenExteriorBackerCount == S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length
                && HiddenExteriorBalconyFloorSkinCount == S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length,
                $"backers={HiddenExteriorBackerCount}; skins={HiddenExteriorBalconyFloorSkinCount}");
            Add(checks, "exterior_base_f00_hoistway_aperture_applied",
                ExteriorBaseShaftApertureApplied, ExteriorBaseShaftApertureDetail);

            S3ElevatorButton[] buttons = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            Add(checks, "all_physical_controls_route_to_s3c", buttons.Length == 16
                && buttons.All(item => item.ElevatorController == elevatorController && item.DoorController == null)
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.Floor) == 7
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.LandingCall) == 7,
                "buttons=" + buttons.Length);

            bool outsideRejected = !elevatorController.RequestFloor("F01");
            bool invalidRejected = !elevatorController.RequestFloor("F99");
            Add(checks, "outside_cabin_and_invalid_requests_rejected", outsideRejected && invalidRejected,
                elevatorController.LastResult);
            PlacePassengerInsideCabin();
            Add(checks, "passenger_physically_inside_cabin_before_route",
                elevatorController.IsPassengerInsideCabin(), xrOrigin.transform.position.ToString("F3"));

            elevatorController.CurrentDoorController.SetImmediateOpenForGate();
            SetReviewObstruction(true);
            bool obstructionRejected = !elevatorController.RequestFloor("F01");
            SetReviewObstruction(false);
            Add(checks, "obstruction_rejects_travel", obstructionRejected
                && elevatorController.CommittedFloorId == "F00", elevatorController.LastResult);
            elevatorController.CurrentDoorController.SetImmediateClosedForGate();

            string evidenceDir = ArgumentValue("-s3cEvidenceDir");
            if (!string.IsNullOrWhiteSpace(evidenceDir))
                yield return CaptureScreenshot(Path.Combine(evidenceDir, "01_F00_Ready.png"));

            for (int i = 1; i < route.Length; i++)
            {
                string origin = elevatorController.CommittedFloorId;
                string target = route[i];
                Vector3 offsetBefore = xrOrigin.transform.position - elevatorController.CabinRoot.position;
                float driftBefore = elevatorController.MaximumPassengerDriftM;
                bool accepted = elevatorController.RequestFloor(target);
                yield return WaitUntilTravelSettles(60f);
                yield return OpenCurrentDoorForGate();
                bool endpoint = elevatorController.ValidateEndpoint(out string endpointDetail);
                bool exact = elevatorContract.ApprovedS2FloorContract.TryGetFloor(target,
                    out S2FloorRecord expected) && Mathf.Abs(elevatorController.CabinFloorElevationM
                    - expected.elevationM) <= 0.001f;
                bool passenger = elevatorController.IsPassengerInsideCabin()
                    && Vector3.Distance(offsetBefore, xrOrigin.transform.position
                        - elevatorController.CabinRoot.position) <= 0.002f;
                bool indicator = elevatorController.CabinTravelIndicator != null
                    && elevatorController.CabinTravelIndicator.text == target;
                travelRecords.Add(new S3CTravelRecord
                {
                    origin = origin,
                    destination = target,
                    accepted = accepted,
                    exactDatum = exact,
                    oneActiveFloor = LoadedFloorMarkers().Length == 1
                        && LoadedFloorMarkers()[0].FloorId == target,
                    passengerStayedInCabin = passenger,
                    finalCabinElevationM = elevatorController.CabinFloorElevationM,
                    passengerDriftM = elevatorController.MaximumPassengerDriftM - driftBefore,
                });
                Add(checks, target + "_travel_endpoint",
                    accepted && endpoint && exact && passenger && indicator,
                    endpointDetail + "; passenger=" + passenger + "; indicator="
                        + (elevatorController.CabinTravelIndicator == null ? "missing"
                            : elevatorController.CabinTravelIndicator.text));
                bool landingSafe = elevatorController.ValidateLandingDoorSafety(out string landingSafetyDetail);
                Add(checks, target + "_noncurrent_landings_locked", landingSafe, landingSafetyDetail);
                if (!string.IsNullOrWhiteSpace(evidenceDir) && target == "F03")
                    yield return CaptureScreenshot(Path.Combine(evidenceDir, "02_F03_Arrival.png"));
                if (!string.IsNullOrWhiteSpace(evidenceDir) && target == "F06")
                    yield return CaptureScreenshot(Path.Combine(evidenceDir, "03_F06_Arrival.png"));
            }

            Add(checks, "required_f00_f06_f00_route_complete", travelRecords.Count == 7
                && travelRecords.All(item => item.accepted && item.exactDatum && item.oneActiveFloor
                    && item.passengerStayedInCabin)
                && elevatorController.CompletedTravelCount == 7,
                string.Join(" -> ", route));
            Add(checks, "passenger_never_teleported_to_s2_anchor",
                elevatorController.MaximumPassengerDriftM <= 0.002f,
                "maximum cabin-relative drift=" + elevatorController.MaximumPassengerDriftM.ToString("F4") + " m");
            Add(checks, "correct_cabin_indicator_after_route",
                elevatorController.CabinTravelIndicator != null
                && elevatorController.CabinTravelIndicator.text == "F00",
                elevatorController.CabinTravelIndicator == null ? "missing"
                    : elevatorController.CabinTravelIndicator.text);

            bool sameFloorAccepted = elevatorController.RequestFloor("F00");
            yield return OpenCurrentDoorForGate();
            Add(checks, "same_floor_request_reopens", sameFloorAccepted
                && elevatorController.CurrentDoorController.FullyOpen
                && elevatorController.CommittedFloorId == "F00", elevatorController.LastResult);

            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            bool busyPrimary = elevatorController.RequestFloor("F01");
            bool busyRejected = !elevatorController.RequestFloor("F02");
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            Add(checks, "busy_request_rejected", busyPrimary && busyRejected
                && elevatorController.CommittedFloorId == "F01", elevatorController.LastResult);
            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            elevatorController.RequestFloor("F00");
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();

            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            bool cancelTravelAccepted = elevatorController.RequestFloor("F02");
            yield return WaitForElevatorState(S3ElevatorState.Moving, 30f);
            bool cancelAccepted = elevatorController.CancelActiveTravel();
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            bool cancellationEndpoint = elevatorController.ValidateEndpoint(out string cancellationDetail);
            Add(checks, "cancellation_rolls_back_to_committed_floor", cancelTravelAccepted && cancelAccepted
                && cancellationEndpoint && elevatorController.CommittedFloorId == "F00"
                && elevatorController.ActiveFloorId == "F00", cancellationDetail);

            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            elevatorController.InjectNextDestinationLoadFailureForGate();
            bool failedLoadRequestAccepted = elevatorController.RequestFloor("F01");
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            bool loadFailureEndpoint = elevatorController.ValidateEndpoint(out string loadFailureDetail);
            Add(checks, "load_failure_rolls_back_without_motion", failedLoadRequestAccepted
                && loadFailureEndpoint && elevatorController.CommittedFloorId == "F00"
                && elevatorController.ActiveFloorId == "F00", loadFailureDetail);

            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            bool faultTravelAccepted = elevatorController.RequestFloor("F01");
            yield return WaitForElevatorState(S3ElevatorState.Moving, 30f);
            elevatorController.InjectNextRecoveryFailureForGate();
            bool faultCancelAccepted = elevatorController.CancelActiveTravel();
            yield return WaitUntilTravelSettles(60f);
            bool enclosedFault = elevatorController.IsFaultedSafe
                && elevatorController.CurrentDoorController.FullyClosed
                && elevatorController.CurrentDoorController.CabinInterlock
                && elevatorController.CurrentDoorController.LandingInterlock;
            Add(checks, "failed_rollback_enters_enclosed_faulted_safe", faultTravelAccepted
                && faultCancelAccepted && enclosedFault, elevatorController.LastFault);
            bool reviewRecoveryAccepted = elevatorController.RequestReviewRecovery();
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            bool recoveredEndpoint = elevatorController.ValidateEndpoint(out string recoveredDetail);
            Add(checks, "review_only_fault_recovery", reviewRecoveryAccepted && recoveredEndpoint
                && elevatorController.CommittedFloorId == "F00" && elevatorController.FaultCount == 1,
                recoveredDetail);

            bool landingCallAccepted = elevatorController.RequestLandingCall("F00");
            bool remoteLandingRejected = !elevatorController.RequestLandingCall("F06");
            yield return OpenCurrentDoorForGate();
            Add(checks, "landing_call_reopens_present_and_rejects_inactive_remote",
                landingCallAccepted && remoteLandingRejected
                && elevatorController.CurrentDoorController.FullyOpen, elevatorController.LastResult);
            Add(checks, "door_timing_and_clearance_preserved",
                Mathf.Abs(elevatorContract.DoorOpenSeconds - 1.2f) <= 0.001f
                && Mathf.Abs(elevatorContract.DoorDwellSeconds - 4f) <= 0.001f
                && Mathf.Abs(elevatorContract.DoorCloseSeconds - 1.2f) <= 0.001f
                && elevatorController.CurrentDoorController.MeasuredClearWidthM
                    >= elevatorContract.ClearDoorM.x,
                $"open/dwell/close={elevatorContract.DoorOpenSeconds:F1}/{elevatorContract.DoorDwellSeconds:F1}/{elevatorContract.DoorCloseSeconds:F1}; clear={elevatorController.CurrentDoorController.MeasuredClearWidthM:F3}");
            Add(checks, "review_scale_and_controls",
                Mathf.Abs(elevatorContract.ApprovedS2FloorContract.EyeHeightM - 1.7f) <= 0.001f
                && Mathf.Abs(locomotion.WalkSpeedMps - 2f) <= 0.001f
                && Mathf.Abs(locomotion.SprintSpeedMps - 4f) <= 0.001f,
                $"eye={elevatorContract.ApprovedS2FloorContract.EyeHeightM:F1}; walk={locomotion.WalkSpeedMps:F1}; sprint={locomotion.SprintSpeedMps:F1}");

            if (!string.IsNullOrWhiteSpace(evidenceDir))
                yield return CaptureScreenshot(Path.Combine(evidenceDir, "04_F00_Return.png"));
            SetReviewObstruction(false);
            elevatorController.SetGateManualTick(false);

            string output = ArgumentValue("-s3cReportPath");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Application.persistentDataPath,
                    "StageI1_R03_S3C_RuntimeGate.json");
            var report = new S3CRuntimeGateReport
            {
                status = checks.All(item => item.pass) && string.IsNullOrEmpty(FatalError)
                    ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                route = route,
                loadedProductionScenes = loadedProductionScenes.ToArray(),
                travelRecords = travelRecords.ToArray(),
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))
                ?? Application.persistentDataPath);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3C_RUNTIME_GATE={report.status}; {report.passCount}/{report.totalCount}; {output}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }

        private IEnumerator WaitUntilTravelSettles(float timeout)
        {
            float elapsed = 0f;
            while (elevatorController.IsBusy && elapsed < timeout)
            {
                elapsed += 0.08f;
                yield return null;
            }
        }

        private IEnumerator WaitForElevatorState(S3ElevatorState expected, float timeout)
        {
            float elapsed = 0f;
            while (elevatorController.State != expected && elevatorController.IsBusy
                && elapsed < timeout)
            {
                elapsed += 0.08f;
                yield return null;
            }
        }

        private IEnumerator OpenCurrentDoorForGate()
        {
            float elapsed = 0f;
            S3DoorController door = elevatorController.CurrentDoorController;
            if (door.State == S3ElevatorState.ReadyClosed)
                door.RequestOpen();
            while (!door.FullyOpen && elapsed < 2f)
            {
                elevatorController.AdvanceCurrentDoorForGate(0.08f);
                elapsed += 0.08f;
                yield return null;
            }
        }

        private IEnumerator CaptureScreenshot(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))
                ?? Application.persistentDataPath);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
        }

        private void PlacePassengerInsideCabin()
        {
            CharacterController character = xrOrigin.GetComponent<CharacterController>();
            if (character != null)
                character.enabled = false;
            xrOrigin.transform.position = new Vector3(elevatorContract.CabinCenterXZ.x,
                elevatorController.CabinFloorElevationM + 0.03f,
                elevatorContract.CabinCenterXZ.y - 0.35f);
            if (character != null)
                character.enabled = true;
            Physics.SyncTransforms();
        }

        public bool ApplyExteriorBaseShaftAperture(out string detail)
        {
            ExteriorBaseShaftApertureApplied = false;
            if (exteriorBaseShaftCutMesh == null)
            {
                detail = "shaft-cut exterior mesh is missing";
                ExteriorBaseShaftApertureDetail = detail;
                return false;
            }
            Scene exteriorBase = SceneManager.GetSceneByName(ExteriorBaseSceneName);
            if (!exteriorBase.IsValid() || !exteriorBase.isLoaded)
            {
                detail = ExteriorBaseSceneName + " is not loaded";
                ExteriorBaseShaftApertureDetail = detail;
                return false;
            }
            MeshFilter[] targets = exteriorBase.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<MeshFilter>(true))
                .Where(item => item.name == ExteriorBaseBlockerName).ToArray();
            if (targets.Length != 1)
            {
                detail = "expected one " + ExteriorBaseBlockerName + "; found " + targets.Length;
                ExteriorBaseShaftApertureDetail = detail;
                return false;
            }
            targets[0].sharedMesh = exteriorBaseShaftCutMesh;
            foreach (MeshCollider collider in targets[0].GetComponents<MeshCollider>())
                collider.sharedMesh = exteriorBaseShaftCutMesh;
            Physics.SyncTransforms();
            ExteriorBaseShaftApertureApplied = targets[0].sharedMesh == exteriorBaseShaftCutMesh;
            detail = "target=" + targets[0].name + "; mesh=" + targets[0].sharedMesh.name
                + "; opening=X 0.300..3.000 Z 3.000..5.700";
            ExteriorBaseShaftApertureDetail = detail;
            return ExteriorBaseShaftApertureApplied;
        }

        private bool ReferencesComplete() => elevatorContract != null && elevatorController != null
            && xrOrigin != null && reviewCamera != null && locomotion != null
            && shaftStructure != null && floorAperture != null
            && cabinObservationDesign != null && reviewObstructionProxy != null
            && exteriorBaseShaftCutMesh != null;

        private S2FloorSceneMarker[] LoadedFloorMarkers()
            => FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S3C_RUNTIME_FAIL: " + message, this);
        }

        private static AsyncOperation LoadSceneAdditive(string path)
        {
#if UNITY_EDITOR
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
#endif
        }

        private static void Add(ICollection<S3CRuntimeCheck> checks, string name,
            bool pass, string detail)
            => checks.Add(new S3CRuntimeCheck { name = name, pass = pass, detail = detail });

        private static bool HasArgument(string key)
            => Environment.GetCommandLineArgs().Any(item => string.Equals(item, key,
                StringComparison.OrdinalIgnoreCase));

        private static string ArgumentValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return string.Empty;
        }
    }
}
