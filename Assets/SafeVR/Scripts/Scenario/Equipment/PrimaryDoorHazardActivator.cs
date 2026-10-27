using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Assessment only: interactable on the Primary Door that starts the drill.
    /// Player action drives the emergency (no auto timer): activation blocks the primary
    /// door, fires the alarm, and unlocks the Alternate Door. Completion still happens
    /// only at SafeZoneTrigger. Desktop input via Interactor; VR-ready (same IInteractable).
    /// </summary>
    public class PrimaryDoorHazardActivator : InteractableBase
    {
        [Header("Hazard State")]
        [SerializeField] private bool primaryDoorBlocked = false;
        [SerializeField] private bool hazardActivationRequired = true;

        [Header("Scenario References")]
        [SerializeField] private AlarmController alarmSource;
        [SerializeField] private GameObject primaryDoorReference;
        [SerializeField] private GameObject alternateDoorReference;
        [SerializeField] private GameObject safeZoneReference;
        [SerializeField] private EvacuationScenarioManager evacScenario;

        public bool IsActivated => primaryDoorBlocked;

        protected override void Awake()
        {
            base.Awake();
            if (alarmSource == null) alarmSource = FindFirstObjectByType<AlarmController>();
            if (alarmSource == null) alarmSource = FindAnyObjectByType<AlarmController>();
            if (evacScenario == null) evacScenario = FindFirstObjectByType<EvacuationScenarioManager>();
            if (evacScenario == null) evacScenario = FindAnyObjectByType<EvacuationScenarioManager>();
            if (primaryDoorReference == null) primaryDoorReference = FindIncludingInactive("PrimaryDoor");
            if (alternateDoorReference == null) alternateDoorReference = FindIncludingInactive("AlternativeExit");
            if (safeZoneReference == null) safeZoneReference = FindIncludingInactive("SafeZoneTrigger");
        }

        public override string GetPromptText()
        {
            if (primaryDoorBlocked) return "Primary Door [BLOCKED] — Use the alternate door (south)";
            return "[E] Activate Primary Door Blocked Hazard";
        }

        public override void OnInteract(Interactor interactor)
        {
            base.OnInteract(interactor);
            if (primaryDoorBlocked)
            {
                UI.ScenarioHUD.Instance?.ShowWarning("Primary door blocked — use the alternate door (south)", 2.5f);
                return;
            }
            if (evacScenario == null)
            {
                evacScenario = FindFirstObjectByType<EvacuationScenarioManager>();
                if (evacScenario == null) evacScenario = FindAnyObjectByType<EvacuationScenarioManager>();
            }

            primaryDoorBlocked = true;
            ApplyHighlight(false);

            // Alarm feedback even if manager refs fail (belt and suspenders; manager also fires it).
            if (alarmSource != null) alarmSource.SetActive(true);

            evacScenario?.ActivatePrimaryDoorHazard();
            Core.SoundEffectManager.Instance?.PlayInteract();
            UI.ScenarioHUD.Instance?.ShowWarning("Primary door blocked. Use the alternate door.", 3.5f);
        }

        private static GameObject FindIncludingInactive(string name)
        {
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
                if (t.name == name) return t.gameObject;
            return null;
        }
    }
}
