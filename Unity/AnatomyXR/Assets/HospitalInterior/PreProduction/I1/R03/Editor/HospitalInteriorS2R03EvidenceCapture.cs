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
    public static class HospitalInteriorS2R03EvidenceCapture
    {
        [Serializable]
        private sealed class EvidenceRecord
        {
            public string schema = "HospitalInterior.R03.S2.EvidenceRecord.v1";
            public string status;
            public string unityVersion;
            public string[] files;
            public string[] sha256;
            public long[] bytes;
            public string note;
        }

        public static readonly string[] EvidenceNames = HospitalInteriorS2R03Builder.FloorIds
            .Select((floor, index) => (index + 1).ToString("00") + "_S2_" + floor + "_ElevatorArrival.png").ToArray();

        public static readonly string[] SeamEvidenceNames =
        {
            "08_S2_F01_BalconySeam_Corrected.png",
            "09_S2_F01_MainGlassSeam_Corrected.png",
            "10_S2_F01_StackSeparation_Corrected.png",
            "11_S2_F04_BalconyJunction_Smoothed.png",
        };

        public static readonly string[] AllEvidenceNames = EvidenceNames.Concat(SeamEvidenceNames).ToArray();

        [MenuItem("Hospital Interior/R03 S2/Capture arrival and seam-correction views", priority = 30)]
        public static void Capture()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HospitalInteriorS2R03Builder.BootstrapScenePath) == null)
                HospitalInteriorS2R03Builder.BuildStage();
            S2FloorContract contract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath);
            if (contract == null || contract.Floors.Count != 7)
                throw new InvalidOperationException("S2 floor contract is unavailable for evidence capture.");

            string review = HospitalInteriorS2R03Builder.ReviewFolder();
            Directory.CreateDirectory(review);
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            var suppressedStates = new Dictionary<Renderer, bool>();
            try
            {
                Scene bootstrap = EditorSceneManager.OpenScene(HospitalInteriorS2R03Builder.BootstrapScenePath, OpenSceneMode.Single);
                foreach (string path in HospitalInteriorS2R03Builder.ProductionScenes)
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                HashSet<string> suppressedNames = new HashSet<string>(S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames
                    .Concat(S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames), StringComparer.Ordinal);
                foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,
                             FindObjectsSortMode.None).Where(renderer => suppressedNames.Contains(renderer.name)))
                {
                    suppressedStates[renderer] = renderer.enabled;
                    renderer.enabled = false;
                }
                int expectedSuppressed = S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length
                    + S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length;
                if (suppressedStates.Count != expectedSuppressed)
                    throw new InvalidOperationException($"Expected {expectedSuppressed} balcony overlap/backer renderers for evidence suppression; found {suppressedStates.Count}.");

                Camera camera = bootstrap.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
                camera.enabled = true;
                camera.fieldOfView = 68f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 300f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);

                for (int i = 0; i < contract.Floors.Count; i++)
                {
                    S2FloorRecord floor = contract.Floors[i];
                    Scene floorScene = EditorSceneManager.OpenScene(floor.scenePath, OpenSceneMode.Additive);
                    S2FloorSceneMarker marker = floorScene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<S2FloorSceneMarker>(true)).Single();
                    S2ArrivalAnchor arrival = marker.GetComponentInChildren<S2ArrivalAnchor>(true);
                    TextMesh label = marker.GetComponentInChildren<TextMesh>(true);
                    camera.transform.position = arrival.transform.position + new Vector3(0f, contract.EyeHeightM, 0f);
                    Vector3 focus = new Vector3(label.transform.position.x, floor.elevationM + 1.75f, label.transform.position.z + 6f);
                    camera.transform.rotation = Quaternion.LookRotation((focus - camera.transform.position).normalized, Vector3.up);
                    CaptureView(camera, Path.Combine(review, EvidenceNames[i]));
                    EditorSceneManager.CloseScene(floorScene, true);
                }

                S2FloorRecord seamFloor = contract.Floors[1];
                Scene seamScene = EditorSceneManager.OpenScene(seamFloor.scenePath, OpenSceneMode.Additive);
                camera.fieldOfView = 62f;

                camera.transform.position = new Vector3(-26.0f, seamFloor.elevationM + 1.9f, -11.5f);
                Vector3 balconyFocus = new Vector3(-35.4f, seamFloor.elevationM + 0.05f, -16.25f);
                camera.transform.rotation = Quaternion.LookRotation((balconyFocus - camera.transform.position).normalized, Vector3.up);
                CaptureView(camera, Path.Combine(review, SeamEvidenceNames[0]));

                camera.transform.position = new Vector3(13.0f, seamFloor.elevationM + 1.75f, -11.5f);
                Vector3 glassFocus = new Vector3(17.75f, seamFloor.elevationM + 0.08f, -11.5f);
                camera.transform.rotation = Quaternion.LookRotation((glassFocus - camera.transform.position).normalized, Vector3.up);
                CaptureView(camera, Path.Combine(review, SeamEvidenceNames[1]));

                camera.transform.position = new Vector3(-26.0f, seamFloor.elevationM + 1.7f, -11.5f);
                Vector3 stackFocus = new Vector3(-30.0f, contract.Floors[2].elevationM - 0.20f, -14.5f);
                camera.transform.rotation = Quaternion.LookRotation((stackFocus - camera.transform.position).normalized, Vector3.up);
                CaptureView(camera, Path.Combine(review, SeamEvidenceNames[2]));
                EditorSceneManager.CloseScene(seamScene, true);

                S2FloorRecord junctionFloor = contract.Floors[4];
                Scene junctionScene = EditorSceneManager.OpenScene(junctionFloor.scenePath, OpenSceneMode.Additive);
                camera.fieldOfView = 68f;
                camera.transform.position = new Vector3(-20.0f, junctionFloor.elevationM + 1.70f, -2.5f);
                Vector3 junctionFocus = new Vector3(-30.25f, junctionFloor.elevationM + 1.05f, -10.5f);
                camera.transform.rotation = Quaternion.LookRotation((junctionFocus - camera.transform.position).normalized, Vector3.up);
                CaptureView(camera, Path.Combine(review, SeamEvidenceNames[3]));
                EditorSceneManager.CloseScene(junctionScene, true);
            }
            finally
            {
                foreach (KeyValuePair<Renderer, bool> state in suppressedStates)
                    if (state.Key != null)
                        state.Key.enabled = state.Value;
                RestoreSceneSetupOrCreateEmpty(setup);
            }

            string[] paths = AllEvidenceNames.Select(name => Path.Combine(review, name)).ToArray();
            bool pass = paths.All(path => File.Exists(path) && new FileInfo(path).Length > 10000);
            var record = new EvidenceRecord
            {
                status = pass ? "CAPTURED_PENDING_USER_VISUAL_REVIEW" : "FAIL",
                unityVersion = Application.unityVersion,
                files = AllEvidenceNames,
                sha256 = paths.Select(path => File.Exists(path) ? HospitalInteriorS2R03Builder.Sha256(path) : string.Empty).ToArray(),
                bytes = paths.Select(path => File.Exists(path) ? new FileInfo(path).Length : 0L).ToArray(),
                note = "Images 01-07 are captured from the corresponding E01 arrival anchors. Images 08-09 verify the corrected F01 balcony and main-glass facade seams; image 10 verifies persistent structural storey separation; image 11 verifies the smoothed F04 balcony floor/soffit junction with duplicate exterior floor skins and exterior-only backers suppressed. Protected exterior sources remain unchanged; the floor-scene allowlist remains one slab plus one Fxx label.",
            };
            File.WriteAllText(Path.Combine(review, "StageI1_R03_S2_EvidenceRecord.json"), JsonUtility.ToJson(record, true));
            if (!pass)
                throw new InvalidOperationException("One or more S2 arrival or seam-correction images was not captured correctly.");
            Debug.Log("HOSPITAL_INTERIOR_R03_S2_EVIDENCE=CAPTURED_PENDING_USER_VISUAL_REVIEW; " + review);
        }

        public static void CaptureBatch()
        {
            try { Capture(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void CaptureView(Camera camera, string output)
        {
            RenderTexture target = RenderTexture.GetTemporary(1600, 900, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                texture.Apply(false, false);
                File.WriteAllBytes(output, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] setup)
        {
            if (setup.Any(item => item.isLoaded && item.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
