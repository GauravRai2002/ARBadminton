using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Creates and manages placement adjustment UI programmatically.
    /// Shows directional buttons + 3-axis rotation + lock/reset after net is placed.
    /// All movement/rotation buttons support tap-and-hold for continuous adjustment.
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
        
        // Generous safe-area padding for curved edges and Dynamic Island
        private static readonly float dpadBtnSize = 90f;
        private static readonly float gap = 10f;
        private static readonly float safeTop = 130f;
        private static readonly float safeBottom = 120f;
        private static readonly float safeSide = 60f;
        
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
                "Position the Net", 26,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -(safeTop + 10)), new Vector2(400, 45));
            
            // === D-PAD (bottom-left) ===
            float leftX = safeSide + dpadBtnSize + gap;
            float bottomY = safeBottom + dpadBtnSize * 2f;
            
            CreateHoldableButton(adjustmentPanel.transform, "ForwardBtn", "UP",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY + dpadBtnSize + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(GetForwardDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "BackBtn", "DN",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY - dpadBtnSize - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(-GetForwardDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "LeftBtn", "LT",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX - dpadBtnSize - gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(-GetRightDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "RightBtn", "RT",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX + dpadBtnSize + gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNet(GetRightDir()), btnColor, 28);
            
            CreateLabel(adjustmentPanel.transform, "MoveLabel", "MOVE", 14,
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY), new Vector2(dpadBtnSize, dpadBtnSize));
            
            // === HEIGHT (bottom-right) ===
            float rightX = -(safeSide + dpadBtnSize / 2 + gap);
            
            CreateHoldableButton(adjustmentPanel.transform, "UpBtn", "UP",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY + dpadBtnSize / 2 + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNetVertical(heightStep), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "DownBtn", "DN",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY - dpadBtnSize / 2 - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveNetVertical(-heightStep), btnColor, 28);
            
            CreateLabel(adjustmentPanel.transform, "HeightLabel", "HEIGHT", 14,
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY - dpadBtnSize * 1.5f - gap), new Vector2(dpadBtnSize, 25));
            
            // === ROTATION (center, stacked) ===
            float rotBtnW = 110f;
            float rotBtnH = 60f;
            float rotRowGap = 25f;
            float rotBaseY = safeBottom + 15f;
            
            // Row 1: Spin (Y-axis)
            CreateLabel(adjustmentPanel.transform, "SpinLabel", "Spin (Y)", 14,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + (rotBtnH + rotRowGap) * 2 + 28), new Vector2(90, 22));
            
            CreateHoldableButton(adjustmentPanel.transform, "SpinL", "< -",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 5, rotBaseY + (rotBtnH + rotRowGap) * 2), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.up, -rotateStep), rotSpinColor, 24);
            
            CreateHoldableButton(adjustmentPanel.transform, "SpinR", "+ >",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 5, rotBaseY + (rotBtnH + rotRowGap) * 2), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.up, rotateStep), rotSpinColor, 24);
            
            // Row 2: Tilt (X-axis)
            CreateLabel(adjustmentPanel.transform, "TiltLabel", "Tilt (X)", 14,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + (rotBtnH + rotRowGap) + 28), new Vector2(90, 22));
            
            CreateHoldableButton(adjustmentPanel.transform, "TiltL", "< -",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 5, rotBaseY + (rotBtnH + rotRowGap)), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.right, -rotateStep), rotTiltColor, 24);
            
            CreateHoldableButton(adjustmentPanel.transform, "TiltR", "+ >",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 5, rotBaseY + (rotBtnH + rotRowGap)), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.right, rotateStep), rotTiltColor, 24);
            
            // Row 3: Roll (Z-axis)
            CreateLabel(adjustmentPanel.transform, "RollLabel", "Roll (Z)", 14,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, rotBaseY + 28), new Vector2(90, 22));
            
            CreateHoldableButton(adjustmentPanel.transform, "RollL", "< -",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-rotBtnW/2 - 5, rotBaseY), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.forward, -rotateStep), rotRollColor, 24);
            
            CreateHoldableButton(adjustmentPanel.transform, "RollR", "+ >",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(rotBtnW/2 + 5, rotBaseY), new Vector2(rotBtnW, rotBtnH),
                () => RotateNetAxis(Vector3.forward, rotateStep), rotRollColor, 24);
            
            // === LOCK & RESET (side by side above D-pad) ===
            float actionY = safeBottom + dpadBtnSize * 3.5f + 20;
            
            CreateButton(adjustmentPanel.transform, "LockBtn", "LOCK",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, actionY), new Vector2(200, 55),
                OnLockPressed, lockColor, 22);
            
            CreateButton(adjustmentPanel.transform, "ResetBtn", "RESET",
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
            
            CreateLabel(lockedPanel.transform, "LockedLabel",
                "Net Locked", 24,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                new Vector2(0, -(safeTop + 10)), new Vector2(300, 40));
            
            CreateButton(lockedPanel.transform, "UnlockBtn", "UNLOCK",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(-110, safeBottom + 30), new Vector2(200, 55),
                OnUnlockPressed, unlockColor, 22);
            
            CreateButton(lockedPanel.transform, "ResetBtn2", "RESET",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(110, safeBottom + 30), new Vector2(200, 55),
                OnResetPressed, resetColor, 22);
        }
        
        // ====== BUTTON CREATORS ======
        
        /// <summary>
        /// Create a button that supports hold-to-repeat for continuous adjustment.
        /// </summary>
        private GameObject CreateHoldableButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            System.Action holdAction, Color bgColor, int fontSize = 28)
        {
            var btnGO = CreateButton(parent, name, text, anchorMin, anchorMax, position, size,
                () => holdAction(), bgColor, fontSize);
            
            // Add hold-to-repeat behavior
            var hold = btnGO.AddComponent<HoldButton>();
            hold.SetAction(holdAction);
            
            return btnGO;
        }
        
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
