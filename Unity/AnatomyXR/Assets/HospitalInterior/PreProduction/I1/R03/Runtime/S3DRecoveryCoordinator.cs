using System;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DRecoveryCoordinator : MonoBehaviour
    {
        [SerializeField] private S3DAdaptiveRig adaptiveRig;
        [SerializeField] private S3ElevatorController elevatorController;
        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private float fallMarginM = 3f;

        private Vector3 lastF00Checkpoint = S3DIntegrationContract.GateSpawn;
        private Quaternion lastF00Rotation = Quaternion.identity;

        public int RecoveryCount { get; private set; }
        public string LastRecoveryReason { get; private set; } = string.Empty;
        public string LastCheckpointName { get; private set; } = "Gate";

        public void Configure(S3DAdaptiveRig rig, S3ElevatorController elevator,
            S3ElevatorContract contract)
        {
            adaptiveRig = rig;
            elevatorController = elevator;
            elevatorContract = contract;
        }

        private void Update()
        {
            if (adaptiveRig == null || !adaptiveRig.EnvironmentReady
                || elevatorController == null || elevatorContract == null)
                return;
            UpdateF00Checkpoint();
            RecoverIfNeeded();
        }

        private void UpdateF00Checkpoint()
        {
            if (!string.Equals(elevatorController.ActiveFloorId, "F00", StringComparison.Ordinal)
                || elevatorController.IsBusy || elevatorController.IsPassengerInsideCabin())
                return;

            Vector3 position = adaptiveRig.transform.position;
            TryCheckpoint(position, S3DIntegrationContract.GateSpawn, "Gate", 8f);
            TryCheckpoint(position, S3DIntegrationContract.LobbyCheckpoint, "Lobby_Forecourt", 8f);
            TryCheckpoint(position, S3DIntegrationContract.EntranceCheckpoint, "Main_Entrance", 7f);
            if (elevatorContract.ApprovedS2FloorContract.TryGetFloor("F00", out S2FloorRecord floor))
                TryCheckpoint(position, floor.arrivalPosition, "F00_Elevator_Arrival", 7f);
        }

        private void TryCheckpoint(Vector3 current, Vector3 checkpoint, string name, float radius)
        {
            Vector2 delta = new Vector2(current.x - checkpoint.x, current.z - checkpoint.z);
            if (delta.magnitude > radius || Mathf.Abs(current.y - checkpoint.y) > 1.5f)
                return;
            lastF00Checkpoint = checkpoint;
            lastF00Rotation = adaptiveRig.transform.rotation;
            LastCheckpointName = name;
        }

        public bool RecoverIfNeeded()
        {
            if (adaptiveRig == null || elevatorController == null || elevatorContract == null
                || elevatorController.IsBusy || elevatorController.IsPassengerInsideCabin())
                return false;
            if (!elevatorContract.ApprovedS2FloorContract.TryGetFloor(
                elevatorController.ActiveFloorId, out S2FloorRecord floor))
                return false;
            if (adaptiveRig.transform.position.y >= floor.elevationM - fallMarginM)
                return false;

            bool f00 = string.Equals(floor.floorId, "F00", StringComparison.Ordinal);
            Vector3 destination = f00 ? lastF00Checkpoint : floor.arrivalPosition;
            Quaternion rotation = f00 ? lastF00Rotation : Quaternion.Euler(floor.arrivalEuler);
            adaptiveRig.SetPositionSafely(destination, rotation);
            RecoveryCount++;
            LastRecoveryReason = "Fall recovery from " + floor.floorId + " to "
                + (f00 ? LastCheckpointName : floor.floorId + " emergency arrival anchor") + ".";
            return true;
        }

        public bool ForceRecoveryForGate(string floorId, out Vector3 destination)
        {
            destination = adaptiveRig == null ? Vector3.zero : adaptiveRig.transform.position;
            if (adaptiveRig == null || elevatorController == null
                || !string.Equals(elevatorController.ActiveFloorId, floorId, StringComparison.Ordinal))
                return false;
            if (!elevatorContract.ApprovedS2FloorContract.TryGetFloor(floorId,
                out S2FloorRecord floor))
                return false;
            Vector3 fallen = adaptiveRig.transform.position;
            fallen.y = floor.elevationM - fallMarginM - 1f;
            adaptiveRig.SetPositionSafely(fallen, adaptiveRig.transform.rotation);
            bool recovered = RecoverIfNeeded();
            destination = adaptiveRig.transform.position;
            return recovered;
        }
    }
}
