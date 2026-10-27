using System;
using UnityEngine;

namespace SafeVR.Scenario
{
    public abstract class ScenarioManagerBase : MonoBehaviour
    {
        [Header("Scenario Config")]
        [SerializeField] protected ScenarioType scenarioType = ScenarioType.ElectricalFire;
        [SerializeField] protected float failTimeLimit = 120f;

        public ScenarioState CurrentState { get; protected set; } = ScenarioState.Normal;
        public ScenarioMetrics Metrics { get; protected set; }
        public float ElapsedTime { get; protected set; }

        public event Action<ScenarioState, ScenarioState> OnStateChanged;
        public event Action<ScenarioMetrics> OnScenarioEnded;

        protected bool scenarioEnded;

        protected virtual void Awake()
        {
            Metrics = new ScenarioMetrics
            {
                scenarioType = scenarioType,
                totalRequiredActions = GetRequiredActionCount(),
                elapsedTime = 0f
            };
        }

        protected virtual void Update()
        {
            if (scenarioEnded) return;
            ElapsedTime += Time.deltaTime;
            Metrics.elapsedTime = ElapsedTime;
            Tick(Time.deltaTime);

            if (ElapsedTime >= failTimeLimit && CurrentState != ScenarioState.Completed && CurrentState != ScenarioState.Failed)
            {
                // Timeout -> fail if not completed
                if (CurrentState == ScenarioState.EmergencyActive || CurrentState == ScenarioState.HazardDetected)
                {
                    RegisterUnsafeAction("Failed to respond in time - hazard escalated");
                    FailScenario("Time expired - emergency not contained");
                }
            }
        }

        protected abstract void Tick(float dt);
        protected abstract int GetRequiredActionCount();

        protected void TransitionTo(ScenarioState next)
        {
            if (CurrentState == next) return;
            var prev = CurrentState;
            CurrentState = next;
            Debug.Log($"[Scenario] {prev} -> {next} @ {ElapsedTime:F1}s");
            OnStateChanged?.Invoke(prev, next);
            OnEnterState(next, prev);
        }

        protected virtual void OnEnterState(ScenarioState next, ScenarioState prev) { }

        public void RegisterCorrectAction(string description)
        {
            Metrics.correctActions++;
            Metrics.actionLog.Add($"[+][{ElapsedTime:F1}s] {description}");
            Debug.Log($"[Scenario] Correct: {description}");
        }

        public void RegisterUnsafeAction(string description)
        {
            Metrics.unsafeActions++;
            Metrics.actionLog.Add($"[!][{ElapsedTime:F1}s] {description}");
            Debug.LogWarning($"[Scenario] Unsafe: {description}");
            Core.SoundEffectManager.Instance?.PlayFailure();
        }

        protected void CompleteScenario()
        {
            if (scenarioEnded) return;
            scenarioEnded = true;
            Metrics.scenarioCompleted = true;
            Metrics.scenarioFailed = false;
            TransitionTo(ScenarioState.Completed);
            OnScenarioEnded?.Invoke(Metrics);
            Core.SoundEffectManager.Instance?.PlaySuccess();
            Debug.Log($"[Scenario] COMPLETED in {ElapsedTime:F1}s Score {Metrics.SafetyScore}");
        }

        protected void FailScenario(string reason)
        {
            if (scenarioEnded) return;
            scenarioEnded = true;
            Metrics.scenarioFailed = true;
            Metrics.scenarioCompleted = false;
            Metrics.actionLog.Add($"[FAIL][{ElapsedTime:F1}s] {reason}");
            TransitionTo(ScenarioState.Failed);
            OnScenarioEnded?.Invoke(Metrics);
            Core.SoundEffectManager.Instance?.PlayFailure();
            Debug.LogWarning($"[Scenario] FAILED: {reason}");
        }

        public bool IsInState(params ScenarioState[] states)
        {
            foreach (var s in states) if (CurrentState == s) return true;
            return false;
        }
    }
}
