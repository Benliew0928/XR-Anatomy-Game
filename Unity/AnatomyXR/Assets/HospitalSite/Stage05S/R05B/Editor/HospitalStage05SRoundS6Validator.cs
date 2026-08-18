using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalSite.Stage05S;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CutMyBodyPlease.HospitalSite.Stage05S.Editor
{
    /// <summary>Independent, fail-closed validation of the artifacts produced by the S6 builder.</summary>
    public static class HospitalStage05SRoundS6Validator
    {
        [MenuItem("Hospital/Stage 5S/Validate Round S6 final gate")]
        public static void ValidateFinalGate()
        {
            string workspace = HospitalStage05SRoundS6Builder.WorkspaceRoot();
            string reviews = HospitalStage05SRoundS6Builder.Reviews(workspace);
            Directory.CreateDirectory(reviews);
            var checks = new List<GateCheck>();

            Check(checks, "protected_preflight", () => HospitalStage05SRoundS6Builder.ValidateProtectedInputs(workspace));
            Check(checks, "copied_source_hashes", ValidateCopiedSources);
            Check(checks, "import_policy", ValidateImportPolicy);
            Check(checks, "controlled_materials", ValidateMaterials);
            Check(checks, "raw_payload_preserved", ValidateRawPayload);
            Check(checks, "vegetation_lod_contract", ValidateLods);
            Check(checks, "collision_contract", ValidateCollision);
            Check(checks, "grass_runtime_contract", ValidateGrass);
            Check(checks, "gate_root_contract", ValidateGateRoots);
            Check(checks, "three_additive_site_scenes", ValidateSiteScenes);
            Check(checks, "eight_scene_build_settings", ValidateBuildSettings);
            Check(checks, "scene_load_matrix", ValidateSceneLoadMatrix);
            Check(checks, "visual_qa_evidence", () => ValidateEvidence(reviews));
            Check(checks, "performance_proxy_structure", () => ValidatePerformanceRecord(reviews));
            Check(checks, "original_regressions", () => ValidateRegressions(workspace));
            Check(checks, "required_integration_records", () => ValidateRecords(reviews));

            bool pass = checks.All(c => c.passed);
            var gate = new GateRecord
            {
                schema = "HospitalExterior.Stage05S.S6.UnityGate.v1",
                status = pass ? "PASS" : "FAIL",
                pass = pass,
                authority_sha256 = HospitalStage05SRoundS6Builder.AuthoritySha,
                approval = "S6 APPROVED / STAGE 5S COMPLETE",
                unity_editor = Application.unityVersion,
                measurement_scope = "S6 integration and editor proxy only; Quest Link/Air Link performance gate deferred to Stage 6",
                protected_road_path_and_landscape_changes = 0,
                checks_passed = checks.Count(c => c.passed),
                checks_total = checks.Count,
                checks = checks.ToArray(),
                next_required_action = pass ? "Begin Stage 6A Quest Link/Air Link headset traversal harness and real stereo baseline." : "Resolve failed checks without changing protected R05B/R40/R44 authority bytes.",
            };
            HospitalStage05SRoundS6Builder.WriteJson(gate, Path.Combine(reviews, "Stage05S_RoundS6_TechnicalGate.json"));
            HospitalStage05SRoundS6Builder.WriteJson(gate, Path.Combine(reviews, "Stage05S_RoundS6_UnityGate.json"));
            HospitalStage05SRoundS6Builder.WriteJson(gate, HospitalStage05SRoundS6Builder.AssetPathToAbsolute(HospitalStage05SRoundS6Builder.SettingsFolder + "/Stage05S_RoundS6_UnityGate.json"));
            WriteManifest(workspace, reviews);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.SaveAssets();
            if (!pass) throw new BuildFailedException("Stage 05S Round S6 validation failed. See " + Path.Combine(reviews, "Stage05S_RoundS6_UnityGate.json"));
        }

        private static void ValidateCopiedSources()
        {
            foreach (var input in HospitalStage05SRoundS6Builder.SiteInputs)
                Require(HospitalStage05SRoundS6Builder.Sha256(HospitalStage05SRoundS6Builder.AssetPathToAbsolute(HospitalStage05SRoundS6Builder.SourceFolder + "/" + input.relative)) == input.sha256, "FBX copy mismatch: " + input.relative);
            foreach (var input in HospitalStage05SRoundS6Builder.TextureInputs)
            {
                string asset = input.relative == "GrassRuntimeSpec.json" ? HospitalStage05SRoundS6Builder.GeneratedFolder + "/GrassRuntimeSpec.json" : HospitalStage05SRoundS6Builder.TextureFolder + "/" + Path.GetFileName(input.relative);
                Require(HospitalStage05SRoundS6Builder.Sha256(HospitalStage05SRoundS6Builder.AssetPathToAbsolute(asset)) == input.sha256, "Texture/contract copy mismatch: " + input.relative);
            }
            for (int i = 0; i < HospitalStage05SRoundS6Builder.HospitalScenes.Length; i++)
                Require(HospitalStage05SRoundS6Builder.Sha256(HospitalStage05SRoundS6Builder.AssetPathToAbsolute(HospitalStage05SRoundS6Builder.HospitalScenes[i])) == HospitalStage05SRoundS6Builder.HospitalSceneHashes[i], "Frozen R44 scene changed: " + HospitalStage05SRoundS6Builder.HospitalScenes[i]);
        }

        private static void ValidateImportPolicy()
        {
            foreach (var input in HospitalStage05SRoundS6Builder.SiteInputs)
            {
                string path = HospitalStage05SRoundS6Builder.SourceFolder + "/" + input.relative;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                Require(importer != null, "Missing importer: " + path);
                Require(Mathf.Approximately(importer.globalScale, 1f) && importer.useFileScale && importer.bakeAxisConversion, "Metre/common-origin import mismatch: " + path);
                Require(!importer.importAnimation && importer.animationType == ModelImporterAnimationType.None && !importer.addCollider, "Animation/automatic collider enabled: " + path);
                Require(importer.importNormals == ModelImporterNormals.Import && importer.importTangents == ModelImporterTangents.CalculateMikk, "Normals/tangents mismatch: " + path);
                Require(!importer.isReadable && importer.meshCompression == ModelImporterMeshCompression.Off && importer.preserveHierarchy, "Read/write, compression, or hierarchy mismatch: " + path);
                Require(importer.materialLocation == ModelImporterMaterialLocation.External, "Materials must be external controlled assets: " + path);
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Require(source.GetComponentsInChildren<Camera>(true).Length == 0 && source.GetComponentsInChildren<Light>(true).Length == 0 && source.GetComponentsInChildren<Collider>(true).Length == 0, "Forbidden imported component: " + path);
            }
            var baseColor = (TextureImporter)AssetImporter.GetAtPath(HospitalStage05SRoundS6Builder.TextureFolder + "/T_S05S_Grass005_BaseColor_2K.png");
            var normal = (TextureImporter)AssetImporter.GetAtPath(HospitalStage05SRoundS6Builder.TextureFolder + "/T_S05S_Grass005_NormalGL_2K.png");
            var roughness = (TextureImporter)AssetImporter.GetAtPath(HospitalStage05SRoundS6Builder.TextureFolder + "/T_S05S_Grass005_Roughness_2K.png");
            var ao = (TextureImporter)AssetImporter.GetAtPath(HospitalStage05SRoundS6Builder.TextureFolder + "/T_S05S_Grass005_AO_2K.png");
            var mask = (TextureImporter)AssetImporter.GetAtPath(HospitalStage05SRoundS6Builder.TextureFolder + "/T_S05S_GrassCoverageMask_2K.png");
            Require(baseColor.sRGBTexture && !baseColor.isReadable, "Grass Base Color must be sRGB and runtime non-readable.");
            Require(normal.textureType == TextureImporterType.NormalMap && !normal.sRGBTexture && !normal.flipGreenChannel && !normal.isReadable, "NormalGL must be a linear Y+/OpenGL normal map without green-channel inversion.");
            Require(!roughness.sRGBTexture && !roughness.isReadable && !ao.sRGBTexture && !ao.isReadable, "Grass Roughness/AO must be linear runtime data.");
            Require(!mask.sRGBTexture && !mask.isReadable, "Runtime mask must be linear and GPU-only/non-readable.");
        }

        private static void ValidateMaterials()
        {
            string[] site = Directory.GetFiles(HospitalStage05SRoundS6Builder.AssetPathToAbsolute(HospitalStage05SRoundS6Builder.MaterialFolder), "*.mat", SearchOption.TopDirectoryOnly);
            Require(site.Length == 20, "Expected exactly 20 site materials; found " + site.Length);
            foreach (string shared in new[] { "MAT_Aluminium_Charcoal", "MAT_Composite_LightGrey", "MAT_Composite_WarmOffWhite", "MAT_Entrance_HonedTravertine", "MAT_Entrance_RefinedBronze" })
                Require(AssetDatabase.LoadAssetAtPath<Material>("Assets/Hospital/Materials/" + shared + ".mat") != null, "Missing reused hospital material: " + shared);
            foreach (var input in HospitalStage05SRoundS6Builder.SiteInputs)
            {
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.SourceFolder + "/" + input.relative);
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) foreach (Material material in renderer.sharedMaterials)
                {
                    string path = AssetDatabase.GetAssetPath(material);
                    Require(path.StartsWith(HospitalStage05SRoundS6Builder.MaterialFolder + "/") || path.StartsWith("Assets/Hospital/Materials/"), "Generated/duplicate material found: " + path);
                }
            }
            Material blade = AssetDatabase.LoadAssetAtPath<Material>(HospitalStage05SRoundS6Builder.MaterialFolder + "/MAT_S05S_S5B_GrassBlade.mat");
            Material lawn = AssetDatabase.LoadAssetAtPath<Material>(HospitalStage05SRoundS6Builder.MaterialFolder + "/MAT_S05S_S5B_GrassLawn.mat");
            Require(blade.enableInstancing && lawn.enableInstancing, "Grass materials must support instancing.");
            Require(Mathf.Approximately(lawn.GetFloat("_BaseTileMetres"), 2f) && Mathf.Approximately(lawn.GetFloat("_MacroTileMetres"), 14f) && Mathf.Approximately(lawn.GetFloat("_NormalStrength"), .32f), "Approved lawn mapping changed.");
        }

        private static void ValidateRawPayload()
        {
            var expected = new Dictionary<string, int>
            {
                { "Hospital_SITE_CoreRoadParking_R05B.fbx", 120 }, { "Hospital_SITE_GrassRuntimeAssets_R05B.fbx", 2 },
                { "Hospital_SITE_LandscapeHardscape_R05B.fbx", 12 }, { "Hospital_SITE_MainGate_R05B.fbx", 21 },
                { "Hospital_SITE_PerimeterFence_R05B.fbx", 9 }, { "Hospital_SITE_PropsVegetation_R05B.fbx", 308 },
                { "Hospital_SITE_RearService_R05B.fbx", 6 },
            };
            int totalRenderers = 0; long totalTriangles = 0;
            foreach (var item in expected)
            {
                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.SourceFolder + "/" + item.Key);
                MeshRenderer[] renderers = source.GetComponentsInChildren<MeshRenderer>(true); Require(renderers.Length == item.Value, $"{item.Key}: expected {item.Value} renderers; found {renderers.Length}");
                totalRenderers += renderers.Length; totalTriangles += renderers.Sum(HospitalStage05SRoundS6Builder.TriangleCount);
            }
            Require(totalRenderers == 478, "Raw renderer total changed: " + totalRenderers);
            Require(totalTriangles == 6941798, "Raw all-tier triangle total changed: " + totalTriangles);
        }

        private static void ValidateLods()
        {
            GameObject prefab = P("SITE_PropsVegetation"); LODGroup[] groups = prefab.GetComponentsInChildren<LODGroup>(true);
            Require(groups.Length == 99, "Expected 99 LODGroups; found " + groups.Length);
            Require(groups.Count(g => g.name.Contains("Tree_")) == 27 && groups.Count(g => g.name.Contains("TopiaryColumn_")) == 15 && groups.Count(g => g.name.Contains("Hedge_")) == 41 && groups.Count(g => g.name.Contains("AlliumDrift_")) == 16, "Vegetation family group counts changed.");
            Require(!prefab.GetComponentsInChildren<Transform>(true).Any(t => t.name.IndexOf("OrnamentalGrass", StringComparison.OrdinalIgnoreCase) >= 0 || t.name.IndexOf("TallTuft", StringComparison.OrdinalIgnoreCase) >= 0), "Removed ornamental-grass family reappeared.");
            var meshes = new HashSet<Mesh>(); long[] totals = new long[3];
            foreach (LODGroup group in groups)
            {
                LOD[] lods = group.GetLODs(); Require(lods.Length == 3 && lods.All(l => l.renderers.Length == 1), "Each group must have exactly three single-renderer tiers: " + group.name);
                Require(Approximately(lods[0].screenRelativeTransitionHeight,.60f) && Approximately(lods[1].screenRelativeTransitionHeight,.22f) && Approximately(lods[2].screenRelativeTransitionHeight,.06f), "LOD thresholds changed: " + group.name);
                Require(lods.All(l => Approximately(l.fadeTransitionWidth,.05f)) && group.fadeMode == LODFadeMode.CrossFade && !group.animateCrossFading, "LOD cross-fade contract changed: " + group.name);
                for (int tier=0;tier<3;tier++) { Renderer r=lods[tier].renderers[0]; meshes.Add(r.GetComponent<MeshFilter>().sharedMesh); totals[tier]+=HospitalStage05SRoundS6Builder.TriangleCount(r); }
                ValidateShadowContract(group);
            }
            Require(meshes.Count == 18, "Expected 18 shared vegetation meshes; found " + meshes.Count);
            Require(totals[0] == 5367488 && totals[1] == 1180274 && totals[2] == 348274, $"LOD triangle totals changed: {totals[0]}/{totals[1]}/{totals[2]}");
            Require(groups.All(g => g.GetLODs().SelectMany(l => l.renderers).All(r => r.GetComponent<Renderer>().gameObject.isStatic == false)), "Vegetation must not be static batched.");
        }

        private static void ValidateShadowContract(LODGroup group)
        {
            LOD[] lods=group.GetLODs(); ShadowCastingMode[] actual=lods.Select(l=>l.renderers[0].shadowCastingMode).ToArray();
            if(group.name.Contains("Tree_")) Require(actual[0]==ShadowCastingMode.On&&actual[1]==ShadowCastingMode.On&&actual[2]==ShadowCastingMode.Off,"Tree shadow tiers changed: "+group.name);
            else if(group.name.Contains("Hedge_")||group.name.Contains("TopiaryColumn_")) Require(actual[0]==ShadowCastingMode.On&&actual[1]==ShadowCastingMode.Off&&actual[2]==ShadowCastingMode.Off,"Hedge/topiary shadow tiers changed: "+group.name);
            else Require(actual.All(x=>x==ShadowCastingMode.Off),"Allium must not cast shadows: "+group.name);
        }

        private static void ValidateCollision()
        {
            GameObject core=P("SITE_CoreRoadParking"), fence=P("SITE_PerimeterFence"), gate=P("SITE_MainGate"), props=P("SITE_PropsVegetation"), grass=P("SITE_GrassRuntime");
            Require(core.GetComponentsInChildren<MeshCollider>(true).Length==20&&core.GetComponentsInChildren<BoxCollider>(true).Length==0&&core.GetComponentsInChildren<CapsuleCollider>(true).Length==0,"Core collision must be exactly 20 route MeshColliders.");
            Require(core.GetComponentsInChildren<MeshCollider>(true).All(c=>c.GetComponent<Renderer>()==null&&c.sharedMesh!=null),"Visual road/path detail received collision.");
            Require(fence.GetComponentsInChildren<BoxCollider>(true).Length==5&&fence.GetComponentsInChildren<MeshCollider>(true).Length==0,"Fence collision must be five boxes.");
            Require(gate.GetComponentsInChildren<BoxCollider>(true).Length==2,"Gate leaves require exactly two fitted boxes.");
            Require(props.GetComponentsInChildren<MeshCollider>(true).Length==0&&props.GetComponentsInChildren<BoxCollider>(true).Length==5&&props.GetComponentsInChildren<CapsuleCollider>(true).Length==32,"Props require five bench boxes, five bin capsules, and 27 tree capsules.");
            Require(grass.GetComponentsInChildren<Collider>(true).Length==0,"Grass must never participate in collision.");
        }

        private static void ValidateGrass()
        {
            HospitalSiteGrassRuntimeConfig config=AssetDatabase.LoadAssetAtPath<HospitalSiteGrassRuntimeConfig>(HospitalStage05SRoundS6Builder.SettingsFolder+"/HospitalSiteGrassRuntimeConfig.asset");
            string detail = "config missing";
            Require(config!=null&&config.IsContractValid(out detail),"Grass config invalid: "+detail);
            Require(config.CountOccupied()>0&&config.CountOccupied()<config.OccupancyLength,"Occupancy grid is empty/full and cannot exclude hardscape.");
            foreach(Vector2 p in new[]{new Vector2(0,-64),new Vector2(10,-64),new Vector2(36,-64),new Vector2(0,-42),new Vector2(-56,-80)})
                Require(!HospitalStage05SRoundS6Builder.OccupiedAt(config,p.x,p.y),"Grass broad phase includes protected non-lawn at "+p);
            foreach(Vector2 p in new[]{new Vector2(-8,-52),new Vector2(-24,-56),new Vector2(-10,-46)})
                Require(HospitalStage05SRoundS6Builder.OccupiedAt(config,p.x,p.y),"Grass broad phase lost approved lawn at "+p);
            Require(HospitalStage05SRoundS6Builder.TriangleCount(config.lod0Patch)==768&&HospitalStage05SRoundS6Builder.TriangleCount(config.lod1Patch)==320,"Grass patch mesh counts changed.");
            GameObject instance=(GameObject)PrefabUtility.InstantiatePrefab(P("SITE_GrassRuntime"));
            try { HospitalSiteGrassRenderer renderer=instance.GetComponent<HospitalSiteGrassRenderer>(); renderer.ForceRebuild(new Vector3(-8f,1.7f,-52f)); Require(renderer.ActiveTriangles>0&&renderer.ActiveTriangles<=65000&&renderer.ActiveDrawSubmissions<=2,"Grass active budget failed."); }
            finally { Object.DestroyImmediate(instance); }
        }

        private static void ValidateGateRoots()
        {
            Transform[] roots=P("SITE_MainGate").GetComponentsInChildren<Transform>(true).Where(t=>t.name.IndexOf("GateLeafRoot",StringComparison.OrdinalIgnoreCase)>=0).ToArray();
            Require(roots.Length==2&&roots.All(r=>r.GetComponent<BoxCollider>()!=null),"Exactly two independent collidable gate leaf roots are required.");
        }

        private static void ValidateSiteScenes()
        {
            foreach(var spec in HospitalStage05SRoundS6Builder.SiteScenes)
            {
                Scene scene=EditorSceneManager.OpenScene(HospitalStage05SRoundS6Builder.ScenePath(spec.name),OpenSceneMode.Single);
                string[] roots=scene.GetRootGameObjects().Select(r=>r.name).OrderBy(x=>x).ToArray(); Require(roots.SequenceEqual(spec.zones.OrderBy(x=>x)),"Scene ownership mismatch: "+spec.name);
                Require(scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Camera>(true).Length)==0&&scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Light>(true).Length)==0,"Production site scene contains camera/light: "+spec.name);
            }
        }

        private static void ValidateBuildSettings()
        {
            string[] expected=HospitalStage05SRoundS6Builder.HospitalScenes.Concat(HospitalStage05SRoundS6Builder.SiteScenes.Select(s=>HospitalStage05SRoundS6Builder.ScenePath(s.name))).ToArray();
            Require(EditorBuildSettings.scenes.Length==8&&EditorBuildSettings.scenes.All(s=>s.enabled)&&EditorBuildSettings.scenes.Select(s=>s.path).SequenceEqual(expected),"Build Settings must contain the five frozen hospital scenes plus exactly three site scenes.");
        }

        private static void ValidateSceneLoadMatrix()
        {
            foreach(string path in HospitalStage05SRoundS6Builder.HospitalScenes) EditorSceneManager.OpenScene(path,path==HospitalStage05SRoundS6Builder.HospitalScenes[0]?OpenSceneMode.Single:OpenSceneMode.Additive);
            Require(Object.FindObjectsByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Hospital-only state contains a site grass manager.");
            foreach(var scene in HospitalStage05SRoundS6Builder.SiteScenes) EditorSceneManager.OpenScene(HospitalStage05SRoundS6Builder.ScenePath(scene.name),OpenSceneMode.Additive);
            Require(SceneManager.sceneCount==8,"All-eight load state failed.");
            Require(Object.FindObjectsByType<HospitalSiteGrassRenderer>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Combined state requires exactly one grass manager.");
            Require(Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0&&Object.FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==0,"Production scenes duplicated cameras/lights.");
            Require(Object.FindObjectsByType<LODGroup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==99,"Combined state duplicated/lost vegetation.");
        }

        private static void ValidateEvidence(string reviews)
        {
            string[] names={"WholeSiteAerial","GateArrival","PlayerEyeArrival","CirculationTopdown","CrossingsTopdown","RoadPathClose","Parking","RearService","LakeBenchEdge","GrassClose","GrassMedium","GrassAerial","ForcedLOD0","ForcedLOD1","ForcedLOD2"};
            foreach(string name in names)
            {
                string path=Path.Combine(reviews,"Stage05S_RoundS6_Unity_"+name+".png"); Require(File.Exists(path)&&new FileInfo(path).Length>10000,"Missing/empty visual evidence: "+name);
                byte[] header=File.ReadAllBytes(path).Take(24).ToArray(); int width=(header[16]<<24)|(header[17]<<16)|(header[18]<<8)|header[19]; int height=(header[20]<<24)|(header[21]<<16)|(header[22]<<8)|header[23]; Require(width==1600&&height==1200,"Evidence resolution changed: "+name);
            }
        }

        private static void ValidatePerformanceRecord(string reviews)
        {
            string path=Path.Combine(reviews,"Stage05S_RoundS6_PerformanceProxy.json"); Require(File.Exists(path),"Missing performance proxy."); string json=File.ReadAllText(path);
            Require(json.Contains("\"status\": \"PASS\"")&&json.Contains("PROXY_ONLY")&&json.Contains("\"active_grass_triangles\"")&&json.Contains("\"steady_state_gc_bytes\": 0"),"Performance structure or proxy label failed.");
        }

        private static void ValidateRegressions(string workspace)
        {
            foreach(string path in new[]{"Reviews/HospitalExterior/Stage05S_RoundS5B/Stage05S_RoundS5B_Validation.json","Reviews/HospitalExterior/Stage05S_RoundS5/Stage05S_RoundS5_Validation.json"})
            { string json=File.ReadAllText(Path.Combine(workspace,path)); Require(json.Contains("\"status\": \"PASS\""),"Regression no longer PASS: "+path); }
        }

        private static void ValidateRecords(string reviews)
        {
            string[] files={"Approval","InputHashes","ImportSettings","MaterialMap","LODInventory","CollisionInventory","SceneLoadMatrix","GateRoots","Traversal","GrassOccupancyAudit","PerformanceProxy","SourceChangeLog"};
            foreach(string file in files) Require(File.Exists(Path.Combine(reviews,"Stage05S_RoundS6_"+file+".json")),"Missing S6 record: "+file);
            string userApproval=Path.Combine(reviews,"Stage05S_RoundS6_UserApproval.json");
            Require(File.Exists(userApproval)&&File.ReadAllText(userApproval).Contains("S6 APPROVED / STAGE 5S COMPLETE"),"Missing or invalid S6 user approval record.");
            Require(File.Exists(Path.Combine(reviews,"Stage05S_RoundS6_ReviewChecklist.md")),"Missing S6 review checklist.");
            Require(File.Exists(Path.Combine(HospitalStage05SRoundS6Builder.WorkspaceRoot(),"Archive/HospitalExterior/Docs/HospitalExterior_Stage05S_RoundS6_Checkpoint.md")),"Missing frozen S6 checkpoint document.");
        }

        private static void WriteManifest(string workspace,string reviews)
        {
            var files=new List<string>();
            files.AddRange(Directory.GetFiles(HospitalStage05SRoundS6Builder.AssetPathToAbsolute(HospitalStage05SRoundS6Builder.Root),"*",SearchOption.AllDirectories).Where(p=>!p.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)));
            files.AddRange(Directory.GetFiles(reviews,"*",SearchOption.TopDirectoryOnly).Where(p=>!p.EndsWith("Stage05S_RoundS6_Manifest.json",StringComparison.OrdinalIgnoreCase)));
            foreach(string relative in new[]{"Archive/HospitalExterior/Docs/HospitalExterior_Stage05S_RoundS6_Checkpoint.md","HOSPITAL_EXTERIOR_PROGRESS.md","HOSPITAL_SITE_EXPANSION_DESIGN_PLAN.md"})
            {
                string path=Path.Combine(workspace,relative.Replace('/',Path.DirectorySeparatorChar)); if(File.Exists(path)) files.Add(path);
            }
            var manifest=new Manifest { schema="HospitalExterior.Stage05S.S6.Manifest.v1",status="PASS",authority_sha256=HospitalStage05SRoundS6Builder.AuthoritySha,files=files.Distinct().OrderBy(x=>x).Select(path=>new ManifestFile{path=Path.GetRelativePath(workspace,path).Replace('\\','/'),sha256=HospitalStage05SRoundS6Builder.Sha256(path),bytes=new FileInfo(path).Length}).ToArray() };
            HospitalStage05SRoundS6Builder.WriteJson(manifest,Path.Combine(reviews,"Stage05S_RoundS6_Manifest.json"));
        }

        private static GameObject P(string zone) { GameObject p=AssetDatabase.LoadAssetAtPath<GameObject>(HospitalStage05SRoundS6Builder.PrefabFolder+"/"+zone+".prefab"); Require(p!=null,"Missing prefab: "+zone); return p; }
        private static bool Approximately(float a,float b)=>Mathf.Abs(a-b)<.0001f;
        private static void Check(List<GateCheck> checks,string name,Action action){try{action();checks.Add(new GateCheck{name=name,passed=true,detail="PASS"});}catch(Exception e){checks.Add(new GateCheck{name=name,passed=false,detail=e.Message});Debug.LogError(name+": "+e);}}
        private static void Require(bool value,string message){if(!value)throw new InvalidDataException(message);}

        [Serializable] private sealed class GateRecord { public string schema,status,authority_sha256,approval,unity_editor,measurement_scope,next_required_action; public bool pass; public int protected_road_path_and_landscape_changes,checks_passed,checks_total; public GateCheck[] checks; }
        [Serializable] private sealed class GateCheck { public string name,detail; public bool passed; }
        [Serializable] private sealed class Manifest { public string schema,status,authority_sha256; public ManifestFile[] files; }
        [Serializable] private sealed class ManifestFile { public string path,sha256; public long bytes; }
    }
}
