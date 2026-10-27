using System.Collections.Generic;
using UnityEngine;
using SafeVR.Core;
using SafeVR.Scenario.Equipment;

namespace SafeVR.Scenario
{
    public class ElectricalFireScenarioManager : ScenarioManagerBase
    {
        [Header("Scenario Timing")]
        [SerializeField] private float normalPhaseDuration = 8f;
        [SerializeField] private float hazardToFireDelay = 4f;
        [SerializeField] private float maxFireDurationBeforeFail = 35f;

        [Header("Refs - Controllers")]
        [SerializeField] private HazardController hazardController;
        [SerializeField] private FireController fireController;
        [SerializeField] private AlarmController alarmController;
        [SerializeField] private SmokeController smokeController;

        [Header("Refs - Equipment")]
        [SerializeField] private GameObject evacuationExitCollider;

        private bool powerIsolated;
        private bool extinguisherEquipped;
        private float fireActiveTime;

        [Header("Phase 2 — Required Extinguisher")]
        [SerializeField] private ExtinguisherType requiredExtinguisher = ExtinguisherType.CO2;

        private readonly HashSet<ExtinguisherType> inspectedTypes = new HashSet<ExtinguisherType>();
        private bool hasSelection;
        private ExtinguisherType selectedExtinguisher = ExtinguisherType.CO2;
        private bool correctExtinguisherEquipped;
        private bool pinPullCounted;
        private FireExtinguisherPickup activePickup;

        // isolate power, equip extinguisher, pull pin, suppress fire, evacuate
        protected override int GetRequiredActionCount() => 5;

        protected override void Awake()
        {
            base.Awake();
            scenarioType = ScenarioType.ElectricalFire;
            Metrics.totalRequiredActions = 5;
            CurrentState = ScenarioState.Normal;
        }

        protected override void Tick(float dt)
        {
            switch (CurrentState)
            {
                case ScenarioState.Normal:
                    if (ElapsedTime >= normalPhaseDuration)
                    {
                        TransitionTo(ScenarioState.HazardDetected);
                    }
                    break;

                case ScenarioState.HazardDetected:
                    if (ElapsedTime >= normalPhaseDuration + 1.5f)
                    {
                        TransitionTo(ScenarioState.EmergencyActive);
                    }
                    break;

                case ScenarioState.EmergencyActive:
                    // Auto fire start
                    if (!hazardController.IsFireStarted && ElapsedTime >= normalPhaseDuration + hazardToFireDelay)
                    {
                        hazardController.TriggerFire();
                        fireActiveTime = 0f;
                    }
                    if (hazardController.IsFireStarted)
                    {
                        fireActiveTime += dt;
                        smokeController?.SetDensity(fireController.intensity / 100f);
                        Metrics.fireMaxIntensityReached = Mathf.Max(Metrics.fireMaxIntensityReached, fireController.intensity);

                        if (fireController.IsCritical && !scenarioEnded)
                        {
                            // Taking too long -> fail
                            if (fireActiveTime > maxFireDurationBeforeFail)
                            {
                                FailScenario("Fire grew out of control - response too slow");
                            }
                        }
                        // If player ignores power isolation and tries to extinguish, consequence handled via FireController.Suppression failure + RegisterUnsafeAction elsewhere
                    }
                    break;

                case ScenarioState.PowerIsolated:
                    // Power isolation must NOT extinguish the fire — it only stops sparks.
                    // Fire stays visibly active and must still be suppressed with the extinguisher.
                    EnsureDelayedFireStart();
                    fireActiveTime += dt;
                    if (fireController != null) smokeController?.SetDensity(fireController.intensity / 100f);
                    if (fireController != null) Metrics.fireMaxIntensityReached = Mathf.Max(Metrics.fireMaxIntensityReached, fireController.intensity);
                    if (IsFireSuppressed())
                    {
                        TransitionTo(ScenarioState.FireControlled);
                    }
                    break;

                case ScenarioState.ExtinguisherEquipped:
                case ScenarioState.FireSuppressing:
                    EnsureDelayedFireStart();
                    fireActiveTime += dt;
                    if (fireController != null) smokeController?.SetDensity(fireController.intensity / 100f);
                    if (fireController != null) Metrics.fireMaxIntensityReached = Mathf.Max(Metrics.fireMaxIntensityReached, fireController.intensity);
                    if (IsFireSuppressed())
                    {
                        TransitionTo(ScenarioState.FireControlled);
                    }
                    break;

                case ScenarioState.FireControlled:
                    // Wait for evacuation
                    smokeController?.SetDensity(fireController.intensity / 100f);
                    break;
            }
        }

