using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3ShaftFloorAperture : MonoBehaviour
    {
        [SerializeField] private S3ElevatorContract contract;
        [SerializeField] private Mesh f00ShaftCutSlab;
        [SerializeField] private Mesh upperShaftCutSlab;
        [SerializeField] private Mesh upperShaftCutUnderside;

        public S3ElevatorContract Contract => contract;
        public Mesh F00ShaftCutSlab => f00ShaftCutSlab;
        public Mesh UpperShaftCutSlab => upperShaftCutSlab;
        public Mesh UpperShaftCutUnderside => upperShaftCutUnderside;
        public string LastAppliedFloorId { get; private set; } = string.Empty;

        public void Configure(S3ElevatorContract elevatorContract, Mesh f00Mesh, Mesh upperMesh, Mesh undersideMesh)
        {
            contract = elevatorContract;
            f00ShaftCutSlab = f00Mesh;
            upperShaftCutSlab = upperMesh;
            upperShaftCutUnderside = undersideMesh;
        }

        public bool ApplyToFloorScene(Scene floorScene, string floorId, out string detail)
        {
            if (!floorScene.IsValid() || !floorScene.isLoaded || contract == null)
            {
                detail = "floor scene or shaft contract unavailable";
                return false;
            }
            Mesh replacement = floorId == "F00" ? f00ShaftCutSlab : upperShaftCutSlab;
            bool found = TryGetSlabComponents(floorScene, floorId, out S2FloorSceneMarker marker,
                out Transform slab, out MeshFilter filter, out MeshCollider collider);
            bool valid = replacement != null && filter != null && collider != null
                && ValidateMeshOpening(replacement, out _);
            if (!found || !valid)
            {
                detail = $"floor={floorId}; marker={(marker != null)}; slab={(slab != null)}; replacement={(replacement != null)}";
                return false;
            }
            filter.sharedMesh = replacement;
            collider.sharedMesh = null;
            collider.sharedMesh = replacement;
            LastAppliedFloorId = floorId;
            Physics.SyncTransforms();
            detail = $"floor={floorId}; mesh={replacement.name}; vertices={replacement.vertexCount}; triangles={replacement.triangles.Length / 3}";
            return true;
        }

        public bool ValidateAppliedToFloorScene(Scene floorScene, string floorId, out string detail)
        {
            Mesh replacement = floorId == "F00" ? f00ShaftCutSlab : upperShaftCutSlab;
            bool found = TryGetSlabComponents(floorScene, floorId, out _, out _, out MeshFilter filter,
                out MeshCollider collider);
            bool opening = ValidateMeshOpening(replacement, out string openingDetail);
            bool applied = found && opening && filter.sharedMesh == replacement && collider.sharedMesh == replacement;
            detail = $"floor={floorId}; found={found}; renderMesh={(filter == null ? "missing" : filter.sharedMesh?.name)}; "
                + $"collisionMesh={(collider == null ? "missing" : collider.sharedMesh?.name)}; {openingDetail}";
            return applied;
        }

        public bool ValidateAssets(out string detail)
        {
            bool f00 = ValidateMeshOpening(f00ShaftCutSlab, out string f00Detail);
            bool upper = ValidateMeshOpening(upperShaftCutSlab, out string upperDetail);
            bool underside = ValidateMeshOpening(upperShaftCutUnderside, out string undersideDetail);
            bool facing = MeshFaces(f00ShaftCutSlab, true) && MeshFaces(upperShaftCutSlab, true)
                && MeshFaces(upperShaftCutUnderside, false);
            detail = $"F00[{f00Detail}]; Upper[{upperDetail}]; Underside[{undersideDetail}]; facing={facing}";
            return contract != null && f00 && upper && underside && facing;
        }

        public bool ValidateMeshOpening(Mesh mesh, out string detail)
        {
            if (mesh == null || contract == null)
            {
                detail = "mesh or contract missing";
                return false;
            }
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            float minX = contract.ShaftInnerMinXZ.x - contract.ShaftSideWallThicknessM;
            float maxX = contract.ShaftInnerMaxXZ.x + contract.ShaftSideWallThicknessM;
            float minZ = contract.ShaftInnerMinXZ.y;
            float maxZ = contract.ShaftInnerMaxXZ.y + contract.ShaftRearWallThicknessM;
            bool triangleInsideHole = false;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                float triangleMinX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
                float triangleMaxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
                float triangleMinZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));
                float triangleMaxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));
                if (triangleMinX < maxX - 0.001f && triangleMaxX > minX + 0.001f
                    && triangleMinZ < maxZ - 0.001f && triangleMaxZ > minZ + 0.001f)
                {
                    triangleInsideHole = true;
                    break;
                }
            }
            bool perimeterCoverage = vertices.Any(item => Mathf.Abs(item.x - minX) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.x - maxX) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - minZ) <= 0.001f)
                && vertices.Any(item => Mathf.Abs(item.z - maxZ) <= 0.001f);
            detail = $"vertices={vertices.Length}; triangles={triangles.Length / 3}; holeX={minX:F3}..{maxX:F3}; holeZ={minZ:F3}..{maxZ:F3}; intrusions={triangleInsideHole}";
            return vertices.Length > 0 && triangles.Length > 0 && !triangleInsideHole && perimeterCoverage;
        }

        private static bool TryGetSlabComponents(Scene floorScene, string floorId, out S2FloorSceneMarker marker,
            out Transform slab, out MeshFilter filter, out MeshCollider collider)
        {
            marker = floorScene.IsValid() && floorScene.isLoaded
                ? floorScene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<S2FloorSceneMarker>(true))
                    .SingleOrDefault(item => item.FloorId == floorId)
                : null;
            slab = marker == null ? null : marker.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => item.name == "S2_R03_EmptySlab");
            filter = slab == null ? null : slab.GetComponent<MeshFilter>();
            collider = slab == null ? null : slab.GetComponent<MeshCollider>();
            return marker != null && slab != null && filter != null && collider != null;
        }

        private static bool MeshFaces(Mesh mesh, bool upward)
        {
            if (mesh == null || mesh.normals == null || mesh.normals.Length == 0)
                return false;
            float expectedSign = upward ? 1f : -1f;
            return mesh.normals.All(normal => normal.y * expectedSign > 0.99f);
        }
    }
}
