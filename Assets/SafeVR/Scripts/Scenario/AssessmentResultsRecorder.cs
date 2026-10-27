using UnityEngine;
using SafeVR.Core;

namespace SafeVR.Scenario
{
    /// <summary>
    /// Assessment scene only. Subscribes to the live EvacuationScenarioManager and
    /// snapshots its real ScenarioMetrics into AssessmentResultsStore when the run ends
    /// (success or failure). No changes to existing managers; TrainingRoom untouched.
    /// </summary>
    public class AssessmentResultsRecorder : MonoBehaviour
    {
        private EvacuationScenarioManager scenario;

        private void Awake()
        {
            scenario = FindFirstObjectByType<EvacuationScenarioManager>();
            if (scenario == null) scenario = FindAnyObjectByType<EvacuationScenarioManager>();
            if (scenario != null) scenario.OnScenarioEnded += HandleEnded;
        }

        private void OnDestroy()
        {
            if (scenario != null) scenario.OnScenarioEnded -= HandleEnded;
        }

        private void HandleEnded(ScenarioMetrics m)
        {
            AssessmentResultsStore.Save(m, "Evacuation Assessment");
            AttemptHistory.Record(AttemptHistory.KeyForMetrics(m), m);
        }
    }
}
