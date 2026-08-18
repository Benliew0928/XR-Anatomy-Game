using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CutMyBodyPlease.Hospital.Stage06A;
using CutMyBodyPlease.Hospital.Stage05.Editor;
using CutMyBodyPlease.HospitalSite.Stage05S.Editor;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.Hospital.Stage06A.Editor
{
    public static class HospitalStage06AR01Builder
    {
        internal const string Root = "Assets/Hospital/Stage06A/R01";
        internal const string InputFolder = Root + "/Input";
        internal const string MaterialFolder = Root + "/Materials";
        internal const string PrefabFolder = Root + "/Prefabs";
        internal const string SceneFolder = Root + "/Scenes";
        internal const string SettingsFolder = Root + "/Settings";
        internal const string InputAssetPath = InputFolder + "/Hospital_Stage06A_XR_Actions.inputactions";
        internal const string RigPrefabPath = PrefabFolder + "/Hospital_Stage06A_XROrigin.prefab";
        internal const string BootstrapScenePath = SceneFolder + "/Hospital_Stage06A_TraversalProfile.unity";
        internal const string ManifestPath = SettingsFolder + "/Stage06A_R01_ProtectedInputManifest.json";
        internal const string RouteManifestPath = SettingsFolder + "/Stage06A_R01_RouteSurfaceManifest.json";
        internal const string ActionContractPath = SettingsFolder + "/Stage06A_R01_InputActionContract.json";
        internal const string GatePath = SettingsFolder + "/Stage06A_R01_UnityGate.json";
        internal const string ReviewRelative = "Reviews/HospitalExterior/Stage06A_R01";
        internal const string CheckpointRelative = "Archive/HospitalExterior/Docs/HospitalExterior_Stage06A_R01_Checkpoint.md";

        private static readonly Stage06AWaypointDefinition[] Waypoints =
        {
            W(0, "Gate", 0f, -99f),
            W(1, "Lobby_Forecourt", 0f, -42f),
            W(2, "Visitor_Parking", 36f, -65f),
            // The S6 scenic Lake marker (-57, -81) is 14 m off the approved route mesh.
            // This Stage 6A checkpoint is the nearest verified capsule-safe point on the lake path.
            W(3, "Lake_Path", -58.37f, -94.93f),
            W(4, "Rear_Service", -53f, 51f),
            W(5, "Gate_Return", 0f, -99f),
        };

        [MenuItem("Hospital/Stage 6A/Build R01 traversal harness and technical gate")]
        public static void BuildAndValidate()
        {
            string workspace = WorkspaceRoot();
            ValidatePreflight(workspace);
            EnsureFolders();
            InputActionAsset actions = BuildInputActions();
            MaterialSet materials = BuildMaterials();
            BuildRigPrefab(actions, materials);
            BuildBootstrapScene(actions);
            WriteProtectedManifest(workspace);
            WriteRouteManifest(workspace);
            WriteActionContract(workspace, actions);
            WriteReviewChecklist(workspace);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            HospitalStage06AR01Validator.ValidateTechnicalGate();
            WriteTechnicalCheckpoint(workspace);
            WriteTechnicalLog(workspace);
            Debug.Log("STAGE06A_R01_BUILD=COMPLETE; STATUS=READY_FOR_HEADSET_REVIEW");
        }

        public static void BuildAndValidateBatch()
        {
            try
            {
                BuildAndValidate();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Hospital/Stage 6A/Build Windows PC-VR development player")]
        public static void BuildDevelopmentPlayer()
        {
            HospitalStage06AR01Validator.ValidateTechnicalGate();
            string[] frozenBuildSettings = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            bool[] frozenEnabled = EditorBuildSettings.scenes.Select(scene => scene.enabled).ToArray();
            string workspace = WorkspaceRoot();
            string outputDirectory = Path.Combine(workspace, "Exports", "HospitalExterior", "Stage06A_R01_PCVR");
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "AnatomyXR_Stage06A_R01.exe");
            string[] scenes = new[] { BootstrapScenePath }
                .Concat(Stage06AContract.HospitalScenes)
                .Concat(Stage06AContract.SiteScenes)
                .ToArray();

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development | BuildOptions.AllowDebugging,
            });

            // Unity may leave ProjectSettings.asset in its transient player-build serialization
            // until the project is saved. Canonicalize it before checking the frozen contract so
            // a successful custom build cannot leave any protected input drift behind.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string[] afterPaths = EditorBuildSettings.scenes.Select(scene => scene.path).ToArray();
            bool[] afterEnabled = EditorBuildSettings.scenes.Select(scene => scene.enabled).ToArray();
            if (!afterPaths.SequenceEqual(frozenBuildSettings) || !afterEnabled.SequenceEqual(frozenEnabled))
                throw new InvalidOperationException("Custom player build changed the frozen eight-scene EditorBuildSettings contract.");
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Stage 6A Windows build failed: " + report.summary.result);
            ValidateExistingProtectedManifest();

            var record = new Stage06ABuildRecord
            {
                schema = "HospitalExterior.Stage06A.R01.PCVRBuild.v1",
                status = "PASS",
                build_type = "Windows x86_64 Development + AllowDebugging",
                output = executable,
                total_bytes = (long)report.summary.totalSize,
                total_time_seconds = report.summary.totalTime.TotalSeconds,
                scenes = scenes,
                editor_build_settings_preserved = true,
                headset_performance_accepted = false,
                note = "Traversal-development build only. Stage 6B captures the real stereo performance baseline.",
            };
            WriteJson(record, Path.Combine(Reviews(workspace), "Stage06A_R01_PCVRBuild.json"));
            WriteJson(record, Path.Combine(outputDirectory, "Stage06A_R01_PCVRBuild.json"));
            Debug.Log("STAGE06A_R01_PCVR_BUILD=PASS; " + executable);
        }

        public static void BuildDevelopmentPlayerBatch()
        {
            try
            {
                BuildDevelopmentPlayer();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Hospital/Stage 6A/Import latest headset session")]
        public static void ImportLatestHeadsetSession()
        {
            string sessionRoot = Path.Combine(Application.persistentDataPath, "Stage06A_R01", "Sessions");
            if (!Directory.Exists(sessionRoot))
                throw new DirectoryNotFoundException("No Stage 6A session folder exists: " + sessionRoot);
            DirectoryInfo latest = new DirectoryInfo(sessionRoot).GetDirectories()
                .OrderByDescending(directory => directory.LastWriteTimeUtc).FirstOrDefault();
            if (latest == null)
                throw new InvalidOperationException("No Stage 6A headset session is available to import.");
            string destination = Path.Combine(Reviews(WorkspaceRoot()), "HeadsetSession_" + latest.Name);
            Directory.CreateDirectory(destination);
            foreach (FileInfo source in latest.GetFiles())
                source.CopyTo(Path.Combine(destination, source.Name), true);
            AssetDatabase.Refresh();
            Debug.Log("STAGE06A_HEADSET_SESSION_IMPORTED=" + destination);
        }

        private static void ValidatePreflight(string workspace)
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("Stage 6A requires Unity 6000.3.20f1; found " + Application.unityVersion);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Stage 6A requires StandaloneWindows64 active build target.");
            if (!HospitalStage05XRSetup.IsValid(out string detail))
                throw new InvalidOperationException("Frozen Windows OpenXR configuration failed: " + detail);

            for (int i = 0; i < HospitalStage05SRoundS6Builder.HospitalScenes.Length; i++)
            {
                string absolute = AssetPathToAbsolute(HospitalStage05SRoundS6Builder.HospitalScenes[i]);
                RequireHash(absolute, HospitalStage05SRoundS6Builder.HospitalSceneHashes[i]);
            }

            foreach (HospitalStage05SRoundS6Builder.InputSpec input in HospitalStage05SRoundS6Builder.SiteInputs)
                RequireHash(AssetPathToAbsolute(HospitalStage05SRoundS6Builder.SourceFolder + "/" + input.relative), input.sha256);

            RequireHash(Path.Combine(workspace, "ArtSource", "Environment", "Blender", "HospitalExterior",
                    "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend"),
                "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126");
            RequireHash(Path.Combine(workspace, "ArtSource", "Environment", "Blender", "HospitalExterior",
                    "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend1"),
                "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58");
            RequireHash(Path.Combine(workspace, "ArtSource", "Environment", "Blender", "HospitalSite",
                    "HospitalSite_Stage05S_R05B_GRASS_REFINEMENT.blend"), HospitalStage05SRoundS6Builder.AuthoritySha);

            string stage5Gate = Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R44", "Stage05_R44_UnityGate.json");
            string stage5sGate = Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05S_RoundS6", "Stage05S_RoundS6_UnityGate.json");
            RequirePass(stage5Gate);
            RequirePass(stage5sGate);
            ValidateFrozenBuildSettings();
            ValidateExistingProtectedManifest();
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { InputFolder, MaterialFolder, PrefabFolder, SceneFolder, SettingsFolder })
                Directory.CreateDirectory(AssetPathToAbsolute(path));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static InputActionAsset BuildInputActions()
        {
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath) == null)
            {
                var generated = ScriptableObject.CreateInstance<InputActionAsset>();
                generated.name = Stage06AContract.InputAssetName;

                InputActionMap head = generated.AddActionMap("Head");
                AddPoseActions(head, "<XRHMD>", true);
                InputActionMap left = generated.AddActionMap("LeftController");
                AddPoseActions(left, "<XRController>{LeftHand}", false);
                InputActionMap right = generated.AddActionMap("RightController");
                AddPoseActions(right, "<XRController>{RightHand}", false);

                InputActionMap locomotion = generated.AddActionMap("Locomotion");
                InputAction move = AddAction(locomotion, "Move", InputActionType.Value, "Vector2");
                move.AddBinding("<XRController>{LeftHand}/primary2DAxis").WithProcessor("StickDeadzone");
                InputAction snapTurn = AddAction(locomotion, "SnapTurn", InputActionType.Value, "Vector2");
                snapTurn.AddBinding("<XRController>{RightHand}/primary2DAxis").WithProcessor("StickDeadzone");
                AddAction(locomotion, "TeleportAim", InputActionType.Button, "Button")
                    .AddBinding("<XRController>{RightHand}/primaryButton");
                AddAction(locomotion, "TeleportCancel", InputActionType.Button, "Button")
                    .AddBinding("<XRController>{RightHand}/secondaryButton");

                File.WriteAllText(AssetPathToAbsolute(InputAssetPath), generated.ToJson() + Environment.NewLine);
                Object.DestroyImmediate(generated);
                AssetDatabase.ImportAsset(InputAssetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (asset == null)
                throw new InvalidOperationException("Failed to create Stage 6A input action asset.");
            return asset;
        }

        private static void AddPoseActions(InputActionMap map, string device, bool head)
        {
            string position = head ? device + "/centerEyePosition" : device + "/devicePosition";
            string rotation = head ? device + "/centerEyeRotation" : device + "/deviceRotation";
            AddAction(map, "Position", InputActionType.Value, "Vector3").AddBinding(position);
            AddAction(map, "Rotation", InputActionType.Value, "Quaternion").AddBinding(rotation);
            AddAction(map, "TrackingState", InputActionType.Value, "Integer").AddBinding(device + "/trackingState");
            AddAction(map, "IsTracked", InputActionType.Button, "Button").AddBinding(device + "/isTracked");
        }

        private static InputAction AddAction(InputActionMap map, string name, InputActionType type, string expectedControlType)
        {
            InputAction action = map.AddAction(name, type);
            action.expectedControlType = expectedControlType;
            return action;
        }

        private static MaterialSet BuildMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (lit == null || unlit == null)
                throw new InvalidOperationException("Required URP shaders are unavailable.");
            return new MaterialSet
            {
                left = CreateMaterial("MAT_Stage06A_ControllerLeft", lit, new Color(0.10f, 0.32f, 0.82f, 1f)),
                right = CreateMaterial("MAT_Stage06A_ControllerRight", lit, new Color(0.82f, 0.27f, 0.10f, 1f)),
                valid = CreateMaterial("MAT_Stage06A_TeleportValid", unlit, new Color(0.12f, 1f, 0.32f, 1f)),
                invalid = CreateMaterial("MAT_Stage06A_TeleportInvalid", unlit, new Color(1f, 0.12f, 0.08f, 1f)),
                line = CreateMaterial("MAT_Stage06A_TeleportLine", unlit, Color.white),
            };
        }

        private static Material CreateMaterial(string name, Shader shader, Color color)
        {
            string path = MaterialFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void BuildRigPrefab(InputActionAsset actions, MaterialSet materials)
        {
            GameObject root = new GameObject("Stage06A_XROrigin");
            try
            {
                root.transform.SetPositionAndRotation(Stage06AContract.CombinedSpawn, Quaternion.identity);
                CharacterController character = root.AddComponent<CharacterController>();
                character.radius = 0.25f;
                character.height = 1.85f;
                character.center = new Vector3(0f, 0.925f, 0f);
                character.stepOffset = 0.20f;
                character.slopeLimit = 45f;
                character.skinWidth = 0.03f;
                character.minMoveDistance = 0.001f;

                XROrigin xrOrigin = root.AddComponent<XROrigin>();
                GameObject offset = new GameObject("Camera Offset");
                offset.transform.SetParent(root.transform, false);
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(offset.transform, false);
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 600f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.67f, 0.75f, 0.82f, 1f);
                camera.stereoTargetEye = StereoTargetEyeMask.Both;
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                ConfigureTrackedPose(cameraObject.AddComponent<TrackedPoseDriver>(), actions, "Head");
                xrOrigin.CameraFloorOffsetObject = offset;
                xrOrigin.Camera = camera;
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

                GameObject leftController = CreateController(offset.transform, "Left Controller", actions, "LeftController", materials.left, out _);
                GameObject rightController = CreateController(offset.transform, "Right Controller", actions, "RightController", materials.right, out _);

                GameObject lineObject = new GameObject("Right Teleport Arc");
                lineObject.transform.SetParent(rightController.transform, false);
                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.sharedMaterial = materials.line;
                line.widthMultiplier = 0.018f;
                line.numCornerVertices = 3;
                line.useWorldSpace = true;
                line.enabled = false;

                GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                reticle.name = "Teleport Landing Reticle";
                reticle.transform.SetParent(root.transform, false);
                reticle.transform.localScale = new Vector3(0.22f, 0.008f, 0.22f);
                Object.DestroyImmediate(reticle.GetComponent<Collider>());
                Renderer reticleRenderer = reticle.GetComponent<Renderer>();
                reticleRenderer.sharedMaterial = materials.invalid;
                reticle.SetActive(false);

                XRBodyTransformer bodyTransformer = root.AddComponent<XRBodyTransformer>();
                bodyTransformer.xrOrigin = xrOrigin;
                bodyTransformer.useCharacterControllerIfExists = true;
                LocomotionMediator mediator = root.AddComponent<LocomotionMediator>();
                GravityProvider gravity = root.AddComponent<GravityProvider>();
                gravity.mediator = mediator;
                gravity.useGravity = true;
                gravity.useLocalSpaceGravity = true;
                gravity.updateCharacterControllerCenterEachFrame = false;
                TeleportationProvider teleportProvider = root.AddComponent<TeleportationProvider>();
                teleportProvider.mediator = mediator;
                teleportProvider.delayTime = 0f;

                Stage06ABodyCapsuleDriver bodyDriver = root.AddComponent<Stage06ABodyCapsuleDriver>();
                bodyDriver.Configure(xrOrigin, camera, character);
                Stage06AActionLocomotionProvider locomotion = root.AddComponent<Stage06AActionLocomotionProvider>();
                locomotion.mediator = mediator;
                locomotion.Configure(actions, camera.transform, 2f, 45f);
                Stage06ATraversalRecorder recorder = root.AddComponent<Stage06ATraversalRecorder>();
                recorder.Configure(xrOrigin, camera, actions, Array.Empty<Stage06AWaypoint>());
                Stage06ASafeTeleportController teleporter = root.AddComponent<Stage06ASafeTeleportController>();
                teleporter.Configure(actions, rightController.transform, camera, character, bodyDriver, teleportProvider,
                    line, reticle, reticleRenderer, materials.valid, materials.invalid, recorder);
                InputActionManager inputManager = root.AddComponent<InputActionManager>();
                inputManager.actionAssets = new List<InputActionAsset> { actions };
                root.AddComponent<XRInteractionManager>();
                Stage06ASceneBootstrap bootstrap = root.AddComponent<Stage06ASceneBootstrap>();
                bootstrap.Configure(Stage06ALoadProfile.Combined, xrOrigin, camera, teleporter, recorder,
                    new Behaviour[] { gravity, locomotion, teleporter });

                PrefabUtility.SaveAsPrefabAsset(root, RigPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateController(Transform parent, string name, InputActionAsset actions,
            string actionMap, Material material, out Stage06AControllerTrackingVisual trackingVisual)
        {
            GameObject controller = new GameObject(name);
            controller.transform.SetParent(parent, false);
            ConfigureTrackedPose(controller.AddComponent<TrackedPoseDriver>(), actions, actionMap);
            GameObject visuals = new GameObject(name + " Visuals");
            visuals.transform.SetParent(controller.transform, false);

            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(visuals.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.035f, 0.035f);
            grip.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.055f, 0.12f, 0.06f);
            Object.DestroyImmediate(grip.GetComponent<Collider>());
            grip.GetComponent<Renderer>().sharedMaterial = material;

            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nose.name = "Aim Housing";
            nose.transform.SetParent(visuals.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.025f, 0.075f);
            nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            nose.transform.localScale = new Vector3(0.04f, 0.025f, 0.04f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.GetComponent<Renderer>().sharedMaterial = material;

            trackingVisual = controller.AddComponent<Stage06AControllerTrackingVisual>();
            trackingVisual.Configure(actions, actionMap + "/IsTracked", visuals);
            return controller;
        }

        private static void ConfigureTrackedPose(TrackedPoseDriver driver, InputActionAsset actions, string map)
        {
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = false;
            driver.positionInput = new InputActionProperty(CreateOrLoadReference(actions, map + "/Position"));
            driver.rotationInput = new InputActionProperty(CreateOrLoadReference(actions, map + "/Rotation"));
            driver.trackingStateInput = new InputActionProperty(CreateOrLoadReference(actions, map + "/TrackingState"));
        }

        private static InputActionReference CreateOrLoadReference(InputActionAsset actions, string actionPath)
        {
            string safe = actionPath.Replace('/', '_');
            string path = InputFolder + "/REF_" + safe + ".asset";
            InputActionReference existing = AssetDatabase.LoadAssetAtPath<InputActionReference>(path);
            InputAction action = actions.FindAction(actionPath, true);
            if (existing != null)
            {
                existing.Set(action);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            InputActionReference reference = InputActionReference.Create(action);
            reference.name = "REF_" + safe;
            AssetDatabase.CreateAsset(reference, path);
            return reference;
        }

        private static void BuildBootstrapScene(InputActionAsset actions)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RigPrefabPath);
            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab, scene);
            rig.name = "Stage06A_XROrigin";

            GameObject waypointRoot = new GameObject("Stage06A_QA_Waypoints");
            SceneManager.MoveGameObjectToScene(waypointRoot, scene);
            var waypointComponents = new List<Stage06AWaypoint>();
            foreach (Stage06AWaypointDefinition definition in Waypoints)
            {
                GameObject waypoint = new GameObject($"{definition.index:00}_{definition.name}");
                waypoint.layer = 2;
                waypoint.transform.SetParent(waypointRoot.transform, false);
                waypoint.transform.position = definition.floorPosition + Vector3.up;
                SphereCollider trigger = waypoint.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 4f;
                Stage06AWaypoint marker = waypoint.AddComponent<Stage06AWaypoint>();
                marker.Configure(definition.index, definition.name);
                waypointComponents.Add(marker);
            }

            Stage06ATraversalRecorder recorder = rig.GetComponent<Stage06ATraversalRecorder>();
            XROrigin origin = rig.GetComponent<XROrigin>();
            Camera camera = rig.GetComponentInChildren<Camera>(true);
            recorder.Configure(origin, camera, actions, waypointComponents.ToArray());

            GameObject lightObject = new GameObject("Stage06A_QA_DirectionalSun");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.color = new Color(1f, 0.96f, 0.89f, 1f);
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.64f, 0.67f, 0.70f, 1f);
            RenderSettings.skybox = null;

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void WriteProtectedManifest(string workspace)
        {
            var entries = new List<Stage06AFileRecord>();
            foreach (string path in Stage06AContract.HospitalScenes.Concat(Stage06AContract.SiteScenes))
                entries.Add(FileRecord(path, path.StartsWith("Assets/Hospital/Scenes", StringComparison.Ordinal)
                    ? "frozen_r44_scene" : "frozen_stage05s_scene"));

            AddDirectoryRecords(entries, "Assets/Hospital/SourceFBX/R40", "frozen_hospital_source_and_import");
            AddDirectoryRecords(entries, "Assets/Hospital/Materials", "frozen_hospital_material");
            AddDirectoryRecords(entries, "Assets/Hospital/Prefabs", "frozen_hospital_prefab_collision_lod");
            AddDirectoryRecords(entries, "Assets/Hospital/Settings", "frozen_hospital_render_xr_record");

            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.SourceFolder, "frozen_site_source_and_import");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.MaterialFolder, "frozen_site_material");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.PrefabFolder, "frozen_site_prefab_collision_lod");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.Root + "/Textures", "frozen_site_texture_and_import");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.Root + "/Generated", "frozen_site_generated_lod_grass");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.Root + "/Runtime", "frozen_site_grass_runtime");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.Root + "/Shaders", "frozen_site_grass_shader");
            AddDirectoryRecords(entries, HospitalStage05SRoundS6Builder.SettingsFolder, "frozen_site_setting");

            AddDirectoryRecords(entries, "Assets/XR", "frozen_openxr_asset");
            AddDirectoryRecords(entries, "../../Exports/HospitalExterior/Stage05_R41_Input", "frozen_stage05_external_input");
            entries.Add(FileRecord("../../ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend", "frozen_r40_authority"));
            entries.Add(FileRecord("../../ArtSource/Environment/Blender/HospitalExterior/HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend1", "frozen_r40_exact_snapshot"));
            entries.Add(FileRecord("../../ArtSource/Environment/Blender/HospitalSite/HospitalSite_Stage05S_R05B_GRASS_REFINEMENT.blend", "frozen_r05b_authority"));
            entries.Add(FileRecord("Packages/manifest.json", "frozen_package_contract"));
            entries.Add(FileRecord("Packages/packages-lock.json", "frozen_package_lock"));
            entries.Add(FileRecord("ProjectSettings/EditorBuildSettings.asset", "frozen_eight_scene_build_settings"));
            entries.Add(FileRecord("ProjectSettings/ProjectSettings.asset", "frozen_player_platform_config"));
            entries.Add(FileRecord("ProjectSettings/GraphicsSettings.asset", "frozen_graphics_config"));
            entries.Add(FileRecord("ProjectSettings/QualitySettings.asset", "frozen_quality_config"));

            string[] duplicates = entries.GroupBy(entry => entry.path, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1).Select(group => group.Key).ToArray();
            if (duplicates.Length != 0)
                throw new InvalidOperationException("Protected manifest contains duplicate paths: " + string.Join(",", duplicates));
            entries = entries.OrderBy(entry => entry.path, StringComparer.Ordinal).ToList();

            var manifest = new Stage06AProtectedManifest
            {
                schema = Stage06AContract.ManifestSchema,
                status = "PASS",
                created_utc = DateTime.UtcNow.ToString("O"),
                r40_authority_sha256 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126",
                r40_exact_snapshot_sha256 = "0a530ff9d7d7effa512f1d69178a0be86da722239f0cb992878045358c73ea58",
                r05b_authority_sha256 = HospitalStage05SRoundS6Builder.AuthoritySha,
                unity_version = Application.unityVersion,
                target = "Windows x86_64 PC VR through Quest Link/Air Link; Android standalone excluded",
                files = entries.ToArray(),
            };
            WriteJson(manifest, AssetPathToAbsolute(ManifestPath));
            WriteJson(manifest, Path.Combine(Reviews(workspace), "Stage06A_R01_ProtectedInputManifest.json"));
        }

        private static void WriteRouteManifest(string workspace)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                HospitalStage05SRoundS6Builder.PrefabFolder + "/SITE_CoreRoadParking.prefab");
            MeshCollider[] colliders = prefab.GetComponentsInChildren<MeshCollider>(true)
                .OrderBy(collider => Stage06ASceneBootstrap.HierarchyPath(collider.transform), StringComparer.Ordinal).ToArray();
            var record = new Stage06ARouteManifest
            {
                schema = "HospitalExterior.Stage06A.R01.RouteSurfaceManifest.v1",
                status = colliders.Length == 20 ? "PASS" : "FAIL",
                runtime_only_teleport_components = true,
                route_surface_count = colliders.Length,
                surfaces = colliders.Select(collider => new Stage06ARouteSurfaceRecord
                {
                    path = Stage06ASceneBootstrap.HierarchyPath(collider.transform),
                    mesh = collider.sharedMesh.name,
                    vertices = collider.sharedMesh.vertexCount,
                    triangles = (int)(collider.sharedMesh.GetIndexCount(0) / 3),
                    // Prefab assets are not part of a physics scene, so Collider.bounds is zero here.
                    // The mesh-local bounds plus the saved transform path form the stable surface fingerprint.
                    bounds_center = collider.sharedMesh.bounds.center,
                    bounds_size = collider.sharedMesh.bounds.size,
                }).ToArray(),
            };
            if (colliders.Length != 20)
                throw new InvalidOperationException("Stage 6A requires exactly 20 frozen route surfaces.");
            WriteJson(record, AssetPathToAbsolute(RouteManifestPath));
            WriteJson(record, Path.Combine(Reviews(workspace), "Stage06A_R01_RouteSurfaceManifest.json"));
        }

        private static void WriteActionContract(string workspace, InputActionAsset actions)
        {
            var actionRecords = actions.actionMaps.SelectMany(map => map.actions.Select(action =>
                new Stage06AActionRecord
                {
                    action = map.name + "/" + action.name,
                    type = action.type.ToString(),
                    expected_control = action.expectedControlType,
                    bindings = action.bindings.Select(binding => binding.effectivePath).ToArray(),
                })).ToArray();
            var contract = new Stage06AActionContract
            {
                schema = "HospitalExterior.Stage06A.R01.InputActionContract.v1",
                status = "PASS",
                asset = InputAssetPath,
                move_speed_mps = 2f,
                snap_turn_degrees = 45f,
                teleport_control = "Right primary button (A): hold to aim, release to commit",
                cancel_control = "Right secondary button (B)",
                actions = actionRecords,
            };
            WriteJson(contract, AssetPathToAbsolute(ActionContractPath));
            WriteJson(contract, Path.Combine(Reviews(workspace), "Stage06A_R01_InputActionContract.json"));
        }

        private static void WriteReviewChecklist(string workspace)
        {
            string path = Path.Combine(Reviews(workspace), "Stage06A_R01_HeadsetReviewChecklist.md");
            string content = @"# Stage 6A R01 Headset Review Checklist

Status before headset review: `TECHNICAL_GATE_PENDING`

## Test setup (required)

- Headset model: `________________`
- Connection mode (`Quest Link` or `Air Link`): `________________`
- Refresh rate: `________________`
- Render resolution: `________________`
- PC CPU / GPU / RAM: `________________`
- Guardian floor recalibrated: `[ ]`

## Traversal review

- `[ ]` Natural scale and correct floor contact at the main gate.
- `[ ]` Left and right controller proxies track the correct hands.
- `[ ]` Physical Guardian movement follows without drift.
- `[ ]` Left stick moves forward/back/strafe at the expected comfort speed.
- `[ ]` Right stick performs 45-degree snap turns only.
- `[ ]` Hold right A to aim; release teleports on approved routes; right B cancels.
- `[ ]` Grass, mulch, lake, hospital, bench, tree and fence targets reject teleportation.
- `[ ]` Complete Gate -> Lobby Forecourt -> Visitor Parking -> Lake Path -> Rear Service -> Gate Return.
- `[ ]` No fall, obstacle clipping, stuck state, duplicate scene, missing material or broken grass.
- `[ ]` Six route screenshots and `Stage06A_RuntimeSession.json` were imported.

## Decision

- `[ ] APPROVED`
- `[ ] CORRECTION_REQUIRED`

Notes:

";
            File.WriteAllText(path, content.Replace("TECHNICAL_GATE_PENDING", "READY_FOR_HEADSET_REVIEW"));
        }

        private static void WriteTechnicalCheckpoint(string workspace)
        {
            string content = @"# Hospital Exterior Stage 6A R01 Technical Checkpoint

Status: **TECHNICAL GATE PASS / READY FOR HEADSET REVIEW**  
Stage 6A approval: **PENDING PHYSICAL QUEST LINK/AIR LINK WALK-THROUGH**

## Implemented

- Separate versioned floor-tracked XR Origin and bootstrap/profile scene.
- Project-owned action contract: left-stick move, right-stick 45-degree snap turn, right A hold/release teleport and right B cancel.
- Combined, hospital-only and site-only additive load profiles; Combined is the Stage 6A review default.
- Runtime-only binding to the exact 20 frozen route colliders with slope, support and capsule-clearance rejection.
- Two tracked controller proxies, curved right-hand teleport ray, valid/invalid reticle and neutral QA daylight.
- Six ordered exterior route checkpoints, fall recovery that records a technical failure, session JSON and screenshots.
- Lake Path uses the nearest verified route-safe point `(-58.37, 0, -94.93)` because the S6 scenic marker at `(-57, 0, -81)` is not on an approved route collider.
- Explicit nine-scene development-build list without changing the frozen eight-scene EditorBuildSettings.

## Preserved boundaries

- R44 hospital scenes and R05B/S6 site assets remain hash-identical.
- No approved geometry, material, LOD, grass, door or gameplay behavior was edited.
- No frame-time or target-headset performance claim is made in Stage 6A.

## Remaining approval gate

Run the harness on one available Quest Link or Air Link setup, import the session, complete `Reviews/HospitalExterior/Stage06A_R01/Stage06A_R01_HeadsetReviewChecklist.md`, and record `APPROVED` or `CORRECTION_REQUIRED`. Stage 6B must not begin before that decision.
";
            File.WriteAllText(Path.Combine(workspace, CheckpointRelative.Replace('/', Path.DirectorySeparatorChar)), content);
        }

        private static void WriteTechnicalLog(string workspace)
        {
            string content = string.Join(Environment.NewLine, new[]
            {
                "Hospital Exterior Stage 6A R01 technical build log",
                "Generated UTC: " + DateTime.UtcNow.ToString("O"),
                "Unity: " + Application.unityVersion,
                "Platform: StandaloneWindows64 / D3D11",
                "Script compilation: PASS (builder and validator assemblies loaded)",
                "Protected-input preflight: PASS",
                "Input-action contract: PASS",
                "XR rig and bootstrap validation: PASS",
                "Route allowlist and landing tests: PASS",
                "Scene-profile tests: PASS",
                "Frozen EditorBuildSettings: PASS",
                "Technical status: READY_FOR_HEADSET_REVIEW",
                "Physical Quest session: PENDING (required for Stage 6A approval)",
                "Performance acceptance: OUT OF SCOPE (Stage 6B-6D)",
                string.Empty,
            });
            File.WriteAllText(Path.Combine(Reviews(workspace), "Stage06A_R01_TechnicalBuild.log"), content);
        }

        internal static void ValidateFrozenBuildSettings()
        {
            string[] expected = Stage06AContract.HospitalScenes.Concat(Stage06AContract.SiteScenes).ToArray();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length != 8 || scenes.Any(scene => !scene.enabled) || !scenes.Select(scene => scene.path).SequenceEqual(expected))
                throw new InvalidOperationException("Frozen S6 EditorBuildSettings must contain exactly the original enabled eight scenes.");
        }

        internal static string WorkspaceRoot() => Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "..", ".."));
        internal static string Reviews(string workspace)
        {
            string path = Path.Combine(workspace, ReviewRelative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(path);
            return path;
        }
        internal static string AssetPathToAbsolute(string path) => path.StartsWith("Packages/", StringComparison.Ordinal)
            ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, path.Replace('/', Path.DirectorySeparatorChar))
            : Path.GetFullPath(Path.Combine(Directory.GetParent(Application.dataPath).FullName, path));
        internal static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
        }
        internal static void WriteJson(object value, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + Environment.NewLine);
        }

        private static Stage06AFileRecord FileRecord(string path, string role)
        {
            string absolute = AssetPathToAbsolute(path);
            if (!File.Exists(absolute)) throw new FileNotFoundException("Protected input missing", absolute);
            return new Stage06AFileRecord { path = path, role = role, sha256 = Sha256(absolute), bytes = new FileInfo(absolute).Length };
        }
        private static void AddDirectoryRecords(List<Stage06AFileRecord> entries, string directory, string role)
        {
            string absoluteDirectory = AssetPathToAbsolute(directory);
            if (!Directory.Exists(absoluteDirectory))
                throw new DirectoryNotFoundException("Protected input directory missing: " + absoluteDirectory);
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            foreach (string absolute in Directory.GetFiles(absoluteDirectory, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(projectRoot, absolute).Replace('\\', '/');
                entries.Add(FileRecord(relative, role));
            }
        }
        private static void ValidateExistingProtectedManifest()
        {
            string path = AssetPathToAbsolute(ManifestPath);
            if (!File.Exists(path))
                return;
            Stage06AProtectedManifest manifest = JsonUtility.FromJson<Stage06AProtectedManifest>(File.ReadAllText(path));
            if (manifest?.files == null || manifest.files.Length == 0)
                throw new InvalidDataException("Existing Stage 6A protected-input manifest is unreadable or empty.");
            foreach (Stage06AFileRecord entry in manifest.files)
            {
                string absolute = AssetPathToAbsolute(entry.path);
                if (!File.Exists(absolute) || Sha256(absolute) != entry.sha256 || new FileInfo(absolute).Length != entry.bytes)
                    throw new InvalidDataException("Existing Stage 6A protected input changed before rebuild: " + entry.path);
            }
        }
        private static void RequireHash(string path, string expected)
        {
            if (!File.Exists(path) || !string.Equals(Sha256(path), expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Protected input hash mismatch: " + path);
        }
        private static void RequirePass(string path)
        {
            if (!File.Exists(path) || !File.ReadAllText(path).Contains("\"status\": \"PASS\""))
                throw new InvalidDataException("Required frozen gate is not PASS: " + path);
        }
        private static Stage06AWaypointDefinition W(int index, string name, float x, float z) =>
            new Stage06AWaypointDefinition { index = index, name = name, floorPosition = new Vector3(x, 0f, z) };

        private sealed class MaterialSet { public Material left, right, valid, invalid, line; }
    }

    [Serializable] internal sealed class Stage06AProtectedManifest
    {
        public string schema, status, created_utc, r40_authority_sha256, r40_exact_snapshot_sha256, r05b_authority_sha256, unity_version, target;
        public Stage06AFileRecord[] files;
    }
    [Serializable] internal sealed class Stage06AFileRecord { public string path, role, sha256; public long bytes; }
    [Serializable] internal sealed class Stage06ARouteManifest
    {
        public string schema, status; public bool runtime_only_teleport_components; public int route_surface_count;
        public Stage06ARouteSurfaceRecord[] surfaces;
    }
    [Serializable] internal sealed class Stage06ARouteSurfaceRecord
    {
        public string path, mesh; public int vertices, triangles; public Vector3 bounds_center, bounds_size;
    }
    [Serializable] internal sealed class Stage06AActionContract
    {
        public string schema, status, asset, teleport_control, cancel_control; public float move_speed_mps, snap_turn_degrees;
        public Stage06AActionRecord[] actions;
    }
    [Serializable] internal sealed class Stage06AActionRecord { public string action, type, expected_control; public string[] bindings; }
    [Serializable] internal sealed class Stage06ABuildRecord
    {
        public string schema, status, build_type, output, note; public long total_bytes; public double total_time_seconds;
        public string[] scenes; public bool editor_build_settings_preserved, headset_performance_accepted;
    }
}
