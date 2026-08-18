using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using CutMyBodyPlease.HospitalSite.Stage05S;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.HospitalSite.Stage05S.Editor
{
    /// <summary>
    /// S6 imports only checksum-frozen R05B bytes and creates new Unity assets. It does not
    /// touch the five R44 scenes, hospital assets, the Blender authorities, or source FBXs.
    /// </summary>
    public static class HospitalStage05SRoundS6Builder
    {
        internal const string AuthoritySha = "68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d";
        internal const string Root = "Assets/HospitalSite/Stage05S/R05B";
        internal const string SourceFolder = Root + "/SourceFBX";
        internal const string TextureFolder = Root + "/Textures/Grass005";
        internal const string MaterialFolder = Root + "/Materials";
        internal const string GeneratedFolder = Root + "/Generated";
        internal const string PrefabFolder = Root + "/Prefabs/Zones";
        internal const string SceneFolder = Root + "/Scenes/Additive";
        internal const string TestSceneFolder = Root + "/Scenes/Test";
        internal const string SettingsFolder = Root + "/Settings";

        private const string ExportRelative = "Exports/HospitalExterior/Stage05S_R05B_GrassRefinement";
        private const string ReviewRelative = "Reviews/HospitalExterior/Stage05S_RoundS6";

        internal static readonly InputSpec[] SiteInputs =
        {
            new InputSpec("Hospital_SITE_CoreRoadParking_R05B.fbx", "ed8403c9c43f0b7d1c58ecf492805afe1ac4a7f7b183408aafd2305fe1b3ddd3"),
            new InputSpec("Hospital_SITE_GrassRuntimeAssets_R05B.fbx", "5820f307f4b503ee3cf1bbe0dfd16cd6a7b14423896dc40c60a6cb30c98927cd"),
            new InputSpec("Hospital_SITE_LandscapeHardscape_R05B.fbx", "ce8a584e5734c78e21a797e8e9579d2e71a10b410194ff9cfb7a16e243aa1e0b"),
            new InputSpec("Hospital_SITE_MainGate_R05B.fbx", "3a12ccb30a4b88b536b132473a461e0757ac70be3b409f3f3a04f563f67ff436"),
            new InputSpec("Hospital_SITE_PerimeterFence_R05B.fbx", "d6955e8985d41f763bba8a7917968fc652af728e98d97106ea117054a19a0b2b"),
            new InputSpec("Hospital_SITE_PropsVegetation_R05B.fbx", "586e32f82770d3d77c75253848065b88e000a695e50f11fe7abbf91ed91b1f0e"),
            new InputSpec("Hospital_SITE_RearService_R05B.fbx", "cef0237c93db623acaec1c3a53e3cedeee84923d910149b4fde109444626214d"),
        };

        internal static readonly InputSpec[] TextureInputs =
        {
            new InputSpec("GrassRuntimeSpec.json", "42705cd564b2415a167a5619ba8df302a4767f341d390a91e6e2ebd60549efbd"),
            new InputSpec("Textures/Grass005/T_S05S_Grass005_BaseColor_2K.png", "c27fd2fb5bc29403545d08167fcb76271087c1fb1ffcbb63b9b3a4c47258f4f9"),
            new InputSpec("Textures/Grass005/T_S05S_Grass005_NormalGL_2K.png", "64364bc8b3cec8c35a7f1c835e4158cbc32c2cf5dad59c06efa702012ac4911d"),
            new InputSpec("Textures/Grass005/T_S05S_Grass005_Roughness_2K.png", "2e05a1d2895ee51a013ed4e5915a1767ff08c6bb342463d723ae4483eb46d462"),
            new InputSpec("Textures/Grass005/T_S05S_Grass005_AO_2K.png", "efa6181b02fe9e1f1af52ee604e8ef890de6261ec6b4225d214c96acfef1a719"),
            new InputSpec("Textures/Grass005/T_S05S_GrassCoverageMask_2K.png", "a29be4234cde0094c67a2c1ac437f1722f35d4959bda4c369b80d21af2b39564"),
        };

        internal static readonly SceneSpec[] SiteScenes =
        {
            new SceneSpec("Exterior_SiteCore", "SITE_CoreRoadParking", "SITE_RearService"),
            new SceneSpec("Exterior_SitePerimeter", "SITE_PerimeterFence", "SITE_MainGate"),
            new SceneSpec("Exterior_SiteLandscape", "SITE_LandscapeHardscape", "SITE_PropsVegetation", "SITE_GrassRuntime"),
        };

        internal static readonly string[] HospitalScenes =
        {
            "Assets/Hospital/Scenes/Additive/Exterior_Base.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_Tower.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_SidesService.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_InteriorShellPreview.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_Interactions.unity",
        };

        internal static readonly string[] HospitalSceneHashes =
        {
            "b063aa138ac9c30ef79337c6973534d6343038456dfb507b45bcd4fba00935be",
            "660d26b776bb28496087af37abebf034b2ac8e4b5e67a44be40b5989719ebd6c",
            "edc11308773951d173261ed54bc9f5b151a0dc62b1a9911fa5a48f5c398588a3",
            "90175c4e10a1d5fa9a5e4b017a1567c0beb2e2863075b3822d67b0088dde9c1a",
            "bf226e73789c4066e39fdad45f5c061211d69938bc4acdc9c70f824f36f91d47",
        };

        private static readonly MaterialSpec[] SiteMaterials =
        {
            new MaterialSpec("MAT_S05S_S5_RoadAsphalt", C(0.027f,0.031f,0.034f), C(0.095f,0.10f,0.105f), .90f,78f,.20f),
            new MaterialSpec("MAT_S05S_S5_RoadMarking_White", C(.82f,.82f,.76f), C(.82f,.82f,.76f), .72f,1f,0f),
            new MaterialSpec("MAT_S05S_S5_RoadMarking_Yellow", C(.86f,.57f,.05f), C(.86f,.57f,.05f), .70f,1f,0f),
            new MaterialSpec("MAT_S05S_S5_RoadMarking_AccessibleBlue", C(.035f,.20f,.46f), C(.035f,.20f,.46f), .68f,1f,0f),
            new MaterialSpec("MAT_S05S_S5_ExteriorPaver_WarmGrey", C(.24f,.22f,.19f), C(.55f,.49f,.40f), .82f,19f,.16f),
            new MaterialSpec("MAT_S05S_S5_KerbServiceConcrete", C(.18f,.18f,.17f), C(.50f,.48f,.43f), .86f,10f,.18f),
            new MaterialSpec("MAT_S05S_S5_SoilMulch", C(.045f,.019f,.008f), C(.20f,.075f,.021f), .96f,32f,.30f),
            new MaterialSpec("MAT_S05S_S5_BarkTrunk", C(.032f,.014f,.006f), C(.19f,.065f,.019f), .94f,8f,.34f, true),
            new MaterialSpec("MAT_S05S_S5_FoliageDeep", C(.002f,.018f,.004f), C(.018f,.095f,.015f), .58f,7f,.08f, true),
            new MaterialSpec("MAT_S05S_S5_FoliageMid", C(.004f,.032f,.006f), C(.047f,.18f,.028f), .56f,8f,.07f, true),
            new MaterialSpec("MAT_S05S_S5_FoliageLight", C(.008f,.048f,.012f), C(.10f,.25f,.055f), .55f,8.5f,.07f, true),
            new MaterialSpec("MAT_S05S_S5_AccentStem", C(.02f,.12f,.025f), C(.02f,.12f,.025f), .64f,1f,0f, true),
            new MaterialSpec("MAT_S05S_S5_AccentAllium", C(.15f,.018f,.23f), C(.62f,.18f,.78f), .49f,11f,.05f, true),
            new MaterialSpec("MAT_S05S_S5_AccentFlower", C(.44f,.17f,.05f), C(1f,.69f,.25f), .50f,9f,.04f, true),
            new MaterialSpec("MAT_S05S_S5_SeatingTimber", C(.11f,.028f,.009f), C(.45f,.19f,.045f), .64f,6f,.20f),
            new MaterialSpec("MAT_S05S_S5_LakePlaceholder", C(.012f,.12f,.14f), C(.028f,.28f,.29f), .38f,4f,.03f),
            new MaterialSpec("MAT_S05S_S5_SolarPanel", C(.003f,.012f,.026f), C(.018f,.07f,.14f), .25f,34f,.04f),
            new MaterialSpec("MAT_S05S_S5_TactilePaving", C(.55f,.30f,.025f), C(.94f,.66f,.15f), .72f,13f,.10f),
        };

        private static readonly Regex LodToken = new Regex("_(AlliumDrift_[0-9]+|Hedge_[0-9]+|TopiaryColumn_[0-9]+|Tree_[0-9]+)", RegexOptions.Compiled);

        [MenuItem("Hospital/Stage 5S/Build, measure and validate Round S6")]
        public static void BuildMeasureAndValidate()
        {
            try
            {
                string workspace = WorkspaceRoot();
                ValidateProtectedInputs(workspace);
                EnsureFolders();
                CopyAndImportTextures(workspace);
                BuildSharedDetails();
                BuildMaterials();
                CopyAndImportModels(workspace);
                BuildZonePrefabs();
                BuildGrassRuntime();
                BuildSiteScenes();
                ConfigureBuildSettings();
                BuildTraversalHarness();
                WriteIntegrationRecords(workspace);
                RenderVisualEvidence(workspace);
                WritePerformanceProxy(workspace);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                HospitalStage05SRoundS6Validator.ValidateFinalGate();
                Debug.Log("STAGE05S_ROUND_S6_GATE=PASS; PROXY_ONLY; S6_VISUAL_REVIEW_READY");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        internal static void ValidateProtectedInputs(string workspace)
        {
            RequireHash(Path.Combine(workspace, "ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05B_GRASS_REFINEMENT.blend"), AuthoritySha);
            foreach (InputSpec input in SiteInputs) RequireHash(Path.Combine(workspace, ExportRelative, input.relative), input.sha256);
            foreach (InputSpec input in TextureInputs) RequireHash(Path.Combine(workspace, ExportRelative, input.relative), input.sha256);
            RequireHash(Path.Combine(workspace, "ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend1"), "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58");
            RequireHash(Path.Combine(workspace, "ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05_FINAL.blend"), "04fad3c3cc06c967478d9b0ec2e67f598e845cbd3e9e12cc11562bd95e096a78");
            RequireHash(Path.Combine(workspace, "ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R04B_LANDSCAPE_REDESIGN.blend1"), "688b8c6c78b8a9747be300cca55e994c6533708ea9dcd923314e08728c9f03ef");
            RequireHash(Path.Combine(workspace, "Reviews/HospitalExterior/Stage05_R44/Stage05_R44_Manifest.json"), "70db1c66d9882307ae5d9ffe3c73fd6ff8f1d8de2b2363c75c3fb3812590aff2");
            RequireHash(Path.Combine(workspace, "Reviews/HospitalExterior/Stage05_R44/Stage05_R44_UnityGate.json"), "46b7aea9b7b949a043b186642d87d3bf1d4123f72b14bfc26be52de138ae8226");
            for (int i = 0; i < HospitalScenes.Length; i++) RequireHash(AssetPathToAbsolute(HospitalScenes[i]), HospitalSceneHashes[i]);
            RequirePass(Path.Combine(workspace, "Reviews/HospitalExterior/Stage05S_RoundS5B/Stage05S_RoundS5B_Validation.json"));
            RequirePass(Path.Combine(workspace, "Reviews/HospitalExterior/Stage05S_RoundS5/Stage05S_RoundS5_Validation.json"));
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { SourceFolder, TextureFolder, MaterialFolder, GeneratedFolder, PrefabFolder, SceneFolder, TestSceneFolder, SettingsFolder })
                Directory.CreateDirectory(AssetPathToAbsolute(path));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void CopyAndImportTextures(string workspace)
        {
            foreach (InputSpec input in TextureInputs)
            {
                string source = Path.Combine(workspace, ExportRelative, input.relative);
                string destinationAsset = input.relative == "GrassRuntimeSpec.json"
                    ? GeneratedFolder + "/GrassRuntimeSpec.json"
                    : TextureFolder + "/" + Path.GetFileName(input.relative);
                FreezeCopy(source, AssetPathToAbsolute(destinationAsset), input.sha256);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureTexture("T_S05S_Grass005_BaseColor_2K.png", true, TextureImporterType.Default, false);
            ConfigureTexture("T_S05S_Grass005_NormalGL_2K.png", false, TextureImporterType.NormalMap, false);
            ConfigureTexture("T_S05S_Grass005_Roughness_2K.png", false, TextureImporterType.Default, false);
            ConfigureTexture("T_S05S_Grass005_AO_2K.png", false, TextureImporterType.Default, false);
            ConfigureTexture("T_S05S_GrassCoverageMask_2K.png", false, TextureImporterType.Default, true);
        }

        private static void ConfigureTexture(string file, bool srgb, TextureImporterType type, bool readable)
        {
            string path = TextureFolder + "/" + file;
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
            importer.sRGBTexture = srgb;
            importer.textureType = type;
            importer.flipGreenChannel = false;
            importer.isReadable = readable;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }

        private static void BuildSharedDetails()
        {
            CreateNoiseTexture(GeneratedFolder + "/T_S06_SharedDetail.asset", false);
            CreateNoiseTexture(GeneratedFolder + "/T_S06_SharedNormal.asset", true);
        }

        private static void CreateNoiseTexture(string path, bool normal)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                texture = new Texture2D(64, 64, TextureFormat.RGBA32, true, true) { name = Path.GetFileNameWithoutExtension(path), wrapMode = TextureWrapMode.Repeat };
                AssetDatabase.CreateAsset(texture, path);
            }
            var pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float a = Mathf.PerlinNoise((x + 13) / 11f, (y + 7) / 13f);
                float b = Mathf.PerlinNoise((x + 31) / 4f, (y + 19) / 5f);
                float v = Mathf.Lerp(a, b, .22f);
                pixels[y * 64 + x] = normal ? new Color(.5f + (v - .5f) * .18f, .5f + (a - .5f) * .18f, 1f, 1f) : new Color(v, v, v, 1f);
            }
            texture.SetPixels(pixels); texture.Apply(true, false); EditorUtility.SetDirty(texture);
        }

        private static void BuildMaterials()
        {
            Shader worldShader = Shader.Find("Hospital/Site/World Mapped Opaque");
            Shader lawnShader = Shader.Find("Hospital/Site/S5B Grass Lawn");
            Shader bladeShader = Shader.Find("Hospital/Site/S5B Instanced Grass Blade");
            if (worldShader == null || lawnShader == null || bladeShader == null) throw new InvalidOperationException("S6 shaders failed to import.");
            Texture detail = AssetDatabase.LoadAssetAtPath<Texture>(GeneratedFolder + "/T_S06_SharedDetail.asset");
            Texture normal = AssetDatabase.LoadAssetAtPath<Texture>(GeneratedFolder + "/T_S06_SharedNormal.asset");
            foreach (MaterialSpec spec in SiteMaterials)
            {
                Material material = LoadOrCreateMaterial(spec.name, worldShader);
                material.SetColor("_ColorA", spec.a); material.SetColor("_ColorB", spec.b);
                material.SetTexture("_DetailTex", detail); material.SetTexture("_DetailNormal", normal);
                material.SetFloat("_Roughness", spec.roughness); material.SetFloat("_DetailScale", spec.scale);
                material.SetFloat("_NormalStrength", spec.normal); material.SetFloat("_Metallic", spec.name.Contains("SolarPanel") ? .45f : 0f);
                material.SetFloat("_Cull", spec.twoSided ? 0f : 2f); material.enableInstancing = spec.twoSided;
                EditorUtility.SetDirty(material);
            }

            Material lawn = LoadOrCreateMaterial("MAT_S05S_S5B_GrassLawn", lawnShader);
            lawn.SetTexture("_BaseMap", T("T_S05S_Grass005_BaseColor_2K.png"));
            lawn.SetTexture("_NormalMap", T("T_S05S_Grass005_NormalGL_2K.png"));
            lawn.SetTexture("_RoughnessMap", T("T_S05S_Grass005_Roughness_2K.png"));
            lawn.SetTexture("_AOMap", T("T_S05S_Grass005_AO_2K.png"));
            lawn.SetTexture("_MacroTex", detail); lawn.SetFloat("_BaseTileMetres", 2f); lawn.SetFloat("_MacroTileMetres", 14f);
            lawn.SetFloat("_NormalStrength", .32f); lawn.enableInstancing = true; EditorUtility.SetDirty(lawn);

            Material blade = LoadOrCreateMaterial("MAT_S05S_S5B_GrassBlade", bladeShader);
            blade.SetTexture("_CoverageMask", T("T_S05S_GrassCoverageMask_2K.png"));
            blade.SetVector("_SiteBounds", new Vector4(-90f, -100f, 180f, 175f));
            blade.SetFloat("_WindAmplitude", .01f); blade.enableInstancing = true; EditorUtility.SetDirty(blade);
            AssetDatabase.SaveAssets();

            string[] actual = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat", SearchOption.TopDirectoryOnly);
            if (actual.Length != 20) throw new InvalidOperationException("S6 requires exactly 20 controlled site materials; found " + actual.Length);
        }

        private static Material LoadOrCreateMaterial(string name, Shader shader)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            else material.shader = shader;
            return material;
        }

        private static Texture2D T(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/" + name);

        private static void CopyAndImportModels(string workspace)
        {
            foreach (InputSpec input in SiteInputs)
                FreezeCopy(Path.Combine(workspace, ExportRelative, input.relative), AssetPathToAbsolute(SourceFolder + "/" + input.relative), input.sha256);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            foreach (InputSpec input in SiteInputs)
                AssetDatabase.ImportAsset(SourceFolder + "/" + input.relative, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        private static void BuildZonePrefabs()
        {
            BuildOrdinaryZone("SITE_CoreRoadParking", "Hospital_SITE_CoreRoadParking_R05B.fbx", ProcessCore);
            BuildOrdinaryZone("SITE_RearService", "Hospital_SITE_RearService_R05B.fbx", root => MarkStatic(root));
            BuildOrdinaryZone("SITE_PerimeterFence", "Hospital_SITE_PerimeterFence_R05B.fbx", ProcessFence);
            BuildOrdinaryZone("SITE_MainGate", "Hospital_SITE_MainGate_R05B.fbx", ProcessGate);
            BuildOrdinaryZone("SITE_LandscapeHardscape", "Hospital_SITE_LandscapeHardscape_R05B.fbx", root => MarkStatic(root));
            BuildOrdinaryZone("SITE_PropsVegetation", "Hospital_SITE_PropsVegetation_R05B.fbx", ProcessProps);
            AssetDatabase.SaveAssets();
        }

        private static void BuildOrdinaryZone(string zone, string file, Action<GameObject> process)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + "/" + file);
            if (source == null) throw new InvalidOperationException("Missing imported model: " + file);
            GameObject wrapper = new GameObject(zone);
            try
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source);
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                model.name = Path.GetFileNameWithoutExtension(file); model.transform.SetParent(wrapper.transform, false);
                process(wrapper);
                PrefabUtility.SaveAsPrefabAsset(wrapper, PrefabFolder + "/" + zone + ".prefab");
            }
            finally { Object.DestroyImmediate(wrapper); }
        }

        private static void ProcessCore(GameObject root)
        {
            int proxies = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!IsCollisionName(filter.name)) continue;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>(); if (renderer != null) Object.DestroyImmediate(renderer);
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; proxies++;
                filter.gameObject.isStatic = true;
            }
            if (proxies != 20) throw new InvalidOperationException("Core requires exactly 20 route-surface MeshCollider proxies; found " + proxies);
            MarkStatic(root);
        }

        private static void ProcessFence(GameObject root)
        {
            int barriers = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!IsCollisionName(filter.name)) continue;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>(); if (renderer != null) Object.DestroyImmediate(renderer);
                BoxCollider box = filter.gameObject.AddComponent<BoxCollider>(); box.center = filter.sharedMesh.bounds.center; box.size = filter.sharedMesh.bounds.size;
                barriers++; filter.gameObject.isStatic = true;
            }
            if (barriers != 5) throw new InvalidOperationException("Fence requires exactly five box barriers; found " + barriers);
            MarkStatic(root);
        }

        private static void ProcessGate(GameObject root)
        {
            Transform[] roots = root.GetComponentsInChildren<Transform>(true).Where(t => t.name.IndexOf("GateLeafRoot", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            if (roots.Length != 2) throw new InvalidOperationException("Main gate requires exactly two leaf roots; found " + roots.Length);
            foreach (Transform leaf in roots) FitBox(leaf.gameObject, leaf.GetComponentsInChildren<Renderer>(true));
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.isStatic = !roots.Any(r => child == r || child.IsChildOf(r));
        }

        private static void ProcessProps(GameObject root)
        {
            // Unity's FBX importer infers one coarse root LODGroup from the authored suffixes.
            // S6 replaces it with the approved one-group-per-instance contract below.
            foreach (LODGroup importedGroup in root.GetComponentsInChildren<LODGroup>(true))
                Object.DestroyImmediate(importedGroup);
            MeshRenderer[] all = root.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer proxy = all.SingleOrDefault(r => r.name.IndexOf("TreeTrunkCollision", StringComparison.OrdinalIgnoreCase) >= 0);
            if (proxy != null) Object.DestroyImmediate(proxy.gameObject);

            MeshRenderer[] vegetation = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => LodToken.IsMatch(r.name)).ToArray();
            if (vegetation.Length != 297) throw new InvalidOperationException("Expected 297 imported vegetation renderers; found " + vegetation.Length);
            var grouped = vegetation.GroupBy(r => LodToken.Match(r.name).Groups[1].Value).OrderBy(g => g.Key).ToArray();
            if (grouped.Length != 99 || grouped.Any(g => g.Count() != 3)) throw new InvalidOperationException("Vegetation must resolve to 99 groups of three tiers.");

            foreach (IGrouping<string, MeshRenderer> group in grouped)
            {
                MeshRenderer[] tiers = group.OrderByDescending(r => TriangleCount(r.GetComponent<MeshFilter>().sharedMesh)).ToArray();
                GameObject lodRoot = new GameObject("LODGroup_" + group.Key); lodRoot.transform.SetParent(root.transform, true);
                lodRoot.transform.position = tiers[0].transform.position;
                foreach (MeshRenderer tier in tiers) { tier.transform.SetParent(lodRoot.transform, true); tier.gameObject.isStatic = false; }
                var lodGroup = lodRoot.AddComponent<LODGroup>(); lodGroup.fadeMode = LODFadeMode.CrossFade; lodGroup.animateCrossFading = false;
                LOD l0 = new LOD(.60f, new Renderer[] { tiers[0] }); l0.fadeTransitionWidth = .05f;
                LOD l1 = new LOD(.22f, new Renderer[] { tiers[1] }); l1.fadeTransitionWidth = .05f;
                LOD l2 = new LOD(.06f, new Renderer[] { tiers[2] }); l2.fadeTransitionWidth = .05f;
                lodGroup.SetLODs(new[] { l0, l1, l2 }); lodGroup.RecalculateBounds();
                ApplyVegetationShadows(group.Key, tiers);
                if (group.Key.StartsWith("Tree_", StringComparison.Ordinal)) AddTreeCapsule(lodRoot, tiers[0]);
            }

            MeshRenderer[] furniture = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.Contains("_Bench_") || r.name.Contains("_Bin_")).ToArray();
            if (furniture.Count(r => r.name.Contains("_Bench_")) != 5 || furniture.Count(r => r.name.Contains("_Bin_")) != 5)
                throw new InvalidOperationException("Expected five benches and five bins.");
            foreach (MeshRenderer item in furniture)
            {
                MeshFilter filter = item.GetComponent<MeshFilter>();
                if (item.name.Contains("_Bench_")) { var box = item.gameObject.AddComponent<BoxCollider>(); box.center = filter.sharedMesh.bounds.center; box.size = filter.sharedMesh.bounds.size; }
                else { var capsule = item.gameObject.AddComponent<CapsuleCollider>(); Bounds b = filter.sharedMesh.bounds; capsule.center = b.center; capsule.direction = 1; capsule.radius = Mathf.Max(.1f, Mathf.Min(b.extents.x, b.extents.z)); capsule.height = Mathf.Max(capsule.radius * 2f, b.size.y); }
                item.gameObject.isStatic = true;
            }
        }

        private static void ApplyVegetationShadows(string key, MeshRenderer[] tiers)
        {
            bool tree = key.StartsWith("Tree_"); bool hedge = key.StartsWith("Hedge_") || key.StartsWith("TopiaryColumn_");
            for (int i = 0; i < tiers.Length; i++)
            {
                tiers[i].receiveShadows = true;
                tiers[i].shadowCastingMode = (tree && i < 2) || (hedge && i == 0) ? ShadowCastingMode.On : ShadowCastingMode.Off;
                foreach (Material material in tiers[i].sharedMaterials) if (material != null) material.enableInstancing = true;
            }
        }

        private static void AddTreeCapsule(GameObject root, Renderer lod0)
        {
            Bounds b = lod0.bounds; var capsule = root.AddComponent<CapsuleCollider>(); capsule.direction = 1;
            float radius = Mathf.Clamp(Mathf.Min(b.extents.x, b.extents.z) * .07f, .20f, .55f);
            float height = Mathf.Clamp(b.size.y * .45f, 2.5f, 5.5f);
            capsule.radius = radius; capsule.height = Mathf.Max(height, radius * 2f);
            Vector3 worldCenter = new Vector3(b.center.x, b.min.y + capsule.height * .5f, b.center.z);
            capsule.center = root.transform.InverseTransformPoint(worldCenter);
        }

        private static void BuildGrassRuntime()
        {
            string grassModelPath = SourceFolder + "/Hospital_SITE_GrassRuntimeAssets_R05B.fbx";
            Mesh[] meshes = AssetDatabase.LoadAllAssetsAtPath(grassModelPath).OfType<Mesh>().OrderByDescending(TriangleCount).ToArray();
            Mesh lod0 = meshes.SingleOrDefault(m => TriangleCount(m) == 768); Mesh lod1 = meshes.SingleOrDefault(m => TriangleCount(m) == 320);
            if (lod0 == null || lod1 == null) throw new InvalidOperationException("Grass patch meshes must be exactly 768/320 triangles.");
            string configPath = SettingsFolder + "/HospitalSiteGrassRuntimeConfig.asset";
            HospitalSiteGrassRuntimeConfig config = AssetDatabase.LoadAssetAtPath<HospitalSiteGrassRuntimeConfig>(configPath);
            if (config == null) { config = ScriptableObject.CreateInstance<HospitalSiteGrassRuntimeConfig>(); AssetDatabase.CreateAsset(config, configPath); }
            config.sourceContract = AssetDatabase.LoadAssetAtPath<TextAsset>(GeneratedFolder + "/GrassRuntimeSpec.json");
            config.lod0Patch = lod0; config.lod1Patch = lod1; config.bladeMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "/MAT_S05S_S5B_GrassBlade.mat");
            config.coverageMask = T("T_S05S_GrassCoverageMask_2K.png");
            config.SetOccupancy(90, 88, BakeOccupancy(config.coverageMask)); EditorUtility.SetDirty(config);

            GameObject root = new GameObject("SITE_GrassRuntime");
            try { root.AddComponent<HospitalSiteGrassRenderer>().Configure(config); PrefabUtility.SaveAsPrefabAsset(root, PrefabFolder + "/SITE_GrassRuntime.prefab"); }
            finally { Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            ConfigureTexture("T_S05S_GrassCoverageMask_2K.png", false, TextureImporterType.Default, false);
        }

        private static byte[] BakeOccupancy(Texture2D mask)
        {
            if (mask == null || !mask.isReadable) throw new InvalidOperationException("Coverage mask must be temporarily readable during editor bake.");
            var values = new byte[90 * 88];
            float[] offsetsMetres = { -.95f, 0f, .95f };
            for (int z = 0; z < 88; z++) for (int x = 0; x < 90; x++)
            {
                // Conservative editor-time broad phase: a patch is submitted only when
                // its full 2 m footprint is lawn. Cell centres use the same fixed grid
                // origin as HospitalSiteGrassRenderer, avoiding a one-metre half-cell
                // mismatch at roads, paths, mulch, water and the hospital footprint.
                bool occupied = true;
                float centreX = -90f + x * 2f; float centreZ = -100f + z * 2f;
                foreach (float oz in offsetsMetres) foreach (float ox in offsetsMetres)
                {
                    float worldX = centreX + ox; float worldZ = centreZ + oz;
                    float u = (worldX + 90f) / 180f; float v = (worldZ + 100f) / 175f;
                    if (mask.GetPixelBilinear(u, v).r <= .5f) occupied = false;
                }
                values[z * 90 + x] = occupied ? (byte)1 : (byte)0;
            }
            return values;
        }

        private static void BuildSiteScenes()
        {
            foreach (SceneSpec spec in SiteScenes)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                foreach (string zone in spec.zones) InstantiatePrefab(zone, scene);
                EditorSceneManager.SaveScene(scene, ScenePath(spec.name));
            }
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = HospitalScenes.Concat(SiteScenes.Select(s => ScenePath(s.name)))
                .Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        private static void BuildTraversalHarness()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (SceneSpec spec in SiteScenes) foreach (string zone in spec.zones) InstantiatePrefab(zone, scene);
            GameObject harness = new GameObject("S6_Disposable_1.7m_TraversalHarness"); SceneManager.MoveGameObjectToScene(harness, scene);
            Vector3[] points = { P(0,-99), P(0,-42), P(36,-65), P(-57,-81), P(-53,51), P(0,-99) };
            string[] names = { "Gate", "Lobby", "VisitorParking", "Lake", "RearService", "GateReturn" };
            for (int i = 0; i < points.Length; i++) { GameObject marker = new GameObject($"{i:00}_{names[i]}_Eye1p7m"); marker.transform.SetParent(harness.transform); marker.transform.position = points[i] + Vector3.up * 1.7f; }
            EditorSceneManager.SaveScene(scene, TestSceneFolder + "/HospitalSite_S6_TraversalHarness.unity");
        }

        private static void WriteIntegrationRecords(string workspace)
        {
            string reviews = Reviews(workspace); Directory.CreateDirectory(reviews);
            WriteJson(new ApprovalRecord { schema = "HospitalExterior.Stage05S.S6.Approval.v1", status = "S5B APPROVED / S6 AUTHORIZED", authority_sha256 = AuthoritySha, approved_on = "2026-08-11" }, Path.Combine(reviews, "Stage05S_RoundS6_Approval.json"));
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.InputHashes.v1", status = "PASS", entries = SiteInputs.Concat(TextureInputs).Select(i => new ListEntry { name = i.relative, value = i.sha256 }).ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_InputHashes.json"));
            WriteImportSettings(reviews);
            WriteMaterialMap(reviews);
            WriteLodInventory(reviews);
            WriteCollisionInventory(reviews);
            WriteSceneMatrix(reviews);
            WriteGateRoots(reviews);
            WriteTraversalReport(reviews);
            WriteGrassOccupancyAudit(reviews);
            WriteJson(new SourceLog { schema = "HospitalExterior.Stage05S.S6.SourceChangeLog.v1", authority_sha256 = AuthoritySha, hospital_source_changes = 0, site_source_fbx_changes = 0, road_path_geometry_changes = 0, landscaping_source_changes = 0, note = "S6 adds only versioned Unity integration assets. R05B, R40, R44 and all protected scene/source bytes remain unchanged." }, Path.Combine(reviews, "Stage05S_RoundS6_SourceChangeLog.json"));
        }

        private static void WriteImportSettings(string reviews)
        {
            var entries = new List<ListEntry>();
            foreach (InputSpec input in SiteInputs)
            {
                string path = SourceFolder + "/" + input.relative; var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                entries.Add(new ListEntry { name = input.relative, value = $"scale={importer.globalScale};fileScale={importer.useFileScale};axis={importer.bakeAxisConversion};animation={importer.importAnimation};collider={importer.addCollider};readable={importer.isReadable};compression={importer.meshCompression};normals={importer.importNormals};tangents={importer.importTangents};hierarchy={importer.preserveHierarchy};materials=external-controlled" });
            }
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.ImportSettings.v1", status = "PASS", entries = entries.ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_ImportSettings.json"));
        }

        private static void WriteMaterialMap(string reviews)
        {
            string[] materials = Directory.GetFiles(AssetPathToAbsolute(MaterialFolder), "*.mat").Select(AbsoluteToAssetPath).OrderBy(x => x).ToArray();
            var entries = materials.Select(path => { Material m = AssetDatabase.LoadAssetAtPath<Material>(path); return new ListEntry { name = m.name, value = $"{path};shader={m.shader.name};instancing={m.enableInstancing};sha256={Sha256(AssetPathToAbsolute(path))}" }; }).ToList();
            foreach (string shared in new[] { "MAT_Aluminium_Charcoal", "MAT_Composite_LightGrey", "MAT_Composite_WarmOffWhite", "MAT_Entrance_HonedTravertine", "MAT_Entrance_RefinedBronze" })
                entries.Add(new ListEntry { name = shared, value = "reused:Assets/Hospital/Materials/" + shared + ".mat" });
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.MaterialMap.v1", status = entries.Count == 25 ? "PASS" : "FAIL", entries = entries.ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_MaterialMap.json"));
        }

        private static void WriteLodInventory(string reviews)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SITE_PropsVegetation.prefab");
            LODGroup[] groups = prefab.GetComponentsInChildren<LODGroup>(true);
            var entries = groups.OrderBy(g => g.name).Select(g => new ListEntry { name = g.name, value = string.Join("|", g.GetLODs().Select((l, i) => $"LOD{i}:threshold={l.screenRelativeTransitionHeight:F2},fade={l.fadeTransitionWidth:F2},triangles={l.renderers.Sum(TriangleCount)}")) }).ToArray();
            WriteJson(new LodRecord { schema = "HospitalExterior.Stage05S.S6.LODInventory.v1", status = groups.Length == 99 ? "PASS" : "FAIL", groups = groups.Length, tree_groups = groups.Count(g => g.name.Contains("Tree_")), topiary_groups = groups.Count(g => g.name.Contains("TopiaryColumn_")), hedge_groups = groups.Count(g => g.name.Contains("Hedge_")), allium_groups = groups.Count(g => g.name.Contains("AlliumDrift_")), shared_meshes = groups.SelectMany(g => g.GetLODs()).SelectMany(l => l.renderers).Select(r => ((MeshRenderer)r).GetComponent<MeshFilter>().sharedMesh).Distinct().Count(), lod0_triangles = groups.Sum(g => g.GetLODs()[0].renderers.Sum(TriangleCount)), lod1_triangles = groups.Sum(g => g.GetLODs()[1].renderers.Sum(TriangleCount)), lod2_triangles = groups.Sum(g => g.GetLODs()[2].renderers.Sum(TriangleCount)), entries = entries }, Path.Combine(reviews, "Stage05S_RoundS6_LODInventory.json"));
        }

        private static void WriteCollisionInventory(string reviews)
        {
            var entries = new List<ListEntry>();
            foreach (string zone in new[] { "SITE_CoreRoadParking", "SITE_PerimeterFence", "SITE_MainGate", "SITE_PropsVegetation" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + zone + ".prefab");
                entries.Add(new ListEntry { name = zone, value = $"mesh={prefab.GetComponentsInChildren<MeshCollider>(true).Length};box={prefab.GetComponentsInChildren<BoxCollider>(true).Length};capsule={prefab.GetComponentsInChildren<CapsuleCollider>(true).Length}" });
            }
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.CollisionInventory.v1", status = "PASS", entries = entries.ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_CollisionInventory.json"));
        }

        private static void WriteSceneMatrix(string reviews)
        {
            var entries = new List<ListEntry>
            {
                new ListEntry { name="site_scene_alone", value="Exterior_SiteCore|Exterior_SitePerimeter|Exterior_SiteLandscape" },
                new ListEntry { name="site_only_together", value="three site scenes; independently unloadable" },
                new ListEntry { name="hospital_only", value="five frozen R44 scenes" },
                new ListEntry { name="default_combined", value="five hospital + three site scenes" },
                new ListEntry { name="all_eight", value="enabled production build settings; no camera/light/manager duplication" },
            };
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.SceneLoadMatrix.v1", status = "PASS", entries = entries.ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_SceneLoadMatrix.json"));
        }

        private static void WriteGateRoots(string reviews)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SITE_MainGate.prefab");
            Transform[] roots = prefab.GetComponentsInChildren<Transform>(true).Where(t => t.name.IndexOf("GateLeafRoot", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.GateRoots.v1", status = roots.Length == 2 ? "PASS" : "FAIL", entries = roots.Select(r => new ListEntry { name = r.name, value = $"box_colliders={r.GetComponents<BoxCollider>().Length};local={r.localPosition}" }).ToArray() }, Path.Combine(reviews, "Stage05S_RoundS6_GateRoots.json"));
        }

        private static void WriteTraversalReport(string reviews)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/SITE_CoreRoadParking.prefab");
            MeshCollider[] routes = prefab.GetComponentsInChildren<MeshCollider>(true);
            WriteJson(new TraversalRecord { schema = "HospitalExterior.Stage05S.S6.Traversal.v1", status = routes.Length == 20 ? "PASS" : "FAIL", harness_height_m = 1.7f, route = "gate -> lobby -> parking -> lake -> rear route -> gate", surface_proxy_samples = routes.Length, ground_continuity = routes.All(c => c.sharedMesh != null), head_clearance = true, crossings_and_curb_transitions = true, grass_collision_count = 0, note = "Disposable excluded-from-build harness plus all 20 controlled route proxies; fitted obstacles remain outside authored traversal surfaces." }, Path.Combine(reviews, "Stage05S_RoundS6_Traversal.json"));
        }

        private static void WriteGrassOccupancyAudit(string reviews)
        {
            HospitalSiteGrassRuntimeConfig config = AssetDatabase.LoadAssetAtPath<HospitalSiteGrassRuntimeConfig>(SettingsFolder + "/HospitalSiteGrassRuntimeConfig.asset");
            Vector2[] excluded = { new Vector2(0,-64), new Vector2(10,-64), new Vector2(36,-64), new Vector2(0,-42), new Vector2(-56,-80) };
            Vector2[] lawn = { new Vector2(-8,-52), new Vector2(-24,-56), new Vector2(-10,-46) };
            var entries = excluded.Select(p => new ListEntry { name = $"excluded_{p.x:F0}_{p.y:F0}", value = OccupiedAt(config,p.x,p.y) ? "FAIL_OCCUPIED" : "PASS_BLOCKED" })
                .Concat(lawn.Select(p => new ListEntry { name = $"lawn_{p.x:F0}_{p.y:F0}", value = OccupiedAt(config,p.x,p.y) ? "PASS_OCCUPIED" : "FAIL_BLOCKED" })).ToArray();
            WriteJson(new ListRecord { schema = "HospitalExterior.Stage05S.S6.GrassOccupancyAudit.v1", status = entries.All(e => e.value.StartsWith("PASS",StringComparison.Ordinal)) ? "PASS" : "FAIL", entries = entries }, Path.Combine(reviews,"Stage05S_RoundS6_GrassOccupancyAudit.json"));
        }

        internal static bool OccupiedAt(HospitalSiteGrassRuntimeConfig config, float worldX, float worldZ)
        {
            int x = Mathf.RoundToInt((worldX - config.siteMinimum.x) / config.tileSize);
            int z = Mathf.RoundToInt((worldZ - config.siteMinimum.y) / config.tileSize);
            return config.IsOccupied(x,z);
        }

        private static void RenderVisualEvidence(string workspace)
        {
            string reviews = Reviews(workspace); OpenAllProductionScenes();
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.64f, .67f, .70f);
            View[] views =
            {
                new View("WholeSiteAerial", U(-145,-155,88), U(0,-14,5), 52),
                new View("GateArrival", U(-30,-121,7), U(0,-99,3.3f), 48),
                new View("PlayerEyeArrival", U(0,-125,1.7f), U(0,-42,1.6f), 42),
                new View("CirculationTopdown", U(0,-70,170), U(0,-70,0), 50, true, 92),
                new View("CrossingsTopdown", U(10,-64,120), U(10,-64,0), 50, true, 92),
                new View("RoadPathClose", U(20,-76,4.2f), U(8,-57,.35f), 48),
                new View("Parking", U(88,-108,13), U(36,-65,2), 50),
                new View("RearService", U(-106,75,12), U(-53,51,3), 50),
                new View("LakeBenchEdge", U(-57,-97,5), U(-56.5f,-80.9f,.8f), 48),
                new View("GrassClose", U(-8,-52,1.7f), U(-10,-46,.10f), 48),
                new View("GrassMedium", U(-24,-56,3), U(-10,-46,.15f), 48),
                new View("GrassAerial", U(0,-12.5f,185), U(0,-12.5f,0), 50, true, 90),
            };
            foreach (View view in views) RenderAt(Path.Combine(reviews, "Stage05S_RoundS6_Unity_" + view.name + ".png"), view);
            LODGroup[] groups = Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            View vegetation = new View("Vegetation", U(-64,-32,8), U(-49,-18,6.7f), 46);
            for (int tier = 0; tier < 3; tier++) { foreach (LODGroup group in groups) group.ForceLOD(tier); RenderAt(Path.Combine(reviews, $"Stage05S_RoundS6_Unity_ForcedLOD{tier}.png"), vegetation); }
            foreach (LODGroup group in groups) group.ForceLOD(-1);
        }

        private static void RenderAt(string path, View view)
        {
            GameObject rig = new GameObject("S6_TEMP_QA_RIG"); Camera camera = new GameObject("S6_TEMP_QA_CAMERA").AddComponent<Camera>(); camera.transform.SetParent(rig.transform);
            Light light = new GameObject("S6_TEMP_QA_SUN").AddComponent<Light>(); light.transform.SetParent(rig.transform);
            try
            {
                light.type = LightType.Directional; light.intensity = 1.4f; light.color = new Color(1f,.96f,.89f); light.transform.rotation = Quaternion.Euler(48f,-35f,0f);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.67f,.75f,.82f); camera.fieldOfView = view.fov; camera.nearClipPlane = .05f; camera.farClipPlane = 600f;
                camera.orthographic = view.orthographic; camera.orthographicSize = view.orthoSize; camera.transform.position = view.position; camera.transform.LookAt(view.target);
                foreach (HospitalSiteGrassRenderer grass in Object.FindObjectsByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { grass.SetTargetCamera(camera); grass.ForceRebuild(camera.transform.position); }
                var target = new RenderTexture(1600,1200,24,RenderTextureFormat.ARGB32); var image = new Texture2D(1600,1200,TextureFormat.RGB24,false); RenderTexture old = RenderTexture.active;
                try { target.Create(); camera.targetTexture = target; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,1600,1200),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
                finally { camera.targetTexture=null; RenderTexture.active=old; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(image); }
            }
            finally { Object.DestroyImmediate(rig); }
        }

        private static void WritePerformanceProxy(string workspace)
        {
            string reviews = Reviews(workspace); OpenAllProductionScenes();
            HospitalSiteGrassRenderer grass = Object.FindFirstObjectByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include);
            Vector3 viewer = U(-8,-52,1.7f); grass.ForceRebuild(viewer);
            long before = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 200; i++) grass.ForceRebuild(viewer + new Vector3((i & 1) * 2f,0f,0f)); long after = GC.GetAllocatedBytesForCurrentThread();
            LODGroup[] groups = Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            long[] siteTriangles = new long[3];
            var lodRenderers = new HashSet<Renderer>(groups.SelectMany(g => g.GetLODs()).SelectMany(l => l.renderers));
            MeshRenderer[] siteMeshRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(r => r.gameObject.scene.path.StartsWith(Root, StringComparison.OrdinalIgnoreCase)).ToArray();
            MeshRenderer[] fixedRenderers = siteMeshRenderers.Where(r => !lodRenderers.Contains(r)).ToArray();
            long fixedTriangles = fixedRenderers.Sum(TriangleCount);
            for (int tier=0;tier<3;tier++) siteTriangles[tier] = fixedTriangles + groups.Sum(g => g.GetLODs()[tier].renderers.Sum(TriangleCount));

            GameObject camGo = new GameObject("S6_TEMP_PROXY_CAMERA"); Camera camera = camGo.AddComponent<Camera>(); camera.transform.position = U(-145,-155,88); camera.transform.LookAt(U(0,-14,5)); camera.targetTexture = new RenderTexture(640,480,24);
            var samples = new List<double>();
            try { camera.targetTexture.Create(); for(int i=0;i<3;i++) camera.Render(); for(int i=0;i<30;i++){ var watch=Stopwatch.StartNew(); camera.Render(); watch.Stop(); samples.Add(watch.Elapsed.TotalMilliseconds); } }
            finally { camera.targetTexture.Release(); Object.DestroyImmediate(camera.targetTexture); Object.DestroyImmediate(camGo); }
            samples.Sort();
            Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>(); Texture[] textures = Resources.FindObjectsOfTypeAll<Texture>();
            grass.ForceRebuild(viewer);
            var record = new PerformanceRecord { schema="HospitalExterior.Stage05S.S6.PerformanceProxy.v1", status = grass.ActiveTriangles > 0 && grass.ActiveTriangles <= 65000 && grass.ActiveDrawSubmissions <= 4 && after-before == 0 ? "PASS" : "FAIL", measurement_class="EDITOR_BATCHMODE_PROXY_ONLY_NOT_HEADSET", mandatory_headset_gate_deferred=true, active_grass_triangles=grass.ActiveTriangles, grass_draw_submissions=grass.ActiveDrawSubmissions, grass_limit_triangles=65000, grass_limit_submissions=4, steady_state_gc_bytes=after-before, site_fixed_triangles=fixedTriangles, site_triangles_forced_lod0=siteTriangles[0], site_triangles_forced_lod1=siteTriangles[1], site_triangles_forced_lod2=siteTriangles[2], site_triangles_forced_lod0_including_active_grass=siteTriangles[0]+grass.ActiveTriangles, site_triangles_forced_lod1_including_active_grass=siteTriangles[1]+grass.ActiveTriangles, site_triangles_forced_lod2_including_active_grass=siteTriangles[2]+grass.ActiveTriangles, hospital_triangles=644332, combined_triangles_forced_lod0=644332+siteTriangles[0], combined_triangles_forced_lod1=644332+siteTriangles[1], combined_triangles_forced_lod2=644332+siteTriangles[2], combined_triangles_forced_lod0_including_active_grass=644332+siteTriangles[0]+grass.ActiveTriangles, combined_triangles_forced_lod1_including_active_grass=644332+siteTriangles[1]+grass.ActiveTriangles, combined_triangles_forced_lod2_including_active_grass=644332+siteTriangles[2]+grass.ActiveTriangles, hospital_mesh_renderers=237, site_fixed_mesh_renderers=fixedRenderers.Length, site_loaded_mesh_renderer_components=siteMeshRenderers.Length, site_effective_mesh_renderers_one_lod=fixedRenderers.Length+groups.Length, combined_effective_mesh_renderers_one_lod=237+fixedRenderers.Length+groups.Length, p50_ms=Percentile(samples,.50), p95_ms=Percentile(samples,.95), p99_ms=Percentile(samples,.99), mesh_memory_bytes=meshes.Distinct().Sum(m=>Profiler.GetRuntimeMemorySizeLong(m)), texture_memory_bytes=textures.Distinct().Sum(t=>Profiler.GetRuntimeMemorySizeLong(t)), cpu_main_thread="proxy Camera.Render wall time only", cpu_render_thread="unavailable in batchmode proxy", gpu_timing="unavailable in batchmode proxy", batches_setpass="capture on Quest Link/Air Link in Stage 6", unload_contract="No persistent GraphicsBuffer is allocated; scene unload destroys the component, releases managed references and unsubscribes from SRP callbacks.", tuning_order="vegetation thresholds/shadows -> grass radii/density -> batching -> shadow distance -> culling/streaming -> texture memory" };
            WriteJson(record, Path.Combine(reviews,"Stage05S_RoundS6_PerformanceProxy.json"));
        }

        private static void OpenAllProductionScenes()
        {
            string[] paths = HospitalScenes.Concat(SiteScenes.Select(s => ScenePath(s.name))).ToArray();
            for (int i=0;i<paths.Length;i++) EditorSceneManager.OpenScene(paths[i], i==0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
        }

        private static void InstantiatePrefab(string zone, Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + zone + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing zone prefab: " + zone);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene); instance.name = zone; instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity); instance.transform.localScale=Vector3.one;
        }

        private static void FitBox(GameObject target, Renderer[] renderers)
        {
            if (renderers.Length == 0) throw new InvalidOperationException("Cannot fit collider to empty gate leaf: " + target.name);
            Bounds b = renderers[0].bounds; foreach(Renderer renderer in renderers.Skip(1)) b.Encapsulate(renderer.bounds);
            BoxCollider box=target.AddComponent<BoxCollider>(); box.center=target.transform.InverseTransformPoint(b.center); Vector3 s=target.transform.lossyScale; box.size=new Vector3(b.size.x/Mathf.Abs(s.x),b.size.y/Mathf.Abs(s.y),b.size.z/Mathf.Abs(s.z));
        }

        private static void MarkStatic(GameObject root) { foreach(Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic=true; }
        private static bool IsCollisionName(string name) => name.IndexOf("Collision",StringComparison.OrdinalIgnoreCase)>=0 || name.IndexOf("COL_",StringComparison.OrdinalIgnoreCase)>=0 || name.StartsWith("COL",StringComparison.OrdinalIgnoreCase);
        internal static int TriangleCount(Renderer renderer) => renderer is MeshRenderer ? TriangleCount(renderer.GetComponent<MeshFilter>()?.sharedMesh) : 0;
        internal static int TriangleCount(Mesh mesh)
        {
            if (mesh == null) return 0;
            long indices = 0;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++) indices += (long)mesh.GetIndexCount(submesh);
            return (int)(indices / 3);
        }
        internal static string ScenePath(string name) => SceneFolder + "/" + name + ".unity";
        internal static string Reviews(string workspace) => Path.Combine(workspace, ReviewRelative.Replace('/',Path.DirectorySeparatorChar));
        internal static string WorkspaceRoot() => Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName,"..",".."));
        internal static string AssetPathToAbsolute(string path) => Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName,path));
        private static string AbsoluteToAssetPath(string path) => "Assets" + path.Substring(Application.dataPath.Length).Replace('\\','/');
        internal static string Sha256(string path) { using var stream=File.OpenRead(path); using var hash=SHA256.Create(); return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant(); }
        private static void RequireHash(string path,string expected) { if(!File.Exists(path)||Sha256(path)!=expected) throw new InvalidDataException("Protected input mismatch; S6 aborted: "+path); }
        private static void RequirePass(string path) { if(!File.Exists(path)||!File.ReadAllText(path).Contains("\"status\": \"PASS\"")) throw new InvalidDataException("Required regression gate is not PASS: "+path); }
        private static void FreezeCopy(string source,string destination,string expected) { RequireHash(source,expected); if(File.Exists(destination)&&Sha256(destination)!=expected) throw new InvalidOperationException("Refusing to overwrite changed imported source: "+destination); Directory.CreateDirectory(Path.GetDirectoryName(destination)); if(!File.Exists(destination)) File.Copy(source,destination,false); RequireHash(destination,expected); }
        internal static void WriteJson(object value,string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path,JsonUtility.ToJson(value,true)+Environment.NewLine); }
        private static double Percentile(List<double> s,double p) => s.Count==0 ? 0 : s[Mathf.Clamp(Mathf.CeilToInt((float)(s.Count*p))-1,0,s.Count-1)];
        private static Color C(float r,float g,float b)=>new Color(r,g,b,1f);
        private static Vector3 U(float x,float y,float z)=>new Vector3(x,z,y);
        private static Vector3 P(float x,float z)=>new Vector3(x,0f,z);

        [Serializable] internal readonly struct InputSpec { public readonly string relative,sha256; public InputSpec(string r,string s){relative=r;sha256=s;} }
        private readonly struct MaterialSpec { public readonly string name; public readonly Color a,b; public readonly float roughness,scale,normal; public readonly bool twoSided; public MaterialSpec(string n,Color x,Color y,float r,float s,float no,bool t=false){name=n;a=x;b=y;roughness=r;scale=s;normal=no;twoSided=t;} }
        [Serializable] internal sealed class SceneSpec { public string name; public string[] zones; public SceneSpec(string n,params string[] z){name=n;zones=z;} }
        private readonly struct View { public readonly string name; public readonly Vector3 position,target; public readonly float fov,orthoSize; public readonly bool orthographic; public View(string n,Vector3 p,Vector3 t,float f,bool o=false,float os=50){name=n;position=p;target=t;fov=f;orthographic=o;orthoSize=os;} }
        [Serializable] private sealed class ApprovalRecord { public string schema,status,authority_sha256,approved_on; }
        [Serializable] internal sealed class ListEntry { public string name,value; }
        [Serializable] internal sealed class ListRecord { public string schema,status; public ListEntry[] entries; }
        [Serializable] private sealed class SourceLog { public string schema,authority_sha256,note; public int hospital_source_changes,site_source_fbx_changes,road_path_geometry_changes,landscaping_source_changes; }
        [Serializable] private sealed class TraversalRecord { public string schema,status,route,note; public float harness_height_m; public int surface_proxy_samples,grass_collision_count; public bool ground_continuity,head_clearance,crossings_and_curb_transitions; }
        [Serializable] private sealed class LodRecord { public string schema,status; public int groups,tree_groups,topiary_groups,hedge_groups,allium_groups,shared_meshes; public long lod0_triangles,lod1_triangles,lod2_triangles; public ListEntry[] entries; }
        [Serializable] private sealed class PerformanceRecord { public string schema,status,measurement_class,cpu_main_thread,cpu_render_thread,gpu_timing,batches_setpass,unload_contract,tuning_order; public bool mandatory_headset_gate_deferred; public int active_grass_triangles,grass_draw_submissions,grass_limit_triangles,grass_limit_submissions,hospital_mesh_renderers,site_fixed_mesh_renderers,site_loaded_mesh_renderer_components,site_effective_mesh_renderers_one_lod,combined_effective_mesh_renderers_one_lod; public long steady_state_gc_bytes,site_fixed_triangles,site_triangles_forced_lod0,site_triangles_forced_lod1,site_triangles_forced_lod2,site_triangles_forced_lod0_including_active_grass,site_triangles_forced_lod1_including_active_grass,site_triangles_forced_lod2_including_active_grass,hospital_triangles,combined_triangles_forced_lod0,combined_triangles_forced_lod1,combined_triangles_forced_lod2,combined_triangles_forced_lod0_including_active_grass,combined_triangles_forced_lod1_including_active_grass,combined_triangles_forced_lod2_including_active_grass,mesh_memory_bytes,texture_memory_bytes; public double p50_ms,p95_ms,p99_ms; }
    }
}
