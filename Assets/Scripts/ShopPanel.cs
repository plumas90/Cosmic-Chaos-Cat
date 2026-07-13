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

        private Button   buyNBtn,  buyRBtn,  buySRBtn;
        private TMP_Text buyNText, buyRText, buySRText;
        private readonly List<GameObject> upgradeRows = new List<GameObject>();
        private int activeTab = 0;
        private static TMP_FontAsset customFont;
        private Transform upgradeScrollContent;   // kept for lazy-rebuild

        // ── Colors ──────────────────────────────────────────────────────────
        private static readonly Color BG          = new Color(0.06f, 0.08f, 0.13f, 0.97f);
        private static readonly Color PanelBorder = new Color(0.20f, 0.28f, 0.45f, 1.00f);
        private static readonly Color HeaderBG    = new Color(0.09f, 0.12f, 0.20f, 1.00f);
        private static readonly Color TabNormal   = new Color(0.12f, 0.17f, 0.27f, 1.00f);
        private static readonly Color TabActiveC  = new Color(0.18f, 0.26f, 0.44f, 1.00f);
        private static readonly Color Indicator   = new Color(0.40f, 0.70f, 1.00f, 1.00f);
        private static readonly Color BtnBuy      = new Color(0.18f, 0.55f, 0.28f, 1.00f);
        private static readonly Color BtnDisabled = new Color(0.22f, 0.25f, 0.30f, 1.00f);
        private static readonly Color GoldColor   = new Color(1.00f, 0.85f, 0.30f, 1.00f);
        private static readonly Color ShardColor  = new Color(0.40f, 0.80f, 1.00f, 1.00f);
        private static readonly Color CatClick    = new Color(0.20f, 0.45f, 0.80f, 1.00f);
        private static readonly Color CatGacha    = new Color(0.55f, 0.20f, 0.80f, 1.00f);
        private static readonly Color CatEcon     = new Color(0.75f, 0.55f, 0.10f, 1.00f);
        private static readonly Color ColN        = new Color(0.40f, 0.40f, 0.45f, 1.00f);
        private static readonly Color ColR        = new Color(0.22f, 0.42f, 0.82f, 1.00f);
        private static readonly Color ColSR       = new Color(0.58f, 0.18f, 0.82f, 1.00f);

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            EnsureParentedToCanvas();
            EnsureReferencesResolved();
            gm = FindObjectOfType<GameManager>(true);
            if (!Application.isPlaying || coinText == null)
            {
                BuildUI();
            }
            BindListeners();
        }

        // Start() runs after ALL Awake()s — guarantees UpgradeCatalog is loaded
        private void Start()
        {
            EnsureReferencesResolved();
            if (upgradeScrollContent == null || upgradeScrollContent.childCount == 0)
            {
                if (gm == null) gm = FindObjectOfType<GameManager>(true);
                if (gm != null && gm.UpgradeCatalog != null)
                {
                    // Reset guard so BuildUpgradesTab can proceed
                    upgradeScrollContent = null;
                    BuildUpgradesTab();
                    BindListeners();
                    Refresh();
                }
            }
        }

        private void OnEnable()
        {
            EnsureReferencesResolved();
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null) gm.StateChanged += Refresh;

            // Lazy-build upgrade rows if catalog wasn't ready at Awake/Start
            if ((upgradeScrollContent == null || upgradeScrollContent.childCount == 0)
                && gm != null && gm.UpgradeCatalog != null)
            {
                upgradeScrollContent = null;
                BuildUpgradesTab();
                BindListeners();
            }
            else
            {
                BindListeners();
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gm != null) gm.StateChanged -= Refresh;
        }

        // ── Setup ────────────────────────────────────────────────────────────
        private void EnsureParentedToCanvas()
        {
            if (GetComponentInParent<Canvas>() == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null) transform.SetParent(canvas.transform, false);
            }
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localScale = Vector3.one;
            // Scale is set at bake time — do NOT override at runtime
        }

#if UNITY_EDITOR
        [ContextMenu("Reset UI Layout")]
        private void ResetUILayout()
        {
            coinText = null;
            var children = new List<GameObject>();
            foreach (Transform child in transform)
            {
                children.Add(child.gameObject);
            }
            foreach (var child in children)
            {
                SafeDestroy(child);
            }
            BuildUI();
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[ShopPanel] UI layout has been successfully reset to default coordinates.");
        }
