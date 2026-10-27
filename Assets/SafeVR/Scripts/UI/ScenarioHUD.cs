using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeVR.Scenario;

namespace SafeVR.UI
{
    public class ScenarioHUD : MonoBehaviour
    {
        public static ScenarioHUD Instance { get; private set; }

        [Header("Objective")]
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text statusBannerText;
        [SerializeField] private GameObject bannerPanel;

        [Header("Fire")]
        [SerializeField] private GameObject fireIntensityPanel;
        [SerializeField] private Image fireIntensityFill;
        [SerializeField] private TMP_Text fireIntensityText;

        [Header("Hold Progress")]
        [SerializeField] private GameObject holdPanel;
        [SerializeField] private Image holdFill;
        [SerializeField] private TMP_Text holdText;

        [Header("Phase 3 — Charge")]
        [SerializeField] private GameObject chargePanel;
        [SerializeField] private Image chargeFill;
        [SerializeField] private TMP_Text chargeText;

        [Header("Warning")]
        [SerializeField] private GameObject warningPanel;
        [SerializeField] private TMP_Text warningText;
        private float warningTimer;

        private ScenarioManagerBase scenario;
        private ElectricalFireScenarioManager fireScenario;

        private void Awake()
        {
            Instance = this;
            scenario = FindFirstObjectByType<ScenarioManagerBase>();
            fireScenario = scenario as ElectricalFireScenarioManager;
            if (holdPanel != null) holdPanel.SetActive(false);
            if (chargePanel != null) chargePanel.SetActive(false);
            if (warningPanel != null) warningPanel.SetActive(false);
            if (bannerPanel != null) bannerPanel.SetActive(false);
        }

        private void OnEnable()
        {
            if (scenario != null) scenario.OnStateChanged += HandleStateChanged;
            var interactor = FindFirstObjectByType<SafeVR.Interaction.Interactor>();
            if (interactor != null) interactor.OnHoverChanged += OnHoverChanged;
        }

        private void OnDisable()
        {
            if (scenario != null) scenario.OnStateChanged -= HandleStateChanged;
            var interactor = FindFirstObjectByType<SafeVR.Interaction.Interactor>();
            if (interactor != null) interactor.OnHoverChanged -= OnHoverChanged;
        }

        private void Update()
        {
            if (scenario != null)
            {
                if (timerText != null) timerText.text = $"{scenario.ElapsedTime:F1}s";
                UpdateObjective();
                UpdateFireBar();
                UpdateHoldBar();
                UpdateCharge();
            }
            if (warningTimer > 0f)
            {
                warningTimer -= Time.deltaTime;
                if (warningTimer <= 0f && warningPanel != null) warningPanel.SetActive(false);
            }
        }

