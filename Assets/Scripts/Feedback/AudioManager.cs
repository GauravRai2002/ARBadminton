using UnityEngine;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Feedback
{
    /// <summary>
    /// Manages audio playback for net collision events
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Clips")]
        [SerializeField] private AudioClip netHitSound;
        [SerializeField] private AudioClip placementConfirmSound;
        
        [Header("Settings")]
        [SerializeField] private float volume = 1.5f; // Increased for louder audio
        [SerializeField] private bool useDifferentTonesPerSide = false;
        [SerializeField] private float sideAPitch = 1.0f;
        [SerializeField] private float sideBPitch = 0.8f;
        
        private AudioSource audioSource;
        private ProceduralAudioGenerator proceduralAudio;
        
        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.volume = volume;
            audioSource.playOnAwake = false;
            
            // Get or add procedural audio generator as fallback
            proceduralAudio = GetComponent<ProceduralAudioGenerator>();
            if (proceduralAudio == null)
            {
                proceduralAudio = gameObject.AddComponent<ProceduralAudioGenerator>();
                Debug.Log("[AudioManager] Added ProceduralAudioGenerator for fallback audio");
            }
        }
        
        public void PlayNetHitSound(NetSide side)
        {
            // Use audio clip if assigned
            if (netHitSound != null)
            {
                // Adjust pitch based on side if enabled
                if (useDifferentTonesPerSide)
                {
                    audioSource.pitch = side == NetSide.SideA ? sideAPitch : sideBPitch;
                }
                else
                {
                    audioSource.pitch = 1.0f;
                }
                
                audioSource.PlayOneShot(netHitSound);
                Debug.Log($"Playing net hit sound (clip) for {side}");
            }
            else
            {
                // Fallback to procedural audio
                if (proceduralAudio != null)
                {
                    // Use different frequencies for different sides
                    float frequency = side == NetSide.SideA ? 550f : 440f;
                    proceduralAudio.PlayBeep(frequency);
                    Debug.Log($"Playing procedural beep ({frequency}Hz) for {side}");
                }
                else
                {
                    Debug.LogWarning("No audio source available (neither clip nor procedural)");
                }
            }
        }
        
        public void PlayPlacementConfirmSound()
        {
            if (placementConfirmSound == null)
            {
                Debug.LogWarning("Placement confirm sound not assigned");
                return;
            }
            
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(placementConfirmSound);
        }
        
        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
            audioSource.volume = volume;
        }
        
        public void Mute()
        {
            audioSource.mute = true;
        }
        
        public void Unmute()
        {
            audioSource.mute = false;
        }
    }
}
