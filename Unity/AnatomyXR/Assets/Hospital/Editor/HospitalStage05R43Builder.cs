using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CutMyBodyPlease.Hospital.Stage05;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

namespace CutMyBodyPlease.Hospital.Stage05.Editor
{
    /// <summary>R43 door, collision, scale and profiling prototype builder.</summary>
    public static class HospitalStage05R43Builder
    {
        private const string HospitalRoot = "Assets/Hospital";
        private const string ZonePrefabFolder = HospitalRoot + "/Prefabs/Zones";
        private const string DoorPrefabFolder = HospitalRoot + "/Prefabs/Doors";
        private const string CollisionPrefabFolder = HospitalRoot + "/Prefabs/Collision";
        private const string AdditiveFolder = HospitalRoot + "/Scenes/Additive";
        private const string SandboxFolder = HospitalRoot + "/Scenes/IntegrationSandbox";
        private const string SettingsFolder = HospitalRoot + "/Settings";
        private const string CollisionPrefabPath = CollisionPrefabFolder + "/HospitalExterior_R43_CollisionPrototype.prefab";
        private const string TraversalScenePath = SandboxFolder + "/Hospital_R43_TraversalHarness.unity";
        private const string InteractionScenePath = AdditiveFolder + "/Exterior_Interactions.unity";
        private const string BaseScenePath = AdditiveFolder + "/Exterior_Base.unity";
        private const string R42GateRelative = "Reviews/HospitalExterior/Stage05_R42/Stage05_R42_UnityGate.json";

        private static readonly string[] ExpectedDoorRoots =
        {
            "UE_S03B_DOOR_INNER_SlidingLeaf_01_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_03_ROOT",
            "UE_S03B_DOOR_INNER_SlidingLeaf_04_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_01_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_02_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_03_ROOT",
            "UE_S03B_DOOR_OUTER_SlidingLeaf_04_ROOT",
            "UE_S03C_REAR_FireExit_B_Leaf_ROOT",
            "UE_S03C_REAR_LoadingDoor_Leaf_ROOT",
            "UE_S03C_REAR_ServiceDoor_A_Leaf_ROOT",
            "UE_S03C_ROOF_AccessDoor_Leaf_ROOT",
        };

        private static readonly string[] StaticZoneNames =
        {
            "EXT_Balconies", "EXT_FrontTower", "EXT_GlobalStructure", "EXT_GroundEntrance",
            "EXT_InteriorShell", "EXT_LeftFacadePodium", "EXT_RearService", "EXT_RightFacadeWing", "EXT_RoofService",
        };

        private static readonly string[] ExteriorOnlyZones =
        {
            "EXT_Balconies", "EXT_FrontTower", "EXT_GlobalStructure", "EXT_GroundEntrance",
            "EXT_LeftFacadePodium", "EXT_RearService", "EXT_RightFacadeWing", "EXT_RoofService",
        };

        private static readonly StaticEditorFlags OpaqueStaticFlags =
            StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;

        private static readonly CollisionSpec[] CollisionSpecs = BuildCollisionSpecs();