        protected override void OnEnterState(ScenarioState next, ScenarioState prev)
        {
            switch (next)
            {
                case ScenarioState.HazardDetected:
                    hazardController?.ActivateHazard();
                    Metrics.alarmActivated = true;
                    alarmController?.SetActive(true);
                    SoundEffectManager.Instance?.PlayInteract();
                    break;
                case ScenarioState.EmergencyActive:
                    // alarm already
                    break;
                case ScenarioState.PowerIsolated:
                    hazardController?.DeactivateHazard();
                    powerIsolated = true;
                    Metrics.powerIsolated = true;
                    Metrics.powerIsolationTime = ElapsedTime;
                    RegisterCorrectAction("Isolated electrical power");
                    // if correct extinguisher already equipped, move to suppressing (wrong type must be swapped first)
                    if (extinguisherEquipped && correctExtinguisherEquipped) TransitionTo(ScenarioState.FireSuppressing);
                    break;
                case ScenarioState.ExtinguisherEquipped:
                    extinguisherEquipped = true;
                    Metrics.extinguisherUsedCorrectly = false; // will be true when fire out with power isolated
                    // Correct-action credit only for the required type; wrong selections are unsafe (logged in Notify).
                    if (!hasSelection || correctExtinguisherEquipped)
                        RegisterCorrectAction($"Equipped fire extinguisher ({selectedExtinguisher.Label()})");
                    if (powerIsolated && correctExtinguisherEquipped) TransitionTo(ScenarioState.FireSuppressing);
                    break;
                case ScenarioState.FireSuppressing:
                    break;
                case ScenarioState.FireControlled:
                    alarmController?.SetActive(false);
                    smokeController?.SetActive(false);
                    Metrics.extinguisherUsedCorrectly = powerIsolated && correctExtinguisherEquipped;
                    Metrics.suppressionTime = ElapsedTime;
                    Metrics.evacuatedSafely = false;
                    RegisterCorrectAction("Fire fully suppressed");
                    // Enable exit prompt
                    break;
                case ScenarioState.Evacuating:
                    // Door opened — player must now physically walk pathway to SafeZoneTrigger.
                    // Do NOT complete here. Completion happens in NotifyReachedSafeZone().
                    RegisterCorrectAction("Exit door opened — evacuating through pathway");
                    break;
                case ScenarioState.Completed:
                    break;
                case ScenarioState.Failed:
                    alarmController?.SetActive(false);
                    break;
            }
        }

        /// <summary>
        /// Fire must still ignite on schedule even if power was isolated early.
        /// Isolating power stops sparks only — it never extinguishes the fire.
        /// </summary>
        private void EnsureDelayedFireStart()
        {
            if (hazardController != null && !hazardController.IsFireStarted
                && ElapsedTime >= normalPhaseDuration + hazardToFireDelay)
            {
                hazardController.TriggerFire();
                fireActiveTime = 0f;
            }
        }

        /// <summary>
        /// Fire counts as suppressed only after it has actually started and
        /// intensity dropped via extinguisher discharge. Guards against
        /// intensity==0 (never ignited) falsely reading as extinguished.
        /// </summary>
        private bool IsFireSuppressed()
        {
            return hazardController != null && hazardController.IsFireStarted
                && fireController != null && fireController.IsExtinguished;
        }

        // Called by Switch
        public void NotifyPowerIsolated()
        {
            if (IsInState(ScenarioState.EmergencyActive, ScenarioState.HazardDetected, ScenarioState.Normal))
            {
                TransitionTo(ScenarioState.PowerIsolated);
            }
            else if (CurrentState == ScenarioState.PowerIsolated || CurrentState == ScenarioState.FireControlled) { /* ignore */ }
            else
            {
                RegisterUnsafeAction("Power isolation attempted at wrong time");
            }
        }

