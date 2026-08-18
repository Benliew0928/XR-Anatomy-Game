using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    public static class HospitalInteriorS3BR03Validator
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
            public string schema = "HospitalInterior.R03.S3B.StaticGate.v4";
            public string status;
            public string unityVersion;
            public int passCount;
            public int totalCount;
            public StaticCheck[] checks;
        }

        [Serializable]
        private sealed class S1ClashAuthority
        {
            public string status;
            public float f06CeilingBottomM;
            public bool elevatorContainedByUpperEnvelope;
            public string[] elevatorExpectedSlabIntersections;
            public string[] elevatorUnexpectedGeometryIntersections;
        }

        [MenuItem("Hospital Interior/R03 S3B/Validate static gate", priority = 10)]
        public static void ValidateStaticGate()
        {
            var checks = new List<StaticCheck>();
            HospitalInteriorS3BR03Builder.AssertApprovedS2Baseline(out int baselineCount);
            Add(checks, "approved_s2_baseline_unchanged", baselineCount == 38, "files=" + baselineCount + "; mismatches=0");
            ValidateSources(checks);
            S3ElevatorContract contract = ValidateContract(checks);
            ValidateScene(contract, checks);

            bool pass = checks.All(item => item.pass);
            var gate = new StaticGate
            {
                status = pass ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(item => item.pass),
                totalCount = checks.Count,
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(HospitalInteriorS3BR03Builder.ReviewFolder());
            string output = Path.Combine(HospitalInteriorS3BR03Builder.ReviewFolder(), "StageI1_R03_S3B_StaticGate.json");
            File.WriteAllText(output, JsonUtility.ToJson(gate, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S3B_STATIC_GATE={gate.status}; {gate.passCount}/{gate.totalCount}; {output}");
            if (!pass)
                throw new InvalidOperationException("S3B static validation failed. See " + output);
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static void ValidateSources(ICollection<StaticCheck> checks)
        {
            string cabinExternal = HospitalInteriorS3BR03Builder.ExternalCabinFbx();
            string landingExternal = HospitalInteriorS3BR03Builder.ExternalLandingFbx();
            string cabinImported = Path.Combine(HospitalInteriorS3BR03Builder.ProjectRoot(), HospitalInteriorS3BR03Builder.CabinFbxPath);
            string landingImported = Path.Combine(HospitalInteriorS3BR03Builder.ProjectRoot(), HospitalInteriorS3BR03Builder.LandingFbxPath);
            bool hashes = File.Exists(cabinExternal) && File.Exists(landingExternal)
                && File.Exists(cabinImported) && File.Exists(landingImported)
                && HospitalInteriorS3BR03Builder.Sha256(cabinExternal) == HospitalInteriorS3BR03Builder.Sha256(cabinImported)
                && HospitalInteriorS3BR03Builder.Sha256(landingExternal) == HospitalInteriorS3BR03Builder.Sha256(landingImported);
            Add(checks, "approved_s3a_fbx_bytes_preserved", hashes,
                $"cabin={Path.GetFileName(cabinExternal)}; landing={Path.GetFileName(landingExternal)}");

            ModelImporter cabinImporter = AssetImporter.GetAtPath(HospitalInteriorS3BR03Builder.CabinFbxPath) as ModelImporter;
            ModelImporter landingImporter = AssetImporter.GetAtPath(HospitalInteriorS3BR03Builder.LandingFbxPath) as ModelImporter;
            bool importSettings = cabinImporter != null && landingImporter != null
                && Approximately(cabinImporter.globalScale, 1f) && Approximately(landingImporter.globalScale, 1f)
                && cabinImporter.useFileScale && landingImporter.useFileScale
                && cabinImporter.bakeAxisConversion && landingImporter.bakeAxisConversion
                && !cabinImporter.importCameras && !landingImporter.importCameras
                && !cabinImporter.importLights && !landingImporter.importLights
                && !cabinImporter.importAnimation && !landingImporter.importAnimation
                && !cabinImporter.addCollider && !landingImporter.addCollider
                && cabinImporter.isReadable && landingImporter.isReadable;
            Add(checks, "identity_metric_model_import", importSettings,
                "globalScale=1; fileScale=true; baked axis; no imported cameras/lights/animation/automatic colliders");

            GameObject cabin = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalInteriorS3BR03Builder.CabinFbxPath);
            GameObject landing = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalInteriorS3BR03Builder.LandingFbxPath);
            Transform[] cabinDoors = cabin == null ? Array.Empty<Transform>() : cabin.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "S3A_R03_E01_CabinDoor_L" || item.name == "S3A_R03_E01_CabinDoor_R").ToArray();
            Transform[] landingDoors = landing == null ? Array.Empty<Transform>() : landing.GetComponentsInChildren<Transform>(true)
                .Where(item => item.name == "S3A_R03_E01_LandingDoor_L" || item.name == "S3A_R03_E01_LandingDoor_R").ToArray();
            bool independentLeaves = cabinDoors.Length == 2 && landingDoors.Length == 2
                && cabinDoors.All(item => item.GetComponent<MeshFilter>()?.sharedMesh != null)
                && landingDoors.All(item => item.GetComponent<MeshFilter>()?.sharedMesh != null)
                && cabinDoors.Select(item => item.name).Distinct().Count() == 2
                && landingDoors.Select(item => item.name).Distinct().Count() == 2;
            Add(checks, "four_independent_imported_door_meshes", independentLeaves,
                $"cabin={cabinDoors.Length}; landing={landingDoors.Length}");
        }

        private static S3ElevatorContract ValidateContract(ICollection<StaticCheck> checks)
        {
            S3ElevatorContract contract = AssetDatabase.LoadAssetAtPath<S3ElevatorContract>(HospitalInteriorS3BR03Builder.ContractPath);
            Add(checks, "s3_elevator_contract_exists", contract != null, HospitalInteriorS3BR03Builder.ContractPath);
            if (contract == null)
                return null;
            bool approvedReference = contract.ApprovedS2FloorContract
                == AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath);
            Add(checks, "contract_references_approved_s2", approvedReference, HospitalInteriorS2R03Builder.ContractPath);
            bool approvedS1Reference = contract.ApprovedS1DatumContract
                == AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath);
            Add(checks, "contract_references_approved_s1_vertical_authority", approvedS1Reference,
                HospitalInteriorS1R03Builder.ContractPath);
            bool spatial = contract.ValidateSpatialFit(out string detail);
            Add(checks, "contract_building_core_fit", spatial, detail);
            bool shaft = contract.ValidateShaftFit(out string shaftDetail);
            Add(checks, "contract_full_height_shaft_fit", shaft, shaftDetail);
            string s1ReportPath = Path.Combine(HospitalInteriorS3BR03Builder.WorkspaceRoot(), "Reviews",
                "HospitalInterior", "StageI1_R03", "StageI1_R03_S1_DatumReport.json");
            S1ClashAuthority clashAuthority = File.Exists(s1ReportPath)
                ? JsonUtility.FromJson<S1ClashAuthority>(File.ReadAllText(s1ReportPath)) : null;
            bool clashAuthorityValid = clashAuthority != null && clashAuthority.status == "PASS"
                && clashAuthority.elevatorContainedByUpperEnvelope
                && clashAuthority.elevatorExpectedSlabIntersections?.Length == 7
                && clashAuthority.elevatorUnexpectedGeometryIntersections?.Length == 0
                && Mathf.Abs(clashAuthority.f06CeilingBottomM - (contract.ShaftMaxY - 0.10f)) <= 0.002f;
            Add(checks, "approved_s1_clash_authority_expected_crossings_only", clashAuthorityValid,
                clashAuthority == null ? "missing S1 datum report" : $"expectedSlabCrossings={clashAuthority.elevatorExpectedSlabIntersections?.Length}; "
                    + $"unexpectedGeometry={clashAuthority.elevatorUnexpectedGeometryIntersections?.Length}; F06CeilingBottom={clashAuthority.f06CeilingBottomM:F3}; topClosureBottom={contract.ShaftMaxY - 0.10f:F3}");
            bool timings = Approximately(contract.DoorOpenSeconds, 1.2f) && Approximately(contract.DoorDwellSeconds, 4f)
                && Approximately(contract.DoorCloseSeconds, 1.2f) && Approximately(contract.DoorTravelM, 0.5975f)
                && Approximately(contract.ClearDoorM.x, 1.2f) && Approximately(contract.ClearDoorM.y, 2.2f)
                && Approximately(contract.ModelFloorTopM + contract.ModelRootVerticalOffsetM, 0f);
            Add(checks, "normal_door_timing_and_flush_datum", timings,
                $"open={contract.DoorOpenSeconds:F1}; dwell={contract.DoorDwellSeconds:F1}; close={contract.DoorCloseSeconds:F1}; floor={contract.ModelFloorTopM + contract.ModelRootVerticalOffsetM:F3}");
            return contract;
        }

        private static void ValidateScene(S3ElevatorContract contract, ICollection<StaticCheck> checks)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(HospitalInteriorS3BR03Builder.BootstrapScenePath);
            Add(checks, "s3b_review_scene_exists", sceneAsset != null, HospitalInteriorS3BR03Builder.BootstrapScenePath);
            if (sceneAsset == null || contract == null)
                return;
            Scene scene = EditorSceneManager.OpenScene(HospitalInteriorS3BR03Builder.BootstrapScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            bool rootIdentity = roots.Length == 1 && IsIdentity(roots[0].transform);
            Add(checks, "single_identity_bootstrap_root", rootIdentity, "roots=" + string.Join(",", roots.Select(item => item.name)));

            S3BReviewBootstrap[] bootstraps = roots.SelectMany(item => item.GetComponentsInChildren<S3BReviewBootstrap>(true)).ToArray();
            S3DoorController[] controllers = roots.SelectMany(item => item.GetComponentsInChildren<S3DoorController>(true)).ToArray();
            S3ShaftStructure[] shafts = roots.SelectMany(item => item.GetComponentsInChildren<S3ShaftStructure>(true)).ToArray();
            S3ShaftFloorAperture[] apertures = roots.SelectMany(item => item.GetComponentsInChildren<S3ShaftFloorAperture>(true)).ToArray();
            S3CabinObservationDesign[] observations = roots.SelectMany(item => item.GetComponentsInChildren<S3CabinObservationDesign>(true)).ToArray();
            XROrigin[] origins = roots.SelectMany(item => item.GetComponentsInChildren<XROrigin>(true)).ToArray();
            Camera[] cameras = roots.SelectMany(item => item.GetComponentsInChildren<Camera>(true)).ToArray();
            AudioListener[] listeners = roots.SelectMany(item => item.GetComponentsInChildren<AudioListener>(true)).ToArray();
            S3BReviewLocomotion[] locomotion = roots.SelectMany(item => item.GetComponentsInChildren<S3BReviewLocomotion>(true)).ToArray();
            bool bootstrapRefs = bootstraps.Length == 1 && controllers.Length == 1 && shafts.Length == 1
                && apertures.Length == 1 && observations.Length == 1 && origins.Length == 1
                && cameras.Length == 1 && listeners.Length == 1 && locomotion.Length == 1
                && bootstraps[0].ElevatorContract == contract && bootstraps[0].DoorController == controllers[0]
                && bootstraps[0].XrOrigin == origins[0] && bootstraps[0].ReviewCamera == cameras[0]
                && bootstraps[0].Locomotion == locomotion[0] && bootstraps[0].ShaftStructure == shafts[0]
                && bootstraps[0].FloorAperture == apertures[0]
                && bootstraps[0].CabinObservationDesign == observations[0];
            Add(checks, "complete_single_review_bootstrap", bootstrapRefs,
                $"bootstrap={bootstraps.Length}; controller={controllers.Length}; shaft={shafts.Length}; aperture={apertures.Length}; observationCabin={observations.Length}; rig={origins.Length}; camera={cameras.Length}");
            if (!bootstrapRefs)
                return;

            S3BReviewBootstrap bootstrap = bootstraps[0];
            S3DoorController door = controllers[0];
            Transform core = roots[0].transform.Find("S3B_R03_E01_PersistentCore");
            Add(checks, "identity_persistent_e01_core", core != null && IsIdentity(core), core == null ? "missing" : core.name);
            S3ShaftStructure shaftStructure = shafts[0];
            S3ShaftFloorAperture floorAperture = apertures[0];
            bool shaftStructureValid = shaftStructure.ValidateStructure(out string shaftStructureDetail);
            Add(checks, "continuous_shaft_walls_landings_and_rails", shaftStructureValid, shaftStructureDetail);
            bool cabinSweep = shaftStructure.ValidateCabinSweep(out string cabinSweepDetail);
            Add(checks, "f00_to_f06_cabin_sweep_clear", cabinSweep, cabinSweepDetail);
            bool glassDesign = shaftStructure.ValidateGlassDesign(out string glassDesignDetail);
            Add(checks, "full_height_transparent_glass_hoistway", glassDesign, glassDesignDetail);
            S3CabinObservationDesign cabinObservation = observations[0];
            bool observationDesign = cabinObservation.ValidateObservationDesign(out string observationDesignDetail);
            Add(checks, "transparent_protected_cabin_observation_zones", observationDesign, observationDesignDetail);
            bool cabinNested = cabinObservation.ValidateNestedInShaft(shaftStructure, out string cabinNestingDetail);
            Add(checks, "cabin_fully_nested_inside_glass_hoistway", cabinNested, cabinNestingDetail);
            bool shaftAuthority = shaftStructure.LeftWall.bounds.min.x >= contract.E01OperatingCellMinXZ.x - 0.001f
                && shaftStructure.RightWall.bounds.max.x <= contract.E01OperatingCellMaxXZ.x + 0.001f
                && shaftStructure.RearWall.bounds.max.z <= contract.E01OperatingCellMaxXZ.y + 0.001f
                && contract.ShaftInnerMinXZ.y - contract.ShaftFrontWallDepthM >= 2.90f - 0.001f
                && shaftStructure.LeftWall.bounds.min.y >= contract.ShaftMinY - 0.001f
                && shaftStructure.TopClosure.bounds.max.y <= contract.ApprovedS1DatumContract.ElevatorReference.maxY + 0.001f;
            Add(checks, "shaft_contained_by_approved_core_and_portal_pocket", shaftAuthority,
                $"outerX={shaftStructure.LeftWall.bounds.min.x:F3}..{shaftStructure.RightWall.bounds.max.x:F3}; "
                + $"outerZ={contract.ShaftInnerMinXZ.y - contract.ShaftFrontWallDepthM:F3}..{shaftStructure.RearWall.bounds.max.z:F3}; "
                 + $"vertical={contract.ShaftMinY:F3}..{shaftStructure.TopClosure.bounds.max.y:F3}; front 0.100 m shares the approved landing-portal pocket");
            bool apertureAssets = floorAperture.ValidateAssets(out string apertureAssetDetail)
                && floorAperture.F00ShaftCutSlab == AssetDatabase.LoadAssetAtPath<Mesh>(HospitalInteriorS3BR03Builder.F00ShaftCutMeshPath)
                && floorAperture.UpperShaftCutSlab == AssetDatabase.LoadAssetAtPath<Mesh>(HospitalInteriorS3BR03Builder.UpperShaftCutMeshPath)
                && floorAperture.UpperShaftCutUnderside == AssetDatabase.LoadAssetAtPath<Mesh>(HospitalInteriorS3BR03Builder.UpperShaftCutUndersidePath);
            Add(checks, "s3_owned_floor_aperture_meshes", apertureAssets, apertureAssetDetail);

            MeshFilter[] structuralUndersides = roots.SelectMany(item => item.GetComponentsInChildren<MeshFilter>(true))
                .Where(item => item.name.StartsWith("S3B_R03_StructuralUnderside_", StringComparison.Ordinal)).ToArray();
            bool persistentUndersidesCut = structuralUndersides.Length == 6
                && structuralUndersides.All(item => item.sharedMesh == floorAperture.UpperShaftCutUnderside);
            Add(checks, "persistent_upper_undersides_use_shaft_cut_mesh", persistentUndersidesCut,
                $"undersides={structuralUndersides.Length}; mesh={floorAperture.UpperShaftCutUnderside?.name}; aperture=X 0.300..3.000, Z 3.000..5.700");

            S2FloorRecord f00Record = contract.ApprovedS2FloorContract.Floors[0];
            Scene f00Scene = EditorSceneManager.OpenScene(f00Record.scenePath, OpenSceneMode.Additive);
            string appliedDetail = "aperture validation was not reached";
            bool applied = floorAperture.ApplyToFloorScene(f00Scene, f00Record.floorId, out string applyDetail)
                && floorAperture.ValidateAppliedToFloorScene(f00Scene, f00Record.floorId, out appliedDetail);
            Add(checks, "f00_runtime_instance_uses_shaft_aperture", applied,
                applyDetail + (applied ? "; " + appliedDetail : string.Empty));
            bool modelRoots = bootstrap.CabinModelRoot != null && bootstrap.LandingPortalRoots.Length == 7
                && bootstrap.CabinModelRoot.localScale == Vector3.one
                && bootstrap.LandingPortalRoots.All(item => item != null && item.localScale == Vector3.one
                    && item.localRotation == Quaternion.identity);
            Add(checks, "approved_model_roots_identity_scale", modelRoots,
                $"cabinScale={bootstrap.CabinModelRoot?.localScale}; portals={bootstrap.LandingPortalRoots.Length}");

            bool cabinPlacement = bootstrap.CabinModelRoot != null
                && Vector3.Distance(bootstrap.CabinModelRoot.localPosition, new Vector3(0f, -0.11f, 0f)) <= 0.001f
                && bootstrap.CabinModelRoot.localRotation == Quaternion.identity;
            Add(checks, "exact_cabin_integration_offset", cabinPlacement,
                bootstrap.CabinModelRoot == null ? "missing" : bootstrap.CabinModelRoot.localPosition.ToString("F3"));

            bool landingDatums = bootstrap.LandingPortalRoots.Length == 7;
            if (landingDatums)
                for (int i = 0; i < 7; i++)
                    landingDatums &= Mathf.Abs(bootstrap.LandingPortalRoots[i].localPosition.y
                        - (contract.ApprovedS2FloorContract.Floors[i].elevationM - 0.11f)) <= 0.001f;
            Add(checks, "seven_portals_on_approved_datums", landingDatums,
                string.Join(",", bootstrap.LandingPortalRoots.Select(item => item.localPosition.y.ToString("F3"))));

            Transform[] leaves = door.MovingLeaves;
            bool fourMovingLeaves = leaves.Length == 4 && leaves.All(item => item != null && item.GetComponent<BoxCollider>() != null)
                && leaves.Select(item => item.name).Distinct().Count() == 4;
            Add(checks, "four_current_moving_leaf_colliders", fourMovingLeaves,
                string.Join(",", leaves.Where(item => item != null).Select(item => item.name)));
            bool leafDimensions = fourMovingLeaves && leaves.All(item =>
            {
                BoxCollider collider = item.GetComponent<BoxCollider>();
                Vector3 worldSize = Vector3.Scale(collider.size, item.lossyScale);
                return Mathf.Abs(worldSize.x - contract.DoorLeafWidthM) <= 0.01f
                    && Mathf.Abs(worldSize.y - contract.ClearDoorM.y) <= 0.01f
                    && Mathf.Abs(worldSize.z - contract.DoorLeafDepthM) <= 0.01f;
            });
            Add(checks, "door_leaf_collider_metric_sizes", leafDimensions,
                "expected=0.595 x 2.200 x 0.075 m");

            bool nonCurrent = bootstrap.NonCurrentLandingDoors.Length == 12
                && bootstrap.NonCurrentLandingDoors.All(item => item != null && item.GetComponent<BoxCollider>() != null
                    && Mathf.Abs(item.localPosition.x - (item.name.EndsWith("_L", StringComparison.Ordinal) ? 1.3475f : 1.9525f)) <= 0.002f);
            Add(checks, "noncurrent_landing_doors_closed_collidable", nonCurrent,
                "leaves=" + bootstrap.NonCurrentLandingDoors.Length);

            Transform floor = HospitalInteriorS3BR03Builder.RequireDescendant(bootstrap.CabinModelRoot, "S3A_R03_E01_CabinFloor");
            Transform threshold = HospitalInteriorS3BR03Builder.RequireDescendant(bootstrap.LandingPortalRoots[0], "S3A_R03_E01_LandingThreshold");
            BoxCollider floorCollider = floor.GetComponent<BoxCollider>();
            BoxCollider thresholdCollider = threshold.GetComponent<BoxCollider>();
            bool exactDatum = floorCollider != null && thresholdCollider != null
                && Mathf.Abs(floorCollider.bounds.max.y) <= 0.001f && Mathf.Abs(thresholdCollider.bounds.max.y) <= 0.001f
                && leaves.All(item => Mathf.Abs(item.GetComponent<BoxCollider>().bounds.min.y) <= 0.001f);
            Add(checks, "floor_threshold_and_door_bottom_exact_datum", exactDatum,
                $"floorTop={floorCollider?.bounds.max.y:F3}; thresholdTop={thresholdCollider?.bounds.max.y:F3}");

            Renderer[] cabinRenderers = bootstrap.CabinModelRoot.GetComponentsInChildren<Renderer>(true);
            Bounds cabinBounds = CombinedBounds(cabinRenderers);
            float minHorizontalClearance = Mathf.Min(cabinBounds.min.x - contract.E01OperatingCellMinXZ.x,
                contract.E01OperatingCellMaxXZ.x - cabinBounds.max.x,
                cabinBounds.min.z - contract.E01OperatingCellMinXZ.y,
                contract.E01OperatingCellMaxXZ.y - cabinBounds.max.z);
            bool actualCabinFit = cabinBounds.min.x >= contract.E01OperatingCellMinXZ.x
                && cabinBounds.max.x <= contract.E01OperatingCellMaxXZ.x
                && cabinBounds.min.z >= contract.E01OperatingCellMinXZ.y
                && cabinBounds.max.z <= contract.E01OperatingCellMaxXZ.y
                && minHorizontalClearance >= 0.05f && cabinBounds.max.y <= 2.51f;
            Add(checks, "actual_cabin_mesh_clear_of_building_core", actualCabinFit,
                $"boundsMin={cabinBounds.min:F3}; boundsMax={cabinBounds.max:F3}; minimumClearance={minHorizontalClearance:F3}; outliers="
                + string.Join(",", cabinRenderers.Where(item => item.bounds.min.z < 2.95f || item.bounds.max.y > 2.65f)
                    .Select(item => $"{item.name}[{item.bounds.min:F2}..{item.bounds.max:F2}]")));

            bool portalEnvelope = true;
            float minimumOverhead = float.MaxValue;
            for (int i = 0; i < bootstrap.LandingPortalRoots.Length; i++)
            {
                Bounds bounds = CombinedBounds(bootstrap.LandingPortalRoots[i].GetComponentsInChildren<Renderer>(true));
                float datum = contract.ApprovedS2FloorContract.Floors[i].elevationM;
                portalEnvelope &= bounds.min.x >= -0.001f && bounds.max.x <= 3.001f
                    && bounds.min.z >= 2.70f && bounds.max.z <= 3.30f
                    && bounds.min.y >= datum - 0.111f && bounds.max.y <= datum + 2.60f;
                if (i < bootstrap.LandingPortalRoots.Length - 1)
                    minimumOverhead = Mathf.Min(minimumOverhead,
                        contract.ApprovedS2FloorContract.Floors[i + 1].elevationM - bounds.max.y);
            }
            Add(checks, "landing_portals_clear_building_and_storeys", portalEnvelope && minimumOverhead >= 1.29f,
                $"minimumOverheadToNextDatum={minimumOverhead:F3}; F00Outliers="
                + string.Join(",", bootstrap.LandingPortalRoots[0].GetComponentsInChildren<Renderer>(true)
                    .Where(item => item.bounds.max.y > 2.65f).Select(item => $"{item.name}[{item.bounds.max.y:F2}]")));

            S3DoorObstructionSensor[] sensors = roots.SelectMany(item => item.GetComponentsInChildren<S3DoorObstructionSensor>(true)).ToArray();
            bool sensor = sensors.Length == 1 && sensors[0] == door.ObstructionSensor
                && sensors[0].GetComponent<BoxCollider>() is BoxCollider trigger && trigger.isTrigger
                && Vector3.Distance(trigger.size, new Vector3(1.16f, 2.18f, 0.35f)) <= 0.001f
                && sensors[0].GetComponent<Rigidbody>() is Rigidbody body && body.isKinematic && !body.useGravity;
            Add(checks, "threshold_obstruction_sensor", sensor, sensors.Length == 0 ? "missing" : "1.160 x 2.180 x 0.350 m trigger");

            S3ElevatorButton[] buttons = roots.SelectMany(item => item.GetComponentsInChildren<S3ElevatorButton>(true)).ToArray();
            bool buttonContract = buttons.Count(item => item.Command == S3ElevatorButtonCommand.Floor) == 7
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorOpen) == 1
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorClose) == 1
                && buttons.Count(item => item.Command == S3ElevatorButtonCommand.LandingCall) == 7
                && buttons.All(item => item.DoorController == door && item.GetComponent<BoxCollider>()?.isTrigger == true);
            Add(checks, "all_physical_door_and_call_controls", buttonContract,
                $"floor={buttons.Count(item => item.Command == S3ElevatorButtonCommand.Floor)}; open={buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorOpen)}; close={buttons.Count(item => item.Command == S3ElevatorButtonCommand.DoorClose)}; calls={buttons.Count(item => item.Command == S3ElevatorButtonCommand.LandingCall)}");

            Renderer[] modelRenderers = bootstrap.CabinModelRoot.GetComponentsInChildren<Renderer>(true)
                .Concat(bootstrap.LandingPortalRoots.SelectMany(item => item.GetComponentsInChildren<Renderer>(true)))
                .Concat(shaftStructure.GetComponentsInChildren<Renderer>(true)).ToArray();
            Renderer[] shadedRenderers = modelRenderers.Where(item => item.enabled && item.GetComponent<TextMesh>() == null).ToArray();
            bool urpMaterials = shadedRenderers.All(item => item.sharedMaterials.All(material => material != null
                && material.shader != null && material.shader.name == "Universal Render Pipeline/Lit"));
            string[] nonUrp = shadedRenderers.Where(item => item.sharedMaterials.Any(material => material == null
                    || material.shader == null || material.shader.name != "Universal Render Pipeline/Lit"))
                .Select(item => item.name + ":" + string.Join("/", item.sharedMaterials
                    .Select(material => material?.shader?.name ?? "null"))).ToArray();
            Add(checks, "approved_design_uses_urp_materials", urpMaterials,
                "shadedRenderers=" + shadedRenderers.Length + "; dynamicTextRenderers=7; nonUrp=" + string.Join(",", nonUrp));

            bool noTravel = roots.SelectMany(item => item.GetComponentsInChildren<MonoBehaviour>(true))
                .All(item => item == null || item.GetType().Name != "S3ElevatorController")
                && door.State == S3ElevatorState.ReadyClosed && door.MovementInterlockSafe;
            Add(checks, "s3c_travel_absent_and_closed_interlock_safe", noTravel,
                $"state={door.State}; movementInterlockSafe={door.MovementInterlockSafe}");
        }

        private static Bounds CombinedBounds(IReadOnlyList<Renderer> renderers)
        {
            if (renderers.Count == 0)
                return new Bounds();
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Count; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static bool IsIdentity(Transform transform) => transform.localPosition.sqrMagnitude <= 0.000001f
            && Quaternion.Angle(transform.localRotation, Quaternion.identity) <= 0.001f
            && (transform.localScale - Vector3.one).sqrMagnitude <= 0.000001f;

        private static bool Approximately(float actual, float expected) => Mathf.Abs(actual - expected) <= 0.001f;

        private static void Add(ICollection<StaticCheck> checks, string name, bool pass, string detail)
            => checks.Add(new StaticCheck { name = name, pass = pass, detail = detail });
    }
}
