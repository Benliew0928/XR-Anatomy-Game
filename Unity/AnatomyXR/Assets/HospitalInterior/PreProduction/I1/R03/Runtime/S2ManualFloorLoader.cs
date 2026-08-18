using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S2RuntimeCheck
    {
        public string name;
        public bool pass;
        public string detail;
    }

    [Serializable]
    public sealed class S2RuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S2.RuntimeGate.v1";
        public string status;
        public string unityVersion;
        public int passCount;
        public int totalCount;
        public string[] route;
        public string[] loadedProductionScenes;
        public S2RuntimeCheck[] checks;
    }

    [DisallowMultipleComponent]
    public sealed class S2ManualFloorLoader : MonoBehaviour
    {
        public const string InteriorShellPreviewScene = "Exterior_InteriorShellPreview";
        public const string SiteScenePrefix = "Exterior_Site";

        public static readonly string[] ExteriorOnlyBalconyBackerNames =
        {
            "UE_EXT_Balconies_OPAQUE_L02_05",
            "UE_EXT_Balconies_OPAQUE_L03_05",
            "UE_EXT_Balconies_OPAQUE_L04_05",
            "UE_EXT_Balconies_OPAQUE_L05_05",
            "UE_EXT_Balconies_OPAQUE_L06_05",
            "UE_EXT_Balconies_OPAQUE_L07_05",
        };

        public static readonly string[] ExteriorOverlappingBalconyFloorSkinNames =
        {
            "UE_EXT_Balconies_OPAQUE_L02_04",
            "UE_EXT_Balconies_OPAQUE_L03_04",
            "UE_EXT_Balconies_OPAQUE_L04_04",
            "UE_EXT_Balconies_OPAQUE_L05_04",
            "UE_EXT_Balconies_OPAQUE_L06_04",
            "UE_EXT_Balconies_OPAQUE_L07_04",
        };

        [SerializeField] private S2FloorContract floorContract;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S2ReviewLocomotion locomotion;

        private readonly List<string> loadedProductionScenes = new List<string>();
        private bool switching;

        public bool EnvironmentReady { get; private set; }
        public int HiddenExteriorBackerCount { get; private set; }
        public int HiddenExteriorBalconyFloorSkinCount { get; private set; }
        public string CurrentFloorId { get; private set; } = string.Empty;
        public string FatalError { get; private set; } = string.Empty;
        public S2FloorContract FloorContract => floorContract;
        public XROrigin XrOrigin => xrOrigin;
        public Camera ReviewCamera => reviewCamera;
        public S2ReviewLocomotion Locomotion => locomotion;
        public bool IsSwitching => switching;

        public void Configure(S2FloorContract contract, XROrigin origin, Camera camera, S2ReviewLocomotion reviewLocomotion)
        {
            floorContract = contract;
            xrOrigin = origin;
            reviewCamera = camera;
            locomotion = reviewLocomotion;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            if (floorContract == null || xrOrigin == null || reviewCamera == null || locomotion == null)
            {
                Fail("Bootstrap references are incomplete.");
                yield break;
            }

            foreach (string path in floorContract.ProductionScenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                Scene existing = SceneManager.GetSceneByName(sceneName);
                if (existing.isLoaded)
                {
                    Fail("Duplicate/preloaded production scene: " + sceneName);
                    yield break;
                }
                AsyncOperation operation = LoadSceneAdditive(path);
                if (operation == null)
                {
                    Fail("Could not begin additive production load: " + path);
                    yield break;
                }
                while (!operation.isDone)
                    yield return null;
                loadedProductionScenes.Add(sceneName);
            }

            HiddenExteriorBackerCount = HideExteriorOnlyBalconyBackers();
            if (HiddenExteriorBackerCount != ExteriorOnlyBalconyBackerNames.Length)
            {
                Fail($"Expected to suppress {ExteriorOnlyBalconyBackerNames.Length} exterior-only balcony backers; found {HiddenExteriorBackerCount}.");
                yield break;
            }
            HiddenExteriorBalconyFloorSkinCount = HideExteriorOverlappingBalconyFloorSkins();
            if (HiddenExteriorBalconyFloorSkinCount != ExteriorOverlappingBalconyFloorSkinNames.Length)
            {
                Fail($"Expected to suppress {ExteriorOverlappingBalconyFloorSkinNames.Length} overlapping balcony floor skins; found {HiddenExteriorBalconyFloorSkinCount}.");
                yield break;
            }

            yield return SwitchFloorInternal(floorContract.InitialFloorId);
            if (!string.IsNullOrEmpty(FatalError))
                yield break;
            EnvironmentReady = true;

            if (HasArgument("-s2AutoGate"))
                yield return RunAutoGate();
        }

        private void Update()
        {
            if (!EnvironmentReady || switching || Keyboard.current == null)
                return;
            int requested = RequestedFloorIndex(Keyboard.current);
            if (requested >= 0)
                RequestFloor("F" + requested.ToString("00"));
        }

        public bool RequestFloor(string floorId)
        {
            if (!EnvironmentReady || switching || string.Equals(CurrentFloorId, floorId, StringComparison.Ordinal))
                return false;
            if (!floorContract.TryGetFloor(floorId, out _))
                return false;
            StartCoroutine(SwitchFloorInternal(floorId));
            return true;
        }

        public IEnumerator SwitchFloorForGate(string floorId)
        {
            if (!string.Equals(CurrentFloorId, floorId, StringComparison.Ordinal))
                yield return SwitchFloorInternal(floorId);
        }

        private IEnumerator SwitchFloorInternal(string floorId)
        {
            if (!floorContract.TryGetFloor(floorId, out S2FloorRecord requested))
            {
                Fail("Unknown floor request: " + floorId);
                yield break;
            }

            switching = true;
            try
            {
                if (!string.IsNullOrEmpty(CurrentFloorId) && floorContract.TryGetFloor(CurrentFloorId, out S2FloorRecord current))
                {
                    Scene currentScene = SceneManager.GetSceneByPath(current.scenePath);
                    if (currentScene.isLoaded)
                    {
                        AsyncOperation unload = SceneManager.UnloadSceneAsync(currentScene);
                        if (unload == null)
                        {
                            Fail("Could not unload floor scene: " + current.scenePath);
                            yield break;
                        }
                        while (!unload.isDone)
                            yield return null;
                    }
                    CurrentFloorId = string.Empty;
                }

                AsyncOperation load = LoadSceneAdditive(requested.scenePath);
                if (load == null)
                {
                    Fail("Could not begin floor load: " + requested.scenePath);
                    yield break;
                }
                while (!load.isDone)
                    yield return null;

                Scene loaded = SceneManager.GetSceneByPath(requested.scenePath);
                if (!loaded.isLoaded)
                {
                    Fail("Floor scene did not become loaded: " + requested.scenePath);
                    yield break;
                }
                SceneManager.SetActiveScene(loaded);

                S2ArrivalAnchor[] anchors = loaded.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<S2ArrivalAnchor>(true)).ToArray();
                if (anchors.Length != 1 || !string.Equals(anchors[0].FloorId, floorId, StringComparison.Ordinal))
                {
                    Fail("Floor arrival-anchor contract failed for " + floorId + ".");
                    yield break;
                }

                TeleportTo(anchors[0].transform);
                CurrentFloorId = floorId;
                Physics.SyncTransforms();
                yield return null;
            }
            finally
            {
                switching = false;
            }
        }

        private void TeleportTo(Transform anchor)
        {
            CharacterController controller = xrOrigin.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            xrOrigin.transform.SetPositionAndRotation(anchor.position, Quaternion.Euler(0f, anchor.eulerAngles.y, 0f));
            reviewCamera.transform.localRotation = Quaternion.identity;
            if (controller != null)
                controller.enabled = true;
        }

        private IEnumerator RunAutoGate()
        {
            var checks = new List<S2RuntimeCheck>();
            string[] route = { "F00", "F01", "F02", "F03", "F04", "F05", "F06", "F00" };

            Add(checks, "bootstrap_references", floorContract != null && xrOrigin != null && reviewCamera != null && locomotion != null,
                "Contract, XROrigin, camera, and locomotion are assigned.");
            string[] expectedProduction = floorContract.ProductionScenePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            Add(checks, "exact_production_scene_set", loadedProductionScenes.SequenceEqual(expectedProduction),
                string.Join(",", loadedProductionScenes));
            bool siteLoaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                .Any(scene => scene.name.StartsWith(SiteScenePrefix, StringComparison.Ordinal));
            Add(checks, "no_shell_preview_or_site", !SceneManager.GetSceneByName(InteriorShellPreviewScene).isLoaded && !siteLoaded,
                $"shell={SceneManager.GetSceneByName(InteriorShellPreviewScene).isLoaded}; site={siteLoaded}");
            Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Renderer[] balconyBackers = allRenderers.Where(renderer => ExteriorOnlyBalconyBackerNames.Contains(renderer.name)).ToArray();
            Add(checks, "exterior_only_balcony_backers_hidden",
                HiddenExteriorBackerCount == ExteriorOnlyBalconyBackerNames.Length
                && balconyBackers.Length == ExteriorOnlyBalconyBackerNames.Length
                && balconyBackers.All(renderer => !renderer.enabled),
                $"hidden={HiddenExteriorBackerCount}; found={balconyBackers.Length}; enabled={balconyBackers.Count(renderer => renderer.enabled)}");
            Renderer[] balconyFloorSkins = allRenderers.Where(renderer => ExteriorOverlappingBalconyFloorSkinNames.Contains(renderer.name)).ToArray();
            Add(checks, "overlapping_balcony_floor_skins_hidden",
                HiddenExteriorBalconyFloorSkinCount == ExteriorOverlappingBalconyFloorSkinNames.Length
                && balconyFloorSkins.Length == ExteriorOverlappingBalconyFloorSkinNames.Length
                && balconyFloorSkins.All(renderer => !renderer.enabled),
                $"hidden={HiddenExteriorBalconyFloorSkinCount}; found={balconyFloorSkins.Length}; enabled={balconyFloorSkins.Count(renderer => renderer.enabled)}");
            Renderer[] structuralUndersides = allRenderers
                .Where(renderer => renderer.name.StartsWith("S2_R03_StructuralUnderside_", StringComparison.Ordinal)).ToArray();
            bool structuralContext = structuralUndersides.Length == 6
                && structuralUndersides.All(renderer => renderer.GetComponent<Collider>() == null
                    && floorContract.TryGetFloor(renderer.name.Substring(renderer.name.Length - 3), out S2FloorRecord floor)
                    && Mathf.Abs(renderer.transform.position.y - (floor.elevationM - 0.20f)) <= 0.001f);
            Add(checks, "persistent_structural_storey_separation", structuralContext,
                "undersides=" + string.Join(",", structuralUndersides.Select(renderer => renderer.name)));
            Add(checks, "single_review_camera_light_listener",
                FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1
                && FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1
                && FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
                "Exactly one bootstrap camera, light, and audio listener.");
            Add(checks, "review_scale_and_controls", Mathf.Abs(floorContract.EyeHeightM - 1.7f) <= 0.001f
                && Mathf.Abs(locomotion.WalkSpeedMps - 2f) <= 0.001f && Mathf.Abs(locomotion.SprintSpeedMps - 4f) <= 0.001f,
                $"eye={floorContract.EyeHeightM:F1}; walk={locomotion.WalkSpeedMps:F1}; sprint={locomotion.SprintSpeedMps:F1}");

            foreach (string floorId in route)
            {
                yield return SwitchFloorForGate(floorId);
                if (!string.IsNullOrEmpty(FatalError))
                    break;
                ValidateActiveFloor(checks, floorId);
            }

            string output = ArgumentValue("-s2ReportPath");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Application.persistentDataPath, "StageI1_R03_S2_RuntimeGate.json");
            var report = new S2RuntimeGateReport
            {
                status = checks.All(check => check.pass) && string.IsNullOrEmpty(FatalError) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(check => check.pass),
                totalCount = checks.Count,
                route = route,
                loadedProductionScenes = loadedProductionScenes.ToArray(),
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? Application.persistentDataPath);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S2_RUNTIME_GATE={report.status}; {report.passCount}/{report.totalCount}; {output}");
            yield return new WaitForSecondsRealtime(0.25f);
            Application.Quit(report.status == "PASS" ? 0 : 1);
        }

        private void ValidateActiveFloor(ICollection<S2RuntimeCheck> checks, string floorId)
        {
            floorContract.TryGetFloor(floorId, out S2FloorRecord expected);
            S2FloorSceneMarker[] markers = FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            bool oneMarker = markers.Length == 1 && markers[0].FloorId == floorId && Mathf.Abs(markers[0].ElevationM - expected.elevationM) <= 0.001f;
            Add(checks, floorId + "_single_active_floor", oneMarker, markers.Length == 0 ? "none" : string.Join(",", markers.Select(item => item.FloorId)));

            if (!oneMarker)
                return;
            Transform root = markers[0].transform;
            bool identity = root.position.sqrMagnitude < 0.000001f
                && Quaternion.Angle(root.rotation, Quaternion.identity) < 0.001f
                && (root.localScale - Vector3.one).sqrMagnitude < 0.000001f;
            TextMesh[] labels = root.GetComponentsInChildren<TextMesh>(true);
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool visibleAllowlist = renderers.All(renderer => renderer.name == "S2_R03_EmptySlab" || renderer.name == "S2_R03_FloorLabel");
            Add(checks, floorId + "_identity_and_visible_allowlist", identity && labels.Length == 1
                && labels[0].text == floorId && visibleAllowlist,
                $"identity={identity}; labels={labels.Length}; renderers={string.Join(",", renderers.Select(item => item.name))}");

            S2ArrivalAnchor[] anchors = root.GetComponentsInChildren<S2ArrivalAnchor>(true);
            bool arrival = anchors.Length == 1 && Vector3.Distance(xrOrigin.transform.position, anchors[0].transform.position) <= 0.02f;
            MeshCollider slabCollider = root.GetComponentsInChildren<MeshCollider>(true)
                .SingleOrDefault(item => item.name == "S2_R03_EmptySlab");
            bool walkable = anchors.Length == 1 && slabCollider != null
                && slabCollider.Raycast(new Ray(anchors[0].transform.position + Vector3.up, Vector3.down), out _, 3f);
            Add(checks, floorId + "_safe_walkable_arrival", arrival && walkable,
                $"arrival={arrival}; walkable={walkable}; position={xrOrigin.transform.position}");

            Vector2[] probes = expected.integrationCoverageProbes ?? Array.Empty<Vector2>();
            bool integrationSeams = floorId == "F00"
                ? probes.Length == 0 && expected.footprint.Length == 8
                : slabCollider != null && probes.Length == 4 && probes.All(probe => slabCollider.Raycast(
                    new Ray(new Vector3(probe.x, expected.elevationM + 1f, probe.y), Vector3.down), out _, 2f));
            Add(checks, floorId + "_integration_seam_coverage", integrationSeams,
                floorId == "F00" ? "approved F00 production-derived footprint"
                    : "facade/balcony probes=" + string.Join(",", probes.Select(probe => $"({probe.x:F2},{probe.y:F2})")));

            BoxCollider[] boundaries = root.GetComponentsInChildren<BoxCollider>(true)
                .Where(item => item.name.StartsWith("S2_R03_SafetyBoundary_", StringComparison.Ordinal)).ToArray();
            bool safety = boundaries.Length >= 4;
            if (floorId == "F00")
            {
                Vector3 opening = new Vector3((expected.entranceOpeningMinX + expected.entranceOpeningMaxX) * 0.5f,
                    expected.elevationM + 1f, expected.minZ);
                safety &= boundaries.All(boundary => !boundary.bounds.Contains(opening));
            }
            Add(checks, floorId + "_safety_boundary", safety, "boundaryColliders=" + boundaries.Length);
        }

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S2_RUNTIME_FAIL: " + message, this);
        }

        private static int RequestedFloorIndex(Keyboard keyboard)
        {
            if (keyboard.digit0Key.wasPressedThisFrame || keyboard.numpad0Key.wasPressedThisFrame) return 0;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 1;
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 2;
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 3;
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 4;
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 5;
            if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame) return 6;
            return -1;
        }

        private static void Add(ICollection<S2RuntimeCheck> checks, string name, bool pass, string detail)
            => checks.Add(new S2RuntimeCheck { name = name, pass = pass, detail = detail });

        public static int HideExteriorOnlyBalconyBackers()
        {
            HashSet<string> names = new HashSet<string>(ExteriorOnlyBalconyBackerNames, StringComparer.Ordinal);
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(renderer => names.Contains(renderer.name)).ToArray();
            foreach (Renderer renderer in renderers)
                renderer.enabled = false;
            return renderers.Length;
        }

        public static int HideExteriorOverlappingBalconyFloorSkins()
        {
            HashSet<string> names = new HashSet<string>(ExteriorOverlappingBalconyFloorSkinNames, StringComparer.Ordinal);
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(renderer => names.Contains(renderer.name)).ToArray();
            foreach (Renderer renderer in renderers)
                renderer.enabled = false;
            return renderers.Length;
        }

        private static AsyncOperation LoadSceneAdditive(string path)
        {
#if UNITY_EDITOR
            // The review build uses an explicit scene list, while S2 intentionally leaves
            // EditorBuildSettings untouched. Play Mode therefore needs the editor API to
            // load S2 scene assets that are intentionally not registered in EditorBuildSettings.
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
#endif
        }

        private static bool HasArgument(string key)
            => Environment.GetCommandLineArgs().Any(argument => string.Equals(argument, key, StringComparison.OrdinalIgnoreCase));

        private static string ArgumentValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            return string.Empty;
        }
    }
}
