using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S5AElevatorInteraction : MonoBehaviour, IS5AInteractable
    {
        [SerializeField] private S3ElevatorButtonCommand command;
        [SerializeField] private string floorId;
        [SerializeField] private S5AVerticalCirculationController circulation;

        public S3ElevatorButtonCommand Command => command;
        public string FloorId => floorId;
        public string InteractionLabel => command + (string.IsNullOrEmpty(floorId) ? string.Empty : " " + floorId);
        public string LastInteractionResult { get; private set; } = string.Empty;
        public int ActivationCount { get; private set; }

        public void Configure(S3ElevatorButtonCommand buttonCommand, string targetFloor,
            S5AVerticalCirculationController owner)
        {
            command = buttonCommand;
            floorId = targetFloor ?? string.Empty;
            circulation = owner;
        }

        public bool ActivateInteraction()
        {
            ActivationCount++;
            if (circulation == null)
            {
                LastInteractionResult = "S5A circulation unavailable.";
                return false;
            }
            bool accepted;
            switch (command)
            {
                case S3ElevatorButtonCommand.Floor:
                    accepted = circulation.RequestElevatorFloor(floorId);
                    break;
                case S3ElevatorButtonCommand.LandingCall:
                    accepted = circulation.RequestLandingCall(floorId);
                    break;
                case S3ElevatorButtonCommand.DoorOpen:
                    accepted = circulation.RequestDoorOpen();
                    break;
                case S3ElevatorButtonCommand.DoorClose:
                    accepted = circulation.RequestDoorClose();
                    break;
                default:
                    accepted = false;
                    break;
            }
            LastInteractionResult = circulation.LastResult;
            return accepted;
        }
    }
}
