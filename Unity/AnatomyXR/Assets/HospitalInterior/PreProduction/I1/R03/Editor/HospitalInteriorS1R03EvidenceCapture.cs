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
    public static class HospitalInteriorS1R03EvidenceCapture
    {
        [Serializable]
        private sealed class EvidenceRecord
        {
            public string schema = "HospitalInterior.R03.S1.EvidenceRecord.v1";
            public string status;
            public string unityVersion;
            public string[] files;
            public string[] sha256;
            public long[] bytes;
            public string note;
        }

        private static readonly string[] EvidenceNames =
        {
            "01_S1_ExteriorInteriorOverlay.png",
            "02_S1_EntranceThreshold_PlayerEye.png",
            "03_S1_AllFloor_LongitudinalSection.png",
            "04_S1_ElevatorCoreAlignment.png",
        };

        [MenuItem("Hospital Interior/R03 S1/Capture four-view evidence", priority = 30)]
        public static void Capture()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(HospitalInteriorS1R03Builder.BootstrapScenePath) == null)
                HospitalInteriorS1R03Builder.BuildIntegrationScene();

            string review = HospitalInteriorS1R03Builder.ReviewFolder();
            Directory.CreateDirectory(review);
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            var rendererStates = new List<RendererState>();
            Material ghostMaterial = null;
            Material captureDatumMaterial = null;
            Material captureCageMaterial = null;
            try
            {
                Scene bootstrap = EditorSceneManager.OpenScene(HospitalInteriorS1R03Builder.BootstrapScenePath, OpenSceneMode.Single);
                foreach (string path in HospitalInteriorS1R03Builder.ProductionScenes)
                    EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

                Camera camera = bootstrap.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Single();
                S1DiagnosticRoot diagnostic = bootstrap.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<S1DiagnosticRoot>(true)).Single();
                camera.enabled = true;
                camera.fieldOfView = 64f;
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 350f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.055f, 0.075f, 0.095f, 1f);

                Renderer[] productionRenderers = HospitalInteriorS1R03Builder.ProductionScenes
                    .Select(SceneManager.GetSceneByPath)
                    .SelectMany(scene => scene.GetRootGameObjects())
                    .SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
                Renderer[] diagnosticRenderers = diagnostic.GetComponentsInChildren<Renderer>(true);
                rendererStates.AddRange(productionRenderers.Concat(diagnosticRenderers).Distinct()
                    .Select(renderer => new RendererState(renderer)));

                // Evidence-only ghosting keeps the approved exterior readable while exposing the
                // internal datum/cage diagnostics. These transient materials are never saved.
                ghostMaterial = CreateCaptureMaterial("S1_Capture_ExteriorGhost", new Color(0.62f, 0.69f, 0.77f, 0.16f), 3000);
                captureDatumMaterial = CreateCaptureMaterial("S1_Capture_Datums", new Color(0.02f, 0.92f, 1f, 0.78f), 3100);
                captureCageMaterial = CreateCaptureMaterial("S1_Capture_E01", new Color(1f, 0.32f, 0.015f, 1f), 3110);
                SetMaterial(diagnostic.GetComponentsInChildren<S1DatumProxy>(true)
                    .SelectMany(proxy => proxy.GetComponentsInChildren<Renderer>(true)), captureDatumMaterial);
                SetMaterial(diagnostic.GetComponentInChildren<S1ElevatorReference>(true)
                    .GetComponentsInChildren<Renderer>(true), captureCageMaterial);

                SetDiagnosticVisibility(diagnostic.transform, true, true);
                SetMaterial(productionRenderers, ghostMaterial);
                CaptureView(camera, new Vector3(58f, 38f, -62f), new Vector3(-4f, 13f, 0f),
                    Path.Combine(review, EvidenceNames[0]), false, 0f);

                RestoreRenderers(rendererStates, productionRenderers);
                SetDiagnosticVisibility(diagnostic.transform, false, false);
                S1DatumProxy f00 = diagnostic.GetComponentsInChildren<S1DatumProxy>(true).Single(proxy => proxy.FloorId == "F00");
                foreach (Renderer renderer in f00.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                CaptureView(camera, new Vector3(4f, 1.7f, -30f), new Vector3(-4f, 1.10f, -18f),
                    Path.Combine(review, EvidenceNames[1]), false, 0f);

                SetDiagnosticVisibility(diagnostic.transform, true, true);
                SetMaterial(productionRenderers, ghostMaterial);
                CaptureView(camera, new Vector3(-9f, 14.5f, -70f), new Vector3(-9f, 14.5f, 0f),
                    Path.Combine(review, EvidenceNames[2]), true, 17.2f);

                SetDiagnosticVisibility(diagnostic.transform, false, true);
                foreach (S1DatumProxy proxy in diagnostic.GetComponentsInChildren<S1DatumProxy>(true))
                    foreach (Renderer renderer in proxy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
                CaptureView(camera, new Vector3(34f, 33f, -32f), new Vector3(3f, 13.5f, 4.35f),
                    Path.Combine(review, EvidenceNames[3]), true, 19.5f);
            }
            finally
            {
                RestoreRenderers(rendererStates, rendererStates.Select(state => state.Renderer));
                if (ghostMaterial != null) UnityEngine.Object.DestroyImmediate(ghostMaterial);
                if (captureDatumMaterial != null) UnityEngine.Object.DestroyImmediate(captureDatumMaterial);
                if (captureCageMaterial != null) UnityEngine.Object.DestroyImmediate(captureCageMaterial);
                RestoreSceneSetupOrCreateEmpty(setup);
            }

            string[] paths = EvidenceNames.Select(name => Path.Combine(review, name)).ToArray();
            bool pass = paths.All(path => File.Exists(path) && new FileInfo(path).Length > 10000);
            var record = new EvidenceRecord
            {
                status = pass ? "CAPTURED_PENDING_USER_VISUAL_REVIEW" : "FAIL",
                unityVersion = Application.unityVersion,
                files = EvidenceNames,
                sha256 = paths.Select(path => File.Exists(path) ? HospitalInteriorS1R03Builder.Sha256(path) : string.Empty).ToArray(),
                bytes = paths.Select(path => File.Exists(path) ? new FileInfo(path).Length : 0L).ToArray(),
                note = "01/03/04 use transient unsaved exterior ghosting to expose diagnostics. Capture completeness does not grant visual approval.",
            };
            File.WriteAllText(Path.Combine(review, "StageI1_R03_S1_EvidenceRecord.json"), JsonUtility.ToJson(record, true));
            if (!pass)
                throw new InvalidOperationException("One or more required S1 evidence images was not captured correctly.");
            Debug.Log("HOSPITAL_INTERIOR_R03_S1_EVIDENCE=CAPTURED_PENDING_USER_VISUAL_REVIEW; " + review);
        }

        public static void CaptureBatch()
        {
            try { Capture(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void SetDiagnosticVisibility(Transform diagnostic, bool datums, bool cage)
        {
            foreach (Renderer renderer in diagnostic.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            if (datums)
                foreach (S1DatumProxy proxy in diagnostic.GetComponentsInChildren<S1DatumProxy>(true))
                    foreach (Renderer renderer in proxy.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            if (cage)
            {
                S1ElevatorReference reference = diagnostic.GetComponentInChildren<S1ElevatorReference>(true);
                foreach (Renderer renderer in reference.GetComponentsInChildren<Renderer>(true)) renderer.enabled = true;
            }
        }

        private static void CaptureView(Camera camera, Vector3 position, Vector3 focus, string output,
            bool orthographic, float orthographicSize)
        {
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.LookRotation((focus - position).normalized, Vector3.up);
            camera.orthographic = orthographic;
            if (orthographic)
                camera.orthographicSize = orthographicSize;
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

        private static Material CreateCaptureMaterial(string name, Color color, int renderQueue)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader is unavailable for S1 evidence capture.");
            var material = new Material(shader) { name = name, renderQueue = renderQueue };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            return material;
        }

        private static void SetMaterial(IEnumerable<Renderer> renderers, Material material)
        {
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterials = Enumerable.Repeat(material, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
        }

        private static void RestoreRenderers(IEnumerable<RendererState> states, IEnumerable<Renderer> renderers)
        {
            var requested = new HashSet<Renderer>(renderers);
            foreach (RendererState state in states.Where(state => state.Renderer != null && requested.Contains(state.Renderer)))
            {
                state.Renderer.enabled = state.Enabled;
                state.Renderer.sharedMaterials = state.Materials;
            }
        }

        private sealed class RendererState
        {
            public readonly Renderer Renderer;
            public readonly bool Enabled;
            public readonly Material[] Materials;

            public RendererState(Renderer renderer)
            {
                Renderer = renderer;
                Enabled = renderer.enabled;
                Materials = renderer.sharedMaterials;
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
