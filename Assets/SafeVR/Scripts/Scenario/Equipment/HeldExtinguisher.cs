using UnityEngine;

namespace SafeVR.Scenario.Equipment
{
    /// <summary>
    /// Phase 3 — Marker + nozzle + pin/gauge visuals for the in-hand extinguisher
    /// (HeldExtinguisher under WeaponHold). Charge/pin STATE lives on the active
    /// <see cref="FireExtinguisherPickup"/> station unit; this component only mirrors
    /// visuals and owns the spray anchor, so VR controllers can drive the same flow.
    /// </summary>
    public class HeldExtinguisher : MonoBehaviour
    {
        [Header("Nozzle")]
        [SerializeField] private Transform nozzle;
        [SerializeField] private ParticleSystem sprayParticles;

        [Header("Phase 3 — Pin & Charge Visuals (wired by builder)")]
        [SerializeField] private GameObject pinVisual;
        [SerializeField] private Renderer gaugeRenderer;

        public Transform Nozzle => nozzle;
        public ParticleSystem SprayParticles => sprayParticles;
        public bool IsActive => gameObject.activeSelf;

        private void Awake()
        {
            if (nozzle == null) nozzle = transform.Find("HornNozzle");
            if (sprayParticles == null) sprayParticles = GetComponentInChildren<ParticleSystem>();
            if (pinVisual == null)
            {
                var pin = transform.Find("Pin");
                if (pin != null) pinVisual = pin.gameObject;
            }
            if (gaugeRenderer == null)
            {
                var gauge = transform.Find("Gauge");
                if (gauge != null) gaugeRenderer = gauge.GetComponent<Renderer>();
            }
            if (sprayParticles != null) sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void SetSpraying(bool spraying)
        {
            if (sprayParticles == null) return;
            if (spraying && !sprayParticles.isPlaying) sprayParticles.Play();
            if (!spraying && sprayParticles.isPlaying) sprayParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Mirror of the active unit's pin state: pin visible until pulled.</summary>
        public void SetPinRemoved(bool removed)
        {
            if (pinVisual == null)
            {
                var pin = transform.Find("Pin");
                if (pin != null) pinVisual = pin.gameObject;
            }
            if (pinVisual != null) pinVisual.SetActive(!removed);
            var ring = transform.Find("PinRing");
            if (ring != null) ring.gameObject.SetActive(!removed);
        }

        /// <summary>Mirror of the active unit's remaining charge (0..1): green → yellow → red.</summary>
        public void SetCharge01(float charge01)
        {
            if (gaugeRenderer == null)
            {
                var gauge = transform.Find("Gauge");
                if (gauge != null) gaugeRenderer = gauge.GetComponent<Renderer>();
            }
            if (gaugeRenderer == null) return;
            float c = Mathf.Clamp01(charge01);
            Color col = c > 0.5f
                ? Color.Lerp(Color.yellow, Color.green, (c - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, c * 2f);
            // material is a per-gauge instance created by the builder — safe to tint.
            if (gaugeRenderer.material != null && gaugeRenderer.material.HasProperty("_BaseColor"))
                gaugeRenderer.material.SetColor("_BaseColor", col);
            else if (gaugeRenderer.material != null && gaugeRenderer.material.HasProperty("_Color"))
                gaugeRenderer.material.SetColor("_Color", col);
        }
    }
}
