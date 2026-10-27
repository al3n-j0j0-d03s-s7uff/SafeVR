using UnityEngine;

namespace SafeVR.Interaction
{
    /// <summary>
    /// Interactable switch or lever (e.g. Emergency Power Cutoff).
    /// Animates handle rotation and triggers hazard deactivation.
    /// </summary>
    public class SwitchInteractable : InteractableBase
    {
        [Header("Switch Mechanics")]
        [SerializeField] private Transform switchHandle;
        [SerializeField] private Vector3 offRotation = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 onRotation = new Vector3(-45, 0, 0);
        [SerializeField] private bool isActivated = false;
        [SerializeField] private GameObject hazardVisualEffect;

        public bool IsActivated => isActivated;

        protected override void Awake()
        {
            base.Awake();
            UpdateSwitchVisuals(false);
        }

        public override string GetPromptText()
        {
            return isActivated ? "Emergency Cutoff [SECURED]" : "[E] Activate Cutoff Switch";
        }

        public override void OnInteract(Interactor interactor)
        {
            if (isActivated) return;

            isActivated = true;
            UpdateSwitchVisuals(true);

            if (hazardVisualEffect != null)
            {
                hazardVisualEffect.SetActive(false);
            }

            base.OnInteract(interactor);

            // Inform training manager – direct path (no EquipmentInteractable required)
            var tm = Training.TrainingManager.Instance;
            if (tm != null)
            {
                // Check if an EquipmentInteractable is also present (legacy)
                EquipmentInteractable eq = GetComponent<EquipmentInteractable>();
                if (eq != null)
                {
                    tm.OnEquipmentInteracted(eq);
                }
                else
                {
                    // Standalone switch: complete cutoff objective directly
                    tm.CompleteObjective("cutoff_switch");
                    Core.TrainingProgress.ElectricalHazardMitigated = true;
                    Core.SoundEffectManager.Instance?.PlaySuccess();
                    UI.TrainingRoomUI.Instance?.ShowEquipmentInspection(
                        "Emergency Cutoff Switch Activated",
                        "High voltage circuit isolated.\nSparking hazard successfully mitigated.\nPower supply safely locked out."
                    );
                }
            }
        }

        private void UpdateSwitchVisuals(bool active)
        {
            if (switchHandle != null)
            {
                switchHandle.localEulerAngles = active ? onRotation : offRotation;
            }
        }
    }
}
