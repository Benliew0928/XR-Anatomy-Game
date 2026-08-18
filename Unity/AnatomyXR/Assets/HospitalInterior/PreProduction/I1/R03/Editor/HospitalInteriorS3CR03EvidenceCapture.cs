using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3CR03EvidenceCapture
    {
        private sealed class View
        {
            public string file;
            public int floorIndex;
            public Vector3 positionAtDatum;
            public Vector3 targetAtDatum;
            public bool doorsOpen;
            public bool orthographic;
            public bool coreOnly;
            public bool shaftSection;
            public float sizeOrFov;
        }

        [MenuItem("Hospital Interior/R03 S3C/Capture travel endpoint evidence", priority = 40)]
        public static void Capture()
        {
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out _);
            Directory.CreateDirectory(EvidenceFolder());

            Scene bootstrapScene = EditorSceneManager.OpenScene(
                HospitalInteriorS3CR03Builder.BootstrapScenePath, OpenSceneMode.Single);
            foreach (string path in HospitalInteriorS2R03Builder.ProductionScenes)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            S2ManualFloorLoader.HideExteriorOnlyBalconyBackers();
            S2ManualFloorLoader.HideExteriorOverlappingBalconyFloorSkins();

            S3CReviewBootstrap reviewBootstrap = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3CReviewBootstrap>(true)).Single();
            if (!reviewBootstrap.ApplyExteriorBaseShaftAperture(out string exteriorApertureDetail))
                throw new InvalidOperationException("S3C evidence exterior aperture failed: "
                    + exteriorApertureDetail);

            S3ElevatorController elevator = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3ElevatorController>(true)).Single();
            S3ElevatorContract contract = elevator.Contract;
            S3ShaftFloorAperture aperture = elevator.FloorAperture;
            S3DoorController[] doors = elevator.FloorDoorControllers;
            Transform cabin = elevator.CabinRoot;
            S3ShaftStructure shaft = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3ShaftStructure>(true)).SingleOrDefault();
            if (contract == null || aperture == null || cabin == null || doors.Length != 7 || shaft == null)
                throw new InvalidOperationException("S3C evidence references are incomplete.");

            reviewBootstrap.enabled = false;
            foreach (S3CReviewLocomotion locomotion in bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3CReviewLocomotion>(true)))
                locomotion.enabled = false;

            Camera[] existingCameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera existing in existingCameras)
                existing.enabled = false;

            GameObject cameraObject = new GameObject("S3C_R03_EvidenceCamera");
            SceneManager.MoveGameObjectToScene(cameraObject, bootstrapScene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 350f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.032f, 0.040f, 1f);
            camera.allowHDR = true;
            camera.allowMSAA = true;

            var views = new[]
            {
                new View
                {
                    file = "01_F00_Ready.png", floorIndex = 0,
                    positionAtDatum = new Vector3(1.65f, 1.48f, -0.10f),
                    targetAtDatum = new Vector3(1.65f, 1.36f, 4.35f),
                    doorsOpen = true, sizeOrFov = 56f,
                },
                new View
                {
                    file = "02_F03_Arrival.png", floorIndex = 3,
                    positionAtDatum = new Vector3(1.65f, 1.58f, 3.92f),
                    targetAtDatum = new Vector3(2.72f, 1.62f, 5.24f),
                    doorsOpen = false, sizeOrFov = 68f,
                },
                new View
                {
                    file = "03_F06_Arrival.png", floorIndex = 6,
                    positionAtDatum = new Vector3(4.35f, 1.60f, 2.00f),
                    targetAtDatum = new Vector3(1.65f, 1.30f, 4.35f),
                    doorsOpen = false, sizeOrFov = 58f,
                },
                new View
                {
                    file = "04_F00_Return.png", floorIndex = 0,
                    positionAtDatum = new Vector3(0.92f, 1.36f, 3.28f),
                    targetAtDatum = new Vector3(2.71f, 1.18f, 4.30f),
                    doorsOpen = true, sizeOrFov = 60f,
                },
                new View
                {
                    file = "05_F03_ShaftTravelContext.png", floorIndex = 3,
                    positionAtDatum = new Vector3(-10f, 0.60f, 4.35f),
                    targetAtDatum = new Vector3(1.65f, 0.60f, 4.35f),
                    doorsOpen = false, orthographic = true, coreOnly = true,
                    shaftSection = true, sizeOrFov = 15.0f,
                },
                new View
                {
                    file = "06_F00_OpenCabinRoof_Hoistway.png", floorIndex = 0,
                    positionAtDatum = new Vector3(1.65f, 1.48f, 4.35f),
                    targetAtDatum = new Vector3(1.65f, 18.0f, 4.35f),
                    doorsOpen = false, sizeOrFov = 64f,
                },
            };

            Renderer[] initialRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            var originalRendererStates = initialRenderers.ToDictionary(item => item, item => item.enabled);
            var shaftSectionCuts = new HashSet<Renderer>(shaft.LandingSpandrels
                .Select(item => item == null ? null : item.GetComponent<Renderer>())
                .Where(item => item != null));
            Renderer leftShaftWall = shaft.LeftWall == null ? null : shaft.LeftWall.GetComponent<Renderer>();
            if (leftShaftWall != null)
                shaftSectionCuts.Add(leftShaftWall);

            Scene loadedFloor = default;
            foreach (View view in views)
            {
                if (loadedFloor.IsValid())
                    EditorSceneManager.CloseScene(loadedFloor, true);
                loadedFloor = EditorSceneManager.OpenScene(
                    HospitalInteriorS2R03Builder.FloorScenePaths[view.floorIndex], OpenSceneMode.Additive);
                S2FloorRecord floor = contract.ApprovedS2FloorContract.Floors[view.floorIndex];
                bool apertureApplied = aperture.ApplyToFloorScene(
                    loadedFloor, floor.floorId, out string apertureDetail);
                bool apertureValidated = aperture.ValidateAppliedToFloorScene(
                    loadedFloor, floor.floorId, out string validationDetail);
                if (!apertureApplied || !apertureValidated)
                    throw new InvalidOperationException("S3C evidence cannot use an uncut shaft floor: "
                        + apertureDetail + "; " + validationDetail);

                Vector3 cabinLocal = cabin.localPosition;
                cabinLocal.y = floor.elevationM + contract.ModelRootVerticalOffsetM;
                cabin.localPosition = cabinLocal;
                if (elevator.CabinTravelIndicator != null)
                    elevator.CabinTravelIndicator.text = floor.floorId;

                foreach (S3DoorController door in doors)
                {
                    door.SetGateManualTick(true);
                    door.SetImmediateClosedForGate();
                }
                if (view.doorsOpen)
                    doors[view.floorIndex].SetImmediateOpenForGate();

                Renderer[] currentRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (Renderer renderer in currentRenderers)
                {
                    if (!originalRendererStates.TryGetValue(renderer, out bool originallyEnabled))
                    {
                        originallyEnabled = renderer.enabled;
                        originalRendererStates.Add(renderer, originallyEnabled);
                    }
                    bool enabled = originallyEnabled && (!view.coreOnly
                        || renderer.transform.IsChildOf(elevator.transform));
                    if (view.shaftSection && shaftSectionCuts.Contains(renderer))
                        enabled = false;
                    renderer.enabled = enabled;
                }

                camera.orthographic = view.orthographic;
                if (view.orthographic)
                    camera.orthographicSize = view.sizeOrFov;
                else
                camera.fieldOfView = view.sizeOrFov;
                Vector3 datumOffset = Vector3.up * floor.elevationM;
                camera.transform.position = view.positionAtDatum + datumOffset;
                Vector3 target = view.targetAtDatum + datumOffset;
                Vector3 forward = (target - camera.transform.position).normalized;
                // A vertical look cannot use world-up as its roll reference. Use the
                // shaft's Z axis instead so the open-car-roof evidence is readable.
                Vector3 cameraUp = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f
                    ? Vector3.forward : Vector3.up;
                camera.transform.rotation = Quaternion.LookRotation(forward, cameraUp);
                Physics.SyncTransforms();
                Render(camera, Path.Combine(EvidenceFolder(), view.file));
            }

            foreach (S3DoorController door in doors)
            {
                door.SetImmediateClosedForGate();
                door.SetGateManualTick(false);
            }
            foreach (KeyValuePair<Renderer, bool> entry in originalRendererStates)
                if (entry.Key != null)
                    entry.Key.enabled = entry.Value;
            UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (Camera existing in existingCameras)
                if (existing != null)
                    existing.enabled = true;
            Debug.Log("HOSPITAL_INTERIOR_R03_S3C_EVIDENCE=PASS; views=" + views.Length);
        }

        public static void CaptureBatch()
        {
            try { Capture(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static string EvidenceFolder()
            => Path.Combine(HospitalInteriorS3CR03Builder.ReviewFolder(), "RuntimeEvidence");

        private static void Render(Camera camera, string path)
        {
            const int width = 1600;
            const int height = 1000;
            RenderTexture target = RenderTexture.GetTemporary(
                width, height, 24, RenderTextureFormat.ARGB32);
            target.antiAliasing = 4;
            camera.targetTexture = target;
            RenderTexture previous = RenderTexture.active;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            texture.Apply(false, false);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
