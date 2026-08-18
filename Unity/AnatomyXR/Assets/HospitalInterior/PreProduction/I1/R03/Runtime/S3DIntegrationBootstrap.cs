using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DIntegrationBootstrap : MonoBehaviour
    {
        public const string ExteriorBaseSceneName = "Exterior_Base";
        public const string ExteriorBaseBlockerName = "UE_EXT_GlobalStructure_OPAQUE_ALL_02";
        public const string InteriorShellPreviewSceneName = "Exterior_InteriorShellPreview";

        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private S3ElevatorController elevatorController;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera integrationCamera;
        [SerializeField] private S3ShaftStructure shaftStructure;
        [SerializeField] private S3ShaftFloorAperture floorAperture;
        [SerializeField] private S3CabinObservationDesign cabinObservationDesign;
        [SerializeField] private Mesh exteriorBaseShaftCutMesh;
        [SerializeField] private S3DAdaptiveRig adaptiveRig;
        [SerializeField] private S3DEntranceDoorController entranceDoors;
        [SerializeField] private S3DTeleportSurfaceCoordinator teleportCoordinator;
        [SerializeField] private S3DRecoveryCoordinator recoveryCoordinator;
        [SerializeField] private S3DSiteCollisionCoordinator siteCollisionCoordinator;
        [SerializeField] private S3DFacadeFloorEdgeCoordinator facadeFloorEdgeCoordinator;

        private readonly List<string> loadedPersistentScenes = new List<string>();

        public bool EnvironmentReady { get; private set; }
        public string FatalError { get; private set; } = string.Empty;
        public int HiddenExteriorBackerCount { get; private set; }
        public int HiddenExteriorBalconyFloorSkinCount { get; private set; }
        public bool ExteriorBaseShaftApertureApplied { get; private set; }
        public string ExteriorBaseShaftApertureDetail { get; private set; } = "not applied";
        public IReadOnlyList<string> LoadedPersistentScenes => loadedPersistentScenes;
        public S3ElevatorContract ElevatorContract => elevatorContract;
        public S3ElevatorController ElevatorController => elevatorController;
        public XROrigin XrOrigin => xrOrigin;
        public Camera IntegrationCamera => integrationCamera;
        public S3DAdaptiveRig AdaptiveRig => adaptiveRig;
        public S3DEntranceDoorController EntranceDoors => entranceDoors;
        public S3DTeleportSurfaceCoordinator TeleportCoordinator => teleportCoordinator;
        public S3DRecoveryCoordinator RecoveryCoordinator => recoveryCoordinator;
        public S3DSiteCollisionCoordinator SiteCollisionCoordinator => siteCollisionCoordinator;
        public S3DFacadeFloorEdgeCoordinator FacadeFloorEdgeCoordinator
            => facadeFloorEdgeCoordinator;

        public void Configure(S3ElevatorContract contract, S3ElevatorController elevator,
            XROrigin origin, Camera camera, S3ShaftStructure shaft,
            S3ShaftFloorAperture aperture, S3CabinObservationDesign observation,
            Mesh exteriorShaftCutMesh, S3DAdaptiveRig rig,
            S3DEntranceDoorController automaticEntrance,
            S3DTeleportSurfaceCoordinator teleports, S3DRecoveryCoordinator recovery,
            S3DSiteCollisionCoordinator siteCollision,
            S3DFacadeFloorEdgeCoordinator facadeFloorEdges)
        {
            elevatorContract = contract;
            elevatorController = elevator;
            xrOrigin = origin;
            integrationCamera = camera;
            shaftStructure = shaft;
            floorAperture = aperture;
            cabinObservationDesign = observation;
            exteriorBaseShaftCutMesh = exteriorShaftCutMesh;
            adaptiveRig = rig;
            entranceDoors = automaticEntrance;
            teleportCoordinator = teleports;
            recoveryCoordinator = recovery;
            siteCollisionCoordinator = siteCollision;
            facadeFloorEdgeCoordinator = facadeFloorEdges;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            if (!ReferencesComplete())
            {
                Fail("S3D bootstrap references are incomplete.");
                yield break;
            }

            float modeTimeout = 0f;
            while (!adaptiveRig.ModeReady && modeTimeout < 5f)
            {
                modeTimeout += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!adaptiveRig.ModeReady)
            {
                Fail("Adaptive rig mode selection timed out.");
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
                Fail("Approved S3C authority is invalid: " + spatialDetail + "; "
                    + shaftContractDetail + "; " + shaftDetail + "; " + sweepDetail + "; "
                    + glassDetail + "; " + observationDetail + "; " + nestingDetail + "; "
                    + apertureDetail);
                yield break;
            }

            adaptiveRig.SetPositionSafely(S3DIntegrationContract.GateSpawn, Quaternion.identity);
            foreach (string path in elevatorContract.ApprovedS2FloorContract.ProductionScenePaths
                .Concat(S3DIntegrationContract.SiteScenePaths))
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (SceneManager.GetSceneByName(sceneName).isLoaded)
                {
                    Fail("Duplicate/preloaded persistent scene: " + sceneName);
                    yield break;
                }
                AsyncOperation operation = LoadSceneAdditive(path);
                if (operation == null)
                {
                    Fail("Could not begin persistent scene load: " + path);
                    yield break;
                }
                while (!operation.isDone)
                    yield return null;
                loadedPersistentScenes.Add(sceneName);
            }

            if (SceneManager.GetSceneByName(InteriorShellPreviewSceneName).isLoaded)
            {
                Fail("The duplicate interior-shell preview was loaded.");
                yield break;
            }
            if (!ApplyExteriorBaseShaftAperture(out string exteriorDetail))
            {
                Fail("Could not apply the approved exterior shaft aperture: " + exteriorDetail);
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
                Fail("Could not begin F00 load.");
                yield break;
            }
            while (!floorLoad.isDone)
                yield return null;
            Scene floorScene = SceneManager.GetSceneByPath(initialFloor.scenePath);
            if (!floorScene.IsValid() || !floorScene.isLoaded)
            {
                Fail("F00 did not become loaded.");
                yield break;
            }
            SceneManager.SetActiveScene(floorScene);

            HiddenExteriorBackerCount = S2ManualFloorLoader.HideExteriorOnlyBalconyBackers();
            HiddenExteriorBalconyFloorSkinCount = S2ManualFloorLoader.HideExteriorOverlappingBalconyFloorSkins();
            if (HiddenExteriorBackerCount != S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length
                || HiddenExteriorBalconyFloorSkinCount
                    != S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length)
            {
                Fail("Approved exterior suppression contract did not match S2/S3C.");
                yield break;
            }
            if (!elevatorController.InitializeAtFloor(initialFloorId, floorScene,
                out string initializeDetail))
            {
                Fail("E01 initialization failed: " + initializeDetail);
                yield break;
            }
            if (!entranceDoors.DiscoverAndInitialize(out string entranceDetail))
            {
                Fail("Main entrance initialization failed: " + entranceDetail);
                yield break;
            }

            HospitalSiteGrassRenderer[] grass = FindObjectsByType<HospitalSiteGrassRenderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (grass.Length != 1)
            {
                Fail("Expected one approved site grass manager; found " + grass.Length);
                yield break;
            }
            grass[0].SetTargetCamera(integrationCamera);
            grass[0].SetAnchor(integrationCamera.transform);
            if (!siteCollisionCoordinator.Initialize(grass[0].Config, entranceDoors,
                    elevatorContract.ApprovedS2FloorContract, out string siteCollisionDetail))
            {
                Fail("S3D site collision integration failed: " + siteCollisionDetail);
                yield break;
            }
            grass[0].ForceRebuild(adaptiveRig.transform.position);

            adaptiveRig.NotifyEnvironmentReady();
            teleportCoordinator.RefreshBindings();
            Physics.SyncTransforms();
            EnvironmentReady = true;

            if (HasArgument("-s3dAutoGate"))
                yield return RunAutoGate();
        }

        private IEnumerator RunAutoGate()
        {
            var checks = new List<S3DCheckRecord>();
            var travelRecords = new List<S3DTravelRecord>();
            string[] route = S3DIntegrationContract.RequiredRoute;
            string evidenceDirectory = ArgumentValue("-s3dEvidenceDir");
            adaptiveRig.SetGateSimulationSuspended(true);
            elevatorController.SetGateManualTick(true);
            entranceDoors.SetGateManualTick(true);
            SetElevatorObstruction(false);
            elevatorController.CurrentDoorController.SetImmediateClosedForGate();

            Add(checks, "bootstrap_references", ReferencesComplete(),
                "Integration bootstrap, adaptive rig, approved E01, entrance, teleport, and recovery are assigned.");
            string[] expectedPersistent = elevatorContract.ApprovedS2FloorContract.ProductionScenePaths
                .Concat(S3DIntegrationContract.SiteScenePaths)
                .Select(Path.GetFileNameWithoutExtension).ToArray();
            Add(checks, "exact_seven_persistent_scenes",
                loadedPersistentScenes.SequenceEqual(expectedPersistent)
                && PersistentScenesRetained(), string.Join(",", loadedPersistentScenes));
            Add(checks, "interior_shell_preview_excluded",
                !SceneManager.GetSceneByName(InteriorShellPreviewSceneName).isLoaded,
                InteriorShellPreviewSceneName + " loaded="
                    + SceneManager.GetSceneByName(InteriorShellPreviewSceneName).isLoaded);
            Add(checks, "single_integration_runtime_stack",
                FindObjectsByType<S3DIntegrationBootstrap>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 1
                && FindObjectsByType<S3DAdaptiveRig>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 1
                && FindObjectsByType<Camera>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 1
                && FindObjectsByType<AudioListener>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 1,
                "bootstrap=1; rig=1; camera=1; listener=1");
            Add(checks, "desktop_gate_mode_selected",
                adaptiveRig.ActiveMode == S3DIntegrationMode.Desktop,
                adaptiveRig.ActiveMode.ToString());
            Add(checks, "forgiving_elevator_control_reach_and_aim_assist",
                adaptiveRig.InteractionDistanceM >= 5.5f
                && adaptiveRig.InteractionAimAssistRadiusM >= 0.18f,
                $"reach={adaptiveRig.InteractionDistanceM:F2}m; assistRadius={adaptiveRig.InteractionAimAssistRadiusM:F2}m");
            Add(checks, "gate_spawn_exact",
                Vector3.Distance(adaptiveRig.transform.position,
                    S3DIntegrationContract.GateSpawn) <= 0.02f,
                adaptiveRig.transform.position.ToString("F3"));
            Add(checks, "site_grass_bound_once",
                FindObjectsByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length == 1,
                 "grassManagers=" + FindObjectsByType<HospitalSiteGrassRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None).Length);
            Add(checks, "visible_lawn_has_complete_support_collision",
                siteCollisionCoordinator.ValidateGrassSupport(out string lawnSupportDetail),
                lawnSupportDetail);
            int groundedLawnSamples = 0;
            var lawnGroundingDetails = new List<string>();
            foreach (Vector3 sample in siteCollisionCoordinator.LawnGroundingSamples)
            {
                PositionRig(sample + Vector3.up * 0.25f, 0f);
                CollisionFlags collisionFlags = CollisionFlags.None;
                for (int frame = 0; frame < 10; frame++)
                {
                    collisionFlags |= adaptiveRig.CharacterController.Move(
                        Vector3.down * 0.05f);
                    yield return null;
                }
                // CharacterController can omit Below at seams shared with a slightly
                // higher curb/route collider. The decisive fall-through check is that
                // an attempted 0.50 m descent is physically stopped at or above the
                // rendered lawn instead of passing through it.
                bool grounded = adaptiveRig.transform.position.y >= sample.y - 0.10f;
                if (grounded)
                    groundedLawnSamples++;
                lawnGroundingDetails.Add($"({sample.x:F0},{sample.z:F0})={grounded}"
                    + $"@{adaptiveRig.transform.position.y:F2}/flags={collisionFlags}");
            }
            Add(checks, "desktop_character_grounded_across_rendered_lawn",
                groundedLawnSamples == siteCollisionCoordinator.LawnGroundingSamples.Count,
                $"grounded={groundedLawnSamples}/{siteCollisionCoordinator.LawnGroundingSamples.Count}; "
                    + string.Join(",", lawnGroundingDetails));
            Add(checks, "single_initial_floor",
                LoadedFloorMarkers().Length == 1 && LoadedFloorMarkers()[0].FloorId == "F00",
                "markers=" + string.Join(",", LoadedFloorMarkers().Select(item => item.FloorId)));
            Add(checks, "exterior_suppressions_and_shaft_aperture",
                HiddenExteriorBackerCount == S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length
                && HiddenExteriorBalconyFloorSkinCount
                    == S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length
                && ExteriorBaseShaftApertureApplied,
                $"backers={HiddenExteriorBackerCount}; skins={HiddenExteriorBalconyFloorSkinCount}; {ExteriorBaseShaftApertureDetail}");

            teleportCoordinator.RefreshBindings();
            Add(checks, "f00_teleport_allowlist",
                teleportCoordinator.BoundSiteSurfaceCount == 20
                && teleportCoordinator.BoundFloorSurfaceCount == 1
                && !teleportCoordinator.TeleportPermitted,
                $"site={teleportCoordinator.BoundSiteSurfaceCount}; floor={teleportCoordinator.BoundFloorSurfaceCount}; permitted={teleportCoordinator.TeleportPermitted}");
            Add(checks, "gate_and_lobby_route_support",
                HasWalkableSupport(S3DIntegrationContract.GateSpawn)
                && HasWalkableSupport(S3DIntegrationContract.LobbyCheckpoint),
                "gate=" + HasWalkableSupport(S3DIntegrationContract.GateSpawn)
                    + "; lobby=" + HasWalkableSupport(S3DIntegrationContract.LobbyCheckpoint));

            PositionRig(S3DIntegrationContract.GateSpawn, 0f);
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
                yield return CaptureScreenshot(Path.Combine(evidenceDirectory, "01_GateSpawn.png"));
            PositionRig(S3DIntegrationContract.LobbyCheckpoint, 0f);
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
                yield return CaptureScreenshot(Path.Combine(evidenceDirectory, "02_LobbyApproach.png"));

            bool entranceMetadata = entranceDoors.ValidateMetadata(out string entranceMetadataDetail);
            PositionRig(S3DIntegrationContract.EntranceCheckpoint, 0f);
            entranceDoors.SetGateOccupancy(true);
            AdvanceEntrance(1.3f);
            bool openPassage = entranceDoors.ValidateOpenPassage(out string passageDetail);
            Add(checks, "automatic_entrance_opens_and_clears",
                entranceMetadata && entranceDoors.FullyOpen && openPassage,
                entranceMetadataDetail + "; " + passageDetail);
            Add(checks, "dropoff_to_lobby_entrance_corridor_clear",
                siteCollisionCoordinator.ValidateOpenEntranceTraversal(adaptiveRig,
                    out string entranceTraversalDetail), entranceTraversalDetail);
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
                yield return CaptureScreenshot(Path.Combine(evidenceDirectory,
                    "03_AutomaticEntrancePassage.png"));

            PositionRig(S3DIntegrationContract.LobbyCheckpoint, 0f);
            entranceDoors.SetGateOccupancy(false);
            AdvanceEntrance(2.7f);
            bool wasClosing = entranceDoors.Openness < 0.95f && entranceDoors.Openness > 0.05f;
            entranceDoors.SetGateOccupancy(true);
            AdvanceEntrance(1.3f);
            bool reversedOpen = entranceDoors.FullyOpen;
            entranceDoors.SetGateOccupancy(false);
            AdvanceEntrance(3.3f);
            Add(checks, "automatic_entrance_dwell_close_and_reversal",
                wasClosing && reversedOpen && entranceDoors.FullyClosed,
                $"wasClosing={wasClosing}; reversed={reversedOpen}; final={entranceDoors.Openness:F3}");
            entranceDoors.SetGateManualTick(false);

            S3ElevatorButton[] buttons = FindObjectsByType<S3ElevatorButton>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Add(checks, "sixteen_physical_controls_preserved",
                buttons.Length == 16 && buttons.All(item => item.ElevatorController == elevatorController)
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.Floor) == 7
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.LandingCall) == 7,
                "buttons=" + buttons.Length);

            elevatorContract.ApprovedS2FloorContract.TryGetFloor("F00", out S2FloorRecord f00);
            PositionRig(f00.arrivalPosition, 0f);
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
                yield return CaptureScreenshot(Path.Combine(evidenceDirectory,
                    "04_F00_ElevatorArrival.png"));
            bool outsideRejected = !elevatorController.RequestFloor("F01");
            bool invalidRejected = !elevatorController.RequestFloor("F99");
            Add(checks, "outside_cabin_and_invalid_requests_rejected",
                outsideRejected && invalidRejected, elevatorController.LastResult);

            PlacePassengerInsideCabin();
            teleportCoordinator.RefreshBindings();
            Add(checks, "teleport_disabled_inside_cabin",
                elevatorController.IsPassengerInsideCabin()
                && !teleportCoordinator.TeleportPermitted,
                $"inside={elevatorController.IsPassengerInsideCabin()}; permitted={teleportCoordinator.TeleportPermitted}");
            elevatorController.CurrentDoorController.SetImmediateOpenForGate();
            SetElevatorObstruction(true);
            bool obstructionRejected = !elevatorController.RequestFloor("F01");
            SetElevatorObstruction(false);
            Add(checks, "obstruction_rejects_travel", obstructionRejected
                && elevatorController.CommittedFloorId == "F00", elevatorController.LastResult);
            elevatorController.CurrentDoorController.SetImmediateClosedForGate();

            for (int i = 1; i < route.Length; i++)
            {
                string origin = elevatorController.CommittedFloorId;
                string target = route[i];
                Vector3 offsetBefore = xrOrigin.transform.position - elevatorController.CabinRoot.position;
                float driftBefore = elevatorController.MaximumPassengerDriftM;
                bool accepted = elevatorController.RequestFloor(target);
                yield return WaitUntilTravelSettles(60f);
                yield return OpenCurrentDoorForGate();
                teleportCoordinator.RefreshBindings();
                bool endpoint = elevatorController.ValidateEndpoint(out string endpointDetail);
                bool exact = elevatorContract.ApprovedS2FloorContract.TryGetFloor(target,
                    out S2FloorRecord expected) && Mathf.Abs(elevatorController.CabinFloorElevationM
                    - expected.elevationM) <= 0.001f;
                bool passenger = elevatorController.IsPassengerInsideCabin()
                    && Vector3.Distance(offsetBefore, xrOrigin.transform.position
                        - elevatorController.CabinRoot.position) <= 0.002f;
                bool persistent = PersistentScenesRetained();
                travelRecords.Add(new S3DTravelRecord
                {
                    origin = origin,
                    destination = target,
                    accepted = accepted,
                    exactDatum = exact,
                    oneActiveFloor = LoadedFloorMarkers().Length == 1
                        && LoadedFloorMarkers()[0].FloorId == target,
                    persistentScenesRetained = persistent,
                    passengerStayedInCabin = passenger,
                    finalCabinElevationM = elevatorController.CabinFloorElevationM,
                    passengerDriftM = elevatorController.MaximumPassengerDriftM - driftBefore,
                });
                Add(checks, target + "_integrated_travel_endpoint",
                    accepted && endpoint && exact && passenger && persistent
                    && LoadedFloorMarkers().Length == 1
                    && LoadedFloorMarkers()[0].FloorId == target,
                    endpointDetail + "; persistent=" + persistent + "; passenger=" + passenger);
                bool landingSafe = elevatorController.ValidateLandingDoorSafety(out string landingDetail);
                Add(checks, target + "_noncurrent_landings_locked", landingSafe, landingDetail);
                if (target != "F00")
                {
                    bool seamCoverage = ValidateUpperFloorIntegrationCoverage(expected,
                        out string seamDetail);
                    Add(checks, target + "_main_glass_floor_edge_closed",
                        seamCoverage, seamDetail);
                    if (target == "F01" && !string.IsNullOrWhiteSpace(evidenceDirectory))
                        yield return CaptureF01MainGlassSeam(Path.Combine(evidenceDirectory,
                            "07_F01_MainGlassSeam_Corrected.png"));
                }
                if (target == "F06" && !string.IsNullOrWhiteSpace(evidenceDirectory))
                    yield return CaptureScreenshot(Path.Combine(evidenceDirectory,
                        "05_F06_Arrival.png"));
            }

            Add(checks, "required_integrated_route_complete",
                travelRecords.Count == 7 && travelRecords.All(item => item.accepted
                    && item.exactDatum && item.oneActiveFloor && item.persistentScenesRetained
                    && item.passengerStayedInCabin), string.Join(" -> ", route));
            Add(checks, "passenger_relative_drift_within_s3c_tolerance",
                elevatorController.MaximumPassengerDriftM <= 0.002f,
                elevatorController.MaximumPassengerDriftM.ToString("F6") + " m");

            bool sameFloorAccepted = elevatorController.RequestFloor("F00");
            yield return OpenCurrentDoorForGate();
            Add(checks, "same_floor_request_reopens", sameFloorAccepted
                && elevatorController.CurrentDoorController.FullyOpen, elevatorController.LastResult);

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
            Add(checks, "cancellation_rolls_back_to_committed_floor", cancelTravelAccepted
                && cancelAccepted && cancellationEndpoint
                && elevatorController.CommittedFloorId == "F00"
                && elevatorController.ActiveFloorId == "F00", cancellationDetail);

            elevatorController.CurrentDoorController.SetImmediateClosedForGate();
            elevatorController.InjectNextDestinationLoadFailureForGate();
            bool loadFailureAccepted = elevatorController.RequestFloor("F01");
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            bool loadFailureEndpoint = elevatorController.ValidateEndpoint(out string loadFailureDetail);
            Add(checks, "load_failure_rolls_back_without_motion", loadFailureAccepted
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
            Add(checks, "failed_rollback_enters_enclosed_faulted_safe",
                faultTravelAccepted && faultCancelAccepted && enclosedFault,
                elevatorController.LastFault);
            bool recoveryAccepted = elevatorController.RequestReviewRecovery();
            yield return WaitUntilTravelSettles(60f);
            yield return OpenCurrentDoorForGate();
            bool recoveredEndpoint = elevatorController.ValidateEndpoint(out string recoveredDetail);
            Add(checks, "review_only_elevator_fault_recovery", recoveryAccepted
                && recoveredEndpoint && elevatorController.CommittedFloorId == "F00",
                recoveredDetail);

            bool landingCallAccepted = elevatorController.RequestLandingCall("F00");
            bool remoteLandingRejected = !elevatorController.RequestLandingCall("F06");
            yield return OpenCurrentDoorForGate();
            Add(checks, "landing_call_present_only", landingCallAccepted
                && remoteLandingRejected && elevatorController.CurrentDoorController.FullyOpen,
                elevatorController.LastResult);
            Add(checks, "approved_elevator_door_timing_and_clearance",
                Mathf.Abs(elevatorContract.DoorOpenSeconds - 1.2f) <= 0.001f
                && Mathf.Abs(elevatorContract.DoorDwellSeconds - 4f) <= 0.001f
                && Mathf.Abs(elevatorContract.DoorCloseSeconds - 1.2f) <= 0.001f
                && elevatorController.CurrentDoorController.MeasuredClearWidthM
                    >= elevatorContract.ClearDoorM.x,
                $"open/dwell/close={elevatorContract.DoorOpenSeconds:F1}/{elevatorContract.DoorDwellSeconds:F1}/{elevatorContract.DoorCloseSeconds:F1}; clear={elevatorController.CurrentDoorController.MeasuredClearWidthM:F3}");

            PositionRig(S3DIntegrationContract.EntranceCheckpoint, 180f);
            yield return null;
            bool fallRecovered = recoveryCoordinator.ForceRecoveryForGate("F00",
                out Vector3 recoveredPosition);
            Add(checks, "f00_fall_recovery_uses_safe_checkpoint", fallRecovered
                && recoveredPosition.y >= -0.1f,
                recoveryCoordinator.LastRecoveryReason + "; position="
                    + recoveredPosition.ToString("F3"));

            PositionRig(S3DIntegrationContract.EntranceCheckpoint, 180f);
            entranceDoors.SetGateManualTick(true);
            entranceDoors.SetGateOccupancy(true);
            AdvanceEntrance(1.3f);
            if (!string.IsNullOrWhiteSpace(evidenceDirectory))
                yield return CaptureScreenshot(Path.Combine(evidenceDirectory,
                    "06_F00_ReturnAndExit.png"));
            Add(checks, "return_exit_entrance_operational",
                entranceDoors.FullyOpen && PersistentScenesRetained()
                && LoadedFloorMarkers().Length == 1
                && LoadedFloorMarkers()[0].FloorId == "F00",
                $"open={entranceDoors.Openness:F3}; floor={LoadedFloorMarkers().FirstOrDefault()?.FloorId}");

            entranceDoors.SetGateOccupancy(false);
            entranceDoors.SetGateManualTick(false);
            SetElevatorObstruction(false);
            elevatorController.SetGateManualTick(false);
            adaptiveRig.SetGateSimulationSuspended(false);

            string output = ArgumentValue("-s3dReportPath");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Application.persistentDataPath,
                    "StageI1_R03_S3D_RuntimeGate.json");
            var report = new S3DRuntimeGateReport
            {
                status = checks.All(item => item.pass) && string.IsNullOrEmpty(FatalError)
                    ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                integrationMode = adaptiveRig.ActiveMode.ToString(),
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                route = route,
                loadedPersistentScenes = loadedPersistentScenes.ToArray(),
                travelRecords = travelRecords.ToArray(),
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))
                ?? Application.persistentDataPath);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3D_RUNTIME_GATE={report.status}; {report.passCount}/{report.totalCount}; {output}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }

        private bool ReferencesComplete() => elevatorContract != null && elevatorController != null
            && xrOrigin != null && integrationCamera != null && shaftStructure != null
            && floorAperture != null && cabinObservationDesign != null
            && exteriorBaseShaftCutMesh != null && adaptiveRig != null
            && entranceDoors != null && teleportCoordinator != null
            && recoveryCoordinator != null && siteCollisionCoordinator != null
            && facadeFloorEdgeCoordinator != null;

        private bool ValidateUpperFloorIntegrationCoverage(S2FloorRecord expected,
            out string detail)
        {
            S2FloorSceneMarker[] markers = LoadedFloorMarkers();
            MeshCollider slab = markers.Length == 1
                ? markers[0].GetComponentsInChildren<MeshCollider>(true)
                    .SingleOrDefault(item => item.name == "S2_R03_EmptySlab")
                : null;
            Vector2[] probes = expected.integrationCoverageProbes ?? Array.Empty<Vector2>();
            bool approvedSlabCoverage = slab != null && probes.Length == 4
                && probes.All(probe => slab.Raycast(new Ray(new Vector3(probe.x,
                    expected.elevationM + 1f, probe.y), Vector3.down), out _, 2f));
            bool edgeFinish = facadeFloorEdgeCoordinator.ValidateFloor(expected.floorId,
                expected.elevationM, out string edgeDetail);
            detail = $"approvedSlabProbes={approvedSlabCoverage} ({probes.Length}/4); {edgeDetail}";
            return approvedSlabCoverage && edgeFinish;
        }

        private bool PersistentScenesRetained()
        {
            string[] expected = elevatorContract.ApprovedS2FloorContract.ProductionScenePaths
                .Concat(S3DIntegrationContract.SiteScenePaths).ToArray();
            return expected.All(path =>
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                return scene.IsValid() && scene.isLoaded;
            });
        }

        private bool ApplyExteriorBaseShaftAperture(out string detail)
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

        private void PositionRig(Vector3 position, float yaw)
            => adaptiveRig.SetPositionSafely(position, Quaternion.Euler(0f, yaw, 0f));

        private void PlacePassengerInsideCabin()
        {
            adaptiveRig.SetPositionSafely(new Vector3(elevatorContract.CabinCenterXZ.x,
                elevatorController.CabinFloorElevationM + 0.03f,
                elevatorContract.CabinCenterXZ.y - 0.35f), Quaternion.identity);
        }

        private void SetElevatorObstruction(bool obstructed)
        {
            if (elevatorController?.CurrentDoorController?.ObstructionSensor != null)
                elevatorController.CurrentDoorController.ObstructionSensor
                    .SetReviewForcedObstruction(obstructed);
            Physics.SyncTransforms();
        }

        private void AdvanceEntrance(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                float delta = Mathf.Min(0.08f, seconds - elapsed);
                entranceDoors.AdvanceForGate(delta);
                elapsed += delta;
            }
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

            const int width = 1280;
            const int height = 720;
            var destination = new RenderTexture(width, height, 24,
                RenderTextureFormat.ARGB32)
            {
                name = "S3D_RuntimeEvidence",
                antiAliasing = 1,
            };
            destination.Create();
            var request = new RenderPipeline.StandardRequest
            {
                destination = destination,
                mipLevel = 0,
                slice = 0,
                face = CubemapFace.Unknown,
            };
            RenderPipeline.SubmitRenderRequest(integrationCamera, request);
            yield return null;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = destination;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
            image.Apply(false, false);
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = previous;
            Destroy(image);
            destination.Release();
            Destroy(destination);
        }

        private IEnumerator CaptureF01MainGlassSeam(string path)
        {
            Vector3 savedPosition = integrationCamera.transform.position;
            Quaternion savedRotation = integrationCamera.transform.rotation;
            Vector3 cameraPosition = new Vector3(13f, 7.55f, -11.5f);
            Vector3 focus = new Vector3(17.92f, 5.88f, -11.5f);
            integrationCamera.transform.SetPositionAndRotation(cameraPosition,
                Quaternion.LookRotation(focus - cameraPosition, Vector3.up));
            yield return null;
            yield return CaptureScreenshot(path);
            integrationCamera.transform.SetPositionAndRotation(savedPosition, savedRotation);
        }

        private bool HasWalkableSupport(Vector3 position)
        {
            Ray ray = new Ray(position + Vector3.up * 2f, Vector3.down);
            return Physics.Raycast(ray, out RaycastHit hit, 4f, ~0,
                QueryTriggerInteraction.Ignore) && hit.collider != null;
        }

        private S2FloorSceneMarker[] LoadedFloorMarkers()
            => FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S3D_RUNTIME_FAIL: " + message, this);
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

        private static void Add(ICollection<S3DCheckRecord> checks, string name,
            bool pass, string detail)
            => checks.Add(new S3DCheckRecord { name = name, pass = pass, detail = detail });

        private static bool HasArgument(string key)
            => Environment.GetCommandLineArgs().Any(item => string.Equals(item, key,
                StringComparison.OrdinalIgnoreCase));

        private static string ArgumentValue(string key)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
                if (string.Equals(arguments[i], key, StringComparison.OrdinalIgnoreCase))
                    return arguments[i + 1];
            return string.Empty;
        }
    }
}
