using System;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DFacadeFloorEdgeCoordinator : MonoBehaviour
    {
        public const string FinishPrefix = "S3D_R03_MainGlassFloorEdgeFinish_";
        public const float MinX = -23.95f;
        public const float MaxX = 18.80f;
        public const float FacadeEdgeZ = -17.30f;
        public const float FloorJoinZ = -15.48f;
        public const float DeckThicknessM = 0.20f;

        [SerializeField] private Material approvedFloorMaterial;
        [SerializeField] private Renderer[] finishes = Array.Empty<Renderer>();
        [SerializeField] private BoxCollider[] supportColliders = Array.Empty<BoxCollider>();

        private int lastFloorMarkerInstanceId;

        public string LastRemappedFloorId { get; private set; } = string.Empty;
        public string LastBoundaryDetail { get; private set; } = "waiting for an upper floor";

        public Material ApprovedFloorMaterial => approvedFloorMaterial;
        public Renderer[] Finishes => finishes;
        public BoxCollider[] SupportColliders => supportColliders;

        public void Configure(Material floorMaterial, Renderer[] floorEdgeFinishes,
            BoxCollider[] colliders)
        {
            approvedFloorMaterial = floorMaterial;
            finishes = floorEdgeFinishes ?? Array.Empty<Renderer>();
            supportColliders = colliders ?? Array.Empty<BoxCollider>();
        }

        public bool ValidateConfiguration(out string detail)
        {
            bool renderersValid = approvedFloorMaterial != null && finishes.Length == 6
                && finishes.All(item => item != null && item.enabled
                    && item.name.StartsWith(FinishPrefix, StringComparison.Ordinal)
                    && item.sharedMaterial == approvedFloorMaterial);
            bool collidersValid = supportColliders.Length == 6
                && supportColliders.All(item => item != null && item.enabled && !item.isTrigger);
            detail = $"finishes={finishes.Length}; colliders={supportColliders.Length}; "
                + $"material={(approvedFloorMaterial == null ? "missing" : approvedFloorMaterial.name)}; "
                + $"spanX={MinX:F2}..{MaxX:F2}; spanZ={FacadeEdgeZ:F2}..{FloorJoinZ:F2}; "
                + $"thickness={DeckThicknessM:F2}";
            return renderersValid && collidersValid;
        }

        private void LateUpdate()
        {
            S2FloorSceneMarker marker = FindObjectsByType<S2FloorSceneMarker>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).SingleOrDefault();
            if (marker == null || marker.GetInstanceID() == lastFloorMarkerInstanceId)
                return;

            lastFloorMarkerInstanceId = marker.GetInstanceID();
            if (string.Equals(marker.FloorId, "F00", StringComparison.Ordinal))
            {
                LastRemappedFloorId = string.Empty;
                LastBoundaryDetail = "F00 uses its entrance-specific S3D boundary mapping";
                return;
            }

            RemapFrontSafetyBoundary(marker, out string detail);
            LastBoundaryDetail = detail;
        }

        public bool RemapFrontSafetyBoundary(S2FloorSceneMarker marker, out string detail)
        {
            if (marker == null || string.Equals(marker.FloorId, "F00",
                    StringComparison.Ordinal))
            {
                detail = "upper-floor marker unavailable";
                return false;
            }

            Transform boundary = marker.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => string.Equals(item.name,
                    "S2_R03_SafetyBoundary_03", StringComparison.Ordinal));
            BoxCollider collider = boundary == null ? null : boundary.GetComponent<BoxCollider>();
            if (boundary == null || collider == null)
            {
                detail = $"floor={marker.FloorId}; original front boundary missing";
                return false;
            }

            boundary.localPosition = new Vector3((MinX + MaxX) * 0.5f,
                marker.ElevationM + 1.1f, FacadeEdgeZ);
            boundary.localRotation = Quaternion.identity;
            collider.center = Vector3.zero;
            collider.size = new Vector3(MaxX - MinX, 2.2f, 0.18f);
            collider.enabled = true;
            Physics.SyncTransforms();

            Bounds bounds = collider.bounds;
            bool aligned = Mathf.Abs(bounds.min.x - MinX) <= 0.015f
                && Mathf.Abs(bounds.max.x - MaxX) <= 0.015f
                && Mathf.Abs(bounds.center.z - FacadeEdgeZ) <= 0.015f;
            LastRemappedFloorId = aligned ? marker.FloorId : string.Empty;
            detail = $"floor={marker.FloorId}; aligned={aligned}; boundaryZ={bounds.center.z:F2}; "
                + $"spanX={bounds.min.x:F2}..{bounds.max.x:F2}";
            return aligned;
        }

        public bool ValidateFloor(string floorId, float elevationM, out string detail)
        {
            string expectedName = FinishPrefix + floorId;
            Renderer finish = finishes.SingleOrDefault(item => item != null
                && string.Equals(item.name, expectedName, StringComparison.Ordinal));
            BoxCollider support = supportColliders.SingleOrDefault(item => item != null
                && string.Equals(item.name, expectedName, StringComparison.Ordinal));
            if (finish == null || support == null)
            {
                detail = $"floor={floorId}; finish={(finish != null)}; support={(support != null)}";
                return false;
            }

            Bounds bounds = support.bounds;
            bool aligned = Mathf.Abs(bounds.min.x - MinX) <= 0.015f
                && Mathf.Abs(bounds.max.x - MaxX) <= 0.015f
                && Mathf.Abs(bounds.min.z - FacadeEdgeZ) <= 0.015f
                && Mathf.Abs(bounds.max.z - FloorJoinZ) <= 0.015f
                && Mathf.Abs(bounds.max.y - elevationM) <= 0.015f
                && Mathf.Abs(bounds.size.y - DeckThicknessM) <= 0.015f;
            bool material = finish.sharedMaterial == approvedFloorMaterial;
            bool solid = support.enabled && !support.isTrigger
                && support.Raycast(new Ray(new Vector3((MinX + MaxX) * 0.5f,
                    elevationM + 1f, (FacadeEdgeZ + FloorJoinZ) * 0.5f), Vector3.down), out _, 2f);
            bool boundary = string.Equals(LastRemappedFloorId, floorId,
                StringComparison.Ordinal);
            detail = $"floor={floorId}; aligned={aligned}; material={material}; solid={solid}; boundary={boundary}; "
                + $"bounds={bounds.min.ToString("F3")}..{bounds.max.ToString("F3")}";
            return aligned && material && solid && boundary;
        }
    }
}
