using UnityEngine;

namespace SafeVR.Core
{
    /// <summary>
    /// Holds session-level progress and metrics across scene changes.
    /// </summary>
    public static class TrainingProgress
    {
        public static bool TutorialCompleted { get; set; } = false;
        public static bool TrainingCompleted { get; set; } = false;
        public static int ExtinguishersInspected { get; set; } = 0;
        public static bool ElectricalHazardMitigated { get; set; } = false;
        public static bool EmergencyExitLocated { get; set; } = false;

        public static int LastAssessmentScore { get; set; } = 0;
        public static int LastAssessmentTotal { get; set; } = 3;
        public static bool LastAssessmentPassed { get; set; } = false;

        public static void ResetProgress()
        {
            TutorialCompleted = false;
            TrainingCompleted = false;
            ExtinguishersInspected = 0;
            ElectricalHazardMitigated = false;
            EmergencyExitLocated = false;
            LastAssessmentScore = 0;
            LastAssessmentPassed = false;
        }
    }
}
