using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DoorObstruction : MonoBehaviour
    {
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
    public sealed class S3DoorObstructionSensor : MonoBehaviour
    {
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        [SerializeField] private bool reviewForcedObstruction;

        public bool IsObstructed
        {
            get
            {
                occupants.RemoveWhere(item => item == null || !item.enabled || !item.gameObject.activeInHierarchy);
                return reviewForcedObstruction || occupants.Count > 0;
            }
        }

        public int OccupantCount => occupants.Count(item => item != null && item.enabled && item.gameObject.activeInHierarchy);
        public bool ReviewForcedObstruction => reviewForcedObstruction;

        public void SetReviewForcedObstruction(bool obstructed) => reviewForcedObstruction = obstructed;

        private void OnTriggerEnter(Collider other)
        {
            if (IsRelevant(other))
                occupants.Add(other);
        }

        private void OnTriggerExit(Collider other) => occupants.Remove(other);

        private void OnDisable()
        {
            occupants.Clear();
            reviewForcedObstruction = false;
        }

        private static bool IsRelevant(Collider other) => other != null
            && (other.GetComponentInParent<CharacterController>() != null
                || other.GetComponentInParent<S3DoorObstruction>() != null);
    }
}
