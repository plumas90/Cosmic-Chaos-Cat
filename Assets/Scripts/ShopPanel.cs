using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public sealed class ShopPanel : MonoBehaviour
    {
        private GameManager gm;

        [SerializeField] private GameObject upgradesContent;
        [SerializeField] private GameObject shardContent;
        [SerializeField] private GameObject gachaUnlockContent;

        [SerializeField] private TMP_Text shardText;
        [SerializeField] private TMP_Text coinText;

        // Colors
        private static readonly Color BG = new Color(0.06f, 0.08f, 0.14f, 0.97f);
        private static readonly Color TabNormal = new Color(0.15f, 0.20f, 0.30f);
        private static readonly Color TabActive = new Color(0.25f, 0.35f, 0.50f);
        private static readonly Color BtnBuy = new Color(0.18f, 0.62f, 0.30f);
        private static readonly Color BtnLocked = new Color(0.40f, 0.40f, 0.40f);

        private readonly List<GameObject> upgradeRows = new List<GameObject>();
        private Button buyNBtn;
        private Button buyRBtn;
        private Button buySRBtn;
        private TMP_Text buyNText;
        private TMP_Text buyRText;
        private TMP_Text buySRText;

        private void Awake()
        {
            EnsureParentedToCanvas();
            gm = FindObjectOfType<GameManager>(true);
            BuildUI();
            if (coinText != null) BindListeners();
        }

        private void BindListeners()
        {
            var btns = GetComponentsInChildren<Button>(true);
            foreach (var b in btns)
            {
                var txt = b.GetComponentInChildren<TMP_Text>();
                if (txt != null)
                {
                    if (txt.text == "업그레이드") { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ShowTab(0)); }
                    else if (txt.text == "조각 교환") { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ShowTab(1)); }
                    else if (txt.text == "가챠 해금") { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ShowTab(2)); }
                    else if (txt.text == "✕") { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => gameObject.SetActive(false)); }
                    else if (txt.text.Contains("N급 뽑기")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => BuyRandomCard(CardRarity.N, 5)); }
                    else if (txt.text.Contains("R급 뽑기")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => BuyRandomCard(CardRarity.R, 15)); }
                    else if (txt.text.Contains("SR급 뽑기")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => BuyRandomCard(CardRarity.SR, 50)); }
                    else if (txt.text.Contains("레어 가챠")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => { if (gm != null) gm.UnlockGachaType(GachaType.Rare, 5000); }); }
                    else if (txt.text.Contains("슈퍼 가챠")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => { if (gm != null) gm.UnlockGachaType(GachaType.Super, 20000); }); }
                }
            }
        }

        private void OnEnable()
        {
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gm != null)
            {
                gm.StateChanged -= Refresh;
            }
        }

        private void EnsureParentedToCanvas()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    transform.SetParent(canvas.transform, false);
                }
            }
            
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localScale = Vector3.one;
        }

        private void BuildUI()
        {
            if (coinText != null) return; // 이미 연결되어 있으면 스킵
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.7f);

            var panel = MakePanel(transform, Vector2.zero, new Vector2(700, 500));
            
            MakeText(panel.transform, "상점", new Vector2(0, 220), new Vector2(200, 40), 24, Color.white).fontStyle = FontStyles.Bold;

            coinText = MakeText(panel.transform, "코인: 0", new Vector2(-150, 220), new Vector2(200, 30), 16, new Color(1f, 0.9f, 0.4f));
            shardText = MakeText(panel.transform, "조각: 0", new Vector2(150, 220), new Vector2(200, 30), 16, new Color(0.4f, 0.8f, 1f));

            // Tabs
            var tabParent = new GameObject("Tabs");
            tabParent.transform.SetParent(panel.transform, false);
            var tabRt = tabParent.AddComponent<RectTransform>();
            tabRt.anchoredPosition = new Vector2(0, 170);
            tabRt.sizeDelta = new Vector2(600, 40);

            MakeTabButton(tabParent.transform, "업그레이드", new Vector2(-200, 0), () => ShowTab(0));
            MakeTabButton(tabParent.transform, "조각 뽑기", new Vector2(0, 0), () => ShowTab(1));
            MakeTabButton(tabParent.transform, "가챠 해금", new Vector2(200, 0), () => ShowTab(2));

            // Contents
            upgradesContent = new GameObject("UpgradesContent");
            upgradesContent.transform.SetParent(panel.transform, false);
            var uRt = upgradesContent.AddComponent<RectTransform>();
            uRt.anchoredPosition = new Vector2(0, -30);
            uRt.sizeDelta = new Vector2(650, 360);

            shardContent = new GameObject("ShardContent");
            shardContent.transform.SetParent(panel.transform, false);
            var sRt = shardContent.AddComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, -30);
            sRt.sizeDelta = new Vector2(650, 360);

            gachaUnlockContent = new GameObject("GachaUnlockContent");
            gachaUnlockContent.transform.SetParent(panel.transform, false);
            var gRt = gachaUnlockContent.AddComponent<RectTransform>();
            gRt.anchoredPosition = new Vector2(0, -30);
            gRt.sizeDelta = new Vector2(650, 360);

            BuildUpgradesTab();
            BuildShardTab();
            BuildGachaUnlockTab();

            // Close
            MakeButton(panel.transform, "✕ 닫기", new Vector2(0, -220), new Vector2(120, 40), new Color(0.5f, 0.15f, 0.15f), () => gameObject.SetActive(false));

            ShowTab(0);
        }

        private void BuildUpgradesTab()
        {
            if (gm == null || gm.UpgradeCatalog == null) return;

            // Simple Scroll View Setup
            var scroll = new GameObject("Scroll");
            scroll.transform.SetParent(upgradesContent.transform, false);
            var srt = scroll.AddComponent<RectTransform>();
            srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero; srt.offsetMax = Vector2.zero;
            scroll.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            var scrollRect = scroll.AddComponent<ScrollRect>();
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.horizontal = false;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scroll.transform, false);
            var vrt = viewport.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scrollRect.viewport = vrt;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = new Vector2(0, 0); crt.offsetMax = new Vector2(0, 0);
            scrollRect.content = crt;

            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlHeight = false; layout.childControlWidth = true;
            layout.childForceExpandHeight = false; layout.childForceExpandWidth = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (var upg in gm.UpgradeCatalog.Upgrades)
            {
                if (upg == null) continue;
                var row = new GameObject("Row_" + upg.UpgradeId);
                row.transform.SetParent(content.transform, false);
                var rRt = row.AddComponent<RectTransform>();
                rRt.sizeDelta = new Vector2(0, 60);
                row.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f);

                var nameText = MakeText(row.transform, upg.DisplayName, new Vector2(100, 10), new Vector2(180, 25), 16, Color.white);
                nameText.alignment = TextAlignmentOptions.Left;
                
                var descText = MakeText(row.transform, upg.Description, new Vector2(100, -15), new Vector2(300, 25), 12, Color.gray);
                descText.alignment = TextAlignmentOptions.Left;
                descText.overflowMode = TextOverflowModes.Ellipsis;

                var lvText = MakeText(row.transform, "Lv.0/0", new Vector2(100, 0), new Vector2(100, 30), 14, Color.cyan);
                lvText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); lvText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

                var btn = MakeButton(row.transform, "0 코인", new Vector2(-60, 0), new Vector2(100, 40), BtnBuy, () => gm.BuyUpgrade(upg.UpgradeId));
                btn.GetComponent<RectTransform>().anchorMin = new Vector2(1, 0.5f);
                btn.GetComponent<RectTransform>().anchorMax = new Vector2(1, 0.5f);
                
                // Store refs in RowInfo
                var info = row.AddComponent<UpgradeRowInfo>();
                info.UpgradeId = upg.UpgradeId;
                info.LevelText = lvText;
                info.BuyButton = btn.GetComponent<Button>();
                info.CostText = btn.GetComponentInChildren<TMP_Text>();
                info.BgImage = btn.GetComponent<Image>();

                upgradeRows.Add(row);
            }
        }

        private void BuildShardTab()
        {
            MakeText(shardContent.transform, "조각으로 미획득 카드 뽑기", new Vector2(0, 120), new Vector2(400, 30), 18, Color.white);
            
            var nGo = MakeButton(shardContent.transform, "N급 뽑기\n(5 조각)", new Vector2(-150, 0), new Vector2(120, 80), new Color(0.4f, 0.4f, 0.4f), () => BuyRandomCard(CardRarity.N, 5));
            buyNBtn = nGo.GetComponent<Button>();
            buyNText = nGo.GetComponentInChildren<TMP_Text>();

            var rGo = MakeButton(shardContent.transform, "R급 뽑기\n(15 조각)", new Vector2(0, 0), new Vector2(120, 80), new Color(0.2f, 0.4f, 0.8f), () => BuyRandomCard(CardRarity.R, 15));
            buyRBtn = rGo.GetComponent<Button>();
            buyRText = rGo.GetComponentInChildren<TMP_Text>();

            var srGo = MakeButton(shardContent.transform, "SR급 뽑기\n(50 조각)", new Vector2(150, 0), new Vector2(120, 80), new Color(0.6f, 0.2f, 0.8f), () => BuyRandomCard(CardRarity.SR, 50));
            buySRBtn = srGo.GetComponent<Button>();
            buySRText = srGo.GetComponentInChildren<TMP_Text>();
        }

        private void BuildGachaUnlockTab()
        {
            MakeText(gachaUnlockContent.transform, "가챠 해금", new Vector2(0, 120), new Vector2(400, 30), 18, Color.white);

            var rareBtn = MakeButton(gachaUnlockContent.transform, "레어 가챠 해금\n5000 코인", new Vector2(-120, 0), new Vector2(180, 80), new Color(0.2f, 0.4f, 0.8f), () => gm.UnlockGachaType(GachaType.Rare, 5000));
            var superBtn = MakeButton(gachaUnlockContent.transform, "슈퍼 가챠 해금\n20000 코인", new Vector2(120, 0), new Vector2(180, 80), new Color(0.8f, 0.6f, 0.1f), () => gm.UnlockGachaType(GachaType.Super, 20000));

            // Component to update buttons
            var updater = gachaUnlockContent.AddComponent<GachaUnlockUpdater>();
            updater.gm = gm;
            updater.rareBtn = rareBtn.GetComponent<Button>();
            updater.superBtn = superBtn.GetComponent<Button>();
        }

        private void ShowTab(int index)
        {
            upgradesContent.SetActive(index == 0);
            shardContent.SetActive(index == 1);
            gachaUnlockContent.SetActive(index == 2);
            Refresh();
        }

        private void Refresh()
        {
            if (gm == null) return;

            if (coinText != null) coinText.text = $"{gm.Money:F1}";
            if (shardText != null) shardText.text = $"{gm.Shards}";

            // Refresh Upgrades
            if (upgradesContent.activeSelf)
            {
                foreach (var row in upgradeRows)
                {
                    var info = row.GetComponent<UpgradeRowInfo>();
                    if (info == null) continue;
                    
                    var entry = gm.UpgradeCatalog.FindById(info.UpgradeId);
                    int level = gm.GetUpgradeLevel(info.UpgradeId);
                    bool maxed = level >= entry.MaxLevel;

                    info.LevelText.text = $"Lv.{level}/{entry.MaxLevel}";

                    if (maxed)
                    {
                        info.CostText.text = "MAX";
                        info.BuyButton.interactable = false;
                        info.BgImage.color = new Color(0.8f, 0.6f, 0.1f);
                    }
                    else
                    {
                        double cost = entry.CostPerLevel != null && level < entry.CostPerLevel.Length ? entry.CostPerLevel[level] : 0;
                        info.CostText.text = $"{cost:0} 코인";
                        bool afford = gm.Money >= cost;
                        info.BuyButton.interactable = afford;
                        info.BgImage.color = afford ? BtnBuy : BtnLocked;
                    }
                }
            }

            // Refresh Gacha Unlock
            if (gachaUnlockContent.activeSelf)
            {
                var updater = gachaUnlockContent.GetComponent<GachaUnlockUpdater>();
                if (updater != null) updater.Refresh();
            }

            // Refresh Shard Tab Buttons
            if (shardContent.activeSelf)
            {
                RefreshShardButton(CardRarity.N, buyNBtn, buyNText, 5);
                RefreshShardButton(CardRarity.R, buyRBtn, buyRText, 15);
                RefreshShardButton(CardRarity.SR, buySRBtn, buySRText, 50);
            }
        }

        private void RefreshShardButton(CardRarity rarity, Button btn, TMP_Text txt, int cost)
        {
            if (btn == null || txt == null || gm == null) return;

            int lockedCount = 0;
            var states = gm.GetCardStates();
            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && card.Rarity == rarity)
                {
                    bool owned = states.TryGetValue(card.Id, out var state) && state.Unlocked;
                    if (!owned) lockedCount++;
                }
            }

            if (lockedCount == 0)
            {
                btn.interactable = false;
                txt.text = $"{rarity}급\n보유 완료";
                btn.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);
            }
            else
            {
                bool afford = gm.Shards >= cost;
                btn.interactable = afford;
                txt.text = $"{rarity}급 뽑기\n({cost} 조각)";
                
                Color baseColor = rarity == CardRarity.SR ? new Color(0.6f, 0.2f, 0.8f) : (rarity == CardRarity.R ? new Color(0.2f, 0.4f, 0.8f) : new Color(0.4f, 0.4f, 0.4f));
                btn.GetComponent<Image>().color = afford ? baseColor : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private void BuyRandomCard(CardRarity rarity, int cost)
        {
            if (gm == null || gm.CardCatalog == null) return;
            if (gm.Shards < cost) { Debug.Log("조각이 부족합니다."); return; }

            var candidates = new List<CardEntry>();
            var states = gm.GetCardStates();
            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && card.Rarity == rarity)
                {
                    bool owned = states.TryGetValue(card.Id, out var state) && state.Unlocked;
                    if (!owned)
                    {
                        candidates.Add(card);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                Debug.Log($"이미 모든 {rarity}급 카드를 보유하고 있습니다!");
                return;
            }

            var chosen = candidates[Random.Range(0, candidates.Count)];
            
            // Deduct shards
            var type = gm.GetType();
            type.GetProperty("Shards").SetValue(gm, gm.Shards - cost);
            
            // Apply Draw
            var applyDraw = type.GetMethod("ApplyDraw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (applyDraw != null) applyDraw.Invoke(gm, new object[] { chosen });

            gm.SaveGame();
            
            // Notify state
            var notify = type.GetMethod("NotifyState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (notify != null) notify.Invoke(gm, null);
        }

        // ── Helpers ──
        private static GameObject MakePanel(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = BG;
            return go;
        }

        private static TMP_Text MakeText(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tx = go.AddComponent<TextMeshProUGUI>();
            tx.text = text;
            tx.fontSize = fontSize;
            tx.color = col;
            tx.alignment = TextAlignmentOptions.Center;
            return tx;
        }

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = bgColor * 1.3f;
            cs.pressedColor = bgColor * 0.7f;
            cs.disabledColor = new Color(0.3f, 0.3f, 0.3f);
            btn.colors = cs;
            btn.onClick.AddListener(onClick);

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            tx.text = label;
            tx.fontSize = 14;
            tx.alignment = TextAlignmentOptions.Center;
            tx.color = Color.white;

            return go;
        }

        private static GameObject MakeTabButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            return MakeButton(parent, label, pos, new Vector2(180, 40), TabNormal, onClick);
        }
    }

    public class UpgradeRowInfo : MonoBehaviour
    {
        public string UpgradeId;
        public TMP_Text LevelText;
        public Button BuyButton;
        public TMP_Text CostText;
        public Image BgImage;
    }

    public class GachaUnlockUpdater : MonoBehaviour
    {
        public GameManager gm;
        public Button rareBtn;
        public Button superBtn;

        public void Refresh()
        {
            if (gm == null) return;

            if (gm.UnlockedRareGacha)
            {
                rareBtn.interactable = false;
                rareBtn.GetComponentInChildren<TMP_Text>().text = "레어 가챠\n해금됨";
                rareBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
            }
            else
            {
                bool afford = gm.Money >= 5000;
                rareBtn.interactable = afford;
                rareBtn.GetComponent<Image>().color = afford ? new Color(0.2f, 0.4f, 0.8f) : new Color(0.4f, 0.4f, 0.4f);
            }

            if (gm.UnlockedSuperGacha)
            {
                superBtn.interactable = false;
                superBtn.GetComponentInChildren<TMP_Text>().text = "슈퍼 가챠\n해금됨";
                superBtn.GetComponent<Image>().color = new Color(0.4f, 0.4f, 0.4f);
            }
            else
            {
                bool afford = gm.Money >= 20000;
                superBtn.interactable = afford;
                superBtn.GetComponent<Image>().color = afford ? new Color(0.8f, 0.6f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
            }
        }
    }
}
