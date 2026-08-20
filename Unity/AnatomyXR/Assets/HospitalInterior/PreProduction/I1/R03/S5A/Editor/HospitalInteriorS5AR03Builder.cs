using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS5AR03Builder
    {
        public const string Root = HospitalInteriorS3DR03Builder.Root;
        public const string S5ARoot = Root + "/S5A";
        public const string RuntimeFolder = S5ARoot + "/Runtime";
        public const string EditorFolder = S5ARoot + "/Editor";
        public const string GeneratedFolder = S5ARoot + "/Generated";
        public const string MaterialsFolder = S5ARoot + "/Materials";
        public const string SettingsFolder = S5ARoot + "/Settings";
        public const string SourceFbxPath = S5ARoot
            + "/SourceFBX/HospitalInterior_StairA_S5A_R03.fbx";
        public const string SourceScenePath = HospitalInteriorS3DR03Builder.BootstrapScenePath;
        public const string BootstrapScenePath = Root
            + "/Scenes/HospitalInterior_S5A_R03_StairAIntegration.unity";
        public const string ContractPath = SettingsFolder
            + "/HospitalInterior_S5A_R03_StairContract.asset";
        public const string ProtectedManifestPath = SettingsFolder
            + "/HospitalInterior_S5A_R03_ProtectedAuthority.json";
        public const string StaticGateAssetPath = SettingsFolder
            + "/HospitalInterior_S5A_R03_StaticGate.json";
        public const string F00DualSlabPath = GeneratedFolder
            + "/HospitalInterior_S5A_R03_F00_E01_StairA_DualAperture.asset";
        public const string UpperDualSlabPath = GeneratedFolder
            + "/HospitalInterior_S5A_R03_Upper_E01_StairA_DualAperture.asset";
        public const string UpperDualUndersidePath = GeneratedFolder
            + "/HospitalInterior_S5A_R03_UpperUnderside_E01_StairA_DualAperture.asset";
        public const string ExteriorDualStructurePath = GeneratedFolder
            + "/HospitalInterior_S5A_R03_ExteriorGlobalStructure02_E01_StairA_DualAperture.asset";
        public const string ExpectedBuildSettingsSha256 =
            "62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb";

        public static readonly string[] PlayerScenes = new[] { BootstrapScenePath }
            .Concat(HospitalInteriorS2R03Builder.ProductionScenes)
            .Concat(S3DIntegrationContract.SiteScenePaths)
            .Concat(HospitalInteriorS2R03Builder.FloorScenePaths).ToArray();

        [Serializable]
        private sealed class GateStatus { public string status; }

        [Serializable]
        private sealed class ProtectedRecord
        {
            public string path;
            public string sha256;
            public long bytes;
        }

        [Serializable]
        private sealed class ProtectedManifest
        {
            public string schema = "HospitalInterior.R03.S5A.ProtectedAuthority.v1";
            public string status = "PASS";
            public string capturedUtc;
            public string baselineCommit = "45c9e3b";
            public ProtectedRecord[] files;
        }

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = "HospitalInterior.R03.S5A.Build.v1";
            public string status;
            public string unityVersion;
            public string sourceS3DScene;
            public string bootstrapScene;
            public string sourceFbx;
            public string[] playerScenes;
            public string executable;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public int protectedFileCount;
            public string target;
            public string scope;
        }

        private sealed class MaterialSet
        {
            public Material warm, stone, charcoal, bronze, glass, light, interaction;
        }

        private struct ClipVertex
        {
            public Vector3 local;
            public Vector3 world;
            public Vector3 normal;
            public Vector2 uv;
        }

        private readonly struct CutPlane
        {
            private readonly Vector3 normal;
            private readonly float distance;
            public CutPlane(Vector3 planeNormal, float planeDistance)
            {
                normal = planeNormal;
                distance = planeDistance;
            }
            public float Distance(Vector3 point) => Vector3.Dot(normal, point) + distance;
        }

        [MenuItem("Hospital Interior/R03 S5A/Build Stair A integration", priority = 1)]
        public static void BuildStage()
        {
            RequireInputs();
            EnsureFolders();
            CaptureOrAssertProtectedAuthorities(out int protectedCount);
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings",
                "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);

            ConfigureModelImporter();
            MaterialSet materials = CreateMaterials();
            S3ElevatorContract elevatorContract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(
                HospitalInteriorS3BR03Builder.ContractPath);
            Mesh f00 = CreateDualFloorMesh(F00DualSlabPath,
                elevatorContract.ApprovedS2FloorContract.Floors[0].footprint, false);
            Mesh upper = CreateDualFloorMesh(UpperDualSlabPath,
                elevatorContract.ApprovedS2FloorContract.Floors[1].footprint, false);
            Mesh underside = CreateDualFloorMesh(UpperDualUndersidePath,
                elevatorContract.ApprovedS2FloorContract.Floors[1].footprint, true);
            Mesh exterior = CreateExteriorDualStructure();
            S5AStairContract contract = CreateContract(elevatorContract.ApprovedS2FloorContract,
                f00, upper, underside, exterior);
            BuildIntegrationScene(contract, elevatorContract, materials);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            CaptureOrAssertProtectedAuthorities(out int protectedAfter);
            string settingsAfter = Sha256(settingsPath);
            if (protectedAfter != protectedCount)
                throw new InvalidOperationException("Protected authority count changed during S5A build.");
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(settingsAfter, ExpectedBuildSettingsSha256,
                    StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("EditorBuildSettings changed during S5A build.");
            WriteBuildRecord("SCENE_BUILT_PENDING_AUTOMATED_GATES", string.Empty,
                settingsAfter, protectedCount);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildStageBatch()
        {
            try { BuildStage(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S5A/Build Windows desktop-PCVR review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildStage();
            HospitalInteriorS5AR03Validator.ValidateStaticGate();
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings",
                "EditorBuildSettings.asset");
            string before = Sha256(settingsPath);
            Directory.CreateDirectory(ExportFolder());
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = PlayerExecutable(),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S5A player build failed: " + report.summary.result);
            // Let Unity flush and reimport its transient player-build serialization
            // before comparing the frozen Stage06/S3D authority hashes.
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            string after = Sha256(settingsPath);
            if (before != after || after != ExpectedBuildSettingsSha256)
                throw new InvalidOperationException("EditorBuildSettings changed during S5A player build.");
            CaptureOrAssertProtectedAuthorities(out int protectedCount);
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", PlayerExecutable(), after,
                protectedCount);
            Debug.Log("HOSPITAL_INTERIOR_R03_S5A_PLAYER_BUILD=PASS; " + PlayerExecutable());
        }

        public static void BuildWindowsReviewPlayerBatch()
        {
            try { BuildWindowsReviewPlayer(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void BuildIntegrationScene(S5AStairContract contract,
            S3ElevatorContract elevatorContract, MaterialSet materials)
        {
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, BootstrapScenePath, true))
                throw new InvalidOperationException("Could not clone the approved S3D scene for S5A.");
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            S3DIntegrationBootstrap legacy = Roots(scene)
                .SelectMany(root => root.GetComponentsInChildren<S3DIntegrationBootstrap>(true)).Single();
            GameObject integrationRoot = legacy.gameObject;
            integrationRoot.name = "HospitalInterior_S5A_R03_BootstrapRoot";

            ReplacePersistentFloorMeshes(scene, contract);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbxPath);
            GameObject stair = (GameObject)PrefabUtility.InstantiatePrefab(fbx, scene);
            stair.name = "S5A_R03_Persistent_StairA";
            stair.transform.SetParent(integrationRoot.transform, false);
            stair.transform.localPosition = Vector3.zero;
            stair.transform.localRotation = Quaternion.identity;
            stair.transform.localScale = Vector3.one;
            AssignMaterialsAndColliders(stair, materials);

            S5ADualAperture aperture = integrationRoot.AddComponent<S5ADualAperture>();
            aperture.Configure(contract);
            S5AFloorSceneCoordinator coordinator = integrationRoot
                .AddComponent<S5AFloorSceneCoordinator>();
            coordinator.Configure(contract, aperture, legacy.FacadeFloorEdgeCoordinator);
            S3ElevatorController legacyElevator = legacy.ElevatorController;
            S5AVerticalCirculationController circulation = integrationRoot
                .AddComponent<S5AVerticalCirculationController>();
            circulation.Configure(elevatorContract, contract, coordinator,
                legacyElevator.CabinRoot, legacy.XrOrigin.transform,
                legacyElevator.FloorDoorControllers, legacyElevator.CabinTravelIndicator);

            S5AStairDoorController[] stairDoors = CreateStairDoors(stair.transform,
                contract, circulation, legacy.XrOrigin.transform);
            circulation.ConfigureStairDoors(stairDoors);
            RewireElevatorControls(scene, circulation);

            LineRenderer interactionLine = CreateInteractionLine(
                legacy.AdaptiveRig.RightController, materials.interaction);
            S5AAdaptiveInteractionRouter router = legacy.XrOrigin.gameObject
                .AddComponent<S5AAdaptiveInteractionRouter>();
            router.Configure(legacy.AdaptiveRig, legacy.IntegrationCamera,
                legacy.AdaptiveRig.RightController, legacy.AdaptiveRig.Actions,
                interactionLine);
            S5ARecoveryCoordinator recovery = legacy.XrOrigin.gameObject
                .AddComponent<S5ARecoveryCoordinator>();
            recovery.Configure(legacy.AdaptiveRig, coordinator, contract);
            S5AIntegrationBootstrap bootstrap = integrationRoot
                .AddComponent<S5AIntegrationBootstrap>();
            bootstrap.Configure(legacy, contract, aperture, coordinator, circulation,
                stairDoors, router, recovery, stair.transform);

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static S5AStairDoorController[] CreateStairDoors(Transform stair,
            S5AStairContract contract, S5AVerticalCirculationController circulation,
            Transform player)
        {
            const float exteriorApproachMaxX = -19.0f;
            const float interiorApproachInsetM = 0.175f;
            float approachMinX = contract.BoundsMinXZ.x + interiorApproachInsetM;
            float approachCentreX = (approachMinX + exteriorApproachMaxX) * 0.5f;
            float approachWidthX = exteriorApproachMaxX - approachMinX;
            var result = new List<S5AStairDoorController>();
            foreach (S2FloorRecord floor in contract.ApprovedFloorContract.Floors)
            {
                Transform leaf = stair.GetComponentsInChildren<Transform>(true).Single(item
                    => item.name == "S5A_R03_StairA_DoorLeaf_" + floor.floorId);
                Transform vision = stair.GetComponentsInChildren<Transform>(true).Single(item
                    => item.name == "S5A_R03_StairA_DoorVision_" + floor.floorId);
                vision.SetParent(leaf, true);
                GameObject trigger = new GameObject("S5A_R03_StairDoorTrigger_" + floor.floorId);
                trigger.transform.SetParent(stair, false);
                trigger.transform.position = new Vector3(approachCentreX,
                    floor.elevationM + 1.125f,
                    contract.DoorCentreZ);
                BoxCollider zone = trigger.AddComponent<BoxCollider>();
                zone.isTrigger = true;
                zone.size = new Vector3(approachWidthX, 2.25f, 1.60f);
                Rigidbody body = trigger.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                S5AStairDoorController controller = trigger
                    .AddComponent<S5AStairDoorController>();
                controller.Configure(floor.floorId, leaf, vision, player, circulation,
                    contract.DoorSlideM);
                result.Add(controller);
            }
            return result.ToArray();
        }

        private static void RewireElevatorControls(Scene scene,
            S5AVerticalCirculationController circulation)
        {
            foreach (S3ElevatorButton old in Roots(scene)
                .SelectMany(root => root.GetComponentsInChildren<S3ElevatorButton>(true)).ToArray())
            {
                S5AElevatorInteraction replacement = old.gameObject
                    .AddComponent<S5AElevatorInteraction>();
                replacement.Configure(old.Command, old.FloorId, circulation);
                Object.DestroyImmediate(old);
            }
        }

        private static void ReplacePersistentFloorMeshes(Scene scene,
            S5AStairContract contract)
        {
            foreach (MeshFilter filter in Roots(scene)
                .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true)))
            {
                if (filter.name.StartsWith("S3C_R03_E01_PersistentFloorPlate_F00",
                    StringComparison.Ordinal))
                    filter.sharedMesh = contract.F00DualApertureSlab;
                else if (filter.name.StartsWith("S3C_R03_E01_PersistentFloorPlate_",
                    StringComparison.Ordinal))
                    filter.sharedMesh = contract.UpperDualApertureSlab;
                else if (filter.name.StartsWith("S3B_R03_StructuralUnderside_",
                    StringComparison.Ordinal))
                    filter.sharedMesh = contract.UpperDualApertureUnderside;
            }
        }

        private static void AssignMaterialsAndColliders(GameObject stair,
            MaterialSet materials)
        {
            foreach (MeshRenderer renderer in stair.GetComponentsInChildren<MeshRenderer>(true))
            {
                string name = renderer.name;
                Material material = name.Contains("Step_") || name.Contains("Landing_")
                    || name.Contains("FloorLanding_") || name.Contains("MidLanding_")
                    ? materials.stone
                    : name.Contains("Handrail_") || name.Contains("RailPost_")
                        || name.Contains("Guard") || name.Contains("DoorFrame_")
                        ? materials.bronze
                        : name.Contains("Vision_") ? materials.glass
                        : name.Contains("Light_") ? materials.light
                        : name.Contains("Nosing_") || name.Contains("Stringer_")
                            || name.Contains("DoorLeaf_") || name.Contains("Threshold_")
                            ? materials.charcoal : materials.warm;
                renderer.sharedMaterial = material;
            }
            foreach (MeshFilter filter in stair.GetComponentsInChildren<MeshFilter>(true))
            {
                string name = filter.name;
                bool collision = !name.Contains("Nosing_") && !name.Contains("Vision_")
                    && !name.Contains("Light_") && !name.Contains("Label_");
                if (!collision)
                    continue;
                foreach (Collider existing in filter.gameObject.GetComponents<Collider>())
                    Object.DestroyImmediate(existing);
                if (name.Contains("DoorLeaf_"))
                {
                    BoxCollider box = filter.gameObject.AddComponent<BoxCollider>();
                    box.center = filter.sharedMesh.bounds.center;
                    box.size = filter.sharedMesh.bounds.size;
                }
                else
                {
                    MeshCollider collider = filter.gameObject.AddComponent<MeshCollider>();
                    collider.sharedMesh = filter.sharedMesh;
                    collider.convex = false;
                }
            }
        }

        private static LineRenderer CreateInteractionLine(Transform parent,
            Material material)
        {
            GameObject lineObject = new GameObject("S5A Shared Circulation Interaction Ray");
            lineObject.transform.SetParent(parent, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.widthMultiplier = 0.012f;
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.enabled = false;
            return line;
        }

        private static S5AStairContract CreateContract(S2FloorContract floorContract,
            Mesh f00, Mesh upper, Mesh underside, Mesh exterior)
        {
            // Opening Exterior_Base to cook the structure cut can invalidate asset
            // wrappers held before the scene switch. Resolve every persistent input
            // again so the contract never captures fake-null UnityEngine.Objects.
            floorContract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(
                HospitalInteriorS2R03Builder.ContractPath);
            f00 = AssetDatabase.LoadAssetAtPath<Mesh>(F00DualSlabPath);
            upper = AssetDatabase.LoadAssetAtPath<Mesh>(UpperDualSlabPath);
            underside = AssetDatabase.LoadAssetAtPath<Mesh>(UpperDualUndersidePath);
            exterior = AssetDatabase.LoadAssetAtPath<Mesh>(ExteriorDualStructurePath);
            S5AStairContract contract = AssetDatabase.LoadAssetAtPath<S5AStairContract>(ContractPath);
            if (contract == null)
            {
                contract = ScriptableObject.CreateInstance<S5AStairContract>();
                AssetDatabase.CreateAsset(contract, ContractPath);
            }
            contract.Configure(floorContract, f00, upper, underside, exterior);
            if (!contract.Validate(out string detail))
                throw new InvalidOperationException("Generated S5A contract is invalid: " + detail);
            EditorUtility.SetDirty(contract);
            return contract;
        }

        private static Mesh CreateDualFloorMesh(string path, Vector2[] outline,
            bool downwardFacing)
        {
            Rect elevator = new Rect(0.30f, 2.90f, 2.70f, 2.80f);
            Rect stair = new Rect(-29.0f, -5.0f, 7f, 10f);
            float[] xs = outline.Select(point => point.x)
                .Concat(new[] { elevator.xMin, elevator.xMax, stair.xMin, stair.xMax })
                .Distinct().OrderBy(item => item).ToArray();
            float[] zs = outline.Select(point => point.y)
                .Concat(new[] { elevator.yMin, elevator.yMax, stair.yMin, stair.yMax })
                .Distinct().OrderBy(item => item).ToArray();
            float minX = outline.Min(item => item.x);
            float maxX = outline.Max(item => item.x);
            float minZ = outline.Min(item => item.y);
            float maxZ = outline.Max(item => item.y);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            for (int xi = 0; xi < xs.Length - 1; xi++)
            for (int zi = 0; zi < zs.Length - 1; zi++)
            {
                float x0 = xs[xi], x1 = xs[xi + 1], z0 = zs[zi], z1 = zs[zi + 1];
                Vector2 centre = new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
                if (!PointInPolygon(centre, outline) || elevator.Contains(centre)
                    || stair.Contains(centre))
                    continue;
                int start = vertices.Count;
                vertices.Add(new Vector3(x0, 0f, z0));
                vertices.Add(new Vector3(x0, 0f, z1));
                vertices.Add(new Vector3(x1, 0f, z1));
                vertices.Add(new Vector3(x1, 0f, z0));
                uv.Add(new Vector2(Mathf.InverseLerp(minX, maxX, x0), Mathf.InverseLerp(minZ, maxZ, z0)));
                uv.Add(new Vector2(Mathf.InverseLerp(minX, maxX, x0), Mathf.InverseLerp(minZ, maxZ, z1)));
                uv.Add(new Vector2(Mathf.InverseLerp(minX, maxX, x1), Mathf.InverseLerp(minZ, maxZ, z1)));
                uv.Add(new Vector2(Mathf.InverseLerp(minX, maxX, x1), Mathf.InverseLerp(minZ, maxZ, z0)));
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
            Mesh mesh = LoadOrCreateMesh(path);
            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        private static Mesh CreateExteriorDualStructure()
        {
            Scene exterior = EditorSceneManager.OpenScene(
                HospitalInteriorS2R03Builder.ProductionScenes[0], OpenSceneMode.Single);
            MeshFilter sourceFilter = Roots(exterior)
                .SelectMany(root => root.GetComponentsInChildren<MeshFilter>(true))
                .Single(item => item.name == S5AIntegrationBootstrap.ExteriorBaseBlockerName);
            Mesh shaftCut = AssetDatabase.LoadAssetAtPath<Mesh>(
                HospitalInteriorS3CR03Builder.ExteriorBaseShaftCutMeshPath);
            if (shaftCut == null)
                throw new InvalidOperationException("Approved S3C exterior shaft-cut mesh is missing.");
            Mesh generated = ClipMeshOutsidePrism(shaftCut, sourceFilter.transform,
                new Vector3(-29.0f, -0.20f, -5.0f),
                new Vector3(-22.0f, 28.83f, 5.0f));
            generated.name = Path.GetFileNameWithoutExtension(ExteriorDualStructurePath);
            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(ExteriorDualStructurePath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(generated, ExteriorDualStructurePath);
                asset = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, asset);
                asset.name = generated.name;
                EditorUtility.SetDirty(asset);
                Object.DestroyImmediate(generated);
            }
            return asset;
        }

        private static Mesh ClipMeshOutsidePrism(Mesh source, Transform sourceTransform,
            Vector3 min, Vector3 max)
        {
            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUv = source.uv;
            bool hasNormals = sourceNormals.Length == sourceVertices.Length;
            bool hasUv = sourceUv.Length == sourceVertices.Length;
            Matrix4x4 localToWorld = sourceTransform.localToWorldMatrix;
            var planes = new[]
            {
                new CutPlane(Vector3.right, -min.x),
                new CutPlane(Vector3.left, max.x),
                new CutPlane(Vector3.up, -min.y),
                new CutPlane(Vector3.down, max.y),
                new CutPlane(Vector3.forward, -min.z),
                new CutPlane(Vector3.back, max.z),
            };
            var vertices = new List<ClipVertex>();
            var subTriangles = Enumerable.Range(0, source.subMeshCount)
                .Select(_ => new List<int>()).ToArray();
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                for (int index = 0; index < triangles.Length; index += 3)
                {
                    ClipVertex a = ReadVertex(triangles[index]);
                    ClipVertex b = ReadVertex(triangles[index + 1]);
                    ClipVertex c = ReadVertex(triangles[index + 2]);
                    AppendOutside(a, b, c, planes, vertices, subTriangles[sub]);
                }
            }
            Mesh result = new Mesh
            {
                indexFormat = vertices.Count > ushort.MaxValue
                    ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };
            result.SetVertices(vertices.Select(item => item.local).ToList());
            if (hasNormals)
                result.SetNormals(vertices.Select(item => item.normal).ToList());
            if (hasUv)
                result.SetUVs(0, vertices.Select(item => item.uv).ToList());
            result.subMeshCount = subTriangles.Length;
            for (int sub = 0; sub < subTriangles.Length; sub++)
                result.SetTriangles(subTriangles[sub], sub, false);
            if (!hasNormals)
                result.RecalculateNormals();
            result.RecalculateBounds();
            return result;

            ClipVertex ReadVertex(int vertexIndex)
            {
                Vector3 local = sourceVertices[vertexIndex];
                return new ClipVertex
                {
                    local = local,
                    world = localToWorld.MultiplyPoint3x4(local),
                    normal = hasNormals ? sourceNormals[vertexIndex] : Vector3.up,
                    uv = hasUv ? sourceUv[vertexIndex] : Vector2.zero,
                };
            }
        }

        private static void AppendOutside(ClipVertex a, ClipVertex b, ClipVertex c,
            IEnumerable<CutPlane> planes, ICollection<ClipVertex> vertices,
            ICollection<int> triangles)
        {
            var candidates = new List<List<ClipVertex>> { new List<ClipVertex> { a, b, c } };
            foreach (CutPlane plane in planes)
            {
                var nextInside = new List<List<ClipVertex>>();
                foreach (List<ClipVertex> polygon in candidates)
                {
                    List<ClipVertex> outside = ClipPolygon(polygon, plane, false);
                    if (outside.Count >= 3)
                        AppendPolygon(outside, vertices, triangles);
                    List<ClipVertex> inside = ClipPolygon(polygon, plane, true);
                    if (inside.Count >= 3)
                        nextInside.Add(inside);
                }
                candidates = nextInside;
                if (candidates.Count == 0)
                    break;
            }
        }

        private static List<ClipVertex> ClipPolygon(IReadOnlyList<ClipVertex> polygon,
            CutPlane plane, bool keepPositive)
        {
            const float epsilon = 0.00001f;
            var output = new List<ClipVertex>();
            ClipVertex previous = polygon[polygon.Count - 1];
            float previousDistance = plane.Distance(previous.world);
            bool previousKept = keepPositive ? previousDistance >= -epsilon
                : previousDistance < -epsilon;
            foreach (ClipVertex current in polygon)
            {
                float currentDistance = plane.Distance(current.world);
                bool currentKept = keepPositive ? currentDistance >= -epsilon
                    : currentDistance < -epsilon;
                if (currentKept != previousKept)
                {
                    float t = previousDistance / (previousDistance - currentDistance);
                    output.Add(Lerp(previous, current, t));
                }
                if (currentKept)
                    output.Add(current);
                previous = current;
                previousDistance = currentDistance;
                previousKept = currentKept;
            }
            return output;
        }

        private static ClipVertex Lerp(ClipVertex a, ClipVertex b, float t)
            => new ClipVertex
            {
                local = Vector3.LerpUnclamped(a.local, b.local, t),
                world = Vector3.LerpUnclamped(a.world, b.world, t),
                normal = Vector3.LerpUnclamped(a.normal, b.normal, t).normalized,
                uv = Vector2.LerpUnclamped(a.uv, b.uv, t),
            };

        private static void AppendPolygon(IReadOnlyList<ClipVertex> polygon,
            ICollection<ClipVertex> vertices, ICollection<int> triangles)
        {
            int start = vertices.Count;
            foreach (ClipVertex vertex in polygon)
                vertices.Add(vertex);
            for (int index = 1; index < polygon.Count - 1; index++)
            {
                triangles.Add(start);
                triangles.Add(start + index);
                triangles.Add(start + index + 1);
            }
        }

        private static MaterialSet CreateMaterials()
        {
            return new MaterialSet
            {
                warm = CreateMaterial("MAT_S5A_WarmOffWhite", new Color(0.76f, 0.73f, 0.66f), 0f, 0.38f),
                stone = CreateMaterial("MAT_S5A_StoneTerrazzo", new Color(0.34f, 0.36f, 0.36f), 0f, 0.58f),
                charcoal = CreateMaterial("MAT_S5A_Charcoal", new Color(0.025f, 0.032f, 0.038f), 0.52f, 0.76f),
                bronze = CreateMaterial("MAT_S5A_Bronze", new Color(0.24f, 0.095f, 0.035f), 0.72f, 0.76f),
                glass = CreateMaterial("MAT_S5A_BlueGreyDoorGlass", new Color(0.06f, 0.16f, 0.21f, 0.48f), 0.04f, 0.88f),
                light = CreateMaterial("MAT_S5A_NeutralLandingLight", new Color(0.88f, 0.80f, 0.62f), 0f, 0.75f, new Color(1f, 0.80f, 0.58f) * 1.8f),
                interaction = CreateMaterial("MAT_S5A_InteractionRay", new Color(0.20f, 0.85f, 1f), 0f, 0.8f, new Color(0.20f, 0.85f, 1f)),
            };
        }

        private static Material CreateMaterial(string name, Color color, float metallic,
            float smoothness, Color? emission = null)
        {
            string path = MaterialsFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard");
            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            material.color = color;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
            }
            if (color.a < 0.99f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.renderQueue = 3000;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Mesh LoadOrCreateMesh(string path)
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;
            mesh = new Mesh();
            AssetDatabase.CreateAsset(mesh, path);
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

        private static void ConfigureModelImporter()
        {
            AssetDatabase.ImportAsset(SourceFbxPath, ImportAssetOptions.ForceSynchronousImport);
            ModelImporter importer = AssetImporter.GetAtPath(SourceFbxPath) as ModelImporter;
            if (importer == null)
                throw new InvalidOperationException("S5A Stair A FBX importer unavailable.");
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        private static void RequireInputs()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S5A requires Unity 6000.3.20f1; found "
                    + Application.unityVersion + ".");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("S5A requires StandaloneWindows64 active build target.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new FileNotFoundException("Approved S3D scene missing.", SourceScenePath);
            if (!File.Exists(AssetPathToAbsolute(SourceFbxPath)))
                throw new FileNotFoundException("S5A Stair A FBX missing.", SourceFbxPath);
            RequirePass(Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S5A_StairA", "StageI1_R03_S5A_StairA_BlenderGate.json"));
            RequirePass(Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S3D_TechnicalIntegration", "StageI1_R03_S3D_StaticGate.json"));
            RequirePass(Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S3D_TechnicalIntegration", "StageI1_R03_S3D_RuntimeGate.json"));
            string approval = Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S4_F00_Plan", "StageI1_R03_S4_F00_UserApproval.md");
            if (!File.Exists(approval) || Sha256(approval) != S5AStairContract.S4ApprovalSha256)
                throw new InvalidOperationException("Immutable S4 user approval hash mismatch.");
            string plan = Path.Combine(WorkspaceRoot(), "Reviews", "HospitalInterior",
                "StageI1_R03", "S4_F00_Plan", "StageI1_R03_S4_F00_Plan_V03_Contract.json");
            if (!File.Exists(plan) || Sha256(plan) != S5AStairContract.S4PlanContractSha256)
                throw new InvalidOperationException("Immutable S4 plan contract hash mismatch.");
        }

        private static void RequirePass(string path)
        {
            GateStatus gate = File.Exists(path)
                ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(path)) : null;
            if (gate?.status != "PASS")
                throw new InvalidOperationException("Required PASS gate missing: " + path);
        }

        private static void CaptureOrAssertProtectedAuthorities(out int count)
        {
            HospitalInteriorS3DR03Builder.AssertProtectedAuthorities(out _, out _, out _,
                out _, out _);
            string manifestPath = AssetPathToAbsolute(ProtectedManifestPath);
            if (!File.Exists(manifestPath))
            {
                string[] files = ProtectedFiles().OrderBy(item => item,
                    StringComparer.OrdinalIgnoreCase).ToArray();
                var manifest = new ProtectedManifest
                {
                    capturedUtc = DateTime.UtcNow.ToString("O"),
                    files = files.Select(path => new ProtectedRecord
                    {
                        path = WorkspaceRelative(path),
                        sha256 = Sha256(path),
                        bytes = new FileInfo(path).Length,
                    }).ToArray(),
                };
                File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
                AssetDatabase.ImportAsset(ProtectedManifestPath,
                    ImportAssetOptions.ForceSynchronousImport);
            }
            ProtectedManifest loaded = JsonUtility.FromJson<ProtectedManifest>(
                File.ReadAllText(manifestPath));
            if (loaded?.status != "PASS" || loaded.files == null)
                throw new InvalidOperationException("S5A protected manifest is invalid.");
            var mismatches = new List<string>();
            foreach (ProtectedRecord record in loaded.files)
            {
                string full = Path.Combine(WorkspaceRoot(), record.path.Replace('/',
                    Path.DirectorySeparatorChar));
                if (!File.Exists(full))
                    mismatches.Add(record.path + ":missing");
                else if (Sha256(full) != record.sha256)
                    mismatches.Add(record.path + ":hash");
            }
            if (mismatches.Count > 0)
                throw new InvalidOperationException("Approved S2/S3/S4 authority changed: "
                    + string.Join(",", mismatches));
            count = loaded.files.Length;
        }

        public static void AssertProtectedAuthorities(out int count)
            => CaptureOrAssertProtectedAuthorities(out count);

        private static IEnumerable<string> ProtectedFiles()
        {
            string project = ProjectRoot();
            string workspace = WorkspaceRoot();
            string runtime = Path.Combine(project, Root, "Runtime");
            string s3d = Path.Combine(project, HospitalInteriorS3DR03Builder.S3DRoot);
            string s3dReviews = Path.Combine(workspace, "Reviews", "HospitalInterior",
                "StageI1_R03", "S3D_TechnicalIntegration");
            string s4Reviews = Path.Combine(workspace, "Reviews", "HospitalInterior",
                "StageI1_R03", "S4_F00_Plan");
            foreach (string folder in new[] { runtime, s3d, s3dReviews, s4Reviews })
                foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                    yield return Path.GetFullPath(file);
            yield return AssetPathToAbsolute(SourceScenePath);
            yield return AssetPathToAbsolute(SourceScenePath + ".meta");
            yield return Path.Combine(workspace, "Tools", "HospitalInterior",
                "generate_hospital_interior_s4_plan.py");
            yield return Path.Combine(workspace, "HOSPITAL_INTERIOR_DESIGN_PLAN.md");
        }

        private static void EnsureFolders()
        {
            EnsureAssetFolder(S5ARoot, "Generated");
            EnsureAssetFolder(S5ARoot, "Materials");
            EnsureAssetFolder(S5ARoot, "Settings");
            EnsureAssetFolder(S5ARoot, "Runtime");
            EnsureAssetFolder(S5ARoot, "Editor");
        }

        private static void EnsureAssetFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject[] Roots(Scene scene) => scene.GetRootGameObjects();

        private static void WriteBuildRecord(string status, string executable,
            string settingsHash, int protectedCount)
        {
            Directory.CreateDirectory(ReviewFolder());
            var record = new BuildRecord
            {
                status = status,
                unityVersion = Application.unityVersion,
                sourceS3DScene = SourceScenePath,
                bootstrapScene = BootstrapScenePath,
                sourceFbx = SourceFbxPath,
                playerScenes = PlayerScenes,
                executable = executable,
                editorBuildSettingsSha256 = settingsHash,
                editorBuildSettingsUntouched = true,
                protectedFileCount = protectedCount,
                target = "Windows x86_64 desktop plus OpenXR Quest Link/Air Link; Android excluded",
                scope = "Continuous playable Stair A only. S5B lobby content and upper-floor programmes absent.",
            };
            File.WriteAllText(Path.Combine(ReviewFolder(),
                "StageI1_R03_S5A_BuildRecord.json"), JsonUtility.ToJson(record, true));
        }

        public static string ProjectRoot() => HospitalInteriorS3DR03Builder.ProjectRoot();
        public static string WorkspaceRoot() => HospitalInteriorS3DR03Builder.WorkspaceRoot();
        public static string ReviewFolder() => Path.Combine(WorkspaceRoot(), "Reviews",
            "HospitalInterior", "StageI1_R03", "S5A_StairA");
        public static string ExportFolder() => Path.Combine(WorkspaceRoot(), "Exports",
            "HospitalInterior", "StageI1_R03_S5A_StairA");
        public static string PlayerExecutable() => Path.Combine(ExportFolder(),
            "HospitalInterior_S5A_R03_StairA.exe");
        public static string EvidenceFolder() => Path.Combine(ReviewFolder(), "RuntimeEvidence");
        public static string RuntimeReportPath() => Path.Combine(ReviewFolder(),
            "StageI1_R03_S5A_RuntimeGate.json");

        public static string Sha256(string path)
        {
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(path);
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty)
                .ToLowerInvariant();
        }

        private static string AssetPathToAbsolute(string assetPath)
            => Path.GetFullPath(Path.Combine(ProjectRoot(), assetPath));
        private static string WorkspaceRelative(string path)
            => Path.GetRelativePath(WorkspaceRoot(), path).Replace('\\', '/');
    }
}
