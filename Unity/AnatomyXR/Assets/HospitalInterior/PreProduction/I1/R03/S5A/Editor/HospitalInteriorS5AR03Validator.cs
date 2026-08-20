using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS5AR03Validator
    {
        [Serializable]
        private sealed class Check
        {
            public string id;
            public string status;
            public string detail;
        }

        [Serializable]
        private sealed class Report
        {
            public string schema = "HospitalInterior.R03.S5A.StaticGate.v1";
            public string status;
            public string unityVersion;
            public int passed;
            public int failed;
            public int total;
            public int protectedFileCount;
            public string editorBuildSettingsSha256;
            public Check[] checks;
        }

        [Serializable]
        private sealed class GateStatus { public string status; }

        [MenuItem("Hospital Interior/R03 S5A/Validate static gate", priority = 30)]
        public static void ValidateStaticGate()
        {
            var checks = new List<Check>();
            void Add(string id, bool passed, string detail) => checks.Add(new Check
            {
                id = id,
                status = passed ? "PASS" : "FAIL",
                detail = detail,
            });

            string settingsPath = Path.Combine(HospitalInteriorS5AR03Builder.ProjectRoot(),
                "ProjectSettings", "EditorBuildSettings.asset");
            string settingsHash = HospitalInteriorS5AR03Builder.Sha256(settingsPath);
            Add("unity_version", Application.unityVersion == "6000.3.20f1",
                Application.unityVersion);
            Add("editor_build_settings_immutable",
                settingsHash == HospitalInteriorS5AR03Builder.ExpectedBuildSettingsSha256,
                settingsHash);
            Add("explicit_player_scene_list",
                HospitalInteriorS5AR03Builder.PlayerScenes.Length == 15
                && HospitalInteriorS5AR03Builder.PlayerScenes[0]
                    == HospitalInteriorS5AR03Builder.BootstrapScenePath
                && HospitalInteriorS5AR03Builder.PlayerScenes.Skip(1).Distinct().Count() == 14,
                string.Join(",", HospitalInteriorS5AR03Builder.PlayerScenes));

            HospitalInteriorS5AR03Builder.AssertProtectedAuthorities(out int protectedCount);
            Add("all_approved_s2_s3_s4_hashes", protectedCount > 100,
                "protectedFiles=" + protectedCount + "; baseline=45c9e3b");
            string s4Approval = Path.Combine(HospitalInteriorS5AR03Builder.WorkspaceRoot(),
                "Reviews", "HospitalInterior", "StageI1_R03", "S4_F00_Plan",
                "StageI1_R03_S4_F00_UserApproval.md");
            string s4Contract = Path.Combine(HospitalInteriorS5AR03Builder.WorkspaceRoot(),
                "Reviews", "HospitalInterior", "StageI1_R03", "S4_F00_Plan",
                "StageI1_R03_S4_F00_Plan_V03_Contract.json");
            Add("approved_s4_authority_hashes",
                HospitalInteriorS5AR03Builder.Sha256(s4Approval)
                    == S5AStairContract.S4ApprovalSha256
                && HospitalInteriorS5AR03Builder.Sha256(s4Contract)
                    == S5AStairContract.S4PlanContractSha256,
                "approval=" + HospitalInteriorS5AR03Builder.Sha256(s4Approval)
                    + "; contract=" + HospitalInteriorS5AR03Builder.Sha256(s4Contract));
            Add("blender_independent_gate",
                ReadGate(Path.Combine(HospitalInteriorS5AR03Builder.ReviewFolder(),
                    "StageI1_R03_S5A_StairA_BlenderGate.json")) == "PASS",
                "40/40 gate required");

            S5AStairContract contract = AssetDatabase.LoadAssetAtPath<S5AStairContract>(
                HospitalInteriorS5AR03Builder.ContractPath);
            string contractDetail = "missing";
            bool contractValid = contract != null && contract.Validate(out contractDetail);
            Add("stair_contract_asset", contractValid,
                contract == null ? "missing" : contractDetail);
            Add("exact_a06_bounds", contract != null
                && contract.BoundsMinXZ == new Vector2(-29.0f, -5.0f)
                && contract.BoundsMaxXZ == new Vector2(-22.0f, 5.0f)
                && Mathf.Abs(contract.BaseY + 0.20f) <= 0.0001f
                && Mathf.Abs(contract.TopY - 28.83f) <= 0.0001f,
                contract == null ? "missing" : $"{contract.BoundsMinXZ}..{contract.BoundsMaxXZ}; Y={contract.BaseY}..{contract.TopY}");
            Add("exact_datums_and_intervals", contract != null
                && contract.ApprovedFloorContract.Floors.Select(item => item.elevationM)
                    .SequenceEqual(new[] { 0f, 5.8f, 9.7f, 13.6f, 17.5f, 21.4f, 25.3f })
                && contract.Intervals.Count == 6
                && contract.Intervals.Sum(item => item.totalRisers) == 164,
                "F00-F06 datums; 12 flights; 164 risers");
            Add("measured_lanes_landings_doors", contract != null
                && contract.LaneCentresX == new Vector2(-24.18f, -26.82f)
                && Mathf.Abs(contract.LaneWidthM - 1.80f) <= 0.0001f
                && contract.LaneWidthM >= contract.MinimumUsableWidthM
                && Mathf.Abs(contract.FlightRunM - 5.12f) <= 0.0001f
                && Mathf.Abs(contract.DoorCentreZ + 3.76f) <= 0.0001f
                && contract.DoorClearM == new Vector2(1.20f, 2.25f),
                "lane=1.80/usable>=1.45; run=5.12; door=1.20x2.25@Z-3.76");
            Add("safety_dimensions", contract != null
                && contract.MinimumHeadroomM >= 2.20f
                && contract.HandrailHeightM >= 0.95f
                && contract.GuardHeightM >= 1.10f,
                "headroom>=2.20; rail=0.95; guard=1.10");

            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(
                HospitalInteriorS5AR03Builder.SourceFbxPath);
            MeshFilter[] fbxMeshes = fbx == null ? Array.Empty<MeshFilter>()
                : fbx.GetComponentsInChildren<MeshFilter>(true);
            Add("separated_child_fbx", fbx != null && fbxMeshes.Length > 350,
                "meshChildren=" + fbxMeshes.Length);
            Add("fbx_geometry_counts",
                fbxMeshes.Count(item => item.name.Contains("_Step_")) == 164
                && fbxMeshes.Count(item => item.name.Contains("_FloorLanding_")) == 7
                && fbxMeshes.Count(item => item.name.Contains("_MidLanding_")) == 6
                && fbxMeshes.Count(item => item.name.Contains("_DoorLeaf_")) == 7,
                "steps/landings/intermediate/doors=164/7/6/7");
            Add("f06_top_closure", fbxMeshes.Count(item => item.name.Contains("TopClosure")) == 1,
                "one retained roof/top-closure mesh");

            Scene scene = EditorSceneManager.OpenScene(
                HospitalInteriorS5AR03Builder.BootstrapScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            S5AIntegrationBootstrap bootstrap = roots
                .SelectMany(root => root.GetComponentsInChildren<S5AIntegrationBootstrap>(true))
                .SingleOrDefault();
            Add("cloned_s3d_bootstrap", roots.SelectMany(root => root
                .GetComponentsInChildren<S3DIntegrationBootstrap>(true)).Count() == 1,
                "approved S3D bootstrap retained as startup authority in clone");
            Add("single_s5a_runtime_stack", bootstrap != null
                && roots.SelectMany(root => root.GetComponentsInChildren<S5AFloorSceneCoordinator>(true)).Count() == 1
                && roots.SelectMany(root => root.GetComponentsInChildren<S5AVerticalCirculationController>(true)).Count() == 1
                && roots.SelectMany(root => root.GetComponentsInChildren<S5ARecoveryCoordinator>(true)).Count() == 1,
                "bootstrap/coordinator/circulation/recovery=1 each");
            Add("seven_stair_door_controllers", bootstrap != null
                && bootstrap.StairDoors.Length == 7
                && bootstrap.StairDoors.Select(item => item.FloorId).Distinct().Count() == 7,
                bootstrap == null ? "bootstrap missing" : string.Join(",", bootstrap.StairDoors.Select(item => item.FloorId)));
            bool automaticSensors = bootstrap != null && bootstrap.StairDoors.All(item =>
            {
                BoxCollider zone = item.GetComponent<BoxCollider>();
                Rigidbody body = item.GetComponent<Rigidbody>();
                return zone != null && zone.isTrigger && zone.bounds.min.x <= -28.824f
                    && zone.bounds.max.x >= -19.001f
                    && zone.bounds.min.z <= contract.DoorCentreZ - 0.79f
                    && zone.bounds.max.z >= contract.DoorCentreZ + 0.79f
                    && body != null && body.isKinematic && !body.useGravity;
            });
            Add("automatic_door_approach_sensors", automaticSensors,
                "seven kinematic trigger zones cover internal landings and the full east clear approach through X=-19.0");
            Add("elevator_controls_rewired", roots.SelectMany(root => root
                .GetComponentsInChildren<S3ElevatorButton>(true)).Count() == 0
                && roots.SelectMany(root => root
                .GetComponentsInChildren<S5AElevatorInteraction>(true)).Count() >= 11,
                "legacy buttons=0; S5A shared interaction targets installed");

            Transform stairRoot = bootstrap?.StairRoot;
            MeshFilter[] stairMeshes = stairRoot == null ? Array.Empty<MeshFilter>()
                : stairRoot.GetComponentsInChildren<MeshFilter>(true);
            Add("tread_aligned_collision", stairMeshes.Count(item => item.name.Contains("_Step_")
                && item.GetComponent<MeshCollider>() != null) == 164,
                "all 164 visible treads own MeshCollider");
            Add("no_hidden_ramp", !stairMeshes.Any(item => item.name.IndexOf("ramp",
                StringComparison.OrdinalIgnoreCase) >= 0), "ramp meshes=0");
            Add("stair_teleport_exclusion", stairRoot != null
                && !stairRoot.GetComponentsInChildren<MonoBehaviour>(true)
                    .Any(item => item.GetType().Name.IndexOf("TeleportSurface",
                        StringComparison.OrdinalIgnoreCase) >= 0),
                "zero teleport surface components inside stair root");
            Add("a06_scene_containment", ValidateBounds(stairMeshes, out string boundsDetail),
                boundsDetail);
            MeshFilter[] eastShell = stairMeshes.Where(item => item.name.StartsWith(
                "S5A_R03_StairA_EastWall_", StringComparison.Ordinal)).ToArray();
            bool separatedSkin = eastShell.Length == 21 && eastShell.All(item =>
                TransformBounds(item.transform, item.sharedMesh.bounds).max.x <= -22.019f);
            Add("exterior_skin_depth_separation", separatedSkin,
                "21 east shell segments end at or west of X=-22.020, separated from the exterior cut face at X=-22.000");

            string[] requiredMaterials =
            {
                "MAT_S5A_WarmOffWhite", "MAT_S5A_StoneTerrazzo", "MAT_S5A_Charcoal",
                "MAT_S5A_Bronze", "MAT_S5A_BlueGreyDoorGlass",
                "MAT_S5A_NeutralLandingLight",
            };
            string[] usedMaterials = stairRoot == null ? Array.Empty<string>()
                : stairRoot.GetComponentsInChildren<MeshRenderer>(true)
                    .SelectMany(item => item.sharedMaterials).Where(item => item != null)
                    .Select(item => item.name).Distinct().ToArray();
            Add("s5a_owned_functional_finish", requiredMaterials.All(usedMaterials.Contains),
                string.Join(",", usedMaterials));

            S5ADualAperture aperture = roots.SelectMany(root => root
                .GetComponentsInChildren<S5ADualAperture>(true)).SingleOrDefault();
            string apertureDetail = "missing";
            bool apertureValid = aperture != null && aperture.ValidateAssets(out apertureDetail);
            Add("dual_aperture_mesh_assets", apertureValid,
                aperture == null ? "missing" : apertureDetail);
            string persistentDetail = "contract missing";
            bool persistentValid = contract != null
                && ValidatePersistentMeshes(roots, contract, out persistentDetail);
            Add("persistent_floorplates_dual_cut", persistentValid,
                contract == null ? "contract missing" : persistentDetail);
            string exteriorDetail = "contract missing";
            bool exteriorValid = contract != null
                && ValidateExteriorCut(contract.ExteriorDualApertureStructure,
                    out exteriorDetail);
            Add("exterior_structure_dual_cut", exteriorValid,
                contract == null ? "contract missing" : exteriorDetail);
            Add("floor_scenes_not_edited", HospitalInteriorS2R03Builder.FloorScenePaths
                .All(path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null),
                "seven protected S2 scene assets remain authoritative");
            Add("no_s5b_content", stairRoot != null
                && !stairRoot.GetComponentsInChildren<Transform>(true)
                    .Any(item => item.name.IndexOf("S5B", StringComparison.OrdinalIgnoreCase) >= 0
                        || item.name.IndexOf("Furniture", StringComparison.OrdinalIgnoreCase) >= 0),
                "no lobby furniture, programme, or S5B anchors");
            Add("windows_pcvr_only", EditorUserBuildSettings.activeBuildTarget
                == BuildTarget.StandaloneWindows64,
                "StandaloneWindows64; Android/standalone Quest deferred");

            int failed = checks.Count(item => item.status == "FAIL");
            var report = new Report
            {
                status = failed == 0 ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passed = checks.Count - failed,
                failed = failed,
                total = checks.Count,
                protectedFileCount = protectedCount,
                editorBuildSettingsSha256 = settingsHash,
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(HospitalInteriorS5AR03Builder.ReviewFolder());
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.Combine(HospitalInteriorS5AR03Builder.ReviewFolder(),
                "StageI1_R03_S5A_StaticGate.json"), json);
            File.WriteAllText(Path.Combine(HospitalInteriorS5AR03Builder.ProjectRoot(),
                HospitalInteriorS5AR03Builder.StaticGateAssetPath), json);
            AssetDatabase.ImportAsset(HospitalInteriorS5AR03Builder.StaticGateAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_STATIC_GATE=" + report.status
                + $"; {report.passed}/{report.total}");
            if (failed > 0)
                throw new InvalidOperationException("S5A static gate failed: "
                    + string.Join(",", checks.Where(item => item.status == "FAIL")
                        .Select(item => item.id + "[" + item.detail + "]")));
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static bool ValidateBounds(IEnumerable<MeshFilter> filters,
            out string detail)
        {
            bool valid = true;
            int count = 0;
            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null)
                    continue;
                count++;
                Bounds bounds = TransformBounds(filter.transform, filter.sharedMesh.bounds);
                valid &= bounds.min.x >= -29.001f && bounds.max.x <= -21.999f
                    && bounds.min.z >= -5.001f && bounds.max.z <= 5.001f
                    && bounds.min.y >= -0.201f && bounds.max.y <= 28.831f;
            }
            detail = "contained=" + valid + "; meshCount=" + count;
            return valid && count > 350;
        }

        private static Bounds TransformBounds(Transform transform, Bounds local)
        {
            Vector3 centre = transform.TransformPoint(local.center);
            Vector3 extents = local.extents;
            Vector3 axisX = transform.TransformVector(extents.x, 0f, 0f);
            Vector3 axisY = transform.TransformVector(0f, extents.y, 0f);
            Vector3 axisZ = transform.TransformVector(0f, 0f, extents.z);
            extents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
            return new Bounds(centre, extents * 2f);
        }

        private static bool ValidatePersistentMeshes(IEnumerable<GameObject> roots,
            S5AStairContract contract, out string detail)
        {
            MeshFilter[] filters = roots.SelectMany(root => root
                .GetComponentsInChildren<MeshFilter>(true)).ToArray();
            int f00 = filters.Count(item => item.name.StartsWith(
                "S3C_R03_E01_PersistentFloorPlate_F00", StringComparison.Ordinal)
                && item.sharedMesh == contract.F00DualApertureSlab);
            int upper = filters.Count(item => item.name.StartsWith(
                "S3C_R03_E01_PersistentFloorPlate_F", StringComparison.Ordinal)
                && !item.name.EndsWith("F00", StringComparison.Ordinal)
                && item.sharedMesh == contract.UpperDualApertureSlab);
            int undersides = filters.Count(item => item.name.StartsWith(
                "S3B_R03_StructuralUnderside_", StringComparison.Ordinal)
                && item.sharedMesh == contract.UpperDualApertureUnderside);
            detail = $"f00={f00}; upper={upper}; undersides={undersides}";
            return f00 == 1 && upper == 6 && undersides == 6;
        }

        private static bool ValidateExteriorCut(Mesh mesh, out string detail)
        {
            if (mesh == null)
            {
                detail = "mesh missing";
                return false;
            }
            int intrusions = 0;
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            for (int index = 0; index < triangles.Length; index += 3)
            {
                Vector3 centre = (vertices[triangles[index]] + vertices[triangles[index + 1]]
                    + vertices[triangles[index + 2]]) / 3f;
                if (centre.x > -29.0f && centre.x < -22.0f
                    && centre.y > -0.2f && centre.y < 28.83f
                    && centre.z > -5.0f && centre.z < 5.0f)
                    intrusions++;
            }
            detail = $"vertices={vertices.Length}; triangles={triangles.Length / 3}; stairPrismIntrusions={intrusions}";
            return vertices.Length > 0 && triangles.Length > 0 && intrusions == 0;
        }

        private static string ReadGate(string path)
        {
            GateStatus gate = File.Exists(path)
                ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(path)) : null;
            return gate?.status ?? "MISSING";
        }
    }
}