        private void UpdateObjective()
        {
            if (objectiveText == null || scenario == null) return;
            string obj = "";
            switch (scenario.CurrentState)
            {
                case ScenarioState.Normal: obj = "<b>Phase:</b> Orientation\n<color=#9CA3AF>Explore the training room. Locate safety equipment.</color>"; break;
                case ScenarioState.HazardDetected: obj = "<b>!</b> <color=#FACC08>Hazard Detected</color>\nInvestigate the electrical panel."; break;
                case ScenarioState.EmergencyActive: obj = "<b>!!! EMERGENCY !!!</b>\n<color=#FF4444>Electrical fire! Isolate power first, then equip extinguisher.</color>"; break;
                case ScenarioState.PowerIsolated: obj = "<b>Power Isolated</b> <color=#22C55E>✓</color>\nInspect labels, equip the <b>CO2 (Class C)</b> extinguisher, then suppress fire."; break;
                case ScenarioState.ExtinguisherEquipped:
                    if (fireScenario != null && fireScenario.HasExtinguisherSelection && !fireScenario.IsCorrectExtinguisherEquipped)
                        obj = "<b><color=#EF4444>Wrong extinguisher!</color></b>\nSwap to <b>CO2 (Class C)</b> — water is unsafe on electrical fire.";
                    else if (fireScenario != null && !fireScenario.IsPinRemoved)
                        obj = "<b>CO2 Equipped — Pin In</b>\nAim at the fire and press <b>[E]</b> to pull the pin.";
                    else
                        obj = "<b>CO2 Ready</b>\nAim at fire base and <b>HOLD E/LMB</b>, sweep side-to-side."; break;
                case ScenarioState.FireSuppressing: obj = "<b>Suppressing Fire</b>\nKeep holding, sweep across the base. Watch charge."; break;
                case ScenarioState.FireControlled: obj = "<b>Fire Controlled</b> <color=#22C55E>✓</color>\nEvacuate via emergency exit."; break;
                case ScenarioState.Evacuating: obj = "<b>Evacuating...</b>"; break;
                case ScenarioState.Completed: obj = "<color=#22C55E><b>SCENARIO COMPLETED</b></color>"; break;
                case ScenarioState.Failed: obj = "<color=#EF4444><b>SCENARIO FAILED</b></color>"; break;
            }
            // Evacuation scenario alt (player-driven drill: activate at primary door first)
            if (scenario is EvacuationScenarioManager evac)
            {
                switch (evac.CurrentState)
                {
                    case ScenarioState.Normal: obj = "<b>PRIMARY DRILL:</b>\nGo to the <color=#FACC08>Primary Door (north)</color> and press <b>[E]</b> to Activate Primary Door Blocked Hazard."; break;
                    case ScenarioState.HazardDetected: obj = "<b>!</b> <color=#FACC08>Primary door blocked. Use the alternate door.</color>"; break;
                    case ScenarioState.EmergencyActive: obj = "<b>ALARM ACTIVE</b> — <color=#FF4444>Primary door blocked. Use the alternate door.</color>\nOpen the south Alternate Door and follow the route."; break;
                    case ScenarioState.Evacuating: obj = "<b>Routing to Safe Zone</b>\nFollow illuminated signs."; break;
                }
                if (evac.CurrentState == ScenarioState.Completed) obj = "<color=#22C55E><b>EVACUATION SUCCESS</b></color>";
                if (evac.CurrentState == ScenarioState.Failed) obj = "<color=#EF4444><b>EVACUATION FAILED</b></color>";
            }
            objectiveText.text = obj;
        }

        private void UpdateFireBar()
        {
            if (fireScenario == null || fireIntensityPanel == null) return;
            var fire = FindFirstObjectByType<FireController>();
            if (fire == null)
            {
                fireIntensityPanel.SetActive(false);
                return;
            }
            bool show = fireScenario.CurrentState == ScenarioState.EmergencyActive || fireScenario.CurrentState == ScenarioState.PowerIsolated || fireScenario.CurrentState == ScenarioState.ExtinguisherEquipped || fireScenario.CurrentState == ScenarioState.FireSuppressing;
            fireIntensityPanel.SetActive(show && !fire.IsExtinguished);
            if (fireIntensityFill != null) fireIntensityFill.fillAmount = fire.intensity / 100f;
            if (fireIntensityText != null) fireIntensityText.text = $"FIRE {fire.intensity:F0}%";
        }

        private void UpdateHoldBar()
        {
            // Hold progress is driven by FireDischargeInteractable; we poll any IHoldInteractable being held
            var interactor = FindFirstObjectByType<SafeVR.Interaction.Interactor>();
            if (interactor == null || holdPanel == null) return;
            // Not directly tracking; FireDischargeInteractable will call ShowHold
        }

        public void ShowHold(float progress, string status = null)
        {
            if (holdPanel == null) return;
            holdPanel.SetActive(progress > 0.01f && progress < 0.99f);
            if (holdFill != null) holdFill.fillAmount = progress;
            if (holdText != null) holdText.text = string.IsNullOrEmpty(status)
                ? $"Discharging {progress*100:F0}%"
                : $"Discharging {progress*100:F0}% — {status}";
        }

