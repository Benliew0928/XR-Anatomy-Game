using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S5AVerticalOperation
    {
        None,
        ElevatorJourney,
        LandingCall,
        StairDoorTransition,
        Recovery,
    }

    [DisallowMultipleComponent]
    public sealed class S5AVerticalCirculationController : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private S5AStairContract stairContract;
        [SerializeField] private S5AFloorSceneCoordinator floorCoordinator;
        [SerializeField] private Transform cabinRoot;
        [SerializeField] private Transform passengerRoot;
        [SerializeField] private S3DoorController[] elevatorDoors = Array.Empty<S3DoorController>();
        [SerializeField] private TextMesh cabinIndicator;
        [SerializeField] private S5AStairDoorController[] stairDoors = Array.Empty<S5AStairDoorController>();
        [SerializeField] private string cabinFloorId = "F00";
        [SerializeField] private S5AVerticalOperation activeOperation;

        private Coroutine operation;
        private bool initialized;
        private bool gatePassengerOverride;
        private float gateDurationMultiplier = 1f;
        private bool cancellationRequested;
        private S5AStairDoorController openStairDoor;

        public S3ElevatorContract ElevatorContract => elevatorContract;
        public S5AStairContract StairContract => stairContract;
        public S5AFloorSceneCoordinator FloorCoordinator => floorCoordinator;
        public Transform CabinRoot => cabinRoot;
        public Transform PassengerRoot => passengerRoot;
        public S3DoorController[] ElevatorDoors => elevatorDoors;
        public S5AStairDoorController[] StairDoors => stairDoors;
        public string CabinFloorId => cabinFloorId;
        public string ActiveFloorId => floorCoordinator == null ? string.Empty : floorCoordinator.ActiveFloorId;
        public S5AVerticalOperation ActiveOperation => activeOperation;
        public bool IsBusy => operation != null || floorCoordinator != null && floorCoordinator.IsTransitioning;
        public bool IsInitialized => initialized;
        public string LastResult { get; private set; } = string.Empty;
        public int CompletedElevatorJourneys { get; private set; }
        public int CompletedStairTransitions { get; private set; }
        public int RejectedRequestCount { get; private set; }
        public int LandingCallCount { get; private set; }

        public void Configure(S3ElevatorContract approvedElevator,
            S5AStairContract approvedStair, S5AFloorSceneCoordinator coordinator,
            Transform movingCabin, Transform passenger, S3DoorController[] doors,
            TextMesh indicator)
        {
            elevatorContract = approvedElevator;
            stairContract = approvedStair;
            floorCoordinator = coordinator;
            cabinRoot = movingCabin;
            passengerRoot = passenger;
            elevatorDoors = doors ?? Array.Empty<S3DoorController>();
            cabinIndicator = indicator;
        }

        public void ConfigureStairDoors(S5AStairDoorController[] doors)
            => stairDoors = doors ?? Array.Empty<S5AStairDoorController>();

        public bool InitializeAtFloor(string floorId, out string detail)
        {
            if (elevatorContract == null || stairContract == null || floorCoordinator == null
                || cabinRoot == null || passengerRoot == null || elevatorDoors.Length != 7
                || stairDoors.Length != 7 || !stairContract.TryGetFloor(floorId, out S2FloorRecord floor))
            {
                detail = "vertical-circulation references are incomplete";
                return false;
            }
            cabinFloorId = floorId;
            SetCabinElevation(floor.elevationM, false);
            SelectElevatorDoor(FloorIndex(floorId));
            foreach (S5AStairDoorController door in stairDoors)
                door.SetImmediateClosedForGate();
            activeOperation = S5AVerticalOperation.None;
            initialized = true;
            UpdateIndicator();
            detail = "S5A vertical circulation initialized with cabin and active floor at " + floorId + ".";
            return true;
        }

        public bool RequestElevatorFloor(string floorId)
        {
            if (!CanStart(floorId))
                return false;
            if (!IsPassengerInsideCabin())
                return Reject("Enter E01 before selecting " + floorId + ".");
            if (floorId == cabinFloorId)
                return RequestDoorOpen();
            operation = StartCoroutine(ElevatorJourney(floorId, true, true));
            activeOperation = S5AVerticalOperation.ElevatorJourney;
            LastResult = "E01 journey accepted for " + floorId + ".";
            return true;
        }

        public bool RequestLandingCall(string floorId)
        {
            if (!CanStart(floorId))
                return false;
            if (floorId != floorCoordinator.ActiveFloorId)
                return Reject("Landing call rejected: " + floorId + " is not the active floor.");
            if (floorId == cabinFloorId)
                return RequestDoorOpen();
            operation = StartCoroutine(ElevatorJourney(floorId, false, false));
            activeOperation = S5AVerticalOperation.LandingCall;
            LastResult = "Landing call accepted at " + floorId + "; E01 remains at its actual cabin datum until travel.";
            return true;
        }

        public bool RequestStairAccess(S5AStairDoorController destinationDoor)
        {
            if (destinationDoor == null || !CanStart(destinationDoor.FloorId))
                return false;
            operation = StartCoroutine(StairTransition(destinationDoor));
            activeOperation = S5AVerticalOperation.StairDoorTransition;
            LastResult = "Stair transition accepted for " + destinationDoor.FloorId + ".";
            return true;
        }

        public bool RequestDoorOpen()
        {
            if (!initialized || IsBusy)
                return Reject("E01 door-open rejected while vertical circulation is busy.");
            if (cabinFloorId != floorCoordinator.ActiveFloorId)
                return Reject("E01 landing door remains closed toward an unloaded floor.");
            S3DoorController door = CurrentElevatorDoor;
            bool accepted = door != null && door.RequestOpen();
            LastResult = accepted ? "E01 door opening at " + cabinFloorId + "."
                : "E01 door-open rejected safely.";
            return accepted;
        }

        public bool RequestDoorClose()
        {
            if (!initialized || IsBusy)
                return Reject("E01 door-close rejected while vertical circulation is busy.");
            bool accepted = CurrentElevatorDoor != null && CurrentElevatorDoor.RequestClose();
            LastResult = accepted ? "E01 door closing at " + cabinFloorId + "."
                : "E01 door-close blocked or rejected safely.";
            return accepted;
        }

        public bool CancelActiveOperation()
        {
            if (!IsBusy || activeOperation == S5AVerticalOperation.StairDoorTransition)
                return Reject("No cancellable E01 operation is active.");
            cancellationRequested = true;
            LastResult = "E01 cancellation requested; the cabin will retain its last safe floor.";
            return true;
        }

        public void SetGatePassengerOverride(bool enabled) => gatePassengerOverride = enabled;
        public void SetGateDurationMultiplier(float multiplier)
            => gateDurationMultiplier = Mathf.Clamp(multiplier, 0.01f, 1f);

        public bool IsPassengerInsideCabin()
        {
            if (gatePassengerOverride)
                return true;
            if (elevatorContract == null || passengerRoot == null)
                return false;
            Vector3 point = passengerRoot.position;
            float halfX = elevatorContract.CabinClearFloorM.x * 0.5f;
            float halfZ = elevatorContract.CabinClearFloorM.y * 0.5f;
            return Mathf.Abs(point.x - elevatorContract.CabinCenterXZ.x) <= halfX
                && Mathf.Abs(point.z - elevatorContract.CabinCenterXZ.y) <= halfZ
                && stairContract.TryGetFloor(cabinFloorId, out S2FloorRecord floor)
                && point.y >= floor.elevationM - 0.25f && point.y <= floor.elevationM + 0.45f;
        }

        public bool ValidateEndpoint(out string detail)
        {
            bool floor = stairContract.TryGetFloor(cabinFloorId, out S2FloorRecord record);
            bool cabin = floor && Mathf.Abs(cabinRoot.localPosition.y
                - (record.elevationM + elevatorContract.ModelRootVerticalOffsetM)) <= 0.002f;
            bool oneDoor = elevatorDoors.Count(item => item != null && item.enabled) == 1;
            bool steady = floorCoordinator.ValidateSteadyState(out string floorDetail);
            detail = $"cabin={cabinFloorId}@{cabinRoot.localPosition.y:F3}; active={ActiveFloorId}; elevatorDoors={elevatorDoors.Count(item => item != null && item.enabled)}; {floorDetail}";
            return initialized && !IsBusy && cabin && oneDoor && steady;
        }

        private IEnumerator ElevatorJourney(string targetFloorId, bool carryPassenger,
            bool transitionFloor)
        {
            cancellationRequested = false;
            string originCabin = cabinFloorId;
            stairContract.TryGetFloor(originCabin, out S2FloorRecord origin);
            stairContract.TryGetFloor(targetFloorId, out S2FloorRecord destination);
            if (openStairDoor != null)
            {
                openStairDoor.RequestClose();
                openStairDoor = null;
            }
            S3DoorController originDoor = CurrentElevatorDoor;
            if (originDoor != null)
            {
                originDoor.RequestClose();
                float closeTimeout = Time.realtimeSinceStartup + 5f;
                while (!originDoor.MovementInterlockSafe && Time.realtimeSinceStartup < closeTimeout)
                    yield return null;
                if (!originDoor.MovementInterlockSafe)
                {
                    Finish(false, "E01 stayed at " + originCabin + " because its door interlock did not close.");
                    yield break;
                }
            }
            if (transitionFloor && targetFloorId != floorCoordinator.ActiveFloorId)
            {
                if (!floorCoordinator.RequestTransition(targetFloorId))
                {
                    Finish(false, floorCoordinator.LastResult);
                    yield break;
                }
                while (floorCoordinator.IsTransitioning)
                    yield return null;
                if (!floorCoordinator.LastTransitionSucceeded)
                {
                    if (originDoor != null && originCabin == floorCoordinator.ActiveFloorId)
                        originDoor.RequestOpen();
                    Finish(false, floorCoordinator.LastResult);
                    yield break;
                }
            }
            if (cancellationRequested)
            {
                Finish(false, "E01 cancellation completed before motion at " + originCabin + ".");
                yield break;
            }

            float startY = origin.elevationM;
            float duration = Mathf.Max(0.25f,
                Mathf.Abs(destination.elevationM - startY) * elevatorContract.TravelSecondsPerMeter
                * gateDurationMultiplier);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * t * (t * (t * 6f - 15f) + 10f);
                SetCabinElevation(Mathf.LerpUnclamped(startY, destination.elevationM, eased), carryPassenger);
                yield return null;
            }
            SetCabinElevation(destination.elevationM, carryPassenger);
            cabinFloorId = targetFloorId;
            SelectElevatorDoor(FloorIndex(targetFloorId));
            if (targetFloorId == floorCoordinator.ActiveFloorId)
                CurrentElevatorDoor.RequestOpen();
            if (activeOperation == S5AVerticalOperation.LandingCall)
                LandingCallCount++;
            else
                CompletedElevatorJourneys++;
            Finish(true, "E01 arrived at " + targetFloorId + "; active floor is "
                + floorCoordinator.ActiveFloorId + ".");
        }

        private IEnumerator StairTransition(S5AStairDoorController destinationDoor)
        {
            string target = destinationDoor.FloorId;
            if (openStairDoor != null && openStairDoor != destinationDoor)
            {
                openStairDoor.RequestClose();
                float timeout = Time.realtimeSinceStartup + 5f;
                while (!openStairDoor.FullyClosed && Time.realtimeSinceStartup < timeout)
                    yield return null;
                if (!openStairDoor.FullyClosed)
                {
                    Finish(false, "Previous Stair A door could not close safely.");
                    yield break;
                }
            }
            if (!floorCoordinator.RequestTransition(target))
            {
                Finish(false, floorCoordinator.LastResult);
                yield break;
            }
            while (floorCoordinator.IsTransitioning)
                yield return null;
            if (!floorCoordinator.LastTransitionSucceeded
                || !floorCoordinator.IsFloorLoadedAndValidated(target))
            {
                destinationDoor.SetImmediateClosedForGate();
                Finish(false, floorCoordinator.LastResult + " Door validation: "
                    + floorCoordinator.LastFloorValidationDetail);
                yield break;
            }
            // The coordinator has just committed and independently validated the
            // destination. Open through this transaction-owned path so trigger
            // callbacks cannot observe an intermediate coroutine bookkeeping frame.
            if (!destinationDoor.BeginOpenAfterValidatedCommit())
            {
                destinationDoor.SetImmediateClosedForGate();
                Finish(false, destinationDoor.LastResult);
                yield break;
            }
            openStairDoor = destinationDoor;
            CompletedStairTransitions++;
            Finish(true, "Stair A committed " + target + " without moving E01 from " + cabinFloorId + ".");
        }

        private bool CanStart(string floorId)
        {
            if (!initialized)
                return Reject("Vertical circulation is not initialized.");
            if (IsBusy)
                return Reject("Conflicting elevator/stair request rejected until the active transition finishes.");
            if (!stairContract.TryGetFloor(floorId, out _))
                return Reject("Unknown floor " + floorId + ".");
            return true;
        }

        private bool Reject(string reason)
        {
            RejectedRequestCount++;
            LastResult = reason;
            return false;
        }

        private void Finish(bool success, string result)
        {
            LastResult = result;
            operation = null;
            activeOperation = S5AVerticalOperation.None;
            cancellationRequested = false;
            UpdateIndicator();
            if (!success)
                Debug.LogWarning("HOSPITAL_INTERIOR_R03_S5A_SAFE_REJECTION: " + result, this);
        }

        private void SelectElevatorDoor(int selected)
        {
            for (int index = 0; index < elevatorDoors.Length; index++)
            {
                S3DoorController door = elevatorDoors[index];
                if (door == null)
                    continue;
                door.enabled = true;
                door.ConfirmLandingAlignment(true);
                door.SetImmediateClosedForGate();
                door.enabled = index == selected;
            }
        }

        private void SetCabinElevation(float elevation, bool carryPassenger)
        {
            float previousCabinY = cabinRoot.localPosition.y;
            Vector3 local = cabinRoot.localPosition;
            float newCabinY = elevation + elevatorContract.ModelRootVerticalOffsetM;
            float deltaY = newCabinY - previousCabinY;
            local.y = newCabinY;
            cabinRoot.localPosition = local;
            if (carryPassenger && passengerRoot != null)
            {
                CharacterController controller = passengerRoot.GetComponent<CharacterController>();
                if (controller != null && controller.enabled)
                    controller.Move(Vector3.up * deltaY);
                else
                    passengerRoot.position += Vector3.up * deltaY;
            }
            UpdateIndicator();
        }

        private int FloorIndex(string floorId)
            => int.TryParse(floorId?.Substring(1), out int index) ? index : 0;

        private S3DoorController CurrentElevatorDoor
        {
            get
            {
                int index = FloorIndex(cabinFloorId);
                return index >= 0 && index < elevatorDoors.Length ? elevatorDoors[index] : null;
            }
        }

        private void UpdateIndicator()
        {
            if (cabinIndicator != null)
                cabinIndicator.text = "E01 " + cabinFloorId + " | ACTIVE " + ActiveFloorId
                    + (IsBusy ? " | " + activeOperation : string.Empty);
        }
    }
}
