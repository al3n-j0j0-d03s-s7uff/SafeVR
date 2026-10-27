using UnityEngine;

namespace SafeVR.Core
{
    /// <summary>
    /// Procedural audio generator and player for UI clicks, task completions, and alerts.
    /// Eliminates external asset dependencies while providing clear auditory feedback.
    /// </summary>
    public class SoundEffectManager : MonoBehaviour
    {
        public static SoundEffectManager Instance { get; private set; }

        private AudioSource audioSource;
        private AudioClip clickClip;
        private AudioClip successClip;
        private AudioClip failureClip;
        private AudioClip interactClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D

            GenerateAudioClips();
        }

        private void GenerateAudioClips()
        {
            clickClip = CreateToneClip("Click", 800f, 0.05f, 0.2f);
            interactClip = CreateToneClip("Interact", 550f, 0.1f, 0.3f);
            successClip = CreateArpeggioClip("Success", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.08f);
            failureClip = CreateArpeggioClip("Failure", new float[] { 440f, 415.30f, 392f, 349.23f }, 0.12f);
        }

        public void PlayClick()
        {
            if (audioSource != null && clickClip != null)
                audioSource.PlayOneShot(clickClip, 0.4f);
        }

        public void PlayInteract()
        {
            if (audioSource != null && interactClip != null)
                audioSource.PlayOneShot(interactClip, 0.5f);
        }

        public void PlaySuccess()
        {
            if (audioSource != null && successClip != null)
                audioSource.PlayOneShot(successClip, 0.6f);
        }

        public void PlayFailure()
        {
            if (audioSource != null && failureClip != null)
                audioSource.PlayOneShot(failureClip, 0.6f);
        }

        private AudioClip CreateToneClip(string name, float frequency, float duration, float volume)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1f - ((float)i / sampleCount); // linear fade out
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip CreateArpeggioClip(string name, float[] frequencies, float noteDuration)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.CeilToInt(sampleRate * noteDuration * frequencies.Length);
            float[] samples = new float[totalSamples];
            int noteSamples = Mathf.CeilToInt(sampleRate * noteDuration);

            for (int n = 0; n < frequencies.Length; n++)
            {
                float freq = frequencies[n];
                int offset = n * noteSamples;

                for (int i = 0; i < noteSamples && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1f - ((float)i / noteSamples);
                    samples[offset + i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.35f;
                }
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
