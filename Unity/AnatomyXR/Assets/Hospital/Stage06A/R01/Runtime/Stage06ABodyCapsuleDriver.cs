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
    public sealed class Stage06ABodyCapsuleDriver : MonoBehaviour
    {
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private float radius = 0.25f;
        [SerializeField] private float fallbackEyeHeight = 1.7f;

        public float DesiredHeight { get; private set; } = 1.85f;

        public void Configure(XROrigin origin, Camera camera, CharacterController controller)
        {
            xrOrigin = origin;
            xrCamera = camera;
            characterController = controller;
        }

        private void LateUpdate()
        {
            if (xrOrigin == null || xrCamera == null || characterController == null)
                return;

            Vector3 localHead = xrOrigin.Origin.transform.InverseTransformPoint(xrCamera.transform.position);
            float eyeHeight = localHead.y > 0.5f ? localHead.y : fallbackEyeHeight;
            DesiredHeight = Mathf.Clamp(eyeHeight + 0.15f, 1.2f, 2.2f);
            characterController.radius = radius;
            characterController.height = Mathf.Max(DesiredHeight, radius * 2f);
            characterController.center = new Vector3(localHead.x, characterController.height * 0.5f, localHead.z);
        }
    }
}

