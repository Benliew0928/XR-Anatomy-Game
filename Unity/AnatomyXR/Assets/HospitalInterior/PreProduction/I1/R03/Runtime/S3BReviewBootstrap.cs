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
    public sealed class S3BRuntimeCheck
    {
        public string name;
        public bool pass;
        public string detail;
    }

    [Serializable]
    public sealed class S3BRuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S3B.RuntimeGate.v4";
        public string status;
        public string unityVersion;
        public int passCount;
        public int totalCount;
        public string currentFloorId;
        public S3BRuntimeCheck[] checks;
    }

    [DisallowMultipleComponent]
    public sealed class S3BReviewBootstrap : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private S3DoorController doorController;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S3BReviewLocomotion locomotion;
        [SerializeField] private Transform cabinModelRoot;
        [SerializeField] private Transform[] landingPortalRoots = Array.Empty<Transform>();
        [SerializeField] private Transform[] nonCurrentLandingDoors = Array.Empty<Transform>();
        [SerializeField] private GameObject reviewObstructionProxy;
        [SerializeField] private S3ShaftStructure shaftStructure;
        [SerializeField] private S3ShaftFloorAperture floorAperture;
        [SerializeField] private S3CabinObservationDesign cabinObservationDesign;

        private readonly List<string> loadedProductionScenes = new List<string>();

        public bool EnvironmentReady { get; private set; }
        public string FatalError { get; private set; } = string.Empty;
        public string CurrentFloorId { get; private set; } = "F00";
        public int HiddenExteriorBackerCount { get; private set; }
        public int HiddenExteriorBalconyFloorSkinCount { get; private set; }
        public S3ElevatorContract ElevatorContract => elevatorContract;
        public S3DoorController DoorController => doorController;
        public XROrigin XrOrigin => xrOrigin;
        public Camera ReviewCamera => reviewCamera;
        public S3BReviewLocomotion Locomotion => locomotion;
        public Transform CabinModelRoot => cabinModelRoot;
        public Transform[] LandingPortalRoots => landingPortalRoots;
        public Transform[] NonCurrentLandingDoors => nonCurrentLandingDoors;
        public bool ReviewObstructionEnabled => reviewObstructionProxy != null && reviewObstructionProxy.activeSelf;
        public S3ShaftStructure ShaftStructure => shaftStructure;
        public S3ShaftFloorAperture FloorAperture => floorAperture;
        public S3CabinObservationDesign CabinObservationDesign => cabinObservationDesign;

        public void Configure(S3ElevatorContract contract, S3DoorController doors, XROrigin origin, Camera camera,
            S3BReviewLocomotion reviewLocomotion, Transform cabinRoot, Transform[] portalRoots,
            Transform[] lockedLandingDoors, GameObject obstructionProxy, S3ShaftStructure shaft,
            S3ShaftFloorAperture aperture, S3CabinObservationDesign observationDesign)
        {
            elevatorContract = contract;
            doorController = doors;
            xrOrigin = origin;
            reviewCamera = camera;
            locomotion = reviewLocomotion;
            cabinModelRoot = cabinRoot;
            landingPortalRoots = portalRoots ?? Array.Empty<Transform>();
            nonCurrentLandingDoors = lockedLandingDoors ?? Array.Empty<Transform>();
            reviewObstructionProxy = obstructionProxy;
            shaftStructure = shaft;
            floorAperture = aperture;
            cabinObservationDesign = observationDesign;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            if (!ReferencesComplete())
            {
                Fail("S3B bootstrap references are incomplete.");
                yield break;
            }
            if (!elevatorContract.ValidateSpatialFit(out string spatialDetail))
            {
                Fail("S3B contract does not fit the approved core: " + spatialDetail);
                yield break;
            }
            bool shaftContractFits = elevatorContract.ValidateShaftFit(out string shaftDetail);
            bool shaftStructureFits = shaftStructure.ValidateStructure(out string structureDetail);
            bool shaftSweepFits = shaftStructure.ValidateCabinSweep(out string sweepDetail);
            bool shaftGlassFits = shaftStructure.ValidateGlassDesign(out string glassDetail);
            bool cabinObservationFits = cabinObservationDesign.ValidateObservationDesign(out string cabinObservationDetail);
            bool cabinNested = cabinObservationDesign.ValidateNestedInShaft(shaftStructure, out string cabinNestingDetail);
            bool apertureAssetsFit = floorAperture.ValidateAssets(out string apertureAssetDetail);
            if (!shaftContractFits || !shaftStructureFits || !shaftSweepFits || !shaftGlassFits
                || !cabinObservationFits || !cabinNested || !apertureAssetsFit)
            {
                Fail("S3B continuous shaft is invalid: " + shaftDetail + "; " + structureDetail + "; "
                    + sweepDetail + "; " + glassDetail + "; " + cabinObservationDetail + "; "
                    + cabinNestingDetail + "; " + apertureAssetDetail);
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

            if (!elevatorContract.ApprovedS2FloorContract.TryGetFloor(CurrentFloorId, out S2FloorRecord floor))
            {
                Fail("Approved F00 floor record is unavailable.");
                yield break;
            }
            AsyncOperation floorLoad = LoadSceneAdditive(floor.scenePath);
            if (floorLoad == null)
            {
                Fail("Could not begin approved F00 floor load.");
                yield break;
            }
            while (!floorLoad.isDone)
                yield return null;
            Scene floorScene = SceneManager.GetSceneByPath(floor.scenePath);
            if (!floorScene.isLoaded)
            {
                Fail("Approved F00 floor did not become loaded.");
                yield break;
            }
            if (!floorAperture.ApplyToFloorScene(floorScene, CurrentFloorId, out string apertureApplyDetail))
            {
                Fail("Could not open the shaft aperture in the active S2 floor instance: " + apertureApplyDetail);
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

            Physics.SyncTransforms();
            EnvironmentReady = true;
            bool autoGate = HasArgument("-s3bAutoGate");
            if (autoGate)
                yield return RunAutoGate();
            else
                doorController.RequestOpen();
        }

        public void ToggleReviewObstruction()
            => SetReviewObstruction(!ReviewObstructionEnabled);

        public void SetReviewObstruction(bool obstructed)
        {
            if (reviewObstructionProxy != null)
                reviewObstructionProxy.SetActive(obstructed);
            if (doorController?.ObstructionSensor != null)
                doorController.ObstructionSensor.SetReviewForcedObstruction(obstructed);
            Physics.SyncTransforms();
        }

        private IEnumerator RunAutoGate()
        {
            var checks = new List<S3BRuntimeCheck>();
            doorController.SetGateManualTick(true);
            SetReviewObstruction(false);
            doorController.SetImmediateClosedForGate();
            Physics.SyncTransforms();

            Add(checks, "bootstrap_references", ReferencesComplete(),
                "Contract, shaft, door controller, rig, camera, locomotion, cabin, seven portals, and obstruction proxy are assigned.");
            Add(checks, "approved_s2_reference", elevatorContract.ApprovedS2FloorContract != null
                && elevatorContract.ApprovedS2FloorContract.Floors.Count == 7,
                elevatorContract.ApprovedS2FloorContract == null ? "missing" : "seven approved floors");
            bool spatialFit = elevatorContract.ValidateSpatialFit(out string spatialDetail);
            Add(checks, "contract_spatial_fit", spatialFit, spatialDetail);
            bool shaftContractFit = elevatorContract.ValidateShaftFit(out string shaftDetail);
            bool shaftGeometryFit = shaftStructure.ValidateStructure(out string shaftStructureDetail);
            bool shaftFit = shaftContractFit && shaftGeometryFit;
            Add(checks, "continuous_full_height_shaft", shaftFit, shaftDetail + "; " + shaftStructureDetail);
            bool cabinSweep = shaftStructure.ValidateCabinSweep(out string cabinSweepDetail);
            Add(checks, "cabin_sweep_clear_f00_to_f06", cabinSweep, cabinSweepDetail);
            bool glassDesign = shaftStructure.ValidateGlassDesign(out string glassDesignDetail);
            Add(checks, "transparent_glass_hoistway_design", glassDesign, glassDesignDetail);
            bool observationDesign = cabinObservationDesign.ValidateObservationDesign(out string observationDetail);
            Add(checks, "transparent_cabin_observation_zones", observationDesign, observationDetail);
            bool cabinNested = cabinObservationDesign.ValidateNestedInShaft(shaftStructure, out string nestingDetail);
            Add(checks, "cabin_fully_nested_for_f00_f06_sweep", cabinNested, nestingDetail);
            bool apertureAssets = floorAperture.ValidateAssets(out string apertureAssetDetail);
            Add(checks, "shaft_floor_aperture_assets", apertureAssets, apertureAssetDetail);
            elevatorContract.ApprovedS2FloorContract.TryGetFloor(CurrentFloorId, out S2FloorRecord activeFloor);
            Scene activeFloorScene = activeFloor == null ? default : SceneManager.GetSceneByPath(activeFloor.scenePath);
            string apertureAppliedDetail = "aperture was not applied";
            bool apertureApplied = activeFloor != null && floorAperture.LastAppliedFloorId == CurrentFloorId
                && floorAperture.ValidateAppliedToFloorScene(activeFloorScene, CurrentFloorId, out apertureAppliedDetail);
            Add(checks, "active_f00_slab_aperture_applied", apertureApplied,
                activeFloor == null ? "approved floor record missing" : apertureAppliedDetail);
            Add(checks, "exact_production_scene_set", loadedProductionScenes.SequenceEqual(
                    elevatorContract.ApprovedS2FloorContract.ProductionScenePaths.Select(Path.GetFileNameWithoutExtension)),
                string.Join(",", loadedProductionScenes));
            S2FloorSceneMarker[] floorMarkers = FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Add(checks, "single_approved_f00_scene", floorMarkers.Length == 1 && floorMarkers[0].FloorId == "F00",
                string.Join(",", floorMarkers.Select(item => item.FloorId)));
            Add(checks, "seven_exact_landing_datums", LandingDatumsCorrect(),
                string.Join(",", landingPortalRoots.Select(item => item.localPosition.y.ToString("F3"))));
            Add(checks, "identity_scale_import_roots", cabinModelRoot.localScale == Vector3.one
                && landingPortalRoots.All(item => item != null && item.localScale == Vector3.one),
                "cabin and landing portal roots use scale 1,1,1");
            Add(checks, "twelve_noncurrent_leaves_closed_locked", NonCurrentLandingDoorsClosed(),
                "nonCurrentLeaves=" + nonCurrentLandingDoors.Length);

            bool opened = doorController.RequestOpen();
            yield return AdvanceSeconds(0.60f);
            Add(checks, "smooth_synchronized_opening_midpoint", opened
                && doorController.State == S3ElevatorState.DoorOpening
                && doorController.LinearOpenness > 0.45f && doorController.LinearOpenness < 0.55f
                && doorController.DoorPairsSynchronized(elevatorContract.DoorPositionToleranceM),
                $"state={doorController.State}; openness={doorController.LinearOpenness:F3}");
            yield return AdvanceUntilState(S3ElevatorState.ReadyOpen, 1.0f);
            Physics.SyncTransforms();
            Collider[] openingBlockers = Physics.OverlapBox(new Vector3(1.65f, 1.10f, 3.04f),
                new Vector3(0.59f, 1.09f, 0.15f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            Add(checks, "full_1200mm_clear_opening", doorController.FullyOpen
                && doorController.MeasuredClearWidthM >= elevatorContract.ClearDoorM.x
                && openingBlockers.Length == 0,
                $"clearWidth={doorController.MeasuredClearWidthM:F3}; blockers={string.Join(",", openingBlockers.Select(item => item.name))}");

            bool closeAccepted = doorController.RequestClose();
            yield return AdvanceUntilState(S3ElevatorState.ReadyClosed, 1.4f);
            Add(checks, "normal_close_and_positive_interlocks", closeAccepted && doorController.FullyClosed
                && doorController.MovementInterlockSafe && doorController.CabinInterlock && doorController.LandingInterlock,
                $"state={doorController.State}; movementSafe={doorController.MovementInterlockSafe}");

            doorController.SetImmediateOpenForGate();
            bool earlyCloseAccepted = doorController.RequestClose();
            yield return AdvanceSeconds(0.35f);
            bool closingBeforeOpen = doorController.State == S3ElevatorState.DoorClosing;
            bool reopenAccepted = doorController.RequestOpen();
            yield return AdvanceUntilState(S3ElevatorState.ReadyOpen, 1.4f);
            Add(checks, "door_open_reopens_closing_pair", earlyCloseAccepted && closingBeforeOpen && reopenAccepted
                && doorController.FullyOpen && doorController.DoorPairsSynchronized(elevatorContract.DoorPositionToleranceM),
                $"state={doorController.State}; openness={doorController.LinearOpenness:F3}");

            doorController.SetImmediateOpenForGate();
            yield return AdvanceSeconds(3.90f);
            bool stayedOpenForDwell = doorController.State == S3ElevatorState.ReadyOpen;
            yield return AdvanceSeconds(0.20f);
            Add(checks, "four_second_dwell_then_auto_close", stayedOpenForDwell
                && doorController.State == S3ElevatorState.DoorClosing,
                $"state={doorController.State}; remaining={doorController.DwellRemaining:F3}");

            doorController.SetImmediateOpenForGate();
            SetReviewObstruction(true);
            bool blockedCloseRejected = !doorController.RequestClose() && doorController.State == S3ElevatorState.ReadyOpen;
            SetReviewObstruction(false);
            bool closeAfterClear = doorController.RequestClose();
            yield return AdvanceSeconds(0.30f);
            SetReviewObstruction(true);
            yield return AdvanceSeconds(0.02f);
            bool reversing = doorController.State == S3ElevatorState.DoorOpening;
            SetReviewObstruction(false);
            yield return AdvanceUntilState(S3ElevatorState.ReadyOpen, 1.5f);
            Add(checks, "obstruction_blocks_and_reverses_close", blockedCloseRejected && closeAfterClear && reversing
                && doorController.FullyOpen, $"reversing={reversing}; finalState={doorController.State}");

            S3ElevatorButton openButton = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.Command == S3ElevatorButtonCommand.DoorOpen);
            S3ElevatorButton closeButton = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.Command == S3ElevatorButtonCommand.DoorClose);
            S3ElevatorButton landingCall = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(item => item.Command == S3ElevatorButtonCommand.LandingCall && item.FloorId == "F00");
            S3ElevatorButton[] floorButtons = FindObjectsByType<S3ElevatorButton>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.Command == S3ElevatorButtonCommand.Floor).ToArray();
            doorController.SetImmediateClosedForGate();
            bool callAccepted = landingCall.Activate();
            yield return AdvanceUntilState(S3ElevatorState.ReadyOpen, 1.4f);
            bool closeButtonAccepted = closeButton.Activate();
            yield return AdvanceSeconds(0.25f);
            bool openButtonAccepted = openButton.Activate();
            Add(checks, "physical_open_close_and_landing_call_buttons", callAccepted && closeButtonAccepted
                && openButtonAccepted && floorButtons.Length == 7 && floorButtons.All(item => !item.Activate()),
                $"floorButtons={floorButtons.Length}; floorTravelIntentionallyDisabled=true");

            SetReviewObstruction(false);
            doorController.SetImmediateOpenForGate();
            doorController.SetGateManualTick(false);

            string output = ArgumentValue("-s3bReportPath");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Application.persistentDataPath, "StageI1_R03_S3B_RuntimeGate.json");
            var report = new S3BRuntimeGateReport
            {
                status = checks.All(item => item.pass) && string.IsNullOrEmpty(FatalError) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                currentFloorId = CurrentFloorId,
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? Application.persistentDataPath);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3B_RUNTIME_GATE={report.status}; {report.passCount}/{report.totalCount}; {output}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }

        private IEnumerator AdvanceSeconds(float seconds)
        {
            const float step = 0.02f;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float delta = Mathf.Min(step, seconds - elapsed);
                doorController.AdvanceForGate(delta);
                Physics.SyncTransforms();
                elapsed += delta;
                yield return null;
            }
        }

        private IEnumerator AdvanceUntilState(S3ElevatorState target, float timeout)
        {
            const float step = 0.02f;
            float elapsed = 0f;
            while (doorController.State != target && elapsed < timeout)
            {
                doorController.AdvanceForGate(step);
                Physics.SyncTransforms();
                elapsed += step;
                yield return null;
            }
        }

        private bool LandingDatumsCorrect()
        {
            if (landingPortalRoots.Length != elevatorContract.ApprovedS2FloorContract.Floors.Count)
                return false;
            for (int i = 0; i < landingPortalRoots.Length; i++)
            {
                Transform portal = landingPortalRoots[i];
                S2FloorRecord floor = elevatorContract.ApprovedS2FloorContract.Floors[i];
                if (portal == null || Mathf.Abs(portal.localPosition.y
                    - (floor.elevationM + elevatorContract.ModelRootVerticalOffsetM)) > 0.001f)
                    return false;
            }
            return true;
        }

        private bool NonCurrentLandingDoorsClosed()
        {
            if (nonCurrentLandingDoors.Length != 12)
                return false;
            foreach (Transform leaf in nonCurrentLandingDoors)
            {
                if (leaf == null || leaf.GetComponent<BoxCollider>() == null)
                    return false;
                bool left = leaf.name.EndsWith("_L", StringComparison.Ordinal);
                float expectedX = left ? 1.3475f : 1.9525f;
                if (Mathf.Abs(leaf.localPosition.x - expectedX) > elevatorContract.DoorPositionToleranceM)
                    return false;
            }
            return true;
        }

        private bool ReferencesComplete() => elevatorContract != null && elevatorContract.ApprovedS2FloorContract != null
            && doorController != null && xrOrigin != null && reviewCamera != null && locomotion != null
            && cabinModelRoot != null && landingPortalRoots.Length == 7 && nonCurrentLandingDoors.Length == 12
            && reviewObstructionProxy != null && shaftStructure != null && floorAperture != null
            && cabinObservationDesign != null;

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S3B_RUNTIME_FAIL: " + message, this);
        }

        private static AsyncOperation LoadSceneAdditive(string path)
        {
#if UNITY_EDITOR
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
#endif
        }

        private static void Add(ICollection<S3BRuntimeCheck> checks, string name, bool pass, string detail)
            => checks.Add(new S3BRuntimeCheck { name = name, pass = pass, detail = detail });

        private static bool HasArgument(string key)
            => Environment.GetCommandLineArgs().Any(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));

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
