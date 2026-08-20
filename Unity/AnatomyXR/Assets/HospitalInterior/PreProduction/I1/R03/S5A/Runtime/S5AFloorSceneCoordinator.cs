using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S5AFloorTransitionState
    {
        Uninitialized,
        Steady,
        LoadingDestination,
        ValidatingDestination,
        CommittingDestination,
        UnloadingOrigin,
        RollingBack,
        FaultedSafe,
    }

    [DisallowMultipleComponent]
    public sealed class S5AFloorSceneCoordinator : MonoBehaviour
    {
        [SerializeField] private S5AStairContract contract;
        [SerializeField] private S5ADualAperture dualAperture;
        [SerializeField] private S3DFacadeFloorEdgeCoordinator facadeFloorEdges;
        [SerializeField] private string activeFloorId = "F00";
        [SerializeField] private string destinationFloorId = string.Empty;
        [SerializeField] private S5AFloorTransitionState state = S5AFloorTransitionState.Uninitialized;

        private bool injectNextLoadFailure;
        private bool injectNextUnloadFailure;
        private bool injectNextValidationFailure;
        private Coroutine transition;

        public S5AStairContract Contract => contract;
        public S5ADualAperture DualAperture => dualAperture;
        public string ActiveFloorId => activeFloorId;
        public string DestinationFloorId => destinationFloorId;
        public S5AFloorTransitionState State => state;
        public bool IsTransitioning => transition != null;
        public bool IsSteady => state == S5AFloorTransitionState.Steady && !IsTransitioning;
        public bool LastTransitionSucceeded { get; private set; }
        public string LastResult { get; private set; } = string.Empty;
        public string LastFloorValidationDetail { get; private set; } = string.Empty;
        public int CompletedTransitionCount { get; private set; }
        public int RejectedTransitionCount { get; private set; }
        public int RollbackCount { get; private set; }
        public int MaximumSimultaneousFloorCount { get; private set; }
        public event Action<string> ActiveFloorChanged;

        public void Configure(S5AStairContract stairContract, S5ADualAperture aperture,
            S3DFacadeFloorEdgeCoordinator facadeCoordinator)
        {
            contract = stairContract;
            dualAperture = aperture;
            facadeFloorEdges = facadeCoordinator;
            activeFloorId = contract?.ApprovedFloorContract?.InitialFloorId ?? "F00";
            destinationFloorId = string.Empty;
            state = S5AFloorTransitionState.Uninitialized;
        }

        public bool InitializeFromLoadedFloor(string floorId, Scene floorScene, out string detail)
        {
            if (contract == null || dualAperture == null || !contract.TryGetFloor(floorId, out _))
            {
                detail = "coordinator references or floor record missing";
                return false;
            }
            if (!ValidateAndApply(floorScene, floorId, out detail))
                return false;
            activeFloorId = floorId;
            destinationFloorId = string.Empty;
            state = S5AFloorTransitionState.Steady;
            MaximumSimultaneousFloorCount = Mathf.Max(MaximumSimultaneousFloorCount, LoadedFloorMarkers().Length);
            LastResult = "S5A floor coordinator initialized at " + floorId + ".";
            return ValidateSteadyState(out detail);
        }

        public bool RequestTransition(string targetFloorId)
        {
            if (contract == null || dualAperture == null || state == S5AFloorTransitionState.Uninitialized)
                return Reject("Floor transition rejected before initialization.");
            if (IsTransitioning || state != S5AFloorTransitionState.Steady)
                return Reject("Floor transition rejected while another vertical-circulation transition is active.");
            if (!contract.TryGetFloor(targetFloorId, out _))
                return Reject("Unknown floor " + targetFloorId + ".");
            if (string.Equals(targetFloorId, activeFloorId, StringComparison.Ordinal))
            {
                LastTransitionSucceeded = true;
                LastResult = targetFloorId + " is already the committed active floor.";
                return true;
            }
            LastTransitionSucceeded = false;
            destinationFloorId = targetFloorId;
            transition = StartCoroutine(TransitionRoutine(targetFloorId));
            return true;
        }

        public bool IsFloorLoadedAndValidated(string floorId)
        {
            if (!string.Equals(floorId, activeFloorId, StringComparison.Ordinal)
                || state != S5AFloorTransitionState.Steady
                || !contract.TryGetFloor(floorId, out S2FloorRecord floor))
            {
                LastFloorValidationDetail = $"state={state}; active={activeFloorId}; requested={floorId}; record={(contract != null && contract.TryGetFloor(floorId, out _))}";
                return false;
            }
            Scene scene = SceneManager.GetSceneByPath(floor.scenePath);
            string apertureDetail = "scene unavailable";
            bool valid = scene.IsValid() && scene.isLoaded
                && dualAperture.ValidateApplied(scene, floorId, out apertureDetail);
            LastFloorValidationDetail = $"sceneValid={scene.IsValid()}; loaded={scene.isLoaded}; {apertureDetail}";
            return valid;
        }

        public void InjectNextDestinationLoadFailureForGate() => injectNextLoadFailure = true;
        public void InjectNextOriginUnloadFailureForGate() => injectNextUnloadFailure = true;
        public void InjectNextMissingApertureFailureForGate() => injectNextValidationFailure = true;

        public bool ValidateSteadyState(out string detail)
        {
            S2FloorSceneMarker[] markers = LoadedFloorMarkers();
            bool one = markers.Length == 1 && markers[0].FloorId == activeFloorId;
            string apertureDetail = "floor record unavailable";
            bool applied = contract != null && contract.TryGetFloor(activeFloorId, out S2FloorRecord floor)
                && dualAperture.ValidateApplied(SceneManager.GetSceneByPath(floor.scenePath), activeFloorId, out apertureDetail);
            detail = $"state={state}; active={activeFloorId}; destination={destinationFloorId}; markers={string.Join(",", markers.Select(item => item.FloorId))}; aperture={apertureDetail}";
            return IsSteady && one && applied;
        }

        private IEnumerator TransitionRoutine(string targetFloorId)
        {
            string originFloorId = activeFloorId;
            contract.TryGetFloor(originFloorId, out S2FloorRecord origin);
            contract.TryGetFloor(targetFloorId, out S2FloorRecord destination);
            Scene destinationScene = default;

            state = S5AFloorTransitionState.LoadingDestination;
            if (injectNextLoadFailure)
            {
                injectNextLoadFailure = false;
                // No scene mutation has occurred yet, so preserve the committed
                // origin synchronously. This also avoids an unnecessary nested
                // rollback coroutine for the pre-load failure gate.
                activeFloorId = originFloorId;
                destinationFloorId = string.Empty;
                LastTransitionSucceeded = false;
                RollbackCount++;
                state = S5AFloorTransitionState.Steady;
                transition = null;
                LastResult = "Injected destination-load failure. Last committed floor remains "
                    + originFloorId + ".";
                yield break;
            }
            AsyncOperation load = LoadSceneAdditive(destination.scenePath);
            if (load == null)
            {
                yield return Rollback(originFloorId, default, false,
                    "Destination load could not start.");
                yield break;
            }
            float timeout = Time.realtimeSinceStartup + 20f;
            while (!load.isDone && Time.realtimeSinceStartup < timeout)
                yield return null;
            destinationScene = SceneManager.GetSceneByPath(destination.scenePath);
            MaximumSimultaneousFloorCount = Mathf.Max(MaximumSimultaneousFloorCount, LoadedFloorMarkers().Length);
            state = S5AFloorTransitionState.ValidatingDestination;
            if (injectNextValidationFailure)
            {
                injectNextValidationFailure = false;
                yield return Rollback(originFloorId, destinationScene,
                    destinationScene.IsValid() && destinationScene.isLoaded,
                    "Injected missing dual-aperture asset failure.");
                yield break;
            }
            string validationDetail = "destination load incomplete";
            bool destinationValid = load.isDone && destinationScene.IsValid()
                && destinationScene.isLoaded
                && ValidateAndApply(destinationScene, targetFloorId, out validationDetail);
            if (!destinationValid)
            {
                yield return Rollback(originFloorId, destinationScene,
                    destinationScene.IsValid() && destinationScene.isLoaded,
                    "Destination validation failed: " + validationDetail);
                yield break;
            }

            if (LoadedFloorMarkers().Length != 2)
            {
                yield return Rollback(originFloorId, destinationScene, true,
                    "Transition did not contain exactly origin plus destination.");
                yield break;
            }
            state = S5AFloorTransitionState.CommittingDestination;
            if (!SceneManager.SetActiveScene(destinationScene))
            {
                yield return Rollback(originFloorId, destinationScene, true,
                    "Destination could not become the active Unity scene.");
                yield break;
            }

            state = S5AFloorTransitionState.UnloadingOrigin;
            Scene originScene = SceneManager.GetSceneByPath(origin.scenePath);
            if (injectNextUnloadFailure)
            {
                injectNextUnloadFailure = false;
                yield return Rollback(originFloorId, destinationScene, true,
                    "Injected origin-unload failure.");
                yield break;
            }
            AsyncOperation unload = originScene.IsValid() && originScene.isLoaded
                ? SceneManager.UnloadSceneAsync(originScene) : null;
            if (unload == null)
            {
                yield return Rollback(originFloorId, destinationScene, true,
                    "Origin unload could not start.");
                yield break;
            }
            timeout = Time.realtimeSinceStartup + 20f;
            while (!unload.isDone && Time.realtimeSinceStartup < timeout)
                yield return null;
            if (!unload.isDone || SceneManager.GetSceneByPath(origin.scenePath).isLoaded)
            {
                yield return Rollback(originFloorId, destinationScene, true,
                    "Origin unload timed out.");
                yield break;
            }

            activeFloorId = targetFloorId;
            destinationFloorId = string.Empty;
            state = S5AFloorTransitionState.Steady;
            transition = null;
            LastTransitionSucceeded = true;
            CompletedTransitionCount++;
            LastResult = "Committed " + targetFloorId + " with both E01 and Stair A apertures.";
            ActiveFloorChanged?.Invoke(targetFloorId);
        }

        private IEnumerator Rollback(string originFloorId, Scene destinationScene,
            bool destinationLoaded, string reason)
        {
            state = S5AFloorTransitionState.RollingBack;
            contract.TryGetFloor(originFloorId, out S2FloorRecord origin);
            Scene originScene = SceneManager.GetSceneByPath(origin.scenePath);
            if (!originScene.IsValid() || !originScene.isLoaded)
            {
                AsyncOperation reload = LoadSceneAdditive(origin.scenePath);
                if (reload != null)
                    while (!reload.isDone)
                        yield return null;
                originScene = SceneManager.GetSceneByPath(origin.scenePath);
            }
            if (originScene.IsValid() && originScene.isLoaded)
            {
                ValidateAndApply(originScene, originFloorId, out _);
                SceneManager.SetActiveScene(originScene);
            }
            if (destinationLoaded && destinationScene.IsValid() && destinationScene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(destinationScene);
                if (unload != null)
                    while (!unload.isDone)
                        yield return null;
            }
            activeFloorId = originFloorId;
            destinationFloorId = string.Empty;
            LastTransitionSucceeded = false;
            RollbackCount++;
            transition = null;
            state = originScene.IsValid() && originScene.isLoaded
                ? S5AFloorTransitionState.Steady : S5AFloorTransitionState.FaultedSafe;
            LastResult = reason + " Last committed floor remains " + originFloorId + ".";
        }

        private bool ValidateAndApply(Scene scene, string floorId, out string detail)
        {
            S2FloorSceneMarker[] markers = scene.IsValid() && scene.isLoaded
                ? scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<S2FloorSceneMarker>(true)).ToArray()
                : Array.Empty<S2FloorSceneMarker>();
            if (markers.Length != 1 || markers[0].FloorId != floorId)
            {
                detail = $"expected one {floorId} marker; found {string.Join(",", markers.Select(item => item.FloorId))}";
                return false;
            }
            bool applied = dualAperture.ApplyToFloorScene(scene, floorId,
                out string applyDetail);
            string validateDetail = "not validated because apply failed";
            bool validated = applied
                && dualAperture.ValidateApplied(scene, floorId, out validateDetail);
            if (!applied || !validated)
            {
                detail = applyDetail + "; " + validateDetail;
                return false;
            }
            string facadeDetail = "F00 entrance boundary retained";
            bool facadeValid = floorId == "F00" || facadeFloorEdges == null
                || facadeFloorEdges.RemapFrontSafetyBoundary(markers[0], out facadeDetail);
            if (!facadeValid)
            {
                detail = applyDetail + "; " + validateDetail + "; " + facadeDetail;
                return false;
            }
            detail = applyDetail + "; " + validateDetail + "; " + facadeDetail;
            return true;
        }

        private bool Reject(string reason)
        {
            RejectedTransitionCount++;
            LastTransitionSucceeded = false;
            LastResult = reason;
            return false;
        }

        private static S2FloorSceneMarker[] LoadedFloorMarkers()
            => FindObjectsByType<S2FloorSceneMarker>(FindObjectsInactive.Include,
                FindObjectsSortMode.None);

        private static AsyncOperation LoadSceneAdditive(string path)
        {
#if UNITY_EDITOR
            return EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Additive));
#else
            return SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
#endif
        }
    }
}
