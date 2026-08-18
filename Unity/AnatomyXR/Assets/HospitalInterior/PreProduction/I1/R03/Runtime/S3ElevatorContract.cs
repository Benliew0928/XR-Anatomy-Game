using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S3ElevatorState
    {
        ReadyOpen,
        ReadyClosed,
        DoorOpening,
        DoorClosing,
        Moving,
        Recovering,
        FaultedSafe,
    }

    [CreateAssetMenu(menuName = "Hospital Interior/R03/S3 Elevator Contract")]
    public sealed class S3ElevatorContract : ScriptableObject
    {
        public const string Revision = "R03";
        public const string Stage = "S3";
        public const string ElevatorId = "E01";

        [SerializeField] private S1DatumContract approvedS1DatumContract;
        [SerializeField] private S2FloorContract approvedS2FloorContract;
        [SerializeField] private Vector2 approvedCoreMinXZ = new Vector2(0f, 3f);
        [SerializeField] private Vector2 approvedCoreMaxXZ = new Vector2(6f, 5.7f);
        [SerializeField] private Vector2 e01OperatingCellMinXZ = new Vector2(0.3f, 3f);
        [SerializeField] private Vector2 e01OperatingCellMaxXZ = new Vector2(3f, 5.7f);
        [SerializeField] private Vector2 cabinCenterXZ = new Vector2(1.65f, 4.35f);
        [SerializeField] private Vector3 cabinOuterSizeM = new Vector3(2.35f, 2.60f, 2.45f);
        [SerializeField] private Vector2 cabinClearFloorM = new Vector2(2.10f, 2.20f);
        [SerializeField] private Vector2 clearDoorM = new Vector2(1.20f, 2.20f);
        [SerializeField] private Vector2 shaftInnerMinXZ = new Vector2(0.40f, 3.00f);
        [SerializeField] private Vector2 shaftInnerMaxXZ = new Vector2(2.90f, 5.65f);
        [SerializeField] private float shaftSideWallThicknessM = 0.10f;
        [SerializeField] private float shaftRearWallThicknessM = 0.05f;
        [SerializeField] private float shaftFrontWallDepthM = 0.10f;
        [SerializeField] private float shaftMinY = -0.20f;
        [SerializeField] private float shaftMaxY = 28.83f;
        [SerializeField] private float shaftLandingEnvelopeHeightM = 2.60f;
        [SerializeField] private Vector3 shaftGuideRailSizeM = new Vector3(0.045f, 29.03f, 0.025f);
        [SerializeField] private float minimumShaftRunningClearanceM = 0.07f;
        [SerializeField] private float modelFloorTopM = 0.11f;
        [SerializeField] private float modelRootVerticalOffsetM = -0.11f;
        [SerializeField] private float doorLeafWidthM = 0.595f;
        [SerializeField] private float doorLeafDepthM = 0.075f;
        [SerializeField] private float doorTravelM = 0.5975f;
        [SerializeField] private float doorOpenSeconds = 1.2f;
        [SerializeField] private float doorDwellSeconds = 4f;
        [SerializeField] private float doorCloseSeconds = 1.2f;
        [SerializeField] private float doorPositionToleranceM = 0.002f;
        [SerializeField] private float landingAlignmentToleranceM = 0.015f;
        [SerializeField] private float travelSecondsPerMeter = 0.65f;
        [SerializeField] private float sceneLoadTimeoutSeconds = 20f;
        [SerializeField] private float recoveryTimeoutSeconds = 10f;

        public S1DatumContract ApprovedS1DatumContract => approvedS1DatumContract;
        public S2FloorContract ApprovedS2FloorContract => approvedS2FloorContract;
        public Vector2 ApprovedCoreMinXZ => approvedCoreMinXZ;
        public Vector2 ApprovedCoreMaxXZ => approvedCoreMaxXZ;
        public Vector2 E01OperatingCellMinXZ => e01OperatingCellMinXZ;
        public Vector2 E01OperatingCellMaxXZ => e01OperatingCellMaxXZ;
        public Vector2 CabinCenterXZ => cabinCenterXZ;
        public Vector3 CabinOuterSizeM => cabinOuterSizeM;
        public Vector2 CabinClearFloorM => cabinClearFloorM;
        public Vector2 ClearDoorM => clearDoorM;
        public Vector2 ShaftInnerMinXZ => shaftInnerMinXZ;
        public Vector2 ShaftInnerMaxXZ => shaftInnerMaxXZ;
        public float ShaftSideWallThicknessM => shaftSideWallThicknessM;
        public float ShaftRearWallThicknessM => shaftRearWallThicknessM;
        public float ShaftFrontWallDepthM => shaftFrontWallDepthM;
        public float ShaftMinY => shaftMinY;
        public float ShaftMaxY => shaftMaxY;
        public float ShaftLandingEnvelopeHeightM => shaftLandingEnvelopeHeightM;
        public Vector3 ShaftGuideRailSizeM => shaftGuideRailSizeM;
        public float MinimumShaftRunningClearanceM => minimumShaftRunningClearanceM;
        public float ModelFloorTopM => modelFloorTopM;
        public float ModelRootVerticalOffsetM => modelRootVerticalOffsetM;
        public float DoorLeafWidthM => doorLeafWidthM;
        public float DoorLeafDepthM => doorLeafDepthM;
        public float DoorTravelM => doorTravelM;
        public float DoorOpenSeconds => doorOpenSeconds;
        public float DoorDwellSeconds => doorDwellSeconds;
        public float DoorCloseSeconds => doorCloseSeconds;
        public float DoorPositionToleranceM => doorPositionToleranceM;
        public float LandingAlignmentToleranceM => landingAlignmentToleranceM;
        public float TravelSecondsPerMeter => travelSecondsPerMeter;
        public float SceneLoadTimeoutSeconds => sceneLoadTimeoutSeconds;
        public float RecoveryTimeoutSeconds => recoveryTimeoutSeconds;

        public void Configure(S1DatumContract datumContract, S2FloorContract floorContract)
        {
            approvedS1DatumContract = datumContract;
            approvedS2FloorContract = floorContract;
            approvedCoreMinXZ = new Vector2(0f, 3f);
            approvedCoreMaxXZ = new Vector2(6f, 5.7f);
            e01OperatingCellMinXZ = new Vector2(0.3f, 3f);
            e01OperatingCellMaxXZ = new Vector2(3f, 5.7f);
            cabinCenterXZ = new Vector2(1.65f, 4.35f);
            cabinOuterSizeM = new Vector3(2.35f, 2.60f, 2.45f);
            cabinClearFloorM = new Vector2(2.10f, 2.20f);
            clearDoorM = new Vector2(1.20f, 2.20f);
            shaftInnerMinXZ = new Vector2(0.40f, 3.00f);
            shaftInnerMaxXZ = new Vector2(2.90f, 5.65f);
            shaftSideWallThicknessM = 0.10f;
            shaftRearWallThicknessM = 0.05f;
            shaftFrontWallDepthM = 0.10f;
            shaftMinY = -0.20f;
            shaftMaxY = datumContract?.ElevatorReference?.maxY ?? 28.83f;
            shaftLandingEnvelopeHeightM = 2.60f;
            shaftGuideRailSizeM = new Vector3(0.045f, shaftMaxY - shaftMinY, 0.025f);
            minimumShaftRunningClearanceM = 0.07f;
            modelFloorTopM = 0.11f;
            modelRootVerticalOffsetM = -modelFloorTopM;
            doorLeafWidthM = 0.595f;
            doorLeafDepthM = 0.075f;
            doorTravelM = 0.5975f;
            doorOpenSeconds = 1.2f;
            doorDwellSeconds = 4f;
            doorCloseSeconds = 1.2f;
            doorPositionToleranceM = 0.002f;
            landingAlignmentToleranceM = 0.015f;
            travelSecondsPerMeter = 0.65f;
            sceneLoadTimeoutSeconds = 20f;
            recoveryTimeoutSeconds = 10f;
        }

        public bool ValidateShaftFit(out string detail)
        {
            float cabinMinX = cabinCenterXZ.x - cabinOuterSizeM.x * 0.5f;
            float cabinMaxX = cabinCenterXZ.x + cabinOuterSizeM.x * 0.5f;
            float cabinMinZ = cabinCenterXZ.y - cabinOuterSizeM.z * 0.5f;
            float cabinMaxZ = cabinCenterXZ.y + cabinOuterSizeM.z * 0.5f;
            float left = cabinMinX - shaftInnerMinXZ.x;
            float right = shaftInnerMaxXZ.x - cabinMaxX;
            float front = cabinMinZ - shaftInnerMinXZ.y;
            float rear = shaftInnerMaxXZ.y - cabinMaxZ;
            float firstCabinBottom = modelRootVerticalOffsetM;
            float lastDatum = approvedS2FloorContract == null || approvedS2FloorContract.Floors.Count == 0
                ? 0f : approvedS2FloorContract.Floors[approvedS2FloorContract.Floors.Count - 1].elevationM;
            float lastCabinTop = lastDatum + modelRootVerticalOffsetM + cabinOuterSizeM.y;
            float baseClearance = firstCabinBottom - shaftMinY;
            float topOverrun = shaftMaxY - lastCabinTop;

            S1ElevatorReferenceRecord authority = approvedS1DatumContract?.ElevatorReference;
            bool authorityMatches = authority != null
                && Approximately(authority.minX, approvedCoreMinXZ.x)
                && Approximately(authority.maxX, approvedCoreMaxXZ.x)
                && Approximately(authority.minZ, approvedCoreMinXZ.y)
                && Approximately(authority.maxZ, approvedCoreMaxXZ.y)
                && Approximately(authority.maxY, shaftMaxY);
            bool horizontal = Mathf.Min(left, right, front, rear) >= minimumShaftRunningClearanceM
                && shaftInnerMinXZ.x - shaftSideWallThicknessM >= e01OperatingCellMinXZ.x - 0.001f
                && shaftInnerMaxXZ.x + shaftSideWallThicknessM <= e01OperatingCellMaxXZ.x + 0.001f
                && shaftInnerMaxXZ.y + shaftRearWallThicknessM <= e01OperatingCellMaxXZ.y + 0.001f
                && shaftInnerMinXZ.y - shaftFrontWallDepthM >= 2.90f - 0.001f;
            bool vertical = approvedS2FloorContract != null && approvedS2FloorContract.Floors.Count == 7
                && shaftMinY <= firstCabinBottom - 0.08f && topOverrun >= 0.90f
                && shaftLandingEnvelopeHeightM >= 2.60f;
            bool guideRail = Approximately(shaftGuideRailSizeM.x, 0.045f)
                && Approximately(shaftGuideRailSizeM.y, shaftMaxY - shaftMinY)
                && Approximately(shaftGuideRailSizeM.z, 0.025f);

            detail = $"shaftInnerX={shaftInnerMinXZ.x:F3}..{shaftInnerMaxXZ.x:F3}; shaftInnerZ={shaftInnerMinXZ.y:F3}..{shaftInnerMaxXZ.y:F3}; "
                + $"runningClearanceL/R/F/R={left:F3}/{right:F3}/{front:F3}/{rear:F3}; "
                + $"vertical={shaftMinY:F3}..{shaftMaxY:F3}; baseClearance={baseClearance:F3}; F06Overrun={topOverrun:F3}";
            return authorityMatches && horizontal && vertical && guideRail;
        }

        public bool ValidateSpatialFit(out string detail)
        {
            float cabinMinX = cabinCenterXZ.x - cabinOuterSizeM.x * 0.5f;
            float cabinMaxX = cabinCenterXZ.x + cabinOuterSizeM.x * 0.5f;
            float cabinMinZ = cabinCenterXZ.y - cabinOuterSizeM.z * 0.5f;
            float cabinMaxZ = cabinCenterXZ.y + cabinOuterSizeM.z * 0.5f;
            float leftClearance = cabinMinX - e01OperatingCellMinXZ.x;
            float rightClearance = e01OperatingCellMaxXZ.x - cabinMaxX;
            float frontClearance = cabinMinZ - e01OperatingCellMinXZ.y;
            float rearClearance = e01OperatingCellMaxXZ.y - cabinMaxZ;

            bool dimensions = Approximately(cabinOuterSizeM.x, 2.35f)
                && Approximately(cabinOuterSizeM.y, 2.60f)
                && Approximately(cabinOuterSizeM.z, 2.45f)
                && Approximately(cabinClearFloorM.x, 2.10f)
                && Approximately(cabinClearFloorM.y, 2.20f)
                && Approximately(clearDoorM.x, 1.20f)
                && Approximately(clearDoorM.y, 2.20f)
                && Approximately(modelFloorTopM + modelRootVerticalOffsetM, 0f);
            bool core = cabinMinX >= e01OperatingCellMinXZ.x && cabinMaxX <= e01OperatingCellMaxXZ.x
                && cabinMinZ >= e01OperatingCellMinXZ.y && cabinMaxZ <= e01OperatingCellMaxXZ.y
                && e01OperatingCellMinXZ.x >= approvedCoreMinXZ.x && e01OperatingCellMaxXZ.x <= approvedCoreMaxXZ.x
                && e01OperatingCellMinXZ.y >= approvedCoreMinXZ.y && e01OperatingCellMaxXZ.y <= approvedCoreMaxXZ.y
                && Mathf.Min(leftClearance, rightClearance, frontClearance, rearClearance) >= 0.12f;
            bool floors = approvedS2FloorContract != null && approvedS2FloorContract.Floors.Count == 7
                && approvedS2FloorContract.Floors.Select(item => item.floorId)
                    .SequenceEqual(new[] { "F00", "F01", "F02", "F03", "F04", "F05", "F06" })
                && MinimumStoreyHeight() >= cabinOuterSizeM.y + 1.29f;

            detail = $"cabinX={cabinMinX:F3}..{cabinMaxX:F3}; cabinZ={cabinMinZ:F3}..{cabinMaxZ:F3}; "
                + $"clearanceL/R/F/R={leftClearance:F3}/{rightClearance:F3}/{frontClearance:F3}/{rearClearance:F3}; "
                + $"minimumStorey={MinimumStoreyHeight():F3}; floorDatum={modelFloorTopM + modelRootVerticalOffsetM:F3}";
            return dimensions && core && floors;
        }

        public float MinimumStoreyHeight()
        {
            if (approvedS2FloorContract == null || approvedS2FloorContract.Floors.Count < 2)
                return 0f;
            IReadOnlyList<S2FloorRecord> floors = approvedS2FloorContract.Floors;
            float minimum = float.MaxValue;
            for (int i = 1; i < floors.Count; i++)
                minimum = Mathf.Min(minimum, floors[i].elevationM - floors[i - 1].elevationM);
            return minimum;
        }

        private static bool Approximately(float actual, float expected) => Mathf.Abs(actual - expected) <= 0.001f;
    }
}
