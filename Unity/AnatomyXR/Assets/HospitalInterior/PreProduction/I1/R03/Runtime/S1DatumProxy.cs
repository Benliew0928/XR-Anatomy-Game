using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S1DatumProxy : MonoBehaviour
    {
        [SerializeField] private string floorId;
        [SerializeField] private float elevationM;
        [SerializeField] private Vector2 boundsX;
        [SerializeField] private Vector2 boundsZ;
        [SerializeField] private bool productionDerivedOutline;

        public string FloorId => floorId;
        public float ElevationM => elevationM;
        public Vector2 BoundsX => boundsX;
        public Vector2 BoundsZ => boundsZ;
        public bool ProductionDerivedOutline => productionDerivedOutline;

        public void Configure(string id, float elevation, Vector2 xBounds, Vector2 zBounds, bool derivedOutline)
        {
            floorId = id;
            elevationM = elevation;
            boundsX = xBounds;
            boundsZ = zBounds;
            productionDerivedOutline = derivedOutline;
        }
    }

}
