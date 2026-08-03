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

        [Header("Inspector Explicit Localized Text Fields")]
        [SerializeField] private TMP_Text shopTitleText;
        [SerializeField] private TMP_Text tab0Text;
        [SerializeField] private TMP_Text tab1Text;
        [SerializeField] private TMP_Text tab2Text;
        [SerializeField] private TMP_Text sechdrClickText;
        [SerializeField] private TMP_Text sechdrGachaText;
        [SerializeField] private TMP_Text sechdrEconomyText;
        [SerializeField] private TMP_Text sechdrSpecialText;

        [Header("Rarity Frame & Mark Sprites")]
        [SerializeField] private Sprite spriteFrameN;
        [SerializeField] private Sprite spriteFrameR;
        [SerializeField] private Sprite spriteFrameSR;
        [SerializeField] private Sprite spriteFrameSSR;
        [SerializeField] private Sprite spriteFrameH;
        [SerializeField] private Sprite spriteFrameLock;
        [SerializeField] private Sprite spriteMarkN;
        [SerializeField] private Sprite spriteMarkR;
        [SerializeField] private Sprite spriteMarkSR;
        [SerializeField] private Sprite spriteMarkSSR;
        [SerializeField] private Sprite spriteMarkH;

        [Header("Currency Symbol Sprites")]
        [SerializeField] private Sprite coinSymbolSprite;
        [SerializeField] private Sprite shardSymbolSprite;

        public Sprite CoinSymbolSprite { get => coinSymbolSprite; set => coinSymbolSprite = value; }
        public Sprite ShardSymbolSprite { get => shardSymbolSprite; set => shardSymbolSprite = value; }

        [Header("Section Header Sprites")]
        [SerializeField] private Sprite secHdrSpriteClick;
        [SerializeField] private Sprite secHdrSpriteGacha;
        [SerializeField] private Sprite secHdrSpriteEconomy;
        [SerializeField] private Sprite secHdrSpriteSpecial;

        [Header("Category Row Background/Icon Sprites")]
        [SerializeField] private Sprite rowSpriteClick;
        [SerializeField] private Sprite rowSpriteGacha;
        [SerializeField] private Sprite rowSpriteEconomy;
        [SerializeField] private Sprite rowSpriteSpecial;

        [Header("Particle Sprites")]
        [SerializeField] private Sprite pawPrintParticleSprite;

        public Sprite PawPrintParticleSprite { get => pawPrintParticleSprite; set => pawPrintParticleSprite = value; }

        public Sprite SecHdrSpriteClick { get => secHdrSpriteClick; set => secHdrSpriteClick = value; }
        public Sprite SecHdrSpriteGacha { get => secHdrSpriteGacha; set => secHdrSpriteGacha = value; }
        public Sprite SecHdrSpriteEconomy { get => secHdrSpriteEconomy; set => secHdrSpriteEconomy = value; }
        public Sprite SecHdrSpriteSpecial { get => secHdrSpriteSpecial; set => secHdrSpriteSpecial = value; }

        public Sprite RowSpriteClick { get => rowSpriteClick; set => rowSpriteClick = value; }
        public Sprite RowSpriteGacha { get => rowSpriteGacha; set => rowSpriteGacha = value; }
        public Sprite RowSpriteEconomy { get => rowSpriteEconomy; set => rowSpriteEconomy = value; }
        public Sprite RowSpriteSpecial { get => rowSpriteSpecial; set => rowSpriteSpecial = value; }

        // Shard Exchange UI References
        private Button buyNBtn, buyRBtn, buySRBtn, buySSRBtn;
        private TMP_Text buyNText, buyRText, buySRText, buySSRText;

        // Products UI References
        private readonly List<GameObject> productSlotGOs = new List<GameObject>();
        private Button prevProductPageBtn;
        private Button nextProductPageBtn;
        private Component productPageText;

        private Component selectedProductNameText;
        private Component selectedProductDescText;
        private Button buyProductBtn;
        private Component buyProductBtnText;

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
        private static readonly Color TabNormal   = new Color(0.22f, 0.22f, 0.24f, 1.00f); // Neutral Dark Gray (No Blue)
        private static readonly Color TabActiveC  = new Color(0.04f, 0.04f, 0.05f, 1.00f); // Pressed Black
        private static readonly Color Indicator   = new Color(0.40f, 0.70f, 1.00f, 1.00f);
        private static readonly Color BtnBuy      = new Color(0.18f, 0.26f, 0.44f, 1.00f);
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
            transform.SetAsLastSibling();
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

        private static void SetTextComponent(Component comp, string text, Color? color = null, float? fontSize = null)
        {
            if (comp == null) return;
            if (comp is TMP_Text tmp)
            {
                tmp.text = text;
                if (color.HasValue) tmp.color = color.Value;
                if (fontSize.HasValue) tmp.fontSize = fontSize.Value;
            }
            else if (comp is UnityEngine.UI.Text leg)
            {
                leg.text = text;
                if (color.HasValue) leg.color = color.Value;
                if (fontSize.HasValue) leg.fontSize = Mathf.RoundToInt(fontSize.Value);
            }
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
                case CardRarity.H: return spriteFrameH != null ? spriteFrameH : (spriteFrameSSR != null ? spriteFrameSSR : spriteFrameN);
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
                case CardRarity.H: return spriteMarkH != null ? spriteMarkH : (spriteMarkSSR != null ? spriteMarkSSR : spriteMarkN);
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
            productCatalog.Clear();

            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null && gm.CardCatalog != null && gm.CardCatalog.Cards != null)
            {
                foreach (var card in gm.CardCatalog.Cards)
                {
                    if (card != null && card.IsShop)
                    {
                        ProductCurrencyType cur = (card.ShopCurrency == CardShopCurrency.Shard) 
                            ? ProductCurrencyType.Shard : ProductCurrencyType.Coin;
                        
                        double p = card.ShopPrice > 0 ? card.ShopPrice : (cur == ProductCurrencyType.Coin ? 1000 : 100);

                        Sprite iconSp = card.CardSprite;
                        if (iconSp == null && gm != null)
                        {
                            iconSp = gm.GetCardSpriteForDisplay(card.Id);
                        }

                        string nameStr = !string.IsNullOrEmpty(card.DisplayName) ? card.DisplayName : card.Id;
                        string descStr = !string.IsNullOrEmpty(card.Description) ? card.Description : $"상점 전용 {card.Rarity} 등급 카드입니다.";

                        productCatalog.Add(new ShopProductItem
                        {
                            id = "prod-card-" + card.Id,
                            displayName = nameStr,
                            description = descStr,
                            currencyType = cur,
                            price = p,
                            productType = ShopProductType.Card,
                            targetId = card.Id,
                            rarity = card.Rarity,
                            iconSprite = iconSp
                        });
                    }
                }
            }

            if (selectedProduct != null)
            {
                var match = productCatalog.Find(p => p.id == selectedProduct.id);
                if (match != null)
                {
                    selectedProduct = match;
                }
                else
                {
                    selectedProduct = productCatalog.Count > 0 ? productCatalog[0] : null;
                }
            }
            else
            {
                selectedProduct = productCatalog.Count > 0 ? productCatalog[0] : null;
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

            var titleLbl = MakeLabel(inner.transform, "상 점", new Vector2(0, 249), new Vector2(220, 40), 22, Color.white, FontStyles.Bold);
            (titleLbl.GetComponent<LocalizeText>() ?? titleLbl.gameObject.AddComponent<LocalizeText>()).Key = "shop_title";

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
            string[] tabKeys = { "shop_tab_upgrades", "shop_tab_shard_exchange", "shop_tab_products" };
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
                var tLbl = MakeLabel(tBtn, tabLabels[i], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                (tLbl.GetComponent<LocalizeText>() ?? tLbl.gameObject.AddComponent<LocalizeText>()).Key = tabKeys[i];

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

        private Sprite GetHdrSprite(UpgradeCategory cat)
        {
            switch (cat)
            {
                case UpgradeCategory.Click: return secHdrSpriteClick;
                case UpgradeCategory.Gacha: return secHdrSpriteGacha;
                case UpgradeCategory.Economy: return secHdrSpriteEconomy;
                default: return secHdrSpriteClick;
            }
        }

        private Sprite GetRowSprite(UpgradeCategory cat)
        {
            switch (cat)
            {
                case UpgradeCategory.Click: return rowSpriteClick;
                case UpgradeCategory.Gacha: return rowSpriteGacha;
                case UpgradeCategory.Economy: return rowSpriteEconomy;
                default: return rowSpriteClick;
            }
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
            if (upgradeScrollContent == null) return;

            upgradeRows.Clear();

            Transform templateRow = upgradeScrollContent.Find("Row_upg-crit-chance");

            var cats  = new[] { UpgradeCategory.Click, UpgradeCategory.Gacha, UpgradeCategory.Economy };
            var cNames = new[] { "클릭 계열", "가챠 계열", "경제 계열" };
            var cCols  = new[] { CatClick, CatGacha, CatEcon };

            foreach (var cat in cats)
            {
                int ci = System.Array.IndexOf(cats, cat);
                bool anyInCat = false;
                foreach (var u in catalog.Upgrades) if (u != null && u.Category == cat) { anyInCat = true; break; }
                if (!anyInCat) continue;

                // Bind or search existing Section Header
                Transform hdr = upgradeScrollContent.Find("SecHdr_" + cat);
                if (hdr == null)
                {
                    // Search if user named it SecHdr_name or similar
                    hdr = upgradeScrollContent.Find("SecHdr_name");
                }

                if (hdr == null)
                {
                    var hdrGO = new GameObject("SecHdr_" + cat, typeof(RectTransform));
                    hdrGO.transform.SetParent(upgradeScrollContent, false);
                    hdr = hdrGO.transform;
                    var hdrRT = hdr.GetComponent<RectTransform>();
                    hdrRT.sizeDelta = new Vector2(0, 32);

                    var hdrTxt = MakeLabel(hdr, cNames[ci], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                    hdrTxt.alignment = TextAlignmentOptions.Left; hdrTxt.margin = new Vector4(10, 0, 0, 0);
                }

                var hdrImg = hdr.GetComponent<Image>();
                if (hdrImg == null) hdrImg = hdr.gameObject.AddComponent<Image>();

                Sprite sHdrSprite = GetHdrSprite(cat);
                if (sHdrSprite != null)
                {
                    hdrImg.sprite = sHdrSprite;
                    hdrImg.color = Color.white;
                }
                else
                {
                    hdrImg.color = cCols[ci] * new Color(1, 1, 1, 0.45f);
                }

                hdr.SetAsLastSibling();

                foreach (var upg in catalog.Upgrades)
                {
                    if (upg == null || upg.Category != cat) continue;

                    bool visible = true;
                    if (upg.UpgradeId == "upg-normal-prob-2" && gm.GetUpgradeLevel("upg-normal-prob-1") < 5) visible = false;
                    if (upg.UpgradeId == "upg-rare-prob-2" && gm.GetUpgradeLevel("upg-rare-prob-1") < 5) visible = false;
                    if (upg.UpgradeId == "upg-super-prob-2" && gm.GetUpgradeLevel("upg-super-prob-1") < 5) visible = false;

                    // Reuse existing row if present in hierarchy (DO NOT Destroy user's pre-designed rows!)
                    Transform row = upgradeScrollContent.Find("Row_" + upg.UpgradeId);

                    if (row == null)
                    {
                        if (templateRow != null)
                        {
                            var rowGO = Instantiate(templateRow.gameObject, upgradeScrollContent, false);
                            rowGO.name = "Row_" + upg.UpgradeId;
                            row = rowGO.transform;
                        }
                        else
                        {
                            var rowGO = new GameObject("Row_" + upg.UpgradeId, typeof(RectTransform));
                            rowGO.transform.SetParent(upgradeScrollContent, false);
                            row = rowGO.transform;
                        }
                    }

                    row.SetAsLastSibling();

                    row.gameObject.SetActive(visible);
                    if (!visible) continue;

                    // Apply category row background sprite if assigned
                    var rImg = row.GetComponent<Image>();
                    if (rImg != null)
                    {
                        Sprite sRowSprite = GetRowSprite(cat);
                        if (sRowSprite != null)
                        {
                            rImg.sprite = sRowSprite;
                            rImg.color = Color.white;
                        }
                    }

                    // Apply accent side bar color if present
                    var acc = row.Find("Acc");
                    if (acc != null)
                    {
                        var accImg = acc.GetComponent<Image>();
                        if (accImg != null) accImg.color = cCols[ci];
                    }

                    // Title & Description text update
                    var labels = row.GetComponentsInChildren<TMP_Text>();
                    if (labels != null && labels.Length >= 2)
                    {
                        labels[0].text = upg.DisplayName;
                        labels[1].text = upg.Description + GetUpgradeValuesString(upg);
                    }

                    // UpgradeRowInfo binding
                    var info = row.GetComponent<UpgradeRowInfo>();
                    if (info == null) info = row.gameObject.AddComponent<UpgradeRowInfo>();
                    info.UpgradeId = upg.UpgradeId;

                    var lvBadge = row.Find("LvBadge");
                    if (lvBadge != null) info.LevelText = lvBadge.GetComponentInChildren<TMP_Text>();

                    var buyBtn = row.Find("BuyBtn");
                    if (buyBtn != null)
                    {
                        var btn = buyBtn.GetComponent<Button>();
                        info.BuyButton = btn;
                        info.CostText = buyBtn.GetComponentInChildren<TMP_Text>();
                        info.BgImage = buyBtn.GetComponent<Image>();
                        info.SymbolImage = buyBtn.Find("Symbol")?.GetComponent<Image>()
                                        ?? buyBtn.Find("CoinSymbol")?.GetComponent<Image>()
                                        ?? buyBtn.Find("ShardSymbol")?.GetComponent<Image>();

                        if (buyBtn.GetComponent<UIHoverClickEffect>() == null)
                            buyBtn.gameObject.AddComponent<UIHoverClickEffect>();

                        if (btn != null)
                        {
                            btn.onClick.RemoveAllListeners();
                            string uid = upg.UpgradeId;
                            btn.onClick.AddListener(() => gm.BuyUpgrade(uid));
                        }
                    }

                    upgradeRows.Add(row.gameObject);
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Generate Upgrade Rows In Scene Hierarchy")]
        public void GenerateUpgradeRowsInScene()
        {
            var catalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalogSO>("Assets/ScriptableObjects/UpgradeCatalog.asset");
            if (catalog == null && gm != null) catalog = gm.UpgradeCatalog;
            if (catalog == null)
            {
                Debug.LogError("[ShopPanel] UpgradeCatalog.asset을 찾을 수 없습니다.");
                return;
            }

            if (upgradeScrollContent == null && upgradesContent != null)
            {
                upgradeScrollContent = upgradesContent.transform.Find("Scroll/Viewport/Content");
            }
            if (upgradeScrollContent == null)
            {
                Debug.LogError("[ShopPanel] upgradeScrollContent (Scroll/Viewport/Content)를 찾을 수 없습니다.");
                return;
            }

            Transform templateRow = upgradeScrollContent.Find("Row_upg-crit-chance");
            if (templateRow == null)
            {
                foreach (Transform child in upgradeScrollContent)
                {
                    if (child.name.StartsWith("Row_"))
                    {
                        templateRow = child;
                        break;
                    }
                }
            }

            if (templateRow == null)
            {
                Debug.LogError("[ShopPanel] 복제 템플릿으로 사용할 Row (예: Row_upg-crit-chance)를 upgradeScrollContent 안에서 찾을 수 없습니다.");
                return;
            }

            var cats = new[] { UpgradeCategory.Click, UpgradeCategory.Gacha, UpgradeCategory.Economy };
            var cNames = new[] { "클릭 계열", "가챠 계열", "경제 계열" };
            var cCols = new[] { CatClick, CatGacha, CatEcon };

            UnityEditor.Undo.RegisterFullObjectHierarchyUndo(upgradeScrollContent.gameObject, "Generate Upgrade Rows");

            foreach (var cat in cats)
            {
                int ci = System.Array.IndexOf(cats, cat);
                bool anyInCat = false;
                foreach (var u in catalog.Upgrades) if (u != null && u.Category == cat) { anyInCat = true; break; }
                if (!anyInCat) continue;

                // Bind or create Section Header
                Transform hdr = upgradeScrollContent.Find("SecHdr_" + cat);
                if (hdr == null) hdr = upgradeScrollContent.Find("SecHdr_name");
                if (hdr == null)
                {
                    var hdrGO = new GameObject("SecHdr_" + cat, typeof(RectTransform));
                    hdrGO.transform.SetParent(upgradeScrollContent, false);
                    hdr = hdrGO.transform;
                    var hdrRT = hdr.GetComponent<RectTransform>();
                    hdrRT.sizeDelta = new Vector2(0, 32);

                    var hdrTxt = MakeLabel(hdr, cNames[ci], Vector2.zero, Vector2.zero, 13, Color.white, FontStyles.Bold);
                    hdrTxt.alignment = TextAlignmentOptions.Left; hdrTxt.margin = new Vector4(10, 0, 0, 0);
                    hdrGO.AddComponent<Image>().color = cCols[ci] * new Color(1, 1, 1, 0.45f);
                }
                else
                {
                    hdr.name = "SecHdr_" + cat;
                }

                var hdrImg = hdr.GetComponent<Image>();
                if (hdrImg == null) hdrImg = hdr.gameObject.AddComponent<Image>();

                Sprite sHdrSprite = GetHdrSprite(cat);
                if (sHdrSprite != null)
                {
                    hdrImg.sprite = sHdrSprite;
                    hdrImg.color = Color.white;
                }
                else
                {
                    hdrImg.color = cCols[ci] * new Color(1, 1, 1, 0.45f);
                }

                hdr.SetAsLastSibling();

                foreach (var upg in catalog.Upgrades)
                {
                    if (upg == null || upg.Category != cat) continue;

                    Transform row = upgradeScrollContent.Find("Row_" + upg.UpgradeId);
                    if (row != null && row != templateRow)
                    {
                        // Replace outdated existing row with templateRow clone so Acc removal, size, and sub-object structure match templateRow 100%!
                        UnityEditor.Undo.DestroyObjectImmediate(row.gameObject);
                        row = null;
                    }

                    if (row == null)
                    {
                        var rowGO = Instantiate(templateRow.gameObject, upgradeScrollContent, false);
                        rowGO.name = "Row_" + upg.UpgradeId;
                        row = rowGO.transform;
                        UnityEditor.Undo.RegisterCreatedObjectUndo(rowGO, "Create Upgrade Row");
                    }

                    row.SetAsLastSibling();

                    // Apply category row background sprite directly in Scene
                    var rImg = row.GetComponent<Image>();
                    if (rImg != null)
                    {
                        Sprite sRowSprite = GetRowSprite(cat);
                        if (sRowSprite != null)
                        {
                            rImg.sprite = sRowSprite;
                            rImg.color = Color.white;
                        }
                    }

                    // Apply accent bar color if present
                    var acc = row.Find("Acc");
                    if (acc != null)
                    {
                        var accImg = acc.GetComponent<Image>();
                        if (accImg != null) accImg.color = cCols[ci];
                    }

                    // Update BuyBtn Symbol Image (Coin vs Shard)
                    var buyBtnTrans = row.Find("BuyBtn");
                    if (buyBtnTrans != null)
                    {
                        var symbolImg = buyBtnTrans.Find("Symbol")?.GetComponent<Image>()
                                     ?? buyBtnTrans.Find("CoinSymbol")?.GetComponent<Image>()
                                     ?? buyBtnTrans.Find("ShardSymbol")?.GetComponent<Image>();
                        if (symbolImg != null)
                        {
                            bool isShard = upg.UpgradeId.StartsWith("upg-unlock-");
                            Sprite curSprite = isShard ? shardSymbolSprite : coinSymbolSprite;
                            if (curSprite != null) symbolImg.sprite = curSprite;
                        }
                    }

                    // Update Labels with DisplayName and Description
                    var labels = row.GetComponentsInChildren<TMP_Text>(true);
                    if (labels != null && labels.Length >= 1)
                    {
                        labels[0].text = upg.DisplayName;
                    }
                    if (labels != null && labels.Length >= 2)
                    {
                        labels[1].text = upg.Description;
                    }
                }
            }

            UnityEditor.EditorUtility.SetDirty(upgradeScrollContent.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(upgradeScrollContent.gameObject.scene);
            Debug.Log("[ShopPanel] ✅ 씬(Scene) 뷰 계층 구조에 모든 업그레이드 Row 배치가 완료되었습니다!");
        }
#endif

        private void BuildShardTab()
        {
            var descLbl = MakeLabel(shardContent.transform, "조각으로 미획득 카드를 교환 획득합니다",
                new Vector2(0, 145), new Vector2(500,26), 15, new Color(0.75f,0.80f,0.90f));
            (descLbl.GetComponent<LocalizeText>() ?? descLbl.gameObject.AddComponent<LocalizeText>()).Key = "shop_shard_desc";

            MakeLabel(shardContent.transform, "", new Vector2(0, 120), new Vector2(500,20), 11, new Color(0.50f,0.55f,0.65f));

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

            var lbl = MakeLabel(cardIn, label, new Vector2(0,72), new Vector2(145,26), 14, col, FontStyles.Bold);
            (lbl.GetComponent<LocalizeText>() ?? lbl.gameObject.AddComponent<LocalizeText>()).Key = "shop_shard_tab_" + rarity.ToString().ToLower();

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

            // 1. Bind Pagination Controls First
            if (prevProductPageBtn == null)
            {
                var b = productsContent.transform.Find("Btn_◀") 
                     ?? productsContent.transform.Find("PrevBtn") 
                     ?? productsContent.transform.Find("Btn_Prev")
                     ?? productsContent.transform.Find("LeftBtn")
                     ?? productsContent.transform.Find("Btn_Left");
                if (b != null) prevProductPageBtn = b.GetComponent<Button>();
                else
                {
                    foreach (var btn in productsContent.GetComponentsInChildren<Button>(true))
                    {
                        string txt = btn.GetComponentInChildren<TMP_Text>()?.text ?? "";
                        string n = btn.name.ToLower();
                        if (n.Contains("prev") || n.Contains("left") || txt.Contains("◀") || txt.Contains("<"))
                        {
                            prevProductPageBtn = btn;
                            break;
                        }
                    }
                }
            }

            if (nextProductPageBtn == null)
            {
                var b = productsContent.transform.Find("Btn_▶") 
                     ?? productsContent.transform.Find("NextBtn") 
                     ?? productsContent.transform.Find("Btn_Next")
                     ?? productsContent.transform.Find("RightBtn")
                     ?? productsContent.transform.Find("Btn_Right");
                if (b != null) nextProductPageBtn = b.GetComponent<Button>();
                else
                {
                    foreach (var btn in productsContent.GetComponentsInChildren<Button>(true))
                    {
                        if (btn == prevProductPageBtn) continue;
                        string txt = btn.GetComponentInChildren<TMP_Text>()?.text ?? "";
                        string n = btn.name.ToLower();
                        if (n.Contains("next") || n.Contains("right") || txt.Contains("▶") || txt.Contains(">"))
                        {
                            nextProductPageBtn = btn;
                            break;
                        }
                    }
                }
            }

            if (productPageText == null)
            {
                var t = productsContent.transform.Find("ProductPageText") 
                     ?? productsContent.transform.Find("PageText")
                     ?? productsContent.transform.Find("Text_Page")
                     ?? productsContent.transform.Find("Page_Text")
                     ?? productsContent.transform.Find("Page");
                if (t != null) productPageText = (Component)t.GetComponent<TMP_Text>() ?? (Component)t.GetComponent<UnityEngine.UI.Text>() ?? (Component)t.GetComponentInChildren<TMP_Text>() ?? (Component)t.GetComponentInChildren<UnityEngine.UI.Text>();
                else
                {
                    foreach (var tmp in productsContent.GetComponentsInChildren<Graphic>(true))
                    {
                        if (!(tmp is TMP_Text || tmp is UnityEngine.UI.Text)) continue;
                        string n = tmp.name.ToLower();
                        string val = tmp is TMP_Text t1 ? t1.text : ((UnityEngine.UI.Text)tmp).text;
                        if (n.Contains("page") || val.Contains("/"))
                        {
                            productPageText = tmp;
                            break;
                        }
                    }
                }
            }

            // 2. Bind Buy Panel & Buy Button
            var buyPanelTrans = productsContent.transform.Find("BuyPanel") 
                             ?? productsContent.transform.Find("BottomPanel")
                             ?? productsContent.transform.Find("DetailPanel");

            if (buyProductBtn == null)
            {
                Transform buyBtnTrans = null;

                if (buyPanelTrans != null)
                {
                    buyBtnTrans = buyPanelTrans.Find("But btn")
                               ?? buyPanelTrans.Find("Btn_구매하기") 
                               ?? buyPanelTrans.Find("BuyButton") 
                               ?? buyPanelTrans.Find("BuyBtn")
                               ?? buyPanelTrans.Find("Buy_Btn")
                               ?? buyPanelTrans.Find("PurchaseBtn");
                    if (buyBtnTrans == null)
                    {
                        foreach (Transform child in buyPanelTrans)
                        {
                            string n = child.name.ToLower();
                            if (n.Contains("buy") || n.Contains("구매") || n.Contains("purchase") || n.Contains("btn") || n.Contains("but"))
                            {
                                buyBtnTrans = child;
                                break;
                            }
                        }
                    }
                }

                if (buyBtnTrans == null)
                {
                    foreach (Transform child in productsContent.GetComponentsInChildren<Transform>(true))
                    {
                        if (prevProductPageBtn != null && child == prevProductPageBtn.transform) continue;
                        if (nextProductPageBtn != null && child == nextProductPageBtn.transform) continue;

                        string n = child.name.ToLower();
                        string txt = (child.GetComponentInChildren<TMP_Text>(true)?.text ?? child.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text ?? "").Trim().ToLower();

                        if (n.Contains("buy") || n.Contains("구매") || n.Contains("purchase") || txt.Contains("구매하기") || txt.Contains("buy"))
                        {
                            buyBtnTrans = child;
                            break;
                        }
                    }
                }

                if (buyBtnTrans != null)
                {
                    var img = buyBtnTrans.GetComponent<Image>();
                    if (img == null) img = buyBtnTrans.gameObject.AddComponent<Image>();
                    if (img.color.a < 0.02f) img.color = new Color(0.18f, 0.26f, 0.44f, 1f);
                    img.raycastTarget = true;

                    buyProductBtn = buyBtnTrans.GetComponent<Button>();
                    if (buyProductBtn == null) buyProductBtn = buyBtnTrans.gameObject.AddComponent<Button>();
                    if (buyProductBtn.targetGraphic == null) buyProductBtn.targetGraphic = img;

                    // Ensure child graphics don't block button clicks
                    foreach (var childGraphic in buyBtnTrans.GetComponentsInChildren<Graphic>(true))
                    {
                        if (childGraphic != img) childGraphic.raycastTarget = false;
                    }

                    Debug.Log($"[ShopPanel] buyProductBtn successfully bound/created on: {buyBtnTrans.name}");
                }
            }

            if (buyProductBtn != null && buyProductBtnText == null)
            {
                buyProductBtnText = (Component)buyProductBtn.GetComponentInChildren<TMP_Text>(true)
                                 ?? (Component)buyProductBtn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            }

            if (selectedProductNameText == null)
            {
                if (buyPanelTrans != null)
                {
                    var t = buyPanelTrans.Find("InfoBox/NameText") ?? buyPanelTrans.Find("InfoBox/Label") ?? buyPanelTrans.Find("NameText");
                    if (t != null)
                    {
                        selectedProductNameText = (Component)t.GetComponent<TMP_Text>() ?? (Component)t.GetComponent<UnityEngine.UI.Text>();
                    }
                }
                if (selectedProductNameText == null)
                {
                    foreach (var txt in productsContent.GetComponentsInChildren<Graphic>(true))
                    {
                        if (!(txt is TMP_Text || txt is UnityEngine.UI.Text)) continue;
                        if (txt.transform == productPageText?.transform || (buyProductBtn != null && txt.transform.IsChildOf(buyProductBtn.transform))) continue;
                        string n = txt.name.ToLower();
                        if (n.Contains("nametext") || n.Contains("productname") || (n.Contains("name") && !n.Contains("page")))
                        {
                            selectedProductNameText = txt;
                            break;
                        }
                    }
                }
            }

            if (selectedProductDescText == null)
            {
                if (buyPanelTrans != null)
                {
                    var t = buyPanelTrans.Find("InfoBox/DescText") ?? buyPanelTrans.Find("InfoBox/Description") ?? buyPanelTrans.Find("DescText");
                    if (t != null)
                    {
                        selectedProductDescText = (Component)t.GetComponent<TMP_Text>() ?? (Component)t.GetComponent<UnityEngine.UI.Text>();
                    }
                }
                if (selectedProductDescText == null)
                {
                    foreach (var txt in productsContent.GetComponentsInChildren<Graphic>(true))
                    {
                        if (!(txt is TMP_Text || txt is UnityEngine.UI.Text)) continue;
                        if (txt.transform == productPageText?.transform || txt.transform == selectedProductNameText?.transform || (buyProductBtn != null && txt.transform.IsChildOf(buyProductBtn.transform))) continue;
                        string n = txt.name.ToLower();
                        if (n.Contains("desctext") || n.Contains("description") || n.Contains("desc") || n.Contains("info"))
                        {
                            selectedProductDescText = txt;
                            break;
                        }
                    }
                }
            }

            // 3. Recursive Deep Scan for Slots across all subtrees under productsContent
            var slotsFound = new List<GameObject>();

            void FindSlotsRecursive(Transform current)
            {
                foreach (Transform child in current)
                {
                    if (prevProductPageBtn != null && (child == prevProductPageBtn.transform || child.gameObject == prevProductPageBtn.gameObject)) continue;
                    if (nextProductPageBtn != null && (child == nextProductPageBtn.transform || child.gameObject == nextProductPageBtn.gameObject)) continue;
                    if (buyProductBtn != null && (child == buyProductBtn.transform || child.gameObject == buyProductBtn.gameObject)) continue;
                    if (buyPanelTrans != null && (child == buyPanelTrans || child.gameObject == buyPanelTrans.gameObject)) continue;

                    string n = child.name.ToLower();
                    if (n.Contains("page") || n.Contains("title") || n.Contains("header") || n == "infobox" || n == "buypanel" || n == "bottompanel") continue;

                    bool isSlot = n.Contains("slot") || n.Contains("card_") || n.StartsWith("card") || n.StartsWith("item") || n.StartsWith("prod");

                    if (isSlot)
                    {
                        slotsFound.Add(child.gameObject);
                    }
                    else
                    {
                        FindSlotsRecursive(child);
                    }
                }
            }

            FindSlotsRecursive(productsContent.transform);

            if (slotsFound.Count == 0)
            {
                foreach (var t in productsContent.GetComponentsInChildren<Transform>(true))
                {
                    string n = t.name.ToLower();
                    if (n == "content" || n.Contains("grid") || n.Contains("slots") || n.Contains("goods"))
                    {
                        foreach (Transform child in t)
                        {
                            if (child.name != "Viewport" && child.name != "Scrollbar" && !child.name.Contains("Page") && !child.name.Contains("Btn"))
                            {
                                slotsFound.Add(child.gameObject);
                            }
                        }
                        if (slotsFound.Count > 0) break;
                    }
                }
            }

            productSlotGOs.Clear();
            productSlotGOs.AddRange(slotsFound);
            if (productSlotGOs.Count == 0) return false;

            // Wire slot click listeners on slotGO
            for (int i = 0; i < productSlotGOs.Count; i++)
            {
                var slotGO = productSlotGOs[i];
                if (slotGO == null) continue;

                int idx = i;

                // Ensure slotGO root itself has an Image raycast target
                var slotImg = slotGO.GetComponent<Image>();
                if (slotImg == null)
                {
                    var frameImg = slotGO.transform.Find("Frame")?.GetComponent<Image>()
                                ?? slotGO.transform.Find("Frame_Image")?.GetComponent<Image>();
                    slotImg = frameImg ?? slotGO.AddComponent<Image>();
                }
                if (slotImg != null)
                {
                    if (slotImg.color.a < 0.01f) slotImg.color = new Color(0f, 0f, 0f, 0.01f);
                    slotImg.raycastTarget = true;
                }

                // Bind ALL Button components on slotGO and its children!
                var buttonsInSlot = slotGO.GetComponentsInChildren<Button>(true);
                if (buttonsInSlot.Length == 0)
                {
                    var newBtn = slotGO.AddComponent<Button>();
                    newBtn.targetGraphic = slotImg;
                    buttonsInSlot = new[] { newBtn };
                }

                foreach (var b in buttonsInSlot)
                {
                    b.interactable = true;
                    if (b.targetGraphic != null) b.targetGraphic.raycastTarget = true;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => {
                        Debug.Log($"[ShopPanel] Slot Button {idx} Clicked on {b.name}!");
                        OnProductSlotClicked(idx);
                    });
                }

                // Ensure raycastTarget = false on non-button child graphics so clicks cleanly land on the slot button
                foreach (var graphic in slotGO.GetComponentsInChildren<Graphic>(true))
                {
                    if (graphic != slotImg && graphic.GetComponent<Button>() == null)
                    {
                        graphic.raycastTarget = false;
                    }
                }
            }

            // Wire pagination & buy button listeners
            if (prevProductPageBtn != null)
            {
                prevProductPageBtn.onClick.RemoveAllListeners();
                prevProductPageBtn.onClick.AddListener(OnPrevProductPageClicked);
                prevProductPageBtn.gameObject.SetActive(true);
            }
            if (nextProductPageBtn != null)
            {
                nextProductPageBtn.onClick.RemoveAllListeners();
                nextProductPageBtn.onClick.AddListener(OnNextProductPageClicked);
                nextProductPageBtn.gameObject.SetActive(true);
            }
            if (buyProductBtn != null)
            {
                buyProductBtn.onClick.RemoveAllListeners();
                buyProductBtn.onClick.AddListener(OnBuySelectedProductClicked);
                buyProductBtn.gameObject.SetActive(true);
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
                iconImg.raycastTarget = false;

                // rereMark Rarity Mark Image
                var markGO = MakeRT("rereMark", slotGO, new Vector2(-48, 30), new Vector2(24, 24));
                var markImg = markGO.gameObject.AddComponent<Image>();
                markImg.color = Color.white;
                markImg.raycastTarget = false;

                // Product Name Text
                var nameTxt = MakeLabel(slotGO, "---", new Vector2(0, -18), new Vector2(120, 20), 10, Color.white, FontStyles.Bold);
                nameTxt.alignment = TextAlignmentOptions.Center;
                nameTxt.raycastTarget = false;

                // Price Symbol & Value Text
                var priceTxt = MakeLabel(slotGO, "0", new Vector2(0, -34), new Vector2(120, 20), 10, GoldColor, FontStyles.Bold);
                priceTxt.alignment = TextAlignmentOptions.Center;
                priceTxt.raycastTarget = false;

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
            var nameLabel = MakeLabel(infoBox, "상품 선택", new Vector2(0, 15), new Vector2(350, 24), 13, GoldColor, FontStyles.Bold);
            nameLabel.alignment = TextAlignmentOptions.Left;
            selectedProductNameText = nameLabel;

            var descLabel = MakeLabel(infoBox, "슬롯을 클릭하여 원하는 상품을 구매하세요.", new Vector2(0, -10), new Vector2(350, 30), 10, new Color(0.7f, 0.75f, 0.85f));
            descLabel.alignment = TextAlignmentOptions.Left;
            selectedProductDescText = descLabel;

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
            Debug.Log($"[ShopPanel] OnBuySelectedProductClicked triggered. selectedProduct={(selectedProduct != null ? selectedProduct.displayName : "null")}");
            if (selectedProduct == null || gm == null)
            {
                Debug.LogWarning("[ShopPanel] Buy failed: selectedProduct or gm is null.");
                return;
            }

            string status = GetProductStatus(selectedProduct);
            if (status == "MaxBreakthrough")
            {
                Debug.LogWarning("[ShopPanel] Buy failed: Already at Max Breakthrough.");
                return;
            }

            bool afford = selectedProduct.currencyType == ProductCurrencyType.Coin ?
                (gm.Money >= selectedProduct.price) : (gm.Shards >= selectedProduct.price);

            if (!afford)
            {
                Debug.LogWarning($"[ShopPanel] Buy failed: Not enough currency. Money={gm.Money}, Shards={gm.Shards}, Price={selectedProduct.price}");
                return;
            }

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
                // Do NOT auto-equip on buy to preserve user's equipped card preference!
            }

            gm.SaveGame();
            gm.NotifyStateChange();
            Refresh();
            Debug.Log($"[ShopPanel] Purchase SUCCESS! Product={selectedProduct.displayName}");
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
                if (states.TryGetValue(prod.targetId, out var st) && st != null)
                {
                    if (st.Copies >= 5) return "MaxBreakthrough";
                    if (st.Unlocked || st.Copies > 0)
                    {
                        if (gm.EquippedCardId == prod.targetId) return "Equipped";
                        return "Purchased";
                    }
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

            // Update Tab Button selection state (Active = Darkened Pressed Feel + Sunk Scale, Inactive = Full Normal Color)
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                string t = (btn.GetComponentInChildren<TMP_Text>(true)?.text ?? btn.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text ?? "").Trim().ToLower();
                string n = btn.name.ToLower();

                int tabIdx = -1;
                if (t == "업그레이드" || n.Contains("tab0") || n.Contains("upgrade")) tabIdx = 0;
                else if (t == "조각 교환" || t == "조각교환" || n.Contains("tab1") || n.Contains("shard")) tabIdx = 1;
                else if (t == "상품" || n.Contains("tab2") || n.Contains("product") || n.Contains("goods")) tabIdx = 2;

                if (tabIdx >= 0)
                {
                    bool isTabActive = (tabIdx == index);
                    
                    // Disable interactable on currently open tab to prevent re-click and trigger UI pressed state
                    btn.interactable = !isTabActive;

                    var img = btn.GetComponent<Image>();
                    if (img != null)
                    {
                        // Active tab gets pressed black color (#1F1F24), inactive gets 100% natural inspector color
                        img.color = isTabActive ? new Color(0.12f, 0.12f, 0.14f, 1f) : Color.white;
                    }

                    // Text color: Golden text when pressed black, white when unpressed
                    var tmpTxt = btn.GetComponentInChildren<TMP_Text>(true);
                    if (tmpTxt != null) tmpTxt.color = isTabActive ? GoldColor : Color.white;

                    var legTxt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (legTxt != null) legTxt.color = isTabActive ? GoldColor : Color.white;

                    // Sunk scale for pressed tab
                    btn.transform.localScale = isTabActive ? new Vector3(0.95f, 0.95f, 1f) : Vector3.one;

                    var ind = btn.transform.Find("Ind") 
                           ?? btn.transform.Find("Indicator") 
                           ?? btn.transform.Find("Selected") 
                           ?? btn.transform.Find("Active");
                    if (ind != null)
                    {
                        ind.gameObject.SetActive(isTabActive);
                    }
                }
            }
            Refresh();
        }

        // ── Refresh ───────────────────────────────────────────────────────────
        private void Refresh()
        {
            EnsureReferencesResolved();
            if (gm == null) return;
            if (coinText  != null) coinText.text  = GameManager.FormatNumber(gm.Money);
            if (shardText != null) shardText.text = GameManager.FormatNumber(gm.Shards);

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
                    if (info.SymbolImage != null)
                    {
                        info.SymbolImage.gameObject.SetActive(false);
                    }
                    if (info.CostText  != null)
                    {
                        info.CostText.text = "MAX";
                        info.CostText.color = new Color(0.65f, 0.65f, 0.70f, 1f);
                    }
                    if (info.BuyButton != null) info.BuyButton.interactable = false;
                    if (info.BgImage   != null) info.BgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);
                }
                else
                {
                    double cost = (entry.CostPerLevel != null && level < entry.CostPerLevel.Length)
                        ? entry.CostPerLevel[level] : 0;
                    bool isShardUpgrade = entry.UpgradeId.StartsWith("upg-unlock-");
                    bool afford = isShardUpgrade ? (gm.Shards >= (int)cost) : (gm.Money >= cost);

                    if (info.SymbolImage != null)
                    {
                        Sprite curSprite = isShardUpgrade ? shardSymbolSprite : coinSymbolSprite;
                        if (curSprite != null)
                        {
                            info.SymbolImage.sprite = curSprite;
                            info.SymbolImage.gameObject.SetActive(true);
                        }
                    }

                    if (info.CostText != null)
                    {
                        info.CostText.text = GameManager.FormatNumber(cost);
                        info.CostText.color = isShardUpgrade ? ShardColor : GoldColor;
                    }

                    if (info.BuyButton != null) info.BuyButton.interactable = afford;
                    if (info.BgImage   != null) info.BgImage.color = afford ? BtnBuy : BtnDisabled;
                }
            }
        }

        private void RefreshShardButton(CardRarity rarity, Button btn, TMP_Text txt)
        {
            if (gm == null || gm.CardCatalog == null) return;

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

            string displayStr;
            Color baseCol = rarity == CardRarity.SSR ? ColSSR : rarity == CardRarity.SR ? ColSR : rarity == CardRarity.R ? ColR : ColN;
            bool afford = gm.Shards >= currentCost;

            if (candCount == 0)
            {
                displayStr = "MAX";
            }
            else
            {
                displayStr = GameManager.FormatNumber(currentCost);
            }

            void ApplyToButton(Button targetBtn)
            {
                if (targetBtn == null) return;

                targetBtn.interactable = candCount > 0 && afford;

                var childTMPs = targetBtn.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in childTMPs)
                {
                    t.text = displayStr;
                }
                var childTexts = targetBtn.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                foreach (var t in childTexts)
                {
                    t.text = displayStr;
                }
            }

            ApplyToButton(btn);

            if (shardContent != null)
            {
                string rKey = rarity.ToString().ToLower();
                foreach (Transform child in shardContent.GetComponentsInChildren<Transform>(true))
                {
                    string cName = child.name.ToLower();
                    if (cName.EndsWith("_" + rKey) || cName.StartsWith("card_" + rKey) || cName == rKey)
                    {
                        var bList = child.GetComponentsInChildren<Button>(true);
                        foreach (var b in bList)
                        {
                            ApplyToButton(b);
                        }
                    }
                }
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

            if (selectedProduct == null && productCatalog.Count > 0)
            {
                selectedProduct = productCatalog[0];
            }

            int totalProducts = productCatalog.Count;
            int maxPages = Mathf.Max(1, Mathf.CeilToInt(totalProducts / (float)PRODUCTS_PER_PAGE));
            currentProductPage = Mathf.Clamp(currentProductPage, 0, maxPages - 1);

            if (productPageText != null)
            {
                SetTextComponent(productPageText, $"{currentProductPage + 1} / {maxPages}");
                productPageText.gameObject.SetActive(true);
            }
            if (prevProductPageBtn != null)
            {
                prevProductPageBtn.gameObject.SetActive(true);
                prevProductPageBtn.interactable = currentProductPage > 0;
            }
            if (nextProductPageBtn != null)
            {
                nextProductPageBtn.gameObject.SetActive(true);
                nextProductPageBtn.interactable = currentProductPage < maxPages - 1;
            }

            // Render 10 slots with Rarity Frame, Art, rereMark
            for (int i = 0; i < productSlotGOs.Count; i++)
            {
                var slotGO = productSlotGOs[i];
                int itemIdx = currentProductPage * PRODUCTS_PER_PAGE + i;

                if (itemIdx < productCatalog.Count)
                {
                    slotGO.SetActive(true);
                    var prod = productCatalog[itemIdx];
                    bool isSelected = selectedProduct != null && prod != null && (selectedProduct == prod || selectedProduct.id == prod.id);

                    // 1. Rarity Frame Image
                    var slotImg = slotGO.transform.Find("Frame")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("Frame_Image")?.GetComponent<Image>()
                               ?? slotGO.GetComponent<Image>();

                    // 2. Art / Icon Image
                    var iconImg = slotGO.transform.Find("Art")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("Icon")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("Image")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("CardArt")?.GetComponent<Image>();
                    if (iconImg == null)
                    {
                        var imgs = slotGO.GetComponentsInChildren<Image>(true);
                        foreach (var img in imgs)
                        {
                            if (img != slotImg && img != slotGO.transform.Find("rereMark")?.GetComponent<Image>())
                            {
                                iconImg = img;
                                break;
                            }
                        }
                    }
                    if (iconImg != null)
                    {
                        if (prod.iconSprite != null) { iconImg.sprite = prod.iconSprite; iconImg.color = Color.white; iconImg.gameObject.SetActive(true); }
                        else { iconImg.gameObject.SetActive(false); }
                    }

                    // 3. rereMark Image
                    var markImg = slotGO.transform.Find("rereMark")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("rereMark_Image")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("Mark")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("RarityMark")?.GetComponent<Image>()
                               ?? slotGO.transform.Find("RareMark")?.GetComponent<Image>();
                    if (markImg == null)
                    {
                        var imgs = slotGO.GetComponentsInChildren<Image>(true);
                        foreach (var img in imgs)
                        {
                            string n = img.name.ToLower();
                            if (n.Contains("mark") || n.Contains("rere") || n.Contains("rare"))
                            {
                                markImg = img;
                                break;
                            }
                        }
                    }
                    if (markImg != null)
                    {
                        Sprite markSp = GetMarkSpriteForRarity(prod.rarity);
                        if (markSp != null) { markImg.sprite = markSp; markImg.color = Color.white; markImg.gameObject.SetActive(true); }
                        else { markImg.gameObject.SetActive(false); }
                    }

                    // 4 & 5. Name Text & Price Text Resolution
                    Component nameTxt = (Component)slotGO.transform.Find("Label")?.GetComponent<TMP_Text>() 
                                   ?? (Component)slotGO.transform.Find("Name")?.GetComponent<TMP_Text>()
                                   ?? (Component)slotGO.transform.Find("NameText")?.GetComponent<TMP_Text>()
                                   ?? (Component)slotGO.transform.Find("Text_Name")?.GetComponent<TMP_Text>()
                                   ?? (Component)slotGO.transform.Find("Title")?.GetComponent<TMP_Text>()
                                   ?? (Component)slotGO.transform.Find("Label")?.GetComponent<UnityEngine.UI.Text>()
                                   ?? (Component)slotGO.transform.Find("Name")?.GetComponent<UnityEngine.UI.Text>()
                                   ?? (Component)slotGO.transform.Find("NameText")?.GetComponent<UnityEngine.UI.Text>()
                                   ?? (Component)slotGO.transform.Find("Text_Name")?.GetComponent<UnityEngine.UI.Text>()
                                   ?? (Component)slotGO.transform.Find("Title")?.GetComponent<UnityEngine.UI.Text>();

                    Component priceTxt = (Component)slotGO.transform.Find("Price")?.GetComponent<TMP_Text>()
                                    ?? (Component)slotGO.transform.Find("Cost")?.GetComponent<TMP_Text>()
                                    ?? (Component)slotGO.transform.Find("PriceText")?.GetComponent<TMP_Text>()
                                    ?? (Component)slotGO.transform.Find("Text_Price")?.GetComponent<TMP_Text>()
                                    ?? (Component)slotGO.transform.Find("Value")?.GetComponent<TMP_Text>()
                                    ?? (Component)slotGO.transform.Find("Price")?.GetComponent<UnityEngine.UI.Text>()
                                    ?? (Component)slotGO.transform.Find("Cost")?.GetComponent<UnityEngine.UI.Text>()
                                    ?? (Component)slotGO.transform.Find("PriceText")?.GetComponent<UnityEngine.UI.Text>()
                                    ?? (Component)slotGO.transform.Find("Text_Price")?.GetComponent<UnityEngine.UI.Text>()
                                    ?? (Component)slotGO.transform.Find("Value")?.GetComponent<UnityEngine.UI.Text>();

                    var allSlotTMPTexts = slotGO.GetComponentsInChildren<TMP_Text>(true);
                    var allSlotLegTexts = slotGO.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                    
                    if (nameTxt == null || priceTxt == null)
                    {
                        var unassignedTexts = new List<Component>();
                        foreach (var txt in allSlotTMPTexts)
                        {
                            string n = txt.name.ToLower();
                            if (nameTxt == null && (n.Contains("name") || n.Contains("label") || n.Contains("title")))
                                nameTxt = txt;
                            else if (priceTxt == null && (n.Contains("price") || n.Contains("cost") || n.Contains("coin") || n.Contains("shard") || n.Contains("val")))
                                priceTxt = txt;
                            else if (n != "badge" && n != "status" && !n.Contains("mark"))
                                unassignedTexts.Add(txt);
                        }
                        foreach (var txt in allSlotLegTexts)
                        {
                            string n = txt.name.ToLower();
                            if (nameTxt == null && (n.Contains("name") || n.Contains("label") || n.Contains("title")))
                                nameTxt = txt;
                            else if (priceTxt == null && (n.Contains("price") || n.Contains("cost") || n.Contains("coin") || n.Contains("shard") || n.Contains("val")))
                                priceTxt = txt;
                            else if (n != "badge" && n != "status" && !n.Contains("mark"))
                                unassignedTexts.Add(txt);
                        }

                        if (nameTxt == null && unassignedTexts.Count > 0)
                        {
                            nameTxt = unassignedTexts[0];
                            unassignedTexts.RemoveAt(0);
                        }
                        if (priceTxt == null && unassignedTexts.Count > 0)
                        {
                            priceTxt = unassignedTexts[0];
                            unassignedTexts.RemoveAt(0);
                        }
                    }

                    if (nameTxt != null)
                    {
                        SetTextComponent(nameTxt, prod.displayName);
                        nameTxt.gameObject.SetActive(true);
                    }

                    // 5. Price Symbol Image & Value Text
                    var symbolImg = slotGO.transform.Find("Symbol")?.GetComponent<Image>()
                                 ?? slotGO.transform.Find("CoinSymbol")?.GetComponent<Image>()
                                 ?? slotGO.transform.Find("ShardSymbol")?.GetComponent<Image>()
                                 ?? slotGO.transform.Find("CurrencyIcon")?.GetComponent<Image>()
                                 ?? slotGO.transform.Find("SymbolImage")?.GetComponent<Image>()
                                 ?? slotGO.transform.Find("PriceIcon")?.GetComponent<Image>();

                    Sprite curSprite = prod.currencyType == ProductCurrencyType.Coin ? coinSymbolSprite : shardSymbolSprite;
                    if (symbolImg != null)
                    {
                        if (curSprite != null)
                        {
                            symbolImg.sprite = curSprite;
                            symbolImg.color = Color.white;
                            symbolImg.gameObject.SetActive(true);
                        }
                        else
                        {
                            symbolImg.gameObject.SetActive(false);
                        }
                    }

                    if (priceTxt != null)
                    {
                        Color pCol = prod.currencyType == ProductCurrencyType.Coin ? GoldColor : ShardColor;
                        string formattedPrice = GameManager.FormatNumber(prod.price);
                        SetTextComponent(priceTxt, formattedPrice, pCol);
                        priceTxt.gameObject.SetActive(true);
                    }

                    // 6. Selection Highlight Border & Scale Highlight
                    var selBorder = slotGO.transform.Find("SelectedBorder")
                                 ?? slotGO.transform.Find("Selected")
                                 ?? slotGO.transform.Find("Outline")
                                 ?? slotGO.transform.Find("Highlight")
                                 ?? slotGO.transform.Find("Border");
                    if (selBorder != null)
                    {
                        selBorder.gameObject.SetActive(isSelected);
                        var bImg = selBorder.GetComponent<Image>();
                        if (bImg != null) bImg.raycastTarget = false;
                    }

                    if (slotImg != null)
                    {
                        Sprite frameSp = GetFrameSpriteForRarity(prod.rarity);
                        if (frameSp != null)
                        {
                            slotImg.sprite = frameSp;
                            slotImg.color = isSelected ? new Color(1.0f, 0.92f, 0.45f, 1f) : Color.white;
                        }
                        else
                        {
                            slotImg.color = isSelected ? new Color(0.35f, 0.55f, 0.90f, 1f) : new Color(0.10f, 0.14f, 0.22f, 1f);
                        }
                    }

                    slotGO.transform.localScale = isSelected ? new Vector3(1.10f, 1.10f, 1f) : Vector3.one;

                    // 7. Status Badge Text & Breakthrough Limit
                    Component badgeTxt = (Component)slotGO.transform.Find("Badge")?.GetComponent<TMP_Text>()
                                ?? (Component)slotGO.transform.Find("Status")?.GetComponent<TMP_Text>()
                                ?? (Component)slotGO.transform.Find("Badge")?.GetComponent<UnityEngine.UI.Text>()
                                ?? (Component)slotGO.transform.Find("Status")?.GetComponent<UnityEngine.UI.Text>();

                    if (badgeTxt == null)
                    {
                        var badges = slotGO.GetComponentsInChildren<TMP_Text>(true);
                        foreach (var b in badges)
                        {
                            string n = b.name.ToLower();
                            if (n == "badge" || n == "status")
                            {
                                badgeTxt = b;
                                break;
                            }
                        }
                        if (badgeTxt == null)
                        {
                            var legBadges = slotGO.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                            foreach (var b in legBadges)
                            {
                                string n = b.name.ToLower();
                                if (n == "badge" || n == "status")
                                {
                                    badgeTxt = b;
                                    break;
                                }
                            }
                        }
                    }

                    string status = GetProductStatus(prod);
                    if (badgeTxt != null)
                    {
                        if (status == "Equipped") SetTextComponent(badgeTxt, "장착중", Indicator);
                        else if (status == "Purchased") SetTextComponent(badgeTxt, "보유중", GoldColor);
                        else if (status == "MaxBreakthrough") SetTextComponent(badgeTxt, "풀강", ColSSR);
                        else SetTextComponent(badgeTxt, string.Empty);
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
                SetTextComponent(selectedProductNameText, selectedProduct.displayName);
                SetTextComponent(selectedProductDescText, selectedProduct.description);

                string status = GetProductStatus(selectedProduct);
                bool afford = selectedProduct.currencyType == ProductCurrencyType.Coin ?
                    (gm.Money >= selectedProduct.price) : (gm.Shards >= selectedProduct.price);

                if (buyProductBtn != null)
                {
                    int currentCopies = 0;
                    int maxCopies = 5;
                    float buyFontSize = selectedProduct.productType == ShopProductType.Card ? 20f : 40f;

                    if (selectedProduct.productType == ShopProductType.Card)
                    {
                        var states = gm.GetCardStates();
                        if (states.TryGetValue(selectedProduct.targetId, out var st) && st != null)
                        {
                            currentCopies = st.Copies;
                        }
                    }

                    string lang = gm != null ? gm.SelectedLanguage : "KR";
                    bool isEN = lang == "EN";

                    if (status == "MaxBreakthrough")
                    {
                        buyProductBtn.interactable = false;
                        SetTextComponent(buyProductBtnText, isEN ? "Max Breakthrough" : "최대 한계돌파 완료", null, buyFontSize);
                    }
                    else if (status == "Equipped" && selectedProduct.productType != ShopProductType.Card)
                    {
                        buyProductBtn.interactable = false;
                        SetTextComponent(buyProductBtnText, isEN ? "Equipped" : "장착중", null, buyFontSize);
                    }
                    else
                    {
                        buyProductBtn.interactable = afford;
                        string btnTextVal = "";
                        if (selectedProduct.productType == ShopProductType.Card)
                        {
                            if (status == "Purchased")
                            {
                                btnTextVal = isEN ?
                                    (afford ? $"Buy Copy ({currentCopies}/{maxCopies})" : $"Need Coins ({currentCopies}/{maxCopies})") :
                                    (afford ? $"추가 구매 ({currentCopies}/{maxCopies})" : $"소지금 부족 ({currentCopies}/{maxCopies})");
                            }
                            else
                            {
                                btnTextVal = isEN ?
                                    (afford ? $"Buy (Owned {currentCopies}/{maxCopies})" : $"Need Coins ({currentCopies}/{maxCopies})") :
                                    (afford ? $"구매하기 (보유 {currentCopies}/{maxCopies})" : $"소지금 부족 (보유 {currentCopies}/{maxCopies})");
                            }
                        }
                        else
                        {
                            btnTextVal = isEN ? (afford ? "Buy" : "Need Coins") : (afford ? "구매하기" : "소지금 부족");
                        }

                        SetTextComponent(buyProductBtnText, btnTextVal, null, buyFontSize);

                        var cs = buyProductBtn.colors;
                        cs.normalColor = Color.white;
                        cs.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                        cs.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
                        cs.selectedColor = Color.white;
                        cs.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                        cs.colorMultiplier = 1f;
                        cs.fadeDuration = 0.08f;
                        buyProductBtn.colors = cs;
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

            PlayShardPurchaseCutscene(chosen, rarity);
        }

        // ── Shard Purchase Animation Cutscene (AnimContainer) ──────────────────
        private GameObject shardAnimContainer;
        private RectTransform shardCardContainer;
        private RectTransform shardCardRT;
        private GameObject shardCardBack;
        private GameObject shardCardFront;
        private Coroutine activeShardAnim;

        private void EnsureShardAnimContainerBuilt()
        {
            if (shardAnimContainer == null)
            {
                var existingTrans = transform.Find("ShardAnimContainer");
                if (existingTrans != null)
                {
                    shardAnimContainer = existingTrans.gameObject;
                }
                else
                {
                    var rootCanvas = FindObjectOfType<Canvas>();
                    shardAnimContainer = new GameObject("ShardAnimContainer", typeof(RectTransform), typeof(CanvasGroup));
                    if (rootCanvas != null) shardAnimContainer.transform.SetParent(rootCanvas.transform, false);
                    else shardAnimContainer.transform.SetParent(transform, false);

                    var rt = shardAnimContainer.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                    var cg = shardAnimContainer.GetComponent<CanvasGroup>();
                    cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true;

                    // Background Overlay
                    var bgGO = new GameObject("BgOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    bgGO.transform.SetParent(shardAnimContainer.transform, false);
                    var bgRt = bgGO.GetComponent<RectTransform>();
                    bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
                    bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
                    var bgImg = bgGO.GetComponent<Image>();
                    bgImg.color = new Color(0.04f, 0.05f, 0.09f, 0.92f);
                    bgImg.raycastTarget = true;
                }
            }

            // Find or build CardContainer
            var cTrans = shardAnimContainer.transform.Find("CardContainer");
            if (cTrans != null)
            {
                shardCardContainer = cTrans.GetComponent<RectTransform>();
            }
            else
            {
                var cGO = new GameObject("CardContainer", typeof(RectTransform));
                cGO.transform.SetParent(shardAnimContainer.transform, false);
                shardCardContainer = cGO.GetComponent<RectTransform>();
                shardCardContainer.anchorMin = new Vector2(0.5f, 0.5f);
                shardCardContainer.anchorMax = new Vector2(0.5f, 0.5f);
                shardCardContainer.anchoredPosition = Vector2.zero;
            }

            // Find existing CardBase or clone from GachaPanel without overwriting Inspector values
            var cardTrans = shardCardContainer.Find("CardBase") ?? shardCardContainer.Find("Card");
            GameObject cardGo = null;
            if (cardTrans != null)
            {
                cardGo = cardTrans.gameObject;
            }
            else
            {
                var gachaPanel = FindObjectOfType<GachaPanel>(true);
                cardGo = gachaPanel != null ? gachaPanel.CreateCardBase(shardCardContainer) : null;
                if (cardGo == null)
                {
                    cardGo = new GameObject("CardBase", typeof(RectTransform));
                    cardGo.transform.SetParent(shardCardContainer, false);
                }
            }

            shardCardRT = cardGo.GetComponent<RectTransform>();

            // Resolve Back transform & GameObject
            var backTrans = cardGo.transform.Find("Back");
            if (backTrans == null)
            {
                var backGo = new GameObject("Back", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backGo.transform.SetParent(cardGo.transform, false);
                var bRt = backGo.GetComponent<RectTransform>();
                bRt.anchorMin = Vector2.zero; bRt.anchorMax = Vector2.one;
                bRt.offsetMin = Vector2.zero; bRt.offsetMax = Vector2.zero;
                backGo.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.26f, 1f);

                var qTxt = MakeLabel(backGo.transform, "?", Vector2.zero, new Vector2(150, 150), 72, new Color(0.7f, 0.8f, 1f), FontStyles.Bold);
                qTxt.alignment = TextAlignmentOptions.Center;
                backTrans = backGo.transform;
            }
            shardCardBack = backTrans.gameObject;

            // Resolve Front transform & GameObject
            var frontTrans = cardGo.transform.Find("Front");
            if (frontTrans == null)
            {
                var frontGo = new GameObject("Front", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                frontGo.transform.SetParent(cardGo.transform, false);
                var fRt = frontGo.GetComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
                fRt.offsetMin = Vector2.zero; fRt.offsetMax = Vector2.zero;
                frontGo.GetComponent<Image>().color = Color.white;

                var artGO = MakeRT("Art", frontGo.transform, Vector2.zero, Vector2.zero);
                var aRt = artGO.GetComponent<RectTransform>();
                aRt.anchorMin = Vector2.zero; aRt.anchorMax = Vector2.one;
                aRt.offsetMin = new Vector2(10f, 60f); aRt.offsetMax = new Vector2(-10f, -10f);
                artGO.gameObject.AddComponent<Image>();

                var frameGO = MakeRT("Frame", frontGo.transform, Vector2.zero, Vector2.zero);
                frameGO.anchorMin = Vector2.zero; frameGO.anchorMax = Vector2.one;
                frameGO.offsetMin = Vector2.zero; frameGO.offsetMax = Vector2.zero;
                frameGO.gameObject.AddComponent<Image>();

                var rareGO = MakeRT("Rare_Mark", frontGo.transform, Vector2.zero, new Vector2(60, 60));
                rareGO.anchorMin = new Vector2(1, 1); rareGO.anchorMax = new Vector2(1, 1);
                rareGO.anchoredPosition = new Vector2(-35, -35);
                rareGO.gameObject.AddComponent<Image>();

                var nameTxt = MakeLabel(frontGo.transform, "---", new Vector2(0, -210), new Vector2(270, 40), 16, Color.white, FontStyles.Bold);
                nameTxt.alignment = TextAlignmentOptions.Center;
                frontTrans = frontGo.transform;
            }
            shardCardFront = frontTrans.gameObject;

            var canvasObj = FindObjectOfType<Canvas>();
            if (canvasObj != null && shardAnimContainer.transform.parent != canvasObj.transform)
            {
                shardAnimContainer.transform.SetParent(canvasObj.transform, false);
            }

            shardAnimContainer.transform.SetAsLastSibling();
            shardAnimContainer.SetActive(false);
        }

        private void PlayShardPurchaseCutscene(CardEntry chosenCard, CardRarity rarity)
        {
            if (activeShardAnim != null) StopCoroutine(activeShardAnim);
            activeShardAnim = StartCoroutine(RunShardPurchaseCutscene(chosenCard, rarity));
        }

        private System.Collections.IEnumerator RunShardPurchaseCutscene(CardEntry chosenCard, CardRarity rarity)
        {
            EnsureShardAnimContainerBuilt();
            shardAnimContainer.SetActive(true);
            shardAnimContainer.transform.SetAsLastSibling();

            shardCardBack.SetActive(true);
            shardCardFront.SetActive(false);

            Vector3 baseScale = shardCardRT != null ? shardCardRT.localScale : Vector3.one;
            if (shardCardRT != null) shardCardRT.localScale = baseScale;

            // Bind front card data using GachaPanel's Rarity Theme / Frame / Art binding!
            var gachaPanel = FindObjectOfType<GachaPanel>(true);
            if (gachaPanel != null && shardCardFront != null)
            {
                gachaPanel.BindCardFrontData(shardCardFront.transform, chosenCard);
            }
            else if (shardCardFront != null)
            {
                var frontImg = shardCardFront.GetComponent<Image>() ?? shardCardFront.transform.Find("Bg")?.GetComponent<Image>();
                if (frontImg != null && chosenCard.GachaBgSprite != null)
                {
                    frontImg.sprite = chosenCard.GachaBgSprite;
                    frontImg.color = Color.white;
                }
                var artImg = shardCardFront.transform.Find("Art")?.GetComponent<Image>()
                          ?? shardCardFront.transform.Find("CardArt")?.GetComponent<Image>()
                          ?? shardCardFront.transform.Find("Image")?.GetComponent<Image>();
                if (artImg != null)
                {
                    artImg.sprite = chosenCard.CardSprite;
                    artImg.color = chosenCard.CardSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);
                }
            }

            // Set name to "???" during rotation flip loop
            var cardNameTxt = shardCardFront != null ? shardCardFront.GetComponentInChildren<TMP_Text>(true) : null;
            if (cardNameTxt != null) cardNameTxt.text = "???";

            // ── Flip Rotation Animation Loop ───────────────────────────────────
            float t = 0f;
            bool isShowingFront = false;

            if (rarity == CardRarity.N)
            {
                // Normal (N): 3 flips alternating between Back and Front!
                for (int turn = 0; turn < 3; turn++)
                {
                    // Shrink 1.0 -> 0.0
                    t = 0f;
                    while (t < 0.15f)
                    {
                        t += Time.deltaTime;
                        float s = Mathf.Lerp(1.0f, 0.0f, t / 0.15f);
                        shardCardRT.localScale = new Vector3(s * baseScale.x, baseScale.y, baseScale.z);
                        yield return null;
                    }

                    // Toggle face at 0.0 scale!
                    isShowingFront = !isShowingFront;
                    shardCardBack.SetActive(!isShowingFront);
                    shardCardFront.SetActive(isShowingFront);

                    // Expand 0.0 -> 1.0
                    t = 0f;
                    while (t < 0.15f)
                    {
                        t += Time.deltaTime;
                        float s = Mathf.Lerp(0.0f, 1.0f, t / 0.15f);
                        shardCardRT.localScale = new Vector3(s * baseScale.x, baseScale.y, baseScale.z);
                        yield return null;
                    }
                }

                // Final Reveal: ensure Front is active and reveal real card name!
                shardCardBack.SetActive(false);
                shardCardFront.SetActive(true);
                shardCardRT.localScale = baseScale;
                if (cardNameTxt != null) cardNameTxt.text = chosenCard.DisplayName;

                float holdTime = 1.2f;
                while (holdTime > 0f)
                {
                    holdTime -= Time.deltaTime;
                    yield return null;
                }
            }
            else
            {
                // Rare (3s) or SR / SSR (4s)
                float totalTime = rarity == CardRarity.R ? 3.0f : 4.0f;
                float elapsed = 0f;
                float currentFlipDuration = 0.25f;

                while (elapsed < totalTime)
                {
                    float progress = elapsed / totalTime;
                    currentFlipDuration = Mathf.Lerp(0.25f, 0.08f, progress);

                    // Shrink 1.0 -> 0.0
                    t = 0f;
                    while (t < currentFlipDuration)
                    {
                        t += Time.deltaTime;
                        elapsed += Time.deltaTime;
                        float s = Mathf.Lerp(1.0f, 0.0f, t / currentFlipDuration);
                        shardCardRT.localScale = new Vector3(s * baseScale.x, baseScale.y, baseScale.z);

                        if (rarity >= CardRarity.SR)
                        {
                            if (Random.value < 0.25f) SpawnShardUIStarBurst(shardCardContainer, 3, 180f, 400f, 25f, 65f);
                            if (rarity >= CardRarity.SSR && Random.value < 0.15f) SpawnShardUIStarImplosion(shardCardContainer, 10, 1.2f, 35f, 85f);
                        }
                        yield return null;
                    }

                    // Toggle face at 0.0 scale!
                    isShowingFront = !isShowingFront;
                    shardCardBack.SetActive(!isShowingFront);
                    shardCardFront.SetActive(isShowingFront);

                    // Expand 0.0 -> 1.0
                    t = 0f;
                    while (t < currentFlipDuration)
                    {
                        t += Time.deltaTime;
                        elapsed += Time.deltaTime;
                        float s = Mathf.Lerp(0.0f, 1.0f, t / currentFlipDuration);
                        shardCardRT.localScale = new Vector3(s * baseScale.x, baseScale.y, baseScale.z);

                        if (rarity >= CardRarity.SR)
                        {
                            if (Random.value < 0.25f) SpawnShardUIStarBurst(shardCardContainer, 3, 180f, 400f, 25f, 65f);
                        }
                        yield return null;
                    }
                }

                // Final Flip Reveal: ensure Front is active and reveal real card name!
                shardCardBack.SetActive(false);
                shardCardFront.SetActive(true);
                if (cardNameTxt != null) cardNameTxt.text = chosenCard.DisplayName;

                var effectPlayer = FindObjectOfType<ClickEffectPlayer>();
                if (rarity >= CardRarity.SR)
                {
                    effectPlayer?.PlayGachaEffect(rarity);
                    SpawnShardUIStarBurst(shardCardContainer, rarity >= CardRarity.SSR ? 44 : 28, 450f, 950f, 45f, 110f);
                    if (rarity >= CardRarity.SSR) SpawnShardUIStarImplosion(shardCardContainer, 36, 1.5f, 50f, 120f);
                }

                t = 0f;
                while (t < 0.18f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(0.0f, 1.0f, t / 0.18f);
                    shardCardRT.localScale = new Vector3(s, 1.0f, 1.0f);
                    yield return null;
                }
                shardCardRT.localScale = Vector3.one;

                float holdTime = rarity >= CardRarity.SR ? 2.0f : 1.5f;
                while (holdTime > 0f)
                {
                    holdTime -= Time.deltaTime;
                    yield return null;
                }
            }

            shardAnimContainer.SetActive(false);
            Refresh();
        }

        private static Sprite cachedShardStarSprite;

        private static Sprite GetOrCreateShardStarSprite()
        {
            if (cachedShardStarSprite != null) return cachedShardStarSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float maxR = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y) - center;
                    float dist = pos.magnitude;
                    float angle = Mathf.Atan2(pos.y, pos.x);
                    float starFactor = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2f)), 5f);
                    float currentMaxR = maxR * (0.20f + 0.80f * starFactor);

                    if (dist > currentMaxR) cols[y * size + x] = Color.clear;
                    else
                    {
                        float normDist = dist / currentMaxR;
                        float alpha = Mathf.Clamp01(1f - normDist);
                        alpha = Mathf.Pow(alpha, 1.2f);
                        Color c = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.4f, 1f), normDist * 0.5f);
                        c.a = alpha;
                        cols[y * size + x] = c;
                    }
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            cachedShardStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedShardStarSprite;
        }

        private static Sprite cachedPawPrintSprite;

        private Sprite GetPawPrintParticleSprite()
        {
            if (pawPrintParticleSprite != null) return pawPrintParticleSprite;
            if (cachedPawPrintSprite != null) return cachedPawPrintSprite;
#if UNITY_EDITOR
            cachedPawPrintSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/image/main_game_ui/귀여운_발자국_파티클_50x50.png");
            if (cachedPawPrintSprite != null) return cachedPawPrintSprite;
#endif
            return GetOrCreateShardStarSprite();
        }

        private void SpawnShardUIStarBurst(Transform parent, int count, float minSpeed, float maxSpeed, float minSize, float maxSize)
        {
            if (parent == null) return;
            Sprite starSp = GetPawPrintParticleSprite();

            int finalCount = Mathf.Max(1, Mathf.RoundToInt(count * (2f / 3f)));

            for (int i = 0; i < finalCount; i++)
            {
                var starGO = new GameObject("PawParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starGO.transform.SetParent(parent, false);
                var img = starGO.GetComponent<Image>();
                img.sprite = starSp; img.raycastTarget = false;
                img.color = Color.white;
                var sRT = starGO.GetComponent<RectTransform>();
                
                float baseScale = Random.Range(0.8f, 1.2f);
                sRT.sizeDelta = new Vector2(50f, 50f);
                sRT.localScale = Vector3.one * baseScale;
                
                Vector2 startOffset = new Vector2(Random.Range(-25f, 25f), Random.Range(-25f, 25f));
                sRT.anchoredPosition = startOffset;

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float speed = Random.Range(minSpeed, maxSpeed);
                float rotSpeed = Random.Range(-360f, 360f);
                float lifetime = Random.Range(0.6f, 1.1f);
                StartCoroutine(AnimateShardStar(starGO, sRT, img, startOffset, dir, speed, rotSpeed, lifetime, baseScale));
            }
        }

        private System.Collections.IEnumerator AnimateShardStar(GameObject starGO, RectTransform rt, Image img, Vector2 startPos, Vector2 dir, float speed, float rotSpeed, float duration, float baseScale = 1.0f)
        {
            float elapsed = 0f;
            Vector2 pos = startPos;
            while (elapsed < duration)
            {
                if (starGO == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                pos += dir * speed * (1f - t * 0.5f) * Time.deltaTime;
                rt.anchoredPosition = pos;
                rt.Rotate(0, 0, rotSpeed * Time.deltaTime);
                float currentScale = baseScale * (1f - t * 0.2f);
                rt.localScale = new Vector3(currentScale, currentScale, 1f);
                img.color = new Color(1f, 1f, 1f, 1f - t);
                yield return null;
            }
            if (starGO != null) Destroy(starGO);
        }

        private void SpawnShardUIStarImplosion(Transform parent, int count, float duration, float minSize, float maxSize)
        {
            if (parent == null) return;
            Sprite starSp = GetPawPrintParticleSprite();

            for (int i = 0; i < count; i++)
            {
                var starGO = new GameObject("PawParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starGO.transform.SetParent(parent, false);
                var img = starGO.GetComponent<Image>();
                img.sprite = starSp; img.raycastTarget = false;
                img.color = Color.white;
                var sRT = starGO.GetComponent<RectTransform>();
                
                float baseScale = Random.Range(0.8f, 1.2f);
                sRT.sizeDelta = new Vector2(50f, 50f);
                sRT.localScale = Vector3.one * baseScale;
                sRT.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float startRadius = Random.Range(400f, 800f);
                Vector2 startPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * startRadius;
                sRT.anchoredPosition = startPos;

                float rotSpeed = Random.Range(-360f, 360f);
                StartCoroutine(AnimateShardStarImplosion(starGO, sRT, img, startPos, duration, baseScale, rotSpeed));
            }
        }

        private System.Collections.IEnumerator AnimateShardStarImplosion(GameObject starGO, RectTransform sRT, Image img, Vector2 startPos, float duration, float baseScale = 1.0f, float rotSpeed = 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (starGO == null || sRT == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                sRT.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, t * t);
                sRT.Rotate(0, 0, rotSpeed * Time.deltaTime);
                float currentScale = baseScale * (1f - t * 0.4f);
                sRT.localScale = new Vector3(currentScale, currentScale, 1f);
                if (img != null) img.color = new Color(1f, 1f, 1f, t < 0.2f ? (t / 0.2f) : (1f - t));
                yield return null;
            }
            if (starGO != null) Destroy(starGO);
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

        private void BindCloseButton()
        {
            Button closeBtn = null;

            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                string n = btn.name.ToLower();
                
                var tmpTxt = btn.GetComponentInChildren<TMP_Text>(true);
                var legTxt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
                string txt = tmpTxt != null ? tmpTxt.text.Trim().ToLower() : (legTxt != null ? legTxt.text.Trim().ToLower() : "");
                
                if (n == "closebutton" || n == "btn_close" || n.Contains("close") || txt == "✕" || txt == "x" || txt == "닫기")
                {
                    closeBtn = btn;
                    break;
                }
            }

            if (closeBtn != null)
            {
                var cs = closeBtn.colors;
                cs.normalColor = Color.white;
                cs.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cs.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
                cs.selectedColor = Color.white;
                cs.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                cs.colorMultiplier = 1f;
                cs.fadeDuration = 0.08f;
                closeBtn.colors = cs;

                var img = closeBtn.GetComponent<Image>();
                if (img != null && (img.color.r > 0.4f && img.color.g < 0.25f && img.color.b < 0.25f))
                {
                    img.color = new Color(0.2f, 0.22f, 0.28f, 1f);
                }

                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => {
                    Debug.Log("[ShopPanel] CloseButton clicked. Deactivating ShopPanel.");
                    gameObject.SetActive(false);
                });
            }
            else
            {
                Debug.LogWarning("[ShopPanel] CloseButton NOT FOUND in hierarchy!");
            }
        }

        private void BindListeners()
        {
            // 1. Bind CloseButton explicitly
            BindCloseButton();

            // 2. Bind Upgrade Row buttons
            var upgradeInfos = GetComponentsInChildren<UpgradeRowInfo>(true);
            upgradeRows.Clear();
            foreach (var info in upgradeInfos)
            {
                if (info != null && info.gameObject != null)
                {
                    upgradeRows.Add(info.gameObject);
                    var btn = info.GetComponentInChildren<Button>(true);
                    if (btn != null)
                    {
                        string upgId = info.UpgradeId;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { if (gm != null) gm.BuyUpgrade(upgId); });
                    }
                }
            }

            // 3. Bind Tab Header Buttons (업그레이드, 조각 교환, 상품)
            foreach (var btn in GetComponentsInChildren<Button>(true))
            {
                if (btn == null) continue;
                string t = btn.GetComponentInChildren<TMP_Text>()?.text.Trim() ?? "";
                string n = btn.name.ToLower();

                if (t == "업그레이드" || n.Contains("tab0") || n.Contains("upgrade"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowTab(0));
                }
                else if (t == "조각 교환" || n.Contains("tab1") || n.Contains("shard"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowTab(1));
                }
                else if (t == "상품" || n.Contains("tab2") || n.Contains("product") || n.Contains("goods"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ShowTab(2));
                }
            }

            // 3.5. Bind Shard Exchange Buttons (N, R, SR, SSR) under shardContent
            if (shardContent != null)
            {
                void BindShardCard(Transform cardTrans, CardRarity rarity)
                {
                    if (cardTrans == null) return;
                    var buttons = cardTrans.GetComponentsInChildren<Button>(true);
                    if (buttons.Length == 0)
                    {
                        var slotImg = cardTrans.GetComponent<Image>() ?? cardTrans.gameObject.AddComponent<Image>();
                        slotImg.raycastTarget = true;
                        var btn = cardTrans.GetComponent<Button>() ?? cardTrans.gameObject.AddComponent<Button>();
                        buttons = new[] { btn };
                    }
                    foreach (var b in buttons)
                    {
                        b.interactable = true;
                        var img = b.GetComponent<Image>();
                        if (img != null) img.raycastTarget = true;
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() => {
                            Debug.Log($"[ShopPanel] Shard Buy Button clicked for {rarity}!");
                            BuyRandomCard(rarity);
                        });

                        TMP_Text t = b.GetComponentInChildren<TMP_Text>(true);
                        if (rarity == CardRarity.N)   { buyNBtn = b;   buyNText = t; }
                        else if (rarity == CardRarity.R)   { buyRBtn = b;   buyRText = t; }
                        else if (rarity == CardRarity.SR)  { buySRBtn = b;  buySRText = t; }
                        else if (rarity == CardRarity.SSR) { buySSRBtn = b; buySSRText = t; }
                    }

                    foreach (var g in cardTrans.GetComponentsInChildren<Graphic>(true))
                    {
                        if (g.GetComponent<Button>() == null) g.raycastTarget = false;
                    }
                }

                foreach (Transform child in shardContent.GetComponentsInChildren<Transform>(true))
                {
                    string cName = child.name.ToLower();
                    string txt = (child.GetComponentInChildren<TMP_Text>(true)?.text ?? child.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text ?? "").Trim().ToLower();

                    if (cName.EndsWith("_n") || cName.StartsWith("card_n") || cName == "n" || txt.Contains("노말") || (txt.Contains("normal") && !txt.Contains("tab")))
                    {
                        if (child.GetComponent<Button>() != null || child.name.StartsWith("Card") || child.name.StartsWith("Btn") || child.name.StartsWith("Buy"))
                            BindShardCard(child, CardRarity.N);
                    }
                    else if (cName.EndsWith("_r") || cName.StartsWith("card_r") || cName == "r" || txt.Contains("레어") || txt.Contains("rare"))
                    {
                        if (child.GetComponent<Button>() != null || child.name.StartsWith("Card") || child.name.StartsWith("Btn") || child.name.StartsWith("Buy"))
                            BindShardCard(child, CardRarity.R);
                    }
                    else if (cName.EndsWith("_sr") || cName.StartsWith("card_sr") || cName == "sr" || txt.Contains("슈퍼레어") || txt.Contains("super rare"))
                    {
                        if (child.GetComponent<Button>() != null || child.name.StartsWith("Card") || child.name.StartsWith("Btn") || child.name.StartsWith("Buy"))
                            BindShardCard(child, CardRarity.SR);
                    }
                    else if (cName.EndsWith("_ssr") || cName.StartsWith("card_ssr") || cName == "ssr" || txt.Contains("최상위") || txt.Contains("ssr"))
                    {
                        if (child.GetComponent<Button>() != null || child.name.StartsWith("Card") || child.name.StartsWith("Btn") || child.name.StartsWith("Buy"))
                            BindShardCard(child, CardRarity.SSR);
                    }
                }

                if (buyNBtn != null)   { buyNBtn.onClick.RemoveAllListeners();   buyNBtn.onClick.AddListener(() => BuyRandomCard(CardRarity.N)); }
                if (buyRBtn != null)   { buyRBtn.onClick.RemoveAllListeners();   buyRBtn.onClick.AddListener(() => BuyRandomCard(CardRarity.R)); }
                if (buySRBtn != null)  { buySRBtn.onClick.RemoveAllListeners();  buySRBtn.onClick.AddListener(() => BuyRandomCard(CardRarity.SR)); }
                if (buySSRBtn != null) { buySSRBtn.onClick.RemoveAllListeners(); buySSRBtn.onClick.AddListener(() => BuyRandomCard(CardRarity.SSR)); }
            }

            // 4. Bind Existing Products UI (Slot buttons, Buy button, Page buttons)
            BindExistingProductsUI();
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

    public class UIHoverClickEffect : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler, UnityEngine.EventSystems.IPointerDownHandler, UnityEngine.EventSystems.IPointerUpHandler
    {
        public float hoverScale = 1.025f;
        public float pressScale = 0.96f;
        private Vector3 initialScale = Vector3.one;
        private bool scaleCached = false;

        private void EnsureScaleCached()
        {
            if (!scaleCached)
            {
                initialScale = transform.localScale;
                scaleCached = true;
            }
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            EnsureScaleCached();
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null && !btn.interactable) return;
            transform.localScale = initialScale * hoverScale;
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            EnsureScaleCached();
            transform.localScale = initialScale;
        }

        public void OnPointerDown(UnityEngine.EventSystems.PointerEventData eventData)
        {
            EnsureScaleCached();
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null && !btn.interactable) return;
            transform.localScale = initialScale * pressScale;
        }

        public void OnPointerUp(UnityEngine.EventSystems.PointerEventData eventData)
        {
            EnsureScaleCached();
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null && !btn.interactable) return;
            transform.localScale = initialScale * hoverScale;
        }

        private void OnDisable()
        {
            if (scaleCached) transform.localScale = initialScale;
        }
    }
}
