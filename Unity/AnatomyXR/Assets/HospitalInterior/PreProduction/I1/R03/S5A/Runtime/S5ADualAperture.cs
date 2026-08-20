using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S5ADualAperture : MonoBehaviour
    {
        [SerializeField] private S5AStairContract contract;

        public S5AStairContract Contract => contract;
        public string LastAppliedFloorId { get; private set; } = string.Empty;

        public void Configure(S5AStairContract stairContract) => contract = stairContract;

        public bool ApplyToFloorScene(Scene floorScene, string floorId, out string detail)
        {
            if (!TryGetSlab(floorScene, floorId, out MeshFilter filter,
                    out MeshCollider collider, out string lookupDetail))
            {
                detail = lookupDetail;
                return false;
            }
            Mesh replacement = floorId == "F00"
                ? contract.F00DualApertureSlab : contract.UpperDualApertureSlab;
            if (!ValidateMeshOpenings(replacement, out string meshDetail))
            {
                detail = meshDetail;
                return false;
            }
            filter.sharedMesh = replacement;
            collider.sharedMesh = null;
            collider.sharedMesh = replacement;
            LastAppliedFloorId = floorId;
            Physics.SyncTransforms();
            detail = $"floor={floorId}; mesh={replacement.name}; {meshDetail}";
            return true;
        }

        public bool ValidateApplied(Scene floorScene, string floorId, out string detail)
        {
            Mesh expected = floorId == "F00"
                ? contract.F00DualApertureSlab : contract.UpperDualApertureSlab;
            bool found = TryGetSlab(floorScene, floorId, out MeshFilter filter,
                out MeshCollider collider, out string lookupDetail);
            bool openings = ValidateMeshOpenings(expected, out string meshDetail);
            bool applied = found && filter.sharedMesh == expected && collider.sharedMesh == expected;
            detail = $"found={found}; applied={applied}; {lookupDetail}; {meshDetail}";
            return applied && openings;
        }

        public bool ValidateAssets(out string detail)
        {
            if (contract == null)
            {
                detail = "S5A contract missing";
                return false;
            }
            bool f00 = ValidateMeshOpenings(contract.F00DualApertureSlab, out string f00Detail);
            bool upper = ValidateMeshOpenings(contract.UpperDualApertureSlab, out string upperDetail);
            bool underside = ValidateMeshOpenings(contract.UpperDualApertureUnderside, out string undersideDetail);
            detail = $"F00[{f00Detail}]; Upper[{upperDetail}]; Underside[{undersideDetail}]";
            return f00 && upper && underside;
        }

        public bool ValidateMeshOpenings(Mesh mesh, out string detail)
        {
            if (mesh == null || contract == null)
            {
                detail = "mesh or contract missing";
                return false;
            }
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Rect e01 = new Rect(0.30f, 2.90f, 2.70f, 2.80f);
            Rect stair = new Rect(contract.BoundsMinXZ.x, contract.BoundsMinXZ.y,
                contract.BoundsMaxXZ.x - contract.BoundsMinXZ.x,
                contract.BoundsMaxXZ.y - contract.BoundsMinXZ.y);
            int intrusions = 0;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector3 a = vertices[triangles[index]];
                Vector3 b = vertices[triangles[index + 1]];
                Vector3 c = vertices[triangles[index + 2]];
                Vector2 centre = new Vector2((a.x + b.x + c.x) / 3f, (a.z + b.z + c.z) / 3f);
                if (e01.Contains(centre) || stair.Contains(centre))
                    intrusions++;
            }
            bool stairEdges = vertices.Any(item => Mathf.Abs(item.x - contract.BoundsMinXZ.x) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.x - contract.BoundsMaxXZ.x) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - contract.BoundsMinXZ.y) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - contract.BoundsMaxXZ.y) <= 0.001f);
            bool e01Edges = vertices.Any(item => Mathf.Abs(item.x - e01.xMin) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.x - e01.xMax) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - e01.yMin) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - e01.yMax) <= 0.001f);
            detail = $"vertices={vertices.Length}; triangles={triangles.Length / 3}; intrusions={intrusions}; e01Edges={e01Edges}; stairEdges={stairEdges}";
            return vertices.Length > 0 && triangles.Length > 0 && intrusions == 0
                && e01Edges && stairEdges;
        }

        private bool TryGetSlab(Scene floorScene, string floorId, out MeshFilter filter,
            out MeshCollider collider, out string detail)
        {
            filter = null;
            collider = null;
            S2FloorSceneMarker marker = floorScene.IsValid() && floorScene.isLoaded
                ? floorScene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<S2FloorSceneMarker>(true))
                    .SingleOrDefault(item => item.FloorId == floorId)
                : null;
            Transform slab = marker == null ? null : marker.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "S2_R03_EmptySlab");
            if (slab != null)
            {
                filter = slab.GetComponent<MeshFilter>();
                collider = slab.GetComponent<MeshCollider>();
            }
            detail = $"marker={(marker != null)}; slab={(slab != null)}; filter={(filter != null)}; collider={(collider != null)}";
            return marker != null && slab != null && filter != null && collider != null;
        }
    }
}