#endif

        private void BuildUI()
        {
            EnsureReferencesResolved();

            // If UI elements already exist in the hierarchy, do NOT destroy or rebuild them.
            // This preserves all manual hierarchy/design changes made by the user in the Scene editor!
            if (coinText != null) return;

            // Clear existing UI elements to prevent duplication only during Editor baking
            if (!Application.isPlaying)
            {
                var children = new List<GameObject>();
                foreach (Transform child in transform)
                {
                    children.Add(child.gameObject);
                }
                foreach (var child in children)
                {
                    SafeDestroy(child);
                }
            }

            // Reset references (only in editor mode rebuilds)
            if (!Application.isPlaying)
            {
                coinText = null;
                shardText = null;
                upgradesContent = null;
                shardContent = null;
                gachaUnlockContent = null;
                buyNBtn = null; buyRBtn = null; buySRBtn = null;
                buyNText = null; buyRText = null; buySRText = null;
                upgradeRows.Clear();
                upgradeScrollContent = null;
            }

            // Resolve font: try scene first, then AssetDatabase (for Edit Mode / baking)
            var anyText = FindObjectOfType<TextMeshProUGUI>(true);
            if (anyText != null) customFont = anyText.font;
#if UNITY_EDITOR
            if (customFont == null)
                customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Font/Galmuri9 SDF.asset");
#endif

            // Dim overlay
            var overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            // Outer border panel  (scale is explicitly set to 1.5x)
            var panel = MakeRT("Panel", transform, Vector2.zero, new Vector2(740, 570));
            panel.gameObject.AddComponent<Image>().color = PanelBorder;
            panel.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            // Inner dark fill
            var inner = MakeRT("Inner", panel.transform, Vector2.zero, Vector2.zero);
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(2,2); inner.offsetMax = new Vector2(-2,-2);
            inner.gameObject.AddComponent<Image>().color = BG;

            // ── Header ──────────────────────────────────────────────────────
            var hdr = MakeRT("Header", inner.transform, Vector2.zero, Vector2.zero);
            hdr.anchorMin = new Vector2(0,1); hdr.anchorMax = new Vector2(1,1);
            hdr.pivot = new Vector2(0.5f, 1);
            hdr.offsetMin = new Vector2(0,-62); hdr.offsetMax = Vector2.zero;
            hdr.gameObject.AddComponent<Image>().color = HeaderBG;

            // Title (center)
            var titleTxt = MakeLabel(inner.transform, "상 점", new Vector2(0, 249), new Vector2(220, 40), 22, Color.white, FontStyles.Bold);

            // Coin (left)
            coinText = MakeLabel(inner.transform, "0", new Vector2(-280, 249), new Vector2(170, 36), 14, GoldColor);
            coinText.gameObject.name = "CoinText";
            coinText.alignment = TextAlignmentOptions.Left;

            // Shard (right)
            shardText = MakeLabel(inner.transform, "0", new Vector2(210, 249), new Vector2(130, 36), 14, ShardColor);
            shardText.gameObject.name = "ShardText";
            shardText.alignment = TextAlignmentOptions.Left;

            // Close button
            var closeGO = MakeButton(inner.transform, "✕", new Vector2(330, 249), new Vector2(40, 36),
                new Color(0.55f,0.12f,0.12f), () => gameObject.SetActive(false), 16);
            closeGO.name = "CloseButton";

            // ── Tab Bar ──────────────────────────────────────────────────────
            float[] tabX = { -240f, 0f, 240f };
            string[] tabLabels = { "업그레이드", "조각 교환", "가챠 해금" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var tBtn = MakeRT("Tab" + i, inner.transform, new Vector2(tabX[i], 203), new Vector2(230, 38));
                tBtn.gameObject.AddComponent<Image>().color = (i == 0) ? TabActiveC : TabNormal;
                var tb = tBtn.gameObject.AddComponent<Button>();
                var cs = tb.colors;
                cs.highlightedColor = new Color(0.25f, 0.35f, 0.52f);
                cs.pressedColor = new Color(0.12f, 0.18f, 0.30f);
                tb.colors = cs;
                tb.onClick.AddListener(() => ShowTab(idx));
                MakeLabel(tBtn, tabLabels[i], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                var lblRT = tBtn.GetComponentInChildren<TMP_Text>().GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = Vector2.zero; lblRT.offsetMax = Vector2.zero;

                // Indicator line
                var ind = MakeRT("Ind", tBtn, new Vector2(0,-19), new Vector2(230,3));
                ind.gameObject.AddComponent<Image>().color = (i == 0) ? Indicator : Color.clear;
            }

            // ── Content areas (fill from just below tabs to near bottom) ─────
            upgradesContent    = BuildContentArea(inner.transform, "UpgradesContent");
            shardContent       = BuildContentArea(inner.transform, "ShardContent");
            gachaUnlockContent = BuildContentArea(inner.transform, "GachaUnlockContent");

            BuildUpgradesTab();
            BuildShardTab();
            BuildGachaUnlockTab();

            ShowTab(0);
        }

        private GameObject BuildContentArea(Transform parent, string name)
        {
            var go = MakeRT(name, parent, Vector2.zero, Vector2.zero);
            go.anchorMin = new Vector2(0.015f, 0.02f);
            go.anchorMax = new Vector2(0.985f, 0.66f);
            go.offsetMin = Vector2.zero;
            go.offsetMax = Vector2.zero;
            return go.gameObject;
        }

        // ── Upgrades Tab ─────────────────────────────────────────────────────
        private void BuildUpgradesTab()
        {
            // Resolve catalog: accept this.gm OR search scene (works in both Edit & Play mode)
            UpgradeCatalogSO catalog = gm?.UpgradeCatalog;
            if (catalog == null)
            {
#if UNITY_EDITOR
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalogSO>(
                    "Assets/ScriptableObjects/UpgradeCatalog.asset");
#endif
                if (catalog == null)
                {
                    var fallbackGm = FindObjectOfType<GameManager>(true);
                    if (fallbackGm != null) { gm = fallbackGm; catalog = fallbackGm.UpgradeCatalog; }
                }
            }
            if (catalog == null)
            {
                Debug.LogWarning("[ShopPanel] BuildUpgradesTab skipped – UpgradeCatalog not ready.");
                return;
            }
            if (upgradeScrollContent == null && upgradesContent != null)
            {
                var contentTrans = upgradesContent.transform.Find("Scroll/Viewport/Content");
                if (contentTrans != null) upgradeScrollContent = contentTrans;
            }

            // Clean up old Mask / Image components from existing Viewport to avoid transparent alpha masking bugs
            if (upgradesContent != null)
            {
                var vpTrans = upgradesContent.transform.Find("Scroll/Viewport");
                if (vpTrans != null)
                {
                    var oldMask = vpTrans.GetComponent<Mask>();
                    if (oldMask != null) SafeDestroy(oldMask);
                    var oldImg = vpTrans.GetComponent<Image>();
                    if (oldImg != null) SafeDestroy(oldImg);

                    if (vpTrans.GetComponent<RectMask2D>() == null)
                    {
                        vpTrans.gameObject.AddComponent<RectMask2D>();
                    }
                }
            }

            // Enforce Clamped movement type on existing ScrollRect component to prevent elastic bounce/shakes
            if (upgradesContent != null)
            {
                var scrollTrans = upgradesContent.transform.Find("Scroll");
                if (scrollTrans != null)
                {
                    var sr = scrollTrans.GetComponent<ScrollRect>();
                    if (sr != null)
                    {
                        sr.movementType = ScrollRect.MovementType.Clamped;
                    }
                }
            }

            // Don't rebuild ScrollRect structure if it already exists
            bool hasScroll = upgradeScrollContent != null;
            if (!hasScroll)
            {
                if (upgradesContent == null) return;

                // ScrollRect setup
                var scroll = MakeRT("Scroll", upgradesContent.transform, Vector2.zero, Vector2.zero);
                scroll.anchorMin = Vector2.zero; scroll.anchorMax = Vector2.one;
                scroll.offsetMin = Vector2.zero; scroll.offsetMax = Vector2.zero;
                scroll.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.2f);
                var sr = scroll.gameObject.AddComponent<ScrollRect>();
                sr.horizontal = false;
                sr.movementType = ScrollRect.MovementType.Clamped;
                sr.scrollSensitivity = 25f;

                var vp = MakeRT("Viewport", scroll, Vector2.zero, Vector2.zero);
                vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one;
                vp.offsetMin = Vector2.zero; vp.offsetMax = Vector2.zero;
                // RectMask2D avoids alpha rendering bugs and keeps scroll viewport visible
                vp.gameObject.AddComponent<RectMask2D>();
                sr.viewport = vp;

                var ct = MakeRT("Content", vp, Vector2.zero, Vector2.zero);
                ct.anchorMin = new Vector2(0,1); ct.anchorMax = new Vector2(1,1);
                ct.pivot = new Vector2(0.5f,1);
                ct.offsetMin = ct.offsetMax = Vector2.zero;
                sr.content = ct;
                upgradeScrollContent = ct;   // save reference for lazy rebuild

                var vlg = ct.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4; vlg.padding = new RectOffset(6,6,6,6);
                vlg.childControlHeight = false; vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
                ct.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // Bake time: only generate the Scroll/Viewport/Content structure
            // Runtime (isPlaying): generate actual dynamic items
            if (!Application.isPlaying) return;

            // Clear any previously instantiated dynamic rows during playmode to prevent duplication
            var oldRows = new List<GameObject>();
            foreach (Transform child in upgradeScrollContent)
            {
                oldRows.Add(child.gameObject);
            }
            foreach (var r in oldRows)
            {
                SafeDestroy(r);
            }
            upgradeRows.Clear();

            // Category groups
            var cats  = new[] { UpgradeCategory.Click, UpgradeCategory.Gacha, UpgradeCategory.Economy };
            var cNames = new[] { "클릭 계열", "가챠 계열", "경제 계열" };
            var cCols  = new[] { CatClick, CatGacha, CatEcon };

            foreach (var cat in cats)
            {
                int ci = System.Array.IndexOf(cats, cat);
                bool anyInCat = false;
                foreach (var u in catalog.Upgrades)
                    if (u != null && u.Category == cat) { anyInCat = true; break; }
                if (!anyInCat) continue;

                // Section header
                var hdrGO = new GameObject("SecHdr_" + cat, typeof(RectTransform));
                hdrGO.transform.SetParent(upgradeScrollContent, false);
                var hdrRT = hdrGO.GetComponent<RectTransform>();
                hdrRT.sizeDelta = new Vector2(0, 26);
                hdrGO.AddComponent<Image>().color = cCols[ci] * new Color(1,1,1,0.45f);

                // Use the stable MakeLabel helper to create the label text
                var hdrTxt = MakeLabel(hdrGO.transform, cNames[ci], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                hdrTxt.alignment = TextAlignmentOptions.Left;
                hdrTxt.margin = new Vector4(10, 0, 0, 0);
                
                var hRt = hdrTxt.GetComponent<RectTransform>();
                hRt.anchorMin = Vector2.zero; hRt.anchorMax = Vector2.one;
                hRt.offsetMin = Vector2.zero; hRt.offsetMax = Vector2.zero;

                foreach (var upg in catalog.Upgrades)
                {
                    if (upg == null || upg.Category != cat) continue;

                    // Unlock dependency check: tier-2 upgrades are only shown when tier-1 is at max level (5)
                    if (upg.UpgradeId == "upg-normal-prob-2" && gm.GetUpgradeLevel("upg-normal-prob-1") < 5) continue;
                    if (upg.UpgradeId == "upg-rare-prob-2" && gm.GetUpgradeLevel("upg-rare-prob-1") < 5) continue;
                    if (upg.UpgradeId == "upg-super-prob-2" && gm.GetUpgradeLevel("upg-super-prob-1") < 5) continue;

                    var row = new GameObject("Row_" + upg.UpgradeId, typeof(RectTransform));
                    row.transform.SetParent(upgradeScrollContent, false);
                    var rRT = row.GetComponent<RectTransform>();
                    rRT.sizeDelta = new Vector2(0, 80);
                    row.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 1f);

                    // Left accent bar
                    var acc = MakeRT("Acc", row.transform, Vector2.zero, Vector2.zero);
                    acc.anchorMin = new Vector2(0,0); acc.anchorMax = new Vector2(0,1);
                    acc.offsetMin = Vector2.zero; acc.offsetMax = new Vector2(4,0);
                    acc.gameObject.AddComponent<Image>().color = cCols[ci];

                    // Name text
                    var nm = MakeLabel(row.transform, upg.DisplayName, Vector2.zero, Vector2.zero, 13, Color.white);
                    var nmRT = nm.GetComponent<RectTransform>();
                    nmRT.anchorMin = new Vector2(0, 0.60f); nmRT.anchorMax = new Vector2(0.58f, 0.95f);
                    nmRT.offsetMin = new Vector2(14,0); nmRT.offsetMax = Vector2.zero;
                    nm.alignment = TextAlignmentOptions.Left;

                    // Desc text - supporting 2 lines wrapping
                    string descText = upg.Description + GetUpgradeValuesString(upg);
                    var dc = MakeLabel(row.transform, descText, Vector2.zero, Vector2.zero, 10, new Color(0.60f,0.65f,0.75f));
                    var dcRT = dc.GetComponent<RectTransform>();
                    dcRT.anchorMin = new Vector2(0, 0.05f); dcRT.anchorMax = new Vector2(0.58f, 0.55f);
                    dcRT.offsetMin = new Vector2(14,0); dcRT.offsetMax = Vector2.zero;
                    dc.alignment = TextAlignmentOptions.Left;
                    dc.overflowMode = TextOverflowModes.Overflow;
                    dc.enableWordWrapping = true;

                    // Level badge
                    var lbRT = MakeRT("LvBadge", row.transform, Vector2.zero, Vector2.zero);
                    lbRT.anchorMin = new Vector2(0.58f, 0.25f); lbRT.anchorMax = new Vector2(0.73f, 0.75f);
                    lbRT.offsetMin = new Vector2(2,0); lbRT.offsetMax = new Vector2(-2,0);
                    lbRT.gameObject.AddComponent<Image>().color = new Color(0.14f,0.17f,0.26f);
                    var lvTxt = MakeLabel(lbRT, "Lv.0/0", Vector2.zero, Vector2.zero, 11, new Color(0.5f,0.85f,1f), FontStyles.Bold);
                    var ltRT = lvTxt.GetComponent<RectTransform>();
                    ltRT.anchorMin = Vector2.zero; ltRT.anchorMax = Vector2.one;
                    ltRT.offsetMin = Vector2.zero; ltRT.offsetMax = Vector2.zero;

                    // Buy button
                    var bbRT = MakeRT("BuyBtn", row.transform, Vector2.zero, Vector2.zero);
                    bbRT.anchorMin = new Vector2(0.74f, 0.20f); bbRT.anchorMax = new Vector2(0.99f, 0.80f);
                    bbRT.offsetMin = Vector2.zero; bbRT.offsetMax = Vector2.zero;
                    bbRT.gameObject.AddComponent<Image>().color = BtnBuy;
                    var bb = bbRT.gameObject.AddComponent<Button>();
                    string uid = upg.UpgradeId;
                    bb.onClick.AddListener(() => gm.BuyUpgrade(uid));
                    var costTxt = MakeLabel(bbRT, "0", Vector2.zero, Vector2.zero, 11, Color.white);
                    var ctRT = costTxt.GetComponent<RectTransform>();
                    ctRT.anchorMin = Vector2.zero; ctRT.anchorMax = Vector2.one;
                    ctRT.offsetMin = Vector2.zero; ctRT.offsetMax = Vector2.zero;

                    var info = row.AddComponent<UpgradeRowInfo>();
                    info.UpgradeId = upg.UpgradeId;
                    info.LevelText = lvTxt;
                    info.BuyButton = bb;
                    info.CostText  = costTxt;
                    info.BgImage   = bbRT.GetComponent<Image>();

                    upgradeRows.Add(row);
                }
            }
        }

        // ── Shard Tab ────────────────────────────────────────────────────────
        private void BuildShardTab()
        {
            MakeLabel(shardContent.transform, "조각으로 미획득 카드를 뽑습니다",
                new Vector2(0, 140), new Vector2(500,26), 15, new Color(0.75f,0.80f,0.90f));
            MakeLabel(shardContent.transform, "이미 보유한 카드는 뽑기 대상에서 제외됩니다",
                new Vector2(0, 115), new Vector2(500,20), 11, new Color(0.50f,0.55f,0.65f));

            BuildShardCard(CardRarity.N,  "N 등급", "일반 카드",   500,  ColN,  -205f);
            BuildShardCard(CardRarity.R,  "R 등급", "레어 카드",  5000,  ColR,    0f);
            BuildShardCard(CardRarity.SR, "SR 등급","슈퍼레어",   25000, ColSR,  205f);
        }

        private void BuildShardCard(CardRarity rarity, string label, string sub, int cost, Color col, float xPos)
        {
            // Outer border
            var card = MakeRT("Card_" + rarity, shardContent.transform, new Vector2(xPos, 15f), new Vector2(175, 195));
            card.gameObject.AddComponent<Image>().color = col * new Color(1,1,1,0.45f);

            var cardIn = MakeRT("In", card, Vector2.zero, Vector2.zero);
            cardIn.anchorMin = Vector2.zero; cardIn.anchorMax = Vector2.one;
            cardIn.offsetMin = new Vector2(2,2); cardIn.offsetMax = new Vector2(-2,-2);
            cardIn.gameObject.AddComponent<Image>().color = new Color(0.09f,0.11f,0.18f);

            MakeLabel(cardIn, label, new Vector2(0,68), new Vector2(165,26), 14, col, FontStyles.Bold);
            MakeLabel(cardIn, sub,   new Vector2(0,46), new Vector2(165,20), 12, new Color(0.65f,0.70f,0.80f));

            // Cost badge
            var cb = MakeRT("Cost", cardIn, new Vector2(0,16), new Vector2(145,28));
            cb.gameObject.AddComponent<Image>().color = new Color(0.07f,0.09f,0.15f);
            var cbTxt = MakeLabel(cb, $"{cost} 조각", Vector2.zero, Vector2.zero, 12, ShardColor);
            var cbRT = cbTxt.GetComponent<RectTransform>();
            cbRT.anchorMin = Vector2.zero; cbRT.anchorMax = Vector2.one;
            cbRT.offsetMin = Vector2.zero; cbRT.offsetMax = Vector2.zero;

            // Buy button
            var bb = MakeRT("Buy", cardIn, new Vector2(0,-58), new Vector2(150, 44));
            bb.gameObject.AddComponent<Image>().color = col;
            var btn = bb.gameObject.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = col * 1.3f; cs.pressedColor = col * 0.7f;
            cs.disabledColor = BtnDisabled; btn.colors = cs;
            int c = cost; CardRarity r = rarity;
            btn.onClick.AddListener(() => BuyRandomCard(r, c));
            var btnTxt = MakeLabel(bb, "뽑기", Vector2.zero, Vector2.zero, 14, Color.white, FontStyles.Bold);
            var btRT = btnTxt.GetComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;

            if (rarity == CardRarity.N)  { buyNBtn = btn;  buyNText = btnTxt; }
            if (rarity == CardRarity.R)  { buyRBtn = btn;  buyRText = btnTxt; }
            if (rarity == CardRarity.SR) { buySRBtn = btn; buySRText = btnTxt; }
        }

        // ── Gacha Unlock Tab ─────────────────────────────────────────────────
        private void BuildGachaUnlockTab()
        {
            MakeLabel(gachaUnlockContent.transform, "더 높은 등급의 가챠를 해금합니다",
                new Vector2(0, 145), new Vector2(500,26), 15, new Color(0.75f,0.80f,0.90f));
            MakeLabel(gachaUnlockContent.transform, "해금 후 가챠 패널에서 사용할 수 있습니다",
                new Vector2(0, 120), new Vector2(500,20), 11, new Color(0.50f,0.55f,0.65f));

            BuildGachaCard(GachaType.Rare,  "레어 가챠", "R 이상 카드 등장\n확률 대폭 상승", 5000,  ColR,  -185f);
            BuildGachaCard(GachaType.Super, "슈퍼 가챠", "SR 이상 카드 등장\n확률 대폭 상승", 20000, ColSR, 185f);
        }

        private void BuildGachaCard(GachaType gtype, string label, string desc, double cost, Color col, float xPos)
        {
            var card = MakeRT("Card_" + gtype, gachaUnlockContent.transform, new Vector2(xPos, 15f), new Vector2(295, 195));
            card.gameObject.AddComponent<Image>().color = col * new Color(1,1,1,0.40f);

            var cardIn = MakeRT("In", card, Vector2.zero, Vector2.zero);
            cardIn.anchorMin = Vector2.zero; cardIn.anchorMax = Vector2.one;
            cardIn.offsetMin = new Vector2(2,2); cardIn.offsetMax = new Vector2(-2,-2);
            cardIn.gameObject.AddComponent<Image>().color = new Color(0.09f,0.11f,0.18f);

            MakeLabel(cardIn, label, new Vector2(0,68), new Vector2(275,28), 16, col, FontStyles.Bold);
            MakeLabel(cardIn, desc,  new Vector2(0,34), new Vector2(275,42), 12, new Color(0.65f,0.70f,0.80f));

            var cb = MakeRT("Cost", cardIn, new Vector2(0,-8), new Vector2(240,28));
            cb.gameObject.AddComponent<Image>().color = new Color(0.07f,0.09f,0.15f);
            string costStr = cost >= 1000 ? $"{cost/1000:0}K" : $"{cost:0}";
            var cbTxt = MakeLabel(cb, costStr, Vector2.zero, Vector2.zero, 13, GoldColor);
            var cbRT = cbTxt.GetComponent<RectTransform>();
            cbRT.anchorMin = Vector2.zero; cbRT.anchorMax = Vector2.one;
            cbRT.offsetMin = Vector2.zero; cbRT.offsetMax = Vector2.zero;

            var bb = MakeRT("Buy", cardIn, new Vector2(0,-62), new Vector2(260,44));
            bb.gameObject.AddComponent<Image>().color = col;
            var btn = bb.gameObject.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = col * 1.3f; cs.pressedColor = col * 0.7f;
            cs.disabledColor = BtnDisabled; btn.colors = cs;
            GachaType gt = gtype; double co = cost;
            btn.onClick.AddListener(() => { if (gm != null) gm.UnlockGachaType(gt, co); });
            var btnTxt = MakeLabel(bb, "해금하기", Vector2.zero, Vector2.zero, 14, Color.white, FontStyles.Bold);
            var btRT = btnTxt.GetComponent<RectTransform>();
            btRT.anchorMin = Vector2.zero; btRT.anchorMax = Vector2.one;
            btRT.offsetMin = Vector2.zero; btRT.offsetMax = Vector2.zero;

            var upd = card.gameObject.AddComponent<GachaUnlockCardUpdater>();
            upd.GachaType     = gtype;
            upd.BuyButton     = btn;
            upd.BtnText       = btnTxt;
            upd.LockedColor   = col;
            upd.UnlockedColor = new Color(0.22f,0.48f,0.26f);
        }

        // ── Tab Switching ─────────────────────────────────────────────────────
        private void ShowTab(int index)
        {
            activeTab = index;
            upgradesContent.SetActive(index == 0);
            shardContent.SetActive(index == 1);
            gachaUnlockContent.SetActive(index == 2);

            // Update tab visuals
            var inner = transform.Find("Panel/Inner");
            if (inner != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    var t = inner.Find("Tab" + i);
                    if (t == null) continue;
                    var img = t.GetComponent<Image>();
                    if (img != null) img.color = (i == index) ? TabActiveC : TabNormal;
                    var ind = t.Find("Ind");
                    if (ind != null) { var indImg = ind.GetComponent<Image>(); if (indImg != null) indImg.color = (i == index) ? Indicator : Color.clear; }
                }
            }
            Refresh();
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        private void Refresh()
        {
            EnsureReferencesResolved();
            if (gm == null) return;
            if (coinText  != null) coinText.text  = $"{gm.Money:F1}";
            if (shardText != null) shardText.text = $"{gm.Shards}";

            if (upgradesContent != null && upgradesContent.activeSelf) RefreshUpgrades();

            if (shardContent != null && shardContent.activeSelf)
            {
                RefreshShardButton(CardRarity.N,  buyNBtn,  buyNText,   500);
                RefreshShardButton(CardRarity.R,  buyRBtn,  buyRText,  5000);
                RefreshShardButton(CardRarity.SR, buySRBtn, buySRText, 25000);
            }

            if (gachaUnlockContent != null && gachaUnlockContent.activeSelf)
                foreach (var u in gachaUnlockContent.GetComponentsInChildren<GachaUnlockCardUpdater>(true))
                    u.Refresh(gm);
        }

        private void RefreshUpgrades()
        {
            if (gm == null || gm.UpgradeCatalog == null) return;
            foreach (var row in upgradeRows)
            {
                if (row == null) continue;
                var info = row.GetComponent<UpgradeRowInfo>();
                if (info == null) continue;
                var entry = gm.UpgradeCatalog.FindById(info.UpgradeId);
                if (entry == null) continue;
                int level = gm.GetUpgradeLevel(info.UpgradeId);
                bool maxed = level >= entry.MaxLevel;
                if (info.LevelText != null) info.LevelText.text = $"Lv.{level}/{entry.MaxLevel}";
                if (maxed)
                {
                    if (info.CostText  != null)
                    {
                        info.CostText.text = "MAX";
                        info.CostText.color = Color.white;
                    }
                    if (info.BuyButton != null) info.BuyButton.interactable = false;
                    if (info.BgImage   != null) info.BgImage.color = new Color(0.70f,0.55f,0.10f);
                }
                else
                {
                    double cost = (entry.CostPerLevel != null && level < entry.CostPerLevel.Length)
                        ? entry.CostPerLevel[level] : 0;
                    bool isShardUpgrade = entry.UpgradeId.StartsWith("upg-unlock-");
                    bool afford = isShardUpgrade ? (gm.Shards >= (int)cost) : (gm.Money >= cost);
                    
                    if (info.CostText != null)
                    {
                        if (isShardUpgrade)
                        {
                            info.CostText.text = $"{cost} 조각";
                            info.CostText.color = ShardColor;
                        }
                        else
                        {
                            info.CostText.text = cost >= 1000 ? $"{cost/1000:0.#}K" : $"{cost:0}";
                            info.CostText.color = Color.white;
                        }
                    }

                    if (info.BuyButton != null) info.BuyButton.interactable = afford;
                    if (info.BgImage   != null) info.BgImage.color = afford ? BtnBuy : BtnDisabled;
                }
            }
        }

        private void RefreshShardButton(CardRarity rarity, Button btn, TMP_Text txt, int cost)
        {
            if (btn == null || txt == null || gm == null || gm.CardCatalog == null) return;
            var states = gm.GetCardStates();
            int candCount = 0;

            // Pass 1: Undiscovered cards (locked or Copies == 0)
            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && card.Rarity == rarity)
                {
                    if (!(states.TryGetValue(card.Id, out var st) && st.Unlocked && st.Copies > 0))
                    {
                        candCount++;
                    }
                }
            }

            // Pass 2: If all cards of this rarity are unlocked, check cards that still need duplicates for max breakthrough
            if (candCount == 0)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && !card.IsHidden && card.Rarity == rarity)
                    {
                        int maxBreakthroughCopies = 6; // absolute max copies for max breakthrough stage 5
                        if (states.TryGetValue(card.Id, out var st))
                        {
                            if (st.Copies < maxBreakthroughCopies)
                            {
                                candCount++;
                            }
                        }
                    }
                }
            }

            Color baseCol = rarity == CardRarity.SR ? ColSR : rarity == CardRarity.R ? ColR : ColN;
            if (candCount == 0)
            {
                btn.interactable = false;
                txt.text = "완료";
                btn.GetComponent<Image>().color = new Color(0.22f,0.48f,0.26f);
            }
            else
            {
                bool afford = gm.Shards >= cost;
                btn.interactable = afford;
                txt.text = afford ? "뽑기" : "조각 부족";
                btn.GetComponent<Image>().color = afford ? baseCol : BtnDisabled;
            }
        }

        // ── Shard Purchase ────────────────────────────────────────────────────
        private void BuyRandomCard(CardRarity rarity, int cost)
        {
            if (gm == null || gm.CardCatalog == null || gm.Shards < cost) return;
            var cands = new List<CardEntry>();
            var states = gm.GetCardStates();

            // Pass 1: Undiscovered cards (locked or Copies == 0)
            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && card.Rarity == rarity)
                {
                    if (!(states.TryGetValue(card.Id, out var st) && st.Unlocked && st.Copies > 0))
                    {
                        cands.Add(card);
                    }
                }
            }

            // Pass 2: If all cards of this rarity are unlocked, get those that still need duplicate copies for max breakthrough
            if (cands.Count == 0)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && !card.IsHidden && card.Rarity == rarity)
                    {
                        int maxBreakthroughCopies = 6; // absolute max copies for max breakthrough stage 5
                        if (states.TryGetValue(card.Id, out var st))
                        {
                            if (st.Copies < maxBreakthroughCopies)
                            {
                                cands.Add(card);
                            }
                        }
                    }
                }
            }

            if (cands.Count == 0) return; // Completely sold out (all cards maxed out)

            var chosen = cands[Random.Range(0, cands.Count)];
            var type = gm.GetType();
            type.GetProperty("Shards").SetValue(gm, gm.Shards - cost);
            var applyDraw = type.GetMethod("ApplyDraw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            applyDraw?.Invoke(gm, new object[] { chosen });
            gm.SaveGame();
            var notify = type.GetMethod("NotifyState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            notify?.Invoke(gm, null);
        }

        // ── UI Helpers ────────────────────────────────────────────────────────
        private static RectTransform MakeRT(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size; rt.localScale = Vector3.one;
            return rt;
        }

        private static TMP_Text MakeLabel(Transform parent, string text, Vector2 pos, Vector2 size,
            int fs, Color col, FontStyles style = FontStyles.Normal)
        {
            if (customFont == null)
            {
                var anyText = FindObjectOfType<TextMeshProUGUI>(true);
                if (anyText != null) customFont = anyText.font;
#if UNITY_EDITOR
                if (customFont == null)
                    customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/Font/Galmuri9 SDF.asset");
#endif
            }

            var go = new GameObject("Lbl", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size; rt.localScale = Vector3.one;
            var tx = go.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tx.font = customFont;
            tx.text = text; tx.fontSize = fs; tx.color = col;
            tx.fontStyle = style; tx.alignment = TextAlignmentOptions.Center;
            tx.overflowMode = TextOverflowModes.Ellipsis;
            return tx;
        }

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
            Color bgCol, UnityEngine.Events.UnityAction onClick, int fs = 14)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size; rt.localScale = Vector3.one;
            go.AddComponent<Image>().color = bgCol;
            var btn = go.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = bgCol * 1.3f; cs.pressedColor = bgCol * 0.7f;
            cs.disabledColor = new Color(0.3f,0.3f,0.3f); btn.colors = cs;
            btn.onClick.AddListener(onClick);
            var lgo = new GameObject("Lbl", typeof(RectTransform));
            lgo.transform.SetParent(go.transform, false);
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;
            var tx = lgo.AddComponent<TextMeshProUGUI>();
            if (customFont == null)
            {
                var anyText = FindObjectOfType<TextMeshProUGUI>(true);
                if (anyText != null) customFont = anyText.font;
#if UNITY_EDITOR
                if (customFont == null)
                    customFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                        "Assets/Font/Galmuri9 SDF.asset");
#endif
            }
            if (customFont != null) tx.font = customFont;
            tx.text = label; tx.fontSize = fs; tx.alignment = TextAlignmentOptions.Center; tx.color = Color.white;
            return go;
        }

        private void BindListeners()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            var upgradeInfos = GetComponentsInChildren<UpgradeRowInfo>(true);

            // 1. Rebuild dynamic references for refresh updates
            upgradeRows.Clear();
            foreach (var info in upgradeInfos)
            {
                if (info != null && info.gameObject != null)
                {
                    upgradeRows.Add(info.gameObject);
                }
            }

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var card = btn.transform.GetComponentInParent<Transform>();
                while (card != null && !card.name.StartsWith("Card_"))
                {
                    card = card.parent;
                }

                var txt = btn.GetComponentInChildren<TMP_Text>();
                if (card != null && txt != null)
                {
                    if (card.name == "Card_N")       { buyNBtn = btn;  buyNText = txt; }
                    else if (card.name == "Card_R")  { buyRBtn = btn;  buyRText = txt; }
                    else if (card.name == "Card_SR") { buySRBtn = btn; buySRText = txt; }
                }
            }

            // 2. Bind onClick events
            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var txt = btn.GetComponentInChildren<TMP_Text>();
                if (txt == null) continue;

                string t = txt.text.Trim();
                
                // If it is an upgrade buy button, handle separately
                var rowInfo = btn.GetComponentInParent<UpgradeRowInfo>();
                if (rowInfo != null)
                {
                    string upgId = rowInfo.UpgradeId;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { if (gm != null) gm.BuyUpgrade(upgId); });
                    continue;
                }

                btn.onClick.RemoveAllListeners();

                if (t == "업그레이드")
                {
                    btn.onClick.AddListener(() => ShowTab(0));
                }
                else if (t == "조각 교환")
                {
                    btn.onClick.AddListener(() => ShowTab(1));
                }
                else if (t == "가챠 해금")
                {
                    btn.onClick.AddListener(() => ShowTab(2));
                }
                else if (btn.gameObject.name == "CloseButton" || btn.gameObject.name.StartsWith("Btn_✕") || t == "✕" || t.ToLower() == "x")
                {
                    btn.onClick.AddListener(() => gameObject.SetActive(false));
                }
                else if (t == "뽑기")
                {
                    var card = btn.transform.GetComponentInParent<Transform>();
                    while (card != null && !card.name.StartsWith("Card_"))
                    {
                        card = card.parent;
                    }
                    if (card != null)
                    {
                        if (card.name == "Card_N")       btn.onClick.AddListener(() => BuyRandomCard(CardRarity.N, 500));
                        else if (card.name == "Card_R")  btn.onClick.AddListener(() => BuyRandomCard(CardRarity.R, 5000));
                        else if (card.name == "Card_SR") btn.onClick.AddListener(() => BuyRandomCard(CardRarity.SR, 25000));
                    }
                }
                else if (t == "해금하기")
                {
                    var card = btn.transform.GetComponentInParent<Transform>();
                    while (card != null && !card.name.StartsWith("Card_"))
                    {
                        card = card.parent;
                    }
                    if (card != null)
                    {
                        if (card.name == "Card_Rare")       btn.onClick.AddListener(() => { if (gm != null) gm.UnlockGachaType(GachaType.Rare, 5000); });
                        else if (card.name == "Card_Super") btn.onClick.AddListener(() => { if (gm != null) gm.UnlockGachaType(GachaType.Super, 20000); });
                    }
                }
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj, true);
            }
        }

        private void EnsureReferencesResolved()
        {
            var panelTrans = transform.Find("Panel");
            if (panelTrans == null) return;
            var innerTrans = panelTrans.Find("Inner");
            if (innerTrans == null) return;

            if (upgradesContent == null)
            {
                var t = innerTrans.Find("UpgradesContent");
                if (t != null) upgradesContent = t.gameObject;
            }
            if (shardContent == null)
            {
                var t = innerTrans.Find("ShardContent");
                if (t != null) shardContent = t.gameObject;
            }
            if (gachaUnlockContent == null)
            {
                var t = innerTrans.Find("GachaUnlockContent");
                if (t != null) gachaUnlockContent = t.gameObject;
            }

            if (coinText == null)
            {
                var t = innerTrans.Find("CoinText");
                if (t != null) coinText = t.GetComponent<TMP_Text>();
            }
            if (shardText == null)
            {
                var t = innerTrans.Find("ShardText");
                if (t != null) shardText = t.GetComponent<TMP_Text>();
            }
        }

        private string GetUpgradeValuesString(UpgradeEntry entry)
        {
            if (entry == null || entry.EffectValuePerLevel == null || entry.EffectValuePerLevel.Length <= 1)
                return string.Empty;

            var parts = new List<string>();
            for (int i = 0; i < entry.EffectValuePerLevel.Length; i++)
            {
                float val = entry.EffectValuePerLevel[i];
                string formatted;

                switch (entry.EffectType)
                {
                    case UpgradeEffectType.CriticalChance:
                    case UpgradeEffectType.ComboBonus:
                    case UpgradeEffectType.ShardRefundBonus:
                        formatted = $"{Mathf.RoundToInt(val * 100f)}%";
                        break;
                    case UpgradeEffectType.NWeightReduction:
                    case UpgradeEffectType.RWeightReduction:
                        formatted = $"{Mathf.RoundToInt(val * 100f)}%";
                        break;
                    case UpgradeEffectType.GachaDiscount:
                        formatted = $"{Mathf.RoundToInt(val * 100f)}%";
                        break;
                    case UpgradeEffectType.CriticalMultiplier:
                        formatted = $"+{Mathf.RoundToInt(val * 100f)}%";
                        break;
                    default:
                        formatted = $"{val:0.#}";
                        break;
                }
                parts.Add(formatted);
            }

            return " (" + string.Join(", ", parts) + ")";
        }
    }
}
