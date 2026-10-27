using UnityEngine;

namespace SafeVR.Scenario
{
    public class HazardController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject sparkVFX;
        [SerializeField] private Light sparkLight;
        [SerializeField] private AudioSource sparkAudio;
        [SerializeField] private FireController fireController;

        [Header("Timing")]
        [SerializeField] private float sparkInterval = 0.12f;

        private bool hazardActive;
        private bool fireStarted;
        private float sparkTimer;

        private void Awake()
        {
            if (sparkVFX != null) sparkVFX.SetActive(false);
            if (sparkLight != null) sparkLight.enabled = false;
        }

        public void ActivateHazard()
        {
            hazardActive = true;
            if (sparkVFX != null) sparkVFX.SetActive(true);
            if (sparkLight != null) sparkLight.enabled = true;
            if (sparkAudio != null && !sparkAudio.isPlaying) sparkAudio.Play();
        }

        public void DeactivateHazard()
        {
            hazardActive = false;
            if (sparkVFX != null) sparkVFX.SetActive(false);
            if (sparkLight != null) sparkLight.enabled = false;
            if (sparkAudio != null) sparkAudio.Stop();
        }

        public void TriggerFire()
        {
            if (fireStarted) return;
            fireStarted = true;
            fireController?.Ignite();
        }

        public bool IsFireStarted => fireStarted;

        private void Update()
        {
            if (!hazardActive) return;
            sparkTimer += Time.deltaTime;
            if (sparkTimer >= sparkInterval)
            {
                sparkTimer = 0f;
                if (sparkLight != null) sparkLight.intensity = Random.Range(2f, 6f);
                if (sparkVFX != null)
                {
                    float s = Random.Range(0.85f, 1.15f);
                    sparkVFX.transform.localScale = Vector3.one * s;
                }
            }
        }
    }
}
