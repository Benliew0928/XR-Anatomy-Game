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
    public static class HospitalInteriorS1R03Builder
    {
        // The R03 package is intentionally self-contained; R01/R02 remain frozen references only.
        public const string Root = "Assets/HospitalInterior/PreProduction/I1/R03";
        public const string RuntimeFolder = Root + "/Runtime";
        public const string EditorFolder = Root + "/Editor";
        public const string MaterialFolder = Root + "/Materials";
        public const string GeneratedFolder = Root + "/Generated";
        public const string SceneFolder = Root + "/Scenes";
        public const string SettingsFolder = Root + "/Settings";
        public const string BootstrapScenePath = SceneFolder + "/HospitalInterior_S1_R03_Integration_SIM_LOCAL.unity";
        public const string ContractPath = SettingsFolder + "/HospitalInterior_S1_R03_DatumContract.asset";
        public const string DatumMaterialPath = MaterialFolder + "/MAT_S1_R03_DatumProxy.mat";
        public const string CageMaterialPath = MaterialFolder + "/MAT_S1_R03_ElevatorReference.mat";

        public const string ExteriorBase = "Assets/Hospital/Scenes/Additive/Exterior_Base.unity";
        public const string ExteriorTower = "Assets/Hospital/Scenes/Additive/Exterior_Tower.unity";
        public const string ExteriorSides = "Assets/Hospital/Scenes/Additive/Exterior_SidesService.unity";
        public const string ExteriorInteractions = "Assets/Hospital/Scenes/Additive/Exterior_Interactions.unity";
        public const string ExteriorShellPreview = "Assets/Hospital/Scenes/Additive/Exterior_InteriorShellPreview.unity";
        public const string GroundEntrancePrefab = "Assets/Hospital/Prefabs/Zones/EXT_GroundEntrance.prefab";
        public const string CollisionPrefab = "Assets/Hospital/Prefabs/Collision/HospitalExterior_R43_CollisionPrototype.prefab";
        public const string InteriorShellPrefab = "Assets/Hospital/Prefabs/Zones/EXT_InteriorShell.prefab";

        public static readonly string[] ProductionScenes = { ExteriorBase, ExteriorTower, ExteriorSides, ExteriorInteractions };
        public static readonly string[] PlayerScenes = { BootstrapScenePath, ExteriorBase, ExteriorTower, ExteriorSides, ExteriorInteractions };
        public static readonly float[] Elevations = { 0f, 5.8f, 9.7f, 13.6f, 17.5f, 21.4f, 25.3f };
        public static readonly string[] FloorIds = { "F00", "F01", "F02", "F03", "F04", "F05", "F06" };

        public static readonly Vector2 UpperBoundsX = new Vector2(-34.7f, 16.7f);
        public static readonly Vector2 UpperBoundsZ = new Vector2(-15.5f, 15.5f);
        public static readonly Vector2 ElevatorBoundsX = new Vector2(0f, 6f);
        public static readonly Vector2 ElevatorBoundsZ = new Vector2(3f, 5.7f);
        public static readonly Vector3 Spawn = new Vector3(4f, 0.03f, -30f);

        // Union outline measured from the approved low horizontal surfaces:
        // GroundEntrance/MAT_Neutral_Concrete and InteriorShell/L00 rear slab.
        public static readonly Vector2[] F00Outline =
        {
            new Vector2(-35.05f, -22.10f), new Vector2(36.00f, -22.10f),
            new Vector2(36.00f, 12.20f), new Vector2(11.50f, 12.20f),
            new Vector2(11.50f, 16.35f), new Vector2(-27.50f, 16.35f),
            new Vector2(-27.50f, 12.20f), new Vector2(-35.05f, 12.20f),
        };

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = "HospitalInterior.R03.S1.BuildRecord.v1";
            public string status;
            public string unityVersion;
            public string bootstrapScene;
            public string[] productionScenes;
            public string[] playerScenes;
            public string playerExecutable;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public string note;
        }

        [MenuItem("Hospital Interior/R03 S1/Build integration scene", priority = 1)]
        public static void BuildIntegrationScene()
        {
            ValidatePreflight();
            string settingsBefore = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            EnsureFolders();

            Material datumMaterial = CreateDiagnosticMaterial(DatumMaterialPath,
                new Color(0.05f, 0.82f, 1f, 0.22f), true);
            Material cageMaterial = CreateDiagnosticMaterial(CageMaterialPath,
                new Color(1f, 0.48f, 0.06f, 0.88f), false);
            S1DatumContract contract = CreateContract();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HospitalInterior_S1_R03_Root");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;

            S1Bootstrap bootstrap = root.AddComponent<S1Bootstrap>();
            Transform diagnostics = BuildDiagnostics(root.transform, datumMaterial, cageMaterial);
            BuildRig(root.transform, bootstrap, contract, diagnostics, out XROrigin origin,
                out Camera camera, out S1ReviewLocomotion locomotion);
            BuildReviewLight(root.transform);
            bootstrap.Configure(contract, origin, camera, locomotion, diagnostics);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string settingsAfter = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("EditorBuildSettings changed while building S1.");

            WriteBuildRecord("SCENE_BUILT_PENDING_VALIDATION", string.Empty, settingsAfter, true);
            WriteCheckpoint();
            Debug.Log("HOSPITAL_INTERIOR_R03_S1_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildIntegrationSceneBatch()
        {
            try { BuildIntegrationScene(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S1/Build Windows review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildIntegrationScene();
            string settingsBefore = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            string outputDirectory = Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior", "StageI1_R03_S1_AutoGate");
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "HospitalInterior_S1_R03_Review.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S1 Windows player build failed: " + report.summary.result);

            string settingsAfter = Sha256(Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset"));
            bool untouched = string.Equals(settingsBefore, settingsAfter, StringComparison.OrdinalIgnoreCase);
            if (!untouched)
                throw new InvalidOperationException("EditorBuildSettings changed while building the explicit S1 player scene list.");
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", executable, settingsAfter, true);
            Debug.Log("HOSPITAL_INTERIOR_R03_S1_PLAYER_BUILD=PASS; " + executable);
        }

        public static void BuildWindowsReviewPlayerBatch()
        {
            try { BuildWindowsReviewPlayer(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidatePreflight()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S1 requires Unity 6000.3.20f1; found " + Application.unityVersion);
            foreach (string path in ProductionScenes.Concat(new[] { ExteriorShellPreview, GroundEntrancePrefab, CollisionPrefab, InteriorShellPrefab }))
                if (AssetDatabase.LoadMainAssetAtPath(path) == null)
                    throw new FileNotFoundException("Required frozen production asset is missing.", path);
            string master = Path.Combine(WorkspaceRoot(), "ArtSource", "Environment", "Blender", "HospitalExterior",
                "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend");
            if (Sha256(master) != HospitalInteriorS1R03Validator.ExpectedR40Sha256)
                throw new InvalidOperationException("Protected R40 authority checksum changed.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/HospitalInterior/PreProduction/I1", "R03");
            EnsureFolder(Root, "Runtime");
            EnsureFolder(Root, "Editor");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Generated");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Settings");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static Material CreateDiagnosticMaterial(string path, Color color, bool transparent)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader is unavailable.");
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else
            {
                material.shader = shader;
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_ZTest", (float)CompareFunction.Always);
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent + 50;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry + 10;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static S1DatumContract CreateContract()
        {
            S1DatumContract contract = AssetDatabase.LoadAssetAtPath<S1DatumContract>(ContractPath);
            if (contract == null)
            {
                contract = ScriptableObject.CreateInstance<S1DatumContract>();
                AssetDatabase.CreateAsset(contract, ContractPath);
            }

            float f00MinX = F00Outline.Min(point => point.x);
            float f00MaxX = F00Outline.Max(point => point.x);
            float f00MinZ = F00Outline.Min(point => point.y);
            float f00MaxZ = F00Outline.Max(point => point.y);
            var floors = new S1FloorDatumRecord[7];
            for (int i = 0; i < floors.Length; i++)
            {
                bool f00 = i == 0;
                floors[i] = new S1FloorDatumRecord
                {
                    floorId = FloorIds[i],
                    elevationM = Elevations[i],
                    minX = f00 ? f00MinX : UpperBoundsX.x,
                    maxX = f00 ? f00MaxX : UpperBoundsX.y,
                    minZ = f00 ? f00MinZ : UpperBoundsZ.x,
                    maxZ = f00 ? f00MaxZ : UpperBoundsZ.y,
                    footprintSource = f00
                        ? "R40 GroundEntrance low horizontal surfaces + InteriorShell L00 rear slab"
                        : "R40 Exterior_InteriorShellPreview upper slab measurement",
                };
            }
            contract.Configure(ProductionScenes, floors, new S1ElevatorReferenceRecord
            {
                minX = ElevatorBoundsX.x, maxX = ElevatorBoundsX.y,
                minZ = ElevatorBoundsZ.x, maxZ = ElevatorBoundsZ.y,
                minY = 0f, maxY = 28.83f,
            }, Spawn, 1.7f, 2f, 4f);
            EditorUtility.SetDirty(contract);
            return contract;
        }

        private static Transform BuildDiagnostics(Transform parent, Material datumMaterial, Material cageMaterial)
        {
            GameObject root = new GameObject("S1_R03_Diagnostics");
            root.transform.SetParent(parent, false);
            root.AddComponent<S1DiagnosticRoot>();

            GameObject f00 = new GameObject("S1_R03_F00_ProductionDerivedOutline");
            f00.transform.SetParent(root.transform, false);
            f00.AddComponent<S1DatumProxy>().Configure("F00", 0f,
                new Vector2(F00Outline.Min(point => point.x), F00Outline.Max(point => point.x)),
                new Vector2(F00Outline.Min(point => point.y), F00Outline.Max(point => point.y)), true);
            for (int i = 0; i < F00Outline.Length; i++)
            {
                Vector2 start = F00Outline[i];
                Vector2 end = F00Outline[(i + 1) % F00Outline.Length];
                CreateHorizontalBeam($"F00_Outline_{i + 1:00}", f00.transform,
                    new Vector3(start.x, 0.035f, start.y), new Vector3(end.x, 0.035f, end.y), 0.075f, datumMaterial);
            }

            Vector3 upperCenter = new Vector3((UpperBoundsX.x + UpperBoundsX.y) * 0.5f, 0f,
                (UpperBoundsZ.x + UpperBoundsZ.y) * 0.5f);
            Vector3 upperSize = new Vector3(UpperBoundsX.y - UpperBoundsX.x, 0.04f, UpperBoundsZ.y - UpperBoundsZ.x);
            for (int i = 1; i < Elevations.Length; i++)
            {
                GameObject proxy = CreateCube($"S1_R03_{FloorIds[i]}_DatumSlab", root.transform, datumMaterial);
                proxy.transform.localPosition = new Vector3(upperCenter.x, Elevations[i] - 0.02f, upperCenter.z);
                proxy.transform.localScale = upperSize;
                proxy.AddComponent<S1DatumProxy>().Configure(FloorIds[i], Elevations[i], UpperBoundsX, UpperBoundsZ, false);
            }

            GameObject cage = new GameObject("S1_R03_E01_ReferenceCage");
            cage.transform.SetParent(root.transform, false);
            cage.AddComponent<S1ElevatorReference>().Configure(ElevatorBoundsX, ElevatorBoundsZ, new Vector2(0f, 28.83f));
            foreach (float elevation in Elevations)
                CreateRectangleRing(cage.transform, ElevatorBoundsX, ElevatorBoundsZ, elevation + 0.055f, 0.055f, cageMaterial,
                    "E01_Ring_" + elevation.ToString("00.0").Replace('.', '_'));
            foreach (Vector3 corner in new[]
            {
                new Vector3(ElevatorBoundsX.x, 0f, ElevatorBoundsZ.x), new Vector3(ElevatorBoundsX.y, 0f, ElevatorBoundsZ.x),
                new Vector3(ElevatorBoundsX.x, 0f, ElevatorBoundsZ.y), new Vector3(ElevatorBoundsX.y, 0f, ElevatorBoundsZ.y),
            })
                CreateVerticalBeam("E01_Vertical", cage.transform, corner, 28.83f, 0.055f, cageMaterial);
            return root.transform;
        }

        private static void BuildRig(Transform parent, S1Bootstrap bootstrap, S1DatumContract contract,
            Transform diagnostics, out XROrigin origin, out Camera camera, out S1ReviewLocomotion locomotion)
        {
            GameObject rig = new GameObject("S1_R03_XROrigin");
            rig.transform.SetParent(parent, false);
            rig.transform.localPosition = contract.HospitalOnlySpawn;
            CharacterController controller = rig.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.stepOffset = 0.3f;
            controller.slopeLimit = 45f;
            controller.skinWidth = 0.03f;

            origin = rig.AddComponent<XROrigin>();
            GameObject offset = new GameObject("Camera Floor Offset");
            offset.transform.SetParent(rig.transform, false);
            GameObject cameraObject = new GameObject("S1_R03_ReviewCamera");
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, contract.EyeHeightM, 0f);
            camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 300f;
            camera.fieldOfView = 68f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);
            cameraObject.AddComponent<AudioListener>();
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            locomotion = rig.AddComponent<S1ReviewLocomotion>();
            locomotion.Configure(origin, camera, bootstrap, contract.WalkSpeedMps, contract.SprintSpeedMps, 0.12f);
        }

        private static void BuildReviewLight(Transform parent)
        {
            GameObject sun = new GameObject("S1_R03_NeutralReviewSun");
            sun.transform.SetParent(parent, false);
            sun.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.05f;
            light.color = new Color(0.91f, 0.94f, 1f);
            light.shadows = LightShadows.Soft;
        }

        private static GameObject CreateCube(string name, Transform parent, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
            cube.GetComponent<MeshRenderer>().sharedMaterial = material;
            return cube;
        }

        private static void CreateHorizontalBeam(string name, Transform parent, Vector3 start, Vector3 end,
            float width, Material material)
        {
            GameObject beam = CreateCube(name, parent, material);
            Vector3 delta = end - start;
            beam.transform.localPosition = (start + end) * 0.5f;
            beam.transform.localRotation = Quaternion.Euler(0f, -Mathf.Atan2(delta.z, delta.x) * Mathf.Rad2Deg, 0f);
            beam.transform.localScale = new Vector3(delta.magnitude, width, width);
        }

        private static void CreateVerticalBeam(string name, Transform parent, Vector3 bottom, float topY,
            float width, Material material)
        {
            GameObject beam = CreateCube(name, parent, material);
            beam.transform.localPosition = new Vector3(bottom.x, topY * 0.5f, bottom.z);
            beam.transform.localScale = new Vector3(width, topY, width);
        }

        private static void CreateRectangleRing(Transform parent, Vector2 x, Vector2 z, float y,
            float width, Material material, string prefix)
        {
            CreateHorizontalBeam(prefix + "_Front", parent, new Vector3(x.x, y, z.x), new Vector3(x.y, y, z.x), width, material);
            CreateHorizontalBeam(prefix + "_Rear", parent, new Vector3(x.x, y, z.y), new Vector3(x.y, y, z.y), width, material);
            CreateHorizontalBeam(prefix + "_Left", parent, new Vector3(x.x, y, z.x), new Vector3(x.x, y, z.y), width, material);
            CreateHorizontalBeam(prefix + "_Right", parent, new Vector3(x.y, y, z.x), new Vector3(x.y, y, z.y), width, material);
        }

        private static void WriteBuildRecord(string status, string executable, string buildSettingsHash, bool untouched)
        {
            string review = ReviewFolder();
            Directory.CreateDirectory(review);
            File.WriteAllText(Path.Combine(review, "StageI1_R03_S1_BuildRecord.json"), JsonUtility.ToJson(new BuildRecord
            {
                status = status,
                unityVersion = Application.unityVersion,
                bootstrapScene = BootstrapScenePath,
                productionScenes = ProductionScenes,
                playerScenes = PlayerScenes,
                playerExecutable = executable,
                editorBuildSettingsSha256 = buildSettingsHash,
                editorBuildSettingsUntouched = untouched,
                note = "R03 S1 contains diagnostic datums only. S2-S8 and all floor design remain deferred.",
            }, true));
        }

        public static void WriteCheckpoint()
        {
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(), "HospitalInterior_StageS1_R03_Checkpoint.md"),
@"# Hospital Interior R03 Stage S1 Checkpoint

**Active stage:** `S1 — actual-building integration and datum test`  
**Status:** `IMPLEMENTED_PENDING_AUTOMATED_AND_USER_VISUAL_REVIEW`  
**User approval:** not received

## Implemented scope

- One isolated R03 bootstrap scene loads the approved hospital exterior without the site or legacy interior-shell preview.
- Seven non-colliding datum proxies, one production-derived F00 outline, and one E01 reference cage are present.
- The desktop review rig uses 1.7 m eye height, 2/4 m/s movement, RMB look, and Y help.

## Deliberately deferred

S2-S8, independent floor scenes, elevator operation, stairs, rooms, corridors, furniture, decoration, signage, clinical equipment, and any accepted floor design remain unstarted.

## Test scene

`Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S1_R03_Integration_SIM_LOCAL.unity`

S1 becomes authoritative only after its automated gates pass and the user explicitly accepts the four-view evidence set.
");
        }

        public static string ProjectRoot() => Directory.GetParent(Application.dataPath)?.FullName
            ?? throw new InvalidOperationException("Could not resolve the Unity project root.");

        public static string WorkspaceRoot()
        {
            DirectoryInfo project = Directory.GetParent(Application.dataPath)
                ?? throw new InvalidOperationException("Could not resolve the Unity project directory.");
            return project.Parent?.Parent?.FullName
                ?? throw new InvalidOperationException("Could not resolve the workspace root.");
        }

        public static string ReviewFolder() => Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior", "StageI1_R03");

        public static string Sha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var hash = System.Security.Cryptography.SHA256.Create();
            return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
        }
    }
}
