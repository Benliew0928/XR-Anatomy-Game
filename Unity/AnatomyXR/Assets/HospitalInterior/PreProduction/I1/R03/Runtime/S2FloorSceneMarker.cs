using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S2FloorSceneMarker : MonoBehaviour
    {
        [SerializeField] private string floorId;
        [SerializeField] private float elevationM;

        public string FloorId => floorId;
        public float ElevationM => elevationM;

        public void Configure(string id, float elevation)
        {
            floorId = id;
            elevationM = elevation;
        }
    }
}
