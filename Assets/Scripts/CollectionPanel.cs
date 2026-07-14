using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    [Serializable]
    public class CollectibleItem
    {
        public string id;
        public string displayName;
        public string unlockSetId; // The SetId required to unlock this item (from SetCatalogSO).
        public Sprite displaySprite;
    }

    public sealed class CollectionPanel : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameManager gameManager;

        [Header("Tabs")]
        [SerializeField] private Button bgTabBtn;
        [SerializeField] private Button decoTabBtn;
        [SerializeField] private GameObject bgPanel;
        [SerializeField] private GameObject decoPanel;
        [SerializeField] private Button closeBtn;

        [Header("Items Data")]
        [SerializeField] private List<CollectibleItem> backgrounds = new List<CollectibleItem>();
        [SerializeField] private List<CollectibleItem> decorations = new List<CollectibleItem>();

        private bool showBgTab = true;
        private static TMP_FontAsset defaultFont;

        // Styles
        private static readonly Color PanelBG = new Color(0.08f, 0.10f, 0.16f, 0.97f);
        private static readonly Color PageBG  = new Color(0.12f, 0.15f, 0.23f, 0.95f);
        private static readonly Color SlotBG  = new Color(0.18f, 0.22f, 0.33f, 1f);
        private static readonly Color TabActive = new Color(0.6f, 0.2f, 0.6f, 1f); // Purple
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.35f, 1f);

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (defaultFont == null) defaultFont = FindObjectOfType<TMP_Text>()?.font;

            BuildUI();
            AutoWireFields();
            InitializeDefaultItems();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += Refresh;
            }

            // Bind tab click listeners
            if (bgTabBtn != null)
            {
                bgTabBtn.onClick.RemoveAllListeners();
                bgTabBtn.onClick.AddListener(() => { showBgTab = true; Refresh(); });
            }
            if (decoTabBtn != null)
            {
                decoTabBtn.onClick.RemoveAllListeners();
                decoTabBtn.onClick.AddListener(() => { showBgTab = false; Refresh(); });
            }
            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
            }
        }

        public void Refresh()
        {
            AutoWireFields();

            // Toggle active panels
            if (bgPanel != null) bgPanel.SetActive(showBgTab);
            if (decoPanel != null) decoPanel.SetActive(!showBgTab);

            // Update Tab Button styles if available
            UpdateTabButtonStyles();

            // Populate slots
            if (showBgTab && bgPanel != null)
            {
                UpdateSlotsInPanel(bgPanel.transform, backgrounds);
            }
            else if (!showBgTab && decoPanel != null)
            {
                UpdateSlotsInPanel(decoPanel.transform, decorations);
            }
        }

        private void UpdateSlotsInPanel(Transform panelTf, List<CollectibleItem> items)
        {
            // Find all slots (children starting with Slot_ or containing Slot)
            var slotList = new List<Transform>();
            foreach (Transform child in panelTf)
            {
                if (child.name.StartsWith("Slot_") || child.name.ToLower().Contains("slot"))
                {
                    slotList.Add(child);
                }
                // Check in children page containers if any
                foreach (Transform sub in child)
                {
                    if (sub.name.StartsWith("Slot_") || sub.name.ToLower().Contains("slot"))
                    {
                        slotList.Add(sub);
                    }
                }
            }

            // Sort slots alphabetically/numerically by name to ensure consistent mapping
            slotList.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < slotList.Count; i++)
            {
                var slot = slotList[i];
                if (i < items.Count)
                {
                    slot.gameObject.SetActive(true);
                    UpdateSlot(slot, items[i]);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateSlot(Transform slotTf, CollectibleItem item)
        {
            if (slotTf == null || item == null) return;

            // Find image
            var img = slotTf.Find("Art")?.GetComponent<Image>() ?? 
                      slotTf.Find("Image")?.GetComponent<Image>() ?? 
                      slotTf.Find("DetailArt")?.GetComponent<Image>() ?? 
                      slotTf.GetComponentInChildren<Image>();

            // Find name text (exclude any lock/question texts)
            TMP_Text nameTxt = null;
            var texts = slotTf.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in texts)
            {
                string nLower = t.name.ToLower();
                if (nLower.Contains("name") || nLower.Contains("title") || nLower.Contains("text"))
                {
                    nameTxt = t;
                    break;
                }
            }
            if (nameTxt == null && texts.Length > 0) nameTxt = texts[0];

            // Find unknown overlay or ? text
            var unknownOverlay = slotTf.Find("Unknown")?.gameObject ?? 
                                 slotTf.Find("Lock")?.gameObject ?? 
                                 slotTf.Find("?")?.gameObject;

            TMP_Text unknownTxt = null;
            foreach (var t in texts)
            {
                string nLower = t.name.ToLower();
                if (nLower.Contains("?") || nLower.Contains("unk") || nLower.Contains("lock"))
                {
                    unknownTxt = t;
                    break;
                }
            }

            bool unlocked = gameManager != null && (string.IsNullOrEmpty(item.unlockSetId) || gameManager.IsSetCompleted(item.unlockSetId));

            if (img != null)
            {
                img.sprite = unlocked ? item.displaySprite : null;
                img.color = unlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f, 1f); // Solid dark gray when locked
            }

            if (nameTxt != null)
            {
                nameTxt.text = unlocked ? item.displayName : "???";
            }

            if (unknownOverlay != null)
            {
                unknownOverlay.SetActive(!unlocked);
            }

            if (unknownTxt != null)
            {
                // Show ? when locked, blank when unlocked
                unknownTxt.text = unlocked ? "" : "?";
            }
        }

        private void UpdateTabButtonStyles()
        {
            if (bgTabBtn != null)
            {
                var img = bgTabBtn.GetComponent<Image>();
                if (img != null) img.color = showBgTab ? TabActive : TabInactive;
            }

            if (decoTabBtn != null)
            {
                var img = decoTabBtn.GetComponent<Image>();
                if (img != null) img.color = !showBgTab ? TabActive : TabInactive;
            }
        }

        private void InitializeDefaultItems()
        {
            // Populate defaults only if empty
            if (backgrounds.Count == 0)
            {
                backgrounds.Add(new CollectibleItem { id = "bg-normal", displayName = "일반 배경", unlockSetId = "normal" });
                backgrounds.Add(new CollectibleItem { id = "bg-basic", displayName = "기본 배경", unlockSetId = "basic" });
                backgrounds.Add(new CollectibleItem { id = "bg-fantasy", displayName = "판타지 배경", unlockSetId = "fantasy" });
                backgrounds.Add(new CollectibleItem { id = "bg-meme", displayName = "밈 배경", unlockSetId = "meme" });
                backgrounds.Add(new CollectibleItem { id = "bg-internet", displayName = "인터넷 배경", unlockSetId = "internet" });
                backgrounds.Add(new CollectibleItem { id = "bg-food", displayName = "음식 배경", unlockSetId = "food" });
                backgrounds.Add(new CollectibleItem { id = "bg-game", displayName = "게임 배경", unlockSetId = "game" });
            }

            if (decorations.Count == 0)
            {
                decorations.Add(new CollectibleItem { id = "deco-normal", displayName = "일반 장식품", unlockSetId = "normal" });
                decorations.Add(new CollectibleItem { id = "deco-basic", displayName = "기본 장식품", unlockSetId = "basic" });
                decorations.Add(new CollectibleItem { id = "deco-fantasy", displayName = "판타지 장식품", unlockSetId = "fantasy" });
                decorations.Add(new CollectibleItem { id = "deco-meme", displayName = "밈 장식품", unlockSetId = "meme" });
                decorations.Add(new CollectibleItem { id = "deco-internet", displayName = "인터넷 장식품", unlockSetId = "internet" });
                decorations.Add(new CollectibleItem { id = "deco-food", displayName = "음식 장식품", unlockSetId = "food" });
                decorations.Add(new CollectibleItem { id = "deco-game", displayName = "게임 장식품", unlockSetId = "game" });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION (DYNAMICAL RUNTIME BUILD)
        // ════════════════════════════════════════════════════════════════════
        public void BuildUI()
        {
            // If already wired, do not rebuild
            if (bgPanel != null || transform.childCount > 0) return;

            if (defaultFont == null) defaultFont = FindObjectOfType<TMP_Text>()?.font;

            // Fullscreen panel setup
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Translucent black overlay bg
            var overlayImg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.65f);

            // Center Panel (Book size)
            var panel = MakePanel(transform, new Vector2(0, 0), new Vector2(980, 640));
            
            // Title Text
            MakeText(panel.transform, "수집품 도감", new Vector2(0, 275), new Vector2(300, 45), 24, Color.white).fontStyle = FontStyles.Bold;

            // Tabs Construction
            float tabY = 285f;
            bgTabBtn = MakeButton(panel.transform, "배경", new Vector2(-160, tabY), new Vector2(90, 36), TabActive, () => { showBgTab = true; Refresh(); }).GetComponent<Button>();
            decoTabBtn = MakeButton(panel.transform, "장식품", new Vector2(-60, tabY), new Vector2(90, 36), TabInactive, () => { showBgTab = false; Refresh(); }).GetComponent<Button>();

            // Close Button
            closeBtn = MakeButton(panel.transform, "✕", new Vector2(470, 300), new Vector2(44, 44), new Color(0.7f, 0.20f, 0.20f, 1f), () => gameObject.SetActive(false)).GetComponent<Button>();

            // Book Areas
            bgPanel = MakeEmptyRT(panel.transform, "BgPanel", new Vector2(-10, -20), new Vector2(960, 560));
            decoPanel = MakeEmptyRT(panel.transform, "DecoPanel", new Vector2(-10, -20), new Vector2(960, 560));
            decoPanel.SetActive(false);

            // Build slots for both panels (8 on left page, 8 on right page = 16 slots per panel)
            BuildPanelPages(bgPanel.transform);
            BuildPanelPages(decoPanel.transform);
        }

        private void BuildPanelPages(Transform parentTf)
        {
            var leftPage  = MakePage(parentTf, new Vector2(-230, 0), new Vector2(440, 500));
            var rightPage = MakePage(parentTf, new Vector2( 230, 0), new Vector2(440, 500));

            // 4x2 grid of slots on Left Page (indices 0 to 7)
            BuildSlotGrid(leftPage.transform, 0);
            // 4x2 grid of slots on Right Page (indices 8 to 15)
            BuildSlotGrid(rightPage.transform, 8);
        }

        private void BuildSlotGrid(Transform pageTf, int startIdx)
        {
            float colW = 95f, rowH = 180f;
            float startX = -142.5f;
            float startY = 90f;

            for (int row = 0; row < 2; row++)
            for (int col = 0; col < 4; col++)
            {
                int si = startIdx + row * 4 + col;
                float x = startX + col * colW;
                float y = startY - row * rowH;

                var slotGO = new GameObject($"Slot_{si}");
                slotGO.transform.SetParent(pageTf, false);
                slotGO.AddComponent<Button>();

                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.anchoredPosition = new Vector2(x, y);
                slotRT.sizeDelta        = new Vector2(85, 140);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = SlotBG;

                // Frame outline
                MakeImage(slotGO.transform, "Frame", Vector2.zero, new Vector2(75, 130), new Color(0.4f, 0.4f, 0.4f, 0.5f));

                // Image Art
                MakeImage(slotGO.transform, "Art", new Vector2(0, 10), new Vector2(65, 80), Color.gray);

                // Name Text
                var nameTx = MakeText(slotGO.transform, "???", new Vector2(0, -45), new Vector2(75, 20), 10, Color.white);
                nameTx.alignment = TextAlignmentOptions.Center;

                // Locked Overlay (Unknown)
                var unkGO = new GameObject("Unknown");
                unkGO.transform.SetParent(slotGO.transform, false);
                var unkRT = unkGO.AddComponent<RectTransform>();
                unkRT.anchorMin = Vector2.zero; unkRT.anchorMax = Vector2.one;
                unkRT.offsetMin = Vector2.zero; unkRT.offsetMax = Vector2.zero;
                unkGO.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.10f, 0.85f);

                var unkTx = MakeText(unkGO.transform, "?", new Vector2(0, 0), new Vector2(70, 70), 32, new Color(0.5f, 0.5f, 0.6f));
                unkTx.alignment = TextAlignmentOptions.Center;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI FACTORY HELPERS
        // ════════════════════════════════════════════════════════════════════
        private static GameObject MakePanel(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = PanelBG;
            return go;
        }

        private static GameObject MakePage(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Page");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = PageBG;
            return go;
        }

        private static GameObject MakeEmptyRT(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return go;
        }

        private static GameObject MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color col)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = col;
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            var tx = go.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tx.font = defaultFont;
            tx.text      = text;
            tx.fontSize  = fontSize;
            tx.color     = col;
            tx.alignment = TextAlignmentOptions.Center;
            return tx;
        }

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
            Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var cs  = btn.colors;
            cs.highlightedColor = bgColor * 1.25f;
            cs.pressedColor     = bgColor * 0.75f;
            btn.colors          = cs;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tx  = labelGO.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tx.font = defaultFont;
            tx.text      = label;
            tx.fontSize  = Mathf.Clamp((int)(size.y * 0.45f), 10, 20);
            tx.alignment = TextAlignmentOptions.Center;
            tx.color     = Color.white;

            return go;
        }

        private void AutoWireFields()
        {
            if (bgTabBtn == null) bgTabBtn = transform.Find("Panel/Btn_배경")?.GetComponent<Button>() ?? transform.Find("Btn_배경")?.GetComponent<Button>() ?? FindChildContains<Button>("배경");
            if (decoTabBtn == null) decoTabBtn = transform.Find("Panel/Btn_장식품")?.GetComponent<Button>() ?? transform.Find("Btn_장식품")?.GetComponent<Button>() ?? FindChildContains<Button>("장식");
            if (closeBtn == null) closeBtn = transform.Find("Panel/Btn_✕")?.GetComponent<Button>() ?? transform.Find("Btn_✕")?.GetComponent<Button>() ?? FindChildContains<Button>("닫기");

            if (bgPanel == null) bgPanel = transform.Find("Panel/BgPanel")?.gameObject ?? transform.Find("BgPanel")?.gameObject;
            if (decoPanel == null) decoPanel = transform.Find("Panel/DecoPanel")?.gameObject ?? transform.Find("DecoPanel")?.gameObject;
        }

        private T FindChildContains<T>(string keyword) where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            foreach (var c in components)
            {
                if (c.name.Contains(keyword)) return c;
            }
            return null;
        }
    }
}
