using UnityEngine;

namespace SafeVR.Scenario
{
    public class SmokeController : MonoBehaviour
    {
        [Header("Smoke Visual (new particle system)")]
        [SerializeField] private ParticleSystem smokeParticles; // SmokePS
        [SerializeField] private Light smokeLight;
        // Legacy cube fallback
        [SerializeField] private GameObject smokeVolume;

        private bool active;
        private float density;

        private void Awake()
        {
            if (smokeParticles == null) smokeParticles = GetComponentInChildren<ParticleSystem>();
            if (smokeVolume != null) smokeVolume.SetActive(false);
            if (smokeParticles != null) smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            density = 0f;
        }

        public void SetActive(bool on)
        {
            active = on;
            if (smokeParticles != null)
            {
                if (on) smokeParticles.Play();
                else if (density <= 0.02f) smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void SetDensity(float normalizedFromFire) // 0..1
        {
            density = Mathf.Clamp01(normalizedFromFire);
            if (smokeParticles != null)
            {
                var emission = smokeParticles.emission;
                emission.rateOverTime = Mathf.Lerp(0f, 28f, density);
                if (density > 0.01f && !smokeParticles.isPlaying) smokeParticles.Play();
                if (density <= 0.01f && !active) smokeParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (smokeVolume != null) smokeVolume.SetActive(false); // cube deprecated
            if (smokeLight != null)
            {
                smokeLight.intensity = density * 0.6f;
            }
        }

        private void Update()
        {
            if (!active && density > 0f)
            {
                density = Mathf.MoveTowards(density, 0f, Time.deltaTime * 0.15f);
                SetDensity(density);
            }
        }
    }
}
