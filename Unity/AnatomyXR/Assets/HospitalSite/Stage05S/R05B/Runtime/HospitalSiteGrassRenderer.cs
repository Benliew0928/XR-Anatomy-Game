using UnityEngine;
using UnityEngine.Rendering;

namespace CutMyBodyPlease.HospitalSite.Stage05S
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class HospitalSiteGrassRenderer : MonoBehaviour
    {
        private const int MatrixCapacity = 512;
        private static readonly int ViewerPositionId = Shader.PropertyToID("_ViewerPosition");
        private static readonly int LodLevelId = Shader.PropertyToID("_LodLevel");

        [SerializeField] private HospitalSiteGrassRuntimeConfig config;
        [SerializeField] private Transform anchor;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool renderInSceneView = true;

        private readonly Matrix4x4[] lod0Matrices = new Matrix4x4[MatrixCapacity];
        private readonly Matrix4x4[] lod1Matrices = new Matrix4x4[MatrixCapacity];
        private MaterialPropertyBlock lod0Properties;
        private MaterialPropertyBlock lod1Properties;
        private Vector2Int snappedCell = new Vector2Int(int.MinValue, int.MinValue);
        private int lod0Count;
        private int lod1Count;
        private int activeTriangles;
        private bool subscribed;

        public HospitalSiteGrassRuntimeConfig Config => config;
        public int ActiveLod0Instances => lod0Count;
        public int ActiveLod1Instances => lod1Count;
        public int ActiveTriangles => activeTriangles;
        public int ActiveDrawSubmissions => (lod0Count > 0 ? 1 : 0) + (lod1Count > 0 ? 1 : 0);

        public void Configure(HospitalSiteGrassRuntimeConfig value)
        {
            config = value;
            snappedCell = new Vector2Int(int.MinValue, int.MinValue);
        }

        public void SetAnchor(Transform value) => anchor = value;
        public void SetTargetCamera(Camera value) => targetCamera = value;

        private void OnEnable()
        {
            EnsureResources();
            Subscribe();
        }

        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (subscribed) return;
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            subscribed = false;
        }

        private void EnsureResources()
        {
            lod0Properties ??= new MaterialPropertyBlock();
            lod1Properties ??= new MaterialPropertyBlock();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!isActiveAndEnabled || config == null || camera == null ||
                (targetCamera != null && camera != targetCamera) ||
                (!renderInSceneView && camera.cameraType == CameraType.SceneView) ||
                camera.cameraType == CameraType.Preview || camera.cameraType == CameraType.Reflection)
                return;

            Transform viewer = anchor != null ? anchor : camera.transform;
            RenderForViewer(camera, viewer.position);
        }

        public void RenderForViewer(Camera camera, Vector3 viewerPosition)
        {
            if (config == null || config.bladeMaterial == null || config.lod0Patch == null || config.lod1Patch == null)
                return;

            EnsureResources();
            RebuildIfRequired(viewerPosition);
            Vector4 viewer = new Vector4(viewerPosition.x, viewerPosition.y, viewerPosition.z, 1.0f);

            Bounds bounds = new Bounds(
                new Vector3(snappedCell.x * config.tileSize, config.surfaceHeight + 0.05f, snappedCell.y * config.tileSize),
                new Vector3(config.lod1Radius * 2.0f + config.tileSize * 2.0f, 1.0f,
                    config.lod1Radius * 2.0f + config.tileSize * 2.0f));

            if (lod0Count > 0)
            {
                lod0Properties.SetVector(ViewerPositionId, viewer);
                lod0Properties.SetFloat(LodLevelId, 0.0f);
                var parameters = new RenderParams(config.bladeMaterial)
                {
                    camera = camera,
                    layer = gameObject.layer,
                    worldBounds = bounds,
                    matProps = lod0Properties,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false
                };
                Graphics.RenderMeshInstanced(parameters, config.lod0Patch, 0, lod0Matrices, lod0Count);
            }

            if (lod1Count > 0)
            {
                lod1Properties.SetVector(ViewerPositionId, viewer);
                lod1Properties.SetFloat(LodLevelId, 1.0f);
                var parameters = new RenderParams(config.bladeMaterial)
                {
                    camera = camera,
                    layer = gameObject.layer,
                    worldBounds = bounds,
                    matProps = lod1Properties,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = false
                };
                Graphics.RenderMeshInstanced(parameters, config.lod1Patch, 0, lod1Matrices, lod1Count);
            }
        }

        public void ForceRebuild(Vector3 viewerPosition)
        {
            snappedCell = new Vector2Int(int.MinValue, int.MinValue);
            RebuildIfRequired(viewerPosition);
        }

        private void RebuildIfRequired(Vector3 viewerPosition)
        {
            int cellX = Mathf.RoundToInt(viewerPosition.x / config.tileSize);
            int cellZ = Mathf.RoundToInt(viewerPosition.z / config.tileSize);
            var nextCell = new Vector2Int(cellX, cellZ);
            if (nextCell == snappedCell) return;

            snappedCell = nextCell;
            lod0Count = 0;
            lod1Count = 0;
            activeTriangles = 0;

            int radiusCells = Mathf.CeilToInt(config.lod1Radius / config.tileSize);
            for (int dz = -radiusCells; dz <= radiusCells; dz++)
            {
                for (int dx = -radiusCells; dx <= radiusCells; dx++)
                {
                    float worldX = (cellX + dx) * config.tileSize;
                    float worldZ = (cellZ + dz) * config.tileSize;
                    float distance = Vector2.Distance(new Vector2(worldX, worldZ),
                        new Vector2(viewerPosition.x, viewerPosition.z));
                    if (distance > config.lod1Radius) continue;

                    int occupancyX = Mathf.FloorToInt((worldX - config.siteMinimum.x) / config.tileSize);
                    int occupancyZ = Mathf.FloorToInt((worldZ - config.siteMinimum.y) / config.tileSize);
                    if (!config.IsOccupied(occupancyX, occupancyZ)) continue;

                    int triangleCost = distance <= config.lod0Radius
                        ? config.lod0PatchTriangles : config.lod1PatchTriangles;
                    if (activeTriangles + triangleCost > config.maximumActiveTriangles) continue;

                    uint hash = Hash((uint)(cellX + dx), (uint)(cellZ + dz));
                    // The frozen FBX patch mesh is authored Z-up. Rendering the Mesh
                    // directly bypasses its imported root transform, so reproduce that
                    // axis conversion before applying the deterministic world yaw.
                    Quaternion rotation = Quaternion.Euler(0.0f, (hash & 3u) * 90.0f, 0.0f) *
                                          Quaternion.Euler(-90.0f, 0.0f, 0.0f);
                    Matrix4x4 matrix = Matrix4x4.TRS(
                        new Vector3(worldX, config.surfaceHeight, worldZ), rotation, Vector3.one);

                    if (distance <= config.lod0Radius && lod0Count < MatrixCapacity)
                        lod0Matrices[lod0Count++] = matrix;
                    else if (lod1Count < MatrixCapacity)
                        lod1Matrices[lod1Count++] = matrix;
                    else
                        continue;
                    activeTriangles += triangleCost;
                }
            }
        }

        private static uint Hash(uint x, uint y)
        {
            uint value = x * 0x8da6b343u ^ y * 0xd8163841u;
            value ^= value >> 13;
            value *= 0x85ebca6bu;
            return value ^ (value >> 16);
        }
    }
}
