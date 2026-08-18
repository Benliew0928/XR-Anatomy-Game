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
    public static class HospitalInteriorS2R03Validator
    {
        private const string BaselineFileName = "StageI1_R03_S2_ProtectedBaseline.json";

        [Serializable]
        private sealed class BaselineEntry
        {
            public string kind;
            public string path;
            public string sha256;
            public string normalizedSha256;
            public string normalizationReason;
        }

        [Serializable]
        private sealed class ProtectedBaseline
        {
            public string schema;
            public string capturedAtLocal;
            public string unityVersion;
            public BaselineEntry[] files;
        }

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
            public string schema = "HospitalInterior.R03.S2.StaticGate.v1";
            public string status;
            public string unityVersion;
            public int passCount;
            public int totalCount;
            public StaticCheck[] checks;
        }

        [Serializable]
        private sealed class FloorInventory
        {
            public string floorId;
            public string scenePath;
            public float elevationM;
            public int rootCount;
            public string footprintSource;
            public string actualFootprintBounds;
            public Vector2[] integrationCoverageProbes;
            public string[] visibleObjects;
            public string[] colliderObjects;
            public string[] forbiddenObjects;
            public string status;
        }

        [Serializable]
        private sealed class InventoryReport
        {
            public string schema = "HospitalInterior.R03.S2.SceneInventory.v1";
            public string status;
            public string unityVersion;
            public string allowedVisibleContent = "One neutral slab and one Fxx label per floor; protected exterior context is outside floor-scene inventory.";
            public FloorInventory[] floors;
        }

        [MenuItem("Hospital Interior/R03 S2/Validate static gate", priority = 10)]
        public static void ValidateStaticGate()
        {
            var checks = new List<StaticCheck>();
            var inventories = new List<FloorInventory>();
            Add(checks, "unity_version", Application.unityVersion == "6000.3.20f1", Application.unityVersion);

            ProtectedBaseline baseline = LoadBaseline();
            ValidateProtectedBaseline(baseline, checks);
            ValidateS1DiagnosticMaterialSemantics(checks);

            S1DatumContract s1 = AssetDatabase.LoadAssetAtPath<S1DatumContract>(HospitalInteriorS1R03Builder.ContractPath);
            S2FloorContract contract = AssetDatabase.LoadAssetAtPath<S2FloorContract>(HospitalInteriorS2R03Builder.ContractPath);
            Add(checks, "s1_and_s2_contracts", s1 != null && contract != null,
                $"s1={s1 != null}; s2={contract != null}");
            if (s1 == null || contract == null)
                Finish(checks, inventories);

            bool exactProduction = contract.ProductionScenePaths.SequenceEqual(s1.ProductionScenePaths);
            Add(checks, "protected_production_scene_contract", exactProduction,
                string.Join(",", contract.ProductionScenePaths));
            Add(checks, "seven_unique_floor_records", contract.Floors.Count == 7
                && contract.Floors.Select(item => item.floorId).SequenceEqual(HospitalInteriorS2R03Builder.FloorIds)
                && contract.Floors.Select(item => item.scenePath).Distinct(StringComparer.Ordinal).Count() == 7,
                string.Join(",", contract.Floors.Select(item => item.floorId + "=" + item.scenePath)));

            bool copiedDatums = contract.Floors.Count == s1.FloorDatums.Length;
            for (int i = 0; copiedDatums && i < s1.FloorDatums.Length; i++)
            {
                S1FloorDatumRecord source = s1.FloorDatums[i];
                S2FloorRecord target = contract.Floors[i];
                copiedDatums &= source.floorId == target.floorId
                    && Mathf.Abs(source.elevationM - target.elevationM) <= 0.001f
                    && Mathf.Abs(source.minX - target.minX) <= 0.001f
                    && Mathf.Abs(source.maxX - target.maxX) <= 0.001f
                    && Mathf.Abs(source.minZ - target.minZ) <= 0.001f
                    && Mathf.Abs(source.maxZ - target.maxZ) <= 0.001f;
            }
            Add(checks, "s1_datums_copied_exactly", copiedDatums,
                string.Join(",", contract.Floors.Select(item => item.floorId + "=" + item.elevationM.ToString("F2"))));
            Add(checks, "review_contract", contract.InitialFloorId == "F00"
                && Mathf.Abs(contract.EyeHeightM - 1.7f) <= 0.001f
                && Mathf.Abs(contract.WalkSpeedMps - 2f) <= 0.001f
                && Mathf.Abs(contract.SprintSpeedMps - 4f) <= 0.001f,
                $"initial={contract.InitialFloorId}; eye={contract.EyeHeightM}; walk={contract.WalkSpeedMps}; sprint={contract.SprintSpeedMps}");

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                ValidateBootstrap(contract, checks);
                for (int i = 0; i < contract.Floors.Count; i++)
                    ValidateFloorScene(contract.Floors[i], i, checks, inventories);
            }
            finally
            {
                RestoreSceneSetupOrCreateEmpty(setup);
            }

            Finish(checks, inventories);
        }

        public static void ValidateStaticGateBatch()
        {
            try { ValidateStaticGate(); EditorApplication.Exit(0); }
            catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
        }

        private static ProtectedBaseline LoadBaseline()
        {
            string path = Path.Combine(HospitalInteriorS2R03Builder.ReviewFolder(), BaselineFileName);
            if (!File.Exists(path))
                throw new FileNotFoundException("S2 protected baseline is missing.", path);
            ProtectedBaseline baseline = JsonUtility.FromJson<ProtectedBaseline>(File.ReadAllText(path));
            if (baseline == null || baseline.files == null || baseline.files.Length == 0)
                throw new InvalidOperationException("S2 protected baseline could not be parsed.");
            return baseline;
        }

        private static void ValidateProtectedBaseline(ProtectedBaseline baseline, ICollection<StaticCheck> checks)
        {
            string workspace = HospitalInteriorS2R03Builder.WorkspaceRoot();
            foreach (IGrouping<string, BaselineEntry> group in baseline.files.GroupBy(item => item.kind))
            {
                var mismatches = new List<string>();
                int normalized = 0;
                foreach (BaselineEntry entry in group)
                {
                    string fullPath = Path.Combine(workspace, entry.path.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(fullPath))
                    {
                        mismatches.Add(entry.path + ":missing");
                        continue;
                    }
                    string actual = HospitalInteriorS2R03Builder.Sha256(fullPath);
                    if (string.Equals(actual, entry.normalizedSha256, StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(entry.normalizedSha256))
                        normalized++;
                    else if (!string.Equals(actual, entry.sha256, StringComparison.OrdinalIgnoreCase))
                        mismatches.Add(entry.path + ":changed");
                }
                Add(checks, "protected_" + group.Key, mismatches.Count == 0,
                    mismatches.Count == 0 ? group.Count() + " files protected; Unity-normalized=" + normalized : string.Join(",", mismatches));
            }
        }

        private static void ValidateS1DiagnosticMaterialSemantics(ICollection<StaticCheck> checks)
        {
            Material datum = AssetDatabase.LoadAssetAtPath<Material>(HospitalInteriorS1R03Builder.DatumMaterialPath);
            Material elevator = AssetDatabase.LoadAssetAtPath<Material>(HospitalInteriorS1R03Builder.CageMaterialPath);
            bool pass = datum != null && elevator != null
                && datum.shader != null && elevator.shader != null
                && datum.shader.name == "Universal Render Pipeline/Unlit"
                && elevator.shader.name == "Universal Render Pipeline/Unlit"
                && ColorsMatch(datum.GetColor("_BaseColor"), new Color(0.05f, 0.82f, 1f, 0.22f))
                && ColorsMatch(elevator.GetColor("_BaseColor"), new Color(1f, 0.48f, 0.06f, 0.88f))
                && Mathf.Abs(datum.GetFloat("_Surface") - 1f) <= 0.001f
                && Mathf.Abs(datum.GetFloat("_ZWrite")) <= 0.001f
                && Mathf.Abs(elevator.GetFloat("_Surface")) <= 0.001f
                && Mathf.Abs(elevator.GetFloat("_ZWrite") - 1f) <= 0.001f;
            Add(checks, "s1_diagnostic_material_semantics", pass,
                pass ? "Shader, colors, transparency, and depth-write semantics unchanged." : "S1 diagnostic material semantics changed.");
        }

        private static bool ColorsMatch(Color actual, Color expected)
            => Mathf.Abs(actual.r - expected.r) <= 0.001f && Mathf.Abs(actual.g - expected.g) <= 0.001f
               && Mathf.Abs(actual.b - expected.b) <= 0.001f && Mathf.Abs(actual.a - expected.a) <= 0.001f;

        private static void ValidateBootstrap(S2FloorContract contract, ICollection<StaticCheck> checks)
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(HospitalInteriorS2R03Builder.BootstrapScenePath);
            Add(checks, "bootstrap_scene_exists", asset != null, HospitalInteriorS2R03Builder.BootstrapScenePath);
            if (asset == null)
                return;
            Scene scene = EditorSceneManager.OpenScene(HospitalInteriorS2R03Builder.BootstrapScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            S2ManualFloorLoader[] loaders = roots.SelectMany(root => root.GetComponentsInChildren<S2ManualFloorLoader>(true)).ToArray();
            Camera[] cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
            Light[] lights = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
            AudioListener[] listeners = roots.SelectMany(root => root.GetComponentsInChildren<AudioListener>(true)).ToArray();
            S2ReviewLocomotion[] locomotion = roots.SelectMany(root => root.GetComponentsInChildren<S2ReviewLocomotion>(true)).ToArray();
            bool rootIdentity = roots.Length == 1 && IsIdentity(roots[0].transform);
            bool references = loaders.Length == 1 && loaders[0].FloorContract == contract && loaders[0].XrOrigin != null
                && loaders[0].ReviewCamera != null && loaders[0].Locomotion != null;
            Add(checks, "bootstrap_identity_and_references", rootIdentity && references,
                $"roots={roots.Length}; loaders={loaders.Length}; references={references}");
            Add(checks, "bootstrap_unique_review_services", cameras.Length == 1 && lights.Length == 1
                && listeners.Length == 1 && locomotion.Length == 1,
                $"cameras={cameras.Length}; lights={lights.Length}; listeners={listeners.Length}; locomotion={locomotion.Length}");
            bool noDiagnostics = roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true))
                .All(component => component is not S1Bootstrap && component is not S1DatumProxy
                    && component is not S1DiagnosticRoot && component is not S1ElevatorReference);
            Add(checks, "bootstrap_has_no_s1_diagnostics", noDiagnostics, noDiagnostics ? "none" : "S1 diagnostic component found");
            Add(checks, "bootstrap_contains_no_floor_content",
                roots.SelectMany(root => root.GetComponentsInChildren<S2FloorSceneMarker>(true)).Count() == 0,
                "No floor-scene marker is persistent; floor scenes are loaded manually at runtime.");
            MeshRenderer[] structuralUndersides = roots.SelectMany(root => root.GetComponentsInChildren<MeshRenderer>(true))
                .Where(renderer => renderer.name.StartsWith("S2_R03_StructuralUnderside_", StringComparison.Ordinal)).ToArray();
            bool structuralContext = structuralUndersides.Length == 6
                && structuralUndersides.Select(renderer => renderer.name).OrderBy(name => name)
                    .SequenceEqual(HospitalInteriorS2R03Builder.FloorIds.Skip(1)
                        .Select(id => "S2_R03_StructuralUnderside_" + id).OrderBy(name => name))
                && structuralUndersides.All(renderer => renderer.GetComponent<Collider>() == null
                    && renderer.GetComponent<MeshFilter>()?.sharedMesh != null
                    && renderer.sharedMaterial != null
                    && contract.TryGetFloor(renderer.name.Substring(renderer.name.Length - 3), out S2FloorRecord floor)
                    && Mathf.Abs(renderer.transform.position.y - (floor.elevationM - 0.20f)) <= 0.001f);
            Add(checks, "bootstrap_persistent_structural_storey_separation", structuralContext,
                "undersides=" + string.Join(",", structuralUndersides.Select(renderer => renderer.name)));
            Material slabMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalInteriorS2R03Builder.SlabMaterialPath);
            Material undersideMaterial = AssetDatabase.LoadAssetAtPath<Material>(HospitalInteriorS2R03Builder.StructuralUndersideMaterialPath);
            bool junctionMaterials = slabMaterial != null && undersideMaterial != null
                && slabMaterial.shader?.name == "Universal Render Pipeline/Lit"
                && undersideMaterial.shader?.name == "Universal Render Pipeline/Lit"
                && ColorNear(slabMaterial.GetColor("_BaseColor"), new Color(0.28f, 0.29f, 0.29f, 1f))
                && Mathf.Abs(slabMaterial.GetFloat("_Smoothness") - 0.76f) <= 0.001f
                && ColorNear(undersideMaterial.GetColor("_BaseColor"), new Color(0.82f, 0.84f, 0.83f, 1f))
                && Mathf.Abs(undersideMaterial.GetFloat("_Smoothness") - 0.70f) <= 0.001f;
            Add(checks, "bootstrap_balcony_junction_material_continuity", junctionMaterials,
                "S2 floor matches the protected balcony floor finish; structural undersides match the adjoining warm off-white soffit.");
            bool backerContract = S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Length == 6
                && S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.Distinct(StringComparer.Ordinal).Count() == 6
                && S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames.All(name =>
                    name.StartsWith("UE_EXT_Balconies_OPAQUE_L", StringComparison.Ordinal) && name.EndsWith("_05", StringComparison.Ordinal));
            Add(checks, "bootstrap_exterior_only_backer_suppression_contract", backerContract,
                string.Join(",", S2ManualFloorLoader.ExteriorOnlyBalconyBackerNames));
            bool floorSkinContract = S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Length == 6
                && S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.Distinct(StringComparer.Ordinal).Count() == 6
                && S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames.All(name =>
                    name.StartsWith("UE_EXT_Balconies_OPAQUE_L", StringComparison.Ordinal) && name.EndsWith("_04", StringComparison.Ordinal));
            Add(checks, "bootstrap_overlapping_balcony_floor_skin_suppression_contract", floorSkinContract,
                string.Join(",", S2ManualFloorLoader.ExteriorOverlappingBalconyFloorSkinNames));
        }

        private static void ValidateFloorScene(S2FloorRecord floor, int index, ICollection<StaticCheck> checks,
            ICollection<FloorInventory> inventories)
        {
            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(floor.scenePath);
            if (asset == null)
            {
                Add(checks, floor.floorId + "_scene_exists", false, floor.scenePath);
                inventories.Add(new FloorInventory { floorId = floor.floorId, scenePath = floor.scenePath, elevationM = floor.elevationM, status = "FAIL_MISSING" });
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(floor.scenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            S2FloorSceneMarker[] markers = roots.SelectMany(root => root.GetComponentsInChildren<S2FloorSceneMarker>(true)).ToArray();
            S2ArrivalAnchor[] arrivals = roots.SelectMany(root => root.GetComponentsInChildren<S2ArrivalAnchor>(true)).ToArray();
            TextMesh[] labels = roots.SelectMany(root => root.GetComponentsInChildren<TextMesh>(true)).ToArray();
            Renderer[] renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).ToArray();
            Collider[] colliders = roots.SelectMany(root => root.GetComponentsInChildren<Collider>(true)).ToArray();
            string[] visible = renderers.Where(item => item.enabled).Select(item => item.name).OrderBy(name => name).ToArray();
            string[] colliderNames = colliders.Select(item => item.name).OrderBy(name => name).ToArray();
            string[] forbiddenTokens = { "Stair", "Room", "Corridor", "Furniture", "Clinical", "Cabin", "Door", "ControlPanel", "Ceiling", "E02" };
            string[] forbidden = roots.SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(item => item.name).Where(name => forbiddenTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase))).ToArray();

            bool identity = roots.Length == 1 && IsIdentity(roots[0].transform);
            bool stageMarkers = markers.Length == 1 && markers[0].FloorId == floor.floorId
                && Mathf.Abs(markers[0].ElevationM - floor.elevationM) <= 0.001f
                && arrivals.Length == 1 && arrivals[0].FloorId == floor.floorId
                && labels.Length == 1 && labels[0].text == floor.floorId;
            Add(checks, floor.floorId + "_identity_and_markers", identity && stageMarkers,
                $"identity={identity}; marker={markers.Length}; arrival={arrivals.Length}; label={labels.Length}");

            bool allowlist = visible.Length == 2 && visible.Contains("S2_R03_EmptySlab") && visible.Contains("S2_R03_FloorLabel")
                && forbidden.Length == 0 && roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Count() == 0
                && roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).Count() == 0;
            Add(checks, floor.floorId + "_empty_visible_allowlist", allowlist,
                $"visible={string.Join(",", visible)}; forbidden={string.Join(",", forbidden)}");

            MeshCollider[] slabColliders = colliders.OfType<MeshCollider>().Where(item => item.name == "S2_R03_EmptySlab").ToArray();
            float footprintMinX = floor.footprint.Min(point => point.x);
            float footprintMaxX = floor.footprint.Max(point => point.x);
            float footprintMinZ = floor.footprint.Min(point => point.y);
            float footprintMaxZ = floor.footprint.Max(point => point.y);
            bool slab = slabColliders.Length == 1 && slabColliders[0].sharedMesh != null
                && Mathf.Abs(slabColliders[0].transform.position.y - floor.elevationM) <= 0.001f
                && Mathf.Abs(slabColliders[0].sharedMesh.bounds.min.x - footprintMinX) <= 0.001f
                && Mathf.Abs(slabColliders[0].sharedMesh.bounds.max.x - footprintMaxX) <= 0.001f
                && Mathf.Abs(slabColliders[0].sharedMesh.bounds.min.z - footprintMinZ) <= 0.001f
                && Mathf.Abs(slabColliders[0].sharedMesh.bounds.max.z - footprintMaxZ) <= 0.001f;
            bool placement = arrivals.Length == 1 && Vector3.Distance(arrivals[0].transform.localPosition, floor.arrivalPosition) <= 0.001f
                && labels.Length == 1 && Vector3.Distance(labels[0].transform.localPosition, floor.labelPosition) <= 0.001f;
            Add(checks, floor.floorId + "_datum_footprint_and_arrival", slab && placement,
                $"slab={slab}; placement={placement}; elevation={floor.elevationM:F2}; actualX={footprintMinX:F2}..{footprintMaxX:F2}; actualZ={footprintMinZ:F2}..{footprintMaxZ:F2}");

            Vector2[] probes = floor.integrationCoverageProbes ?? Array.Empty<Vector2>();
            bool integrationSeams;
            string integrationDetail;
            if (index == 0)
            {
                integrationSeams = probes.Length == 0 && floor.footprint.Length == 8;
                integrationDetail = "F00 keeps the approved production-derived eight-vertex footprint; upper seam probes are not applicable.";
            }
            else
            {
                Physics.SyncTransforms();
                integrationSeams = slabColliders.Length == 1 && probes.Length == 4
                    && footprintMinX <= -36f && footprintMaxX >= 18f
                    && footprintMinZ <= -16.9f && footprintMaxZ >= 17.1f
                    && !string.IsNullOrWhiteSpace(floor.footprintSource)
                    && probes.All(probe => slabColliders[0].Raycast(
                        new Ray(new Vector3(probe.x, floor.elevationM + 1f, probe.y), Vector3.down), out _, 2f));
                integrationDetail = "probes=" + string.Join(",", probes.Select(probe => $"({probe.x:F2},{probe.y:F2})"))
                    + $"; actualX={footprintMinX:F2}..{footprintMaxX:F2}; actualZ={footprintMinZ:F2}..{footprintMaxZ:F2}";
            }
            Add(checks, floor.floorId + "_integration_seam_coverage", integrationSeams, integrationDetail);

            BoxCollider[] boundaries = colliders.OfType<BoxCollider>()
                .Where(item => item.name.StartsWith("S2_R03_SafetyBoundary_", StringComparison.Ordinal)).ToArray();
            BoxCollider[] safeZones = colliders.OfType<BoxCollider>().Where(item => item.name == "S2_R03_ElevatorArrivalAnchor" && item.isTrigger).ToArray();
            bool safety = boundaries.Length >= 4 && safeZones.Length == 1;
            if (floor.floorId == "F00")
            {
                Vector3 opening = new Vector3((floor.entranceOpeningMinX + floor.entranceOpeningMaxX) * 0.5f,
                    floor.elevationM + 1f, floor.minZ);
                safety &= floor.hasEntranceOpening && floor.entranceOpeningMaxX > floor.entranceOpeningMinX
                    && boundaries.All(boundary => !boundary.bounds.Contains(opening));
            }
            Add(checks, floor.floorId + "_safe_boundary_and_entrance", safety,
                $"boundaries={boundaries.Length}; safeZones={safeZones.Length}; entranceOpening={floor.hasEntranceOpening}");

            string sceneText = File.ReadAllText(Path.Combine(HospitalInteriorS2R03Builder.ProjectRoot(), floor.scenePath));
            bool noLegacy = !sceneText.Contains("PreProduction/I1/R01", StringComparison.OrdinalIgnoreCase)
                && !sceneText.Contains("PreProduction/I1/R02", StringComparison.OrdinalIgnoreCase)
                && !sceneText.Contains("SourceFBX", StringComparison.OrdinalIgnoreCase);
            Add(checks, floor.floorId + "_no_legacy_scene_or_fbx_reference", noLegacy, noLegacy ? "none" : "legacy token found");

            bool floorPass = identity && stageMarkers && allowlist && slab && placement && integrationSeams && safety && noLegacy;
            inventories.Add(new FloorInventory
            {
                floorId = floor.floorId,
                scenePath = floor.scenePath,
                elevationM = floor.elevationM,
                rootCount = roots.Length,
                footprintSource = floor.footprintSource,
                actualFootprintBounds = $"X {footprintMinX:F2}..{footprintMaxX:F2}; Z {footprintMinZ:F2}..{footprintMaxZ:F2}",
                integrationCoverageProbes = probes,
                visibleObjects = visible,
                colliderObjects = colliderNames,
                forbiddenObjects = forbidden,
                status = floorPass ? "PASS" : "FAIL",
            });
        }

        private static void Finish(List<StaticCheck> checks, List<FloorInventory> inventories)
        {
            bool pass = checks.All(check => check.pass) && inventories.Count == 7 && inventories.All(item => item.status == "PASS");
            var gate = new StaticGate
            {
                status = pass ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                passCount = checks.Count(check => check.pass),
                totalCount = checks.Count,
                checks = checks.ToArray(),
            };
            var inventory = new InventoryReport
            {
                status = pass ? "PASS" : "FAIL",
                unityVersion = Application.unityVersion,
                floors = inventories.ToArray(),
            };
            Directory.CreateDirectory(HospitalInteriorS2R03Builder.ReviewFolder());
            File.WriteAllText(Path.Combine(HospitalInteriorS2R03Builder.ReviewFolder(), "StageI1_R03_S2_StaticGate.json"),
                JsonUtility.ToJson(gate, true));
            File.WriteAllText(Path.Combine(HospitalInteriorS2R03Builder.ReviewFolder(), "StageI1_R03_S2_SceneInventory.json"),
                JsonUtility.ToJson(inventory, true));
            Debug.Log($"HOSPITAL_INTERIOR_R03_S2_STATIC_GATE={gate.status}; {gate.passCount}/{gate.totalCount}");
            if (!pass)
                throw new InvalidOperationException("R03 S2 static gate failed. See the generated reports.");
        }

        private static bool IsIdentity(Transform transform)
            => transform.position.sqrMagnitude < 0.000001f
               && Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.001f
               && (transform.localScale - Vector3.one).sqrMagnitude < 0.000001f;

        private static bool ColorNear(Color actual, Color expected)
            => Mathf.Abs(actual.r - expected.r) <= 0.001f
               && Mathf.Abs(actual.g - expected.g) <= 0.001f
               && Mathf.Abs(actual.b - expected.b) <= 0.001f
               && Mathf.Abs(actual.a - expected.a) <= 0.001f;

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
