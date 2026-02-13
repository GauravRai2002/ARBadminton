using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System.Collections;

namespace ARBadmintonNet.Replay
{
    /// <summary>
    /// UI for instant replay with manual record control.
    /// Shows a record toggle button and a replay button when recording.
    /// </summary>
    public class ReplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ReplayManager replayManager;
        
        private Canvas canvas;
        private GameObject recordButton;
        private TextMeshProUGUI recordBtnText;
        private Image recordBtnImage;
        private GameObject replayButton;
        private GameObject videoOverlay;
        private VideoPlayer videoPlayer;
        private RawImage videoDisplay;
        private RenderTexture renderTexture;
        private GameObject loadingIndicator;
        private string currentClipPath;
        private bool isSetup = false;
        private bool isRecording = false;
        
        // Colors
        private static readonly Color recordOffColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);
        private static readonly Color recordOnColor = new Color(0.85f, 0.2f, 0.2f, 0.85f);
        private static readonly Color replayBtnColor = new Color(0.18f, 0.45f, 0.85f, 0.85f);
        private static readonly Color closeBtnColor = new Color(0.25f, 0.25f, 0.3f, 0.85f);
        
        private void Awake()
        {
            if (replayManager == null)
                replayManager = FindObjectOfType<ReplayManager>();
        }
        
        private void Start()
        {
            SetupUI();
            HideAll();
        }
        
        private void OnEnable()
        {
            if (replayManager != null)
            {
                replayManager.OnReplayReady += OnReplayReady;
                replayManager.OnReplayError += OnReplayError;
            }
        }
        
        private void OnDisable()
        {
            if (replayManager != null)
            {
                replayManager.OnReplayReady -= OnReplayReady;
                replayManager.OnReplayError -= OnReplayError;
            }
        }
        
        private void SetupUI()
        {
            if (isSetup) return;
            
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ReplayUI] No Canvas found - will retry later");
                return;
            }
            
            isSetup = true;
            Debug.Log("[ReplayUI] SetupUI - creating buttons");
            CreateRecordButton();
            CreateReplayButton();
            CreateVideoOverlay();
        }
        
        private void CreateRecordButton()
        {
            // Record toggle — top-left, always visible during modes
            recordButton = new GameObject("RecordBtn");
            recordButton.transform.SetParent(canvas.transform, false);
            
            var rt = recordButton.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(90, -140); // Increased margin
            rt.sizeDelta = new Vector2(180, 70); // Larger touch target
            
            recordBtnImage = recordButton.AddComponent<Image>();
            recordBtnImage.color = recordOffColor;
            
            var btn = recordButton.AddComponent<Button>();
            btn.onClick.AddListener(OnRecordToggle);
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(recordButton.transform, false);
            
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            recordBtnText = textGO.AddComponent<TextMeshProUGUI>();
            recordBtnText.text = "[REC] Record";
            recordBtnText.fontSize = 24; // Larger text
            recordBtnText.alignment = TextAlignmentOptions.Center;
            recordBtnText.color = Color.white;
            recordBtnText.fontStyle = FontStyles.Bold;
        }
        
        private void CreateReplayButton()
        {
            // Replay button — below record button, only visible when recording
            replayButton = new GameObject("ReplayBtn");
            replayButton.transform.SetParent(canvas.transform, false);
            
            var rt = replayButton.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(90, -220); // Increased margin
            rt.sizeDelta = new Vector2(180, 70); // Larger touch target
            
            var img = replayButton.AddComponent<Image>();
            img.color = replayBtnColor;
            
            var btn = replayButton.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = replayBtnColor;
            colors.highlightedColor = new Color(0.28f, 0.55f, 0.95f, 0.9f);
            colors.pressedColor = new Color(0.35f, 0.6f, 1f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(OnReplayButtonPressed);
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(replayButton.transform, false);
            
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "<< Replay";
            tmp.fontSize = 24; // Larger text
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
        }
        
        private void CreateVideoOverlay()
        {
            videoOverlay = new GameObject("VideoOverlay");
            videoOverlay.transform.SetParent(canvas.transform, false);
            
            var rt = videoOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            var bg = videoOverlay.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.97f);
            
            // Video display
            var videoGO = new GameObject("VideoDisplay");
            videoGO.transform.SetParent(videoOverlay.transform, false);
            
            var videoRT = videoGO.AddComponent<RectTransform>();
            videoRT.anchorMin = new Vector2(0.03f, 0.12f);
            videoRT.anchorMax = new Vector2(0.97f, 0.85f);
            videoRT.offsetMin = Vector2.zero;
            videoRT.offsetMax = Vector2.zero;
            
            videoDisplay = videoGO.AddComponent<RawImage>();
            videoDisplay.color = Color.white;
            
            videoPlayer = videoGO.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.isLooping = false;
            videoPlayer.loopPointReached += OnVideoFinished;
            
            // Title
            CreateOverlayLabel(videoOverlay.transform, "Title",
                "<< Instant Replay", 30,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -70), new Vector2(400, 50),
                Color.white);
            
            // Close button
            var closeBtnGO = new GameObject("CloseBtn");
            closeBtnGO.transform.SetParent(videoOverlay.transform, false);
            
            var closeRT = closeBtnGO.AddComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(0.5f, 0);
            closeRT.anchorMax = new Vector2(0.5f, 0);
            closeRT.anchoredPosition = new Vector2(0, 70);
            closeRT.sizeDelta = new Vector2(200, 55);
            
            var closeImg = closeBtnGO.AddComponent<Image>();
            closeImg.color = closeBtnColor;
            
            var closeBtn = closeBtnGO.AddComponent<Button>();
            var closeColors = closeBtn.colors;
            closeColors.normalColor = closeBtnColor;
            closeColors.highlightedColor = new Color(0.35f, 0.35f, 0.4f, 0.9f);
            closeColors.pressedColor = new Color(0.45f, 0.45f, 0.5f, 1f);
            closeBtn.colors = closeColors;
            closeBtn.onClick.AddListener(CloseReplay);
            
            CreateOverlayLabel(closeBtnGO.transform, "Text",
                "X Close", 22,
                Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero,
                Color.white);
            
            // Loading indicator
            loadingIndicator = new GameObject("Loading");
            loadingIndicator.transform.SetParent(videoOverlay.transform, false);
            
            var loadRT = loadingIndicator.AddComponent<RectTransform>();
            loadRT.anchorMin = new Vector2(0.5f, 0.5f);
            loadRT.anchorMax = new Vector2(0.5f, 0.5f);
            loadRT.anchoredPosition = Vector2.zero;
            loadRT.sizeDelta = new Vector2(400, 50);
            
            var loadTmp = loadingIndicator.AddComponent<TextMeshProUGUI>();
            loadTmp.text = "Preparing replay...";
            loadTmp.fontSize = 26;
            loadTmp.alignment = TextAlignmentOptions.Center;
            loadTmp.color = new Color(1, 1, 1, 0.6f);
            
            videoOverlay.SetActive(false);
        }
        
        private void CreateOverlayLabel(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            if (size == Vector2.zero) { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
        }
        
        // ====== RECORD TOGGLE ======
        
        private void OnRecordToggle()
        {
            if (replayManager == null) return;
            
            if (isRecording)
            {
                // Stop recording
                replayManager.StopBuffering();
                isRecording = false;
                UpdateRecordButtonState();
                if (replayButton != null) replayButton.SetActive(false);
                Debug.Log("[ReplayUI] Recording stopped by user");
            }
            else
            {
                // Start recording
                replayManager.StartBuffering();
                isRecording = true;
                UpdateRecordButtonState();
                if (replayButton != null) replayButton.SetActive(true);
                Debug.Log("[ReplayUI] Recording started by user");
            }
        }
        
        private void UpdateRecordButtonState()
        {
            if (recordBtnImage != null)
                recordBtnImage.color = isRecording ? recordOnColor : recordOffColor;
            
            if (recordBtnText != null)
                recordBtnText.text = isRecording ? "[STOP]" : "[REC] Record";
            
            // Update button colors
            var btn = recordButton?.GetComponent<Button>();
            if (btn != null)
            {
                var c = btn.colors;
                c.normalColor = isRecording ? recordOnColor : recordOffColor;
                c.highlightedColor = isRecording 
                    ? new Color(0.95f, 0.3f, 0.3f, 0.9f) 
                    : new Color(0.22f, 0.22f, 0.28f, 0.9f);
                btn.colors = c;
            }
        }
        
        // ====== REPLAY ======
        
        private void OnReplayButtonPressed()
        {
            if (replayManager == null || replayManager.IsExporting || !isRecording) return;
            
            videoOverlay.SetActive(true);
            loadingIndicator.SetActive(true);
            
            // Do NOT disable the GameObject, otherwise VideoPlayer component is disabled too!
            // Just disable the RawImage component so it's invisible until ready
            if (videoDisplay != null) videoDisplay.enabled = false;
            
            replayManager.ExportReplay();
        }
        
        private void OnReplayReady(string videoPath)
        {
            currentClipPath = videoPath;
            Debug.Log($"[ReplayUI] Playing replay: {videoPath}");
            StartCoroutine(PlayVideo(videoPath));
        }
        
        private IEnumerator PlayVideo(string videoPath)
        {
            if (renderTexture != null) renderTexture.Release();
            renderTexture = new RenderTexture(Screen.width, Screen.height, 0);
            renderTexture.Create();
            
            videoPlayer.targetTexture = renderTexture;
            videoDisplay.texture = renderTexture;
            
            videoPlayer.url = "file://" + videoPath;
            videoPlayer.Prepare();
            
            float timeout = 10f;
            float elapsed = 0f;
            while (!videoPlayer.isPrepared && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (!videoPlayer.isPrepared)
            {
                loadingIndicator.GetComponent<TextMeshProUGUI>().text = "Failed to load replay";
                yield return new WaitForSeconds(2f);
                CloseReplay();
                yield break;
            }
            
            loadingIndicator.SetActive(false);
            
            // Enable RawImage now that video is ready
            if (videoDisplay != null) videoDisplay.enabled = true;
            
            videoDisplay.gameObject.SetActive(true);
            videoPlayer.Play();
        }
        
        private void OnReplayError(string error)
        {
            Debug.LogError($"[ReplayUI] Replay error: {error}");
            if (loadingIndicator != null)
            {
                var tmp = loadingIndicator.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = $"Replay unavailable\n{error}";
                StartCoroutine(AutoCloseAfterDelay(3f));
            }
        }
        
        private IEnumerator AutoCloseAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            CloseReplay();
        }
        
        private void OnVideoFinished(VideoPlayer vp)
        {
            Debug.Log("[ReplayUI] Replay finished");
        }
        
        public void CloseReplay()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Stop();
            
            if (videoOverlay != null)
                videoOverlay.SetActive(false);
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
            
            DeleteCurrentClip();
        }
        
        private void DeleteCurrentClip()
        {
            if (!string.IsNullOrEmpty(currentClipPath))
            {
                try
                {
                    if (System.IO.File.Exists(currentClipPath))
                    {
                        System.IO.File.Delete(currentClipPath);
                        Debug.Log($"[ReplayUI] Deleted clip: {currentClipPath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ReplayUI] Failed to delete clip: {e.Message}");
                }
                currentClipPath = null;
            }
        }
        
        // ====== PUBLIC API ======
        
        /// <summary>Show the record button when entering a mode.</summary>
        public void ShowRecordButton()
        {
            // Retry setup if it failed earlier (e.g. canvas didn't exist yet)
            if (!isSetup) SetupUI();
            
            Debug.Log($"[ReplayUI] ShowRecordButton called. isSetup={isSetup}, recordButton={recordButton != null}");
            
            if (recordButton != null) recordButton.SetActive(true);
            // Replay button only visible when actively recording
            if (replayButton != null) replayButton.SetActive(isRecording);
        }
        
        /// <summary>Hide everything when returning to startup.</summary>
        public void HideAll()
        {
            // Stop recording if active
            if (isRecording && replayManager != null)
            {
                replayManager.StopBuffering();
                isRecording = false;
                UpdateRecordButtonState();
            }
            
            if (recordButton != null) recordButton.SetActive(false);
            if (replayButton != null) replayButton.SetActive(false);
        }
        
        // Keep old API names working
        public void ShowReplayButton() => ShowRecordButton();
        public void HideReplayButton() => HideAll();
        
        private void OnDestroy()
        {
            DeleteCurrentClip();
            
            if (isRecording && replayManager != null)
                replayManager.StopBuffering();
            
            if (renderTexture != null)
            {
                renderTexture.Release();
                renderTexture = null;
            }
            
            if (videoPlayer != null)
                videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}
