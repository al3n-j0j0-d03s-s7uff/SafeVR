using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Core;
using SafeVR.Scenario;

namespace SafeVR.UI
{
    /// <summary>
    /// Phase 4 — Results scene screen with full performance review sections:
    /// PERFORMANCE (score, grade, breakdown, time), OBJECTIVES, SAFETY (unsafe
    /// review), TIMELINE, RECOMMENDATIONS, HISTORY. Every value derives from the
    /// stored run (AssessmentResultsStore snapshot of the real ScenarioMetrics);
    /// history comes from AttemptHistory. Retry starts a fresh attempt of the
    /// stored scenario; Menu returns to MainMenu.
    /// </summary>
    public class ResultsUI : MonoBehaviour
    {
        [Header("Refs (wired by builder)")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text objectivesText;
        [SerializeField] private TMP_Text mistakesText;
        [SerializeField] private TMP_Text alarmText;
        [SerializeField] private TMP_Text exitText;
        [SerializeField] private TMP_Text safeZoneText;
        [SerializeField] private TMP_Text recommendationsText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button menuButton;
        [Header("Phase 4 — Review Sections (wired by builder)")]
        [SerializeField] private TMP_Text breakdownText;
        [SerializeField] private TMP_Text timelineText;
        [SerializeField] private TMP_Text historyText;

        private void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (menuButton != null) menuButton.onClick.AddListener(OnMenu);
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            FillFromStore();
        }

        private void FillFromStore()
        {
            if (!AssessmentResultsStore.HasResults)
            {
                if (titleText != null) titleText.text = "<color=#FACC08>NO ASSESSMENT DATA</color>";
                if (statusText != null) statusText.text = "Complete the Assessment scenario first — this screen shows your last finished run.";
                SetAll("", "", "", "", "", "", "", "", "", "", "");
                return;
            }

            if (titleText != null)
                titleText.text = AssessmentResultsStore.Completed
                    ? "<color=#22C55E>ASSESSMENT COMPLETE</color>"
                    : "<color=#EF4444>ASSESSMENT FAILED</color>";
            if (statusText != null)
                statusText.text = $"{AssessmentResultsStore.ScenarioName}  •  {(AssessmentResultsStore.Completed ? "Completed" : "Failed")}";
            if (scoreText != null)
                scoreText.text = $"Safety Score: <b>{AssessmentResultsStore.Score}/100</b>  •  <b>{AssessmentResultsStore.Grade}</b>";
            if (breakdownText != null)
                breakdownText.text = string.IsNullOrEmpty(AssessmentResultsStore.Breakdown)
                    ? "" : $"Breakdown — {AssessmentResultsStore.Breakdown}";
            if (timeText != null)
                timeText.text = $"Completion Time: <b>{AssessmentResultsStore.ElapsedTime:F1}s</b>  •  Correct: <b>{AssessmentResultsStore.CorrectActions}/{AssessmentResultsStore.TotalRequiredActions}</b>  •  Unsafe: <b>{AssessmentResultsStore.UnsafeActions}</b>";

            var metrics = RebuildMetrics();
            if (objectivesText != null)
            {
                string o = "<b>OBJECTIVES</b>\n";
                foreach (var item in PerformanceReview.BuildObjectives(metrics))
                    o += item.done ? $"<color=#22C55E>✓ {item.label}</color>\n" : $"<color=#EF4444>✗ {item.label}</color>\n";
                objectivesText.text = o.TrimEnd('\n');
            }
            if (mistakesText != null)
            {
                var review = PerformanceReview.BuildUnsafeReview(AssessmentResultsStore.ActionLog);
                string m = $"<b>SAFETY — {review.Count} unsafe action(s)</b>";
                foreach (var u in review)
                    m += $"\n• {u.time} — {u.action}\n  Reason: {u.reason}\n  Fix: {u.recommendation}";
                if (review.Count == 0) m += "\n• No unsafe actions.";
                mistakesText.text = m;
            }
            if (alarmText != null)
                alarmText.text = $"Alarm Activated: <b>{(AssessmentResultsStore.AlarmActivated ? "YES" : "NO")}</b>";
            if (exitText != null)
                exitText.text = $"Correct Exit Used: <b>{(AssessmentResultsStore.CorrectExitUsed ? "YES" : "NO")}</b>";
            if (safeZoneText != null)
                safeZoneText.text = $"Safe Zone Reached: <b>{(AssessmentResultsStore.ReachedSafeZone ? "YES" : "NO")}</b>";
            if (timelineText != null)
            {
                var timeline = PerformanceReview.BuildTimeline(AssessmentResultsStore.ActionLog);
                string t = "<b>TIMELINE</b>\n";
                int shown = 0;
                foreach (var item in timeline)
                {
                    if (shown >= 14) break;
                    t += item.isUnsafe
                        ? $"<color=#EF4444>{item.time} — {item.text}</color>\n"
                        : $"{item.time} — {item.text}\n";
                    shown++;
                }
                timelineText.text = t.TrimEnd('\n');
            }
            if (recommendationsText != null)
            {
                string r = "<b>RECOMMENDATIONS</b>";
                foreach (var rec in AssessmentResultsStore.Recommendations) r += $"\n• {rec}";
                recommendationsText.text = r;
            }
            if (historyText != null)
            {
                string key = string.IsNullOrEmpty(AssessmentResultsStore.ScenarioKey)
                    ? ScenarioType.Evacuation.ToString() : AssessmentResultsStore.ScenarioKey;
                historyText.text = PerformanceReportUI.FormatHistory(key);
            }
        }

