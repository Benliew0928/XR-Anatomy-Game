using System;
using UnityEditor;
using UnityEngine;

namespace CutMyBodyPlease.HospitalSite.Stage05S.Editor
{
    /// <summary>Strict, non-destructive import policy for the checksum-frozen R05B payload.</summary>
    public sealed class HospitalStage05SImportPolicy : AssetPostprocessor
    {
        public const string SourceFolder = "Assets/HospitalSite/Stage05S/R05B/SourceFBX/";
        public const string MaterialFolder = "Assets/HospitalSite/Stage05S/R05B/Materials/";
        public const string HospitalMaterialFolder = "Assets/Hospital/Materials/";

        private bool IsSiteModel => assetPath.StartsWith(SourceFolder, StringComparison.OrdinalIgnoreCase) &&
                                    assetPath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);

        private void OnPreprocessModel()
        {
            if (!IsSiteModel) return;
            var importer = (ModelImporter)assetImporter;
            importer.globalScale = 1f;
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
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialName = ModelImporterMaterialName.BasedOnMaterialName;
            importer.materialSearch = ModelImporterMaterialSearch.Everywhere;
            importer.materialLocation = ModelImporterMaterialLocation.External;
        }

        private Material OnAssignMaterialModel(Material sourceMaterial, Renderer renderer)
        {
            if (!IsSiteModel) return null;
            if (sourceMaterial.name == "No Name" &&
                (renderer.name.IndexOf("Collision", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 renderer.name.IndexOf("COL_", StringComparison.OrdinalIgnoreCase) >= 0))
                return AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + "MAT_S05S_S5_KerbServiceConcrete.mat");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialFolder + sourceMaterial.name + ".mat") ??
                                AssetDatabase.LoadAssetAtPath<Material>(HospitalMaterialFolder + sourceMaterial.name + ".mat");
            if (material == null)
                throw new InvalidOperationException($"S6 rejected unresolved/duplicate material '{sourceMaterial.name}' on " +
                                                    $"'{renderer.name}' in '{assetPath}'.");
            return material;
        }

        private void OnPostprocessModel(GameObject root)
        {
            if (!IsSiteModel) return;
            if (root.GetComponentsInChildren<Camera>(true).Length != 0 ||
                root.GetComponentsInChildren<Light>(true).Length != 0 ||
                root.GetComponentsInChildren<Collider>(true).Length != 0)
                Debug.LogError("S6 source import created a forbidden camera, light, or automatic collider: " + assetPath);
        }
    }
}
