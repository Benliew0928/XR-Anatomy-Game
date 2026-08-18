using System;
using UnityEditor;
using UnityEngine;

namespace CutMyBodyPlease.Hospital.Stage05.Editor
{
    /// <summary>
    /// Deterministic Stage 5 import policy for the immutable R40 FBX payload.
    /// The source FBX bytes are never modified; Unity import behaviour lives in .meta files.
    /// </summary>
    public sealed class HospitalStage05ImportPolicy : AssetPostprocessor
    {
        public const string SourceFolder = "Assets/Hospital/SourceFBX/R40/";
        public const string MaterialFolder = "Assets/Hospital/Materials/";

        private bool IsHospitalSourceModel =>
            assetPath.StartsWith(SourceFolder, StringComparison.OrdinalIgnoreCase) &&
            assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessModel()
        {
            if (!IsHospitalSourceModel)
                return;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1.0f;
            importer.useFileScale = true;
            importer.bakeAxisConversion = true;

            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.importAnimation = false;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importBlendShapes = false;
            importer.importConstraints = false;
            importer.preserveHierarchy = true;

            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.generateSecondaryUV = false;
            importer.isReadable = false;

            // OnAssignMaterialModel below remaps every authored slot to the controlled
            // external library. No FBX-local or per-model material asset is accepted.
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
            importer.materialLocation = ModelImporterMaterialLocation.External;
        }

        private Material OnAssignMaterialModel(Material sourceMaterial, Renderer renderer)
        {
            if (!IsHospitalSourceModel)
                return null;

            string materialPath = MaterialFolder + sourceMaterial.name + ".mat";
            Material shared = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (shared == null)
            {
                throw new InvalidOperationException(
                    $"Stage 5 import rejected unresolved material '{sourceMaterial.name}' " +
                    $"on renderer '{renderer.name}' in '{assetPath}'. Expected '{materialPath}'.");
            }

            return shared;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsHospitalSourceModel)
                return;

            if (root.GetComponentsInChildren<Camera>(true).Length != 0)
                Debug.LogError($"Stage 5 import policy found an unexpected Camera in {assetPath}.");
            if (root.GetComponentsInChildren<Light>(true).Length != 0)
                Debug.LogError($"Stage 5 import policy found an unexpected Light in {assetPath}.");
            if (root.GetComponentsInChildren<Collider>(true).Length != 0)
                Debug.LogError($"Stage 5 import policy found an unexpected generated Collider in {assetPath}.");
        }
    }
}
