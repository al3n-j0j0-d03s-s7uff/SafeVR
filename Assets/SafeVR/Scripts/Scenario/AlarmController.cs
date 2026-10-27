using UnityEngine;

namespace SafeVR.Scenario
{
    public class AlarmController : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Light alarmLight;
        [SerializeField] private Renderer alarmRenderer;
        [SerializeField] private Color alarmColor = new Color(1f, 0.15f, 0.15f);
        [SerializeField] private float flashSpeed = 2.2f;

        [Header("Audio")]
        [SerializeField] private AudioSource alarmSource;
        private AudioClip alarmClip;

        private bool isActive;
        private float flashTimer;
        private Color baseEmission = Color.black;

        private void Awake()
        {
            if (alarmSource == null) alarmSource = gameObject.AddComponent<AudioSource>();
            alarmSource.loop = true;
            alarmSource.playOnAwake = false;
            alarmSource.spatialBlend = 0.7f;
            alarmSource.volume = 0.6f;
            GenerateAlarmClip();

            if (alarmLight != null) alarmLight.enabled = false;
        }

        private void GenerateAlarmClip()
        {
            int sr = 44100;
            float duration = 0.6f;
            int samples = Mathf.CeilToInt(sr * duration);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sr;
                float env = (i < samples * 0.5f) ? 1f : 0f; // on/off beep
                float tone = Mathf.Sin(2 * Mathf.PI * 900f * t) * 0.3f * env;
                data[i] = tone;
            }
            alarmClip = AudioClip.Create("Alarm", samples, 1, sr, false);
            alarmClip.SetData(data, 0);
            alarmSource.clip = alarmClip;
        }

        public void SetActive(bool active)
        {
            isActive = active;
            if (alarmLight != null) alarmLight.enabled = active;
            if (active && alarmClip != null && !alarmSource.isPlaying) alarmSource.Play();
            if (!active) alarmSource.Stop();
        }

        private void Update()
        {
            if (!isActive) return;
            flashTimer += Time.deltaTime * flashSpeed;
            float pulse = Mathf.PingPong(flashTimer, 1f);
            if (alarmLight != null) alarmLight.intensity = Mathf.Lerp(1f, 6f, pulse);
            if (alarmRenderer != null && alarmRenderer.material.HasProperty("_EmissionColor"))
            {
                alarmRenderer.material.EnableKeyword("_EMISSION");
                alarmRenderer.material.SetColor("_EmissionColor", alarmColor * Mathf.Lerp(0.5f, 3f, pulse));
            }
        }
    }
}
