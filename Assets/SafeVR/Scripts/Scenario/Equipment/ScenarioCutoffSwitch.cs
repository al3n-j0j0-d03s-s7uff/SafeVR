using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    public class ScenarioCutoffSwitch : InteractableBase
    {
        [Header("Switch")]
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 offRot = Vector3.zero;
        [SerializeField] private Vector3 onRot = new Vector3(-60, 0, 0);
        [SerializeField] private GameObject sparkVFX;
        [SerializeField] private ElectricalFireScenarioManager scenario;

        private bool activated;

        protected override void Awake()
        {
            base.Awake();
            if (scenario == null) scenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            UpdateVisual(false);
        }

        public override string GetPromptText()
        {
            return activated ? "Cutoff [ISOLATED]" : "[E] Pull Emergency Cutoff";
        }

        public override void OnInteract(Interactor interactor)
        {
            if (activated) return;
            activated = true;
            UpdateVisual(true);
            if (sparkVFX != null) sparkVFX.SetActive(false);
            base.OnInteract(interactor);
            Core.SoundEffectManager.Instance?.PlayInteract();
            scenario?.NotifyPowerIsolated();
            // Legacy hazard controller deactivate done via scenario state
        }

        private void UpdateVisual(bool on)
        {
            if (handle != null) handle.localEulerAngles = on ? onRot : offRot;
        }
    }
}