        [MenuItem("Hospital/Stage 5/Build and validate R43")]
        public static void BuildAndValidateR43()
        {
            try
            {
                string workspace = WorkspaceRoot();
                string reviews = R43Reviews(workspace);
                Directory.CreateDirectory(reviews);
                EnsureFolders();
                RequireR42Pass(workspace);

                ApplyVerifiedStaticFlags();
                BuildDoorPrefabs();
                BuildInteractionScene();
                BuildCollisionPrefab();
                IntegrateCollisionWithBaseScene();
                BuildTraversalHarness();
                WriteDoorRootReport(reviews);
                WriteCollisionMap(reviews);
                WriteTraversalReport(reviews);
                CaptureProfilerBaseline(reviews);

                R43GateReport gate = ValidateR43(workspace);
                WriteJson(gate, Path.Combine(reviews, "Stage05_R43_UnityGate.json"));
                WriteJson(gate, AssetPathToAbsolute(SettingsFolder + "/Stage05_R43_UnityGate.json"));
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.SaveAssets();
                if (!gate.pass)
                    throw new BuildFailedException("Stage 5 R43 gate failed. See Stage05_R43_UnityGate.json.");
                Debug.Log("STAGE05_R43_GATE=PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                throw;
            }
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { DoorPrefabFolder, CollisionPrefabFolder, SandboxFolder, SettingsFolder })
                Directory.CreateDirectory(AssetPathToAbsolute(path));
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void RequireR42Pass(string workspace)
        {
            if (!IsR42Pass(workspace))
                throw new InvalidOperationException("R43 requires a passing frozen R42 gate.");
        }

        private static bool IsR42Pass(string workspace)
        {
            string path = Path.Combine(workspace, R42GateRelative.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) return false;
            GateStatus status = JsonUtility.FromJson<GateStatus>(File.ReadAllText(path));
            return status != null && status.pass && status.status == "PASS";
        }

        private static void ApplyVerifiedStaticFlags()
        {
            foreach (string zone in StaticZoneNames.Concat(new[] { "ACT_InteractiveDoors" }))
            {
                string path = ZonePrefabFolder + "/" + zone + ".prefab";
                GameObject contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (Renderer renderer in contents.GetComponentsInChildren<Renderer>(true))
                    {
                        bool transparent = renderer.sharedMaterials.Any(IsTransparent);
                        StaticEditorFlags flags = zone == "ACT_InteractiveDoors" || transparent
                            ? 0 : OpaqueStaticFlags;
                        GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, flags);
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
            AssetDatabase.SaveAssets();
        }

        private static bool IsTransparent(Material material) =>
            material != null && material.HasProperty("_Surface") && material.GetFloat("_Surface") > 0.5f;

        private static void BuildDoorPrefabs()
        {
            GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ZonePrefabFolder + "/ACT_InteractiveDoors.prefab");
            if (sourcePrefab == null) throw new InvalidOperationException("ACT_InteractiveDoors zone prefab is missing.");
            GameObject sourceInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            try
            {
                Dictionary<string, Transform> roots = sourceInstance.GetComponentsInChildren<Transform>(true)
                    .Where(transform => ExpectedDoorRoots.Contains(transform.name))
                    .ToDictionary(transform => transform.name);
                if (roots.Count != 12) throw new InvalidOperationException("The source zone does not contain all twelve door roots.");

                float innerCenter = roots.Where(pair => pair.Key.Contains("INNER_SlidingLeaf"))
                    .Average(pair => pair.Value.position.x);
                float outerCenter = roots.Where(pair => pair.Key.Contains("OUTER_SlidingLeaf"))
                    .Average(pair => pair.Value.position.x);

                foreach (string rootName in ExpectedDoorRoots)
                {
                    Transform sourceRoot = roots[rootName];
                    GameObject wrapper = new GameObject(DoorWrapperName(rootName));
                    try
                    {
                        GameObject leaf = UnityObject.Instantiate(sourceRoot.gameObject);
                        leaf.name = rootName;
                        leaf.transform.SetParent(wrapper.transform, true);
                        wrapper.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        wrapper.transform.localScale = Vector3.one;
                        SetDynamicHierarchy(wrapper);

                        Bounds localBounds = RendererBoundsInLocal(leaf.transform);
                        BoxCollider collider = leaf.AddComponent<BoxCollider>();
                        collider.center = localBounds.center;
                        collider.size = ClampSize(localBounds.size, 0.03f);
                        collider.isTrigger = false;
                        Rigidbody body = leaf.AddComponent<Rigidbody>();
                        body.isKinematic = true;
                        body.useGravity = false;
                        body.interpolation = RigidbodyInterpolation.Interpolate;
                        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                        string motionType;
                        Vector3 axis;
                        float distance;
                        float angle;
                        float clearance;
                        if (rootName.Contains("SlidingLeaf"))
                        {
                            motionType = "HorizontalSlide";
                            float center = rootName.Contains("INNER_") ? innerCenter : outerCenter;
                            axis = leaf.transform.position.x < center ? Vector3.left : Vector3.right;
                            distance = Mathf.Max(localBounds.size.x, localBounds.size.z) + 0.15f;
                            angle = 0.0f;
                            clearance = distance;
                        }
                        else if (rootName.Contains("LoadingDoor"))
                        {
                            motionType = "VerticalLift";
                            axis = leaf.transform.InverseTransformDirection(Vector3.up).normalized;
                            distance = RendererBoundsWorld(leaf).size.y + 0.15f;
                            angle = 0.0f;
                            clearance = distance;
                        }
                        else
                        {
                            motionType = "Hinged";
                            axis = leaf.transform.InverseTransformDirection(Vector3.up).normalized;
                            distance = 0.0f;
                            angle = 90.0f;
                            clearance = Mathf.Max(localBounds.size.x, localBounds.size.z) + 0.10f;
                        }

                        HospitalDoorPrototype metadata = leaf.AddComponent<HospitalDoorPrototype>();
                        metadata.Configure(rootName, motionType, leaf.transform.localPosition,
                            leaf.transform.localRotation, axis, distance, angle, clearance);
                        PrefabUtility.SaveAsPrefabAsset(wrapper, DoorPrefabPath(rootName));
                    }
                    finally
                    {
                        UnityObject.DestroyImmediate(wrapper);
                    }
                }
            }
            finally
            {
                UnityObject.DestroyImmediate(sourceInstance);
            }

            string[] prefabs = Directory.GetFiles(AssetPathToAbsolute(DoorPrefabFolder), "*.prefab");
            if (prefabs.Length != 12) throw new InvalidOperationException("R43 requires exactly twelve door prefabs.");
            AssetDatabase.SaveAssets();
        }

        private static void SetDynamicHierarchy(GameObject root)
        {
            int layer = LayerMask.NameToLayer("HospitalDynamic");
            if (layer < 0) throw new InvalidOperationException("HospitalDynamic layer is missing.");
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = layer;
                GameObjectUtility.SetStaticEditorFlags(transform.gameObject, 0);
            }
        }

