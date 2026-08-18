using System;
using UnityEngine;

namespace CutMyBodyPlease.Hospital.Stage06A
{
    public enum Stage06ALoadProfile
    {
        Combined,
        HospitalOnly,
        SiteOnly,
    }

    public static class Stage06AContract
    {
        public const string Revision = "R01";
        public const string InputAssetName = "Hospital_Stage06A_XR_Actions";
        public const string SessionSchema = "HospitalExterior.Stage06A.R01.RuntimeSession.v1";
        public const string GateSchema = "HospitalExterior.Stage06A.R01.UnityGate.v1";
        public const string ManifestSchema = "HospitalExterior.Stage06A.R01.ProtectedInputManifest.v1";

        public static readonly string[] HospitalScenes =
        {
            "Assets/Hospital/Scenes/Additive/Exterior_Base.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_Tower.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_SidesService.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_InteriorShellPreview.unity",
            "Assets/Hospital/Scenes/Additive/Exterior_Interactions.unity",
        };

        public static readonly string[] SiteScenes =
        {
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SiteCore.unity",
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SitePerimeter.unity",
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SiteLandscape.unity",
        };

        public static readonly Vector3 CombinedSpawn = new Vector3(0f, 0.03f, -99f);
        public static readonly Vector3 HospitalOnlySpawn = new Vector3(4f, 0.03f, -30f);

        public static string[] ScenesFor(Stage06ALoadProfile profile)
        {
            switch (profile)
            {
                case Stage06ALoadProfile.HospitalOnly:
                    return HospitalScenes;
                case Stage06ALoadProfile.SiteOnly:
                    return SiteScenes;
                default:
                    var combined = new string[HospitalScenes.Length + SiteScenes.Length];
                    Array.Copy(HospitalScenes, 0, combined, 0, HospitalScenes.Length);
                    Array.Copy(SiteScenes, 0, combined, HospitalScenes.Length, SiteScenes.Length);
                    return combined;
            }
        }
    }

    [Serializable]
    public sealed class Stage06AWaypointDefinition
    {
        public int index;
        public string name;
        public Vector3 floorPosition;
    }
}
