using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S5AAdaptiveInteractionRouter : MonoBehaviour
    {
        [SerializeField] private S3DAdaptiveRig adaptiveRig;
        [SerializeField] private Camera reviewCamera;
        [SerializeField] private Transform rightController;
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private LineRenderer interactionLine;
        [SerializeField] private float interactionDistanceM = 5.5f;
        [SerializeField] private float aimAssistRadiusM = 0.18f;

        private InputAction activateAction;
        private bool environmentReady;

        public string LastInteraction { get; private set; } = string.Empty;
        public float InteractionDistanceM => interactionDistanceM;
        public float AimAssistRadiusM => aimAssistRadiusM;

        public void Configure(S3DAdaptiveRig rig, Camera camera, Transform controller,
            InputActionAsset inputActions, LineRenderer line)
        {
            adaptiveRig = rig;
            reviewCamera = camera;
            rightController = controller;
            actions = inputActions;
            interactionLine = line;
            interactionDistanceM = 5.5f;
            aimAssistRadiusM = 0.18f;
            activateAction = actions?.FindAction("Interaction/Activate", false);
        }

        public void NotifyEnvironmentReady() => environmentReady = true;

        private void Update()
        {
            if (!environmentReady || adaptiveRig == null || reviewCamera == null)
                return;
            if (adaptiveRig.ActiveMode == S3DIntegrationMode.Desktop)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                    Activate(new Ray(reviewCamera.transform.position, reviewCamera.transform.forward));
                return;
            }
            if (rightController == null)
                return;
            activateAction ??= actions?.FindAction("Interaction/Activate", false);
            Ray ray = new Ray(rightController.position, rightController.forward);
            bool found = TryFind(ray, out _, out RaycastHit hit);
            if (interactionLine != null)
            {
                interactionLine.enabled = found;
                interactionLine.positionCount = found ? 2 : 0;
                if (found)
                {
                    interactionLine.SetPosition(0, ray.origin);
                    interactionLine.SetPosition(1, hit.point);
                }
            }
            if (activateAction != null && activateAction.WasPressedThisFrame())
                Activate(ray);
        }

        private void Activate(Ray ray)
        {
            if (!TryFind(ray, out IS5AInteractable target, out _))
            {
                LastInteraction = "No S5A circulation control in reach.";
                return;
            }
            target.ActivateInteraction();
            LastInteraction = target.LastInteractionResult;
        }

        private bool TryFind(Ray ray, out IS5AInteractable target, out RaycastHit hit)
        {
            target = null;
            hit = default;
            RaycastHit[] assisted = Physics.SphereCastAll(ray, aimAssistRadiusM,
                interactionDistanceM, ~0, QueryTriggerInteraction.Collide);
            RaycastHit[] blockers = Physics.RaycastAll(ray, interactionDistanceM, ~0,
                QueryTriggerInteraction.Ignore);
            float nearestBlocker = blockers.Length == 0 ? float.PositiveInfinity
                : blockers.Min(item => item.distance);
            var candidates = assisted.Select(item => new
                {
                    Hit = item,
                    Target = item.collider == null ? null : item.collider
                        .GetComponentsInParent<MonoBehaviour>(true)
                        .OfType<IS5AInteractable>().FirstOrDefault(),
                    Offset = item.collider == null ? float.PositiveInfinity
                        : Vector3.Cross(ray.direction.normalized,
                            item.collider.bounds.center - ray.origin).magnitude,
                })
                .Where(item => item.Target != null
                    && item.Hit.distance <= nearestBlocker + aimAssistRadiusM + 0.12f)
                .OrderBy(item => item.Offset).ThenBy(item => item.Hit.distance).ToArray();
            if (candidates.Length == 0)
                return false;
            target = candidates[0].Target;
            hit = candidates[0].Hit;
            return true;
        }
    }
}
