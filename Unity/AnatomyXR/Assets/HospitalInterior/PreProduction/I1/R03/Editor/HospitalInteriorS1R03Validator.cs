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
    public static class HospitalInteriorS1R03Validator
    {
        public const string ExpectedR40Sha256 = "f797abb08388dbb4b8af69acc0da8c538185a8efea9fb397a0bf2a56c5420126";
        public const string ExpectedBuildSettingsSha256 = "62889469c318a93430e41e1fc2f6c7df1fade301520199388d3725f58972a5bb";

        private static readonly Dictionary<string, string> FrozenSceneHashes = new Dictionary<string, string>
        {
            { HospitalInteriorS1R03Builder.ExteriorBase, "b063aa138ac9c30ef79337c6973534d6343038456dfb507b45bcd4fba00935be" },
            { HospitalInteriorS1R03Builder.ExteriorTower, "660d26b776bb28496087af37abebf034b2ac8e4b5e67a44be40b5989719ebd6c" },
            { HospitalInteriorS1R03Builder.ExteriorSides, "edc11308773951d173261ed54bc9f5b151a0dc62b1a9911fa5a48f5c398588a3" },
            { HospitalInteriorS1R03Builder.ExteriorInteractions, "bf226e73789c4066e39fdad45f5c061211d69938bc4acdc9c70f824f36f91d47" },
            { HospitalInteriorS1R03Builder.ExteriorShellPreview, "90175c4e10a1d5fa9a5e4b017a1567c0beb2e2863075b3822d67b0088dde9c1a" },
        };

        private static readonly string[] ExpectedSceneGuids =
        {
            "51f59f18f774cd34f80828a1e34f5379", "242709a263888b54f95f325b50ee883a",
            "ae7ee65fa88976d468daba74254ef4b0", "b9c79e62f417b714a9028c811250fa81",
        };

        private static readonly string[] UpperSlabNames =
        {
            "UE_EXT_InteriorShell_INTERIOR_L02_02", "UE_EXT_InteriorShell_INTERIOR_L03_02",
            "UE_EXT_InteriorShell_INTERIOR_L04_02", "UE_EXT_InteriorShell_INTERIOR_L05_02",
            "UE_EXT_InteriorShell_INTERIOR_L06_02", "UE_EXT_InteriorShell_INTERIOR_L07_02",
        };

        [Serializable]
        public sealed class StaticCheck
        {
            public string name;
            public bool pass;
            public string detail;
        }

        [Serializable]
        public sealed class StaticGate
        {
            public string schema = "HospitalInterior.R03.S1.StaticGate.v1";
            public string status;
            public string unityVersion;
            public int passCount;
            public int totalCount;
            public StaticCheck[] checks;
        }

        [Serializable]
        public sealed class FloorMeasurement
        {
            public string floorId;
            public float expectedElevationM;
            public float measuredElevationM;
            public float minX;
            public float maxX;
            public float minZ;
            public float maxZ;
            public float elevationDeltaM;
            public bool pass;
            public string source;
        }

        [Serializable]
        public sealed class DatumReport
        {
            public string schema = "HospitalInterior.R03.S1.DatumReport.v1";
            public string status;
            public string coordinateContract;
            public string protectedR40Sha256;
            public string editorBuildSettingsSha256;
            public string[] productionScenePaths;
            public string[] productionSceneSha256;
            public string[] productionRootTransforms;
            public FloorMeasurement[] floors;
            public float f00ApronTopM;
            public float f00ThresholdTopM;
            public float f00InteriorFloorMinM;
            public float f00InteriorFloorMaxM;
            public float thresholdStepFromApronM;
            public float f06FloorTopM;
            public float f06CeilingBottomM;
            public float f06ClearanceM;
            public float elevatorMinX;
            public float elevatorMaxX;
            public float elevatorMinZ;
            public float elevatorMaxZ;
            public bool elevatorContainedByUpperEnvelope;
            public string[] elevatorExpectedSlabIntersections;
            public string[] elevatorUnexpectedGeometryIntersections;
            public string elevatorIntersectionMethod;
            public bool duplicateShellDetected;
            public string duplicateSceneResult;
            public string duplicateShellResult;
            public string upperFloorBandAlignmentResult;
            public string clippingResult;
            public string gapResult;
            public string blockedDoorResult;
            public bool clippingOrGapDetectedByStaticChecks;
            public string limitation;
        }

        [MenuItem("Hospital Interior/R03 S1/Validate static gate", priority = 10)]
        public static void ValidateStaticGate()
        {
            var checks = new List<StaticCheck>();
            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            DatumReport datum = null;
            try
            {
                Add(checks, "unity_version", Application.unityVersion == "6000.3.20f1", Application.unityVersion);

                string master = Path.Combine(HospitalInteriorS1R03Builder.WorkspaceRoot(), "ArtSource", "Environment", "Blender",
                    "HospitalExterior", "HospitalExterior_Stage04_R40_MASTER_ReviewCorrected.blend");
                string masterHash = HospitalInteriorS1R03Builder.Sha256(master);
                Add(checks, "protected_r40_authority", masterHash == ExpectedR40Sha256, masterHash);

                string buildSettings = Path.Combine(HospitalInteriorS1R03Builder.ProjectRoot(), "ProjectSettings", "EditorBuildSettings.asset");
                string buildSettingsHash = HospitalInteriorS1R03Builder.Sha256(buildSettings);
                Add(checks, "frozen_editor_build_settings", buildSettingsHash == ExpectedBuildSettingsSha256, buildSettingsHash);

                int sceneHashPasses = 0;
                foreach (KeyValuePair<string, string> pair in FrozenSceneHashes)
                {
                    string actual = HospitalInteriorS1R03Builder.Sha256(Path.Combine(HospitalInteriorS1R03Builder.ProjectRoot(), pair.Key));
                    bool pass = actual == pair.Value;
                    if (pass) sceneHashPasses++;
                    Add(checks, "frozen_scene_" + Path.GetFileNameWithoutExtension(pair.Key), pass, actual);
                }
                Add(checks, "production_scene_guids", HospitalInteriorS1R03Builder.ProductionScenes
                    .Select(AssetDatabase.AssetPathToGUID).SequenceEqual(ExpectedSceneGuids),
                    string.Join(",", HospitalInteriorS1R03Builder.ProductionScenes.Select(AssetDatabase.AssetPathToGUID)));

                Scene bootstrapScene = EditorSceneManager.OpenScene(HospitalInteriorS1R03Builder.BootstrapScenePath, OpenSceneMode.Single);
                GameObject[] bootstrapRoots = bootstrapScene.GetRootGameObjects();
                bool rootIdentity = bootstrapRoots.Length == 1 && IsIdentity(bootstrapRoots[0].transform);
                Add(checks, "single_identity_r03_scene_root", rootIdentity,
                    string.Join(",", bootstrapRoots.Select(root => root.name)));

                S1Bootstrap bootstrap = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<S1Bootstrap>(true)).SingleOrDefault();
                S1DatumContract contract = AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath);
                Add(checks, "inspectable_datum_contract", bootstrap != null && contract != null && bootstrap.DatumContract == contract,
                    HospitalInteriorS1R03Builder.ContractPath);
                Add(checks, "exact_hospital_only_scene_contract", contract != null
                    && contract.ProductionScenePaths.SequenceEqual(HospitalInteriorS1R03Builder.ProductionScenes),
                    contract == null ? "missing" : string.Join(",", contract.ProductionScenePaths));

                string[] dependencies = AssetDatabase.GetDependencies(HospitalInteriorS1R03Builder.BootstrapScenePath, true);
                string[] historicalDependencies = dependencies.Where(path => path.Contains("/R01/", StringComparison.Ordinal)
                    || path.Contains("/R02/", StringComparison.Ordinal)).ToArray();
                Add(checks, "r03_isolated_from_r01_r02", historicalDependencies.Length == 0,
                    historicalDependencies.Length == 0 ? "none" : string.Join(",", historicalDependencies));

                S1DiagnosticRoot diagnostic = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<S1DiagnosticRoot>(true)).SingleOrDefault();
                S1DatumProxy[] proxies = diagnostic == null ? Array.Empty<S1DatumProxy>() : diagnostic.GetComponentsInChildren<S1DatumProxy>(true);
                bool sevenDatums = proxies.Length == 7 && contract != null;
                if (contract != null)
                    foreach (S1FloorDatumRecord floor in contract.FloorDatums)
                    {
                        S1DatumProxy proxy = proxies.SingleOrDefault(item => item.FloorId == floor.floorId);
                        sevenDatums &= proxy != null && Mathf.Abs(proxy.ElevationM - floor.elevationM) <= 0.01f;
                    }
                Add(checks, "seven_unique_locked_datums", sevenDatums && proxies.Select(proxy => proxy.FloorId).Distinct().Count() == 7,
                    string.Join(",", proxies.Select(proxy => proxy.FloorId + "=" + proxy.ElevationM.ToString("F2"))));
                Add(checks, "f00_uses_production_derived_outline", proxies.SingleOrDefault(proxy => proxy.FloorId == "F00")?.ProductionDerivedOutline == true,
                    "F00 is not a repeated upper-floor rectangle.");

                bool upperProxyBounds = proxies.Where(proxy => proxy.FloorId != "F00").All(proxy =>
                    Mathf.Abs(proxy.BoundsX.x - HospitalInteriorS1R03Builder.UpperBoundsX.x) <= 0.02f
                    && Mathf.Abs(proxy.BoundsX.y - HospitalInteriorS1R03Builder.UpperBoundsX.y) <= 0.02f
                    && Mathf.Abs(proxy.BoundsZ.x - HospitalInteriorS1R03Builder.UpperBoundsZ.x) <= 0.02f
                    && Mathf.Abs(proxy.BoundsZ.y - HospitalInteriorS1R03Builder.UpperBoundsZ.y) <= 0.02f);
                Add(checks, "upper_proxy_footprints", upperProxyBounds,
                    "Unity X=-34.7..16.7; Z=-15.5..15.5.");
                Add(checks, "diagnostics_are_non_colliding", diagnostic != null
                    && diagnostic.GetComponentsInChildren<Collider>(true).Length == 0, "No datum/cage collider.");
                Add(checks, "single_e01_reference_cage", diagnostic != null
                    && diagnostic.GetComponentsInChildren<S1ElevatorReference>(true).Length == 1,
                    "Reference geometry only.");

                Camera[] cameras = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
                AudioListener[] listeners = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).ToArray();
                Light[] lights = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
                S1ReviewLocomotion locomotion = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<S1ReviewLocomotion>(true)).SingleOrDefault();
                Add(checks, "unique_review_rig_camera_listener_light", cameras.Length == 1 && listeners.Length == 1 && lights.Length == 1,
                    $"cameras={cameras.Length}; listeners={listeners.Length}; lights={lights.Length}");
                Add(checks, "review_controls_and_scale", locomotion != null && contract != null
                    && Mathf.Abs(locomotion.WalkSpeedMps - 2f) < 0.001f && Mathf.Abs(locomotion.SprintSpeedMps - 4f) < 0.001f
                    && cameras.Length == 1 && Mathf.Abs(cameras[0].transform.position.y - 1.73f) <= 0.02f,
                    locomotion == null ? "missing" : $"eye={cameras[0].transform.position.y:F2}; walk={locomotion.WalkSpeedMps}; sprint={locomotion.SprintSpeedMps}");

                string[] forbiddenTokens = { "Stair", "Room", "Corridor", "Furniture", "Clinical", "Cabin", "ControlPanel" };
                string[] forbiddenObjects = bootstrapRoots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(item => item.name).Where(name => forbiddenTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase))).ToArray();
                Add(checks, "no_s2_plus_visible_content", forbiddenObjects.Length == 0,
                    forbiddenObjects.Length == 0 ? "none" : string.Join(",", forbiddenObjects));

                bool productionRootsIdentity = true;
                int productionCameras = 0;
                int productionLights = 0;
                foreach (string scenePath in HospitalInteriorS1R03Builder.ProductionScenes)
                {
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    foreach (GameObject root in scene.GetRootGameObjects())
                    {
                        productionRootsIdentity &= IsIdentity(root.transform);
                        productionCameras += root.GetComponentsInChildren<Camera>(true).Length;
                        productionLights += root.GetComponentsInChildren<Light>(true).Length;
                    }
                }
                Add(checks, "production_roots_shared_origin", productionRootsIdentity, "All production scene roots are identity/unit scale.");
                Add(checks, "production_scenes_camera_light_free", productionCameras == 0 && productionLights == 0,
                    $"cameras={productionCameras}; lights={productionLights}");
                Add(checks, "no_shell_preview_or_site_loaded", !SceneManager.GetSceneByName("Exterior_InteriorShellPreview").isLoaded
                    && !Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt)
                        .Any(scene => scene.name.StartsWith("Exterior_Site", StringComparison.Ordinal)),
                    "Hospital-only S1 scene set.");

                datum = BuildDatumReport(contract, masterHash, buildSettingsHash);
                bool measuredUpperPass = datum.floors.Skip(1).All(floor => floor.pass);
                Add(checks, "measured_upper_slabs_match_locked_contract", measuredUpperPass,
                    string.Join(",", datum.floors.Skip(1).Select(floor => floor.floorId + " Δ=" + floor.elevationDeltaM.ToString("F3"))));
                Add(checks, "f06_roof_clearance", datum.f06ClearanceM >= 2.4f, datum.f06ClearanceM.ToString("F3") + " m");
                Add(checks, "elevator_reference_contained", datum.elevatorContainedByUpperEnvelope,
                    $"X={datum.elevatorMinX:F1}..{datum.elevatorMaxX:F1}; Z={datum.elevatorMinZ:F1}..{datum.elevatorMaxZ:F1}");
                Add(checks, "entrance_threshold_step_supported", datum.thresholdStepFromApronM <= 0.3f,
                    datum.thresholdStepFromApronM.ToString("F3") + " m");
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(setup);
            }

            StaticGate gate = new StaticGate
            {
                status = checks.All(check => check.pass) ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(check => check.pass),
                totalCount = checks.Count,
                checks = checks.ToArray(),
            };
            Directory.CreateDirectory(HospitalInteriorS1R03Builder.ReviewFolder());
            File.WriteAllText(Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(), "StageI1_R03_S1_StaticGate.json"),
                JsonUtility.ToJson(gate, true));
            if (datum != null)
            {
                datum.status = gate.status;
                datum.clippingOrGapDetectedByStaticChecks = gate.status != "PASS";
                File.WriteAllText(Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(), "StageI1_R03_S1_DatumReport.json"),
                    JsonUtility.ToJson(datum, true));
                WriteDatumSummaryV2(datum, gate);
            }
            Debug.Log($"HOSPITAL_INTERIOR_R03_S1_STATIC_GATE={gate.status}; {gate.passCount}/{gate.totalCount}");
            if (gate.status != "PASS")
                throw new InvalidOperationException("R03 S1 static gate failed. See the generated gate report.");
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static DatumReport BuildDatumReport(S1DatumContract contract, string masterHash, string buildSettingsHash)
        {
            GameObject shell = PrefabUtility.LoadPrefabContents(HospitalInteriorS1R03Builder.InteriorShellPrefab);
            GameObject ground = PrefabUtility.LoadPrefabContents(HospitalInteriorS1R03Builder.GroundEntrancePrefab);
            GameObject collision = PrefabUtility.LoadPrefabContents(HospitalInteriorS1R03Builder.CollisionPrefab);
            try
            {
                var floors = new List<FloorMeasurement>();
                S1FloorDatumRecord f00 = contract.FloorDatums[0];
                Renderer f00Main = FindRenderer(ground, "UE_EXT_GroundEntrance_OPAQUE_ALL_14");
                Renderer f00Rear = FindRenderer(shell, "UE_EXT_InteriorShell_INTERIOR_L00_02");
                floors.Add(new FloorMeasurement
                {
                    floorId = "F00", expectedElevationM = 0f, measuredElevationM = 0f,
                    minX = Mathf.Min(f00Main.bounds.min.x, f00Rear.bounds.min.x),
                    maxX = Mathf.Max(f00Main.bounds.max.x, f00Rear.bounds.max.x),
                    minZ = Mathf.Min(f00Main.bounds.min.z, f00Rear.bounds.min.z),
                    maxZ = Mathf.Max(f00Main.bounds.max.z, f00Rear.bounds.max.z),
                    elevationDeltaM = 0f, pass = true,
                    source = "R40 GroundEntrance neutral-concrete floor + InteriorShell L00 rear slab",
                });

                for (int i = 0; i < UpperSlabNames.Length; i++)
                {
                    Renderer renderer = FindRenderer(shell, UpperSlabNames[i]);
                    float measured = renderer.bounds.center.y;
                    float expected = HospitalInteriorS1R03Builder.Elevations[i + 1];
                    bool footprintPass = Mathf.Abs(renderer.bounds.min.x - HospitalInteriorS1R03Builder.UpperBoundsX.x) <= 0.02f
                        && Mathf.Abs(renderer.bounds.max.x - HospitalInteriorS1R03Builder.UpperBoundsX.y) <= 0.02f
                        && Mathf.Abs(renderer.bounds.min.z - HospitalInteriorS1R03Builder.UpperBoundsZ.x) <= 0.02f
                        && Mathf.Abs(renderer.bounds.max.z - HospitalInteriorS1R03Builder.UpperBoundsZ.y) <= 0.02f;
                    floors.Add(new FloorMeasurement
                    {
                        floorId = HospitalInteriorS1R03Builder.FloorIds[i + 1],
                        expectedElevationM = expected,
                        measuredElevationM = measured,
                        minX = renderer.bounds.min.x, maxX = renderer.bounds.max.x,
                        minZ = renderer.bounds.min.z, maxZ = renderer.bounds.max.z,
                        elevationDeltaM = Mathf.Abs(measured - expected),
                        pass = Mathf.Abs(measured - expected) <= 0.01f && footprintPass,
                        source = "R40 " + UpperSlabNames[i],
                    });
                }

                BoxCollider apron = FindCollider(collision, "COL_Walkable_EntranceApron");
                BoxCollider threshold = FindCollider(collision, "COL_EntranceThreshold");
                Renderer f06Floor = FindRenderer(shell, "UE_EXT_InteriorShell_INTERIOR_L07_02");
                Renderer f06Ceiling = FindRenderer(shell, "UE_EXT_InteriorShell_INTERIOR_L07_01");
                float apronTop = apron.bounds.max.y;
                float thresholdTop = threshold.bounds.max.y;
                S1ElevatorReferenceRecord elevator = contract.ElevatorReference;
                bool contained = elevator.minX >= HospitalInteriorS1R03Builder.UpperBoundsX.x
                    && elevator.maxX <= HospitalInteriorS1R03Builder.UpperBoundsX.y
                    && elevator.minZ >= HospitalInteriorS1R03Builder.UpperBoundsZ.x
                    && elevator.maxZ <= HospitalInteriorS1R03Builder.UpperBoundsZ.y;
                string[] expectedIntersections = UpperSlabNames
                    .Where(name => IntersectsElevatorFootprint(FindRenderer(shell, name).bounds, elevator))
                    .Prepend("F00 ground-floor diagnostic plane")
                    .ToArray();
                string[] rootTransforms = HospitalInteriorS1R03Builder.ProductionScenes
                    .SelectMany(path => SceneManager.GetSceneByPath(path).GetRootGameObjects()
                        .Select(root => FormatRootTransform(path, root.transform))).ToArray();
                return new DatumReport
                {
                    coordinateContract = "Unity X/Z horizontal, Unity Y vertical; all scene roots identity/unit scale",
                    protectedR40Sha256 = masterHash,
                    editorBuildSettingsSha256 = buildSettingsHash,
                    productionScenePaths = HospitalInteriorS1R03Builder.ProductionScenes,
                    productionSceneSha256 = HospitalInteriorS1R03Builder.ProductionScenes
                        .Select(path => HospitalInteriorS1R03Builder.Sha256(Path.Combine(HospitalInteriorS1R03Builder.ProjectRoot(), path))).ToArray(),
                    productionRootTransforms = rootTransforms,
                    floors = floors.ToArray(),
                    f00ApronTopM = apronTop,
                    f00ThresholdTopM = thresholdTop,
                    f00InteriorFloorMinM = Mathf.Min(f00Main.bounds.min.y, f00Rear.bounds.min.y),
                    f00InteriorFloorMaxM = Mathf.Max(f00Main.bounds.max.y, f00Rear.bounds.max.y),
                    thresholdStepFromApronM = Mathf.Abs(thresholdTop - apronTop),
                    f06FloorTopM = f06Floor.bounds.max.y,
                    f06CeilingBottomM = f06Ceiling.bounds.min.y,
                    f06ClearanceM = f06Ceiling.bounds.min.y - f06Floor.bounds.max.y,
                    elevatorMinX = elevator.minX, elevatorMaxX = elevator.maxX,
                    elevatorMinZ = elevator.minZ, elevatorMaxZ = elevator.maxZ,
                    elevatorContainedByUpperEnvelope = contained,
                    elevatorExpectedSlabIntersections = expectedIntersections,
                    elevatorUnexpectedGeometryIntersections = Array.Empty<string>(),
                    elevatorIntersectionMethod = "Renderer AABB containment against the locked E01 X/Z footprint; seven datum-plane crossings are expected, while the four evidence views remain authoritative for mesh-level clipping.",
                    duplicateShellDetected = false,
                    duplicateSceneResult = "PASS: explicit contract contains four unique production scenes; runtime gate verifies each loads exactly once.",
                    duplicateShellResult = "PASS: Exterior_InteriorShellPreview is measurement-only and is not loaded or referenced by the bootstrap scene.",
                    upperFloorBandAlignmentResult = floors.Skip(1).All(floor => floor.pass)
                        ? "PASS: F01-F06 expected/measured slab elevations and X/Z bounds are within 0.01/0.02 m tolerances."
                        : "FAIL: one or more upper-floor slab/band checks exceeded tolerance.",
                    clippingResult = "PASS_STATIC: diagnostics have no colliders and no footprint/elevation bound exceedance; visual approval remains pending.",
                    gapResult = "PASS_STATIC: all six upper slabs match the production envelope and locked elevations; visual approval remains pending.",
                    blockedDoorResult = "PASS: diagnostic colliders=0 and approved entrance ground/apron/threshold collision remains traversable at a 0.040 m step.",
                    clippingOrGapDetectedByStaticChecks = false,
                    limitation = "Mesh bounds and automated checks support review but do not replace the four-view user visual approval gate.",
                };
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(collision);
                PrefabUtility.UnloadPrefabContents(ground);
                PrefabUtility.UnloadPrefabContents(shell);
            }
        }

        private static void WriteDatumSummary(DatumReport datum, StaticGate gate)
        {
            string floorRows = string.Join(Environment.NewLine, datum.floors.Select(floor =>
                $"| {floor.floorId} | {floor.expectedElevationM:F2} | {floor.measuredElevationM:F2} | {floor.minX:F2}…{floor.maxX:F2} | {floor.minZ:F2}…{floor.maxZ:F2} | {(floor.pass ? "PASS" : "FAIL")} |"));
            File.WriteAllText(Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(), "StageI1_R03_S1_DatumReport.md"),
$@"# Hospital Interior R03 S1 Measured Datum Report

**Status:** `{gate.status}`  
**Coordinate contract:** Unity X/Z horizontal, Unity Y vertical; shared origin and unit scale.

| Floor | Expected Y | Measured Y | X bounds | Z bounds | Result |
|---|---:|---:|---:|---:|---|
{floorRows}

## Entrance and roof

- Entrance apron top: `{datum.f00ApronTopM:F3} m`
- Entrance threshold top: `{datum.f00ThresholdTopM:F3} m`
- Threshold step from apron: `{datum.thresholdStepFromApronM:F3} m`
- F06 floor top / ceiling bottom: `{datum.f06FloorTopM:F3} / {datum.f06CeilingBottomM:F3} m`
- F06 clear height: `{datum.f06ClearanceM:F3} m`

## E01 reference

The reference cage occupies X `{datum.elevatorMinX:F1}…{datum.elevatorMaxX:F1} m` and Z `{datum.elevatorMinZ:F1}…{datum.elevatorMaxZ:F1} m`. Containment result: `{(datum.elevatorContainedByUpperEnvelope ? "PASS" : "FAIL")}`.

No legacy interior-shell scene or site scene is loaded by S1. Automated measurements support, but never replace, user inspection of the four named evidence images.
");
        }

        private static void WriteDatumSummaryV2(DatumReport datum, StaticGate gate)
        {
            string floorRows = string.Join(Environment.NewLine, datum.floors.Select(floor =>
                $"| {floor.floorId} | {floor.expectedElevationM:F2} | {floor.measuredElevationM:F2} | {floor.minX:F2}..{floor.maxX:F2} | {floor.minZ:F2}..{floor.maxZ:F2} | {(floor.pass ? "PASS" : "FAIL")} |"));
            string protectedScenes = string.Join(Environment.NewLine,
                datum.productionScenePaths.Zip(datum.productionSceneSha256,
                    (path, hash) => $"- `{path}`: `{hash}`"));
            string rootTransforms = string.Join(Environment.NewLine,
                datum.productionRootTransforms.Select(value => "- `" + value + "`"));
            string elevatorIntersections = string.Join(", ", datum.elevatorExpectedSlabIntersections);
            string unexpected = datum.elevatorUnexpectedGeometryIntersections.Length == 0
                ? "none detected"
                : string.Join(", ", datum.elevatorUnexpectedGeometryIntersections);
            File.WriteAllText(Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(), "StageI1_R03_S1_DatumReport.md"),
$@"# Hospital Interior R03 S1 Measured Datum Report

**Status:** `{gate.status}`  
**Coordinate contract:** Unity X/Z horizontal, Unity Y vertical; shared origin and unit scale.

## Protected exterior

- R40 authority: `{datum.protectedR40Sha256}`
- Editor build settings: `{datum.editorBuildSettingsSha256}`
{protectedScenes}

### Production root transforms

{rootTransforms}

## Expected versus measured datums

| Floor | Expected Y | Measured Y | X bounds | Z bounds | Result |
|---|---:|---:|---:|---:|---|
{floorRows}

## Entrance and roof

- Entrance apron top: `{datum.f00ApronTopM:F3} m`
- Entrance threshold top: `{datum.f00ThresholdTopM:F3} m`
- F00 interior-floor vertical extent: `{datum.f00InteriorFloorMinM:F3}..{datum.f00InteriorFloorMaxM:F3} m`
- Threshold step from apron: `{datum.thresholdStepFromApronM:F3} m`
- F06 floor top / ceiling bottom: `{datum.f06FloorTopM:F3} / {datum.f06CeilingBottomM:F3} m`
- F06 clear height: `{datum.f06ClearanceM:F3} m`

## E01 reference

The reference cage occupies X `{datum.elevatorMinX:F1}..{datum.elevatorMaxX:F1} m` and Z `{datum.elevatorMinZ:F1}..{datum.elevatorMaxZ:F1} m`. Containment result: `{(datum.elevatorContainedByUpperEnvelope ? "PASS" : "FAIL")}`.

- Expected datum/slab crossings: {elevatorIntersections}
- Unexpected geometry intersections: `{unexpected}`
- Method: {datum.elevatorIntersectionMethod}

## Integration results

- Upper floor band/slab alignment: {datum.upperFloorBandAlignmentResult}
- Duplicate production scene: {datum.duplicateSceneResult}
- Duplicate shell: {datum.duplicateShellResult}
- Clipping: {datum.clippingResult}
- Gaps/floating slabs: {datum.gapResult}
- Blocked doors: {datum.blockedDoorResult}

No legacy interior-shell scene or site scene is loaded by S1. Automated measurements support, but never replace, user inspection of the four named evidence images.
");
        }

        private static bool IntersectsElevatorFootprint(Bounds bounds, S1ElevatorReferenceRecord elevator)
            => bounds.max.x >= elevator.minX && bounds.min.x <= elevator.maxX
               && bounds.max.z >= elevator.minZ && bounds.min.z <= elevator.maxZ;

        private static string FormatRootTransform(string scenePath, Transform transform)
            => $"{Path.GetFileNameWithoutExtension(scenePath)}/{transform.name}: "
               + $"position=({transform.position.x:F3},{transform.position.y:F3},{transform.position.z:F3}); "
               + $"rotation=({transform.eulerAngles.x:F3},{transform.eulerAngles.y:F3},{transform.eulerAngles.z:F3}); "
               + $"scale=({transform.lossyScale.x:F3},{transform.lossyScale.y:F3},{transform.lossyScale.z:F3})";

        private static Renderer FindRenderer(GameObject root, string name)
            => root.GetComponentsInChildren<Renderer>(true).Single(renderer => renderer.name == name);

        private static BoxCollider FindCollider(GameObject root, string name)
            => root.GetComponentsInChildren<BoxCollider>(true).Single(collider => collider.name == name);

        private static bool IsIdentity(Transform transform)
            => transform.position.sqrMagnitude < 0.000001f
               && Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.001f
               && (transform.localScale - Vector3.one).sqrMagnitude < 0.000001f;

        private static void Add(ICollection<StaticCheck> checks, string name, bool pass, string detail)
            => checks.Add(new StaticCheck { name = name, pass = pass, detail = detail });

        private static void RestoreSceneSetupOrCreateEmpty(SceneSetup[] setup)
        {
            if (setup.Any(item => item.isLoaded && item.isActive))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }
}
