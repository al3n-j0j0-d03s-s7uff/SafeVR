using UnityEngine;
using SafeVR.Core;

namespace SafeVR.Scenario
{
    /// <summary>
    /// Phase 4 — generic per-scene attempt recorder. Subscribes to the live
    /// <see cref="ScenarioManagerBase"/> and, when the run ends, appends the real
    /// metrics to the local <see cref="AttemptHistory"/> (and optionally to the
    /// cross-scene <see cref="AssessmentResultsStore"/>). Retries never delete history.
    /// </summary>
    public class ScenarioAttemptRecorder : MonoBehaviour
    {
        [Tooltip("History key, e.g. ElectricalFire or Evacuation. Defaults to the live scenario type.")]
        [SerializeField] private string scenarioKey = "";
        [Tooltip("Also snapshot into AssessmentResultsStore (Results scene). Only one scene should enable this.")]
        [SerializeField] private bool saveToStore = false;
        [SerializeField] private string storeScenarioName = "";

        private ScenarioManagerBase scenario;

        private void Awake()
        {
            scenario = FindFirstObjectByType<ScenarioManagerBase>();
            if (scenario != null) scenario.OnScenarioEnded += HandleEnded;
        }

        private void OnDestroy()
        {
            if (scenario != null) scenario.OnScenarioEnded -= HandleEnded;
        }

        private void HandleEnded(ScenarioMetrics m)
        {
            string key = string.IsNullOrEmpty(scenarioKey) ? AttemptHistory.KeyForMetrics(m) : scenarioKey;
            AttemptHistory.Record(key, m);
            if (saveToStore) AssessmentResultsStore.Save(m, storeScenarioName);
        }
    }
}
