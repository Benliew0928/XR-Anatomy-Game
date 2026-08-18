using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CutMyBodyPlease.HospitalInterior.PreProduction.I1.R03.Editor
{
    [InitializeOnLoad]
    public static class HospitalInteriorS2R03AutoRunner
    {
        private const string RequestAsset = HospitalInteriorS2R03Builder.Root + "/S2_AUTORUN.request";

        [Serializable]
        private sealed class AutoRunResult
        {
            public string schema = "HospitalInterior.R03.S2.AutoRun.v1";
            public string status;
            public string unityVersion;
            public string[] completedSteps;
            public string error;
        }

        static HospitalInteriorS2R03AutoRunner() => EditorApplication.delayCall += TryRun;

        [MenuItem("Hospital Interior/R03 S2/Run complete automated S2 pipeline", priority = 100)]
        public static void RunCompletePipeline()
        {
            string review = HospitalInteriorS2R03Builder.ReviewFolder();
            Directory.CreateDirectory(review);
            string resultPath = Path.Combine(review, "StageI1_R03_S2_AutoRunComplete.json");
            var steps = new List<string>();
            try
            {
                HospitalInteriorS2R03Builder.BuildStage();
                steps.Add("contract_and_seven_floor_scenes_built");
                HospitalInteriorS2R03Validator.ValidateStaticGate();
                steps.Add("static_gate_passed");
                HospitalInteriorS2R03Builder.BuildWindowsReviewPlayer();
                steps.Add("windows_player_built");
                RunPlayerGate(review);
                steps.Add("runtime_route_gate_passed");
                HospitalInteriorS2R03EvidenceCapture.Capture();
                steps.Add("seven_arrival_and_four_integration_correction_views_captured");
                HospitalInteriorS2R03Validator.ValidateStaticGate();
                steps.Add("post_capture_static_regression_passed");
                HospitalInteriorS2R03Builder.WriteCheckpoint("AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW");
                FinalizeBuildRecord();
                File.WriteAllText(resultPath, JsonUtility.ToJson(new AutoRunResult
                {
                    status = "AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW",
                    unityVersion = Application.unityVersion,
                    completedSteps = steps.ToArray(),
                    error = string.Empty,
                }, true));
                Debug.Log("HOSPITAL_INTERIOR_R03_S2_AUTORUN=AUTOMATED_PASS_PENDING_USER_VISUAL_REVIEW");
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
                Debug.LogError("HOSPITAL_INTERIOR_R03_S2_AUTORUN=FAIL; " + resultPath);
            }
        }

        private static void TryRun()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += TryRun;
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(RequestAsset) == null)
                return;
            AssetDatabase.DeleteAsset(RequestAsset);
            AssetDatabase.Refresh();
            RunCompletePipeline();
        }

        private static void RunPlayerGate(string review)
        {
            string executable = Path.Combine(HospitalInteriorS2R03Builder.WorkspaceRoot(), "Exports", "HospitalInterior",
                "StageI1_R03_S2_AutoGate", "HospitalInterior_S2_R03_Review.exe");
            if (!File.Exists(executable))
                throw new FileNotFoundException("S2 Windows player executable is missing.", executable);
            string report = Path.Combine(review, "StageI1_R03_S2_RuntimeGate.json");
            string log = Path.Combine(review, "Unity_S2_R03_RuntimeGate.log");
            var start = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = $"-batchmode -nographics -s2AutoGate -s2ReportPath \"{report}\" -logFile \"{log}\"",
                WorkingDirectory = HospitalInteriorS2R03Builder.WorkspaceRoot(),
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start S2 runtime gate player.");
            if (!process.WaitForExit(180000))
            {
                process.Kill();
                throw new TimeoutException("S2 runtime gate player exceeded 180 seconds.");
            }
            if (process.ExitCode != 0)
                throw new InvalidOperationException("S2 runtime gate player returned exit code " + process.ExitCode + ".");
            if (!File.Exists(report))
                throw new FileNotFoundException("S2 runtime gate report was not written.", report);
            S2RuntimeGateReport gate = JsonUtility.FromJson<S2RuntimeGateReport>(File.ReadAllText(report));
            if (gate == null || gate.status != "PASS" || gate.passCount != gate.totalCount)
                throw new InvalidOperationException("S2 runtime gate report did not pass.");
        }

        private static void FinalizeBuildRecord()
        {
            string path = Path.Combine(HospitalInteriorS2R03Builder.ReviewFolder(), "StageI1_R03_S2_BuildRecord.json");
            if (!File.Exists(path))
                return;
            string content = File.ReadAllText(path)
                .Replace("PLAYER_BUILT_PENDING_RUNTIME_GATE", "PLAYER_BUILT_RUNTIME_GATE_PASS_PENDING_USER_REVIEW");
            File.WriteAllText(path, content);
        }
    }
}
