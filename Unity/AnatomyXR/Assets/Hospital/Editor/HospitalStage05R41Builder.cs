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
    public static class HospitalStage05R41Builder
    {
        private const string ExpectedUnity = "6000.3.20f1";
        private const string HospitalRoot = "Assets/Hospital";
        private const string SourceFolder = HospitalRoot + "/SourceFBX/R40";
        private const string MaterialFolder = HospitalRoot + "/Materials";
        private const string ZonePrefabFolder = HospitalRoot + "/Prefabs/Zones";
        private const string DoorPrefabFolder = HospitalRoot + "/Prefabs/Doors";
        private const string SandboxFolder = HospitalRoot + "/Scenes/IntegrationSandbox";
        private const string AdditiveFolder = HospitalRoot + "/Scenes/Additive";
        private const string SettingsFolder = HospitalRoot + "/Settings";
        private const string EditorFolder = HospitalRoot + "/Editor";
        private const string PipelineAssetPath = SettingsFolder + "/Hospital_PCVR_URP.asset";
        private const string RendererAssetPath = SettingsFolder + "/Hospital_PCVR_Renderer.asset";
        private const string SandboxScenePath = SandboxFolder + "/Hospital_R41_IntegrationSandbox.unity";

        private static readonly ZoneSpec[] R41Zones =
        {
            new ZoneSpec("EXT_GlobalStructure", "Hospital_EXT_GlobalStructure.fbx",
                "e9a324bc88a54a702fd3a76c804d16afa696949ef6ba4bb6508589b0b8e1a055",
                5, 26864, 26864, 0, new Vector3(73.199996f, 30.33f, 39.0f)),
            new ZoneSpec("EXT_GroundEntrance", "Hospital_EXT_GroundEntrance.fbx",
                "dc29aa06f1d09ed1c7e767382fcfb39b6821e3f49158952feb193d27a934512c",
                20, 85036, 84822, 214, new Vector3(72.525f, 6.34f, 45.0f)),
            new ZoneSpec("ACT_InteractiveDoors", "Hospital_ACT_InteractiveDoors.fbx",
                "99ed517ff96389940343911076079726d2b146984262469731df175bd2c2f37f",
                44, 6160, 6160, 0, new Vector3(48.435f, 31.768f, 40.416f)),
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

        [MenuItem("Hospital/Stage 5/Build and validate R41")]
        public static void BuildAndValidateR41()
        {
            try
            {
                string workspace = WorkspaceRoot();
                string reviews = Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R41");
                Directory.CreateDirectory(reviews);

                EnsureFolderLayout();
                HospitalStage05XRSetup.Configure();
                RemoveEmptyDuplicateXrFolders();
                ConfigureProject();
                EnsureUrpAssets();

                MaterialSource source = LoadMaterialSource(workspace);
                CopyR41ReferenceRecords(workspace);
                BuildSharedMaterials(source);
                WriteMaterialMap(source, reviews);
                CopyR41Inputs(workspace);
                BuildZonePrefabs();
                BuildSandboxScene(reviews);

                R41GateReport report = ValidateR41(source);
                WriteJson(report, Path.Combine(reviews, "Stage05_R41_UnityGate.json"));
                WriteJson(report, AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_UnityGate.json"));
                WriteProjectBaseline(reviews);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();

                if (!report.pass)
                    throw new BuildFailedException("Stage 5 R41 automatic gate failed. See Stage05_R41_UnityGate.json.");

                Debug.Log("STAGE05_R41_GATE=PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void RemoveEmptyDuplicateXrFolders()
        {
            string[] paths =
            {
                "Assets/XR 1",
                "Assets/XR 2",
                "Assets/XR/Settings 1",
                "Assets/XR/Settings 2",
            };

            foreach (string path in paths)
            {
                if (!AssetDatabase.IsValidFolder(path)) continue;
                string absolutePath = AssetPathToAbsolute(path);
                if (Directory.EnumerateFileSystemEntries(absolutePath).Any())
                    throw new InvalidOperationException("Refusing to remove non-empty duplicate XR folder: " + path);
                if (!AssetDatabase.DeleteAsset(path))
                    throw new InvalidOperationException("Failed to remove empty duplicate XR folder: " + path);
            }
        }

        private static void EnsureFolderLayout()
        {
            string[] folders =
            {
                HospitalRoot, SourceFolder, MaterialFolder, ZonePrefabFolder, DoorPrefabFolder,
                SandboxFolder, AdditiveFolder, SettingsFolder, EditorFolder,
            };
            foreach (string folder in folders)
                Directory.CreateDirectory(AssetPathToAbsolute(folder));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureProject()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(
                BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            PlayerSettings.companyName = "CutMyBodyPlease";
            PlayerSettings.productName = "Anatomy XR Hospital";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.cutmybodyplease.anatomyxr");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetGraphicsAPIs(
                BuildTarget.StandaloneWindows64,
                new[] { GraphicsDeviceType.Direct3D11 });
            QualitySettings.vSyncCount = 0;
            SetLayer(8, "HospitalStatic");
            SetLayer(9, "HospitalGlass");
            SetLayer(10, "HospitalDynamic");
            SetLayer(11, "HospitalCollision");
            SetSerializedProjectInt("activeInputHandler", 1);
        }

        private static void SetLayer(int index, string name)
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = manager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != name)
                throw new InvalidOperationException($"Layer {index} is already assigned to '{layer.stringValue}'.");
            layer.stringValue = name;
            manager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSerializedProjectInt(string propertyName, int value)
        {
            UnityObject settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
                throw new InvalidOperationException($"Project setting '{propertyName}' was not found.");
            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int GetSerializedProjectInt(string propertyName)
        {
            UnityObject settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var serialized = new SerializedObject(settings);
            SerializedProperty property = serialized.FindProperty(propertyName);
            return property == null ? int.MinValue : property.intValue;
        }

        private static void EnsureUrpAssets()
        {
            UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererAssetPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererAssetPath);
            }

            UniversalRenderPipelineAsset pipeline =
                AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.name = "Hospital_PCVR_URP";
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);
            }

            pipeline.renderScale = 1.0f;
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = true;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }

        private static MaterialSource LoadMaterialSource(string workspace)
        {
            string path = Path.Combine(
                workspace, "Reviews", "HospitalExterior", "Stage05_R41", "Stage05_R41_MaterialSource.json");
            MaterialSource source = JsonUtility.FromJson<MaterialSource>(File.ReadAllText(path));
            if (source == null || source.materials == null || source.materials.Length != 30)
                throw new InvalidDataException("The Stage 5 material source must contain exactly 30 materials.");
            return source;
        }

        private static void BuildSharedMaterials(MaterialSource source)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader was not found.");

            foreach (MaterialDefinition definition in source.materials.OrderBy(item => item.name))
            {
                string path = MaterialFolder + "/" + definition.name + ".mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader) { name = definition.name };
                    AssetDatabase.CreateAsset(material, path);
                }
                else
                {
                    material.shader = shader;
                }

                Color baseColor = ToColor(definition.base_color_linear);
                material.SetFloat("_Metallic", Mathf.Clamp01(definition.metallic));
                material.SetFloat("_Smoothness", 1.0f - Mathf.Clamp01(definition.roughness));
                material.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                material.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                material.enableInstancing = true;

                if (definition.role == "glass")
                {
                    baseColor.a = Mathf.Clamp(0.55f - (definition.transmission * 0.42f), 0.14f, 0.42f);
                    ConfigureTransparent(material);
                }
                else
                {
                    baseColor.a = 1.0f;
                    ConfigureOpaque(material);
                }
                material.SetColor("_BaseColor", baseColor);

                if (definition.emission_strength > 0.0f)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(
                        "_EmissionColor",
                        ToColor(definition.emission_color_linear) * definition.emission_strength);
                }
                else
                {
                    material.DisableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.black);
                }
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }

        private static void CopyR41ReferenceRecords(string workspace)
        {
            var records = new Dictionary<string, string>
            {
                {
                    Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input", "Stage05_R41_InputManifest.json"),
                    AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_InputManifest.json")
                },
                {
                    Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R41", "Stage05_R41_MaterialSource.json"),
                    AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_MaterialSource.json")
                },
            };
            foreach (KeyValuePair<string, string> record in records)
                File.Copy(record.Key, record.Value, true);
        }

        private static void ConfigureOpaque(Material material)
        {
            material.SetFloat("_Surface", 0.0f);
            material.SetFloat("_Blend", 0.0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1.0f);
            material.SetOverrideTag("RenderType", "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Geometry;
        }

        private static void ConfigureTransparent(Material material)
        {
            material.SetFloat("_Surface", 1.0f);
            material.SetFloat("_Blend", 0.0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0.0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void WriteMaterialMap(MaterialSource source, string reviews)
        {
            var record = new MaterialMapRecord
            {
                schema = "HospitalExterior.Stage05.R41.MaterialMap.v1",
                authority_sha256 = source.authority_sha256,
                render_pipeline = "Universal Render Pipeline 17.3",
                entries = source.materials.OrderBy(item => item.name).Select(item => new MaterialMapEntry
                {
                    source_material = item.name,
                    unity_asset = MaterialFolder + "/" + item.name + ".mat",
                    role = item.role,
                    shader = item.shader,
                    rendering_mode = item.role == "glass" ? "Transparent Alpha" : "Opaque",
                    texture_ownership = item.texture_ownership,
                    permitted_zones = item.permitted_zones,
                }).ToArray(),
            };
            WriteJson(record, Path.Combine(reviews, "Stage05_R41_MaterialMap.json"));
            WriteJson(record, AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_MaterialMap.json"));
        }

        private static void CopyR41Inputs(string workspace)
        {
            string inputFolder = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input");
            foreach (ZoneSpec zone in R41Zones)
            {
                string source = Path.Combine(inputFolder, zone.file);
                string destinationAsset = SourceFolder + "/" + zone.file;
                string destination = AssetPathToAbsolute(destinationAsset);
                if (File.Exists(destination) && Sha256(destination) != zone.sha256)
                    throw new InvalidOperationException($"Refusing to overwrite changed Unity source input: {destination}");
                if (!File.Exists(destination))
                    File.Copy(source, destination, false);
                if (Sha256(destination) != zone.sha256)
                    throw new InvalidDataException($"Input checksum mismatch after copying {zone.file}.");
            }

            string[] imported = Directory.GetFiles(AssetPathToAbsolute(SourceFolder), "*.fbx", SearchOption.TopDirectoryOnly);
            if (imported.Length != R41Zones.Length)
                throw new InvalidOperationException("R41 must import exactly three FBX files.");

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            foreach (ZoneSpec zone in R41Zones)
                AssetDatabase.ImportAsset(SourceFolder + "/" + zone.file,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void BuildZonePrefabs()
        {
            foreach (ZoneSpec zone in R41Zones)
            {
                string sourcePath = SourceFolder + "/" + zone.file;
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (source == null)
                    throw new InvalidOperationException($"Imported model is missing: {sourcePath}");

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

        private static void BuildSandboxScene(string reviews)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roots = new Dictionary<string, GameObject>();
            foreach (ZoneSpec zone in R41Zones)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    ZonePrefabFolder + "/" + zone.zone + ".prefab");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = zone.zone;
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                roots.Add(zone.zone, instance);
            }

            RenderQaImage(
                Path.Combine(reviews, "Stage05_R41_Unity_EntranceProof.png"),
                CombinedBounds(roots.Values),
                new Vector3(-0.18f, 0.10f, -1.0f),
                null);
            RenderQaImage(
                Path.Combine(reviews, "Stage05_R41_Unity_PlayerEye.png"),
                RendererBounds(roots["EXT_GroundEntrance"]),
                new Vector3(-0.20f, 0.0f, -1.0f),
                1.7f);

            EditorSceneManager.SaveScene(scene, SandboxScenePath);
        }

        private static void RenderQaImage(string path, Bounds bounds, Vector3 viewDirection, float? eyeHeight)
        {
            GameObject rig = new GameObject("R41_TEMP_QA_RIG");
            GameObject cameraObject = new GameObject("R41_TEMP_QA_CAMERA");
            GameObject lightObject = new GameObject("R41_TEMP_QA_LIGHT");
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
                camera.farClipPlane = 400.0f;
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
                    UnityObject.DestroyImmediate(target);
                    UnityObject.DestroyImmediate(texture);
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(rig);
            }
        }

        private static R41GateReport ValidateR41(MaterialSource source)
        {
            var report = new R41GateReport
            {
                schema = "HospitalExterior.Stage05.R41.UnityGate.v1",
                unity_editor = Application.unityVersion,
                platform = "Windows x86_64 PC VR",
                xr_runtime = "OpenXR",
                headset_scope = "Meta Quest 2 / Quest 3 via Quest Link or Air Link; Android standalone excluded",
                acceptance_refresh_hz = 72,
                acceptance_frame_budget_ms = 13.89f,
                preferred_refresh_hz = 90,
                preferred_frame_budget_ms = 11.11f,
                checks = new List<GateCheck>(),
                zones = new List<ZoneMeasurement>(),
            };

            AddCheck(report, "unity_version", Application.unityVersion == ExpectedUnity,
                $"expected={ExpectedUnity}; actual={Application.unityVersion}");
            AddCheck(report, "build_target", EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64,
                EditorUserBuildSettings.activeBuildTarget.ToString());
            int activeInputHandler = GetSerializedProjectInt("activeInputHandler");
            AddCheck(report, "input_system_backend", activeInputHandler == 1,
                "activeInputHandler=" + activeInputHandler);
            AddCheck(report, "urp_active", GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,
                GraphicsSettings.defaultRenderPipeline == null ? "none" : GraphicsSettings.defaultRenderPipeline.name);
            bool xrValid = HospitalStage05XRSetup.IsValid(out string xrDetail);
            AddCheck(report, "windows_openxr_quest_profiles", xrValid, xrDetail);
            string[] duplicateXrFolders =
            {
                "Assets/XR 1",
                "Assets/XR 2",
                "Assets/XR/Settings 1",
                "Assets/XR/Settings 2",
            };
            string[] existingDuplicateXrFolders = duplicateXrFolders
                .Where(path => AssetDatabase.IsValidFolder(path)).ToArray();
            AddCheck(report, "no_duplicate_xr_settings_folders", existingDuplicateXrFolders.Length == 0,
                existingDuplicateXrFolders.Length == 0
                    ? "none"
                    : string.Join(",", existingDuplicateXrFolders));
            AddPackageChecks(report);

            string workspace = WorkspaceRoot();
            string externalInputManifest = Path.Combine(
                workspace, "Exports", "HospitalExterior", "Stage05_R41_Input", "Stage05_R41_InputManifest.json");
            string projectInputManifest = AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_InputManifest.json");
            string externalMaterialSource = Path.Combine(
                workspace, "Reviews", "HospitalExterior", "Stage05_R41", "Stage05_R41_MaterialSource.json");
            string projectMaterialSource = AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_MaterialSource.json");
            AddCheck(report, "project_input_manifest_copy",
                File.Exists(projectInputManifest) && Sha256(projectInputManifest) == Sha256(externalInputManifest),
                File.Exists(projectInputManifest) ? Sha256(projectInputManifest) : "missing");
            AddCheck(report, "project_material_source_copy",
                File.Exists(projectMaterialSource) && Sha256(projectMaterialSource) == Sha256(externalMaterialSource),
                File.Exists(projectMaterialSource) ? Sha256(projectMaterialSource) : "missing");

            string[] sourceFiles = Directory.GetFiles(AssetPathToAbsolute(SourceFolder), "*.fbx");
            AddCheck(report, "only_three_r41_fbx_imported", sourceFiles.Length == 3,
                string.Join(",", sourceFiles.Select(Path.GetFileName).OrderBy(value => value)));

            int totalRenderers = 0;
            long totalTriangles = 0;
            int totalCameras = 0;
            int totalLights = 0;
            int totalColliders = 0;
            int meshesMissingNormals = 0;
            int meshesMissingUv0 = 0;
            var usedMaterials = new HashSet<Material>();
            var doorRoots = new HashSet<string>();

            foreach (ZoneSpec zone in R41Zones)
            {
                string assetPath = SourceFolder + "/" + zone.file;
                string absolutePath = AssetPathToAbsolute(assetPath);
                string sha = Sha256(absolutePath);
                AddCheck(report, zone.zone + "_checksum", sha == zone.sha256, sha);

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                bool importerPass = importer != null && !importer.importCameras && !importer.importLights &&
                                    !importer.addCollider && !importer.importAnimation && importer.globalScale == 1.0f &&
                                    importer.bakeAxisConversion;
                AddCheck(report, zone.zone + "_import_policy", importerPass,
                    importer == null ? "missing importer" :
                    $"scale={importer.globalScale}; cameras={importer.importCameras}; lights={importer.importLights}; colliders={importer.addCollider}; animation={importer.importAnimation}; bakedAxis={importer.bakeAxisConversion}");

                Material[] embedded = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Material>().ToArray();
                AddCheck(report, zone.zone + "_no_embedded_material_assets", embedded.Length == 0,
                    "embedded=" + embedded.Length);

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone.zone + ".prefab");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                    long triangles = 0;
                    foreach (Renderer renderer in renderers)
                    {
                        foreach (Material material in renderer.sharedMaterials)
                            if (material != null) usedMaterials.Add(material);
                        Mesh mesh = MeshForRenderer(renderer);
                        if (mesh == null) continue;
                        triangles += TriangleCount(mesh);
                        if (!mesh.HasVertexAttribute(VertexAttribute.Normal)) meshesMissingNormals++;
                        if (!mesh.HasVertexAttribute(VertexAttribute.TexCoord0)) meshesMissingUv0++;
                    }

                    totalRenderers += renderers.Length;
                    totalTriangles += triangles;
                    totalCameras += instance.GetComponentsInChildren<Camera>(true).Length;
                    totalLights += instance.GetComponentsInChildren<Light>(true).Length;
                    totalColliders += instance.GetComponentsInChildren<Collider>(true).Length;
                    if (zone.zone == "ACT_InteractiveDoors")
                    {
                        foreach (Transform transform in instance.GetComponentsInChildren<Transform>(true))
                            if (transform.name.EndsWith("_ROOT", StringComparison.Ordinal)) doorRoots.Add(transform.name);
                    }

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
                        bounds_center = VectorValues(bounds.center),
                        bounds_size = VectorValues(bounds.size),
                        expected_bounds_size = VectorValues(zone.expectedUnitySize),
                        common_origin_transform = transformPass,
                    });
                    AddCheck(report, zone.zone + "_renderer_count", renderers.Length == zone.renderers,
                        $"expected={zone.renderers}; actual={renderers.Length}");
                    AddCheck(report, zone.zone + "_triangle_count", triangles == zone.expectedUnityTriangles,
                        $"source={zone.sourceTriangles}; zero_area_discarded={zone.degenerateTrianglesDiscarded}; " +
                        $"expected_unity={zone.expectedUnityTriangles}; actual={triangles}");
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

            AddCheck(report, "r41_renderer_allowance", totalRenderers == 69 && totalRenderers <= 255,
                $"actual={totalRenderers}; expected=69; maximum=255");
            AddCheck(report, "r41_triangle_baseline", totalTriangles == 117846 && totalTriangles <= 118060,
                $"source=118060; zero_area_discarded=214; expected_unity=117846; actual={totalTriangles}");
            AddCheck(report, "normals_present", meshesMissingNormals == 0, "missing=" + meshesMissingNormals);
            AddCheck(report, "uv0_present", meshesMissingUv0 == 0, "missing=" + meshesMissingUv0);
            AddCheck(report, "no_camera_light_collider_payload",
                totalCameras == 0 && totalLights == 0 && totalColliders == 0,
                $"cameras={totalCameras}; lights={totalLights}; colliders={totalColliders}");

            string[] actualDoorRoots = doorRoots.OrderBy(value => value).ToArray();
            bool doorsPass = actualDoorRoots.SequenceEqual(ExpectedDoorRoots.OrderBy(value => value));
            AddCheck(report, "twelve_door_roots", doorsPass,
                $"actual={actualDoorRoots.Length}; names={string.Join(",", actualDoorRoots)}");
            report.door_roots = actualDoorRoots;

            string[] badMaterialPaths = usedMaterials
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => !path.StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                .OrderBy(path => path).ToArray();
            int libraryCount = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat").Length;
            AddCheck(report, "shared_material_library", badMaterialPaths.Length == 0 && libraryCount == 30,
                $"library_assets={libraryCount}; used_unique={usedMaterials.Count}; external_or_embedded={string.Join(",", badMaterialPaths)}");

            Scene sandbox = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameObject[] roots = sandbox.GetRootGameObjects();
            bool scenePass = roots.Length == 3 && roots.Select(root => root.name).OrderBy(value => value)
                .SequenceEqual(R41Zones.Select(zone => zone.zone).OrderBy(value => value)) &&
                roots.All(root => root.GetComponentsInChildren<Camera>(true).Length == 0 &&
                                  root.GetComponentsInChildren<Light>(true).Length == 0 &&
                                  root.GetComponentsInChildren<Collider>(true).Length == 0);
            AddCheck(report, "sandbox_has_only_planned_zone_roots", scenePass,
                string.Join(",", roots.Select(root => root.name)));

            report.renderer_count = totalRenderers;
            report.triangle_count = totalTriangles;
            report.source_triangle_count = 118060;
            report.degenerate_triangles_discarded = 214;
            report.shared_material_assets = libraryCount;
            report.used_material_assets = usedMaterials.Count;
            report.pass = report.checks.All(check => check.pass);
            report.status = report.pass ? "PASS" : "FAIL";
            return report;
        }

        private static void AddPackageChecks(R41GateReport report)
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

        private static void WriteProjectBaseline(string reviews)
        {
            string[] selected =
            {
                "com.unity.render-pipelines.universal", "com.unity.inputsystem", "com.unity.xr.management",
                "com.unity.xr.openxr", "com.unity.xr.interaction.toolkit",
            };
            var packages = PackageManagerPackageInfo.GetAllRegisteredPackages()
                .Where(package => selected.Contains(package.name))
                .OrderBy(package => package.name)
                .Select(package => new PackageRecord { name = package.name, version = package.version })
                .ToArray();
            var baseline = new ProjectBaseline
            {
                schema = "HospitalExterior.Stage05.R41.ProjectBaseline.v1",
                unity_editor = Application.unityVersion,
                build_target = EditorUserBuildSettings.activeBuildTarget.ToString(),
                platform = "Windows x86_64 PC VR",
                render_pipeline = "Universal Render Pipeline",
                xr_runtime = "OpenXR",
                interaction = "XR Interaction Toolkit + Input System",
                target_headsets = new[] { "Meta Quest 2", "Meta Quest 3" },
                connection = new[] { "Quest Link", "Air Link" },
                standalone_android = false,
                acceptance_refresh_hz = 72,
                acceptance_frame_budget_ms = 13.89f,
                preferred_refresh_hz = 90,
                preferred_frame_budget_ms = 11.11f,
                packages = packages,
            };
            WriteJson(baseline, Path.Combine(reviews, "Stage05_R41_ProjectBaseline.json"));
            WriteJson(baseline, AssetPathToAbsolute(SettingsFolder + "/Stage05_R41_ProjectBaseline.json"));
        }

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
            if (renderers.Length == 0)
                throw new InvalidOperationException("No renderers found under " + root.name);
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
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indexCount += (long)mesh.GetIndexCount(subMesh);
            return indexCount / 3L;
        }

        private static Color ToColor(float[] values)
        {
            if (values == null || values.Length < 3) return Color.white;
            return new Color(values[0], values[1], values[2], values.Length > 3 ? values[3] : 1.0f);
        }

        private static float[] VectorValues(Vector3 value) => new[] { value.x, value.y, value.z };
        private static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

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

        private static void AddCheck(R41GateReport report, string name, bool pass, string detail)
        {
            report.checks.Add(new GateCheck { name = name, pass = pass, detail = detail });
        }

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

        [Serializable] private sealed class MaterialSource
        {
            public string authority_sha256;
            public MaterialDefinition[] materials;
        }

        [Serializable] private sealed class MaterialDefinition
        {
            public string name;
            public string role;
            public string shader;
            public float[] base_color_linear;
            public float metallic;
            public float roughness;
            public float transmission;
            public float[] emission_color_linear;
            public float emission_strength;
            public string texture_ownership;
            public string[] permitted_zones;
        }

        [Serializable] private sealed class MaterialMapRecord
        {
            public string schema;
            public string authority_sha256;
            public string render_pipeline;
            public MaterialMapEntry[] entries;
        }

        [Serializable] private sealed class MaterialMapEntry
        {
            public string source_material;
            public string unity_asset;
            public string role;
            public string shader;
            public string rendering_mode;
            public string texture_ownership;
            public string[] permitted_zones;
        }

        [Serializable] private sealed class GateCheck
        {
            public string name;
            public bool pass;
            public string detail;
        }

        [Serializable] private sealed class ZoneMeasurement
        {
            public string zone;
            public int renderers;
            public int expected_renderers;
            public long triangles;
            public long source_triangles;
            public long expected_unity_triangles;
            public int degenerate_triangles_discarded;
            public float[] bounds_center;
            public float[] bounds_size;
            public float[] expected_bounds_size;
            public bool common_origin_transform;
        }

        [Serializable] private sealed class R41GateReport
        {
            public string schema;
            public string status;
            public bool pass;
            public string unity_editor;
            public string platform;
            public string xr_runtime;
            public string headset_scope;
            public int acceptance_refresh_hz;
            public float acceptance_frame_budget_ms;
            public int preferred_refresh_hz;
            public float preferred_frame_budget_ms;
            public int renderer_count;
            public long triangle_count;
            public long source_triangle_count;
            public int degenerate_triangles_discarded;
            public int shared_material_assets;
            public int used_material_assets;
            public string[] door_roots;
            public List<ZoneMeasurement> zones;
            public List<GateCheck> checks;
        }

        [Serializable] private sealed class ProjectBaseline
        {
            public string schema;
            public string unity_editor;
            public string build_target;
            public string platform;
            public string render_pipeline;
            public string xr_runtime;
            public string interaction;
            public string[] target_headsets;
            public string[] connection;
            public bool standalone_android;
            public int acceptance_refresh_hz;
            public float acceptance_frame_budget_ms;
            public int preferred_refresh_hz;
            public float preferred_frame_budget_ms;
            public PackageRecord[] packages;
        }

        [Serializable] private sealed class PackageRecord
        {
            public string name;
            public string version;
        }
    }
}
