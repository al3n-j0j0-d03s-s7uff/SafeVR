using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Phase 2 — Typed extinguisher station (CO2 / Water) for the TrainingRoom
    /// electrical-fire scenario. Two-step flow in scenario scenes:
    /// first interact INSPECTS the label, second interact EQUIPS.
    /// Tutorial/practice scenes (no scenario manager) keep single-press equip.
    /// </summary>
    public class FireExtinguisherPickup : InteractableBase
    {
        [Header("Pickup")]
        [SerializeField] private GameObject pickupVisualRoot;
        [SerializeField] private Transform weaponHold; // WeaponHold under PlayerCamera
        [SerializeField] private GameObject heldExtinguisher; // HeldExtinguisher (pre-built, inactive)

        [Header("Phase 2 — Extinguisher Type")]
        [SerializeField] private ExtinguisherType extinguisherType = ExtinguisherType.CO2;

        [Header("Phase 3 — Pin & Charge")]
        [SerializeField] private float maxCharge = 100f;
        [Tooltip("Charge units drained per second of discharge. 100 charge ≈ 20 s of spray.")]
        [SerializeField] private float drainRate = 5f; // charge units per second of discharge
        [SerializeField] private float charge = 100f;
        [SerializeField] private bool pinRemoved;

        public ExtinguisherType Type => extinguisherType;
        public bool IsPicked => isPicked;
        public bool WasInspected => inspected;
        public bool IsPinRemoved => pinRemoved;
        public float Charge => charge;
        public float MaxCharge => maxCharge;
        public float Charge01 => maxCharge > 0f ? Mathf.Clamp01(charge / maxCharge) : 0f;
        public bool HasCharge => charge > 0.5f;
        /// <summary>Ready to discharge: equipped, pin out, charge remaining.</summary>
        public bool IsReady => isPicked && pinRemoved && HasCharge;

        private bool isPicked;
        private bool inspected;
        private ElectricalFireScenarioManager scenario;

        protected override void Awake()
        {
            base.Awake();
            scenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            if (scenario == null) scenario = FindAnyObjectByType<ElectricalFireScenarioManager>();
            if (pickupVisualRoot == null) pickupVisualRoot = gameObject;
            ResolveHoldRefs();
        }

        private void ResolveHoldRefs()
        {
            if (weaponHold == null)
            {
                var wh = FindIncludingInactive("WeaponHold");
                if (wh != null) weaponHold = wh.transform;
            }
            if (heldExtinguisher == null)
            {
                var held = FindIncludingInactive("HeldExtinguisher");
                if (held != null) heldExtinguisher = held;
            }
        }

        private static GameObject FindIncludingInactive(string name)
        {
            // GameObject.Find skips inactive HeldExtinguisher, so search all transforms
            var all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in all)
                if (t.name == name) return t.gameObject;
            return null;
        }

        public override string GetPromptText()
        {
            if (isPicked)
            {
                if (!HasCharge) return $"{extinguisherType.Label()} [EMPTY] — unit spent";
                if (!pinRemoved) return $"{extinguisherType.Label()} [EQUIPPED] — Aim at fire, [E] to pull pin";
                return $"{extinguisherType.Label()} [READY {Charge01 * 100f:F0}%] — Aim at base and HOLD";
            }
            if (scenario == null) return $"[E] Pick Up {extinguisherType.Label()}";
            if (!inspected) return $"[E] Inspect {extinguisherType.Label()} — {extinguisherType.ClassTag()}";
            return $"[E] Equip {extinguisherType.Label()} — {extinguisherType.ShortHint()}";
        }

        public override void OnInteract(Interactor interactor)
        {
            if (isPicked) return;
            // Resolve scenario now (scene may have been reloaded)
            if (scenario == null)
            {
                scenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
                if (scenario == null) scenario = FindAnyObjectByType<ElectricalFireScenarioManager>();
            }
            if (scenario != null && scenario.CurrentState == ScenarioState.Normal)
            {
                scenario.RegisterUnsafeAction("Picked up extinguisher before emergency — keep mounted until alarm");
                UI.ScenarioHUD.Instance?.ShowWarning("Emergency not yet active — observe first", 2.5f);
            }

            // Practice scenes (Tutorial): no electrical hazard — single-press equip, unchanged behavior.
            if (scenario == null)
            {
                EquipHeld();
                Core.SoundEffectManager.Instance?.PlayInteract();
                UI.ScenarioHUD.Instance?.ShowWarning($"{extinguisherType.Label()} in hand — aim at fire, [E] pulls pin, then HOLD + sweep", 3.5f);
                base.OnInteract(interactor);
                return;
            }

            // Step 1 — INSPECT label before selecting. Stays interactable for the equip press.
            if (!inspected)
            {
                inspected = true;
                scenario.NotifyExtinguisherInspected(extinguisherType);
                Core.SoundEffectManager.Instance?.PlayInteract();
                UI.ScenarioHUD.Instance?.ShowWarning(
                    $"LABEL: {extinguisherType.InspectionText()}\nPress [E] again to equip.", 4f);
                base.OnInteract(interactor);
                return;
            }

            // Step 2 — EQUIP (supports swapping: previously equipped station returns to its mount).
            ResetOtherPickups();
            EquipHeld();
            bool correct = scenario.RequiredExtinguisher == extinguisherType;
            scenario.NotifyExtinguisherEquipped(extinguisherType, this);
            Core.SoundEffectManager.Instance?.PlayInteract();
            // Scenario owns wrong-type feedback; only add guidance when correct so toasts don't overwrite.
            if (correct)
            {
                UI.ScenarioHUD.Instance?.ShowWarning(
                    scenario.IsPowerIsolated
                        ? "CO2 in hand — aim at fire, [E] to pull pin, then HOLD to discharge"
                        : "CO2 in hand — Cut power, pull pin ([E] at fire), then HOLD", 3.5f);
            }
            base.OnInteract(interactor);
        }

        /// <summary>
        /// VR-ready pin pull: explicit safety step between equip and discharge.
        /// Returns true only on the transition (first pull).
        /// </summary>
        public bool PullPin()
        {
            if (!isPicked || pinRemoved) return false;
            pinRemoved = true;
            SyncHeldVisuals();
            return true;
        }

        /// <summary>Drain charge for dt seconds of spraying. Returns remaining charge.</summary>
        public float DrainCharge(float dt)
        {
            if (!isPicked || charge <= 0f) return charge;
            charge = Mathf.Max(0f, charge - drainRate * dt);
            SyncHeldVisuals();
            return charge;
        }

        /// <summary>Mirror this unit's pin/charge state onto the shared in-hand visual.</summary>
        public void SyncHeldVisuals()
        {
            ResolveHoldRefs();
            if (heldExtinguisher == null) return;
            var held = heldExtinguisher.GetComponent<HeldExtinguisher>();
            if (held == null) return;
            held.SetPinRemoved(pinRemoved);
            held.SetCharge01(Charge01);
        }

        private void EquipHeld()
        {
            isPicked = true;
            ResolveHoldRefs();
            SyncHeldVisuals();
            // Hide station model, disable its collider so prompt disappears
            if (pickupVisualRoot != null)
            {
                foreach (var r in pickupVisualRoot.GetComponentsInChildren<Renderer>()) r.enabled = false;
                foreach (var c in pickupVisualRoot.GetComponentsInChildren<Collider>()) c.enabled = false;
            }
            IsInteractable = false;
            ApplyHighlight(false);

            // Show in-hand extinguisher under WeaponHold (actual 3D held object)
            if (heldExtinguisher != null)
            {
                heldExtinguisher.SetActive(true);
                if (weaponHold != null && heldExtinguisher.transform.parent != weaponHold)
                    heldExtinguisher.transform.SetParent(weaponHold, false);
            }
            else if (weaponHold != null)
            {
                Debug.LogWarning("[SafeVR] HeldExtinguisher not found — check TrainingRoom hierarchy");
            }
        }

        /// <summary>Return this station to its mount (used when the player swaps to the other type).</summary>
        public void ResetToStation()
        {
            isPicked = false;
            IsInteractable = true;
            if (pickupVisualRoot != null)
            {
                foreach (var r in pickupVisualRoot.GetComponentsInChildren<Renderer>()) r.enabled = true;
                foreach (var c in pickupVisualRoot.GetComponentsInChildren<Collider>()) c.enabled = true;
            }
            ApplyHighlight(false);
        }

        private void ResetOtherPickups()
        {
            var all = FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var other in all)
            {
                if (other != null && other != this && other.isPicked) other.ResetToStation();
            }
        }
    }
}
