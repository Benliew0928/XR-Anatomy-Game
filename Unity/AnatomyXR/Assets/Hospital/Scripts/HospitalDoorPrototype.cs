using UnityEngine;

namespace CutMyBodyPlease.Hospital.Stage05
{
    /// <summary>
    /// Frozen R43 metadata for an animation-ready door leaf. This component does
    /// not animate the door; gameplay behaviour remains out of Stage 5 scope.
    /// </summary>
    public sealed class HospitalDoorPrototype : MonoBehaviour
    {
        [SerializeField] private string sourceRootName;
        [SerializeField] private string motionType;
        [SerializeField] private Vector3 closedLocalPosition;
        [SerializeField] private Quaternion closedLocalRotation = Quaternion.identity;
        [SerializeField] private Vector3 motionAxis = Vector3.right;
        [SerializeField] private float openDistanceMeters;
        [SerializeField] private float openAngleDegrees;
        [SerializeField] private float verifiedClearanceMeters;

        public string SourceRootName => sourceRootName;
        public string MotionType => motionType;
        public Vector3 ClosedLocalPosition => closedLocalPosition;
        public Quaternion ClosedLocalRotation => closedLocalRotation;
        public Vector3 MotionAxis => motionAxis;
        public float OpenDistanceMeters => openDistanceMeters;
        public float OpenAngleDegrees => openAngleDegrees;
        public float VerifiedClearanceMeters => verifiedClearanceMeters;

        public void Configure(string rootName, string type, Vector3 closedPosition, Quaternion closedRotation,
            Vector3 axis, float openDistance, float openAngle, float clearance)
        {
            sourceRootName = rootName;
            motionType = type;
            closedLocalPosition = closedPosition;
            closedLocalRotation = closedRotation;
            motionAxis = axis.normalized;
            openDistanceMeters = openDistance;
            openAngleDegrees = openAngle;
            verifiedClearanceMeters = clearance;
        }
    }
}
