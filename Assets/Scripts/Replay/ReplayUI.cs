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
        private bool isWaitingForPermission = false;
        private GameObject permissionAlert;
        
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
                replayManager.OnBufferingStarted += OnBufferingConfirmed;
                replayManager.OnBufferingFailed += OnBufferingDenied;
            }
        }
        
        private void OnDisable()
        {
            if (replayManager != null)
            {
                replayManager.OnReplayReady -= OnReplayReady;
                replayManager.OnReplayError -= OnReplayError;
                replayManager.OnBufferingStarted -= OnBufferingConfirmed;
                replayManager.OnBufferingFailed -= OnBufferingDenied;
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
            CreatePermissionAlert();
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
        
        // ====== VIDEO OVERLAY & CONTROLS ======
        
        private Slider seekSlider;
        private Image playPauseImg;
        private TextMeshProUGUI speedText;
        private bool isDraggingSlider = false;
        private float currentSpeed = 1.0f;
        
        private void CreateVideoOverlay()
        {
            videoOverlay = new GameObject("VideoOverlay");
            videoOverlay.transform.SetParent(canvas.transform, false);
            videoOverlay.transform.SetAsLastSibling(); // Ensure on top
            
            var rt = videoOverlay.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            var bg = videoOverlay.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 1.0f); // Fully opaque
            
            // Video display area (Top 70%)
            var videoGO = new GameObject("VideoDisplay");
            videoGO.transform.SetParent(videoOverlay.transform, false);
            
            var videoRT = videoGO.AddComponent<RectTransform>();
            videoRT.anchorMin = new Vector2(0, 0.22f); // increased video height
            videoRT.anchorMax = new Vector2(1, 1);
            videoRT.offsetMin = Vector2.zero;
            videoRT.offsetMax = Vector2.zero;
            
            videoDisplay = videoGO.AddComponent<RawImage>();
            videoDisplay.color = Color.white;
            
            videoPlayer = videoGO.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.isLooping = true;
            
            // Title
            CreateOverlayLabel(videoOverlay.transform, "Title",
                "INSTANT REPLAY", 30,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -60), new Vector2(400, 50),
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
            
            // ====== CONTROLS AREA (Bottom 30%) ======
            var controlsPanel = new GameObject("ControlsPanel");
            controlsPanel.transform.SetParent(videoOverlay.transform, false);
            var controlsRT = controlsPanel.AddComponent<RectTransform>();
            controlsRT.anchorMin = new Vector2(0, 0);
            controlsRT.anchorMax = new Vector2(1, 0.22f); // reduced controls height
            controlsRT.offsetMin = Vector2.zero;
            controlsRT.offsetMax = Vector2.zero;
            
            // 1. Seekbar (Slider) - Above buttons
            CreateSeekbar(controlsPanel.transform);
            
            // 2. Control Buttons - Bottom Row
            float btnY = 100; // 100px from bottom edge
            float btnSize = 90; // Larger buttons
            float gap = 30;
            
            // Play/Pause (Center)
            CreateControlButton(controlsPanel.transform, "PlayPause", "||", 
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, btnY), new Vector2(btnSize, btnSize), OnPlayPause, out playPauseImg);
                
            // Backward -2s (Left of Play)
            CreateControlButton(controlsPanel.transform, "BackBtn", "<<", 
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-(btnSize + gap), btnY), new Vector2(btnSize, btnSize), () => OnSkip(-2f));
                
            // Forward +2s (Right of Play)
            CreateControlButton(controlsPanel.transform, "FwdBtn", ">>", 
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(btnSize + gap, btnY), new Vector2(btnSize, btnSize), () => OnSkip(2f));
                
            // Speed Toggle (Bottom-Left)
            var speedBtn = CreateControlButton(controlsPanel.transform, "SpeedBtn", "1.0x", 
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(100, btnY), new Vector2(btnSize, btnSize), OnSpeedToggle);
            speedText = speedBtn.GetComponentInChildren<TextMeshProUGUI>();
            
            // Close Button (Bottom-Right)
            CreateControlButton(controlsPanel.transform, "CloseBtn", "X", 
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-100, btnY), new Vector2(btnSize, btnSize), CloseReplay);

            // SAVE Button (Center-Right, next to Fwd)
            CreateControlButton(controlsPanel.transform, "SaveBtn", "SAVE", 
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(250, btnY), new Vector2(btnSize + 20, btnSize), OnSaveReplay);
            
            videoOverlay.SetActive(false);
        }
        
        private void CreateSeekbar(Transform parent)
        {
            var sliderGO = new GameObject("Seekbar");
            sliderGO.transform.SetParent(parent, false);
            
            var rt = sliderGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.8f);
            rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.sizeDelta = new Vector2(0, 40);
            
            seekSlider = sliderGO.AddComponent<Slider>();
            seekSlider.direction = Slider.Direction.LeftToRight;
            seekSlider.minValue = 0;
            seekSlider.maxValue = 1;
            seekSlider.onValueChanged.AddListener(OnSeek);
            
            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = new Vector2(0, 0.25f);
            bgRT.anchorMax = new Vector2(1, 0.75f);
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(1, 1, 1, 0.2f);
            seekSlider.targetGraphic = bgImg;
            
            // Fill Area
            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderGO.transform, false);
            var fillAreaRT = fillArea.AddComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5, 0);
            fillAreaRT.offsetMax = new Vector2(-5, 0);
            
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRT = fill.AddComponent<RectTransform>();
            fillRT.sizeDelta = Vector2.zero;
            var fillImg = fill.AddComponent<Image>();
            fillImg.color = new Color(0.18f, 0.45f, 0.85f, 1f); // Blue fill
            seekSlider.fillRect = fillRT;
            
            // Handle
            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderGO.transform, false);
            var handleAreaRT = handleArea.AddComponent<RectTransform>();
            handleAreaRT.anchorMin = Vector2.zero;
            handleAreaRT.anchorMax = Vector2.one;
            handleAreaRT.offsetMin = new Vector2(10, 0);
            handleAreaRT.offsetMax = new Vector2(-10, 0);
            
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRT = handle.AddComponent<RectTransform>();
            handleRT.sizeDelta = new Vector2(40, 40);
            var handleImg = handle.AddComponent<Image>();
            handleImg.color = Color.white;
            seekSlider.handleRect = handleRT;
            
            // Add Event Trigger for Dragging state
            var entryDown = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
            entryDown.callback.AddListener((data) => { isDraggingSlider = true; });
            
            var entryUp = new UnityEngine.EventSystems.EventTrigger.Entry();
            entryUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
            entryUp.callback.AddListener((data) => { isDraggingSlider = false; });
            
            var trigger = sliderGO.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            trigger.triggers.Add(entryDown);
            trigger.triggers.Add(entryUp);
        }
        
        private GameObject CreateControlButton(Transform parent, string name, string text, 
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick, out Image iconImg)
        {
            var btnGO = CreateControlButton(parent, name, text, anchorMin, anchorMax, pos, size, onClick);
            iconImg = btnGO.GetComponent<Image>();
            return btnGO;
        }

        private GameObject CreateControlButton(Transform parent, string name, string text, 
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);
            
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            return go;
        }

        private void Update()
        {
            // Sync slider with video time if not dragging
            if (videoPlayer != null && videoPlayer.isPlaying && !isDraggingSlider && seekSlider != null && videoPlayer.length > 0)
            {
                seekSlider.value = (float)(videoPlayer.time / videoPlayer.length);
            }
        }
        
        // ====== CONTROL LOGIC ======
        
        private void OnSeek(float val)
        {
            if (videoPlayer != null && videoPlayer.isPrepared)
            {
                videoPlayer.time = val * videoPlayer.length;
            }
        }
        
        private void OnPlayPause()
        {
            if (videoPlayer == null) return;
            
            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                if (playPauseImg != null) playPauseImg.GetComponentInChildren<TextMeshProUGUI>().text = ">";
            }
            else
            {
                videoPlayer.Play();
                if (playPauseImg != null) playPauseImg.GetComponentInChildren<TextMeshProUGUI>().text = "||";
            }
        }
        
        private void OnSkip(float seconds)
        {
            if (videoPlayer == null) return;
            videoPlayer.time += seconds;
        }
        
        private void OnSpeedToggle()
        {
            if (videoPlayer == null) return;
            
            if (currentSpeed >= 1.0f) currentSpeed = 0.5f;
            else if (currentSpeed >= 0.5f) currentSpeed = 0.25f;
            else currentSpeed = 1.0f;
            
            videoPlayer.playbackSpeed = currentSpeed;
            if (speedText != null) speedText.text = $"{currentSpeed:0.0}x";
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
        
        // ... (rest of class)

        
        // ====== RECORD TOGGLE ======
        
        private void OnRecordToggle()
        {
            if (replayManager == null) return;
            if (isWaitingForPermission) return; // Don't allow toggling while waiting
            
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
                // Start recording — wait for confirmation before updating UI
                replayManager.StartBuffering();
                isWaitingForPermission = true;
                
                // Show pending state on the button
                if (recordBtnText != null) recordBtnText.text = "Starting...";
                if (recordBtnImage != null) recordBtnImage.color = new Color(0.5f, 0.5f, 0.2f, 0.85f);
                
                Debug.Log("[ReplayUI] Recording requested, waiting for permission...");
            }
        }
        
        private void OnBufferingConfirmed()
        {
            isWaitingForPermission = false;
            isRecording = true;
            UpdateRecordButtonState();
            if (replayButton != null) replayButton.SetActive(true);
            Debug.Log("[ReplayUI] Recording confirmed and started");
        }
        
        private void OnBufferingDenied(string reason)
        {
            isWaitingForPermission = false;
            isRecording = false;
            UpdateRecordButtonState();
            if (replayButton != null) replayButton.SetActive(false);
            Debug.LogWarning($"[ReplayUI] Recording denied: {reason}");
            
            ShowPermissionAlert(reason);
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
            
            // Ensure overlay is on top of everything
            if (videoOverlay != null) videoOverlay.transform.SetAsLastSibling();
            
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
            
            // Reset speed to normal 1x
            currentSpeed = 1.0f;
            videoPlayer.playbackSpeed = 1.0f;
            if (speedText != null) speedText.text = "1.0x";

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

        private void OnSaveReplay()
        {
            if (string.IsNullOrEmpty(currentClipPath) || !System.IO.File.Exists(currentClipPath))
            {
                if (loadingIndicator != null)
                {
                    loadingIndicator.SetActive(true);
                    var tmp = loadingIndicator.GetComponent<TextMeshProUGUI>();
                    if(tmp) tmp.text = "Error: No file found!";
                    StartCoroutine(AutoHideLoader(2f));
                }
                return;
            }

            // Generate a timestamped filename
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"BadmintonReplay_{timestamp}.mp4";
            
            // For now, save to persistentDataPath which is accessible via Files app on iOS
            string destPath = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            
            try
            {
                System.IO.File.Copy(currentClipPath, destPath, true);
                Debug.Log($"[ReplayUI] Saved to: {destPath}");
                
                // Show success message
                if (loadingIndicator != null)
                {
                    loadingIndicator.SetActive(true);
                    var tmp = loadingIndicator.GetComponent<TextMeshProUGUI>();
                    // Show truncated path for readability
                    if(tmp) tmp.text = $"Saved to Files!\n{fileName}";
                }
                
                // Ideally we would use NativeGallery.SaveVideoToGallery(destPath, "ARBadminton", fileName);
                // But without the plugin, this is the best we can do for now.
                // The user can access it via the "Files" app on their device.
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ReplayUI] Save failed: {e.Message}");
                if (loadingIndicator != null)
                {
                    loadingIndicator.SetActive(true);
                    var tmp = loadingIndicator.GetComponent<TextMeshProUGUI>();
                    if(tmp) tmp.text = "Save Failed!";
                }
            }
            
            StartCoroutine(AutoCloseAfterDelay(3f)); // Auto close replay after saving? Or just hide loader?
            // Actually let's just hide the loader so they can keep watching
            StopAllCoroutines(); // Stop any previous auto-close
            StartCoroutine(AutoHideLoader(3f));
        }

        private IEnumerator AutoHideLoader(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
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
                isWaitingForPermission = false;
                UpdateRecordButtonState();
            }
            
            if (recordButton != null) recordButton.SetActive(false);
            if (replayButton != null) replayButton.SetActive(false);
            if (permissionAlert != null) permissionAlert.SetActive(false);
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
        
        // ====== PERMISSION ALERT ======
        
        private void CreatePermissionAlert()
        {
            permissionAlert = new GameObject("PermissionAlert");
            permissionAlert.transform.SetParent(canvas.transform, false);
            permissionAlert.transform.SetAsLastSibling();
            
            var rt = permissionAlert.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.35f);
            rt.anchorMax = new Vector2(0.9f, 0.65f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            var bg = permissionAlert.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            
            // Alert message
            var msgGO = new GameObject("Message");
            msgGO.transform.SetParent(permissionAlert.transform, false);
            
            var msgRT = msgGO.AddComponent<RectTransform>();
            msgRT.anchorMin = new Vector2(0.05f, 0.35f);
            msgRT.anchorMax = new Vector2(0.95f, 0.9f);
            msgRT.offsetMin = Vector2.zero;
            msgRT.offsetMax = Vector2.zero;
            
            var msgTmp = msgGO.AddComponent<TextMeshProUGUI>();
            msgTmp.text = "";
            msgTmp.fontSize = 22;
            msgTmp.alignment = TextAlignmentOptions.Center;
            msgTmp.color = Color.white;
            msgTmp.enableWordWrapping = true;
            
            // OK button
            var okGO = new GameObject("OKBtn");
            okGO.transform.SetParent(permissionAlert.transform, false);
            
            var okRT = okGO.AddComponent<RectTransform>();
            okRT.anchorMin = new Vector2(0.3f, 0.05f);
            okRT.anchorMax = new Vector2(0.7f, 0.3f);
            okRT.offsetMin = Vector2.zero;
            okRT.offsetMax = Vector2.zero;
            
            var okImg = okGO.AddComponent<Image>();
            okImg.color = replayBtnColor;
            
            var okBtn = okGO.AddComponent<Button>();
            okBtn.onClick.AddListener(() => { if (permissionAlert != null) permissionAlert.SetActive(false); });
            
            var okTextGO = new GameObject("Text");
            okTextGO.transform.SetParent(okGO.transform, false);
            
            var okTextRT = okTextGO.AddComponent<RectTransform>();
            okTextRT.anchorMin = Vector2.zero;
            okTextRT.anchorMax = Vector2.one;
            okTextRT.offsetMin = Vector2.zero;
            okTextRT.offsetMax = Vector2.zero;
            
            var okTmp = okTextGO.AddComponent<TextMeshProUGUI>();
            okTmp.text = "OK";
            okTmp.fontSize = 24;
            okTmp.alignment = TextAlignmentOptions.Center;
            okTmp.color = Color.white;
            okTmp.fontStyle = FontStyles.Bold;
            
            permissionAlert.SetActive(false);
        }
        
        private void ShowPermissionAlert(string reason)
        {
            if (permissionAlert == null) return;
            
            var msgTmp = permissionAlert.transform.Find("Message")?.GetComponent<TextMeshProUGUI>();
            if (msgTmp != null)
            {
                msgTmp.text = $"Screen Recording Denied\n\n{reason}";
            }
            
            permissionAlert.SetActive(true);
            permissionAlert.transform.SetAsLastSibling();
            
            // Auto-dismiss after 4 seconds
            StartCoroutine(AutoDismissAlert(4f));
        }
        
        private IEnumerator AutoDismissAlert(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (permissionAlert != null) permissionAlert.SetActive(false);
        }
    }
}
