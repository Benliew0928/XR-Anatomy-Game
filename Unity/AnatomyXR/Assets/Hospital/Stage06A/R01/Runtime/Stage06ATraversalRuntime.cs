using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CutMyBodyPlease.Hospital.Stage06A
{
    public sealed class Stage06ASceneBootstrap : MonoBehaviour
    {
        [SerializeField] private Stage06ALoadProfile defaultProfile = Stage06ALoadProfile.Combined;
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private Stage06ASafeTeleportController teleportController;
        [SerializeField] private Stage06ATraversalRecorder recorder;
        [SerializeField] private Behaviour[] enableAfterLoad;

        public Stage06ALoadProfile ActiveProfile { get; private set; }
        public bool EnvironmentReady { get; private set; }
        public string LastError { get; private set; }

        public void Configure(Stage06ALoadProfile profile, XROrigin origin, Camera camera,
            Stage06ASafeTeleportController teleporter, Stage06ATraversalRecorder sessionRecorder,
            Behaviour[] deferredBehaviours)
        {
            defaultProfile = profile;
            xrOrigin = origin;
            xrCamera = camera;
            teleportController = teleporter;
            recorder = sessionRecorder;
            enableAfterLoad = deferredBehaviours;
        }

        private IEnumerator Start()
        {
            ActiveProfile = ParseProfile(defaultProfile);
            if (enableAfterLoad != null)
            {
                foreach (Behaviour behaviour in enableAfterLoad)
                    if (behaviour != null)
                        behaviour.enabled = false;
            }

            string[] requested = Stage06AContract.ScenesFor(ActiveProfile);
            foreach (string path in requested)
            {
                string sceneName = Path.GetFileNameWithoutExtension(path);
                Scene existing = SceneManager.GetSceneByName(sceneName);
                if (existing.isLoaded)
                {
                    Fail("Duplicate/preloaded production scene: " + sceneName);
                    yield break;
                }

                AsyncOperation operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                if (operation == null)
                {
                    Fail("Could not begin additive load: " + path);
                    yield break;
                }
                while (!operation.isDone)
                    yield return null;
            }

            yield return null;
            if (!ValidateLoadedState(requested))
                yield break;

            PositionRigForProfile();
            MeshCollider[] routeSurfaces = FindRouteSurfaces();
            if (ActiveProfile != Stage06ALoadProfile.HospitalOnly && routeSurfaces.Length != 20)
            {
                Fail("Expected exactly 20 approved route colliders; found " + routeSurfaces.Length);
                yield break;
            }

            teleportController.BindRouteSurfaces(routeSurfaces);
            foreach (HospitalSiteGrassRenderer grass in FindObjectsByType<HospitalSiteGrassRenderer>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                grass.SetTargetCamera(xrCamera);
                grass.SetAnchor(xrCamera.transform);
            }

            if (enableAfterLoad != null)
            {
                foreach (Behaviour behaviour in enableAfterLoad)
                    if (behaviour != null)
                        behaviour.enabled = true;
            }

            EnvironmentReady = true;
            recorder.OnEnvironmentReady(ActiveProfile, requested, routeSurfaces.Length);
        }

        private Stage06ALoadProfile ParseProfile(Stage06ALoadProfile fallback)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (!string.Equals(args[i], "-stage06aProfile", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (Enum.TryParse(args[i + 1], true, out Stage06ALoadProfile parsed))
                    return parsed;
            }
            return fallback;
        }

        private bool ValidateLoadedState(string[] requested)
        {
            foreach (string path in requested)
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                if (!scene.isLoaded)
                    return Fail("Requested scene is not loaded: " + path);
                int cameras = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Camera>(true).Length);
                int lights = scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Light>(true).Length);
                if (cameras != 0 || lights != 0)
                    return Fail($"Production scene owns unexpected camera/light: {path}; cameras={cameras}; lights={lights}");
            }

            string[] loadedRequested = requested.Select(Path.GetFileNameWithoutExtension).ToArray();
            if (loadedRequested.Distinct(StringComparer.Ordinal).Count() != loadedRequested.Length)
                return Fail("Profile contains duplicate scene names.");

            int grassCount = FindObjectsByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            int expectedGrass = ActiveProfile == Stage06ALoadProfile.HospitalOnly ? 0 : 1;
            if (grassCount != expectedGrass)
                return Fail($"Grass manager count mismatch; expected={expectedGrass}; actual={grassCount}");

            return true;
        }

        private MeshCollider[] FindRouteSurfaces()
        {
            Scene core = SceneManager.GetSceneByPath(Stage06AContract.SiteScenes[0]);
            if (!core.isLoaded)
                return Array.Empty<MeshCollider>();
            return core.GetRootGameObjects()
                .Where(root => root.name == "SITE_CoreRoadParking")
                .SelectMany(root => root.GetComponentsInChildren<MeshCollider>(true))
                .Where(collider => collider.sharedMesh != null && !collider.isTrigger)
                .OrderBy(collider => HierarchyPath(collider.transform), StringComparer.Ordinal)
                .ToArray();
        }

        private void PositionRigForProfile()
        {
            Vector3 target = ActiveProfile == Stage06ALoadProfile.HospitalOnly
                ? Stage06AContract.HospitalOnlySpawn
                : Stage06AContract.CombinedSpawn;
            xrOrigin.Origin.transform.SetPositionAndRotation(target, Quaternion.identity);
        }

        private bool Fail(string message)
        {
            LastError = message;
            Debug.LogError("STAGE06A_RUNTIME_FAIL: " + message, this);
            recorder?.RecordRuntimeError(message);
            return false;
        }

        public static string HierarchyPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}

