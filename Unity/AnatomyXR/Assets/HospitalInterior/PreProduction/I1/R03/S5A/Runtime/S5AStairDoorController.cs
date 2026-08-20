using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S5AStairDoorState
    {
        Closed,
        Opening,
        Open,
        Closing,
        FaultedSafe,
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class S5AStairDoorController : MonoBehaviour
    {
        [SerializeField] private string floorId;
        [SerializeField] private Transform doorLeaf;
        [SerializeField] private Transform visionPanel;
        [SerializeField] private Transform playerRoot;
        [SerializeField] private S5AVerticalCirculationController circulation;
        [SerializeField] private S5AStairDoorState state = S5AStairDoorState.Closed;
        [SerializeField, Range(0f, 1f)] private float linearOpenness;
        [SerializeField] private float clearDwellSeconds = 2.5f;
        [SerializeField] private bool gateManualTick;

        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private Vector3 openLocalPosition;
        private int occupancyCount;
        private float clearRemaining;
        [SerializeField] private bool configured;

        public string FloorId => floorId;
        public Transform DoorLeaf => doorLeaf;
        public Transform VisionPanel => visionPanel;
        public S5AStairDoorState State => state;
        public float LinearOpenness => linearOpenness;
        public bool FullyClosed => linearOpenness <= 0.0001f;
        public bool FullyOpen => linearOpenness >= 0.9999f;
        public bool IsOccupied => occupancyCount > 0;
        public bool IsObstructed => IsOccupied && state == S5AStairDoorState.Closing;
        public int ReversalCount { get; private set; }
        public string LastResult { get; private set; } = string.Empty;

        public void Configure(string targetFloorId, Transform leaf, Transform vision,
            Transform player, S5AVerticalCirculationController owner, float slideM)
        {
            floorId = targetFloorId;
            doorLeaf = leaf;
            visionPanel = vision;
            playerRoot = player;
            circulation = owner;
            if (doorLeaf == null || circulation == null)
            {
                state = S5AStairDoorState.FaultedSafe;
                configured = false;
                return;
            }
            if (visionPanel != null && visionPanel.parent != doorLeaf)
                visionPanel.SetParent(doorLeaf, true);
            closedLocalPosition = doorLeaf.localPosition;
            openLocalPosition = closedLocalPosition + Vector3.forward * slideM;
            configured = true;
            SetImmediateClosedForGate();
        }

        private void Update()
        {
            if (!gateManualTick)
                Advance(Time.unscaledDeltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (BelongsToPlayer(other))
            {
                occupancyCount++;
                clearRemaining = clearDwellSeconds;
                if (state == S5AStairDoorState.Closing)
                {
                    ReversalCount++;
                    BeginOpenIfSafe();
                }
                else if (FullyClosed)
                    RequestAccess();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (BelongsToPlayer(other))
            {
                occupancyCount = Mathf.Max(0, occupancyCount - 1);
                if (occupancyCount == 0)
                    clearRemaining = clearDwellSeconds;
            }
        }

        public bool RequestAccess()
        {
            if (!configured || state == S5AStairDoorState.FaultedSafe)
            {
                LastResult = "Stair door references are incomplete.";
                return false;
            }
            if (circulation.FloorCoordinator.IsFloorLoadedAndValidated(floorId))
                return BeginOpenIfSafe();
            bool accepted = circulation.RequestStairAccess(this);
            LastResult = accepted
                ? "Requested validated floor transition for Stair A " + floorId + "."
                : circulation.LastResult;
            return accepted;
        }

        public bool BeginOpenIfSafe()
        {
            if (!configured || !circulation.FloorCoordinator.IsFloorLoadedAndValidated(floorId))
            {
                LastResult = "Door remains closed: " + floorId + " is not loaded and validated.";
                LastResult += " " + circulation.FloorCoordinator.LastFloorValidationDetail;
                return false;
            }
            return BeginOpenAfterValidatedCommit();
        }

        public bool BeginOpenAfterValidatedCommit()
        {
            if (!configured || state == S5AStairDoorState.FaultedSafe)
                return false;
            clearRemaining = clearDwellSeconds;
            state = FullyOpen ? S5AStairDoorState.Open : S5AStairDoorState.Opening;
            LastResult = "Stair A door opening at " + floorId + ".";
            return true;
        }

        public bool RequestClose()
        {
            if (!configured || state == S5AStairDoorState.FaultedSafe)
                return false;
            if (IsOccupied)
            {
                if (state == S5AStairDoorState.Closing)
                    ReversalCount++;
                return BeginOpenIfSafe();
            }
            state = FullyClosed ? S5AStairDoorState.Closed : S5AStairDoorState.Closing;
            LastResult = "Stair A door closing at " + floorId + ".";
            return true;
        }

        public void SetGateOccupancy(bool occupied)
        {
            occupancyCount = occupied ? 1 : 0;
            clearRemaining = clearDwellSeconds;
            if (occupied && state == S5AStairDoorState.Closing)
            {
                ReversalCount++;
                BeginOpenIfSafe();
            }
        }

        public void SetGateManualTick(bool enabled) => gateManualTick = enabled;

        public void AdvanceForGate(float deltaTime)
        {
            if (!gateManualTick)
                return;
            Advance(deltaTime);
        }

        public void SetImmediateClosedForGate()
        {
            if (doorLeaf == null)
                return;
            linearOpenness = 0f;
            doorLeaf.localPosition = closedLocalPosition;
            state = configured ? S5AStairDoorState.Closed : S5AStairDoorState.FaultedSafe;
            clearRemaining = 0f;
        }

        private void Advance(float deltaTime)
        {
            if (!configured || deltaTime <= 0f || state == S5AStairDoorState.FaultedSafe)
                return;
            switch (state)
            {
                case S5AStairDoorState.Opening:
                    linearOpenness = Mathf.MoveTowards(linearOpenness, 1f, deltaTime / 1.2f);
                    ApplyPosition();
                    if (FullyOpen)
                    {
                        state = S5AStairDoorState.Open;
                        clearRemaining = clearDwellSeconds;
                    }
                    break;
                case S5AStairDoorState.Open:
                    if (IsOccupied)
                        clearRemaining = clearDwellSeconds;
                    else
                    {
                        clearRemaining = Mathf.Max(0f, clearRemaining - deltaTime);
                        if (clearRemaining <= 0f)
                            RequestClose();
                    }
                    break;
                case S5AStairDoorState.Closing:
                    if (IsOccupied)
                    {
                        ReversalCount++;
                        BeginOpenIfSafe();
                        break;
                    }
                    linearOpenness = Mathf.MoveTowards(linearOpenness, 0f, deltaTime / 1.2f);
                    ApplyPosition();
                    if (FullyClosed)
                        state = S5AStairDoorState.Closed;
                    break;
            }
        }

        private void ApplyPosition()
        {
            float t = linearOpenness * linearOpenness * (3f - 2f * linearOpenness);
            doorLeaf.localPosition = Vector3.LerpUnclamped(closedLocalPosition,
                openLocalPosition, t);
        }

        private bool BelongsToPlayer(Collider other)
        {
            if (other == null || playerRoot == null)
                return false;
            return other.transform == playerRoot || other.transform.IsChildOf(playerRoot)
                || playerRoot.IsChildOf(other.transform);
        }
    }
}
