using System;
using UnityEngine;

namespace CutMyBodyPlease.HospitalSite.Stage05S
{
    [CreateAssetMenu(menuName = "Hospital Site/Stage 05S/Grass Runtime Config")]
    public sealed class HospitalSiteGrassRuntimeConfig : ScriptableObject
    {
        [Header("Immutable S5B contract")]
        public string schema = "HospitalExterior.Stage05S.GrassRuntimeSpec.v1";
        public string sourceSha256 = "68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d";
        public TextAsset sourceContract;

        [Header("Runtime assets")]
        public Mesh lod0Patch;
        public Mesh lod1Patch;
        public Material bladeMaterial;
        public Texture2D coverageMask;

        [Header("World-space contract")]
        public Vector2 siteMinimum = new Vector2(-90.0f, -100.0f);
        public Vector2 siteMaximum = new Vector2(90.0f, 75.0f);
        public float surfaceHeight = 0.08f;
        public float tileSize = 2.0f;
        public float lod0Radius = 5.0f;
        public float lod1Radius = 12.0f;
        public float fadeStart = 10.5f;
        public int maximumActiveTriangles = 65000;
        public int maximumDrawSubmissions = 4;
        public int lod0PatchTriangles = 768;
        public int lod1PatchTriangles = 320;

        [Header("Editor-baked occupancy")]
        public int occupancyWidth;
        public int occupancyHeight;
        [SerializeField] private byte[] occupancy = Array.Empty<byte>();

        public int OccupancyLength => occupancy?.Length ?? 0;

        public void SetOccupancy(int width, int height, byte[] values)
        {
            occupancyWidth = width;
            occupancyHeight = height;
            occupancy = values == null ? Array.Empty<byte>() : (byte[])values.Clone();
        }

        public bool IsOccupied(int x, int z)
        {
            if (occupancy == null || x < 0 || z < 0 || x >= occupancyWidth || z >= occupancyHeight)
                return false;
            return occupancy[z * occupancyWidth + x] != 0;
        }

        public bool IsContractValid(out string detail)
        {
            bool valid = schema == "HospitalExterior.Stage05S.GrassRuntimeSpec.v1" &&
                         sourceSha256 == "68df8c9fd460dcadc1f42b12d1009338493eb398bfc8d012246f882d611e8f7d" &&
                         lod0Patch != null && lod1Patch != null && bladeMaterial != null && coverageMask != null &&
                         Mathf.Approximately(tileSize, 2.0f) && Mathf.Approximately(lod0Radius, 5.0f) &&
                         Mathf.Approximately(lod1Radius, 12.0f) && maximumActiveTriangles == 65000 &&
                         maximumDrawSubmissions == 4 && occupancyWidth == 90 && occupancyHeight == 88 &&
                         OccupancyLength == occupancyWidth * occupancyHeight;
            detail = $"schema={schema}; meshes={lod0Patch != null}/{lod1Patch != null}; mask={coverageMask != null}; " +
                     $"grid={occupancyWidth}x{occupancyHeight}; occupied={CountOccupied()}; bytes={OccupancyLength}";
            return valid;
        }

        public int CountOccupied()
        {
            if (occupancy == null) return 0;
            int count = 0;
            for (int index = 0; index < occupancy.Length; index++)
                if (occupancy[index] != 0) count++;
            return count;
        }
    }
}
