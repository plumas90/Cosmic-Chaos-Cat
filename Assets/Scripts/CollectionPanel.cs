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
        [TextArea(2, 4)]
        public string description;
        public string unlockSetId; // SetId required to unlock (from SetCatalogSO)
        public CardRarity rarity = CardRarity.N;
        public bool isTestUnlocked = true; // Unlocked by default for testing
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

        [Header("Title & Tabs")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Text titleTextUi;
        [SerializeField] private Button bgTabBtn;
        [SerializeField] private Button decoTabBtn;
        [SerializeField] private GameObject bgPanel;
        [SerializeField] private GameObject decoPanel;
        [SerializeField] private Button closeBtn;

        [Header("Pagination Controls")]
        [SerializeField] private Button prevPageBtn;
        [SerializeField] private Button nextPageBtn;
        [SerializeField] private TMP_Text pageText;
        [SerializeField] private Text pageTextUi;

        [Header("Rarity Frame & Mark Sprites")]
        [SerializeField] private Sprite spriteFrameN;
        [SerializeField] private Sprite spriteFrameR;
        [SerializeField] private Sprite spriteFrameSR;
        [SerializeField] private Sprite spriteFrameSSR;
        [SerializeField] private Sprite spriteFrameLock;
        [SerializeField] private Sprite spriteMarkN;
        [SerializeField] private Sprite spriteMarkR;
        [SerializeField] private Sprite spriteMarkSR;
        [SerializeField] private Sprite spriteMarkSSR;
        [SerializeField] private Sprite spriteLockOverlay;

        [Header("Items Data")]
        [SerializeField] private List<CollectibleItem> backgrounds = new List<CollectibleItem>();
        [SerializeField] private List<CollectibleItem> decorations = new List<CollectibleItem>();

        [Header("Detail Popup")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image detailFrameImage;
        [SerializeField] private Image detailImage;
        [SerializeField] private Image detailRarityMark;
        [SerializeField] private TMP_Text detailName;
        [SerializeField] private TMP_Text detailDesc;
        [SerializeField] private Button detailEquipBtn;
        [SerializeField] private TMP_Text detailEquipText;

        private bool showBgTab = true;
        private int bgPageIndex = 0;
        private int decoPageIndex = 0;
        private const int SLOTS_PER_PAGE = 12;

        private static TMP_FontAsset defaultFont;
        private bool _isInitialized = false;

        // Slot pools
        private GameObject bgSlotTemplate;
        private GameObject decoSlotTemplate;
        private readonly List<GameObject> bgSlots = new List<GameObject>();
        private readonly List<GameObject> decoSlots = new List<GameObject>();

        private CollectibleItem selectedItem;
        private bool selectedIsBg;

        // Tab Color Styles
        private static readonly Color TabActive = new Color(0.6f, 0.2f, 0.6f, 1f);
        private static readonly Color TabInactive = new Color(0.25f, 0.25f, 0.35f, 1f);

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
            EnsureInit();
            AutoWireFields();

            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
                gameManager.StateChanged += Refresh;
            }

            // Bind tab click listeners
            if (bgTabBtn != null)
            {
                bgTabBtn.onClick.RemoveAllListeners();
                bgTabBtn.onClick.AddListener(() => { showBgTab = true; bgPageIndex = 0; Refresh(); });
            }
            if (decoTabBtn != null)
            {
                decoTabBtn.onClick.RemoveAllListeners();
                decoTabBtn.onClick.AddListener(() => { showBgTab = false; decoPageIndex = 0; Refresh(); });
            }
            if (closeBtn != null)
            {
                closeBtn.transform.SetAsLastSibling();
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => gameObject.SetActive(false));
            }
            if (prevPageBtn != null)
            {
                prevPageBtn.onClick.RemoveAllListeners();
                prevPageBtn.onClick.AddListener(OnPrevPageClicked);
            }
            if (nextPageBtn != null)
            {
                nextPageBtn.onClick.RemoveAllListeners();
                nextPageBtn.onClick.AddListener(OnNextPageClicked);
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
            if (string.IsNullOrEmpty(id) || id == "bg-none" || id == "bg")
            {
                var defaultItem = backgrounds.Find(x => x.id == "bg" || x.id == "bg-none");
                if (defaultItem != null && defaultItem.displaySprite != null) return defaultItem.displaySprite;
            }
            var item = backgrounds.Find(x => x.id == id);
            return item?.displaySprite;
        }

        public Sprite GetDecorationSprite(string id)
        {
            EnsureInit();
            var item = decorations.Find(x => x.id == id);
            return item?.displaySprite;
        }

        public Sprite GetFrameSpriteForRarity(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.N: return spriteFrameN;
                case CardRarity.R: return spriteFrameR != null ? spriteFrameR : spriteFrameN;
                case CardRarity.SR: return spriteFrameSR != null ? spriteFrameSR : spriteFrameN;
                case CardRarity.SSR: return spriteFrameSSR != null ? spriteFrameSSR : spriteFrameN;
                case CardRarity.UR: return spriteFrameSSR != null ? spriteFrameSSR : spriteFrameN;
                default: return spriteFrameN;
            }
        }

        public Sprite GetMarkSpriteForRarity(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.N: return spriteMarkN;
                case CardRarity.R: return spriteMarkR != null ? spriteMarkR : spriteMarkN;
                case CardRarity.SR: return spriteMarkSR != null ? spriteMarkSR : spriteMarkN;
                case CardRarity.SSR: return spriteMarkSSR != null ? spriteMarkSSR : spriteMarkN;
                case CardRarity.UR: return spriteMarkSSR != null ? spriteMarkSSR : spriteMarkN;
                default: return spriteMarkN;
            }
        }

        private void OnPrevPageClicked()
        {
            if (showBgTab)
            {
                if (bgPageIndex > 0) { bgPageIndex--; Refresh(); }
            }
            else
            {
                if (decoPageIndex > 0) { decoPageIndex--; Refresh(); }
            }
        }

        private void OnNextPageClicked()
        {
            var itemList = showBgTab ? backgrounds : decorations;
            int maxPages = Mathf.Max(1, Mathf.CeilToInt(itemList.Count / (float)SLOTS_PER_PAGE));

            if (showBgTab)
            {
                if (bgPageIndex < maxPages - 1) { bgPageIndex++; Refresh(); }
            }
            else
            {
                if (decoPageIndex < maxPages - 1) { decoPageIndex++; Refresh(); }
            }
        }

        private void InitSlots()
        {
            // Initialize Background slots
            if (bgPanel != null && bgSlots.Count == 0)
            {
                var children = bgPanel.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child != bgPanel.transform && child.name.ToLower().Contains("slot") && !child.name.ToLower().Contains("template"))
                    {
                        if (!bgSlots.Contains(child.gameObject)) bgSlots.Add(child.gameObject);
                    }
                }

                if (bgSlots.Count == 0)
                {
                    bgSlotTemplate = FindTemplateSlot(bgPanel.transform);
                    if (bgSlotTemplate != null)
                    {
                        bgSlotTemplate.SetActive(false);
                        var parent = bgSlotTemplate.transform.parent;

                        for (int i = 0; i < SLOTS_PER_PAGE; i++)
                        {
                            var clone = Instantiate(bgSlotTemplate, parent);
                            clone.name = $"BgSlot_{i}";
                            bgSlots.Add(clone);
                        }
                    }
                }
            }

            // Initialize Decoration slots
            if (decoPanel != null && decoSlots.Count == 0)
            {
                var children = decoPanel.GetComponentsInChildren<Transform>(true);
                foreach (var child in children)
                {
                    if (child != decoPanel.transform && child.name.ToLower().Contains("slot") && !child.name.ToLower().Contains("template"))
                    {
                        if (!decoSlots.Contains(child.gameObject)) decoSlots.Add(child.gameObject);
                    }
                }

                if (decoSlots.Count == 0)
                {
                    decoSlotTemplate = FindTemplateSlot(decoPanel.transform);
                    if (decoSlotTemplate != null)
                    {
                        decoSlotTemplate.SetActive(false);
                        var parent = decoSlotTemplate.transform.parent;

                        for (int i = 0; i < SLOTS_PER_PAGE; i++)
                        {
                            var clone = Instantiate(decoSlotTemplate, parent);
                            clone.name = $"DecoSlot_{i}";
                            decoSlots.Add(clone);
                        }
                    }
                }
            }
        }

        private Vector2 GetSlotCellSize(GameObject template)
        {
            var rt = template.GetComponent<RectTransform>();
            return rt != null ? rt.sizeDelta : new Vector2(130f, 160f);
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

        private void SetTitleText(string text)
        {
            if (titleText != null && titleText.text != text) titleText.text = text;
            if (titleTextUi != null && titleTextUi.text != text) titleTextUi.text = text;
        }

        private void SetPageText(string text)
        {
            if (pageText != null && pageText.text != text) pageText.text = text;
            if (pageTextUi != null && pageTextUi.text != text) pageTextUi.text = text;
        }

        public void Refresh()
        {
            AutoWireFields();
            InitSlots();

            // Active Panel Toggle
            if (bgPanel != null) bgPanel.SetActive(showBgTab);
            if (decoPanel != null) decoPanel.SetActive(!showBgTab);

            // Title Text Update
            SetTitleText(showBgTab ? "배경" : "장식품");

            // Update Tab Button Styles
            UpdateTabButtonStyles();

            // Calculate pagination parameters
            var currentSlots = showBgTab ? bgSlots : decoSlots;
            int slotsPerPage = currentSlots.Count > 0 ? currentSlots.Count : SLOTS_PER_PAGE;
            var itemList = showBgTab ? backgrounds : decorations;
            int maxPages = Mathf.Max(1, Mathf.CeilToInt(itemList.Count / (float)slotsPerPage));
            int curPageIndex = showBgTab ? bgPageIndex : decoPageIndex;
            curPageIndex = Mathf.Clamp(curPageIndex, 0, maxPages - 1);

            if (showBgTab) bgPageIndex = curPageIndex;
            else decoPageIndex = curPageIndex;

            // Page Text Update ("1 / N")
            string pageStr = $"{curPageIndex + 1} / {maxPages}";
            SetPageText(pageStr);

            // Prev / Next Page Buttons interactability
            if (prevPageBtn != null) prevPageBtn.interactable = curPageIndex > 0;
            if (nextPageBtn != null) nextPageBtn.interactable = curPageIndex < maxPages - 1;

            // Populate current page grid slots
            int startIdx = curPageIndex * slotsPerPage;

            for (int i = 0; i < currentSlots.Count; i++)
            {
                var slotGO = currentSlots[i];
                int itemIdx = startIdx + i;

                if (itemIdx < itemList.Count)
                {
                    slotGO.SetActive(true);
                    var item = itemList[itemIdx];
                    UpdateSlot(slotGO.transform, item, showBgTab);

                    var btn = GetOrAddComponent<Button>(slotGO);
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OpenDetail(item, showBgTab));
                }
                else
                {
                    slotGO.SetActive(false);
                }
            }

            // Update Main Background & Decoration in scene
            UpdateMainBackground();
            UpdateMainDecoration();
        }

        private void UpdateSlot(Transform slotTf, CollectibleItem item, bool isBg)
        {
            if (slotTf == null || item == null) return;

            bool unlocked = item.isTestUnlocked || string.IsNullOrEmpty(item.unlockSetId) || (gameManager != null && gameManager.IsSetCompleted(item.unlockSetId));
            bool isEquipped = gameManager != null && (isBg ? gameManager.EquippedBackgroundId == item.id : gameManager.EquippedDecorationId == item.id);

            // 1. Frame Image
            var frameImg = FindChildByNameRecursive(slotTf, "Frame")?.GetComponent<Image>() ??
                           FindChildByNameRecursive(slotTf, "frame")?.GetComponent<Image>() ??
                           FindChildByNameRecursive(slotTf, "DetailFrame")?.GetComponent<Image>();

            if (frameImg != null)
            {
                if (unlocked)
                {
                    if (!frameImg.gameObject.activeSelf) frameImg.gameObject.SetActive(true);
                    frameImg.enabled = true;
                    Sprite frameSp = GetFrameSpriteForRarity(item.rarity);
                    if (frameSp != null)
                    {
                        if (frameImg.sprite != frameSp) frameImg.sprite = frameSp;
                        if (frameImg.color != Color.white) frameImg.color = Color.white;
                    }
                    else
                    {
                        Color rarityCol = (Color)RarityToColor(item.rarity);
                        if (frameImg.color != rarityCol) frameImg.color = rarityCol;
                    }
                }
                else
                {
                    // Hide Frame completely when locked
                    if (frameImg.gameObject.activeSelf) frameImg.gameObject.SetActive(false);
                    frameImg.enabled = false;
                }
            }

            // 2. Art Image
            var artImg = FindChildByNameRecursive(slotTf, "Art")?.GetComponent<Image>() ??
                         FindChildByNameRecursive(slotTf, "art")?.GetComponent<Image>() ??
                         FindChildByNameRecursive(slotTf, "DetailArt")?.GetComponent<Image>() ??
                         FindChildByNameRecursive(slotTf, "Image")?.GetComponent<Image>();

            // 3. Lock Overlay Object Search (Recursive)
            var lockObj = FindChildByNameRecursive(slotTf, "Lock") ??
                          FindChildByNameRecursive(slotTf, "lock") ??
                          FindChildByNameRecursive(slotTf, "Unknown") ??
                          FindChildByNameRecursive(slotTf, "LockOverlay") ??
                          FindChildByNameRecursive(slotTf, "lockoverlay") ??
                          FindChildByNameRecursive(slotTf, "LockIcon") ??
                          FindChildByNameRecursive(slotTf, "lockicon");

            if (unlocked)
            {
                if (artImg != null)
                {
                    if (!artImg.gameObject.activeSelf) artImg.gameObject.SetActive(true);
                    artImg.transform.localScale = Vector3.one; // Normal scale (1.0x)
                    if (artImg.sprite != item.displaySprite && item.displaySprite != null) artImg.sprite = item.displaySprite;
                    if (artImg.color != Color.white) artImg.color = Color.white;
                    artImg.preserveAspect = !isBg;
                }
                if (lockObj != null) lockObj.gameObject.SetActive(false);
            }
            else
            {
                if (lockObj != null)
                {
                    if (!lockObj.gameObject.activeSelf) lockObj.gameObject.SetActive(true);
                    lockObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f); // 1.1x scale for lock
                    var lockImg = lockObj.GetComponent<Image>() ?? lockObj.GetComponentInChildren<Image>();
                    if (lockImg != null && spriteLockOverlay != null)
                    {
                        lockImg.sprite = spriteLockOverlay;
                        lockImg.color = Color.white;
                    }
                }

                if (artImg != null)
                {
                    if (!artImg.gameObject.activeSelf) artImg.gameObject.SetActive(true);
                    artImg.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f); // 1.1x scale for locked art
                    if (spriteLockOverlay != null && lockObj == null)
                    {
                        artImg.sprite = spriteLockOverlay;
                        artImg.color = Color.white;
                    }
                    else if (item.displaySprite != null)
                    {
                        artImg.sprite = item.displaySprite;
                        artImg.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
                    }
                }
            }

            // 4. Rarity Mark Image
            var markImg = FindChildByNameRecursive(slotTf, "rereMark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "reremark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "rareMark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "raremark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "RarityMark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "rarityMark")?.GetComponent<Image>() ??
                          FindChildByNameRecursive(slotTf, "rare_mark")?.GetComponent<Image>();

            if (markImg != null)
            {
                if (unlocked)
                {
                    if (!markImg.gameObject.activeSelf) markImg.gameObject.SetActive(true);
                    Sprite markSp = GetMarkSpriteForRarity(item.rarity);
                    if (markSp != null)
                    {
                        if (markImg.sprite != markSp) markImg.sprite = markSp;
                        if (markImg.color != Color.white) markImg.color = Color.white;
                    }
                }
                else
                {
                    if (markImg.gameObject.activeSelf) markImg.gameObject.SetActive(false);
                }
            }

            // 5. Name Text
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

            if (nameTxt != null)
            {
                string targetText = unlocked ? item.displayName : "미해금";
                if (nameTxt.text != targetText) nameTxt.text = targetText;
            }

            // 6. Equipped Highlight
            var eqIndicator = slotTf.Find("Equipped")?.gameObject ??
                              slotTf.Find("EquipHighlight")?.gameObject ??
                              slotTf.Find("Active")?.gameObject;
            if (eqIndicator != null)
            {
                bool showEq = unlocked && isEquipped;
                if (eqIndicator.activeSelf != showEq) eqIndicator.SetActive(showEq);
            }

            // 7. Hover Color
            var slotBtn = slotTf.GetComponent<Button>();
            if (slotBtn != null && slotBtn.transition == Selectable.Transition.ColorTint)
            {
                var cs = slotBtn.colors;
                cs.highlightedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                cs.pressedColor     = new Color(0.08f, 0.08f, 0.08f, 1f);
                slotBtn.colors      = cs;
            }
        }

        private Color32 RarityToColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N:   return new Color32(180, 180, 180, 255);
                case CardRarity.R:   return new Color32( 80, 160, 240, 255);
                case CardRarity.SR:  return new Color32(180,  90, 240, 255);
                case CardRarity.SSR: return new Color32(255, 180,  40, 255);
                case CardRarity.UR:  return new Color32(255,  80, 100, 255);
                default:             return new Color32(255, 255, 255, 255);
            }
        }

        public void CloseDetailPopup()
        {
            if (detailRoot != null)
            {
                detailRoot.SetActive(false);
            }
        }

        public void WireDetailPopupCloseListeners()
        {
            if (detailRoot == null) return;

            // Remove button component from DetailPopupRoot background if present
            var bgBtn = detailRoot.GetComponent<Button>();
            if (bgBtn != null) Destroy(bgBtn);

            // Remove button component from DetailBox if present
            var boxTf = detailRoot.transform.Find("DetailBox") ?? detailRoot.transform.Find("box") ?? detailRoot.transform.Find("Box");
            if (boxTf != null)
            {
                var boxBtn = boxTf.GetComponent<Button>();
                if (boxBtn != null) Destroy(boxBtn);
            }

            var children = detailRoot.GetComponentsInChildren<Transform>(true);
            foreach (var ch in children)
            {
                if (ch == detailRoot.transform) continue;
                if (boxTf != null && ch == boxTf) continue;

                string nLower = ch.name.ToLower();
                bool isCloseBtn = nLower == "btn_✕" || nLower == "btn_btn_✕" || nLower == "btn_close"
                               || nLower == "closebtn" || nLower == "close_btn" || nLower == "close"
                               || nLower == "btn_닫기" || nLower == "닫기" || nLower == "btn_x" || nLower == "x";

                if (isCloseBtn)
                {
                    var btn = ch.GetComponent<Button>();
                    if (btn == null) btn = ch.gameObject.AddComponent<Button>();
                    var img = ch.GetComponent<Image>();
                    if (img != null) img.raycastTarget = true;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(CloseDetailPopup);
                }
            }
        }

        public void OpenDetail(CollectibleItem item, bool isBg)
        {
            if (item == null) return;
            selectedItem = item;
            selectedIsBg = isBg;

            if (detailRoot == null)
            {
                CreateDetailPopupInHierarchy();
            }

            if (detailRoot != null)
            {
                WireDetailPopupCloseListeners();
                detailRoot.SetActive(true);
                detailRoot.transform.SetAsLastSibling();

                bool unlocked = item.isTestUnlocked || string.IsNullOrEmpty(item.unlockSetId) || (gameManager != null && gameManager.IsSetCompleted(item.unlockSetId));
                bool isEquipped = gameManager != null && (isBg ? gameManager.EquippedBackgroundId == item.id : gameManager.EquippedDecorationId == item.id);

                if (detailImage != null)
                {
                    if (unlocked)
                    {
                        detailImage.transform.localScale = Vector3.one;
                        detailImage.sprite = item.displaySprite;
                        detailImage.color = Color.white;
                    }
                    else
                    {
                        detailImage.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                        detailImage.sprite = spriteLockOverlay != null ? spriteLockOverlay : item.displaySprite;
                        detailImage.color = spriteLockOverlay != null ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.6f);
                    }
                    detailImage.preserveAspect = !isBg;
                }

                if (detailFrameImage != null)
                {
                    if (unlocked)
                    {
                        if (!detailFrameImage.gameObject.activeSelf) detailFrameImage.gameObject.SetActive(true);
                        Sprite frameSp = GetFrameSpriteForRarity(item.rarity);
                        if (frameSp != null)
                        {
                            detailFrameImage.sprite = frameSp;
                            detailFrameImage.color = Color.white;
                        }
                    }
                    else
                    {
                        if (detailFrameImage.gameObject.activeSelf) detailFrameImage.gameObject.SetActive(false);
                    }
                }

                if (detailRarityMark != null)
                {
                    Sprite markSp = unlocked ? GetMarkSpriteForRarity(item.rarity) : null;
                    if (markSp != null)
                    {
                        detailRarityMark.sprite = markSp;
                        detailRarityMark.color = Color.white;
                        detailRarityMark.gameObject.SetActive(true);
                    }
                    else
                    {
                        detailRarityMark.gameObject.SetActive(false);
                    }
                }

                if (detailName != null) detailName.text = unlocked ? item.displayName : "??? (미해금)";
                if (detailDesc != null) detailDesc.text = unlocked ? item.description : $"세트 ID '{item.unlockSetId}' 수집 완료 시 해금됩니다.";

                if (detailEquipBtn != null)
                {
                    detailEquipBtn.onClick.RemoveAllListeners();
                    if (!unlocked)
                    {
                        if (detailEquipText != null) detailEquipText.text = "미해금";
                        detailEquipBtn.interactable = false;
                    }
                    else if (isEquipped)
                    {
                        if (detailEquipText != null) detailEquipText.text = "장착됨";
                        detailEquipBtn.interactable = false;
                    }
                    else
                    {
                        if (detailEquipText != null) detailEquipText.text = "장착하기";
                        detailEquipBtn.interactable = true;
                        detailEquipBtn.onClick.AddListener(() => EquipItem(item, isBg));
                    }
                }
            }
        }

        private void EquipItem(CollectibleItem item, bool isBg)
        {
            if (gameManager == null || item == null) return;
            if (isBg)
            {
                gameManager.EquipBackground(item.id);
            }
            else
            {
                gameManager.EquipDecoration(item.id);
            }

            Refresh();
            if (selectedItem == item) OpenDetail(item, isBg);
        }

        [ContextMenu("DetailPopupRoot 하이어라키에 생성 (에디터 전용)")]
        public GameObject CreateDetailPopupInHierarchy()
        {
            var existing = transform.Find("DetailPopupRoot");
            if (existing != null)
            {
                detailRoot = existing.gameObject;
                AutoWireFields();
                return detailRoot;
            }

            detailRoot = new GameObject("DetailPopupRoot");
            detailRoot.transform.SetParent(transform, false);

            var rt = detailRoot.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var bgImg = detailRoot.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.7f);

            var bgBtn = detailRoot.AddComponent<Button>();
            bgBtn.onClick.AddListener(() => detailRoot.SetActive(false));

            var cardBox = new GameObject("DetailBox");
            cardBox.transform.SetParent(detailRoot.transform, false);
            var boxRT = cardBox.AddComponent<RectTransform>();
            boxRT.sizeDelta = new Vector2(400f, 500f);

            var boxImg = cardBox.AddComponent<Image>();
            boxImg.color = new Color(0.12f, 0.15f, 0.23f, 0.98f);
            detailFrameImage = boxImg;

            // Frame Image (inside box)
            var frameGO = new GameObject("Frame");
            frameGO.transform.SetParent(cardBox.transform, false);
            var frameRT = frameGO.AddComponent<RectTransform>();
            frameRT.anchorMin = Vector2.zero; frameRT.anchorMax = Vector2.one;
            frameRT.offsetMin = Vector2.zero; frameRT.offsetMax = Vector2.zero;
            var frameImgComp = frameGO.AddComponent<Image>();
            frameImgComp.color = new Color(1f, 1f, 1f, 0f);

            // Art Image
            var artGO = new GameObject("Art");
            artGO.transform.SetParent(cardBox.transform, false);
            var artRT = artGO.AddComponent<RectTransform>();
            artRT.anchoredPosition = new Vector2(0, 70);
            artRT.sizeDelta = new Vector2(260f, 260f);
            detailImage = artGO.AddComponent<Image>();

            // Rarity Mark Image
            var markGO = new GameObject("rereMark");
            markGO.transform.SetParent(cardBox.transform, false);
            var markRT = markGO.AddComponent<RectTransform>();
            markRT.anchoredPosition = new Vector2(-150, 200);
            markRT.sizeDelta = new Vector2(50, 50);
            detailRarityMark = markGO.AddComponent<Image>();

            // Name Text
            detailName = MakeText(cardBox.transform, "Name", "---", new Vector2(0, -90), new Vector2(360, 40), 22, new Color(1f, 0.75f, 0.1f, 1f));

            // Description Text
            detailDesc = MakeText(cardBox.transform, "Desc", "---", new Vector2(0, -145), new Vector2(360, 60), 14, Color.white);

            // Equip Button
            var equipBtnGO = MakeButton(cardBox.transform, "Btn_장착하기", new Vector2(0, -205), new Vector2(160, 45), new Color(0.2f, 0.6f, 0.9f, 1f), null);
            detailEquipBtn = equipBtnGO.GetComponent<Button>();
            detailEquipText = equipBtnGO.GetComponentInChildren<TMP_Text>();

            // Close button inside box
            MakeButton(cardBox.transform, "Btn_✕", new Vector2(175, 225), new Vector2(36, 36), new Color(0.8f, 0.2f, 0.2f, 1f), () => detailRoot.SetActive(false));

            detailRoot.SetActive(false);
            AutoWireFields();
            return detailRoot;
        }

        private void UpdateTabButtonStyles()
        {
            // Preserve button colors and visual styles set in Unity Editor!
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
                        if (nameLower.Contains("panel") || nameLower.Contains("hud") || nameLower.Contains("popup")) continue;
                        if (nameLower.Contains("bg") || nameLower.Contains("background") || child.GetSiblingIndex() == 0)
                        {
                            img.sprite = sprite;
                            img.color = Color.white;
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

        private void InitializeDefaultItems()
        {
            Sprite LoadBgSprite(string fileName)
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/image/A_BG/{fileName}");
#else
                return null;
#endif
            }

            backgrounds = new List<CollectibleItem>
            {
                new CollectibleItem { id = "bg", displayName = "기본 배경", description = "기본으로 제공되는 고양이 방 테마 배경입니다.", unlockSetId = "", rarity = CardRarity.N, isTestUnlocked = true, displaySprite = LoadBgSprite("bg.png") },
                new CollectibleItem { id = "bg2", displayName = "사막 오아시스 배경", description = "세트 2 수집 완료 보상! 신비로운 사막 오아시스 테마 배경입니다.", unlockSetId = "2", rarity = CardRarity.R, isTestUnlocked = false, displaySprite = LoadBgSprite("bg2.png") },
                new CollectibleItem { id = "bg3", displayName = "달콤한 디저트 배경", description = "세트 3 수집 완료 보상! 달콤한 과자와 케이크 테마 배경입니다.", unlockSetId = "3", rarity = CardRarity.R, isTestUnlocked = false, displaySprite = LoadBgSprite("bg3.png") },
                new CollectibleItem { id = "bg4", displayName = "심해 바다 배경", description = "세트 4 수집 완료 보상! 신비로운 푸른 심해 수족관 테마 배경입니다.", unlockSetId = "4", rarity = CardRarity.SR, isTestUnlocked = false, displaySprite = LoadBgSprite("bg4.png") },
                new CollectibleItem { id = "bg5", displayName = "저자거리 축제 배경", description = "세트 5 수집 완료 보상! 흥겨운 민속 저자거리 축제 테마 배경입니다.", unlockSetId = "5", rarity = CardRarity.SR, isTestUnlocked = false, displaySprite = LoadBgSprite("bg5.png") }
            };

            if (decorations == null || decorations.Count == 0)
            {
                decorations = new List<CollectibleItem>
                {
                    new CollectibleItem { id = "deco-none", displayName = "장식 없음", description = "배경에 장식을 배치하지 않습니다.", unlockSetId = "", rarity = CardRarity.N, displaySprite = null },
                    new CollectibleItem { id = "deco-cat-house", displayName = "푹신한 캣타워", description = "고양이들이 좋아하는 푹신한 캣타워 장식입니다.", unlockSetId = "1", rarity = CardRarity.N, displaySprite = CreateDecorationSprite(new Color(0.8f, 0.6f, 0.4f, 1f)) },
                    new CollectibleItem { id = "deco-pyramid", displayName = "미니 피라미드", description = "사막 오아시스 수호 피라미드 오브제입니다.", unlockSetId = "2", rarity = CardRarity.R, displaySprite = CreateDecorationSprite(new Color(0.9f, 0.8f, 0.3f, 1f)) },
                    new CollectibleItem { id = "deco-cake", displayName = "3단 디저트 케이크", description = "달콤한 생크림 케이크 장식입니다.", unlockSetId = "3", rarity = CardRarity.R, displaySprite = CreateDecorationSprite(new Color(0.9f, 0.5f, 0.7f, 1f)) },
                    new CollectibleItem { id = "deco-aquarium", displayName = "무지개 산호 수족관", description = "영롱한 해저 산호 수족관 장식입니다.", unlockSetId = "4", rarity = CardRarity.SR, displaySprite = CreateDecorationSprite(new Color(0.3f, 0.7f, 0.9f, 1f)) },
                    new CollectibleItem { id = "deco-drum", displayName = "사물놀이 북", description = "축제의 흥을 끌어올리는 사물놀이 북 장식입니다.", unlockSetId = "5", rarity = CardRarity.SR, displaySprite = CreateDecorationSprite(new Color(0.8f, 0.4f, 0.2f, 1f)) },
                    new CollectibleItem { id = "deco-robot", displayName = "골든 라이온 로봇", description = "위풍당당한 사령관 변신 로봇 피규어입니다.", unlockSetId = "6", rarity = CardRarity.SSR, displaySprite = CreateDecorationSprite(new Color(0.95f, 0.75f, 0.1f, 1f)) },
                    new CollectibleItem { id = "deco-airship", displayName = "스팀펑크 황금 비행선", description = "정교한 태엽 비행선 모형 장식입니다.", unlockSetId = "7", rarity = CardRarity.SSR, displaySprite = CreateDecorationSprite(new Color(0.7f, 0.45f, 0.2f, 1f)) },
                    new CollectibleItem { id = "deco-aurora", displayName = "사계절 오로라 수정구", description = "신비로운 사계절 오로라가 일렁이는 수정구입니다.", unlockSetId = "8", rarity = CardRarity.SSR, displaySprite = CreateDecorationSprite(new Color(0.4f, 0.8f, 0.6f, 1f)) }
                };
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
            return CreateSolidColorSprite(color, 32, 32);
        }

        public void BuildUI() { }

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

            if (titleText == null && titleTextUi == null)
            {
                var t = FindChildByNameRecursive(transform, "titleText") ??
                        FindChildByNameRecursive(transform, "TitleText") ??
                        FindChildByNameRecursive(transform, "panelTitle") ??
                        FindChildByNameRecursive(transform, "Title") ??
                        FindChildByNameRecursive(transform, "title") ??
                        FindChildByNameRecursive(transform, "HeaderTitle");
                if (t != null)
                {
                    titleText = t.GetComponent<TMP_Text>();
                    if (titleText == null) titleTextUi = t.GetComponent<Text>();
                }
            }

            if (prevPageBtn == null)
            {
                var b = FindChildContains<Button>("Btn_Prev") ??
                        FindChildContains<Button>("prevBtn") ??
                        FindChildContains<Button>("PrevBtn") ??
                        FindChildContains<Button>("Btn_Left") ??
                        FindChildContains<Button>("leftBtn");
                if (b != null) prevPageBtn = b;
            }

            if (nextPageBtn == null)
            {
                var b = FindChildContains<Button>("Btn_Next") ??
                        FindChildContains<Button>("nextBtn") ??
                        FindChildContains<Button>("NextBtn") ??
                        FindChildContains<Button>("Btn_Right") ??
                        FindChildContains<Button>("rightBtn");
                if (b != null) nextPageBtn = b;
            }

            if (pageText == null && pageTextUi == null)
            {
                var t = FindChildByNameRecursive(transform, "pageText") ??
                        FindChildByNameRecursive(transform, "PageText") ??
                        FindChildByNameRecursive(transform, "pageLabel") ??
                        FindChildByNameRecursive(transform, "PageLabel") ??
                        FindChildByNameRecursive(transform, "Page_Text") ??
                        FindChildByNameRecursive(transform, "Page_counter") ??
                        FindChildByNameRecursive(transform, "page_counter") ??
                        FindChildByNameRecursive(transform, "PageCounter") ??
                        FindChildByNameRecursive(transform, "Page") ??
                        FindChildByNameRecursive(transform, "page") ??
                        FindChildByNameRecursive(transform, "Text (TMP)");
                if (t != null)
                {
                    pageText = t.GetComponent<TMP_Text>();
                    if (pageText == null) pageTextUi = t.GetComponent<Text>();
                }
            }

            if (detailRoot == null)
            {
                var d = transform.Find("DetailPopupRoot")?.gameObject ?? transform.Find("DetailRoot")?.gameObject ?? transform.Find("DetailPanel")?.gameObject;
                if (d != null)
                {
                    detailRoot = d;
                    var rt = d.transform;
                    detailFrameImage = rt.Find("DetailBox")?.GetComponent<Image>() ?? rt.GetComponentInChildren<Image>();
                    detailImage = rt.Find("DetailBox/Art")?.GetComponent<Image>() ?? FindChildByNameRecursive(rt, "Art")?.GetComponent<Image>();
                    detailRarityMark = rt.Find("DetailBox/rereMark")?.GetComponent<Image>() ?? FindChildByNameRecursive(rt, "rereMark")?.GetComponent<Image>() ?? FindChildByNameRecursive(rt, "rareMark")?.GetComponent<Image>();
                    detailName = rt.Find("DetailBox/Name")?.GetComponent<TMP_Text>() ?? FindChildByNameRecursive(rt, "Name")?.GetComponent<TMP_Text>();
                    detailDesc = rt.Find("DetailBox/Desc")?.GetComponent<TMP_Text>() ?? FindChildByNameRecursive(rt, "Desc")?.GetComponent<TMP_Text>();
                    detailEquipBtn = rt.Find("DetailBox/Btn_장착하기")?.GetComponent<Button>() ?? FindChildContains<Button>("장착");
                    if (detailEquipBtn != null) detailEquipText = detailEquipBtn.GetComponentInChildren<TMP_Text>();
                }
            }

            if (detailRoot != null)
            {
                WireDetailPopupCloseListeners();
            }
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

        private static TMP_Text MakeText(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize, Color col)
        {
            var go = new GameObject(name);
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

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
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
            cs.highlightedColor = new Color(0.15f, 0.15f, 0.15f, 1f);
            cs.pressedColor     = new Color(0.08f, 0.08f, 0.08f, 1f);
            btn.colors          = cs;
            if (onClick != null) btn.onClick.AddListener(onClick);

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

        private void EnsureGridLayout(Transform parent, Vector2 templateSize)
        {
            // Preserve user's manual slot positions and layout settings configured in Unity Editor!
        }
    }
}
