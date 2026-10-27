using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    public class EvacuationExit : InteractableBase
    {
        [Header("Evacuation")]
        [SerializeField] private bool isAlternativeExit;
        [SerializeField] private ElectricalFireScenarioManager fireScenario;
        [SerializeField] private EvacuationScenarioManager evacScenario;

        protected override void Awake()
        {
            base.Awake();
            if (fireScenario == null) fireScenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            if (evacScenario == null) evacScenario = FindFirstObjectByType<EvacuationScenarioManager>();
        }

        public override string GetPromptText()
        {
            if (fireScenario != null)
            {
                if (fireScenario.CurrentState == ScenarioState.FireControlled) return "[E] Evacuate Through Emergency Exit";
                if (fireScenario.CurrentState == ScenarioState.Completed || fireScenario.CurrentState == ScenarioState.Failed) return "Exit [DONE]";
                return "Exit - control fire first";
            }
            if (evacScenario != null)
            {
                if (isAlternativeExit) return "[E] Use Alternative Exit (Safe Route)";
                return "[E] Attempt Primary Exit";
            }
            return "[E] Exit";
        }

        public override void OnInteract(Interactor interactor)
        {
            base.OnInteract(interactor);
            Core.SoundEffectManager.Instance?.PlayInteract();

            if (fireScenario != null)
            {
                fireScenario.NotifyEvacuate();
                return;
            }
            if (evacScenario != null)
            {
                if (isAlternativeExit)
                {
                    evacScenario.NotifyAlternativeRouteFound();
                    // Then need to reach safe zone; we also treat interact as route found
                }
                else
                {
                    evacScenario.NotifyAttemptBlockedExit();
                }
            }
        }
    }


}
