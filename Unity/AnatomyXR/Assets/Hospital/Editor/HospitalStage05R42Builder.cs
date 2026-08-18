using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace CutMyBodyPlease.Hospital.Stage05.Editor
{
    /// <summary>
    /// R42 expands the checksum-frozen R41 proof to all ten source zones and five
    /// additive scenes. It never modifies the FBX authorities in place.
    /// </summary>
    public static class HospitalStage05R42Builder
    {
        private const string ExpectedUnity = "6000.3.20f1";
        private const string HospitalRoot = "Assets/Hospital";
        private const string SourceFolder = HospitalRoot + "/SourceFBX/R40";
        private const string MaterialFolder = HospitalRoot + "/Materials";
        private const string ZonePrefabFolder = HospitalRoot + "/Prefabs/Zones";
        private const string SandboxFolder = HospitalRoot + "/Scenes/IntegrationSandbox";
        private const string AdditiveFolder = HospitalRoot + "/Scenes/Additive";
        private const string SettingsFolder = HospitalRoot + "/Settings";
        private const string SandboxScenePath = SandboxFolder + "/Hospital_R42_CompleteIntegrationSandbox.unity";
        private const string MaterialSourceRelative = "Reviews/HospitalExterior/Stage05_R41/Stage05_R41_MaterialSource.json";
        private const string R41GateRelative = "Reviews/HospitalExterior/Stage05_R41/Stage05_R41_UnityGate.json";
        private const string VisualReviewFile = "Stage05_R42_VisualReview.json";

        private static readonly ZoneSpec[] Zones =
        {
            new ZoneSpec("ACT_InteractiveDoors", "Hospital_ACT_InteractiveDoors.fbx",
                "99ed517ff96389940343911076079726d2b146984262469731df175bd2c2f37f",
                44, 6160, 6160, 0, new Vector3(48.435f, 31.768f, 40.416f)),
            new ZoneSpec("EXT_Balconies", "Hospital_EXT_Balconies.fbx",
                "cd5437d668d0aba757c3e503c00412edc3a44f9eeea5e0168af0bcdac00172c7",
                56, 96672, 96672, 0, new Vector3(12.075f, 23.375f, 12.075f)),
            new ZoneSpec("EXT_FrontTower", "Hospital_EXT_FrontTower.fbx",
                "0a919954f7112dcbafae6de6848bb9b1d6352599cbd4696d5e362a81986fb20b",
                10, 132840, 132840, 0, new Vector3(43.4825f, 23.94f, 3.4625f)),
            new ZoneSpec("EXT_GlobalStructure", "Hospital_EXT_GlobalStructure.fbx",
                "e9a324bc88a54a702fd3a76c804d16afa696949ef6ba4bb6508589b0b8e1a055",
                5, 26864, 26864, 0, new Vector3(73.199996f, 30.33f, 39.0f)),
            new ZoneSpec("EXT_GroundEntrance", "Hospital_EXT_GroundEntrance.fbx",
                "dc29aa06f1d09ed1c7e767382fcfb39b6821e3f49158952feb193d27a934512c",
                20, 85036, 84822, 214, new Vector3(72.525f, 6.34f, 45.0f)),
            new ZoneSpec("EXT_InteriorShell", "Hospital_EXT_InteriorShell.fbx",
                "873f89f9519d0ee8795cb45dbc4c64b7c36afd2628fcec7e18ebd4d7717fc3d6",
                48, 12120, 12120, 0, new Vector3(66.48f, 28.70f, 32.45f)),
            new ZoneSpec("EXT_LeftFacadePodium", "Hospital_EXT_LeftFacadePodium.fbx",
                "a3410fb311b7a07d9b72f6c79b0dd65962b10eac03cbab556da3071efe1e800b",
                15, 57924, 57924, 0, new Vector3(0.84f, 28.56f, 33.55f)),
            new ZoneSpec("EXT_RearService", "Hospital_EXT_RearService.fbx",
                "440740db0143f1ec6781bc6f40d5ffc2117ce2c4fe21be86f66527697d93d417",
                15, 165720, 165628, 92, new Vector3(72.45f, 28.68f, 9.18f)),
            new ZoneSpec("EXT_RightFacadeWing", "Hospital_EXT_RightFacadeWing.fbx",
                "2ac4dd9c6d9a98b5e967c0b0b504a4b0973ae980ac76fa6bf14b448ff9b5f356",
                17, 48684, 48382, 302, new Vector3(27.28f, 28.56f, 39.39f)),
            new ZoneSpec("EXT_RoofService", "Hospital_EXT_RoofService.fbx",
                "799334220d700a871f9f23bbb32ddc1b818252cbdfbffdf8411d2e3a89c77287",
                7, 12976, 12920, 56, new Vector3(72.88f, 27.51f, 35.62f)),
        };

        private static readonly AdditiveSceneSpec[] AdditiveScenes =
        {
            new AdditiveSceneSpec("Exterior_Base", "EXT_GlobalStructure", "EXT_GroundEntrance"),
            new AdditiveSceneSpec("Exterior_Tower", "EXT_FrontTower", "EXT_Balconies"),
            new AdditiveSceneSpec("Exterior_SidesService", "EXT_LeftFacadePodium", "EXT_RightFacadeWing", "EXT_RearService", "EXT_RoofService"),
            new AdditiveSceneSpec("Exterior_InteriorShellPreview", "EXT_InteriorShell"),
            new AdditiveSceneSpec("Exterior_Interactions", "ACT_InteractiveDoors"),
        };

        private static readonly string[] ExpectedDoorRoots =
        {
            "UE_S03B_DOOR_INNER_SlidingLeaf_01_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_03_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_04_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_01_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_03_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_04_ROOT",
            "UE_S03C_REAR_FireExit_B_Leaf_ROOT",
            "UE_S03C_REAR_LoadingDoor_Leaf_ROOT",
            "UE_S03C_REAR_ServiceDoor_A_Leaf_ROOT",
            "UE_S03C_ROOF_AccessDoor_Leaf_ROOT",
        };

        private static readonly string[] QaImageFiles =
        {
            "Stage05_R42_Unity_Front.png",
            "Stage05_R42_Unity_Left.png",
            "Stage05_R42_Unity_Right.png",
            "Stage05_R42_Unity_Rear.png",
            "Stage05_R42_Unity_Roof.png",
            "Stage05_R42_Unity_Entrance.png",
            "Stage05_R42_Unity_Aerial.png",
            "Stage05_R42_Unity_PlayerEye.png",
        };

        [MenuItem("Hospital/Stage 5/Build and measure R42")]
        public static void BuildAndMeasureR42()
        {
            try
            {
                string workspace = WorkspaceRoot();
                string reviews = R42Reviews(workspace);
                Directory.CreateDirectory(reviews);
                Directory.CreateDirectory(AssetPathToAbsolute(SourceFolder));
                Directory.CreateDirectory(AssetPathToAbsolute(ZonePrefabFolder));
                Directory.CreateDirectory(AssetPathToAbsolute(SandboxFolder));
                Directory.CreateDirectory(AssetPathToAbsolute(AdditiveFolder));
                Directory.CreateDirectory(AssetPathToAbsolute(SettingsFolder));

                RequireR41Pass(workspace);
                CopyAllFrozenInputs(workspace);
                BuildAllZonePrefabs();
                BuildCompleteSandbox();
                BuildAdditiveScenes();
                WriteSceneLoadMatrix(reviews);
                WriteMaterialUsage(reviews, LoadMaterialSource(workspace));
                RenderQaEvidence(reviews);
                WriteQaEvidenceManifest(reviews);

                R42GateReport report = ValidateR42Internal(workspace, false);
                report.schema = "HospitalExterior.Stage05.R42.TechnicalGate.v1";
                WriteJson(report, Path.Combine(reviews, "Stage05_R42_TechnicalGate.json"));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();

                if (!report.pass)
                    throw new BuildFailedException("Stage 5 R42 technical gate failed. See Stage05_R42_TechnicalGate.json.");
                Debug.Log("STAGE05_R42_TECHNICAL_GATE=PASS; VISUAL_REVIEW_REQUIRED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Hospital/Stage 5/Validate R42 final gate")]
        public static void ValidateR42FinalGate()
        {
            string workspace = WorkspaceRoot();
            string reviews = R42Reviews(workspace);
            R42GateReport report = ValidateR42Internal(workspace, true);
            report.schema = "HospitalExterior.Stage05.R42.UnityGate.v1";
            WriteJson(report, Path.Combine(reviews, "Stage05_R42_UnityGate.json"));
            WriteJson(report, AssetPathToAbsolute(SettingsFolder + "/Stage05_R42_UnityGate.json"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            if (!report.pass)
                throw new BuildFailedException("Stage 5 R42 final gate failed. See Stage05_R42_UnityGate.json.");
            Debug.Log("STAGE05_R42_GATE=PASS");
        }

        private static void RequireR41Pass(string workspace)
        {
            string path = Path.Combine(workspace, R41GateRelative.Replace('/', Path.DirectorySeparatorChar));
            R41Status gate = JsonUtility.FromJson<R41Status>(File.ReadAllText(path));
            if (gate == null || !gate.pass || gate.status != "PASS")
                throw new InvalidOperationException("R42 requires a passing frozen R41 gate.");
        }

        private static MaterialSource LoadMaterialSource(string workspace)
        {
            string path = Path.Combine(workspace, MaterialSourceRelative.Replace('/', Path.DirectorySeparatorChar));
            MaterialSource source = JsonUtility.FromJson<MaterialSource>(File.ReadAllText(path));
            if (source == null || source.materials == null || source.materials.Length != 30)
                throw new InvalidDataException("The frozen Stage 5 material source must contain exactly 30 materials.");
            return source;
        }

        private static void CopyAllFrozenInputs(string workspace)
        {
            string inputFolder = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input");
            foreach (ZoneSpec zone in Zones)
            {
                string source = Path.Combine(inputFolder, zone.file);
                string destination = AssetPathToAbsolute(SourceFolder + "/" + zone.file);
                if (Sha256(source) != zone.sha256)
                    throw new InvalidDataException("Frozen external input checksum mismatch: " + zone.file);
                if (File.Exists(destination) && Sha256(destination) != zone.sha256)
                    throw new InvalidOperationException("Refusing to overwrite changed Unity source input: " + destination);
                if (!File.Exists(destination)) File.Copy(source, destination, false);
                if (Sha256(destination) != zone.sha256)
                    throw new InvalidDataException("Unity input checksum mismatch after copy: " + zone.file);
            }

            string[] imported = Directory.GetFiles(AssetPathToAbsolute(SourceFolder), "*.fbx", SearchOption.TopDirectoryOnly);
            if (imported.Length != Zones.Length)
                throw new InvalidOperationException("R42 requires exactly ten imported FBX files.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            foreach (ZoneSpec zone in Zones)
                AssetDatabase.ImportAsset(SourceFolder + "/" + zone.file,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void BuildAllZonePrefabs()
        {
            foreach (ZoneSpec zone in Zones)
            {
                string sourcePath = SourceFolder + "/" + zone.file;
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null) throw new InvalidOperationException("Imported model is missing: " + sourcePath);

                GameObject wrapper = new GameObject(zone.zone);
                try
                {
                    GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    model.name = Path.GetFileNameWithoutExtension(zone.file);
                    model.transform.SetParent(wrapper.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;
                    wrapper.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                    wrapper.transform.localScale = Vector3.one;
                    PrefabUtility.SaveAsPrefabAsset(wrapper, ZonePrefabFolder + "/" + zone.zone + ".prefab");
                }
                finally
                {
                    UnityObject.DestroyImmediate(wrapper);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static void BuildCompleteSandbox()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (ZoneSpec zone in Zones) InstantiateZone(zone.zone, scene);
            EditorSceneManager.SaveScene(scene, SandboxScenePath);
        }

        private static void BuildAdditiveScenes()
        {
            foreach (AdditiveSceneSpec sceneSpec in AdditiveScenes)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (string zone in sceneSpec.zones) InstantiateZone(zone, scene);
                EditorSceneManager.SaveScene(scene, AdditiveScenePath(sceneSpec.name));
            }
        }

        private static GameObject InstantiateZone(string zone, Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Zone prefab is missing: " + zone);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = zone;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void WriteSceneLoadMatrix(string reviews)
        {
            var record = new SceneLoadMatrix
            {
                schema = "HospitalExterior.Stage05.R42.SceneLoadMatrix.v1",
                scenes = AdditiveScenes.Select(scene => new SceneLoadEntry
                {
                    scene = scene.name,
                    asset = AdditiveScenePath(scene.name),
                    zones = scene.zones,
                    default_exterior_load = scene.name != "Exterior_InteriorShellPreview" && scene.name != "Exterior_Interactions",
                    optional = scene.name == "Exterior_InteriorShellPreview" || scene.name == "Exterior_Interactions",
                }).ToArray(),
            };
            WriteJson(record, Path.Combine(reviews, "Stage05_R42_SceneLoadMatrix.json"));
            WriteJson(record, AssetPathToAbsolute(SettingsFolder + "/Stage05_R42_SceneLoadMatrix.json"));
        }

        private static void WriteMaterialUsage(string reviews, MaterialSource source)
        {
            var use = source.materials.ToDictionary(item => item.name, item => new HashSet<string>());
            foreach (ZoneSpec zone in Zones)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone.zone + ".prefab");
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                    foreach (Material material in renderer.sharedMaterials)
                        if (material != null && use.TryGetValue(material.name, out HashSet<string> zones)) zones.Add(zone.zone);
            }

            var record = new MaterialUsageRecord
            {
                schema = "HospitalExterior.Stage05.R42.MaterialUsage.v1",
                authority_sha256 = source.authority_sha256,
                entries = source.materials.OrderBy(item => item.name).Select(item => new MaterialUsageEntry
                {
                    material = item.name,
                    unity_asset = MaterialFolder + "/" + item.name + ".mat",
                    role = item.role,
                    permitted_zones = item.permitted_zones.OrderBy(value => value).ToArray(),
                    actual_zones = use[item.name].OrderBy(value => value).ToArray(),
                }).ToArray(),
            };
            WriteJson(record, Path.Combine(reviews, "Stage05_R42_MaterialUsage.json"));
            WriteJson(record, AssetPathToAbsolute(SettingsFolder + "/Stage05_R42_MaterialUsage.json"));
        }

        private static void RenderQaEvidence(string reviews)
        {
            Scene scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            Dictionary<string, GameObject> roots = scene.GetRootGameObjects().ToDictionary(root => root.name);
            Bounds full = CombinedBounds(roots.Values);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[0]), full, new Vector3(0.0f, 0.06f, -1.0f), null);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[1]), full, new Vector3(-1.0f, 0.05f, 0.0f), null);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[2]), full, new Vector3(1.0f, 0.05f, 0.0f), null);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[3]), full, new Vector3(0.0f, 0.05f, 1.0f), null);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[4]), full, new Vector3(0.0f, 1.0f, 0.18f), null);
            RenderQaImageAt(Path.Combine(reviews, QaImageFiles[5]),
                new Vector3(0.0f, 6.5f, -38.0f), new Vector3(0.0f, 2.8f, -17.0f), 48.0f);
            RenderQaImage(Path.Combine(reviews, QaImageFiles[6]), full, new Vector3(-0.70f, 0.48f, -1.0f), null);
            RenderQaImageAt(Path.Combine(reviews, QaImageFiles[7]),
                new Vector3(4.0f, 1.7f, -30.0f), new Vector3(0.0f, 2.5f, -17.0f), 60.0f);
        }

        private static void RenderQaImage(string path, Bounds bounds, Vector3 viewDirection, float? eyeHeight)
        {
            GameObject rig = new GameObject("R42_TEMP_QA_RIG");
            GameObject cameraObject = new GameObject("R42_TEMP_QA_CAMERA");
            GameObject lightObject = new GameObject("R42_TEMP_QA_LIGHT");
            cameraObject.transform.SetParent(rig.transform, false);
            lightObject.transform.SetParent(rig.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            Light light = lightObject.AddComponent<Light>();
            try
            {
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1.0f, 0.96f, 0.90f);
                lightObject.transform.rotation = Quaternion.Euler(42.0f, -32.0f, 0.0f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.68f, 0.74f, 0.80f, 1.0f);
                camera.fieldOfView = 50.0f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 500.0f;
                Vector3 direction = viewDirection.normalized;
                float distance = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 1.18f;
                camera.transform.position = bounds.center + direction * distance;
                Vector3 targetPoint = bounds.center + Vector3.up * bounds.extents.y * 0.05f;
                if (eyeHeight.HasValue)
                {
                    Vector3 playerEye = camera.transform.position;
                    playerEye.y = eyeHeight.Value;
                    camera.transform.position = playerEye;
                    targetPoint.y = Mathf.Min(bounds.max.y, eyeHeight.Value + 1.25f);
                }
                camera.transform.LookAt(targetPoint);

                var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    if (!target.Create()) throw new InvalidOperationException("Failed to create R42 QA render target.");
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    target.Release();
                    UnityObject.DestroyImmediate(target);
                    UnityObject.DestroyImmediate(texture);
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(rig);
            }
        }

        private static void RenderQaImageAt(string path, Vector3 cameraPosition, Vector3 targetPoint, float fieldOfView)
        {
            GameObject rig = new GameObject("R42_TEMP_QA_RIG");
            GameObject cameraObject = new GameObject("R42_TEMP_QA_CAMERA");
            GameObject lightObject = new GameObject("R42_TEMP_QA_LIGHT");
            cameraObject.transform.SetParent(rig.transform, false);
            lightObject.transform.SetParent(rig.transform, false);
            Camera camera = cameraObject.AddComponent<Camera>();
            Light light = lightObject.AddComponent<Light>();
            try
            {
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                light.color = new Color(1.0f, 0.96f, 0.90f);
                lightObject.transform.rotation = Quaternion.Euler(42.0f, -32.0f, 0.0f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.68f, 0.74f, 0.80f, 1.0f);
                camera.fieldOfView = fieldOfView;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 500.0f;
                camera.transform.position = cameraPosition;
                camera.transform.LookAt(targetPoint);

                var target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    if (!target.Create()) throw new InvalidOperationException("Failed to create R42 QA render target.");
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = null;
                    RenderTexture.active = previous;
                    target.Release();
                    UnityObject.DestroyImmediate(target);
                    UnityObject.DestroyImmediate(texture);
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(rig);
            }
        }

        private static void WriteQaEvidenceManifest(string reviews)
        {
            var record = new QaEvidenceManifest
            {
                schema = "HospitalExterior.Stage05.R42.QaEvidence.v1",
                images = QaImageFiles.Select(file =>
                {
                    string path = Path.Combine(reviews, file);
                    return new QaEvidenceEntry { file = file, sha256 = Sha256(path), bytes = new FileInfo(path).Length };
                }).ToArray(),
            };
            WriteJson(record, Path.Combine(reviews, "Stage05_R42_QaEvidence.json"));
        }

        private static R42GateReport ValidateR42Internal(string workspace, bool requireVisualReview)
        {
            MaterialSource source = LoadMaterialSource(workspace);
            Dictionary<string, string> materialRoles = source.materials.ToDictionary(item => item.name, item => item.role);
            var report = new R42GateReport
            {
                status = "FAIL",
                unity_editor = Application.unityVersion,
                platform = "Windows x86_64 PC VR",
                headset_scope = "Meta Quest 2 / Quest 3 via Quest Link or Air Link; Android standalone excluded",
                renderer_count = 0,
                source_triangle_count = 644996,
                expected_unity_triangle_count = 644332,
                degenerate_triangles_discarded = 664,
                shared_material_assets = 0,
                used_material_assets = 0,
                zones = new List<ZoneMeasurement>(),
                additive_scenes = new List<AdditiveSceneMeasurement>(),
                checks = new List<GateCheck>(),
            };

            AddCheck(report, "r41_prerequisite", IsR41Pass(workspace), R41GateRelative);
            AddCheck(report, "unity_version", Application.unityVersion == ExpectedUnity,
                $"expected={ExpectedUnity}; actual={Application.unityVersion}");
            AddCheck(report, "build_target", EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64,
                EditorUserBuildSettings.activeBuildTarget.ToString());
            AddCheck(report, "urp_active", GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,
                GraphicsSettings.defaultRenderPipeline == null ? "none" : GraphicsSettings.defaultRenderPipeline.name);
            bool xrValid = HospitalStage05XRSetup.IsValid(out string xrDetail);
            AddCheck(report, "windows_openxr_quest_profiles", xrValid, xrDetail);
            AddPackageChecks(report);

            string[] imported = Directory.GetFiles(AssetPathToAbsolute(SourceFolder), "*.fbx", SearchOption.TopDirectoryOnly);
            AddCheck(report, "exactly_ten_frozen_fbx_imports", imported.Length == 10 &&
                imported.Select(Path.GetFileName).OrderBy(value => value).SequenceEqual(Zones.Select(zone => zone.file).OrderBy(value => value)),
                string.Join(",", imported.Select(Path.GetFileName).OrderBy(value => value)));

            int totalRenderers = 0;
            long totalTriangles = 0;
            int cameras = 0;
            int lights = 0;
            int colliders = 0;
            int meshesMissingNormals = 0;
            int meshesMissingUv0 = 0;
            int nullMaterials = 0;
            int mixedRoleRenderers = 0;
            var usedMaterials = new HashSet<Material>();
            var doorRoots = new HashSet<string>();

            foreach (ZoneSpec zone in Zones)
            {
                string assetPath = SourceFolder + "/" + zone.file;
                string absolutePath = AssetPathToAbsolute(assetPath);
                string sha = Sha256(absolutePath);
                AddCheck(report, zone.zone + "_checksum", sha == zone.sha256, sha);

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                bool importerPass = importer != null && !importer.importCameras && !importer.importLights &&
                                    !importer.addCollider && !importer.importAnimation && importer.globalScale == 1.0f &&
                                    importer.bakeAxisConversion && importer.materialLocation == ModelImporterMaterialLocation.External;
                AddCheck(report, zone.zone + "_import_policy", importerPass,
                    importer == null ? "missing" :
                    $"scale={importer.globalScale}; cameras={importer.importCameras}; lights={importer.importLights}; colliders={importer.addCollider}; animation={importer.importAnimation}; bakedAxis={importer.bakeAxisConversion}; materials={importer.materialLocation}");
                Material[] embedded = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Material>().ToArray();
                AddCheck(report, zone.zone + "_no_embedded_material_assets", embedded.Length == 0, "embedded=" + embedded.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone.zone + ".prefab");
                AddCheck(report, zone.zone + "_prefab_exists", prefab != null, ZonePrefabFolder + "/" + zone.zone + ".prefab");
                if (prefab == null) continue;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    long triangles = 0;
                    foreach (Renderer renderer in renderers)
                    {
                        var roles = new HashSet<string>();
                        foreach (Material material in renderer.sharedMaterials)
                        {
                            if (material == null) { nullMaterials++; continue; }
                            usedMaterials.Add(material);
                            if (materialRoles.TryGetValue(material.name, out string role)) roles.Add(role);
                            else roles.Add("unresolved");
                        }
                        if (roles.Count > 1 || roles.Contains("unresolved")) mixedRoleRenderers++;
                        Mesh mesh = MeshForRenderer(renderer);
                        if (mesh == null) continue;
                        triangles += TriangleCount(mesh);
                        if (!mesh.HasVertexAttribute(VertexAttribute.Normal)) meshesMissingNormals++;
                        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0)) meshesMissingUv0++;
                    }

                    totalRenderers += renderers.Length;
                    totalTriangles += triangles;
                    cameras += instance.GetComponentsInChildren<Camera>(true).Length;
                    lights += instance.GetComponentsInChildren<Light>(true).Length;
                    colliders += instance.GetComponentsInChildren<Collider>(true).Length;
                    if (zone.zone == "ACT_InteractiveDoors")
                        foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
                            if (transform.name.EndsWith("_ROOT", StringComparison.Ordinal)) doorRoots.Add(transform.name);

                    Bounds bounds = RendererBounds(instance);
                    Vector3 delta = Abs(bounds.size - zone.expectedUnitySize);
                    bool sizePass = delta.x <= 0.03f && delta.y <= 0.03f && delta.z <= 0.03f;
                    bool transformPass = instance.transform.position == Vector3.zero &&
                                         instance.transform.rotation == Quaternion.identity &&
                                         instance.transform.localScale == Vector3.one;
                    report.zones.Add(new ZoneMeasurement
                    {
                        zone = zone.zone,
                        renderers = renderers.Length,
                        expected_renderers = zone.renderers,
                        triangles = triangles,
                        source_triangles = zone.sourceTriangles,
                        expected_unity_triangles = zone.expectedUnityTriangles,
                        degenerate_triangles_discarded = zone.degenerateTrianglesDiscarded,
                        bounds_size = VectorValues(bounds.size),
                        expected_bounds_size = VectorValues(zone.expectedUnitySize),
                        common_origin_transform = transformPass,
                    });
                    AddCheck(report, zone.zone + "_renderer_count", renderers.Length == zone.renderers,
                        $"expected={zone.renderers}; actual={renderers.Length}");
                    AddCheck(report, zone.zone + "_triangle_count", triangles == zone.expectedUnityTriangles,
                        $"source={zone.sourceTriangles}; exact_zero_area_discarded={zone.degenerateTrianglesDiscarded}; expected_unity={zone.expectedUnityTriangles}; actual={triangles}");
                    AddCheck(report, zone.zone + "_bounds_size", sizePass,
                        $"expected={zone.expectedUnitySize}; actual={bounds.size}; delta={delta}");
                    AddCheck(report, zone.zone + "_common_origin", transformPass,
                        $"position={instance.transform.position}; rotation={instance.transform.rotation.eulerAngles}; scale={instance.transform.localScale}");
                }
                finally
                {
                    UnityObject.DestroyImmediate(instance);
                }
            }

            AddCheck(report, "complete_renderer_budget", totalRenderers == 237 && totalRenderers <= 255,
                $"expected=237; actual={totalRenderers}; maximum=255");
            AddCheck(report, "complete_triangle_baseline", totalTriangles == 644332 && totalTriangles <= 644996,
                $"source=644996; exact_zero_area_discarded=664; expected_unity=644332; actual={totalTriangles}");
            AddCheck(report, "normals_present", meshesMissingNormals == 0, "missing=" + meshesMissingNormals);
            AddCheck(report, "uv0_present", meshesMissingUv0 == 0, "missing=" + meshesMissingUv0);
            AddCheck(report, "no_camera_light_collider_payload", cameras == 0 && lights == 0 && colliders == 0,
                $"cameras={cameras}; lights={lights}; colliders={colliders}");
            AddCheck(report, "material_roles_separated", nullMaterials == 0 && mixedRoleRenderers == 0,
                $"null_assignments={nullMaterials}; mixed_or_unresolved_renderers={mixedRoleRenderers}");

            string[] badMaterialPaths = usedMaterials.Select(AssetDatabase.GetAssetPath)
                .Where(path => !path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal)).OrderBy(path => path).ToArray();
            int materialCount = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat").Length;
            AddCheck(report, "complete_shared_material_library",
                materialCount == 30 && usedMaterials.Count == 30 && badMaterialPaths.Length == 0,
                $"library={materialCount}; used={usedMaterials.Count}; external_or_embedded={string.Join(",", badMaterialPaths)}");
            ValidateMaterialUsage(report, workspace, source);

            string[] actualDoors = doorRoots.OrderBy(value => value).ToArray();
            AddCheck(report, "twelve_door_roots", actualDoors.SequenceEqual(ExpectedDoorRoots.OrderBy(value => value)),
                $"actual={actualDoors.Length}; names={string.Join(",", actualDoors)}");
            report.door_roots = actualDoors;

            ValidateSandbox(report);
            ValidateAdditiveScenes(report);
            if (requireVisualReview) ValidateVisualReview(report, workspace);

            report.renderer_count = totalRenderers;
            report.triangle_count = totalTriangles;
            report.shared_material_assets = materialCount;
            report.used_material_assets = usedMaterials.Count;
            report.pass = report.checks.All(check => check.pass);
            report.status = report.pass ? "PASS" : "FAIL";
            return report;
        }

        private static void ValidateMaterialUsage(R42GateReport report, string workspace, MaterialSource source)
        {
            string path = Path.Combine(R42Reviews(workspace), "Stage05_R42_MaterialUsage.json");
            MaterialUsageRecord record = File.Exists(path)
                ? JsonUtility.FromJson<MaterialUsageRecord>(File.ReadAllText(path)) : null;
            bool valid = record != null && record.entries != null && record.entries.Length == 30;
            var failures = new List<string>();
            if (valid)
            {
                foreach (MaterialUsageEntry entry in record.entries)
                {
                    if (entry.actual_zones == null || entry.actual_zones.Length == 0) failures.Add(entry.material + ":unused");
                    else if (entry.actual_zones.Except(entry.permitted_zones ?? Array.Empty<string>()).Any())
                        failures.Add(entry.material + ":out_of_scope");
                }
            }
            AddCheck(report, "material_usage_map_resolved", valid && failures.Count == 0,
                valid ? (failures.Count == 0 ? "30/30 used within permitted zones" : string.Join(",", failures)) : "missing or invalid record");
        }

        private static void ValidateSandbox(R42GateReport report)
        {
            Scene scene = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            bool names = roots.Select(root => root.name).OrderBy(value => value)
                .SequenceEqual(Zones.Select(zone => zone.zone).OrderBy(value => value));
            bool transforms = roots.All(ZeroTransform);
            bool connected = roots.All(root => PrefabUtility.GetPrefabInstanceStatus(root) == PrefabInstanceStatus.Connected);
            bool payload = roots.All(root => root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                                             root.GetComponentsInChildren<Light>(true).Length == 0 &&
                                             root.GetComponentsInChildren<Collider>(true).Length == 0);
            AddCheck(report, "complete_sandbox_exact_zone_roots",
                roots.Length == 10 && names && transforms && connected && payload,
                $"roots={string.Join(",", roots.Select(root => root.name))}; zero={transforms}; connected={connected}; clean_payload={payload}");
        }

        private static void ValidateAdditiveScenes(R42GateReport report)
        {
            var allZones = new List<string>();
            bool allPass = true;
            foreach (AdditiveSceneSpec expected in AdditiveScenes)
            {
                string path = AdditiveScenePath(expected.name);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                GameObject[] roots = scene.GetRootGameObjects();
                string[] names = roots.Select(root => root.name).OrderBy(value => value).ToArray();
                bool exact = names.SequenceEqual(expected.zones.OrderBy(value => value));
                bool zero = roots.All(ZeroTransform);
                bool connected = roots.All(root => PrefabUtility.GetPrefabInstanceStatus(root) == PrefabInstanceStatus.Connected);
                bool clean = roots.All(root => root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                                               root.GetComponentsInChildren<Light>(true).Length == 0 &&
                                               root.GetComponentsInChildren<Collider>(true).Length == 0);
                bool pass = exact && zero && connected && clean;
                allPass &= pass;
                allZones.AddRange(names);
                report.additive_scenes.Add(new AdditiveSceneMeasurement
                {
                    scene = expected.name,
                    asset = path,
                    zones = names,
                    pass = pass,
                    zero_transforms = zero,
                    connected_prefabs = connected,
                    clean_payload = clean,
                });
            }
            bool exactlyOnce = allZones.Count == 10 && allZones.Distinct().Count() == 10 &&
                               allZones.OrderBy(value => value).SequenceEqual(Zones.Select(zone => zone.zone).OrderBy(value => value));
            AddCheck(report, "five_additive_scenes_exact_zone_matrix", allPass && exactlyOnce,
                $"scenes={AdditiveScenes.Length}; zone_instances={allZones.Count}; distinct={allZones.Distinct().Count()}");
            AddCheck(report, "interior_shell_independently_loadable",
                AdditiveScenes.Single(scene => scene.name == "Exterior_InteriorShellPreview").zones.SequenceEqual(new[] { "EXT_InteriorShell" }) &&
                AdditiveScenes.Where(scene => scene.name != "Exterior_InteriorShellPreview").All(scene => !scene.zones.Contains("EXT_InteriorShell")),
                "Exterior_InteriorShellPreview contains only EXT_InteriorShell");
        }

        private static void ValidateVisualReview(R42GateReport report, string workspace)
        {
            string reviews = R42Reviews(workspace);
            string path = Path.Combine(reviews, VisualReviewFile);
            VisualReviewRecord review = File.Exists(path)
                ? JsonUtility.FromJson<VisualReviewRecord>(File.ReadAllText(path)) : null;
            bool valid = review != null && review.status == "PASS" && review.checks != null &&
                         review.checks.Length > 0 && review.checks.All(check => check.pass) &&
                         review.images != null && review.images.Length == QaImageFiles.Length;
            var details = new List<string>();
            if (valid)
            {
                foreach (string file in QaImageFiles)
                {
                    VisualReviewImage entry = review.images.SingleOrDefault(item => item.file == file);
                    string imagePath = Path.Combine(reviews, file);
                    if (entry == null || !File.Exists(imagePath) || entry.sha256 != Sha256(imagePath))
                    {
                        valid = false;
                        details.Add(file + ":checksum_mismatch");
                    }
                }
            }
            AddCheck(report, "full_exterior_visual_comparison", valid,
                valid ? "eight checksum-bound R42 views reviewed PASS" :
                (File.Exists(path) ? string.Join(",", details.DefaultIfEmpty("invalid review record")) : "visual review record missing"));
        }

        private static void AddPackageChecks(R42GateReport report)
        {
            var expected = new Dictionary<string, string>
            {
                { "com.unity.render-pipelines.universal", "17.3.0" },
                { "com.unity.inputsystem", "1.20.0" },
                { "com.unity.xr.management", "4.7.0" },
                { "com.unity.xr.openxr", "1.17.1" },
                { "com.unity.xr.interaction.toolkit", "3.5.1" },
            };
            Dictionary<string, string> actual = PackageManagerPackageInfo.GetAllRegisteredPackages()
                .ToDictionary(package => package.name, package => package.version);
            foreach (KeyValuePair<string, string> pair in expected)
            {
                actual.TryGetValue(pair.Key, out string version);
                AddCheck(report, "package_" + pair.Key, version == pair.Value,
                    $"expected={pair.Value}; actual={version ?? "missing"}");
            }
        }

        private static bool IsR41Pass(string workspace)
        {
            string path = Path.Combine(workspace, R41GateRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return false;
            R41Status gate = JsonUtility.FromJson<R41Status>(File.ReadAllText(path));
            return gate != null && gate.pass && gate.status == "PASS";
        }

        private static bool ZeroTransform(GameObject root) =>
            root.transform.position == Vector3.zero && root.transform.rotation == Quaternion.identity &&
            root.transform.localScale == Vector3.one;

        private static string AdditiveScenePath(string name) => AdditiveFolder + "/" + name + ".unity";
        private static string R42Reviews(string workspace) => Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R42");

        private static Bounds CombinedBounds(IEnumerable<GameObject> roots)
        {
            bool initialized = false;
            Bounds result = default;
            foreach (GameObject root in roots)
            {
                Bounds bounds = RendererBounds(root);
                if (!initialized) { result = bounds; initialized = true; }
                else result.Encapsulate(bounds);
            }
            if (!initialized) throw new InvalidOperationException("No renderer bounds were found.");
            return result;
        }

        private static Bounds RendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("No renderers found under " + root.name);
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static Mesh MeshForRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? null : filter.sharedMesh;
        }

        private static long TriangleCount(Mesh mesh)
        {
            long indexCount = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++) indexCount += (long)mesh.GetIndexCount(subMesh);
            return indexCount / 3L;
        }

        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        private static float[] VectorValues(Vector3 value) => new[] { value.x, value.y, value.z };

        private static string WorkspaceRoot()
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetFullPath(Path.Combine(project, "..", ".."));
        }

        private static string AssetPathToAbsolute(string assetPath)
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(project, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path))
            using (var hash = SHA256.Create())
                return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void AddCheck(R42GateReport report, string name, bool pass, string detail) =>
            report.checks.Add(new GateCheck { name = name, pass = pass, detail = detail });

        private static void WriteJson(object value, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + Environment.NewLine);
        }

        private sealed class ZoneSpec
        {
            public readonly string zone;
            public readonly string file;
            public readonly string sha256;
            public readonly int renderers;
            public readonly long sourceTriangles;
            public readonly long expectedUnityTriangles;
            public readonly int degenerateTrianglesDiscarded;
            public readonly Vector3 expectedUnitySize;

            public ZoneSpec(string zone, string file, string sha256, int renderers, long sourceTriangles,
                long expectedUnityTriangles, int degenerateTrianglesDiscarded, Vector3 expectedUnitySize)
            {
                this.zone = zone;
                this.file = file;
                this.sha256 = sha256;
                this.renderers = renderers;
                this.sourceTriangles = sourceTriangles;
                this.expectedUnityTriangles = expectedUnityTriangles;
                this.degenerateTrianglesDiscarded = degenerateTrianglesDiscarded;
                this.expectedUnitySize = expectedUnitySize;
            }
        }

        private sealed class AdditiveSceneSpec
        {
            public readonly string name;
            public readonly string[] zones;
            public AdditiveSceneSpec(string name, params string[] zones) { this.name = name; this.zones = zones; }
        }

        [Serializable] private sealed class R41Status { public string status; public bool pass; }
        [Serializable] private sealed class MaterialSource { public string authority_sha256; public MaterialDefinition[] materials; }
        [Serializable] private sealed class MaterialDefinition { public string name; public string role; public string[] permitted_zones; }
        [Serializable] private sealed class GateCheck { public string name; public bool pass; public string detail; }
        [Serializable] private sealed class ZoneMeasurement
        {
            public string zone; public int renderers; public int expected_renderers; public long triangles;
            public long source_triangles; public long expected_unity_triangles; public int degenerate_triangles_discarded;
            public float[] bounds_size; public float[] expected_bounds_size; public bool common_origin_transform;
        }
        [Serializable] private sealed class AdditiveSceneMeasurement
        {
            public string scene; public string asset; public string[] zones; public bool pass;
            public bool zero_transforms; public bool connected_prefabs; public bool clean_payload;
        }
        [Serializable] private sealed class R42GateReport
        {
            public string schema; public string status; public bool pass; public string unity_editor; public string platform;
            public string headset_scope; public int renderer_count; public long triangle_count; public long source_triangle_count;
            public long expected_unity_triangle_count; public int degenerate_triangles_discarded;
            public int shared_material_assets; public int used_material_assets; public string[] door_roots;
            public List<ZoneMeasurement> zones; public List<AdditiveSceneMeasurement> additive_scenes; public List<GateCheck> checks;
        }
        [Serializable] private sealed class SceneLoadMatrix { public string schema; public SceneLoadEntry[] scenes; }
        [Serializable] private sealed class SceneLoadEntry
        {
            public string scene; public string asset; public string[] zones; public bool default_exterior_load; public bool optional;
        }
        [Serializable] private sealed class MaterialUsageRecord
        {
            public string schema; public string authority_sha256; public MaterialUsageEntry[] entries;
        }
        [Serializable] private sealed class MaterialUsageEntry
        {
            public string material; public string unity_asset; public string role; public string[] permitted_zones; public string[] actual_zones;
        }
        [Serializable] private sealed class QaEvidenceManifest { public string schema; public QaEvidenceEntry[] images; }
        [Serializable] private sealed class QaEvidenceEntry { public string file; public string sha256; public long bytes; }
        [Serializable] private sealed class VisualReviewRecord
        {
            public string schema; public string status; public VisualReviewCheck[] checks; public VisualReviewImage[] images;
        }
        [Serializable] private sealed class VisualReviewCheck { public string name; public bool pass; public string detail; }
        [Serializable] private sealed class VisualReviewImage { public string file; public string sha256; }
    }
}
