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

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
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
            Color activeColor = new Color(0.6f, 0.2f, 0.6f, 1f); // Purple theme active
            Color inactiveColor = new Color(0.25f, 0.25f, 0.35f, 1f);

            if (bgTabBtn != null)
            {
                var img = bgTabBtn.GetComponent<Image>();
                if (img != null) img.color = showBgTab ? activeColor : inactiveColor;
            }

            if (decoTabBtn != null)
            {
                var img = decoTabBtn.GetComponent<Image>();
                if (img != null) img.color = !showBgTab ? activeColor : inactiveColor;
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

        private void AutoWireFields()
        {
            if (bgTabBtn == null) bgTabBtn = transform.Find("Tab_Bg")?.GetComponent<Button>() ?? transform.Find("Tab_배경")?.GetComponent<Button>() ?? FindChildContains<Button>("배경");
            if (decoTabBtn == null) decoTabBtn = transform.Find("Tab_Deco")?.GetComponent<Button>() ?? transform.Find("Tab_장식품")?.GetComponent<Button>() ?? FindChildContains<Button>("장식");
            if (closeBtn == null) closeBtn = transform.Find("CloseBtn")?.GetComponent<Button>() ?? transform.Find("Btn_Close")?.GetComponent<Button>() ?? FindChildContains<Button>("닫기");

            if (bgPanel == null) bgPanel = transform.Find("BgPanel")?.gameObject ?? transform.Find("Panel_Bg")?.gameObject ?? transform.Find("배경패널")?.gameObject;
            if (decoPanel == null) decoPanel = transform.Find("DecoPanel")?.gameObject ?? transform.Find("Panel_Deco")?.gameObject ?? transform.Find("장식패널")?.gameObject;
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
