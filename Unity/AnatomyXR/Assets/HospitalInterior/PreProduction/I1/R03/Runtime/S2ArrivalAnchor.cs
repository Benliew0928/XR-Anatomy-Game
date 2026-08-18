using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S2ArrivalAnchor : MonoBehaviour
    {
        [SerializeField] private string floorId;
        [SerializeField] private Vector3 safeZoneSize = new Vector3(3f, 2f, 2f);

        public string FloorId => floorId;
        public Vector3 SafeZoneSize => safeZoneSize;

        public void Configure(string id, Vector3 zoneSize)
        {
            floorId = id;
            safeZoneSize = zoneSize;
        }
    }
}
