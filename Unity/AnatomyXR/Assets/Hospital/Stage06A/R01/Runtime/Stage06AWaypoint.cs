using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CutMyBodyPlease.Hospital.Stage06A
{
    public sealed class Stage06AWaypoint : MonoBehaviour
    {
        [SerializeField] private int index;
        [SerializeField] private string waypointName;

        public int Index => index;
        public string WaypointName => waypointName;

        public void Configure(int routeIndex, string routeName)
        {
            index = routeIndex;
            waypointName = routeName;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<XROrigin>() == null)
                return;
            Stage06ATraversalRecorder recorder = FindFirstObjectByType<Stage06ATraversalRecorder>();
            // Triggers are centered one metre above the route so the capsule crosses them reliably.
            // Recovery checkpoints must retain the route floor, not the trigger's elevated center.
            recorder?.EnterWaypoint(index, waypointName, transform.position - Vector3.up);
        }
    }
}
