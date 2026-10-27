using UnityEngine;

namespace SafeVR.Scenario
{
    public class FireController : MonoBehaviour
    {
        [Header("Fire Visual (new particle system)")]
        [SerializeField] private ParticleSystem fireParticles; // FirePS
        [SerializeField] private GameObject emberMesh; // shown briefly after extinguish
        [SerializeField] private Light fireLight;
        // Legacy cube fallback (hidden once FirePS exists)
        [SerializeField] private GameObject fireMesh;

        [Header("Settings")]
        [Range(0f, 100f)] public float intensity = 0f;
        [SerializeField] private float growthRate = 6f;
        [SerializeField] private float suppressRate = 38f;
        [SerializeField] private float maxIntensity = 100f;

        private bool isBurning;

        public bool IsExtinguished => intensity <= 1f;
        public bool IsCritical => intensity >= 85f;
        public bool IsBurning => isBurning;

        private void Awake()
        {
            if (fireParticles == null) fireParticles = GetComponentInChildren<ParticleSystem>();
            if (fireMesh != null) fireMesh.SetActive(false);
            if (emberMesh != null) emberMesh.SetActive(false);
            if (fireLight != null) fireLight.enabled = false;
            if (fireParticles != null) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Ignite()
        {
            isBurning = true;
            intensity = 18f;
            if (fireMesh != null) fireMesh.SetActive(false); // cube deprecated
            if (emberMesh != null) emberMesh.SetActive(false);
            if (fireLight != null) fireLight.enabled = true;
            if (fireParticles != null) fireParticles.Play();
        }

        public void Extinguish()
        {
            isBurning = false;
            intensity = 0f;
            if (fireMesh != null) fireMesh.SetActive(false);
            if (fireLight != null) fireLight.enabled = false;
            if (fireParticles != null) fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (emberMesh != null) emberMesh.SetActive(true); // embers transition
        }

        public void AddIntensity(float amount)
        {
            intensity = Mathf.Clamp(intensity + amount, 0f, maxIntensity);
        }

        private void Update()
        {
            if (!isBurning) return;

            // Auto growth over time (simulates spreading if not suppressed)
            if (intensity > 0f && intensity < maxIntensity)
            {
                intensity += growthRate * Time.deltaTime * (intensity > 65f ? 1.6f : 1f); // accelerate when large
                intensity = Mathf.Clamp(intensity, 0f, maxIntensity);
            }

            // Particle emission scales with intensity + flicker
            if (fireParticles != null)
            {
                var emission = fireParticles.emission;
                float flick = 1f + Mathf.Sin(Time.time * 13f) * 0.12f + Mathf.Sin(Time.time * 31f) * 0.06f;
                float rate = Mathf.Lerp(8f, 90f, intensity / maxIntensity) * flick;
                emission.rateOverTime = rate;
                var main = fireParticles.main;
                main.startSize = Mathf.Lerp(0.12f, 0.42f, intensity / maxIntensity);
                main.startSpeed = Mathf.Lerp(0.5f, 1.4f, intensity / maxIntensity);
            }
            if (fireLight != null)
            {
                fireLight.intensity = Mathf.Lerp(0.6f, 7f, intensity / maxIntensity) * (0.9f + Mathf.Sin(Time.time * 9f) * 0.12f + Mathf.Sin(Time.time * 23f) * 0.06f);
                fireLight.range = Mathf.Lerp(3f, 9f, intensity / maxIntensity);
            }
        }

        /// <summary>
        /// Called by extinguisher ray while aiming correctly and power isolated.
        /// Returns true if suppression succeeded this frame.
        /// </summary>
        public bool Suppress(float dt, bool powerIsolated)
        {
            if (!isBurning || intensity <= 0f) return false;
            if (!powerIsolated)
            {
                // Wrong to spray electrical fire with power on -> ineffective, still grows, also unsafe
                AddIntensity(6f * dt);
                return false;
            }
            intensity -= suppressRate * dt;
            intensity = Mathf.Max(0f, intensity);
            if (intensity <= 1f)
            {
                Extinguish();
                return true;
            }
            return false;
        }
    }
}
