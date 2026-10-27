using UnityEngine;

namespace SafeVR.Interaction
{
    public enum EquipmentType
    {
        FireExtinguisher,
        EmergencyCutoffSwitch,
        EmergencyExit,
        FirstAidKit,
        SafetySign
    }

    /// <summary>
    /// Specialized interactable for safety equipment (Extinguishers, Cutoff switches, Exit doors).
    /// </summary>
    public class EquipmentInteractable : InteractableBase
    {
        [Header("Equipment Information")]
        [SerializeField] private EquipmentType equipmentType = EquipmentType.FireExtinguisher;
        [SerializeField] private string equipmentTitle = "CO2 Fire Extinguisher";
        [TextArea(2, 5)]
        [SerializeField] private string equipmentDescription = "Used for electrical and flammable liquid fires. Follow the P.A.S.S. technique.";
        [SerializeField] private string actionVerb = "Inspect";

        public EquipmentType Type => equipmentType;
        public string Title => equipmentTitle;
        public string Description => equipmentDescription;

        public override string GetPromptText()
        {
            return $"[E] {actionVerb} {equipmentTitle}";
        }

        public override void OnInteract(Interactor interactor)
        {
            base.OnInteract(interactor);
            // Broadcast interaction event for TrainingManager
            Training.TrainingManager.Instance?.OnEquipmentInteracted(this);
        }
    }
}
