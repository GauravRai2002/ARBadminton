using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Startup mode selection UI that shows on app launch.
    /// Allows choosing between Net Mode and Court Mode, and switching between them anytime.
    /// A persistent mode-switch button stays visible in all modes.
    /// </summary>
    public class ModeSelectionUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARBadmintonNet.AR.NetPlacementController netPlacement;
        [SerializeField] private ARBadmintonNet.AR.CourtPlacementController courtPlacement;
        [SerializeField] private NetPlacementUI netUI;
        [SerializeField] private CourtPlacementUI courtUI;
        [SerializeField] private ARBadmintonNet.Detection.MotionBasedTracker motionTracker;
        [SerializeField] private ARBadmintonNet.AR.ARSessionManager arSessionManager;
        [SerializeField] private ARBadmintonNet.Replay.ReplayManager replayManager;
        [SerializeField] private ARBadmintonNet.Replay.ReplayUI replayUI;
        
        public enum AppMode { None, Net, Court }
        
        private AppMode currentMode = AppMode.None;
        private Canvas canvas;
        private GameObject startupPanel;
        private GameObject modeSwitchButton;
        private bool isSetup = false;
        
        private static readonly Color netModeColor = new Color(0.18f, 0.45f, 0.85f, 0.85f);
        private static readonly Color courtModeColor = new Color(0.15f, 0.65f, 0.35f, 0.85f);
        private static readonly Color switchBtnColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);
        
        public AppMode CurrentMode => currentMode;
        
        private void Awake()
        {
            if (netPlacement == null)
                netPlacement = FindObjectOfType<ARBadmintonNet.AR.NetPlacementController>();
            if (courtPlacement == null)
                courtPlacement = FindObjectOfType<ARBadmintonNet.AR.CourtPlacementController>();
            if (netUI == null)
                netUI = FindObjectOfType<NetPlacementUI>();
            if (courtUI == null)
                courtUI = FindObjectOfType<CourtPlacementUI>();
            if (motionTracker == null)
                motionTracker = FindObjectOfType<ARBadmintonNet.Detection.MotionBasedTracker>();
            if (arSessionManager == null)
                arSessionManager = FindObjectOfType<ARBadmintonNet.AR.ARSessionManager>();
            if (replayManager == null)
                replayManager = FindObjectOfType<ARBadmintonNet.Replay.ReplayManager>();
            if (replayUI == null)
                replayUI = FindObjectOfType<ARBadmintonNet.Replay.ReplayUI>();
        }
        
        private void Start()
        {
            SetupUI();
            ShowStartupScreen();
            
            // Disable both modes initially
            SetNetActive(false);
            SetCourtActive(false);
        }
        
        private void SetupUI()
        {
            if (isSetup) return;
            isSetup = true;
            
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("ModeCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            CreateStartupPanel();
            CreateModeSwitchButton();
        }
        
        private void CreateStartupPanel()
        {
            startupPanel = new GameObject("StartupPanel");
            startupPanel.transform.SetParent(canvas.transform, false);
            
            var rt = startupPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Solid dark background — matches app palette
            var bg = startupPanel.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.1f, 1f);
            
            // Title
            CreateLabel(startupPanel.transform, "Title",
                "🏸 AR Badminton", 52,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 280), new Vector2(600, 80),
                Color.white);
            
            // Subtitle
            CreateLabel(startupPanel.transform, "Subtitle",
                "Choose a mode to get started", 24,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 210), new Vector2(500, 40),
                new Color(1, 1, 1, 0.5f));
            
            // Mode buttons
            float btnWidth = 650f;
            float btnHeight = 150f;
            
            CreateModeButton(startupPanel.transform, "NetModeBtn",
                "🏸  Net Mode", "Place a virtual badminton net",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, 50), new Vector2(btnWidth, btnHeight),
                () => SwitchToMode(AppMode.Net), netModeColor);
            
            CreateModeButton(startupPanel.transform, "CourtModeBtn",
                "📐  Court Mode", "Place court line markings",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -130), new Vector2(btnWidth, btnHeight),
                () => SwitchToMode(AppMode.Court), courtModeColor);
            
            // Instruction
            CreateLabel(startupPanel.transform, "Instruction",
                "You can switch modes anytime.", 18,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -250), new Vector2(400, 35),
                new Color(1, 1, 1, 0.35f));
        }
        
        private void CreateModeSwitchButton()
        {
            modeSwitchButton = new GameObject("ModeSwitchBtn");
            modeSwitchButton.transform.SetParent(canvas.transform, false);
            
            var rt = modeSwitchButton.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);  // Top-right
            rt.anchorMax = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -110);
            rt.sizeDelta = new Vector2(200, 50);
            
            var img = modeSwitchButton.AddComponent<Image>();
            img.color = switchBtnColor;
            
            var btn = modeSwitchButton.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = switchBtnColor;
            colors.highlightedColor = new Color(0.7f, 0.4f, 0.9f, 0.95f);
            colors.pressedColor = new Color(0.8f, 0.5f, 1f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(OnModeSwitchPressed);
            
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(modeSwitchButton.transform, false);
            
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "⇄ Switch";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            modeSwitchButton.SetActive(false);
        }
        
        private void CreateModeButton(Transform parent, string name,
            string title, string description,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction onClick, Color bgColor)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            var img = btnGO.AddComponent<Image>();
            img.color = bgColor;
            
            var btn = btnGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.15f, 1f),
                Mathf.Min(bgColor.b + 0.15f, 1f),
                bgColor.a);
            colors.pressedColor = new Color(
                Mathf.Min(bgColor.r + 0.3f, 1f),
                Mathf.Min(bgColor.g + 0.3f, 1f),
                Mathf.Min(bgColor.b + 0.3f, 1f),
                bgColor.a);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            
            // Title text
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(btnGO.transform, false);
            
            var titleRT = titleGO.AddComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 0.5f);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.offsetMin = new Vector2(30, 0);
            titleRT.offsetMax = new Vector2(-30, -5);
            
            var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 32;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = Color.white;
            titleTmp.fontStyle = FontStyles.Bold;
            
            // Description text
            var descGO = new GameObject("Description");
            descGO.transform.SetParent(btnGO.transform, false);
            
            var descRT = descGO.AddComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0, 0);
            descRT.anchorMax = new Vector2(1, 0.5f);
            descRT.offsetMin = new Vector2(30, 10);
            descRT.offsetMax = new Vector2(-30, 0);
            
            var descTmp = descGO.AddComponent<TextMeshProUGUI>();
            descTmp.text = description;
            descTmp.fontSize = 18;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = new Color(1, 1, 1, 0.6f);
        }
        
        private GameObject CreateLabel(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
        {
            var labelGO = new GameObject(name);
            labelGO.transform.SetParent(parent, false);
            
            var rt = labelGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontStyle = FontStyles.Bold;
            
            return labelGO;
        }
        
        // === MODE MANAGEMENT ===
        
        public void ShowStartupScreen()
        {
            if (startupPanel != null) startupPanel.SetActive(true);
            if (modeSwitchButton != null) modeSwitchButton.SetActive(false);
            
            // Stop replay buffering when back at startup
            if (replayManager != null) replayManager.StopBuffering();
            if (replayUI != null) replayUI.HideReplayButton();
        }
        
        public void SwitchToMode(AppMode mode)
        {
            // Start AR session when first mode is selected
            if (arSessionManager != null)
                arSessionManager.StartARSession();
            
            // Start replay buffering
            if (replayManager != null) replayManager.StartBuffering();
            if (replayUI != null) replayUI.ShowReplayButton();
            
            // Hide startup panel
            if (startupPanel != null) startupPanel.SetActive(false);
            
            // Clean up previous mode
            if (currentMode == AppMode.Net)
            {
                if (netPlacement != null && netPlacement.IsNetPlaced)
                {
                    // Keep the net placed, just hide UI
                }
                if (netUI != null) netUI.HideAllUI();
                SetNetActive(false);
            }
            else if (currentMode == AppMode.Court)
            {
                if (courtUI != null) courtUI.HideAllUI();
                SetCourtActive(false);
            }
            
            // Activate new mode
            currentMode = mode;
            
            if (mode == AppMode.Net)
            {
                SetNetActive(true);
                SetCourtActive(false);
                
                // Update switch button text
                UpdateSwitchButtonText("⇄ Court Mode");
            }
            else if (mode == AppMode.Court)
            {
                SetNetActive(false);
                SetCourtActive(true);
                
                UpdateSwitchButtonText("⇄ Net Mode");
            }
            
            // Show the persistent mode switch button
            if (modeSwitchButton != null) modeSwitchButton.SetActive(true);
            
            Debug.Log($"[ModeSelection] Switched to {mode} mode");
        }
        
        private void OnModeSwitchPressed()
        {
            if (currentMode == AppMode.Net)
            {
                SwitchToMode(AppMode.Court);
            }
            else if (currentMode == AppMode.Court)
            {
                SwitchToMode(AppMode.Net);
            }
            else
            {
                ShowStartupScreen();
            }
        }
        
        private void UpdateSwitchButtonText(string text)
        {
            if (modeSwitchButton == null) return;
            var tmp = modeSwitchButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
        
        private void SetNetActive(bool active)
        {
            if (netPlacement != null)
            {
                netPlacement.enabled = active;
                if (active && !netPlacement.IsNetPlaced)
                {
                    netPlacement.EnablePlacementMode();
                }
                else if (!active)
                {
                    netPlacement.DisablePlacementMode();
                }
            }
            
            if (netUI != null)
            {
                netUI.enabled = active;
                if (!active) netUI.HideAllUI();
            }
            
            // Motion tracker only in net mode
            if (motionTracker != null)
            {
                motionTracker.enabled = active;
            }
        }
        
        private void SetCourtActive(bool active)
        {
            if (courtPlacement != null)
            {
                courtPlacement.enabled = active;
                if (active && !courtPlacement.IsCourtPlaced)
                {
                    courtPlacement.EnablePlacementMode();
                }
                else if (!active)
                {
                    courtPlacement.DisablePlacementMode();
                }
            }
            
            if (courtUI != null)
            {
                courtUI.enabled = active;
                if (!active) courtUI.HideAllUI();
            }
        }
    }
}
