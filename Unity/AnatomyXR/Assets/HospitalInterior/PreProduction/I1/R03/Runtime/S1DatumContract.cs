using System;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [Serializable]
    public sealed class S1FloorDatumRecord
    {
        public string floorId;
        public float elevationM;
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public string footprintSource;
    }

    [Serializable]
    public sealed class S1ElevatorReferenceRecord
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
        public float minY;
        public float maxY;
    }

    [CreateAssetMenu(menuName = "Hospital Interior/R03/S1 Datum Contract")]
    public sealed class S1DatumContract : ScriptableObject
    {
        public const string Revision = "R03";
        public const string Stage = "S1";

        [SerializeField] private string[] productionScenePaths = Array.Empty<string>();
        [SerializeField] private S1FloorDatumRecord[] floorDatums = Array.Empty<S1FloorDatumRecord>();
        [SerializeField] private S1ElevatorReferenceRecord elevatorReference = new S1ElevatorReferenceRecord();
        [SerializeField] private Vector3 hospitalOnlySpawn = new Vector3(4f, 0.03f, -30f);
        [SerializeField] private float eyeHeightM = 1.7f;
        [SerializeField] private float walkSpeedMps = 2f;
        [SerializeField] private float sprintSpeedMps = 4f;

        public string[] ProductionScenePaths => productionScenePaths;
        public S1FloorDatumRecord[] FloorDatums => floorDatums;
        public S1ElevatorReferenceRecord ElevatorReference => elevatorReference;
        public Vector3 HospitalOnlySpawn => hospitalOnlySpawn;
        public float EyeHeightM => eyeHeightM;
        public float WalkSpeedMps => walkSpeedMps;
        public float SprintSpeedMps => sprintSpeedMps;

        public void Configure(string[] scenes, S1FloorDatumRecord[] datums, S1ElevatorReferenceRecord elevator,
            Vector3 spawn, float eyeHeight, float walkSpeed, float sprintSpeed)
        {
            productionScenePaths = scenes ?? Array.Empty<string>();
            floorDatums = datums ?? Array.Empty<S1FloorDatumRecord>();
            elevatorReference = elevator ?? new S1ElevatorReferenceRecord();
            hospitalOnlySpawn = spawn;
            eyeHeightM = eyeHeight;
            walkSpeedMps = walkSpeed;
            sprintSpeedMps = sprintSpeed;
        }
    }
}
