using UnityEngine;

namespace ARBadmintonNet.Feedback
{
    /// <summary>
    /// Generates simple beep sounds procedurally at runtime.
    /// No audio files needed - creates waveforms in code.
    /// </summary>
    public class ProceduralAudioGenerator : MonoBehaviour
    {
        [Header("Beep Settings")]
        [SerializeField] private float frequency = 440f; // Hz (A4 note)
        [SerializeField] private float duration = 0.15f; // seconds
        [SerializeField] private int sampleRate = 44100;
        
        private AudioSource audioSource;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            audioSource.playOnAwake = false;
        }
        
        /// <summary>
        /// Generate and play a beep sound with the given frequency
        /// </summary>
        public void PlayBeep(float customFrequency = 0f)
        {
            float freq = customFrequency > 0 ? customFrequency : frequency;
            AudioClip beep = GenerateBeep(freq, duration);
            audioSource.PlayOneShot(beep);
        }
        
        /// <summary>
        /// Generate a beep AudioClip with a sine wave
        /// </summary>
        private AudioClip GenerateBeep(float frequency, float duration)
        {
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            AudioClip clip = AudioClip.Create("ProceduralBeep", sampleCount, 1, sampleRate, false);
            
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                
                // Sine wave
                float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
                
                // Apply envelope (fade in/out to avoid clicks)
                float envelope = 1f;
                float fadeTime = 0.01f; // 10ms fade
                int fadeSamples = Mathf.CeilToInt(sampleRate * fadeTime);
                
                if (i < fadeSamples)
                {
                    // Fade in
                    envelope = (float)i / fadeSamples;
                }
                else if (i > sampleCount - fadeSamples)
                {
                    // Fade out
                    envelope = (float)(sampleCount - i) / fadeSamples;
                }
                
                samples[i] = sine * envelope * 1.0f; // Full volume for louder beep
            }
            
            clip.SetData(samples, 0);
            return clip;
        }
        
        /// <summary>
        /// Generate a click/pop sound (very short beep)
        /// </summary>
        public void PlayClick()
        {
            AudioClip click = GenerateBeep(1000f, 0.05f);
            audioSource.PlayOneShot(click);
        }
    }
}
