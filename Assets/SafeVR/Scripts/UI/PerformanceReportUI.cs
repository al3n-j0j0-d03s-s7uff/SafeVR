using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Scenario;
using SafeVR.Core;

namespace SafeVR.UI
{
    public class PerformanceReportUI : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject reportPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private TMP_Text recommendText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [Header("Optional")]
        [Tooltip("Wired only in Assessment. Left empty in TrainingRoom (behavior unchanged there).")]
        [SerializeField] private Button resultsButton;
        [Header("Phase 4 — Review Sections (wired by builder)")]
        [SerializeField] private TMP_Text breakdownText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private TMP_Text timelineText;
        [SerializeField] private TMP_Text historyText;

        private ScenarioManagerBase scenario;

        private void Awake()
        {
            scenario = FindFirstObjectByType<ScenarioManagerBase>();
            if (reportPanel != null) reportPanel.SetActive(false);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
            if (resultsButton != null)
            {
                resultsButton.onClick.AddListener(OnViewResults);
                resultsButton.gameObject.SetActive(false);
            }
            if (scenario != null) scenario.OnScenarioEnded += HandleEnded;
        }

        private void OnDestroy()
        {
            if (scenario != null) scenario.OnScenarioEnded -= HandleEnded;
        }

        private void HandleEnded(ScenarioMetrics m)
        {
            ShowReport(m);
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            var fps = FindFirstObjectByType<Player.DesktopFPSController>();
            if (fps != null) { fps.CanMove = false; fps.CanLook = false; }
        }

        private void ShowReport(ScenarioMetrics m)
        {
            if (reportPanel == null) return;
            reportPanel.SetActive(true);
            // Results scene opens only after a successful Assessment run.
            if (resultsButton != null) resultsButton.gameObject.SetActive(m.scenarioCompleted);
            if (titleText != null)
            {
                titleText.text = m.scenarioFailed ? "<color=#EF4444>SCENARIO FAILED</color>" : "<color=#22C55E>SCENARIO COMPLETED</color>";
            }
            var b = m.ComputeBreakdown();
            if (scoreText != null) scoreText.text = $"Safety Score: <b>{m.SafetyScore}/100</b>  •  <b>{m.Grade}</b>";
            if (breakdownText != null)
                breakdownText.text = $"Procedure {b.procedure:F0} • Equipment {b.equipment:F0} • Suppression {b.suppression:F0} • Evacuation {b.evacuation:F0} • Time {b.time:F0} • Unsafe −{b.unsafePenalty:F0}";
            if (timeText != null) timeText.text = $"Time: {m.elapsedTime:F1}s   •   Correct: {m.correctActions}/{m.totalRequiredActions}   •   Unsafe: {m.unsafeActions}";
            if (objectivesText != null)
            {
                string o = "<b>Objectives:</b>  ";
                foreach (var item in PerformanceReview.BuildObjectives(m))
                    o += item.done ? $"<color=#22C55E>✓ {item.label}</color>   " : $"<color=#EF4444>✗ {item.label}</color>   ";
                objectivesText.text = o;
            }
            if (detailText != null)
            {
                var unsafeItems = PerformanceReview.BuildUnsafeReview(m.actionLog);
                string log = $"<b>Safety ({unsafeItems.Count} unsafe):</b>";
                int shown = 0;
                foreach (var u in unsafeItems)
                {
                    if (shown >= 3) break;
                    log += $"\n• {u.time} — {u.action}";
                    shown++;
                }
                if (unsafeItems.Count == 0) log += "\n• No unsafe actions.";
                else if (unsafeItems.Count > 3) log += $"\n• …and {unsafeItems.Count - 3} more (see Results).";
                if (m.fireMaxIntensityReached > 0f) log += $"\nMax Fire Intensity: {m.fireMaxIntensityReached:F0}%";
                detailText.text = log;
            }
            if (timelineText != null)
            {
                var timeline = PerformanceReview.BuildTimeline(m.actionLog);
                string t = "<b>Timeline:</b>  ";
                int shown = 0;
                foreach (var item in timeline)
                {
                    if (shown >= 6) break;
                    t += $"{item.time} {item.text}   •   ";
                    shown++;
                }
                timelineText.text = t.TrimEnd(' ', '•');
            }
            if (recommendText != null)
            {
                var recs = m.GetRecommendations();
                string r = "<b>Recommendations:</b>\n";
                for (int i = 0; i < recs.Count && i < 3; i++) r += $"• {recs[i]}\n";
                recommendText.text = r;
            }
            if (historyText != null) historyText.text = FormatHistory(AttemptHistory.KeyForMetrics(m));
        }

        internal static string FormatHistory(string scenarioKey)
        {
            var last = AttemptHistory.GetLast(scenarioKey);
            var prev = AttemptHistory.GetPrevious(scenarioKey);
            var best = AttemptHistory.GetBest(scenarioKey);
            var imp = AttemptHistory.GetImprovement(scenarioKey);
            string cur = last != null ? last.score.ToString() : "—";
            string prv = prev != null ? prev.score.ToString() : "—";
            string bst = best != null ? best.score.ToString() : "—";
            string delta = imp.HasValue ? (imp.Value >= 0 ? $"+{imp.Value}" : imp.Value.ToString()) : "—";
            return $"History — Previous: <b>{prv}</b>  •  Current: <b>{cur}</b>  •  Best: <b>{bst}</b>  •  Improvement: <b>{delta}</b>";
        }

        private void OnRetry()
        {
            Core.SoundEffectManager.Instance?.PlayClick();
            if (scenario != null) ScenarioResetHelper.ResetMetrics();
            SceneFlowManager.Instance?.ReloadCurrentScene();
        }

        private void OnMenu()
        {
            Core.SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadMainMenu();
        }

        private void OnViewResults()
        {
            Core.SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadResults();
        }
    }

    public static class ScenarioResetHelper
    {
        public static void ResetMetrics()
        {
            TrainingProgress.ResetProgress();
        }
    }
}
