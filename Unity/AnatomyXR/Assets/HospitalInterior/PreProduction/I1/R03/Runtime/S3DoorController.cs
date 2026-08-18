using System;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DoorController : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract contract;
        [SerializeField] private string currentFloorId = "F00";
        [SerializeField] private Transform cabinLeft;
        [SerializeField] private Transform cabinRight;
        [SerializeField] private Transform landingLeft;
        [SerializeField] private Transform landingRight;
        [SerializeField] private S3DoorObstructionSensor obstructionSensor;
        [SerializeField] private S3ElevatorState state = S3ElevatorState.ReadyClosed;
        [SerializeField, Range(0f, 1f)] private float linearOpenness;
        [SerializeField] private float dwellRemaining;
        [SerializeField] private bool cabinInterlock = true;
        [SerializeField] private bool landingInterlock = true;
        [SerializeField] private bool alignmentConfirmed;
        [SerializeField] private bool gateManualTick;

        [SerializeField] private Vector3 cabinLeftClosed;
        [SerializeField] private Vector3 cabinRightClosed;
        [SerializeField] private Vector3 landingLeftClosed;
        [SerializeField] private Vector3 landingRightClosed;
        [SerializeField] private Vector3 cabinLeftOpen;
        [SerializeField] private Vector3 cabinRightOpen;
        [SerializeField] private Vector3 landingLeftOpen;
        [SerializeField] private Vector3 landingRightOpen;
        private bool earlyCloseRequested;

        public S3ElevatorContract Contract => contract;
        public string CurrentFloorId => currentFloorId;
        public S3DoorObstructionSensor ObstructionSensor => obstructionSensor;
        public S3ElevatorState State => state;
        public float LinearOpenness => linearOpenness;
        public float EasedOpenness => Smooth01(linearOpenness);
        public float DwellRemaining => dwellRemaining;
        public bool CabinInterlock => cabinInterlock;
        public bool LandingInterlock => landingInterlock;
        public bool AlignmentConfirmed => alignmentConfirmed;
        public bool IsObstructed => obstructionSensor != null && obstructionSensor.IsObstructed;
        public bool MovementInterlockSafe => state == S3ElevatorState.ReadyClosed && FullyClosed
            && cabinInterlock && landingInterlock && alignmentConfirmed;
        public bool FullyClosed => linearOpenness <= 0.0001f;
        public bool FullyOpen => linearOpenness >= 0.9999f;
        public float MeasuredClearWidthM => contract == null ? 0f
            : 0.01f + 2f * contract.DoorTravelM * EasedOpenness;
        public Transform[] MovingLeaves => new[] { cabinLeft, cabinRight, landingLeft, landingRight };

        public void Configure(S3ElevatorContract elevatorContract, string floorId,
            Transform cabinLeftLeaf, Transform cabinRightLeaf, Transform landingLeftLeaf, Transform landingRightLeaf,
            S3DoorObstructionSensor sensor)
        {
            contract = elevatorContract;
            currentFloorId = floorId;
            cabinLeft = cabinLeftLeaf;
            cabinRight = cabinRightLeaf;
            landingLeft = landingLeftLeaf;
            landingRight = landingRightLeaf;
            obstructionSensor = sensor;
            alignmentConfirmed = contract != null && cabinLeft != null && cabinRight != null
                && landingLeft != null && landingRight != null && obstructionSensor != null;
            if (!alignmentConfirmed)
            {
                EnterFaultedSafe("Door controller references are incomplete.");
                return;
            }

            cabinLeftClosed = cabinLeft.localPosition;
            cabinRightClosed = cabinRight.localPosition;
            landingLeftClosed = landingLeft.localPosition;
            landingRightClosed = landingRight.localPosition;
            Vector3 travel = Vector3.right * contract.DoorTravelM;
            cabinLeftOpen = cabinLeftClosed - travel;
            cabinRightOpen = cabinRightClosed + travel;
            landingLeftOpen = landingLeftClosed - travel;
            landingRightOpen = landingRightClosed + travel;
            SetImmediateClosedForGate();
        }

        private void Update()
        {
            if (!gateManualTick)
                Advance(Time.unscaledDeltaTime);
        }

        public bool RequestOpen()
        {
            if (!CanOperate())
                return false;
            earlyCloseRequested = false;
            dwellRemaining = contract.DoorDwellSeconds;
            if (state == S3ElevatorState.ReadyOpen)
                return true;
            cabinInterlock = false;
            landingInterlock = false;
            state = S3ElevatorState.DoorOpening;
            return true;
        }

        public bool RequestClose()
        {
            if (!CanOperate())
                return false;
            if (IsObstructed)
            {
                dwellRemaining = contract.DoorDwellSeconds;
                if (state == S3ElevatorState.DoorClosing)
                    RequestOpen();
                return false;
            }
            if (state == S3ElevatorState.ReadyClosed || state == S3ElevatorState.DoorClosing)
                return true;
            if (state == S3ElevatorState.DoorOpening)
            {
                earlyCloseRequested = true;
                return true;
            }
            cabinInterlock = false;
            landingInterlock = false;
            dwellRemaining = 0f;
            state = S3ElevatorState.DoorClosing;
            return true;
        }

        public void ConfirmLandingAlignment(bool confirmed)
        {
            alignmentConfirmed = confirmed;
            if (!confirmed && state != S3ElevatorState.FaultedSafe)
                EnterFaultedSafe("Landing alignment was lost.");
        }

        public void AdvanceForGate(float unscaledDeltaTime)
        {
            if (!gateManualTick)
                throw new InvalidOperationException("Manual gate ticking is not enabled.");
            Advance(unscaledDeltaTime);
        }

        public void SetGateManualTick(bool enabled) => gateManualTick = enabled;

        public void SetImmediateClosedForGate()
        {
            if (!HasLeaves())
                return;
            linearOpenness = 0f;
            dwellRemaining = 0f;
            earlyCloseRequested = false;
            ApplyLeafPositions();
            cabinInterlock = alignmentConfirmed;
            landingInterlock = alignmentConfirmed;
            state = alignmentConfirmed ? S3ElevatorState.ReadyClosed : S3ElevatorState.FaultedSafe;
        }

        public void SetImmediateOpenForGate()
        {
            if (!HasLeaves())
                return;
            linearOpenness = 1f;
            dwellRemaining = contract == null ? 0f : contract.DoorDwellSeconds;
            earlyCloseRequested = false;
            ApplyLeafPositions();
            cabinInterlock = false;
            landingInterlock = false;
            state = S3ElevatorState.ReadyOpen;
        }

        public bool DoorPairsSynchronized(float toleranceM)
        {
            if (!HasLeaves())
                return false;
            float cabinLeftDelta = Mathf.Abs(cabinLeft.localPosition.x - cabinLeftClosed.x);
            float cabinRightDelta = Mathf.Abs(cabinRight.localPosition.x - cabinRightClosed.x);
            float landingLeftDelta = Mathf.Abs(landingLeft.localPosition.x - landingLeftClosed.x);
            float landingRightDelta = Mathf.Abs(landingRight.localPosition.x - landingRightClosed.x);
            return Mathf.Abs(cabinLeftDelta - cabinRightDelta) <= toleranceM
                && Mathf.Abs(cabinLeftDelta - landingLeftDelta) <= toleranceM
                && Mathf.Abs(cabinLeftDelta - landingRightDelta) <= toleranceM;
        }

        private void Advance(float deltaTime)
        {
            if (contract == null || !HasLeaves() || state == S3ElevatorState.FaultedSafe || deltaTime <= 0f)
                return;

            switch (state)
            {
                case S3ElevatorState.DoorOpening:
                    linearOpenness = Mathf.MoveTowards(linearOpenness, 1f, deltaTime / contract.DoorOpenSeconds);
                    ApplyLeafPositions();
                    if (FullyOpen)
                    {
                        state = S3ElevatorState.ReadyOpen;
                        dwellRemaining = earlyCloseRequested ? 0f : contract.DoorDwellSeconds;
                        bool closeNow = earlyCloseRequested;
                        earlyCloseRequested = false;
                        if (closeNow)
                            RequestClose();
                    }
                    break;

                case S3ElevatorState.ReadyOpen:
                    if (IsObstructed)
                    {
                        dwellRemaining = contract.DoorDwellSeconds;
                        break;
                    }
                    dwellRemaining = Mathf.Max(0f, dwellRemaining - deltaTime);
                    if (dwellRemaining <= 0f)
                        RequestClose();
                    break;

                case S3ElevatorState.DoorClosing:
                    if (IsObstructed)
                    {
                        RequestOpen();
                        break;
                    }
                    linearOpenness = Mathf.MoveTowards(linearOpenness, 0f, deltaTime / contract.DoorCloseSeconds);
                    ApplyLeafPositions();
                    if (FullyClosed)
                    {
                        cabinInterlock = true;
                        landingInterlock = true;
                        state = S3ElevatorState.ReadyClosed;
                    }
                    break;
            }
        }

        private void ApplyLeafPositions()
        {
            float t = Smooth01(linearOpenness);
            cabinLeft.localPosition = Vector3.LerpUnclamped(cabinLeftClosed, cabinLeftOpen, t);
            cabinRight.localPosition = Vector3.LerpUnclamped(cabinRightClosed, cabinRightOpen, t);
            landingLeft.localPosition = Vector3.LerpUnclamped(landingLeftClosed, landingLeftOpen, t);
            landingRight.localPosition = Vector3.LerpUnclamped(landingRightClosed, landingRightOpen, t);
        }

        private bool CanOperate() => contract != null && HasLeaves() && alignmentConfirmed
            && state != S3ElevatorState.Moving && state != S3ElevatorState.Recovering
            && state != S3ElevatorState.FaultedSafe;

        private bool HasLeaves() => cabinLeft != null && cabinRight != null && landingLeft != null && landingRight != null;

        private void EnterFaultedSafe(string reason)
        {
            state = S3ElevatorState.FaultedSafe;
            cabinInterlock = false;
            landingInterlock = false;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S3B_DOOR_FAULT: " + reason, this);
        }

        private static float Smooth01(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }
    }
}
