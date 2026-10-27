using UnityEngine;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Phase 2 — Extinguisher types for the TrainingRoom electrical-fire scenario.
    /// CO2 (Class C) is the correct choice for energized electrical fires.
    /// Water (Class A) is incorrect and dangerous on electrical fires.
    /// </summary>
    public enum ExtinguisherType
    {
        CO2 = 0,
        Water = 1
    }

    public static class ExtinguisherTypeInfo
    {
        public static string Label(this ExtinguisherType type)
        {
            return type == ExtinguisherType.CO2 ? "CO2 Extinguisher" : "Water Extinguisher";
        }

        public static string ClassTag(this ExtinguisherType type)
        {
            return type == ExtinguisherType.CO2
                ? "Class C — Energized electrical"
                : "Class A — Ordinary combustibles ONLY";
        }

        public static string ShortHint(this ExtinguisherType type)
        {
            return type == ExtinguisherType.CO2
                ? "SAFE for electrical fire"
                : "NEVER on electrical fire";
        }

        public static Color LabelColor(this ExtinguisherType type)
        {
            return type == ExtinguisherType.CO2
                ? new Color(0.2f, 0.9f, 0.5f)
                : new Color(1f, 0.35f, 0.3f);
        }

        public static string InspectionText(this ExtinguisherType type)
        {
            return type == ExtinguisherType.CO2
                ? "CO2 — Class C (non-conductive). CORRECT for electrical fire. Equip after isolating power, then HOLD on fire base."
                : "WATER — Class A (conductive). WRONG for electrical fire — shock + spread risk. Use CO2 instead.";
        }
    }
}
