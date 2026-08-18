using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S2FloorRecord
    {
        public string floorId;
        public string scenePath;
        public float elevationM;
        public bool polygonFootprint;
        public Vector2[] footprint = Array.Empty<Vector2>();
        public Vector2[] integrationCoverageProbes = Array.Empty<Vector2>();
        public string footprintSource;
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public Vector3 arrivalPosition;
        public Vector3 arrivalEuler;
        public Vector3 labelPosition;
        public Vector3 labelEuler;
        public bool hasEntranceOpening;
        public float entranceOpeningMinX;
        public float entranceOpeningMaxX;
    }

    [CreateAssetMenu(menuName = "Hospital Interior/R03/S2 Floor Contract")]
    public sealed class S2FloorContract : ScriptableObject
    {
        public const string Revision = "R03";
        public const string Stage = "S2";

        [SerializeField] private string[] productionScenePaths = Array.Empty<string>();
        [SerializeField] private S2FloorRecord[] floors = Array.Empty<S2FloorRecord>();
        [SerializeField] private string initialFloorId = "F00";
        [SerializeField] private float eyeHeightM = 1.7f;
        [SerializeField] private float walkSpeedMps = 2f;
        [SerializeField] private float sprintSpeedMps = 4f;

        public string[] ProductionScenePaths => productionScenePaths;
        public IReadOnlyList<S2FloorRecord> Floors => floors;
        public string InitialFloorId => initialFloorId;
        public float EyeHeightM => eyeHeightM;
        public float WalkSpeedMps => walkSpeedMps;
        public float SprintSpeedMps => sprintSpeedMps;

        public void Configure(string[] productionScenes, S2FloorRecord[] floorRecords, string initial,
            float eyeHeight, float walkSpeed, float sprintSpeed)
        {
            productionScenePaths = productionScenes ?? Array.Empty<string>();
            floors = floorRecords ?? Array.Empty<S2FloorRecord>();
            initialFloorId = string.IsNullOrWhiteSpace(initial) ? "F00" : initial;
            eyeHeightM = eyeHeight;
            walkSpeedMps = walkSpeed;
            sprintSpeedMps = sprintSpeed;
        }

        public bool TryGetFloor(string floorId, out S2FloorRecord record)
        {
            record = floors.FirstOrDefault(item => string.Equals(item.floorId, floorId, StringComparison.Ordinal));
            return record != null;
        }
    }
}
