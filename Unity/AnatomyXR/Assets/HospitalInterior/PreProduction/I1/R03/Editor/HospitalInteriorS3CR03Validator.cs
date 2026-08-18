using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3CR03Validator
    {
        [Serializable]
        private sealed class StaticCheck
        {
            public string name;
            public bool pass;
            public string detail;
        }

        [Serializable]
        private sealed class StaticGate
        {
            public string schema = "HospitalInterior.R03.S3C.StaticGate.v1";
            public string status;
            public string unityVersion;
            public int passCount;
            public int totalCount;
            public StaticCheck[] checks;
        }

        [MenuItem("Hospital Interior/R03 S3C/Validate static gate", priority = 30)]
        public static void ValidateStaticGate()
        {
            var checks = new List<StaticCheck>();
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int s2Count);
            HospitalInteriorS3CR03Builder.AssertLockedS3BInputs(out int s3bCount);
            Add(checks, "approved_s2_baseline_unchanged", s2Count == 38,
                "protected files=" + s2Count + "; mismatches=0");
            Add(checks, "approved_s3b_inputs_unchanged", s3bCount == 12,
                "protected files=" + s3bCount + "; mismatches=0");

            string settingsPath = Path.Combine(HospitalInteriorS3CR03Builder.ProjectRoot(),
                "ProjectSettings", "EditorBuildSettings.asset");
            string settingsHash = HospitalInteriorS3CR03Builder.Sha256(settingsPath);
            Add(checks, "editor_build_settings_untouched",
                settingsHash == "62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb",
                settingsHash);
            Add(checks, "explicit_player_scene_matrix", HospitalInteriorS3CR03Builder.PlayerScenes.Length == 12
                && HospitalInteriorS3CR03Builder.PlayerScenes.Distinct().Count() == 12
                && HospitalInteriorS3CR03Builder.PlayerScenes.All(path
                    => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null),
                string.Join(",", HospitalInteriorS3CR03Builder.PlayerScenes));

            S3ElevatorContract contract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(
                HospitalInteriorS3BR03Builder.ContractPath);
            bool contractReferences = contract != null && contract.ApprovedS1DatumContract != null
                && contract.ApprovedS2FloorContract != null
                && contract.ApprovedS2FloorContract.Floors.Count == 7;
            Add(checks, "approved_contract_reused", contractReferences,
                contract == null ? "missing" : $"floors={contract.ApprovedS2FloorContract?.Floors.Count}");
            if (contract != null)
            {
                bool spatialFit = contract.ValidateSpatialFit(out string spatialDetail);
                bool shaftFit = contract.ValidateShaftFit(out string shaftDetail);
                bool fit = spatialFit && shaftFit;
                Add(checks, "locked_spatial_and_shaft_datums", fit,
                    spatialDetail + "; " + shaftDetail);
                Add(checks, "travel_and_failure_timing_contract",
                    Mathf.Abs(contract.TravelSecondsPerMeter - 0.65f) <= 0.001f
                    && Mathf.Abs(contract.SceneLoadTimeoutSeconds - 20f) <= 0.001f
                    && Mathf.Abs(contract.RecoveryTimeoutSeconds - 10f) <= 0.001f,
                    $"travel={contract.TravelSecondsPerMeter:F2}s/m; load={contract.SceneLoadTimeoutSeconds:F1}s; recovery={contract.RecoveryTimeoutSeconds:F1}s");
            }

            ValidateScene(contract, checks);
            Directory.CreateDirectory(HospitalInteriorS3CR03Builder.ReviewFolder());
            var gate = new StaticGate
            {
                status = checks.All(item => item.pass) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                checks = checks.ToArray(),
            };
            string output = Path.Combine(HospitalInteriorS3CR03Builder.ReviewFolder(),
                "StageI1_R03_S3C_StaticGate.json");
            File.WriteAllText(output, JsonUtility.ToJson(gate, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3C_STATIC_GATE={gate.status}; {gate.passCount}/{gate.totalCount}; {output}");
            if (gate.status != "PASS")
                throw new InvalidOperationException("S3C static gate failed: "
                    + string.Join(",", checks.Where(item => !item.pass).Select(item => item.name)));
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidateScene(S3ElevatorContract contract,
            ICollection<StaticCheck> checks)
        {
            Scene scene = EditorSceneManager.OpenScene(HospitalInteriorS3CR03Builder.BootstrapScenePath,
                OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            S3CReviewBootstrap[] bootstraps = roots.SelectMany(item
                => item.GetComponentsInChildren<S3CReviewBootstrap>(true)).ToArray();
            S3ElevatorController[] elevators = roots.SelectMany(item
                => item.GetComponentsInChildren<S3ElevatorController>(true)).ToArray();
            S3CReviewLocomotion[] locomotions = roots.SelectMany(item
                => item.GetComponentsInChildren<S3CReviewLocomotion>(true)).ToArray();
            Add(checks, "single_s3c_runtime_stack", bootstraps.Length == 1
                && elevators.Length == 1 && locomotions.Length == 1,
                $"bootstraps={bootstraps.Length}; elevators={elevators.Length}; locomotions={locomotions.Length}");
            Add(checks, "legacy_review_controllers_absent",
                roots.SelectMany(item => item.GetComponentsInChildren<S3BReviewBootstrap>(true)).Count() == 0
                && roots.SelectMany(item => item.GetComponentsInChildren<S3BReviewLocomotion>(true)).Count() == 0
                && roots.SelectMany(item => item.GetComponentsInChildren<S2ManualFloorLoader>(true)).Count() == 0,
                "No S2 manual or S3B review controller remains in the S3C scene.");

            Transform[] allTransforms = roots.SelectMany(item
                => item.GetComponentsInChildren<Transform>(true)).ToArray();
            string[] missingScriptObjects = allTransforms
                .Where(item => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0)
                .Select(item => item.name + ":"
                    + GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject)).ToArray();
            Add(checks, "no_missing_script_references", missingScriptObjects.Length == 0,
                missingScriptObjects.Length == 0 ? "objects=" + allTransforms.Length
                    : string.Join(",", missingScriptObjects));
            Add(checks, "single_review_camera_listener",
                roots.SelectMany(item => item.GetComponentsInChildren<Camera>(true)).Count() == 1
                && roots.SelectMany(item => item.GetComponentsInChildren<AudioListener>(true)).Count() == 1,
                "Exactly one camera and one audio listener are serialized.");

            if (elevators.Length != 1 || bootstraps.Length != 1 || locomotions.Length != 1)
                return;
            S3ElevatorController elevator = elevators[0];
            S3CReviewBootstrap bootstrap = bootstraps[0];
            S3CReviewLocomotion locomotion = locomotions[0];
            Add(checks, "runtime_references_complete", elevator.Contract == contract
                && bootstrap.ElevatorContract == contract && bootstrap.ElevatorController == elevator
                && bootstrap.XrOrigin != null && bootstrap.ReviewCamera != null
                && bootstrap.Locomotion == locomotion && elevator.CabinRoot != null
                && elevator.PassengerRoot == bootstrap.XrOrigin.transform
                && elevator.FloorAperture != null && elevator.CabinTravelIndicator != null
                && bootstrap.ExteriorBaseShaftCutMesh != null,
                "Contract, bootstrap, cabin, passenger, floor/exterior apertures, and indicator references are assigned.");
            Add(checks, "seven_exact_portals_and_door_controllers",
                elevator.LandingPortalRoots.Length == 7
                && elevator.FloorDoorControllers.Length == 7
                && elevator.LandingPortalRoots.All(item => item != null)
                && elevator.FloorDoorControllers.All(item => item != null)
                && elevator.FloorDoorControllers.Select(item => item.CurrentFloorId)
                    .SequenceEqual(new[] { "F00", "F01", "F02", "F03", "F04", "F05", "F06" })
                && elevator.FloorDoorControllers.Count(item => item.enabled) == 1
                && elevator.FloorDoorControllers[0].enabled,
                "portals=" + elevator.LandingPortalRoots.Length + "; controllers="
                    + elevator.FloorDoorControllers.Length);
            bool sharedCabinLeaves = elevator.FloorDoorControllers.All(item
                => item.MovingLeaves[0] == elevator.FloorDoorControllers[0].MovingLeaves[0]
                    && item.MovingLeaves[1] == elevator.FloorDoorControllers[0].MovingLeaves[1]);
            bool uniqueLandingPairs = elevator.FloorDoorControllers
                .SelectMany(item => item.MovingLeaves.Skip(2)).Distinct().Count() == 14;
            Add(checks, "one_cabin_pair_seven_unique_landing_pairs",
                sharedCabinLeaves && uniqueLandingPairs,
                $"sharedCabin={sharedCabinLeaves}; uniqueLandingLeaves={elevator.FloorDoorControllers.SelectMany(item => item.MovingLeaves.Skip(2)).Distinct().Count()}");

            S3ElevatorButton[] buttons = roots.SelectMany(item
                => item.GetComponentsInChildren<S3ElevatorButton>(true)).ToArray();
            Add(checks, "sixteen_physical_controls_routed_to_s3c", buttons.Length == 16
                && buttons.All(item => item.ElevatorController == elevator && item.DoorController == null)
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.Floor) == 7
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.LandingCall) == 7
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorOpen) == 1
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorClose) == 1,
                "buttons=" + buttons.Length);
            Add(checks, "review_controls_and_scale", contract != null
                && Mathf.Abs(contract.ApprovedS2FloorContract.EyeHeightM - 1.7f) <= 0.001f
                && Mathf.Abs(locomotion.WalkSpeedMps - 2f) <= 0.001f
                && Mathf.Abs(locomotion.SprintSpeedMps - 4f) <= 0.001f,
                $"walk={locomotion.WalkSpeedMps:F1}; sprint={locomotion.SprintSpeedMps:F1}");

            S3ShaftStructure shaft = roots.SelectMany(item
                => item.GetComponentsInChildren<S3ShaftStructure>(true)).SingleOrDefault();
            S3CabinObservationDesign observation = roots.SelectMany(item
                => item.GetComponentsInChildren<S3CabinObservationDesign>(true)).SingleOrDefault();
            S3ShaftFloorAperture aperture = roots.SelectMany(item
                => item.GetComponentsInChildren<S3ShaftFloorAperture>(true)).SingleOrDefault();
            string structureDetail = "shaft missing";
            string sweepDetail = "shaft missing";
            string glassDetail = "shaft missing";
            bool shaftStructureValid = shaft != null && shaft.ValidateStructure(out structureDetail);
            bool shaftSweepValid = shaft != null && shaft.ValidateCabinSweep(out sweepDetail);
            bool shaftGlassValid = shaft != null && shaft.ValidateGlassDesign(out glassDetail);
            bool shaftValid = shaftStructureValid && shaftSweepValid && shaftGlassValid;
            Add(checks, "approved_full_height_glass_shaft_preserved", shaftValid,
                structureDetail + "; " + sweepDetail + "; " + glassDetail);
            string observationDetail = "observation design missing";
            string nestingDetail = "observation design missing";
            bool observationDesignValid = observation != null
                && observation.ValidateObservationDesign(out observationDetail);
            bool observationNestingValid = observation != null && shaft != null
                && observation.ValidateNestedInShaft(shaft, out nestingDetail);
            bool observationValid = observationDesignValid && observationNestingValid;
            Add(checks, "approved_observation_cabin_preserved", observationValid,
                observationDetail + "; " + nestingDetail);
            string apertureDetail = "aperture missing";
            bool apertureValid = aperture != null && aperture.ValidateAssets(out apertureDetail);
            Add(checks, "approved_shaft_apertures_preserved", apertureValid, apertureDetail);
            ValidateContinuousHollowHoistway(contract, aperture, shaft, roots, checks);
            ValidateExteriorBaseF00Hoistway(bootstrap, contract, checks);
            ValidateOpenCabinRoof(elevator.CabinRoot, checks);

            Light[] lights = roots.SelectMany(item => item.GetComponentsInChildren<Light>(true)).ToArray();
            Transform practical = roots.SelectMany(item
                => item.GetComponentsInChildren<Transform>(true)).SingleOrDefault(item
                    => item.name == "S3B_R03_E01_CabinPracticalLight");
            Add(checks, "cabin_practical_moves_with_cabin", lights.Length == 2
                && practical != null && practical.IsChildOf(elevator.CabinRoot),
                "lights=" + lights.Length + "; practicalParent="
                    + (practical == null ? "missing" : practical.parent.name));
            Add(checks, "travel_indicator_is_cabin_owned", elevator.CabinTravelIndicator != null
                && elevator.CabinTravelIndicator.transform.IsChildOf(elevator.CabinRoot)
                && elevator.CabinTravelIndicator.text == "F00",
                elevator.CabinTravelIndicator == null ? "missing"
                    : elevator.CabinTravelIndicator.transform.parent.name);
            Add(checks, "scene_roots_identity", roots.All(item
                => item.transform.position.sqrMagnitude <= 0.000001f
                    && Quaternion.Angle(item.transform.rotation, Quaternion.identity) <= 0.001f
                    && (item.transform.localScale - Vector3.one).sqrMagnitude <= 0.000001f),
                string.Join(",", roots.Select(item => item.name)));
        }

        private static void ValidateContinuousHollowHoistway(S3ElevatorContract contract,
            S3ShaftFloorAperture aperture, S3ShaftStructure shaft, IEnumerable<GameObject> roots,
            ICollection<StaticCheck> checks)
        {
            if (contract == null || aperture == null || shaft == null)
            {
                Add(checks, "f00_f06_hollow_hoistway_storey_structure_and_roof_retained", false,
                    "contract, aperture, or shaft is missing");
                return;
            }

            var floorResults = new List<string>();
            bool everyFloorIsCut = true;
            foreach (S2FloorRecord floor in contract.ApprovedS2FloorContract.Floors)
            {
                Scene floorScene = default;
                try
                {
                    floorScene = EditorSceneManager.OpenScene(floor.scenePath, OpenSceneMode.Additive);
                    bool applied = aperture.ApplyToFloorScene(floorScene, floor.floorId, out _);
                    bool valid = aperture.ValidateAppliedToFloorScene(
                        floorScene, floor.floorId, out string detail);
                    everyFloorIsCut &= applied && valid;
                    floorResults.Add(floor.floorId + "=" + (applied && valid ? "clear" : detail));
                }
                catch (Exception exception)
                {
                    everyFloorIsCut = false;
                    floorResults.Add(floor.floorId + "=" + exception.GetType().Name);
                }
                finally
                {
                    if (floorScene.IsValid() && floorScene.isLoaded)
                        EditorSceneManager.CloseScene(floorScene, true);
                }
            }

            MeshFilter[] persistentUndersides = roots.SelectMany(item
                => item.GetComponentsInChildren<MeshFilter>(true))
                .Where(item => item.name.StartsWith("S3B_R03_StructuralUnderside_",
                    StringComparison.Ordinal)).ToArray();
            bool everyCeilingUndersideIsCut = persistentUndersides.Length == 6
                && persistentUndersides.All(item => item.sharedMesh == aperture.UpperShaftCutUnderside
                    && aperture.ValidateMeshOpening(item.sharedMesh, out _));
            MeshFilter[] persistentFloorPlates = roots.SelectMany(item
                => item.GetComponentsInChildren<MeshFilter>(true))
                .Where(item => item.name.StartsWith("S3C_R03_E01_PersistentFloorPlate_",
                    StringComparison.Ordinal)).ToArray();
            const float renderSurfaceOffsetM = 0.015f;
            bool everyFloorStructureIsPresent = persistentFloorPlates.Length == 7
                && contract.ApprovedS2FloorContract.Floors.All(floor =>
                {
                    MeshFilter plate = persistentFloorPlates.SingleOrDefault(item => item.name
                        == "S3C_R03_E01_PersistentFloorPlate_" + floor.floorId);
                    Mesh expected = floor.floorId == "F00" ? aperture.F00ShaftCutSlab
                        : aperture.UpperShaftCutSlab;
                    return plate != null && plate.sharedMesh == expected
                        && plate.GetComponent<Collider>() == null
                        && Mathf.Abs(plate.transform.position.y
                            - (floor.elevationM - renderSurfaceOffsetM)) <= 0.002f
                        && aperture.ValidateMeshOpening(plate.sharedMesh, out _);
                });
            bool roofRetained = shaft.TopClosure != null
                && Mathf.Abs(shaft.TopClosure.bounds.min.y - (contract.ShaftMaxY - 0.10f)) <= 0.002f
                && Mathf.Abs(shaft.TopClosure.bounds.max.y - contract.ShaftMaxY) <= 0.002f;
            Add(checks, "f00_f06_hollow_hoistway_storey_structure_and_roof_retained",
                everyFloorIsCut && everyCeilingUndersideIsCut && everyFloorStructureIsPresent
                    && roofRetained,
                "floors=" + string.Join(",", floorResults) + "; upperUndersides="
                    + persistentUndersides.Length + "/6; persistentFloorPlates="
                    + persistentFloorPlates.Length + "/7; opening=X 0.300..3.000 Z 3.000..5.700; roof="
                    + roofRetained);
        }

        private static void ValidateExteriorBaseF00Hoistway(S3CReviewBootstrap bootstrap,
            S3ElevatorContract contract, ICollection<StaticCheck> checks)
        {
            Scene exteriorBase = default;
            MeshFilter target = null;
            Mesh originalMesh = null;
            MeshCollider[] originalColliders = Array.Empty<MeshCollider>();
            Mesh[] originalColliderMeshes = Array.Empty<Mesh>();
            MeshCollider probe = null;
            try
            {
                Mesh expected = AssetDatabase.LoadAssetAtPath<Mesh>(
                    HospitalInteriorS3CR03Builder.ExteriorBaseShaftCutMeshPath);
                exteriorBase = EditorSceneManager.OpenScene(
                    HospitalInteriorS2R03Builder.ProductionScenes[0], OpenSceneMode.Additive);
                target = exteriorBase.GetRootGameObjects()
                    .SelectMany(item => item.GetComponentsInChildren<MeshFilter>(true))
                    .SingleOrDefault(item => item.name
                        == HospitalInteriorS3CR03Builder.ExteriorBaseBlockerName);
                if (target == null || expected == null || bootstrap.ExteriorBaseShaftCutMesh != expected)
                    throw new InvalidOperationException("The serialized exterior shaft-cut asset is missing or mismatched.");

                originalMesh = target.sharedMesh;
                originalColliders = target.GetComponents<MeshCollider>();
                originalColliderMeshes = originalColliders.Select(item => item.sharedMesh).ToArray();
                if (!bootstrap.ApplyExteriorBaseShaftAperture(out string applyDetail))
                    throw new InvalidOperationException(applyDetail);
                probe = target.gameObject.AddComponent<MeshCollider>();
                probe.sharedMesh = target.sharedMesh;

                Vector2 min = contract.E01OperatingCellMinXZ;
                Vector2 max = contract.E01OperatingCellMaxXZ;
                float rayStartY = contract.ShaftMinY + 0.01f;
                float rayDistance = contract.ShaftMaxY - 0.12f - rayStartY;
                var blockers = new List<string>();
                for (int ix = 0; ix < 5; ix++)
                for (int iz = 0; iz < 5; iz++)
                {
                    float x = Mathf.Lerp(min.x + 0.02f, max.x - 0.02f, ix / 4f);
                    float z = Mathf.Lerp(min.y + 0.02f, max.y - 0.02f, iz / 4f);
                    if (probe.Raycast(new Ray(new Vector3(x, rayStartY, z), Vector3.up),
                        out RaycastHit hit, rayDistance))
                        blockers.Add($"({x:F3},{z:F3})@{hit.point.y:F3}");
                }
                bool pass = target.sharedMesh == expected && blockers.Count == 0;
                Add(checks, "exterior_base_f00_hoistway_matches_upper_levels", pass,
                    applyDetail + "; verticalSamples=" + (25 - blockers.Count)
                    + "/25 clear; blockers=" + (blockers.Count == 0
                        ? "none" : string.Join(",", blockers)));
            }
            catch (Exception exception)
            {
                Add(checks, "exterior_base_f00_hoistway_matches_upper_levels", false,
                    exception.GetType().Name + ": " + exception.Message);
            }
            finally
            {
                if (probe != null)
                    UnityEngine.Object.DestroyImmediate(probe);
                if (target != null && originalMesh != null)
                    target.sharedMesh = originalMesh;
                for (int i = 0; i < originalColliders.Length; i++)
                    if (originalColliders[i] != null)
                        originalColliders[i].sharedMesh = originalColliderMeshes[i];
                if (exteriorBase.IsValid() && exteriorBase.isLoaded)
                    EditorSceneManager.CloseScene(exteriorBase, true);
            }
        }

        private static void ValidateOpenCabinRoof(Transform cabin, ICollection<StaticCheck> checks)
        {
            if (cabin == null)
            {
                Add(checks, "cabin_roof_open_for_continuous_f00_f06_view", false,
                    "cabin is missing");
                return;
            }

            var results = new List<string>();
            bool open = true;
            foreach (string partName in HospitalInteriorS3CR03Builder.OpenCabinRoofPartNames)
            {
                Transform part = cabin.GetComponentsInChildren<Transform>(true)
                    .SingleOrDefault(item => item.name == partName);
                if (part == null)
                {
                    open = false;
                    results.Add(partName + "=missing");
                    continue;
                }
                int enabledRenderers = part.GetComponentsInChildren<Renderer>(true)
                    .Count(item => item.enabled);
                int enabledColliders = part.GetComponentsInChildren<Collider>(true)
                    .Count(item => item.enabled);
                bool partOpen = enabledRenderers == 0 && enabledColliders == 0;
                open &= partOpen;
                results.Add(partName + "=" + (partOpen ? "open" : "renderers="
                    + enabledRenderers + ";colliders=" + enabledColliders));
            }
            Add(checks, "cabin_roof_open_for_continuous_f00_f06_view", open,
                string.Join(",", results) + "; building roof remains separately validated above F06");
        }

        private static void Add(ICollection<StaticCheck> checks, string name,
            bool pass, string detail)
            => checks.Add(new StaticCheck { name = name, pass = pass, detail = detail });
    }
}
