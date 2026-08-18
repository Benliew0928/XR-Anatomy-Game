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
    public sealed class Stage06AControllerTrackingVisual : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private string trackedActionPath;
        [SerializeField] private GameObject visualRoot;
        private InputAction trackedAction;

        public bool TrackingObserved { get; private set; }

        public void Configure(InputActionAsset inputActions, string actionPath, GameObject visuals)
        {
            actions = inputActions;
            trackedActionPath = actionPath;
            visualRoot = visuals;
            trackedAction = actions?.FindAction(trackedActionPath, false);
        }

        private void Awake() => trackedAction = actions?.FindAction(trackedActionPath, false);

        private void Update()
        {
            bool tracked = trackedAction != null && trackedAction.ReadValue<float>() > 0.5f;
            TrackingObserved |= tracked;
            if (visualRoot != null && visualRoot.activeSelf != tracked)
                visualRoot.SetActive(tracked);
        }
    }
}