        /// <summary>Reconstruct review inputs from the stored snapshot (no hard-coded results).</summary>
        private static ScenarioMetrics RebuildMetrics()
        {
            var m = new ScenarioMetrics();
            string key = AssessmentResultsStore.ScenarioKey;
            m.scenarioType = key == ScenarioType.ElectricalFire.ToString()
                ? ScenarioType.ElectricalFire : ScenarioType.Evacuation;
            m.elapsedTime = AssessmentResultsStore.ElapsedTime;
            m.correctActions = AssessmentResultsStore.CorrectActions;
            m.totalRequiredActions = AssessmentResultsStore.TotalRequiredActions;
            m.unsafeActions = AssessmentResultsStore.UnsafeActions;
            m.scenarioCompleted = AssessmentResultsStore.Completed;
            m.scenarioFailed = !AssessmentResultsStore.Completed;
            m.powerIsolated = AssessmentResultsStore.PowerIsolated;
            m.extinguisherUsedCorrectly = AssessmentResultsStore.ExtinguisherUsedCorrectly;
            m.pinRemoved = AssessmentResultsStore.PinRemoved;
            m.exitOpened = AssessmentResultsStore.ExitOpened;
            m.hazardIdentified = AssessmentResultsStore.HazardIdentified;
            m.correctExitUsed = AssessmentResultsStore.CorrectExitUsed;
            m.evacuatedSafely = AssessmentResultsStore.ReachedSafeZone;
            m.alarmActivated = AssessmentResultsStore.AlarmActivated;
            m.actionLog = new System.Collections.Generic.List<string>(AssessmentResultsStore.ActionLog);
            return m;
        }

        private void SetAll(string a, string b, string c, string d, string e, string f, string g, string h, string i, string j, string k)
        {
            if (scoreText != null) scoreText.text = a;
            if (breakdownText != null) breakdownText.text = b;
            if (timeText != null) timeText.text = c;
            if (objectivesText != null) objectivesText.text = d;
            if (mistakesText != null) mistakesText.text = e;
            if (alarmText != null) alarmText.text = f;
            if (exitText != null) exitText.text = g;
            if (safeZoneText != null) safeZoneText.text = h;
            if (timelineText != null) timelineText.text = i;
            if (recommendationsText != null) recommendationsText.text = j;
            if (historyText != null) historyText.text = k;
        }

        private void OnRetry()
        {
            SoundEffectManager.Instance?.PlayClick();
            // Fresh attempt of the stored scenario (default: Assessment).
            if (AssessmentResultsStore.ScenarioKey == ScenarioType.ElectricalFire.ToString())
                SceneFlowManager.Instance?.LoadTrainingRoom();
            else
                SceneFlowManager.Instance?.LoadAssessment();
        }

        private void OnMenu()
        {
            SoundEffectManager.Instance?.PlayClick();
            SceneFlowManager.Instance?.LoadMainMenu();
        }
    }
}
