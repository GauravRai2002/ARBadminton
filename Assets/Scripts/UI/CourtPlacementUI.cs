using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Creates and manages placement adjustment UI for the badminton court markings.
    /// Shows directional buttons + Y rotation + lock/reset after court is placed.
    /// Court only needs Y-axis rotation (flat on ground) and XZ movement.
    /// </summary>
    public class CourtPlacementUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ARBadmintonNet.AR.CourtPlacementController courtPlacement;
        
        [Header("Movement Settings")]
        [SerializeField] private float moveStep = 0.1f;
        [SerializeField] private float rotateStep = 5f; // smaller for precision alignment
        
        private Canvas canvas;
        private GameObject adjustmentPanel;
        private GameObject lockedPanel;
        private bool isSetup = false;
        
        private static readonly float largeBtnSize = 130f;
        private static readonly float mediumBtnSize = 110f;
        private static readonly float gap = 15f;
        
        private static readonly Color btnColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        private static readonly Color btnHighlight = new Color(0.3f, 0.3f, 0.3f, 0.95f);
        private static readonly Color lockColor = new Color(0.1f, 0.7f, 0.2f, 0.95f);
        private static readonly Color resetColor = new Color(0.8f, 0.2f, 0.2f, 0.95f);
        private static readonly Color unlockColor = new Color(0.9f, 0.6f, 0.1f, 0.95f);
        private static readonly Color rotYColor = new Color(0.3f, 0.9f, 0.3f, 0.9f);
        
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
        
        private void OnCourtPlaced(Vector3 pos, Quaternion rot)
        {
            ShowAdjustmentUI();
        }
        
        private void OnCourtRemoved()
        {
            HideAllUI();
        }
        
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
            
            float safeLeftPadding = 80f;
            float safeRightPadding = 80f;
            float safeTopPadding = 130f; // Increased for Dynamic Island
            float safeBottomPadding = 100f;
            
            // === INSTRUCTION TEXT ===
            CreateLabel(adjustmentPanel.transform, "InstructionLabel",
                "Position the Court", 28,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0, -(30 + safeTopPadding)), new Vector2(500, 50));
            
            // === MOVEMENT D-PAD (bottom-left) ===
            float dpadX = 140f + safeLeftPadding;
            float dpadY = 220f + safeBottomPadding;
            
            CreateButton(adjustmentPanel.transform, "ForwardBtn", "▲",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY + largeBtnSize + gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveCourt(GetForwardDir()), btnColor, 36);
            
            CreateButton(adjustmentPanel.transform, "BackBtn", "▼",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY - largeBtnSize - gap), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveCourt(-GetForwardDir()), btnColor, 36);
            
            CreateButton(adjustmentPanel.transform, "LeftBtn", "◀",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX - largeBtnSize - gap, dpadY), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveCourt(-GetRightDir()), btnColor, 36);
            
            CreateButton(adjustmentPanel.transform, "RightBtn", "▶",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX + largeBtnSize + gap, dpadY), new Vector2(largeBtnSize, largeBtnSize),
                () => MoveCourt(GetRightDir()), btnColor, 36);
            
            CreateLabel(adjustmentPanel.transform, "MoveLabel",
                "MOVE", 14,
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(dpadX, dpadY), new Vector2(largeBtnSize, largeBtnSize));
            
            // === Y-AXIS ROTATION ONLY (court is flat on ground) ===
            // Anchor to Bottom-Right (1,0)
            float rotX = -(140f + safeRightPadding);
            
            CreateLabel(adjustmentPanel.transform, "RotLabel",
                "ROTATE", 14,
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rotX, dpadY), new Vector2(largeBtnSize, largeBtnSize));
            
            CreateButton(adjustmentPanel.transform, "RotLeftBtn", "↺",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rotX - 60f, dpadY + largeBtnSize + gap), new Vector2(mediumBtnSize, mediumBtnSize),
                () => RotateCourt(-rotateStep), rotYColor, 32);
            
            CreateButton(adjustmentPanel.transform, "RotRightBtn", "↻",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rotX + 60f, dpadY + largeBtnSize + gap), new Vector2(mediumBtnSize, mediumBtnSize),
                () => RotateCourt(rotateStep), rotYColor, 32);
            
            // Fine rotation (1 degree)
            CreateButton(adjustmentPanel.transform, "RotLeftFineBtn", "↺ 1°",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rotX - 60f, dpadY - largeBtnSize - gap), new Vector2(mediumBtnSize, mediumBtnSize),
                () => RotateCourt(-1f), btnColor, 20);
            
            CreateButton(adjustmentPanel.transform, "RotRightFineBtn", "↻ 1°",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(rotX + 60f, dpadY - largeBtnSize - gap), new Vector2(mediumBtnSize, mediumBtnSize),
                () => RotateCourt(1f), btnColor, 20);
            
            // === LOCK BUTTON ===
            CreateButton(adjustmentPanel.transform, "LockBtn", "✓  Lock Court",
                new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, 60 + safeBottomPadding), new Vector2(250, 65),
                OnLockPressed, lockColor, 24);
            
            // === RESET BUTTON ===
            CreateButton(adjustmentPanel.transform, "ResetBtn", "✗ Reset",
                new Vector2(1, 1), new Vector2(1, 1),
                new Vector2(-(120 + safeRightPadding), -(60 + safeTopPadding)), new Vector2(200, 70),
                OnResetPressed, resetColor, 28);
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
            
            float safeLeftPadding = 60f;
            float safeRightPadding = 60f;
            float safeBottomPadding = 80f;
            
            CreateButton(lockedPanel.transform, "UnlockBtn", "Adjust Court",
                new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(-(100 + safeRightPadding), 50 + safeBottomPadding), new Vector2(180, 55),
                OnUnlockPressed, unlockColor, 20);
            
            CreateButton(lockedPanel.transform, "ResetBtn2", "✗ Reset",
                new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(100 + safeLeftPadding, 50 + safeBottomPadding), new Vector2(160, 55),
                OnResetPressed, resetColor, 20);
        }
        
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
            
            var img = btnGO.AddComponent<Image>();
            img.color = bgColor;
            
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
