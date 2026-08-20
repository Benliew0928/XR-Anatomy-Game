using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S5ARecoveryCoordinator : MonoBehaviour
    {
        [SerializeField] private S3DAdaptiveRig adaptiveRig;
        [SerializeField] private S5AFloorSceneCoordinator floorCoordinator;
        [SerializeField] private S5AStairContract contract;
        [SerializeField] private Vector3 latestSafePosition;
        [SerializeField] private Quaternion latestSafeRotation = Quaternion.identity;
        [SerializeField] private string latestSafeLabel = "F00";

        private bool environmentReady;

        public Vector3 LatestSafePosition => latestSafePosition;
        public string LatestSafeLabel => latestSafeLabel;
        public int RecoveryCount { get; private set; }

        public void Configure(S3DAdaptiveRig rig, S5AFloorSceneCoordinator coordinator,
            S5AStairContract stairContract)
        {
            adaptiveRig = rig;
            floorCoordinator = coordinator;
            contract = stairContract;
            latestSafePosition = rig == null ? Vector3.zero : rig.transform.position;
            latestSafeRotation = rig == null ? Quaternion.identity : rig.transform.rotation;
            latestSafeLabel = "gate";
        }

        public void NotifyEnvironmentReady() => environmentReady = true;

        private void Update()
        {
            if (!environmentReady || adaptiveRig == null || floorCoordinator == null)
                return;
            Vector3 point = adaptiveRig.transform.position;
            if (point.y < -2f)
            {
                RecoverNow();
                return;
            }
            if (IsSafeLanding(point, out string label))
            {
                latestSafePosition = point;
                latestSafeRotation = adaptiveRig.transform.rotation;
                latestSafeLabel = label;
            }
        }

        public void SetSafePointForGate(Vector3 point, string label)
        {
            latestSafePosition = point;
            latestSafeRotation = Quaternion.identity;
            latestSafeLabel = label;
        }

        public bool RecoverNow()
        {
            if (adaptiveRig == null)
                return false;
            adaptiveRig.SetPositionSafely(latestSafePosition, latestSafeRotation);
            RecoveryCount++;
            Physics.SyncTransforms();
            return true;
        }

        private bool IsSafeLanding(Vector3 point, out string label)
        {
            label = string.Empty;
            bool insideCore = point.x >= contract.BoundsMinXZ.x && point.x <= contract.BoundsMaxXZ.x
                && point.z >= contract.BoundsMinXZ.y && point.z <= contract.BoundsMaxXZ.y;
            if (insideCore)
            {
                foreach (S2FloorRecord floor in contract.ApprovedFloorContract.Floors)
                {
                    if (Mathf.Abs(point.y - floor.elevationM) <= 0.25f
                        && point.z >= contract.FloorLandingZ.x - 0.05f
                        && point.z <= contract.FloorLandingZ.y + 0.05f)
                    {
                        label = floor.floorId;
                        return true;
                    }
                }
                float[] midpoints = contract.Intervals.Select((item, index) =>
                    (contract.ApprovedFloorContract.Floors[index].elevationM
                    + contract.ApprovedFloorContract.Floors[index + 1].elevationM) * 0.5f).ToArray();
                for (int index = 0; index < midpoints.Length; index++)
                {
                    if (Mathf.Abs(point.y - midpoints[index]) <= 0.25f
                        && point.z >= contract.IntermediateLandingZ.x - 0.05f)
                    {
                        label = "MID_" + index;
                        return true;
                    }
                }
            }
            if (point.y >= -0.15f && point.y <= 0.35f)
            {
                label = floorCoordinator.ActiveFloorId;
                return true;
            }
            return false;
        }
    }
}
