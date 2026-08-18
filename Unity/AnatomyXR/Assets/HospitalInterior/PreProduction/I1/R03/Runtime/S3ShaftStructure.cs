using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3ShaftStructure : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract contract;
        [SerializeField] private BoxCollider leftWall;
        [SerializeField] private BoxCollider rightWall;
        [SerializeField] private BoxCollider rearWall;
        [SerializeField] private BoxCollider baseClosure;
        [SerializeField] private BoxCollider topClosure;
        [SerializeField] private BoxCollider[] landingSpandrels = Array.Empty<BoxCollider>();
        [SerializeField] private BoxCollider[] guideRails = Array.Empty<BoxCollider>();
        [SerializeField] private Transform travelCenterline;
        [SerializeField] private LineRenderer travelCenterlineRenderer;
        [SerializeField] private Renderer[] glassPanels = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] architecturalFrames = Array.Empty<Renderer>();

        public S3ElevatorContract Contract => contract;
        public BoxCollider LeftWall => leftWall;
        public BoxCollider RightWall => rightWall;
        public BoxCollider RearWall => rearWall;
        public BoxCollider BaseClosure => baseClosure;
        public BoxCollider TopClosure => topClosure;
        public BoxCollider[] LandingSpandrels => landingSpandrels;
        public BoxCollider[] GuideRails => guideRails;
        public Transform TravelCenterline => travelCenterline;
        public LineRenderer TravelCenterlineRenderer => travelCenterlineRenderer;
        public Renderer[] GlassPanels => glassPanels;
        public Renderer[] ArchitecturalFrames => architecturalFrames;

        public void Configure(S3ElevatorContract elevatorContract, BoxCollider left, BoxCollider right,
            BoxCollider rear, BoxCollider basePlate, BoxCollider topPlate, BoxCollider[] spandrels,
            BoxCollider[] rails, Transform centerline, LineRenderer centerlineRenderer,
            Renderer[] glazing, Renderer[] frames)
        {
            contract = elevatorContract;
            leftWall = left;
            rightWall = right;
            rearWall = rear;
            baseClosure = basePlate;
            topClosure = topPlate;
            landingSpandrels = spandrels ?? Array.Empty<BoxCollider>();
            guideRails = rails ?? Array.Empty<BoxCollider>();
            travelCenterline = centerline;
            travelCenterlineRenderer = centerlineRenderer;
            glassPanels = glazing ?? Array.Empty<Renderer>();
            architecturalFrames = frames ?? Array.Empty<Renderer>();
        }

        public bool ValidateStructure(out string detail)
        {
            if (contract == null || leftWall == null || rightWall == null || rearWall == null
                || baseClosure == null || topClosure == null || travelCenterline == null
                || travelCenterlineRenderer == null)
            {
                detail = "one or more shaft references are missing";
                return false;
            }

            float epsilon = 0.002f;
            bool rootIdentity = transform.localPosition.sqrMagnitude <= 0.000001f
                && Quaternion.Angle(transform.localRotation, Quaternion.identity) <= 0.001f
                && (transform.localScale - Vector3.one).sqrMagnitude <= 0.000001f;
            bool continuousWalls = Mathf.Abs(leftWall.bounds.max.x - contract.ShaftInnerMinXZ.x) <= epsilon
                && Mathf.Abs(rightWall.bounds.min.x - contract.ShaftInnerMaxXZ.x) <= epsilon
                && Mathf.Abs(rearWall.bounds.min.z - contract.ShaftInnerMaxXZ.y) <= epsilon
                && new[] { leftWall, rightWall, rearWall }.All(item =>
                    Mathf.Abs(item.bounds.min.y - contract.ShaftMinY) <= epsilon
                    && Mathf.Abs(item.bounds.max.y - contract.ShaftMaxY) <= epsilon);
            bool closures = Mathf.Abs(baseClosure.bounds.max.y - contract.ShaftMinY) <= epsilon
                && Mathf.Abs(topClosure.bounds.max.y - contract.ShaftMaxY) <= epsilon
                && Mathf.Abs(topClosure.bounds.min.y - (contract.ShaftMaxY - 0.10f)) <= epsilon;

            bool spandrels = landingSpandrels.Length == 7;
            if (spandrels)
            {
                IReadOnlyList<S2FloorRecord> floors = contract.ApprovedS2FloorContract.Floors;
                for (int i = 0; i < landingSpandrels.Length; i++)
                {
                    float expectedMin = floors[i].elevationM + contract.ShaftLandingEnvelopeHeightM;
                    float expectedMax = i + 1 < floors.Count ? floors[i + 1].elevationM : contract.ShaftMaxY;
                    BoxCollider spandrel = landingSpandrels[i];
                    spandrels &= spandrel != null
                        && Mathf.Abs(spandrel.bounds.min.y - expectedMin) <= epsilon
                        && Mathf.Abs(spandrel.bounds.max.y - expectedMax) <= epsilon
                        && Mathf.Abs(spandrel.bounds.max.z - contract.ShaftInnerMinXZ.y) <= epsilon;
                }
            }

            bool rails = guideRails.Length == 2 && guideRails.All(item => item != null
                && Mathf.Abs(item.bounds.size.x - contract.ShaftGuideRailSizeM.x) <= epsilon
                && Mathf.Abs(item.bounds.size.y - contract.ShaftGuideRailSizeM.y) <= epsilon
                && Mathf.Abs(item.bounds.size.z - contract.ShaftGuideRailSizeM.z) <= epsilon
                && Mathf.Abs(item.bounds.min.y - contract.ShaftMinY) <= epsilon
                && Mathf.Abs(item.bounds.max.y - contract.ShaftMaxY) <= epsilon);
            bool centerline = Vector3.Distance(travelCenterline.localPosition,
                    new Vector3(contract.CabinCenterXZ.x, contract.ShaftMinY, contract.CabinCenterXZ.y)) <= epsilon
                && !travelCenterlineRenderer.enabled;
            bool contractFit = contract.ValidateShaftFit(out string contractDetail);

            detail = $"wallsContinuous={continuousWalls}; spandrels={landingSpandrels.Length}; rails={guideRails.Length}; "
                + $"centerline=({travelCenterline.localPosition.x:F3},{travelCenterline.localPosition.z:F3}); {contractDetail}";
            return rootIdentity && continuousWalls && closures && spandrels && rails && centerline && contractFit;
        }

        public bool ValidateGlassDesign(out string detail)
        {
            bool panelCount = glassPanels.Length == 11 && glassPanels.All(item => item != null && item.enabled);
            bool materials = panelCount && glassPanels.All(item =>
            {
                Material material = item.sharedMaterial;
                return material != null && material.name.StartsWith("MAT_S3B_R03_ShaftGlass", StringComparison.Ordinal)
                    && material.shader != null
                    && material.shader.name == "Universal Render Pipeline/Lit"
                    && material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                    && (!material.HasProperty("_BaseColor")
                        || (material.GetColor("_BaseColor").a >= 0.10f
                            && material.GetColor("_BaseColor").a <= 0.24f))
                    && material.renderQueue >= 3000;
            });
            bool frames = architecturalFrames.Length == 42 && architecturalFrames.All(item => item != null
                && item.enabled && item.GetComponent<Collider>() == null
                && item.sharedMaterial != null
                && (item.sharedMaterial.name == "MAT_S3B_R03_ShaftFrame"
                    || item.sharedMaterial.name == "MAT_S3A_R03_RefinedBronze"));
            bool glassDoesNotCast = panelCount && glassPanels.All(item =>
                item.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off);
            string glassState = glassPanels.Length == 0 || glassPanels[0]?.sharedMaterial == null ? "missing"
                : $"name={glassPanels[0].sharedMaterial.name}; shader={glassPanels[0].sharedMaterial.shader?.name}; "
                    + $"alpha={(glassPanels[0].sharedMaterial.HasProperty("_BaseColor") ? glassPanels[0].sharedMaterial.GetColor("_BaseColor").a : -1f):F3}; "
                    + $"queue={glassPanels[0].sharedMaterial.renderQueue}; transparentKeyword={glassPanels[0].sharedMaterial.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")}";
            detail = $"glassPanels={glassPanels.Length}/11; frameMembers={architecturalFrames.Length}/42; "
                + $"transparentMaterials={materials}; nonCollidingFrames={frames}; glassShadowsOff={glassDoesNotCast}; {glassState}";
            return panelCount && materials && frames && glassDoesNotCast;
        }

        public bool ValidateCabinSweep(out string detail)
        {
            if (contract == null)
            {
                detail = "shaft contract missing";
                return false;
            }

            float cabinMinX = contract.CabinCenterXZ.x - contract.CabinOuterSizeM.x * 0.5f;
            float cabinMaxX = contract.CabinCenterXZ.x + contract.CabinOuterSizeM.x * 0.5f;
            float cabinMinZ = contract.CabinCenterXZ.y - contract.CabinOuterSizeM.z * 0.5f;
            float cabinMaxZ = contract.CabinCenterXZ.y + contract.CabinOuterSizeM.z * 0.5f;
            float wallClearance = Mathf.Min(cabinMinX - contract.ShaftInnerMinXZ.x,
                contract.ShaftInnerMaxXZ.x - cabinMaxX,
                cabinMinZ - contract.ShaftInnerMinXZ.y,
                contract.ShaftInnerMaxXZ.y - cabinMaxZ);
            float railFront = guideRails.Length == 0 ? contract.ShaftInnerMaxXZ.y
                : guideRails.Min(item => item.bounds.min.z);
            float railClearance = railFront - cabinMaxZ;
            float lastDatum = contract.ApprovedS2FloorContract.Floors.Last().elevationM;
            float finalCabinTop = lastDatum + contract.ModelRootVerticalOffsetM + contract.CabinOuterSizeM.y;
            float topClearance = topClosure.bounds.min.y - finalCabinTop;
            float firstCabinBottom = contract.ModelRootVerticalOffsetM;
            float baseClearance = firstCabinBottom - baseClosure.bounds.max.y;
            bool clear = wallClearance >= contract.MinimumShaftRunningClearanceM
                && railClearance >= 0.03f && topClearance >= 0.90f && baseClearance >= 0.08f;
            detail = $"wall={wallClearance:F3}; guideRail={railClearance:F3}; base={baseClearance:F3}; F06Top={topClearance:F3}";
            return clear;
        }
    }
}
