using System;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace CutMyBodyPlease.Hospital.Stage05.Editor
{
    /// <summary>
    /// Freezes the Stage 5 XR choice to Windows OpenXR for Quest Link/Air Link.
    /// It intentionally creates no Android XR settings.
    /// </summary>
    public static class HospitalStage05XRSetup
    {
        private const string SettingsAssetPath =
            "Assets/Hospital/Settings/XRGeneralSettingsPerBuildTarget.asset";

        [MenuItem("Hospital/Stage 5/Configure Windows OpenXR")]
        public static void Configure()
        {
            XRGeneralSettingsPerBuildTarget perTarget = GetOrCreatePerTargetSettings();
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Standalone))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
            XRManagerSettings manager = perTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Standalone);
            if (general == null || manager == null)
                throw new InvalidOperationException("Failed to create Standalone XR Management settings.");

            general.InitManagerOnStart = true;
            bool assigned = XRPackageMetadataStore.AssignLoader(
                manager, typeof(OpenXRLoader).FullName, BuildTargetGroup.Standalone);
            if (!assigned && !manager.activeLoaders.OfType<OpenXRLoader>().Any())
                throw new InvalidOperationException("Failed to assign the OpenXR loader for Standalone.");

            OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            if (openXr == null)
                throw new InvalidOperationException("Standalone OpenXR settings were not created.");

            OculusTouchControllerProfile quest2 = openXr.GetFeature<OculusTouchControllerProfile>();
            MetaQuestTouchPlusControllerProfile quest3 = openXr.GetFeature<MetaQuestTouchPlusControllerProfile>();
            if (quest2 == null || quest3 == null)
                throw new InvalidOperationException("Quest controller interaction profiles are unavailable.");

            quest2.enabled = true;
            quest3.enabled = true;
            openXr.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;

            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(openXr);
            EditorUtility.SetDirty(quest2);
            EditorUtility.SetDirty(quest3);
            AssetDatabase.SaveAssets();

            if (!IsValid(out string detail))
                throw new InvalidOperationException("OpenXR validation failed: " + detail);
            Debug.Log("STAGE05_WINDOWS_OPENXR=CONFIGURED " + detail);
        }

        public static bool IsValid(out string detail)
        {
            XRGeneralSettings general =
                XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
            XRManagerSettings manager = general == null ? null : general.Manager;
            OpenXRSettings openXr = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
            bool openXrLoader = manager != null && manager.activeLoaders.OfType<OpenXRLoader>().Any();
            bool onlyOpenXrLoader = manager != null && manager.activeLoaders.Count == 1 && openXrLoader;
            bool quest2 = openXr != null &&
                          openXr.GetFeature<OculusTouchControllerProfile>()?.enabled == true;
            bool quest3 = openXr != null &&
                          openXr.GetFeature<MetaQuestTouchPlusControllerProfile>()?.enabled == true;
            bool singlePass = openXr != null &&
                              openXr.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced;
            bool initOnStart = general != null && general.InitManagerOnStart;

            detail = $"openxr_loader={openXrLoader}; only_loader={onlyOpenXrLoader}; " +
                     $"quest2_profile={quest2}; quest3_profile={quest3}; " +
                     $"single_pass_instanced={singlePass}; init_on_start={initOnStart}";
            return onlyOpenXrLoader && quest2 && quest3 && singlePass && initOnStart;
        }

        private static XRGeneralSettingsPerBuildTarget GetOrCreatePerTargetSettings()
        {
            if (EditorBuildSettings.TryGetConfigObject(
                    XRGeneralSettings.settingsKey,
                    out XRGeneralSettingsPerBuildTarget current) && current != null)
            {
                return current;
            }

            XRGeneralSettingsPerBuildTarget asset =
                AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(SettingsAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(asset, SettingsAssetPath);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, asset, true);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
