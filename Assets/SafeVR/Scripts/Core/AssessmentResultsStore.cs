using System.Collections.Generic;
using SafeVR.Scenario;

namespace SafeVR.Core
{
    /// <summary>
    /// Static cross-scene carrier for the last finished Assessment run.
    /// Written by AssessmentResultsRecorder (Assessment scene only) from the live
    /// EvacuationScenarioManager metrics; read by ResultsUI in the Results scene.
    /// No fake scoring — every value is copied from the real ScenarioMetrics.
    /// </summary>
    public static class AssessmentResultsStore
    {
        public static bool HasResults { get; private set; } = false;
        public static string ScenarioName { get; private set; } = "";
        public static bool Completed { get; private set; } = false;
        public static int Score { get; private set; } = 0;
        public static string Grade { get; private set; } = "-";
        public static float ElapsedTime { get; private set; } = 0f;
        public static int CorrectActions { get; private set; } = 0;
        public static int TotalRequiredActions { get; private set; } = 0;
        public static int UnsafeActions { get; private set; } = 0;
        public static bool AlarmActivated { get; private set; } = false;
        public static bool CorrectExitUsed { get; private set; } = false;
        public static bool ReachedSafeZone { get; private set; } = false;
        public static List<string> ActionLog { get; private set; } = new List<string>();
        public static List<string> Recommendations { get; private set; } = new List<string>();
        // Phase 4 — review fields (additive; every value copied from the real metrics).
        public static string ScenarioKey { get; private set; } = "";
        public static bool PinRemoved { get; private set; } = false;
        public static bool ExitOpened { get; private set; } = false;
        public static bool HazardIdentified { get; private set; } = false;
        public static bool PowerIsolated { get; private set; } = false;
        public static bool ExtinguisherUsedCorrectly { get; private set; } = false;
        public static string Breakdown { get; private set; } = "";

        public static void Save(ScenarioMetrics m, string scenarioName)
        {
            if (m == null) return;
            HasResults = true;
            ScenarioName = scenarioName;
            ScenarioKey = AttemptHistory.KeyForMetrics(m);
            Completed = m.scenarioCompleted;
            Score = (int)m.SafetyScore;
            Grade = m.Grade;
            ElapsedTime = m.elapsedTime;
            CorrectActions = m.correctActions;
            TotalRequiredActions = m.totalRequiredActions;
            UnsafeActions = m.unsafeActions;
            AlarmActivated = m.alarmActivated;
            CorrectExitUsed = m.correctExitUsed || m.evacuatedSafely;
            ReachedSafeZone = m.evacuatedSafely;
            ActionLog = new List<string>(m.actionLog);
            Recommendations = new List<string>(m.GetRecommendations());
            PinRemoved = m.pinRemoved;
            ExitOpened = m.exitOpened;
            HazardIdentified = m.hazardIdentified;
            PowerIsolated = m.powerIsolated;
            ExtinguisherUsedCorrectly = m.extinguisherUsedCorrectly;
            var b = m.ComputeBreakdown();
            Breakdown = $"Procedure {b.procedure:F0} • Equipment {b.equipment:F0} • "
                + $"Suppression {b.suppression:F0} • Evacuation {b.evacuation:F0} • "
                + $"Time {b.time:F0} • Unsafe −{b.unsafePenalty:F0}";
        }

        public static void Clear()
        {
            HasResults = false;
            ScenarioName = "";
            Completed = false;
            Score = 0;
            Grade = "-";
            ElapsedTime = 0f;
            CorrectActions = 0;
            TotalRequiredActions = 0;
            UnsafeActions = 0;
            AlarmActivated = false;
            CorrectExitUsed = false;
            ReachedSafeZone = false;
            ActionLog = new List<string>();
            Recommendations = new List<string>();
            ScenarioKey = "";
            PinRemoved = false;
            ExitOpened = false;
            HazardIdentified = false;
            PowerIsolated = false;
            ExtinguisherUsedCorrectly = false;
            Breakdown = "";
        }
    }
}
