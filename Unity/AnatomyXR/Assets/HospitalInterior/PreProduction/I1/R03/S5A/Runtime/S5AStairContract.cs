using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S5AStairIntervalRecord
    {
        public string fromFloor;
        public string toFloor;
        public float heightM;
        public int risersPerFlight;
        public int totalRisers;
        public float riseM;
        public float goingM;
    }

    [CreateAssetMenu(menuName = "Hospital Interior/R03/S5A Stair Contract")]
    public sealed class S5AStairContract : ScriptableObject
    {
        public const string Revision = "R03";
        public const string Stage = "S5A";
        public const string StairId = "STAIR_A";
        public const string S4ApprovalSha256 = "2388300aa24207f57ab5ba5a7a9077e319c5cef5372f720b6b70774c9c3b80e0";
        public const string S4PlanContractSha256 = "d6ca47e14de243b424453fc69732b5179b76ad63191b51c3ea4b992105ac3145";

        [SerializeField] private S2FloorContract approvedFloorContract;
        [SerializeField] private Vector2 boundsMinXZ = new Vector2(-29.0f, -5.0f);
        [SerializeField] private Vector2 boundsMaxXZ = new Vector2(-22.0f, 5.0f);
        [SerializeField] private float baseY = -0.20f;
        [SerializeField] private float topY = 28.83f;
        [SerializeField] private Vector2 laneCentresX = new Vector2(-24.18f, -26.82f);
        [SerializeField] private float laneWidthM = 1.80f;
        [SerializeField] private float minimumUsableWidthM = 1.45f;
        [SerializeField] private Vector2 floorLandingZ = new Vector2(-4.96f, -2.56f);
        [SerializeField] private Vector2 intermediateLandingZ = new Vector2(2.56f, 4.96f);
        [SerializeField] private float flightRunM = 5.12f;
        [SerializeField] private float doorCentreZ = -3.76f;
        [SerializeField] private Vector2 doorClearM = new Vector2(1.20f, 2.25f);
        [SerializeField] private float doorSlideM = 1.28f;
        [SerializeField] private float minimumHeadroomM = 2.20f;
        [SerializeField] private float handrailHeightM = 0.95f;
        [SerializeField] private float guardHeightM = 1.10f;
        [SerializeField] private S5AStairIntervalRecord[] intervals = Array.Empty<S5AStairIntervalRecord>();
        [SerializeField] private Mesh f00DualApertureSlab;
        [SerializeField] private Mesh upperDualApertureSlab;
        [SerializeField] private Mesh upperDualApertureUnderside;
        [SerializeField] private Mesh exteriorDualApertureStructure;

        public S2FloorContract ApprovedFloorContract => approvedFloorContract;
        public Vector2 BoundsMinXZ => boundsMinXZ;
        public Vector2 BoundsMaxXZ => boundsMaxXZ;
        public float BaseY => baseY;
        public float TopY => topY;
        public Vector2 LaneCentresX => laneCentresX;
        public float LaneWidthM => laneWidthM;
        public float MinimumUsableWidthM => minimumUsableWidthM;
        public Vector2 FloorLandingZ => floorLandingZ;
        public Vector2 IntermediateLandingZ => intermediateLandingZ;
        public float FlightRunM => flightRunM;
        public float DoorCentreZ => doorCentreZ;
        public Vector2 DoorClearM => doorClearM;
        public float DoorSlideM => doorSlideM;
        public float MinimumHeadroomM => minimumHeadroomM;
        public float HandrailHeightM => handrailHeightM;
        public float GuardHeightM => guardHeightM;
        public IReadOnlyList<S5AStairIntervalRecord> Intervals => intervals;
        public Mesh F00DualApertureSlab => f00DualApertureSlab;
        public Mesh UpperDualApertureSlab => upperDualApertureSlab;
        public Mesh UpperDualApertureUnderside => upperDualApertureUnderside;
        public Mesh ExteriorDualApertureStructure => exteriorDualApertureStructure;

        public void Configure(S2FloorContract floorContract, Mesh f00, Mesh upper,
            Mesh underside, Mesh exterior)
        {
            approvedFloorContract = floorContract;
            boundsMinXZ = new Vector2(-29.0f, -5.0f);
            boundsMaxXZ = new Vector2(-22.0f, 5.0f);
            baseY = -0.20f;
            topY = 28.83f;
            laneCentresX = new Vector2(-24.18f, -26.82f);
            laneWidthM = 1.80f;
            minimumUsableWidthM = 1.45f;
            floorLandingZ = new Vector2(-4.96f, -2.56f);
            intermediateLandingZ = new Vector2(2.56f, 4.96f);
            flightRunM = 5.12f;
            doorCentreZ = -3.76f;
            doorClearM = new Vector2(1.20f, 2.25f);
            doorSlideM = 1.28f;
            minimumHeadroomM = 2.20f;
            handrailHeightM = 0.95f;
            guardHeightM = 1.10f;
            f00DualApertureSlab = f00;
            upperDualApertureSlab = upper;
            upperDualApertureUnderside = underside;
            exteriorDualApertureStructure = exterior;

            var built = new List<S5AStairIntervalRecord>();
            if (floorContract != null)
            {
                for (int index = 0; index < floorContract.Floors.Count - 1; index++)
                {
                    S2FloorRecord lower = floorContract.Floors[index];
                    S2FloorRecord upperFloor = floorContract.Floors[index + 1];
                    int perFlight = index == 0 ? 17 : 13;
                    float height = upperFloor.elevationM - lower.elevationM;
                    built.Add(new S5AStairIntervalRecord
                    {
                        fromFloor = lower.floorId,
                        toFloor = upperFloor.floorId,
                        heightM = height,
                        risersPerFlight = perFlight,
                        totalRisers = perFlight * 2,
                        riseM = height / (perFlight * 2f),
                        goingM = flightRunM / perFlight,
                    });
                }
            }
            intervals = built.ToArray();
        }

        public bool Validate(out string detail)
        {
            int steps = intervals.Sum(item => item.totalRisers);
            bool datums = approvedFloorContract != null && approvedFloorContract.Floors.Count == 7
                && Mathf.Abs(approvedFloorContract.Floors[0].elevationM) <= 0.0001f
                && Mathf.Abs(approvedFloorContract.Floors[6].elevationM - 25.3f) <= 0.0001f;
            bool measured = boundsMinXZ == new Vector2(-29.0f, -5.0f)
                && boundsMaxXZ == new Vector2(-22.0f, 5.0f)
                && Mathf.Abs(topY - 28.83f) <= 0.0001f
                && intervals.Length == 6 && steps == 164
                && Mathf.Abs(laneWidthM - 1.80f) <= 0.0001f
                && laneWidthM >= minimumUsableWidthM
                && Mathf.Abs(doorCentreZ + 3.76f) <= 0.0001f
                && doorClearM == new Vector2(1.20f, 2.25f)
                && minimumHeadroomM >= 2.20f && handrailHeightM >= 0.95f
                && guardHeightM >= 1.10f;
            bool assets = f00DualApertureSlab != null && upperDualApertureSlab != null
                && upperDualApertureUnderside != null && exteriorDualApertureStructure != null;
            detail = $"datums={datums}; intervals={intervals.Length}; flights={intervals.Length * 2}; "
                + $"steps={steps}; bounds={boundsMinXZ}..{boundsMaxXZ}; assets={assets}";
            return datums && measured && assets;
        }

        public bool TryGetFloor(string floorId, out S2FloorRecord floor)
        {
            floor = null;
            return approvedFloorContract != null
                && approvedFloorContract.TryGetFloor(floorId, out floor);
        }
    }
}
