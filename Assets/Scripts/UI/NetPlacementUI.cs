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
        
        private Canvas canvas;
        private GameObject adjustmentPanel;
        private GameObject lockedPanel;
        private bool isSetup = false;
        
        // ====== UNIFIED DESIGN SYSTEM ======
        private static readonly Color btnColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);
        private static readonly Color btnHighlight = new Color(0.22f, 0.22f, 0.28f, 0.9f);
        private static readonly Color lockColor = new Color(0.15f, 0.65f, 0.35f, 0.85f);
        private static readonly Color resetColor = new Color(0.75f, 0.22f, 0.22f, 0.85f);
        private static readonly Color unlockColor = new Color(0.85f, 0.55f, 0.15f, 0.85f);
        private static readonly Color rotSpinColor = new Color(0.3f, 0.75f, 0.4f, 0.85f);
        private static readonly Color rotTiltColor = new Color(0.75f, 0.3f, 0.3f, 0.85f);
        private static readonly Color rotRollColor = new Color(0.3f, 0.45f, 0.8f, 0.85f);
        
        private static readonly float dpadBtnSize = 100f;
        private static readonly float gap = 12f;
        private static readonly float safeTop = 110f;
        private static readonly float safeBottom = 90f;
        private static readonly float safeSide = 25f;
        
        private void Awake()
        {
            if (netPlacement == null)
                netPlacement = FindObjectOfType<ARBadmintonNet.AR.NetPlacementController>();
        }
        
        private void Start()
        {
            SetupUI();
            
            if (netPlacement != null)
            {
                netPlacement.OnNetPlaced += OnNetPlaced;
                netPlacement.OnNetRemoved += OnNetRemoved;
            }
            
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
        
        private void OnNetPlaced(Vector3 pos, Quaternion rot) => ShowAdjustmentUI();
        private void OnNetRemoved() => HideAllUI();
        
        private void SetupUI()
        {
            if (isSetup) return;
            isSetup = true;
            
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
            
            // === INSTRUCTION ===
            CreateLabel(adjustmentPanel.transform, "InstructionLabel",
                "🎯 Position the Net", 26,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -(safeTop + 20)), new Vector2(400, 45));
            
            // === D-PAD (bottom-left) ===
            float leftX = safeSide + dpadBtnSize * 1.5f;
            float bottomY = safeBottom + dpadBtnSize * 2f;
            
            CreateButton(adjustmentPanel.transform, "ForwardBtn", "▲",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY + dpadBtnSize + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(GetForwardDir()), btnColor, 32);
            
            CreateButton(adjustmentPanel.transform, "BackBtn", "▼",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY - dpadBtnSize - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(-GetForwardDir()), btnColor, 32);
            
            CreateButton(adjustmentPanel.transform, "LeftBtn", "◀",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX - dpadBtnSize - gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(-GetRightDir()), btnColor, 32);
            
            CreateButton(adjustmentPanel.transform, "RightBtn", "▶",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX + dpadBtnSize + gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(GetRightDir()), btnColor, 32);
            
            CreateLabel(adjustmentPanel.transform, "MoveLabel", "MOVE", 16,
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY), new Vector2(dpadBtnSize, dpadBtnSize));
            
            // === HEIGHT (bottom-right) ===
            float rightX = -safeSide - dpadBtnSize / 2;
            
            CreateButton(adjustmentPanel.transform, "UpBtn", "⬆",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY + dpadBtnSize / 2 + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNetVertical(heightStep), btnColor, 32);
            
            CreateButton(adjustmentPanel.transform, "DownBtn", "⬇",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY - dpadBtnSize / 2 - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNetVertical(-heightStep), btnColor, 32);
            
            CreateLabel(adjustmentPanel.transform, "HeightLabel", "HEIGHT", 16,
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY - dpadBtnSize * 1.5f - gap * 2), new Vector2(dpadBtnSize, 30));
            
            // === ROTATION (center, stacked) ===
            float rotBtnW = 100f;
            float rotBtnH = 55f;
            float rotRowGap = 8f;
            float rotBaseY = safeBottom + 20f;
            
            // Row 1: Spin (Y-axis)
            CreateLabel(adjustmentPanel.transform, "SpinLabel", "↔ Spin", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + (rotBtnH + rotRowGap) * 2 + 30), new Vector2(100, 25));
            
            CreateButton(adjustmentPanel.transform, "SpinL", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 6, rotBaseY + (rotBtnH + rotRowGap) * 2), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.up, -rotateStep), rotSpinColor, 26);
            
            CreateButton(adjustmentPanel.transform, "SpinR", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 6, rotBaseY + (rotBtnH + rotRowGap) * 2), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.up, rotateStep), rotSpinColor, 26);
            
            // Row 2: Tilt (X-axis)
            CreateLabel(adjustmentPanel.transform, "TiltLabel", "↕ Tilt", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + (rotBtnH + rotRowGap) + 30), new Vector2(100, 25));
            
            CreateButton(adjustmentPanel.transform, "TiltL", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 6, rotBaseY + (rotBtnH + rotRowGap)), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.right, -rotateStep), rotTiltColor, 26);
            
            CreateButton(adjustmentPanel.transform, "TiltR", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 6, rotBaseY + (rotBtnH + rotRowGap)), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.right, rotateStep), rotTiltColor, 26);
            
            // Row 3: Roll (Z-axis)
            CreateLabel(adjustmentPanel.transform, "RollLabel", "↗ Roll", 16,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + 30), new Vector2(100, 25));
            
            CreateButton(adjustmentPanel.transform, "RollL", "↺",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 6, rotBaseY), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.forward, -rotateStep), rotRollColor, 26);
            
            CreateButton(adjustmentPanel.transform, "RollR", "↻",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 6, rotBaseY), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.forward, rotateStep), rotRollColor, 26);
            
            // === LOCK & RESET (side by side, bottom bar) ===
            float actionY = safeBottom + dpadBtnSize * 3.5f;
            
            CreateButton(adjustmentPanel.transform, "LockBtn", "✓ Lock",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, actionY), new Vector2(200, 55),
                OnLockPressed, lockColor, 22);
            
            CreateButton(adjustmentPanel.transform, "ResetBtn", "✗ Reset",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(110, actionY), new Vector2(200, 55),
                OnResetPressed, resetColor, 22);
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
            
            // Locked status text
            CreateLabel(lockedPanel.transform, "LockedLabel",
                "🔒 Net Locked", 24,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -(safeTop + 20)), new Vector2(300, 40));
            
            // Unlock + Reset side by side at bottom
            CreateButton(lockedPanel.transform, "UnlockBtn", "🔓 Adjust",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, safeBottom + 30), new Vector2(200, 55),
                OnUnlockPressed, unlockColor, 22);
            
            CreateButton(lockedPanel.transform, "ResetBtn2", "✗ Reset",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(110, safeBottom + 30), new Vector2(200, 55),
                OnResetPressed, resetColor, 22);
        }
        
        // ====== BUTTON & LABEL CREATORS ======
        
        private GameObject CreateButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            UnityEngine.Events.UnityAction onClick, Color bgColor, int fontSize = 28)
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
            colors.highlightedColor = btnHighlight;
            colors.pressedColor = new Color(
                Mathf.Min(bgColor.r + 0.2f, 1f),
                Mathf.Min(bgColor.g + 0.2f, 1f),
                Mathf.Min(bgColor.b + 0.2f, 1f),
                bgColor.a);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);
            
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
            tmp.color = new Color(1f, 1f, 1f, 0.7f);
            tmp.fontStyle = FontStyles.Bold;
            
            return labelGO;
        }
        
        // ====== MOVEMENT ACTIONS ======
        
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
                netPlacement.MoveNet(direction * moveStep);
        }
        
        private void MoveNetVertical(float amount)
        {
            if (netPlacement != null)
                netPlacement.MoveNet(Vector3.up * amount);
        }
        
        private void RotateNetAxis(Vector3 axis, float degrees)
        {
            if (netPlacement != null)
                netPlacement.RotateNetAxis(axis, degrees);
        }
        
        private void OnLockPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.LockNet();
                ShowLockedUI();
            }
        }
        
        private void OnUnlockPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.UnlockNet();
                ShowAdjustmentUI();
            }
        }
        
        private void OnResetPressed()
        {
            if (netPlacement != null)
            {
                netPlacement.RemoveNet();
                HideAllUI();
            }
        }
        
        // ====== UI STATE ======
        
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
