using System;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3CabinObservationDesign : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract contract;
        [SerializeField] private Renderer[] observationGlassWalls = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] lowerProtectionPanels = Array.Empty<Renderer>();
        [SerializeField] private Renderer[] observationFrames = Array.Empty<Renderer>();

        public S3ElevatorContract Contract => contract;
        public Renderer[] ObservationGlassWalls => observationGlassWalls;
        public Renderer[] LowerProtectionPanels => lowerProtectionPanels;
        public Renderer[] ObservationFrames => observationFrames;

        public void Configure(S3ElevatorContract elevatorContract, Renderer[] glassWalls,
            Renderer[] protectionPanels, Renderer[] frames)
        {
            contract = elevatorContract;
            observationGlassWalls = glassWalls ?? Array.Empty<Renderer>();
            lowerProtectionPanels = protectionPanels ?? Array.Empty<Renderer>();
            observationFrames = frames ?? Array.Empty<Renderer>();
        }

        public bool ValidateObservationDesign(out string detail)
        {
            bool glassCount = observationGlassWalls.Length == 3
                && observationGlassWalls.All(item => item != null && item.enabled);
            bool transparent = glassCount && observationGlassWalls.All(item =>
            {
                Material material = item.sharedMaterial;
                return material != null
                    && material.name.StartsWith("MAT_S3B_R03_CabinObservationGlass", StringComparison.Ordinal)
                    && material.shader != null && material.shader.name == "Universal Render Pipeline/Lit"
                    && material.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")
                    && material.renderQueue >= 3000
                    && (!material.HasProperty("_BaseColor")
                        || (material.GetColor("_BaseColor").a >= 0.07f
                            && material.GetColor("_BaseColor").a <= 0.16f));
            });
            bool protection = lowerProtectionPanels.Length == 3
                && lowerProtectionPanels.All(item => item != null && item.enabled
                    && item.sharedMaterial != null
                    && item.sharedMaterial.name == "MAT_S3A_R03_BrushedStainless");
            bool frames = observationFrames.Length == 10 && observationFrames.All(item => item != null
                && item.enabled && item.GetComponent<Collider>() == null
                && item.sharedMaterial != null
                && (item.sharedMaterial.name == "MAT_S3B_R03_ShaftFrame"
                    || item.sharedMaterial.name == "MAT_S3A_R03_RefinedBronze"));
            bool glassShadowsOff = glassCount && observationGlassWalls.All(item =>
                item.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.Off);
            bool safetyBoundary = glassCount && observationGlassWalls.All(item =>
                item.GetComponent<BoxCollider>() is BoxCollider collider && !collider.isTrigger);
            float top = glassCount ? observationGlassWalls.Min(item => item.bounds.max.y) : float.MinValue;
            float protectionTop = protection ? lowerProtectionPanels.Max(item => item.bounds.max.y) : float.MaxValue;
            float clearViewHeight = top - protectionTop;
            bool sightline = clearViewHeight >= 1.45f && top >= 2.38f && protectionTop <= 1.00f;
            string materialState = glassCount && observationGlassWalls[0].sharedMaterial != null
                ? $"alpha={(observationGlassWalls[0].sharedMaterial.HasProperty("_BaseColor") ? observationGlassWalls[0].sharedMaterial.GetColor("_BaseColor").a : -1f):F3}; queue={observationGlassWalls[0].sharedMaterial.renderQueue}"
                : "material=missing";
            detail = $"glassWalls={observationGlassWalls.Length}/3; protectionPanels={lowerProtectionPanels.Length}/3; "
                + $"frames={observationFrames.Length}/10; transparent={transparent}; protected={protection}; "
                + $"nonCollidingFrames={frames}; safetyBoundary={safetyBoundary}; glassShadowsOff={glassShadowsOff}; "
                + $"sightline={sightline}; glassTop={top:F3}; protectionTop={protectionTop:F3}; "
                + $"clearViewHeight={clearViewHeight:F3}; {materialState}";
            return contract != null && glassCount && transparent && protection && frames
                && safetyBoundary && glassShadowsOff && sightline;
        }

        public bool ValidateNestedInShaft(S3ShaftStructure shaft, out string detail)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true)
                .Where(item => item.enabled && item != shaft?.TravelCenterlineRenderer).ToArray();
            if (contract == null || shaft == null || renderers.Length == 0)
            {
                detail = "cabin, shaft, or renderers unavailable";
                return false;
            }
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            float left = bounds.min.x - contract.ShaftInnerMinXZ.x;
            float right = contract.ShaftInnerMaxXZ.x - bounds.max.x;
            float front = bounds.min.z - contract.ShaftInnerMinXZ.y;
            float rear = contract.ShaftInnerMaxXZ.y - bounds.max.z;
            float minimum = Mathf.Min(left, right, front, rear);
            bool nested = minimum >= contract.MinimumShaftRunningClearanceM
                && bounds.min.x >= contract.ShaftInnerMinXZ.x
                && bounds.max.x <= contract.ShaftInnerMaxXZ.x
                && bounds.min.z >= contract.ShaftInnerMinXZ.y
                && bounds.max.z <= contract.ShaftInnerMaxXZ.y;
            detail = $"cabinBoundsX={bounds.min.x:F3}..{bounds.max.x:F3}; cabinBoundsZ={bounds.min.z:F3}..{bounds.max.z:F3}; "
                + $"innerShaftX={contract.ShaftInnerMinXZ.x:F3}..{contract.ShaftInnerMaxXZ.x:F3}; "
                + $"innerShaftZ={contract.ShaftInnerMinXZ.y:F3}..{contract.ShaftInnerMaxXZ.y:F3}; "
                + $"clearanceL/R/F/R={left:F3}/{right:F3}/{front:F3}/{rear:F3}";
            return nested;
        }
    }
}
