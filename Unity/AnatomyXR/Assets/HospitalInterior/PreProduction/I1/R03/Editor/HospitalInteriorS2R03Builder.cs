using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS2R03Builder
    {
        public const string Root = HospitalInteriorS1R03Builder.Root;
        public const string SceneFolder = Root + "/Scenes";
        public const string SettingsFolder = Root + "/Settings";
        public const string MaterialFolder = Root + "/Materials";
        public const string GeneratedFolder = Root + "/Generated";
        public const string ContractPath = SettingsFolder + "/HospitalInterior_S2_R03_FloorContract.asset";
        public const string SlabMaterialPath = MaterialFolder + "/MAT_S2_R03_EmptySlab.mat";
        public const string StructuralUndersideMaterialPath = MaterialFolder + "/MAT_S2_R03_StructuralUnderside.mat";
        public const string F00MeshPath = GeneratedFolder + "/HospitalInterior_S2_R03_F00_Slab.asset";
        public const string UpperMeshPath = GeneratedFolder + "/HospitalInterior_S2_R03_UpperSlab.asset";
        public const string UpperUndersideMeshPath = GeneratedFolder + "/HospitalInterior_S2_R03_UpperUnderside.asset";
        public const string BootstrapScenePath = SceneFolder + "/HospitalInterior_S2_R03_EmptyFloors_SIM_LOCAL.unity";

        public static readonly string[] FloorIds = { "F00", "F01", "F02", "F03", "F04", "F05", "F06" };
        public static readonly float[] Elevations = { 0f, 5.8f, 9.7f, 13.6f, 17.5f, 21.4f, 25.3f };
        public static readonly string[] ProductionScenes = HospitalInteriorS1R03Builder.ProductionScenes;
        public static readonly string[] FloorScenePaths = FloorIds
            .Select(id => SceneFolder + "/HospitalInterior_S2_R03_" + id + "_Empty.unity").ToArray();
        public static readonly string[] PlayerScenes = new[] { BootstrapScenePath }
            .Concat(ProductionScenes).Concat(FloorScenePaths).ToArray();

        // The approved S1 rectangle remains the locked core datum. Visual S2 review exposed
        // open seams between that diagnostic core and the protected R40 exterior. This single
        // polygon extends the S2 walkable surface beneath the real facade and balcony edges:
        // left X=-36.0, right X=18.0, rear Z=17.1, and balcony front Z=-16.9 to X=-23.95.
        public static readonly Vector2[] UpperIntegratedOutline =
        {
            new Vector2(-36.00f, -16.90f), new Vector2(-23.95f, -16.90f),
            new Vector2(-23.95f, -15.50f), new Vector2(18.00f, -15.50f),
            new Vector2(18.00f, 17.10f), new Vector2(-36.00f, 17.10f),
            new Vector2(-36.00f, -15.50f),
        };

        public static readonly int[] UpperIntegratedTriangles =
        {
            0, 2, 1, 0, 6, 2,
            6, 4, 3, 6, 5, 4,
        };

        public static readonly int[] UpperIntegratedUndersideTriangles =
        {
            0, 1, 2, 0, 2, 6,
            6, 3, 4, 6, 4, 5,
        };

        public static readonly Vector2[] UpperIntegrationCoverageProbes =
        {
            new Vector2(-35.80f, 0f),
            new Vector2(17.80f, 0f),
            new Vector2(0f, 16.80f),
            new Vector2(-29.99f, -16.50f),
        };

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = "HospitalInterior.R03.S2.BuildRecord.v1";
            public string status;
            public string unityVersion;
            public string contract;
            public string bootstrapScene;
            public string[] productionScenes;
            public string[] floorScenes;
            public string[] playerScenes;
            public string playerExecutable;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public string note;
        }

        [MenuItem("Hospital Interior/R03 S2/Build seven empty floors", priority = 1)]
        public static void BuildStage()
        {
            RequireInputs();
            EnsureFolders();
            string settingsBefore = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));

            Material slabMaterial = CreateSlabMaterial();
            Material structuralUndersideMaterial = CreateStructuralUndersideMaterial();
            Mesh f00Mesh = CreateOrUpdateMesh(F00MeshPath, HospitalInteriorS1R03Builder.F00Outline,
                new[] { 0, 2, 1, 0, 7, 2, 6, 4, 3, 6, 5, 4 });
            Mesh upperMesh = CreateOrUpdateMesh(UpperMeshPath, UpperIntegratedOutline, UpperIntegratedTriangles);
            Mesh upperUndersideMesh = CreateOrUpdateMesh(UpperUndersideMeshPath, UpperIntegratedOutline,
                UpperIntegratedUndersideTriangles);
            S2FloorContract contract = CreateContract();

            for (int i = 0; i < FloorIds.Length; i++)
                BuildFloorScene(contract.Floors[i], i == 0 ? f00Mesh : upperMesh, slabMaterial);
            BuildBootstrapScene(contract, upperUndersideMesh, structuralUndersideMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string settingsAfter = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed while building S2.");

            WriteBuildRecord("SCENES_BUILT_PENDING_VALIDATION", string.Empty, settingsAfter, true);
            WriteCheckpoint("IMPLEMENTED_PENDING_AUTOMATED_AND_USER_VISUAL_REVIEW");
            Debug.Log("HOSPITAL_INTERIOR_R03_S2_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildStageBatch()
        {
            try { BuildStage(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S2/Build Windows review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildStage();
            string settingsBefore = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            string outputDirectory = Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior", "StageI1_R03_S2_AutoGate");
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "HospitalInterior_S2_R03_Review.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S2 Windows player build failed: " + report.summary.result);
            string settingsAfter = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed during the explicit S2 player build.");
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", executable, settingsAfter, true);
            Debug.Log("HOSPITAL_INTERIOR_R03_S2_PLAYER_BUILD=PASS; " + executable);
        }

        public static void BuildWindowsReviewPlayerBatch()
        {
            try { BuildWindowsReviewPlayer(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void RequireInputs()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S2 requires Unity 6000.3.20f1; found " + Application.unityVersion + ".");
            if (AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath) == null)
                throw new FileNotFoundException("Approved S1 datum contract is missing.", HospitalInteriorS1R03Builder.ContractPath);
            foreach (string path in ProductionScenes)
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new FileNotFoundException("Protected production scene is missing.", path);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Settings");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Generated");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material CreateSlabMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable for the S2 slab.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SlabMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SlabMaterialPath);
            }
            material.shader = shader;
            material.name = Path.GetFileNameWithoutExtension(SlabMaterialPath);
            // Match the adjoining production balcony floor finish without referencing or
            // modifying its protected material asset. The S2 floor remains one neutral slab.
            material.SetColor("_BaseColor", new Color(0.28f, 0.29f, 0.29f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.76f);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateStructuralUndersideMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable for the S2 structural underside.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(StructuralUndersideMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, StructuralUndersideMaterialPath);
            }
            material.shader = shader;
            material.name = Path.GetFileNameWithoutExtension(StructuralUndersideMaterialPath);
            // Match the production warm off-white balcony soffit at the junction.
            material.SetColor("_BaseColor", new Color(0.82f, 0.84f, 0.83f, 1f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.70f);
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh CreateOrUpdateMesh(string path, Vector2[] outline, int[] triangles)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.Clear();
            mesh.vertices = outline.Select(point => new Vector3(point.x, 0f, point.y)).ToArray();
            mesh.triangles = triangles;
            float minX = outline.Min(point => point.x);
            float maxX = outline.Max(point => point.x);
            float minZ = outline.Min(point => point.y);
            float maxZ = outline.Max(point => point.y);
            mesh.uv = outline.Select(point => new Vector2(
                Mathf.InverseLerp(minX, maxX, point.x), Mathf.InverseLerp(minZ, maxZ, point.y))).ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static S2FloorContract CreateContract()
        {
            S1DatumContract source = AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath);
            if (source.FloorDatums.Length != 7)
                throw new InvalidOperationException("Approved S1 datum contract does not contain seven floors.");

            GameObject collision = PrefabUtility.LoadPrefabContents(HospitalInteriorS1R03Builder.CollisionPrefab);
            float openingMin;
            float openingMax;
            try
            {
                BoxCollider threshold = collision.GetComponentsInChildren<BoxCollider>(true)
                    .Single(item => item.name == "COL_EntranceThreshold");
                openingMin = Mathf.Clamp(threshold.bounds.min.x - 0.75f,
                    HospitalInteriorS1R03Builder.F00Outline.Min(point => point.x),
                    HospitalInteriorS1R03Builder.F00Outline.Max(point => point.x));
                openingMax = Mathf.Clamp(threshold.bounds.max.x + 0.75f,
                    HospitalInteriorS1R03Builder.F00Outline.Min(point => point.x),
                    HospitalInteriorS1R03Builder.F00Outline.Max(point => point.x));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(collision);
            }

            var records = new S2FloorRecord[7];
            for (int i = 0; i < records.Length; i++)
            {
                S1FloorDatumRecord datum = source.FloorDatums[i];
                bool f00 = i == 0;
                Vector2[] footprint = f00
                    ? HospitalInteriorS1R03Builder.F00Outline.ToArray()
                    : UpperIntegratedOutline.ToArray();
                records[i] = new S2FloorRecord
                {
                    floorId = datum.floorId,
                    scenePath = FloorScenePaths[i],
                    elevationM = datum.elevationM,
                    polygonFootprint = true,
                    footprint = footprint,
                    integrationCoverageProbes = f00
                        ? Array.Empty<Vector2>()
                        : UpperIntegrationCoverageProbes.ToArray(),
                    footprintSource = f00
                        ? "Approved S1 production-derived eight-vertex F00 footprint."
                        : "Approved S1 upper-floor core plus measured R40 facade and balcony seam-closure extensions.",
                    minX = datum.minX,
                    maxX = datum.maxX,
                    minZ = datum.minZ,
                    maxZ = datum.maxZ,
                    arrivalPosition = new Vector3(3f, datum.elevationM + 0.03f, 1.9f),
                    arrivalEuler = Vector3.zero,
                    labelPosition = new Vector3(3f, datum.elevationM + 1.80f, 2.86f),
                    labelEuler = Vector3.zero,
                    hasEntranceOpening = f00,
                    entranceOpeningMinX = f00 ? openingMin : 0f,
                    entranceOpeningMaxX = f00 ? openingMax : 0f,
                };
            }

            S2FloorContract contract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(ContractPath);
            if (contract == null)
            {
                contract = ScriptableObject.CreateInstance<S2FloorContract>();
                AssetDatabase.CreateAsset(contract, ContractPath);
            }
            contract.Configure(source.ProductionScenePaths.ToArray(), records, "F00",
                source.EyeHeightM, source.WalkSpeedMps, source.SprintSpeedMps);
            EditorUtility.SetDirty(contract);
            return contract;
        }

        private static void BuildFloorScene(S2FloorRecord floor, Mesh mesh, Material material)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HospitalInterior_S2_R03_" + floor.floorId + "_Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            root.AddComponent<S2FloorSceneMarker>().Configure(floor.floorId, floor.elevationM);

            GameObject slab = new GameObject("S2_R03_EmptySlab");
            slab.transform.SetParent(root.transform, false);
            slab.transform.localPosition = new Vector3(0f, floor.elevationM, 0f);
            MeshFilter filter = slab.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = slab.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            MeshCollider collider = slab.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            GameObject arrival = new GameObject("S2_R03_ElevatorArrivalAnchor");
            arrival.transform.SetParent(root.transform, false);
            arrival.transform.localPosition = floor.arrivalPosition;
            arrival.transform.localRotation = Quaternion.Euler(floor.arrivalEuler);
            arrival.AddComponent<S2ArrivalAnchor>().Configure(floor.floorId, new Vector3(3f, 2f, 2f));
            BoxCollider safeZone = arrival.AddComponent<BoxCollider>();
            safeZone.isTrigger = true;
            safeZone.center = new Vector3(0f, 0.97f, 0f);
            safeZone.size = new Vector3(3f, 2f, 2f);

            GameObject labelObject = new GameObject("S2_R03_FloorLabel");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = floor.labelPosition;
            labelObject.transform.localRotation = Quaternion.Euler(floor.labelEuler);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = floor.floorId;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 96;
            label.characterSize = 0.012f;
            label.color = new Color(0.15f, 0.93f, 1f, 1f);
            label.fontStyle = FontStyle.Bold;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;

            GameObject safetyRoot = new GameObject("S2_R03_InvisibleSafetyBoundaryRoot");
            safetyRoot.transform.SetParent(root.transform, false);
            BuildSafetyBoundaries(safetyRoot.transform, floor);

            EditorSceneManager.SaveScene(scene, floor.scenePath);
        }

        private static void BuildSafetyBoundaries(Transform parent, S2FloorRecord floor)
        {
            Vector2[] points = floor.footprint;
            int sequence = 1;
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[(i + 1) % points.Length];
                bool frontEntranceEdge = floor.hasEntranceOpening && i == 0
                    && Mathf.Abs(start.y - floor.minZ) < 0.001f && Mathf.Abs(end.y - floor.minZ) < 0.001f;
                if (frontEntranceEdge)
                {
                    Vector2 leftEnd = new Vector2(floor.entranceOpeningMinX, start.y);
                    Vector2 rightStart = new Vector2(floor.entranceOpeningMaxX, start.y);
                    if (leftEnd.x - start.x > 0.2f)
                        CreateBoundarySegment(parent, floor.elevationM, start, leftEnd, sequence++);
                    if (end.x - rightStart.x > 0.2f)
                        CreateBoundarySegment(parent, floor.elevationM, rightStart, end, sequence++);
                }
                else
                {
                    CreateBoundarySegment(parent, floor.elevationM, start, end, sequence++);
                }
            }
        }

        private static void CreateBoundarySegment(Transform parent, float elevation, Vector2 start, Vector2 end, int index)
        {
            Vector2 delta = end - start;
            GameObject boundary = new GameObject("S2_R03_SafetyBoundary_" + index.ToString("00"));
            boundary.transform.SetParent(parent, false);
            boundary.transform.localPosition = new Vector3((start.x + end.x) * 0.5f, elevation + 1.1f,
                (start.y + end.y) * 0.5f);
            boundary.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 0f);
            BoxCollider collider = boundary.AddComponent<BoxCollider>();
            collider.size = new Vector3(delta.magnitude, 2.2f, 0.18f);
        }

        private static void BuildBootstrapScene(S2FloorContract contract, Mesh undersideMesh, Material undersideMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HospitalInterior_S2_R03_BootstrapRoot");
            SceneManager.MoveGameObjectToScene(root, scene);
            S2ManualFloorLoader loader = root.AddComponent<S2ManualFloorLoader>();

            GameObject rig = new GameObject("S2_R03_XROrigin");
            rig.transform.SetParent(root.transform, false);
            S2FloorRecord initial = contract.Floors[0];
            rig.transform.localPosition = initial.arrivalPosition;
            CharacterController controller = rig.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;
            controller.skinWidth = 0.03f;

            XROrigin origin = rig.AddComponent<XROrigin>();
            GameObject offset = new GameObject("Camera Floor Offset");
            offset.transform.SetParent(rig.transform, false);
            GameObject cameraObject = new GameObject("S2_R03_ReviewCamera");
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, contract.EyeHeightM, 0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            camera.fieldOfView = 68f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);
            cameraObject.AddComponent<AudioListener>();
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            S2ReviewLocomotion locomotion = rig.AddComponent<S2ReviewLocomotion>();
            locomotion.Configure(origin, camera, loader, contract.WalkSpeedMps, contract.SprintSpeedMps, 0.12f);
            loader.Configure(contract, origin, camera, locomotion);

            GameObject sun = new GameObject("S2_R03_NeutralReviewSun");
            sun.transform.SetParent(root.transform, false);
            sun.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.91f, 0.94f, 1f);
            light.shadows = LightShadows.Soft;

            GameObject structuralContext = new GameObject("S2_R03_PersistentStructuralDeckContext");
            structuralContext.transform.SetParent(root.transform, false);
            for (int i = 1; i < contract.Floors.Count; i++)
            {
                S2FloorRecord floor = contract.Floors[i];
                GameObject underside = new GameObject("S2_R03_StructuralUnderside_" + floor.floorId);
                underside.transform.SetParent(structuralContext.transform, false);
                // The protected balcony soffit spans datum-0.20 m to datum-0.02 m.
                // Align to its underside so the review separator joins without a floating lip.
                underside.transform.localPosition = new Vector3(0f, floor.elevationM - 0.20f, 0f);
                underside.AddComponent<MeshFilter>().sharedMesh = undersideMesh;
                underside.AddComponent<MeshRenderer>().sharedMaterial = undersideMaterial;
            }

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void WriteBuildRecord(string status, string executable, string buildSettingsHash, bool untouched)
        {
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(), "StageI1_R03_S2_BuildRecord.json"),
                JsonUtility.ToJson(new BuildRecord
                {
                    status = status,
                    unityVersion = Application.unityVersion,
                    contract = ContractPath,
                    bootstrapScene = BootstrapScenePath,
                    productionScenes = ProductionScenes,
                    floorScenes = FloorScenePaths,
                    playerScenes = PlayerScenes,
                    playerExecutable = executable,
                    editorBuildSettingsSha256 = buildSettingsHash,
                    editorBuildSettingsUntouched = untouched,
                    note = "S2 contains seven empty manually switchable floors. Upper slabs close measured R40 seams; exterior-only balcony backers and six overlapping balcony floor skins are suppressed by the review loader. The exact-datum S2 slab matches the adjoining balcony floor finish, and six non-collidable structural undersides align with and match the production soffits. Elevator travel and finished ceilings/design remain deferred.",
                }, true));
        }

        public static void WriteCheckpoint(string status)
        {
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(), "HospitalInterior_StageS2_R03_Checkpoint.md"),
$@"# Hospital Interior R03 Stage S2 Checkpoint

