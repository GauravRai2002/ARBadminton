using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ARBadmintonNet.UI
{
    /// <summary>
    /// Manages the scoring UI with P1/P2 scores and reset functionality.
    /// Resides at the top of the screen, below the dynamic island area.
    /// </summary>
    public class ScoreUI : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float topMargin = 350f; // Safe distance from top (Dynamic Island)
        [SerializeField] private Color overlayColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        [SerializeField] private Color fullscreenColor = new Color(0, 0, 0, 1f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color btnColor = new Color(0.25f, 0.25f, 0.3f, 1f);
        
        private int scoreP1 = 0;
        private int scoreP2 = 0;
        
        // UI References for Resizing
        private GameObject panel;
        private RectTransform panelRT;
        
        private TextMeshProUGUI scoreTextP1, scoreTextP2;
        private RectTransform containerP1, containerP2;
        private RectTransform btnPlusP1, btnMinusP1;
        private RectTransform btnPlusP2, btnMinusP2;
        private RectTransform btnReset, btnSwap;
        
        private void Start()
        {
            SetupUI();
            // Default to hidden on startup so it doesn't block Home screen
            Hide();
        }
        
        private void SetupUI()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("ScoreUI: No canvas found!");
                return;
            }
            
            // Main Panel
            panel = new GameObject("ScorePanel");
            panel.transform.SetParent(canvas.transform, false);
            
            panelRT = panel.AddComponent<RectTransform>();
            var img = panel.AddComponent<Image>();
            img.color = overlayColor;
            
            // Layout: [P1 UI] [VS/Reset] [P2 UI]
            
            // === PLAYER 1 (Left) ===
            CreatePlayerSection(panel.transform, "P1", out containerP1, out scoreTextP1, out btnPlusP1, out btnMinusP1,
                () => ChangeScore(1, 1), () => ChangeScore(1, -1));
                
            // === PLAYER 2 (Right) ===
            CreatePlayerSection(panel.transform, "P2", out containerP2, out scoreTextP2, out btnPlusP2, out btnMinusP2,
                () => ChangeScore(2, 1), () => ChangeScore(2, -1));
                
            // === CENTER (Reset & Swap) ===
            CreateCenterButtons(panel.transform);
            
            UpdateDisplay();
            
            // specific initial state (Mini)
            SetLayout(false);
        }
        
        public void SetLayout(bool fullscreen)
        {
            if (panelRT == null) return;
            
            if (fullscreen)
            {
                // -- FULLSCREEN MODE --
                
                // Panel: Full stretch
                panelRT.anchorMin = Vector2.zero;
                panelRT.anchorMax = Vector2.one;
                panelRT.offsetMin = Vector2.zero;
                panelRT.offsetMax = Vector2.zero;
                panel.GetComponent<Image>().color = fullscreenColor;
                
                // P1 (Left Half)
                SetPlayerLayout(containerP1, scoreTextP1, btnPlusP1, btnMinusP1, 
                    new Vector2(0, 0), new Vector2(0.5f, 1), // Anchors
                    300, 150); // Font, Btn Offset
                    
                // P2 (Right Half)
                SetPlayerLayout(containerP2, scoreTextP2, btnPlusP2, btnMinusP2, 
                    new Vector2(0.5f, 0), new Vector2(1, 1), 
                    300, 150);
                    
                // Center Buttons (Reset/Swap)
                // Put them in bottom center
                if (btnReset) 
                {
                    btnReset.anchoredPosition = new Vector2(0, 100);
                    btnReset.sizeDelta = new Vector2(200, 80);
                }
                if (btnSwap) 
                {
                    btnSwap.anchoredPosition = new Vector2(0, -100); 
                    // Actually let's put Swap above Reset or side-by-side?
                    // Let's keep vertical stack in center: Reset Top, Swap Bottom
                    // Center of screen?
                    btnReset.anchorMin = new Vector2(0.5f, 0.5f);
                    btnReset.anchorMax = new Vector2(0.5f, 0.5f);
                    btnReset.anchoredPosition = new Vector2(0, 80);
                    
                    btnSwap.anchorMin = new Vector2(0.5f, 0.5f);
                    btnSwap.anchorMax = new Vector2(0.5f, 0.5f);
                    btnSwap.anchoredPosition = new Vector2(0, -80);
                    btnSwap.sizeDelta = new Vector2(200, 80);
                }
            }
            else
            {
                // -- MINI OVERLAY MODE --
                
                // Panel: Top Center
                panelRT.anchorMin = new Vector2(0.5f, 1);
                panelRT.anchorMax = new Vector2(0.5f, 1);
                panelRT.anchoredPosition = new Vector2(0, -topMargin);
                panelRT.sizeDelta = new Vector2(600, 250);
                panel.GetComponent<Image>().color = overlayColor;
                
                // P1 (Left Side of Panel)
                // We reset anchors to center-ish relative to panel size
                // But CreatePlayerSection sets them to center... let's just use anchoredPosition
                
                // Hack: SetPlayerLayout assumes full-panel anchors for fullscreen.
                // For mini, we want specific pos/size.
                
                SetPlayerLayoutMini(containerP1, scoreTextP1, btnPlusP1, btnMinusP1, new Vector2(-200, 0));
                SetPlayerLayoutMini(containerP2, scoreTextP2, btnPlusP2, btnMinusP2, new Vector2(200, 0));
                
                if (btnReset)
                {
                    btnReset.anchorMin = new Vector2(0.5f, 0.5f);
                    btnReset.anchorMax = new Vector2(0.5f, 0.5f);
                    btnReset.anchoredPosition = new Vector2(0, 60);
                    btnReset.sizeDelta = new Vector2(120, 60);
                }
                if (btnSwap)
                {
                    btnSwap.anchorMin = new Vector2(0.5f, 0.5f);
                    btnSwap.anchorMax = new Vector2(0.5f, 0.5f);
                    btnSwap.anchoredPosition = new Vector2(0, -60);
                    btnSwap.sizeDelta = new Vector2(120, 60);
                }
            }
        }
        
        private void SetPlayerLayout(RectTransform container, TextMeshProUGUI text, RectTransform plus, RectTransform minus,
            Vector2 anchorMin, Vector2 anchorMax, float fontSize, float btnYOffset)
        {
            if (container == null) return;
            
            container.anchorMin = anchorMin;
            container.anchorMax = anchorMax;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.sizeDelta = Vector2.zero; // Stretch
            
            if (text != null) text.fontSize = fontSize;
            
            // Buttons large
            Vector2 btnSize = new Vector2(150, 120);
            
            if (plus != null)
            {
                plus.anchoredPosition = new Vector2(0, btnYOffset + 100); // Higher up
                plus.sizeDelta = btnSize;
            }
            if (minus != null)
            {
                minus.anchoredPosition = new Vector2(0, -(btnYOffset + 100));
                minus.sizeDelta = btnSize;
            }
        }
        
        private void SetPlayerLayoutMini(RectTransform container, TextMeshProUGUI text, RectTransform plus, RectTransform minus, Vector2 pos)
        {
            if (container == null) return;
            
            container.anchorMin = new Vector2(0.5f, 0.5f);
            container.anchorMax = new Vector2(0.5f, 0.5f);
            container.anchoredPosition = pos;
            container.sizeDelta = new Vector2(200, 200);
            
            if (text != null) text.fontSize = 120;
            
            Vector2 btnSize = new Vector2(100, 80);
            
            if (plus != null)
            {
                plus.anchoredPosition = new Vector2(0, 120);
                plus.sizeDelta = btnSize;
            }
            if (minus != null)
            {
                minus.anchoredPosition = new Vector2(0, -120);
                minus.sizeDelta = btnSize;
            }
        }

        private void CreatePlayerSection(Transform parent, string name, 
            out RectTransform containerRT, out TextMeshProUGUI scoreValidation, out RectTransform plusRT, out RectTransform minusRT,
            UnityEngine.Events.UnityAction onAdd, UnityEngine.Events.UnityAction onSub)
        {
            var container = new GameObject(name);
            container.transform.SetParent(parent, false);
            containerRT = container.AddComponent<RectTransform>();
            // Pos set by Layout
            
            // Score Text (Center)
            var textGO = new GameObject("Score");
            textGO.transform.SetParent(container.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "0";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            scoreValidation = tmp;
            
            // Buttons
            plusRT = CreateMiniButton(container.transform, "+", onAdd);
            minusRT = CreateMiniButton(container.transform, "-", onSub);
        }
        
        private RectTransform CreateMiniButton(Transform parent, string label, UnityEngine.Events.UnityAction action)
        {
            var btnGO = new GameObject("Btn" + label);
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.AddComponent<RectTransform>();
            // Pos/Size set by layout
            
            var img = btnGO.AddComponent<Image>();
            img.color = btnColor;
            
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(action);
            
            var tGO = new GameObject("T");
            tGO.transform.SetParent(btnGO.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            
            var tmp = tGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 60;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            return rt;
        }
        
        private void CreateCenterButtons(Transform parent)
        {
            // Reset Button
            btnReset = CreateCenterButton(parent, "RESET", new Color(0.6f, 0.2f, 0.2f, 1f), ResetScores);
            
            // Swap Button
            btnSwap = CreateCenterButton(parent, "SWAP", new Color(0.2f, 0.4f, 0.6f, 1f), SwapScores);
        }
        
        private RectTransform CreateCenterButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var btnGO = new GameObject(label + "Btn");
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.AddComponent<RectTransform>();
            // Pos/Size set by layout
            
            var img = btnGO.AddComponent<Image>();
            img.color = color;
            
            var btn = btnGO.AddComponent<Button>();
            btn.onClick.AddListener(action);
            
            var tGO = new GameObject("T");
            tGO.transform.SetParent(btnGO.transform, false);
            var tRT = tGO.AddComponent<RectTransform>();
            tRT.anchorMin = Vector2.zero;
            tRT.anchorMax = Vector2.one;
            
            var tmp = tGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            
            return rt;
        }
        
        private void ChangeScore(int player, int delta)
        {
            if (player == 1)
            {
                scoreP1 += delta;
                if (scoreP1 < 0) scoreP1 = 0;
            }
            else
            {
                scoreP2 += delta;
                if (scoreP2 < 0) scoreP2 = 0;
            }
            UpdateDisplay();
        }
        
        public void ResetScores()
        {
            scoreP1 = 0;
            scoreP2 = 0;
            UpdateDisplay();
        }
        
        public void SwapScores()
        {
            int temp = scoreP1;
            scoreP1 = scoreP2;
            scoreP2 = temp;
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (scoreTextP1 != null) scoreTextP1.text = scoreP1.ToString();
            if (scoreTextP2 != null) scoreTextP2.text = scoreP2.ToString();
        }
        
        public void Show()
        {
            if (panel != null) panel.SetActive(true);
        }
        
        public void Hide()
        {
            if (panel != null) panel.SetActive(false);
        }
    }
}
