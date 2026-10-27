using UnityEngine;
using SafeVR.Core;

namespace SafeVR.Scenario
{
    public class EvacuationScenarioManager : ScenarioManagerBase
    {
        [Header("Timing")]
        [SerializeField] private float blockRevealDelay = 2f;
        [SerializeField] private float smokeDelay = 1f;

        [Header("Refs")]
        [SerializeField] private AlarmController alarmController;
        [SerializeField] private SmokeController smokeController;
        [SerializeField] private GameObject blockedExitVisual;
        [SerializeField] private GameObject safeZoneTrigger;

        private bool primaryBlocked;
        private bool reachedSafeZone;
        private bool alarmActive;

        protected override int GetRequiredActionCount() => 3; // activate hazard, alternative route, safe zone

        protected override void Awake()
        {
            base.Awake();
            scenarioType = ScenarioType.Evacuation;
            Metrics.totalRequiredActions = 3;
            CurrentState = ScenarioState.Normal;
            if (blockedExitVisual != null) blockedExitVisual.SetActive(false);
            if (safeZoneTrigger != null) safeZoneTrigger.SetActive(false);
        }

        /// <summary>
        /// Player-driven drill start (PrimaryDoorHazardActivator). No auto timer: the emergency
        /// begins when the player activates the primary-door hazard, which reveals the blockage,
        /// fires the alarm, and unlocks the alternate door. Never completes the scenario.
        /// </summary>
        public void ActivatePrimaryDoorHazard()
        {
            if (CurrentState != ScenarioState.Normal) return;
            RegisterCorrectAction("Activated primary door blocked hazard");
            TransitionTo(ScenarioState.HazardDetected); // reveals crates + alarm
            TransitionTo(ScenarioState.EmergencyActive); // smoke + safe zone active, alternate unlocked
        }

        protected override void Tick(float dt)
        {
            if (CurrentState == ScenarioState.EmergencyActive)
            {
                smokeController?.SetDensity(Mathf.PingPong(ElapsedTime * 0.15f, 0.55f));
                if (ElapsedTime > 65f && !scenarioEnded)
                {
                    RegisterUnsafeAction("Took too long to evacuate");
                    FailScenario("Evacuation time exceeded");
                }
            }
        }

        protected override void OnEnterState(ScenarioState next, ScenarioState prev)
        {
            switch (next)
            {
                case ScenarioState.HazardDetected:
                    primaryBlocked = true;
                    if (blockedExitVisual != null) blockedExitVisual.SetActive(true);
                    // Emergency begins here: alarm + beacon fire automatically (same AlarmController as TrainingRoom).
                    Metrics.alarmActivated = true;
                    alarmController?.SetActive(true);
                    SoundEffectManager.Instance?.PlayInteract();
                    break;
                case ScenarioState.EmergencyActive:
                    alarmController?.SetActive(true);
                    smokeController?.SetActive(true);
                    if (safeZoneTrigger != null) safeZoneTrigger.SetActive(true);
                    alarmActive = true;
                    SoundEffectManager.Instance?.PlayInteract();
                    break;
                case ScenarioState.Evacuating:
                    break;
                case ScenarioState.Completed:
                    alarmController?.SetActive(false);
                    break;
                case ScenarioState.Failed:
                    alarmController?.SetActive(false);
                    break;
            }
        }

        public void NotifyHazardIdentified()
        {
            if (CurrentState == ScenarioState.HazardDetected)
            {
                Metrics.hazardIdentified = true;
                RegisterCorrectAction("Identified blocked primary exit");
                TransitionTo(ScenarioState.EmergencyActive);
            }
        }

        public void NotifyAttemptBlockedExit()
        {
            RegisterUnsafeAction("Attempted to use blocked exit - crate obstruction, fallback required");
            // Do not fail immediately, but warn and keep emergency active
            if (CurrentState == ScenarioState.EmergencyActive)
            {
                // stay
            }
        }

        public void NotifyAlternativeRouteFound()
        {
            if (CurrentState == ScenarioState.EmergencyActive)
            {
                Metrics.correctExitUsed = true;
                Metrics.exitOpened = true;
                RegisterCorrectAction("Located alternative evacuation route");
                TransitionTo(ScenarioState.Evacuating);
            }
        }

        public void NotifyReachedSafeZone()
        {
            if (IsInState(ScenarioState.Evacuating, ScenarioState.EmergencyActive))
            {
                reachedSafeZone = true;
                Metrics.evacuatedSafely = true;
                RegisterCorrectAction("Reached safe evacuation point");
                CompleteScenario();
            }
            else if (CurrentState == ScenarioState.Normal || CurrentState == ScenarioState.HazardDetected)
            {
                RegisterUnsafeAction("Evacuated before identifying hazard - poor situational awareness");
                FailScenario("Evacuated without proper procedure");
            }
        }

        public void NotifyUnsafeAction(string reason)
        {
            RegisterUnsafeAction(reason);
        }
    }
}
