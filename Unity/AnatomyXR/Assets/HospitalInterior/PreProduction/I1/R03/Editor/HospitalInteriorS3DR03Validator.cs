using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.Hospital.Stage05;
using CutMyBodyPlease.Hospital.Stage06A;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3DR03Validator
    {
        [Serializable]
        private sealed class StaticCheck
        {
            public string name;
            public bool pass;
            public string detail;
        }

        [Serializable]
        private sealed class StaticGate
        {
            public string schema = S3DIntegrationContract.StaticSchema;
            public string status;
            public string unityVersion;
            public int passCount;
            public int totalCount;
            public StaticCheck[] checks;
        }

        [MenuItem("Hospital Interior/R03 S3D/Validate static gate", priority = 30)]
        public static void ValidateStaticGate()
        {
            var checks = new List<StaticCheck>();
            HospitalInteriorS3DR03Builder.AssertProtectedAuthorities(out int s2Count,
                out int s3bCount, out int s3cCount, out int stage06Exact,
                out int stage06Semantic);
            Add(checks, "approved_s2_s3_authorities_unchanged",
                s2Count == 38 && s3bCount == 12 && s3cCount >= 17,
                $"S2={s2Count}; S3B={s3bCount}; S3C={s3cCount}; mismatches=0");
            Add(checks, "approved_r44_r05b_stage06_authority_unchanged",
                stage06Exact == 294 && stage06Semantic == 1,
                $"exact={stage06Exact}; knownSemanticNormalization={stage06Semantic}; unexpected=0");
            string buildSettings = Path.Combine(HospitalInteriorS3DR03Builder.ProjectRoot(),
                "ProjectSettings", "EditorBuildSettings.asset");
            string buildSettingsHash = HospitalInteriorS3DR03Builder.Sha256(buildSettings);
            Add(checks, "editor_build_settings_byte_identical",
                string.Equals(buildSettingsHash,
                    HospitalInteriorS3DR03Builder.ExpectedBuildSettingsSha256,
                    StringComparison.OrdinalIgnoreCase), buildSettingsHash);
            Add(checks, "explicit_fifteen_scene_player_matrix",
                HospitalInteriorS3DR03Builder.PlayerScenes.Length == 15
                && HospitalInteriorS3DR03Builder.PlayerScenes.Distinct().Count() == 15
                && HospitalInteriorS3DR03Builder.PlayerScenes.All(path
                    => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                && !HospitalInteriorS3DR03Builder.PlayerScenes.Any(path
                    => path.EndsWith("Exterior_InteriorShellPreview.unity",
                        StringComparison.Ordinal)),
                string.Join(",", HospitalInteriorS3DR03Builder.PlayerScenes));

            ValidateInputActions(checks);
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    HospitalInteriorS3DR03Builder.BootstrapScenePath, OpenSceneMode.Single);
                ValidateIntegrationScene(scene, checks);
                ValidateEntranceAuthority(checks);
            }
            finally
            {
                if (setup.Length > 0 && setup.Any(item => item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            var report = new StaticGate
            {
                status = checks.All(item => item.pass) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(HospitalInteriorS3DR03Builder.ReviewFolder());
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText(Path.Combine(HospitalInteriorS3DR03Builder.ReviewFolder(),
                "StageI1_R03_S3D_StaticGate.json"), json);
            File.WriteAllText(Path.GetFullPath(Path.Combine(
                HospitalInteriorS3DR03Builder.ProjectRoot(),
                HospitalInteriorS3DR03Builder.StaticGateAssetPath)), json);
            AssetDatabase.ImportAsset(HospitalInteriorS3DR03Builder.StaticGateAssetPath,
                ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3D_STATIC_GATE={report.status}; {report.passCount}/{report.totalCount}");
            if (report.status != "PASS")
                throw new InvalidOperationException("S3D static gate failed: "
                    + string.Join("; ", checks.Where(item => !item.pass)
                        .Select(item => item.name + "=" + item.detail)));
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidateInputActions(ICollection<StaticCheck> checks)
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                HospitalInteriorS3DR03Builder.InputAssetPath);
            bool complete = actions != null
                && HasBinding(actions, "Head/Position", "<XRHMD>/centerEyePosition")
                && HasBinding(actions, "Head/Rotation", "<XRHMD>/centerEyeRotation")
                && HasBinding(actions, "Locomotion/Move",
                    "<XRController>{LeftHand}/primary2DAxis")
                && HasBinding(actions, "Locomotion/SnapTurn",
                    "<XRController>{RightHand}/primary2DAxis")
                && HasBinding(actions, "Locomotion/TeleportAim",
                    "<XRController>{RightHand}/primaryButton")
                && HasBinding(actions, "Locomotion/TeleportCancel",
                    "<XRController>{RightHand}/secondaryButton")
                && HasBinding(actions, "Interaction/Activate",
                    "<XRController>{RightHand}/triggerPressed");
            Add(checks, "desktop_pcvr_input_contract", complete,
                actions == null ? "missing" : string.Join(",", actions.actionMaps
                    .SelectMany(map => map.actions).Select(action => action.actionMap.name
                        + "/" + action.name)));
        }

        private static bool HasBinding(InputActionAsset asset, string actionPath,
            string bindingPath)
        {
            InputAction action = asset?.FindAction(actionPath, false);
            return action != null && action.bindings.Any(binding
                => string.Equals(binding.path, bindingPath, StringComparison.Ordinal));
        }

        private static void ValidateIntegrationScene(Scene scene,
            ICollection<StaticCheck> checks)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            T[] All<T>() where T : Component => roots.SelectMany(root
                => root.GetComponentsInChildren<T>(true)).ToArray();

            Add(checks, "single_s3d_runtime_stack",
                All<S3DIntegrationBootstrap>().Length == 1
                && All<S3DAdaptiveRig>().Length == 1
                && All<S3DEntranceDoorController>().Length == 1
                && All<S3DTeleportSurfaceCoordinator>().Length == 1
                && All<S3DRecoveryCoordinator>().Length == 1
                && All<S3DSiteCollisionCoordinator>().Length == 1
                && All<S3DFacadeFloorEdgeCoordinator>().Length == 1
                && All<S3ElevatorController>().Length == 1,
                $"bootstrap={All<S3DIntegrationBootstrap>().Length}; rig={All<S3DAdaptiveRig>().Length}; entrance={All<S3DEntranceDoorController>().Length}; teleport={All<S3DTeleportSurfaceCoordinator>().Length}; recovery={All<S3DRecoveryCoordinator>().Length}; siteCollision={All<S3DSiteCollisionCoordinator>().Length}; facadeEdges={All<S3DFacadeFloorEdgeCoordinator>().Length}; elevator={All<S3ElevatorController>().Length}");
            Add(checks, "review_only_s3c_stack_removed",
                All<S3CReviewBootstrap>().Length == 0
                && All<S3CReviewLocomotion>().Length == 0
                && All<S2ManualFloorLoader>().Length == 0
                && All<S3BReviewBootstrap>().Length == 0,
                "No earlier review bootstrap or locomotion remains serialized in S3D.");
            Add(checks, "one_adaptive_origin_camera_listener",
                All<XROrigin>().Length == 1 && All<Camera>().Length == 1
                && All<AudioListener>().Length == 1
                && All<CharacterController>().Length == 1,
                $"origins={All<XROrigin>().Length}; cameras={All<Camera>().Length}; listeners={All<AudioListener>().Length}; characters={All<CharacterController>().Length}");
            Add(checks, "pcvr_rig_components_and_tracked_poses",
                All<TrackedPoseDriver>().Length == 3
                && All<InputActionManager>().Length == 1
                && All<XRInteractionManager>().Length == 1
                && All<Stage06AActionLocomotionProvider>().Length == 1
                && All<Stage06ASafeTeleportController>().Length == 1,
                $"poseDrivers={All<TrackedPoseDriver>().Length}; actionManagers={All<InputActionManager>().Length}; interactionManagers={All<XRInteractionManager>().Length}; locomotion={All<Stage06AActionLocomotionProvider>().Length}; teleporter={All<Stage06ASafeTeleportController>().Length}");
            Add(checks, "one_integration_directional_light",
                All<Light>().Count(item => item.type == LightType.Directional) == 1
                && All<Light>().Any(item => item.gameObject.name == "S3D_R03_IntegrationSun"),
                "directional=" + All<Light>().Count(item => item.type == LightType.Directional)
                    + "; total=" + All<Light>().Length);
            Add(checks, "bootstrap_references_and_gate_spawn",
                All<S3DIntegrationBootstrap>().Single().ElevatorController != null
                && All<S3DIntegrationBootstrap>().Single().AdaptiveRig != null
                && All<S3DIntegrationBootstrap>().Single().EntranceDoors != null
                && All<S3DIntegrationBootstrap>().Single().TeleportCoordinator != null
                && All<S3DIntegrationBootstrap>().Single().RecoveryCoordinator != null
                && All<S3DIntegrationBootstrap>().Single().SiteCollisionCoordinator != null
                && All<S3DIntegrationBootstrap>().Single().FacadeFloorEdgeCoordinator != null
                && Vector3.Distance(All<S3DAdaptiveRig>().Single().transform.position,
                    S3DIntegrationContract.GateSpawn) <= 0.001f,
                All<S3DAdaptiveRig>().Single().transform.position.ToString("F3"));
            S3DAdaptiveRig adaptive = All<S3DAdaptiveRig>().Single();
            Add(checks, "forgiving_elevator_interaction_targeting",
                adaptive.InteractionDistanceM >= 5.5f
                && adaptive.InteractionAimAssistRadiusM >= 0.18f,
                $"reach={adaptive.InteractionDistanceM:F2}m; aimAssistRadius={adaptive.InteractionAimAssistRadiusM:F2}m");
            bool facadeEdges = All<S3DFacadeFloorEdgeCoordinator>().Single()
                .ValidateConfiguration(out string facadeEdgeDetail);
            Add(checks, "six_natural_main_glass_floor_edge_finishes",
                facadeEdges, facadeEdgeDetail);
            Add(checks, "sixteen_elevator_controls_still_routed",
                All<S3ElevatorButton>().Length == 16
                && All<S3ElevatorButton>().All(item => item.ElevatorController
                    == All<S3ElevatorController>().Single()),
                "buttons=" + All<S3ElevatorButton>().Length);
            int missing = roots.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
            Add(checks, "no_missing_script_references", missing == 0,
                "missingScripts=" + missing + "; roots=" + roots.Length);

            S3ElevatorContract contract = All<S3ElevatorController>().Single().Contract;
            string spatial = "contract missing";
            string shaftDetail = "contract missing";
            string structure = "shaft missing";
            string sweep = "shaft missing";
            bool shaft = contract != null && contract.ValidateSpatialFit(out spatial)
                && contract.ValidateShaftFit(out shaftDetail)
                && All<S3ShaftStructure>().Single().ValidateStructure(out structure)
                && All<S3ShaftStructure>().Single().ValidateCabinSweep(out sweep);
            Add(checks, "approved_s3c_spatial_elevator_contract", shaft,
                shaft ? spatial + "; " + shaftDetail + "; " + structure + "; " + sweep
                    : "approved S3C contract/shaft validation failed");
        }

        private static void ValidateEntranceAuthority(ICollection<StaticCheck> checks)
        {
            Scene interactions = EditorSceneManager.OpenScene(
                HospitalInteriorS2R03Builder.ProductionScenes[3], OpenSceneMode.Additive);
            HospitalDoorPrototype[] leaves = interactions.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<HospitalDoorPrototype>(true))
                .Where(item => item.SourceRootName.StartsWith(
                        "UE_S03B_DOOR_INNER_SlidingLeaf_", StringComparison.Ordinal)
                    || item.SourceRootName.StartsWith(
                        "UE_S03B_DOOR_OUTER_SlidingLeaf_", StringComparison.Ordinal))
                .ToArray();
            bool valid = leaves.Length == 8
                && leaves.Count(item => item.SourceRootName.Contains("_INNER_")) == 4
                && leaves.Count(item => item.SourceRootName.Contains("_OUTER_")) == 4
                && leaves.All(item => item.MotionType == "HorizontalSlide"
                    && item.OpenDistanceMeters > 3f
                    && item.MotionAxis.sqrMagnitude >= 0.99f
                    && item.GetComponentsInChildren<Collider>(true).Length > 0);
            Add(checks, "eight_metadata_driven_automatic_entrance_leaves", valid,
                "leaves=" + leaves.Length + "; "
                    + string.Join(",", leaves.OrderBy(item => item.SourceRootName)
                        .Select(item => item.SourceRootName)));
            EditorSceneManager.CloseScene(interactions, true);
        }

        private static void Add(ICollection<StaticCheck> checks, string name,
            bool pass, string detail)
            => checks.Add(new StaticCheck { name = name, pass = pass, detail = detail });
    }
}
