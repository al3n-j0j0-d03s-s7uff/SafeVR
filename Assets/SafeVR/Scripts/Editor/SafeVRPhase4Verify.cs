using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using SafeVR.Core;
using SafeVR.Scenario;
using SafeVR.UI;

namespace SafeVR.Editor
{
    /// <summary>
    /// Phase 4 verification — Retry + Scoring + Performance Review.
    /// Batchmode-safe (edit-mode): checks wiring, API surface, scoring math on
    /// synthetic metrics, history behavior, and untouched flows. Live walk-through
    /// (pause → retry, perfect/poor runs) requires Play mode.
    /// </summary>
    public static class SafeVRPhase4Verify
    {
        public static void Verify()
        {
            int fails = 0;
            void Check(bool cond, string name)
            {
                Debug.Log((cond ? "[PASS] " : "[FAIL] ") + name);
                if (!cond) fails++;
            }

            // ---- Pause menu in both scenario scenes ----
            foreach (var path in new[] { "Assets/Scenes/TrainingRoom.unity", "Assets/Scenes/Assessment.unity" })
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string sname = System.IO.Path.GetFileNameWithoutExtension(path);
                var pause = Object.FindFirstObjectByType<ScenarioPauseMenu>();
                Check(pause != null, $"{sname}: ScenarioPauseMenu exists");
                if (pause != null)
                {
                    var so = new SerializedObject(pause);
                    foreach (var p in new[] { "pausePanel", "resumeButton", "retryButton", "menuButton", "confirmPanel", "confirmRestartButton", "cancelButton" })
                        Check(so.FindProperty(p).objectReferenceValue != null, $"{sname}: pause {p} wired");
                    var t = typeof(ScenarioPauseMenu);
                    Check(t.GetMethod("ConfirmRetry") != null && t.GetMethod("Resume") != null, $"{sname}: VR-ready pause methods exist");
                }
                Check(FindIncludingInactive("PausePanel") != null, $"{sname}: PausePanel object exists");
                Check(FindIncludingInactive("RetryConfirmPanel") != null, $"{sname}: RetryConfirmPanel exists");
                Check(Object.FindFirstObjectByType<ScenarioAttemptRecorder>() != null
                    || Object.FindFirstObjectByType<AssessmentResultsRecorder>() != null,
                    $"{sname}: attempt recorder exists");
            }
            EditorSceneManager.OpenScene("Assets/Scenes/TrainingRoom.unity", OpenSceneMode.Single);
            var trRec = Object.FindFirstObjectByType<ScenarioAttemptRecorder>();
            Check(trRec != null, "TrainingRoom: ScenarioAttemptRecorder (history) exists");

            // ---- Performance report sections ----
            var report = Object.FindFirstObjectByType<PerformanceReportUI>();
            Check(report != null, "TrainingRoom: PerformanceReportUI exists");
            if (report != null)
            {
                var so = new SerializedObject(report);
                foreach (var p in new[] { "breakdownText", "objectivesText", "timelineText", "historyText" })
                    Check(so.FindProperty(p).objectReferenceValue != null, $"Report: {p} wired");
            }

            // ---- Scoring math: perfect run ----
            var perfect = new ScenarioMetrics
            {
                scenarioType = ScenarioType.ElectricalFire,
                elapsedTime = 60f, correctActions = 5, totalRequiredActions = 5, unsafeActions = 0,
                powerIsolated = true, extinguisherUsedCorrectly = true, pinRemoved = true,
                exitOpened = true, evacuatedSafely = true, scenarioCompleted = true,
                suppressionTime = 40f, fireMaxIntensityReached = 60f
            };
            Check(perfect.SafetyScore >= 90f, $"Perfect run scores high ({perfect.SafetyScore})");
            Check(perfect.Grade == "EXCELLENT", $"Perfect run grade EXCELLENT ({perfect.Grade})");
            var perfectRecs = perfect.GetRecommendations();
            Check(perfectRecs.Count == 1 && perfectRecs[0].Contains("Excellent performance"),
                "Perfect run yields only the perfect-run recommendation");

            // ---- Scoring math: poor run ----
            var poor = new ScenarioMetrics
            {
                scenarioType = ScenarioType.ElectricalFire,
                elapsedTime = 100f, correctActions = 1, totalRequiredActions = 5, unsafeActions = 4,
                scenarioFailed = true, fireMaxIntensityReached = 95f,
                actionLog = new System.Collections.Generic.List<string>
                {
                    "[!][19.0s] Discharged without isolating power — ineffective",
                    "[!][30.0s] Selected wrong extinguisher (Water Extinguisher on electrical fire — use CO2 Extinguisher)",
                    "[!][50.0s] Wasting extinguisher charge — aim at the fire base and sweep"
                }
            };
            Check(poor.SafetyScore < 60f, $"Poor run scores low ({poor.SafetyScore})");
            Check(poor.SafetyScore >= 0f, "Score never negative");
            var poorRecs = string.Join("|", poor.GetRecommendations());
            Check(poorRecs.Contains("isolate electrical power"), "Poor run: isolation recommendation");
            Check(poorRecs.Contains("extinguisher selection"), "Poor run: selection recommendation");
            Check(poorRecs.Contains("aim and sweep"), "Poor run: technique recommendation");

