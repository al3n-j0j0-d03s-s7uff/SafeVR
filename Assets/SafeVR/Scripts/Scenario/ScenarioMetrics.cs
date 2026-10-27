using System;
using System.Collections.Generic;
using UnityEngine;
using SafeVR.Core;

namespace SafeVR.Scenario
{
    [Serializable]
    public class ScenarioMetrics
    {
        public ScenarioType scenarioType;
        public float elapsedTime;
        public int correctActions;
        public int unsafeActions;
        public int totalRequiredActions;
        public bool powerIsolated;
        public bool extinguisherUsedCorrectly;
        public bool evacuatedSafely;
        public bool scenarioCompleted;
        public bool scenarioFailed;
        public bool alarmActivated;
        public float fireMaxIntensityReached;

        [Header("Phase 4 — Review Tracking")]
        public bool pinRemoved;
        public bool exitOpened;
        public bool correctExitUsed;
        public bool hazardIdentified;
        public float powerIsolationTime = -1f;
        public float suppressionTime = -1f;

        public List<string> actionLog = new List<string>();

        /// <summary>
        /// Phase 4 — unified 0–100 Safety Score (see <see cref="SafeVR.Core.SafetyScoreConfig"/>).
        /// Positive components (procedure / equipment / suppression / evacuation / time)
        /// are normalised to 100, then the unsafe-action penalty applies. Never negative.
        /// </summary>
        public float SafetyScore => Mathf.Round(ComputeBreakdown().total);

        public ScoreBreakdown ComputeBreakdown()
        {
            var b = new ScoreBreakdown();
            bool isFire = scenarioType == ScenarioType.ElectricalFire;

            // Correct procedure (25): share of required actions completed.
            b.procedure = totalRequiredActions > 0
                ? (correctActions / (float)totalRequiredActions) * SafetyScoreConfig.WeightProcedure
                : 0f;

            // Equipment selection (15).
            if (isFire)
                b.equipment = (extinguisherUsedCorrectly ? 10f : 0f) + (pinRemoved ? 5f : 0f);
            else
                b.equipment = (correctExitUsed ? 10f : 0f) + (alarmActivated ? 5f : 0f);

            // Suppression / hazard response (20).
            if (isFire)
            {
                if (extinguisherUsedCorrectly)
                {
                    float t = suppressionTime >= 0f ? suppressionTime : elapsedTime;
                    if (t <= SafetyScoreConfig.SuppressFastTime) b.suppression = SafetyScoreConfig.WeightSuppression;
                    else if (t >= SafetyScoreConfig.SuppressSlowTime) b.suppression = SafetyScoreConfig.SuppressFloor;
                    else
                    {
                        float f = (t - SafetyScoreConfig.SuppressFastTime)
                            / (SafetyScoreConfig.SuppressSlowTime - SafetyScoreConfig.SuppressFastTime);
                        b.suppression = Mathf.Lerp(SafetyScoreConfig.WeightSuppression, SafetyScoreConfig.SuppressFloor, f);
                    }
                }
                else b.suppression = 0f;
            }
            else
            {
                b.suppression = (alarmActivated ? 12f : 0f) + (hazardIdentified ? 8f : 0f);
            }

            // Evacuation (15).
            b.evacuation = evacuatedSafely ? SafetyScoreConfig.WeightEvacuation : 0f;

            // Time / performance (5): full marks at/under par, zero at 2× par.
            float par = isFire ? SafetyScoreConfig.ParTimeElectricalFire : SafetyScoreConfig.ParTimeEvacuation;
            if (elapsedTime <= par) b.time = SafetyScoreConfig.WeightTime;
            else if (elapsedTime >= par * 2f) b.time = 0f;
            else b.time = SafetyScoreConfig.WeightTime * (1f - (elapsedTime - par) / par);

            float positive = (b.procedure + b.equipment + b.suppression + b.evacuation + b.time)
                * SafetyScoreConfig.PositiveNormaliser;
            b.unsafePenalty = Mathf.Min(SafetyScoreConfig.UnsafePenaltyMax,
                unsafeActions * SafetyScoreConfig.UnsafePenaltyEach);
            float total = Mathf.Clamp(positive - b.unsafePenalty, 0f, 100f);
            if (scenarioFailed) total = Mathf.Min(total, SafetyScoreConfig.FailedScoreCap);
            b.total = total;
            return b;
        }

        public string Grade
        {
            get
            {
                float s = SafetyScore;
                if (s >= SafetyScoreConfig.ExcellentAt) return "EXCELLENT";
                if (s >= SafetyScoreConfig.GoodAt) return "GOOD";
                if (s >= SafetyScoreConfig.SatisfactoryAt) return "SATISFACTORY";
                if (s >= SafetyScoreConfig.NeedsImprovementAt) return "NEEDS IMPROVEMENT";
                return "UNSAFE";
            }
        }

        /// <summary>
        /// Phase 4 — recommendations driven only by actual mistakes in this run.
        /// A clean run yields exactly the perfect-run message.
        /// </summary>
        public List<string> GetRecommendations()
        {
            var recs = new List<string>();
            bool isFire = scenarioType == ScenarioType.ElectricalFire;
            string log = actionLog != null ? string.Join("\n", actionLog).ToLowerInvariant() : "";

            if (isFire && !powerIsolated)
                recs.Add("Always isolate power before tackling an electrical fire – never use water.");
            if (log.Contains("without isolating power"))
                recs.Add("Remember to isolate electrical power before attempting suppression.");
            if (log.Contains("wrong extinguisher"))
                recs.Add("Review extinguisher selection for different fire types.");
            if (isFire && !extinguisherUsedCorrectly)
                recs.Add("Use P.A.S.S. technique: Pull, Aim (base), Squeeze, Sweep. Hold aim on fire base.");
            if (log.Contains("safety pin"))
                recs.Add("Pull the safety pin only once aimed, then squeeze and sweep.");
            if (log.Contains("wasting") || log.Contains("aim at the fire base"))
                recs.Add("Improve aim and sweep technique to use discharge efficiently.");
            if (log.Contains("empty extinguisher"))
                recs.Add("Watch the charge gauge and avoid spraying away from the fire.");
            if (log.Contains("blocked exit"))
                recs.Add("Improve evacuation response and route recognition.");
            if (log.Contains("before fire was controlled") || log.Contains("without controlling fire"))
                recs.Add("Control the fire fully before evacuating, then move without delay.");
            if (unsafeActions > 0 && recs.Count == 0)
                recs.Add("Avoid unsafe actions: blocked exits, wrong equipment, or delaying evacuation.");
            float par = isFire ? SafetyScoreConfig.ParTimeElectricalFire : SafetyScoreConfig.ParTimeEvacuation;
            if (elapsedTime > par)
                recs.Add("Improve evacuation response and route recognition.");
            if (recs.Count == 0)
                recs.Add("Excellent performance. All required safety procedures were followed.");
            return recs;
        }

        [Serializable]
        public struct ScoreBreakdown
        {
            public float procedure;
            public float equipment;
            public float suppression;
            public float evacuation;
            public float time;
            public float unsafePenalty;
            public float total;
        }
    }
}