        private static void BuildInteractionScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string rootName in ExpectedDoorRoots)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath(rootName));
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.name = DoorWrapperName(rootName);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
            }
            EditorSceneManager.SaveScene(scene, InteractionScenePath);
        }

        private static CollisionSpec[] BuildCollisionSpecs()
        {
            var specs = new List<CollisionSpec>
            {
                new CollisionSpec("COL_Walkable_ExteriorGround", "walkable_ground", new Vector3(0f, -0.15f, -5.5f), new Vector3(80f, 0.30f, 58f)),
                new CollisionSpec("COL_Walkable_EntranceApron", "walkable_entrance", new Vector3(0f, 0.02f, -26f), new Vector3(24f, 0.08f, 12f)),
                new CollisionSpec("COL_EntranceThreshold", "entrance_threshold", new Vector3(0f, 0.05f, -17.7f), new Vector3(8.4f, 0.10f, 0.24f)),
                new CollisionSpec("COL_Perimeter_Front", "unsafe_perimeter", new Vector3(0f, 1.0f, -34.1f), new Vector3(80f, 2.0f, 0.20f)),
                new CollisionSpec("COL_Perimeter_Rear", "unsafe_perimeter", new Vector3(0f, 1.0f, 22.2f), new Vector3(80f, 2.0f, 0.20f)),
                new CollisionSpec("COL_Perimeter_Left", "unsafe_perimeter", new Vector3(-40.1f, 1.0f, -6f), new Vector3(0.20f, 2.0f, 56f)),
                new CollisionSpec("COL_Perimeter_Right", "unsafe_perimeter", new Vector3(40.1f, 1.0f, -6f), new Vector3(0.20f, 2.0f, 56f)),
                new CollisionSpec("COL_ServiceBoundary_LoadingWest", "service_boundary", new Vector3(16f, 1.0f, 20f), new Vector3(0.25f, 2.0f, 4f)),
                new CollisionSpec("COL_ServiceBoundary_LoadingEast", "service_boundary", new Vector3(36f, 1.0f, 20f), new Vector3(0.25f, 2.0f, 4f)),
                new CollisionSpec("COL_ServiceBoundary_LoadingRear", "service_boundary", new Vector3(26f, 1.0f, 21.8f), new Vector3(20f, 2.0f, 0.25f)),
            };
            for (int floor = 0; floor < 6; floor++)
            {
                int level = floor + 1;
                float y = 5.65f + floor * 3.9f;
                specs.Add(new CollisionSpec($"COL_Balcony_L{level}_Floor", "walkable_balcony",
                    new Vector3(-29.9875f, y, -10.8875f), new Vector3(12.0f, 0.12f, 12.0f)));
                specs.Add(new CollisionSpec($"COL_Balcony_L{level}_OuterGuard", "unsafe_balcony_edge",
                    new Vector3(-29.9875f, y + 0.65f, -16.90f), new Vector3(12.0f, 1.30f, 0.12f)));
                specs.Add(new CollisionSpec($"COL_Balcony_L{level}_WestGuard", "unsafe_balcony_edge",
                    new Vector3(-36.02f, y + 0.65f, -10.8875f), new Vector3(0.12f, 1.30f, 12.0f)));
            }
            return specs.ToArray();
        }

        private static void BuildCollisionPrefab()
        {
            GameObject root = new GameObject("R43_CollisionPrototype");
            try
            {
                int layer = LayerMask.NameToLayer("HospitalCollision");
                if (layer < 0) throw new InvalidOperationException("HospitalCollision layer is missing.");
                foreach (CollisionSpec spec in CollisionSpecs)
                {
                    GameObject item = new GameObject(spec.name);
                    item.layer = layer;
                    item.transform.SetParent(root.transform, false);
                    item.transform.localPosition = spec.center;
                    BoxCollider collider = item.AddComponent<BoxCollider>();
                    collider.center = Vector3.zero;
                    collider.size = spec.size;
                    collider.isTrigger = false;
                    GameObjectUtility.SetStaticEditorFlags(item, 0);
                }
                PrefabUtility.SaveAsPrefabAsset(root, CollisionPrefabPath);
            }
            finally
            {
                UnityObject.DestroyImmediate(root);
            }
        }

        private static void IntegrateCollisionWithBaseScene()
        {
            Scene scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);
            foreach (GameObject existing in scene.GetRootGameObjects().Where(root => root.name == "R43_CollisionPrototype"))
                UnityObject.DestroyImmediate(existing);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollisionPrefabPath);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = "R43_CollisionPrototype";
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            EditorSceneManager.SaveScene(scene, BaseScenePath);
        }

        private static void BuildTraversalHarness()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            foreach (string zone in StaticZoneNames) InstantiateZone(zone, scene);
            foreach (string rootName in ExpectedDoorRoots)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath(rootName));
                PrefabUtility.InstantiatePrefab(prefab, scene);
            }
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CollisionPrefabPath), scene);

            GameObject originObject = new GameObject("R43_TEMP_XR_ORIGIN");
            originObject.transform.position = new Vector3(4.0f, 0.0f, -30.0f);
            XROrigin xrOrigin = originObject.AddComponent<XROrigin>();
            GameObject floorOffset = new GameObject("CameraFloorOffset");
            floorOffset.transform.SetParent(originObject.transform, false);
            GameObject cameraObject = new GameObject("Eye_1_70m");
            cameraObject.transform.SetParent(floorOffset.transform, false);
            cameraObject.transform.localPosition = new Vector3(0.0f, 1.7f, 0.0f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            xrOrigin.CameraFloorOffsetObject = floorOffset;
            xrOrigin.Camera = camera;
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            GameObject routeRoot = new GameObject("R43_ROUTE_REFERENCES");
            Transform[] routes =
            {
                CreateRoute(routeRoot.transform, "EntranceApproach", new Vector3(0f, 1.7f, -24f)),
                CreateRoute(routeRoot.transform, "CanopyClearance_4_20m", new Vector3(0f, 4.2f, -17f)),
                CreateRoute(routeRoot.transform, "BalconyLevel1Access", new Vector3(-30f, 6.50f, -11f)),
                CreateRoute(routeRoot.transform, "ExteriorLeftRoute", new Vector3(-20f, 1.7f, -26f)),
                CreateRoute(routeRoot.transform, "LoadingServiceLimit", new Vector3(26f, 1.7f, 18f)),
            };
            HospitalScaleHarness harness = originObject.AddComponent<HospitalScaleHarness>();
            harness.Configure(1.7f, cameraObject.transform, routes);
            EditorSceneManager.SaveScene(scene, TraversalScenePath);
        }

        private static Transform CreateRoute(Transform parent, string name, Vector3 position)
        {
            GameObject item = new GameObject(name);
            item.transform.SetParent(parent, false);
            item.transform.position = position;
            return item.transform;
        }

        private static GameObject InstantiateZone(string zone, Scene scene)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone + ".prefab");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            instance.name = zone;
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            return instance;
        }

        private static void WriteDoorRootReport(string reviews)
        {
            var entries = new List<DoorRecord>();
            foreach (string rootName in ExpectedDoorRoots)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath(rootName));
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    HospitalDoorPrototype door = instance.GetComponentInChildren<HospitalDoorPrototype>(true);
                    BoxCollider collider = door.GetComponent<BoxCollider>();
                    Rigidbody body = door.GetComponent<Rigidbody>();
                    entries.Add(new DoorRecord
                    {
                        root = rootName,
                        prefab = DoorPrefabPath(rootName),
                        motion_type = door.MotionType,
                        closed_local_position = Values(door.ClosedLocalPosition),
                        closed_local_rotation = Values(door.ClosedLocalRotation),
                        pivot_world_position_at_common_origin = Values(door.transform.position),
                        motion_axis = Values(door.MotionAxis),
                        open_distance_m = door.OpenDistanceMeters,
                        open_angle_degrees = door.OpenAngleDegrees,
                        verified_clearance_m = door.VerifiedClearanceMeters,
                        collider_center = Values(collider.center),
                        collider_size = Values(collider.size),
                        kinematic_rigidbody = body != null && body.isKinematic,
                        renderer_count = door.GetComponentsInChildren<Renderer>(true).Length,
                    });
                }
                finally
                {
                    UnityObject.DestroyImmediate(instance);
                }
            }
            WriteJson(new DoorReport
            {
                schema = "HospitalExterior.Stage05.R43.DoorRootReport.v1",
                source_zone = "ACT_InteractiveDoors",
                doors = entries.ToArray(),
            }, Path.Combine(reviews, "Stage05_R43_DoorRootReport.json"));
        }

        private static void WriteCollisionMap(string reviews)
        {
            WriteJson(new CollisionMap
            {
                schema = "HospitalExterior.Stage05.R43.CollisionMap.v1",
                layer = "HospitalCollision",
                static_box_colliders = CollisionSpecs.Select(spec => new CollisionRecord
                {
                    name = spec.name, role = spec.role, center = Values(spec.center), size = Values(spec.size), collider = "BoxCollider",
                }).ToArray(),
                dynamic_door_box_colliders = ExpectedDoorRoots,
                mesh_colliders = 0,
                decorative_geometry_colliders = 0,
            }, Path.Combine(reviews, "Stage05_R43_CollisionMap.json"));
        }

        private static void WriteTraversalReport(string reviews)
        {
            Scene scene = EditorSceneManager.OpenScene(TraversalScenePath, OpenSceneMode.Single);
            HospitalScaleHarness harness = UnityObject.FindFirstObjectByType<HospitalScaleHarness>();
            XROrigin origin = harness.GetComponent<XROrigin>();
            BoxCollider[] staticColliders = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<BoxCollider>(true))
                .Where(collider => collider.gameObject.layer == LayerMask.NameToLayer("HospitalCollision")).ToArray();
            var routes = new List<RouteRecord>();
            foreach (Transform route in harness.RouteReferences)
            {
                bool canopy = route.name.StartsWith("CanopyClearance", StringComparison.Ordinal);
                bool balcony = route.name == "BalconyLevel1Access";
                bool supported = canopy || staticColliders.Any(collider =>
                    IsWalkableCollider(collider.name) && HorizontalContains(collider.bounds, route.position));
                bool blocked = staticColliders.Any(collider =>
                    IsBoundaryCollider(collider.name) && collider.bounds.Contains(new Vector3(route.position.x, 1.0f, route.position.z)));
                routes.Add(new RouteRecord
                {
                    name = route.name,
                    position = Values(route.position),
                    walkable_or_clearance_supported = supported,
                    blocked_by_boundary = blocked,
                });
            }
            WriteJson(new TraversalReport
            {
                schema = "HospitalExterior.Stage05.R43.TraversalScaleReport.v1",
                scene = TraversalScenePath,
                temporary_harness = true,
                target = harness.Target,
                xr_origin_component = origin != null,
                eye_height_m = harness.EyeReference.position.y - harness.transform.position.y,
                disabled_test_camera = origin != null && origin.Camera != null && !origin.Camera.enabled,
                routes = routes.ToArray(),
                note = "Scale/traversal harness only; no final locomotion or gameplay controller is included.",
            }, Path.Combine(reviews, "Stage05_R43_TraversalScaleReport.json"));
        }

        private static bool IsWalkableCollider(string name) =>
            name.Contains("Walkable") || name.Contains("Floor") || name.Contains("Threshold");

        private static bool IsBoundaryCollider(string name) =>
            name.Contains("Perimeter") || name.Contains("Boundary") || name.Contains("Guard");

        private static bool HorizontalContains(Bounds bounds, Vector3 point) =>
            point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;

        private static void CaptureProfilerBaseline(string reviews)
        {
            var states = new[]
            {
                CaptureProfileState("ExteriorOnly", ExteriorOnlyZones, false),
                CaptureProfileState("ExteriorPlusInteriorShell", ExteriorOnlyZones.Concat(new[] { "EXT_InteriorShell" }).ToArray(), false),
                CaptureProfileState("DoorInteraction", ExteriorOnlyZones.Concat(new[] { "EXT_InteriorShell" }).ToArray(), true),
            };
            var record = new ProfilerBaseline
            {
                schema = "HospitalExterior.Stage05.R43.ProfilerBaseline.v1",
                selected_platform = "Windows x86_64 PC VR",
                headset_scope = "Meta Quest 2 / Quest 3 via Quest Link or Air Link",
                measurement_context = "Unity Editor D3D11 single-view offscreen proxy at 1832x1920; no active headset runtime",
                acceptance_refresh_hz = 72,
                acceptance_frame_budget_ms = 13.89f,
                preferred_refresh_hz = 90,
                preferred_frame_budget_ms = 11.11f,
                graphics_device = SystemInfo.graphicsDeviceName,
                graphics_api = SystemInfo.graphicsDeviceVersion,
                graphics_memory_mb = SystemInfo.graphicsMemorySize,
                processor = SystemInfo.processorType,
                system_memory_mb = SystemInfo.systemMemorySize,
                headset_profile_required = true,
                headset_profile_note = "Editor proxy is attributable and repeatable but is not a stereo Quest Link/Air Link performance claim. Capture headset CPU/GPU timing before Stage 6 acceptance.",
                states = states,
            };
            WriteJson(record, Path.Combine(reviews, "Stage05_R43_ProfilerBaseline.json"));
        }

        private static ProfileState CaptureProfileState(string stateName, string[] zones, bool includeDoors)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var roots = new List<GameObject>();
            foreach (string zone in zones) roots.Add(InstantiateZone(zone, scene));
            if (includeDoors)
            {
                foreach (string rootName in ExpectedDoorRoots)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath(rootName));
                    roots.Add((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene));
                }
            }

            Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            long triangles = renderers.Sum(renderer =>
            {
                Mesh mesh = MeshForRenderer(renderer);
                return mesh == null ? 0L : TriangleCount(mesh);
            });
            Renderer[] transparent = renderers.Where(renderer => renderer.sharedMaterials.Any(IsTransparent)).ToArray();
            long transparentTriangles = transparent.Sum(renderer =>
            {
                Mesh mesh = MeshForRenderer(renderer);
                return mesh == null ? 0L : TriangleCount(mesh);
            });

            GameObject rig = new GameObject("R43_TEMP_PROFILE_RIG");
            Camera camera = new GameObject("R43_TEMP_PROFILE_CAMERA").AddComponent<Camera>();
            camera.transform.SetParent(rig.transform, false);
            Light light = new GameObject("R43_TEMP_PROFILE_LIGHT").AddComponent<Light>();
            light.transform.SetParent(rig.transform, false);
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.68f, 0.74f, 0.80f, 1f);
            camera.fieldOfView = 52f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 500f;
            Bounds bounds = CombinedBounds(roots);
            camera.transform.position = bounds.center + new Vector3(-0.65f, 0.35f, -1f).normalized * 92f;
            camera.transform.LookAt(bounds.center + Vector3.up * 2f);

            var target = new RenderTexture(1832, 1920, 24, RenderTextureFormat.ARGB32);
            if (!target.Create()) throw new InvalidOperationException("Failed to create R43 profiling render target.");
            camera.targetTexture = target;
            for (int index = 0; index < 8; index++) camera.Render();
            var samples = new List<double>();
            var stopwatch = new Stopwatch();
            for (int index = 0; index < 24; index++)
            {
                stopwatch.Restart();
                camera.Render();
                stopwatch.Stop();
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            uint timingCount = FrameTimingManager.GetLatestTimings(1, timings);
            bool gpuAvailable = timingCount > 0 && timings[0].gpuFrameTime > 0.0;
            double[] sorted = samples.OrderBy(value => value).ToArray();
            double median = sorted[sorted.Length / 2];
            double p95 = sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * 0.95f) - 1, 0, sorted.Length - 1)];
            int batches = UnityStat("batches");
            int drawCalls = UnityStat("drawCalls");
            int setPass = UnityStat("setPassCalls");
            bool countersAvailable = batches > 0 && drawCalls > 0 && setPass > 0;
            bool countersEstimated = false;
            string counterSource = "UnityEditor.UnityStats after offscreen camera render";
            if (!countersAvailable)
            {
                drawCalls = renderers.Sum(renderer =>
                {
                    Mesh mesh = MeshForRenderer(renderer);
                    return mesh == null ? 0 : mesh.subMeshCount;
                });
                batches = drawCalls;
                setPass = renderers.SelectMany(renderer => renderer.sharedMaterials)
                    .Where(material => material != null).Distinct().Count();
                countersEstimated = true;
                counterSource = "Deterministic raw submesh/material estimate: draw calls and batches are pre-batching upper bounds; SetPass is the unique-material lower bound because UnityStats returned zero in batch mode.";
            }

            camera.targetTexture = null;
            target.Release();
            UnityObject.DestroyImmediate(target);
            UnityObject.DestroyImmediate(rig);
            foreach (GameObject root in roots) UnityObject.DestroyImmediate(root);

            bool budgetPass = median <= 13.89;
            return new ProfileState
            {
                state = stateName,
                renderer_count = renderers.Length,
                triangle_count = triangles,
                transparent_renderer_count = transparent.Length,
                transparent_triangle_count = transparentTriangles,
                draw_calls = drawCalls,
                batches = batches,
                set_pass_calls = setPass,
                render_counter_available = countersAvailable,
                render_counter_estimated = countersEstimated,
                render_counter_source = counterSource,
                cpu_render_median_ms = (float)median,
                cpu_render_p95_ms = (float)p95,
                cpu_samples = samples.Count,
                gpu_frame_time_available = gpuAvailable,
                gpu_frame_time_ms = gpuAvailable ? (float)timings[0].gpuFrameTime : -1f,
                allocated_memory_bytes = Profiler.GetTotalAllocatedMemoryLong(),
                reserved_memory_bytes = Profiler.GetTotalReservedMemoryLong(),
                cpu_proxy_within_72hz_budget = budgetPass,
                performance_disposition = budgetPass
                    ? "Editor single-view CPU render proxy is within 13.89 ms; headset stereo CPU/GPU capture remains required."
                    : "Editor single-view CPU render proxy exceeds 13.89 ms; result is documented for Stage 6 optimization and headset validation.",
                transparent_overdraw_observation =
                    $"{transparent.Length} transparent renderers / {transparentTriangles} transparent triangles; no stacked-material regression observed in R42 QA. Headset GPU overdraw capture remains required.",
            };
        }

        private static int UnityStat(string propertyName)
        {
            Type type = typeof(EditorWindow).Assembly.GetType("UnityEditor.UnityStats");
            PropertyInfo property = type?.GetProperty(propertyName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null) return -1;
            object value = property.GetValue(null);
            return value == null ? -1 : Convert.ToInt32(value);
        }

        private static R43GateReport ValidateR43(string workspace)
        {
            string reviews = R43Reviews(workspace);
            var report = new R43GateReport
            {
                schema = "HospitalExterior.Stage05.R43.UnityGate.v1",
                status = "FAIL",
                checks = new List<GateCheck>(),
                doors = new List<DoorGateMeasurement>(),
            };
            AddCheck(report, "r42_prerequisite", IsR42Pass(workspace), R42GateRelative);
            ValidateDoors(report);
            ValidateCollision(report);
            ValidateStaticFlags(report);
            ValidateTraversal(report, reviews);
            ValidateProfiles(report, reviews);
            report.pass = report.checks.All(check => check.pass);
            report.status = report.pass ? "PASS" : "FAIL";
            return report;
        }

        private static void ValidateDoors(R43GateReport report)
        {
            string[] prefabFiles = Directory.GetFiles(AssetPathToAbsolute(DoorPrefabFolder), "*.prefab");
            AddCheck(report, "twelve_door_prefabs", prefabFiles.Length == 12,
                string.Join(",", prefabFiles.Select(Path.GetFileName).OrderBy(value => value)));
            var foundRoots = new List<string>();
            foreach (string expected in ExpectedDoorRoots)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoorPrefabPath(expected));
                bool exists = prefab != null;
                AddCheck(report, expected + "_prefab_exists", exists, DoorPrefabPath(expected));
                if (!exists) continue;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    HospitalDoorPrototype door = instance.GetComponentInChildren<HospitalDoorPrototype>(true);
                    BoxCollider[] colliders = instance.GetComponentsInChildren<BoxCollider>(true);
                    Rigidbody[] bodies = instance.GetComponentsInChildren<Rigidbody>(true);
                    Renderer[] doorRenderers = instance.GetComponentsInChildren<Renderer>(true);
                    bool wrapperZero = ZeroTransform(instance);
                    bool hierarchy = door != null && door.name == expected && door.SourceRootName == expected;
                    bool dynamic = instance.GetComponentsInChildren<Transform>(true).All(transform =>
                        !transform.gameObject.isStatic && transform.gameObject.layer == LayerMask.NameToLayer("HospitalDynamic"));
                    bool physics = colliders.Length == 1 && bodies.Length == 1 && bodies[0].isKinematic &&
                                   colliders[0].size.x > 0.02f && colliders[0].size.y > 0.02f && colliders[0].size.z > 0.02f;
                    bool closed = door != null && Approximately(door.transform.localPosition, door.ClosedLocalPosition, 0.0001f) &&
                                  Quaternion.Angle(door.transform.localRotation, door.ClosedLocalRotation) < 0.01f;
                    bool clearance = door != null && door.VerifiedClearanceMeters >= 0.5f &&
                                     ((door.MotionType == "Hinged" && Mathf.Approximately(door.OpenAngleDegrees, 90f)) ||
                                      (door.MotionType != "Hinged" && door.OpenDistanceMeters > 0.5f));
                    bool pass = wrapperZero && hierarchy && dynamic && physics && closed && clearance && doorRenderers.Length > 0;
                    AddCheck(report, expected + "_root_pivot_closed_clearance", pass,
                        $"wrapper_zero={wrapperZero}; hierarchy={hierarchy}; dynamic={dynamic}; physics={physics}; closed={closed}; clearance={clearance}; renderers={doorRenderers.Length}");
                    foundRoots.Add(door?.SourceRootName);
                    report.doors.Add(new DoorGateMeasurement
                    {
                        root = expected,
                        motion_type = door?.MotionType,
                        pivot_position = door == null ? null : Values(door.transform.position),
                        clearance_m = door == null ? 0f : door.VerifiedClearanceMeters,
                        collider_size = colliders.Length == 1 ? Values(colliders[0].size) : null,
                        pass = pass,
                    });
                }
                finally
                {
                    UnityObject.DestroyImmediate(instance);
                }
            }
            AddCheck(report, "door_root_inventory_exact", foundRoots.OrderBy(value => value)
                .SequenceEqual(ExpectedDoorRoots.OrderBy(value => value)), string.Join(",", foundRoots));

            Scene scene = EditorSceneManager.OpenScene(InteractionScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            int renderers = roots.Sum(root => root.GetComponentsInChildren<Renderer>(true).Length);
            long triangles = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).Sum(renderer =>
            {
                Mesh mesh = MeshForRenderer(renderer);
                return mesh == null ? 0L : TriangleCount(mesh);
            });
            bool scenePass = roots.Length == 12 && roots.All(ZeroTransform) &&
                             roots.All(root => PrefabUtility.GetPrefabInstanceStatus(root) == PrefabInstanceStatus.Connected) &&
                             renderers == 44 && triangles == 6160 &&
                             roots.Sum(root => root.GetComponentsInChildren<BoxCollider>(true).Length) == 12 &&
                             roots.Sum(root => root.GetComponentsInChildren<Rigidbody>(true).Length) == 12;
            AddCheck(report, "interaction_scene_uses_only_twelve_door_prefabs", scenePass,
                $"roots={roots.Length}; renderers={renderers}; triangles={triangles}; colliders={roots.Sum(root => root.GetComponentsInChildren<BoxCollider>(true).Length)}");
        }

        private static void ValidateCollision(R43GateReport report)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CollisionPrefabPath);
            BoxCollider[] boxes = prefab == null ? Array.Empty<BoxCollider>() : prefab.GetComponentsInChildren<BoxCollider>(true);
            MeshCollider[] meshes = prefab == null ? Array.Empty<MeshCollider>() : prefab.GetComponentsInChildren<MeshCollider>(true);
            Renderer[] renderers = prefab == null ? Array.Empty<Renderer>() : prefab.GetComponentsInChildren<Renderer>(true);
            bool names = boxes.Select(box => box.name).OrderBy(value => value)
                .SequenceEqual(CollisionSpecs.Select(spec => spec.name).OrderBy(value => value));
            bool valid = prefab != null && boxes.Length == 28 && meshes.Length == 0 && renderers.Length == 0 && names &&
                         boxes.All(box => box.gameObject.layer == LayerMask.NameToLayer("HospitalCollision") &&
                                              box.size.x > 0f && box.size.y > 0f && box.size.z > 0f);
            AddCheck(report, "documented_simple_collision_inventory", valid,
                $"box={boxes.Length}; mesh={meshes.Length}; renderers={renderers.Length}; names={names}");

            int geometryColliders = StaticZoneNames.Concat(new[] { "ACT_InteractiveDoors" }).Sum(zone =>
            {
                GameObject zonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone + ".prefab");
                return zonePrefab.GetComponentsInChildren<Collider>(true).Length;
            });
            AddCheck(report, "source_zone_prefabs_remain_collider_free", geometryColliders == 0,
                "colliders=" + geometryColliders);

            Scene baseScene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);
            GameObject[] roots = baseScene.GetRootGameObjects();
            bool integrated = roots.Select(root => root.name).OrderBy(value => value).SequenceEqual(
                                  new[] { "EXT_GlobalStructure", "EXT_GroundEntrance", "R43_CollisionPrototype" }.OrderBy(value => value)) &&
                              roots.Single(root => root.name == "R43_CollisionPrototype")
                                  .GetComponentsInChildren<BoxCollider>(true).Length == 28;
            AddCheck(report, "collision_integrated_only_with_exterior_base", integrated,
                string.Join(",", roots.Select(root => root.name)));
        }

        private static void ValidateStaticFlags(R43GateReport report)
        {
            int opaque = 0;
            int opaqueCorrect = 0;
            int dynamic = 0;
            int dynamicCorrect = 0;
            foreach (string zone in StaticZoneNames.Concat(new[] { "ACT_InteractiveDoors" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ZonePrefabFolder + "/" + zone + ".prefab");
                foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
                {
                    bool shouldBeStatic = zone != "ACT_InteractiveDoors" && !renderer.sharedMaterials.Any(IsTransparent);
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                    if (shouldBeStatic)
                    {
                        opaque++;
                        if ((flags & OpaqueStaticFlags) == OpaqueStaticFlags) opaqueCorrect++;
                    }
                    else
                    {
                        dynamic++;
                        if (flags == 0) dynamicCorrect++;
                    }
                }
            }
            AddCheck(report, "only_verified_opaque_architecture_static",
                opaque > 0 && opaque == opaqueCorrect && dynamic == dynamicCorrect,
                $"opaque={opaqueCorrect}/{opaque}; glass_or_doors_dynamic={dynamicCorrect}/{dynamic}");
        }

        private static void ValidateTraversal(R43GateReport report, string reviews)
        {
            string path = Path.Combine(reviews, "Stage05_R43_TraversalScaleReport.json");
            TraversalReport traversal = File.Exists(path)
                ? JsonUtility.FromJson<TraversalReport>(File.ReadAllText(path)) : null;
            bool pass = traversal != null && traversal.temporary_harness && traversal.xr_origin_component &&
                        Mathf.Abs(traversal.eye_height_m - 1.7f) <= 0.001f && traversal.disabled_test_camera &&
                        traversal.routes != null && traversal.routes.Length == 5 &&
                        traversal.routes.All(route => route.walkable_or_clearance_supported && !route.blocked_by_boundary);
            AddCheck(report, "temporary_xr_scale_and_traversal_harness", pass,
                traversal == null ? "missing" :
                $"eye={traversal.eye_height_m}; xr_origin={traversal.xr_origin_component}; routes={traversal.routes?.Length}; camera_disabled={traversal.disabled_test_camera}");
            bool notInBuild = EditorBuildSettings.scenes.All(scene => scene.path != TraversalScenePath || !scene.enabled);
            AddCheck(report, "temporary_harness_excluded_from_build", notInBuild, TraversalScenePath);
        }

        private static void ValidateProfiles(R43GateReport report, string reviews)
        {
            string path = Path.Combine(reviews, "Stage05_R43_ProfilerBaseline.json");
            ProfilerBaseline profile = File.Exists(path)
                ? JsonUtility.FromJson<ProfilerBaseline>(File.ReadAllText(path)) : null;
            bool states = profile != null && profile.states != null && profile.states.Length == 3 &&
                          profile.states.Select(state => state.state).OrderBy(value => value).SequenceEqual(
                              new[] { "ExteriorOnly", "ExteriorPlusInteriorShell", "DoorInteraction" }.OrderBy(value => value));
            AddCheck(report, "three_profile_states_captured", states,
                profile == null ? "missing" : string.Join(",", profile.states.Select(state => state.state)));
            if (!states) return;
            var expected = new Dictionary<string, Vector2Int>
            {
                { "ExteriorOnly", new Vector2Int(145, 626052) },
                { "ExteriorPlusInteriorShell", new Vector2Int(193, 638172) },
                { "DoorInteraction", new Vector2Int(237, 644332) },
            };
            foreach (ProfileState state in profile.states)
            {
                Vector2Int metrics = expected[state.state];
                bool geometry = state.renderer_count == metrics.x && state.triangle_count == metrics.y;
                bool timing = state.cpu_samples == 24 && state.cpu_render_median_ms > 0f && state.cpu_render_p95_ms > 0f;
                bool counters = (state.render_counter_available || state.render_counter_estimated) &&
                                state.draw_calls > 0 && state.batches > 0 && state.set_pass_calls > 0 &&
                                !string.IsNullOrWhiteSpace(state.render_counter_source);
                bool memory = state.allocated_memory_bytes > 0 && state.reserved_memory_bytes > 0;
                bool disposition = !string.IsNullOrWhiteSpace(state.performance_disposition) &&
                                   !string.IsNullOrWhiteSpace(state.transparent_overdraw_observation);
                AddCheck(report, "profile_" + state.state,
                    geometry && timing && counters && memory && disposition,
                    $"renderers={state.renderer_count}; triangles={state.triangle_count}; draw={state.draw_calls}; batches={state.batches}; setpass={state.set_pass_calls}; counter_estimated={state.render_counter_estimated}; cpu_med={state.cpu_render_median_ms:F3}; gpu_available={state.gpu_frame_time_available}; memory={state.allocated_memory_bytes}");
            }
            AddCheck(report, "headset_gpu_timing_limitation_explicit",
                profile.headset_profile_required && profile.measurement_context.Contains("no active headset runtime") &&
                !string.IsNullOrWhiteSpace(profile.headset_profile_note), profile.headset_profile_note);
        }

        private static string DoorWrapperName(string rootName) => "DOORPREFAB_" + rootName;
        private static string DoorPrefabPath(string rootName) => DoorPrefabFolder + "/" + DoorWrapperName(rootName) + ".prefab";

        private static Bounds RendererBoundsInLocal(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Door root has no renderers: " + root.name);
            bool initialized = false;
            Bounds result = default;
            foreach (Renderer renderer in renderers)
            {
                Bounds world = renderer.bounds;
                foreach (Vector3 corner in BoundsCorners(world))
                {
                    Vector3 local = root.InverseTransformPoint(corner);
                    if (!initialized) { result = new Bounds(local, Vector3.zero); initialized = true; }
                    else result.Encapsulate(local);
                }
            }
            return result;
        }

        private static Bounds RendererBoundsWorld(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Door root has no renderers: " + root.name);
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static IEnumerable<Vector3> BoundsCorners(Bounds bounds)
        {
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                yield return bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
        }

        private static Vector3 ClampSize(Vector3 size, float minimum) =>
            new Vector3(Mathf.Max(size.x, minimum), Mathf.Max(size.y, minimum), Mathf.Max(size.z, minimum));

        private static Bounds CombinedBounds(IEnumerable<GameObject> roots)
        {
            Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No renderers available for bounds.");
            Bounds result = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) result.Encapsulate(renderers[index].bounds);
            return result;
        }

        private static Mesh MeshForRenderer(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            return filter == null ? null : filter.sharedMesh;
        }

        private static long TriangleCount(Mesh mesh)
        {
            long indices = 0;
            for (int index = 0; index < mesh.subMeshCount; index++) indices += (long)mesh.GetIndexCount(index);
            return indices / 3L;
        }

        private static bool ZeroTransform(GameObject value) =>
            value.transform.position == Vector3.zero && value.transform.rotation == Quaternion.identity &&
            value.transform.localScale == Vector3.one;

        private static bool Approximately(Vector3 a, Vector3 b, float tolerance) => (a - b).sqrMagnitude <= tolerance * tolerance;
        private static float[] Values(Vector3 value) => new[] { value.x, value.y, value.z };
        private static float[] Values(Quaternion value) => new[] { value.x, value.y, value.z, value.w };

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

        private static string R43Reviews(string workspace) => Path.Combine(workspace, "Reviews", "HospitalExterior", "Stage05_R43");

        private static void AddCheck(R43GateReport report, string name, bool pass, string detail) =>
            report.checks.Add(new GateCheck { name = name, pass = pass, detail = detail });

        private static void WriteJson(object value, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(value, true) + Environment.NewLine);
        }

        private sealed class CollisionSpec
        {
            public readonly string name; public readonly string role; public readonly Vector3 center; public readonly Vector3 size;
            public CollisionSpec(string name, string role, Vector3 center, Vector3 size)
            { this.name = name; this.role = role; this.center = center; this.size = size; }
        }

        [Serializable] private sealed class GateStatus { public string status; public bool pass; }
        [Serializable] private sealed class GateCheck { public string name; public bool pass; public string detail; }
        [Serializable] private sealed class R43GateReport
        {
            public string schema; public string status; public bool pass; public List<GateCheck> checks; public List<DoorGateMeasurement> doors;
        }
        [Serializable] private sealed class DoorGateMeasurement
        {
            public string root; public string motion_type; public float[] pivot_position; public float clearance_m; public float[] collider_size; public bool pass;
        }
        [Serializable] private sealed class DoorReport { public string schema; public string source_zone; public DoorRecord[] doors; }
        [Serializable] private sealed class DoorRecord
        {
            public string root; public string prefab; public string motion_type; public float[] closed_local_position;
            public float[] closed_local_rotation; public float[] pivot_world_position_at_common_origin; public float[] motion_axis;
            public float open_distance_m; public float open_angle_degrees; public float verified_clearance_m;
            public float[] collider_center; public float[] collider_size; public bool kinematic_rigidbody; public int renderer_count;
        }
        [Serializable] private sealed class CollisionMap
        {
            public string schema; public string layer; public CollisionRecord[] static_box_colliders;
            public string[] dynamic_door_box_colliders; public int mesh_colliders; public int decorative_geometry_colliders;
        }
        [Serializable] private sealed class CollisionRecord
        { public string name; public string role; public float[] center; public float[] size; public string collider; }
        [Serializable] private sealed class TraversalReport
        {
            public string schema; public string scene; public bool temporary_harness; public string target;
            public bool xr_origin_component; public float eye_height_m; public bool disabled_test_camera; public RouteRecord[] routes; public string note;
        }
        [Serializable] private sealed class RouteRecord
        { public string name; public float[] position; public bool walkable_or_clearance_supported; public bool blocked_by_boundary; }
        [Serializable] private sealed class ProfilerBaseline
        {
            public string schema; public string selected_platform; public string headset_scope; public string measurement_context;
            public int acceptance_refresh_hz; public float acceptance_frame_budget_ms; public int preferred_refresh_hz;
            public float preferred_frame_budget_ms; public string graphics_device; public string graphics_api;
            public int graphics_memory_mb; public string processor; public int system_memory_mb; public bool headset_profile_required;
            public string headset_profile_note; public ProfileState[] states;
        }
        [Serializable] private sealed class ProfileState
        {
            public string state; public int renderer_count; public long triangle_count; public int transparent_renderer_count;
            public long transparent_triangle_count; public int draw_calls; public int batches; public int set_pass_calls;
            public bool render_counter_available; public bool render_counter_estimated; public string render_counter_source;
            public float cpu_render_median_ms; public float cpu_render_p95_ms;
            public int cpu_samples; public bool gpu_frame_time_available; public float gpu_frame_time_ms;
            public long allocated_memory_bytes; public long reserved_memory_bytes; public bool cpu_proxy_within_72hz_budget;
            public string performance_disposition; public string transparent_overdraw_observation;
        }
    }
}
