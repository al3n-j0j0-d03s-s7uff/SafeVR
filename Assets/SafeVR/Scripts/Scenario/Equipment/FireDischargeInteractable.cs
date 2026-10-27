using System.Collections.Generic;
using UnityEngine;
using SafeVR.Interaction;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Phase 3 — realistic discharge on the fire volume (hold target).
    /// Procedure enforced through the existing IInteractable/IHoldInteractable flow:
    /// tap [E] pulls the safety pin (OnInteract), HOLD sprays (OnHoldStart/Tick).
    /// Spray is effective only with power isolated + correct unit + pin out +
    /// charge remaining, scaled by aim-at-base and sweep motion. All state calls
    /// are plain methods so VR controllers can drive the same sequence.
    /// </summary>
    public class FireDischargeInteractable : InteractableBase, IHoldInteractable
    {
        [Header("Hold Discharge")]
        [SerializeField] private float requiredHold = 3.5f;
        [SerializeField] private FireController fireController;
        [SerializeField] private ElectricalFireScenarioManager scenario;
        [SerializeField] private ParticleSystem dischargeParticles;
        [SerializeField] private AudioSource dischargeAudio;
        [SerializeField] private GameObject sprayBeam; // visible cone
        [SerializeField] private Transform sprayOrigin; // where spray starts (player aim point prox)

        [Header("Phase 3 — Aim & Sweep Tuning")]
        [Tooltip("Aim error (m) within which suppression is fully effective. Forgiving cone around the base.")]
        [SerializeField] private float fullAimRadius = 0.8f;
        [Tooltip("Aim error (m) beyond which suppression is fully ineffective.")]
        [SerializeField] private float zeroAimRadius = 2.5f;
        [Tooltip("Lateral sweep range (m) over the sample window for full effectiveness.")]
        [SerializeField] private float fullSweepRange = 0.9f;
        [SerializeField] private float sweepWindow = 3.0f;
        [Tooltip("Effectiveness floor: a slow but on-target sweep still suppresses.")]
        [SerializeField] private float minSweepFactor = 0.55f;
        [Tooltip("Below this effectiveness, spraying counts as waste (unsafe after wasteGraceTime).")]
        [SerializeField] private float wasteEffectiveness = 0.15f;
        [SerializeField] private float wasteGraceTime = 2.0f;

        private float holdProgress;
        private bool isHolding;
        private bool fireOutNotified;
        private bool wrongTypeWarned;
        private bool lowEffToastShown;
        private bool wasteWarned;
        private float wasteTime;
        private float pinBlockedAt = -1f;
        private readonly List<SweepSample> sweepSamples = new List<SweepSample>();
        private FireExtinguisherPickup holdUnit;

        private struct SweepSample
        {
            public float time;
            public Vector3 point;
        }

        public float HoldDuration => requiredHold;
        public float CurrentHoldProgress => Mathf.Clamp01(holdProgress / requiredHold);
        public bool IsHolding => isHolding;
        /// <summary>Last computed suppression effectiveness (0..1). 1 = power+type+pin+charge+aim+sweep all good.</summary>
        public float LastEffectiveness { get; private set; } = 1f;

        protected override void Awake()
        {
            base.Awake();
            if (fireController == null) fireController = GetComponentInParent<FireController>();
            if (fireController == null) fireController = FindFirstObjectByType<FireController>();
            if (scenario == null) scenario = FindFirstObjectByType<ElectricalFireScenarioManager>();
            if (scenario == null) scenario = FindAnyObjectByType<ElectricalFireScenarioManager>();
            holdProgress = 0f;
            if (dischargeParticles != null) dischargeParticles.Stop();
            if (dischargeAudio != null)
            {
                dischargeAudio.loop = true;
                dischargeAudio.playOnAwake = false;
                dischargeAudio.spatialBlend = 0.85f;
                // generate simple hiss clip if none
                if (dischargeAudio.clip == null)
                {
                    int sr = 24000;
                    int samples = sr / 2;
                    float[] data = new float[samples];
                    var rng = new System.Random(123);
                    for (int i = 0; i < samples; i++) data[i] = (float)(rng.NextDouble()*2-1) * 0.12f * Mathf.Clamp01(1f - i/(float)samples);
                    var clip = AudioClip.Create("Hiss", samples, 1, sr, false);
                    clip.SetData(data, 0);
                    dischargeAudio.clip = clip;
                }
            }
            if (sprayBeam != null) sprayBeam.SetActive(false);
            // auto-create sprayBeam if missing
            if (sprayBeam == null)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "SprayBeam_Auto";
                go.transform.parent = transform;
                go.transform.localPosition = new Vector3(0, 0.6f, -0.9f);
                go.transform.localRotation = Quaternion.Euler(90,0,0);
                go.transform.localScale = new Vector3(0.12f, 0.9f, 0.12f);
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    // simple translucent white
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    mat.SetColor("_BaseColor", new Color(0.92f,0.95f,1f,0.55f));
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // transparent
                    rend.sharedMaterial = mat;
                }
                DestroyImmediate(go.GetComponent<Collider>());
                go.SetActive(false);
                sprayBeam = go;
                sprayOrigin = go.transform;
            }
        }

        private void Update()
        {
            // Deferred pin-attempt feedback: a tap that only pulled the pin clears
            // pinBlockedAt the same frame; a genuine hold-against-pin logs after 0.3s.
            if (pinBlockedAt >= 0f && Time.time - pinBlockedAt > 0.3f)
            {
                pinBlockedAt = -1f;
                if (IsPinBlocking())
                {
                    scenario?.RegisterUnsafeAction("Attempted discharge without removing the safety pin");
                    UI.ScenarioHUD.Instance?.ShowWarning("PIN IN! Tap [E] on the fire to pull the pin first", 3f);
                    Core.SoundEffectManager.Instance?.PlayFailure();
                }
            }
        }

        public override string GetPromptText()
        {
            if (fireController != null && fireController.IsExtinguished) return "Fire [EXTINGUISHED] — Evacuate";
            if (scenario != null)
            {
                if (!scenario.IsExtinguisherEquipped) return "Need CO2 extinguisher — inspect labels, [E] twice (east wall)";
                if (!scenario.IsCorrectExtinguisherEquipped) return "WRONG extinguisher — swap to CO2 (Class C) for electrical fire!";
                if (!scenario.IsPinRemoved) return "[E] Pull safety pin — then HOLD to discharge";
                if (!scenario.HasActiveCharge) return "EXTINGUISHER EMPTY — restart scenario for a fresh unit";
                if (!scenario.IsPowerIsolated) return "[HOLD E/LMB] to spray — WARNING: Cut power first!";
            }
            else
            {
                var unit = FindPracticeUnit();
                if (unit == null || !unit.IsPicked) return "[E] Pick up extinguisher first";
                if (!unit.IsPinRemoved) return "[E] Pull safety pin — then HOLD to discharge";
                if (!unit.HasCharge) return "EXTINGUISHER EMPTY";
            }
            if (isHolding) return $"Spraying... {CurrentHoldProgress*100:F0}%  [HOLD]";
            return "[HOLD E / LMB] Aim at orange fire base, sweep side-to-side";
        }

        public override bool IsInteractable
        {
            get => base.IsInteractable && fireController != null && !fireController.IsExtinguished;
            set => base.IsInteractable = value;
        }

        /// <summary>
        /// Tap interaction on the fire: pulls the safety pin when the correct unit
        /// is equipped. VR controllers call this before StartHold-equivalent.
        /// </summary>
        public override void OnInteract(Interactor interactor)
        {
            if (!IsInteractable) return;
            if (scenario != null)
            {
                if (!scenario.IsExtinguisherEquipped || !scenario.IsCorrectExtinguisherEquipped) return;
                var unit = scenario.ActivePickup;
                if (unit == null || unit.IsPinRemoved) return;
                pinBlockedAt = -1f; // consume any paired hold-start refusal from this same press
                if (unit.PullPin())
                {
                    scenario.NotifyPinRemoved();
                    Core.SoundEffectManager.Instance?.PlayInteract();
                    UI.ScenarioHUD.Instance?.ShowWarning("PIN PULLED — CO2 ready. HOLD discharge, sweep across base", 3.5f);
                }
                return;
            }
            // Tutorial/practice (no scenario manager): same pin step on the practice unit.
            var practice = FindPracticeUnit();
            if (practice == null || !practice.IsPicked || practice.IsPinRemoved) return;
            pinBlockedAt = -1f;
            if (practice.PullPin())
            {
                Core.SoundEffectManager.Instance?.PlayInteract();
                UI.ScenarioHUD.Instance?.ShowWarning("PIN PULLED — HOLD discharge, sweep across base", 3.5f);
            }
        }

        public void OnHoldStart(Interactor interactor)
        {
            if (!IsInteractable) return;
            if (scenario != null)
            {
                if (!scenario.IsExtinguisherEquipped) return;
                // Wrong unit may be held to demonstrate ineffectiveness (warned in tick).
                if (scenario.IsCorrectExtinguisherEquipped)
                {
                    if (!scenario.IsPinRemoved)
                    {
                        // Silent refuse: desktop pairs this with OnInteract (pin pull) on the
                        // same press; Update() logs only genuine hold-against-pin attempts.
                        pinBlockedAt = Time.time;
                        return;
                    }
                    if (!scenario.HasActiveCharge)
                    {
                        scenario.RegisterUnsafeAction("Attempted discharge with an empty extinguisher");
                        UI.ScenarioHUD.Instance?.ShowWarning("EXTINGUISHER EMPTY — no discharge", 3f);
                        Core.SoundEffectManager.Instance?.PlayFailure();
                        return;
                    }
                }
            }
            else
            {
                var unit = FindPracticeUnit();
                if (unit == null || !unit.IsPicked) return;
                if (!unit.IsPinRemoved)
                {
                    pinBlockedAt = Time.time;
                    return;
                }
                if (!unit.HasCharge)
                {
                    UI.ScenarioHUD.Instance?.ShowWarning("EXTINGUISHER EMPTY — no discharge", 3f);
                    return;
                }
            }
            isHolding = true;
            holdProgress = 0f;
            holdUnit = ResolveHoldUnit();
            fireOutNotified = false;
            wrongTypeWarned = false;
            lowEffToastShown = false;
            wasteWarned = false;
            wasteTime = 0f;
            sweepSamples.Clear();
            if (dischargeParticles != null) dischargeParticles.Play();
            if (dischargeAudio != null && !dischargeAudio.isPlaying) dischargeAudio.Play();
            if (sprayBeam != null) sprayBeam.SetActive(true);
            UpdateSprayBeam(interactor);
            UI.ScenarioHUD.Instance?.ShowHold(0f);
        }

        public void OnHoldTick(Interactor interactor, float dt)
        {
            if (!isHolding || fireController == null) return;
            // Must stay equipped; power check handled via consequences not hard block
            if (scenario != null && !scenario.IsExtinguisherEquipped)
            {
                OnHoldCancel(interactor);
                return;
            }
            if (holdUnit == null) holdUnit = ResolveHoldUnit();
            // No drain or suppression needed once out — hold visuals run to completion.
            if (fireController.IsExtinguished)
            {
                holdProgress += dt;
                UI.ScenarioHUD.Instance?.ShowHold(CurrentHoldProgress);
                UpdateSprayBeam(interactor);
                if (!fireOutNotified)
                {
                    fireOutNotified = true;
                    UI.ScenarioHUD.Instance?.ShowWarning("FIRE OUT — Go to exit!", 3f);
                }
                return;
            }
            holdProgress += dt;
            UpdateSprayBeam(interactor);

            // No scenario manager (e.g. Tutorial practice fire) means no electrical hazard: spray is effective.
            bool powerOk = scenario != null ? scenario.IsPowerIsolated : true;
            bool typeOk = scenario != null ? scenario.IsCorrectExtinguisherEquipped : true;
            if (!powerOk || !typeOk)
            {
                // Ineffective by procedure — existing consequence path (fire keeps growing).
                LastEffectiveness = 0f;
                fireController.Suppress(dt, false);
                UI.ScenarioHUD.Instance?.ShowHold(CurrentHoldProgress, "BLOCKED — check power + type");
                if (!typeOk && holdProgress > 0.8f && !wrongTypeWarned)
                {
                    wrongTypeWarned = true;
                    scenario?.RegisterUnsafeAction("Discharged wrong extinguisher (water on electrical fire — ineffective, shock risk)");
                    UI.ScenarioHUD.Instance?.ShowWarning("WRONG EXTINGUISHER! Water conducts — swap to CO2", 3f);
                }
                if (!powerOk && typeOk && holdProgress > 0.8f && !fireOutNotified)
                {
                    scenario?.RegisterUnsafeAction("Discharged without isolating power — ineffective");
                    UI.ScenarioHUD.Instance?.ShowWarning("CUT POWER FIRST! Spray is ineffective", 2.5f);
                    fireOutNotified = true; // prevent spam
                }
                return;
            }

            // Effective path: pin + charge + aim + sweep.
            float aim = ComputeAimFactor(interactor);
            float sweep = ComputeSweepFactor(interactor);
            float eff = aim * sweep;
            LastEffectiveness = eff;

            // Charge drains whenever agent is really spraying (waste included).
            if (holdUnit != null)
            {
                holdUnit.DrainCharge(dt);
                if (!holdUnit.HasCharge)
                {
                    UI.ScenarioHUD.Instance?.ShowWarning("EXTINGUISHER EMPTY — release and restart scenario", 3f);
                    Core.SoundEffectManager.Instance?.PlayFailure();
                    OnHoldCancel(interactor);
                    return;
                }
            }

            fireController.Suppress(dt * eff, true);
            UI.ScenarioHUD.Instance?.ShowHold(CurrentHoldProgress, $"Aim {aim * 100f:F0}% • Sweep {sweep * 100f:F0}%");

            if (eff < 0.3f && !lowEffToastShown)
            {
                lowEffToastShown = true;
                UI.ScenarioHUD.Instance?.ShowWarning("Aim at the BASE and SWEEP side-to-side!", 2.5f);
            }
            // Only genuinely ineffective spraying (spraying wide of the fire) counts as
            // waste — a slow but on-target sweep must never be penalised.
            if (eff < wasteEffectiveness)
            {
                wasteTime += dt;
                if (wasteTime > wasteGraceTime && !wasteWarned)
                {
                    wasteWarned = true;
                    scenario?.RegisterUnsafeAction("Wasting extinguisher charge — spraying away from the fire");
                }
            }
            else wasteTime = 0f;

            if (fireController.IsExtinguished && !fireOutNotified)
            {
                fireOutNotified = true;
                UI.ScenarioHUD.Instance?.ShowWarning("FIRE OUT — Go to exit!", 3f);
                // Let scenario manager detect and transition; we keep hold until progress complete
            }
            // If fire out early, continue holding to complete progress for satisfying hold
        }

        public void OnHoldEnd(Interactor interactor, bool completed)
        {
            isHolding = false;
            holdProgress = 0f;
            holdUnit = null;
            if (dischargeParticles != null) dischargeParticles.Stop();
            if (dischargeAudio != null) dischargeAudio.Stop();
            if (sprayBeam != null) sprayBeam.SetActive(false);
            ResolveHeld()?.SetSpraying(false);
            UI.ScenarioHUD.Instance?.HideHold();
            if (completed && fireController.IsExtinguished)
            {
                Core.SoundEffectManager.Instance?.PlaySuccess();
            }
        }

        public void OnHoldCancel(Interactor interactor)
        {
            isHolding = false;
            holdProgress = 0f;
            holdUnit = null;
            if (dischargeParticles != null) dischargeParticles.Stop();
            if (dischargeAudio != null) dischargeAudio.Stop();
            if (sprayBeam != null) sprayBeam.SetActive(false);
            ResolveHeld()?.SetSpraying(false);
            UI.ScenarioHUD.Instance?.HideHold();
        }

        private HeldExtinguisher ResolveHeld()
        {
            var all = FindObjectsByType<HeldExtinguisher>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] : null;
        }

        private FireExtinguisherPickup ResolveHoldUnit()
        {
            if (scenario != null) return scenario.ActivePickup;
            return FindPracticeUnit();
        }

        private FireExtinguisherPickup FindPracticeUnit()
        {
            var all = FindObjectsByType<FireExtinguisherPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in all)
                if (p != null && p.IsPicked) return p;
            return all.Length > 0 ? all[0] : null;
        }

        private bool IsPinBlocking()
        {
            if (scenario != null)
                return scenario.IsCorrectExtinguisherEquipped && !scenario.IsPinRemoved;
            var u = FindPracticeUnit();
            return u != null && u.IsPicked && !u.IsPinRemoved;
        }

        private Vector3 FireBaseCenter()
        {
            if (fireController != null) return fireController.transform.position + Vector3.up * 0.35f;
            return transform.position + Vector3.up * 0.35f;
        }

        /// <summary>
        /// 0..1 aim quality vs the fire base. Measured as the closest approach of the
        /// player's aim ray to the base — NOT the raw hit point — so aiming at the
        /// base through the (large, forgiving) trigger volume scores full marks.
        /// VR-direct holds without a desktop ray are not penalised.
        /// </summary>
        private float ComputeAimFactor(Interactor interactor)
        {
            Vector3 baseCenter = FireBaseCenter();
            if (!TryGetAimRay(interactor, out Vector3 origin, out Vector3 dir)) return 1f;
            Vector3 toBase = baseCenter - origin;
            float t = Vector3.Dot(toBase, dir);
            Vector3 closest = origin + dir * Mathf.Max(t, 0f);
            Vector3 d = closest - baseCenter;
            float err = new Vector2(d.x, d.z).magnitude;
            if (err <= fullAimRadius) return 1f;
            if (err >= zeroAimRadius) return 0f;
            return 1f - (err - fullAimRadius) / Mathf.Max(0.001f, zeroAimRadius - fullAimRadius);
        }

        /// <summary>
        /// minSweepFactor..1 sweep factor from recent lateral motion of the aim ray's
        /// closest-approach point across the fire. Natural side-to-side sweeping
        /// quickly reaches full coverage; holding still still suppresses (slower).
        /// </summary>
        private float ComputeSweepFactor(Interactor interactor)
        {
            Vector3 baseCenter = FireBaseCenter();
            Vector3 trackPoint = baseCenter;
            if (TryGetAimRay(interactor, out Vector3 origin, out Vector3 dir))
            {
                Vector3 toBase = baseCenter - origin;
                float t = Vector3.Dot(toBase, dir);
                trackPoint = origin + dir * Mathf.Max(t, 0f);
            }
            float now = Time.time;
            sweepSamples.Add(new SweepSample { time = now, point = trackPoint });
            while (sweepSamples.Count > 0 && now - sweepSamples[0].time > sweepWindow)
                sweepSamples.RemoveAt(0);
            if (sweepSamples.Count < 2) return 0.75f;
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            foreach (var s in sweepSamples)
            {
                Vector3 o = s.point - baseCenter;
                if (o.x < minX) minX = o.x; if (o.x > maxX) maxX = o.x;
                if (o.z < minZ) minZ = o.z; if (o.z > maxZ) maxZ = o.z;
            }
            float range = Mathf.Sqrt((maxX - minX) * (maxX - minX) + (maxZ - minZ) * (maxZ - minZ));
            return minSweepFactor + (1f - minSweepFactor) * Mathf.Clamp01(range / Mathf.Max(0.001f, fullSweepRange));
        }

        /// <summary>
        /// Desktop aim ray (camera through the current hit point). False for VR-direct
        /// holds, where callers must not be penalised for the missing ray.
        /// </summary>
        private bool TryGetAimRay(Interactor interactor, out Vector3 origin, out Vector3 dir)
        {
            origin = Vector3.zero;
            dir = Vector3.forward;
            if (interactor == null) return false;
            if (interactor.CurrentInteractable != (IInteractable)this) return false;
            var hit = interactor.LastHit;
            if (hit.collider == null) return false;
            origin = interactor.transform.position;
            Vector3 toHit = hit.point - origin;
            if (toHit.sqrMagnitude < 0.0001f) return false;
            dir = toHit.normalized;
            return true;
        }

        private void UpdateSprayBeam(Interactor interactor)
        {
            if (sprayBeam == null) return;
            // Prefer nozzle origin (actual held extinguisher direction) — VR-ready
            Vector3 origin;
            Vector3 target = transform.position + Vector3.up * 0.35f;
            var held = ResolveHeld();
            if (held != null && held.IsActive && held.Nozzle != null)
            {
                origin = held.Nozzle.position;
                Vector3 dir = (target - origin).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = held.Nozzle.forward;
                sprayBeam.transform.position = origin + dir * 0.45f;
                sprayBeam.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                float dist = Vector3.Distance(origin, target);
                sprayBeam.transform.localScale = new Vector3(0.14f, Mathf.Max(0.4f, dist * 0.5f), 0.14f);
                held.SetSpraying(isHolding);
            }
            else if (interactor != null)
            {
                origin = interactor.transform.position;
                Vector3 dir = (target - origin).normalized;
                sprayBeam.transform.position = origin + dir * 0.9f;
                sprayBeam.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90, 0, 0);
                float dist = Vector3.Distance(origin, target);
                sprayBeam.transform.localScale = new Vector3(0.12f, dist * 0.5f, 0.12f);
            }
        }

        public override void OnHoverExit(Interactor interactor)
        {
            if (isHolding) OnHoldCancel(interactor);
            base.OnHoverExit(interactor);
        }
    }
}
