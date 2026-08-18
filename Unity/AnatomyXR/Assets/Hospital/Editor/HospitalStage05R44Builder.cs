using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CutMyBodyPlease.Hospital.Stage05;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using PackageManagerPackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace CutMyBodyPlease.Hospital.Stage05.Editor
{
    /// <summary>R44 clean-load QA, evidence generation and Stage 5 freeze.</summary>
    public static class HospitalStage05R44Builder
    {
        private const string ExpectedUnity = "6000.3.20f1";
        private const string HospitalRoot = "Assets/Hospital";
        private const string SourceFolder = HospitalRoot + "/SourceFBX/R40";
        private const string MaterialFolder = HospitalRoot + "/Materials";
        private const string ZonePrefabFolder = HospitalRoot + "/Prefabs/Zones";
        private const string DoorPrefabFolder = HospitalRoot + "/Prefabs/Doors";
        private const string CollisionPrefabPath = HospitalRoot + "/Prefabs/Collision/HospitalExterior_R43_CollisionPrototype.prefab";
        private const string AdditiveFolder = HospitalRoot + "/Scenes/Additive";
        private const string SandboxFolder = HospitalRoot + "/Scenes/IntegrationSandbox";
        private const string SettingsFolder = HospitalRoot + "/Settings";
        private const string R43GateRelative = "Reviews/HospitalExterior/Stage05_R43/Stage05_R43_UnityGate.json";
        private const string VisualReviewFile = "Stage05_R44_VisualReview.json";
        private const string AuthorityRelative = "ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend";
        private const string AuthoritySha = "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58";

        private static readonly ZoneSpec[] Zones =
        {
            new ZoneSpec("ACT_InteractiveDoors", "Hospital_ACT_InteractiveDoors.fbx", "99ed517ff96389940343911076079726d2b146984262469731df175bd2c2f37f", 44, 6160),
            new ZoneSpec("EXT_Balconies", "Hospital_EXT_Balconies.fbx", "cd5437d668d0aba757c3e503c00412edc3a44f9eeea5e0168af0bcdac00172c7", 56, 96672),
            new ZoneSpec("EXT_FrontTower", "Hospital_EXT_FrontTower.fbx", "0a919954f7112dcbafae6de6848bb9b1d6352599cbd4696d5e362a81986fb20b", 10, 132840),
            new ZoneSpec("EXT_GlobalStructure", "Hospital_EXT_GlobalStructure.fbx", "e9a324bc88a54a702fd3a76c804d16afa696949ef6ba4bb6508589b0b8e1a055", 5, 26864),
            new ZoneSpec("EXT_GroundEntrance", "Hospital_EXT_GroundEntrance.fbx", "dc29aa06f1d09ed1c7e767382fcfb39b6821e3f49158952feb193d27a934512c", 20, 84822),
            new ZoneSpec("EXT_InteriorShell", "Hospital_EXT_InteriorShell.fbx", "873f89f9519d0ee8795cb45dbc4c64b7c36afd2628fcec7e18ebd4d7717fc3d6", 48, 12120),
            new ZoneSpec("EXT_LeftFacadePodium", "Hospital_EXT_LeftFacadePodium.fbx", "a3410fb311b7a07d9b72f6c79b0dd65962b10eac03cbab556da3071efe1e800b", 15, 57924),
            new ZoneSpec("EXT_RearService", "Hospital_EXT_RearService.fbx", "440740db0143f1ec6781bc6f40d5ffc2117ce2c4fe21be86f66527697d93d417", 15, 165628),
            new ZoneSpec("EXT_RightFacadeWing", "Hospital_EXT_RightFacadeWing.fbx", "2ac4dd9c6d9a98b5e967c0b0b504a4b0973ae980ac76fa6bf14b448ff9b5f356", 17, 48382),
            new ZoneSpec("EXT_RoofService", "Hospital_EXT_RoofService.fbx", "799334220d700a871f9f23bbb32ddc1b818252cbdfbffdf8411d2e3a89c77287", 7, 12920),
        };

        private static readonly string[] DoorRoots =
        {
            "UE_S03B_DOOR_INNER_SlidingLeaf_01_ROOT", "UE_S03B_DOOR_INNER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_03_ROOT", "UE_S03B_DOOR_INNER_SlidingLeaf_04_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_01_ROOT", "UE_S03B_DOOR_OUTER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_03_ROOT", "UE_S03B_DOOR_OUTER_SlidingLeaf_04_ROOT",
            "UE_S03C_REAR_FireExit_B_Leaf_ROOT", "UE_S03C_REAR_LoadingDoor_Leaf_ROOT",
            "UE_S03C_REAR_ServiceDoor_A_Leaf_ROOT", "UE_S03C_ROOF_AccessDoor_Leaf_ROOT",
        };

        private static readonly SceneSpec[] Scenes =
        {
            new SceneSpec("Exterior_Base", 25, 111686, 28, "EXT_GlobalStructure", "EXT_GroundEntrance", "R43_CollisionPrototype"),
            new SceneSpec("Exterior_Tower", 66, 229512, 0, "EXT_FrontTower", "EXT_Balconies"),
            new SceneSpec("Exterior_SidesService", 54, 284854, 0, "EXT_LeftFacadePodium", "EXT_RightFacadeWing", "EXT_RearService", "EXT_RoofService"),
            new SceneSpec("Exterior_InteriorShellPreview", 48, 12120, 0, "EXT_InteriorShell"),
            new SceneSpec("Exterior_Interactions", 44, 6160, 12, DoorRoots.Select(DoorWrapperName).ToArray()),
        };

        private static readonly CombinationSpec[] Combinations =
        {
            new CombinationSpec("ExteriorOnly", 145, 626052, 28, "Exterior_Base", "Exterior_Tower", "Exterior_SidesService"),
            new CombinationSpec("ExteriorWithShell", 193, 638172, 28, "Exterior_Base", "Exterior_Tower", "Exterior_SidesService", "Exterior_InteriorShellPreview"),
            new CombinationSpec("ExteriorWithDoors", 189, 632212, 40, "Exterior_Base", "Exterior_Tower", "Exterior_SidesService", "Exterior_Interactions"),
            new CombinationSpec("FullHospital", 237, 644332, 40, "Exterior_Base", "Exterior_Tower", "Exterior_SidesService", "Exterior_InteriorShellPreview", "Exterior_Interactions"),
        };

        private static readonly string[] QaFiles =
        {
            "Stage05_R44_Unity_Front.png", "Stage05_R44_Unity_Left.png", "Stage05_R44_Unity_Right.png",
            "Stage05_R44_Unity_Rear.png", "Stage05_R44_Unity_Roof.png", "Stage05_R44_Unity_Entrance.png",
            "Stage05_R44_Unity_Aerial.png", "Stage05_R44_Unity_PlayerEye.png",
            "Stage05_R44_Target_FloorBands.png", "Stage05_R44_Target_RightWingRearClosure.png",
            "Stage05_R44_Target_LoadingArea.png",
        };

        private static readonly string[] DisposableScenes =
        {
            SandboxFolder + "/Hospital_R41_IntegrationSandbox.unity",
            SandboxFolder + "/Hospital_R42_CompleteIntegrationSandbox.unity",
            SandboxFolder + "/Hospital_R43_TraversalHarness.unity",
        };

        [MenuItem("Hospital/Stage 5/Build and measure R44")]
        public static void BuildAndMeasureR44()
        {
            try
            {
                string workspace = WorkspaceRoot();
                string reviews = R44Reviews(workspace);
                Directory.CreateDirectory(reviews);
                RequireR43Pass(workspace);
                ForceCleanFrozenImport(workspace);
                ConfigureFinalBuildSettings();
                WriteImportSettings(reviews);
                WriteFinalMaterialMap(reviews);
                WriteFinalSceneLoadMatrix(reviews);
                WriteSourceChangeLog(reviews);
                RenderFinalQa(reviews);
                WriteQaEvidence(reviews);
                ArchiveDisposableScenes(workspace);
                WriteFinalManifest(workspace, false);

                R44GateReport report = ValidateR44(workspace, false);
                report.schema = "HospitalExterior.Stage05.R44.TechnicalGate.v1";
                WriteJson(report, Path.Combine(reviews, "Stage05_R44_TechnicalGate.json"));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();
                if (!report.pass)
                    throw new BuildFailedException("R44 technical gate failed. See Stage05_R44_TechnicalGate.json.");
                Debug.Log("STAGE05_R44_TECHNICAL_GATE=PASS; VISUAL_REVIEW_REQUIRED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        [MenuItem("Hospital/Stage 5/Validate R44 final gate")]
        public static void ValidateR44FinalGate()
        {
            string workspace = WorkspaceRoot();
            string reviews = R44Reviews(workspace);
            WriteFinalManifest(workspace, true);
            R44GateReport report = ValidateR44(workspace, true);
            report.schema = "HospitalExterior.Stage05.R44.UnityGate.v1";
            WriteJson(report, Path.Combine(reviews, "Stage05_R44_UnityGate.json"));
            WriteJson(report, AssetPathToAbsolute(SettingsFolder + "/Stage05_R44_UnityGate.json"));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            if (!report.pass)
                throw new BuildFailedException("R44 final gate failed. See Stage05_R44_UnityGate.json.");
            Debug.Log("STAGE05_R44_GATE=PASS; STAGE05_FREEZE=COMPLETE");
        }

        private static void RequireR43Pass(string workspace)
        {
            if (!IsGatePass(Path.Combine(workspace, R43GateRelative.Replace('/', Path.DirectorySeparatorChar))))
                throw new InvalidOperationException("R44 requires a passing frozen R43 gate.");
        }

        private static bool IsGatePass(string path)
        {
            if (!File.Exists(path)) return false;
            GateStatus status = JsonUtility.FromJson<GateStatus>(File.ReadAllText(path));
            return status != null && status.pass && status.status == "PASS";
        }

        private static void ForceCleanFrozenImport(string workspace)
        {
            string external = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input");
            foreach (ZoneSpec zone in Zones)
            {
                string externalPath = Path.Combine(external, zone.file);
                string unityPath = AssetPathToAbsolute(SourceFolder + "/" + zone.file);
                if (Sha256(externalPath) != zone.sha256 || Sha256(unityPath) != zone.sha256)
                    throw new InvalidDataException("Frozen FBX checksum mismatch: " + zone.file);
                AssetDatabase.ImportAsset(SourceFolder + "/" + zone.file,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureFinalBuildSettings()
        {
            EditorBuildSettings.scenes = Scenes.Select(scene => new EditorBuildSettingsScene(ScenePath(scene.name), true)).ToArray();
        }

        private static void WriteImportSettings(string reviews)
        {
            var entries = new List<ImportSettingEntry>();
            foreach (ZoneSpec zone in Zones)
            {
                string path = SourceFolder + "/" + zone.file;
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                entries.Add(new ImportSettingEntry
                {
                    zone = zone.zone, asset = path, sha256 = Sha256(AssetPathToAbsolute(path)),
                    global_scale = importer.globalScale, use_file_scale = importer.useFileScale,
                    bake_axis_conversion = importer.bakeAxisConversion, import_cameras = importer.importCameras,
                    import_lights = importer.importLights, add_collider = importer.addCollider,
                    import_animation = importer.importAnimation, preserve_hierarchy = importer.preserveHierarchy,
                    normals = importer.importNormals.ToString(), tangents = importer.importTangents.ToString(),
                    material_location = importer.materialLocation.ToString(),
                });
            }
            WriteJson(new ImportSettingsRecord
            {
                schema = "HospitalExterior.Stage05.R44.ImportSettings.v1",
                unity_editor = Application.unityVersion,
                entries = entries.ToArray(),
            }, Path.Combine(reviews, "Stage05_R44_ImportSettings.json"));
        }

        private static void WriteFinalMaterialMap(string reviews)
        {
            Material[] materials = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat")
                .Select(path => AssetDatabase.LoadAssetAtPath<Material>(AbsoluteToAssetPath(path))).OrderBy(material => material.name).ToArray();
            WriteJson(new FinalMaterialMap
            {
                schema = "HospitalExterior.Stage05.R44.MaterialMap.v1",
                authority_sha256 = AuthoritySha,
                entries = materials.Select(material => new FinalMaterialEntry
                {
                    material = material.name,
                    asset = AssetDatabase.GetAssetPath(material),
                    asset_sha256 = Sha256(AssetPathToAbsolute(AssetDatabase.GetAssetPath(material))),
                    role = IsTransparent(material) ? "glass" : "opaque",
                    shader = material.shader == null ? "missing" : material.shader.name,
                    instancing = material.enableInstancing,
                }).ToArray(),
            }, Path.Combine(reviews, "Stage05_R44_MaterialMap.json"));
        }

        private static void WriteFinalSceneLoadMatrix(string reviews)
        {
            WriteJson(new FinalSceneMatrix
            {
                schema = "HospitalExterior.Stage05.R44.SceneLoadMatrix.v1",
                scenes = Scenes.Select(scene => new FinalSceneEntry
                {
                    scene = scene.name, asset = ScenePath(scene.name), roots = scene.roots,
                    renderer_count = scene.renderers, triangle_count = scene.triangles, collider_count = scene.colliders,
                }).ToArray(),
                combinations = Combinations.Select(value => new FinalCombinationEntry
                {
                    state = value.name, scenes = value.scenes, renderer_count = value.renderers,
                    triangle_count = value.triangles, collider_count = value.colliders,
                }).ToArray(),
            }, Path.Combine(reviews, "Stage05_R44_SceneLoadMatrix.json"));
        }

        private static void WriteSourceChangeLog(string reviews)
        {
            WriteJson(new SourceChangeLog
            {
                schema = "HospitalExterior.Stage05.R44.SourceChangeLog.v1",
                authority = AuthorityRelative,
                authority_sha256 = AuthoritySha,
                source_geometry_changes_during_stage05 = 0,
                source_fbx_changes_during_stage05 = 0,
                note = "The user-approved twelve-triangle GlobalStructure edit predates the Stage 5 freeze. R41 re-froze it; R41-R44 modify only Unity assets and records.",
                rounds = new[]
                {
                    "R41: PC-VR URP/OpenXR foundation, three-zone proof and shared materials",
                    "R42: complete ten-zone prefabs, five additive scenes and visual QA",
                    "R43: twelve door prefabs, simple collision, scale harness and attributable profile proxy",
                    "R44: clean import/load QA, final visual comparison, record freeze and disposable-scene archive",
                },
            }, Path.Combine(reviews, "Stage05_R44_SourceChangeLog.json"));
        }

        private static void RenderFinalQa(string reviews)
        {
            List<Scene> loaded = OpenCombination(Combinations.Single(value => value.name == "FullHospital"));
            GameObject[] geometry = loaded.SelectMany(scene => scene.GetRootGameObjects())
                .Where(root => root.GetComponentsInChildren<Renderer>(true).Length > 0).ToArray();
            Bounds full = CombinedBounds(geometry);
            RenderBoundView(Path.Combine(reviews, QaFiles[0]), full, new Vector3(0f, 0.06f, -1f));
            RenderBoundView(Path.Combine(reviews, QaFiles[1]), full, new Vector3(-1f, 0.05f, 0f));
            RenderBoundView(Path.Combine(reviews, QaFiles[2]), full, new Vector3(1f, 0.05f, 0f));
            RenderBoundView(Path.Combine(reviews, QaFiles[3]), full, new Vector3(0f, 0.05f, 1f));
            RenderBoundView(Path.Combine(reviews, QaFiles[4]), full, new Vector3(-0.10f, 1f, 0.18f));
            RenderAt(Path.Combine(reviews, QaFiles[5]), new Vector3(0f, 6.5f, -38f), new Vector3(0f, 2.8f, -17f), 48f);
            RenderBoundView(Path.Combine(reviews, QaFiles[6]), full, new Vector3(-0.70f, 0.48f, -1f));
            RenderAt(Path.Combine(reviews, QaFiles[7]), new Vector3(4f, 1.7f, -30f), new Vector3(0f, 2.5f, -17f), 60f);
            RenderAt(Path.Combine(reviews, QaFiles[8]), new Vector3(30f, 18f, -46f), new Vector3(10f, 16f, -16f), 42f);
            RenderAt(Path.Combine(reviews, QaFiles[9]), new Vector3(43f, 3.5f, 29f), new Vector3(28f, 3f, 16f), 48f);
            RenderAt(Path.Combine(reviews, QaFiles[10]), new Vector3(-32f, 1.7f, 27f), new Vector3(-32f, 2.2f, 17.3f), 55f);
        }

        private static void RenderBoundView(string path, Bounds bounds, Vector3 direction)
        {
            float distance = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z)) * 1.18f;
            Vector3 position = bounds.center + direction.normalized * distance;
            RenderAt(path, position, bounds.center + Vector3.up * bounds.extents.y * 0.05f, 50f);
        }

        private static void RenderAt(string path, Vector3 position, Vector3 targetPoint, float fieldOfView)
        {
            GameObject rig = new GameObject("R44_TEMP_QA_RIG");
            Camera camera = new GameObject("R44_TEMP_QA_CAMERA").AddComponent<Camera>();
            camera.transform.SetParent(rig.transform, false);
            Light light = new GameObject("R44_TEMP_QA_LIGHT").AddComponent<Light>();
            light.transform.SetParent(rig.transform, false);
            try
            {
                light.type = LightType.Directional; light.intensity = 1.35f;
                light.color = new Color(1f, 0.96f, 0.90f); light.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.68f, 0.74f, 0.80f, 1f);
                camera.fieldOfView = fieldOfView; camera.nearClipPlane = 0.05f; camera.farClipPlane = 500f;
                camera.transform.position = position; camera.transform.LookAt(targetPoint);
                var renderTexture = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32);
                var texture = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
                RenderTexture previous = RenderTexture.active;
                try
                {
                    if (!renderTexture.Create()) throw new InvalidOperationException("Failed to create R44 QA render target.");
                    camera.targetTexture = renderTexture; camera.Render(); RenderTexture.active = renderTexture;
                    texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = null; RenderTexture.active = previous; renderTexture.Release();
                    UnityObject.DestroyImmediate(renderTexture); UnityObject.DestroyImmediate(texture);
                }
            }
            finally { UnityObject.DestroyImmediate(rig); }
        }

        private static void WriteQaEvidence(string reviews)
        {
            WriteJson(new QaEvidence
            {
                schema = "HospitalExterior.Stage05.R44.QaEvidence.v1",
                images = QaFiles.Select(file => new QaEvidenceEntry
                {
                    file = file, sha256 = Sha256(Path.Combine(reviews, file)), bytes = new FileInfo(Path.Combine(reviews, file)).Length,
                }).ToArray(),
            }, Path.Combine(reviews, "Stage05_R44_QaEvidence.json"));
        }

        private static void ArchiveDisposableScenes(string workspace)
        {
            string archive = Path.Combine(workspace, "Archive", "HospitalExterior", "Unity", "Stage05_DisposableScenes");
            Directory.CreateDirectory(archive);
            foreach (string assetPath in DisposableScenes)
            {
                string source = AssetPathToAbsolute(assetPath);
                if (!File.Exists(source)) continue;
                FreezeCopy(source, Path.Combine(archive, Path.GetFileName(source)));
                if (File.Exists(source + ".meta")) FreezeCopy(source + ".meta", Path.Combine(archive, Path.GetFileName(source) + ".meta"));
                if (!AssetDatabase.DeleteAsset(assetPath))
                    throw new InvalidOperationException("Failed to archive/remove disposable scene: " + assetPath);
            }
            var manifest = new DisposableArchiveManifest
            {
                schema = "HospitalExterior.Stage05.R44.DisposableArchive.v1",
                files = Directory.GetFiles(archive).OrderBy(path => path).Select(path => new FrozenFile
                { file = Path.GetFileName(path), sha256 = Sha256(path), bytes = new FileInfo(path).Length }).ToArray(),
            };
            WriteJson(manifest, Path.Combine(archive, "Stage05_DisposableScenes_Manifest.json"));
        }

        private static void FreezeCopy(string source, string destination)
        {
            if (File.Exists(destination))
            {
                if (Sha256(source) != Sha256(destination))
                    throw new InvalidOperationException("Archive target differs; refusing overwrite: " + destination);
                return;
            }
            File.Copy(source, destination, false);
        }

        private static void WriteFinalManifest(string workspace, bool includeVisual)
        {
            string reviews = R44Reviews(workspace);
            string inputManifest = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input", "Stage05_R41_InputManifest.json");
            var recordFiles = new[]
            {
                "Stage05_R44_ImportSettings.json", "Stage05_R44_MaterialMap.json", "Stage05_R44_SceneLoadMatrix.json",
                "Stage05_R44_SourceChangeLog.json", "Stage05_R44_QaEvidence.json",
            }.Concat(includeVisual ? new[] { VisualReviewFile } : Array.Empty<string>()).ToArray();
            var packages = PackageManagerPackageInfo.GetAllRegisteredPackages()
                .Where(package => new[] { "com.unity.render-pipelines.universal", "com.unity.inputsystem", "com.unity.xr.management", "com.unity.xr.openxr", "com.unity.xr.interaction.toolkit" }.Contains(package.name))
                .OrderBy(package => package.name).Select(package => new PackageEntry { name = package.name, version = package.version }).ToArray();
            var manifest = new Stage05Manifest
            {
                schema = "HospitalExterior.Stage05.R44.Manifest.v1",
                status = includeVisual ? "FINAL_FROZEN" : "TECHNICAL_CANDIDATE",
                authority = AuthorityRelative, authority_sha256 = AuthoritySha,
                input_manifest_sha256 = Sha256(inputManifest), unity_editor = Application.unityVersion,
                platform = "Windows x86_64 PC VR", xr_runtime = "OpenXR",
                headset_scope = "Meta Quest 2 / Quest 3 via Quest Link or Air Link; Android standalone excluded",
                source_triangles = 644996, unity_triangles = 644332, zero_area_triangles_discarded = 664,
                renderers = 237, materials = 30, door_prefabs = 12, static_box_colliders = 28,
                dynamic_door_box_colliders = 12, mesh_colliders = 0,
                fbx = Zones.Select(zone => new ManifestFbx { zone = zone.zone, file = zone.file, sha256 = zone.sha256 }).ToArray(),
                packages = packages,
                additive_scenes = Scenes.Select(scene => ScenePath(scene.name)).ToArray(),
                records = recordFiles.Select(file => new FrozenFile
                {
                    file = file, sha256 = Sha256(Path.Combine(reviews, file)), bytes = new FileInfo(Path.Combine(reviews, file)).Length,
                }).ToArray(),
                prior_gates = new[]
                {
                    FreezeReview(workspace, "Stage05_R41", "Stage05_R41_UnityGate.json"),
                    FreezeReview(workspace, "Stage05_R42", "Stage05_R42_UnityGate.json"),
                    FreezeReview(workspace, "Stage05_R43", "Stage05_R43_UnityGate.json"),
                },
                performance_disposition = "R43 editor/D3D11 proxy is frozen; Quest Link/Air Link stereo CPU/GPU capture is required before Stage 6 performance acceptance.",
            };
            WriteJson(manifest, Path.Combine(reviews, "Stage05_R44_Manifest.json"));
            WriteJson(manifest, AssetPathToAbsolute(SettingsFolder + "/Stage05_R44_Manifest.json"));
        }

        private static FrozenFile FreezeReview(string workspace, string round, string file)
        {
            string path = Path.Combine(workspace, "Reviews", "HospitalExterior", round, file);
            return new FrozenFile { file = round + "/" + file, sha256 = Sha256(path), bytes = new FileInfo(path).Length };
        }

        private static R44GateReport ValidateR44(string workspace, bool requireVisual)
        {
            string reviews = R44Reviews(workspace);
            var report = new R44GateReport
            {
                status = "FAIL", unity_editor = Application.unityVersion,
                platform = "Windows x86_64 PC VR", checks = new List<GateCheck>(), scenes = new List<SceneMeasurement>(),
                combinations = new List<CombinationMeasurement>(),
            };
            AddCheck(report, "r43_prerequisite", IsGatePass(Path.Combine(workspace, R43GateRelative.Replace('/', Path.DirectorySeparatorChar))), R43GateRelative);
            string authority = Path.Combine(workspace, AuthorityRelative.Replace('/', Path.DirectorySeparatorChar));
            AddCheck(report, "authority_checksum", File.Exists(authority) && Sha256(authority) == AuthoritySha,
                File.Exists(authority) ? Sha256(authority) : "missing");
            AddCheck(report, "unity_version", Application.unityVersion == ExpectedUnity, Application.unityVersion);
            bool xr = HospitalStage05XRSetup.IsValid(out string xrDetail);
            AddCheck(report, "pcvr_openxr_frozen", xr, xrDetail);
            ValidateInputs(report, workspace);
            ValidateBuildSettings(report);
            ValidateScenes(report);
            ValidateCombinations(report);
            ValidateMaterials(report);
            ValidateFinalRecords(report, reviews, requireVisual);
            ValidateActiveProjectClean(report, workspace);
            if (requireVisual) ValidateVisualReview(report, reviews);
            report.pass = report.checks.All(check => check.pass);
            report.status = report.pass ? "PASS" : "FAIL";
            return report;
        }

        private static void ValidateInputs(R44GateReport report, string workspace)
        {
            string external = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage05_R41_Input");
            string[] unityFiles = Directory.GetFiles(AssetPathToAbsolute(SourceFolder), "*.fbx");
            AddCheck(report, "exact_ten_unity_fbx", unityFiles.Length == 10, string.Join(",", unityFiles.Select(Path.GetFileName)));
            foreach (ZoneSpec zone in Zones)
            {
                string externalPath = Path.Combine(external, zone.file);
                string assetPath = SourceFolder + "/" + zone.file;
                string unityPath = AssetPathToAbsolute(assetPath);
                bool checksum = Sha256(externalPath) == zone.sha256 && Sha256(unityPath) == zone.sha256;
                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                bool import = importer != null && importer.globalScale == 1f && importer.useFileScale && importer.bakeAxisConversion &&
                              !importer.importCameras && !importer.importLights && !importer.addCollider && !importer.importAnimation &&
                              importer.preserveHierarchy && importer.materialLocation == ModelImporterMaterialLocation.External;
                AddCheck(report, zone.zone + "_clean_import", checksum && import,
                    $"checksum={checksum}; import_policy={import}");
            }
        }

        private static void ValidateBuildSettings(R44GateReport report)
        {
            EditorBuildSettingsScene[] settings = EditorBuildSettings.scenes;
            bool pass = settings.Length == 5 && settings.All(scene => scene.enabled) &&
                        settings.Select(scene => scene.path).SequenceEqual(Scenes.Select(scene => ScenePath(scene.name)));
            AddCheck(report, "five_final_additive_scenes_in_build_settings", pass,
                string.Join(",", settings.Select(scene => scene.path + ":" + scene.enabled)));
        }

        private static void ValidateScenes(R44GateReport report)
        {
            foreach (SceneSpec expected in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath(expected.name), OpenSceneMode.Single);
                GameObject[] roots = scene.GetRootGameObjects();
                Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
                long triangles = renderers.Sum(RendererTriangles);
                int colliders = roots.Sum(root => root.GetComponentsInChildren<Collider>(true).Length);
                int cameras = roots.Sum(root => root.GetComponentsInChildren<Camera>(true).Length);
                int lights = roots.Sum(root => root.GetComponentsInChildren<Light>(true).Length);
                bool names = roots.Select(root => root.name).OrderBy(value => value).SequenceEqual(expected.roots.OrderBy(value => value));
                bool connected = roots.All(root => PrefabUtility.GetPrefabInstanceStatus(root) == PrefabInstanceStatus.Connected);
                bool zero = roots.All(root => root.transform.position == Vector3.zero && root.transform.rotation == Quaternion.identity && root.transform.localScale == Vector3.one);
                bool pass = names && connected && zero && renderers.Length == expected.renderers && triangles == expected.triangles &&
                            colliders == expected.colliders && cameras == 0 && lights == 0;
                report.scenes.Add(new SceneMeasurement
                {
                    scene = expected.name, roots = roots.Select(root => root.name).ToArray(), renderers = renderers.Length,
                    triangles = triangles, colliders = colliders, cameras = cameras, lights = lights, pass = pass,
                });
                AddCheck(report, "scene_" + expected.name, pass,
                    $"roots={string.Join(",", roots.Select(root => root.name))}; renderers={renderers.Length}; triangles={triangles}; colliders={colliders}; connected={connected}; zero={zero}; cameras={cameras}; lights={lights}");
            }
        }

        private static void ValidateCombinations(R44GateReport report)
        {
            foreach (CombinationSpec expected in Combinations)
            {
                List<Scene> scenes = OpenCombination(expected);
                GameObject[] roots = scenes.SelectMany(scene => scene.GetRootGameObjects()).ToArray();
                Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
                long triangles = renderers.Sum(RendererTriangles);
                int colliders = roots.Sum(root => root.GetComponentsInChildren<Collider>(true).Length);
                int cameras = roots.Sum(root => root.GetComponentsInChildren<Camera>(true).Length);
                int lights = roots.Sum(root => root.GetComponentsInChildren<Light>(true).Length);
                bool pass = renderers.Length == expected.renderers && triangles == expected.triangles &&
                            colliders == expected.colliders && cameras == 0 && lights == 0;
                report.combinations.Add(new CombinationMeasurement
                { state = expected.name, renderers = renderers.Length, triangles = triangles, colliders = colliders, pass = pass });
                AddCheck(report, "combination_" + expected.name, pass,
                    $"renderers={renderers.Length}; triangles={triangles}; colliders={colliders}; cameras={cameras}; lights={lights}");
            }
        }

        private static List<Scene> OpenCombination(CombinationSpec combination)
        {
            var loaded = new List<Scene>();
            for (int index = 0; index < combination.scenes.Length; index++)
                loaded.Add(EditorSceneManager.OpenScene(ScenePath(combination.scenes[index]),
                    index == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive));
            return loaded;
        }

        private static void ValidateMaterials(R44GateReport report)
        {
            string[] files = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat");
            bool library = files.Length == 30;
            var used = new HashSet<Material>();
            var bad = new List<string>();
            foreach (string sceneName in Scenes.Select(scene => scene.name))
            {
                Scene scene = EditorSceneManager.OpenScene(ScenePath(sceneName), OpenSceneMode.Single);
                foreach (Renderer renderer in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material == null) { bad.Add("null"); continue; }
                        used.Add(material);
                        if (!AssetDatabase.GetAssetPath(material).StartsWith(MaterialFolder + "/", StringComparison.Ordinal))
                            bad.Add(AssetDatabase.GetAssetPath(material));
                    }
            }
            AddCheck(report, "final_shared_material_library", library && used.Count == 30 && bad.Count == 0,
                $"library={files.Length}; used={used.Count}; bad={string.Join(",", bad)}");
        }

        private static void ValidateFinalRecords(R44GateReport report, string reviews, bool requireVisual)
        {
            string[] required =
            {
                "Stage05_R44_ImportSettings.json", "Stage05_R44_MaterialMap.json", "Stage05_R44_SceneLoadMatrix.json",
                "Stage05_R44_SourceChangeLog.json", "Stage05_R44_QaEvidence.json", "Stage05_R44_Manifest.json",
            };
            bool pass = required.All(file => File.Exists(Path.Combine(reviews, file)) && new FileInfo(Path.Combine(reviews, file)).Length > 20);
            if (requireVisual) pass &= File.Exists(Path.Combine(reviews, VisualReviewFile));
            AddCheck(report, "final_frozen_records_present", pass, string.Join(",", required));
            string r43Profile = Path.Combine(Directory.GetParent(reviews).FullName, "Stage05_R43", "Stage05_R43_ProfilerBaseline.json");
            ProfilerDisposition profile = JsonUtility.FromJson<ProfilerDisposition>(File.ReadAllText(r43Profile));
            AddCheck(report, "profile_limitation_preserved", profile.headset_profile_required &&
                profile.measurement_context.Contains("no active headset runtime") && !string.IsNullOrWhiteSpace(profile.headset_profile_note),
                profile.headset_profile_note);
        }

        private static void ValidateActiveProjectClean(R44GateReport report, string workspace)
        {
            bool noSandbox = DisposableScenes.All(path => !File.Exists(AssetPathToAbsolute(path)));
            string archive = Path.Combine(workspace, "Archive", "HospitalExterior", "Unity", "Stage05_DisposableScenes");
            bool archived = File.Exists(Path.Combine(archive, "Stage05_DisposableScenes_Manifest.json"));
            AddCheck(report, "disposable_scenes_archived_and_removed", noSandbox && archived,
                $"active_removed={noSandbox}; archive_manifest={archived}");
            string[] assetPaths = AssetDatabase.GetAllAssetPaths();
            bool noR39 = !assetPaths.Any(path => path.Contains("R39", StringComparison.OrdinalIgnoreCase));
            AddCheck(report, "no_legacy_r39_assets", noR39, noR39 ? "none" : "R39 asset path found");
            string[] duplicateXr = { "Assets/XR 1", "Assets/XR 2", "Assets/XR/Settings 1", "Assets/XR/Settings 2" };
            AddCheck(report, "no_duplicate_xr_folders", duplicateXr.All(path => !AssetDatabase.IsValidFolder(path)), "checked");
            int doorPrefabs = Directory.GetFiles(AssetPathToAbsolute(DoorPrefabFolder), "*.prefab").Length;
            GameObject collision = AssetDatabase.LoadAssetAtPath<GameObject>(CollisionPrefabPath);
            bool inventories = doorPrefabs == 12 && collision != null &&
                               collision.GetComponentsInChildren<BoxCollider>(true).Length == 28 &&
                               collision.GetComponentsInChildren<MeshCollider>(true).Length == 0;
            AddCheck(report, "final_door_collision_inventory", inventories,
                $"door_prefabs={doorPrefabs}; collision_boxes={collision?.GetComponentsInChildren<BoxCollider>(true).Length}; mesh=0");
        }

        private static void ValidateVisualReview(R44GateReport report, string reviews)
        {
            string path = Path.Combine(reviews, VisualReviewFile);
            VisualReview review = File.Exists(path) ? JsonUtility.FromJson<VisualReview>(File.ReadAllText(path)) : null;
            bool pass = review != null && review.status == "PASS" && review.checks != null && review.checks.All(check => check.pass) &&
                        review.images != null && review.images.Length == QaFiles.Length;
            if (pass)
            {
                foreach (string file in QaFiles)
                {
                    VisualImage image = review.images.SingleOrDefault(value => value.file == file);
                    if (image == null || image.sha256 != Sha256(Path.Combine(reviews, file))) { pass = false; break; }
                }
            }
            AddCheck(report, "final_standard_and_targeted_visual_review", pass,
                pass ? "eleven checksum-bound views PASS" : "missing, failed or checksum-mismatched visual review");
        }

        private static long RendererTriangles(Renderer renderer)
        {
            Mesh mesh = MeshForRenderer(renderer);
            if (mesh == null) return 0;
            long indices = 0; for (int index = 0; index < mesh.subMeshCount; index++) indices += (long)mesh.GetIndexCount(index);
            return indices / 3L;
        }

        private static Mesh MeshForRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            MeshFilter filter = renderer.GetComponent<MeshFilter>(); return filter == null ? null : filter.sharedMesh;
        }

        private static bool IsTransparent(Material material) =>
            material != null && material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;

        private static Bounds CombinedBounds(IEnumerable<GameObject> roots)
        {
            Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static string ScenePath(string scene) => AdditiveFolder + "/" + scene + ".unity";
        private static string DoorWrapperName(string root) => "DOORPREFAB_" + root;
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
        private static string AbsoluteToAssetPath(string absolute)
        {
            string project = Directory.GetParent(Application.dataPath).FullName;
            return Path.GetRelativePath(project, absolute).Replace(Path.DirectorySeparatorChar, '/');
        }
        private static string R44Reviews(string workspace) => Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R44");
        private static string Sha256(string path)
        {
            using (var stream = File.OpenRead(path)) using (var hash = SHA256.Create())
                return string.Concat(hash.ComputeHash(stream).Select(value => value.ToString("x2")));
        }
        private static void AddCheck(R44GateReport report, string name, bool pass, string detail) =>
            report.checks.Add(new GateCheck { name = name, pass = pass, detail = detail });
        private static void WriteJson(object value, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + Environment.NewLine);
        }

        private sealed class ZoneSpec
        {
            public readonly string zone, file, sha256; public readonly int renderers; public readonly long triangles;
            public ZoneSpec(string zone, string file, string sha256, int renderers, long triangles)
            { this.zone = zone; this.file = file; this.sha256 = sha256; this.renderers = renderers; this.triangles = triangles; }
        }
        private sealed class SceneSpec
        {
            public readonly string name; public readonly int renderers; public readonly long triangles; public readonly int colliders; public readonly string[] roots;
            public SceneSpec(string name, int renderers, long triangles, int colliders, params string[] roots)
            { this.name = name; this.renderers = renderers; this.triangles = triangles; this.colliders = colliders; this.roots = roots; }
        }
        private sealed class CombinationSpec
        {
            public readonly string name; public readonly int renderers; public readonly long triangles; public readonly int colliders; public readonly string[] scenes;
            public CombinationSpec(string name, int renderers, long triangles, int colliders, params string[] scenes)
            { this.name = name; this.renderers = renderers; this.triangles = triangles; this.colliders = colliders; this.scenes = scenes; }
        }

        [Serializable] private sealed class GateStatus { public string status; public bool pass; }
        [Serializable] private sealed class GateCheck { public string name; public bool pass; public string detail; }
        [Serializable] private sealed class R44GateReport
        {
            public string schema, status, unity_editor, platform; public bool pass; public List<GateCheck> checks;
            public List<SceneMeasurement> scenes; public List<CombinationMeasurement> combinations;
        }
        [Serializable] private sealed class SceneMeasurement
        { public string scene; public string[] roots; public int renderers; public long triangles; public int colliders, cameras, lights; public bool pass; }
        [Serializable] private sealed class CombinationMeasurement
        { public string state; public int renderers; public long triangles; public int colliders; public bool pass; }
        [Serializable] private sealed class ImportSettingsRecord { public string schema, unity_editor; public ImportSettingEntry[] entries; }
        [Serializable] private sealed class ImportSettingEntry
        {
            public string zone, asset, sha256, normals, tangents, material_location; public float global_scale;
            public bool use_file_scale, bake_axis_conversion, import_cameras, import_lights, add_collider, import_animation, preserve_hierarchy;
        }
        [Serializable] private sealed class FinalMaterialMap { public string schema, authority_sha256; public FinalMaterialEntry[] entries; }
        [Serializable] private sealed class FinalMaterialEntry
        { public string material, asset, asset_sha256, role, shader; public bool instancing; }
        [Serializable] private sealed class FinalSceneMatrix { public string schema; public FinalSceneEntry[] scenes; public FinalCombinationEntry[] combinations; }
        [Serializable] private sealed class FinalSceneEntry
        { public string scene, asset; public string[] roots; public int renderer_count; public long triangle_count; public int collider_count; }
        [Serializable] private sealed class FinalCombinationEntry
        { public string state; public string[] scenes; public int renderer_count; public long triangle_count; public int collider_count; }
        [Serializable] private sealed class SourceChangeLog
        { public string schema, authority, authority_sha256, note; public int source_geometry_changes_during_stage05, source_fbx_changes_during_stage05; public string[] rounds; }
        [Serializable] private sealed class QaEvidence { public string schema; public QaEvidenceEntry[] images; }
        [Serializable] private sealed class QaEvidenceEntry { public string file, sha256; public long bytes; }
        [Serializable] private sealed class DisposableArchiveManifest { public string schema; public FrozenFile[] files; }
        [Serializable] private sealed class FrozenFile { public string file, sha256; public long bytes; }
        [Serializable] private sealed class PackageEntry { public string name, version; }
        [Serializable] private sealed class ManifestFbx { public string zone, file, sha256; }
        [Serializable] private sealed class Stage05Manifest
        {
            public string schema, status, authority, authority_sha256, input_manifest_sha256, unity_editor, platform, xr_runtime, headset_scope;
            public long source_triangles, unity_triangles; public int zero_area_triangles_discarded, renderers, materials, door_prefabs;
            public int static_box_colliders, dynamic_door_box_colliders, mesh_colliders; public ManifestFbx[] fbx; public PackageEntry[] packages;
            public string[] additive_scenes; public FrozenFile[] records, prior_gates; public string performance_disposition;
        }
        [Serializable] private sealed class ProfilerDisposition
        { public string measurement_context, headset_profile_note; public bool headset_profile_required; }
        [Serializable] private sealed class VisualReview { public string schema, status; public VisualCheck[] checks; public VisualImage[] images; }
        [Serializable] private sealed class VisualCheck { public string name, detail; public bool pass; }
        [Serializable] private sealed class VisualImage { public string file, sha256; }
    }
}
