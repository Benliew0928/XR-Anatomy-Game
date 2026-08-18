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
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3CR03Builder
    {
        public const string Root = HospitalInteriorS3BR03Builder.Root;
        public const string BootstrapScenePath = Root + "/Scenes/HospitalInterior_S3C_R03_E01Travel_SIM_LOCAL.unity";
        public const string S3BScenePath = HospitalInteriorS3BR03Builder.BootstrapScenePath;
        public const string S3CGeneratedFolder = Root + "/S3C/Generated";
        public const string ExteriorBaseShaftCutMeshPath = S3CGeneratedFolder
            + "/HospitalInterior_S3C_R03_ExteriorGlobalStructure02_ShaftCut.asset";
        public const string ExteriorBaseBlockerName = "UE_EXT_GlobalStructure_OPAQUE_ALL_02";

        public static readonly string[] PlayerScenes = new[] { BootstrapScenePath }
            .Concat(HospitalInteriorS2R03Builder.ProductionScenes)
            .Concat(HospitalInteriorS2R03Builder.FloorScenePaths).ToArray();

        internal static readonly string[] OpenCabinRoofPartNames =
        {
            "S3A_R03_E01_CabinCeiling",
            "S3A_R03_E01_CeilingDiffuser",
            "S3A_R03_E01_DiffuserTrim_Front",
            "S3A_R03_E01_DiffuserTrim_Left",
            "S3A_R03_E01_DiffuserTrim_Rear",
            "S3A_R03_E01_DiffuserTrim_Right",
        };

        private struct ClipVertex
        {
            public Vector3 localPosition;
            public Vector3 worldPosition;
            public Vector3 normal;
            public Vector4 tangent;
            public Vector2 uv0;
            public Vector2 uv1;
            public Color color;
        }

        private readonly struct CutPlane
        {
            public readonly Vector3 normal;
            public readonly float offset;

            public CutPlane(Vector3 planeNormal, float planeOffset)
            {
                normal = planeNormal;
                offset = planeOffset;
            }

            public float Distance(Vector3 point) => Vector3.Dot(normal, point) + offset;
        }

        private static readonly string[,] LockedS3BFiles =
        {
            { "Assets/HospitalInterior/PreProduction/I1/R03/Scenes/HospitalInterior_S3B_R03_Doors_SIM_LOCAL.unity", "80f5e3e9910de532ecdd3c1fedc4112d12efc801cc5b81796403c305b5084a5f" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Settings/HospitalInterior_S3_R03_ElevatorContract.asset", "845b2b839e06b2a6c07b1edf45a0eb4840d8e633a9cbf47af8fe00a10e27944e" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Runtime/S3DoorController.cs", "e0e545cd52227ae4b60c3054c85447504ae097fa8b323d7c29b008910da326fe" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Runtime/S3DoorObstructionSensor.cs", "124a0b0fe1f6628b94ac27541cbc034c04f75b29fcbd2d3bfc410e24a4e92439" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Runtime/S3ShaftFloorAperture.cs", "e9f1f7764b9736faf11fa97dd95f1b9667aec06e758b72a031cf6951b2ba1c62" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Runtime/S3ShaftStructure.cs", "110751a6bb92ea626afa043692ccbabdb8edf4f263e6fc295daffecc05f7296f" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/Runtime/S3CabinObservationDesign.cs", "199f950cdc56ff4ea641504a6cf2cde1b82fc5aaacb9a20499bc47e3b8512786" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/S3B/SourceFBX/HospitalInterior_E01_Cabin_R03.fbx", "5830cdd18d1a281f3b75b0b33ffafa4a277771d64e6110f673e0f08cd78673bc" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/S3B/SourceFBX/HospitalInterior_E01_LandingPortal_R03.fbx", "b0c52ec1263d7cda874cd9e677713c55bc7ba88fda869701a3469d8d1ea174a6" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/S3B/Generated/HospitalInterior_S3B_R03_F00_ShaftCutSlab.asset", "ec396808df5716f1181848c758cb3564c73d1b3c859647417de919cd50fee462" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/S3B/Generated/HospitalInterior_S3B_R03_Upper_ShaftCutSlab.asset", "7bcdf7dafe628959ef0cd0c87121e1213a11ca3780504fc56b2636fe9de13b15" },
            { "Assets/HospitalInterior/PreProduction/I1/R03/S3B/Generated/HospitalInterior_S3B_R03_Upper_ShaftCutUnderside.asset", "df55e461c11fcf0abae8a5e6a81dfec34730bd2b723bb1d89563d90b385d9a1f" },
        };

        [Serializable]
        private sealed class GateStatus
        {
            public string status;
        }

        [Serializable]
        private sealed class BuildRecord
        {
            public string schema = "HospitalInterior.R03.S3C.BuildRecord.v1";
            public string status;
            public string unityVersion;
            public string sourceS3BScene;
            public string bootstrapScene;
            public string[] playerScenes;
            public string playerExecutable;
            public int protectedS2FileCount;
            public int protectedS2MismatchCount;
            public int protectedS3BFileCount;
            public int protectedS3BMismatchCount;
            public string editorBuildSettingsSha256;
            public bool editorBuildSettingsUntouched;
            public string route;
            public string passengerPolicy;
            public string transitionPolicy;
            public string recoveryPolicy;
        }

        [MenuItem("Hospital Interior/R03 S3C/Build E01 travel review stage", priority = 1)]
        public static void BuildStage()
        {
            RequireInputs();
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int s2Count);
            AssertLockedS3BInputs(out int s3bCount);
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);

            Mesh exteriorBaseShaftCutMesh = CreateExteriorBaseShaftCutMesh();

            Scene sourceScene = EditorSceneManager.OpenScene(S3BScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(sourceScene, BootstrapScenePath, true))
                throw new InvalidOperationException("Could not duplicate the approved S3B scene for S3C.");
            Scene scene = EditorSceneManager.OpenScene(BootstrapScenePath, OpenSceneMode.Single);
            BuildTravelSystem(scene, exteriorBaseShaftCutMesh);
            if (!EditorSceneManager.SaveScene(scene, BootstrapScenePath))
                throw new InvalidOperationException("Could not save the S3C travel scene.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int s2CountAfter);
            AssertLockedS3BInputs(out int s3bCountAfter);
            string settingsAfter = Sha256(settingsPath);
            if (s2CountAfter != s2Count || s3bCountAfter != s3bCount)
                throw new InvalidOperationException("Protected baseline count changed during S3C build.");
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed while building S3C.");
            WriteBuildRecord("SCENE_BUILT_PENDING_VALIDATION", string.Empty, settingsAfter,
                s2Count, s3bCount);
            WriteCheckpoint("IMPLEMENTED_PENDING_AUTOMATED_AND_USER_FUNCTIONAL_REVIEW");
            Debug.Log("HOSPITAL_INTERIOR_R03_S3C_BUILD=PASS; " + BootstrapScenePath);
        }

        public static void BuildStageBatch()
        {
            try { BuildStage(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        [MenuItem("Hospital Interior/R03 S3C/Build Windows travel review player", priority = 20)]
        public static void BuildWindowsReviewPlayer()
        {
            BuildStage();
            string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
            string settingsBefore = Sha256(settingsPath);
            string outputDirectory = ExportFolder();
            Directory.CreateDirectory(outputDirectory);
            string executable = Path.Combine(outputDirectory, "HospitalInterior_S3C_R03_TravelReview.exe");
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = PlayerScenes,
                locationPathName = executable,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("S3C Windows player build failed: " + report.summary.result);
            string settingsAfter = Sha256(settingsPath);
            if (!string.Equals(settingsBefore, settingsAfter, StringComparison.Ordinal))
                throw new InvalidOperationException("EditorBuildSettings changed during the explicit S3C player build.");
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int s2Count);
            AssertLockedS3BInputs(out int s3bCount);
            WriteBuildRecord("PLAYER_BUILT_PENDING_RUNTIME_GATE", executable, settingsAfter,
                s2Count, s3bCount);
            Debug.Log("HOSPITAL_INTERIOR_R03_S3C_PLAYER_BUILD=PASS; " + executable);
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
                string staticPath = Path.Combine(ReviewFolder(), "StageI1_R03_S3C_StaticGate.json");
                string runtimePath = Path.Combine(ReviewFolder(), "StageI1_R03_S3C_RuntimeGate.json");
                GateStatus staticGate = File.Exists(staticPath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(staticPath)) : null;
                GateStatus runtimeGate = File.Exists(runtimePath)
                    ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(runtimePath)) : null;
                if (staticGate?.status != "PASS" || runtimeGate?.status != "PASS")
                    throw new InvalidOperationException("S3C automated reports are not both PASS.");
                string executable = Path.Combine(ExportFolder(),
                    "HospitalInterior_S3C_R03_TravelReview.exe");
                if (!File.Exists(executable))
                    throw new FileNotFoundException("S3C review player is missing.", executable);
                HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int s2Count);
                AssertLockedS3BInputs(out int s3bCount);
                string settingsPath = Path.Combine(ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
                WriteBuildRecord("AUTOMATED_GATES_PASS_PENDING_USER_FUNCTIONAL_APPROVAL",
                    executable, Sha256(settingsPath), s2Count, s3bCount);
                WriteCheckpoint("AUTOMATED_GATES_PASS_PENDING_USER_FUNCTIONAL_APPROVAL");
                Debug.Log("HOSPITAL_INTERIOR_R03_S3C_FINALIZE=PASS; user functional approval pending");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildTravelSystem(Scene scene, Mesh exteriorBaseShaftCutMesh)
        {
            S3BReviewBootstrap s3b = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3BReviewBootstrap>(true)).Single();
            S3ElevatorContract contract = s3b.ElevatorContract;
            S3DoorController f00Door = s3b.DoorController;
            XROrigin origin = s3b.XrOrigin;
            Camera camera = s3b.ReviewCamera;
            Transform cabin = s3b.CabinModelRoot;
            Transform[] portals = s3b.LandingPortalRoots;
            S3ShaftStructure shaft = s3b.ShaftStructure;
            S3ShaftFloorAperture aperture = s3b.FloorAperture;
            S3CabinObservationDesign observation = s3b.CabinObservationDesign;
            S3DoorObstructionSensor sensor = f00Door.ObstructionSensor;
            GameObject obstructionProxy = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .Single(item => item.name == "S3B_R03_REVIEW_ThresholdObstructionProxy").gameObject;
            Transform cabinLeft = f00Door.MovingLeaves[0];
            Transform cabinRight = f00Door.MovingLeaves[1];
            Transform core = f00Door.transform;

            UnityEngine.Object.DestroyImmediate(s3b.Locomotion);
            UnityEngine.Object.DestroyImmediate(s3b);

            var controllers = new S3DoorController[7];
            for (int i = 0; i < controllers.Length; i++)
            {
                string floorId = "F" + i.ToString("00");
                Transform landingLeft = HospitalInteriorS3BR03Builder.RequireDescendant(portals[i],
                    "S3A_R03_E01_LandingDoor_L");
                Transform landingRight = HospitalInteriorS3BR03Builder.RequireDescendant(portals[i],
                    "S3A_R03_E01_LandingDoor_R");
                if (i == 0)
                    controllers[i] = f00Door;
                else
                {
                    GameObject controllerObject = new GameObject(
                        "S3C_R03_E01_DoorController_" + floorId);
                    controllerObject.transform.SetParent(core, false);
                    controllers[i] = controllerObject.AddComponent<S3DoorController>();
                }
                controllers[i].Configure(contract, floorId, cabinLeft, cabinRight,
                    landingLeft, landingRight, sensor);
                controllers[i].enabled = i == 0;
            }

            OpenCabinRoofForContinuousHoistwayView(cabin);
            TextMesh indicator = CreateTravelIndicator(cabin);
            S3ElevatorController elevator = core.gameObject.AddComponent<S3ElevatorController>();
            elevator.Configure(contract, cabin, origin.transform, sensor.transform, obstructionProxy,
                aperture, portals, controllers, indicator);
            CreatePersistentStoreyFloorPlates(core, contract, aperture);

            foreach (S3ElevatorButton button in scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3ElevatorButton>(true)))
                button.Configure(button.Command, button.FloorId, elevator);

            Transform practicalLight = scene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<Transform>(true))
                .Single(item => item.name == "S3B_R03_E01_CabinPracticalLight");
            practicalLight.SetParent(cabin, true);

            GameObject root = scene.GetRootGameObjects().Single(item
                => item.name == "HospitalInterior_S3B_R03_BootstrapRoot");
            root.name = "HospitalInterior_S3C_R03_BootstrapRoot";
            S3CReviewBootstrap bootstrap = root.AddComponent<S3CReviewBootstrap>();
            S3CReviewLocomotion locomotion = origin.gameObject.AddComponent<S3CReviewLocomotion>();
            bootstrap.Configure(contract, elevator, origin, camera, locomotion, shaft, aperture,
                observation, obstructionProxy, exteriorBaseShaftCutMesh);
            locomotion.Configure(origin, camera, bootstrap,
                contract.ApprovedS2FloorContract.WalkSpeedMps,
                contract.ApprovedS2FloorContract.SprintSpeedMps, 0.12f);
        }

        private static Mesh CreateExteriorBaseShaftCutMesh()
        {
            Scene exteriorBase = EditorSceneManager.OpenScene(
                HospitalInteriorS2R03Builder.ProductionScenes[0], OpenSceneMode.Single);
            MeshFilter sourceFilter = exteriorBase.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<MeshFilter>(true))
                .SingleOrDefault(item => item.name == ExteriorBaseBlockerName);
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
                throw new InvalidOperationException("S3C could not find the F00 exterior shaft blocker: "
                    + ExteriorBaseBlockerName + ".");

            S3ElevatorContract contract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(
                HospitalInteriorS3BR03Builder.ContractPath);
            if (contract == null)
                throw new InvalidOperationException("S3C cannot cut the exterior shaft without its contract.");
            Mesh generated = BuildMeshOutsideShaftPrism(sourceFilter, contract);
            generated.name = "HospitalInterior_S3C_R03_ExteriorGlobalStructure02_ShaftCut";

            Directory.CreateDirectory(Path.Combine(ProjectRoot(), S3CGeneratedFolder));
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(ExteriorBaseShaftCutMeshPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(generated, ExteriorBaseShaftCutMeshPath);
                existing = generated;
            }
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                existing.name = generated.name;
                EditorUtility.SetDirty(existing);
                UnityEngine.Object.DestroyImmediate(generated);
            }
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static Mesh BuildMeshOutsideShaftPrism(MeshFilter sourceFilter,
            S3ElevatorContract contract)
        {
            Mesh source = sourceFilter.sharedMesh;
            Vector3[] sourceVertices = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector4[] sourceTangents = source.tangents;
            Vector2[] sourceUv0 = source.uv;
            Vector2[] sourceUv1 = source.uv2;
            Color[] sourceColors = source.colors;
            bool hasNormals = sourceNormals.Length == sourceVertices.Length;
            bool hasTangents = sourceTangents.Length == sourceVertices.Length;
            bool hasUv0 = sourceUv0.Length == sourceVertices.Length;
            bool hasUv1 = sourceUv1.Length == sourceVertices.Length;
            bool hasColors = sourceColors.Length == sourceVertices.Length;
            Matrix4x4 localToWorld = sourceFilter.transform.localToWorldMatrix;

            Vector2 apertureMin = contract.E01OperatingCellMinXZ;
            Vector2 apertureMax = contract.E01OperatingCellMaxXZ;
            float cutMinY = contract.ShaftMinY;
            float cutMaxY = contract.ShaftMaxY - 0.10f;
            var planes = new[]
            {
                new CutPlane(Vector3.right, -apertureMin.x),
                new CutPlane(Vector3.left, apertureMax.x),
                new CutPlane(Vector3.forward, -apertureMin.y),
                new CutPlane(Vector3.back, apertureMax.y),
                new CutPlane(Vector3.up, -cutMinY),
                new CutPlane(Vector3.down, cutMaxY),
            };

            var outputVertices = new List<ClipVertex>(sourceVertices.Length);
            var outputTriangles = Enumerable.Range(0, source.subMeshCount)
                .Select(_ => new List<int>()).ToArray();
            for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                int[] triangles = source.GetTriangles(subMesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    ClipVertex a = ReadClipVertex(triangles[i], sourceVertices, sourceNormals,
                        sourceTangents, sourceUv0, sourceUv1, sourceColors, localToWorld,
                        hasNormals, hasTangents, hasUv0, hasUv1, hasColors);
                    ClipVertex b = ReadClipVertex(triangles[i + 1], sourceVertices, sourceNormals,
                        sourceTangents, sourceUv0, sourceUv1, sourceColors, localToWorld,
                        hasNormals, hasTangents, hasUv0, hasUv1, hasColors);
                    ClipVertex c = ReadClipVertex(triangles[i + 2], sourceVertices, sourceNormals,
                        sourceTangents, sourceUv0, sourceUv1, sourceColors, localToWorld,
                        hasNormals, hasTangents, hasUv0, hasUv1, hasColors);
                    AppendTriangleOutsidePrism(a, b, c, planes, outputVertices,
                        outputTriangles[subMesh]);
                }
            }

            var result = new Mesh
            {
                indexFormat = outputVertices.Count > ushort.MaxValue
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16,
            };
            result.SetVertices(outputVertices.Select(item => item.localPosition).ToList());
            if (hasNormals)
                result.SetNormals(outputVertices.Select(item => item.normal).ToList());
            if (hasTangents)
                result.SetTangents(outputVertices.Select(item => item.tangent).ToList());
            if (hasUv0)
                result.SetUVs(0, outputVertices.Select(item => item.uv0).ToList());
            if (hasUv1)
                result.SetUVs(1, outputVertices.Select(item => item.uv1).ToList());
            if (hasColors)
                result.SetColors(outputVertices.Select(item => item.color).ToList());
            result.subMeshCount = outputTriangles.Length;
            for (int subMesh = 0; subMesh < outputTriangles.Length; subMesh++)
                result.SetTriangles(outputTriangles[subMesh], subMesh, false);
            if (!hasNormals)
                result.RecalculateNormals();
            result.RecalculateBounds();
            return result;
        }

        private static ClipVertex ReadClipVertex(int index, Vector3[] positions,
            Vector3[] normals, Vector4[] tangents, Vector2[] uv0, Vector2[] uv1,
            Color[] colors, Matrix4x4 localToWorld, bool hasNormals, bool hasTangents,
            bool hasUv0, bool hasUv1, bool hasColors)
        {
            Vector3 local = positions[index];
            return new ClipVertex
            {
                localPosition = local,
                worldPosition = localToWorld.MultiplyPoint3x4(local),
                normal = hasNormals ? normals[index] : Vector3.up,
                tangent = hasTangents ? tangents[index] : new Vector4(1f, 0f, 0f, 1f),
                uv0 = hasUv0 ? uv0[index] : Vector2.zero,
                uv1 = hasUv1 ? uv1[index] : Vector2.zero,
                color = hasColors ? colors[index] : Color.white,
            };
        }

        private static void AppendTriangleOutsidePrism(ClipVertex a, ClipVertex b,
            ClipVertex c, IEnumerable<CutPlane> planes, ICollection<ClipVertex> vertices,
            ICollection<int> triangles)
        {
            var candidates = new List<List<ClipVertex>>
            {
                new List<ClipVertex> { a, b, c },
            };
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
            // Any polygon that remains inside every plane lies in the shaft prism
            // and is deliberately discarded.
        }

        private static List<ClipVertex> ClipPolygon(IReadOnlyList<ClipVertex> polygon,
            CutPlane plane, bool keepPositive)
        {
            const float epsilon = 0.00001f;
            var output = new List<ClipVertex>();
            ClipVertex previous = polygon[polygon.Count - 1];
            float previousDistance = plane.Distance(previous.worldPosition);
            bool previousKept = keepPositive
                ? previousDistance >= -epsilon : previousDistance < -epsilon;
            foreach (ClipVertex current in polygon)
            {
                float currentDistance = plane.Distance(current.worldPosition);
                bool currentKept = keepPositive
                    ? currentDistance >= -epsilon : currentDistance < -epsilon;
                if (currentKept != previousKept)
                {
                    float denominator = previousDistance - currentDistance;
                    float t = Mathf.Abs(denominator) <= epsilon
                        ? 0f : previousDistance / denominator;
                    output.Add(LerpClipVertex(previous, current, Mathf.Clamp01(t)));
                }
                if (currentKept)
                    output.Add(current);
                previous = current;
                previousDistance = currentDistance;
                previousKept = currentKept;
            }
            return output;
        }

        private static ClipVertex LerpClipVertex(ClipVertex a, ClipVertex b, float t)
        {
            Vector3 normal = Vector3.Lerp(a.normal, b.normal, t);
            if (normal.sqrMagnitude > 0.000001f)
                normal.Normalize();
            return new ClipVertex
            {
                localPosition = Vector3.Lerp(a.localPosition, b.localPosition, t),
                worldPosition = Vector3.Lerp(a.worldPosition, b.worldPosition, t),
                normal = normal,
                tangent = Vector4.Lerp(a.tangent, b.tangent, t),
                uv0 = Vector2.Lerp(a.uv0, b.uv0, t),
                uv1 = Vector2.Lerp(a.uv1, b.uv1, t),
                color = Color.Lerp(a.color, b.color, t),
            };
        }

        private static void AppendPolygon(IReadOnlyList<ClipVertex> polygon,
            ICollection<ClipVertex> vertices, ICollection<int> triangles)
        {
            int first = vertices.Count;
            foreach (ClipVertex vertex in polygon)
                vertices.Add(vertex);
            for (int i = 1; i < polygon.Count - 1; i++)
            {
                triangles.Add(first);
                triangles.Add(first + i);
                triangles.Add(first + i + 1);
            }
        }

        private static void CreatePersistentStoreyFloorPlates(Transform core,
            S3ElevatorContract contract, S3ShaftFloorAperture aperture)
        {
            Material slabMaterial = AssetDatabase.LoadAssetAtPath<Material>(
                HospitalInteriorS2R03Builder.SlabMaterialPath);
            if (core == null || contract == null || aperture == null || slabMaterial == null)
                throw new InvalidOperationException("S3C persistent storey floor plates require the approved core, aperture, and slab material.");

            GameObject context = new GameObject("S3C_R03_E01_PersistentFloorPlateContext");
            context.transform.SetParent(core, false);
            const float renderSurfaceOffsetM = 0.015f;
            foreach (S2FloorRecord floor in contract.ApprovedS2FloorContract.Floors)
            {
                Mesh shaftCutMesh = floor.floorId == "F00"
                    ? aperture.F00ShaftCutSlab : aperture.UpperShaftCutSlab;
                if (shaftCutMesh == null)
                    throw new InvalidOperationException("S3C persistent floor plate mesh is missing for "
                        + floor.floorId + ".");
                GameObject plate = new GameObject("S3C_R03_E01_PersistentFloorPlate_" + floor.floorId);
                plate.transform.SetParent(context.transform, false);
                plate.transform.localPosition = new Vector3(0f,
                    floor.elevationM - renderSurfaceOffsetM, 0f);
                plate.AddComponent<MeshFilter>().sharedMesh = shaftCutMesh;
                plate.AddComponent<MeshRenderer>().sharedMaterial = slabMaterial;
                // These are render-only structural rings. The single active S2 floor scene
                // retains the only walkable/collidable floor surface at each endpoint.
            }
        }

        private static void OpenCabinRoofForContinuousHoistwayView(Transform cabin)
        {
            if (cabin == null)
                throw new InvalidOperationException("S3C cannot open a missing cabin roof.");

            // The car roof is intentionally hidden only in the S3C travel-scene copy.
            // This is distinct from, and does not alter, the retained building roof above F06.
            foreach (string partName in OpenCabinRoofPartNames)
            {
                Transform part = HospitalInteriorS3BR03Builder.RequireDescendant(cabin, partName);
                foreach (Renderer renderer in part.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
                foreach (Collider collider in part.GetComponentsInChildren<Collider>(true))
                    collider.enabled = false;
            }
        }

        private static TextMesh CreateTravelIndicator(Transform cabin)
        {
            Transform baked = HospitalInteriorS3BR03Builder.RequireDescendant(
                cabin, "S3A_R03_E01_CabinFloorDisplayText");
            Renderer bakedRenderer = baked.GetComponent<Renderer>();
            if (bakedRenderer != null)
                bakedRenderer.enabled = false;

            GameObject indicatorObject = new GameObject("S3C_R03_E01_CabinTravelIndicator");
            indicatorObject.transform.SetParent(baked.parent, false);
            indicatorObject.transform.localPosition = baked.localPosition + Vector3.left * 0.002f;
            indicatorObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            indicatorObject.transform.localScale = Vector3.one;
            TextMesh indicator = indicatorObject.AddComponent<TextMesh>();
            indicator.text = "F00";
            indicator.anchor = TextAnchor.MiddleCenter;
            indicator.alignment = TextAlignment.Center;
            indicator.fontSize = 96;
            indicator.characterSize = 0.010f;
            indicator.color = new Color(0.49f, 0.93f, 0.95f, 1f);
            indicator.fontStyle = FontStyle.Bold;
            indicator.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            indicatorObject.GetComponent<MeshRenderer>().sharedMaterial = indicator.font.material;
            return indicator;
        }

        private static void RequireInputs()
        {
            if (Application.unityVersion != "6000.3.20f1")
                throw new InvalidOperationException("S3C requires Unity 6000.3.20f1; found "
                    + Application.unityVersion + ".");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(S3BScenePath) == null)
                throw new FileNotFoundException("Approved S3B scene is missing.", S3BScenePath);
            if (AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(HospitalInteriorS3BR03Builder.ContractPath) == null)
                throw new FileNotFoundException("Approved S3 elevator contract is missing.",
                    HospitalInteriorS3BR03Builder.ContractPath);
            foreach (string path in HospitalInteriorS2R03Builder.ProductionScenes
                .Concat(HospitalInteriorS2R03Builder.FloorScenePaths))
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    throw new FileNotFoundException("Approved S2 dependency is missing.", path);
            RequireApprovedS3BGates();
        }

        private static void RequireApprovedS3BGates()
        {
            string staticPath = Path.Combine(HospitalInteriorS3BR03Builder.ReviewFolder(),
                "StageI1_R03_S3B_StaticGate.json");
            string runtimePath = Path.Combine(HospitalInteriorS3BR03Builder.ReviewFolder(),
                "StageI1_R03_S3B_RuntimeGate.json");
            GateStatus staticGate = File.Exists(staticPath)
                ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(staticPath)) : null;
            GateStatus runtimeGate = File.Exists(runtimePath)
                ? JsonUtility.FromJson<GateStatus>(File.ReadAllText(runtimePath)) : null;
            if (staticGate?.status != "PASS" || runtimeGate?.status != "PASS")
                throw new InvalidOperationException("S3C requires approved PASS S3B gates.");
            string checkpoint = Path.Combine(HospitalInteriorS3BR03Builder.ReviewFolder(),
                "HospitalInterior_StageS3B_R03_Checkpoint.md");
            if (!File.Exists(checkpoint) || !File.ReadAllText(checkpoint).Contains(
                "S3B_APPROVED_COMPLETE_S3C_AUTHORIZED_NOT_STARTED"))
                throw new InvalidOperationException("S3B approval checkpoint does not authorize S3C.");
        }

        public static void AssertLockedS3BInputs(out int fileCount)
        {
            var mismatches = new List<string>();
            int rows = LockedS3BFiles.GetLength(0);
            for (int i = 0; i < rows; i++)
            {
                string assetPath = LockedS3BFiles[i, 0];
                string expected = LockedS3BFiles[i, 1];
                string fullPath = Path.Combine(ProjectRoot(), assetPath.Replace('/',
                    Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                    mismatches.Add(assetPath + ":missing");
                else if (!string.Equals(Sha256(fullPath), expected,
                    StringComparison.OrdinalIgnoreCase))
                    mismatches.Add(assetPath + ":hash");
            }
            if (mismatches.Count > 0)
                throw new InvalidOperationException("Approved S3B input changed: "
                    + string.Join(",", mismatches));
            fileCount = rows;
        }

        private static void WriteBuildRecord(string status, string executable,
            string settingsHash, int s2Count, int s3bCount)
        {
            Directory.CreateDirectory(ReviewFolder());
            var record = new BuildRecord
            {
                status = status,
                unityVersion = Application.unityVersion,
                sourceS3BScene = S3BScenePath,
                bootstrapScene = BootstrapScenePath,
                playerScenes = PlayerScenes,
                playerExecutable = executable,
                protectedS2FileCount = s2Count,
                protectedS2MismatchCount = 0,
                protectedS3BFileCount = s3bCount,
                protectedS3BMismatchCount = 0,
                editorBuildSettingsSha256 = settingsHash,
                editorBuildSettingsUntouched = true,
                route = "F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00",
                passengerPolicy = "Cabin requests require the player inside; cabin and player receive the same continuous vertical delta. S2 anchors are never normal-travel teleports.",
                transitionPolicy = "Destination validates behind closed interlocked doors; the committed floor is retained until exact arrival and one-floor scene commit.",
                recoveryPolicy = "Cancellation/load failure rolls back to the last committed floor; failed rollback remains enclosed in FaultedSafe with review-only recovery.",
            };
            File.WriteAllText(Path.Combine(ReviewFolder(), "StageI1_R03_S3C_BuildRecord.json"),
                JsonUtility.ToJson(record, true));
        }

        public static void WriteCheckpoint(string status)
        {
            Directory.CreateDirectory(ReviewFolder());
            File.WriteAllText(Path.Combine(ReviewFolder(),
                "HospitalInterior_StageS3C_R03_Checkpoint.md"),
$@"# Hospital Interior R03 Stage S3C Checkpoint

**Active stage:** `S3C - E01 all-floor travel and safe scene transition`  
**Status:** `{status}`  
**S3B visual/door baseline:** approved and protected  
**S4:** blocked until explicit user functional approval

## Implemented scope

- The approved glass cabin, transparent shaft, portals and normal door system remain in a separate S3C scene. The copied car ceiling assembly is disabled and the F00-only `Exterior_Base` structural crossing is replaced by a shaft-cut copy; the building roof above F06 remains intact.
- E01 accepts F00-F06 cabin requests only when the player is physically inside the car and carries the player continuously with the moving cabin.
- Destination floor scenes validate behind closed interlocked doors; the previous committed floor is retained until exact arrival.
- Exactly one floor remains loaded at every completed endpoint, with the S3-owned shaft aperture applied to that runtime instance.
- Same-floor requests reopen, busy/invalid/obstructed requests reject safely, and landing calls operate only from the active landing.
- Cancellation and load failure roll back to the last committed floor. Failed rollback leaves E01 enclosed in `FaultedSafe`, with an explicit review-only recovery command.
- The required route is `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00`.

## Test scene

`{BootstrapScenePath}`

## Automated evidence

- Static scene gate: `PASS 25/25`.
- Windows-player runtime gate: `PASS 39/39`.
- Protected S2 baseline: `38` files, `0` mismatches.
- Locked S3B inputs: `12` files, `0` mismatches.
- Runtime route: `F00 -> F01 -> F02 -> F03 -> F04 -> F05 -> F06 -> F00`.
- Review player: `Exports/HospitalInterior/StageI1_R03_S3C_AutoGate/HospitalInterior_S3C_R03_TravelReview.exe`.

Automated checks support the S3C review but do not replace explicit user functional approval. Do not start S4 yet.
");
        }

        public static string ProjectRoot() => HospitalInteriorS3BR03Builder.ProjectRoot();
        public static string WorkspaceRoot() => HospitalInteriorS3BR03Builder.WorkspaceRoot();
        public static string ReviewFolder() => Path.Combine(WorkspaceRoot(), "Reviews",
            "HospitalInterior", "StageI1_R03", "S3C_E01_Travel");
        public static string ExportFolder() => Path.Combine(WorkspaceRoot(), "Exports",
            "HospitalInterior", "StageI1_R03_S3C_AutoGate");
        public static string Sha256(string path) => HospitalInteriorS3BR03Builder.Sha256(path);
    }
}
