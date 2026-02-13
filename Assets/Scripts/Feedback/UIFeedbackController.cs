using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ARBadmintonNet.Models;

namespace ARBadmintonNet.Feedback
{
    /// <summary>
    /// Manages UI feedback display when shuttle hits the net
    /// </summary>
    public class UIFeedbackController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI feedbackText;
        [SerializeField] private GameObject feedbackPanel;
        [SerializeField] private Image backgroundImage;
        
        [Header("Display Settings")]
        [SerializeField] private float displayDuration = 2f;
        [SerializeField] private float fadeInDuration = 0.2f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color sideAColor = Color.green;
        [SerializeField] private Color sideBColor = Color.blue;
        
        private CanvasGroup canvasGroup;
        private float displayTimer = 0f;
        private bool isDisplaying = false;
        
        private void Awake()
        {
            // Get or add canvas group for fading
            if (feedbackPanel != null)
            {
                canvasGroup = feedbackPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = feedbackPanel.AddComponent<CanvasGroup>();
                    
                feedbackPanel.SetActive(false);
            }
        }
        
        private void Update()
        {
            if (isDisplaying)
            {
                displayTimer += Time.deltaTime;
                
                // Handle fade in/out
                if (displayTimer < fadeInDuration)
                {
                    // Fade in
                    float alpha = displayTimer / fadeInDuration;
                    canvasGroup.alpha = alpha;
                }
                else if (displayTimer > displayDuration - fadeOutDuration)
                {
                    // Fade out
                    float fadeOutProgress = (displayTimer - (displayDuration - fadeOutDuration)) / fadeOutDuration;
                    canvasGroup.alpha = 1f - fadeOutProgress;
                }
                else
                {
                    // Full opacity
                    canvasGroup.alpha = 1f;
                }
                
                // Hide when done
                if (displayTimer >= displayDuration)
                {
                    HideMessage();
                }
            }
        }
        
        public void ShowHitMessage(string message, NetSide side)
        {
            if (feedbackPanel == null || feedbackText == null)
            {
                Debug.LogWarning("UI feedback components not assigned");
                return;
            }
            
            // Set text
            feedbackText.text = message;
            
            // Set color based on side
            Color textColor = side == NetSide.SideA ? sideAColor : sideBColor;
            feedbackText.color = textColor;
            
            if (backgroundImage != null)
            {
                Color bgColor = textColor;
                bgColor.a = 0.3f; // Semi-transparent background
                backgroundImage.color = bgColor;
            }
            
            // Show panel
            feedbackPanel.SetActive(true);
            canvasGroup.alpha = 0f;
            
            // Start timer
            displayTimer = 0f;
            isDisplaying = true;
            
            Debug.Log($"Showing UI message: {message}");
        }
        
        public void ShowMessage(string message, Color color)
        {
            if (feedbackPanel == null || feedbackText == null)
            {
                Debug.LogWarning("UI feedback components not assigned");
                return;
            }
            
            feedbackText.text = message;
            feedbackText.color = color;
            
            if (backgroundImage != null)
            {
                Color bgColor = color;
                bgColor.a = 0.3f;
                backgroundImage.color = bgColor;
            }
            
            feedbackPanel.SetActive(true);
            canvasGroup.alpha = 0f;
            
            displayTimer = 0f;
            isDisplaying = true;
        }
        
        private void HideMessage()
        {
            isDisplaying = false;
            if (feedbackPanel != null)
                feedbackPanel.SetActive(false);
        }
        
        public void SetDisplayDuration(float duration)
        {
            displayDuration = duration;
        }
    }
}
