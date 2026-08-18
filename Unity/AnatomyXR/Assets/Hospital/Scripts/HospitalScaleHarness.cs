using UnityEngine;

namespace CutMyBodyPlease.Hospital.Stage05
{
    /// <summary>Temporary R43 scale/traversal metadata. Not a gameplay controller.</summary>
    public sealed class HospitalScaleHarness : MonoBehaviour
    {
        [SerializeField] private float eyeHeightMeters = 1.7f;
        [SerializeField] private string target = "Quest 2 / Quest 3 PC VR via Link or Air Link";
        [SerializeField] private Transform eyeReference;
        [SerializeField] private Transform[] routeReferences;

        public float EyeHeightMeters => eyeHeightMeters;
        public string Target => target;
        public Transform EyeReference => eyeReference;
        public Transform[] RouteReferences => routeReferences;

        public void Configure(float eyeHeight, Transform eye, Transform[] routes)
        {
            eyeHeightMeters = eyeHeight;
            eyeReference = eye;
            routeReferences = routes;
        }
    }
}