            // ---- Slow-but-correct sweep is not unsafe by itself ----
            var slow = new ScenarioMetrics
            {
                scenarioType = ScenarioType.ElectricalFire,
                elapsedTime = 80f, correctActions = 5, totalRequiredActions = 5, unsafeActions = 0,
                powerIsolated = true, extinguisherUsedCorrectly = true, pinRemoved = true,
                exitOpened = true, evacuatedSafely = true, scenarioCompleted = true,
                suppressionTime = 70f, fireMaxIntensityReached = 90f
            };
            Check(slow.SafetyScore >= 75f, $"Slow-but-correct run still GOOD+ ({slow.SafetyScore}, {slow.Grade})");

            // ---- Objectives / timeline / unsafe review from real data ----
            var objectives = PerformanceReview.BuildObjectives(perfect);
            Check(objectives.Count == 6 && objectives.TrueForAll(o => o.done), "Objectives: 6/6 from real flags (fire)");
            var evac = new ScenarioMetrics { scenarioType = ScenarioType.Evacuation };
            Check(PerformanceReview.BuildObjectives(evac).Count == 5, "Objectives: 5 mapped items (evacuation)");
            var timeline = PerformanceReview.BuildTimeline(poor.actionLog);
            Check(timeline.Count == 4 && timeline[0].time == "00:00", "Timeline: parsed real timestamps (+ run origin)");
            var review = PerformanceReview.BuildUnsafeReview(poor.actionLog);
            Check(review.Count == 3, "Unsafe review: one entry per logged mistake");
            Check(review[0].reason.Contains("energized") && review[0].recommendation.Contains("Isolate"),
                "Unsafe review: reason + fix for power mistake");

            // ---- History (PlayerPrefs, isolated test keys via production API) ----
            string hkey = AttemptHistory.KeyForMetrics(perfect);
            Check(hkey == "ElectricalFire", "History key derives from scenario type");
            // Exercise record/read on a scratch key without touching real history.
            var before = AttemptHistory.GetHistory("__Phase4Test__").Count;
            var rec = new ScenarioMetrics
            {
                scenarioType = ScenarioType.ElectricalFire, elapsedTime = 10f,
                correctActions = 1, totalRequiredActions = 5
            };
            AttemptHistory.Record("__Phase4Test__", rec);
            AttemptHistory.Record("__Phase4Test__", perfect);
            var hist = AttemptHistory.GetHistory("__Phase4Test__");
            Check(hist.Count == before + 2, "History: attempts appended locally");
            Check(AttemptHistory.GetBest("__Phase4Test__")?.score == (int)perfect.SafetyScore, "History: best score tracked");
            Check(AttemptHistory.GetPrevious("__Phase4Test__") != null, "History: previous tracked after 2 attempts");
            Check(AttemptHistory.GetImprovement("__Phase4Test__").HasValue, "History: improvement computed");
            Check(AttemptHistory.GetImprovement("__NoSuchKey__") == null, "History: first attempt handled (no previous)");

            // ---- Results scene sections ----
            EditorSceneManager.OpenScene("Assets/Scenes/Results.unity", OpenSceneMode.Single);
            var results = Object.FindFirstObjectByType<ResultsUI>();
            Check(results != null, "Results: ResultsUI exists");
            if (results != null)
            {
                var so = new SerializedObject(results);
                foreach (var p in new[] { "titleText", "statusText", "scoreText", "breakdownText", "timeText",
                    "objectivesText", "mistakesText", "alarmText", "exitText", "safeZoneText",
                    "timelineText", "recommendationsText", "historyText", "retryButton", "menuButton" })
                    Check(so.FindProperty(p).objectReferenceValue != null, $"Results: {p} wired");
            }

            // ---- Flow intact ----
            var buildScenes = EditorBuildSettings.scenes;
            Check(buildScenes.Length == 5, $"Build Settings: 5 scenes (found {buildScenes.Length})");
            string flowSrc = System.IO.File.ReadAllText("Assets/SafeVR/Scripts/Core/SceneFlowManager.cs");
            Check(flowSrc.Contains("ReloadCurrentScene"), "SceneFlowManager: reload path preserved");

            Debug.Log(fails == 0 ? "[SafeVRPhase4Verify] ALL CHECKS PASSED" : $"[SafeVRPhase4Verify] {fails} CHECKS FAILED");
            if (fails > 0) EditorApplication.Exit(1);
        }

        private static GameObject FindIncludingInactive(string name)
        {
            var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
