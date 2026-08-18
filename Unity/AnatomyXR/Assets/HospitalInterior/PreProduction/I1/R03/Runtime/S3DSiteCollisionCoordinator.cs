using System;
using System.Collections.Generic;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DSiteCollisionCoordinator : MonoBehaviour
    {
        public const string GrassSupportName = "S3D_R03_LawnSupportSurface";
        public const string EntranceBoundaryRootName = "S3D_R03_F00_EntranceBoundaryRemap";
        private const string F00BoundaryRootName = "S2_R03_InvisibleSafetyBoundaryRoot";
        private const string ObsoleteExteriorFrontPerimeterName = "COL_Perimeter_Front";
        private const float OpeningMarginM = 0.2f;

        private HospitalSiteGrassRuntimeConfig grassConfig;
        private S3DEntranceDoorController entranceDoors;
        private S2FloorRecord f00;
        [SerializeField] private MeshCollider grassSupportCollider;
        [SerializeField] private string bakedLawnRendererName;
        [SerializeField] private string bakedLawnMeshName;
        [SerializeField] private int bakedLawnTriangleCount;
        private MeshFilter lawnVisualFilter;
        private Transform entranceBoundaryRoot;
        private bool initialized;
        private readonly List<Vector3> lawnGroundingSamples = new List<Vector3>();

        public int GrassOccupiedCellCount { get; private set; }
        public int GrassSupportTriangleCount { get; private set; }
        public int SuppressedF00FrontBoundaryCount { get; private set; }
        public int ReplacementF00FrontBoundaryCount { get; private set; }
        public int SuppressedExteriorFrontPerimeterCount { get; private set; }
        public float EntranceOpeningMinX { get; private set; }
        public float EntranceOpeningMaxX { get; private set; }
        public MeshCollider GrassSupportCollider => grassSupportCollider;
        public IReadOnlyList<Vector3> LawnGroundingSamples => lawnGroundingSamples;

        public void ConfigureBakedLawnSupport(MeshCollider supportCollider,
            string sourceRendererName, string sourceMeshName, int triangleCount)
        {
            grassSupportCollider = supportCollider;
            bakedLawnRendererName = sourceRendererName;
            bakedLawnMeshName = sourceMeshName;
            bakedLawnTriangleCount = triangleCount;
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        public bool Initialize(HospitalSiteGrassRuntimeConfig config,
            S3DEntranceDoorController automaticEntrance, S2FloorContract floorContract,
            out string detail)
        {
            grassConfig = config;
            entranceDoors = automaticEntrance;
            if (grassConfig == null || entranceDoors == null || floorContract == null
                || !floorContract.TryGetFloor("F00", out f00))
            {
                detail = "grass, entrance, or F00 contract is missing";
                return false;
            }
            if (!grassConfig.IsContractValid(out string grassDetail))
            {
                detail = "approved grass contract is invalid: " + grassDetail;
                return false;
            }
            if (!entranceDoors.TryGetOuterClosedOpeningBounds(out Bounds outerOpening))
            {
                detail = "outer entrance bounds are unavailable";
                return false;
            }

            EntranceOpeningMinX = outerOpening.min.x - OpeningMarginM;
            EntranceOpeningMaxX = outerOpening.max.x + OpeningMarginM;
            if (EntranceOpeningMinX <= f00.minX || EntranceOpeningMaxX >= f00.maxX
                || EntranceOpeningMaxX - EntranceOpeningMinX < 4f)
            {
                detail = $"door-derived opening is invalid: {EntranceOpeningMinX:F3}..{EntranceOpeningMaxX:F3}";
                return false;
            }

            if (!BindRenderedLawnSupport(out string lawnDetail))
            {
                detail = lawnDetail;
                return false;
            }
            if (!SuppressObsoleteExteriorFrontPerimeter(out string perimeterDetail))
            {
                detail = perimeterDetail;
                return false;
            }
            Scene floorScene = SceneManager.GetSceneByPath(f00.scenePath);
            if (!ApplyF00EntranceBoundary(floorScene, out string boundaryDetail))
            {
                detail = boundaryDetail;
                return false;
            }
            initialized = true;
            Physics.SyncTransforms();
            detail = $"displayedLawnMesh={lawnVisualFilter.sharedMesh.name}; lawnTriangles={GrassSupportTriangleCount}; "
                + $"bladeOccupancyCells={GrassOccupiedCellCount}; groundingSamples={lawnGroundingSamples.Count}; "
                + $"obsoleteFrontPerimeterSuppressed={SuppressedExteriorFrontPerimeterCount}; "
                + $"F00FrontSuppressed={SuppressedF00FrontBoundaryCount}; replacements={ReplacementF00FrontBoundaryCount}; "
                + $"doorOpeningX={EntranceOpeningMinX:F3}..{EntranceOpeningMaxX:F3}";
            return true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!initialized || !string.Equals(scene.path, f00.scenePath,
                    StringComparison.Ordinal))
                return;
            if (!ApplyF00EntranceBoundary(scene, out string detail))
                Debug.LogError("HOSPITAL_INTERIOR_R03_S3D_F00_BOUNDARY_REMAP_FAILED: "
                    + detail, this);
        }

        private bool BindRenderedLawnSupport(out string detail)
        {
            if (grassSupportCollider == null || grassSupportCollider.sharedMesh == null
                || bakedLawnTriangleCount <= 0)
            {
                detail = "the editor-baked continuous-lawn collider is missing";
                return false;
            }

            MeshRenderer[] lawnRenderers = FindObjectsByType<MeshRenderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(renderer => renderer.sharedMaterials.Any(material => material != null
                    && string.Equals(material.name, "MAT_S05S_S5B_GrassLawn",
                        StringComparison.Ordinal)))
                .ToArray();
            if (lawnRenderers.Length != 1)
            {
                detail = "expected one rendered continuous lawn; found " + lawnRenderers.Length;
                return false;
            }
            lawnVisualFilter = lawnRenderers[0].GetComponent<MeshFilter>();
            if (lawnVisualFilter == null || lawnVisualFilter.sharedMesh == null)
            {
                detail = "continuous lawn renderer has no source mesh";
                return false;
            }
            if (!string.Equals(lawnRenderers[0].name, bakedLawnRendererName,
                    StringComparison.Ordinal))
            {
                detail = $"baked lawn renderer mismatch: {bakedLawnRendererName} != {lawnRenderers[0].name}";
                return false;
            }

            Bounds renderedBounds = lawnRenderers[0].bounds;
            Bounds collisionBounds = grassSupportCollider.bounds;
            float boundsDelta = Vector3.Distance(renderedBounds.min, collisionBounds.min)
                + Vector3.Distance(renderedBounds.max, collisionBounds.max);
            if (boundsDelta > 0.05f)
            {
                detail = $"baked lawn collider is not aligned to the rendered lawn; boundsDelta={boundsDelta:F4}";
                return false;
            }
            GrassOccupiedCellCount = grassConfig.CountOccupied();
            GrassSupportTriangleCount = bakedLawnTriangleCount;
            BuildLawnGroundingSamples();
            detail = $"renderer={lawnRenderers[0].name}; bakedMesh={bakedLawnMeshName}; "
                + $"displayMesh={lawnVisualFilter.sharedMesh.name}; boundsDelta={boundsDelta:F4}; "
                + $"triangles={GrassSupportTriangleCount}; samples={lawnGroundingSamples.Count}";
            return lawnGroundingSamples.Count >= 8;
        }

        private void BuildLawnGroundingSamples()
        {
            lawnGroundingSamples.Clear();
            if (grassSupportCollider == null)
                return;
            Bounds bounds = grassSupportCollider.bounds;
            var excludedBladeCandidates = new List<Vector3>();
            var occupiedBladeCandidates = new List<Vector3>();
            float rayY = bounds.max.y + 5f;
            const float accessiblePerimeterInsetM = 5f;
            for (float z = bounds.min.z + accessiblePerimeterInsetM;
                 z <= bounds.max.z - accessiblePerimeterInsetM; z += 3f)
            {
                for (float x = bounds.min.x + accessiblePerimeterInsetM;
                     x <= bounds.max.x - accessiblePerimeterInsetM; x += 3f)
                {
                    Ray ray = new Ray(new Vector3(x, rayY, z), Vector3.down);
                    if (!grassSupportCollider.Raycast(ray, out RaycastHit hit, 12f))
                        continue;
                    int gridX = Mathf.RoundToInt((hit.point.x - grassConfig.siteMinimum.x)
                        / grassConfig.tileSize);
                    int gridZ = Mathf.RoundToInt((hit.point.z - grassConfig.siteMinimum.y)
                        / grassConfig.tileSize);
                    (grassConfig.IsOccupied(gridX, gridZ)
                        ? occupiedBladeCandidates : excludedBladeCandidates).Add(hit.point);
                }
            }
            AddDistributedSamples(excludedBladeCandidates, 8);
            AddDistributedSamples(occupiedBladeCandidates, 4);
        }

        private void AddDistributedSamples(IEnumerable<Vector3> candidates, int maximum)
        {
            var pool = candidates.ToList();
            int added = 0;
            while (pool.Count > 0 && added < maximum && lawnGroundingSamples.Count < 12)
            {
                Vector3 point = pool.OrderByDescending(candidate =>
                {
                    Vector2 candidateXZ = new Vector2(candidate.x, candidate.z);
                    if (lawnGroundingSamples.Count == 0)
                    {
                        Vector3 center = grassSupportCollider.bounds.center;
                        return -Vector2.Distance(candidateXZ,
                            new Vector2(center.x, center.z));
                    }
                    return lawnGroundingSamples.Min(existing => Vector2.Distance(candidateXZ,
                        new Vector2(existing.x, existing.z)));
                }).First();
                float separation = lawnGroundingSamples.Count == 0 ? float.MaxValue
                    : lawnGroundingSamples.Min(existing => Vector2.Distance(
                        new Vector2(point.x, point.z), new Vector2(existing.x, existing.z)));
                if (separation < 12f)
                    break;
                lawnGroundingSamples.Add(point);
                pool.Remove(point);
                added++;
            }
        }

        private bool SuppressObsoleteExteriorFrontPerimeter(out string detail)
        {
            Collider[] obsolete = FindObjectsByType<Collider>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .Where(item => string.Equals(item.name, ObsoleteExteriorFrontPerimeterName,
                    StringComparison.Ordinal))
                .ToArray();
            if (obsolete.Length != 1)
            {
                detail = $"expected one obsolete exterior front perimeter; found {obsolete.Length}";
                return false;
            }
            foreach (Collider collider in obsolete)
                collider.enabled = false;
            SuppressedExteriorFrontPerimeterCount = obsolete.Length;
            Physics.SyncTransforms();
            detail = $"suppressed={obsolete.Length}; bounds={obsolete[0].bounds}";
            return true;
        }

        private bool ApplyF00EntranceBoundary(Scene floorScene, out string detail)
        {
            if (!floorScene.IsValid() || !floorScene.isLoaded)
            {
                detail = "F00 is not loaded for entrance-boundary integration";
                return false;
            }
            Transform sourceRoot = floorScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(item => item.name == F00BoundaryRootName);
            if (sourceRoot == null)
            {
                detail = "F00 invisible safety-boundary root is missing";
                return false;
            }

            BoxCollider[] front = sourceRoot.GetComponentsInChildren<BoxCollider>(true)
                .Where(item => Mathf.Abs(item.bounds.center.z - f00.minZ) <= 0.05f
                    && item.bounds.size.x > 10f && item.bounds.size.z <= 0.25f)
                .ToArray();
            if (front.Length != 2)
            {
                detail = "expected two original F00 front boundaries; found " + front.Length;
                return false;
            }
            foreach (BoxCollider collider in front)
                collider.enabled = false;
            SuppressedF00FrontBoundaryCount = front.Length;

            if (entranceBoundaryRoot == null)
            {
                var root = new GameObject(EntranceBoundaryRootName);
                root.transform.SetParent(transform, false);
                entranceBoundaryRoot = root.transform;
            }
            ConfigureBoundary("S3D_R03_F00_FrontBoundary_Left", f00.minX,
                EntranceOpeningMinX);
            ConfigureBoundary("S3D_R03_F00_FrontBoundary_Right", EntranceOpeningMaxX,
                f00.maxX);
            ReplacementF00FrontBoundaryCount = entranceBoundaryRoot
                .GetComponentsInChildren<BoxCollider>(true).Length;
            Physics.SyncTransforms();
            detail = $"suppressed={front.Length}; replacements={ReplacementF00FrontBoundaryCount}; "
                + $"openingX={EntranceOpeningMinX:F3}..{EntranceOpeningMaxX:F3}";
            return ReplacementF00FrontBoundaryCount == 2;
        }

        private void ConfigureBoundary(string name, float startX, float endX)
        {
            Transform child = entranceBoundaryRoot.Find(name);
            if (child == null)
            {
                var created = new GameObject(name);
                created.transform.SetParent(entranceBoundaryRoot, false);
                child = created.transform;
            }
            child.position = new Vector3((startX + endX) * 0.5f,
                f00.elevationM + 1.1f, f00.minZ);
            child.rotation = Quaternion.identity;
            BoxCollider collider = child.GetComponent<BoxCollider>();
            if (collider == null)
                collider = child.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(endX - startX, 2.2f, 0.18f);
            collider.isTrigger = false;
            collider.enabled = true;
        }

        public bool ValidateGrassSupport(out string detail)
        {
            bool exactRenderedMesh = grassSupportCollider != null && lawnVisualFilter != null
                && lawnVisualFilter.sharedMesh != null && grassSupportCollider.sharedMesh != null
                && string.Equals(lawnVisualFilter.name, bakedLawnRendererName,
                    StringComparison.Ordinal)
                && Vector3.Distance(lawnVisualFilter.GetComponent<Renderer>().bounds.min,
                       grassSupportCollider.bounds.min)
                   + Vector3.Distance(lawnVisualFilter.GetComponent<Renderer>().bounds.max,
                       grassSupportCollider.bounds.max) <= 0.05f;
            bool valid = initialized && grassSupportCollider != null
                && exactRenderedMesh && GrassSupportTriangleCount > 0
                && lawnGroundingSamples.Count >= 8;
            detail = $"exactRenderedMesh={exactRenderedMesh}; triangles={GrassSupportTriangleCount}; "
                + $"bladeOccupancyCells={GrassOccupiedCellCount}; samples={lawnGroundingSamples.Count}";
            return valid;
        }

        public bool ValidateOpenEntranceTraversal(S3DAdaptiveRig adaptiveRig, out string detail)
        {
            if (!initialized || !entranceDoors.FullyOpen || adaptiveRig == null
                || adaptiveRig.CharacterController == null)
            {
                detail = "site collision/rig is not initialized or entrance is not fully open";
                return false;
            }

            float[] lanes =
            {
                EntranceOpeningMinX + 0.55f,
                (EntranceOpeningMinX + EntranceOpeningMaxX) * 0.5f,
                EntranceOpeningMaxX - 0.55f,
            };
            var blockers = new HashSet<string>(StringComparer.Ordinal);
            Vector3 officialDropoff = new Vector3(0f, 0f, -42f);
            Vector3 officialDoorApproach = new Vector3(lanes[1], 0f, -25.5f);
            Vector3 picturedLawn = new Vector3(14f, 0f, -36f);
            Vector3 picturedDoorApproach = new Vector3(lanes[2], 0f, -25.5f);
            ScanCapsuleSegment(officialDropoff, officialDoorApproach, blockers);
            ScanCapsuleSegment(picturedLawn, picturedDoorApproach, blockers);
            foreach (float x in lanes)
            {
                for (float z = -25.5f; z <= -18f; z += 0.25f)
                {
                    Vector3 bottom = new Vector3(x, 0.4f, z);
                    Vector3 top = new Vector3(x, 1.5f, z);
                    foreach (Collider collider in Physics.OverlapCapsule(bottom, top, 0.28f,
                                 ~0, QueryTriggerInteraction.Ignore))
                    {
                        if (!collider.enabled || collider == grassSupportCollider
                            || collider.bounds.max.y <= 0.35f
                            || collider.GetComponentInParent<S3DAdaptiveRig>() != null)
                            continue;
                        blockers.Add(collider.name);
                    }
                }
            }
            Vector3 savedPosition = adaptiveRig.transform.position;
            Quaternion savedRotation = adaptiveRig.transform.rotation;
            float officialApproachRemaining = MoveCharacterAlong(adaptiveRig,
                officialDropoff, officialDoorApproach);
            float picturedApproachRemaining = MoveCharacterAlong(adaptiveRig,
                picturedLawn, picturedDoorApproach);
            var endpoints = new List<float>(lanes.Length);
            foreach (float x in lanes)
            {
                adaptiveRig.SetPositionSafely(new Vector3(x, 0.11f, -25.5f),
                    Quaternion.identity);
                for (int step = 0; step < 32; step++)
                    adaptiveRig.CharacterController.Move(new Vector3(0f, 0f, 0.25f));
                endpoints.Add(adaptiveRig.transform.position.z);
            }
            adaptiveRig.SetPositionSafely(savedPosition, savedRotation);
            detail = $"lanes={string.Join(",", lanes.Select(item => item.ToString("F2")))}; "
                + "capsuleBlockers=" + string.Join(",", blockers.OrderBy(item => item,
                    StringComparer.Ordinal)) + "; characterEndpoints="
                + string.Join(",", endpoints.Select(item => item.ToString("F2")))
                + $"; approachRemaining={officialApproachRemaining:F2}/{picturedApproachRemaining:F2}";
            return blockers.Count == 0 && endpoints.All(item => item >= -17.7f)
                && officialApproachRemaining <= 0.45f && picturedApproachRemaining <= 0.45f;
        }

        private void ScanCapsuleSegment(Vector3 start, Vector3 end,
            ISet<string> blockers)
        {
            float distance = Vector3.Distance(start, end);
            int steps = Mathf.CeilToInt(distance / 0.25f);
            for (int step = 0; step <= steps; step++)
            {
                Vector3 point = Vector3.Lerp(start, end, step / (float)steps);
                Vector3 bottom = new Vector3(point.x, 0.4f, point.z);
                Vector3 top = new Vector3(point.x, 1.5f, point.z);
                foreach (Collider collider in Physics.OverlapCapsule(bottom, top, 0.28f,
                             ~0, QueryTriggerInteraction.Ignore))
                {
                    if (!collider.enabled || collider == grassSupportCollider
                        || collider.bounds.max.y <= 0.35f
                        || collider.GetComponentInParent<S3DAdaptiveRig>() != null)
                        continue;
                    blockers.Add(collider.name);
                }
            }
        }

        private static float MoveCharacterAlong(S3DAdaptiveRig adaptiveRig,
            Vector3 start, Vector3 target)
        {
            start.y = 0.11f;
            target.y = 0.11f;
            adaptiveRig.SetPositionSafely(start, Quaternion.identity);
            for (int step = 0; step < 300; step++)
            {
                Vector3 remaining = target - adaptiveRig.transform.position;
                remaining.y = 0f;
                if (remaining.magnitude <= 0.05f)
                    break;
                Vector3 horizontal = remaining.normalized * Mathf.Min(0.2f,
                    remaining.magnitude);
                horizontal.y = -0.05f;
                adaptiveRig.CharacterController.Move(horizontal);
            }
            Vector2 delta = new Vector2(adaptiveRig.transform.position.x - target.x,
                adaptiveRig.transform.position.z - target.z);
            return delta.magnitude;
        }
    }
}
