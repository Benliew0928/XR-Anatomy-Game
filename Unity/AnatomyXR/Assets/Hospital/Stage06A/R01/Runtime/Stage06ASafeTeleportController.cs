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
    public sealed class Stage06ASafeTeleportController : MonoBehaviour
    {
        private const int ArcSegments = 32;
        private const float ArcStepSeconds = 0.08f;
        private const float LaunchSpeed = 12f;

        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Transform aimSource;
        [SerializeField] private Camera xrCamera;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Stage06ABodyCapsuleDriver bodyDriver;
        [SerializeField] private TeleportationProvider teleportationProvider;
        [SerializeField] private LineRenderer line;
        [SerializeField] private GameObject reticle;
        [SerializeField] private Renderer reticleRenderer;
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private Stage06ATraversalRecorder recorder;

        private readonly Vector3[] arcPoints = new Vector3[ArcSegments];
        private readonly RaycastHit[] rayHits = new RaycastHit[32];
        private readonly Collider[] overlapResults = new Collider[64];
        private readonly List<MeshCollider> routeSurfaces = new List<MeshCollider>(20);
        private readonly List<TeleportationArea> runtimeAreas = new List<TeleportationArea>(20);
        private InputAction aimAction;
        private InputAction cancelAction;
        private bool aiming;
        private bool validLanding;
        private Vector3 landingPoint;
        private Vector3 landingNormal;

        public int BoundRouteSurfaceCount => routeSurfaces.Count;
        public bool IsAiming => aiming;
        public bool HasValidLanding => validLanding;

        public void Configure(InputActionAsset inputActions, Transform source, Camera camera,
            CharacterController controller, Stage06ABodyCapsuleDriver capsuleDriver,
            TeleportationProvider provider, LineRenderer trajectory, GameObject landingReticle,
            Renderer landingRenderer, Material valid, Material invalid, Stage06ATraversalRecorder sessionRecorder)
        {
            actions = inputActions;
            aimSource = source;
            xrCamera = camera;
            characterController = controller;
            bodyDriver = capsuleDriver;
            teleportationProvider = provider;
            line = trajectory;
            reticle = landingReticle;
            reticleRenderer = landingRenderer;
            validMaterial = valid;
            invalidMaterial = invalid;
            recorder = sessionRecorder;
            ResolveActions();
            SetVisuals(false);
        }

        public void BindRouteSurfaces(IEnumerable<MeshCollider> colliders)
        {
            routeSurfaces.Clear();
            runtimeAreas.Clear();
            foreach (MeshCollider collider in colliders.Where(item => item != null)
                         .OrderBy(item => Stage06ASceneBootstrap.HierarchyPath(item.transform), StringComparer.Ordinal))
            {
                routeSurfaces.Add(collider);
                TeleportationArea area = collider.GetComponent<TeleportationArea>();
                if (area == null)
                    area = collider.gameObject.AddComponent<TeleportationArea>();
                area.teleportationProvider = teleportationProvider;
                area.matchOrientation = MatchOrientation.None;
                area.teleportTrigger = BaseTeleportationInteractable.TeleportTrigger.OnSelectExited;
                area.filterSelectionByHitNormal = true;
                runtimeAreas.Add(area);
            }
        }

        private void Awake()
        {
            ResolveActions();
            SetVisuals(false);
        }

        private void ResolveActions()
        {
            aimAction = actions?.FindAction("Locomotion/TeleportAim", false);
            cancelAction = actions?.FindAction("Locomotion/TeleportCancel", false);
        }

        private void Update()
        {
            if (aimAction == null || routeSurfaces.Count == 0)
            {
                SetVisuals(false);
                return;
            }

            if (cancelAction != null && cancelAction.WasPressedThisFrame())
            {
                aiming = false;
                validLanding = false;
                recorder?.RecordTeleportAttempt(false, "cancelled", Vector3.zero);
                SetVisuals(false);
                return;
            }

            if (aimAction.WasPressedThisFrame())
                aiming = true;

            if (aiming && aimAction.IsPressed())
            {
                EvaluateArc();
                SetVisuals(true);
            }

            if (!aiming || !aimAction.WasReleasedThisFrame())
                return;

            if (validLanding)
            {
                bool queued = teleportationProvider.QueueTeleportRequest(new TeleportRequest
                {
                    destinationPosition = landingPoint + landingNormal * 0.02f,
                    destinationRotation = Quaternion.identity,
                    matchOrientation = MatchOrientation.None,
                    requestTime = Time.time,
                });
                recorder?.RecordTeleportAttempt(queued, queued ? "valid_route_surface" : "provider_rejected", landingPoint);
            }
            else
            {
                recorder?.RecordTeleportAttempt(false, "invalid_or_blocked_surface", landingPoint);
            }

            aiming = false;
            validLanding = false;
            SetVisuals(false);
        }

        private void EvaluateArc()
        {
            validLanding = false;
            landingPoint = aimSource.position + aimSource.forward * 2f;
            landingNormal = Vector3.up;
            arcPoints[0] = aimSource.position;
            Vector3 velocity = aimSource.forward * LaunchSpeed;
            int used = 1;

            for (int i = 1; i < ArcSegments; i++)
            {
                float t = i * ArcStepSeconds;
                Vector3 next = aimSource.position + velocity * t + Physics.gravity * (0.5f * t * t);
                Vector3 segment = next - arcPoints[i - 1];
                if (TryGetFirstRelevantHit(arcPoints[i - 1], segment, out RaycastHit hit))
                {
                    arcPoints[i] = hit.point;
                    used = i + 1;
                    landingPoint = hit.point;
                    landingNormal = hit.normal;
                    validLanding = routeSurfaces.Contains(hit.collider as MeshCollider) &&
                                   ValidateLanding(hit, routeSurfaces, characterController,
                                       bodyDriver != null ? bodyDriver.DesiredHeight : 2.1f, overlapResults);
                    break;
                }

                arcPoints[i] = next;
                used = i + 1;
            }

            line.positionCount = used;
            line.SetPositions(arcPoints);
            if (reticle != null)
            {
                reticle.transform.position = landingPoint + landingNormal * 0.015f;
                reticle.transform.rotation = Quaternion.FromToRotation(Vector3.up, landingNormal);
            }
            if (reticleRenderer != null)
                reticleRenderer.sharedMaterial = validLanding ? validMaterial : invalidMaterial;
            line.startColor = line.endColor = validLanding ? new Color(0.12f, 1f, 0.32f, 1f) : new Color(1f, 0.12f, 0.08f, 1f);
        }

        private bool TryGetFirstRelevantHit(Vector3 origin, Vector3 segment, out RaycastHit result)
        {
            result = default;
            float distance = segment.magnitude;
            if (distance <= 0.0001f)
                return false;
            int count = Physics.RaycastNonAlloc(origin, segment / distance, rayHits, distance, ~0, QueryTriggerInteraction.Ignore);
            float closest = float.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                Collider collider = rayHits[i].collider;
                if (collider == null || collider == characterController || collider.transform.IsChildOf(transform.root))
                    continue;
                if (rayHits[i].distance < closest)
                {
                    closest = rayHits[i].distance;
                    result = rayHits[i];
                    found = true;
                }
            }
            return found;
        }

        public static bool ValidateLanding(RaycastHit hit, IReadOnlyCollection<MeshCollider> allowlist,
            CharacterController character, float desiredHeight, Collider[] buffer)
        {
            if (hit.collider == null || !(hit.collider is MeshCollider route) || !allowlist.Contains(route))
                return false;
            if (Vector3.Angle(hit.normal, Vector3.up) > 45f)
                return false;

            float radius = character != null ? character.radius : 0.25f;
            float height = Mathf.Clamp(desiredHeight, 1.2f, 2.2f);
            Vector3 bottom = hit.point + Vector3.up * (radius + 0.04f);
            Vector3 top = hit.point + Vector3.up * Mathf.Max(radius + 0.05f, height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, buffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider overlap = buffer[i];
                if (overlap == null || overlap == character || allowlist.Contains(overlap as MeshCollider))
                    continue;
                return false;
            }
            return true;
        }

        private void SetVisuals(bool visible)
        {
            if (line != null)
                line.enabled = visible;
            if (reticle != null)
                reticle.SetActive(visible);
        }
    }
}

