using UnityEngine;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Feedback
{
    /// <summary>
    /// Central manager for all feedback (audio, visual, UI) when net is hit
    /// </summary>
    public class FeedbackManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private UIFeedbackController uiController;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject hitIndicatorPrefab;
        [SerializeField] private float indicatorLifetime = 2f;
        
        [Header("Feedback Settings")]
        [SerializeField] private float feedbackCooldown = 0.5f; // Prevent spam
        
        private float lastFeedbackTime = -999f;
        
        private void Awake()
        {
            if (audioManager == null)
                audioManager = GetComponent<AudioManager>();
                
            if (uiController == null)
                uiController = GetComponent<UIFeedbackController>();
        }
        
        public void OnNetHit(CollisionEvent collisionEvent)
        {
            // Check cooldown to prevent feedback spam
            if (Time.time - lastFeedbackTime < feedbackCooldown)
                return;
            
            lastFeedbackTime = Time.time;
            
            Debug.Log($"Feedback triggered - Side: {collisionEvent.Side}, Impact: {collisionEvent.ImpactVelocity:F2} m/s");
            
            // ONLY audio feedback (text and visual disabled)
            PlayAudioFeedback(collisionEvent);
            
            // TEXT FEEDBACK DISABLED
            // ShowUIFeedback(collisionEvent);
            
            // VISUAL FEEDBACK DISABLED  
            // ShowVisualFeedback(collisionEvent);
        }
        
        private void PlayAudioFeedback(CollisionEvent collisionEvent)
        {
            if (audioManager != null)
            {
                audioManager.PlayNetHitSound(collisionEvent.Side);
            }
        }
        
        private void ShowVisualFeedback(CollisionEvent collisionEvent)
        {
            if (hitIndicatorPrefab != null)
            {
                // Instantiate visual indicator at collision point
                GameObject indicator = Instantiate(hitIndicatorPrefab, collisionEvent.CollisionPoint, Quaternion.identity);
                
                // Set color based on side
                Color indicatorColor = collisionEvent.Side == NetSide.SideA ? Color.green : Color.blue;
                var renderer = indicator.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = indicatorColor;
                }
                
                // Destroy after lifetime
                Destroy(indicator, indicatorLifetime);
            }
        }
        
        private void ShowUIFeedback(CollisionEvent collisionEvent)
        {
            if (uiController != null)
            {
                string sideName = collisionEvent.Side == NetSide.SideA ? "A" : "B";
                uiController.ShowHitMessage($"Side {sideName} Hit!", collisionEvent.Side);
            }
        }
    }
}