        /// <summary>Phase 2 — record label inspection (does not advance correct-action count).</summary>
        public void NotifyExtinguisherInspected(ExtinguisherType type)
        {
            if (scenarioEnded) return;
            if (inspectedTypes.Add(type))
            {
                Metrics.actionLog.Add($"[i][{ElapsedTime:F1}s] Inspected {type.Label()} ({type.ClassTag()})");
                Debug.Log($"[Scenario] Inspected: {type.Label()}");
            }
        }

        /// <summary>Legacy untyped entry — original single station was CO2.</summary>
        public void NotifyExtinguisherEquipped()
        {
            NotifyExtinguisherEquipped(ExtinguisherType.CO2);
        }

        /// <summary>
        /// Phase 2 — typed selection. Wrong type logs an unsafe action and never
        /// advances toward suppression; correct type follows the existing
        /// Isolate power → Equip → Suppress flow (usability still gated by power).
        /// </summary>
        public void NotifyExtinguisherEquipped(ExtinguisherType type)
        {
            NotifyExtinguisherEquipped(type, FindPickupOfType(type));
        }

        /// <summary>Phase 3 — typed selection with the physical unit reference.</summary>
        public void NotifyExtinguisherEquipped(ExtinguisherType type, FireExtinguisherPickup pickup)
        {
            bool sameUnitReselected = extinguisherEquipped && hasSelection && activePickup == pickup && pickup != null;
            bool sameTypeReselected = extinguisherEquipped && hasSelection && selectedExtinguisher == type && pickup == null;
            if (pickup != null) activePickup = pickup;
            selectedExtinguisher = type;
            hasSelection = true;
            extinguisherEquipped = true;
            if (sameUnitReselected || sameTypeReselected) return;
            if (!inspectedTypes.Contains(type))
            {
                inspectedTypes.Add(type);
                RegisterUnsafeAction($"Equipped {type.Label()} without inspecting its label first");
            }
            if (type != requiredExtinguisher)
            {
                correctExtinguisherEquipped = false;
                RegisterUnsafeAction($"Selected wrong extinguisher ({type.Label()} on electrical fire — use {requiredExtinguisher.Label()})");
                SoundEffectManager.Instance?.PlayFailure();
                UI.ScenarioHUD.Instance?.ShowWarning(
                    $"WRONG EXTINGUISHER! {type.Label()} is {type.ShortHint()}. Swap to {requiredExtinguisher.Label()} ({requiredExtinguisher.ClassTag()}).", 4f);
                // Hold the wrong unit but never advance toward suppression.
                if (IsInState(ScenarioState.FireSuppressing)) TransitionTo(ScenarioState.ExtinguisherEquipped);
                else if (IsInState(ScenarioState.EmergencyActive, ScenarioState.PowerIsolated, ScenarioState.HazardDetected))
                    TransitionTo(ScenarioState.ExtinguisherEquipped);
                else if (CurrentState == ScenarioState.Normal)
                    RegisterUnsafeAction("Picked up extinguisher before emergency — keep mounted until alarm");
                return;
            }
            correctExtinguisherEquipped = true;
            if (IsInState(ScenarioState.EmergencyActive, ScenarioState.PowerIsolated, ScenarioState.HazardDetected, ScenarioState.Normal))
            {
                if (CurrentState == ScenarioState.Normal)
                {
                    RegisterUnsafeAction("Picked up extinguisher before emergency — keep mounted until alarm");
                }
                // Route through ExtinguisherEquipped so equip credit is registered exactly once;
                // its entry auto-advances to FireSuppressing when power is already isolated.
                if (CurrentState != ScenarioState.FireSuppressing && CurrentState != ScenarioState.FireControlled)
                    TransitionTo(ScenarioState.ExtinguisherEquipped);
            }
            else if (CurrentState == ScenarioState.ExtinguisherEquipped && powerIsolated)
            {
                // Swapped back to the correct unit after holding the wrong one.
                RegisterCorrectAction($"Equipped correct extinguisher ({requiredExtinguisher.Label()})");
                TransitionTo(ScenarioState.FireSuppressing);
            }
        }

