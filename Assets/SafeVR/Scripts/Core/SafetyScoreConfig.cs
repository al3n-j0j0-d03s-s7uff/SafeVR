namespace SafeVR.Core
{
    /// <summary>
    /// Phase 4 — configurable weights and thresholds for the unified 0–100 Safety Score.
    /// Positive components sum to 80 and are normalised to 100; the unsafe-action
    /// penalty is applied afterwards and the total is clamped (never negative).
    /// </summary>
    public static class SafetyScoreConfig
    {
        // Component weights (max positive sum = 80, normalised ×1.25 to 100).
        public const float WeightProcedure = 25f;
        public const float WeightEquipment = 15f;
        public const float WeightSuppression = 20f;
        public const float WeightEvacuation = 15f;
        public const float WeightTime = 5f;
        public const float PositiveNormaliser = 100f / 80f;

        // Unsafe-action penalty.
        public const float UnsafePenaltyEach = 5f;
        public const float UnsafePenaltyMax = 20f;

        // Failed runs are capped so a fail can never look like a pass.
        public const float FailedScoreCap = 45f;

        // Time pars (seconds) for the time component: full marks at/under par, zero at 2× par.
        public const float ParTimeElectricalFire = 90f;
        public const float ParTimeEvacuation = 60f;

        // Suppression speed (seconds from run start to suppressed) for full marks / floor.
        public const float SuppressFastTime = 45f;
        public const float SuppressSlowTime = 90f;
        public const float SuppressFloor = 8f;

        // Grade thresholds.
        public const float ExcellentAt = 90f;
        public const float GoodAt = 75f;
        public const float SatisfactoryAt = 60f;
        public const float NeedsImprovementAt = 40f;
    }
}
