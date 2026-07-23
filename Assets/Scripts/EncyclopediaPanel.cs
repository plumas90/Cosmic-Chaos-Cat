using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 도감 패널 – 책 형태 레이아웃.
    /// 왼쪽 페이지: 3×3 카드 그리드 + 등급 필터 탭 (전체/N/R/SR/SSR/UR)
    /// 오른쪽 페이지: 선택된 카드 고정 상세 패널
    /// Set 탭: 기존 로직 유지
    /// </summary>
    public sealed class EncyclopediaPanel : MonoBehaviour
    {
        // ── Rarity filter state ──────────────────────────────────────────────
        private enum RarityFilter { All, N, R, SR, SSR, UR }

        // ── State ────────────────────────────────────────────────────────────
        private GameManager gm;
        private bool        showNoTab      = true;
        private string      selectedCardId = null;
        private int         currentPageIdx = 0;
        private RarityFilter currentFilter  = RarityFilter.All;
        private List<CardEntry> filteredCards = new List<CardEntry>();

        // ── Inspector-wired panels ───────────────────────────────────────────
        [SerializeField] private GameObject noPanel;
        [SerializeField] private GameObject setPanel;

        // No Tab – left page
        [SerializeField] private GameObject prevPageBtn;
        [SerializeField] private GameObject nextPageBtn;
        [SerializeField] private TMP_Text   pageLabel;
        [SerializeField] private TMP_Text   collectionCounterLabel;

        // No Tab – filter tab buttons (전체/N/R/SR/SSR/UR)
        [SerializeField] private GameObject filterTabAll;
        [SerializeField] private GameObject filterTabN;
        [SerializeField] private GameObject filterTabR;
        [SerializeField] private GameObject filterTabSR;
        [SerializeField] private GameObject filterTabSSR;
        [SerializeField] private GameObject filterTabUR;

        // No Tab – right page detail panel (always visible when a card is selected)
        [SerializeField] private GameObject  detailPanel;
        [SerializeField] private Image       detailCardArt;
        [SerializeField] private TMP_Text    detailCardName;
        [SerializeField] private TMP_Text    detailRarityBadge;
        [SerializeField] private TMP_Text    detailDescription;
        [SerializeField] private TMP_Text    detailIncomeText;
        [SerializeField] private TMP_Text    detailBreakthroughText;
        [SerializeField] private Button      detailEquipBtn;
        [SerializeField] private Button      detailBreakthroughBtn;

        // Set tab elements
        [SerializeField] private GameObject  leftSetPage;
        [SerializeField] private GameObject  rightSetPage;
        [SerializeField] private GameObject  prevSetPageBtn;
        [SerializeField] private GameObject  nextSetPageBtn;
        [SerializeField] private TMP_Text    setPageLabel;
        private int setPageIndex = 0;

        // Tab row buttons
        [SerializeField] private GameObject tabNoBtn;
        [SerializeField] private GameObject tabSetBtn;
        [SerializeField] private GameObject closeBtn;

        // ── field guide sprite references (assign in Inspector) ──────────────
        [Header("Field Guide Sprites (assign in Inspector)")]
        [SerializeField] private Sprite spriteBookOpen;
        [SerializeField] private Sprite spriteTabAll;
        [SerializeField] private Sprite spriteTabN;
        [SerializeField] private Sprite spriteTabR;
        [SerializeField] private Sprite spriteTabSR;
        [SerializeField] private Sprite spriteTabSSR;
        [SerializeField] private Sprite spriteCardFrameN;
        [SerializeField] private Sprite spriteCardFrameR;
        [SerializeField] private Sprite spriteCardFrameSR;
        [SerializeField] private Sprite spriteCardFrameSSR;
        [SerializeField] private Sprite spriteCardLocked;
        [SerializeField] private Sprite spriteCollectionCounter;
        [SerializeField] private Sprite spriteBtnRepresentative;
        [SerializeField] private Sprite spriteTitleCatCodex;
        [SerializeField] private Sprite spriteBtnPageLeft;
        [SerializeField] private Sprite spriteBtnPageRight;

        // ── Slot pool (9 slots for left page) ───────────────────────────────
        private const int SLOTS_PER_PAGE = 9;
        private const int GRID_COLS = 3;
        private const int GRID_ROWS = 3;
        private readonly List<SlotBundle> slots = new List<SlotBundle>();

        // ── Set tab slot pools ────────────────────────────────────────────────
        private readonly List<CardSlotUI> leftSetSlots  = new List<CardSlotUI>();
        private readonly List<CardSlotUI> rightSetSlots = new List<CardSlotUI>();

        // ── Style ─────────────────────────────────────────────────────────────
        private static TMP_FontAsset defaultFont;

        // Lazy-init flag: UI is built once, the first time the panel becomes active
        private bool _uiBuilt = false;

        // Book/parchment colors
        private static readonly Color PageBG       = new Color(0.94f, 0.89f, 0.76f, 1.00f);
        private static readonly Color PageBGRight  = new Color(0.90f, 0.85f, 0.72f, 1.00f);
        private static readonly Color DarkOverlay  = new Color(0.08f, 0.10f, 0.16f, 0.97f);
        private static readonly Color TabActive    = new Color(0.30f, 0.55f, 0.90f, 1.00f);
        private static readonly Color TabInactive  = new Color(0.45f, 0.40f, 0.35f, 1.00f);
        private static readonly Color BtnColor     = new Color(0.20f, 0.45f, 0.80f, 1.00f);
        private static readonly Color BtnEquip     = new Color(0.20f, 0.55f, 0.30f, 1.00f);
        private static readonly Color BtnBreak     = new Color(0.70f, 0.45f, 0.05f, 1.00f);

        private static readonly Color32 ColN   = new Color32(180, 180, 180, 255);
        private static readonly Color32 ColR   = new Color32( 80, 150, 255, 255);
        private static readonly Color32 ColSR  = new Color32(180,  80, 255, 255);
        private static readonly Color32 ColSSR = new Color32(255, 200,   0, 255);
        private static readonly Color32 ColUR  = new Color32(255,  80,  30, 255);

        // ─────────────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────
        private void Awake()
        {
            // gm wiring only. UI is built lazily in OnEnable so that panels
            // starting INACTIVE still get their UI when first opened.
            gm = FindObjectOfType<GameManager>(true);
        }

        private void OnEnable()
        {
            // ── Lazy UI build (only once) ─────────────────────────────────────
            if (!_uiBuilt)
            {
                try
                {
                    AutoWireFields();
                    if (noPanel == null)
                    {
                        EnsureParentedToCanvas();
                        BuildUI();
                    }
                    else
                    {
                        BindAllListeners();
                    }
                    EnsureSetPageChildrenBuilt();
                    _uiBuilt = true;
                    Debug.Log("[EncyclopediaPanel] UI built");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EncyclopediaPanel] UI build exception: {e}");
                }
            }

            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null) gm.StateChanged += OnStateChanged;
            currentPageIdx = 0;
            currentFilter  = RarityFilter.All;
            ShowNoTab();
        }

        private void OnDisable()
        {
            if (gm != null) gm.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged()
        {
            if (showNoTab) RefreshNoTab();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EDITOR BAKE  –  에디터 모드에서 씬에 UI를 미리 구워넣습니다
        //  Inspector 우클릭 → "씬에 UI 빌드" 또는
        //  메뉴 Tools → Encyclopedia → 씬에 UI 빌드
        // ─────────────────────────────────────────────────────────────────────
        [ContextMenu("씬에 UI 빌드 (에디터 전용)")]
        public void BakeUIToScene()
        {
            // 폰트 로드 (에디터 전용)
#if UNITY_EDITOR
            if (defaultFont == null)
            {
                defaultFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Font/Galmuri9 SDF.asset");
            }
#endif
            // 기존 EncyPanel 자식 정리
            var existingEncyPanel = transform.Find("EncyPanel");
            if (existingEncyPanel != null) DestroyImmediate(existingEncyPanel.gameObject);

            // 기존 NoPanel / SetPanel 직접 자식도 정리
            if (noPanel  != null) { DestroyImmediate(noPanel);  }
            if (setPanel != null) { DestroyImmediate(setPanel); }

            // 모든 [SerializeField] 레퍼런스 초기화
            ResetAllSerializedFields();
            slots.Clear();
            leftSetSlots.Clear();
            rightSetSlots.Clear();
            _uiBuilt = false;

            // 새 UI 빌드
            EnsureParentedToCanvas();
            BuildUI();
            EnsureSetPageChildrenBuilt();
            _uiBuilt = true;

#if UNITY_EDITOR
            // 씬 더티 마킹 → 저장하면 계층에 남음
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[EncyclopediaPanel] 씬에 UI를 구웠습니다. Ctrl+S로 저장하세요.");
#endif
        }

        /// <summary>모든 직렬화 레퍼런스를 null로 초기화합니다.</summary>
        private void ResetAllSerializedFields()
        {
            noPanel = null; setPanel = null;
            prevPageBtn = null; nextPageBtn = null;
            pageLabel = null; collectionCounterLabel = null;
            filterTabAll = null; filterTabN = null; filterTabR = null;
            filterTabSR = null; filterTabSSR = null; filterTabUR = null;
            detailPanel = null; detailCardArt = null; detailCardName = null;
            detailRarityBadge = null; detailDescription = null;
            detailIncomeText = null; detailBreakthroughText = null;
            detailEquipBtn = null; detailBreakthroughBtn = null;
            leftSetPage = null; rightSetPage = null;
            prevSetPageBtn = null; nextSetPageBtn = null; setPageLabel = null;
            tabNoBtn = null; tabSetBtn = null; closeBtn = null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI CONSTRUCTION  (code-built fallback when no prefab is in the scene)
        // ─────────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            if (noPanel != null) return;

            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Semi-transparent full-screen overlay
            var overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.55f);

            // ── Outer book panel ──────────────────────────────────────────────
            var outerPanel = MakeEmptyRT(transform, "EncyPanel",
                Vector2.zero, new Vector2(1020f, 660f));
            var outerImg = outerPanel.gameObject.AddComponent<Image>();
            outerImg.color = new Color(0.28f, 0.20f, 0.12f, 1f); // Dark brown book cover

            // ── Title ─────────────────────────────────────────────────────────
            var titleGO = MakeText(outerPanel.transform, "고양이 도감",
                new Vector2(0f, 296f), new Vector2(400f, 48f), 28, new Color(0.95f, 0.88f, 0.65f));
            titleGO.fontStyle = FontStyles.Bold;
            titleGO.alignment = TextAlignmentOptions.Center;

            // ── Main tab row ──────────────────────────────────────────────────
            tabNoBtn  = MakeButton(outerPanel.transform, "No.",  new Vector2(-80f, 266f), new Vector2(80f, 30f),
                TabActive, ShowNoTab);
            tabSetBtn = MakeButton(outerPanel.transform, "Set",  new Vector2(10f,  266f), new Vector2(80f, 30f),
                TabInactive, ShowSetTab);
            closeBtn  = MakeButton(outerPanel.transform, "✕",   new Vector2(470f, 300f), new Vector2(38f, 38f),
                new Color(0.65f, 0.18f, 0.18f), OnClose);

            // ── LEFT PAGE (card grid) ────────────────────────────────────────
            noPanel = MakeEmptyRT(outerPanel.transform, "NoPanel",
                new Vector2(-2f, -15f), new Vector2(1020f, 620f)).gameObject;

            var leftPage = MakePage(noPanel.transform, new Vector2(-255f, 0f), new Vector2(490f, 595f), PageBG);

            // Collection counter text (e.g. "수집 6 / 14")
            collectionCounterLabel = MakeText(leftPage.transform, "수집 0 / 0",
                new Vector2(0f, 265f), new Vector2(280f, 28f), 13, new Color(0.25f, 0.18f, 0.10f));
            collectionCounterLabel.alignment = TextAlignmentOptions.Center;

            // Rarity filter tabs
            BuildRarityFilterTabs(leftPage.transform);

            // 3×3 card slot grid
            BuildSlotGrid(leftPage.transform);

            // Page nav buttons
            prevPageBtn = MakeButton(leftPage.transform, "◀",
                new Vector2(-192f, -262f), new Vector2(38f, 38f), TabInactive, () => ChangePage(-1));
            SetButtonSprite(prevPageBtn, spriteBtnPageLeft);
            nextPageBtn = MakeButton(leftPage.transform, "▶",
                new Vector2(192f, -262f), new Vector2(38f, 38f), TabInactive, () => ChangePage(1));
            SetButtonSprite(nextPageBtn, spriteBtnPageRight);

            pageLabel = MakeText(leftPage.transform, "1 / 1",
                new Vector2(0f, -263f), new Vector2(120f, 28f), 13, new Color(0.25f, 0.18f, 0.10f));
            pageLabel.alignment = TextAlignmentOptions.Center;

            // ── RIGHT PAGE (detail panel) ────────────────────────────────────
            var rightPage = MakePage(noPanel.transform, new Vector2(255f, 0f), new Vector2(490f, 595f), PageBGRight);
            BuildDetailPanel(rightPage.transform);

            // ── SET PANEL ────────────────────────────────────────────────────
            BuildSetTabArea(outerPanel.transform);
        }

        private void BuildRarityFilterTabs(Transform parent)
        {
            // Tabs row: 전체 / N / R / SR / SSR / UR
            float startX = -185f;
            float tabY   = 222f;
            float tabW   = 60f;
            float gap    = 65f;

            filterTabAll = MakeButton(parent, "전체", new Vector2(startX + 0 * gap, tabY), new Vector2(tabW, 26f), TabActive,   () => SetFilter(RarityFilter.All));
            filterTabN   = MakeButton(parent, "N",   new Vector2(startX + 1 * gap, tabY), new Vector2(tabW, 26f), TabInactive, () => SetFilter(RarityFilter.N));
            filterTabR   = MakeButton(parent, "R",   new Vector2(startX + 2 * gap, tabY), new Vector2(tabW, 26f), TabInactive, () => SetFilter(RarityFilter.R));
            filterTabSR  = MakeButton(parent, "SR",  new Vector2(startX + 3 * gap, tabY), new Vector2(tabW, 26f), TabInactive, () => SetFilter(RarityFilter.SR));
            filterTabSSR = MakeButton(parent, "SSR", new Vector2(startX + 4 * gap, tabY), new Vector2(tabW + 5f, 26f), TabInactive, () => SetFilter(RarityFilter.SSR));
            filterTabUR  = MakeButton(parent, "UR",  new Vector2(startX + 5 * gap + 5f, tabY), new Vector2(tabW, 26f), TabInactive, () => SetFilter(RarityFilter.UR));

            // Apply rarity colors to N/R/SR/SSR/UR tab labels
            ApplyTabLabelColor(filterTabN,   (Color)ColN);
            ApplyTabLabelColor(filterTabR,   (Color)ColR);
            ApplyTabLabelColor(filterTabSR,  (Color)ColSR);
            ApplyTabLabelColor(filterTabSSR, (Color)ColSSR);
            ApplyTabLabelColor(filterTabUR,  (Color)ColUR);

            // Apply sprites if available
            SetButtonSprite(filterTabAll, spriteTabAll);
            SetButtonSprite(filterTabN,   spriteTabN);
            SetButtonSprite(filterTabR,   spriteTabR);
            SetButtonSprite(filterTabSR,  spriteTabSR);
            SetButtonSprite(filterTabSSR, spriteTabSSR);
        }

        private void BuildSlotGrid(Transform parent)
        {
            slots.Clear();
            float colW   = 145f;
            float rowH   = 155f;
            float startX = -145f;
            float startY = 90f;

            for (int row = 0; row < GRID_ROWS; row++)
            for (int col = 0; col < GRID_COLS; col++)
            {
                int si  = row * GRID_COLS + col;
                float x = startX + col * colW;
                float y = startY - row * rowH;

                var slotGO = new GameObject($"Slot_{si}");
                slotGO.transform.SetParent(parent, false);

                var slotRT = slotGO.AddComponent<RectTransform>();
                slotRT.anchoredPosition = new Vector2(x, y);
                slotRT.sizeDelta        = new Vector2(130f, 145f);

                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = new Color(0.82f, 0.76f, 0.60f, 0.6f);

                slotGO.AddComponent<Button>();

                // Frame
                var frameGO = MakeImage(slotGO.transform, "Frame",
                    Vector2.zero, new Vector2(118f, 132f), Color.clear);
                var frameImg = frameGO.GetComponent<Image>();
                frameImg.type = Image.Type.Sliced;

                // Art
                var artGO = MakeImage(slotGO.transform, "Art",
                    new Vector2(0f, 10f), new Vector2(90f, 90f), Color.gray);

                // Name text
                var nameTx = MakeText(slotGO.transform, "???",
                    new Vector2(0f, -44f), new Vector2(120f, 22f), 10, new Color(0.15f, 0.10f, 0.05f));
                nameTx.alignment          = TextAlignmentOptions.Center;
                nameTx.enableWordWrapping = false;
                nameTx.overflowMode       = TextOverflowModes.Ellipsis;
                nameTx.gameObject.name    = "NameText";

                // Rarity text (small badge top-left)
                var rarityTx = MakeText(slotGO.transform, "",
                    new Vector2(-44f, 56f), new Vector2(44f, 20f), 9, Color.gray);
                rarityTx.alignment       = TextAlignmentOptions.Left;
                rarityTx.fontStyle       = FontStyles.Bold;
                rarityTx.gameObject.name = "RarityText";

                // Unknown overlay
                var unkGO = new GameObject("Unknown");
                unkGO.transform.SetParent(slotGO.transform, false);
                var unkRT = unkGO.AddComponent<RectTransform>();
                unkRT.anchorMin = Vector2.zero; unkRT.anchorMax = Vector2.one;
                unkRT.offsetMin = Vector2.zero; unkRT.offsetMax = Vector2.zero;
                var unkImg = unkGO.AddComponent<Image>();
                if (spriteCardLocked != null) { unkImg.sprite = spriteCardLocked; unkImg.type = Image.Type.Sliced; unkImg.color = Color.white; }
                else unkImg.color = new Color(0.10f, 0.08f, 0.06f, 0.80f);
                unkImg.raycastTarget = false;

                var unkTx = MakeText(unkGO.transform, "미해금\n???",
                    new Vector2(0f, 0f), new Vector2(100f, 80f), 12, new Color(0.60f, 0.55f, 0.45f));
                unkTx.alignment     = TextAlignmentOptions.Center;
                unkTx.raycastTarget = false;

                // CardSlotUI
                var slotUI = slotGO.AddComponent<CardSlotUI>();
                slotUI.InitUI(frameImg, artGO.GetComponent<Image>(), nameTx, rarityTx, unkGO);

                int captured = si;
                slotGO.GetComponent<Button>().onClick.AddListener(() => OnSlotClicked(captured));

                slots.Add(new SlotBundle { go = slotGO, ui = slotUI });
            }
        }

        private void BuildDetailPanel(Transform parent)
        {
            detailPanel = MakeEmptyRT(parent, "DetailPanel",
                Vector2.zero, new Vector2(480f, 580f)).gameObject;

            // ── Card art (upper half, centered) ──────────────────────────────
            var artGO = MakeImage(detailPanel.transform, "DetailArt",
                new Vector2(60f, 130f), new Vector2(200f, 220f), Color.gray);
            detailCardArt = artGO.GetComponent<Image>();
            detailCardArt.preserveAspect = true;

            // ── Rarity badge ─────────────────────────────────────────────────
            var rarityBG = MakeImage(detailPanel.transform, "RarityBG",
                new Vector2(-120f, 220f), new Vector2(56f, 28f), new Color(0.18f, 0.18f, 0.22f));
            detailRarityBadge = MakeText(rarityBG.transform, "N",
                Vector2.zero, new Vector2(56f, 28f), 14, (Color)ColN);
            detailRarityBadge.alignment = TextAlignmentOptions.Center;
            detailRarityBadge.fontStyle = FontStyles.Bold;
            var rrt = detailRarityBadge.GetComponent<RectTransform>();
            rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
            rrt.offsetMin = Vector2.zero; rrt.offsetMax = Vector2.zero;

            // ── Card name ─────────────────────────────────────────────────────
            detailCardName = MakeText(detailPanel.transform, "카드 이름",
                new Vector2(10f, 195f), new Vector2(300f, 36f), 20, new Color(0.15f, 0.10f, 0.05f));
            detailCardName.fontStyle = FontStyles.Bold;
            detailCardName.alignment = TextAlignmentOptions.Left;

            // ── Stats divider line ────────────────────────────────────────────
            var divider = MakeImage(detailPanel.transform, "Divider",
                new Vector2(10f, 173f), new Vector2(340f, 2f), new Color(0.40f, 0.32f, 0.22f, 0.6f));

            // ── Stat lines ────────────────────────────────────────────────────
            // Income (골드 생산)
            detailIncomeText = MakeText(detailPanel.transform, "💰 골드 생산  0.0",
                new Vector2(10f, 150f), new Vector2(340f, 28f), 14, new Color(0.15f, 0.10f, 0.05f));
            detailIncomeText.alignment = TextAlignmentOptions.Left;

            // Breakthrough / enhancement
            detailBreakthroughText = MakeText(detailPanel.transform, "⭐ 강화  0강 / 5강",
                new Vector2(10f, 118f), new Vector2(340f, 28f), 14, new Color(0.15f, 0.10f, 0.05f));
            detailBreakthroughText.alignment = TextAlignmentOptions.Left;

            // Description / 특징
            var descBG = MakeImage(detailPanel.transform, "DescBG",
                new Vector2(150f, 148f), new Vector2(168f, 68f), new Color(0.88f, 0.82f, 0.68f));
            detailDescription = MakeText(descBG.transform, "특징\n—",
                Vector2.zero, new Vector2(160f, 64f), 11, new Color(0.18f, 0.12f, 0.06f));
            detailDescription.alignment        = TextAlignmentOptions.TopLeft;
            detailDescription.textWrappingMode = TextWrappingModes.Normal;
            var drt = detailDescription.GetComponent<RectTransform>();
            drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
            drt.offsetMin = new Vector2(4f,  4f); drt.offsetMax = new Vector2(-4f, -4f);

            // ── Divider 2 ─────────────────────────────────────────────────────
            MakeImage(detailPanel.transform, "Divider2",
                new Vector2(10f, 80f), new Vector2(340f, 2f), new Color(0.40f, 0.32f, 0.22f, 0.6f));

            // ── 대표 설정 (장착하기) button ──────────────────────────────────
            var equipBtnGO = MakeButton(detailPanel.transform, "대표 설정",
                new Vector2(10f, 42f), new Vector2(220f, 48f), BtnEquip, OnDetailEquip);
            if (spriteBtnRepresentative != null)
            {
                var img = equipBtnGO.GetComponent<Image>();
                img.sprite = spriteBtnRepresentative;
                img.type   = Image.Type.Sliced;
                img.color  = Color.white;
            }
            detailEquipBtn = equipBtnGO.GetComponent<Button>();
            var equipLbl = equipBtnGO.GetComponentInChildren<TMP_Text>();
            if (equipLbl != null) { equipLbl.fontSize = 16; equipLbl.color = new Color(0.90f, 0.85f, 0.65f); }

            // ── 한계 돌파 button ──────────────────────────────────────────────
            var breakBtnGO = MakeButton(detailPanel.transform, "한계 돌파",
                new Vector2(10f, -14f), new Vector2(220f, 44f), BtnBreak, OnDetailBreakthrough);
            detailBreakthroughBtn = breakBtnGO.GetComponent<Button>();
            var breakLbl = breakBtnGO.GetComponentInChildren<TMP_Text>();
            if (breakLbl != null) { breakLbl.fontSize = 14; breakLbl.color = Color.white; }

            // Default state: show "카드를 선택하세요"
            SetDetailEmpty();
        }

        private void BuildSetTabArea(Transform parent)
        {
            if (setPanel == null)
            {
                setPanel = MakeEmptyRT(parent, "SetPanel",
                    new Vector2(-2f, -15f), new Vector2(1020f, 620f)).gameObject;
            }
            setPanel.SetActive(false);

            if (leftSetPage == null)
                leftSetPage = MakePage(setPanel.transform, new Vector2(-255f, 0f), new Vector2(490f, 595f), PageBG);
            PrepareSetPageChildren(leftSetPage.transform);

            if (rightSetPage == null)
                rightSetPage = MakePage(setPanel.transform, new Vector2(255f, 0f), new Vector2(490f, 595f), PageBGRight);
            PrepareSetPageChildren(rightSetPage.transform);

            if (prevSetPageBtn == null)
                prevSetPageBtn = MakeButton(setPanel.transform, "◀",
                    new Vector2(-440f, 0f), new Vector2(38f, 38f), TabInactive, () => ChangeSetPage(-1));
            if (nextSetPageBtn == null)
                nextSetPageBtn = MakeButton(setPanel.transform, "▶",
                    new Vector2(440f, 0f), new Vector2(38f, 38f), TabInactive, () => ChangeSetPage(1));
            if (setPageLabel == null)
                setPageLabel = MakeText(setPanel.transform, "1 / 1",
                    new Vector2(0f, -295f), new Vector2(120f, 28f), 13, Color.white);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LISTENERS
        // ─────────────────────────────────────────────────────────────────────
        private void BindAllListeners()
        {
            BindBtn(tabNoBtn,      ShowNoTab);
            BindBtn(tabSetBtn,     ShowSetTab);
            BindBtn(closeBtn,      OnClose);
            BindBtn(prevPageBtn,   () => ChangePage(-1));
            BindBtn(nextPageBtn,   () => ChangePage(1));
            BindBtn(prevSetPageBtn,() => ChangeSetPage(-1));
            BindBtn(nextSetPageBtn,() => ChangeSetPage(1));

            BindBtn(filterTabAll,  () => SetFilter(RarityFilter.All));
            BindBtn(filterTabN,    () => SetFilter(RarityFilter.N));
            BindBtn(filterTabR,    () => SetFilter(RarityFilter.R));
            BindBtn(filterTabSR,   () => SetFilter(RarityFilter.SR));
            BindBtn(filterTabSSR,  () => SetFilter(RarityFilter.SSR));
            BindBtn(filterTabUR,   () => SetFilter(RarityFilter.UR));

            if (detailEquipBtn != null) { detailEquipBtn.onClick.RemoveAllListeners(); detailEquipBtn.onClick.AddListener(OnDetailEquip); }
            if (detailBreakthroughBtn != null) { detailBreakthroughBtn.onClick.RemoveAllListeners(); detailBreakthroughBtn.onClick.AddListener(OnDetailBreakthrough); }

            // Rebind slot click listeners
            slots.Clear();
            if (noPanel != null)
            {
                var childSlots = noPanel.GetComponentsInChildren<CardSlotUI>(true);
                for (int i = 0; i < childSlots.Length; i++)
                {
                    int idx = i;
                    var btn = childSlots[i].GetComponent<Button>();
                    if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => OnSlotClicked(idx)); }
                    slots.Add(new SlotBundle { go = childSlots[i].gameObject, ui = childSlots[i] });
                }
            }
        }

        private static void BindBtn(GameObject go, UnityEngine.Events.UnityAction action)
        {
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TAB SWITCHING
        // ─────────────────────────────────────────────────────────────────────
        private void ShowNoTab()
        {
            showNoTab = true;
            if (noPanel  != null) noPanel.SetActive(true);
            if (setPanel != null) setPanel.SetActive(false);
            SetBtnColor(tabNoBtn,  TabActive);
            SetBtnColor(tabSetBtn, TabInactive);
            RefreshNoTab();
        }

        private void ShowSetTab()
        {
            showNoTab = false;
            if (noPanel  != null) noPanel.SetActive(false);
            if (setPanel != null) setPanel.SetActive(true);
            SetBtnColor(tabNoBtn,  TabInactive);
            SetBtnColor(tabSetBtn, TabActive);
            RefreshSets();
        }

        private void OnClose()
        {
            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FILTER
        // ─────────────────────────────────────────────────────────────────────
        private void SetFilter(RarityFilter f)
        {
            currentFilter  = f;
            currentPageIdx = 0;
            UpdateFilterTabColors();
            RefreshNoTab();
        }

        private void UpdateFilterTabColors()
        {
            SetBtnColor(filterTabAll, currentFilter == RarityFilter.All ? TabActive : TabInactive);
            SetBtnColor(filterTabN,   currentFilter == RarityFilter.N   ? TabActive : TabInactive);
            SetBtnColor(filterTabR,   currentFilter == RarityFilter.R   ? TabActive : TabInactive);
            SetBtnColor(filterTabSR,  currentFilter == RarityFilter.SR  ? TabActive : TabInactive);
            SetBtnColor(filterTabSSR, currentFilter == RarityFilter.SSR ? TabActive : TabInactive);
            SetBtnColor(filterTabUR,  currentFilter == RarityFilter.UR  ? TabActive : TabInactive);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REFRESH – No TAB
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshNoTab()
        {
            if (gm == null) return;
            var allCards = gm.CardCatalog?.Cards;
            if (allCards == null) return;

            // Build filtered list (hidden cards always excluded from No. tab)
            filteredCards.Clear();
            foreach (var c in allCards)
            {
                if (c.IsHidden) continue;
                if (currentFilter == RarityFilter.All) { filteredCards.Add(c); continue; }
                if (currentFilter == RarityFilter.N   && c.Rarity == CardRarity.N)   filteredCards.Add(c);
                if (currentFilter == RarityFilter.R   && c.Rarity == CardRarity.R)   filteredCards.Add(c);
                if (currentFilter == RarityFilter.SR  && c.Rarity == CardRarity.SR)  filteredCards.Add(c);
                if (currentFilter == RarityFilter.SSR && c.Rarity == CardRarity.SSR) filteredCards.Add(c);
                if (currentFilter == RarityFilter.UR  && c.Rarity == CardRarity.UR)  filteredCards.Add(c);
            }

            int total   = filteredCards.Count;
            int maxPage = Mathf.Max(0, (total - 1) / SLOTS_PER_PAGE);
            currentPageIdx = Mathf.Clamp(currentPageIdx, 0, maxPage);

            if (pageLabel != null) pageLabel.text = $"{currentPageIdx + 1} / {maxPage + 1}";

            // Update collection counter with total (non-hidden) collected
            if (collectionCounterLabel != null)
            {
                int totalNonHidden   = 0;
                int unlockedNonHidden = 0;
                var states = gm.GetCardStates();
                foreach (var c in allCards)
                {
                    if (c.IsHidden) continue;
                    totalNonHidden++;
                    states.TryGetValue(c.Id, out var p);
                    if (p != null && p.Unlocked) unlockedNonHidden++;
                }
                collectionCounterLabel.text = $"수집  {unlockedNonHidden} / {totalNonHidden}";
            }

            // Navigation buttons
            if (prevPageBtn != null)
            {
                var b = prevPageBtn.GetComponent<Button>();
                if (b != null) b.interactable = currentPageIdx > 0;
            }
            if (nextPageBtn != null)
            {
                var b = nextPageBtn.GetComponent<Button>();
                if (b != null) b.interactable = currentPageIdx < maxPage;
            }

            // Fill slots
            var cardStates = gm.GetCardStates();
            int startIdx = currentPageIdx * SLOTS_PER_PAGE;

            for (int i = 0; i < slots.Count; i++)
            {
                int ci = startIdx + i;
                var slot = slots[i];
                if (ci < total)
                {
                    slot.go.SetActive(true);
                    var card = filteredCards[ci];
                    cardStates.TryGetValue(card.Id, out var prog);
                    slot.ui.SetData(card, FindOriginalIndex(card, allCards), prog, gm, OnSlotClickedById);
                }
                else
                {
                    slot.go.SetActive(false);
                }
            }

            // Refresh detail panel if a card is selected
            if (!string.IsNullOrEmpty(selectedCardId))
                RefreshDetailPanel(selectedCardId);
        }

        private int FindOriginalIndex(CardEntry card, IReadOnlyList<CardEntry> all)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Id == card.Id) return i + 1;
            return 1;
        }

        private void ChangePage(int delta)
        {
            currentPageIdx += delta;
            RefreshNoTab();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SLOT CLICK
        // ─────────────────────────────────────────────────────────────────────
        private void OnSlotClicked(int slotIndex)
        {
            int ci = currentPageIdx * SLOTS_PER_PAGE + slotIndex;
            if (ci < filteredCards.Count)
                OnSlotClickedById(filteredCards[ci].Id);
        }

        private void OnSlotClickedById(string cardId)
        {
            selectedCardId = cardId;
            RefreshDetailPanel(cardId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DETAIL PANEL (right page)
        // ─────────────────────────────────────────────────────────────────────
        private void SetDetailEmpty()
        {
            if (detailCardName      != null) detailCardName.text      = "카드를 선택하세요";
            if (detailRarityBadge   != null) detailRarityBadge.text   = "";
            if (detailDescription   != null) detailDescription.text   = "";
            if (detailIncomeText    != null) detailIncomeText.text     = "";
            if (detailBreakthroughText != null) detailBreakthroughText.text = "";
            if (detailCardArt       != null) { detailCardArt.sprite = null; detailCardArt.color = new Color(0.4f, 0.35f, 0.28f, 0.4f); }
            if (detailEquipBtn      != null) detailEquipBtn.interactable = false;
            if (detailBreakthroughBtn != null) detailBreakthroughBtn.gameObject.SetActive(false);
        }

        private void RefreshDetailPanel(string cardId)
        {
            if (detailPanel == null) return;

            var card = gm?.CardCatalog?.FindById(cardId);
            var states = gm?.GetCardStates();
            CardProgress prog = null;
            states?.TryGetValue(cardId, out prog);
            bool unlocked = prog != null && prog.Unlocked;

            // Card name
            if (detailCardName != null)
                detailCardName.text = unlocked && card != null ? card.DisplayName : "???";

            // Rarity badge
            if (detailRarityBadge != null && card != null)
            {
                detailRarityBadge.text  = unlocked ? card.Rarity.ToString() : "?";
                detailRarityBadge.color = unlocked ? (Color)RarityToColor(card.Rarity) : Color.gray;
            }

            // Card art
            if (detailCardArt != null)
            {
                if (unlocked && card?.CardSprite != null)
                {
                    detailCardArt.sprite = card.CardSprite;
                    detailCardArt.color  = Color.white;
                }
                else
                {
                    detailCardArt.sprite = null;
                    detailCardArt.color  = new Color(0.30f, 0.26f, 0.20f, 0.6f);
                }
            }

            // Income stat
            if (detailIncomeText != null)
            {
                if (unlocked && card != null && prog != null)
                {
                    int    lvl    = prog.BreakthroughCount;
                    double income = card.ClickMultiplier * (1 + lvl);
                    if (lvl >= 5) income += card.ClickMultiplier;
                    detailIncomeText.text = $"💰 골드 생산   {income:F1} / 초";
                }
                else
                {
                    detailIncomeText.text = "💰 골드 생산   ???";
                }
            }

            // Breakthrough / enhancement
            if (detailBreakthroughText != null)
            {
                if (unlocked && prog != null)
                    detailBreakthroughText.text = $"⭐ 강화   {prog.BreakthroughCount}강 / 5강";
                else
                    detailBreakthroughText.text = "⭐ 강화   미해금";
            }

            // Description (특징)
            if (detailDescription != null)
            {
                if (unlocked && card != null)
                {
                    string raw = card.GetDescription();
                    detailDescription.text = "특징\n" + raw;
                }
                else
                {
                    detailDescription.text = "특징\n???";
                }
            }

            // Equip button
            if (detailEquipBtn != null)
            {
                detailEquipBtn.interactable = unlocked;
                bool isEquipped = gm != null && gm.EquippedCardId == cardId;
                var lbl = detailEquipBtn.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = isEquipped ? "✔ 대표 설정됨" : "대표 설정";
            }

            // Breakthrough button
            if (detailBreakthroughBtn != null)
            {
                if (unlocked && card != null && prog != null)
                {
                    detailBreakthroughBtn.gameObject.SetActive(true);
                    bool hasUnused  = prog.Copies > prog.BreakthroughCount + 1;
                    bool canUpgrade = hasUnused && prog.BreakthroughCount < 5;
                    SetButtonInteractable(detailBreakthroughBtn, canUpgrade);
                    var lbl = detailBreakthroughBtn.GetComponentInChildren<TMP_Text>();
                    if (lbl != null)
                    {
                        if (prog.BreakthroughCount >= 5)
                            lbl.text = "한계 돌파 (최대)";
                        else
                            lbl.text = $"한계 돌파 ({prog.Copies} / {prog.BreakthroughCount + 2} 장)";
                    }
                }
                else
                {
                    detailBreakthroughBtn.gameObject.SetActive(false);
                }
            }
        }

        private void OnDetailEquip()
        {
            if (!string.IsNullOrEmpty(selectedCardId))
            {
                gm?.EquipCard(selectedCardId);
                RefreshDetailPanel(selectedCardId); // update button label
            }
        }

        private void OnDetailBreakthrough()
        {
            if (string.IsNullOrEmpty(selectedCardId) || gm == null) return;
            if (gm.BreakthroughCard(selectedCardId))
                RefreshDetailPanel(selectedCardId);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REFRESH – Set TAB
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshSets()
        {
            if (leftSetPage == null || rightSetPage == null || gm == null) return;
            var sets = gm.SetCatalog?.Sets;
            if (sets == null || sets.Count == 0)
            {
                if (setPageLabel != null) setPageLabel.text = "0 / 0";
                return;
            }

            int maxPages = Mathf.CeilToInt(sets.Count / 2f);
            setPageIndex = Mathf.Clamp(setPageIndex, 0, maxPages - 1);
            if (setPageLabel != null) setPageLabel.text = $"{setPageIndex + 1} / {maxPages}";

            var leftPageImg  = leftSetPage.GetComponent<Image>();
            if (leftPageImg  != null) leftPageImg.raycastTarget  = false;
            var rightPageImg = rightSetPage.GetComponent<Image>();
            if (rightPageImg != null) rightPageImg.raycastTarget = false;

            int leftIdx  = setPageIndex * 2;
            int rightIdx = setPageIndex * 2 + 1;

            SetEntry leftEntry  = leftIdx  < sets.Count ? sets[leftIdx]  : null;
            SetEntry rightEntry = rightIdx < sets.Count ? sets[rightIdx] : null;

            RefreshSetPageData(leftSetPage.transform,  leftEntry,  leftSetSlots);
            RefreshSetPageData(rightSetPage.transform, rightEntry, rightSetSlots);

            if (prevSetPageBtn != null)
            {
                var b = prevSetPageBtn.GetComponent<Button>();
                if (b != null) b.interactable = setPageIndex > 0;
            }
            if (nextSetPageBtn != null)
            {
                var b = nextSetPageBtn.GetComponent<Button>();
                if (b != null) b.interactable = setPageIndex < maxPages - 1;
            }
        }

        private void ChangeSetPage(int delta)
        {
            var sets = gm?.SetCatalog?.Sets;
            if (sets == null || sets.Count == 0) return;
            int maxPages = Mathf.CeilToInt(sets.Count / 2f);
            setPageIndex = Mathf.Clamp(setPageIndex + delta, 0, maxPages - 1);
            RefreshSets();
        }

        private void RefreshSetPageData(Transform pageTf, SetEntry setEntry, List<CardSlotUI> pool)
        {
            if (setEntry == null)
            {
                foreach (var s in pool) if (s != null) s.gameObject.SetActive(false);
                var et = pageTf.Find("SetNameTitle"); if (et != null) et.gameObject.SetActive(false);
                var eb = pageTf.Find("ClaimBtn");     if (eb != null) eb.gameObject.SetActive(false);
                return;
            }

            var titleTf = pageTf.Find("SetNameTitle");
            if (titleTf != null)
            {
                titleTf.gameObject.SetActive(true);
                var t = titleTf.GetComponent<TMP_Text>();
                if (t != null) t.text = setEntry.SetName;
            }

            var states    = gm.GetCardStates();
            var catalog   = gm.CardCatalog?.Cards;
            int cardCount = setEntry.CardIds.Count;
            bool allOwned = true;

            for (int i = 0; i < pool.Count; i++)
            {
                var slot = pool[i];
                if (slot == null) continue;
                if (i < cardCount)
                {
                    slot.gameObject.SetActive(true);
                    string cid = setEntry.CardIds[i];
                    states.TryGetValue(cid, out var prog);
                    bool unlocked = prog != null && prog.Unlocked;
                    if (!unlocked) allOwned = false;

                    int cardIndex  = 1;
                    CardEntry displayCard = null;
                    if (catalog != null)
                    {
                        for (int c = 0; c < catalog.Count; c++)
                        {
                            if (catalog[c].Id == cid) { displayCard = catalog[c]; cardIndex = c + 1; break; }
                        }
                    }
                    if (displayCard != null) slot.SetData(displayCard, cardIndex, prog, gm, OnSlotClickedById);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            var descTx = FindDescriptionText(pageTf);
            if (descTx != null)
            {
                descTx.gameObject.SetActive(true);
                descTx.text = string.IsNullOrWhiteSpace(setEntry.EffectDesc) ? "아무 효과 없음" : setEntry.EffectDesc;
            }

            bool claimed = gm.IsSetRewardClaimed(setEntry.SetId);
            var btnTf    = pageTf.Find("ClaimBtn");
            Button claimBtn  = null;
            TMP_Text claimTx = null;

            if (btnTf != null)
            {
                btnTf.gameObject.SetActive(true);
                claimBtn = btnTf.GetComponent<Button>();
                claimTx  = btnTf.GetComponentInChildren<TMP_Text>();
                if (claimBtn != null)
                {
                    claimBtn.onClick.RemoveAllListeners();
                    claimBtn.onClick.AddListener(() => { gm.ClaimSetReward(setEntry.SetId); RefreshSets(); });
                }
            }

            if (claimTx  != null) claimTx.text = claimed ? "완료" : "보상 받기";
            if (claimBtn != null)
            {
                SetButtonInteractable(claimBtn, !claimed && allOwned);
                var img = claimBtn.GetComponent<Image>();
                if (img != null)
                    img.color = claimed ? new Color(0.2f, 0.45f, 0.2f)
                              : allOwned ? new Color(0.70f, 0.45f, 0.05f)
                              : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SET PAGE HELPERS
        // ─────────────────────────────────────────────────────────────────────
        public void EnsureSetPageChildrenBuilt()
        {
            if (leftSetPage  != null) PrepareSetPageChildren(leftSetPage.transform);
            if (rightSetPage != null) PrepareSetPageChildren(rightSetPage.transform);
        }

        private void PrepareSetPageChildren(Transform pageTf)
        {
            if (pageTf.Find("SetNameTitle") == null)
            {
                var t = MakeText(pageTf, "세트 이름",
                    new Vector2(0f, 265f), new Vector2(380f, 40f), 16, new Color(0.15f, 0.10f, 0.05f));
                t.gameObject.name = "SetNameTitle";
                t.fontStyle       = FontStyles.Bold;
                t.alignment       = TextAlignmentOptions.Center;
                t.raycastTarget   = false;
                t.gameObject.SetActive(false);
            }
            if (pageTf.Find("ClaimBtn") == null)
            {
                var go = MakeButton(pageTf, "보상 받기",
                    new Vector2(0f, -220f), new Vector2(180f, 44f),
                    new Color(0.3f, 0.3f, 0.3f), () => { });
                go.name = "ClaimBtn";
                go.SetActive(false);
            }
        }

        private TMP_Text FindDescriptionText(Transform pageTf)
        {
            for (int k = 0; k < pageTf.childCount; k++)
            {
                var child = pageTf.GetChild(k);
                string n = child.name;
                if (n == "SetNameTitle" || n.StartsWith("Slot_") || n == "ClaimBtn" || n == "BlankPageText") continue;
                var tx = child.GetComponent<TMP_Text>();
                if (tx != null) return tx;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AUTO-WIRE (scene-placed prefab support)
        // ─────────────────────────────────────────────────────────────────────
        private void AutoWireFields()
        {
            // Font from any already-wired TMP text
            if (defaultFont == null && pageLabel     != null) defaultFont = pageLabel.font;
            if (defaultFont == null && detailCardName != null) defaultFont = detailCardName.font;
            if (defaultFont == null && setPageLabel   != null) defaultFont = setPageLabel.font;

            // noPanel / setPanel
            if (noPanel  == null) { var t = FindChildByNameRecursive(transform, "NoPanel")  ?? FindChildByNameRecursive(transform, "NoTabRoot");  if (t != null) noPanel  = t.gameObject; }
            if (setPanel == null) { var t = FindChildByNameRecursive(transform, "SetPanel") ?? FindChildByNameRecursive(transform, "SetTabRoot"); if (t != null) setPanel = t.gameObject; }

            // Tab row buttons
            if (tabNoBtn  == null) { var t = FindChildByNameRecursive(transform, "Btn_No.") ?? FindChildByNameRecursive(transform, "tabNoBtn");  if (t != null) tabNoBtn  = t.gameObject; }
            if (tabSetBtn == null) { var t = FindChildByNameRecursive(transform, "Btn_Set") ?? FindChildByNameRecursive(transform, "tabSetBtn"); if (t != null) tabSetBtn = t.gameObject; }
            if (closeBtn  == null) { var t = FindChildByNameRecursive(transform, "Btn_✕")  ?? FindChildByNameRecursive(transform, "closeBtn");  if (t != null) closeBtn  = t.gameObject; }

            // No tab navigation
            if (noPanel != null)
            {
                var noPanelTf = noPanel.transform;
                if (prevPageBtn == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_◀"); if (t != null) prevPageBtn = t.gameObject; }
                if (nextPageBtn == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_▶"); if (t != null) nextPageBtn = t.gameObject; }
                if (pageLabel   == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "pageLabel");
                    if (t != null) pageLabel = t.GetComponent<TMP_Text>();
                }

                // Filter tabs
                if (filterTabAll == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_전체"); if (t != null) filterTabAll = t.gameObject; }
                if (filterTabN   == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_N");   if (t != null) filterTabN   = t.gameObject; }
                if (filterTabR   == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_R");   if (t != null) filterTabR   = t.gameObject; }
                if (filterTabSR  == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_SR");  if (t != null) filterTabSR  = t.gameObject; }
                if (filterTabSSR == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_SSR"); if (t != null) filterTabSSR = t.gameObject; }
                if (filterTabUR  == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_UR");  if (t != null) filterTabUR  = t.gameObject; }

                // Detail panel
                if (detailPanel == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "DetailPanel");
                    if (t != null) detailPanel = t.gameObject;
                }
                if (detailPanel != null)
                {
                    var rt = detailPanel.transform;
                    if (detailCardArt       == null) { var t = FindChildByNameRecursive(rt, "DetailArt");    if (t != null) detailCardArt       = t.GetComponent<Image>(); }
                    if (detailCardName      == null) { var t = FindChildByNameRecursive(rt, "DetailName");   if (t != null) detailCardName      = t.GetComponent<TMP_Text>(); }
                    if (detailRarityBadge   == null) { var t = FindChildByNameRecursive(rt, "RarityBadge");  if (t != null) detailRarityBadge   = t.GetComponent<TMP_Text>(); }
                    if (detailDescription   == null) { var t = FindChildByNameRecursive(rt, "DetailDesc");   if (t != null) detailDescription   = t.GetComponent<TMP_Text>(); }
                    if (detailIncomeText    == null) { var t = FindChildByNameRecursive(rt, "IncomeText");   if (t != null) detailIncomeText    = t.GetComponent<TMP_Text>(); }
                    if (detailBreakthroughText == null) { var t = FindChildByNameRecursive(rt, "BreakText"); if (t != null) detailBreakthroughText = t.GetComponent<TMP_Text>(); }
                    if (detailEquipBtn      == null) { var t = FindChildByNameRecursive(rt, "Btn_대표 설정"); if (t != null) detailEquipBtn      = t.GetComponent<Button>(); }
                    if (detailBreakthroughBtn == null) { var t = FindChildByNameRecursive(rt, "Btn_한계 돌파"); if (t != null) detailBreakthroughBtn = t.GetComponent<Button>(); }
                }
            }

            // Set tab sub-elements
            if (setPanel != null)
            {
                var setRootTf = setPanel.transform;
                if (leftSetPage  == null) { var t = setRootTf.Find("LeftPage")  ?? FindChildByNameRecursive(setRootTf, "LeftPage");  if (t != null) leftSetPage  = t.gameObject; }
                if (rightSetPage == null) { var t = setRootTf.Find("RightPage") ?? FindChildByNameRecursive(setRootTf, "RightPage"); if (t != null) rightSetPage = t.gameObject; }
                if (prevSetPageBtn == null) { var t = FindChildByNameRecursive(setRootTf, "Btn_◀"); if (t != null) prevSetPageBtn = t.gameObject; }
                if (nextSetPageBtn == null) { var t = FindChildByNameRecursive(setRootTf, "Btn_▶"); if (t != null) nextSetPageBtn = t.gameObject; }
                if (setPageLabel   == null)
                {
                    var t = FindChildByNameRecursive(setRootTf, "setPageLabel");
                    if (t != null) setPageLabel = t.GetComponent<TMP_Text>();
                }

                leftSetSlots.Clear();
                if (leftSetPage != null)
                {
                    var arr = leftSetPage.GetComponentsInChildren<CardSlotUI>(true);
                    leftSetSlots.AddRange(arr);
                }
                rightSetSlots.Clear();
                if (rightSetPage != null)
                {
                    var arr = rightSetPage.GetComponentsInChildren<CardSlotUI>(true);
                    rightSetSlots.AddRange(arr);
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CANVAS ENSURE
        // ─────────────────────────────────────────────────────────────────────
        private void EnsureParentedToCanvas()
        {
            if (GetComponentInParent<Canvas>() == null)
            {
                var c = FindObjectOfType<Canvas>();
                if (c != null) transform.SetParent(c.transform, false);
            }
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
            rt.localScale = Vector3.one;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI FACTORY HELPERS
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject MakePage(Transform parent, Vector2 pos, Vector2 size, Color col)
        {
            var go = new GameObject("Page");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            go.AddComponent<Image>().color = col;
            return go;
        }

        private static RectTransform MakeEmptyRT(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;
            return rt;
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
            tx.text     = text;
            tx.fontSize = fontSize;
            tx.color    = col;
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

            var lbl = new GameObject("Label");
            lbl.transform.SetParent(go.transform, false);
            var lrt = lbl.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tx = lbl.AddComponent<TextMeshProUGUI>();
            if (defaultFont != null) tx.font = defaultFont;
            tx.text      = label;
            tx.fontSize  = Mathf.Clamp((int)(size.y * 0.45f), 10, 22);
            tx.alignment = TextAlignmentOptions.Center;
            tx.color     = Color.white;

            return go;
        }

        private static void SetBtnColor(GameObject go, Color col)
        {
            if (go == null) return;
            var img = go.GetComponent<Image>();
            if (img != null) img.color = col;
        }

        private static void SetButtonInteractable(Button btn, bool interactable)
        {
            if (btn == null) return;
            btn.interactable = interactable;

            // CanvasGroup 방식: 없으면 추가, 그래도 실패하면 Image alpha로 대체
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = interactable ? 1f : 0.4f;
                return;
            }

            // CanvasGroup이 없는 경우 Image 색상 alpha로 대체
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                var c = img.color;
                img.color = new Color(c.r, c.g, c.b, interactable ? 1f : 0.4f);
            }
        }

        private static void SetButtonSprite(GameObject go, Sprite sp)
        {
            if (go == null || sp == null) return;
            var img = go.GetComponent<Image>();
            if (img == null) return;
            img.sprite = sp;
            img.type   = Image.Type.Sliced;
        }

        private static void ApplyTabLabelColor(GameObject tabGO, Color col)
        {
            if (tabGO == null) return;
            var tx = tabGO.GetComponentInChildren<TMP_Text>();
            if (tx != null) tx.color = col;
        }

        private static Color32 RarityToColor(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.R:   return ColR;
                case CardRarity.SR:  return ColSR;
                case CardRarity.SSR: return ColSSR;
                case CardRarity.UR:  return ColUR;
                default:             return ColN;
            }
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

        // ── Inner types ──────────────────────────────────────────────────────
        private struct SlotBundle
        {
            public GameObject go;
            public CardSlotUI ui;
        }
    }
}