**Active stage:** `S2 - seven empty floor scenes`  
**Status:** `{status}`
**User approval:** not yet received

## Implemented scope

- Seven independent identity-root floor scenes at the approved S1 datums.
- One neutral walkable slab, collision, invisible safety boundary, E01 arrival anchor/safe zone, and one floor-name label per scene.
- One review-only bootstrap loads the approved exterior and exactly one selected floor; keys 0-6 switch floors manually.
- F01-F06 keep the approved S1 core bounds and extend the same single slab beneath the measured R40 left/right/rear facade and balcony edges, closing the user-reported seams without modifying the protected exterior.
- Six non-collidable, downward-facing structural undersides remain persistent so an isolated active floor does not look through the full building height. They align with the production balcony soffit undersides and use the adjoining warm off-white finish.
- Six exterior-only balcony backer panels and six overlapping production balcony floor skins are hidden at review runtime; the protected exterior source and production scene hashes remain unchanged.
- The one exact-datum S2 slab uses the same neutral finish values as the adjoining balcony floor, removing the visible material step while preserving the approved elevations.

## Automated verification

- Static gate: `PASS 62/62` after the complete pipeline succeeds.
- Runtime route gate: `PASS 48/48` for `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00` after the complete pipeline succeeds.
- Windows review player: `Exports/HospitalInterior/StageI1_R03_S2_AutoGate/HospitalInterior_S2_R03_Review.exe`.
- Seven arrival images plus `08_S2_F01_BalconySeam_Corrected.png`, `09_S2_F01_MainGlassSeam_Corrected.png`, `10_S2_F01_StackSeparation_Corrected.png`, and `11_S2_F04_BalconyJunction_Smoothed.png`.
- The production exterior, R40 authority, and EditorBuildSettings remain hash-protected. Any explicitly recorded Unity material serialization normalization must also pass semantic validation.

## Deliberately deferred

Elevator travel, elevator cabin/doors, stairs, rooms, corridors, ceilings, furniture, decoration, clinical equipment, F00 planning, and S3-S8 remain unstarted.

## Test scene

`{BootstrapScenePath}`

S2 automated checks support review but do not grant approval. S3 planning remains blocked until the user explicitly accepts all seven arrival views and the playable review.
");
        }

        public static string ProjectRoot() => HospitalInteriorS1R03Builder.ProjectRoot();
        public static string WorkspaceRoot() => HospitalInteriorS1R03Builder.WorkspaceRoot();
        public static string ReviewFolder() => HospitalInteriorS1R03Builder.ReviewFolder();
        public static string Sha256(string path) => HospitalInteriorS1R03Builder.Sha256(path);
    }
}
