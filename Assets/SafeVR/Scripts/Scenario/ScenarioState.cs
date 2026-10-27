namespace SafeVR.Scenario
{
    public enum ScenarioState
    {
        Normal = 0,
        HazardDetected = 1,
        EmergencyActive = 2,
        PowerIsolationRequired = 3,
        PowerIsolated = 4,
        ExtinguisherEquipped = 5,
        FireSuppressing = 6,
        FireControlled = 7,
        Evacuating = 8,
        Completed = 9,
        Failed = 10
    }

    public enum ScenarioType
    {
        ElectricalFire = 0,
        Evacuation = 1
    }
}
