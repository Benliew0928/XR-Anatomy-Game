using System;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    public enum S3DIntegrationMode
    {
        Auto,
        Desktop,
        PCVR,
    }

    [Serializable]
    public sealed class S3DCheckRecord
    {
        public string name;
        public bool pass;
        public string detail;
    }

    [Serializable]
    public sealed class S3DTravelRecord
    {
        public string origin;
        public string destination;
        public bool accepted;
        public bool exactDatum;
        public bool oneActiveFloor;
        public bool persistentScenesRetained;
        public bool passengerStayedInCabin;
        public float finalCabinElevationM;
        public float passengerDriftM;
    }

    [Serializable]
    public sealed class S3DRuntimeGateReport
    {
        public string schema = "HospitalInterior.R03.S3D.RuntimeGate.v1";
        public string status;
        public string unityVersion;
        public string integrationMode;
        public int passCount;
        public int totalCount;
        public string[] route;
        public string[] loadedPersistentScenes;
        public S3DTravelRecord[] travelRecords;
        public S3DCheckRecord[] checks;
    }

    public static class S3DIntegrationContract
    {
        public const string Revision = "R03";
        public const string Stage = "S3D";
        public const string RuntimeSchema = "HospitalInterior.R03.S3D.RuntimeGate.v1";
        public const string StaticSchema = "HospitalInterior.R03.S3D.StaticGate.v1";
        public const string BuildSchema = "HospitalInterior.R03.S3D.BuildRecord.v1";

        public static readonly Vector3 GateSpawn = new Vector3(0f, 0.03f, -99f);
        public static readonly Vector3 LobbyCheckpoint = new Vector3(0f, 0.03f, -42f);
        public static readonly Vector3 EntranceCheckpoint = new Vector3(-4f, 0.03f, -24.5f);

        public static readonly string[] SiteScenePaths =
        {
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SiteCore.unity",
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SitePerimeter.unity",
            "Assets/HospitalSite/Stage05S/R05B/Scenes/Additive/Exterior_SiteLandscape.unity",
        };

        public static readonly string[] RequiredRoute =
        {
            "F00", "F01", "F02", "F03", "F04", "F05", "F06", "F00",
        };
    }
}
