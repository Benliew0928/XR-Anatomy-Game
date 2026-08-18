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
    public static class HospitalInteriorS3BR03EvidenceCapture
    {
        private sealed class View
        {
            public string file;
            public Vector3 position;
            public Vector3 target;
            public bool open;
            public bool coreOnly;
            public bool shaftSection;
            public bool showCenterline;
            public bool orthographic;
            public float sizeOrFov;
        }

        [MenuItem("Hospital Interior/R03 S3B/Capture integrated door evidence", priority = 30)]
        public static void Capture()
        {
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out _);
            Directory.CreateDirectory(HospitalInteriorS3BR03Builder.ReviewFolder());
            Scene bootstrapScene = EditorSceneManager.OpenScene(HospitalInteriorS3BR03Builder.BootstrapScenePath, OpenSceneMode.Single);
            foreach (string path in HospitalInteriorS2R03Builder.ProductionScenes)
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            Scene f00Scene = EditorSceneManager.OpenScene(HospitalInteriorS2R03Builder.FloorScenePaths[0], OpenSceneMode.Additive);
            S2ManualFloorLoader.HideExteriorOnlyBalconyBackers();
            S2ManualFloorLoader.HideExteriorOverlappingBalconyFloorSkins();

            S3ShaftFloorAperture floorAperture = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3ShaftFloorAperture>(true)).Single();
            string appliedDetail = "aperture validation was not reached";
            if (!floorAperture.ApplyToFloorScene(f00Scene, "F00", out string apertureDetail)
                || !floorAperture.ValidateAppliedToFloorScene(f00Scene, "F00", out appliedDetail))
                throw new InvalidOperationException("S3B evidence cannot use an uncut shaft floor: "
                    + apertureDetail + "; " + appliedDetail);

            S3DoorController door = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3DoorController>(true)).Single();
            Transform core = bootstrapScene.GetRootGameObjects()[0].transform.Find("S3B_R03_E01_PersistentCore");
            S3ShaftStructure shaft = bootstrapScene.GetRootGameObjects()
                .SelectMany(item => item.GetComponentsInChildren<S3ShaftStructure>(true)).Single();
            Renderer leftShaftWall = shaft.LeftWall.GetComponent<Renderer>();
            var shaftSectionCuts = new HashSet<Renderer>(shaft.LandingSpandrels
                .Select(item => item.GetComponent<Renderer>()).Where(item => item != null)) { leftShaftWall };
            Camera[] existingCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Camera existing in existingCameras)
                existing.enabled = false;

            GameObject cameraObject = new GameObject("S3B_R03_EvidenceCamera");
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
                new View { file = "01_S3B_F00_Integrated_DoorsClosed.png", position = new Vector3(1.65f, 1.48f, -0.10f), target = new Vector3(1.65f, 1.35f, 3.02f), open = false, sizeOrFov = 56f },
                new View { file = "02_S3B_F00_Integrated_DoorsOpen.png", position = new Vector3(1.65f, 1.48f, -0.10f), target = new Vector3(1.65f, 1.36f, 4.35f), open = true, sizeOrFov = 56f },
                new View { file = "03_S3B_FlushThreshold_ClearOpening.png", position = new Vector3(1.65f, 0.72f, 1.65f), target = new Vector3(1.65f, 0.52f, 3.38f), open = true, sizeOrFov = 54f },
                new View { file = "04_S3B_WalkThrough_CabinInterior.png", position = new Vector3(1.65f, 1.55f, 3.35f), target = new Vector3(1.65f, 1.35f, 5.35f), open = true, sizeOrFov = 62f },
                new View { file = "05_S3B_InteractiveControls.png", position = new Vector3(0.92f, 1.36f, 3.28f), target = new Vector3(2.71f, 1.18f, 4.30f), open = true, sizeOrFov = 60f },
                new View { file = "06_S3B_SevenPortal_StoreyFit.png", position = new Vector3(1.65f, 13.0f, -14f), target = new Vector3(1.65f, 13.0f, 3.0f), open = false, coreOnly = true, orthographic = true, sizeOrFov = 14.5f },
                new View { file = "07_S3B_ContinuousShaft_Section.png", position = new Vector3(-11f, 14.2f, -5f), target = new Vector3(1.65f, 14.2f, 4.35f), open = false, coreOnly = true, shaftSection = true, showCenterline = true, orthographic = true, sizeOrFov = 14.8f },
                new View { file = "08_S3B_F00_GlassHoistway_AtriumView.png", position = new Vector3(6.2f, 1.65f, -1.8f), target = new Vector3(1.65f, 7.2f, 3.45f), open = false, sizeOrFov = 63f },
                new View { file = "09_S3B_CabinNestedInGlassShaft.png", position = new Vector3(4.35f, 1.60f, 2.00f), target = new Vector3(1.65f, 1.30f, 4.35f), open = false, sizeOrFov = 58f },
                new View { file = "10_S3B_CabinObservation_Interior.png", position = new Vector3(1.65f, 1.58f, 3.92f), target = new Vector3(2.72f, 1.62f, 5.24f), open = false, sizeOrFov = 68f },
            };

            Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var originalStates = new Dictionary<Renderer, bool>();
            foreach (Renderer renderer in allRenderers)
                originalStates[renderer] = renderer.enabled;

            foreach (View view in views)
            {
                door.SetGateManualTick(true);
                if (view.open) door.SetImmediateOpenForGate(); else door.SetImmediateClosedForGate();
                foreach (KeyValuePair<Renderer, bool> entry in originalStates)
                {
                    bool enabled = view.coreOnly ? entry.Key.transform.IsChildOf(core) && entry.Value : entry.Value;
                    if (view.shaftSection && shaftSectionCuts.Contains(entry.Key))
                        enabled = false;
                    if (view.showCenterline && entry.Key == shaft.TravelCenterlineRenderer)
                        enabled = true;
                    entry.Key.enabled = enabled;
                }
                camera.orthographic = view.orthographic;
                if (view.orthographic) camera.orthographicSize = view.sizeOrFov; else camera.fieldOfView = view.sizeOrFov;
                camera.transform.position = view.position;
                camera.transform.rotation = Quaternion.LookRotation((view.target - view.position).normalized, Vector3.up);
                Physics.SyncTransforms();
                Render(camera, Path.Combine(HospitalInteriorS3BR03Builder.ReviewFolder(), view.file));
            }

            foreach (KeyValuePair<Renderer, bool> entry in originalStates)
                entry.Key.enabled = entry.Value;
            door.SetImmediateClosedForGate();
            door.SetGateManualTick(false);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            foreach (Camera existing in existingCameras)
                if (existing != null)
                    existing.enabled = true;
            Debug.Log("HOSPITAL_INTERIOR_R03_S3B_EVIDENCE=PASS; views=" + views.Length);
        }

        public static void CaptureBatch()
        {
            try { Capture(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void Render(Camera camera, string path)
        {
            const int width = 1600;
            const int height = 1000;
            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
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
