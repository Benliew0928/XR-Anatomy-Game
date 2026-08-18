using System;
using System.Collections.Generic;
using System.Linq;
using CutMyBodyPlease.Hospital.Stage06A;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DTeleportSurfaceCoordinator : MonoBehaviour
    {
        [SerializeField] private Stage06ASafeTeleportController teleportController;
        [SerializeField] private S3ElevatorController elevatorController;
        [SerializeField] private S3ElevatorContract elevatorContract;
        [SerializeField] private S3DAdaptiveRig adaptiveRig;

        private string boundFloorId = string.Empty;
        private bool lastPermitted;

        public int BoundSiteSurfaceCount { get; private set; }
        public int BoundFloorSurfaceCount { get; private set; }
        public bool TeleportPermitted { get; private set; }

        public void Configure(Stage06ASafeTeleportController teleporter,
            S3ElevatorController elevator, S3ElevatorContract contract, S3DAdaptiveRig rig)
        {
            teleportController = teleporter;
            elevatorController = elevator;
            elevatorContract = contract;
            adaptiveRig = rig;
        }

        private void Update()
        {
            if (teleportController == null || elevatorController == null || adaptiveRig == null
                || !adaptiveRig.EnvironmentReady)
                return;

            bool permitted = adaptiveRig.ActiveMode == S3DIntegrationMode.PCVR
                && !elevatorController.IsBusy && !elevatorController.IsPassengerInsideCabin();
            string activeFloor = elevatorController.ActiveFloorId;
            if (!string.Equals(boundFloorId, activeFloor, StringComparison.Ordinal)
                || permitted != lastPermitted)
                RefreshBindings();
        }

        public bool RefreshBindings()
        {
            if (teleportController == null || elevatorController == null || elevatorContract == null)
                return false;

            string floorId = elevatorController.ActiveFloorId;
            var allowlist = new List<MeshCollider>();
            BoundSiteSurfaceCount = 0;
            BoundFloorSurfaceCount = 0;

            if (string.Equals(floorId, "F00", StringComparison.Ordinal))
            {
                allowlist.AddRange(FindSiteRouteSurfaces());
                BoundSiteSurfaceCount = allowlist.Count;
            }

            MeshCollider floor = FindActiveFloorSlab(floorId);
            if (floor != null)
            {
                allowlist.Add(floor);
                BoundFloorSurfaceCount = 1;
            }

            teleportController.BindRouteSurfaces(allowlist);
            TeleportPermitted = adaptiveRig != null
                && adaptiveRig.ActiveMode == S3DIntegrationMode.PCVR
                && !elevatorController.IsBusy && !elevatorController.IsPassengerInsideCabin();
            teleportController.enabled = TeleportPermitted;
            boundFloorId = floorId;
            lastPermitted = TeleportPermitted;
            return BoundFloorSurfaceCount == 1
                && (floorId == "F00" ? BoundSiteSurfaceCount == 20 : BoundSiteSurfaceCount == 0);
        }

        private static MeshCollider[] FindSiteRouteSurfaces()
        {
            Scene core = SceneManager.GetSceneByPath(S3DIntegrationContract.SiteScenePaths[0]);
            if (!core.IsValid() || !core.isLoaded)
                return Array.Empty<MeshCollider>();
            return core.GetRootGameObjects()
                .Where(root => root.name == "SITE_CoreRoadParking")
                .SelectMany(root => root.GetComponentsInChildren<MeshCollider>(true))
                .Where(item => item.sharedMesh != null && !item.isTrigger)
                .OrderBy(item => HierarchyPath(item.transform), StringComparer.Ordinal)
                .ToArray();
        }

        private MeshCollider FindActiveFloorSlab(string floorId)
        {
            if (!elevatorContract.ApprovedS2FloorContract.TryGetFloor(floorId,
                out S2FloorRecord record))
                return null;
            Scene scene = SceneManager.GetSceneByPath(record.scenePath);
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MeshCollider>(true))
                .SingleOrDefault(item => item.sharedMesh != null && !item.isTrigger);
        }

        private static string HierarchyPath(Transform transform)
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
