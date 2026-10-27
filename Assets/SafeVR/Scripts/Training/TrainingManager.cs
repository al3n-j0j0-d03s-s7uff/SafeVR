using System;
using System.Collections.Generic;
using UnityEngine;
using SafeVR.Core;
using SafeVR.Interaction;

namespace SafeVR.Training
{
    /// <summary>
    /// Coordinates objectives, equipment interaction callbacks, and training room state.
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        public static TrainingManager Instance { get; private set; }

        [Header("Objectives")]
        [SerializeField] private List<TrainingObjective> objectives = new List<TrainingObjective>();

        public event Action<TrainingObjective> OnObjectiveUpdated;
        public event Action OnAllObjectivesCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultObjectives();
        }

        private void InitializeDefaultObjectives()
        {
            if (objectives.Count == 0)
            {
                objectives.Add(new TrainingObjective("extinguisher", "Inspect Fire Extinguisher", "Locate and inspect the CO2 fire extinguisher on the wall mount."));
                objectives.Add(new TrainingObjective("cutoff_switch", "Isolate Electrical Hazard", "Flip the Emergency Cutoff Switch to secure sparking equipment."));
                objectives.Add(new TrainingObjective("emergency_exit", "Verify Emergency Exit", "Inspect the primary evacuation route and clear emergency door."));
            }
        }

        public IReadOnlyList<TrainingObjective> GetObjectives() => objectives;

        public void OnEquipmentInteracted(EquipmentInteractable equipment)
        {
            SoundEffectManager.Instance?.PlayInteract();

            switch (equipment.Type)
            {
                case EquipmentType.FireExtinguisher:
                    CompleteObjective("extinguisher");
                    TrainingProgress.ExtinguishersInspected++;
                    UI.TrainingRoomUI.Instance?.ShowEquipmentInspection(
                        equipment.Title,
                        "<b>P.A.S.S. Technique:</b>\n\n" +
                        "• <b>P - Pull:</b> Pull the safety pin to break the tamper seal.\n" +
                        "• <b>A - Aim:</b> Aim low, pointing the nozzle at the base of the fire.\n" +
                        "• <b>S - Squeeze:</b> Squeeze the handle lever slowly and evenly.\n" +
                        "• <b>S - Sweep:</b> Sweep the nozzle from side to side across the fire base."
                    );
                    break;

                case EquipmentType.EmergencyCutoffSwitch:
                    CompleteObjective("cutoff_switch");
                    TrainingProgress.ElectricalHazardMitigated = true;
                    UI.TrainingRoomUI.Instance?.ShowEquipmentInspection(
                        "Emergency Cutoff Switch Activated",
                        "High voltage circuit isolated.\nSparking hazard successfully mitigated.\nPower supply safely locked out."
                    );
                    break;

                case EquipmentType.EmergencyExit:
                    CompleteObjective("emergency_exit");
                    TrainingProgress.EmergencyExitLocated = true;
                    UI.TrainingRoomUI.Instance?.ShowEquipmentInspection(
                        "Emergency Exit Verified",
                        "Primary egress route inspected.\nExit pathway is illuminated and free from obstructions."
                    );
                    break;
            }
        }

        public void CompleteObjective(string id)
        {
            var obj = objectives.Find(o => o.id == id);
            if (obj != null && !obj.isCompleted)
            {
                obj.isCompleted = true;
                SoundEffectManager.Instance?.PlaySuccess();
                OnObjectiveUpdated?.Invoke(obj);

                CheckCompletion();
            }
        }

        private void CheckCompletion()
        {
            bool allDone = objectives.TrueForAll(o => o.isCompleted);
            if (allDone)
            {
                TrainingProgress.TrainingCompleted = true;
                OnAllObjectivesCompleted?.Invoke();
                UI.TrainingRoomUI.Instance?.ShowTrainingCompleted();
            }
        }
    }
}
