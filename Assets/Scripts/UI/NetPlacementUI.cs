using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Creates and manages placement adjustment UI programmatically.
    /// Shows directional buttons + 3-axis rotation + lock/reset after net is placed.
    /// </summary>
    public class NetPlacementUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARBadmintonNet.AR.NetPlacementController netPlacement;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveStep = 0.1f;     // 10cm per tap
        [SerializeField] private float rotateStep = 15f;    // 15 degrees per tap
        [SerializeField] private float heightStep = 0.05f;  // 5cm per tap
        
        // UI elements
        private Canvas canvas;
        private GameObject adjustmentPanel;
        private GameObject lockedPanel;
        private bool isSetup = false;
        
        // Button styling - LARGER SIZES
        // Button styling - MUCH LARGER SIZES
        private static readonly float largeBtnSize = 130f;  // Increased from 100f
        private static readonly float mediumBtnSize = 110f; // Increased from 85f
        private static readonly float gap = 15f;            // Increased gap
        
        private static readonly Color btnColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        private static readonly Color btnHighlight = new Color(0.3f, 0.3f, 0.3f, 0.95f);
        private static readonly Color lockColor = new Color(0.1f, 0.7f, 0.2f, 0.95f);
        private static readonly Color resetColor = new Color(0.8f, 0.2f, 0.2f, 0.95f);
        private static readonly Color unlockColor = new Color(0.9f, 0.6f, 0.1f, 0.95f);
        private static readonly Color rotXColor = new Color(0.9f, 0.3f, 0.3f, 0.9f);  // Red for X
        private static readonly Color rotYColor = new Color(0.3f, 0.9f, 0.3f, 0.9f);  // Green for Y
        private static readonly Color rotZColor = new Color(0.3f, 0.5f, 0.9f, 0.9f);  // Blue for Z
        
        private void Awake()
        {
            if (netPlacement == null)
                netPlacement = FindObjectOfType<ARBadmintonNet.AR.NetPlacementController>();
        }
        
        private void Start()
        {
            SetupUI();
            
            // Subscribe to placement events
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced += OnNetPlaced;
                netPlacement.OnNetRemoved += OnNetRemoved;
            }
            
            // Initially hidden
            if (adjustmentPanel != null) adjustmentPanel.SetActive(false);
            if (lockedPanel != null) lockedPanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced -= OnNetPlaced;
                netPlacement.OnNetRemoved -= OnNetRemoved;
            }
        }
        
        private void OnNetPlaced(Vector3 pos, Quaternion rot)
        {
            ShowAdjustmentUI();
        }
        
        private void OnNetRemoved()
        {
            HideAllUI();
        }
        
        private void SetupUI()
        {
            if (isSetup) return;
            isSetup = true;
            
            // Find existing canvas or create one
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("PlacementCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            
            CreateAdjustmentPanel();
            CreateLockedPanel();
        }
        
        private void CreateAdjustmentPanel()
        {
            adjustmentPanel = new GameObject("AdjustmentPanel");
            adjustmentPanel.transform.SetParent(canvas.transform, false);
            
            var rt = adjustmentPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Safe area padding for notches and curved displays
            float safeLeftPadding = 80f;
            float safeRightPadding = 80f;
            float safeTopPadding = 130f; // Increased for Dynamic Island
            float safeBottomPadding = 100f;
            
            // === INSTRUCTION TEXT at top ===
            CreateLabel(adjustmentPanel.transform, "InstructionLabel",
                "Position the Net", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -(30 + safeTopPadding)), new Vector2(500, 50));
            
            // === MOVEMENT D-PAD (bottom-left) ===
            float dpadX = 140f + safeLeftPadding;
            float dpadY = 260f + safeBottomPadding;
            
            // Forward (up in screen = forward in world)
            CreateButton(adjustmentPanel.transform, "ForwardBtn", "▲",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY + largeBtnSize + gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNet(GetForwardDir()), btnColor, 36);
            
            // Back
            CreateButton(adjustmentPanel.transform, "BackBtn", "▼",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY - largeBtnSize - gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNet(-GetForwardDir()), btnColor, 36);
            
            // Left
            CreateButton(adjustmentPanel.transform, "LeftBtn", "◀",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX - largeBtnSize - gap, dpadY), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNet(-GetRightDir()), btnColor, 36);
            
            // Right
            CreateButton(adjustmentPanel.transform, "RightBtn", "▶",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX + largeBtnSize + gap, dpadY), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNet(GetRightDir()), btnColor, 36);
            
            // Center label
            CreateLabel(adjustmentPanel.transform, "MoveLabel",
                "MOVE", 14,
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY), new Vector2(largeBtnSize, largeBtnSize));
            
            // === UP/DOWN HEIGHT (bottom-right) ===
            // Anchor to Bottom-Right (1,0)
            float udX = -(140f + safeRightPadding);
            
            // Up
            CreateButton(adjustmentPanel.transform, "UpBtn", "⬆",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(udX, dpadY + largeBtnSize + gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNetVertical(heightStep), btnColor, 36);
            
            // Down
            CreateButton(adjustmentPanel.transform, "DownBtn", "⬇",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(udX, dpadY - largeBtnSize - gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveNetVertical(-heightStep), btnColor, 36);
            
            // Height label
            CreateLabel(adjustmentPanel.transform, "HeightLabel",
                "HEIGHT", 14,
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(udX, dpadY), new Vector2(largeBtnSize, largeBtnSize));
            
            // === 3-AXIS ROTATION (middle section, stacked vertically) ===
            // Anchor to Bottom-Center (0.5, 0)
            float rotYBase = dpadY + 30f;
            float rotBtnWidth = mediumBtnSize * 2.2f;
            float rotBtnHeight = mediumBtnSize;
            
            // Y-axis rotation (Yaw / horizontal spin) - Green
            CreateLabel(adjustmentPanel.transform, "RotYLabel",
                "Yaw (Y)", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - rotBtnWidth/2 - 45, rotYBase + (rotBtnHeight + gap) * 2), new Vector2(80, 30));
            
            CreateButton(adjustmentPanel.transform, "RotYLeftBtn", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - 60f, rotYBase + (rotBtnHeight + gap) * 2), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.up, -rotateStep), rotYColor, 32);
            
            CreateButton(adjustmentPanel.transform, "RotYRightBtn", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 + 60f, rotYBase + (rotBtnHeight + gap) * 2), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.up, rotateStep), rotYColor, 32);
            
            // X-axis rotation (Pitch / tilt forward/back) - Red
            CreateLabel(adjustmentPanel.transform, "RotXLabel",
                "Pitch (X)", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - rotBtnWidth/2 - 45, rotYBase + (rotBtnHeight + gap)), new Vector2(80, 30));
            
            CreateButton(adjustmentPanel.transform, "RotXLeftBtn", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - 60f, rotYBase + (rotBtnHeight + gap)), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.right, -rotateStep), rotXColor, 32);
            
            CreateButton(adjustmentPanel.transform, "RotXRightBtn", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 + 60f, rotYBase + (rotBtnHeight + gap)), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.right, rotateStep), rotXColor, 32);
            
            // Z-axis rotation (Roll / tilt left/right) - Blue
            CreateLabel(adjustmentPanel.transform, "RotZLabel",
                "Roll (Z)", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - rotBtnWidth/2 - 45, rotYBase), new Vector2(80, 30));
            
            CreateButton(adjustmentPanel.transform, "RotZLeftBtn", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 - 60f, rotYBase), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.forward, -rotateStep), rotZColor, 32);
            
            CreateButton(adjustmentPanel.transform, "RotZRightBtn", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0 + 60f, rotYBase), new Vector2(mediumBtnSize, rotBtnHeight),
                () => RotateNetAxis(Vector3.forward, rotateStep), rotZColor, 32);
            
            // === LOCK BUTTON (bottom center) ===
            CreateButton(adjustmentPanel.transform, "LockBtn", "✓  Lock Net",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 60 + safeBottomPadding), new Vector2(250, 65),
                OnLockPressed, lockColor, 24);
            
            // === RESET BUTTON (top-right corner) ===
            CreateButton(adjustmentPanel.transform, "ResetBtn", "✗ Reset",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-(120 + safeRightPadding), -(60 + safeTopPadding)), new Vector2(200, 70),
                OnResetPressed, resetColor, 28);
        }
        
        private void CreateLockedPanel()
        {
            lockedPanel = new GameObject("LockedPanel");
            lockedPanel.transform.SetParent(canvas.transform, false);
            
            var rt = lockedPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Safe area padding (same as adjustment panel)
            float safeLeftPadding = 60f;
            float safeRightPadding = 60f;
            float safeBottomPadding = 80f;
            
            // Unlock button (bottom-right)
            CreateButton(lockedPanel.transform, "UnlockBtn", "🔓 Adjust",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-(100 + safeRightPadding), 50 + safeBottomPadding), new Vector2(160, 55),
                OnUnlockPressed, unlockColor, 20);
            
            // Reset button (bottom-left)
            CreateButton(lockedPanel.transform, "ResetBtn2", "✗ Reset",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(100 + safeLeftPadding, 50 + safeBottomPadding), new Vector2(160, 55),
                OnResetPressed, resetColor, 20);
        }
        
        // === BUTTON/LABEL CREATORS ===
        
        private GameObject CreateButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction onClick, Color bgColor, int fontSize = 32)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);
            
            var rt = btnGO.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            
            // Background
            var img = btnGO.AddComponent<Image>();
            img.color = bgColor;
            
            // Button component
            var btn = btnGO.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = btnHighlight;
            colors.pressedColor = new Color(
                Mathf.Min(bgColor.r + 0.3f, 1f), 
                Mathf.Min(bgColor.g + 0.3f, 1f), 
                Mathf.Min(bgColor.b + 0.3f, 1f), 
                bgColor.a);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            
            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            return btnGO;
        }
        
        private GameObject CreateLabel(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
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
            tmp.color = new Color(1f, 1f, 1f, 0.85f);
            tmp.fontStyle = FontStyles.Bold;
            
            return labelGO;
        }
        
        // === MOVEMENT ACTIONS ===
        
        private Vector3 GetForwardDir()
        {
            if (Camera.main == null) return Vector3.forward;
            Vector3 fwd = Camera.main.transform.forward;
            fwd.y = 0;
            return fwd.normalized;
        }
        
        private Vector3 GetRightDir()
        {
            if (Camera.main == null) return Vector3.right;
            Vector3 right = Camera.main.transform.right;
            right.y = 0;
            return right.normalized;
        }
        
        private void MoveNet(Vector3 direction)
        {
            if (netPlacement != null)
            {
                netPlacement.MoveNet(direction * moveStep);
            }
        }
        
        private void MoveNetVertical(float amount)
        {
            if (netPlacement != null)
            {
                netPlacement.MoveNet(Vector3.up * amount);
            }
        }
        
        private void RotateNetAxis(Vector3 axis, float degrees)
        {
            if (netPlacement != null)
            {
                netPlacement.RotateNetAxis(axis, degrees);
            }
        }
        
        private void OnLockPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.LockNet();
                ShowLockedUI();
                Debug.Log("[NetPlacementUI] Net locked by user");
            }
        }
        
        private void OnUnlockPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.UnlockNet();
                ShowAdjustmentUI();
                Debug.Log("[NetPlacementUI] Net unlocked for adjustment");
            }
        }
        
        private void OnResetPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.RemoveNet();
                HideAllUI();
                Debug.Log("[NetPlacementUI] Net reset");
            }
        }
        
        // === UI STATE ===
        
        private void ShowAdjustmentUI()
        {
            if (adjustmentPanel != null) adjustmentPanel.SetActive(true);
            if (lockedPanel != null) lockedPanel.SetActive(false);
        }
        
        private void ShowLockedUI()
        {
            if (adjustmentPanel != null) adjustmentPanel.SetActive(false);
            if (lockedPanel != null) lockedPanel.SetActive(true);
        }
        
        public void HideAllUI()
        {
            if (adjustmentPanel != null) adjustmentPanel.SetActive(false);
            if (lockedPanel != null) lockedPanel.SetActive(false);
        }
    }
}
