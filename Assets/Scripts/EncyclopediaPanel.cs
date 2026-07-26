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
        [SerializeField] private Image       detailCardFrameImage;
        [SerializeField] private Image       detailCardArt;
        [SerializeField] private TMP_Text    detailCardName;
        [SerializeField] private TMP_Text    detailRarityBadge;
        [SerializeField] private TMP_Text    detailDescription;
        [SerializeField] private TMP_Text    detailIncomeText;
        [SerializeField] private TMP_Text    detailBreakthroughText;
        [SerializeField] private Button      detailEquipBtn;
        [SerializeField] private Button      detailBreakthroughBtn;

        // NumBtn (breakthrough illustration stage buttons 1..5)
        [SerializeField] private GameObject numBtnContainer;
        [SerializeField] private GameObject[] stageObjects = new GameObject[5];  // 1~5 오브젝트 (인스펙터에서 연결)
        private int    selectedIllustrationStage = 1;
        private string lastDetailCardId          = null;

        // Set tab elements
        [SerializeField] private GameObject  leftSetPage;
        [SerializeField] private GameObject  rightSetPage;   // kept for backward compat; hidden in new layout
        [SerializeField] private GameObject  prevSetPageBtn;
        [SerializeField] private GameObject  nextSetPageBtn;
        [SerializeField] private TMP_Text    setPageLabel;
        private int setPageIndex = 0;

        // Panel title (EncyclopediaPanelTitle)
        [SerializeField] private TMP_Text    panelTitle;

        // Set tab selection state
        private int    selectedSetSlotIndex = -1;
        private SetEntry currentSetEntry    = null;

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

        [Header("Rarity Mark Sprites (assign in Inspector)")]
        [SerializeField] private Sprite spriteMarkN;
        [SerializeField] private Sprite spriteMarkR;
        [SerializeField] private Sprite spriteMarkSR;
        [SerializeField] private Sprite spriteMarkSSR;
        [SerializeField] private Sprite spriteMarkUR;

        [SerializeField] private Sprite spriteCollectionCounter;
        [SerializeField] private Sprite spriteBtnRepresentative;
        [SerializeField] private Sprite spriteTitleCatCodex;
        [SerializeField] private Sprite spriteBtnPageLeft;
        [SerializeField] private Sprite spriteBtnPageRight;

        [SerializeField] private Image  detailCardRarityMark;

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

            // Replicate Slot_0's locktext component to all other card slots in the encyclopedia panel at runtime
            ReplicateLockTextToAllSlots();

            // Re-initialize/ensure init for all CardSlotUIs so they bind their lockText field
            var allSlots = GetComponentsInChildren<CardSlotUI>(true);
            foreach (var slot in allSlots)
            {
                if (slot != null)
                {
                    // Reset initialized flag to force search of newly cloned components
                    var field = slot.GetType().GetField("initialized", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(slot, false);
                    slot.EnsureInit();
                }
            }

            // ── Persistent state across close/reopen during game session ────────
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null) gm.StateChanged += OnStateChanged;

            if (!_hasSavedState)
            {
                showNoTab            = true;
                currentPageIdx       = 0;
                currentFilter        = RarityFilter.All;
                selectedCardId       = null;
                setPageIndex         = 0;
                selectedSetSlotIndex = -1;
                _hasSavedState       = true;
            }
            else
            {
                showNoTab            = _savedShowNoTab;
                currentPageIdx       = _savedCurrentPageIdx;
                currentFilter        = _savedCurrentFilter;
                selectedCardId       = _savedSelectedCardId;
                setPageIndex         = _savedSetPageIndex;
                selectedSetSlotIndex = _savedSelectedSetSlotIndex;
            }

            if (showNoTab) ShowNoTab();
            else           ShowSetTab();
        }

        private static bool         _hasSavedState             = false;
        private static bool         _savedShowNoTab            = true;
        private static int          _savedCurrentPageIdx       = 0;
        private static RarityFilter _savedCurrentFilter        = RarityFilter.All;
        private static string       _savedSelectedCardId       = null;
        private static int          _savedSetPageIndex         = 0;
        private static int          _savedSelectedSetSlotIndex = -1;

        private void SavePanelState()
        {
            _hasSavedState             = true;
            _savedShowNoTab            = showNoTab;
            _savedCurrentPageIdx       = currentPageIdx;
            _savedCurrentFilter        = currentFilter;
            _savedSelectedCardId       = selectedCardId;
            _savedSetPageIndex         = setPageIndex;
            _savedSelectedSetSlotIndex = selectedSetSlotIndex;
        }

        private void OnDisable()
        {
            SavePanelState();
            if (gm != null) gm.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged()
        {
            if (showNoTab) RefreshNoTab();
            else           RefreshSets();
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

        [ContextMenu("Slot_0 구조를 모든 슬롯에 복사 (에디터 전용)")]
        public void ReplicateSlot0StructureInEditor()
        {
#if UNITY_EDITOR
            if (noPanel == null) AutoWireFields();
            if (noPanel == null)
            {
                Debug.LogError("[EncyclopediaPanel] noPanel is null. Build or wire UI first.");
                return;
            }

            var childSlots = noPanel.GetComponentsInChildren<CardSlotUI>(true);
            if (childSlots == null || childSlots.Length == 0) return;

            // Find slot0
            CardSlotUI slot0 = null;
            foreach (var slot in childSlots)
            {
                if (slot.name == "Slot_0" || slot.name == "Slot_1" || slot.name.Contains("0"))
                {
                    slot0 = slot;
                    break;
                }
            }
            if (slot0 == null) slot0 = childSlots[0];

            Debug.Log($"[EncyclopediaPanel] Replicating structure of {slot0.name} to all other slots...");

            // Get list of children of slot0 to clone
            var childrenToClone = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in slot0.transform)
            {
                childrenToClone.Add(child.gameObject);
            }

            // Gather all slots (noPanel + setPanel)
            var allSlots = GetComponentsInChildren<CardSlotUI>(true);
            int count = 0;

            foreach (var slot in allSlots)
            {
                if (slot == slot0) continue;

                // 1. Delete all existing children of the target slot
                var childrenToDelete = new System.Collections.Generic.List<GameObject>();
                foreach (Transform child in slot.transform)
                {
                    childrenToDelete.Add(child.gameObject);
                }
                foreach (var child in childrenToDelete)
                {
                    DestroyImmediate(child);
                }

                // 2. Clone children from slot0
                foreach (var templateChild in childrenToClone)
                {
                    var clonedChild = Instantiate(templateChild, slot.transform);
                    clonedChild.name = templateChild.name;

                    // Sync RectTransform values
                    var clonedRT = clonedChild.GetComponent<RectTransform>();
                    var templateRT = templateChild.GetComponent<RectTransform>();
                    if (clonedRT != null && templateRT != null)
                    {
                        clonedRT.anchorMin = templateRT.anchorMin;
                        clonedRT.anchorMax = templateRT.anchorMax;
                        clonedRT.pivot = templateRT.pivot;
                        clonedRT.anchoredPosition = templateRT.anchoredPosition;
                        clonedRT.sizeDelta = templateRT.sizeDelta;
                        clonedRT.localScale = templateRT.localScale;
                        clonedRT.localRotation = templateRT.localRotation;
                    }
                }

                // 3. Auto-wire the new child fields of CardSlotUI and save
                slot.EditorWireFields();
                UnityEditor.EditorUtility.SetDirty(slot.gameObject);
                count++;
            }

            // Mark scene dirty and save
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[EncyclopediaPanel] Successfully replicated {slot0.name} to {count} other slots! Save the scene (Ctrl+S) now.");
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

        // ─── Set panel: refresh references & bind buttons each time tab opens ──
        private void AutoWireSetPanel()
        {
            if (setPanel == null) return;
            var setRootTf = setPanel.transform;

            // Find leftSetPage by common names, then by CardSlotUI presence
            if (leftSetPage == null)
            {
                var t = setRootTf.Find("LeftPage")
                     ?? setRootTf.Find("leftSlots")    // user-named
                     ?? setRootTf.Find("Page")
                     ?? setRootTf.Find("ContentPage")
                     ?? setRootTf.Find("SlotPage")
                     ?? FindChildByNameRecursive(setRootTf, "LeftPage")
                     ?? FindChildByNameRecursive(setRootTf, "leftSlots");

                if (t != null)
                {
                    leftSetPage = t.gameObject;
                    Debug.Log($"[EncyclopediaPanel] AutoWireSetPanel: leftSetPage → '{leftSetPage.name}'");
                }
                else
                {
                    // Fallback: first child that has CardSlotUI descendants
                    for (int ci = 0; ci < setRootTf.childCount; ci++)
                    {
                        var child = setRootTf.GetChild(ci);
                        if (child.GetComponentsInChildren<CardSlotUI>(true).Length > 0)
                        {
                            leftSetPage = child.gameObject;
                            Debug.Log($"[EncyclopediaPanel] AutoWireSetPanel: leftSetPage fallback → '{child.name}'");
                            break;
                        }
                    }
                    // Second fallback: use setPanel itself
                    if (leftSetPage == null && setPanel.GetComponentsInChildren<CardSlotUI>(true).Length > 0)
                    {
                        leftSetPage = setPanel;
                        Debug.Log("[EncyclopediaPanel] AutoWireSetPanel: leftSetPage fallback → setPanel itself");
                    }
                }
            }

            if (rightSetPage == null)
            {
                var t = setRootTf.Find("RightPage")
                     ?? setRootTf.Find("rightSlots")   // user-named
                     ?? FindChildByNameRecursive(setRootTf, "RightPage");
                if (t != null) rightSetPage = t.gameObject;
            }

            // Prev / Next buttons
            if (prevSetPageBtn == null)
            {
                var t = FindChildByNameRecursive(setRootTf, "Btn_◀")
                     ?? FindChildByNameRecursive(setRootTf, "LeftBtn")
                     ?? FindChildByNameRecursive(setRootTf, "PrevBtn")
                     ?? FindChildByNameRecursive(setRootTf, "Btn_Left");
                if (t != null) prevSetPageBtn = t.gameObject;
            }
            if (nextSetPageBtn == null)
            {
                var t = FindChildByNameRecursive(setRootTf, "Btn_▶")
                     ?? FindChildByNameRecursive(setRootTf, "RightBtn")
                     ?? FindChildByNameRecursive(setRootTf, "NextBtn")
                     ?? FindChildByNameRecursive(setRootTf, "Btn_Right");
                if (t != null) nextSetPageBtn = t.gameObject;
            }

            // PageText
            if (setPageLabel == null)
            {
                var t = FindChildByNameRecursive(setRootTf, "PageText")
                     ?? FindChildByNameRecursive(setRootTf, "setPageLabel")
                     ?? FindChildByNameRecursive(setRootTf, "pageLabel");
                if (t != null) setPageLabel = t.GetComponent<TMP_Text>();
            }

            // Rebuild slot pools (BOTH left AND right pages!)
            leftSetSlots.Clear();
            if (leftSetPage != null)
            {
                leftSetSlots.AddRange(leftSetPage.GetComponentsInChildren<CardSlotUI>(true));
                Debug.Log($"[EncyclopediaPanel] AutoWireSetPanel: leftSetSlots found {leftSetSlots.Count} under '{leftSetPage.name}'");
            }

            rightSetSlots.Clear();
            if (rightSetPage != null)
            {
                rightSetSlots.AddRange(rightSetPage.GetComponentsInChildren<CardSlotUI>(true));
                Debug.Log($"[EncyclopediaPanel] AutoWireSetPanel: rightSetSlots found {rightSetSlots.Count} under '{rightSetPage.name}'");
            }
            else
            {
                var rPage = setRootTf.Find("rightSlots") ?? setRootTf.Find("RightPage") ?? FindChildByNameRecursive(setRootTf, "rightSlots");
                if (rPage != null)
                {
                    rightSetPage = rPage.gameObject;
                    rightSetSlots.AddRange(rightSetPage.GetComponentsInChildren<CardSlotUI>(true));
                    Debug.Log($"[EncyclopediaPanel] AutoWireSetPanel: rightSetSlots fallback found {rightSetSlots.Count} under '{rightSetPage.name}'");
                }
            }
        }

        private void BindSetButtons()
        {
            BindBtn(prevSetPageBtn, () => ChangeSetPage(-1));
            BindBtn(nextSetPageBtn, () => ChangeSetPage(1));
        }


        private static void BindBtn(GameObject go, UnityEngine.Events.UnityAction action)
        {
            if (go == null) return;
            var btn = go.GetComponent<Button>();
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        private void SetPanelTitleText(string newText)
        {
            Debug.Log($"[EncyclopediaPanel] SetPanelTitleText -> '{newText}'");

            if (panelTitle != null)
            {
                panelTitle.text = newText;
            }

            var candidateRoots = new List<Transform> { transform };
            if (transform.parent != null) candidateRoots.Add(transform.parent);

            foreach (var root in candidateRoots)
            {
                // Direct search for the exact text child object named EncyclopediaPanelTitle_Text
                var textObj = FindChildByNameRecursive(root, "EncyclopediaPanelTitle_Text")
                           ?? FindChildByNameRecursive(root, "EncyclopediaPanelTitle_text")
                           ?? FindChildByNameRecursive(root, "PanelTitle_Text")
                           ?? FindChildByNameRecursive(root, "Title_Text");

                if (textObj != null)
                {
                    var tmp = textObj.GetComponent<TMP_Text>();
                    if (tmp != null) { panelTitle = tmp; tmp.text = newText; Debug.Log($"[EncyclopediaPanel] Found panelTitle via exact child -> '{textObj.name}'"); return; }

                    var uiText = textObj.GetComponent<UnityEngine.UI.Text>();
                    if (uiText != null) { uiText.text = newText; Debug.Log($"[EncyclopediaPanel] Found UI.Text via exact child -> '{textObj.name}'"); return; }
                }

                // If textObj not found directly, search container object and check GetComponentInChildren
                var containerObj = FindChildByNameRecursive(root, "EncyclopediaPanelTitle")
                                ?? FindChildByNameRecursive(root, "PanelTitle")
                                ?? FindChildByNameRecursive(root, "TitleText")
                                ?? FindChildByNameRecursive(root, "Title");

                if (containerObj != null)
                {
                    var tmp = containerObj.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null) { panelTitle = tmp; tmp.text = newText; Debug.Log($"[EncyclopediaPanel] Found panelTitle via container child -> '{tmp.name}'"); return; }

                    var uiText = containerObj.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (uiText != null) { uiText.text = newText; Debug.Log($"[EncyclopediaPanel] Found UI.Text via container child -> '{uiText.name}'"); return; }
                }
            }

            Debug.LogWarning($"[EncyclopediaPanel] SetPanelTitleText: Could not find title text component to set '{newText}'");
        }

        private void ShowNoTab()
        {
            showNoTab = true;
            if (noPanel  != null) noPanel.SetActive(true);
            if (setPanel != null) setPanel.SetActive(false);
            SetBtnColor(tabNoBtn,  TabActive);
            SetBtnColor(tabSetBtn, TabInactive);
            SetPanelTitleText("도감");
            UpdateFilterTabColors();
            RefreshNoTab();
        }

        private void ShowSetTab()
        {
            showNoTab = false;
            if (noPanel  != null) noPanel.SetActive(false);
            if (setPanel != null) setPanel.SetActive(true);
            SetBtnColor(tabNoBtn,  TabInactive);
            SetBtnColor(tabSetBtn, TabActive);

            // Re-wire every time in case hierarchy changed since first OnEnable
            AutoWireSetPanel();
            BindSetButtons();

            RefreshSets();
        }

        private void OnClose()
        {
            SavePanelState();
            gameObject.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  FILTER
        // ─────────────────────────────────────────────────────────────────────
        private static readonly Color FilterTabSelected   = new Color(44f / 255f, 44f / 255f, 44f / 255f, 1.00f);
        private static readonly Color FilterTabUnselected = Color.white;

        private void SetFilter(RarityFilter f)
        {
            currentFilter  = f;
            currentPageIdx = 0;
            selectedCardId = null;
            UpdateFilterTabColors();
            RefreshNoTab();
        }

        private void UpdateFilterTabColors()
        {
            SetBtnColor(filterTabAll, currentFilter == RarityFilter.All ? FilterTabSelected : FilterTabUnselected);
            SetBtnColor(filterTabN,   currentFilter == RarityFilter.N   ? FilterTabSelected : FilterTabUnselected);
            SetBtnColor(filterTabR,   currentFilter == RarityFilter.R   ? FilterTabSelected : FilterTabUnselected);
            SetBtnColor(filterTabSR,  currentFilter == RarityFilter.SR  ? FilterTabSelected : FilterTabUnselected);
            SetBtnColor(filterTabSSR, currentFilter == RarityFilter.SSR ? FilterTabSelected : FilterTabUnselected);
            SetBtnColor(filterTabUR,  currentFilter == RarityFilter.UR  ? FilterTabSelected : FilterTabUnselected);
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

            string targetPageText = $"{currentPageIdx + 1} / {maxPage + 1}";
            if (pageLabel != null && pageLabel.text != targetPageText) pageLabel.text = targetPageText;

            // Update collection counter with total (non-hidden) collected
            UpdateCollectionCounter();

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
                    Sprite frameSp = GetFrameSpriteForRarity(card.Rarity);
                    Sprite markSp  = GetMarkSpriteForRarity(card.Rarity);
                    slot.ui.SetSprites(frameSp, spriteCardLocked, markSp);
                    slot.ui.SetData(card, FindOriginalIndex(card, allCards), prog, gm, OnSlotClickedById);
                }
                else
                {
                    slot.go.SetActive(false);
                }
            }

            // Default select first card (e.g. ID 1 card) on initial open or filter change
            if (filteredCards.Count > 0)
            {
                if (string.IsNullOrEmpty(selectedCardId) || !filteredCards.Exists(c => c.Id == selectedCardId))
                {
                    selectedCardId = filteredCards[0].Id;
                }
            }

            // Refresh detail panel for the selected card
            if (!string.IsNullOrEmpty(selectedCardId))
                RefreshDetailPanel(selectedCardId);
            else
                SetDetailEmpty();
        }

        private string _lastCollectionCounterText = null;

        private void UpdateCollectionCounter()
        {
            if (gm == null) return;
            var allCards = gm.CardCatalog?.Cards;
            if (allCards == null) return;

            int totalNonHidden    = 0;
            int unlockedNonHidden = 0;
            var states = gm.GetCardStates();
            foreach (var c in allCards)
            {
                if (c.IsHidden) continue;
                totalNonHidden++;
                states.TryGetValue(c.Id, out var p);
                if (p != null && p.Unlocked) unlockedNonHidden++;
            }

            string textValue = $"{unlockedNonHidden} / {totalNonHidden}";

            if (_lastCollectionCounterText == textValue && collectionCounterLabel != null) return;
            _lastCollectionCounterText = textValue;

            if (collectionCounterLabel != null)
            {
                collectionCounterLabel.text = textValue;
            }

            var candidateRoots = new List<Transform>();
            if (noPanel != null) candidateRoots.Add(noPanel.transform);
            candidateRoots.Add(transform);
            if (transform.parent != null) candidateRoots.Add(transform.parent);

            foreach (var root in candidateRoots)
            {
                var t = FindChildByNameRecursive(root, "Collection_counter")
                     ?? FindChildByNameRecursive(root, "collection_counter")
                     ?? FindChildByNameRecursive(root, "CollectionCounter")
                     ?? FindChildByNameRecursive(root, "collectionCounter")
                     ?? FindChildByNameRecursive(root, "Collection_Counter")
                     ?? FindChildByNameRecursive(root, "CollectionText");

                if (t != null)
                {
                    var tmp = t.GetComponent<TMP_Text>() ?? t.GetComponentInChildren<TMP_Text>(true);
                    if (tmp != null)
                    {
                        collectionCounterLabel = tmp;
                        tmp.text = textValue;
                        return;
                    }

                    var uiText = t.GetComponent<UnityEngine.UI.Text>() ?? t.GetComponentInChildren<UnityEngine.UI.Text>(true);
                    if (uiText != null)
                    {
                        uiText.text = textValue;
                        return;
                    }
                }
            }
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
            if (detailCardFrameImage != null) { detailCardFrameImage.sprite = spriteCardLocked; detailCardFrameImage.color = Color.white; }
            if (detailCardArt       != null) { detailCardArt.sprite = null; detailCardArt.color = new Color(0, 0, 0, 0); detailCardArt.gameObject.SetActive(false); }
            if (detailCardRarityMark != null) { detailCardRarityMark.sprite = null; detailCardRarityMark.color = new Color(0, 0, 0, 0); detailCardRarityMark.gameObject.SetActive(false); }
            if (detailCardName      != null) detailCardName.text      = "???";
            if (detailRarityBadge   != null) detailRarityBadge.text   = "";
            if (detailDescription   != null) detailDescription.text   = "???";
            if (detailIncomeText    != null) detailIncomeText.text     = "";
            if (detailBreakthroughText != null) detailBreakthroughText.text = "";
            if (detailEquipBtn      != null) { SetButtonInteractable(detailEquipBtn, false); }
            if (detailBreakthroughBtn != null)
            {
                detailBreakthroughBtn.gameObject.SetActive(true);
                SetButtonInteractable(detailBreakthroughBtn, false);
                var lbl = detailBreakthroughBtn.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = "한계 돌파";
            }
        }

        private void RefreshDetailPanel(string cardId)
        {
            if (detailPanel == null && noPanel == null) return;

            var card = gm?.CardCatalog?.FindById(cardId);
            var states = gm?.GetCardStates();
            CardProgress prog = null;
            states?.TryGetValue(cardId, out prog);
            bool unlocked = prog != null && prog.Unlocked;

            var searchRoot = (detailPanel ?? noPanel)?.transform;

            // ── 0. If detailPanel contains a CardSlotUI component (e.g. Slot_Detail) ─────
            var detailSlotUI = detailPanel != null ? detailPanel.GetComponentInChildren<CardSlotUI>(true) : null;
            if (detailSlotUI != null)
            {
                Sprite frameSp = unlocked && card != null ? GetFrameSpriteForRarity(card.Rarity) : spriteCardLocked;
                Sprite markSp  = unlocked && card != null ? GetMarkSpriteForRarity(card.Rarity) : null;
                detailSlotUI.SetSprites(frameSp, spriteCardLocked, markSp);
                if (card != null)
                {
                    var allCards = gm?.CardCatalog?.Cards;
                    int cardIndex = allCards != null ? FindOriginalIndex(card, allCards) : 1;
                    detailSlotUI.SetData(card, cardIndex, prog, gm, null);
                }
            }

            // ── 0-1. Lock/Unknown overlay on detail panel ─────────────────────
            if (searchRoot != null)
            {
                var lockObj = FindChildByNameRecursive(searchRoot, "LOCKTEXT")
                           ?? FindChildByNameRecursive(searchRoot, "LockText")
                           ?? FindChildByNameRecursive(searchRoot, "Lock")
                           ?? FindChildByNameRecursive(searchRoot, "lock")
                           ?? FindChildByNameRecursive(searchRoot, "Unknown")
                           ?? FindChildByNameRecursive(searchRoot, "unkGO")
                           ?? FindChildByNameRecursive(searchRoot, "unkImg");
                if (lockObj != null)
                {
                    lockObj.gameObject.SetActive(!unlocked);
                }
            }

            // ── 1. Frame Image ────────────────────────────────────────────────
            if (detailCardFrameImage == null && searchRoot != null)
            {
                var fr = FindChildByNameRecursive(searchRoot, "Frame") ?? FindChildByNameRecursive(searchRoot, "frame") ?? FindChildByNameRecursive(searchRoot, "DetailFrame");
                if (fr != null) detailCardFrameImage = fr.GetComponent<Image>();
            }

            if (detailCardFrameImage != null)
            {
                if (!detailCardFrameImage.gameObject.activeSelf) detailCardFrameImage.gameObject.SetActive(true);
                if (unlocked && card != null)
                {
                    Sprite frameSp = GetFrameSpriteForRarity(card.Rarity);
                    if (frameSp != null)
                    {
                        if (detailCardFrameImage.sprite != frameSp) detailCardFrameImage.sprite = frameSp;
                        if (detailCardFrameImage.color != Color.white) detailCardFrameImage.color  = Color.white;
                        if (detailCardFrameImage.type != Image.Type.Sliced) detailCardFrameImage.type   = Image.Type.Sliced;
                    }
                    else
                    {
                        Color targetCol = (Color)RarityToColor(card.Rarity);
                        if (detailCardFrameImage.sprite != null) detailCardFrameImage.sprite = null;
                        if (detailCardFrameImage.color != targetCol) detailCardFrameImage.color  = targetCol;
                    }
                }
                else
                {
                    // Locked -> lock frame sprite
                    if (spriteCardLocked != null)
                    {
                        if (detailCardFrameImage.sprite != spriteCardLocked) detailCardFrameImage.sprite = spriteCardLocked;
                        if (detailCardFrameImage.color != Color.white) detailCardFrameImage.color  = Color.white;
                        if (detailCardFrameImage.type != Image.Type.Sliced) detailCardFrameImage.type   = Image.Type.Sliced;
                    }
                    else
                    {
                        Color targetCol = new Color(0.18f, 0.14f, 0.10f, 1f);
                        if (detailCardFrameImage.sprite != null) detailCardFrameImage.sprite = null;
                        if (detailCardFrameImage.color != targetCol) detailCardFrameImage.color  = targetCol;
                    }
                }
            }

            // Fallback dynamically if name, description, or art were not wired yet
            if (detailCardName == null && searchRoot != null)
            {
                var t = FindChildByNameRecursive(searchRoot, "ImageNametxt")
                     ?? FindChildByNameRecursive(searchRoot, "imageNametxt")
                     ?? FindChildByNameRecursive(searchRoot, "ImageNameTxt")
                     ?? FindChildByNameRecursive(searchRoot, "ImageName")
                     ?? FindChildByNameRecursive(searchRoot, "DetailName")
                     ?? FindChildByNameRecursive(searchRoot, "CardName");
                if (t != null) detailCardName = t.GetComponent<TMP_Text>();
            }

            if (detailCardArt == null && searchRoot != null)
            {
                var ar = FindChildByNameRecursive(searchRoot, "Art")
                      ?? FindChildByNameRecursive(searchRoot, "art")
                      ?? FindChildByNameRecursive(searchRoot, "DetailArt")
                      ?? FindChildByNameRecursive(searchRoot, "CardArt")
                      ?? FindChildByNameRecursive(searchRoot, "Image")
                      ?? FindChildByNameRecursive(searchRoot, "image");
                if (ar != null) detailCardArt = ar.GetComponent<Image>();
            }

            if (detailDescription == null && searchRoot != null)
            {
                var descBgTf = FindChildByNameRecursive(searchRoot, "DescBG")
                            ?? FindChildByNameRecursive(searchRoot, "descBG");
                if (descBgTf != null) detailDescription = descBgTf.GetComponentInChildren<TMP_Text>(true);
                if (detailDescription == null)
                {
                    var t = FindChildByNameRecursive(searchRoot, "description")
                         ?? FindChildByNameRecursive(searchRoot, "Description");
                    if (t != null) detailDescription = t.GetComponent<TMP_Text>();
                }
            }

            // Card name
            if (detailCardName != null)
            {
                if (!detailCardName.gameObject.activeSelf) detailCardName.gameObject.SetActive(true);
                string targetName = unlocked && card != null ? card.DisplayName : "???";
                if (detailCardName.text != targetName) detailCardName.text = targetName;
            }

            // Rarity badge
            if (detailRarityBadge != null)
            {
                if (detailRarityBadge.gameObject.activeSelf != unlocked) detailRarityBadge.gameObject.SetActive(unlocked);
                if (card != null)
                {
                    string targetBadge = unlocked ? card.Rarity.ToString() : "?";
                    if (detailRarityBadge.text != targetBadge) detailRarityBadge.text = targetBadge;
                    Color targetColor = unlocked ? (Color)RarityToColor(card.Rarity) : Color.gray;
                    if (detailRarityBadge.color != targetColor) detailRarityBadge.color = targetColor;
                }
            }

            // Rarity mark (rereMark Image)
            if (detailCardRarityMark == null && searchRoot != null)
            {
                var rm = FindChildByNameRecursive(searchRoot, "rereMark")
                      ?? FindChildByNameRecursive(searchRoot, "reremark")
                      ?? FindChildByNameRecursive(searchRoot, "rareMark")
                      ?? FindChildByNameRecursive(searchRoot, "raremark")
                      ?? FindChildByNameRecursive(searchRoot, "RarityMark")
                      ?? FindChildByNameRecursive(searchRoot, "rarityMark")
                      ?? FindChildByNameRecursive(searchRoot, "rare_mark")
                      ?? FindChildByNameRecursive(searchRoot, "rarity_mark")
                      ?? FindChildByNameRecursive(searchRoot, "RareMark");
                if (rm != null) detailCardRarityMark = rm.GetComponent<Image>();
            }

            if (detailCardRarityMark != null)
            {
                if (unlocked && card != null)
                {
                    Sprite markSp = GetMarkSpriteForRarity(card.Rarity);
                    if (markSp != null)
                    {
                        if (!detailCardRarityMark.gameObject.activeSelf) detailCardRarityMark.gameObject.SetActive(true);
                        if (detailCardRarityMark.sprite != markSp) detailCardRarityMark.sprite = markSp;
                        if (detailCardRarityMark.color != Color.white) detailCardRarityMark.color  = Color.white;
                    }
                    else
                    {
                        if (detailCardRarityMark.sprite != null) detailCardRarityMark.sprite = null;
                        if (detailCardRarityMark.color != new Color(0, 0, 0, 0)) detailCardRarityMark.color  = new Color(0, 0, 0, 0);
                        if (detailCardRarityMark.gameObject.activeSelf) detailCardRarityMark.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (detailCardRarityMark.sprite != null) detailCardRarityMark.sprite = null;
                    if (detailCardRarityMark.color != new Color(0, 0, 0, 0)) detailCardRarityMark.color  = new Color(0, 0, 0, 0);
                    if (detailCardRarityMark.gameObject.activeSelf) detailCardRarityMark.gameObject.SetActive(false);
                }
            }

            // Card art (실제 image)
            if (detailCardArt != null)
            {
                if (unlocked)
                {
                    if (!detailCardArt.gameObject.activeSelf) detailCardArt.gameObject.SetActive(true);
                    Sprite targetSprite = card != null ? card.GetSpriteForStage(selectedIllustrationStage) : null;
                    if (targetSprite == null && card != null) targetSprite = card.CardSprite;

                    if (targetSprite != null)
                    {
                        if (detailCardArt.sprite != targetSprite) detailCardArt.sprite = targetSprite;
                        if (detailCardArt.color != Color.white) detailCardArt.color  = Color.white;
                    }
                    else
                    {
                        if (detailCardArt.color != Color.white) detailCardArt.color  = Color.white;
                    }
                }
                else
                {
                    // Locked -> image 없음 (clear sprite & set inactive)
                    if (detailCardArt.sprite != null) detailCardArt.sprite = null;
                    if (detailCardArt.color != new Color(0, 0, 0, 0)) detailCardArt.color  = new Color(0, 0, 0, 0);
                    if (detailCardArt.gameObject.activeSelf) detailCardArt.gameObject.SetActive(false);
                }
            }

            // NumBtn (illustration variant buttons 1..5)
            if (lastDetailCardId != cardId)
            {
                selectedIllustrationStage = (prog != null && prog.SelectedStage > 0) ? prog.SelectedStage : 1;
                lastDetailCardId = cardId;
            }
            UpdateNumBtnState(card, prog, unlocked);

            // Income stat (Description에 통합되었으므로 별도 항목은 숨김)
            if (detailIncomeText != null)
            {
                detailIncomeText.gameObject.SetActive(false);
            }

            // Breakthrough / enhancement
            if (detailBreakthroughText != null)
            {
                string targetBreakthrough = (unlocked && prog != null)
                    ? $"⭐ 강화   {prog.BreakthroughCount + 1}강 / 5강"
                    : "⭐ 강화   미해금";
                if (detailBreakthroughText.text != targetBreakthrough) detailBreakthroughText.text = targetBreakthrough;
            }

            // Description (No.id \n 설명문 \n 클릭 당 골드 n)
            if (detailDescription != null)
            {
                detailDescription.gameObject.SetActive(true);
                string targetDesc;
                if (unlocked && card != null)
                {
                    double clickGold = gm != null ? gm.GetClickIncome(card, prog) : (card.ClickMultiplier * (1 + (prog != null ? prog.BreakthroughCount : 0)));
                    string goldStr = (clickGold % 1 == 0) ? $"{clickGold:F0}" : $"{clickGold:F1}";
                    targetDesc = $"No.{card.Id}\n{card.GetDescription()}\n클릭 당 골드 {goldStr}";
                }
                else
                {
                    string cardIdStr = card != null ? card.Id : "??";
                    targetDesc = $"No.{cardIdStr}\n???\n클릭 당 골드 ???";
                }

                if (detailDescription.text != targetDesc)
                {
                    detailDescription.text = targetDesc;
                }
            }

            // Equip button
            if (detailEquipBtn != null)
            {
                detailEquipBtn.gameObject.SetActive(true);
                SetButtonInteractable(detailEquipBtn, unlocked);
                bool isEquipped = gm != null && gm.EquippedCardId == cardId;
                var lbl = detailEquipBtn.GetComponentInChildren<TMP_Text>();
                if (lbl != null) lbl.text = isEquipped ? "대표 설정됨" : "대표 설정";
            }

            // Breakthrough button (always active, interactable/translucent when not upgradeable)
            if (detailBreakthroughBtn != null)
            {
                detailBreakthroughBtn.gameObject.SetActive(true);
                bool canUpgrade = false;
                if (unlocked && card != null && prog != null)
                {
                    bool hasUnused  = prog.Copies > prog.BreakthroughCount + 1;
                    canUpgrade = hasUnused && prog.BreakthroughCount < 4;
                    var lbl = detailBreakthroughBtn.GetComponentInChildren<TMP_Text>();
                    if (lbl != null)
                    {
                        if (prog.BreakthroughCount >= 4)
                            lbl.text = "한계 돌파 (최대 5강)";
                        else
                            lbl.text = $"한계 돌파 ({prog.Copies} / {prog.BreakthroughCount + 2} 장)";
                    }
                }
                else
                {
                    var lbl = detailBreakthroughBtn.GetComponentInChildren<TMP_Text>();
                    if (lbl != null) lbl.text = "한계 돌파";
                }
                SetButtonInteractable(detailBreakthroughBtn, canUpgrade);
            }
        }

        private void UpdateNumBtnState(CardEntry card, CardProgress prog, bool unlocked)
        {
            if (numBtnContainer == null) return;

            var validStages    = (unlocked && card != null) ? card.GetBreakthroughStages()
                                                            : new System.Collections.Generic.List<int>();
            int breakthroughCount = (unlocked && prog != null) ? prog.BreakthroughCount : 0;

            bool hasAnyVisible = false;

            for (int s = 1; s <= 5; s++)
            {
                var stageGO = stageObjects[s - 1];
                if (stageGO == null) continue;

                bool isValidStage    = unlocked && card != null && validStages.Contains(s);
                bool isStageUnlocked = unlocked && card != null && breakthroughCount >= (s - 1);
                bool showThis        = isValidStage && isStageUnlocked;

                stageGO.SetActive(showThis);
                if (!showThis) continue;

                hasAnyVisible = true;
                bool isSelected = (s == selectedIllustrationStage);

                // 시각 강조
                var rt = stageGO.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = isSelected ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
                var img = stageGO.GetComponent<Image>();
                if (img != null) img.color = isSelected ? new Color(1f, 0.95f, 0.6f) : Color.white;

                // Button 클릭 처리
                var btn = stageGO.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    int stageNum = s;
                    btn.onClick.AddListener(() =>
                    {
                        selectedIllustrationStage = stageNum;
                        if (card != null && prog != null)
                        {
                            prog.SelectedStage = stageNum;
                            if (gm != null) gm.SetCardSelectedStage(card.Id, stageNum);
                        }
                        UpdateDetailCardArtForStage(card, stageNum);
                        RefreshNoTab();
                        UpdateNumBtnState(card, prog, unlocked);
                    });
                }
            }

            numBtnContainer.SetActive(unlocked && hasAnyVisible);

            if (unlocked && card != null)
                UpdateDetailCardArtForStage(card, selectedIllustrationStage);
        }

        private void UpdateDetailCardArtForStage(CardEntry card, int stage)
        {
            if (card == null) return;
            Sprite stageSprite = card.GetSpriteForStage(stage);
            if (stageSprite == null) stageSprite = card.CardSprite;

            if (detailCardArt != null)
            {
                detailCardArt.gameObject.SetActive(true);
                if (stageSprite != null)
                {
                    detailCardArt.sprite = stageSprite;
                    detailCardArt.color  = Color.white;
                }
            }

            // Also update CardSlotUI inside detailPanel if present (e.g. Slot_Detail)
            var detailSlotUI = detailPanel != null ? detailPanel.GetComponentInChildren<CardSlotUI>(true) : null;
            if (detailSlotUI != null)
                detailSlotUI.SetArtSprite(stageSprite);
        }

        private void OnDetailEquip()
        {
            if (!string.IsNullOrEmpty(selectedCardId))
            {
                gm?.EquipCard(selectedCardId);
                RefreshDetailPanel(selectedCardId);
            }
        }

        private void OnDetailBreakthrough()
        {
            if (string.IsNullOrEmpty(selectedCardId) || gm == null) return;
            var card = gm.CardCatalog?.FindById(selectedCardId);
            if (card == null) return;

            int oldStage = selectedIllustrationStage;
            Sprite oldSprite = card.GetSpriteForStage(oldStage);
            if (oldSprite == null) oldSprite = card.CardSprite;

            if (gm.BreakthroughCard(selectedCardId))
            {
                var states = gm.GetCardStates();
                states.TryGetValue(selectedCardId, out var newProg);
                int newCount = newProg != null ? newProg.BreakthroughCount : 0;
                int newStage = newCount + 1; // Stage unlocked by new breakthrough level

                var validStages = card.GetBreakthroughStages();
                bool hasNewIllustration = validStages.Contains(newStage);

                if (hasNewIllustration)
                {
                    selectedIllustrationStage = newStage;
                }

                if (newProg != null)
                {
                    newProg.SelectedStage = selectedIllustrationStage;
                    gm.SetCardSelectedStage(card.Id, selectedIllustrationStage);
                }

                // ── 등급별 한계돌파 연출 규칙 ───────────────────────────
                // 1. N 등급: 연출 없음 (즉시 강화)
                // 2. R 등급: 실루엣 깜빡임 연출 (별 파티클 효과 없음)
                // 3. SR 이상 (SR, SSR, UR 등급): 실루엣 깜빡임 연출 + 별 파티클 효과 추가!
                if (card.Rarity == CardRarity.N)
                {
                    // N 등급: 연출 없음
                    RefreshDetailPanel(selectedCardId);
                    if (showNoTab) RefreshNoTab();
                }
                else if (card.Rarity == CardRarity.R)
                {
                    // R 등급: 실루엣 깜빡임 컷씬 (별 파티클 없음)
                    Sprite newSprite = card.GetSpriteForStage(selectedIllustrationStage);
                    if (newSprite == null) newSprite = card.CardSprite;

                    var cutscene = BreakthroughCutsceneUI.GetOrCreate();
                    cutscene.PlayCutscene(oldSprite, newSprite, card.DisplayName, newStage, () =>
                    {
                        RefreshDetailPanel(selectedCardId);
                        if (showNoTab) RefreshNoTab();
                    }, enableStarParticles: false);
                }
                else if (card.Rarity >= CardRarity.SR && hasNewIllustration)
                {
                    // SR 이상 등급: 신규 일러스트 해금 시 실루엣 깜빡임 컷씬 + 별 파티클 연출!
                    Sprite newSprite = card.GetSpriteForStage(selectedIllustrationStage);
                    if (newSprite == null) newSprite = card.CardSprite;

                    var cutscene = BreakthroughCutsceneUI.GetOrCreate();
                    cutscene.PlayCutscene(oldSprite, newSprite, card.DisplayName, newStage, () =>
                    {
                        RefreshDetailPanel(selectedCardId);
                        if (showNoTab) RefreshNoTab();
                    }, enableStarParticles: true);
                }
                else
                {
                    // 일러스트 변형이 없는 일반 단계 (예: SR 2강, 4강 등)
                    RefreshDetailPanel(selectedCardId);
                    if (showNoTab) RefreshNoTab();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  REFRESH – Set TAB  (1 page = 1 set)
        // ─────────────────────────────────────────────────────────────────────
        private void RefreshSets()
        {
            // ── Diagnostics ─────────────────────────────────────────
            Debug.Log($"[EncyclopediaPanel] RefreshSets – " +
                      $"gm={gm != null} | setPanel={setPanel != null} | " +
                      $"leftSetPage={(leftSetPage != null ? leftSetPage.name : "NULL")} | " +
                      $"leftSetSlots={leftSetSlots.Count} | " +
                      $"SetCatalog sets={(gm?.SetCatalog?.Sets?.Count ?? -1)}");

            if (setPanel != null)
            {
                var sb = new System.Text.StringBuilder("[EncyclopediaPanel] SetPanel children: ");
                for (int ci = 0; ci < setPanel.transform.childCount; ci++)
                    sb.Append(setPanel.transform.GetChild(ci).name).Append(", ");
                Debug.Log(sb.ToString());
            }
            // ──────────────────────────────────────────────────────

            if (leftSetPage == null || gm == null) return;
            var sets = gm.SetCatalog?.Sets;
            if (sets == null || sets.Count == 0)
            {
                if (setPageLabel != null) setPageLabel.text = "0 / 0";
                SetPanelTitleText("세트");
                if (rightSetPage != null) rightSetPage.SetActive(false);
                return;
            }

            // Show rightSetPage too – slots from rightSlots are part of the same set
            if (rightSetPage != null) rightSetPage.SetActive(true);

            int maxPages = sets.Count;
            setPageIndex = Mathf.Clamp(setPageIndex, 0, maxPages - 1);

            SetEntry entry = sets[setPageIndex];
            currentSetEntry = entry;

            // Update page label (PageText)
            string targetSetPageText = $"{setPageIndex + 1} / {maxPages}";
            if (setPageLabel != null && setPageLabel.text != targetSetPageText) setPageLabel.text = targetSetPageText;

            // Update panel title with set name (e.g. "테스트")
            SetPanelTitleText(entry != null ? entry.SetName : "세트");

            // Combine both slot pools so cat-10 shows in Slot_9 (first of rightSlots)
            var combinedPool = new List<CardSlotUI>(leftSetSlots);
            combinedPool.AddRange(rightSetSlots);

            RefreshSetPageData(leftSetPage.transform, entry, combinedPool, setPanel.transform);


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
            setPageIndex = Mathf.Clamp(setPageIndex + delta, 0, sets.Count - 1);
            selectedSetSlotIndex = -1;
            RefreshSets();
        }

        private void RefreshSetPageData(Transform pageTf, SetEntry setEntry, List<CardSlotUI> pool, Transform panelRootTf = null)
        {
            // For UI elements (description, ClaimBtn, moveBtn) that may be siblings of pageTf inside SetPanel
            Transform uiRoot = panelRootTf ?? pageTf;

            if (setEntry == null)
            {
                foreach (var s in pool) if (s != null) s.gameObject.SetActive(false);
                var et = uiRoot.Find("SetNameTitle") ?? pageTf.Find("SetNameTitle"); if (et != null) et.gameObject.SetActive(false);
                var eb = uiRoot.Find("ClaimBtn")     ?? pageTf.Find("ClaimBtn");     if (eb != null) eb.gameObject.SetActive(false);
                return;
            }

            // SetNameTitle inside the page (optional – title also shown in panelTitle)
            var titleTf = pageTf.Find("SetNameTitle");
            if (titleTf != null)
            {
                titleTf.gameObject.SetActive(true);
                var t = titleTf.GetComponent<TMP_Text>();
                if (t != null) t.text = setEntry.SetName;
            }

            var states    = gm.GetCardStates();
            var catalog   = gm.CardCatalog?.Cards;
            var setCards  = setEntry.GetCardsInSet(catalog);
            int cardCount = setCards.Count;
            bool allOwned = cardCount > 0;

            for (int i = 0; i < pool.Count; i++)
            {
                var slot = pool[i];
                if (slot == null) continue;
                if (i < cardCount)
                {
                    slot.gameObject.SetActive(true);
                    var displayCard = setCards[i];
                    string cid = displayCard.Id;
                    states.TryGetValue(cid, out var prog);
                    bool unlocked = prog != null && prog.Unlocked;
                    if (!unlocked) allOwned = false;

                    int cardIndex = catalog != null ? FindOriginalIndex(displayCard, catalog) : (i + 1);

                    if (displayCard != null)
                    {
                        Sprite frameSp = unlocked ? GetFrameSpriteForRarity(displayCard.Rarity) : spriteCardLocked;
                        Sprite markSp  = unlocked ? GetMarkSpriteForRarity(displayCard.Rarity) : null;
                        slot.SetSprites(frameSp, spriteCardLocked, markSp);

                        int slotIdx = i;
                        slot.SetData(displayCard, cardIndex, prog, gm, id =>
                        {
                            selectedSetSlotIndex = slotIdx;
                            ApplySetSlotHighlight(pool);
                            UpdateSetMoveButton(uiRoot, pageTf, setEntry);
                        });
                    }

                    // Apply highlight state
                    ApplySetSlotHighlightSingle(slot, i == selectedSetSlotIndex);
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }

            // Description: search in uiRoot (SetPanel) first, then in pageTf (leftSlots)
            var descTx = FindDescriptionInRoot(uiRoot) ?? FindDescriptionText(pageTf);
            if (descTx != null)
            {
                descTx.gameObject.SetActive(true);
                string desc   = string.IsNullOrWhiteSpace(setEntry.EffectDesc) ? "세트 보상" : setEntry.EffectDesc;
                string reward = setEntry.GetRewardSummary();
                descTx.text = desc + "\n보상: " + reward;

                // Dynamic Y position: 225 for <=9 slots, -137 for >=10 slots
                float targetY = cardCount <= 9 ? 225f : -137f;
                var rt = descTx.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, targetY);
                }
                if (descTx.transform.parent != null && descTx.transform.parent != uiRoot && descTx.transform.parent != pageTf)
                {
                    var parentRt = descTx.transform.parent.GetComponent<RectTransform>();
                    if (parentRt != null)
                    {
                        parentRt.anchoredPosition = new Vector2(parentRt.anchoredPosition.x, targetY);
                    }
                }
            }

            // MoveBtn – update interactivity and click action
            UpdateSetMoveButton(uiRoot, pageTf, setEntry);

            // ClaimBtn – search in SetPanel first
            bool claimed = gm.IsSetRewardClaimed(setEntry.SetId);
            var btnTf    = uiRoot.Find("ClaimBtn") ?? pageTf.Find("ClaimBtn")
                        ?? FindChildByNameRecursive(uiRoot, "ClaimBtn");
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

            if (claimTx  != null) claimTx.text = claimed ? "완료" : "보상받기";
            if (claimBtn != null)
            {
                SetButtonInteractable(claimBtn, !claimed && allOwned);
                var img = claimBtn.GetComponent<Image>();
                if (img != null)
                    img.color = claimed  ? new Color(0.2f, 0.45f, 0.2f)
                              : allOwned ? new Color(0.70f, 0.45f, 0.05f)
                              : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private void UpdateSetMoveButton(Transform uiRoot, Transform pageTf, SetEntry setEntry)
        {
            var moveBtnTf = uiRoot.Find("moveBtn") ?? uiRoot.Find("MoveBtn")
                         ?? pageTf.Find("moveBtn")  ?? pageTf.Find("MoveBtn")
                         ?? FindChildByNameRecursive(uiRoot, "moveBtn")
                         ?? FindChildByNameRecursive(uiRoot, "MoveBtn");

            if (moveBtnTf == null) return;

            moveBtnTf.gameObject.SetActive(true);
            var mb = moveBtnTf.GetComponent<Button>();
            if (mb == null) return;

            mb.onClick.RemoveAllListeners();

            var catalog  = gm?.CardCatalog?.Cards;
            var setCards = setEntry != null ? setEntry.GetCardsInSet(catalog) : null;

            bool hasSelection = selectedSetSlotIndex >= 0 &&
                                setCards != null &&
                                selectedSetSlotIndex < setCards.Count;

            SetButtonInteractable(mb, hasSelection);

            if (hasSelection)
            {
                string targetCardId = setCards[selectedSetSlotIndex].Id;
                mb.onClick.AddListener(() => OnMoveToCardDetail(targetCardId));
            }
        }

        private void OnMoveToCardDetail(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || gm == null) return;

            var allCards = gm.CardCatalog?.Cards;
            if (allCards == null) return;

            // Switch to ALL filter
            currentFilter = RarityFilter.All;

            // Rebuild filtered list for ALL filter (non-hidden cards)
            filteredCards.Clear();
            foreach (var c in allCards)
            {
                if (!c.IsHidden) filteredCards.Add(c);
            }

            // Find index of the card in the ALL filter list
            int cardListIndex = -1;
            for (int i = 0; i < filteredCards.Count; i++)
            {
                if (filteredCards[i].Id == cardId)
                {
                    cardListIndex = i;
                    break;
                }
            }

            if (cardListIndex >= 0)
            {
                currentPageIdx = cardListIndex / SLOTS_PER_PAGE;
                selectedCardId = cardId;
            }

            // Switch tab to No tab and refresh UI
            ShowNoTab();

            Debug.Log($"[EncyclopediaPanel] OnMoveToCardDetail: Moved to card '{cardId}', page {currentPageIdx + 1}");
        }

        // ── Set slot highlight helpers ────────────────────────────────────────
        private void ApplySetSlotHighlight(List<CardSlotUI> pool)
        {
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null) ApplySetSlotHighlightSingle(pool[i], i == selectedSetSlotIndex);
        }

        private static void ApplySetSlotHighlightSingle(CardSlotUI slot, bool selected)
        {
            if (slot == null) return;
            // Slightly scale up the selected slot as a highlight indicator
            var rt = slot.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = selected ? new Vector3(1.08f, 1.08f, 1f) : Vector3.one;

            // Optionally tint the slot's own Image component
            var img = slot.GetComponent<Image>();
            if (img != null) img.color = selected ? new Color(1f, 0.95f, 0.6f, 1f) : Color.white;
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

        // Find a TMP_Text named exactly 'description' in the root, or fall back to heuristic
        private static TMP_Text FindDescriptionInRoot(Transform root)
        {
            var t = root.Find("description") ?? root.Find("Description");
            if (t != null) return t.GetComponent<TMP_Text>();
            return null;
        }

        private TMP_Text FindDescriptionText(Transform pageTf)
        {
            // Try exact name first
            var byName = pageTf.Find("description") ?? pageTf.Find("Description");
            if (byName != null) { var tx2 = byName.GetComponent<TMP_Text>(); if (tx2 != null) return tx2; }

            // Fallback: find first TMP_Text child that isn’t a known UI element
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
            var bmNo = FindChildByNameRecursive(transform, "BookMark_No");
            if (bmNo != null) tabNoBtn = bmNo.gameObject;
            else if (tabNoBtn == null) { var t = FindChildByNameRecursive(transform, "Btn_No.") ?? FindChildByNameRecursive(transform, "tabNoBtn");  if (t != null) tabNoBtn  = t.gameObject; }

            var bmSet = FindChildByNameRecursive(transform, "BookMark_Set");
            if (bmSet != null) tabSetBtn = bmSet.gameObject;
            else if (tabSetBtn == null) { var t = FindChildByNameRecursive(transform, "Btn_Set") ?? FindChildByNameRecursive(transform, "tabSetBtn"); if (t != null) tabSetBtn = t.gameObject; }

            if (closeBtn  == null) { var t = FindChildByNameRecursive(transform, "Btn_✕")  ?? FindChildByNameRecursive(transform, "closeBtn");  if (t != null) closeBtn  = t.gameObject; }

            // No tab navigation
            if (noPanel != null)
            {
                var noPanelTf = noPanel.transform;
                if (prevPageBtn == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_◀") ?? FindChildByNameRecursive(noPanelTf, "LeftBtn") ?? FindChildByNameRecursive(noPanelTf, "PrevBtn"); if (t != null) prevPageBtn = t.gameObject; }
                if (nextPageBtn == null) { var t = FindChildByNameRecursive(noPanelTf, "Btn_▶") ?? FindChildByNameRecursive(noPanelTf, "RightBtn") ?? FindChildByNameRecursive(noPanelTf, "NextBtn"); if (t != null) nextPageBtn = t.gameObject; }
                if (pageLabel   == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "PageText")
                         ?? FindChildByNameRecursive(noPanelTf, "pageText")
                         ?? FindChildByNameRecursive(noPanelTf, "pageLabel")
                         ?? FindChildByNameRecursive(noPanelTf, "PageLabel");
                    if (t != null) pageLabel = t.GetComponent<TMP_Text>();
                }
                if (collectionCounterLabel == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "Collection_counter")
                         ?? FindChildByNameRecursive(noPanelTf, "collection_counter")
                         ?? FindChildByNameRecursive(noPanelTf, "CollectionCounter")
                         ?? FindChildByNameRecursive(transform, "Collection_counter");
                    if (t != null) collectionCounterLabel = t.GetComponent<TMP_Text>();
                }

                // Filter tabs (strictly look for objects with Button component)
                var foundAll = FindFilterTabButton(noPanelTf, "Btn_전체", "Btn_All", "Btn_ALL", "Tab_All", "Filter_All", "All", "all");
                if (foundAll != null) filterTabAll = foundAll;

                var foundN = FindFilterTabButton(noPanelTf, "Btn_N", "Tab_N", "Filter_N", "filterTabN", "Btn_n", "N_Tab", "TabN");
                if (foundN != null) filterTabN = foundN;

                var foundR = FindFilterTabButton(noPanelTf, "Btn_R", "Tab_R", "Filter_R", "filterTabR", "Btn_r", "R_Tab", "TabR");
                if (foundR != null) filterTabR = foundR;

                var foundSR = FindFilterTabButton(noPanelTf, "Btn_SR", "Tab_SR", "Filter_SR", "filterTabSR", "Btn_sr", "SR_Tab", "TabSR");
                if (foundSR != null) filterTabSR = foundSR;

                var foundSSR = FindFilterTabButton(noPanelTf, "Btn_SSR", "Tab_SSR", "Filter_SSR", "filterTabSSR", "Btn_ssr", "SSR_Tab", "TabSSR");
                if (foundSSR != null) filterTabSSR = foundSSR;

                var foundUR = FindFilterTabButton(noPanelTf, "Btn_UR", "Tab_UR", "Filter_UR", "filterTabUR", "Btn_ur", "UR_Tab", "TabUR");
                if (foundUR != null) filterTabUR = foundUR;

                // Detail panel
                if (detailPanel == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "RightPage")
                         ?? FindChildByNameRecursive(noPanelTf, "rightPage")
                         ?? FindChildByNameRecursive(noPanelTf, "DetailPanel")
                         ?? FindChildByNameRecursive(noPanelTf, "detailPanel")
                         ?? FindChildByNameRecursive(noPanelTf, "Detail");
                    if (t != null) detailPanel = t.gameObject;
                    else detailPanel = noPanel;
                }
                if (detailPanel != null)
                {
                    var rt = detailPanel.transform;

                    // Frame → detailCardFrameImage (오직 Frame / frame 오브젝트만 대상)
                    if (detailCardFrameImage == null)
                    {
                        var fr = FindChildByNameRecursive(rt, "Frame") ?? FindChildByNameRecursive(rt, "frame") ?? FindChildByNameRecursive(rt, "DetailFrame");
                        if (fr != null) detailCardFrameImage = fr.GetComponent<Image>();
                    }

                    // Art → detailCardArt (Art/art/DetailArt/CardArt/image 오브젝트 순서로 탐색)
                    if (detailCardArt == null)
                    {
                        var ar = FindChildByNameRecursive(rt, "Art")
                              ?? FindChildByNameRecursive(rt, "art")
                              ?? FindChildByNameRecursive(rt, "DetailArt")
                              ?? FindChildByNameRecursive(rt, "CardArt")
                              ?? FindChildByNameRecursive(rt, "image")
                              ?? FindChildByNameRecursive(rt, "Image");
                        if (ar != null) detailCardArt = ar.GetComponent<Image>();
                    }

                    // rereMark / rareMark → detailCardRarityMark
                    if (detailCardRarityMark == null)
                    {
                        var rm = FindChildByNameRecursive(rt, "rereMark")
                              ?? FindChildByNameRecursive(rt, "reremark")
                              ?? FindChildByNameRecursive(rt, "rareMark")
                              ?? FindChildByNameRecursive(rt, "raremark")
                              ?? FindChildByNameRecursive(rt, "RarityMark")
                              ?? FindChildByNameRecursive(rt, "rarityMark")
                              ?? FindChildByNameRecursive(rt, "rare_mark")
                              ?? FindChildByNameRecursive(rt, "rarity_mark")
                              ?? FindChildByNameRecursive(rt, "RareMark");
                        if (rm != null) detailCardRarityMark = rm.GetComponent<Image>();
                    }

                    if (detailCardName == null)
                    {
                        var t = FindChildByNameRecursive(rt, "ImageNametxt")
                             ?? FindChildByNameRecursive(rt, "imageNametxt")
                             ?? FindChildByNameRecursive(rt, "ImageNameTxt")
                             ?? FindChildByNameRecursive(rt, "ImageName")
                             ?? FindChildByNameRecursive(rt, "imageName")
                             ?? FindChildByNameRecursive(rt, "DetailName")
                             ?? FindChildByNameRecursive(rt, "CardName")
                             ?? FindChildByNameRecursive(rt, "NameText")
                             ?? FindChildByNameRecursive(rt, "nameText")
                             ?? FindChildByNameRecursive(rt, "name")
                             ?? FindChildByNameRecursive(rt, "Name");
                        if (t != null) detailCardName = t.GetComponent<TMP_Text>();
                    }

                    if (detailDescription == null)
                    {
                        var descBgTf = FindChildByNameRecursive(rt, "DescBG")
                                    ?? FindChildByNameRecursive(rt, "descBG")
                                    ?? FindChildByNameRecursive(rt, "DescBg")
                                    ?? FindChildByNameRecursive(rt, "desc_bg");
                        if (descBgTf != null)
                        {
                            detailDescription = descBgTf.GetComponentInChildren<TMP_Text>(true);
                        }
                        if (detailDescription == null)
                        {
                            var t = FindChildByNameRecursive(rt, "description")
                                 ?? FindChildByNameRecursive(rt, "Description")
                                 ?? FindChildByNameRecursive(rt, "DetailDesc")
                                 ?? FindChildByNameRecursive(rt, "descText")
                                 ?? FindChildByNameRecursive(rt, "desc");
                            if (t != null) detailDescription = t.GetComponent<TMP_Text>();
                        }
                    }

                    if (detailRarityBadge   == null) { var t = FindChildByNameRecursive(rt, "RarityBadge");  if (t != null) detailRarityBadge   = t.GetComponent<TMP_Text>(); }
                    if (detailIncomeText    == null) { var t = FindChildByNameRecursive(rt, "IncomeText");   if (t != null) detailIncomeText    = t.GetComponent<TMP_Text>(); }
                    if (detailBreakthroughText == null) { var t = FindChildByNameRecursive(rt, "BreakText"); if (t != null) detailBreakthroughText = t.GetComponent<TMP_Text>(); }
                    if (detailEquipBtn      == null) { var t = FindChildByNameRecursive(rt, "Btn_대표 설정"); if (t != null) detailEquipBtn      = t.GetComponent<Button>(); }
                    if (detailBreakthroughBtn == null) { var t = FindChildByNameRecursive(rt, "Btn_한계 돌파"); if (t != null) detailBreakthroughBtn = t.GetComponent<Button>(); }
                }

                // NumBtn은 DetailPanel의 형제이므로 noPanel 전체에서 탐색
                if (numBtnContainer == null)
                {
                    var t = FindChildByNameRecursive(noPanelTf, "NumBtn")
                         ?? FindChildByNameRecursive(noPanelTf, "numBtn")
                         ?? FindChildByNameRecursive(noPanelTf, "NumButtons");
                    if (t != null) numBtnContainer = t.gameObject;
                }

                Debug.Log($"[Encyclopedia] detailPanel={detailPanel?.name}, detailCardArt={detailCardArt?.gameObject?.name}, numBtnContainer={numBtnContainer?.name}");
            }

            // Set tab sub-elements
            if (setPanel != null)
            {
                var setRootTf = setPanel.transform;

                // Main content page (single page per set)
                // Try common names first, then fall back to finding first child with CardSlotUI
                if (leftSetPage == null)
                {
                    var t = setRootTf.Find("LeftPage")
                         ?? setRootTf.Find("Page")
                         ?? setRootTf.Find("ContentPage")
                         ?? setRootTf.Find("SlotPage")
                         ?? FindChildByNameRecursive(setRootTf, "LeftPage");

                    if (t != null)
                    {
                        leftSetPage = t.gameObject;
                    }
                    else
                    {
                        // Fallback: use the first child that contains CardSlotUI components
                        for (int ci = 0; ci < setRootTf.childCount; ci++)
                        {
                            var child = setRootTf.GetChild(ci);
                            if (child.GetComponentsInChildren<CardSlotUI>(true).Length > 0)
                            {
                                leftSetPage = child.gameObject;
                                Debug.Log($"[EncyclopediaPanel] leftSetPage fallback → '{child.name}'");
                                break;
                            }
                        }
                        // Second fallback: if there are CardSlotUI directly in setPanel, use setPanel itself
                        if (leftSetPage == null && setPanel.GetComponentsInChildren<CardSlotUI>(true).Length > 0)
                        {
                            leftSetPage = setPanel;
                            Debug.Log("[EncyclopediaPanel] leftSetPage fallback → setPanel itself");
                        }
                    }
                }

                if (rightSetPage == null) { var t = setRootTf.Find("RightPage") ?? FindChildByNameRecursive(setRootTf, "RightPage"); if (t != null) rightSetPage = t.gameObject; }

                // Prev / Next buttons – look for common names
                if (prevSetPageBtn == null)
                {
                    var t = FindChildByNameRecursive(setRootTf, "Btn_◀")
                         ?? FindChildByNameRecursive(setRootTf, "LeftBtn")
                         ?? FindChildByNameRecursive(setRootTf, "PrevBtn")
                         ?? FindChildByNameRecursive(setRootTf, "Btn_Left");
                    if (t != null) prevSetPageBtn = t.gameObject;
                }
                if (nextSetPageBtn == null)
                {
                    var t = FindChildByNameRecursive(setRootTf, "Btn_▶")
                         ?? FindChildByNameRecursive(setRootTf, "RightBtn")
                         ?? FindChildByNameRecursive(setRootTf, "NextBtn")
                         ?? FindChildByNameRecursive(setRootTf, "Btn_Right");
                    if (t != null) nextSetPageBtn = t.gameObject;
                }

                // PageText / setPageLabel
                if (setPageLabel == null)
                {
                    var t = FindChildByNameRecursive(setRootTf, "PageText")
                         ?? FindChildByNameRecursive(setRootTf, "setPageLabel")
                         ?? FindChildByNameRecursive(setRootTf, "pageLabel");
                    if (t != null) setPageLabel = t.GetComponent<TMP_Text>();
                }

                leftSetSlots.Clear();
                if (leftSetPage != null)
                {
                    var arr = leftSetPage.GetComponentsInChildren<CardSlotUI>(true);
                    leftSetSlots.AddRange(arr);
                    Debug.Log($"[EncyclopediaPanel] leftSetSlots found: {leftSetSlots.Count} under '{leftSetPage.name}'");
                }
                rightSetSlots.Clear();
                if (rightSetPage != null)
                {
                    var arr = rightSetPage.GetComponentsInChildren<CardSlotUI>(true);
                    rightSetSlots.AddRange(arr);
                }
            }

            // EncyclopediaPanelTitle
            if (panelTitle == null)
            {
                var t = FindChildByNameRecursive(transform, "EncyclopediaPanelTitle")
                     ?? FindChildByNameRecursive(transform, "PanelTitle");
                if (t != null) panelTitle = t.GetComponent<TMP_Text>();
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
            var img = go.GetComponent<Image>() ?? go.GetComponentInChildren<Image>();
            if (img != null) img.color = col;
        }

        private static void SetButtonInteractable(Button btn, bool interactable)
        {
            if (btn == null) return;
            btn.interactable = interactable;

            // Ensure CanvasGroup exists so all child UI elements (Image, Text, Icons)
            // fade to 40% translucency identically when disabled
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = btn.gameObject.AddComponent<CanvasGroup>();
            }
            cg.alpha = interactable ? 1.0f : 0.4f;

            // Ensure mouse hover highlight is visible when button is interactable
            if (btn.transition == Selectable.Transition.ColorTint)
            {
                var cs = btn.colors;
                cs.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                if (cs.highlightedColor == cs.normalColor || cs.highlightedColor.a == 0)
                {
                    cs.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1.0f);
                }
                btn.colors = cs;
            }
        }

        private GameObject FindFilterTabButton(Transform root, params string[] candidateNames)
        {
            if (root == null) return null;
            foreach (var name in candidateNames)
            {
                var t = FindChildByNameRecursive(root, name);
                if (t != null && t.GetComponent<Button>() != null)
                    return t.gameObject;
            }
            foreach (var name in candidateNames)
            {
                var t = FindChildByNameRecursive(root, name);
                if (t != null)
                    return t.gameObject;
            }
            return null;
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

        private Sprite GetFrameSpriteForRarity(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.R:   return spriteCardFrameR   != null ? spriteCardFrameR   : spriteCardFrameN;
                case CardRarity.SR:  return spriteCardFrameSR  != null ? spriteCardFrameSR  : spriteCardFrameN;
                case CardRarity.SSR: return spriteCardFrameSSR != null ? spriteCardFrameSSR : spriteCardFrameN;
                case CardRarity.UR:  return spriteCardFrameSSR != null ? spriteCardFrameSSR : spriteCardFrameN;
                default:             return spriteCardFrameN;
            }
        }

        private Sprite GetMarkSpriteForRarity(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.R:   return spriteMarkR   != null ? spriteMarkR   : spriteMarkN;
                case CardRarity.SR:  return spriteMarkSR  != null ? spriteMarkSR  : spriteMarkN;
                case CardRarity.SSR: return spriteMarkSSR != null ? spriteMarkSSR : spriteMarkN;
                case CardRarity.UR:  return spriteMarkUR  != null ? spriteMarkUR  : (spriteMarkSSR != null ? spriteMarkSSR : spriteMarkN);
                default:             return spriteMarkN;
            }
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

        private void ReplicateLockTextToAllSlots()
        {
            if (noPanel == null) return;
            var childSlots = noPanel.GetComponentsInChildren<CardSlotUI>(true);
            if (childSlots == null || childSlots.Length == 0) return;

            // Find slot0 (named Slot_0 or index 0)
            CardSlotUI slot0 = null;
            foreach (var slot in childSlots)
            {
                if (slot.name == "Slot_0" || slot.name == "Slot_1" || slot.name.Contains("0"))
                {
                    slot0 = slot;
                    break;
                }
            }
            if (slot0 == null) slot0 = childSlots[0];

            // Find template locktext transform child of slot0
            Transform templateLockTf = null;
            foreach (Transform child in slot0.transform)
            {
                if (child.name.ToLower().Contains("lock"))
                {
                    templateLockTf = child;
                    break;
                }
            }

            if (templateLockTf == null)
            {
                Debug.LogWarning("[EncyclopediaPanel] Could not find locktext template child in Slot_0.");
                return;
            }

            // Clone this template to any other slots if they don't have it
            var allSlotsInPanel = GetComponentsInChildren<CardSlotUI>(true);
            foreach (var slot in allSlotsInPanel)
            {
                if (slot == slot0) continue;

                // Check if slot already has a child with "lock" in its name
                bool hasLock = false;
                foreach (Transform child in slot.transform)
                {
                    if (child.name.ToLower().Contains("lock"))
                    {
                        hasLock = true;
                        break;
                    }
                }

                if (!hasLock)
                {
                    var clonedLock = Instantiate(templateLockTf.gameObject, slot.transform);
                    clonedLock.name = templateLockTf.name;

                    // Synchronize RectTransform values
                    var clonedRT = clonedLock.GetComponent<RectTransform>();
                    var templateRT = templateLockTf.GetComponent<RectTransform>();
                    if (clonedRT != null && templateRT != null)
                    {
                        clonedRT.anchorMin = templateRT.anchorMin;
                        clonedRT.anchorMax = templateRT.anchorMax;
                        clonedRT.pivot = templateRT.pivot;
                        clonedRT.anchoredPosition = templateRT.anchoredPosition;
                        clonedRT.sizeDelta = templateRT.sizeDelta;
                        clonedRT.localScale = templateRT.localScale;
                    }
                }
            }
        }

        // ── Inner types ──────────────────────────────────────────────────────
        private struct SlotBundle
        {
            public GameObject go;
            public CardSlotUI ui;
        }
    }
}