        private FireExtinguisherPickup FindPickupOfType(ExtinguisherType type)
        {
            var all = FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in all)
                if (p != null && p.Type == type) return p;
            return null;
        }

        /// <summary>
        /// Phase 3 — safety-pin removal on the active unit. Counts as a required
        /// procedure step (credited once). VR controllers call the same method.
        /// </summary>
        public void NotifyPinRemoved()
        {
            if (scenarioEnded || pinPullCounted) return;
            if (!extinguisherEquipped || !correctExtinguisherEquipped) return;
            pinPullCounted = true;
            Metrics.pinRemoved = true;
            RegisterCorrectAction("Removed extinguisher safety pin");
        }

        public bool IsPinPullCounted => pinPullCounted;

        /// <summary>
        /// Called when player opens the exit door. Moves FireControlled -> Evacuating
        /// but does NOT complete the scenario. Player must walk to SafeZoneTrigger.
        /// </summary>
        public void NotifyDoorOpened()
        {
            if (IsInState(ScenarioState.FireControlled))
            {
                Metrics.exitOpened = true;
                Metrics.correctExitUsed = true;
                TransitionTo(ScenarioState.Evacuating);
            }
            else if (IsInState(ScenarioState.EmergencyActive, ScenarioState.PowerIsolated, ScenarioState.ExtinguisherEquipped, ScenarioState.FireSuppressing))
            {
                RegisterUnsafeAction("Opened exit before fire was controlled - unsafe");
            }
            else if (CurrentState == ScenarioState.Normal)
            {
                RegisterUnsafeAction("False evacuation - no emergency");
            }
        }

        /// <summary>
        /// Called ONLY by SafeZoneTrigger when player physically reaches safe area.
        /// </summary>
        public void NotifyReachedSafeZone()
        {
            if (IsInState(ScenarioState.Evacuating))
            {
                Metrics.evacuatedSafely = true;
                RegisterCorrectAction("Reached safe evacuation point");
                CompleteScenario();
            }
            else if (IsInState(ScenarioState.FireControlled))
            {
                // Reached safe zone without opening door first (edge case) — count door as opened
                TransitionTo(ScenarioState.Evacuating);
                Metrics.evacuatedSafely = true;
                RegisterCorrectAction("Reached safe evacuation point");
                CompleteScenario();
            }
            else if (IsInState(ScenarioState.EmergencyActive, ScenarioState.PowerIsolated, ScenarioState.ExtinguisherEquipped, ScenarioState.FireSuppressing))
            {
                RegisterUnsafeAction("Reached safe zone before fire was controlled - unsafe");
                FailScenario("Evacuated without controlling fire");
            }
        }

        /// <summary>Legacy entry — door interaction routes here. Kept so old callers open the
        /// door without instantly completing. Safe zone completes.</summary>
        public void NotifyEvacuate()
        {
            NotifyDoorOpened();
        }

        public bool CanDischargeExtinguisher()
        {
            if (!extinguisherEquipped) return false;
            return IsInState(ScenarioState.ExtinguisherEquipped, ScenarioState.FireSuppressing, ScenarioState.PowerIsolated, ScenarioState.EmergencyActive);
        }

        public bool IsPowerIsolated => powerIsolated;
        public bool IsExtinguisherEquipped => extinguisherEquipped;
        public ExtinguisherType RequiredExtinguisher => requiredExtinguisher;
        public bool HasExtinguisherSelection => hasSelection;
        public ExtinguisherType SelectedExtinguisher => selectedExtinguisher;
        public bool IsCorrectExtinguisherEquipped => correctExtinguisherEquipped;
        public bool HasInspected(ExtinguisherType type) => inspectedTypes.Contains(type);
        /// <summary>Phase 3 — physical unit currently in hand (null until equipped).</summary>
        public FireExtinguisherPickup ActivePickup => activePickup;
        public bool IsPinRemoved => activePickup != null && activePickup.IsPinRemoved;
        public float ActiveCharge01 => activePickup != null ? activePickup.Charge01 : 0f;
        public bool HasActiveCharge => activePickup != null && activePickup.HasCharge;
    }
}
