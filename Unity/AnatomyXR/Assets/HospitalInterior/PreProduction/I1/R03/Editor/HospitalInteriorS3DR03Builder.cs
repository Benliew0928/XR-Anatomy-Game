using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CutMyBodyPlease.Hospital.Stage06A;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3DR03Builder
    {
        public const string Root = HospitalInteriorS3CR03Builder.Root;
        public const string S3DRoot = Root + "/S3D";
        public const string InputFolder = S3DRoot + "/Input";
        public const string MaterialFolder = S3DRoot + "/Materials";
        public const string SettingsFolder = S3DRoot + "/Settings";
        public const string LandscapeHardscapePrefabPath =
            "Assets/HospitalSite/Stage05S/R05B/Prefabs/Zones/SITE_LandscapeHardscape.prefab";
        public const string SourceS3CScenePath = HospitalInteriorS3CR03Builder.BootstrapScenePath;
        public const string BootstrapScenePath = Root
            + "/Scenes/HospitalInterior_S3D_R03_FullTechnicalIntegration.unity";
        public const string InputAssetPath = InputFolder
            + "/HospitalInterior_S3D_R03_Actions.inputactions";
        public const string ProtectedManifestPath = SettingsFolder
            + "/HospitalInterior_S3D_R03_ProtectedAuthority.json";
        public const string StaticGateAssetPath = SettingsFolder
            + "/HospitalInterior_S3D_R03_StaticGate.json";
        public const string ExpectedBuildSettingsSha256 =
            "62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb";
        private const float LocomotionSpeedMultiplier = 3f;

        public static readonly string[] PlayerScenes = new[] { BootstrapScenePath }
            .Concat(HospitalInteriorS2R03Builder.ProductionScenes)
            .Concat(S3DIntegrationContract.SiteScenePaths)
            .Concat(HospitalInteriorS2R03Builder.FloorScenePaths)
            .ToArray();

        private static readonly string[] ProtectedS3CPaths =
        {
            SourceS3CScenePath,
            SourceS3CScenePath + ".meta",
            HospitalInteriorS3CR03Builder.ExteriorBaseShaftCutMeshPath,
            HospitalInteriorS3CR03Builder.ExteriorBaseShaftCutMeshPath + ".meta",
            Root + "/Runtime/S3ElevatorController.cs",
            Root + "/Runtime/S3ElevatorController.cs.meta",
            Root + "/Runtime/S3ElevatorButton.cs",
            Root + "/Runtime/S3ElevatorButton.cs.meta",
            Root + "/Runtime/S3CReviewBootstrap.cs",
            Root + "/Runtime/S3CReviewBootstrap.cs.meta",
            Root + "/Runtime/S3CReviewLocomotion.cs",
            Root + "/Runtime/S3CReviewLocomotion.cs.meta",
            "../../Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel/StageI1_R03_S3C_StaticGate.json",
            "../../Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel/StageI1_R03_S3C_RuntimeGate.json",
            "../../Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel/StageI1_R03_S3C_BuildRecord.json",
            "../../Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel/HospitalInterior_StageS3C_R03_Checkpoint.md",
            "../../Reviews/HospitalInterior/StageI1_R03/S3C_E01_Travel/StageI1_R03_S3C_UserApproval.md",
        };

        [Serializable]
        private sealed class GateStatus { public string status; }

        [Serializable]
        private sealed class ProtectedManifest
        {
            public string schema = "HospitalInterior.R03.S3D.ProtectedAuthority.v1";
            public string status = "PASS";
            public string createdUtc;
            public string note;
            public ProtectedFile[] files;
        }

        [Serializable]
        private sealed class ProtectedFile
        {
            public string path;
            public string sha256;
            public long bytes;
        }

        [Serializable]
        private sealed class Stage06Manifest
        {
            public string status;
            public Stage06File[] files;
        }

        [Serializable]
        private sealed class Stage06File
        {
            public string path;
            public string sha256;
            public long bytes;
        }

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = S3DIntegrationContract.BuildSchema;
            public string status;
            public string unityVersion;
            public string sourceS3CScene;
            public string bootstrapScene;
            public string[] playerScenes;
            public string playerExecutable;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public int protectedS2FileCount;
            public int protectedS3BFileCount;
            public int protectedS3CFileCount;
            public int stage06ProtectedExactCount;
            public int stage06KnownSemanticNormalizationCount;
            public string target;
            public string route;
            public string note;
        }

        [MenuItem("Hospital Interior/R03 S3D/Build full technical integration", priority = 1)]
        public static void BuildStage()
        {
            RequireInputs();
            EnsureFolders();
            EnsureProtectedS3CManifest();
            AssertProtectedAuthorities(out int s2Count, out int s3bCount,
                out int s3cCount, out int stage06Exact, out int stage06Semantic);
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings",
                "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);

            InputActionAsset actions = BuildInputActions();
            MaterialSet materials = BuildMaterials();
            BuildIntegrationScene(actions, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            AssertProtectedAuthorities(out int s2After, out int s3bAfter,
                out int s3cAfter, out int stage06ExactAfter, out int stage06SemanticAfter);
            string settingsAfter = Sha256(settingsPath);
            if (s2After != s2Count || s3bAfter != s3bCount || s3cAfter != s3cCount
                || stage06ExactAfter != stage06Exact || stage06SemanticAfter != stage06Semantic)
                throw new InvalidOperationException("Protected authority counts changed during S3D build.");
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(settingsAfter, ExpectedBuildSettingsSha256,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("EditorBuildSettings changed during S3D build.");

            WriteBuildRecord("SCENE_BUILT_PENDING_AUTOMATED_GATES", string.Empty,
                settingsAfter, s2Count, s3bCount, s3cCount, stage06Exact, stage06Semantic);
            WriteCheckpoint("IMPLEMENTED_PENDING_AUTOMATED_AND_USER_REVIEW");
            Debug.Log("HOSPITAL_INTERIOR_R03_S3D_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildStageBatch()
        {
            try { BuildStage(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S3D/Build adaptive Windows review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildStage();
            HospitalInteriorS3DR03Validator.ValidateStaticGate();
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings",
                "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);
            Directory.CreateDirectory(ExportFolder());
            string executable = PlayerExecutable();
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
            });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S3D Windows player build failed: "
                    + report.summary.result);
            string settingsAfter = Sha256(settingsPath);
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(settingsAfter, ExpectedBuildSettingsSha256,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("EditorBuildSettings changed during S3D player build.");
            AssertProtectedAuthorities(out int s2Count, out int s3bCount,
                out int s3cCount, out int stage06Exact, out int stage06Semantic);
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", executable, settingsAfter,
                s2Count, s3bCount, s3cCount, stage06Exact, stage06Semantic);
            Debug.Log("HOSPITAL_INTERIOR_R03_S3D_PLAYER_BUILD=PASS; " + executable);
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
                string staticPath = Path.Combine(ReviewFolder(),
                    "StageI1_R03_S3D_StaticGate.json");
                string runtimePath = Path.Combine(ReviewFolder(),
                    "StageI1_R03_S3D_RuntimeGate.json");
                GateStatus staticGate = File.Exists(staticPath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(staticPath)) : null;
                GateStatus runtimeGate = File.Exists(runtimePath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(runtimePath)) : null;
                if (staticGate?.status != "PASS" || runtimeGate?.status != "PASS")
                    throw new InvalidOperationException("S3D automated reports are not both PASS.");
                if (!File.Exists(PlayerExecutable()))
                    throw new FileNotFoundException("S3D review player is missing.", PlayerExecutable());
                AssertEvidencePackage();
                AssertProtectedAuthorities(out int s2Count, out int s3bCount,
                    out int s3cCount, out int stage06Exact, out int stage06Semantic);
                string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings",
                    "EditorBuildSettings.asset");
                WriteBuildRecord("AUTOMATED_GATES_PASS_PENDING_DESKTOP_AND_PCVR_USER_REVIEW",
                    PlayerExecutable(), Sha256(settingsPath), s2Count, s3bCount, s3cCount,
                    stage06Exact, stage06Semantic);
                WriteCheckpoint("AUTOMATED_GATES_PASS_PENDING_DESKTOP_AND_PCVR_USER_REVIEW");
                PackageReviewArtifacts();
                Debug.Log("HOSPITAL_INTERIOR_R03_S3D_FINALIZE=PASS; user desktop/PCVR review pending");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildIntegrationScene(InputActionAsset actions, MaterialSet materials)
        {
            Scene source = EditorSceneManager.OpenScene(SourceS3CScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, BootstrapScenePath, true))
                throw new InvalidOperationException("Could not duplicate approved S3C scene for S3D.");
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);

            S3CReviewBootstrap s3c = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<S3CReviewBootstrap>(true)).Single();
            S3ElevatorContract contract = s3c.ElevatorContract;
            float walkSpeedMps = contract.ApprovedS2FloorContract.WalkSpeedMps
                * LocomotionSpeedMultiplier;
            float sprintSpeedMps = contract.ApprovedS2FloorContract.SprintSpeedMps
                * LocomotionSpeedMultiplier;
            S3ElevatorController elevator = s3c.ElevatorController;
            XROrigin origin = s3c.XrOrigin;
            Camera camera = s3c.ReviewCamera;
            S3ShaftStructure shaft = s3c.ShaftStructure;
            S3ShaftFloorAperture aperture = s3c.FloorAperture;
            S3CabinObservationDesign observation = s3c.CabinObservationDesign;
            Mesh exteriorCut = s3c.ExteriorBaseShaftCutMesh;
            GameObject root = scene.GetRootGameObjects().Single(item
                => item.name == "HospitalInterior_S3C_R03_BootstrapRoot");

            Object.DestroyImmediate(s3c.Locomotion);
            Object.DestroyImmediate(s3c);
            root.name = "HospitalInterior_S3D_R03_BootstrapRoot";
            origin.gameObject.name = "S3D_R03_AdaptiveXROrigin";
            origin.transform.SetPositionAndRotation(S3DIntegrationContract.GateSpawn,
                Quaternion.identity);
            CharacterController character = origin.GetComponent<CharacterController>();
            camera.gameObject.name = "S3D_R03_IntegrationCamera";
            camera.gameObject.tag = "MainCamera";
            camera.farClipPlane = 600f;
            camera.stereoTargetEye = StereoTargetEyeMask.Both;
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData == null)
                cameraData = camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = false;

            TrackedPoseDriver headDriver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            ConfigureTrackedPose(headDriver, actions, "Head");
            Transform offset = origin.CameraFloorOffsetObject.transform;
            Transform leftController = CreateController(offset, "S3D Left Controller", actions,
                "LeftController", materials.left, out TrackedPoseDriver leftDriver,
                out GameObject leftVisual);
            Transform rightController = CreateController(offset, "S3D Right Controller", actions,
                "RightController", materials.right, out TrackedPoseDriver rightDriver,
                out GameObject rightVisual);

            LineRenderer teleportLine = CreateLine(rightController, "S3D Teleport Arc",
                materials.line, 0.018f);
            GameObject reticle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            reticle.name = "S3D Teleport Landing Reticle";
            reticle.transform.SetParent(root.transform, false);
            reticle.transform.localScale = new Vector3(0.22f, 0.008f, 0.22f);
            Object.DestroyImmediate(reticle.GetComponent<Collider>());
            Renderer reticleRenderer = reticle.GetComponent<Renderer>();
            reticleRenderer.sharedMaterial = materials.invalid;
            reticle.SetActive(false);
            LineRenderer interactionLine = CreateLine(rightController,
                "S3D Elevator Interaction Ray", materials.interaction, 0.012f);

            XRBodyTransformer bodyTransformer = origin.gameObject.AddComponent<XRBodyTransformer>();
            bodyTransformer.xrOrigin = origin;
            bodyTransformer.useCharacterControllerIfExists = true;
            LocomotionMediator mediator = origin.gameObject.AddComponent<LocomotionMediator>();
            GravityProvider gravity = origin.gameObject.AddComponent<GravityProvider>();
            gravity.mediator = mediator;
            gravity.useGravity = true;
            gravity.useLocalSpaceGravity = true;
            gravity.updateCharacterControllerCenterEachFrame = false;
            TeleportationProvider teleportProvider = origin.gameObject.AddComponent<TeleportationProvider>();
            teleportProvider.mediator = mediator;
            teleportProvider.delayTime = 0f;
            Stage06ABodyCapsuleDriver bodyDriver = origin.gameObject.AddComponent<Stage06ABodyCapsuleDriver>();
            bodyDriver.Configure(origin, camera, character);
            Stage06AActionLocomotionProvider pcvrLocomotion =
                origin.gameObject.AddComponent<Stage06AActionLocomotionProvider>();
            pcvrLocomotion.mediator = mediator;
            pcvrLocomotion.Configure(actions, camera.transform, walkSpeedMps, 45f);
            Stage06ASafeTeleportController teleporter =
                origin.gameObject.AddComponent<Stage06ASafeTeleportController>();
            teleporter.Configure(actions, rightController, camera, character, bodyDriver,
                teleportProvider, teleportLine, reticle, reticleRenderer,
                materials.valid, materials.invalid, null);
            InputActionManager inputManager = origin.gameObject.AddComponent<InputActionManager>();
            inputManager.actionAssets = new List<InputActionAsset> { actions };
            origin.gameObject.AddComponent<XRInteractionManager>();

            S3DAdaptiveRig adaptive = origin.gameObject.AddComponent<S3DAdaptiveRig>();
            adaptive.Configure(S3DIntegrationMode.Auto, actions, origin, camera, character,
                rightController, new[] { headDriver, leftDriver, rightDriver },
                new Behaviour[] { gravity, pcvrLocomotion, teleporter },
                new[] { leftVisual, rightVisual }, interactionLine,
                contract.ApprovedS2FloorContract.EyeHeightM,
                walkSpeedMps, sprintSpeedMps);

            S3DEntranceDoorController entrance = root.AddComponent<S3DEntranceDoorController>();
            entrance.Configure(origin.transform);
            S3DTeleportSurfaceCoordinator teleportCoordinator =
                root.AddComponent<S3DTeleportSurfaceCoordinator>();
            teleportCoordinator.Configure(teleporter, elevator, contract, adaptive);
            S3DRecoveryCoordinator recovery = origin.gameObject.AddComponent<S3DRecoveryCoordinator>();
            recovery.Configure(adaptive, elevator, contract);
            S3DSiteCollisionCoordinator siteCollision =
                root.AddComponent<S3DSiteCollisionCoordinator>();
            MeshCollider bakedLawnCollider = CreateBakedLawnCollider(root.transform,
                out string lawnRendererName, out string lawnMeshName,
                out int lawnTriangleCount);
            siteCollision.ConfigureBakedLawnSupport(bakedLawnCollider,
                lawnRendererName, lawnMeshName, lawnTriangleCount);
            S3DFacadeFloorEdgeCoordinator facadeFloorEdges =
                CreateFacadeFloorEdgeFinishes(root.transform, contract, materials.floor);
            S3DIntegrationBootstrap bootstrap = root.AddComponent<S3DIntegrationBootstrap>();
            bootstrap.Configure(contract, elevator, origin, camera, shaft, aperture, observation,
                exteriorCut, adaptive, entrance, teleportCoordinator, recovery, siteCollision,
                facadeFloorEdges);
            adaptive.ConfigureBootstrap(bootstrap);

            Light directional = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Light>(true))
                .Single(item => item.type == LightType.Directional);
            directional.gameObject.name = "S3D_R03_IntegrationSun";
            directional.intensity = 1.15f;
            directional.color = new Color(0.96f, 0.95f, 0.92f, 1f);
            directional.shadows = LightShadows.Soft;

            elevator.Configure(elevator.Contract, elevator.CabinRoot, origin.transform,
                elevator.CurrentDoorController.ObstructionSensor.transform,
                scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                    .Single(item => item.name == "S3B_R03_REVIEW_ThresholdObstructionProxy").gameObject,
                elevator.FloorAperture, elevator.LandingPortalRoots,
                elevator.FloorDoorControllers, elevator.CabinTravelIndicator);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static MeshCollider CreateBakedLawnCollider(Transform integrationRoot,
            out string rendererName, out string meshName, out int triangleCount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                LandscapeHardscapePrefabPath);
            if (prefab == null)
                throw new FileNotFoundException("Approved landscape-hardscape prefab is missing.",
                    LandscapeHardscapePrefabPath);
            MeshRenderer renderer = prefab.GetComponentsInChildren<MeshRenderer>(true)
                .Single(item => item.sharedMaterials.Any(material => material != null
                    && string.Equals(material.name, "MAT_S05S_S5B_GrassLawn",
                        StringComparison.Ordinal)));
            Mesh sourceMesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (sourceMesh == null)
                throw new InvalidOperationException("Continuous lawn source mesh is missing.");

            var support = new GameObject(S3DSiteCollisionCoordinator.GrassSupportName);
            support.transform.SetParent(integrationRoot, false);
            support.transform.localPosition = renderer.transform.position;
            support.transform.localRotation = renderer.transform.rotation;
            support.transform.localScale = renderer.transform.lossyScale;
            MeshCollider collider = support.AddComponent<MeshCollider>();
            collider.sharedMesh = sourceMesh;
            collider.convex = false;

            rendererName = renderer.name;
            meshName = sourceMesh.name;
            long indexCount = 0;
            for (int subMesh = 0; subMesh < sourceMesh.subMeshCount; subMesh++)
                indexCount += (long)sourceMesh.GetIndexCount(subMesh);
            triangleCount = checked((int)(indexCount / 3L));
            if (triangleCount <= 0)
                throw new InvalidOperationException("Continuous lawn source mesh has no triangles.");
            return collider;
        }

        private static S3DFacadeFloorEdgeCoordinator CreateFacadeFloorEdgeFinishes(
            Transform integrationRoot, S3ElevatorContract contract, Material floorMaterial)
        {
            if (contract?.ApprovedS2FloorContract == null || floorMaterial == null)
                throw new InvalidOperationException("Approved floor authority is unavailable for the S3D facade edge finish.");

            var root = new GameObject("S3D_R03_MainGlassFloorEdgeFinishRoot");
            root.transform.SetParent(integrationRoot, false);
            S3DFacadeFloorEdgeCoordinator coordinator =
                root.AddComponent<S3DFacadeFloorEdgeCoordinator>();
            var renderers = new List<Renderer>();
            var colliders = new List<BoxCollider>();
            float width = S3DFacadeFloorEdgeCoordinator.MaxX
                - S3DFacadeFloorEdgeCoordinator.MinX;
            float depth = S3DFacadeFloorEdgeCoordinator.FloorJoinZ
                - S3DFacadeFloorEdgeCoordinator.FacadeEdgeZ;
            float centerX = (S3DFacadeFloorEdgeCoordinator.MinX
                + S3DFacadeFloorEdgeCoordinator.MaxX) * 0.5f;
            float centerZ = (S3DFacadeFloorEdgeCoordinator.FacadeEdgeZ
                + S3DFacadeFloorEdgeCoordinator.FloorJoinZ) * 0.5f;

            foreach (S2FloorRecord floor in contract.ApprovedS2FloorContract.Floors.Skip(1))
            {
                GameObject finish = GameObject.CreatePrimitive(PrimitiveType.Cube);
                finish.name = S3DFacadeFloorEdgeCoordinator.FinishPrefix + floor.floorId;
                finish.transform.SetParent(root.transform, false);
                finish.transform.localPosition = new Vector3(centerX,
                    floor.elevationM - S3DFacadeFloorEdgeCoordinator.DeckThicknessM * 0.5f,
                    centerZ);
                finish.transform.localScale = new Vector3(width,
                    S3DFacadeFloorEdgeCoordinator.DeckThicknessM, depth);
                Renderer renderer = finish.GetComponent<Renderer>();
                renderer.sharedMaterial = floorMaterial;
                renderers.Add(renderer);
                BoxCollider collider = finish.GetComponent<BoxCollider>();
                collider.isTrigger = false;
                colliders.Add(collider);
            }
            coordinator.Configure(floorMaterial, renderers.ToArray(), colliders.ToArray());
            return coordinator;
        }

        private static InputActionAsset BuildInputActions()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:InputActionReference",
                new[] { InputFolder }))
                AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
            if (AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath) != null)
                AssetDatabase.DeleteAsset(InputAssetPath);

            var generated = ScriptableObject.CreateInstance<InputActionAsset>();
            generated.name = "HospitalInterior_S3D_R03_Actions";
            InputActionMap head = generated.AddActionMap("Head");
            AddPoseActions(head, "<XRHMD>", true);
            InputActionMap left = generated.AddActionMap("LeftController");
            AddPoseActions(left, "<XRController>{LeftHand}", false);
            InputActionMap right = generated.AddActionMap("RightController");
            AddPoseActions(right, "<XRController>{RightHand}", false);
            InputActionMap locomotion = generated.AddActionMap("Locomotion");
            AddAction(locomotion, "Move", InputActionType.Value, "Vector2")
                .AddBinding("<XRController>{LeftHand}/primary2DAxis").WithProcessor("StickDeadzone");
            AddAction(locomotion, "SnapTurn", InputActionType.Value, "Vector2")
                .AddBinding("<XRController>{RightHand}/primary2DAxis").WithProcessor("StickDeadzone");
            AddAction(locomotion, "TeleportAim", InputActionType.Button, "Button")
                .AddBinding("<XRController>{RightHand}/primaryButton");
            AddAction(locomotion, "TeleportCancel", InputActionType.Button, "Button")
                .AddBinding("<XRController>{RightHand}/secondaryButton");
            InputActionMap interaction = generated.AddActionMap("Interaction");
            AddAction(interaction, "Activate", InputActionType.Button, "Button")
                .AddBinding("<XRController>{RightHand}/triggerPressed");

            File.WriteAllText(AssetPathToAbsolute(InputAssetPath), generated.ToJson()
                + Environment.NewLine);
            Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(InputAssetPath, ImportAssetOptions.ForceSynchronousImport);
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (asset == null)
                throw new InvalidOperationException("Failed to create S3D input actions.");
            return asset;
        }

        private static void AddPoseActions(InputActionMap map, string device, bool head)
        {
            AddAction(map, "Position", InputActionType.Value, "Vector3")
                .AddBinding(head ? device + "/centerEyePosition" : device + "/devicePosition");
            AddAction(map, "Rotation", InputActionType.Value, "Quaternion")
                .AddBinding(head ? device + "/centerEyeRotation" : device + "/deviceRotation");
            AddAction(map, "TrackingState", InputActionType.Value, "Integer")
                .AddBinding(device + "/trackingState");
            AddAction(map, "IsTracked", InputActionType.Button, "Button")
                .AddBinding(device + "/isTracked");
        }

        private static InputAction AddAction(InputActionMap map, string name,
            InputActionType type, string expectedControlType)
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
            Material floor = AssetDatabase.LoadAssetAtPath<Material>(
                HospitalInteriorS2R03Builder.SlabMaterialPath);
            if (floor == null)
                throw new InvalidOperationException("Approved S2 empty-floor material is unavailable.");
            return new MaterialSet
            {
                floor = floor,
                left = CreateMaterial("MAT_S3D_ControllerLeft", lit,
                    new Color(0.10f, 0.32f, 0.82f, 1f)),
                right = CreateMaterial("MAT_S3D_ControllerRight", lit,
                    new Color(0.82f, 0.27f, 0.10f, 1f)),
                valid = CreateMaterial("MAT_S3D_TeleportValid", unlit,
                    new Color(0.12f, 1f, 0.32f, 1f)),
                invalid = CreateMaterial("MAT_S3D_TeleportInvalid", unlit,
                    new Color(1f, 0.12f, 0.08f, 1f)),
                line = CreateMaterial("MAT_S3D_TeleportLine", unlit, Color.white),
                interaction = CreateMaterial("MAT_S3D_InteractionRay", unlit,
                    new Color(0.20f, 0.80f, 1f, 1f)),
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

        private static Transform CreateController(Transform parent, string name,
            InputActionAsset actions, string map, Material material,
            out TrackedPoseDriver driver, out GameObject visualRoot)
        {
            GameObject controller = new GameObject(name);
            controller.transform.SetParent(parent, false);
            driver = controller.AddComponent<TrackedPoseDriver>();
            ConfigureTrackedPose(driver, actions, map);
            visualRoot = new GameObject(name + " Visuals");
            visualRoot.transform.SetParent(controller.transform, false);
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Grip";
            grip.transform.SetParent(visualRoot.transform, false);
            grip.transform.localPosition = new Vector3(0f, -0.035f, 0.035f);
            grip.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.055f, 0.12f, 0.06f);
            Object.DestroyImmediate(grip.GetComponent<Collider>());
            grip.GetComponent<Renderer>().sharedMaterial = material;
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nose.name = "Aim Housing";
            nose.transform.SetParent(visualRoot.transform, false);
            nose.transform.localPosition = new Vector3(0f, 0.025f, 0.075f);
            nose.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            nose.transform.localScale = new Vector3(0.04f, 0.025f, 0.04f);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.GetComponent<Renderer>().sharedMaterial = material;
            return controller.transform;
        }

        private static LineRenderer CreateLine(Transform parent, string name,
            Material material, float width)
        {
            GameObject lineObject = new GameObject(name);
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = width;
            line.numCornerVertices = 3;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        private static void ConfigureTrackedPose(TrackedPoseDriver driver,
            InputActionAsset actions, string map)
        {
            driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            driver.ignoreTrackingState = false;
            // Input System 1.20 serializes these asset-owned actions directly in the
            // InputActionProperty. InputActionManager enables the shared asset at runtime.
            driver.positionInput = new InputActionProperty(actions.FindAction(
                map + "/Position", true));
            driver.rotationInput = new InputActionProperty(actions.FindAction(
                map + "/Rotation", true));
            driver.trackingStateInput = new InputActionProperty(actions.FindAction(
                map + "/TrackingState", true));
        }

        private static void RequireInputs()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S3D requires Unity 6000.3.20f1; found "
                    + Application.unityVersion + ".");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("S3D requires StandaloneWindows64 active build target.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceS3CScenePath) == null)
                throw new FileNotFoundException("Approved S3C scene is missing.", SourceS3CScenePath);
            foreach (string path in PlayerScenes.Skip(1))
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new FileNotFoundException("S3D dependency scene is missing.", path);

            string s3cReview = Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S3C_E01_Travel");
            RequireGatePass(Path.Combine(s3cReview, "StageI1_R03_S3C_StaticGate.json"));
            RequireGatePass(Path.Combine(s3cReview, "StageI1_R03_S3C_RuntimeGate.json"));
            string approval = Path.Combine(s3cReview, "StageI1_R03_S3C_UserApproval.md");
            if (!File.Exists(approval) || !File.ReadAllText(approval).Contains("APPROVE S3C"))
                throw new InvalidOperationException("Explicit S3C user approval is missing.");
        }

        private static void RequireGatePass(string path)
        {
            GateStatus gate = File.Exists(path)
                ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(path)) : null;
            if (gate?.status != "PASS")
                throw new InvalidOperationException("Required PASS gate is missing: " + path);
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder(Root, "S3D");
            EnsureAssetFolder(S3DRoot, "Input");
            EnsureAssetFolder(S3DRoot, "Materials");
            EnsureAssetFolder(S3DRoot, "Settings");
        }

        private static void EnsureAssetFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void EnsureProtectedS3CManifest()
        {
            string absolute = AssetPathToAbsolute(ProtectedManifestPath);
            if (File.Exists(absolute))
                return;
            var records = new List<ProtectedFile>();
            foreach (string path in ProtectedS3CPaths)
            {
                string full = ResolveProjectRelative(path);
                if (!File.Exists(full))
                    throw new FileNotFoundException("S3C protected authority file is missing.", full);
                records.Add(new ProtectedFile
                {
                    path = path,
                    sha256 = Sha256(full),
                    bytes = new FileInfo(full).Length,
                });
            }
            var manifest = new ProtectedManifest
            {
                createdUtc = DateTime.UtcNow.ToString("O"),
                note = "Captured only after explicit S3C user approval. S3D may reference but never edit these files.",
                files = records.ToArray(),
            };
            File.WriteAllText(absolute, JsonUtility.ToJson(manifest, true));
            AssetDatabase.ImportAsset(ProtectedManifestPath,
                ImportAssetOptions.ForceSynchronousImport);
        }

        public static void AssertProtectedAuthorities(out int s2Count, out int s3bCount,
            out int s3cCount, out int stage06ExactCount, out int stage06SemanticCount)
        {
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out s2Count);
            HospitalInteriorS3CR03Builder.AssertLockedS3BInputs(out s3bCount);
            AssertProtectedS3C(out s3cCount);
            AssertStage06ProtectedManifest(out stage06ExactCount, out stage06SemanticCount);
            string buildSettings = Path.Combine(ProjectRoot(), "ProjectSettings",
                "EditorBuildSettings.asset");
            if (!string.Equals(Sha256(buildSettings), ExpectedBuildSettingsSha256,
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Frozen EditorBuildSettings hash changed.");
        }

        private static void AssertProtectedS3C(out int count)
        {
            string path = AssetPathToAbsolute(ProtectedManifestPath);
            ProtectedManifest manifest = File.Exists(path)
                ? JsonUtility.FromJson<ProtectedManifest>(File.ReadAllText(path)) : null;
            if (manifest?.status != "PASS" || manifest.files == null)
                throw new InvalidOperationException("S3D protected S3C manifest is missing or invalid.");
            var mismatches = new List<string>();
            foreach (ProtectedFile file in manifest.files)
            {
                string full = ResolveProjectRelative(file.path);
                if (!File.Exists(full))
                    mismatches.Add(file.path + ":missing");
                else if (!string.Equals(Sha256(full), file.sha256,
                    StringComparison.OrdinalIgnoreCase))
                    mismatches.Add(file.path + ":hash");
            }
            if (mismatches.Count > 0)
                throw new InvalidOperationException("Approved S3C authority changed: "
                    + string.Join(",", mismatches));
            count = manifest.files.Length;
        }

        private static void AssertStage06ProtectedManifest(out int exactCount,
            out int semanticNormalizationCount)
        {
            string path = Path.Combine(ProjectRoot(), "Assets", "Hospital", "Stage06A", "R01",
                "Settings", "Stage06A_R01_ProtectedInputManifest.json");
            Stage06Manifest manifest = File.Exists(path)
                ? JsonUtility.FromJson<Stage06Manifest>(File.ReadAllText(path)) : null;
            if (manifest?.status != "PASS" || manifest.files == null
                || manifest.files.Length != 295)
                throw new InvalidOperationException("Stage 6A protected input manifest is invalid.");
            exactCount = 0;
            semanticNormalizationCount = 0;
            var mismatches = new List<string>();
            foreach (Stage06File file in manifest.files)
            {
                string full = ResolveProjectRelative(file.path);
                if (!File.Exists(full))
                {
                    mismatches.Add(file.path + ":missing");
                    continue;
                }
                if (string.Equals(Sha256(full), file.sha256, StringComparison.OrdinalIgnoreCase))
                {
                    exactCount++;
                    continue;
                }
                if (file.path == "Assets/HospitalSite/Stage05S/R05B/SourceFBX/Materials/No Name.mat"
                    && ValidateKnownMaterialNormalization(full))
                {
                    semanticNormalizationCount++;
                    continue;
                }
                mismatches.Add(file.path + ":hash");
            }
            if (mismatches.Count > 0 || exactCount != 294 || semanticNormalizationCount != 1)
                throw new InvalidOperationException("Approved R44/R05B/Stage6 authority changed: "
                    + string.Join(",", mismatches) + $"; exact={exactCount}; semantic={semanticNormalizationCount}");
        }

        private static bool ValidateKnownMaterialNormalization(string path)
        {
            string text = File.ReadAllText(path);
            return text.Contains("m_Name: No Name")
                && text.Contains("guid: 933532a4fcc9baf4fa0491de14d08ed7")
                && text.Contains("_BaseColor: {r: 1, g: 1, b: 1, a: 1}")
                && text.Contains("_Metallic: 0")
                && text.Contains("_Smoothness: 0.5");
        }

        private static void WriteBuildRecord(string status, string executable,
            string settingsHash, int s2Count, int s3bCount, int s3cCount,
            int stage06Exact, int stage06Semantic)
        {
            Directory.CreateDirectory(ReviewFolder());
            var record = new BuildRecord
            {
                status = status,
                unityVersion = Application.unityVersion,
                sourceS3CScene = SourceS3CScenePath,
                bootstrapScene = BootstrapScenePath,
                playerScenes = PlayerScenes,
                playerExecutable = executable,
                editorBuildSettingsSha256 = settingsHash,
                editorBuildSettingsUntouched = true,
                protectedS2FileCount = s2Count,
                protectedS3BFileCount = s3bCount,
                protectedS3CFileCount = s3cCount,
                stage06ProtectedExactCount = stage06Exact,
                stage06KnownSemanticNormalizationCount = stage06Semantic,
                target = "One adaptive Windows x86_64 build: desktop plus OpenXR Quest Link/Air Link; Android excluded.",
                route = string.Join(" -> ", S3DIntegrationContract.RequiredRoute),
                note = "F00-F06 remain empty. S4-S7 design work is not included. S3D editor-cooks collision from the exact ContinuousLawnVisual source mesh, suppresses the obsolete R43 COL_Perimeter_Front that crossed the integrated drop-off, and remaps only the F00 front safety boundary to the metadata-derived entrance opening. F01-F06 receive S3D-owned 0.20 m solid entrance-facing main-glass edge finishes spanning X -23.95..18.80 and Z -17.30..-15.48 with the exact approved S2 slab material. Whenever an upper floor loads, S3D moves its original front safety boundary from Z -15.50 to the enlarged floor edge at Z -17.30 and widens it to the same X span. Elevator targeting uses 5.5 m reach plus a 0.18 m occlusion-aware aim-assist sweep. Approved sources remain untouched. The pre-existing imported 'No Name' material serialization normalization is semantically checked and not edited by S3D.",
            };
            File.WriteAllText(Path.Combine(ReviewFolder(),
                "StageI1_R03_S3D_BuildRecord.json"), JsonUtility.ToJson(record, true));
        }

        public static void WriteCheckpoint(string status)
        {
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(),
                "HospitalInterior_StageS3D_R03_Checkpoint.md"),
$@"# Hospital Interior R03 Stage S3D Checkpoint

**Active stage:** `S3D - full technical integration with empty floors`  
**Status:** `{status}`  
**S3C:** approved, complete, and protected  
**S4:** deferred until S3D user approval

## Integrated scope

- One bootstrap combines the approved R05B site, four R44 exterior scenes, the S3C E01 system, and exactly one approved empty S2 floor.
- `Exterior_InteriorShellPreview` is excluded so it cannot duplicate the S2 floor slabs.
- One adaptive XROrigin supports desktop review and Windows OpenXR through Quest Link/Air Link.
- Eight approved hero-entrance sliding leaves operate automatically at runtime from their frozen motion metadata.
- Site/exterior scenes remain persistent while E01 swaps exactly one F00-F06 scene.
- The exact approved ContinuousLawnVisual source mesh is editor-cooked as one S3D-only support collider before runtime static batching; it is walkable but remains excluded from teleport surfaces.
- The obsolete R43 `COL_Perimeter_Front` standalone safety wall is suppressed only in S3D because it crossed the integrated R05B drop-off route; the real R05B perimeter remains unchanged.
- F00's two original front safety-boundary segments are suppressed only at runtime and replaced around the actual metadata-derived entrance width.
- F01-F06 have a solid `0.20 m` entrance-facing main-glass floor-edge finish spanning X `-23.95..18.80` and Z `-17.30..-15.48`, using the exact approved S2 slab material.
- On every F01-F06 load, S3D moves the original invisible front safety boundary from Z `-15.50` to the enlarged floor edge at Z `-17.30` and widens it to X `-23.95..18.80`.
- Desktop and PC-VR elevator targeting uses `5.5 m` reach and a `0.18 m` occlusion-aware aim-assist sweep so the physical controls do not require point-blank positioning.
- S2/S3/R44/R05B authorities, the Stage 6A harness, and EditorBuildSettings remain protected.

## Test scene and player

`{BootstrapScenePath}`

`{PlayerExecutable()}`

## Deliberately deferred

F00 planning, rooms, corridors, furniture, visual finishing, upper-floor design, anatomy content, standalone Quest/Android packaging, final headset-performance acceptance, and S4-S8 work.

Automated gates support review but do not grant S3D approval. Complete one desktop route and one physical Quest Link/Air Link route before authorizing S4.
");
        }

        public static string ProjectRoot() => HospitalInteriorS3CR03Builder.ProjectRoot();
        public static string WorkspaceRoot() => HospitalInteriorS3CR03Builder.WorkspaceRoot();
        public static string ReviewFolder() => Path.Combine(WorkspaceRoot(), "Reviews",
            "HospitalInterior", "StageI1_R03", "S3D_TechnicalIntegration");
        public static string ExportFolder() => Path.Combine(WorkspaceRoot(), "Exports",
            "HospitalInterior", "StageI1_R03_S3D_TechnicalIntegration");
        public static string EvidenceFolder() => Path.Combine(ReviewFolder(), "RuntimeEvidence");
        public static string PlayerExecutable() => Path.Combine(ExportFolder(),
            "HospitalInterior_S3D_R03_TechnicalIntegration.exe");

        private static readonly string[] EvidenceNames =
        {
            "01_GateSpawn.png",
            "02_LobbyApproach.png",
            "03_AutomaticEntrancePassage.png",
            "04_F00_ElevatorArrival.png",
            "05_F06_Arrival.png",
            "06_F00_ReturnAndExit.png",
            "07_F01_MainGlassSeam_Corrected.png",
        };

        private static void AssertEvidencePackage()
        {
            string[] paths = EvidenceNames.Select(name => Path.Combine(EvidenceFolder(), name))
                .ToArray();
            if (paths.Any(path => !File.Exists(path) || new FileInfo(path).Length < 50000))
                throw new InvalidOperationException("S3D evidence is missing or not visibly rendered.");
            if (paths.Select(Sha256).Distinct(StringComparer.OrdinalIgnoreCase).Count()
                != EvidenceNames.Length)
                throw new InvalidOperationException("S3D evidence frames are not distinct renders.");
        }

        private static void PackageReviewArtifacts()
        {
            string package = Path.Combine(ExportFolder(), "S3D_ReviewPackage");
            string evidencePackage = Path.Combine(package, "RuntimeEvidence");
            Directory.CreateDirectory(evidencePackage);
            string[] reviewFiles =
            {
                "StageI1_R03_S3D_StaticGate.json",
                "StageI1_R03_S3D_RuntimeGate.json",
                "StageI1_R03_S3D_BuildRecord.json",
                "HospitalInterior_StageS3D_R03_Checkpoint.md",
                "StageI1_R03_S3D_UserReviewChecklist.md",
                "StageI1_R03_S3D_CorrectionRecord.md",
            };
            foreach (string name in reviewFiles)
            {
                string source = Path.Combine(ReviewFolder(), name);
                if (!File.Exists(source))
                    throw new FileNotFoundException("S3D review artifact is missing.", source);
                File.Copy(source, Path.Combine(package, name), true);
            }
            foreach (string name in EvidenceNames)
                File.Copy(Path.Combine(EvidenceFolder(), name),
                    Path.Combine(evidencePackage, name), true);
        }

        public static string Sha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty)
                .ToLowerInvariant();
        }
        private static string AssetPathToAbsolute(string assetPath)
            => Path.GetFullPath(Path.Combine(ProjectRoot(), assetPath));
        private static string ResolveProjectRelative(string path)
            => Path.GetFullPath(Path.Combine(ProjectRoot(), path.Replace('/',
                Path.DirectorySeparatorChar)));

        private sealed class MaterialSet
        {
            public Material floor, left, right, valid, invalid, line, interaction;
        }
    }
}
