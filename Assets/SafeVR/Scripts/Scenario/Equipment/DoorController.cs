using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Hinged emergency exit door reused by TrainingRoom (fire) and Assessment (evacuation).
    /// Rotates around DoorHinge pivot. Opening NEVER completes the scenario —
    /// completion happens only at SafeZoneTrigger after walking the pathway.
    /// </summary>
    public class DoorController : InteractableBase
    {
        [Header("Door")]
        [SerializeField] private Transform hinge;
        [SerializeField] private float closedYaw = 0f;
        [SerializeField] private float openYaw = -95f;
        [SerializeField] private float openSpeed = 2.2f;
        [SerializeField] private Collider doorBlocker;
        [SerializeField] private GameObject pathway;
        [SerializeField] private ElectricalFireScenarioManager fireScenario;
        [SerializeField] private EvacuationScenarioManager evacScenario;

        private bool isOpen;
        private bool animating;
        private float currentYaw;

        public bool IsOpen => isOpen;

        protected override void Awake()
        {
            base.Awake();
            if (hinge == null) hinge = transform;
            currentYaw = closedYaw;
            ApplyYaw(currentYaw);
            if (fireScenario == null) fireScenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            if (fireScenario == null) fireScenario = FindAnyObjectByType<ElectricalFireScenarioManager>();
            if (evacScenario == null) evacScenario = FindFirstObjectByType<EvacuationScenarioManager>();
            if (evacScenario == null) evacScenario = FindAnyObjectByType<EvacuationScenarioManager>();
            if (pathway != null) pathway.SetActive(true); // pathway geometry always visible beyond door
        }

        /// <summary>
        /// Assessment: exit stays physically blocked until the player completes the required
        /// response (identify blocked primary exit → EmergencyActive). TrainingRoom unaffected
        /// (evacScenario is null there, so never locked).
        /// </summary>
        private bool IsEvacLocked()
        {
            if (evacScenario == null) return false;
            var st = evacScenario.CurrentState;
            return st == ScenarioState.Normal || st == ScenarioState.HazardDetected;
        }

        public override string GetPromptText()
        {
            if (isOpen) return "Exit [OPEN] — Walk through to Safe Zone";
            if (IsEvacLocked()) return "Exit [BLOCKED] — Inspect blocked primary exit first";
            if (fireScenario != null && fireScenario.CurrentState != ScenarioState.FireControlled
                && fireScenario.CurrentState != ScenarioState.Completed
                && fireScenario.CurrentState != ScenarioState.Failed)
                return "[E] Open Exit Door (control fire first)";
            return "[E] Open Exit Door";
        }

        public override void OnInteract(Interactor interactor)
        {
            if (isOpen) return;
            if (IsEvacLocked())
            {
                // Blocked: refuse to open, no state change, no completion.
                Core.SoundEffectManager.Instance?.PlayInteract();
                UI.ScenarioHUD.Instance?.ShowWarning("Exit blocked — inspect the blocked primary exit first", 2.5f);
                return;
            }
            base.OnInteract(interactor);
            Core.SoundEffectManager.Instance?.PlayInteract();
            Open();
            // Door open only starts evacuation walk. Completion happens at SafeZoneTrigger.
            fireScenario?.NotifyDoorOpened();
            // Assessment: opening the alternative exit marks the route found (Evacuating), never completes.
            evacScenario?.NotifyAlternativeRouteFound();
        }

        public void Open()
        {
            if (isOpen) return;
            isOpen = true;
            animating = true;
        }

        private void Update()
        {
            if (!animating || hinge == null) return;
            currentYaw = Mathf.MoveTowards(currentYaw, openYaw, openSpeed * 95f * Time.deltaTime / 95f * 95f * 0.02f + openSpeed * 60f * Time.deltaTime);
            // Simpler: lerp speed
            currentYaw = Mathf.MoveTowards(currentYaw, openYaw, 90f * Time.deltaTime * openSpeed * 0.5f);
            ApplyYaw(currentYaw);
            if (Mathf.Approximately(currentYaw, openYaw))
            {
                animating = false;
                if (doorBlocker != null) doorBlocker.enabled = false; // allow walk-through
            }
        }

        private void ApplyYaw(float yaw)
        {
            if (hinge != null) hinge.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
