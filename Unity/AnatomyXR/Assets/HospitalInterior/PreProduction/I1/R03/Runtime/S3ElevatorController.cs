using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S3TravelRequestKind
    {
        CabinFloor,
        LandingCall,
    }

    [DisallowMultipleComponent]
    public sealed class S3ElevatorController : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract contract;
        [SerializeField] private Transform cabinRoot;
        [SerializeField] private Transform passengerRoot;
        [SerializeField] private Transform obstructionSensorRoot;
        [SerializeField] private GameObject obstructionProxy;
        [SerializeField] private S3ShaftFloorAperture floorAperture;
        [SerializeField] private Transform[] landingPortalRoots = Array.Empty<Transform>();
        [SerializeField] private S3DoorController[] floorDoorControllers = Array.Empty<S3DoorController>();
        [SerializeField] private TextMesh cabinTravelIndicator;
        [SerializeField] private S3ElevatorState state = S3ElevatorState.ReadyClosed;
        [SerializeField] private string committedFloorId = "F00";
        [SerializeField] private string activeFloorId = "F00";
        [SerializeField] private string destinationFloorId = string.Empty;

        private Coroutine activeTravel;
        private bool initialized;
        private bool cancellationRequested;
        private bool gateManualTick;
        private bool injectNextLoadFailure;
        private bool injectNextRecoveryFailure;
        private bool lastFaultCarriedPassenger;
        private string pendingDestinationScenePath = string.Empty;
        private float cabinFloorElevationM;
        private float maximumPassengerDriftM;

        public S3ElevatorContract Contract => contract;
        public Transform CabinRoot => cabinRoot;
        public Transform PassengerRoot => passengerRoot;
        public S3ShaftFloorAperture FloorAperture => floorAperture;
        public Transform[] LandingPortalRoots => landingPortalRoots;
        public S3DoorController[] FloorDoorControllers => floorDoorControllers;
        public TextMesh CabinTravelIndicator => cabinTravelIndicator;
        public S3ElevatorState State => state;
        public string CommittedFloorId => committedFloorId;
        public string ActiveFloorId => activeFloorId;
        public string DestinationFloorId => destinationFloorId;
        public string LastResult { get; private set; } = string.Empty;
        public string LastFault { get; private set; } = string.Empty;
        public string LastFloorValidationDetail { get; private set; } = string.Empty;
        public bool IsInitialized => initialized;
        public bool IsBusy => activeTravel != null;
        public bool IsFaultedSafe => state == S3ElevatorState.FaultedSafe;
        public float CabinFloorElevationM => cabinFloorElevationM;
        public float MaximumPassengerDriftM => maximumPassengerDriftM;
        public int CompletedTravelCount { get; private set; }
        public int RejectedRequestCount { get; private set; }
        public int RecoveryCount { get; private set; }
        public int FaultCount { get; private set; }
        public S3DoorController CurrentDoorController => TryFloorIndex(committedFloorId, out int index)
            && index < floorDoorControllers.Length ? floorDoorControllers[index] : null;

        public void Configure(S3ElevatorContract elevatorContract, Transform movingCabinRoot,
            Transform movingPassengerRoot, Transform thresholdSensorRoot, GameObject thresholdProxy,
            S3ShaftFloorAperture aperture, Transform[] portalRoots, S3DoorController[] doorControllers,
            TextMesh travelIndicator)
        {
            contract = elevatorContract;
            cabinRoot = movingCabinRoot;
            passengerRoot = movingPassengerRoot;
            obstructionSensorRoot = thresholdSensorRoot;
            obstructionProxy = thresholdProxy;
            floorAperture = aperture;
            landingPortalRoots = portalRoots ?? Array.Empty<Transform>();
            floorDoorControllers = doorControllers ?? Array.Empty<S3DoorController>();
            cabinTravelIndicator = travelIndicator;
            committedFloorId = contract?.ApprovedS2FloorContract?.InitialFloorId ?? "F00";
            activeFloorId = committedFloorId;
            destinationFloorId = string.Empty;
            initialized = false;
            SetDoorManualTick(false);
            UpdateIndicator();
        }

        public bool InitializeAtFloor(string floorId, Scene activeFloorScene, out string detail)
        {
            if (!ReferencesComplete() || !TryFloor(floorId, out S2FloorRecord floor, out int index))
            {
                detail = "references or initial floor record are incomplete";
                return false;
            }
            if (!ValidateLoadedFloor(activeFloorScene, floor, true, out detail))
                return false;

            committedFloorId = floorId;
            activeFloorId = floorId;
            destinationFloorId = string.Empty;
            SetCabinElevationImmediate(floor.elevationM, false);
            SelectDoorController(index);
            PositionThresholdAt(floor.elevationM);
            CurrentDoorController.SetImmediateClosedForGate();
            state = S3ElevatorState.ReadyClosed;
            initialized = true;
            LastFault = string.Empty;
            LastResult = "E01 initialized safely at " + floorId + ".";
            UpdateIndicator();
            return true;
        }

        private void LateUpdate()
        {
            if (initialized && activeTravel == null && state != S3ElevatorState.FaultedSafe
                && state != S3ElevatorState.Recovering && CurrentDoorController != null)
            {
                state = CurrentDoorController.State;
                UpdateIndicator();
            }
        }

        public bool RequestFloor(string floorId)
        {
            if (!CanAcceptRequest(floorId, true))
                return false;
            if (!IsPassengerInsideCabin())
                return Reject("Enter the cabin before selecting " + floorId + ".");
            if (string.Equals(floorId, committedFloorId, StringComparison.Ordinal))
                return SameFloorOpen(floorId);
            return BeginTravel(floorId, S3TravelRequestKind.CabinFloor, true, true);
        }

        public bool RequestLandingCall(string floorId)
        {
            if (!CanAcceptRequest(floorId, false))
                return false;
            if (!string.Equals(floorId, activeFloorId, StringComparison.Ordinal))
                return Reject("Landing call rejected because " + floorId + " is not the active floor.");
            if (string.Equals(floorId, committedFloorId, StringComparison.Ordinal))
                return SameFloorOpen(floorId);
            return BeginTravel(floorId, S3TravelRequestKind.LandingCall, false, false);
        }

        public bool RequestDoorOpen()
        {
            if (!initialized || IsBusy || IsFaultedSafe || CurrentDoorController == null)
                return Reject("Door-open rejected while E01 is unavailable.");
            bool accepted = CurrentDoorController.RequestOpen();
            LastResult = accepted ? "Door-open accepted at " + committedFloorId + "."
                : "Door-open rejected safely.";
            if (!accepted)
                RejectedRequestCount++;
            return accepted;
        }

        public bool RequestDoorClose()
        {
            if (!initialized || IsBusy || IsFaultedSafe || CurrentDoorController == null)
                return Reject("Door-close rejected while E01 is unavailable.");
            bool accepted = CurrentDoorController.RequestClose();
            LastResult = accepted ? "Door-close accepted at " + committedFloorId + "."
                : "Door-close blocked or rejected safely.";
            if (!accepted)
                RejectedRequestCount++;
            return accepted;
        }

        public bool CancelActiveTravel()
        {
            if (!IsBusy || state == S3ElevatorState.FaultedSafe)
                return Reject("There is no cancellable E01 journey.");
            cancellationRequested = true;
            LastResult = "Cancellation requested; E01 will return to " + committedFloorId + ".";
            return true;
        }

        public bool RequestReviewRecovery()
        {
            if (!initialized || IsBusy || !IsFaultedSafe)
                return Reject("Review recovery is available only from FaultedSafe.");
            activeTravel = StartCoroutine(ReviewRecoveryRoutine());
            LastResult = "Review-only recovery started toward " + committedFloorId + ".";
            return true;
        }

        public void SetGateManualTick(bool enabled)
        {
            gateManualTick = enabled;
            SetDoorManualTick(enabled);
        }

        public void AdvanceCurrentDoorForGate(float deltaTime)
        {
            if (!gateManualTick || CurrentDoorController == null)
                return;
            CurrentDoorController.AdvanceForGate(deltaTime);
            if (!IsBusy && !IsFaultedSafe)
            {
                state = CurrentDoorController.State;
                UpdateIndicator();
            }
        }

        public void InjectNextDestinationLoadFailureForGate() => injectNextLoadFailure = true;

        public void InjectNextRecoveryFailureForGate() => injectNextRecoveryFailure = true;

        public bool IsPassengerInsideCabin()
        {
            if (contract == null || cabinRoot == null || passengerRoot == null)
                return false;
            Vector3 passenger = passengerRoot.position;
            float halfX = contract.CabinClearFloorM.x * 0.5f;
            float halfZ = contract.CabinClearFloorM.y * 0.5f;
            return Mathf.Abs(passenger.x - contract.CabinCenterXZ.x) <= halfX
                && Mathf.Abs(passenger.z - contract.CabinCenterXZ.y) <= halfZ
                && passenger.y >= cabinFloorElevationM - 0.20f
                && passenger.y <= cabinFloorElevationM + 0.35f;
        }

        public bool ValidateEndpoint(out string detail)
        {
            bool floorFound = TryFloor(committedFloorId, out S2FloorRecord floor, out int index);
            S2FloorRecord active = null;
            bool activeFound = contract != null && contract.ApprovedS2FloorContract != null
                && contract.ApprovedS2FloorContract.TryGetFloor(activeFloorId, out active);
            Scene scene = activeFound ? SceneManager.GetSceneByPath(active.scenePath) : default;
            bool exactCabin = floorFound && Mathf.Abs(cabinFloorElevationM - floor.elevationM) <= 0.001f
                && Mathf.Abs(cabinRoot.localPosition.y - (floor.elevationM + contract.ModelRootVerticalOffsetM)) <= 0.001f;
            bool door = floorFound && index < floorDoorControllers.Length && floorDoorControllers[index].enabled
                && floorDoorControllers.Count(item => item != null && item.enabled) == 1;
            bool oneFloor = LoadedFloorMarkers().Length == 1
                && LoadedFloorMarkers()[0].FloorId == activeFloorId;
            bool activeValid = scene.IsValid() && scene.isLoaded;
            detail = $"committed={committedFloorId}; active={activeFloorId}; cabin={cabinFloorElevationM:F3}; "
                + $"doors={floorDoorControllers.Count(item => item != null && item.enabled)}; markers={string.Join(",", LoadedFloorMarkers().Select(item => item.FloorId))}";
            return initialized && !IsBusy && !IsFaultedSafe && exactCabin && door && oneFloor && activeValid;
        }

        public bool ValidateLandingDoorSafety(out string detail)
        {
            var failures = new List<string>();
            for (int i = 0; i < floorDoorControllers.Length; i++)
            {
                if (i == FloorIndex(committedFloorId))
                    continue;
                S3DoorController controller = floorDoorControllers[i];
                if (controller == null || !controller.FullyClosed)
                {
                    failures.Add("F" + i.ToString("00") + ":state");
                    continue;
                }
                Transform[] leaves = controller.MovingLeaves;
                if (leaves.Length != 4 || leaves[2] == null || leaves[3] == null
                    || leaves[2].GetComponent<BoxCollider>() == null || leaves[3].GetComponent<BoxCollider>() == null)
                    failures.Add("F" + i.ToString("00") + ":leaves");
            }
            detail = failures.Count == 0 ? "all six non-current landing pairs are closed and collidable"
                : string.Join(",", failures);
            return failures.Count == 0;
        }

        private bool BeginTravel(string floorId, S3TravelRequestKind kind, bool carryPassenger,
            bool transitionActiveFloor)
        {
            if (CurrentDoorController.IsObstructed)
                return Reject("Travel rejected because the doorway is obstructed.");
            cancellationRequested = false;
            destinationFloorId = floorId;
            activeTravel = StartCoroutine(TravelRoutine(floorId, kind, carryPassenger, transitionActiveFloor));
            LastResult = kind == S3TravelRequestKind.CabinFloor
                ? "Cabin request accepted for " + floorId + "."
                : "Landing call accepted for " + floorId + ".";
            UpdateIndicator();
            return true;
        }

        private IEnumerator TravelRoutine(string targetFloorId, S3TravelRequestKind kind,
            bool carryPassenger, bool transitionActiveFloor)
        {
            string originCommitted = committedFloorId;
            string originActive = activeFloorId;
            TryFloor(originCommitted, out S2FloorRecord originFloor, out int originIndex);
            TryFloor(targetFloorId, out S2FloorRecord destinationFloor, out int destinationIndex);
            Vector3 startingPassengerOffset = passengerRoot.position - cabinRoot.position;
            pendingDestinationScenePath = string.Empty;

            bool closeAccepted = CurrentDoorController.RequestClose();
            if (!closeAccepted && !CurrentDoorController.MovementInterlockSafe)
            {
                CompleteWithoutTravel("Door closure was blocked; E01 stayed at " + originCommitted + ".");
                yield break;
            }
            yield return WaitForMovementInterlock();
            if (!CurrentDoorController.MovementInterlockSafe)
            {
                CompleteWithoutTravel("Door interlocks did not become safe; E01 stayed at " + originCommitted + ".");
                yield break;
            }

            Scene destinationScene = default;
            bool destinationWasPreloaded = false;
            if (transitionActiveFloor && !string.Equals(originActive, targetFloorId, StringComparison.Ordinal))
            {
                if (injectNextLoadFailure)
                {
                    injectNextLoadFailure = false;
                    yield return RecoverToCommitted(originFloor, originIndex, default, false, carryPassenger,
                        "Injected destination load failure before motion.");
                    yield break;
                }
                AsyncOperation load = LoadSceneAdditive(destinationFloor.scenePath);
                if (load == null)
                {
                    yield return RecoverToCommitted(originFloor, originIndex, default, false, carryPassenger,
                        "Destination load could not start.");
                    yield break;
                }
                pendingDestinationScenePath = destinationFloor.scenePath;
                float loadStart = Time.realtimeSinceStartup;
                while (!load.isDone && Time.realtimeSinceStartup - loadStart < contract.SceneLoadTimeoutSeconds)
                    yield return null;
                destinationScene = SceneManager.GetSceneByPath(destinationFloor.scenePath);
                string destinationDetail = "destination load did not complete";
                if (!load.isDone || !destinationScene.isLoaded
                    || !ValidateLoadedFloor(destinationScene, destinationFloor, true, out destinationDetail))
                {
                    yield return RecoverToCommitted(originFloor, originIndex, destinationScene,
                        destinationScene.IsValid() && destinationScene.isLoaded, carryPassenger,
                        "Destination validation failed: " + destinationDetail);
                    yield break;
                }
                destinationWasPreloaded = true;
            }
            else
            {
                destinationScene = SceneManager.GetSceneByPath(destinationFloor.scenePath);
                if (!ValidateLoadedFloor(destinationScene, destinationFloor, true, out string activeDestinationDetail))
                {
                    yield return RecoverToCommitted(originFloor, originIndex, default, false, carryPassenger,
                        "Active landing validation failed: " + activeDestinationDetail);
                    yield break;
                }
            }

            if (cancellationRequested)
            {
                yield return RecoverToCommitted(originFloor, originIndex, destinationScene,
                    destinationWasPreloaded, carryPassenger, "Travel cancelled before motion.");
                yield break;
            }

            state = S3ElevatorState.Moving;
            UpdateIndicator();
            float startY = cabinFloorElevationM;
            float distance = Mathf.Abs(destinationFloor.elevationM - startY);
            float duration = Mathf.Max(0.25f, distance * contract.TravelSecondsPerMeter);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (cancellationRequested)
                {
                    yield return RecoverToCommitted(originFloor, originIndex, destinationScene,
                        destinationWasPreloaded, carryPassenger, "Travel cancelled during motion.");
                    yield break;
                }
                float delta = TravelDeltaTime();
                if (delta <= 0f)
                {
                    yield return null;
                    continue;
                }
                elapsed = Mathf.Min(duration, elapsed + delta);
                float normalized = elapsed / duration;
                float eased = normalized * normalized * normalized * (normalized * (normalized * 6f - 15f) + 10f);
                SetCabinElevationImmediate(Mathf.LerpUnclamped(startY, destinationFloor.elevationM, eased), carryPassenger);
                if (carryPassenger)
                    maximumPassengerDriftM = Mathf.Max(maximumPassengerDriftM,
                        Vector3.Distance(startingPassengerOffset, passengerRoot.position - cabinRoot.position));
                yield return null;
            }
            SetCabinElevationImmediate(destinationFloor.elevationM, carryPassenger);

            if (transitionActiveFloor && destinationWasPreloaded)
            {
                SceneManager.SetActiveScene(destinationScene);
                if (contract.ApprovedS2FloorContract.TryGetFloor(originActive, out S2FloorRecord activeOrigin))
                {
                    Scene originScene = SceneManager.GetSceneByPath(activeOrigin.scenePath);
                    if (originScene.IsValid() && originScene.isLoaded)
                    {
                        AsyncOperation unload = SceneManager.UnloadSceneAsync(originScene);
                        if (unload == null)
                        {
                            yield return RecoverToCommitted(originFloor, originIndex, destinationScene, true,
                                carryPassenger, "Origin floor unload could not start.");
                            yield break;
                        }
                        float unloadStart = Time.realtimeSinceStartup;
                        while (!unload.isDone && Time.realtimeSinceStartup - unloadStart < contract.SceneLoadTimeoutSeconds)
                            yield return null;
                        if (!unload.isDone)
                        {
                            yield return RecoverToCommitted(originFloor, originIndex, destinationScene, true,
                                carryPassenger, "Origin floor unload timed out.");
                            yield break;
                        }
                    }
                }
                activeFloorId = targetFloorId;
            }

            committedFloorId = targetFloorId;
            destinationFloorId = string.Empty;
            pendingDestinationScenePath = string.Empty;
            SelectDoorController(destinationIndex);
            PositionThresholdAt(destinationFloor.elevationM);
            state = S3ElevatorState.ReadyClosed;
            CurrentDoorController.RequestOpen();
            CompletedTravelCount++;
            LastResult = kind == S3TravelRequestKind.CabinFloor
                ? "Arrived safely at " + targetFloorId + "."
                : "E01 is available at landing " + targetFloorId + ".";
            activeTravel = null;
            UpdateIndicator();
        }

        private IEnumerator RecoverToCommitted(S2FloorRecord originFloor, int originIndex,
            Scene destinationScene, bool destinationLoaded, bool carryPassenger, string reason)
        {
            state = S3ElevatorState.Recovering;
            LastResult = reason + " Rolling back to " + originFloor.floorId + ".";
            UpdateIndicator();
            if (injectNextRecoveryFailure)
            {
                injectNextRecoveryFailure = false;
                EnterFaultedSafe("Rollback failed after: " + reason, carryPassenger);
                yield break;
            }

            float startY = cabinFloorElevationM;
            float distance = Mathf.Abs(originFloor.elevationM - startY);
            float duration = Mathf.Max(0.25f, distance * contract.TravelSecondsPerMeter);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = TravelDeltaTime();
                if (delta <= 0f)
                {
                    yield return null;
                    continue;
                }
                elapsed = Mathf.Min(duration, elapsed + delta);
                float t = elapsed / duration;
                float eased = t * t * (3f - 2f * t);
                SetCabinElevationImmediate(Mathf.LerpUnclamped(startY, originFloor.elevationM, eased), carryPassenger);
                yield return null;
            }
            SetCabinElevationImmediate(originFloor.elevationM, carryPassenger);

            if (destinationLoaded && destinationScene.IsValid() && destinationScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(destinationScene);
                if (unload == null)
                {
                    EnterFaultedSafe("Rollback could not unload the destination scene.", carryPassenger);
                    yield break;
                }
                while (!unload.isDone)
                    yield return null;
            }

            if (!contract.ApprovedS2FloorContract.TryGetFloor(activeFloorId, out S2FloorRecord activeFloor))
            {
                EnterFaultedSafe("Rollback lost the active-floor contract.", carryPassenger);
                yield break;
            }
            Scene activeScene = SceneManager.GetSceneByPath(activeFloor.scenePath);
            if (!activeScene.IsValid() || !activeScene.isLoaded)
            {
                AsyncOperation reload = LoadSceneAdditive(activeFloor.scenePath);
                if (reload == null)
                {
                    EnterFaultedSafe("Rollback could not reload the committed floor.", carryPassenger);
                    yield break;
                }
                while (!reload.isDone)
                    yield return null;
                activeScene = SceneManager.GetSceneByPath(activeFloor.scenePath);
                if (!ValidateLoadedFloor(activeScene, activeFloor, true, out string recoveryFloorDetail))
                {
                    EnterFaultedSafe("Rollback floor validation failed: " + recoveryFloorDetail, carryPassenger);
                    yield break;
                }
            }
            SceneManager.SetActiveScene(activeScene);
            committedFloorId = originFloor.floorId;
            destinationFloorId = string.Empty;
            pendingDestinationScenePath = string.Empty;
            cancellationRequested = false;
            SelectDoorController(originIndex);
            PositionThresholdAt(originFloor.elevationM);
            CurrentDoorController.SetImmediateClosedForGate();
            state = S3ElevatorState.ReadyClosed;
            CurrentDoorController.RequestOpen();
            RecoveryCount++;
            LastResult = "Rollback completed safely at " + originFloor.floorId + ".";
            activeTravel = null;
            UpdateIndicator();
        }

        private IEnumerator ReviewRecoveryRoutine()
        {
            if (!TryFloor(committedFloorId, out S2FloorRecord committed, out int index))
            {
                EnterFaultedSafe("Review recovery could not find the committed floor.", lastFaultCarriedPassenger);
                yield break;
            }
            state = S3ElevatorState.Recovering;
            UpdateIndicator();
            float startY = cabinFloorElevationM;
            float duration = Mathf.Max(0.25f,
                Mathf.Abs(committed.elevationM - startY) * contract.TravelSecondsPerMeter);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float delta = TravelDeltaTime();
                if (delta <= 0f)
                {
                    yield return null;
                    continue;
                }
                elapsed = Mathf.Min(duration, elapsed + delta);
                float t = elapsed / duration;
                SetCabinElevationImmediate(Mathf.Lerp(startY, committed.elevationM, t * t * (3f - 2f * t)),
                    lastFaultCarriedPassenger);
                yield return null;
            }
            SetCabinElevationImmediate(committed.elevationM, lastFaultCarriedPassenger);

            if (!string.IsNullOrEmpty(pendingDestinationScenePath))
            {
                Scene pending = SceneManager.GetSceneByPath(pendingDestinationScenePath);
                if (pending.IsValid() && pending.isLoaded)
                {
                    AsyncOperation unload = SceneManager.UnloadSceneAsync(pending);
                    if (unload != null)
                        while (!unload.isDone)
                            yield return null;
                }
            }

            SelectDoorController(index);
            PositionThresholdAt(committed.elevationM);
            CurrentDoorController.SetImmediateClosedForGate();
            destinationFloorId = string.Empty;
            pendingDestinationScenePath = string.Empty;
            cancellationRequested = false;
            LastFault = string.Empty;
            state = S3ElevatorState.ReadyClosed;
            CurrentDoorController.RequestOpen();
            RecoveryCount++;
            LastResult = "Review recovery restored E01 at " + committedFloorId + ".";
            activeTravel = null;
            UpdateIndicator();
        }

        private IEnumerator WaitForMovementInterlock()
        {
            float elapsed = 0f;
            while (!CurrentDoorController.MovementInterlockSafe && elapsed < contract.RecoveryTimeoutSeconds)
            {
                float delta = TravelDeltaTime();
                if (gateManualTick && delta > 0f)
                    CurrentDoorController.AdvanceForGate(delta);
                elapsed += Mathf.Max(0f, delta);
                yield return null;
            }
        }

        private bool ValidateLoadedFloor(Scene scene, S2FloorRecord expected, bool applyAperture, out string detail)
        {
            if (!scene.IsValid() || !scene.isLoaded || expected == null)
            {
                detail = "scene or expected floor is unavailable";
                LastFloorValidationDetail = detail;
                return false;
            }
            S2FloorSceneMarker[] markers = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S2FloorSceneMarker>(true)).ToArray();
            S2ArrivalAnchor[] anchors = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S2ArrivalAnchor>(true)).ToArray();
            bool identity = markers.Length == 1 && markers[0].FloorId == expected.floorId
                && Mathf.Abs(markers[0].ElevationM - expected.elevationM) <= 0.001f
                && markers[0].transform.position.sqrMagnitude <= 0.000001f
                && Quaternion.Angle(markers[0].transform.rotation, Quaternion.identity) <= 0.001f
                && (markers[0].transform.localScale - Vector3.one).sqrMagnitude <= 0.000001f;
            bool arrival = anchors.Length == 1 && anchors[0].FloorId == expected.floorId;
            bool aperture = !applyAperture || (floorAperture.ApplyToFloorScene(scene, expected.floorId, out _)
                && floorAperture.ValidateAppliedToFloorScene(scene, expected.floorId, out _));
            detail = $"floor={expected.floorId}; markers={markers.Length}; anchors={anchors.Length}; identity={identity}; aperture={aperture}";
            LastFloorValidationDetail = detail;
            return identity && arrival && aperture;
        }

        private void SelectDoorController(int index)
        {
            for (int i = 0; i < floorDoorControllers.Length; i++)
                if (floorDoorControllers[i] != null)
                    floorDoorControllers[i].enabled = i == index;
            S3DoorController current = floorDoorControllers[index];
            current.SetImmediateClosedForGate();
            if (gateManualTick)
                current.SetGateManualTick(true);
        }

        private void PositionThresholdAt(float elevationM)
        {
            if (obstructionSensorRoot != null)
            {
                Vector3 sensor = obstructionSensorRoot.localPosition;
                sensor.y = elevationM + 1.10f;
                obstructionSensorRoot.localPosition = sensor;
            }
            if (obstructionProxy != null)
            {
                Vector3 proxy = obstructionProxy.transform.localPosition;
                proxy.y = elevationM + 1.00f;
                obstructionProxy.transform.localPosition = proxy;
            }
            Physics.SyncTransforms();
        }

        private void SetCabinElevationImmediate(float floorElevation, bool carryPassenger)
        {
            float previous = cabinFloorElevationM;
            Vector3 cabinPosition = cabinRoot.localPosition;
            cabinPosition.y = floorElevation + contract.ModelRootVerticalOffsetM;
            cabinRoot.localPosition = cabinPosition;
            cabinFloorElevationM = floorElevation;
            if (carryPassenger && passengerRoot != null)
                passengerRoot.position += Vector3.up * (floorElevation - previous);
            Physics.SyncTransforms();
        }

        private bool CanAcceptRequest(string floorId, bool requireCabin)
        {
            if (!initialized)
                return Reject("E01 is not initialized.");
            if (!TryFloor(floorId, out _, out _))
                return Reject("Invalid floor request: " + (floorId ?? "<null>") + ".");
            if (IsFaultedSafe)
                return Reject("E01 is enclosed in FaultedSafe; use review recovery.");
            if (IsBusy)
                return Reject("E01 is busy travelling to " + destinationFloorId + ".");
            if (CurrentDoorController == null)
                return Reject("E01 door controller is unavailable.");
            return true;
        }

        private bool SameFloorOpen(string floorId)
        {
            bool accepted = CurrentDoorController.RequestOpen();
            LastResult = accepted ? "E01 reopened at " + floorId + "." : "Same-floor reopen was rejected safely.";
            if (!accepted)
                RejectedRequestCount++;
            return accepted;
        }

        private void CompleteWithoutTravel(string result)
        {
            destinationFloorId = string.Empty;
            cancellationRequested = false;
            LastResult = result;
            state = CurrentDoorController == null ? S3ElevatorState.FaultedSafe : CurrentDoorController.State;
            activeTravel = null;
            UpdateIndicator();
        }

        private void EnterFaultedSafe(string reason, bool carriedPassenger)
        {
            if (CurrentDoorController != null)
                CurrentDoorController.SetImmediateClosedForGate();
            state = S3ElevatorState.FaultedSafe;
            LastFault = reason;
            LastResult = "FaultedSafe: " + reason;
            lastFaultCarriedPassenger = carriedPassenger;
            cancellationRequested = false;
            FaultCount++;
            activeTravel = null;
            UpdateIndicator();
            Debug.LogError("HOSPITAL_INTERIOR_R03_S3C_FAULTED_SAFE: " + reason, this);
        }

        private bool Reject(string result)
        {
            LastResult = result;
            RejectedRequestCount++;
            return false;
        }

        private float TravelDeltaTime() => gateManualTick ? 0.08f : Time.unscaledDeltaTime;

        private bool TryFloor(string floorId, out S2FloorRecord floor, out int index)
        {
            floor = null;
            index = -1;
            if (contract?.ApprovedS2FloorContract == null || string.IsNullOrEmpty(floorId))
                return false;
            IReadOnlyList<S2FloorRecord> floors = contract.ApprovedS2FloorContract.Floors;
            for (int i = 0; i < floors.Count; i++)
                if (string.Equals(floors[i].floorId, floorId, StringComparison.Ordinal))
                {
                    floor = floors[i];
                    index = i;
                    return true;
                }
            return false;
        }

        private bool TryFloorIndex(string floorId, out int index)
            => TryFloor(floorId, out _, out index);

        private int FloorIndex(string floorId) => TryFloorIndex(floorId, out int index) ? index : -1;

        private S2FloorSceneMarker[] LoadedFloorMarkers()
            => FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private bool ReferencesComplete() => contract != null && contract.ApprovedS2FloorContract != null
            && cabinRoot != null && passengerRoot != null && obstructionSensorRoot != null
            && obstructionProxy != null && floorAperture != null && cabinTravelIndicator != null
            && landingPortalRoots.Length == 7 && floorDoorControllers.Length == 7
            && landingPortalRoots.All(item => item != null) && floorDoorControllers.All(item => item != null);

        private void SetDoorManualTick(bool enabled)
        {
            foreach (S3DoorController controller in floorDoorControllers)
                if (controller != null)
                    controller.SetGateManualTick(enabled);
        }

        private void UpdateIndicator()
        {
            if (cabinTravelIndicator == null)
                return;
            if (state == S3ElevatorState.FaultedSafe)
                cabinTravelIndicator.text = "SAFE";
            else if (state == S3ElevatorState.Recovering)
                cabinTravelIndicator.text = "REC " + committedFloorId;
            else if (state == S3ElevatorState.Moving && TryFloor(destinationFloorId, out S2FloorRecord target, out _))
                cabinTravelIndicator.text = (target.elevationM >= cabinFloorElevationM ? "UP " : "DN ") + destinationFloorId;
            else
                cabinTravelIndicator.text = committedFloorId;
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
    }
}
