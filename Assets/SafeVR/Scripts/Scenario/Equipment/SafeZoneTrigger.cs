using UnityEngine;

namespace SafeVR.Scenario.Equipment
{
    public class SafeZoneTrigger : MonoBehaviour
    {
        [SerializeField] private EvacuationScenarioManager evacScenario;
        [SerializeField] private ElectricalFireScenarioManager fireScenario;

        private void Awake()
        {
            if (evacScenario == null) evacScenario = FindFirstObjectByType<EvacuationScenarioManager>();
            if (evacScenario == null) evacScenario = FindAnyObjectByType<EvacuationScenarioManager>();
            if (fireScenario == null) fireScenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            if (fireScenario == null) fireScenario = FindAnyObjectByType<ElectricalFireScenarioManager>();
        }

        private void OnTriggerEnter(Collider other)
        {
            bool isPlayer = other.CompareTag("Player")
                || other.GetComponentInParent<Player.DesktopFPSController>() != null
                || other.GetComponent<Player.DesktopFPSController>() != null;
            if (!isPlayer) return;
            if (evacScenario != null) evacScenario.NotifyReachedSafeZone();
            // TrainingRoom: ONLY the safe zone completes the scenario. Door open alone does not.
            if (fireScenario != null) fireScenario.NotifyReachedSafeZone();
        }
    }
}
