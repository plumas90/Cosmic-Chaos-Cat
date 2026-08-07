using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to card_list_test_btn or auto-wired in scene.
    /// Manually steps through all registered cards from Card 1 to N matching the exact original CardBase specification.
    /// Uses pristine GachaPanel CardBase template so card size, font, name layout, frame, and backgrounds
    /// match the original gacha card design at the very beginning 100%!
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardListTestButton : MonoBehaviour
    {
        [SerializeField] private Button testButton;
        [SerializeField] private GameObject animContainer;

        [Header("Pre-placed Scene Control Buttons (Drag in Inspector or let Auto-Find)")]
        [SerializeField] private Button sceneNextBtn;
        [SerializeField] private Button scenePrevBtn;
        [SerializeField] private Button sceneCloseBtn;

        private Coroutine activeFlowCoroutine;
        private int targetCardIndex = 0;
        private bool isClosedRequested = false;

        private void Awake()
        {
            EnsureSetup();
        }

        private void OnEnable()
        {
            EnsureSetup();
        }

        private void EnsureSetup()
        {
            if (testButton == null) testButton = GetComponent<Button>();
            if (testButton == null && gameObject.name.ToLower().Contains("card_list_test"))
                testButton = gameObject.AddComponent<Button>();

            if (testButton == null)
            {
                var btns = FindObjectsOfType<Button>(true);
                foreach (var b in btns)
                {
                    if (b != null && b.name.ToLower().Contains("card_list_test"))
                    {
                        testButton = b;
                        break;
                    }
                }
            }

            if (testButton != null)
            {
                testButton.onClick.RemoveListener(OnTestButtonClicked);
                testButton.onClick.AddListener(OnTestButtonClicked);

                if (animContainer == null)
                {
                    var ac = testButton.transform.Find("AnimContainer")
                          ?? testButton.transform.Find("animcontainer")
                          ?? testButton.transform.Find("Animcontainer");
                    if (ac != null) animContainer = ac.gameObject;
                }
            }

            AutoWireSceneControlButtons();
        }

        private void AutoWireSceneControlButtons()
        {
            if (animContainer == null && testButton != null)
            {
                var ac = testButton.transform.Find("AnimContainer")
                      ?? testButton.transform.Find("animcontainer");
                if (ac != null) animContainer = ac.gameObject;
            }

            Transform rootSearch = animContainer != null ? animContainer.transform : transform;

            if (sceneNextBtn == null)
            {
                var t = rootSearch.Find("Btn_다음") ?? rootSearch.Find("NextBtn") ?? rootSearch.Find("Controls/Btn_다음") ?? rootSearch.Find("Controls/NextBtn") ?? FindChildByNameRecursive(rootSearch, "Btn_다음");
                if (t != null) sceneNextBtn = t.GetComponent<Button>();
            }
            if (scenePrevBtn == null)
            {
                var t = rootSearch.Find("Btn_이전") ?? rootSearch.Find("PrevBtn") ?? rootSearch.Find("Controls/Btn_이전") ?? rootSearch.Find("Controls/PrevBtn") ?? FindChildByNameRecursive(rootSearch, "Btn_이전");
                if (t != null) scenePrevBtn = t.GetComponent<Button>();
            }
            if (sceneCloseBtn == null)
            {
                var t = rootSearch.Find("Btn_닫기") ?? rootSearch.Find("CloseBtn") ?? rootSearch.Find("Controls/Btn_닫기") ?? rootSearch.Find("Controls/CloseBtn") ?? FindChildByNameRecursive(rootSearch, "Btn_닫기");
                if (t != null) sceneCloseBtn = t.GetComponent<Button>();
            }
        }

        public void OnTestButtonClicked()
        {
            var gm = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>(true);

            IReadOnlyList<CardEntry> cards = null;
#if UNITY_EDITOR
            var editorCat = UnityEditor.AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            if (editorCat != null && editorCat.Cards != null && editorCat.Cards.Count > 0)
                cards = editorCat.Cards;
#endif
            if (cards == null && gm != null && gm.CardCatalog != null)
                cards = gm.CardCatalog.Cards;

            if (cards == null || cards.Count == 0)
            {
                Debug.LogWarning("[CardListTestButton] GameManager or CardCatalog not found!");
                return;
            }

            Debug.Log($"[CardListTestButton] Loaded {cards.Count} total cards from CardCatalog!");

            // Locate or Auto-Create AnimContainer
            if (animContainer == null && testButton != null)
            {
                var ac = testButton.transform.Find("AnimContainer")
                      ?? testButton.transform.Find("animcontainer")
                      ?? testButton.transform.Find("Animcontainer");
                if (ac != null) animContainer = ac.gameObject;
            }

            if (animContainer == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var acGo = new GameObject("AnimContainerAuto", typeof(RectTransform));
                    acGo.transform.SetParent(canvas.transform, false);
                    var rt = acGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                    animContainer = acGo;
                }
            }

            if (animContainer == null)
            {
                Debug.LogWarning("[CardListTestButton] AnimContainer not found!");
                return;
            }

            // If active and running, stop and toggle off
            if (activeFlowCoroutine != null)
            {
                StopCoroutine(activeFlowCoroutine);
                activeFlowCoroutine = null;
                animContainer.SetActive(false);
                return;
            }

            animContainer.SetActive(true);
            animContainer.transform.SetAsLastSibling();

            activeFlowCoroutine = StartCoroutine(PlayManualCardBrowser(animContainer, cards, gm));
        }

        private IEnumerator PlayManualCardBrowser(GameObject container, IReadOnlyList<CardEntry> cards, GameManager gm)
        {
            isClosedRequested = false;

            // 1. Find Korean TMP font asset in scene (filtering out LiberationSans SDF)
            TMP_FontAsset mainFont = null;
            var fontCandidates = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in fontCandidates)
            {
                if (t != null && t.font != null && t.font.name != "LiberationSans SDF")
                {
                    mainFont = t.font;
                    break;
                }
            }
            if (mainFont == null && fontCandidates.Length > 0)
                mainFont = fontCandidates[0].font;

            var gachaPanel = FindObjectOfType<GachaPanel>(true);

            // 2. Obtain exact CardBase template from GachaPanel
            RectTransform cardBaseTemplate = null;
            if (gachaPanel != null)
            {
                cardBaseTemplate = gachaPanel.GetAnimCardTemplate();
            }

            if (cardBaseTemplate == null)
            {
                var cbGo = GameObject.Find("CardBase") ?? GameObject.Find("SummaryCardBase");
                if (cbGo != null) cardBaseTemplate = cbGo.GetComponent<RectTransform>();
            }

            // Keep template CardBase hidden in background
            if (cardBaseTemplate != null)
            {
                cardBaseTemplate.gameObject.SetActive(false);
            }

            // 3. Build / Locate FlowView UI inside AnimContainer
            Transform flowTrans = container.transform.Find("TestFlowView");
            GameObject flowObj = null;

            if (flowTrans == null)
            {
                flowObj = new GameObject("TestFlowView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
                flowObj.transform.SetParent(container.transform, false);
                var fRt = flowObj.GetComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero; fRt.anchorMax = Vector2.one;
                fRt.offsetMin = fRt.offsetMax = Vector2.zero;

                var fImg = flowObj.GetComponent<Image>();
                fImg.color = new Color(0.04f, 0.06f, 0.10f, 0.96f);
            }
            else
            {
                flowObj = flowTrans.gameObject;
            }

            flowObj.SetActive(true);

            // Title & Counter Header at Top (Applied with mainFont)
            TMP_Text headerText = flowObj.transform.Find("Header")?.GetComponent<TMP_Text>();
            if (headerText == null)
            {
                var hGo = new GameObject("Header", typeof(RectTransform));
                hGo.transform.SetParent(flowObj.transform, false);
                var hRt = hGo.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0f, 0.88f); hRt.anchorMax = new Vector2(1f, 0.98f);
                hRt.offsetMin = hRt.offsetMax = Vector2.zero;

                headerText = hGo.AddComponent<TextMeshProUGUI>();
                headerText.fontSize = 20;
                headerText.alignment = TextAlignmentOptions.Center;
                headerText.color = new Color(1f, 0.9f, 0.4f, 1f);
            }
            if (mainFont != null) headerText.font = mainFont;

            // Card Parent Holder (Positioned in center)
            Transform holderTrans = flowObj.transform.Find("CardHolder");
            GameObject holderObj = null;
            if (holderTrans == null)
            {
                holderObj = new GameObject("CardHolder", typeof(RectTransform));
                holderObj.transform.SetParent(flowObj.transform, false);
                var hRt = holderObj.GetComponent<RectTransform>();
                hRt.anchorMin = new Vector2(0.5f, 0.52f); hRt.anchorMax = new Vector2(0.5f, 0.52f);
                hRt.anchoredPosition = new Vector2(0f, 10f);
                hRt.sizeDelta = new Vector2(400f, 300f);
            }
            else
            {
                holderObj = holderTrans.gameObject;
            }
            holderObj.SetActive(true);

            // 4. Controls Panel (Priority: Check for User Pre-placed Buttons in Scene First!)
            AutoWireSceneControlButtons();

            Button prevBtn = scenePrevBtn;
            Button nextBtn = sceneNextBtn;
            Button closeBtn = sceneCloseBtn;

            Button prev10Btn = null;
            Button next10Btn = null;
            Button prev100Btn = null;
            Button next100Btn = null;

            targetCardIndex = 0;

            Transform ctrlTrans = flowObj.transform.Find("Controls");
            if (ctrlTrans == null)
            {
                var ctrlGo = new GameObject("Controls", typeof(RectTransform));
                ctrlGo.transform.SetParent(flowObj.transform, false);
                var cRt = ctrlGo.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.5f, 0.5f); cRt.anchorMax = new Vector2(0.5f, 0.5f);
                cRt.anchoredPosition = new Vector2(0f, -260f);
                cRt.sizeDelta = new Vector2(360f, 130f);
                ctrlTrans = ctrlGo.transform;

                if (prevBtn == null) prevBtn  = CreateControlBtn(ctrlTrans, "Btn_이전", "◀ 이전", new Vector2(-110f, 0f), mainFont);
                if (nextBtn == null) nextBtn  = CreateControlBtn(ctrlTrans, "Btn_다음", "다음 ▶", new Vector2(0f, 0f), mainFont);
                if (closeBtn == null) closeBtn = CreateControlBtn(ctrlTrans, "Btn_닫기", "닫기 ✕", new Vector2(110f, 0f), mainFont);
            }
            else
            {
                var cRt = ctrlTrans.GetComponent<RectTransform>();
                if (cRt != null)
                {
                    cRt.anchorMin = new Vector2(0.5f, 0.5f); cRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cRt.anchoredPosition = new Vector2(0f, -260f);
                }

                if (prevBtn == null) prevBtn  = ctrlTrans.Find("Btn_이전")?.GetComponent<Button>() ?? ctrlTrans.Find("PrevBtn")?.GetComponent<Button>();
                if (nextBtn == null) nextBtn  = ctrlTrans.Find("Btn_다음")?.GetComponent<Button>() ?? ctrlTrans.Find("NextBtn")?.GetComponent<Button>();
                if (closeBtn == null) closeBtn = ctrlTrans.Find("Btn_닫기")?.GetComponent<Button>() ?? ctrlTrans.Find("CloseBtn")?.GetComponent<Button>();
            }

            // Create -10/+10 and -100/+100 buttons below Row 1 (at Y = -50f and Y = -100f)
            prev10Btn  = ctrlTrans.Find("Btn_이전10")?.GetComponent<Button>()  ?? CreateControlBtn(ctrlTrans, "Btn_이전10", "◀ -10", new Vector2(-60f, -50f), mainFont);
            next10Btn  = ctrlTrans.Find("Btn_다음10")?.GetComponent<Button>()  ?? CreateControlBtn(ctrlTrans, "Btn_다음10", "+10 ▶", new Vector2(60f, -50f), mainFont);
            prev100Btn = ctrlTrans.Find("Btn_이전100")?.GetComponent<Button>() ?? CreateControlBtn(ctrlTrans, "Btn_이전100", "◀ -100", new Vector2(-60f, -100f), mainFont);
            next100Btn = ctrlTrans.Find("Btn_다음100")?.GetComponent<Button>() ?? CreateControlBtn(ctrlTrans, "Btn_다음100", "+100 ▶", new Vector2(60f, -100f), mainFont);

            if (prevBtn != null)
            {
                prevBtn.gameObject.SetActive(true);
                prevBtn.onClick.RemoveAllListeners();
                prevBtn.onClick.AddListener(() => { targetCardIndex = Mathf.Max(0, targetCardIndex - 1); });
                var txt = prevBtn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (nextBtn != null)
            {
                nextBtn.gameObject.SetActive(true);
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(() => { targetCardIndex = Mathf.Min(cards.Count - 1, targetCardIndex + 1); });
                var txt = nextBtn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (prev10Btn != null)
            {
                prev10Btn.gameObject.SetActive(true);
                prev10Btn.onClick.RemoveAllListeners();
                prev10Btn.onClick.AddListener(() => { targetCardIndex = Mathf.Max(0, targetCardIndex - 10); });
                var txt = prev10Btn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (next10Btn != null)
            {
                next10Btn.gameObject.SetActive(true);
                next10Btn.onClick.RemoveAllListeners();
                next10Btn.onClick.AddListener(() => { targetCardIndex = Mathf.Min(cards.Count - 1, targetCardIndex + 10); });
                var txt = next10Btn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (prev100Btn != null)
            {
                prev100Btn.gameObject.SetActive(true);
                prev100Btn.onClick.RemoveAllListeners();
                prev100Btn.onClick.AddListener(() => { targetCardIndex = Mathf.Max(0, targetCardIndex - 100); });
                var txt = prev100Btn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (next100Btn != null)
            {
                next100Btn.gameObject.SetActive(true);
                next100Btn.onClick.RemoveAllListeners();
                next100Btn.onClick.AddListener(() => { targetCardIndex = Mathf.Min(cards.Count - 1, targetCardIndex + 100); });
                var txt = next100Btn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            if (closeBtn != null)
            {
                closeBtn.gameObject.SetActive(true);
                closeBtn.onClick.RemoveAllListeners();
                closeBtn.onClick.AddListener(() => { isClosedRequested = true; });
                var txt = closeBtn.GetComponentInChildren<TMP_Text>();
                if (txt != null && mainFont != null) txt.font = mainFont;
            }

            int currentDisplayedIndex = -1;

            while (!isClosedRequested)
            {
                // If displayed index needs update
                if (currentDisplayedIndex != targetCardIndex)
                {
                    currentDisplayedIndex = targetCardIndex;
                    var card = cards[currentDisplayedIndex];
                    if (card != null)
                    {
                        // Clear previous card inside holderObj
                        foreach (Transform child in holderObj.transform)
                            DestroyImmediate(child.gameObject);

                        // Update Header
                        if (headerText != null)
                        {
                            if (mainFont != null) headerText.font = mainFont;
                            headerText.text = $"[ 카드 {currentDisplayedIndex + 1} / {cards.Count} ]  No.{card.Id} {card.GetDisplayName()}";
                        }

                        // Instantiate card slot using PRISTINE CardBase template!
                        GameObject cardSlot = null;
                        if (cardBaseTemplate != null)
                        {
                            cardSlot = Instantiate(cardBaseTemplate.gameObject, holderObj.transform, false);
                        }
                        else
                        {
                            cardSlot = new GameObject($"CardSlot_{card.Id}", typeof(RectTransform), typeof(Image));
                            cardSlot.transform.SetParent(holderObj.transform, false);
                            var rt = cardSlot.GetComponent<RectTransform>();
                            rt.sizeDelta = new Vector2(130f, 190f);
                        }

                        cardSlot.name = $"CardSlot_{card.Id}";
                        cardSlot.SetActive(true);
                        cardSlot.transform.localScale = Vector3.one;

                        var slotRt = cardSlot.GetComponent<RectTransform>();
                        slotRt.anchoredPosition = Vector2.zero;

                        // Render exact original CardBase structure
                        RenderExactCardBaseSlot(cardSlot, card, mainFont, gachaPanel);
                    }
                }

                yield return null;
            }

            // Closed requested
            flowObj.SetActive(false);
            container.SetActive(false);
            if (animContainer != null) animContainer.SetActive(false);

            if (gachaPanel != null)
            {
                var resultField = typeof(GachaPanel).GetField("resultObj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (resultField != null)
                {
                    var resObj = resultField.GetValue(gachaPanel) as GameObject;
                    if (resObj != null && resObj != container && resObj != animContainer) resObj.SetActive(false);
                }
            }

            activeFlowCoroutine = null;
        }

        private void RenderExactCardBaseSlot(GameObject cardSlot, CardEntry card, TMP_FontAsset mainFont, GachaPanel gachaPanel)
        {
            if (cardSlot == null || card == null) return;

            // Show Front face, Hide Back face (exact CardBase specification)
            var backTrans = cardSlot.transform.Find("Back");
            if (backTrans != null) backTrans.gameObject.SetActive(false);

            var frontTrans = cardSlot.transform.Find("Front");
            if (frontTrans == null) frontTrans = cardSlot.transform;
            frontTrans.gameObject.SetActive(true);

            // Bind Card Data via GachaPanel for 100% original card design fidelity
            if (gachaPanel != null)
            {
                gachaPanel.BindCardFrontData(frontTrans, card);
            }

            // Update Name Text inside Front/Name or Front/NameText using original font/layout
            var nameTxt = frontTrans.Find("Name")?.GetComponent<TMP_Text>()
                       ?? frontTrans.Find("NameText")?.GetComponent<TMP_Text>()
                       ?? frontTrans.GetComponentInChildren<TMP_Text>(true);

            if (nameTxt != null)
            {
                if (mainFont != null) nameTxt.font = mainFont;
                nameTxt.text = $"No.{card.Id}\n{card.GetDisplayName()}";
            }
        }

        private Button CreateControlBtn(Transform parent, string name, string labelText, Vector2 pos, TMP_FontAsset font)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(95f, 38f);

            btnGo.GetComponent<Image>().color = new Color(0.18f, 0.28f, 0.42f, 1f);
            var btn = btnGo.GetComponent<Button>();

            var txtGo = new GameObject("Text", typeof(RectTransform));
            txtGo.transform.SetParent(btnGo.transform, false);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
            tRt.offsetMin = tRt.offsetMax = Vector2.zero;

            var txt = txtGo.AddComponent<TextMeshProUGUI>();
            if (font != null) txt.font = font;
            txt.text = labelText;
            txt.fontSize = 14;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            return btn;
        }

        private Transform FindChildByNameRecursive(Transform parent, string name)
        {
            if (parent == null) return null;
            string targetLower = name.ToLower();
            foreach (Transform child in parent)
            {
                if (child.name.ToLower() == targetLower) return child;
                var found = FindChildByNameRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Create Preplaced Control Buttons In Scene")]
        public void CreatePreplacedControlButtonsInScene()
        {
            // Do NOT run scene dirty in Play mode
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) return;

            EnsureSetup();
            if (animContainer == null)
            {
                Debug.LogWarning("[CardListTestButton] animContainer is null. Please assign it first!");
                return;
            }

            TMP_FontAsset mainFont = null;
            var fontCandidates = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in fontCandidates)
            {
                if (t != null && t.font != null && t.font.name != "LiberationSans SDF")
                {
                    mainFont = t.font;
                    break;
                }
            }

            Transform ctrlTrans = animContainer.transform.Find("Controls");
            if (ctrlTrans == null)
            {
                var ctrlGo = new GameObject("Controls", typeof(RectTransform));
                ctrlGo.transform.SetParent(animContainer.transform, false);
                var cRt = ctrlGo.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.5f, 0f); cRt.anchorMax = new Vector2(0.5f, 0f);
                cRt.anchoredPosition = new Vector2(0f, 25f);
                cRt.sizeDelta = new Vector2(360f, 45f);
                ctrlTrans = ctrlGo.transform;
            }

            scenePrevBtn = ctrlTrans.Find("Btn_이전")?.GetComponent<Button>() ?? CreateControlBtn(ctrlTrans, "Btn_이전", "◀ 이전", new Vector2(-110f, 0f), mainFont);
            sceneNextBtn = ctrlTrans.Find("Btn_다음")?.GetComponent<Button>() ?? CreateControlBtn(ctrlTrans, "Btn_다음", "다음 ▶", new Vector2(0f, 0f), mainFont);
            sceneCloseBtn = ctrlTrans.Find("Btn_닫기")?.GetComponent<Button>() ?? CreateControlBtn(ctrlTrans, "Btn_닫기", "닫기 ✕", new Vector2(110f, 0f), mainFont);

            UnityEditor.Undo.RegisterCreatedObjectUndo(ctrlTrans.gameObject, "Create Preplaced Control Buttons");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.EditorUtility.SetDirty(animContainer);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

            Debug.Log("[CardListTestButton] ✅ [Btn_이전], [Btn_다음], [Btn_닫기] 버튼이 씬에 성공적으로 사전 생성되었습니다! 씬 뷰에서 자유롭게 위치를 조정해 보세요.");
        }
#endif
    }
}
