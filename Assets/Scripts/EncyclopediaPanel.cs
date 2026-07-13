using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 도감 패널 – 완전 자급자족형 (코드로 전체 UI 생성).
    /// EncyclopediaPanel 게임오브젝트에 스크립트만 붙이면 동작합니다.
    /// </summary>
    public sealed class EncyclopediaPanel : MonoBehaviour
    {
        // ── State ────────────────────────────────────────────────────────────
        private GameManager  gm;
        private int          currentPageIdx = 0;
        private bool         showNoTab      = true;   // true = No. 탭, false = Set 탭
        private string       selectedCardId;

        // ── Runtime-created UI ───────────────────────────────────────────────
        [SerializeField] private GameObject  noPanel;
        [SerializeField] private GameObject  setPanel;
        [SerializeField] private ScrollRect  setScrollRect;
        [SerializeField] private Transform   setContent;
        [SerializeField] private GameObject  leftSetPage;
        [SerializeField] private GameObject  rightSetPage;
        [SerializeField] private GameObject  prevSetPageBtn;
        [SerializeField] private GameObject  nextSetPageBtn;
        [SerializeField] private TMP_Text    setPageLabel;
        private int setPageIndex = 0;
        
        [SerializeField] private GameObject  tabNoBtn;
        [SerializeField] private GameObject  tabSetBtn;
        [SerializeField] private GameObject  prevPageBtn;
        [SerializeField] private GameObject  nextPageBtn;
        [SerializeField] private GameObject  closeBtn;
        [SerializeField] private TMP_Text    pageLabel;

        // 16개 슬롯 (왼쪽 8 + 오른쪽 8)
        private readonly List<SlotBundle> slots = new List<SlotBundle>();

        // 상세 팝업
        [SerializeField] private GameObject  detailRoot;
        [SerializeField] private Image       detailImage;
        [SerializeField] private TMP_Text    detailName;
        [SerializeField] private TMP_Text    detailDesc;
        [SerializeField] private TMP_Text    detailIncomeText;
        [SerializeField] private Button      detailEquipBtn;
        [SerializeField] private Button      detailBreakthroughBtn;
        [SerializeField] private TMP_Text    detailBreakthroughBtnText;

        // 세트 행 풀
        private readonly List<GameObject> setRows = new List<GameObject>();

        // ── Colors / Style ──────────────────────────────────────────────────
        private static readonly Color BG          = new Color(0.08f, 0.10f, 0.16f, 0.97f);
        private static readonly Color PageBG      = new Color(0.95f, 0.90f, 0.78f, 1.00f);
        private static readonly Color TabActive   = new Color(0.25f, 0.55f, 0.95f, 1.00f);
        private static readonly Color TabInactive = new Color(0.20f, 0.22f, 0.30f, 1.00f);
        private static readonly Color BtnColor    = new Color(0.20f, 0.45f, 0.80f, 1.00f);

        private static readonly Color32 ColN   = new Color32(180, 180, 180, 255);
        private static readonly Color32 ColR   = new Color32( 80, 150, 255, 255);
        private static readonly Color32 ColSR  = new Color32(180,  80, 255, 255);
        private static readonly Color32 ColSSR = new Color32(255, 200,   0, 255);
        private static readonly Color32 ColUR  = new Color32(255,  80,  30, 255);

        private void Awake()
        {
            Debug.Log("[EncyclopediaPanel] Awake started");
            try
            {
                AutoWireFields();
                EnsureParentedToCanvas();
                BuildUI();
                EnsureBreakthroughButtonBuilt();
                EnsureShopButtonCleanedUp();
                if (pageLabel != null) BindListeners();
                gm = FindObjectOfType<GameManager>(true);
                Debug.Log($"[EncyclopediaPanel] Awake complete. pageLabel={pageLabel!=null}, gm={gm!=null}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EncyclopediaPanel] Exception in Awake: {e}");
            }
        }

        private void BindListeners()
        {
            EnsureShopButtonCleanedUp();
            if (tabNoBtn != null) { var b = tabNoBtn.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(ShowNoTab); }
            if (tabSetBtn != null) { var b = tabSetBtn.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(ShowSetTab); }
            if (prevPageBtn != null) { var b = prevPageBtn.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ChangePage(-1)); }
            if (nextPageBtn != null) { var b = nextPageBtn.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => ChangePage(1)); }
            if (closeBtn != null) { var b = closeBtn.GetComponent<Button>(); b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnClose); }
            if (detailEquipBtn != null) { detailEquipBtn.onClick.RemoveAllListeners(); detailEquipBtn.onClick.AddListener(OnDetailEquip); }
            if (detailBreakthroughBtn != null) { detailBreakthroughBtn.onClick.RemoveAllListeners(); detailBreakthroughBtn.onClick.AddListener(OnDetailBreakthrough); }

            if (detailRoot != null)
            {
                var detailBtns = detailRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in detailBtns)
                {
                    var txt = b.GetComponentInChildren<TMP_Text>();
                    string nameLower = b.name.ToLower();
                    bool isCloseBtn = (txt != null && (txt.text.Contains("닫기") || txt.text.Contains("✕") || txt.text.ToLower().Contains("close"))) 
                                      || nameLower.Contains("close") || nameLower.Contains("닫기");

                    if (isCloseBtn)
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() => detailRoot.SetActive(false));
                    }
                }
            }

            slots.Clear();
            if (noPanel != null)
            {
                var childSlots = noPanel.GetComponentsInChildren<CardSlotUI>(true);
                for (int i = 0; i < childSlots.Length; i++)
                {
                    int slotIdx = i;
                    var slotGO = childSlots[i].gameObject;
                    slots.Add(new SlotBundle { go = slotGO, ui = childSlots[i] });
                    var btn = slotGO.GetComponent<Button>();
                    if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => OnSlotClicked(slotIdx)); }
                }
            }
        }

        private void OnEnable()
        {
            Debug.Log("[EncyclopediaPanel] OnEnable started");
            try
            {
                AutoWireFields();
                if (gm == null) gm = FindObjectOfType<GameManager>(true);
                currentPageIdx = 0;
                if (gm != null) gm.StateChanged += OnStateChanged;
                ShowNoTab();
                Refresh();
                Debug.Log("[EncyclopediaPanel] OnEnable complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[EncyclopediaPanel] Exception in OnEnable: {e}");
            }
        }

        private void OnDisable()
        {
            if (gm != null) gm.StateChanged -= OnStateChanged;
            if (detailRoot != null) detailRoot.SetActive(false);
        }

        private void OnStateChanged() => Refresh();

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

        public void EnsureBreakthroughButtonBuilt()
        {
            if (detailRoot == null) return;

            // 1. 이미 돌파 버튼이 씬에 매핑되었는지 체크
            if (detailBreakthroughBtn == null)
            {
                var btns = detailRoot.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    string n = b.name.ToLower();
                    if (n.Contains("breakthrough") || n.Contains("돌파"))
                    {
                        detailBreakthroughBtn = b;
                        detailBreakthroughBtnText = b.GetComponentInChildren<TMP_Text>();
                        break;
                    }
                }
            }

            // 2. 프리팹/씬에 없는 경우 동적으로 새로 빌드해서 부착
            if (detailBreakthroughBtn == null)
            {
                var breakthroughBtnGO = MakeButton(detailRoot.transform, "한계 돌파",
                    new Vector2(110, -95), new Vector2(200, 44), new Color(0.70f, 0.45f, 0.05f), OnDetailBreakthrough);
                
                detailBreakthroughBtn = breakthroughBtnGO.GetComponent<Button>();
                detailBreakthroughBtnText = breakthroughBtnGO.GetComponentInChildren<TMP_Text>();

                if (detailBreakthroughBtnText != null && detailName != null)
                {
                    detailBreakthroughBtnText.font = detailName.font;
                }
            }

            // 3. 수익 텍스트 분리 매핑 및 빌드
            if (detailIncomeText == null)
            {
                var existingText = detailRoot.transform.Find("DetailIncomeText");
                if (existingText != null)
                {
                    detailIncomeText = existingText.GetComponent<TMP_Text>();
                }
                else
                {
                    detailIncomeText = MakeText(detailRoot.transform, "현재 클릭 수익: 0.0 Gold (0강)",
                        new Vector2(110, -45), new Vector2(350, 30), 13, new Color(1f, 0.84f, 0f));
                    detailIncomeText.gameObject.name = "DetailIncomeText";
                    detailIncomeText.alignment = TextAlignmentOptions.Center;
                    if (detailName != null) detailIncomeText.font = detailName.font;
                }
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  UI CONSTRUCTION
        // ════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // 완전히 빌드된 상태: No.탭 + Set탭 둘 다 준비됨
            if (pageLabel != null && setPanel != null && leftSetPage != null) return;

            // Set탭 영역만 없거나 구버전(Page 1개)인 경우 → Set탭만 교체 빌드
            if (pageLabel != null && (setPanel == null || leftSetPage == null))
            {
                if (setPanel != null) DestroyImmediate(setPanel); // 구버전 SetPanel 제거
                setPanel = null;
                Transform bookParent = noPanel != null ? noPanel.transform.parent : transform;
                BuildSetTabArea(bookParent);
                BindListeners();
                return;
            }
            // 루트 RectTransform을 전체화면으로 설정
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // 반투명 오버레이
            var bg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.6f);

            // 중앙 패널 (책 모양)
            var panel = MakePanel(transform, new Vector2(0, 0), new Vector2(980, 640));
            var panelImg = panel.GetComponent<Image>() ?? panel.AddComponent<Image>();
            panelImg.color = BG;

            BuildTabs(panel.transform);
            BuildBookArea(panel.transform);
            BuildDetailPopup(panel.transform);

            // 닫기 버튼 (패널 우상단)
            closeBtn = MakeButton(panel.transform, "✕", new Vector2(470, 300), new Vector2(44, 44),
                new Color(0.7f, 0.20f, 0.20f, 1f), OnClose);
        }

        private void BuildTabs(Transform parent)
        {
            float tabY = 285f;

            tabNoBtn = MakeButton(parent, "No.", new Vector2(-160, tabY), new Vector2(90, 36),
                TabActive, () => ShowNoTab());
            tabSetBtn = MakeButton(parent, "Set", new Vector2(-60, tabY), new Vector2(90, 36),
                TabInactive, () => ShowSetTab());
            
            EnsureShopButtonCleanedUp();
        }

        private void BuildBookArea(Transform parent)
        {
            // ─── No. 탭 영역 ──────────────────────────────────────────────
            noPanel = MakeEmptyRT(parent, "NoPanel",
                new Vector2(-10, -20), new Vector2(960, 560));

            // 페이지 배경 (책 왼쪽 / 오른쪽)
            var leftPage  = MakePage(noPanel.transform, new Vector2(-350, 14), new Vector2(670, 710));
            var rightPage = MakePage(noPanel.transform, new Vector2( 350, 14), new Vector2(670, 710));

            // 페이지 전환 버튼
            prevPageBtn = MakeButton(noPanel.transform, "◀", new Vector2(-830, 0), new Vector2(50, 50),
                BtnColor, () => ChangePage(-1));
            nextPageBtn = MakeButton(noPanel.transform, "▶", new Vector2( 830, 0), new Vector2(50, 50),
                BtnColor, () => ChangePage(1));

            // 페이지 라벨
            pageLabel = MakeText(noPanel.transform, "1 / 1",
                new Vector2(0, -275), new Vector2(200, 30), 14, Color.white);

            // 16 슬롯 생성 (왼쪽 4×2 + 오른쪽 4×2 = 16)
            slots.Clear();
            BuildSlotGrid(noPanel.transform, leftPage.transform, 0, -222f);   // left 8
            BuildSlotGrid(noPanel.transform, rightPage.transform, 8, 22f);    // right 8

            // ─── Set 탭 영역 ─────────────────────────────────────────────
            BuildSetTabArea(parent);
        }

        private void BuildSetTabArea(Transform parent)
        {
            if (setPanel == null)
            {
                setPanel = MakeEmptyRT(parent, "SetPanel",
                    new Vector2(-10, -20), new Vector2(960, 560));
            }
            setPanel.SetActive(false);

            if (leftSetPage == null)
            {
                leftSetPage = MakePage(setPanel.transform, new Vector2(-350, 14), new Vector2(670, 710));
            }
            if (rightSetPage == null)
            {
                rightSetPage = MakePage(setPanel.transform, new Vector2( 350, 14), new Vector2(670, 710));
            }

            if (prevSetPageBtn == null)
            {
                prevSetPageBtn = MakeButton(setPanel.transform, "◀", new Vector2(-830, 0), new Vector2(50, 50),
                    BtnColor, () => ChangeSetPage(-1));
            }
            else
            {
                var btn = prevSetPageBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ChangeSetPage(-1));
                }
            }

            if (nextSetPageBtn == null)
            {
                nextSetPageBtn = MakeButton(setPanel.transform, "▶", new Vector2( 830, 0), new Vector2(50, 50),
                    BtnColor, () => ChangeSetPage(1));
            }
            else
            {
                var btn = nextSetPageBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => ChangeSetPage(1));
                }
            }

            if (setPageLabel == null)
            {
                setPageLabel = MakeText(setPanel.transform, "1 / 1",
                    new Vector2(0, -275), new Vector2(200, 30), 14, Color.white);
            }
        }

        // 4×2 슬롯 그리드를 pageTransform 아래에 생성
        private void BuildSlotGrid(Transform noTabRootTf, Transform pageTf, int startIdx, float xOffset)
        {
            float colW = 133f, rowH = 160f;
            float startX = -199.5f;
            float startY = 80f;

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
                slotRT.sizeDelta        = new Vector2(120, 146);

                // 배경 이미지
                var slotImg = slotGO.AddComponent<Image>();
                slotImg.color = new Color(0.15f, 0.17f, 0.25f, 1f);

                // 레이아웃 요소들
                var frameGO = MakeImage(slotGO.transform, "Frame",
                    Vector2.zero, new Vector2(86, 106), new Color(0.4f, 0.4f, 0.4f, 0.5f));
                var artGO   = MakeImage(slotGO.transform, "Art",
                    new Vector2(0, 12), new Vector2(72, 72), Color.gray);
                var nameTx  = MakeText(slotGO.transform, "???",
                    new Vector2(0, -36), new Vector2(88, 18), 9, Color.white);
                nameTx.alignment = TextAlignmentOptions.Center;
                var rarityTx= MakeText(slotGO.transform, "",
                    new Vector2(0, -48), new Vector2(88, 14), 8, Color.gray);
                rarityTx.alignment = TextAlignmentOptions.Center;
                var stackTx = MakeText(slotGO.transform, "",
                    new Vector2(32, 38), new Vector2(28, 16), 8, Color.yellow);

                // 미해금 오버레이
                var unkGO = new GameObject("Unknown");
                unkGO.transform.SetParent(slotGO.transform, false);
                var unkRT = unkGO.AddComponent<RectTransform>();
                unkRT.anchorMin = Vector2.zero; unkRT.anchorMax = Vector2.one;
                unkRT.offsetMin = Vector2.zero; unkRT.offsetMax = Vector2.zero;
                var unkImg = unkGO.AddComponent<Image>();
                unkImg.color = new Color(0.05f, 0.05f, 0.10f, 0.85f);
                var unkTx = MakeText(unkGO.transform, "?",
                    Vector2.zero, new Vector2(80, 80), 36, new Color(0.5f, 0.5f, 0.6f));
                unkTx.alignment = TextAlignmentOptions.Center;

                // CardSlotUI 컴포넌트 부착 및 초기화
                var slotUI = slotGO.AddComponent<CardSlotUI>();
                slotUI.InitUI(
                    frameGO.GetComponent<Image>(),
                    artGO.GetComponent<Image>(),
                    nameTx, rarityTx, stackTx,
                    unkGO
                );

                slots.Add(new SlotBundle { go = slotGO, ui = slotUI });
            }
        }

        private void BuildDetailPopup(Transform parent)
        {
            if (detailRoot != null) return;

            detailRoot = MakePanel(parent, new Vector2(0, 0), new Vector2(640, 420));
            var dImg = detailRoot.GetComponent<Image>();
            dImg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);
            detailRoot.SetActive(false);

            // 왼쪽 – 카드 이미지
            detailImage = MakeImage(detailRoot.transform, "DetailArt",
                new Vector2(-165, 10), new Vector2(220, 260), Color.gray).GetComponent<Image>();

            // 오른쪽 – 설명 텍스트
            detailName = MakeText(detailRoot.transform, "???",
                new Vector2(110, 155), new Vector2(350, 40), 18, Color.white);
            detailName.fontStyle = FontStyles.Bold;

            detailDesc = MakeText(detailRoot.transform, "???",
                new Vector2(110, 55), new Vector2(350, 150), 13, new Color(0.8f, 0.8f, 0.85f));
            detailDesc.alignment = TextAlignmentOptions.TopLeft;
            detailDesc.textWrappingMode = TextWrappingModes.Normal;

            // 수익 텍스트 분리 (한계 돌파 바로 위)
            var incomeGO = MakeText(detailRoot.transform, "현재 클릭 수익: 0.0 Gold (0강)",
                new Vector2(110, -45), new Vector2(350, 30), 13, new Color(1f, 0.84f, 0f));
            incomeGO.gameObject.name = "DetailIncomeText";
            incomeGO.alignment = TextAlignmentOptions.Center;
            detailIncomeText = incomeGO;

            // 한계 돌파 버튼 (장착하기 위)
            var breakthroughBtnGO = MakeButton(detailRoot.transform, "한계 돌파",
                new Vector2(110, -95), new Vector2(200, 44), new Color(0.70f, 0.45f, 0.05f), OnDetailBreakthrough);
            detailBreakthroughBtn = breakthroughBtnGO.GetComponent<Button>();
            detailBreakthroughBtnText = breakthroughBtnGO.GetComponentInChildren<TMP_Text>();

            // 장착 버튼
            var equipBtnGO = MakeButton(detailRoot.transform, "장착하기",
                new Vector2(110, -155), new Vector2(200, 44), BtnColor, OnDetailEquip);
            detailEquipBtn = equipBtnGO.GetComponent<Button>();

            // 닫기 버튼
            MakeButton(detailRoot.transform, "✕ 닫기",
                new Vector2(290, 185), new Vector2(70, 34), new Color(0.5f, 0.15f, 0.15f), CloseDetail);
        }

        // ════════════════════════════════════════════════════════════════════
        //  TAB SWITCHING
        // ════════════════════════════════════════════════════════════════════
        private void ShowNoTab()
        {
            showNoTab = true;
            if (noPanel  != null) noPanel.SetActive(true);
            if (setPanel != null) setPanel.SetActive(false);

            SetBtnColor(tabNoBtn,  TabActive);
            SetBtnColor(tabSetBtn, TabInactive);
            currentPageIdx = 0;
            Refresh();
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

        // ════════════════════════════════════════════════════════════════════
        //  REFRESH – No. TAB
        // ════════════════════════════════════════════════════════════════════
        private void Refresh()
        {
            if (gm == null) return;
            if (!showNoTab) { RefreshSets(); return; }

            var cards  = gm.CardCatalog?.Cards;
            if (cards == null) return;

            int total   = cards.Count;
            int perPage = slots.Count > 0 ? slots.Count : 16;
            int maxPage = Mathf.Max(0, (total - 1) / perPage);
            currentPageIdx = Mathf.Clamp(currentPageIdx, 0, maxPage);

            if (pageLabel != null)
                pageLabel.text = $"{currentPageIdx + 1} / {maxPage + 1}";
            if (prevPageBtn != null) prevPageBtn.GetComponent<Button>().interactable = currentPageIdx > 0;
            if (nextPageBtn != null) nextPageBtn.GetComponent<Button>().interactable = currentPageIdx < maxPage;

            var states = gm.GetCardStates();
            int startIdx = currentPageIdx * perPage;

            for (int i = 0; i < slots.Count; i++)
            {
                int cardIdx = startIdx + i;
                var slot = slots[i];
                if (cardIdx < total)
                {
                    slot.go.SetActive(true);
                    var card     = cards[cardIdx];
                    states.TryGetValue(card.Id, out var prog);
                    slot.ui.SetData(card, cardIdx + 1, prog, gm, OpenDetail);
                }
                else
                {
                    slot.go.SetActive(false);
                }
            }
        }

        private void OnSlotClicked(int index)
        {
            int perPage = slots.Count > 0 ? slots.Count : 16;
            int cardIdx = (currentPageIdx * perPage) + index;
            var cards = gm.CardCatalog?.Cards;
            if (cards != null && cardIdx < cards.Count) OpenDetail(cards[cardIdx].Id);
        }

        private void ChangePage(int delta)
        {
            currentPageIdx += delta;
            Refresh();
        }

        private void ChangeSetPage(int delta)
        {
            var sets = gm.SetCatalog?.Sets;
            if (sets == null || sets.Count == 0) return;
            int maxPages = Mathf.CeilToInt(sets.Count / 2f);
            setPageIndex = Mathf.Clamp(setPageIndex + delta, 0, maxPages - 1);
            Refresh();
        }

        // ════════════════════════════════════════════════════════════════════
        //  REFRESH – Set TAB
        // ════════════════════════════════════════════════════════════════════
        private void CleanDynamicSetPageChildren(Transform pageTf)
        {
            if (pageTf == null) return;
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in pageTf)
            {
                string n = child.name;
                if (n == "SetNameTitle" || n.StartsWith("Slot_") || n == "ClaimBtn" || n == "BlankPageText")
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
            {
                DestroyImmediate(go);
            }
        }

        private void RefreshSets()
        {
            if (leftSetPage == null || rightSetPage == null) return;
            if (gm == null) return;

            // Clean only dynamically spawned components, preserving user manual design layouts
            CleanDynamicSetPageChildren(leftSetPage.transform);
            CleanDynamicSetPageChildren(rightSetPage.transform);

            var sets = gm.SetCatalog?.Sets;
            if (sets == null || sets.Count == 0)
            {
                if (setPageLabel != null) setPageLabel.text = "0 / 0";
                return;
            }

            int maxPages = Mathf.CeilToInt(sets.Count / 2f);
            if (setPageIndex >= maxPages) setPageIndex = maxPages - 1;
            if (setPageIndex < 0) setPageIndex = 0;

            if (setPageLabel != null) setPageLabel.text = $"{setPageIndex + 1} / {maxPages}";

            // Left Page Set
            int leftIdx = setPageIndex * 2;
            if (leftIdx < sets.Count)
            {
                RenderSetOnPage(leftSetPage.transform, sets[leftIdx]);
            }

            // Right Page Set
            int rightIdx = setPageIndex * 2 + 1;
            if (rightIdx < sets.Count)
            {
                RenderSetOnPage(rightSetPage.transform, sets[rightIdx]);
            }
            else
            {
                var blankTx = MakeText(rightSetPage.transform, "공백 페이지", new Vector2(0, 0), new Vector2(250, 40), 14, new Color(0.6f, 0.6f, 0.6f));
                blankTx.gameObject.name = "BlankPageText";
            }

            // Update Set Nav buttons interactable states
            if (prevSetPageBtn != null) prevSetPageBtn.GetComponent<Button>().interactable = (setPageIndex > 0);
            if (nextSetPageBtn != null) nextSetPageBtn.GetComponent<Button>().interactable = (setPageIndex < maxPages - 1);
        }

        private void RenderSetOnPage(Transform pageTf, SetEntry setEntry)
        {
            if (setEntry == null) return;

            // 1. Set Name
            var title = MakeText(pageTf, setEntry.SetName, new Vector2(0, 180), new Vector2(380, 40), 16, Color.black);
            title.gameObject.name = "SetNameTitle";
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;

            // 2. Card slots layout
            var states = gm.GetCardStates();
            var catalog = gm.CardCatalog?.Cards;
            int cardCount = setEntry.CardIds.Count;

            float spacing = 95f;
            float startX = -(cardCount - 1) * spacing / 2f;

            bool allOwned = true;

            for (int i = 0; i < cardCount; i++)
            {
                string cid = setEntry.CardIds[i];
                states.TryGetValue(cid, out var prog);
                bool unlocked = prog != null && prog.Unlocked;
                if (!unlocked) allOwned = false;

                var displayCard = catalog != null
                    ? System.Linq.Enumerable.FirstOrDefault(catalog, c => c.Id == cid)
                    : null;

                // Slot container (same size as normal slot)
                var slotGO = MakeEmptyRT(pageTf, "Slot_" + cid, new Vector2(startX + (i * spacing), 20), new Vector2(85, 120));
                
                // Card slot background image
                var slotBg = slotGO.AddComponent<Image>();
                slotBg.color = new Color(0.12f, 0.14f, 0.20f, 0.15f);

                if (unlocked && displayCard != null)
                {
                    // Draw card image/icon
                    var imgRT = MakeEmptyRT(slotGO.transform, "Icon", Vector2.zero, new Vector2(75, 75));
                    var img = imgRT.AddComponent<Image>();
                    img.sprite = displayCard.CardSprite; // Use card sprite
                    img.preserveAspect = true;

                    // Detail click button overlay
                    var btn = slotGO.AddComponent<Button>();
                    string capturedId = cid;
                    btn.onClick.AddListener(() => OpenDetail(capturedId));

                    // Show stack multiplier text below icon
                    if (prog.Copies > 1)
                    {
                        var stackTx = MakeText(slotGO.transform, $"x{prog.Copies}", new Vector2(0, -45), new Vector2(75, 20), 10, Color.black);
                        stackTx.fontStyle = FontStyles.Bold;
                        stackTx.alignment = TextAlignmentOptions.Center;
                    }
                }
                else
                {
                    // Undiscovered / Unlocked card placeholder
                    var unkRT = MakeEmptyRT(slotGO.transform, "Unk", Vector2.zero, new Vector2(85, 120));
                    var unkImg = unkRT.AddComponent<Image>();
                    unkImg.color = new Color(0.12f, 0.14f, 0.20f, 0.7f);

                    var qTx = MakeText(unkRT.transform, "?", Vector2.zero, new Vector2(50, 50), 24, new Color(0.5f, 0.5f, 0.5f));
                    qTx.fontStyle = FontStyles.Bold;
                    qTx.alignment = TextAlignmentOptions.Center;
                }
            }

            // 3. Reward / Completion Claim Button
            var claimBtnGO = MakeButton(pageTf, "보상 받기", new Vector2(0, -160), new Vector2(180, 44), new Color(0.70f, 0.45f, 0.05f), () =>
            {
                gm.ClaimSetReward(setEntry.SetId);
                Refresh();
            });
            claimBtnGO.name = "ClaimBtn";

            var claimBtn = claimBtnGO.GetComponent<Button>();
            var claimBtnText = claimBtnGO.GetComponentInChildren<TMP_Text>();

            bool claimed = gm.IsSetRewardClaimed(setEntry.SetId);
            if (claimed)
            {
                if (claimBtnText != null) claimBtnText.text = "수령 완료";
                SetButtonInteractable(claimBtn, false);
                claimBtnGO.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.2f);
            }
            else
            {
                if (claimBtnText != null) claimBtnText.text = "보상 받기";
                SetButtonInteractable(claimBtn, allOwned);
                claimBtnGO.GetComponent<Image>().color = allOwned ? new Color(0.70f, 0.45f, 0.05f) : new Color(0.3f, 0.3f, 0.3f);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  DETAIL POPUP
        // ════════════════════════════════════════════════════════════════════
        private void SetButtonInteractable(Button btn, bool interactable)
        {
            if (btn == null) return;
            btn.interactable = interactable;
            var cg = btn.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = btn.gameObject.AddComponent<CanvasGroup>();
            }
            cg.alpha = interactable ? 1f : 0.4f;
        }

        private void OpenDetail(string cardId)
        {
            selectedCardId = cardId;
            if (detailRoot == null) return;

            var card    = gm?.CardCatalog?.FindById(cardId);
            var states  = gm?.GetCardStates();
            CardProgress prog = null;
            states?.TryGetValue(cardId, out prog);
            bool unlocked = prog != null && prog.Unlocked;

            if (detailName != null)
                detailName.text = unlocked && card != null ? card.DisplayName : "???";

            if (detailDesc != null)
            {
                detailDesc.text = unlocked && card != null ? card.GetDescription() : "수집되지 않은 카드입니다.";
            }

            if (detailIncomeText != null)
            {
                if (unlocked && card != null && prog != null)
                {
                    detailIncomeText.gameObject.SetActive(true);
                    int lvl = prog.BreakthroughCount;
                    double income = card.ClickMultiplier * (1 + lvl);
                    if (lvl >= 5)
                    {
                        income += card.ClickMultiplier;
                    }
                    detailIncomeText.text = $"현재 클릭 수익: {income:F1} Gold ({lvl}강)";
                }
                else
                {
                    detailIncomeText.gameObject.SetActive(false);
                }
            }

            if (detailImage != null)
            {
                if (unlocked && card?.CardSprite != null)
                {
                    detailImage.sprite = card.CardSprite;
                    detailImage.color  = Color.white;
                }
                else if (card != null)
                {
                    detailImage.sprite = null;
                    detailImage.color  = (Color)RarityToColor(card.Rarity);
                }
            }

            SetButtonInteractable(detailEquipBtn, unlocked);

            if (detailBreakthroughBtn != null)
            {
                if (unlocked && card != null && prog != null)
                {
                    detailBreakthroughBtn.gameObject.SetActive(true);

                    bool hasUnusedCopies = prog.Copies > prog.BreakthroughCount + 1;
                    bool canUpgrade = hasUnusedCopies && prog.BreakthroughCount < 5;
                    SetButtonInteractable(detailBreakthroughBtn, canUpgrade);

                    if (detailBreakthroughBtnText != null)
                    {
                        if (prog.BreakthroughCount >= 5)
                        {
                            detailBreakthroughBtnText.text = "한계 돌파\n(최대 강화)";
                        }
                        else
                        {
                            detailBreakthroughBtnText.text = $"한계 돌파\n({prog.Copies} / {prog.BreakthroughCount + 2})";
                        }
                    }
                }
                else
                {
                    detailBreakthroughBtn.gameObject.SetActive(false);
                }
            }

            detailRoot.SetActive(true);
        }

        private void CloseDetail()
        {
            if (detailRoot != null) detailRoot.SetActive(false);
        }

        private void OnDetailEquip()
        {
            if (!string.IsNullOrEmpty(selectedCardId))
                gm?.EquipCard(selectedCardId);
            CloseDetail();
        }

        private void OnDetailBreakthrough()
        {
            if (string.IsNullOrEmpty(selectedCardId) || gm == null) return;
            if (gm.BreakthroughCard(selectedCardId))
            {
                // Refresh popup
                OpenDetail(selectedCardId);
            }
        }

        private void OnClose()
        {
            if (detailRoot != null && detailRoot.activeSelf)
            {
                detailRoot.SetActive(false);
                return;
            }
            gameObject.SetActive(false);
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
            go.AddComponent<Image>().color = BG;
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
            tx.text      = text;
            tx.fontSize  = fontSize;
            tx.color     = col;
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
            tx.text      = label;
            tx.fontSize  = Mathf.Clamp((int)(size.y * 0.45f), 10, 20);
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

        public void EnsureShopButtonCleanedUp()
        {
            var panelTrans = transform.Find("Panel");
            if (panelTrans == null) return;

            var existing = panelTrans.Find("Btn_상점");
            if (existing != null)
            {
                DestroyImmediate(existing.gameObject);
            }
        }

        private void AutoWireFields()
        {
            // 1. noPanel & setPanel (Name mapping: NoPanel/NoTabRoot, SetPanel/SetTabRoot)
            if (noPanel == null)
            {
                var t = transform.Find("Panel/NoPanel") ?? transform.Find("Panel/NoTabRoot") ?? transform.Find("NoPanel") ?? transform.Find("NoTabRoot");
                if (t == null) t = FindChildByNameRecursive(transform, "NoPanel") ?? FindChildByNameRecursive(transform, "NoTabRoot");
                if (t != null) noPanel = t.gameObject;
            }
            if (setPanel == null)
            {
                var t = transform.Find("Panel/SetPanel") ?? transform.Find("Panel/SetTabRoot") ?? transform.Find("SetPanel") ?? transform.Find("SetTabRoot");
                if (t == null) t = FindChildByNameRecursive(transform, "SetPanel") ?? FindChildByNameRecursive(transform, "SetTabRoot");
                if (t != null) setPanel = t.gameObject;
            }

            // 2. Set tab sub-elements if setPanel is found
            if (setPanel != null)
            {
                var setRootTf = setPanel.transform;

                if (leftSetPage == null)
                {
                    var t = setRootTf.Find("LeftPage") ?? setRootTf.Find("Page");
                    if (t == null)
                    {
                        foreach (Transform child in setRootTf)
                        {
                            if (child.name.Contains("Page") && child.gameObject != rightSetPage)
                            {
                                t = child;
                                break;
                            }
                        }
                    }
                    if (t != null) leftSetPage = t.gameObject;
                }

                if (rightSetPage == null)
                {
                    var t = setRootTf.Find("RightPage");
                    if (t == null)
                    {
                        bool skippedFirst = false;
                        foreach (Transform child in setRootTf)
                        {
                            if (child.name.Contains("Page"))
                            {
                                if (!skippedFirst && child.gameObject == leftSetPage)
                                {
                                    skippedFirst = true;
                                    continue;
                                }
                                t = child;
                                break;
                            }
                        }
                    }
                    if (t != null) rightSetPage = t.gameObject;
                }

                if (prevSetPageBtn == null)
                {
                    var t = setRootTf.Find("Btn_◀") ?? setRootTf.Find("prevSetPageBtn") ?? FindChildByNameRecursive(setRootTf, "Btn_◀");
                    if (t != null) prevSetPageBtn = t.gameObject;
                }

                if (nextSetPageBtn == null)
                {
                    var t = setRootTf.Find("Btn_▶") ?? setRootTf.Find("nextSetPageBtn") ?? FindChildByNameRecursive(setRootTf, "Btn_▶");
                    if (t != null) nextSetPageBtn = t.gameObject;
                }

                if (setPageLabel == null)
                {
                    setPageLabel = setRootTf.GetComponentInChildren<TMP_Text>();
                }
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
