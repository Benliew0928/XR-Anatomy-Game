using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S3ElevatorButtonCommand
    {
        Floor,
        DoorOpen,
        DoorClose,
        LandingCall,
    }

    [DisallowMultipleComponent]
    public sealed class S3ElevatorButton : MonoBehaviour
    {
        [SerializeField] private S3ElevatorButtonCommand command;
        [SerializeField] private string floorId;
        [SerializeField] private S3DoorController doorController;
        [SerializeField] private S3ElevatorController elevatorController;

        public S3ElevatorButtonCommand Command => command;
        public string FloorId => floorId;
        public S3DoorController DoorController => doorController;
        public S3ElevatorController ElevatorController => elevatorController;
        public int ActivationCount { get; private set; }
        public string LastResult { get; private set; } = string.Empty;

        public void Configure(S3ElevatorButtonCommand buttonCommand, string targetFloorId, S3DoorController controller)
        {
            command = buttonCommand;
            floorId = targetFloorId ?? string.Empty;
            doorController = controller;
            elevatorController = null;
        }

        public void Configure(S3ElevatorButtonCommand buttonCommand, string targetFloorId,
            S3ElevatorController controller)
        {
            command = buttonCommand;
            floorId = targetFloorId ?? string.Empty;
            elevatorController = controller;
            doorController = null;
        }

        public bool Activate()
        {
            ActivationCount++;
            if (elevatorController != null)
            {
                bool routed;
                switch (command)
                {
                    case S3ElevatorButtonCommand.Floor:
                        routed = elevatorController.RequestFloor(floorId);
                        break;
                    case S3ElevatorButtonCommand.DoorOpen:
                        routed = elevatorController.RequestDoorOpen();
                        break;
                    case S3ElevatorButtonCommand.DoorClose:
                        routed = elevatorController.RequestDoorClose();
                        break;
                    case S3ElevatorButtonCommand.LandingCall:
                        routed = elevatorController.RequestLandingCall(floorId);
                        break;
                    default:
                        routed = false;
                        break;
                }
                LastResult = elevatorController.LastResult;
                return routed;
            }

            if (doorController == null)
            {
                LastResult = "Door controller unavailable.";
                return false;
            }

            bool accepted;
            switch (command)
            {
                case S3ElevatorButtonCommand.DoorOpen:
                    accepted = doorController.RequestOpen();
                    break;
                case S3ElevatorButtonCommand.DoorClose:
                    accepted = doorController.RequestClose();
                    break;
                case S3ElevatorButtonCommand.LandingCall:
                    accepted = string.Equals(floorId, doorController.CurrentFloorId, System.StringComparison.Ordinal)
                        && doorController.RequestOpen();
                    break;
                default:
                    accepted = false;
                    break;
            }
            LastResult = command == S3ElevatorButtonCommand.Floor
                ? "Floor travel is intentionally disabled until S3C."
                : accepted ? command + " accepted." : command + " rejected safely.";
            return accepted;
        }
    }
}
