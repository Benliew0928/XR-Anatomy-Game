using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S1ElevatorReference : MonoBehaviour
    {
        [SerializeField] private Vector2 boundsX;
        [SerializeField] private Vector2 boundsZ;
        [SerializeField] private Vector2 boundsY;

        public Vector2 BoundsX => boundsX;
        public Vector2 BoundsZ => boundsZ;
        public Vector2 BoundsY => boundsY;

        public void Configure(Vector2 xBounds, Vector2 zBounds, Vector2 yBounds)
        {
            boundsX = xBounds;
            boundsZ = zBounds;
            boundsY = yBounds;
        }
    }
}
