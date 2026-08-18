using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S1RuntimeCheck
    {
        public string name;
        public bool pass;
        public string detail;
    }

    [Serializable]
    public sealed class S1RuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S1.RuntimeGate.v1";
        public string status;
        public string unityVersion;
        public int passCount;
        public int totalCount;
        public string[] loadedScenes;
        public S1RuntimeCheck[] checks;
    }

    [DisallowMultipleComponent]
    public sealed class S1Bootstrap : MonoBehaviour
    {
        public const string InteriorShellPreviewScene = "Exterior_InteriorShellPreview";
        public const string SiteScenePrefix = "Exterior_Site";

        [SerializeField] private S1DatumContract datumContract;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private S1ReviewLocomotion locomotion;
        [SerializeField] private Transform diagnosticRoot;

        private readonly List<string> loadedSceneNames = new List<string>();

        public bool EnvironmentReady { get; private set; }
        public string[] LoadedSceneNames => loadedSceneNames.ToArray();
        public string FatalError { get; private set; }
        public S1DatumContract DatumContract => datumContract;
        public Transform DiagnosticRoot => diagnosticRoot;

        public void Configure(S1DatumContract contract, XROrigin origin, Camera camera,
            S1ReviewLocomotion reviewLocomotion, Transform diagnostics)
        {
            datumContract = contract;
            xrOrigin = origin;
            reviewCamera = camera;
            locomotion = reviewLocomotion;
            diagnosticRoot = diagnostics;
        }

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            var checks = new List<S1RuntimeCheck>();
            bool autoGate = HasArgument("-s1AutoGate");

            if (datumContract == null || xrOrigin == null || reviewCamera == null || locomotion == null || diagnosticRoot == null)
            {
                Fail("Bootstrap references are incomplete.");
                Add(checks, "bootstrap_references", false, FatalError);
                yield return FinishGate(checks, autoGate);
                yield break;
            }
            Add(checks, "bootstrap_references", true, "Contract, XROrigin, camera, locomotion, and diagnostics are assigned.");

            foreach (string path in datumContract.ProductionScenePaths)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                Scene preloaded = SceneManager.GetSceneByName(sceneName);
                if (preloaded.isLoaded)
                {
                    Fail("Duplicate/preloaded production scene: " + sceneName);
                    Add(checks, "load_" + sceneName, false, FatalError);
                    yield return FinishGate(checks, autoGate);
                    yield break;
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                if (operation == null)
                {
                    Fail("Could not begin additive load: " + path);
                    Add(checks, "load_" + sceneName, false, FatalError);
                    yield return FinishGate(checks, autoGate);
                    yield break;
                }
                while (!operation.isDone)
                    yield return null;
                loadedSceneNames.Add(sceneName);
                Add(checks, "load_" + sceneName, true, path);
            }

            yield return null;
            ValidateRuntimeState(checks);
            EnvironmentReady = checks.All(check => check.pass);
            if (!EnvironmentReady && string.IsNullOrEmpty(FatalError))
                FatalError = "One or more runtime gate checks failed.";
            yield return FinishGate(checks, autoGate);
        }

        private void ValidateRuntimeState(List<S1RuntimeCheck> checks)
        {
            string[] expected = datumContract.ProductionScenePaths.Select(Path.GetFileNameWithoutExtension).ToArray();
            Add(checks, "exact_production_scene_set", loadedSceneNames.SequenceEqual(expected),
                string.Join(",", loadedSceneNames));

            Scene shell = SceneManager.GetSceneByName(InteriorShellPreviewScene);
            bool siteLoaded = Enumerable.Range(0, SceneManager.sceneCount)
                .Select(SceneManager.GetSceneAt).Any(scene => scene.name.StartsWith(SiteScenePrefix, StringComparison.Ordinal));
            Add(checks, "no_shell_preview_or_site", !shell.isLoaded && !siteLoaded,
                $"shell={shell.isLoaded}; site={siteLoaded}");

            bool productionTransforms = true;
            int productionCameras = 0;
            int productionLights = 0;
            foreach (string path in datumContract.ProductionScenePaths)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.isLoaded)
                {
                    productionTransforms = false;
                    continue;
                }
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Transform transform = root.transform;
                    productionTransforms &= transform.position.sqrMagnitude < 0.000001f;
                    productionTransforms &= Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.001f;
                    productionTransforms &= (transform.localScale - Vector3.one).sqrMagnitude < 0.000001f;
                    productionCameras += root.GetComponentsInChildren<Camera>(true).Length;
                    productionLights += root.GetComponentsInChildren<Light>(true).Length;
                }
            }
            Add(checks, "production_roots_identity", productionTransforms, "All loaded exterior roots remain at shared origin/unit scale.");
            Add(checks, "production_scenes_own_no_camera_or_light", productionCameras == 0 && productionLights == 0,
                $"cameras={productionCameras}; lights={productionLights}");

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Light[] lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Add(checks, "unique_review_rig_camera_listener_light", cameras.Length == 1 && listeners.Length == 1 && lights.Length == 1,
                $"cameras={cameras.Length}; listeners={listeners.Length}; lights={lights.Length}");

            S1DatumProxy[] proxies = diagnosticRoot.GetComponentsInChildren<S1DatumProxy>(true);
            bool datums = proxies.Length == 7 && proxies.Select(proxy => proxy.FloorId).Distinct(StringComparer.Ordinal).Count() == 7;
            foreach (S1FloorDatumRecord expectedFloor in datumContract.FloorDatums)
            {
                S1DatumProxy actual = proxies.SingleOrDefault(proxy => proxy.FloorId == expectedFloor.floorId);
                datums &= actual != null && Mathf.Abs(actual.ElevationM - expectedFloor.elevationM) <= 0.01f;
            }
            Add(checks, "seven_locked_datum_proxies", datums, string.Join(",", proxies.Select(proxy => proxy.FloorId + "=" + proxy.ElevationM.ToString("F2"))));
            Add(checks, "diagnostics_have_no_colliders", diagnosticRoot.GetComponentsInChildren<Collider>(true).Length == 0,
                "Datum and elevator reference geometry is visual-only.");
            Add(checks, "single_elevator_reference", diagnosticRoot.GetComponentsInChildren<S1ElevatorReference>(true).Length == 1,
                "One E01 reference cage; no cabin, controls, doors, or travel behavior.");

            CharacterController controller = xrOrigin.GetComponent<CharacterController>();
            float eyeHeight = reviewCamera.transform.position.y - xrOrigin.Origin.transform.position.y;
            Add(checks, "review_scale_and_locomotion", controller != null && Mathf.Abs(eyeHeight - datumContract.EyeHeightM) <= 0.02f
                && Mathf.Abs(locomotion.WalkSpeedMps - 2f) < 0.001f && Mathf.Abs(locomotion.SprintSpeedMps - 4f) < 0.001f,
                $"eye={eyeHeight:F3}; walk={locomotion.WalkSpeedMps:F1}; sprint={locomotion.SprintSpeedMps:F1}");

            BoxCollider apron = FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "COL_Walkable_EntranceApron");
            BoxCollider threshold = FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "COL_EntranceThreshold");
            BoxCollider ground = FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(item => item.name == "COL_Walkable_ExteriorGround");
            float step = apron != null && threshold != null
                ? Mathf.Abs((threshold.bounds.center.y + threshold.bounds.extents.y) - (apron.bounds.center.y + apron.bounds.extents.y))
                : float.PositiveInfinity;
            Add(checks, "entrance_approach_and_threshold_supported", apron != null && threshold != null && ground != null
                && controller != null && step <= controller.stepOffset + 0.001f,
                $"ground={ground != null}; apron={apron != null}; threshold={threshold != null}; step={step:F3}");
        }

        private IEnumerator FinishGate(List<S1RuntimeCheck> checks, bool autoGate)
        {
            var report = new S1RuntimeGateReport
            {
                status = checks.All(check => check.pass) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(check => check.pass),
                totalCount = checks.Count,
                loadedScenes = loadedSceneNames.ToArray(),
                checks = checks.ToArray(),
            };
            string output = ArgumentValue("-s1ReportPath");
            if (string.IsNullOrWhiteSpace(output))
                output = Path.Combine(Application.persistentDataPath, "StageI1_R03_S1_RuntimeGate.json");
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)) ?? Application.persistentDataPath);
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S1_RUNTIME_GATE={report.status}; {report.passCount}/{report.totalCount}; {output}");

            if (autoGate)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                Application.Quit(report.status == "PASS" ? 0 : 1);
            }
        }

        private void Fail(string message)
        {
            FatalError = message;
            Debug.LogError("HOSPITAL_INTERIOR_R03_S1_RUNTIME_FAIL: " + message, this);
        }

        private static void Add(ICollection<S1RuntimeCheck> checks, string name, bool pass, string detail)
            => checks.Add(new S1RuntimeCheck { name = name, pass = pass, detail = detail });

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
