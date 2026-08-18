using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.Hospital.Stage06A;
using CutMyBodyPlease.Hospital.Stage05.Editor;
using CutMyBodyPlease.HospitalSite.Stage05S;
using CutMyBodyPlease.HospitalSite.Stage05S.Editor;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.Hospital.Stage06A.Editor
{
    public static class HospitalStage06AR01Validator
    {
        [MenuItem("Hospital/Stage 6A/Validate R01 technical gate")]
        public static void ValidateTechnicalGate()
        {
            string workspace = HospitalStage06AR01Builder.WorkspaceRoot();
            var report = new Stage06AGateReport
            {
                schema = Stage06AContract.GateSchema,
                generated_utc = DateTime.UtcNow.ToString("O"),
                stage6a_status = "READY_FOR_HEADSET_REVIEW",
                headset_review_required = true,
                headset_performance_accepted = false,
                checks = new List<Stage06AGateCheck>(),
            };

            Check(report, "frozen_input_manifest", ValidateProtectedManifest);
            Check(report, "unity_openxr_contract", ValidateUnityAndOpenXR);
            Check(report, "frozen_eight_scene_build_settings", HospitalStage06AR01Builder.ValidateFrozenBuildSettings);
            Check(report, "input_action_contract", ValidateInputActions);
            Check(report, "minimal_floor_tracked_xr_rig", ValidateRigPrefab);
            Check(report, "separate_bootstrap_profile_scene", ValidateBootstrapScene);
            Check(report, "exact_runtime_only_route_allowlist", ValidateRouteManifestAndPrefab);
            Check(report, "route_ground_and_head_clearance", ValidateLandingSamples);
            Check(report, "scene_profile_contract", ValidateProfiles);
            Check(report, "frozen_stage05s_runtime_contract", ValidateFrozenSiteRuntime);

            report.pass = report.checks.All(check => check.pass);
            report.status = report.pass ? "PASS" : "FAIL";
            report.disposition = report.pass
                ? "Technical harness gate passed. Physical Quest Link/Air Link review and explicit user approval are still required."
                : "Technical gate failed. Do not begin headset review.";

            string reviewPath = Path.Combine(HospitalStage06AR01Builder.Reviews(workspace), "Stage06A_R01_UnityGate.json");
            HospitalStage06AR01Builder.WriteJson(report, reviewPath);
            HospitalStage06AR01Builder.WriteJson(report, HospitalStage06AR01Builder.AssetPathToAbsolute(HospitalStage06AR01Builder.GatePath));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            if (!report.pass)
                throw new InvalidOperationException("Stage 6A R01 technical gate failed: " + string.Join("; ",
                    report.checks.Where(check => !check.pass).Select(check => check.name + "=" + check.detail)));
            Debug.Log("STAGE06A_R01_TECHNICAL_GATE=PASS; HEADSET_REVIEW_REQUIRED=true");
        }

        public static void ValidateTechnicalGateBatch()
        {
            try
            {
                ValidateTechnicalGate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateProtectedManifest()
        {
            string path = HospitalStage06AR01Builder.AssetPathToAbsolute(HospitalStage06AR01Builder.ManifestPath);
            Require(File.Exists(path), "Protected input manifest is missing.");
            Stage06AProtectedManifest manifest = JsonUtility.FromJson<Stage06AProtectedManifest>(File.ReadAllText(path));
            Require(manifest != null && manifest.status == "PASS" && manifest.files != null && manifest.files.Length >= 200,
                "Protected input manifest is incomplete.");
            Require(manifest.files.Count(entry => entry.role == "frozen_r44_scene") == 5 &&
                    manifest.files.Count(entry => entry.role == "frozen_stage05s_scene") == 3,
                "Protected scene inventory must remain five R44 plus three S6 scenes.");
            Require(manifest.files.Count(entry => entry.role == "frozen_hospital_source_and_import") >= 20 &&
                    manifest.files.Count(entry => entry.role == "frozen_hospital_material") >= 60 &&
                    manifest.files.Count(entry => entry.role == "frozen_hospital_prefab_collision_lod") >= 46,
                "Hospital source/material/prefab freeze coverage is incomplete.");
            Require(manifest.files.Count(entry => entry.role == "frozen_site_source_and_import") >= 14 &&
                    manifest.files.Count(entry => entry.role == "frozen_site_material") >= 40 &&
                    manifest.files.Count(entry => entry.role == "frozen_site_prefab_collision_lod") >= 14 &&
                    manifest.files.Any(entry => entry.role == "frozen_site_setting" &&
                                                entry.path.EndsWith("HospitalSiteGrassRuntimeConfig.asset", StringComparison.Ordinal)),
                "Site source/material/LOD/grass freeze coverage is incomplete.");
            Require(manifest.r40_authority_sha256 == "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126" &&
                    manifest.r40_exact_snapshot_sha256 == "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58" &&
                    manifest.files.Any(entry => entry.role == "frozen_r40_authority") &&
                    manifest.files.Any(entry => entry.role == "frozen_r40_exact_snapshot") &&
                    manifest.files.Any(entry => entry.role == "frozen_r05b_authority") &&
                    manifest.files.Any(entry => entry.role == "frozen_package_lock") &&
                    manifest.files.Any(entry => entry.role == "frozen_openxr_asset"),
                "Authority/package/OpenXR freeze coverage is incomplete.");
            foreach (Stage06AFileRecord entry in manifest.files)
            {
                string absolute = HospitalStage06AR01Builder.AssetPathToAbsolute(entry.path);
                Require(File.Exists(absolute), "Protected file is missing: " + entry.path);
                Require(HospitalStage06AR01Builder.Sha256(absolute) == entry.sha256,
                    "Protected file changed after preflight: " + entry.path);
                Require(new FileInfo(absolute).Length == entry.bytes, "Protected file length changed: " + entry.path);
            }
            for (int i = 0; i < HospitalStage05SRoundS6Builder.HospitalScenes.Length; i++)
                Require(HospitalStage06AR01Builder.Sha256(HospitalStage06AR01Builder.AssetPathToAbsolute(
                            HospitalStage05SRoundS6Builder.HospitalScenes[i])) == HospitalStage05SRoundS6Builder.HospitalSceneHashes[i],
                    "Frozen R44 scene does not match its approved hash.");
        }

        private static void ValidateUnityAndOpenXR()
        {
            Require(Application.unityVersion == "6000.3.20f1", "Unexpected Unity version: " + Application.unityVersion);
            Require(HospitalStage05XRSetup.IsValid(out string detail), "OpenXR contract failed: " + detail);
            string packageManifest = File.ReadAllText(HospitalStage06AR01Builder.AssetPathToAbsolute("Packages/manifest.json"));
            foreach (string required in new[]
                     {
                         "\"com.unity.inputsystem\": \"1.20.0\"",
                         "\"com.unity.render-pipelines.universal\": \"17.3.0\"",
                         "\"com.unity.xr.interaction.toolkit\": \"3.5.1\"",
                         "\"com.unity.xr.management\": \"4.7.0\"",
                         "\"com.unity.xr.openxr\": \"1.17.1\"",
                     })
                Require(packageManifest.Contains(required), "Package contract missing: " + required);
        }

        private static void ValidateInputActions()
        {
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(HospitalStage06AR01Builder.InputAssetPath);
            Require(actions != null, "Stage 6A input action asset is missing.");
            string[] expectedMaps = { "Head", "LeftController", "RightController", "Locomotion" };
            Require(actions.actionMaps.Select(map => map.name).SequenceEqual(expectedMaps), "Unexpected action-map inventory.");

            ExpectBinding(actions, "Head/Position", "<XRHMD>/centerEyePosition");
            ExpectBinding(actions, "Head/Rotation", "<XRHMD>/centerEyeRotation");
            ExpectBinding(actions, "LeftController/Position", "<XRController>{LeftHand}/devicePosition");
            ExpectBinding(actions, "LeftController/Rotation", "<XRController>{LeftHand}/deviceRotation");
            ExpectBinding(actions, "RightController/Position", "<XRController>{RightHand}/devicePosition");
            ExpectBinding(actions, "RightController/Rotation", "<XRController>{RightHand}/deviceRotation");
            ExpectBinding(actions, "Locomotion/Move", "<XRController>{LeftHand}/primary2DAxis");
            ExpectBinding(actions, "Locomotion/SnapTurn", "<XRController>{RightHand}/primary2DAxis");
            ExpectBinding(actions, "Locomotion/TeleportAim", "<XRController>{RightHand}/primaryButton");
            ExpectBinding(actions, "Locomotion/TeleportCancel", "<XRController>{RightHand}/secondaryButton");
            Require(actions.FindAction("Locomotion/Move").bindings.All(binding =>
                    !binding.effectivePath.Contains("RightHand", StringComparison.OrdinalIgnoreCase)),
                "Right controller must not drive continuous movement.");
            Require(actions.FindAction("Locomotion/SnapTurn").bindings.All(binding =>
                    !binding.effectivePath.Contains("LeftHand", StringComparison.OrdinalIgnoreCase)),
                "Left controller must not drive snap turn.");
        }

        private static void ValidateRigPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage06AR01Builder.RigPrefabPath);
            Require(prefab != null, "XR rig prefab is missing.");
            Require(prefab.GetComponentsInChildren<XROrigin>(true).Length == 1, "Rig must own exactly one XR Origin.");
            Require(prefab.GetComponentsInChildren<Camera>(true).Length == 1, "Rig must own exactly one camera.");
            Require(prefab.GetComponentsInChildren<AudioListener>(true).Length == 1, "Rig must own exactly one audio listener.");
            Require(prefab.GetComponentsInChildren<CharacterController>(true).Length == 1, "Rig must own one CharacterController.");
            Require(prefab.GetComponentsInChildren<TrackedPoseDriver>(true).Length == 3, "Head and both controllers require tracked pose drivers.");
            Require(prefab.GetComponentsInChildren<Stage06AControllerTrackingVisual>(true).Length == 2, "Two tracked controller visuals are required.");
            Require(prefab.GetComponentsInChildren<XRInteractionManager>(true).Length == 1, "One XR Interaction Manager is required.");
            Require(prefab.GetComponentsInChildren<InputActionManager>(true).Length == 1, "One Input Action Manager is required.");
            Require(prefab.GetComponentsInChildren<XRBodyTransformer>(true).Length == 1, "One XR Body Transformer is required.");
            Require(prefab.GetComponentsInChildren<LocomotionMediator>(true).Length == 1, "One Locomotion Mediator is required.");
            Require(prefab.GetComponentsInChildren<GravityProvider>(true).Length == 1, "One Gravity Provider is required.");
            Require(prefab.GetComponentsInChildren<TeleportationProvider>(true).Length == 1, "One Teleportation Provider is required.");
            Require(prefab.GetComponentsInChildren<Stage06AActionLocomotionProvider>(true).Length == 1, "Action locomotion provider is missing.");
            Require(prefab.GetComponentsInChildren<Stage06ASafeTeleportController>(true).Length == 1, "Safe teleport controller is missing.");
            Require(prefab.GetComponentsInChildren<Stage06ASceneBootstrap>(true).Length == 1, "Scene bootstrap is missing.");
            Require(prefab.GetComponentsInChildren<Stage06ATraversalRecorder>(true).Length == 1, "Traversal recorder is missing.");

            XROrigin origin = prefab.GetComponent<XROrigin>();
            Require(Vector3.Distance(prefab.transform.position, Stage06AContract.CombinedSpawn) <= 0.001f,
                "Combined harness spawn must remain at the main gate.");
            Require(origin.RequestedTrackingOriginMode == XROrigin.TrackingOriginMode.Floor, "XR Origin must request floor tracking.");
            Require(origin.Camera != null && origin.Camera.CompareTag("MainCamera"), "XR camera must be assigned and tagged MainCamera.");
            Require(origin.Camera.transform.localPosition == Vector3.zero, "Floor tracking must not use a forced 1.7 m camera offset.");
            CharacterController character = prefab.GetComponent<CharacterController>();
            Require(Approximately(character.radius, 0.25f) && Approximately(character.stepOffset, 0.20f) &&
                    Approximately(character.slopeLimit, 45f) && Approximately(character.skinWidth, 0.03f),
                "CharacterController dimensions changed.");
            Stage06AActionLocomotionProvider locomotion = prefab.GetComponent<Stage06AActionLocomotionProvider>();
            Require(Approximately(locomotion.MoveSpeed, 2f) && Approximately(locomotion.SnapTurnDegrees, 45f),
                "Locomotion speed/turn defaults changed.");
            Require(prefab.GetComponentsInChildren<LineRenderer>(true).Length == 1, "One right-hand teleport arc is required.");
            Require(prefab.GetComponentsInChildren<Collider>(true).Count(collider => collider != character) == 0,
                "Controller/reticle visuals must not introduce colliders.");
        }

        private static void ValidateBootstrapScene()
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalStage06AR01Builder.BootstrapScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            string[] names = roots.Select(root => root.name).OrderBy(name => name).ToArray();
            Require(names.SequenceEqual(new[] { "Stage06A_QA_DirectionalSun", "Stage06A_QA_Waypoints", "Stage06A_XROrigin" }),
                "Bootstrap scene root ownership changed: " + string.Join(",", names));
            Require(roots.Sum(root => root.GetComponentsInChildren<Camera>(true).Length) == 1, "Bootstrap requires one camera.");
            Require(roots.Sum(root => root.GetComponentsInChildren<Light>(true).Length) == 1, "Bootstrap requires one QA sun.");
            Stage06AWaypoint[] waypoints = roots.SelectMany(root => root.GetComponentsInChildren<Stage06AWaypoint>(true))
                .OrderBy(waypoint => waypoint.Index).ToArray();
            Require(waypoints.Length == 6 && waypoints.Select(waypoint => waypoint.Index).SequenceEqual(Enumerable.Range(0, 6)),
                "Six ordered QA waypoints are required.");
            Require(waypoints.All(waypoint => waypoint.GetComponent<SphereCollider>() is { isTrigger: true }),
                "Every waypoint must be an invisible trigger.");
            Vector3[] expectedWaypointCenters =
            {
                new Vector3(0f, 1f, -99f), new Vector3(0f, 1f, -42f), new Vector3(36f, 1f, -65f),
                new Vector3(-58.37f, 1f, -94.93f), new Vector3(-53f, 1f, 51f), new Vector3(0f, 1f, -99f),
            };
            Require(waypoints.Select((waypoint, index) =>
                    Vector3.Distance(waypoint.transform.position, expectedWaypointCenters[index]) <= 0.01f &&
                    Approximately(waypoint.GetComponent<SphereCollider>().radius, 4f) && waypoint.gameObject.layer == 2)
                .All(value => value), "Waypoint coordinates, trigger radii or Ignore Raycast layer changed.");
            Require(!EditorBuildSettings.scenes.Any(entry => entry.path == HospitalStage06AR01Builder.BootstrapScenePath),
                "Bootstrap must remain outside frozen EditorBuildSettings; custom build options provide its scene list.");
        }

        private static void ValidateRouteManifestAndPrefab()
        {
            GameObject core = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_CoreRoadParking.prefab");
            MeshCollider[] routes = core.GetComponentsInChildren<MeshCollider>(true);
            Require(routes.Length == 20 && routes.All(route => route.sharedMesh != null && !route.isTrigger),
                "Frozen route collider inventory must remain exactly 20.");
            Require(core.GetComponentsInChildren<TeleportationArea>(true).Length == 0,
                "TeleportationArea components must be runtime-only; frozen site prefab was modified.");
            Stage06ARouteManifest manifest = JsonUtility.FromJson<Stage06ARouteManifest>(File.ReadAllText(
                HospitalStage06AR01Builder.AssetPathToAbsolute(HospitalStage06AR01Builder.RouteManifestPath)));
            Require(manifest != null && manifest.status == "PASS" && manifest.runtime_only_teleport_components &&
                    manifest.route_surface_count == 20 && manifest.surfaces.Length == 20,
                "Route surface manifest is incomplete.");
            string[] actualPaths = routes.Select(route => Stage06ASceneBootstrap.HierarchyPath(route.transform)).OrderBy(value => value).ToArray();
            Require(actualPaths.SequenceEqual(manifest.surfaces.Select(surface => surface.path).OrderBy(value => value)),
                "Route surface transform paths changed after manifest generation.");
        }

        private static void ValidateLandingSamples()
        {
            EditorSceneManager.OpenScene(HospitalStage06AR01Builder.BootstrapScenePath, OpenSceneMode.Single);
            foreach (string path in Stage06AContract.HospitalScenes.Concat(Stage06AContract.SiteScenes))
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Physics.SyncTransforms();

            Scene coreScene = SceneManager.GetSceneByPath(Stage06AContract.SiteScenes[0]);
            MeshCollider[] routes = coreScene.GetRootGameObjects().Where(root => root.name == "SITE_CoreRoadParking")
                .SelectMany(root => root.GetComponentsInChildren<MeshCollider>(true)).ToArray();
            CharacterController character = Object.FindFirstObjectByType<CharacterController>();
            character.enabled = false;
            var buffer = new Collider[64];
            Vector3[] valid =
            {
                new Vector3(0f, 0f, -99f), new Vector3(0f, 0f, -42f), new Vector3(36f, 0f, -65f),
                new Vector3(-58.37f, 0f, -94.93f), new Vector3(-53f, 0f, 51f),
            };
            foreach (Vector3 point in valid)
            {
                bool found = TryFindSafeCheckpointLanding(routes, point, character, buffer,
                    out RaycastHit hit, out float checkpointDistance);
                Require(found && checkpointDistance <= 3.5f,
                    $"No safe approved-route landing inside the 4 m checkpoint trigger at {point}; " +
                    (found ? $"nearest sample={hit.point}, distance={checkpointDistance:F2} m" : "none found within 30 m"));
            }

            Vector3[] invalidLawn =
            {
                new Vector3(-8f, 0f, -52f), new Vector3(-24f, 0f, -56f), new Vector3(-10f, 0f, -46f),
            };
            foreach (Vector3 point in invalidLawn)
                Require(!TryRouteHit(routes, point, out _), "Known lawn point became teleportable: " + point);
        }

        private static void ValidateProfiles()
        {
            Require(Stage06AContract.ScenesFor(Stage06ALoadProfile.Combined).Length == 8, "Combined profile must load eight scenes.");
            Require(Stage06AContract.ScenesFor(Stage06ALoadProfile.HospitalOnly).SequenceEqual(Stage06AContract.HospitalScenes),
                "HospitalOnly profile changed.");
            Require(Stage06AContract.ScenesFor(Stage06ALoadProfile.SiteOnly).SequenceEqual(Stage06AContract.SiteScenes),
                "SiteOnly profile changed.");
            Require(Stage06AContract.ScenesFor(Stage06ALoadProfile.Combined).Distinct().Count() == 8,
                "Combined profile contains duplicates.");
            GameObject grass = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_GrassRuntime.prefab");
            Require(grass.GetComponentsInChildren<HospitalSiteGrassRenderer>(true).Length == 1,
                "Site profile must own exactly one grass runtime manager.");
        }

        private static void ValidateFrozenSiteRuntime()
        {
            GameObject props = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_PropsVegetation.prefab");
            Require(props.GetComponentsInChildren<LODGroup>(true).Length == 99, "Frozen vegetation LOD inventory changed.");
            GameObject grass = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_GrassRuntime.prefab");
            Require(grass.GetComponentsInChildren<HospitalSiteGrassRenderer>(true).Length == 1, "Frozen grass manager changed.");
            GameObject gate = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_MainGate.prefab");
            Require(gate.GetComponentsInChildren<Transform>(true).Count(transform =>
                    transform.name.IndexOf("GateLeafRoot", StringComparison.OrdinalIgnoreCase) >= 0) == 2,
                "Frozen gate-leaf root inventory changed.");
        }

        private static bool TryRouteHit(IEnumerable<MeshCollider> routes, Vector3 floorPoint, out RaycastHit best)
        {
            best = default;
            bool found = false;
            float closest = float.PositiveInfinity;
            var ray = new Ray(floorPoint + Vector3.up * 20f, Vector3.down);
            foreach (MeshCollider route in routes)
            {
                if (route.Raycast(ray, out RaycastHit hit, 40f) && hit.distance < closest)
                {
                    best = hit;
                    closest = hit.distance;
                    found = true;
                }
            }
            return found;
        }

        private static bool TryFindSafeCheckpointLanding(MeshCollider[] routes, Vector3 checkpoint,
            CharacterController character, Collider[] overlapBuffer, out RaycastHit safeHit, out float checkpointDistance)
        {
            // S6 markers are review markers, not authored nav points. Preserve their exact transforms and
            // search only inside their existing 4 m non-blocking trigger for a real approved-route landing.
            const float maximumRadius = 3.5f;
            const float radialStep = 0.25f;
            const int angularSamples = 32;
            for (float radius = 0f; radius <= maximumRadius + 0.001f; radius += radialStep)
            {
                int samples = radius < 0.001f ? 1 : angularSamples;
                for (int index = 0; index < samples; index++)
                {
                    float angle = index * Mathf.PI * 2f / samples;
                    Vector3 sample = checkpoint + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (TryRouteHit(routes, sample, out RaycastHit hit) &&
                        Stage06ASafeTeleportController.ValidateLanding(hit, routes, character, 2.1f, overlapBuffer))
                    {
                        safeHit = hit;
                        checkpointDistance = radius;
                        return true;
                    }
                }
            }
            safeHit = default;
            checkpointDistance = float.PositiveInfinity;
            return false;
        }

        private static void ExpectBinding(InputActionAsset asset, string actionPath, string expectedPath)
        {
            InputAction action = asset.FindAction(actionPath, false);
            Require(action != null, "Missing action: " + actionPath);
            Require(action.bindings.Count == 1 && string.Equals(action.bindings[0].effectivePath, expectedPath, StringComparison.OrdinalIgnoreCase),
                $"Binding mismatch for {actionPath}; expected={expectedPath}; actual={string.Join(",", action.bindings.Select(binding => binding.effectivePath))}");
        }

        private static void Check(Stage06AGateReport report, string name, Action validation)
        {
            try
            {
                validation();
                report.checks.Add(new Stage06AGateCheck { name = name, pass = true, detail = "PASS" });
            }
            catch (Exception exception)
            {
                report.checks.Add(new Stage06AGateCheck { name = name, pass = false, detail = exception.Message });
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static bool Approximately(float a, float b) => Mathf.Abs(a - b) <= 0.001f;
    }

    [Serializable] internal sealed class Stage06AGateReport
    {
        public string schema, status, generated_utc, stage6a_status, disposition;
        public bool pass, headset_review_required, headset_performance_accepted;
        public List<Stage06AGateCheck> checks;
    }
    [Serializable] internal sealed class Stage06AGateCheck { public string name, detail; public bool pass; }
}
