using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public enum ProductCurrencyType
    {
        Coin,
        Shard
    }

    public enum ShopProductType
    {
        GachaUnlock,
        Background,
        Decoration,
        Card
    }

    [System.Serializable]
    public class ShopProductItem
    {
        public string id;
        public string displayName;
        public string description;
        public ProductCurrencyType currencyType;
        public double price;
        public ShopProductType productType;
        public string targetId;
        public CardRarity rarity = CardRarity.N;
        public Sprite iconSprite;
    }

    public sealed class ShopPanel : MonoBehaviour
    {
        private GameManager gm;

        [SerializeField] private GameObject upgradesContent;
        [SerializeField] private GameObject shardContent;
        [SerializeField] private GameObject productsContent;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private TMP_Text coinText;

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

        // Shard Exchange UI References
        private Button buyNBtn, buyRBtn, buySRBtn, buySSRBtn;
        private TMP_Text buyNText, buyRText, buySRText, buySSRText;

        // Products UI References
        private readonly List<GameObject> productSlotGOs = new List<GameObject>();
        private Button prevProductPageBtn;
        private Button nextProductPageBtn;
        private TMP_Text productPageText;

        private TMP_Text selectedProductNameText;
        private TMP_Text selectedProductDescText;
        private Button buyProductBtn;
        private TMP_Text buyProductBtnText;

        private ShopProductItem selectedProduct;
        private int currentProductPage = 0;
        private const int PRODUCTS_PER_PAGE = 10;
        private readonly List<ShopProductItem> productCatalog = new List<ShopProductItem>();

        private readonly List<GameObject> upgradeRows = new List<GameObject>();
        private int activeTab = 0;
        private static TMP_FontAsset customFont;
        private Transform upgradeScrollContent;

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
        private static readonly Color ColSSR      = new Color(0.95f, 0.75f, 0.10f, 1.00f);

        // ── Lifecycle ────────────────────────────────────────────────────────
        private void Awake()
        {
            EnsureParentedToCanvas();
            EnsureReferencesResolved();
            gm = FindObjectOfType<GameManager>(true);
            CopySpritesFromEncyclopedia();
            InitializeProductCatalog();

            if (!Application.isPlaying || coinText == null)
            {
                BuildUI();
            }
            BindListeners();
        }

        private void Start()
        {
            EnsureReferencesResolved();
            if (upgradeScrollContent == null || upgradeScrollContent.childCount == 0)
            {
                if (gm == null) gm = FindObjectOfType<GameManager>(true);
                if (gm != null && gm.UpgradeCatalog != null)
                {
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

            CopySpritesFromEncyclopedia();
            InitializeProductCatalog();

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
            ShowTab(activeTab);
        }

        private void OnDisable()
        {
            if (gm != null) gm.StateChanged -= Refresh;
        }

        // ── Sprite Helper ───────────────────────────────────────────────────
        private void CopySpritesFromEncyclopedia()
        {
            if (spriteMarkN != null && spriteFrameN != null) return;
            var enc = FindObjectOfType<EncyclopediaPanel>(true);
            if (enc != null)
            {
                if (spriteMarkN == null) spriteMarkN = enc.GetMarkSpriteForRarity(CardRarity.N);
                if (spriteMarkR == null) spriteMarkR = enc.GetMarkSpriteForRarity(CardRarity.R);
                if (spriteMarkSR == null) spriteMarkSR = enc.GetMarkSpriteForRarity(CardRarity.SR);
                if (spriteMarkSSR == null) spriteMarkSSR = enc.GetMarkSpriteForRarity(CardRarity.SSR);

                if (spriteFrameN == null) spriteFrameN = enc.GetFrameSpriteForRarity(CardRarity.N);
                if (spriteFrameR == null) spriteFrameR = enc.GetFrameSpriteForRarity(CardRarity.R);
                if (spriteFrameSR == null) spriteFrameSR = enc.GetFrameSpriteForRarity(CardRarity.SR);
                if (spriteFrameSSR == null) spriteFrameSSR = enc.GetFrameSpriteForRarity(CardRarity.SSR);
                if (spriteFrameLock == null) spriteFrameLock = enc.SpriteCardLocked;
            }
        }

        public Sprite GetFrameSpriteForRarity(CardRarity r)
        {
            CopySpritesFromEncyclopedia();
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
            CopySpritesFromEncyclopedia();
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
        }

        private void InitializeProductCatalog()
        {
            if (productCatalog.Count > 0) return;

            // 1. Default Gacha Unlock & Background / Decoration Products
            productCatalog.Add(new ShopProductItem { id = "prod-gacha-rare", displayName = "레어 가챠 해금", description = "가챠 패널에서 R등급 이상 카드를 획득하는 가챠를 해금합니다.", currencyType = ProductCurrencyType.Coin, price = 5000, productType = ShopProductType.GachaUnlock, targetId = "Rare", rarity = CardRarity.R });
            productCatalog.Add(new ShopProductItem { id = "prod-gacha-super", displayName = "슈퍼 가챠 해금", description = "가챠 패널에서 SR등급 이상 카드를 획득하는 가챠를 해금합니다.", currencyType = ProductCurrencyType.Coin, price = 20000, productType = ShopProductType.GachaUnlock, targetId = "Super", rarity = CardRarity.SR });
            productCatalog.Add(new ShopProductItem { id = "prod-bg-desert", displayName = "사막 오아시스 배경", description = "피라미드와 아름다운 오아시스가 우거진 사막 배경입니다.", currencyType = ProductCurrencyType.Coin, price = 10000, productType = ShopProductType.Background, targetId = "bg-desert", rarity = CardRarity.R });
            productCatalog.Add(new ShopProductItem { id = "prod-bg-dessert", displayName = "달콤 디저트 배경", description = "달콤한 케이크와 과자들이 가득한 과자나라 배경입니다.", currencyType = ProductCurrencyType.Coin, price = 30000, productType = ShopProductType.Background, targetId = "bg-dessert", rarity = CardRarity.R });
            productCatalog.Add(new ShopProductItem { id = "prod-bg-sea", displayName = "심해 바다 배경", description = "깊은 바닷속 환상적인 푸른 산호 해저 왕국 배경입니다.", currencyType = ProductCurrencyType.Shard, price = 500, productType = ShopProductType.Background, targetId = "bg-sea", rarity = CardRarity.SR });
            productCatalog.Add(new ShopProductItem { id = "prod-bg-hero", displayName = "우주 레인저 배경", description = "은하수를 누비는 우주 레인저 테마 특수 배경입니다.", currencyType = ProductCurrencyType.Shard, price = 2000, productType = ShopProductType.Background, targetId = "bg-hero", rarity = CardRarity.SSR });
            productCatalog.Add(new ShopProductItem { id = "prod-deco-pyramid", displayName = "미니 피라미드 장식", description = "귀여운 사막 피라미드 수호 오브제 장식품입니다.", currencyType = ProductCurrencyType.Coin, price = 5000, productType = ShopProductType.Decoration, targetId = "deco-pyramid", rarity = CardRarity.R });
            productCatalog.Add(new ShopProductItem { id = "prod-deco-cake", displayName = "디저트 케이크 장식", description = "달콤한 3단 생크림 케이크 장식품입니다.", currencyType = ProductCurrencyType.Coin, price = 15000, productType = ShopProductType.Decoration, targetId = "deco-cake", rarity = CardRarity.R });
            productCatalog.Add(new ShopProductItem { id = "prod-deco-aquarium", displayName = "산호 수족관 장식", description = "신비로운 해저 무지개 산호 수족관 장식품입니다.", currencyType = ProductCurrencyType.Shard, price = 300, productType = ShopProductType.Decoration, targetId = "deco-aquarium", rarity = CardRarity.SR });
            productCatalog.Add(new ShopProductItem { id = "prod-deco-airship", displayName = "황금 비행선 장식", description = "스팀펑크 태엽 비행선 모형 장식품입니다.", currencyType = ProductCurrencyType.Shard, price = 1000, productType = ShopProductType.Decoration, targetId = "deco-airship", rarity = CardRarity.SSR });
            productCatalog.Add(new ShopProductItem { id = "prod-deco-aurora", displayName = "오로라 수정구 장식", description = "사계절 오로라가 일렁이는 신비로운 수정구 장식품입니다.", currencyType = ProductCurrencyType.Shard, price = 5000, productType = ShopProductType.Decoration, targetId = "deco-aurora", rarity = CardRarity.SSR });

            // 2. Scan CardCatalog for IsShop cards (Exclusive Shop Cards)
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null && gm.CardCatalog != null)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && card.IsShop)
                    {
                        ProductCurrencyType cur = card.ShopCurrency == CardShopCurrency.Shard ? ProductCurrencyType.Shard : ProductCurrencyType.Coin;
                        double p = card.ShopPrice > 0 ? card.ShopPrice : (cur == ProductCurrencyType.Coin ? 5000 : 500);

                        productCatalog.Add(new ShopProductItem
                        {
                            id = "prod-card-" + card.Id,
                            displayName = card.DisplayName,
                            description = string.IsNullOrEmpty(card.Description) ? $"상점 전용 {card.Rarity}등급 한정 카드입니다." : card.Description,
                            currencyType = cur,
                            price = p,
                            productType = ShopProductType.Card,
                            targetId = card.Id,
                            rarity = card.Rarity,
                            iconSprite = card.CardSprite
                        });
                    }
                }
            }

            if (selectedProduct == null && productCatalog.Count > 0)
            {
                selectedProduct = productCatalog[0];
            }
        }

        private void BuildUI()
        {
            EnsureReferencesResolved();
            if (coinText != null) return;

            if (!Application.isPlaying)
            {
                var children = new List<GameObject>();
                foreach (Transform child in transform) children.Add(child.gameObject);
                foreach (var child in children) SafeDestroy(child);
            }

            if (!Application.isPlaying)
            {
                coinText = null; shardText = null;
                upgradesContent = null; shardContent = null; productsContent = null;
                buyNBtn = null; buyRBtn = null; buySRBtn = null; buySSRBtn = null;
                buyNText = null; buyRText = null; buySRText = null; buySSRText = null;
                upgradeRows.Clear(); upgradeScrollContent = null; productSlotGOs.Clear();
            }

            var anyText = FindObjectOfType<TextMeshProUGUI>(true);
            if (anyText != null) customFont = anyText.font;

            var overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);

            var panel = MakeRT("Panel", transform, Vector2.zero, new Vector2(740, 570));
            panel.gameObject.AddComponent<Image>().color = PanelBorder;
            panel.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            var inner = MakeRT("Inner", panel.transform, Vector2.zero, Vector2.zero);
            inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
            inner.offsetMin = new Vector2(2,2); inner.offsetMax = new Vector2(-2,-2);
            inner.gameObject.AddComponent<Image>().color = BG;

            // Header
            var hdr = MakeRT("Header", inner.transform, Vector2.zero, Vector2.zero);
            hdr.anchorMin = new Vector2(0,1); hdr.anchorMax = new Vector2(1,1);
            hdr.pivot = new Vector2(0.5f, 1);
            hdr.offsetMin = new Vector2(0,-62); hdr.offsetMax = Vector2.zero;
            hdr.gameObject.AddComponent<Image>().color = HeaderBG;

            MakeLabel(inner.transform, "상 점", new Vector2(0, 249), new Vector2(220, 40), 22, Color.white, FontStyles.Bold);

            coinText = MakeLabel(inner.transform, "0", new Vector2(-280, 249), new Vector2(170, 36), 14, GoldColor);
            coinText.gameObject.name = "CoinText"; coinText.alignment = TextAlignmentOptions.Left;

            shardText = MakeLabel(inner.transform, "0", new Vector2(210, 249), new Vector2(130, 36), 14, ShardColor);
            shardText.gameObject.name = "ShardText"; shardText.alignment = TextAlignmentOptions.Left;

            var closeGO = MakeButton(inner.transform, "✕", new Vector2(330, 249), new Vector2(40, 36),
                new Color(0.55f,0.12f,0.12f), () => gameObject.SetActive(false), 16);
            closeGO.name = "CloseButton";

            // Tab Bar
            float[] tabX = { -240f, 0f, 240f };
            string[] tabLabels = { "업그레이드", "조각 교환", "상품" };
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

                var ind = MakeRT("Ind", tBtn, new Vector2(0,-19), new Vector2(230,3));
                ind.gameObject.AddComponent<Image>().color = (i == 0) ? Indicator : Color.clear;
            }

            // Content areas
            upgradesContent = BuildContentArea(inner.transform, "UpgradesContent");
            shardContent    = BuildContentArea(inner.transform, "ShardContent");
            productsContent = BuildContentArea(inner.transform, "ProductsContent");

            BuildUpgradesTab();
            BuildShardTab();
            BuildProductsTab();

            ShowTab(0);
        }

        private GameObject BuildContentArea(Transform parent, string name)
        {
            var go = MakeRT(name, parent, Vector2.zero, Vector2.zero);
            go.anchorMin = new Vector2(0.015f, 0.02f);
            go.anchorMax = new Vector2(0.985f, 0.66f);
            go.offsetMin = Vector2.zero; go.offsetMax = Vector2.zero;
            return go.gameObject;
        }

        // ── Upgrades Tab ─────────────────────────────────────────────────────
        private void BuildUpgradesTab()
        {
            UpgradeCatalogSO catalog = gm?.UpgradeCatalog;
            if (catalog == null)
            {
#if UNITY_EDITOR
                catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalogSO>(
                    "Assets/ScriptableObjects/UpgradeCatalog.asset");
#endif
                if (catalog == null && gm != null) catalog = gm.UpgradeCatalog;
            }
            if (catalog == null) return;

            if (upgradeScrollContent == null && upgradesContent != null)
            {
                var contentTrans = upgradesContent.transform.Find("Scroll/Viewport/Content");
                if (contentTrans != null) upgradeScrollContent = contentTrans;
            }

            if (upgradesContent != null)
            {
                var vpTrans = upgradesContent.transform.Find("Scroll/Viewport");
                if (vpTrans != null)
                {
                    var oldMask = vpTrans.GetComponent<Mask>();
                    if (oldMask != null) SafeDestroy(oldMask);
                    var oldImg = vpTrans.GetComponent<Image>();
                    if (oldImg != null) SafeDestroy(oldImg);
                    if (vpTrans.GetComponent<RectMask2D>() == null) vpTrans.gameObject.AddComponent<RectMask2D>();
                }

                var scrollTrans = upgradesContent.transform.Find("Scroll");
                if (scrollTrans != null)
                {
                    var sr = scrollTrans.GetComponent<ScrollRect>();
                    if (sr != null) sr.movementType = ScrollRect.MovementType.Clamped;
                }
            }

            bool hasScroll = upgradeScrollContent != null;
            if (!hasScroll && upgradesContent != null)
            {
                var scroll = MakeRT("Scroll", upgradesContent.transform, Vector2.zero, Vector2.zero);
                scroll.anchorMin = Vector2.zero; scroll.anchorMax = Vector2.one;
                scroll.offsetMin = scroll.offsetMax = Vector2.zero;
                scroll.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.2f);
                var sr = scroll.gameObject.AddComponent<ScrollRect>();
                sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 25f;

                var vp = MakeRT("Viewport", scroll, Vector2.zero, Vector2.zero);
                vp.anchorMin = Vector2.zero; vp.anchorMax = Vector2.one;
                vp.offsetMin = vp.offsetMax = Vector2.zero;
                vp.gameObject.AddComponent<RectMask2D>();
                sr.viewport = vp;

                var ct = MakeRT("Content", vp, Vector2.zero, Vector2.zero);
                ct.anchorMin = new Vector2(0,1); ct.anchorMax = new Vector2(1,1);
                ct.pivot = new Vector2(0.5f,1);
                ct.offsetMin = ct.offsetMax = Vector2.zero;
                sr.content = ct;
                upgradeScrollContent = ct;

                var vlg = ct.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 4; vlg.padding = new RectOffset(6,6,6,6);
                vlg.childControlHeight = false; vlg.childControlWidth = true;
                vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = true;
                ct.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (!Application.isPlaying) return;

            var oldRows = new List<GameObject>();
            foreach (Transform child in upgradeScrollContent) oldRows.Add(child.gameObject);
            foreach (var r in oldRows) SafeDestroy(r);
            upgradeRows.Clear();

            var cats  = new[] { UpgradeCategory.Click, UpgradeCategory.Gacha, UpgradeCategory.Economy };
            var cNames = new[] { "클릭 계열", "가챠 계열", "경제 계열" };
            var cCols  = new[] { CatClick, CatGacha, CatEcon };

            foreach (var cat in cats)
            {
                int ci = System.Array.IndexOf(cats, cat);
                bool anyInCat = false;
                foreach (var u in catalog.Upgrades) if (u != null && u.Category == cat) { anyInCat = true; break; }
                if (!anyInCat) continue;

                var hdrGO = new GameObject("SecHdr_" + cat, typeof(RectTransform));
                hdrGO.transform.SetParent(upgradeScrollContent, false);
                var hdrRT = hdrGO.GetComponent<RectTransform>();
                hdrRT.sizeDelta = new Vector2(0, 26);
                hdrGO.AddComponent<Image>().color = cCols[ci] * new Color(1,1,1,0.45f);

                var hdrTxt = MakeLabel(hdrGO.transform, cNames[ci], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                hdrTxt.alignment = TextAlignmentOptions.Left; hdrTxt.margin = new Vector4(10, 0, 0, 0);

                foreach (var upg in catalog.Upgrades)
                {
                    if (upg == null || upg.Category != cat) continue;
                    if (upg.UpgradeId == "upg-normal-prob-2" && gm.GetUpgradeLevel("upg-normal-prob-1") < 5) continue;
                    if (upg.UpgradeId == "upg-rare-prob-2" && gm.GetUpgradeLevel("upg-rare-prob-1") < 5) continue;
                    if (upg.UpgradeId == "upg-super-prob-2" && gm.GetUpgradeLevel("upg-super-prob-1") < 5) continue;

                    var row = new GameObject("Row_" + upg.UpgradeId, typeof(RectTransform));
                    row.transform.SetParent(upgradeScrollContent, false);
                    var rRT = row.GetComponent<RectTransform>();
                    rRT.sizeDelta = new Vector2(0, 80);
                    row.AddComponent<Image>().color = new Color(0.10f, 0.13f, 0.20f, 1f);

                    var acc = MakeRT("Acc", row.transform, Vector2.zero, Vector2.zero);
                    acc.anchorMin = new Vector2(0,0); acc.anchorMax = new Vector2(0,1);
                    acc.offsetMin = Vector2.zero; acc.offsetMax = new Vector2(4,0);
                    acc.gameObject.AddComponent<Image>().color = cCols[ci];

                    var nm = MakeLabel(row.transform, upg.DisplayName, Vector2.zero, Vector2.zero, 13, Color.white);
                    var nmRT = nm.GetComponent<RectTransform>();
                    nmRT.anchorMin = new Vector2(0, 0.60f); nmRT.anchorMax = new Vector2(0.58f, 0.95f);
                    nmRT.offsetMin = new Vector2(14,0); nmRT.offsetMax = Vector2.zero;
                    nm.alignment = TextAlignmentOptions.Left;

                    string descText = upg.Description + GetUpgradeValuesString(upg);
                    var dc = MakeLabel(row.transform, descText, Vector2.zero, Vector2.zero, 10, new Color(0.60f,0.65f,0.75f));
                    var dcRT = dc.GetComponent<RectTransform>();
                    dcRT.anchorMin = new Vector2(0, 0.05f); dcRT.anchorMax = new Vector2(0.58f, 0.55f);
                    dcRT.offsetMin = new Vector2(14,0); dcRT.offsetMax = Vector2.zero;
                    dc.alignment = TextAlignmentOptions.Left; dc.overflowMode = TextOverflowModes.Overflow; dc.enableWordWrapping = true;

                    var lbRT = MakeRT("LvBadge", row.transform, Vector2.zero, Vector2.zero);
                    lbRT.anchorMin = new Vector2(0.58f, 0.25f); lbRT.anchorMax = new Vector2(0.73f, 0.75f);
                    lbRT.offsetMin = new Vector2(2,0); lbRT.offsetMax = new Vector2(-2,0);
                    lbRT.gameObject.AddComponent<Image>().color = new Color(0.14f,0.17f,0.26f);
                    var lvTxt = MakeLabel(lbRT, "Lv.0/0", Vector2.zero, Vector2.zero, 11, new Color(0.5f,0.85f,1f), FontStyles.Bold);

                    var bbRT = MakeRT("BuyBtn", row.transform, Vector2.zero, Vector2.zero);
                    bbRT.anchorMin = new Vector2(0.74f, 0.20f); bbRT.anchorMax = new Vector2(0.99f, 0.80f);
                    bbRT.offsetMin = bbRT.offsetMax = Vector2.zero;
                    bbRT.gameObject.AddComponent<Image>().color = BtnBuy;
                    var bb = bbRT.gameObject.AddComponent<Button>();
                    string uid = upg.UpgradeId;
                    bb.onClick.AddListener(() => gm.BuyUpgrade(uid));
                    var costTxt = MakeLabel(bbRT, "0", Vector2.zero, Vector2.zero, 11, Color.white);

                    var info = row.AddComponent<UpgradeRowInfo>();
                    info.UpgradeId = upg.UpgradeId; info.LevelText = lvTxt; info.BuyButton = bb; info.CostText = costTxt; info.BgImage = bbRT.GetComponent<Image>();
                    upgradeRows.Add(row);
                }
            }
        }

        // ── Shard Exchange Tab (4 Rarities: N, R, SR, SSR) ────────────────────
        private void BuildShardTab()
        {
            MakeLabel(shardContent.transform, "조각으로 미획득 카드를 교환 획득합니다",
                new Vector2(0, 145), new Vector2(500,26), 15, new Color(0.75f,0.80f,0.90f));
            MakeLabel(shardContent.transform, "구매할 때마다 교환 비용이 2배로 증가합니다",
                new Vector2(0, 120), new Vector2(500,20), 11, new Color(0.50f,0.55f,0.65f));

            BuildShardCard(CardRarity.N,   "N 등급",  "일반 카드",   ColN,   -255f);
            BuildShardCard(CardRarity.R,   "R 등급",  "레어 카드",   ColR,    -85f);
            BuildShardCard(CardRarity.SR,  "SR 등급", "슈퍼레어",    ColSR,    85f);
            BuildShardCard(CardRarity.SSR, "SSR 등급","최상위 카드", ColSSR,  255f);
        }

        private void BuildShardCard(CardRarity rarity, string label, string sub, Color col, float xPos)
        {
            var card = MakeRT("Card_" + rarity, shardContent.transform, new Vector2(xPos, 0f), new Vector2(155, 215));
            card.gameObject.AddComponent<Image>().color = col * new Color(1,1,1,0.45f);

            var cardIn = MakeRT("In", card, Vector2.zero, Vector2.zero);
            cardIn.anchorMin = Vector2.zero; cardIn.anchorMax = Vector2.one;
            cardIn.offsetMin = new Vector2(2,2); cardIn.offsetMax = new Vector2(-2,-2);
            cardIn.gameObject.AddComponent<Image>().color = new Color(0.09f,0.11f,0.18f);

            MakeLabel(cardIn, label, new Vector2(0,72), new Vector2(145,26), 14, col, FontStyles.Bold);
            MakeLabel(cardIn, sub,   new Vector2(0,48), new Vector2(145,20), 11, new Color(0.65f,0.70f,0.80f));

            // Unified Buy & Cost Button
            var bb = MakeRT("Buy", cardIn, new Vector2(0,-30), new Vector2(140, 95));
            bb.gameObject.AddComponent<Image>().color = col;
            var btn = bb.gameObject.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = col * 1.3f; cs.pressedColor = col * 0.7f;
            cs.disabledColor = BtnDisabled; btn.colors = cs;

            CardRarity r = rarity;
            btn.onClick.AddListener(() => BuyRandomCard(r));

            var btnTxt = MakeLabel(bb, $"{label} 교환\n...", Vector2.zero, Vector2.zero, 12, Color.white, FontStyles.Bold);
            btnTxt.alignment = TextAlignmentOptions.Center;

            if (rarity == CardRarity.N)   { buyNBtn = btn;   buyNText = btnTxt; }
            if (rarity == CardRarity.R)   { buyRBtn = btn;   buyRText = btnTxt; }
            if (rarity == CardRarity.SR)  { buySRBtn = btn;  buySRText = btnTxt; }
            if (rarity == CardRarity.SSR) { buySSRBtn = btn; buySSRText = btnTxt; }
        }

        private bool BindExistingProductsUI()
        {
            if (productsContent == null) return false;

            productSlotGOs.Clear();

            var gridPanelTrans = productsContent.transform.Find("GridPanel") 
                              ?? productsContent.transform.Find("Grid")
                              ?? productsContent.transform.Find("Slots");

            Transform searchParent = gridPanelTrans != null ? gridPanelTrans : productsContent.transform;

            foreach (Transform child in searchParent)
            {
                if (child.name.StartsWith("ProductSlot_") || child.name.StartsWith("Slot_") || child.name.StartsWith("Slot") || child.name.Contains("Slot") || child.GetComponent<Button>() != null)
                {
                    productSlotGOs.Add(child.gameObject);
                }
            }

            if (productSlotGOs.Count == 0) return false;

            if (prevProductPageBtn == null)
            {
                var b = productsContent.transform.Find("Btn_◀") 
                     ?? productsContent.transform.Find("PrevBtn") 
                     ?? productsContent.transform.Find("Btn_Prev");
                if (b != null) prevProductPageBtn = b.GetComponent<Button>();
            }
            if (nextProductPageBtn == null)
            {
                var b = productsContent.transform.Find("Btn_▶") 
                     ?? productsContent.transform.Find("NextBtn") 
                     ?? productsContent.transform.Find("Btn_Next");
                if (b != null) nextProductPageBtn = b.GetComponent<Button>();
            }
            if (productPageText == null)
            {
                var t = productsContent.transform.Find("ProductPageText") 
                     ?? productsContent.transform.Find("PageText")
                     ?? productsContent.transform.Find("ProductPageText/Label");
                if (t != null) productPageText = t.GetComponent<TMP_Text>();
            }

            var buyPanelTrans = productsContent.transform.Find("BuyPanel") 
                             ?? productsContent.transform.Find("BottomPanel")
                             ?? productsContent.transform.Find("DetailPanel");
            if (buyPanelTrans != null)
            {
                if (selectedProductNameText == null)
                {
                    var t = buyPanelTrans.Find("InfoBox/NameText") 
                         ?? buyPanelTrans.Find("InfoBox/Label") 
                         ?? buyPanelTrans.Find("NameText");
                    if (t == null)
                    {
                        var tmps = buyPanelTrans.GetComponentsInChildren<TMP_Text>(true);
                        if (tmps.Length > 0) selectedProductNameText = tmps[0];
                    }
                    else selectedProductNameText = t.GetComponent<TMP_Text>();
                }

                if (selectedProductDescText == null)
                {
                    var t = buyPanelTrans.Find("InfoBox/DescText") 
                         ?? buyPanelTrans.Find("InfoBox/Description")
                         ?? buyPanelTrans.Find("DescText");
                    if (t == null)
                    {
                        var tmps = buyPanelTrans.GetComponentsInChildren<TMP_Text>(true);
                        if (tmps.Length > 1) selectedProductDescText = tmps[1];
                    }
                    else selectedProductDescText = t.GetComponent<TMP_Text>();
                }

                if (buyProductBtn == null)
                {
                    var b = buyPanelTrans.Find("Btn_구매하기") 
                         ?? buyPanelTrans.Find("BuyButton") 
                         ?? buyPanelTrans.Find("BuyBtn");
                    if (b == null)
                    {
                        var btns = buyPanelTrans.GetComponentsInChildren<Button>(true);
                        if (btns.Length > 0) buyProductBtn = btns[0];
                    }
                    else buyProductBtn = b.GetComponent<Button>();
                }

                if (buyProductBtn != null && buyProductBtnText == null)
                {
                    buyProductBtnText = buyProductBtn.GetComponentInChildren<TMP_Text>();
                }
            }

            for (int i = 0; i < productSlotGOs.Count; i++)
            {
                var slotGO = productSlotGOs[i];
                var btn = slotGO.GetComponent<Button>() ?? slotGO.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    int idx = i;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnProductSlotClicked(idx));
                }
            }

            if (prevProductPageBtn != null)
            {
                prevProductPageBtn.onClick.RemoveAllListeners();
                prevProductPageBtn.onClick.AddListener(OnPrevProductPageClicked);
            }
            if (nextProductPageBtn != null)
            {
                nextProductPageBtn.onClick.RemoveAllListeners();
                nextProductPageBtn.onClick.AddListener(OnNextProductPageClicked);
            }
            if (buyProductBtn != null)
            {
                buyProductBtn.onClick.RemoveAllListeners();
                buyProductBtn.onClick.AddListener(OnBuySelectedProductClicked);
            }

            return true;
        }

        // ── Products Tab (With Rarity Frame & Art Integration) ────────────────
        private void BuildProductsTab()
        {
            if (productsContent == null) return;

            InitializeProductCatalog();

            // Check if user already manually created UI in Hierarchy
            if (BindExistingProductsUI()) return;

            MakeLabel(productsContent.transform, "상점 상품 목록",
                new Vector2(0, 150), new Vector2(500, 24), 15, new Color(0.85f, 0.88f, 0.95f), FontStyles.Bold);

            // Pagination Controls
            prevProductPageBtn = MakeButton(productsContent.transform, "◀", new Vector2(-150, 150), new Vector2(36, 26), new Color(0.18f, 0.26f, 0.44f), OnPrevProductPageClicked, 12).GetComponent<Button>();
            nextProductPageBtn = MakeButton(productsContent.transform, "▶", new Vector2(150, 150), new Vector2(36, 26), new Color(0.18f, 0.26f, 0.44f), OnNextProductPageClicked, 12).GetComponent<Button>();

            var pTxtGO = MakeRT("ProductPageText", productsContent.transform, new Vector2(0, 150), new Vector2(100, 26));
            productPageText = MakeLabel(pTxtGO, "1 / 1", Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);

            // 10 Grid Slots Area (2 rows x 5 cols)
            var gridPanel = MakeRT("GridPanel", productsContent.transform, new Vector2(0, 20), new Vector2(700, 210));

            productSlotGOs.Clear();
            float startX = -280f;
            float stepX = 140f;
            float startY = 55f;
            float stepY = -105f;

            for (int i = 0; i < PRODUCTS_PER_PAGE; i++)
            {
                int row = i / 5;
                int col = i % 5;
                float x = startX + col * stepX;
                float y = startY + row * stepY;

                var slotGO = MakeRT($"ProductSlot_{i}", gridPanel, new Vector2(x, y), new Vector2(130, 95));
                
                // Rarity Frame Image (Background frame for rarity)
                var frameImg = slotGO.gameObject.AddComponent<Image>();
                frameImg.color = Color.white;

                var slotBtn = slotGO.gameObject.AddComponent<Button>();
                int slotIndex = i;
                slotBtn.onClick.AddListener(() => OnProductSlotClicked(slotIndex));

                // Card Art / Product Icon Image
                var iconGO = MakeRT("Art", slotGO, new Vector2(0, 10), new Vector2(50, 50));
                var iconImg = iconGO.gameObject.AddComponent<Image>();
                iconImg.color = Color.white;

                // rereMark Rarity Mark Image
                var markGO = MakeRT("rereMark", slotGO, new Vector2(-48, 30), new Vector2(24, 24));
                var markImg = markGO.gameObject.AddComponent<Image>();
                markImg.color = Color.white;

                // Product Name Text
                var nameTxt = MakeLabel(slotGO, "---", new Vector2(0, -18), new Vector2(120, 20), 10, Color.white, FontStyles.Bold);
                nameTxt.alignment = TextAlignmentOptions.Center;

                // Price Symbol & Value Text
                var priceTxt = MakeLabel(slotGO, "🪙 0", new Vector2(0, -34), new Vector2(120, 20), 10, GoldColor, FontStyles.Bold);
                priceTxt.alignment = TextAlignmentOptions.Center;

                // Selection Border Highlight
                var selBorder = MakeRT("SelectedBorder", slotGO, Vector2.zero, Vector2.zero);
                selBorder.anchorMin = Vector2.zero; selBorder.anchorMax = Vector2.one;
                selBorder.offsetMin = selBorder.offsetMax = Vector2.zero;
                var selImg = selBorder.gameObject.AddComponent<Image>();
                selImg.color = Indicator;
                selImg.raycastTarget = false;
                selBorder.gameObject.SetActive(false);

                // Status Badge Text
                var badgeTxt = MakeLabel(slotGO, "", new Vector2(0, 0), new Vector2(120, 20), 10, Color.yellow, FontStyles.Bold);
                badgeTxt.gameObject.SetActive(false);

                productSlotGOs.Add(slotGO.gameObject);
            }

            // Bottom Product Detail & Purchase Panel
            var buyPanel = MakeRT("BuyPanel", productsContent.transform, new Vector2(0, -125), new Vector2(700, 70));
            buyPanel.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.11f, 0.18f, 1f);

            var infoBox = MakeRT("InfoBox", buyPanel, new Vector2(-150, 0), new Vector2(360, 60));
            selectedProductNameText = MakeLabel(infoBox, "상품 선택", new Vector2(0, 15), new Vector2(350, 24), 13, GoldColor, FontStyles.Bold);
            selectedProductNameText.alignment = TextAlignmentOptions.Left;

            selectedProductDescText = MakeLabel(infoBox, "슬롯을 클릭하여 원하는 상품을 구매하세요.", new Vector2(0, -10), new Vector2(350, 30), 10, new Color(0.7f, 0.75f, 0.85f));
            selectedProductDescText.alignment = TextAlignmentOptions.Left;

            // Main Purchase Button
            var buyBtnGO = MakeButton(buyPanel, "구매하기", new Vector2(220, 0), new Vector2(180, 44), BtnBuy, OnBuySelectedProductClicked, 14);
            buyProductBtn = buyBtnGO.GetComponent<Button>();
            buyProductBtnText = buyBtnGO.GetComponentInChildren<TMP_Text>();
        }

        private void OnPrevProductPageClicked()
        {
            if (currentProductPage > 0)
            {
                currentProductPage--;
                Refresh();
            }
        }

        private void OnNextProductPageClicked()
        {
            int totalProducts = productCatalog.Count;
            int maxPages = Mathf.Max(1, Mathf.CeilToInt(totalProducts / (float)PRODUCTS_PER_PAGE));
            if (currentProductPage < maxPages - 1)
            {
                currentProductPage++;
                Refresh();
            }
        }

        private void OnProductSlotClicked(int slotIndexOnPage)
        {
            int itemIdx = currentProductPage * PRODUCTS_PER_PAGE + slotIndexOnPage;
            if (itemIdx >= 0 && itemIdx < productCatalog.Count)
            {
                selectedProduct = productCatalog[itemIdx];
                Refresh();
            }
        }

        private void OnBuySelectedProductClicked()
        {
            if (selectedProduct == null || gm == null) return;

            string status = GetProductStatus(selectedProduct);
            if (status == "Purchased" || status == "Equipped")
            {
                if (selectedProduct.productType == ShopProductType.Background)
                {
                    gm.EquipBackground(selectedProduct.targetId);
                }
                else if (selectedProduct.productType == ShopProductType.Decoration)
                {
                    gm.EquipDecoration(selectedProduct.targetId);
                }
                else if (selectedProduct.productType == ShopProductType.Card)
                {
                    gm.EquipCard(selectedProduct.targetId);
                }
                Refresh();
                return;
            }

            bool afford = selectedProduct.currencyType == ProductCurrencyType.Coin ?
                (gm.Money >= selectedProduct.price) : (gm.Shards >= selectedProduct.price);

            if (!afford) return;

            if (selectedProduct.currencyType == ProductCurrencyType.Coin) gm.DeductMoney(selectedProduct.price);
            else if (selectedProduct.currencyType == ProductCurrencyType.Shard) gm.DeductShards((int)selectedProduct.price);

            if (selectedProduct.productType == ShopProductType.GachaUnlock)
            {
                if (selectedProduct.targetId == "Rare") gm.UnlockGachaType(GachaType.Rare, 0);
                else if (selectedProduct.targetId == "Super") gm.UnlockGachaType(GachaType.Super, 0);
            }
            else if (selectedProduct.productType == ShopProductType.Background)
            {
                gm.EquipBackground(selectedProduct.targetId);
            }
            else if (selectedProduct.productType == ShopProductType.Decoration)
            {
                gm.EquipDecoration(selectedProduct.targetId);
            }
            else if (selectedProduct.productType == ShopProductType.Card)
            {
                gm.GrantCard(selectedProduct.targetId);
                gm.EquipCard(selectedProduct.targetId);
            }

            gm.SaveGame();
            gm.NotifyStateChange();
            Refresh();
        }

        private string GetProductStatus(ShopProductItem prod)
        {
            if (prod == null || gm == null) return "NotPurchased";

            if (prod.productType == ShopProductType.GachaUnlock)
            {
                if (prod.targetId == "Rare" && gm.UnlockedRareGacha) return "Purchased";
                if (prod.targetId == "Super" && gm.UnlockedSuperGacha) return "Purchased";
                return "NotPurchased";
            }
            else if (prod.productType == ShopProductType.Background)
            {
                if (gm.EquippedBackgroundId == prod.targetId) return "Equipped";
                if (gm.IsSetCompleted(prod.id)) return "Purchased";
                return "NotPurchased";
            }
            else if (prod.productType == ShopProductType.Decoration)
            {
                if (gm.EquippedDecorationId == prod.targetId) return "Equipped";
                if (gm.IsSetCompleted(prod.id)) return "Purchased";
                return "NotPurchased";
            }
            else if (prod.productType == ShopProductType.Card)
            {
                var states = gm.GetCardStates();
                if (states.TryGetValue(prod.targetId, out var st) && st.Unlocked && st.Copies > 0)
                {
                    if (gm.EquippedCardId == prod.targetId) return "Equipped";
                    return "Purchased";
                }
                return "NotPurchased";
            }

            return "NotPurchased";
        }

        // ── Tab Switching ─────────────────────────────────────────────────────
        private void ShowTab(int index)
        {
            activeTab = index;
            EnsureReferencesResolved();

            // Activate target tab content directly
            if (upgradesContent != null) upgradesContent.SetActive(index == 0);
            if (shardContent != null) shardContent.SetActive(index == 1);
            if (productsContent != null) productsContent.SetActive(index == 2);

            // Deactivate any other legacy content container under Panel/Inner
            var inner = transform.Find("Panel/Inner");
            if (inner != null)
            {
                foreach (Transform child in inner)
                {
                    if (child.gameObject != upgradesContent && 
                        child.gameObject != shardContent && 
                        child.gameObject != productsContent)
                    {
                        string n = child.name;
                        if (n.EndsWith("Content") || n.Contains("GachaUnlock") || n.Contains("Goods"))
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }
            }

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
                RefreshShardButton(CardRarity.N,   buyNBtn,   buyNText);
                RefreshShardButton(CardRarity.R,   buyRBtn,   buyRText);
                RefreshShardButton(CardRarity.SR,  buySRBtn,  buySRText);
                RefreshShardButton(CardRarity.SSR, buySSRBtn, buySSRText);
            }

            if (productsContent != null && productsContent.activeSelf)
            {
                RefreshProductsTab();
            }
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
                            info.CostText.text = $"{cost:F0}";
                            info.CostText.color = GoldColor;
                        }
                    }

                    if (info.BuyButton != null) info.BuyButton.interactable = afford;
                    if (info.BgImage   != null) info.BgImage.color = afford ? BtnBuy : BtnDisabled;
                }
            }
        }

        private void RefreshShardButton(CardRarity rarity, Button btn, TMP_Text txt)
        {
            if (btn == null || txt == null || gm == null || gm.CardCatalog == null) return;

            int currentCost = gm.GetShardExchangeCost(rarity);
            var states = gm.GetCardStates();
            int candCount = 0;

            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && !card.IsShop && card.Rarity == rarity)
                {
                    if (!(states.TryGetValue(card.Id, out var st) && st.Unlocked && st.Copies > 0))
                    {
                        candCount++;
                    }
                }
            }

            if (candCount == 0)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && !card.IsHidden && !card.IsShop && card.Rarity == rarity)
                    {
                        int maxBreakthroughCopies = 6;
                        if (states.TryGetValue(card.Id, out var st))
                        {
                            if (st.Copies < maxBreakthroughCopies) candCount++;
                        }
                    }
                }
            }

            Color baseCol = rarity == CardRarity.SSR ? ColSSR : rarity == CardRarity.SR ? ColSR : rarity == CardRarity.R ? ColR : ColN;
            if (candCount == 0)
            {
                btn.interactable = false;
                txt.text = "교환 완료";
                btn.GetComponent<Image>().color = new Color(0.22f,0.48f,0.26f);
            }
            else
            {
                bool afford = gm.Shards >= currentCost;
                btn.interactable = afford;
                txt.text = $"{rarity} 등급 교환\n[{currentCost} 조각]";
                btn.GetComponent<Image>().color = afford ? baseCol : BtnDisabled;
            }
        }

        private void RefreshProductsTab()
        {
            EnsureReferencesResolved();
            if (productsContent == null) return;

            if (productSlotGOs.Count == 0 || prevProductPageBtn == null)
            {
                BuildProductsTab();
            }

            InitializeProductCatalog();

            int totalProducts = productCatalog.Count;
            int maxPages = Mathf.Max(1, Mathf.CeilToInt(totalProducts / (float)PRODUCTS_PER_PAGE));
            currentProductPage = Mathf.Clamp(currentProductPage, 0, maxPages - 1);

            if (productPageText != null) productPageText.text = $"{currentProductPage + 1} / {maxPages}";
            if (prevProductPageBtn != null) prevProductPageBtn.interactable = currentProductPage > 0;
            if (nextProductPageBtn != null) nextProductPageBtn.interactable = currentProductPage < maxPages - 1;

            // Render 10 slots with Rarity Frame, Art, rereMark
            for (int i = 0; i < productSlotGOs.Count; i++)
            {
                var slotGO = productSlotGOs[i];
                int itemIdx = currentProductPage * PRODUCTS_PER_PAGE + i;

                if (itemIdx < productCatalog.Count)
                {
                    slotGO.SetActive(true);
                    var prod = productCatalog[itemIdx];
                    bool isSelected = selectedProduct == prod;

                    // 1. Rarity Frame Image
                    var slotImg = slotGO.GetComponent<Image>();
                    if (slotImg != null)
                    {
                        Sprite frameSp = GetFrameSpriteForRarity(prod.rarity);
                        if (frameSp != null) { slotImg.sprite = frameSp; slotImg.color = Color.white; }
                        else { slotImg.sprite = null; slotImg.color = new Color(0.10f, 0.14f, 0.22f, 1f); }
                    }

                    // 2. Art / Icon Image
                    var iconImg = slotGO.transform.Find("Art")?.GetComponent<Image>();
                    if (iconImg != null)
                    {
                        if (prod.iconSprite != null) { iconImg.sprite = prod.iconSprite; iconImg.color = Color.white; iconImg.gameObject.SetActive(true); }
                        else { iconImg.gameObject.SetActive(false); }
                    }

                    // 3. rereMark Image
                    var markImg = slotGO.transform.Find("rereMark")?.GetComponent<Image>();
                    if (markImg != null)
                    {
                        Sprite markSp = GetMarkSpriteForRarity(prod.rarity);
                        if (markSp != null) { markImg.sprite = markSp; markImg.color = Color.white; markImg.gameObject.SetActive(true); }
                        else { markImg.gameObject.SetActive(false); }
                    }

                    // 4. Name Text
                    var nameTxt = slotGO.transform.Find("Label")?.GetComponent<TMP_Text>() ?? slotGO.transform.Find("Name")?.GetComponent<TMP_Text>();
                    if (nameTxt != null) nameTxt.text = prod.displayName;

                    // 5. Price Symbol & Value
                    var priceTxt = slotGO.transform.Find("Price")?.GetComponent<TMP_Text>();
                    if (priceTxt != null)
                    {
                        string symbolStr = prod.currencyType == ProductCurrencyType.Coin ? "🪙" : "🔷";
                        priceTxt.text = $"{symbolStr} {prod.price:0}";
                        priceTxt.color = prod.currencyType == ProductCurrencyType.Coin ? GoldColor : ShardColor;
                    }

                    // 6. Selection Highlight Border
                    var selBorder = slotGO.transform.Find("SelectedBorder");
                    if (selBorder != null) selBorder.gameObject.SetActive(isSelected);

                    // 7. Status Badge
                    var badgeTxt = slotGO.transform.Find("Badge")?.GetComponent<TMP_Text>();
                    string status = GetProductStatus(prod);
                    if (badgeTxt != null)
                    {
                        if (status == "Equipped") { badgeTxt.gameObject.SetActive(true); badgeTxt.text = "장착중"; badgeTxt.color = Color.green; }
                        else if (status == "Purchased") { badgeTxt.gameObject.SetActive(true); badgeTxt.text = "보유중"; badgeTxt.color = Color.yellow; }
                        else { badgeTxt.gameObject.SetActive(false); }
                    }
                }
                else
                {
                    slotGO.SetActive(false);
                }
            }

            // Update Selected Product Buy Panel
            if (selectedProduct != null)
            {
                if (selectedProductNameText != null) selectedProductNameText.text = selectedProduct.displayName;
                if (selectedProductDescText != null) selectedProductDescText.text = selectedProduct.description;

                string status = GetProductStatus(selectedProduct);
                bool afford = selectedProduct.currencyType == ProductCurrencyType.Coin ?
                    (gm.Money >= selectedProduct.price) : (gm.Shards >= selectedProduct.price);
                string symbolStr = selectedProduct.currencyType == ProductCurrencyType.Coin ? "🪙" : "🔷";

                if (buyProductBtn != null)
                {
                    if (status == "Equipped")
                    {
                        buyProductBtn.interactable = false;
                        if (buyProductBtnText != null) buyProductBtnText.text = "장착중";
                        buyProductBtn.GetComponent<Image>().color = BtnDisabled;
                    }
                    else if (status == "Purchased")
                    {
                        if (selectedProduct.productType == ShopProductType.GachaUnlock)
                        {
                            buyProductBtn.interactable = false;
                            if (buyProductBtnText != null) buyProductBtnText.text = "구매완료";
                            buyProductBtn.GetComponent<Image>().color = BtnDisabled;
                        }
                        else
                        {
                            buyProductBtn.interactable = true;
                            if (buyProductBtnText != null) buyProductBtnText.text = "장착하기";
                            buyProductBtn.GetComponent<Image>().color = BtnBuy;
                        }
                    }
                    else
                    {
                        if (afford)
                        {
                            buyProductBtn.interactable = true;
                            if (buyProductBtnText != null) buyProductBtnText.text = $"구매하기 ({symbolStr} {selectedProduct.price:0})";
                            buyProductBtn.GetComponent<Image>().color = BtnBuy;
                        }
                        else
                        {
                            buyProductBtn.interactable = false;
                            if (buyProductBtnText != null) buyProductBtnText.text = "소지금 부족";
                            buyProductBtn.GetComponent<Image>().color = BtnDisabled;
                        }
                    }
                }
            }
        }

        // ── Shard Purchase ────────────────────────────────────────────────────
        private void BuyRandomCard(CardRarity rarity)
        {
            if (gm == null || gm.CardCatalog == null) return;
            int cost = gm.GetShardExchangeCost(rarity);
            if (gm.Shards < cost) return;

            var cands = new List<CardEntry>();
            var states = gm.GetCardStates();

            foreach (var card in gm.CardCatalog.Cards)
            {
                if (card != null && !card.IsHidden && !card.IsShop && card.Rarity == rarity)
                {
                    if (!(states.TryGetValue(card.Id, out var st) && st.Unlocked && st.Copies > 0))
                    {
                        cands.Add(card);
                    }
                }
            }

            if (cands.Count == 0)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && !card.IsHidden && !card.IsShop && card.Rarity == rarity)
                    {
                        int maxBreakthroughCopies = 6;
                        if (states.TryGetValue(card.Id, out var st))
                        {
                            if (st.Copies < maxBreakthroughCopies) cands.Add(card);
                        }
                    }
                }
            }

            if (cands.Count == 0) return;

            var chosen = cands[Random.Range(0, cands.Count)];

            gm.DeductShards(cost);
            gm.IncrementExchangeCount(rarity);

            var type = gm.GetType();
            var applyDraw = type.GetMethod("ApplyDraw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            applyDraw?.Invoke(gm, new object[] { chosen });
            gm.SaveGame();
            gm.NotifyStateChange();
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
            var go = MakeRT("Label", parent, pos, size);
            var tx = go.gameObject.AddComponent<TextMeshProUGUI>();
            if (customFont != null) tx.font = customFont;
            tx.text = text; tx.fontSize = fs; tx.fontStyle = style;
            tx.alignment = TextAlignmentOptions.Center; tx.color = col;
            return tx;
        }

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bgColor, UnityEngine.Events.UnityAction onClick, int fs = 13)
        {
            var go = MakeRT("Btn_" + label, parent, pos, size);
            var img = go.gameObject.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.gameObject.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = new Color(0.25f, 0.35f, 0.52f);
            cs.pressedColor = new Color(0.12f, 0.18f, 0.30f);
            btn.colors = cs;
            if (onClick != null) btn.onClick.AddListener(onClick);

            MakeLabel(go, label, Vector2.zero, Vector2.zero, fs, Color.white, FontStyles.Bold);
            return go.gameObject;
        }

        private void BindListeners()
        {
            var buttons = GetComponentsInChildren<Button>(true);
            var upgradeInfos = GetComponentsInChildren<UpgradeRowInfo>(true);

            upgradeRows.Clear();
            foreach (var info in upgradeInfos)
            {
                if (info != null && info.gameObject != null) upgradeRows.Add(info.gameObject);
            }

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var card = btn.transform.GetComponentInParent<Transform>();
                while (card != null && !card.name.StartsWith("Card_")) card = card.parent;

                var txt = btn.GetComponentInChildren<TMP_Text>();
                if (card != null && txt != null)
                {
                    if (card.name == "Card_N")   { buyNBtn = btn;   buyNText = txt; }
                    else if (card.name == "Card_R")   { buyRBtn = btn;   buyRText = txt; }
                    else if (card.name == "Card_SR")  { buySRBtn = btn;  buySRText = txt; }
                    else if (card.name == "Card_SSR") { buySSRBtn = btn; buySSRText = txt; }
                }
            }

            foreach (var btn in buttons)
            {
                if (btn == null) continue;
                var txt = btn.GetComponentInChildren<TMP_Text>();
                if (txt == null) continue;

                string t = txt.text.Trim();
                var rowInfo = btn.GetComponentInParent<UpgradeRowInfo>();
                if (rowInfo != null)
                {
                    string upgId = rowInfo.UpgradeId;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { if (gm != null) gm.BuyUpgrade(upgId); });
                    continue;
                }

                btn.onClick.RemoveAllListeners();

                if (t == "업그레이드") btn.onClick.AddListener(() => ShowTab(0));
                else if (t == "조각 교환") btn.onClick.AddListener(() => ShowTab(1));
                else if (t == "상품") btn.onClick.AddListener(() => ShowTab(2));
                else if (btn.gameObject.name == "CloseButton" || btn.gameObject.name.StartsWith("Btn_✕") || t == "✕" || t.ToLower() == "x")
                {
                    btn.onClick.AddListener(() => gameObject.SetActive(false));
                }
                else if (t.Contains("교환"))
                {
                    var card = btn.transform.GetComponentInParent<Transform>();
                    while (card != null && !card.name.StartsWith("Card_")) card = card.parent;
                    if (card != null)
                    {
                        if (card.name == "Card_N")       btn.onClick.AddListener(() => BuyRandomCard(CardRarity.N));
                        else if (card.name == "Card_R")  btn.onClick.AddListener(() => BuyRandomCard(CardRarity.R));
                        else if (card.name == "Card_SR") btn.onClick.AddListener(() => BuyRandomCard(CardRarity.SR));
                        else if (card.name == "Card_SSR")btn.onClick.AddListener(() => BuyRandomCard(CardRarity.SSR));
                    }
                }
            }
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj, true);
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
            if (productsContent == null)
            {
                var t = innerTrans.Find("ProductsContent");
                if (t == null) t = innerTrans.Find("GoodsContent");
                if (t == null) t = innerTrans.Find("GachaUnlockContent");
                if (t != null) productsContent = t.gameObject;
            }

            // If legacy GachaUnlockContent exists alongside ProductsContent, deactivate it
            var legacyGacha = innerTrans.Find("GachaUnlockContent");
            var prodObj = innerTrans.Find("ProductsContent");
            if (legacyGacha != null && prodObj != null && legacyGacha != prodObj)
            {
                legacyGacha.gameObject.SetActive(false);
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
                    case UpgradeEffectType.NWeightReduction:
                    case UpgradeEffectType.RWeightReduction:
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
