using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Creates and manages placement adjustment UI for the badminton court markings.
    /// Court only needs Y-axis rotation (flat on ground) and XZ movement.
    /// All movement/rotation buttons support tap-and-hold for continuous adjustment.
    /// </summary>
    public class CourtPlacementUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARBadmintonNet.AR.CourtPlacementController courtPlacement;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveStep = 0.1f;
        [SerializeField] private float rotateStep = 5f;
        
        private Canvas canvas;
        private GameObject adjustmentPanel;
        private GameObject lockedPanel;
        private bool isSetup = false;
        
        // ====== UNIFIED DESIGN SYSTEM (matches NetPlacementUI) ======
        private static readonly Color btnColor = new Color(0.12f, 0.12f, 0.18f, 0.85f);
        private static readonly Color btnHighlight = new Color(0.22f, 0.22f, 0.28f, 0.9f);
        private static readonly Color lockColor = new Color(0.15f, 0.65f, 0.35f, 0.85f);
        private static readonly Color resetColor = new Color(0.75f, 0.22f, 0.22f, 0.85f);
        private static readonly Color unlockColor = new Color(0.85f, 0.55f, 0.15f, 0.85f);
        private static readonly Color rotColor = new Color(0.3f, 0.75f, 0.4f, 0.85f);
        
        // Generous safe-area padding for curved edges and Dynamic Island
        private static readonly float dpadBtnSize = 90f;
        private static readonly float gap = 10f;
        private static readonly float safeTop = 130f;
        private static readonly float safeBottom = 120f;
        private static readonly float safeSide = 60f;
        
        private void Awake()
        {
            if (courtPlacement == null)
                courtPlacement = FindObjectOfType<ARBadmintonNet.AR.CourtPlacementController>();
        }
        
        private void Start()
        {
            SetupUI();
            
            if (courtPlacement != null)
            {
                courtPlacement.OnCourtPlaced += OnCourtPlaced;
                courtPlacement.OnCourtRemoved += OnCourtRemoved;
            }
            
            if (adjustmentPanel != null) adjustmentPanel.SetActive(false);
            if (lockedPanel != null) lockedPanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            if (courtPlacement != null)
            {
                courtPlacement.OnCourtPlaced -= OnCourtPlaced;
                courtPlacement.OnCourtRemoved -= OnCourtRemoved;
            }
        }
        
        private void OnCourtPlaced(Vector3 pos, Quaternion rot) => ShowAdjustmentUI();
        private void OnCourtRemoved() => HideAllUI();
        
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
            adjustmentPanel = new GameObject("CourtAdjustmentPanel");
            adjustmentPanel.transform.SetParent(canvas.transform, false);
            
            var rt = adjustmentPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // === INSTRUCTION ===
            CreateLabel(adjustmentPanel.transform, "InstructionLabel",
                "Position the Court", 26,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -(safeTop + 10)), new Vector2(400, 45));
            
            // === D-PAD (bottom-left) ===
            float leftX = safeSide + dpadBtnSize + gap;
            float bottomY = safeBottom + dpadBtnSize * 2f;
            
            CreateHoldableButton(adjustmentPanel.transform, "ForwardBtn", "UP",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY + dpadBtnSize + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveCourt(GetForwardDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "BackBtn", "DN",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY - dpadBtnSize - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveCourt(-GetForwardDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "LeftBtn", "LT",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX - dpadBtnSize - gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveCourt(-GetRightDir()), btnColor, 28);
            
            CreateHoldableButton(adjustmentPanel.transform, "RightBtn", "RT",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX + dpadBtnSize + gap, bottomY), new Vector2(dpadBtnSize, dpadBtnSize),
                () => MoveCourt(GetRightDir()), btnColor, 28);
            
            CreateLabel(adjustmentPanel.transform, "MoveLabel", "MOVE", 14,
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(leftX, bottomY), new Vector2(dpadBtnSize, dpadBtnSize));
            
            // === ROTATION (bottom-right, Y-axis only for court) ===
            float rightX = -(safeSide + dpadBtnSize);
            
            CreateLabel(adjustmentPanel.transform, "RotLabel", "Rotate", 14,
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX, bottomY + dpadBtnSize + gap + 28), new Vector2(200, 22));
            
            // Coarse rotation
            CreateHoldableButton(adjustmentPanel.transform, "RotL", "<< 5",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX - 50, bottomY + dpadBtnSize / 2 + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => RotateCourt(-rotateStep), rotColor, 20);
            
            CreateHoldableButton(adjustmentPanel.transform, "RotR", "5 >>",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX + 50, bottomY + dpadBtnSize / 2 + gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => RotateCourt(rotateStep), rotColor, 20);
            
            // Fine rotation (1 degree)
            CreateHoldableButton(adjustmentPanel.transform, "RotLFine", "< 1",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX - 50, bottomY - dpadBtnSize / 2 - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => RotateCourt(-1f), btnColor, 20);
            
            CreateHoldableButton(adjustmentPanel.transform, "RotRFine", "1 >",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rightX + 50, bottomY - dpadBtnSize / 2 - gap), new Vector2(dpadBtnSize, dpadBtnSize),
                () => RotateCourt(1f), btnColor, 20);
            
            // === LOCK & RESET (side by side) ===
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
            lockedPanel = new GameObject("CourtLockedPanel");
            lockedPanel.transform.SetParent(canvas.transform, false);
            
            var rt = lockedPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            CreateLabel(lockedPanel.transform, "LockedLabel",
                "Court Locked", 24,
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
        
        private GameObject CreateHoldableButton(Transform parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
            System.Action holdAction, Color bgColor, int fontSize = 28)
        {
            var btnGO = CreateButton(parent, name, text, anchorMin, anchorMax, position, size,
                () => holdAction(), bgColor, fontSize);
            
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
        
        private void MoveCourt(Vector3 direction)
        {
            if (courtPlacement != null)
                courtPlacement.MoveCourt(direction * moveStep);
        }
        
        private void RotateCourt(float degrees)
        {
            if (courtPlacement != null)
                courtPlacement.RotateCourt(degrees);
        }
        
        private void OnLockPressed()
        {
            if (courtPlacement != null)
            {
                courtPlacement.LockCourt();
                ShowLockedUI();
            }
        }
        
        private void OnUnlockPressed()
        {
            if (courtPlacement != null)
            {
                courtPlacement.UnlockCourt();
                ShowAdjustmentUI();
            }
        }
        
        private void OnResetPressed()
        {
            if (courtPlacement != null)
            {
                courtPlacement.RemoveCourt();
                HideAllUI();
            }
        }
        
        // ====== UI STATE ======
        
        public void ShowAdjustmentUI()
        {
            if (adjustmentPanel != null) adjustmentPanel.SetActive(true);
            if (lockedPanel != null) lockedPanel.SetActive(false);
        }
        
        public void ShowLockedUI()
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
