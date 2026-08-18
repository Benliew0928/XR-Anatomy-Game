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
    public static class HospitalInteriorS3BR03Builder
    {
        private const string ApprovedCompleteStatus = "S3B_APPROVED_COMPLETE_S3C_AUTHORIZED_NOT_STARTED";
        public const string Root = HospitalInteriorS2R03Builder.Root;
        public const string S3BFolder = Root + "/S3B";
        public const string SourceFbxFolder = S3BFolder + "/SourceFBX";
        public const string MaterialFolder = S3BFolder + "/Materials";
        public const string GeneratedFolder = S3BFolder + "/Generated";
        public const string CabinFbxPath = SourceFbxFolder + "/HospitalInterior_E01_Cabin_R03.fbx";
        public const string LandingFbxPath = SourceFbxFolder + "/HospitalInterior_E01_LandingPortal_R03.fbx";
        public const string ContractPath = Root + "/Settings/HospitalInterior_S3_R03_ElevatorContract.asset";
        public const string BootstrapScenePath = Root + "/Scenes/HospitalInterior_S3B_R03_Doors_SIM_LOCAL.unity";
        public const string TerrazzoTexturePath = GeneratedFolder + "/TEX_S3B_R03_DarkTerrazzo.asset";
        public const string F00ShaftCutMeshPath = GeneratedFolder + "/HospitalInterior_S3B_R03_F00_ShaftCutSlab.asset";
        public const string UpperShaftCutMeshPath = GeneratedFolder + "/HospitalInterior_S3B_R03_Upper_ShaftCutSlab.asset";
        public const string UpperShaftCutUndersidePath = GeneratedFolder + "/HospitalInterior_S3B_R03_Upper_ShaftCutUnderside.asset";

        public static readonly string[] PlayerScenes = new[] { BootstrapScenePath }
            .Concat(HospitalInteriorS2R03Builder.ProductionScenes)
            .Concat(new[] { HospitalInteriorS2R03Builder.FloorScenePaths[0] }).ToArray();

        private static readonly string[] CabinColliderNames =
        {
            "S3A_R03_E01_CabinFloor",
            "S3A_R03_E01_CabinCeiling",
            "S3A_R03_E01_RearWall",
            "S3A_R03_E01_LSideWall",
            "S3A_R03_E01_RSideWall",
            "S3A_R03_E01_LFrontPocketReturn",
            "S3A_R03_E01_RFrontPocketReturn",
            "S3A_R03_E01_InteriorDoorHeader",
        };

        private static readonly string[] LandingColliderNames =
        {
            "S3A_R03_E01_LandingWall_L",
            "S3A_R03_E01_LandingWall_R",
            "S3A_R03_E01_LandingWall_Header",
            "S3A_R03_E01_LandingFrame_L",
            "S3A_R03_E01_LandingFrame_R",
            "S3A_R03_E01_LandingFrame_Header",
            "S3A_R03_E01_LandingThreshold",
        };

        [Serializable]
        private sealed class BaselineManifest
        {
            public BaselineFile[] files;
        }

        [Serializable]
        private sealed class BaselineFile
        {
            public string path;
            public string sha256;
        }

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = "HospitalInterior.R03.S3B.BuildRecord.v4";
            public string status;
            public string unityVersion;
            public string contract;
            public string cabinFbx;
            public string landingFbx;
            public string bootstrapScene;
            public string[] playerScenes;
            public string playerExecutable;
            public string externalCabinSha256;
            public string importedCabinSha256;
            public string externalLandingSha256;
            public string importedLandingSha256;
            public int s2BaselineFileCount;
            public int s2BaselineMismatchCount;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public float shaftMinY;
            public float shaftMaxY;
            public string shaftInnerEnvelope;
            public string shaftFloorApertureEnvelope;
            public string[] shaftCutMeshAssets;
            public string shaftArchitecturalFinish;
            public int shaftGlassPanelCount;
            public int shaftFrameMemberCount;
            public string cabinObservationFinish;
            public int cabinObservationGlassWallCount;
            public int cabinObservationFrameCount;
            public float minimumShaftRunningClearanceM;
            public float f06TopOverrunM;
        }

        [Serializable]
        private sealed class GateStatus
        {
            public string status;
        }

        [MenuItem("Hospital Interior/R03 S3B/Build normal door review stage", priority = 1)]
        public static void BuildStage()
        {
            RequireInputs();
            EnsureFolders();
            AssertApprovedS2Baseline(out int baselineCount);
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);

            Dictionary<string, Material> materials = CreateMaterials();
            ImportApprovedFbxSources();
            S3ElevatorContract contract = CreateContract();
            CreateShaftCutFloorMeshes(contract);
            BuildBootstrapScene(contract, materials);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssertApprovedS2Baseline(out int baselineCountAfter);
            string settingsAfter = Sha256(settingsPath);
            if (baselineCountAfter != baselineCount)
                throw new InvalidOperationException("Approved S2 baseline count changed during S3B build.");
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed while building S3B.");
            AssertImportedFbxHashes();
            WriteBuildRecord("SCENE_BUILT_PENDING_VALIDATION", string.Empty, settingsAfter, true, baselineCount);
            WriteCheckpoint("IMPLEMENTED_PENDING_AUTOMATED_AND_USER_FUNCTIONAL_REVIEW");
            Debug.Log("HOSPITAL_INTERIOR_R03_S3B_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildStageBatch()
        {
            try { BuildStage(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S3B/Build Windows door review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildStage();
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);
            string outputDirectory = Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior", "StageI1_R03_S3B_AutoGate");
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "HospitalInterior_S3B_R03_DoorReview.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S3B Windows player build failed: " + report.summary.result);
            string settingsAfter = Sha256(settingsPath);
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed during the explicit S3B player build.");
            AssertApprovedS2Baseline(out int baselineCount);
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", executable, settingsAfter, true, baselineCount);
            Debug.Log("HOSPITAL_INTERIOR_R03_S3B_PLAYER_BUILD=PASS; " + executable);
        }

        public static void BuildWindowsReviewPlayerBatch()
        {
            try { BuildWindowsReviewPlayer(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        public static void FinalizeAutomatedGateEvidenceBatch()
        {
            try
            {
                string staticPath = Path.Combine(ReviewFolder(), "StageI1_R03_S3B_StaticGate.json");
                string runtimePath = Path.Combine(ReviewFolder(), "StageI1_R03_S3B_RuntimeGate.json");
                GateStatus staticGate = File.Exists(staticPath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(staticPath)) : null;
                GateStatus runtimeGate = File.Exists(runtimePath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(runtimePath)) : null;
                if (staticGate?.status != "PASS" || runtimeGate?.status != "PASS")
                    throw new InvalidOperationException("S3B automated reports are not both PASS.");
                string executable = Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior",
                    "StageI1_R03_S3B_AutoGate", "HospitalInterior_S3B_R03_DoorReview.exe");
                if (!File.Exists(executable))
                    throw new FileNotFoundException("S3B review player is missing.", executable);
                AssertApprovedS2Baseline(out int baselineCount);
                string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
                WriteBuildRecord("AUTOMATED_GATES_PASS_PENDING_USER_SHAFT_AND_FUNCTIONAL_APPROVAL",
                    executable, Sha256(settingsPath), true, baselineCount);
                WriteCheckpoint("AUTOMATED_GATES_PASS_PENDING_USER_SHAFT_AND_FUNCTIONAL_APPROVAL");
                Debug.Log("HOSPITAL_INTERIOR_R03_S3B_FINALIZE=PASS; user shaft and functional approval pending");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void RecordUserApprovalAndPrepareS3CBatch()
        {
            try
            {
                string staticPath = Path.Combine(ReviewFolder(), "StageI1_R03_S3B_StaticGate.json");
                string runtimePath = Path.Combine(ReviewFolder(), "StageI1_R03_S3B_RuntimeGate.json");
                GateStatus staticGate = File.Exists(staticPath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(staticPath)) : null;
                GateStatus runtimeGate = File.Exists(runtimePath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(runtimePath)) : null;
                if (staticGate?.status != "PASS" || runtimeGate?.status != "PASS")
                    throw new InvalidOperationException("S3B approval handoff requires both automated reports to pass.");
                string executable = Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior",
                    "StageI1_R03_S3B_AutoGate", "HospitalInterior_S3B_R03_DoorReview.exe");
                if (!File.Exists(executable))
                    throw new FileNotFoundException("S3B review player is missing.", executable);
                AssertApprovedS2Baseline(out int baselineCount);
                string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
                WriteBuildRecord(ApprovedCompleteStatus, executable, Sha256(settingsPath), true, baselineCount);
                WriteCheckpoint(ApprovedCompleteStatus);
                Debug.Log("HOSPITAL_INTERIOR_R03_S3B_APPROVAL=PASS; S3C authorized and not started");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void RequireInputs()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S3B requires Unity 6000.3.20f1; found " + Application.unityVersion + ".");
            if (AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath) == null)
                throw new FileNotFoundException("Approved S2 floor contract is missing.", HospitalInteriorS2R03Builder.ContractPath);
            if (AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath) == null)
                throw new FileNotFoundException("Approved S1 datum contract is missing.", HospitalInteriorS1R03Builder.ContractPath);
            foreach (string path in HospitalInteriorS2R03Builder.ProductionScenes.Concat(HospitalInteriorS2R03Builder.FloorScenePaths))
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new FileNotFoundException("Approved S2 dependency is missing.", path);
            foreach (string external in new[] { ExternalCabinFbx(), ExternalLandingFbx() })
                if (!File.Exists(external))
                    throw new FileNotFoundException("Approved S3A FBX is missing.", external);
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root, "S3B");
            EnsureFolder(S3BFolder, "SourceFBX");
            EnsureFolder(S3BFolder, "Materials");
            EnsureFolder(S3BFolder, "Generated");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void ImportApprovedFbxSources()
        {
            File.Copy(ExternalCabinFbx(), Path.Combine(ProjectRoot(), CabinFbxPath), true);
            File.Copy(ExternalLandingFbx(), Path.Combine(ProjectRoot(), LandingFbxPath), true);
            AssetDatabase.ImportAsset(CabinFbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(LandingFbxPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            ConfigureModelImporter(CabinFbxPath);
            ConfigureModelImporter(LandingFbxPath);
        }

        private static void ConfigureModelImporter(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter
                ?? throw new InvalidOperationException("ModelImporter unavailable for " + assetPath);
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
        }

        private static S3ElevatorContract CreateContract()
        {
            S1DatumContract datumContract = AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath);
            S2FloorContract floorContract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath);
            S3ElevatorContract contract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(ContractPath);
            if (contract == null)
            {
                contract = ScriptableObject.CreateInstance<S3ElevatorContract>();
                AssetDatabase.CreateAsset(contract, ContractPath);
            }
            contract.Configure(datumContract, floorContract);
            if (!contract.ValidateSpatialFit(out string detail))
                throw new InvalidOperationException("S3B contract does not fit the approved building core: " + detail);
            if (!contract.ValidateShaftFit(out string shaftDetail))
                throw new InvalidOperationException("S3B shaft does not fit the approved building authority: " + shaftDetail);
            EditorUtility.SetDirty(contract);
            return contract;
        }

        private static void CreateShaftCutFloorMeshes(S3ElevatorContract contract)
        {
            IReadOnlyList<S2FloorRecord> floors = contract.ApprovedS2FloorContract.Floors;
            if (floors.Count != 7)
                throw new InvalidOperationException("S3B shaft apertures require the seven approved S2 floors.");
            CreateOrUpdateShaftCutMesh(F00ShaftCutMeshPath, floors[0].footprint, contract, false);
            CreateOrUpdateShaftCutMesh(UpperShaftCutMeshPath, floors[1].footprint, contract, false);
            CreateOrUpdateShaftCutMesh(UpperShaftCutUndersidePath, floors[1].footprint, contract, true);
        }

        private static Mesh CreateOrUpdateShaftCutMesh(string path, Vector2[] outline,
            S3ElevatorContract contract, bool downwardFacing)
        {
            if (outline == null || outline.Length < 3)
                throw new InvalidOperationException("Approved S2 footprint is unavailable for " + path);

            float holeMinX = contract.ShaftInnerMinXZ.x - contract.ShaftSideWallThicknessM;
            float holeMaxX = contract.ShaftInnerMaxXZ.x + contract.ShaftSideWallThicknessM;
            float holeMinZ = contract.ShaftInnerMinXZ.y;
            float holeMaxZ = contract.ShaftInnerMaxXZ.y + contract.ShaftRearWallThicknessM;
            float[] xCoordinates = outline.Select(point => point.x).Concat(new[] { holeMinX, holeMaxX })
                .Distinct().OrderBy(value => value).ToArray();
            float[] zCoordinates = outline.Select(point => point.y).Concat(new[] { holeMinZ, holeMaxZ })
                .Distinct().OrderBy(value => value).ToArray();
            float outerMinX = outline.Min(point => point.x);
            float outerMaxX = outline.Max(point => point.x);
            float outerMinZ = outline.Min(point => point.y);
            float outerMaxZ = outline.Max(point => point.y);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();

            for (int xIndex = 0; xIndex < xCoordinates.Length - 1; xIndex++)
            for (int zIndex = 0; zIndex < zCoordinates.Length - 1; zIndex++)
            {
                float x0 = xCoordinates[xIndex];
                float x1 = xCoordinates[xIndex + 1];
                float z0 = zCoordinates[zIndex];
                float z1 = zCoordinates[zIndex + 1];
                Vector2 center = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
                bool inFootprint = PointInPolygon(center, outline);
                bool inShaftOpening = center.x > holeMinX && center.x < holeMaxX
                    && center.y > holeMinZ && center.y < holeMaxZ;
                if (!inFootprint || inShaftOpening)
                    continue;

                int start = vertices.Count;
                vertices.Add(new Vector3(x0, 0f, z0));
                vertices.Add(new Vector3(x0, 0f, z1));
                vertices.Add(new Vector3(x1, 0f, z1));
                vertices.Add(new Vector3(x1, 0f, z0));
                uv.Add(new Vector2(Mathf.InverseLerp(outerMinX, outerMaxX, x0), Mathf.InverseLerp(outerMinZ, outerMaxZ, z0)));
                uv.Add(new Vector2(Mathf.InverseLerp(outerMinX, outerMaxX, x0), Mathf.InverseLerp(outerMinZ, outerMaxZ, z1)));
                uv.Add(new Vector2(Mathf.InverseLerp(outerMinX, outerMaxX, x1), Mathf.InverseLerp(outerMinZ, outerMaxZ, z1)));
                uv.Add(new Vector2(Mathf.InverseLerp(outerMinX, outerMaxX, x1), Mathf.InverseLerp(outerMinZ, outerMaxZ, z0)));
                if (downwardFacing)
                {
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 1);
                    triangles.Add(start); triangles.Add(start + 3); triangles.Add(start + 2);
                }
                else
                {
                    triangles.Add(start); triangles.Add(start + 1); triangles.Add(start + 2);
                    triangles.Add(start); triangles.Add(start + 2); triangles.Add(start + 3);
                }
            }

            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, path);
            }
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[j];
                bool crosses = (a.y > point.y) != (b.y > point.y)
                    && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
                if (crosses)
                    inside = !inside;
            }
            return inside;
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            Texture2D terrazzo = CreateTerrazzoTexture();
            var materials = new Dictionary<string, Material>(StringComparer.Ordinal)
            {
                ["MAT_S3A_R03_HygienicWarmOffWhite"] = CreateMaterial("MAT_S3A_R03_HygienicWarmOffWhite", new Color(0.78f, 0.75f, 0.69f), 0f, 0.42f),
                ["MAT_S3A_R03_CeilingSoftWhite"] = CreateMaterial("MAT_S3A_R03_CeilingSoftWhite", new Color(0.88f, 0.84f, 0.76f), 0f, 0.48f),
                ["MAT_S3A_R03_BrushedStainless"] = CreateMaterial("MAT_S3A_R03_BrushedStainless", new Color(0.48f, 0.51f, 0.52f), 0.92f, 0.38f),
                ["MAT_S3A_R03_CharcoalFrame"] = CreateMaterial("MAT_S3A_R03_CharcoalFrame", new Color(0.025f, 0.032f, 0.038f), 0.52f, 0.24f),
                ["MAT_S3A_R03_RefinedBronze"] = CreateMaterial("MAT_S3A_R03_RefinedBronze", new Color(0.34f, 0.13f, 0.035f), 0.86f, 0.28f),
                ["MAT_S3A_R03_DarkTerrazzo"] = CreateMaterial("MAT_S3A_R03_DarkTerrazzo", new Color(0.09f, 0.10f, 0.105f), 0.05f, 0.46f, terrazzo),
                ["MAT_S3A_R03_Diffuser3500K"] = CreateMaterial("MAT_S3A_R03_Diffuser3500K", new Color(0.98f, 0.84f, 0.66f), 0f, 0.32f, null, new Color(1f, 0.72f, 0.42f) * 2.0f),
                ["MAT_S3A_R03_DisplayGlass"] = CreateMaterial("MAT_S3A_R03_DisplayGlass", new Color(0.008f, 0.015f, 0.020f), 0.22f, 0.16f),
                ["MAT_S3A_R03_DisplayText"] = CreateMaterial("MAT_S3A_R03_DisplayText", new Color(0.49f, 0.93f, 0.95f), 0f, 0.28f, null, new Color(0.25f, 0.95f, 1f) * 1.8f),
                ["MAT_S3A_R03_ControlButton"] = CreateMaterial("MAT_S3A_R03_ControlButton", new Color(0.13f, 0.16f, 0.18f), 0.72f, 0.22f),
                ["MAT_S3A_R03_ControlText"] = CreateMaterial("MAT_S3A_R03_ControlText", new Color(0.94f, 0.92f, 0.84f), 0f, 0.30f, null, new Color(0.9f, 0.78f, 0.55f)),
                ["MAT_S3B_R03_Obstruction"] = CreateMaterial("MAT_S3B_R03_Obstruction", new Color(0.85f, 0.05f, 0.03f), 0f, 0.35f, null, new Color(1f, 0.02f, 0.01f) * 1.5f),
                ["MAT_S3B_R03_ShaftConcrete"] = CreateMaterial("MAT_S3B_R03_ShaftConcrete", new Color(0.25f, 0.28f, 0.30f), 0.02f, 0.18f),
                ["MAT_S3B_R03_ShaftGlass"] = CreateTransparentGlassMaterial("MAT_S3B_R03_ShaftGlass", new Color(0.42f, 0.70f, 0.76f, 0.16f)),
                ["MAT_S3B_R03_CabinObservationGlass"] = CreateTransparentGlassMaterial("MAT_S3B_R03_CabinObservationGlass", new Color(0.60f, 0.82f, 0.84f, 0.10f)),
                ["MAT_S3B_R03_ShaftFrame"] = CreateMaterial("MAT_S3B_R03_ShaftFrame", new Color(0.035f, 0.050f, 0.060f), 0.82f, 0.72f),
                ["MAT_S3B_R03_GuideRail"] = CreateMaterial("MAT_S3B_R03_GuideRail", new Color(0.38f, 0.42f, 0.44f), 0.88f, 0.32f),
            };
            return materials;
        }

        private static Material CreateMaterial(string name, Color color, float metallic, float smoothness,
            Texture texture = null, Color? emission = null)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? throw new InvalidOperationException("URP Lit shader is unavailable for S3B.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseMap", texture);
            material.SetOverrideTag("RenderType", "Opaque");
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            if (texture != null)
                material.SetTextureScale("_BaseMap", new Vector2(3f, 3f));
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            material.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateTransparentGlassMaterial(string name, Color tint)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? throw new InvalidOperationException("URP Lit shader is unavailable for S3B glass.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.name = name;
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Metallic", 0.04f);
            material.SetFloat("_Smoothness", 0.94f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)CullMode.Back);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.doubleSidedGI = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Texture2D CreateTerrazzoTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrazzoTexturePath);
            if (texture == null)
            {
                texture = new Texture2D(128, 128, TextureFormat.RGBA32, true, true) { name = "TEX_S3B_R03_DarkTerrazzo" };
                AssetDatabase.CreateAsset(texture, TerrazzoTexturePath);
            }
            var pixels = new Color[128 * 128];
            for (int y = 0; y < 128; y++)
            for (int x = 0; x < 128; x++)
            {
                uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ 0x9e3779b9u;
                float noise = (hash & 255u) / 255f;
                Color baseColor = Color.Lerp(new Color(0.035f, 0.042f, 0.046f), new Color(0.13f, 0.14f, 0.145f), noise * 0.55f);
                if ((hash % 97u) < 4u)
                    baseColor = Color.Lerp(baseColor, (hash & 256u) == 0u ? new Color(0.46f, 0.42f, 0.32f) : new Color(0.36f, 0.39f, 0.40f), 0.80f);
                pixels[y * 128 + x] = baseColor;
            }
            texture.SetPixels(pixels);
            texture.wrapMode = TextureWrapMode.Repeat;
            texture.filterMode = FilterMode.Trilinear;
            texture.anisoLevel = 4;
            texture.Apply(true, false);
            EditorUtility.SetDirty(texture);
            return texture;
        }

        private static void BuildBootstrapScene(S3ElevatorContract contract, Dictionary<string, Material> materials)
        {
            GameObject cabinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CabinFbxPath)
                ?? throw new FileNotFoundException("Imported S3A cabin prefab is missing.", CabinFbxPath);
            GameObject landingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LandingFbxPath)
                ?? throw new FileNotFoundException("Imported S3A landing prefab is missing.", LandingFbxPath);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HospitalInterior_S3B_R03_BootstrapRoot");
            SceneManager.MoveGameObjectToScene(root, scene);
            S3BReviewBootstrap bootstrap = root.AddComponent<S3BReviewBootstrap>();

            GameObject core = new GameObject("S3B_R03_E01_PersistentCore");
            core.transform.SetParent(root.transform, false);
            core.transform.localPosition = Vector3.zero;
            core.transform.localRotation = Quaternion.identity;
            core.transform.localScale = Vector3.one;
            S3DoorController doorController = core.AddComponent<S3DoorController>();
            S3ShaftStructure shaftStructure = CreateShaftStructure(core.transform, contract, materials);
            Mesh f00ShaftCutMesh = AssetDatabase.LoadAssetAtPath<Mesh>(F00ShaftCutMeshPath);
            Mesh upperShaftCutMesh = AssetDatabase.LoadAssetAtPath<Mesh>(UpperShaftCutMeshPath);
            Mesh upperShaftCutUnderside = AssetDatabase.LoadAssetAtPath<Mesh>(UpperShaftCutUndersidePath);
            GameObject apertureObject = new GameObject("S3B_R03_E01_ShaftFloorAperture");
            apertureObject.transform.SetParent(core.transform, false);
            S3ShaftFloorAperture floorAperture = apertureObject.AddComponent<S3ShaftFloorAperture>();
            floorAperture.Configure(contract, f00ShaftCutMesh, upperShaftCutMesh, upperShaftCutUnderside);
            if (!floorAperture.ValidateAssets(out string apertureDetail))
                throw new InvalidOperationException("Generated S3B shaft aperture assets are invalid: " + apertureDetail);

            GameObject cabin = PrefabUtility.InstantiatePrefab(cabinPrefab, scene) as GameObject
                ?? throw new InvalidOperationException("Could not instantiate the approved cabin FBX.");
            cabin.name = "S3B_R03_E01_CabinModel";
            cabin.transform.SetParent(core.transform, false);
            cabin.transform.localPosition = new Vector3(0f, contract.ModelRootVerticalOffsetM, 0f);
            cabin.transform.localRotation = Quaternion.identity;
            cabin.transform.localScale = Vector3.one;
            AssignMaterials(cabin, materials);
            AddNamedColliders(cabin.transform, CabinColliderNames);
            S3CabinObservationDesign cabinObservation = CreateCabinObservationDesign(cabin.transform,
                contract, shaftStructure, materials);
            Transform cabinLeft = RequireDescendant(cabin.transform, "S3A_R03_E01_CabinDoor_L");
            Transform cabinRight = RequireDescendant(cabin.transform, "S3A_R03_E01_CabinDoor_R");
            AddDoorBoxCollider(cabinLeft, contract);
            AddDoorBoxCollider(cabinRight, contract);

            var portalRoots = new List<Transform>();
            var nonCurrentDoors = new List<Transform>();
            Transform currentLandingLeft = null;
            Transform currentLandingRight = null;
            for (int i = 0; i < contract.ApprovedS2FloorContract.Floors.Count; i++)
            {
                S2FloorRecord floor = contract.ApprovedS2FloorContract.Floors[i];
                GameObject portal = PrefabUtility.InstantiatePrefab(landingPrefab, scene) as GameObject
                    ?? throw new InvalidOperationException("Could not instantiate landing portal " + floor.floorId + ".");
                portal.name = "S3B_R03_E01_LandingPortal_" + floor.floorId;
                portal.transform.SetParent(core.transform, false);
                portal.transform.localPosition = new Vector3(0f, floor.elevationM + contract.ModelRootVerticalOffsetM, 0f);
                portal.transform.localRotation = Quaternion.identity;
                portal.transform.localScale = Vector3.one;
                AssignMaterials(portal, materials);
                AddNamedColliders(portal.transform, LandingColliderNames);
                Transform left = RequireDescendant(portal.transform, "S3A_R03_E01_LandingDoor_L");
                Transform right = RequireDescendant(portal.transform, "S3A_R03_E01_LandingDoor_R");
                AddDoorBoxCollider(left, contract);
                AddDoorBoxCollider(right, contract);
                ReplaceLandingIndicator(portal.transform, floor.floorId);
                portalRoots.Add(portal.transform);
                if (i == 0)
                {
                    currentLandingLeft = left;
                    currentLandingRight = right;
                }
                else
                {
                    nonCurrentDoors.Add(left);
                    nonCurrentDoors.Add(right);
                }
            }

            S3DoorObstructionSensor sensor = CreateObstructionSensor(core.transform);
            GameObject obstructionProxy = CreateObstructionProxy(core.transform, materials["MAT_S3B_R03_Obstruction"]);
            doorController.Configure(contract, "F00", cabinLeft, cabinRight, currentLandingLeft, currentLandingRight, sensor);
            ConfigureCabinButtons(cabin.transform, doorController);
            ConfigureLandingCallButtons(portalRoots, doorController);
            CreateCabinLight(core.transform);
            CreateStructuralContext(root.transform, upperShaftCutUnderside);
            CreateReviewSun(root.transform);

            GameObject rig = CreateReviewRig(root.transform, contract, bootstrap, out XROrigin origin,
                out Camera camera, out S3BReviewLocomotion locomotion);
            bootstrap.Configure(contract, doorController, origin, camera, locomotion, cabin.transform,
                portalRoots.ToArray(), nonCurrentDoors.ToArray(), obstructionProxy, shaftStructure,
                floorAperture, cabinObservation);
            locomotion.Configure(origin, camera, bootstrap, contract.ApprovedS2FloorContract.WalkSpeedMps,
                contract.ApprovedS2FloorContract.SprintSpeedMps, 0.12f);
            rig.transform.localPosition = new Vector3(1.65f, 0.03f, 1.35f);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static S3ShaftStructure CreateShaftStructure(Transform parent, S3ElevatorContract contract,
            IReadOnlyDictionary<string, Material> materials)
        {
            GameObject root = new GameObject("S3B_R03_E01_ContinuousShaft");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            S3ShaftStructure structure = root.AddComponent<S3ShaftStructure>();

            float height = contract.ShaftMaxY - contract.ShaftMinY;
            float midY = (contract.ShaftMinY + contract.ShaftMaxY) * 0.5f;
            float outerMinX = contract.ShaftInnerMinXZ.x - contract.ShaftSideWallThicknessM;
            float outerMaxX = contract.ShaftInnerMaxXZ.x + contract.ShaftSideWallThicknessM;
            float outerWidth = outerMaxX - outerMinX;
            float fullDepth = contract.E01OperatingCellMaxXZ.y - contract.ShaftInnerMinXZ.y;
            float centerZ = (contract.ShaftInnerMinXZ.y + contract.E01OperatingCellMaxXZ.y) * 0.5f;
            Material concrete = materials["MAT_S3B_R03_ShaftConcrete"];
            Material glass = materials["MAT_S3B_R03_ShaftGlass"];
            Material frameMaterial = materials["MAT_S3B_R03_ShaftFrame"];
            Material bronze = materials["MAT_S3A_R03_RefinedBronze"];
            Material railMaterial = materials["MAT_S3B_R03_GuideRail"];

            BoxCollider left = CreateShaftBox(root.transform, "S3B_R03_E01_ShaftWall_Left",
                new Vector3(contract.ShaftInnerMinXZ.x - contract.ShaftSideWallThicknessM * 0.5f, midY, centerZ),
                new Vector3(contract.ShaftSideWallThicknessM, height, fullDepth), glass);
            BoxCollider right = CreateShaftBox(root.transform, "S3B_R03_E01_ShaftWall_Right",
                new Vector3(contract.ShaftInnerMaxXZ.x + contract.ShaftSideWallThicknessM * 0.5f, midY, centerZ),
                new Vector3(contract.ShaftSideWallThicknessM, height, fullDepth), glass);
            BoxCollider rear = CreateShaftBox(root.transform, "S3B_R03_E01_ShaftWall_Rear",
                new Vector3(contract.CabinCenterXZ.x, midY,
                    contract.ShaftInnerMaxXZ.y + contract.ShaftRearWallThicknessM * 0.5f),
                new Vector3(outerWidth, height, contract.ShaftRearWallThicknessM), glass);
            BoxCollider baseClosure = CreateShaftBox(root.transform, "S3B_R03_E01_ShaftBaseClosure",
                new Vector3(contract.CabinCenterXZ.x, contract.ShaftMinY - 0.05f, contract.CabinCenterXZ.y),
                new Vector3(outerWidth, 0.10f, fullDepth), concrete);
            BoxCollider topClosure = CreateShaftBox(root.transform, "S3B_R03_E01_ShaftTopClosure",
                new Vector3(contract.CabinCenterXZ.x, contract.ShaftMaxY - 0.05f, contract.CabinCenterXZ.y),
                new Vector3(outerWidth, 0.10f, fullDepth), glass);

            var spandrels = new List<BoxCollider>();
            IReadOnlyList<S2FloorRecord> floors = contract.ApprovedS2FloorContract.Floors;
            for (int i = 0; i < floors.Count; i++)
            {
                float bottom = floors[i].elevationM + contract.ShaftLandingEnvelopeHeightM;
                float top = i + 1 < floors.Count ? floors[i + 1].elevationM : contract.ShaftMaxY;
                float segmentHeight = top - bottom;
                spandrels.Add(CreateShaftBox(root.transform,
                    $"S3B_R03_E01_ShaftFrontSpandrel_{floors[i].floorId}",
                    new Vector3(contract.CabinCenterXZ.x, (bottom + top) * 0.5f,
                        contract.ShaftInnerMinXZ.y - contract.ShaftFrontWallDepthM * 0.5f),
                    new Vector3(outerWidth, segmentHeight, contract.ShaftFrontWallDepthM), glass));
            }

            var glazing = new List<Renderer>
            {
                left.GetComponent<Renderer>(), right.GetComponent<Renderer>(), rear.GetComponent<Renderer>(),
                topClosure.GetComponent<Renderer>(),
            };
            glazing.AddRange(spandrels.Select(item => item.GetComponent<Renderer>()));

            var frames = new List<Renderer>();
            const float frame = 0.055f;
            float frontFrameZ = contract.ShaftInnerMinXZ.y - contract.ShaftFrontWallDepthM + frame * 0.5f;
            float rearFrameZ = contract.ShaftInnerMaxXZ.y + contract.ShaftRearWallThicknessM - frame * 0.5f;
            float leftFrameX = outerMinX + frame * 0.5f;
            float rightFrameX = outerMaxX - frame * 0.5f;
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_FrontLeft",
                new Vector3(leftFrameX, midY, frontFrameZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_FrontRight",
                new Vector3(rightFrameX, midY, frontFrameZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_RearLeft",
                new Vector3(leftFrameX, midY, rearFrameZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_RearRight",
                new Vector3(rightFrameX, midY, rearFrameZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_SideLeft",
                new Vector3(leftFrameX, midY, centerZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_SideRight",
                new Vector3(rightFrameX, midY, centerZ), new Vector3(frame, height, frame), frameMaterial));
            frames.Add(CreateShaftVisualBox(root.transform, "S3B_R03_E01_GlassMullion_RearCentre",
                new Vector3(contract.CabinCenterXZ.x, midY, rearFrameZ), new Vector3(frame, height, frame), frameMaterial));

            for (int i = 0; i < floors.Count; i++)
            {
                float transomY = floors[i].elevationM + contract.ShaftLandingEnvelopeHeightM;
                frames.Add(CreateShaftVisualBox(root.transform, $"S3B_R03_E01_GlassTransom_Left_{floors[i].floorId}",
                    new Vector3(leftFrameX, transomY, centerZ), new Vector3(frame, frame, fullDepth), frameMaterial));
                frames.Add(CreateShaftVisualBox(root.transform, $"S3B_R03_E01_GlassTransom_Right_{floors[i].floorId}",
                    new Vector3(rightFrameX, transomY, centerZ), new Vector3(frame, frame, fullDepth), frameMaterial));
                frames.Add(CreateShaftVisualBox(root.transform, $"S3B_R03_E01_GlassTransom_Rear_{floors[i].floorId}",
                    new Vector3(contract.CabinCenterXZ.x, transomY, rearFrameZ), new Vector3(outerWidth, frame, frame), frameMaterial));
                frames.Add(CreateShaftVisualBox(root.transform, $"S3B_R03_E01_BronzeLandingTransom_{floors[i].floorId}",
                    new Vector3(contract.CabinCenterXZ.x, transomY, frontFrameZ), new Vector3(outerWidth, 0.032f, frame), bronze));

                float segmentBottom = floors[i].elevationM + contract.ShaftLandingEnvelopeHeightM;
                float segmentTop = i + 1 < floors.Count ? floors[i + 1].elevationM : contract.ShaftMaxY;
                frames.Add(CreateShaftVisualBox(root.transform, $"S3B_R03_E01_GlassMullion_FrontCentre_{floors[i].floorId}",
                    new Vector3(contract.CabinCenterXZ.x, (segmentBottom + segmentTop) * 0.5f, frontFrameZ),
                    new Vector3(0.040f, segmentTop - segmentBottom, frame), frameMaterial));
            }

            float railZ = contract.ShaftInnerMaxXZ.y - contract.ShaftGuideRailSizeM.z * 0.5f - 0.01f;
            BoxCollider railLeft = CreateShaftBox(root.transform, "S3B_R03_E01_GuideRail_L",
                new Vector3(0.85f, midY, railZ), contract.ShaftGuideRailSizeM, railMaterial);
            BoxCollider railRight = CreateShaftBox(root.transform, "S3B_R03_E01_GuideRail_R",
                new Vector3(2.45f, midY, railZ), contract.ShaftGuideRailSizeM, railMaterial);

            GameObject centerlineObject = new GameObject("S3B_R03_REVIEW_E01_TravelCenterline");
            centerlineObject.transform.SetParent(root.transform, false);
            centerlineObject.transform.localPosition = new Vector3(contract.CabinCenterXZ.x,
                contract.ShaftMinY, contract.CabinCenterXZ.y);
            LineRenderer line = centerlineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, new Vector3(0f, height, 0f));
            line.startWidth = 0.025f;
            line.endWidth = 0.025f;
            line.sharedMaterial = materials["MAT_S3A_R03_DisplayText"];
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;

            structure.Configure(contract, left, right, rear, baseClosure, topClosure,
                spandrels.ToArray(), new[] { railLeft, railRight }, centerlineObject.transform, line,
                glazing.ToArray(), frames.ToArray());
            Physics.SyncTransforms();
            bool structureValid = structure.ValidateStructure(out string detail);
            bool sweepValid = structure.ValidateCabinSweep(out string sweepDetail);
            bool glassValid = structure.ValidateGlassDesign(out string glassDetail);
            if (!structureValid || !sweepValid || !glassValid)
                throw new InvalidOperationException("Generated S3B shaft is invalid: " + detail + "; "
                    + sweepDetail + "; " + glassDetail);
            return structure;
        }

        private static BoxCollider CreateShaftBox(Transform parent, string name, Vector3 localPosition,
            Vector3 localSize, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localSize;
            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            bool glass = material != null && material.name == "MAT_S3B_R03_ShaftGlass";
            renderer.shadowCastingMode = glass ? ShadowCastingMode.Off : ShadowCastingMode.On;
            renderer.receiveShadows = !glass;
            BoxCollider collider = box.GetComponent<BoxCollider>();
            collider.isTrigger = false;
            return collider;
        }

        private static Renderer CreateShaftVisualBox(Transform parent, string name, Vector3 localPosition,
            Vector3 localSize, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localSize;
            MeshRenderer renderer = box.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<BoxCollider>());
            return renderer;
        }

        private static S3CabinObservationDesign CreateCabinObservationDesign(Transform cabin,
            S3ElevatorContract contract, S3ShaftStructure shaft,
            IReadOnlyDictionary<string, Material> materials)
        {
            Renderer[] glassWalls = new[]
            {
                RequireDescendant(cabin, "S3A_R03_E01_RearWall").GetComponent<Renderer>(),
                RequireDescendant(cabin, "S3A_R03_E01_LSideWall").GetComponent<Renderer>(),
                RequireDescendant(cabin, "S3A_R03_E01_RSideWall").GetComponent<Renderer>(),
            };
            if (glassWalls.Any(item => item == null))
                throw new InvalidOperationException("Approved cabin observation-wall renderers are missing.");
            foreach (Renderer wall in glassWalls)
            {
                wall.sharedMaterial = materials["MAT_S3B_R03_CabinObservationGlass"];
                wall.shadowCastingMode = ShadowCastingMode.Off;
                wall.receiveShadows = false;
            }

            Renderer[] protectionPanels = new[]
            {
                RequireDescendant(cabin, "S3A_R03_E01_RearProtectionPanel").GetComponent<Renderer>(),
                RequireDescendant(cabin, "S3A_R03_E01_LProtectionPanel").GetComponent<Renderer>(),
                RequireDescendant(cabin, "S3A_R03_E01_RProtectionPanel").GetComponent<Renderer>(),
            };
            if (protectionPanels.Any(item => item == null))
                throw new InvalidOperationException("Approved cabin lower protection panels are missing.");

            GameObject frameRoot = new GameObject("S3B_R03_E01_CabinObservationFrame");
            frameRoot.transform.SetParent(cabin, false);
            float minX = contract.CabinCenterXZ.x - contract.CabinOuterSizeM.x * 0.5f;
            float maxX = contract.CabinCenterXZ.x + contract.CabinOuterSizeM.x * 0.5f;
            float minZ = contract.CabinCenterXZ.y - contract.CabinOuterSizeM.z * 0.5f;
            float maxZ = contract.CabinCenterXZ.y + contract.CabinOuterSizeM.z * 0.5f;
            const float frame = 0.045f;
            float leftX = minX + frame * 0.5f;
            float rightX = maxX - frame * 0.5f;
            float frontZ = minZ + frame * 0.5f;
            float rearZ = maxZ - frame * 0.5f;
            float wallHeight = contract.CabinOuterSizeM.y - 0.20f;
            float wallMidY = 0.10f + wallHeight * 0.5f;
            Material charcoal = materials["MAT_S3B_R03_ShaftFrame"];
            Material bronze = materials["MAT_S3A_R03_RefinedBronze"];
            var frames = new List<Renderer>
            {
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassCorner_FL", new Vector3(leftX, wallMidY, frontZ), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassCorner_FR", new Vector3(rightX, wallMidY, frontZ), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassCorner_RL", new Vector3(leftX, wallMidY, rearZ), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassCorner_RR", new Vector3(rightX, wallMidY, rearZ), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassMullion_Left", new Vector3(leftX, wallMidY, contract.CabinCenterXZ.y), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassMullion_Right", new Vector3(rightX, wallMidY, contract.CabinCenterXZ.y), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassMullion_Rear", new Vector3(contract.CabinCenterXZ.x, wallMidY, rearZ), new Vector3(frame, wallHeight, frame), charcoal),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassProtectionLine_Left", new Vector3(leftX, 1.02f, contract.CabinCenterXZ.y), new Vector3(frame, 0.032f, contract.CabinOuterSizeM.z - 0.10f), bronze),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassProtectionLine_Right", new Vector3(rightX, 1.02f, contract.CabinCenterXZ.y), new Vector3(frame, 0.032f, contract.CabinOuterSizeM.z - 0.10f), bronze),
                CreateShaftVisualBox(frameRoot.transform, "S3B_R03_E01_CabinGlassProtectionLine_Rear", new Vector3(contract.CabinCenterXZ.x, 1.02f, rearZ), new Vector3(contract.CabinOuterSizeM.x - 0.10f, 0.032f, frame), bronze),
            };

            S3CabinObservationDesign observation = cabin.gameObject.AddComponent<S3CabinObservationDesign>();
            observation.Configure(contract, glassWalls, protectionPanels, frames.ToArray());
            Physics.SyncTransforms();
            bool designValid = observation.ValidateObservationDesign(out string designDetail);
            bool nested = observation.ValidateNestedInShaft(shaft, out string nestingDetail);
            if (!designValid || !nested)
                throw new InvalidOperationException("Generated cabin observation design is invalid: "
                    + designDetail + "; " + nestingDetail);
            return observation;
        }

        private static GameObject CreateReviewRig(Transform parent, S3ElevatorContract contract, S3BReviewBootstrap bootstrap,
            out XROrigin origin, out Camera camera, out S3BReviewLocomotion locomotion)
        {
            GameObject rig = new GameObject("S3B_R03_XROrigin");
            rig.transform.SetParent(parent, false);
            CharacterController controller = rig.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.height = 1.8f;
            controller.radius = 0.3f;
            controller.stepOffset = 0.25f;
            controller.slopeLimit = 45f;
            controller.skinWidth = 0.02f;
            origin = rig.AddComponent<XROrigin>();
            GameObject offset = new GameObject("Camera Floor Offset");
            offset.transform.SetParent(rig.transform, false);
            GameObject cameraObject = new GameObject("S3B_R03_ReviewCamera");
            cameraObject.transform.SetParent(offset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, contract.ApprovedS2FloorContract.EyeHeightM, 0f);
            camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.04f;
            camera.farClipPlane = 300f;
            camera.fieldOfView = 68f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.055f, 1f);
            cameraObject.AddComponent<AudioListener>();
            origin.CameraFloorOffsetObject = offset;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            locomotion = rig.AddComponent<S3BReviewLocomotion>();
            return rig;
        }

        private static S3DoorObstructionSensor CreateObstructionSensor(Transform parent)
        {
            GameObject sensorObject = new GameObject("S3B_R03_E01_ThresholdObstructionSensor");
            sensorObject.transform.SetParent(parent, false);
            sensorObject.transform.localPosition = new Vector3(1.65f, 1.10f, 3.04f);
            BoxCollider collider = sensorObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1.16f, 2.18f, 0.35f);
            Rigidbody body = sensorObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            return sensorObject.AddComponent<S3DoorObstructionSensor>();
        }

        private static GameObject CreateObstructionProxy(Transform parent, Material material)
        {
            GameObject proxy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proxy.name = "S3B_R03_REVIEW_ThresholdObstructionProxy";
            proxy.transform.SetParent(parent, false);
            proxy.transform.localPosition = new Vector3(1.65f, 1.0f, 3.04f);
            proxy.transform.localScale = new Vector3(0.32f, 2.0f, 0.24f);
            proxy.GetComponent<MeshRenderer>().sharedMaterial = material;
            proxy.AddComponent<S3DoorObstruction>();
            proxy.SetActive(false);
            return proxy;
        }

        private static void ConfigureCabinButtons(Transform cabin, S3DoorController controller)
        {
            for (int i = 0; i < 7; i++)
            {
                string floorId = "F" + i.ToString("00");
                Transform target = RequireDescendant(cabin, $"S3A_R03_E01_ControlButton_{i + 1:00}_{floorId}");
                AddMeshBoxCollider(target, true);
                target.gameObject.AddComponent<S3ElevatorButton>().Configure(S3ElevatorButtonCommand.Floor, floorId, controller);
            }
            Transform open = RequireDescendant(cabin, "S3A_R03_E01_ControlButton_08_OPEN");
            Transform close = RequireDescendant(cabin, "S3A_R03_E01_ControlButton_09_CLOSE");
            AddMeshBoxCollider(open, true);
            AddMeshBoxCollider(close, true);
            open.gameObject.AddComponent<S3ElevatorButton>().Configure(S3ElevatorButtonCommand.DoorOpen, "F00", controller);
            close.gameObject.AddComponent<S3ElevatorButton>().Configure(S3ElevatorButtonCommand.DoorClose, "F00", controller);
        }

        private static void ConfigureLandingCallButtons(IReadOnlyList<Transform> portals, S3DoorController controller)
        {
            for (int i = 0; i < portals.Count; i++)
            {
                string floorId = "F" + i.ToString("00");
                Transform call = RequireDescendant(portals[i], "S3A_R03_E01_LandingCallButton");
                AddMeshBoxCollider(call, true);
                call.gameObject.AddComponent<S3ElevatorButton>().Configure(S3ElevatorButtonCommand.LandingCall, floorId, controller);
            }
        }

        private static void ReplaceLandingIndicator(Transform portal, string floorId)
        {
            Transform baked = RequireDescendant(portal, "S3A_R03_E01_LandingIndicatorText");
            Renderer bakedRenderer = baked.GetComponent<Renderer>();
            if (bakedRenderer != null)
                bakedRenderer.enabled = false;
            GameObject labelObject = new GameObject("S3B_R03_E01_LandingIndicatorText_" + floorId);
            labelObject.transform.SetParent(portal, false);
            labelObject.transform.localPosition = new Vector3(1.65f, 2.55f, 2.758f);
            labelObject.transform.localRotation = Quaternion.identity;
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = floorId;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 96;
            label.characterSize = 0.012f;
            label.color = new Color(0.49f, 0.93f, 0.95f, 1f);
            label.fontStyle = FontStyle.Bold;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelObject.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
        }

        private static void CreateCabinLight(Transform parent)
        {
            GameObject lightObject = new GameObject("S3B_R03_E01_CabinPracticalLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = new Vector3(1.65f, 2.28f, 4.35f);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.useColorTemperature = true;
            light.colorTemperature = 3500f;
            // Keep the practical restrained in Unity's realtime lighting model.  The
            // approved FBX diffuser supplies the visible warm ceiling source; this
            // light only lifts the cabin surfaces and must not clip the hygienic
            // panels or spill heavily into the landing.
            light.intensity = 2.4f;
            light.range = 3.2f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.45f;
        }

        private static void CreateReviewSun(Transform parent)
        {
            GameObject sun = new GameObject("S3B_R03_NeutralReviewSun");
            sun.transform.SetParent(parent, false);
            sun.transform.localRotation = Quaternion.Euler(52f, -28f, 0f);
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.40f;
            light.color = new Color(0.91f, 0.94f, 1f);
            light.shadows = LightShadows.Soft;
        }

        private static void CreateStructuralContext(Transform parent, Mesh shaftCutUndersideMesh)
        {
            Material undersideMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalInteriorS2R03Builder.StructuralUndersideMaterialPath);
            if (shaftCutUndersideMesh == null || undersideMaterial == null)
                throw new InvalidOperationException("S3 shaft-cut structural context is missing.");
            S2FloorContract floorContract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath);
            GameObject context = new GameObject("S3B_R03_PersistentStructuralDeckContext");
            context.transform.SetParent(parent, false);
            for (int i = 1; i < floorContract.Floors.Count; i++)
            {
                S2FloorRecord floor = floorContract.Floors[i];
                GameObject underside = new GameObject("S3B_R03_StructuralUnderside_" + floor.floorId);
                underside.transform.SetParent(context.transform, false);
                underside.transform.localPosition = new Vector3(0f, floor.elevationM - 0.20f, 0f);
                underside.AddComponent<MeshFilter>().sharedMesh = shaftCutUndersideMesh;
                underside.AddComponent<MeshRenderer>().sharedMaterial = undersideMaterial;
            }
        }

        private static void AssignMaterials(GameObject model, IReadOnlyDictionary<string, Material> materials)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Material[] replacements = renderer.sharedMaterials.Select(original => ResolveMaterial(original, materials)).ToArray();
                renderer.sharedMaterials = replacements;
            }
        }

        private static Material ResolveMaterial(Material imported, IReadOnlyDictionary<string, Material> materials)
        {
            string importedName = imported == null ? string.Empty : imported.name;
            foreach (KeyValuePair<string, Material> pair in materials)
                if (importedName.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    return pair.Value;
            throw new InvalidOperationException("No S3B URP material mapping exists for imported material: " + importedName);
        }

        private static void AddNamedColliders(Transform root, IEnumerable<string> names)
        {
            foreach (string name in names)
                AddMeshBoxCollider(RequireDescendant(root, name), false);
        }

        private static BoxCollider AddMeshBoxCollider(Transform target, bool trigger)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null)
                throw new InvalidOperationException("MeshFilter missing on collider target " + target.name);
            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
                collider = target.gameObject.AddComponent<BoxCollider>();
            collider.center = filter.sharedMesh.bounds.center;
            collider.size = filter.sharedMesh.bounds.size;
            collider.isTrigger = trigger;
            return collider;
        }

        private static BoxCollider AddDoorBoxCollider(Transform target, S3ElevatorContract contract)
        {
            BoxCollider collider = target.GetComponent<BoxCollider>();
            if (collider == null)
                collider = target.gameObject.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = new Vector3(contract.DoorLeafWidthM, contract.ClearDoorM.y, contract.DoorLeafDepthM);
            collider.isTrigger = false;
            return collider;
        }

        public static Transform RequireDescendant(Transform root, string name)
        {
            Transform match = root.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(item => string.Equals(item.name, name, StringComparison.Ordinal));
            return match ?? throw new InvalidOperationException($"Required object '{name}' is missing under '{root.name}'.");
        }

        public static void AssertApprovedS2Baseline(out int fileCount)
        {
            string manifestPath = ApprovedS2BaselineManifest();
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Approved S2 baseline manifest is missing.", manifestPath);
            BaselineManifest manifest = JsonUtility.FromJson<BaselineManifest>(File.ReadAllText(manifestPath));
            if (manifest?.files == null || manifest.files.Length != 38)
                throw new InvalidOperationException("Approved S2 baseline manifest must contain exactly 38 files.");
            var mismatches = new List<string>();
            foreach (BaselineFile entry in manifest.files)
            {
                string fullPath = Path.Combine(ProjectRoot(), entry.path.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                    mismatches.Add(entry.path + ":missing");
                else if (!string.Equals(Sha256(fullPath), entry.sha256, StringComparison.OrdinalIgnoreCase))
                    mismatches.Add(entry.path + ":hash");
            }
            if (mismatches.Count > 0)
                throw new InvalidOperationException("Approved S2 baseline changed: " + string.Join(",", mismatches));
            fileCount = manifest.files.Length;
        }

        private static void AssertImportedFbxHashes()
        {
            string importedCabin = Path.Combine(ProjectRoot(), CabinFbxPath);
            string importedLanding = Path.Combine(ProjectRoot(), LandingFbxPath);
            if (!string.Equals(Sha256(ExternalCabinFbx()), Sha256(importedCabin), StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Sha256(ExternalLandingFbx()), Sha256(importedLanding), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Imported FBX bytes do not match the approved S3A exports.");
        }

        private static void WriteBuildRecord(string status, string executable, string settingsHash, bool untouched, int baselineCount)
        {
            S3ElevatorContract contract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(ContractPath)
                ?? throw new InvalidOperationException("S3 elevator contract missing while writing build record.");
            float f06 = contract.ApprovedS2FloorContract.Floors.Last().elevationM;
            float f06CabinTop = f06 + contract.ModelRootVerticalOffsetM + contract.CabinOuterSizeM.y;
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(), "StageI1_R03_S3B_BuildRecord.json"),
                JsonUtility.ToJson(new BuildRecord
                {
                    status = status,
                    unityVersion = Application.unityVersion,
                    contract = ContractPath,
                    cabinFbx = CabinFbxPath,
                    landingFbx = LandingFbxPath,
                    bootstrapScene = BootstrapScenePath,
                    playerScenes = PlayerScenes,
                    playerExecutable = executable,
                    externalCabinSha256 = Sha256(ExternalCabinFbx()),
                    importedCabinSha256 = Sha256(Path.Combine(ProjectRoot(), CabinFbxPath)),
                    externalLandingSha256 = Sha256(ExternalLandingFbx()),
                    importedLandingSha256 = Sha256(Path.Combine(ProjectRoot(), LandingFbxPath)),
                    s2BaselineFileCount = baselineCount,
                    s2BaselineMismatchCount = 0,
                    editorBuildSettingsSha256 = settingsHash,
                    editorBuildSettingsUntouched = untouched,
                    shaftMinY = contract.ShaftMinY,
                    shaftMaxY = contract.ShaftMaxY,
                    shaftInnerEnvelope = $"X {contract.ShaftInnerMinXZ.x:F3}..{contract.ShaftInnerMaxXZ.x:F3}; Z {contract.ShaftInnerMinXZ.y:F3}..{contract.ShaftInnerMaxXZ.y:F3}",
                    shaftFloorApertureEnvelope = $"X {contract.ShaftInnerMinXZ.x - contract.ShaftSideWallThicknessM:F3}..{contract.ShaftInnerMaxXZ.x + contract.ShaftSideWallThicknessM:F3}; Z {contract.ShaftInnerMinXZ.y:F3}..{contract.ShaftInnerMaxXZ.y + contract.ShaftRearWallThicknessM:F3}",
                    shaftCutMeshAssets = new[] { F00ShaftCutMeshPath, UpperShaftCutMeshPath, UpperShaftCutUndersidePath },
                    shaftArchitecturalFinish = "Low-iron blue-grey transparent laminated glass with slim charcoal metallic mullions and restrained bronze landing transoms.",
                    shaftGlassPanelCount = 11,
                    shaftFrameMemberCount = 42,
                    cabinObservationFinish = "Transparent low-iron side/rear observation glazing above retained stainless lower protection panels, with charcoal mullions and bronze protection lines.",
                    cabinObservationGlassWallCount = 3,
                    cabinObservationFrameCount = 10,
                    minimumShaftRunningClearanceM = contract.MinimumShaftRunningClearanceM,
                    f06TopOverrunM = contract.ShaftMaxY - f06CabinTop,
                }, true));
        }

        public static void WriteCheckpoint(string status)
        {
            bool approved = string.Equals(status, ApprovedCompleteStatus, StringComparison.Ordinal);
            string activeStage = approved
                ? "S3B - approved glass observation cabin and normal door system"
                : "S3B - normal synchronized door system";
            string s3cState = approved
                ? "not implemented; authorized as the next stage and intentionally not started"
                : "not implemented and not authorized";
            string handoffNote = approved
                ? "User approval is recorded. S3C may be planned or implemented next in a separate task; no S3C controller, travel, or scene-transition code exists in this checkpoint."
                : "Automated checks support functional review but do not authorize S3C.";
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(), "HospitalInterior_StageS3B_R03_Checkpoint.md"),
$@"# Hospital Interior R03 Stage S3B Checkpoint

**Active stage:** `{activeStage}`  
**Status:** `{status}`  
**S3A:** explicitly approved  
**S3C travel:** {s3cState}

## Implemented scope

- Approved S3A cabin and landing FBXs imported byte-for-byte at identity scale.
- E01 remains centred at X 1.65 m / Z 4.35 m with 0.175 m side and 0.125 m front/rear structural clearances inside the locked operating cell.
- Cabin floor, threshold, and S2 slab share the exact 0.000 m walk datum after the documented -0.110 m model-root offset.
- Seven portals are fixed to the approved F00-F06 datums; only the F00 landing pair is active in S3B.
- A continuous E01 hoistway now spans Y -0.20 m through the approved S1 roof limit at Y 28.83 m, with uninterrupted side/rear walls, seven between-storey front closures, two full-height guide rails, a structural base closure, and a top closure.
- The full-height enclosure is now low-iron blue-grey transparent glass with 11 continuous glazed faces, 42 slim non-colliding charcoal/bronze frame members, visible brushed guide rails, and no opaque between-storey monolith above F00.
- The cabin is fully nested inside that shaft and now uses three transparent side/rear observation walls above the retained stainless impact panels and rear handrail. Ten slim charcoal/bronze frame members keep the cabin legible as a moving architectural object while preserving a clear standing-eye view of future floor transitions.
- S3-owned floor and underside meshes cut a continuous X 0.300..3.000 m / Z 3.000..5.700 m aperture through all seven datums. Runtime swaps only the loaded floor instance; protected S2 source assets stay byte-identical.
- The nominal cabin sweep keeps at least 0.075 m to the shaft walls, 0.040 m to the guide rails, 0.090 m at the F00 base, and 0.940 m below the F06 top closure.
- Two cabin and two current-landing leaves open and close together in 1.2 s, dwell for 4 s, reverse on obstruction, and interlock before movement can ever be declared safe.
- All twelve non-current landing leaves remain closed, collidable, and locked.
- Door-open, door-close, and seven landing-call controls are interactive. F00-F06 cabin floor requests remain safely disabled until S3C.

## Test scene

`{BootstrapScenePath}`

{handoffNote}
");
        }

        public static string ProjectRoot() => HospitalInteriorS2R03Builder.ProjectRoot();
        public static string WorkspaceRoot() => HospitalInteriorS2R03Builder.WorkspaceRoot();
        public static string ReviewFolder() => Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior", "StageI1_R03", "S3B_E01_Doors");
        public static string Sha256(string path) => HospitalInteriorS2R03Builder.Sha256(path);
        public static string ExternalCabinFbx() => Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior", "StageI1_R03_S3A", "HospitalInterior_E01_Cabin_R03.fbx");
        public static string ExternalLandingFbx() => Path.Combine(WorkspaceRoot(), "Exports", "HospitalInterior", "StageI1_R03_S3A", "HospitalInterior_E01_LandingPortal_R03.fbx");
        public static string ApprovedS2BaselineManifest() => Path.Combine(WorkspaceRoot(), "Archive", "HospitalInterior",
            "Reverted_Unapproved_S3_2026-08-15", "Reviews", "HospitalInterior", "StageI1_R03", "StageI1_R03_S3_ApprovedS2Baseline.json");
    }
}
