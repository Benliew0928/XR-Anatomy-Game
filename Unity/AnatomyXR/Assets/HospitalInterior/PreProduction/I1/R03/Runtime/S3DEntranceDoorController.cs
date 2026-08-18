using System;
using System.Collections.Generic;
using System.Linq;
using CutMyBodyPlease.Hospital.Stage05;
using UnityEngine;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03
{
    [DisallowMultipleComponent]
    public sealed class S3DEntranceDoorController : MonoBehaviour
    {
        public const int RequiredLeafCount = 8;
        public const float TravelSeconds = 1.2f;
        public const float ClearDwellSeconds = 2f;

        [SerializeField] private Transform playerRoot;
        [SerializeField] private float sensorHalfWidthM = 6f;
        [SerializeField] private float sensorHalfDepthM = 5.5f;
        [SerializeField] private Vector3 sensorCenter = new Vector3(-4f, 1.25f, -21.77f);

        private readonly List<LeafRuntime> leaves = new List<LeafRuntime>(RequiredLeafCount);
        private float openness;
        private float clearSeconds;
        private bool manualTick;
        private bool forcedOccupancy;

        public bool IsInitialized { get; private set; }
        public bool IsOccupied { get; private set; }
        public float Openness => openness;
        public bool FullyOpen => openness >= 0.999f;
        public bool FullyClosed => openness <= 0.001f;
        public int LeafCount => leaves.Count;
        public Bounds SensorBounds => new Bounds(sensorCenter,
            new Vector3(sensorHalfWidthM * 2f, 3.5f, sensorHalfDepthM * 2f));

        public bool TryGetOuterClosedOpeningBounds(out Bounds bounds)
        {
            Collider[] colliders = leaves
                .Where(item => item.prototype.SourceRootName.Contains("_OUTER_"))
                .SelectMany(item => item.transform.GetComponentsInChildren<Collider>(true))
                .Where(item => item.enabled && !item.isTrigger)
                .ToArray();
            if (!IsInitialized || colliders.Length == 0)
            {
                bounds = default;
                return false;
            }
            bounds = colliders[0].bounds;
            for (int index = 1; index < colliders.Length; index++)
                bounds.Encapsulate(colliders[index].bounds);
            return true;
        }

        public void Configure(Transform integrationPlayerRoot)
            => playerRoot = integrationPlayerRoot;

        public bool DiscoverAndInitialize(out string detail)
        {
            leaves.Clear();
            HospitalDoorPrototype[] candidates = FindObjectsByType<HospitalDoorPrototype>(
                FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(item => item.SourceRootName.StartsWith("UE_S03B_DOOR_INNER_SlidingLeaf_",
                                   StringComparison.Ordinal)
                    || item.SourceRootName.StartsWith("UE_S03B_DOOR_OUTER_SlidingLeaf_",
                        StringComparison.Ordinal))
                .OrderBy(item => item.SourceRootName, StringComparer.Ordinal)
                .ToArray();

            if (candidates.Length != RequiredLeafCount)
            {
                detail = "expected eight hero entrance leaves; found " + candidates.Length;
                return false;
            }

            foreach (HospitalDoorPrototype prototype in candidates)
            {
                if (!string.Equals(prototype.MotionType, "HorizontalSlide", StringComparison.Ordinal)
                    || prototype.MotionAxis.sqrMagnitude < 0.99f
                    || prototype.OpenDistanceMeters <= 0f)
                {
                    detail = "invalid entrance motion metadata on " + prototype.SourceRootName;
                    leaves.Clear();
                    return false;
                }

                prototype.transform.localPosition = prototype.ClosedLocalPosition;
                prototype.transform.localRotation = prototype.ClosedLocalRotation;
                leaves.Add(new LeafRuntime
                {
                    prototype = prototype,
                    transform = prototype.transform,
                    closedLocalPosition = prototype.ClosedLocalPosition,
                    closedLocalRotation = prototype.ClosedLocalRotation,
                    motionAxis = prototype.MotionAxis.normalized,
                    openDistanceM = prototype.OpenDistanceMeters,
                });
            }

            openness = 0f;
            clearSeconds = 0f;
            ApplyPose();
            Physics.SyncTransforms();
            IsInitialized = true;
            detail = "leaves=" + leaves.Count + "; travel=1.2s; clearDwell=2.0s; sensor="
                + SensorBounds.center.ToString("F2") + "/" + SensorBounds.size.ToString("F2");
            return true;
        }

        private void Update()
        {
            if (!manualTick && IsInitialized)
                Advance(Time.deltaTime);
        }

        public void SetGateManualTick(bool enabled) => manualTick = enabled;
        public void SetGateOccupancy(bool occupied) => forcedOccupancy = occupied;
        public void AdvanceForGate(float deltaTime) => Advance(deltaTime);

        private void Advance(float deltaTime)
        {
            IsOccupied = forcedOccupancy || PlayerInsideSensor();
            if (IsOccupied)
                clearSeconds = 0f;
            else
                clearSeconds += Mathf.Max(0f, deltaTime);

            bool shouldOpen = IsOccupied || clearSeconds < ClearDwellSeconds;
            float target = shouldOpen ? 1f : 0f;
            openness = Mathf.MoveTowards(openness, target,
                Mathf.Max(0f, deltaTime) / TravelSeconds);
            ApplyPose();
        }

        private bool PlayerInsideSensor()
        {
            if (playerRoot == null)
                return false;
            Vector3 position = playerRoot.position;
            Bounds bounds = SensorBounds;
            return position.x >= bounds.min.x && position.x <= bounds.max.x
                && position.y >= bounds.min.y - 1.5f && position.y <= bounds.max.y
                && position.z >= bounds.min.z && position.z <= bounds.max.z;
        }

        private void ApplyPose()
        {
            float eased = Mathf.SmoothStep(0f, 1f, openness);
            foreach (LeafRuntime leaf in leaves)
            {
                leaf.transform.localPosition = leaf.closedLocalPosition
                    + leaf.motionAxis * (leaf.openDistanceM * eased);
                leaf.transform.localRotation = leaf.closedLocalRotation;
            }
            Physics.SyncTransforms();
        }

        public bool ValidateMetadata(out string detail)
        {
            bool valid = IsInitialized && leaves.Count == RequiredLeafCount
                && leaves.Count(item => item.prototype.SourceRootName.Contains("_INNER_")) == 4
                && leaves.Count(item => item.prototype.SourceRootName.Contains("_OUTER_")) == 4
                && leaves.All(item => string.Equals(item.prototype.MotionType, "HorizontalSlide",
                    StringComparison.Ordinal)
                    && item.openDistanceM > 3f
                    && item.transform.GetComponentsInChildren<Collider>(true).Length > 0);
            detail = "initialized=" + IsInitialized + "; leaves=" + leaves.Count
                + "; inner=" + leaves.Count(item => item.prototype.SourceRootName.Contains("_INNER_"))
                + "; outer=" + leaves.Count(item => item.prototype.SourceRootName.Contains("_OUTER_"));
            return valid;
        }

        public bool ValidateOpenPassage(out string detail)
        {
            var passage = new Bounds(new Vector3(-4f, 1.25f, -21.77f),
                new Vector3(1.20f, 2.5f, 4.2f));
            Collider[] blockers = leaves.SelectMany(item =>
                    item.transform.GetComponentsInChildren<Collider>(true))
                .Where(item => item.enabled && !item.isTrigger && item.bounds.Intersects(passage))
                .ToArray();
            detail = "openness=" + openness.ToString("F3") + "; passage="
                + passage.center.ToString("F2") + "; blockers="
                + string.Join(",", blockers.Select(item => item.name));
            return FullyOpen && blockers.Length == 0;
        }

        private sealed class LeafRuntime
        {
            public HospitalDoorPrototype prototype;
            public Transform transform;
            public Vector3 closedLocalPosition;
            public Quaternion closedLocalRotation;
            public Vector3 motionAxis;
            public float openDistanceM;
        }
    }
}
