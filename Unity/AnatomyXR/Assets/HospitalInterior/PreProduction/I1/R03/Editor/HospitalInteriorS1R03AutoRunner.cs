using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    [InitializeOnLoad]
    public static class HospitalInteriorS1R03AutoRunner
    {
        private const string RequestAsset = HospitalInteriorS1R03Builder.Root + "/S1_AUTORUN.request";
        private const string ReviewRefreshRequestAsset = HospitalInteriorS1R03Builder.Root + "/S1_REVIEW_REFRESH.request";

        [Serializable]
        private sealed class AutoRunResult
        {
            public string schema = "HospitalInterior.R03.S1.AutoRun.v1";
            public string status;
            public string unityVersion;
            public string[] completedSteps;
            public string error;
        }

        static HospitalInteriorS1R03AutoRunner()
        {
            EditorApplication.delayCall += TryRun;
        }

        [MenuItem("Hospital Interior/R03 S1/Run complete automated S1 pipeline", priority = 100)]
        public static void RunCompletePipeline()
        {
            string review = HospitalInteriorS1R03Builder.ReviewFolder();
            Directory.CreateDirectory(review);
            string resultPath = Path.Combine(review, "StageI1_R03_S1_AutoRunComplete.json");
            var steps = new System.Collections.Generic.List<string>();
            try
            {
                HospitalInteriorS1R03Builder.BuildIntegrationScene();
                steps.Add("integration_scene_built");
                HospitalInteriorS1R03Validator.ValidateStaticGate();
                steps.Add("static_gate_passed");
                HospitalInteriorS1R03Builder.BuildWindowsReviewPlayer();
                steps.Add("windows_player_built");
                RunPlayerGate(review);
                steps.Add("runtime_gate_passed");
                HospitalInteriorS1R03EvidenceCapture.Capture();
                steps.Add("four_view_evidence_captured");
                HospitalInteriorS1R03Validator.ValidateStaticGate();
                steps.Add("post_build_static_regression_passed");
                FinalizeCheckpoint();
                File.WriteAllText(resultPath, JsonUtility.ToJson(new AutoRunResult
                {
                    status = "AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW",
                    unityVersion = Application.unityVersion,
                    completedSteps = steps.ToArray(),
                    error = string.Empty,
                }, true));
                Debug.Log("HOSPITAL_INTERIOR_R03_S1_AUTORUN=AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW");
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, JsonUtility.ToJson(new AutoRunResult
                {
                    status = "FAIL",
                    unityVersion = Application.unityVersion,
                    completedSteps = steps.ToArray(),
                    error = exception.ToString(),
                }, true));
                Debug.LogException(exception);
                Debug.LogError("HOSPITAL_INTERIOR_R03_S1_AUTORUN=FAIL; " + resultPath);
            }
        }

        private static void TryRun()
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ReviewRefreshRequestAsset) != null)
            {
                AssetDatabase.DeleteAsset(ReviewRefreshRequestAsset);
                AssetDatabase.Refresh();
                RunReviewRefresh();
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RequestAsset) == null)
                return;
            AssetDatabase.DeleteAsset(RequestAsset);
            AssetDatabase.Refresh();
            RunCompletePipeline();
        }

        private static void RunReviewRefresh()
        {
            Directory.CreateDirectory(HospitalInteriorS1R03Builder.ReviewFolder());
            string resultPath = Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(),
                "StageI1_R03_S1_ReviewRefresh.json");
            var steps = new System.Collections.Generic.List<string>();
            try
            {
                HospitalInteriorS1R03Validator.ValidateStaticGate();
                steps.Add("enhanced_static_report_passed");
                HospitalInteriorS1R03EvidenceCapture.Capture();
                steps.Add("complete_four_view_evidence_regenerated");
                HospitalInteriorS1R03Validator.ValidateStaticGate();
                steps.Add("post_capture_static_regression_passed");
                FinalizeCheckpoint();
                File.WriteAllText(resultPath, JsonUtility.ToJson(new AutoRunResult
                {
                    status = "PASS_PENDING_USER_VISUAL_REVIEW",
                    unityVersion = Application.unityVersion,
                    completedSteps = steps.ToArray(),
                    error = string.Empty,
                }, true));
                Debug.Log("HOSPITAL_INTERIOR_R03_S1_REVIEW_REFRESH=PASS_PENDING_USER_VISUAL_REVIEW");
            }
            catch (Exception exception)
            {
                File.WriteAllText(resultPath, JsonUtility.ToJson(new AutoRunResult
                {
                    status = "FAIL",
                    unityVersion = Application.unityVersion,
                    completedSteps = steps.ToArray(),
                    error = exception.ToString(),
                }, true));
                Debug.LogException(exception);
                Debug.LogError("HOSPITAL_INTERIOR_R03_S1_REVIEW_REFRESH=FAIL; " + resultPath);
            }
        }

        public static void ReviewRefreshBatch()
        {
            RunReviewRefresh();
            string resultPath = Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(),
                "StageI1_R03_S1_ReviewRefresh.json");
            bool pass = File.Exists(resultPath)
                && File.ReadAllText(resultPath).Contains("PASS_PENDING_USER_VISUAL_REVIEW", StringComparison.Ordinal);
            EditorApplication.Exit(pass ? 0 : 1);
        }

        private static void RunPlayerGate(string review)
        {
            string executable = Path.Combine(HospitalInteriorS1R03Builder.WorkspaceRoot(), "Exports", "HospitalInterior",
                "StageI1_R03_S1_AutoGate", "HospitalInterior_S1_R03_Review.exe");
            if (!File.Exists(executable))
                throw new FileNotFoundException("S1 Windows player executable is missing.", executable);
            string report = Path.Combine(review, "StageI1_R03_S1_RuntimeGate.json");
            string log = Path.Combine(review, "Unity_S1_R03_RuntimeGate.log");
            var start = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"-batchmode -nographics -s1AutoGate -s1ReportPath \"{report}\" -logFile \"{log}\"",
                WorkingDirectory = HospitalInteriorS1R03Builder.WorkspaceRoot(),
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start S1 runtime gate player.");
            if (!process.WaitForExit(180000))
            {
                process.Kill();
                throw new TimeoutException("S1 runtime gate player exceeded 180 seconds.");
            }
            if (process.ExitCode != 0)
                throw new InvalidOperationException("S1 runtime gate player returned exit code " + process.ExitCode + ".");
            if (!File.Exists(report))
                throw new FileNotFoundException("S1 runtime gate report was not written.", report);
            S1RuntimeGateReport gate = JsonUtility.FromJson<S1RuntimeGateReport>(File.ReadAllText(report));
            if (gate == null || gate.status != "PASS" || gate.passCount != gate.totalCount)
                throw new InvalidOperationException("S1 runtime gate report did not pass.");
        }

        private static void FinalizeCheckpoint()
        {
            string checkpoint = Path.Combine(HospitalInteriorS1R03Builder.ReviewFolder(), "HospitalInterior_StageS1_R03_Checkpoint.md");
            if (!File.Exists(checkpoint))
                return;
            string content = File.ReadAllText(checkpoint)
                .Replace("IMPLEMENTED_PENDING_AUTOMATED_AND_USER_VISUAL_REVIEW", "AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW");
            File.WriteAllText(checkpoint, content);
        }
    }
}