        public void HideHold()
        {
            if (holdPanel != null) holdPanel.SetActive(false);
        }

        /// <summary>Phase 3 — extinguisher charge/pressure indicator (0..1).</summary>
        public void ShowCharge(float charge01, string label = null)
        {
            if (chargePanel == null) return;
            chargePanel.SetActive(true);
            if (chargeFill != null)
            {
                chargeFill.fillAmount = Mathf.Clamp01(charge01);
                chargeFill.color = charge01 > 0.5f ? new Color(0.18f, 0.82f, 0.38f)
                    : charge01 > 0.2f ? new Color(0.98f, 0.78f, 0.08f) : new Color(0.95f, 0.25f, 0.22f);
            }
            if (chargeText != null) chargeText.text = string.IsNullOrEmpty(label)
                ? $"CHARGE {charge01 * 100f:F0}%"
                : label;
        }

        public void HideCharge()
        {
            if (chargePanel != null) chargePanel.SetActive(false);
        }

        private void UpdateCharge()
        {
            if (chargePanel == null) return;
            if (fireScenario != null && fireScenario.IsExtinguisherEquipped && fireScenario.ActivePickup != null)
            {
                var unit = fireScenario.ActivePickup;
                string pin = unit.IsPinRemoved ? "PIN OUT" : "PIN IN";
                ShowCharge(unit.Charge01, $"CHARGE {unit.Charge01 * 100f:F0}% • {pin}");
                return;
            }
            // Tutorial/practice scenes have no scenario manager — mirror the held unit.
            var held = FindFirstObjectByType<SafeVR.Scenario.Equipment.HeldExtinguisher>();
            if (held != null && held.IsActive)
            {
                var units = FindObjectsByType<SafeVR.Scenario.Equipment.FireExtinguisherPickup>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var u in units)
                {
                    if (u != null && u.IsPicked)
                    {
                        ShowCharge(u.Charge01, $"CHARGE {u.Charge01 * 100f:F0}% • {(u.IsPinRemoved ? "PIN OUT" : "PIN IN")}");
                        return;
                    }
                }
            }
            HideCharge();
        }

        public void ShowWarning(string msg, float duration = 3f)
        {
            if (warningPanel == null || warningText == null) return;
            warningPanel.SetActive(true);
            warningText.text = msg;
            warningTimer = duration;
        }

        public void ShowBanner(string msg, Color color)
        {
            if (bannerPanel == null || statusBannerText == null) return;
            bannerPanel.SetActive(true);
            statusBannerText.text = msg;
            statusBannerText.color = color;
        }

        public void HideBanner()
        {
            if (bannerPanel != null) bannerPanel.SetActive(false);
        }

        private void HandleStateChanged(ScenarioState prev, ScenarioState next)
        {
            switch (next)
            {
                case ScenarioState.HazardDetected: ShowBanner("HAZARD DETECTED - INVESTIGATE", new Color(0.98f, 0.78f, 0.08f)); break;
                case ScenarioState.EmergencyActive: ShowBanner("!!! EMERGENCY: ELECTRICAL FIRE !!!", new Color(1f, 0.25f, 0.25f)); break;
                case ScenarioState.PowerIsolated: ShowBanner("POWER ISOLATED", new Color(0.2f, 0.85f, 0.4f)); Invoke(nameof(HideBanner), 2.5f); break;
                case ScenarioState.FireControlled: ShowBanner("FIRE CONTROLLED - EVACUATE NOW", new Color(0.2f, 0.85f, 0.4f)); break;
                case ScenarioState.Completed: ShowBanner("SCENARIO COMPLETED", new Color(0.2f, 0.85f, 0.4f)); break;
                case ScenarioState.Failed: ShowBanner("SCENARIO FAILED", new Color(1f, 0.25f, 0.25f)); break;
            }
        }

        private void OnHoverChanged(SafeVR.Interaction.IInteractable interactable)
        {
            // Could show contextual hint; handled by ReticleHUD
        }
    }
}
