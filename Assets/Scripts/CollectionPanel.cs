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
        public string description; // Added description field
        public string unlockSetId; // The SetId required to unlock this item (from SetCatalogSO).
        public Sprite displaySprite;
    }

    public sealed class CollectionPanel : MonoBehaviour
    {
        private static CollectionPanel _instance;
        public static CollectionPanel Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<CollectionPanel>(true);
                }
                if (_instance != null)
                {
                    _instance.EnsureInit();
                }
                return _instance;
            }
            private set => _instance = value;
        }

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

        // Slot Caching
        private GameObject bgSlotTemplate;
        private GameObject decoSlotTemplate;
        private readonly List<GameObject> bgSlots = new List<GameObject>();
        private readonly List<GameObject> decoSlots = new List<GameObject>();

        // Detail Popup references (dynamically wired)
        private GameObject detailRoot;
        private Image detailImage;
        private TMP_Text detailName;
        private TMP_Text detailDesc;
        private Button detailEquipBtn;
        private CollectibleItem selectedItem;
        private bool selectedIsBg;

        // Styles for dynamic detail popup
        private static readonly Color PanelBG = new Color(0.08f, 0.10f, 0.16f, 0.97f);
        private static readonly Color PageBG  = new Color(0.12f, 0.15f, 0.23f, 0.95f);
        private static readonly Color TabActive = new Color(0.6f, 0.2f, 0.6f, 1f); // Purple
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.35f, 1f);

        private bool _isInitialized = false;

        public void EnsureInit()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (defaultFont == null) defaultFont = FindObjectOfType<TMP_Text>()?.font;

            AutoWireFields();
            InitializeDefaultItems();
            InitSlots();
        }

        private void Awake()
        {
            _instance = this;
            EnsureInit();
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
                closeBtn.transform.SetAsLastSibling();
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

        public Sprite GetBackgroundSprite(string id)
        {
            EnsureInit();
            var item = backgrounds.Find(x => x.id == id);
            return item?.displaySprite;
        }

        public Sprite GetDecorationSprite(string id)
        {
            EnsureInit();
            var item = decorations.Find(x => x.id == id);
            return item?.displaySprite;
        }

        private void InitSlots()
        {
            // Initialize background slots from template
            if (bgPanel != null && bgSlots.Count == 0)
            {
                bgSlotTemplate = FindTemplateSlot(bgPanel.transform);
                if (bgSlotTemplate != null)
                {
                    bgSlotTemplate.SetActive(false);
                    var parent = bgSlotTemplate.transform.parent;

                    // Prevent resizing the main page: use ScrollRect's content container if present
                    var scrollRect = bgPanel.GetComponentInChildren<ScrollRect>(true) ?? bgPanel.GetComponent<ScrollRect>();
                    if (scrollRect != null && scrollRect.content != null)
                    {
                        parent = scrollRect.content;
                    }

                    var templateRT = bgSlotTemplate.GetComponent<RectTransform>();
                    Vector2 cellSize = templateRT != null ? templateRT.sizeDelta : new Vector2(120f, 146f);

                    for (int i = 0; i < backgrounds.Count; i++)
                    {
                        var item = backgrounds[i];
                        var clone = Instantiate(bgSlotTemplate, parent);
                        clone.name = $"Slot_{item.id}";
                        clone.SetActive(true);

                        var btn = GetOrAddComponent<Button>(clone);
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenDetail(item, true));

                        bgSlots.Add(clone);
                    }

                    EnsureLayoutAndScrolling(parent, cellSize);
                }
            }

            // Initialize decoration slots from template
            if (decoPanel != null && decoSlots.Count == 0)
            {
                decoSlotTemplate = FindTemplateSlot(decoPanel.transform);
                if (decoSlotTemplate != null)
                {
                    decoSlotTemplate.SetActive(false);
                    var parent = decoSlotTemplate.transform.parent;

                    // Prevent resizing the main page: use ScrollRect's content container if present
                    var scrollRect = decoPanel.GetComponentInChildren<ScrollRect>(true) ?? decoPanel.GetComponent<ScrollRect>();
                    if (scrollRect != null && scrollRect.content != null)
                    {
                        parent = scrollRect.content;
                    }

                    var templateRT = decoSlotTemplate.GetComponent<RectTransform>();
                    Vector2 cellSize = templateRT != null ? templateRT.sizeDelta : new Vector2(120f, 146f);

                    for (int i = 0; i < decorations.Count; i++)
                    {
                        var item = decorations[i];
                        var clone = Instantiate(decoSlotTemplate, parent);
                        clone.name = $"Slot_{item.id}";
                        clone.SetActive(true);

                        var btn = GetOrAddComponent<Button>(clone);
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OpenDetail(item, false));

                        decoSlots.Add(clone);
                    }

                    EnsureLayoutAndScrolling(parent, cellSize);
                }
            }
        }

        private GameObject FindTemplateSlot(Transform parent)
        {
            if (parent == null) return null;
            var t = parent.Find("Slot_0") ?? parent.Find("Slot") ?? parent.Find("SlotTemplate");
            if (t != null) return t.gameObject;

            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains("slot")) return child.gameObject;
                var found = FindTemplateSlot(child);
                if (found != null) return found;
            }
            return null;
        }

        public void Refresh()
        {
            AutoWireFields();
            InitSlots();

            // Toggle active panels
            if (bgPanel != null) bgPanel.SetActive(showBgTab);
            if (decoPanel != null) decoPanel.SetActive(!showBgTab);

            // Update Tab Button styles
            UpdateTabButtonStyles();

            // Refresh background slots
            for (int i = 0; i < bgSlots.Count; i++)
            {
                if (i < backgrounds.Count)
                {
                    UpdateSlot(bgSlots[i].transform, backgrounds[i], true);
                }
            }

            // Refresh decoration slots
            for (int i = 0; i < decoSlots.Count; i++)
            {
                if (i < decorations.Count)
                {
                    UpdateSlot(decoSlots[i].transform, decorations[i], false);
                }
            }

            // Update main background image
            UpdateMainBackground();
            // Update main decoration image
            UpdateMainDecoration();
        }

        private void UpdateMainBackground()
        {
            if (gameManager == null) return;
            string bgId = gameManager.EquippedBackgroundId;
            if (string.IsNullOrEmpty(bgId)) return;

            var sprite = GetBackgroundSprite(bgId);
            if (sprite == null && bgId != "bg-none") return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                foreach (Transform child in canvas.transform)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null)
                    {
                        string nameLower = child.name.ToLower();
                        // Ignore Panels and UI screens
                        if (nameLower.Contains("panel") || nameLower.Contains("hud") || nameLower.Contains("popup")) continue;
                        
                        if (nameLower.Contains("bg") || nameLower.Contains("background") || child.GetSiblingIndex() == 0)
                        {
                            img.sprite = sprite; // Will set to null if bg-none
                            img.color = Color.white; // Solid white background
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateMainDecoration()
        {
            if (gameManager == null) return;
            string decoId = gameManager.EquippedDecorationId;
            if (string.IsNullOrEmpty(decoId)) return;

            var sprite = GetDecorationSprite(decoId);
            if (sprite == null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                var mainDecoTf = canvas.transform.Find("MainDecoration");
                if (mainDecoTf != null)
                {
                    var img = mainDecoTf.GetComponent<Image>();
                    if (img != null)
                    {
                        img.sprite = sprite;
                        img.enabled = true;
                        img.color = Color.white;
                        img.preserveAspect = true;
                    }
                }
            }
        }

        private void UpdateSlot(Transform slotTf, CollectibleItem item, bool isBg)
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
            bool isEquipped = gameManager != null && 
                (isBg ? gameManager.EquippedBackgroundId == item.id : gameManager.EquippedDecorationId == item.id);

            if (img != null)
            {
                img.sprite = unlocked ? item.displaySprite : null;
                if (!unlocked)
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Solid dark gray when locked
                }
                else if (item.id == "bg-none")
                {
                    img.color = Color.white; // Solid white for 기본 배경
                }
                else if (item.displaySprite == null)
                {
                    img.color = new Color(0.12f, 0.15f, 0.22f, 1f); // Nice soft placeholder slate color
                }
                else
                {
                    img.color = Color.white;
                }
                img.preserveAspect = !isBg; // Stretch backgrounds to fill, preserve aspect ratio for decorations
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
                unknownTxt.text = unlocked ? "" : "?";
            }

            // Optional: Toggle equipped border or indicator
            var eqIndicator = slotTf.Find("Equipped")?.gameObject ?? slotTf.Find("EquipHighlight")?.gameObject ?? slotTf.Find("Active")?.gameObject;
            if (eqIndicator != null)
            {
                eqIndicator.SetActive(unlocked && isEquipped);
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

        private Sprite CreateSolidColorSprite(Color color, int width = 16, int height = 16)
        {
            var tex = new Texture2D(width, height);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateDecorationSprite(Color color)
        {
            int w = 64, h = 64;
            var tex = new Texture2D(w, h);
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (Mathf.Abs(x - w / 2) + Mathf.Abs(y - h / 2) < w / 2 - 4)
                    {
                        pixels[y * w + x] = color;
                    }
                    else
                    {
                        pixels[y * w + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
        }

        private void InitializeDefaultItems()
        {
            backgrounds.Clear();
            decorations.Clear();

            // 1. Add Default "기본 배경" (unlocked by default)
            backgrounds.Add(new CollectibleItem
            {
                id = "bg-none",
                displayName = "기본 배경",
                description = "장착된 배경을 해제하고 심플한 흰색 단색 배경으로 변경합니다.",
                unlockSetId = "", // unlocked by default
                displaySprite = null // empty
            });

            // Generate 10 solid color backgrounds (unlocked by default)
            Color[] bgColors = new Color[]
            {
                new Color(0.85f, 0.35f, 0.35f), // Soft Red
                new Color(0.92f, 0.55f, 0.25f), // Soft Orange
                new Color(0.92f, 0.82f, 0.25f), // Soft Yellow
                new Color(0.35f, 0.75f, 0.35f), // Soft Green
                new Color(0.25f, 0.55f, 0.85f), // Soft Blue
                new Color(0.45f, 0.25f, 0.75f), // Purple
                new Color(0.85f, 0.45f, 0.65f), // Pink
                new Color(0.25f, 0.75f, 0.75f), // Cyan
                new Color(0.55f, 0.35f, 0.15f), // Brown
                new Color(0.35f, 0.35f, 0.35f)  // Gray
            };

            for (int i = 0; i < bgColors.Length; i++)
            {
                var sprite = CreateSolidColorSprite(bgColors[i], 128, 128);
                backgrounds.Add(new CollectibleItem
                {
                    id = $"test-bg-{i + 1}",
                    displayName = $"테스트 배경 {i + 1}",
                    description = $"테스트용 단색 배경 {i + 1} 입니다.",
                    unlockSetId = "", // Empty means unlocked by default
                    displaySprite = sprite
                });
            }

            // 2. Add Default "No Decoration" (unlocked by default)
            decorations.Add(new CollectibleItem
            {
                id = "deco-none",
                displayName = "장식 없음",
                description = "장착된 장식품을 해제합니다.",
                unlockSetId = "", // unlocked by default
                displaySprite = null // empty
            });

            // Generate 5 diamond decorations (unlocked by default)
            Color[] decoColors = new Color[]
            {
                new Color(0.95f, 0.25f, 0.25f), // Red
                new Color(0.25f, 0.95f, 0.25f), // Green
                new Color(0.25f, 0.25f, 0.95f), // Blue
                new Color(0.95f, 0.95f, 0.25f), // Yellow
                new Color(0.95f, 0.25f, 0.95f)  // Magenta
            };

            for (int i = 0; i < decoColors.Length; i++)
            {
                var sprite = CreateDecorationSprite(decoColors[i]);
                decorations.Add(new CollectibleItem
                {
                    id = $"test-deco-{i + 1}",
                    displayName = $"테스트 장식 {i + 1}",
                    description = $"테스트용 다이아몬드 장식품 {i + 1} 입니다.",
                    unlockSetId = "", // Empty means unlocked by default
                    displaySprite = sprite
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  DETAIL POPUP LOGIC
        // ════════════════════════════════════════════════════════════════════
        private void OpenDetail(CollectibleItem item, bool isBg)
        {
            selectedItem = item;
            selectedIsBg = isBg;

            if (detailRoot == null)
            {
                detailRoot = transform.Find("DetailPopup")?.gameObject ?? 
                             transform.Find("DetailPanel")?.gameObject ??
                             transform.Find("Panel/DetailPopup")?.gameObject ?? 
                             transform.Find("Panel/DetailPanel")?.gameObject;
                
                if (detailRoot == null)
                {
                    BuildDetailPopup(transform.Find("Panel") ?? transform);
                }
            }

            if (detailRoot == null) return;

            // Wire detail components recursively if not assigned
            var rootTf = detailRoot.transform;
            if (detailImage == null)
                detailImage = (rootTf.Find("DetailArt") ?? FindChildByNameRecursive(rootTf, "DetailArt") ?? 
                               FindChildByNameRecursive(rootTf, "DetailImage") ?? FindChildByNameRecursive(rootTf, "Art"))?.GetComponent<Image>();

            if (detailName == null)
                detailName = (rootTf.Find("DetailName") ?? FindChildByNameRecursive(rootTf, "DetailName") ??
                              FindChildByNameRecursive(rootTf, "NameText") ?? FindChildByNameRecursive(rootTf, "Name"))?.GetComponent<TMP_Text>();

            if (detailDesc == null)
                detailDesc = (rootTf.Find("DetailDesc") ?? FindChildByNameRecursive(rootTf, "DetailDesc") ??
                              FindChildByNameRecursive(rootTf, "DescText") ?? FindChildByNameRecursive(rootTf, "Desc"))?.GetComponent<TMP_Text>();

            if (detailEquipBtn == null)
                detailEquipBtn = (rootTf.Find("Btn_장착하기") ?? FindChildByNameRecursive(rootTf, "Btn_장착하기") ?? 
                                  FindChildByNameRecursive(rootTf, "Btn_Equip") ?? FindChildByNameRecursive(rootTf, "Equip"))?.GetComponent<Button>();

            // Hide breakthrough or other buttons if any
            var breakBtn = rootTf.Find("Btn_한계 돌파") ?? FindChildByNameRecursive(rootTf, "Btn_한계 돌파") ?? FindChildByNameRecursive(rootTf, "Btn_Breakthrough");
            if (breakBtn != null) breakBtn.gameObject.SetActive(false);

            // Bind Close Button in detail popup
            var closePopupBtn = rootTf.Find("Btn_✕") ?? FindChildByNameRecursive(rootTf, "Btn_✕") ?? FindChildByNameRecursive(rootTf, "CloseBtn");
            if (closePopupBtn != null)
            {
                var b = closePopupBtn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => detailRoot.SetActive(false));
                }
            }

            bool unlocked = gameManager != null && (string.IsNullOrEmpty(item.unlockSetId) || gameManager.IsSetCompleted(item.unlockSetId));
            bool isEquipped = gameManager != null && 
                (isBg ? gameManager.EquippedBackgroundId == item.id : gameManager.EquippedDecorationId == item.id);

            if (detailImage != null)
            {
                detailImage.enabled = true;
                detailImage.sprite = unlocked ? item.displaySprite : null;
                if (!unlocked)
                {
                    detailImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                }
                else if (item.id == "bg-none")
                {
                    detailImage.color = Color.white; // Solid white for 기본 배경
                }
                else if (item.displaySprite == null)
                {
                    detailImage.color = new Color(0.12f, 0.15f, 0.22f, 1f); // Nice soft placeholder slate color
                }
                else
                {
                    detailImage.color = Color.white;
                }
                detailImage.preserveAspect = !isBg; // Stretch backgrounds, preserve ratio for decorations
            }

            if (detailName != null)
            {
                detailName.text = unlocked ? item.displayName : "???";
            }

            if (detailDesc != null)
            {
                detailDesc.text = unlocked ? item.description : "???";
            }

            if (detailEquipBtn != null)
            {
                detailEquipBtn.gameObject.SetActive(unlocked);
                var btnTxt = detailEquipBtn.GetComponentInChildren<TMP_Text>();
                if (btnTxt != null)
                {
                    btnTxt.text = isEquipped ? "장착됨" : "장착하기";
                }
                detailEquipBtn.onClick.RemoveAllListeners();
                detailEquipBtn.onClick.AddListener(OnEquipClicked);

                // Set button state
                detailEquipBtn.interactable = unlocked && !isEquipped;
                var cg = GetOrAddComponent<CanvasGroup>(detailEquipBtn.gameObject);
                cg.alpha = (unlocked && !isEquipped) ? 1f : 0.5f;
            }

            if (detailRoot != null)
            {
                detailRoot.transform.SetAsLastSibling();
                detailRoot.SetActive(true);
            }
        }

        private void OnEquipClicked()
        {
            if (selectedItem == null || gameManager == null) return;

            if (selectedIsBg)
            {
                gameManager.EquipBackground(selectedItem.id);
            }
            else
            {
                gameManager.EquipDecoration(selectedItem.id);
            }

            Refresh();
            
            // Re-open detail to update button text
            if (detailRoot != null && detailRoot.activeSelf)
            {
                OpenDetail(selectedItem, selectedIsBg);
            }
        }

        private void BuildDetailPopup(Transform parent)
        {
            detailRoot = MakePanel(parent, new Vector2(0, 0), new Vector2(640, 420));
            detailRoot.name = "DetailPopup";
            var dImg = detailRoot.GetComponent<Image>();
            dImg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            detailRoot.SetActive(false);

            // Left – Image
            detailImage = MakeImage(detailRoot.transform, "DetailArt",
                new Vector2(-165, 10), new Vector2(220, 260), Color.gray).GetComponent<Image>();

            // Right – Texts
            detailName = MakeText(detailRoot.transform, "???",
                new Vector2(110, 155), new Vector2(350, 40), 18, Color.white);
            detailName.fontStyle = FontStyles.Bold;

            detailDesc = MakeText(detailRoot.transform, "???",
                new Vector2(110, 55), new Vector2(350, 150), 13, new Color(0.8f, 0.8f, 0.85f));
            detailDesc.alignment = TextAlignmentOptions.TopLeft;
            detailDesc.textWrappingMode = TextWrappingModes.Normal;

            // Equip Button
            detailEquipBtn = MakeButton(detailRoot.transform, "장착하기",
                new Vector2(110, -130), new Vector2(160, 44), new Color(0.18f, 0.52f, 0.28f), OnEquipClicked).GetComponent<Button>();

            // Close Button
            MakeButton(detailRoot.transform, "✕", new Vector2(290, 180), new Vector2(36, 36),
                new Color(0.7f, 0.2f, 0.2f), () => detailRoot.SetActive(false));
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

        // Disabled BuildUI
        public void BuildUI()
        {
        }

        private void AutoWireFields()
        {
            if (bgTabBtn == null)
            {
                bgTabBtn = transform.Find("Panel/Btn_배경")?.GetComponent<Button>() ?? 
                           transform.Find("Btn_배경")?.GetComponent<Button>() ?? 
                           FindChildContains<Button>("배경") ??
                           FindChildContains<Button>("bg");
            }
            if (decoTabBtn == null)
            {
                decoTabBtn = transform.Find("Panel/Btn_장식품")?.GetComponent<Button>() ?? 
                             transform.Find("Btn_장식품")?.GetComponent<Button>() ?? 
                             FindChildContains<Button>("장식") ??
                             FindChildContains<Button>("deco");
            }
            if (closeBtn == null)
            {
                closeBtn = transform.Find("Panel/Btn_✕")?.GetComponent<Button>() ?? 
                           transform.Find("Btn_✕")?.GetComponent<Button>() ?? 
                           FindChildContains<Button>("닫기") ??
                           FindChildContains<Button>("✕") ??
                           FindChildContains<Button>("close") ??
                           FindChildContains<Button>("Close") ??
                           FindChildContains<Button>("Btn_✕") ??
                           FindChildContains<Button>("x");
            }

            if (bgPanel == null) bgPanel = transform.Find("Panel/BgPanel")?.gameObject ?? transform.Find("BgPanel")?.gameObject ?? FindChildNameContains("BgPanel");
            if (decoPanel == null) decoPanel = transform.Find("Panel/DecoPanel")?.gameObject ?? transform.Find("DecoPanel")?.gameObject ?? FindChildNameContains("DecoPanel");
        }

        private T FindChildContains<T>(string keyword) where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            foreach (var c in components)
            {
                if (c.name.ToLower().Contains(keyword.ToLower())) return c;
            }
            return null;
        }

        private GameObject FindChildNameContains(string keyword)
        {
            var transforms = GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                // Skip self to prevent matching own GameObject
                if (t == transform) continue;
                if (t.name.ToLower().Contains(keyword.ToLower())) return t.gameObject;
            }
            return null;
        }

        private Transform FindChildByNameRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildByNameRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
        private T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (go == null) return null;
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }

        private void EnsureLayoutAndScrolling(Transform parent, Vector2 templateSize)
        {
            if (parent == null) return;

            // Detect scrolling direction from parent ScrollRect if present
            var scrollRect = parent.GetComponentInParent<ScrollRect>();
            bool isVertical = scrollRect == null || scrollRect.vertical;

            // Get or add GridLayoutGroup for perfect alignment
            var grid = GetOrAddComponent<GridLayoutGroup>(parent.gameObject);
            grid.cellSize = templateSize;
            grid.spacing = new Vector2(25f, 25f);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.UpperLeft;

            // Use Flexible constraint so columns/rows auto-calculate based on the scroll view/page size!
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = isVertical ? GridLayoutGroup.Axis.Horizontal : GridLayoutGroup.Axis.Vertical;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            // Only add ContentSizeFitter if it's a sub-container (like ScrollRect's Content)
            // and NOT the main page panels or objects named "Page" themselves, to preserve their 1400x710 dimensions.
            bool isMainPanel = (bgPanel != null && parent.gameObject == bgPanel) || 
                               (decoPanel != null && parent.gameObject == decoPanel) ||
                               parent.name.ToLower().Contains("page");

            if (!isMainPanel)
            {
                var fitter = GetOrAddComponent<ContentSizeFitter>(parent.gameObject);
                fitter.horizontalFit = !isVertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = isVertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            }
            else
            {
                // Clean up any ContentSizeFitter added to the main panel
                var fitter = parent.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    Destroy(fitter);
                }
            }
        }
    }
}
