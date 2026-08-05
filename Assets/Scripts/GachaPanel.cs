using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public sealed class GachaPanel : MonoBehaviour
    {
        private GameManager gm;
        private ClickEffectPlayer effectPlayer;

        [SerializeField] private TMP_Text moneyText;
        [SerializeField] private TMP_Text rollOnceCostText;
        [SerializeField] private TMP_Text rollTenCostText;
        
        [Header("Type Selection Buttons")]
        [SerializeField] private Button normalBtn;
        [SerializeField] private Button rareBtn;
        [SerializeField] private Button superBtn;

        [Header("Gacha Draw Buttons (Drag Inspector GameObjects here)")]
        [SerializeField] private Button rollOnceBtn; // Btn_1_Gacha
        [SerializeField] private Button rollTenBtn;  // Btn_10_Gacha

        [SerializeField] private GameObject typeSelectionObj;
        [SerializeField] private GameObject resultObj;
        [SerializeField] private Transform cardGrid; // Keep for inspector compatibility

        [System.Serializable]
        public struct RarityTheme
        {
            public Sprite frontSprite;     // Front (Card Background) Sprite
            public Sprite frameSprite;     // Frame (Card Outer Frame) Sprite
            public Sprite rareMarkSprite;  // Rare_Mark Sprite
            public Sprite nameLabelSprite; // NameLabel Sprite
        }

        [Header("Rarity Sprites Configuration")]
        [SerializeField] private RarityTheme normalRaritySprites;
        [SerializeField] private RarityTheme rareRaritySprites;
        [SerializeField] private RarityTheme superRareRaritySprites;
        [SerializeField] private RarityTheme ssrRaritySprites;
        [SerializeField] private RarityTheme urRaritySprites;
        [SerializeField] private RarityTheme hiddenRaritySprites;

        [Header("Shard Conversion Settings")]
        [SerializeField] private Sprite shardSprite;

        private RectTransform animCardTemplate;
        private RectTransform summaryCardTemplate;
        private Vector2 animCardSize = new Vector2(130f, 190f);
        private Vector2 summaryCardSize = new Vector2(110f, 160f);

        private GameObject animContainer;
        private GameObject summaryContainer;
        private RectTransform conveyor;
        private GameObject skipBtn;
        private GameObject confirmBtn;
        private GameObject closeBtn;
        private readonly Dictionary<GameObject, Vector2> slotOriginalPositions = new Dictionary<GameObject, Vector2>();
        private Vector2 confirmBtnOriginalPos = new Vector2(0f, -220f);
        private bool hasSavedOriginalPositions;
        private bool isAnimSkipped;
        private readonly List<bool> currentIsShardDraw = new List<bool>();
        private readonly List<int> currentShardsGained = new List<int>();

        private Coroutine activeGachaSequence;
        private Coroutine activeShardConversion;

        private GachaType currentType = GachaType.Normal;
        
        private static readonly Color BG = new Color(0.06f, 0.08f, 0.14f, 0.97f);
        private static readonly Color BtnType = new Color(0.15f, 0.20f, 0.30f);
        private static readonly Color BtnTypeActive = new Color(0.25f, 0.35f, 0.50f);
        private static readonly Color BtnGacha = new Color(0.20f, 0.60f, 0.35f, 1.00f);
        private static readonly Color BtnGacha10 = new Color(0.70f, 0.45f, 0.05f, 1.00f);
        private static readonly Color BtnClose = new Color(0.50f, 0.15f, 0.15f, 1.00f);

        private void Awake()
        {
            EnsureParentedToCanvas();
            AutoWireCoinText();
            if (transform.childCount == 0)
            {
                BuildUI();
            }
            EnsureGachaUIPartsBuilt();
            AutoWireButtons();
            BindListeners();
            effectPlayer = FindObjectOfType<ClickEffectPlayer>();
        }

        private void Reset()
        {
            AutoWireButtons();
        }

        private void OnValidate()
        {
            AutoWireButtons();
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
        }

        private Button EnsureButton(GameObject go)
        {
            if (go == null) return null;
            var img = go.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();
            btn.interactable = true;
            return btn;
        }

        private void AutoWireButtons()
        {
            Transform rootTrans = typeSelectionObj != null ? typeSelectionObj.transform : transform;

            if (normalBtn == null)
            {
                var t = rootTrans.Find("Btn_Normal") ?? rootTrans.Find("NormalBtn") ?? rootTrans.Find("Normal") ?? FindChildByNameRecursive(transform, "normal");
                if (t != null) normalBtn = EnsureButton(t.gameObject);
            }
            if (rareBtn == null)
            {
                var t = rootTrans.Find("Btn_Rare") ?? rootTrans.Find("RareBtn") ?? rootTrans.Find("Rare") ?? FindChildByNameRecursive(transform, "rare");
                if (t != null) rareBtn = EnsureButton(t.gameObject);
            }
            if (superBtn == null)
            {
                var t = rootTrans.Find("Btn_Super") ?? rootTrans.Find("SuperBtn") ?? rootTrans.Find("Super") ?? FindChildByNameRecursive(transform, "super");
                if (t != null) superBtn = EnsureButton(t.gameObject);
            }

            if (rollOnceBtn == null)
            {
                var t = rootTrans.Find("Btn_1_Gacha") ?? rootTrans.Find("Btn_1") ?? rootTrans.Find("RollOnce") ?? FindChildByNameRecursive(transform, "1_gacha") ?? FindChildByNameRecursive(transform, "rollonce");
                if (t != null) rollOnceBtn = EnsureButton(t.gameObject);
            }
            if (rollTenBtn == null)
            {
                var t = rootTrans.Find("Btn_10_Gacha") ?? rootTrans.Find("Btn_10") ?? rootTrans.Find("RollTen") ?? FindChildByNameRecursive(transform, "10_gacha") ?? FindChildByNameRecursive(transform, "rollten");
                if (t != null) rollTenBtn = EnsureButton(t.gameObject);
            }

            // Fallback deep scan across all transforms under GachaPanel for Btn_1_Gacha / Btn_10_Gacha
            if (rollOnceBtn == null || rollTenBtn == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    string n = child.name.ToLower();
                    if (rollOnceBtn == null && (n == "btn_1_gacha" || n == "btn_1" || (n.Contains("1") && (n.Contains("gacha") || n.Contains("뽑기")) && !n.Contains("10"))))
                    {
                        rollOnceBtn = EnsureButton(child.gameObject);
                    }
                    else if (rollTenBtn == null && (n == "btn_10_gacha" || n == "btn_10" || (n.Contains("10") && (n.Contains("gacha") || n.Contains("뽑기")))))
                    {
                        rollTenBtn = EnsureButton(child.gameObject);
                    }
                }
            }

            if (rollOnceBtn != null) EnsureButton(rollOnceBtn.gameObject);
            if (rollTenBtn != null) EnsureButton(rollTenBtn.gameObject);
            if (normalBtn != null) EnsureButton(normalBtn.gameObject);
            if (rareBtn != null) EnsureButton(rareBtn.gameObject);
            if (superBtn != null) EnsureButton(superBtn.gameObject);

            if (closeBtn == null)
            {
                var t = transform.Find("Panel/Header/Btn_Close") ?? transform.Find("Btn_Close") ?? transform.Find("CloseButton") ?? FindChildByNameRecursive(transform, "close");
                if (t != null) closeBtn = t.gameObject;
            }

            if (confirmBtn == null && resultObj != null)
            {
                var t = resultObj.transform.Find("SummaryContainer/Btn_확인") ?? resultObj.transform.Find("Btn_확인") ?? resultObj.transform.Find("ConfirmBtn") ?? FindChildByNameRecursive(resultObj.transform, "confirm");
                if (t != null) confirmBtn = t.gameObject;
            }

            if (skipBtn == null && resultObj != null)
            {
                var t = resultObj.transform.Find("AnimContainer/Btn_스킵") ?? resultObj.transform.Find("Btn_스킵") ?? resultObj.transform.Find("SkipBtn") ?? FindChildByNameRecursive(resultObj.transform, "skip");
                if (t != null) skipBtn = t.gameObject;
            }
        }

        private void BindListeners()
        {
            AutoWireButtons();

            if (normalBtn != null) { normalBtn.onClick.RemoveAllListeners(); normalBtn.onClick.AddListener(() => SelectType(GachaType.Normal)); }
            if (rareBtn != null) { rareBtn.onClick.RemoveAllListeners(); rareBtn.onClick.AddListener(() => SelectType(GachaType.Rare)); }
            if (superBtn != null) { superBtn.onClick.RemoveAllListeners(); superBtn.onClick.AddListener(() => SelectType(GachaType.Super)); }

            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                var cs = b.colors;
                cs.normalColor = Color.white;
                cs.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cs.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
                cs.selectedColor = Color.white;
                cs.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                cs.colorMultiplier = 1f;
                cs.fadeDuration = 0.08f;
                b.colors = cs;

                string tmpStr = b.GetComponentInChildren<TMP_Text>(true)?.text.ToLower() ?? "";
                string legStr = b.GetComponentInChildren<UnityEngine.UI.Text>(true)?.text.ToLower() ?? "";
                string fullText = (tmpStr + " " + legStr).Trim();
                string bName = b.name.ToLower();

                if (bName.Contains("normal") || fullText.Contains("일반") || fullText.Contains("normal"))
                {
                    if (normalBtn == null) normalBtn = b;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => SelectType(GachaType.Normal));
                    continue;
                }
                else if (bName.Contains("rare") || fullText.Contains("레어") || fullText.Contains("rare"))
                {
                    if (rareBtn == null) rareBtn = b;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => SelectType(GachaType.Rare));
                    continue;
                }
                else if (bName.Contains("super") || fullText.Contains("슈퍼") || fullText.Contains("super"))
                {
                    if (superBtn == null) superBtn = b;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => SelectType(GachaType.Super));
                    continue;
                }

                if (b == normalBtn || b == rareBtn || b == superBtn) continue;

                if (bName.Contains("10") || fullText.Contains("10회") || fullText.Contains("10뽑") || fullText.Contains("10회뽑기") || fullText.Contains("다시") || bName.Contains("10회") || bName.Contains("roll10") || bName.Contains("gacha10") || bName.Contains("reroll") || bName.Contains("ten"))
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnRollTen);
                }
                else if (bName.Contains("1") || fullText.Contains("1회") || fullText.Contains("1뽑") || fullText.Contains("1회뽑기") || bName.Contains("1회") || bName.Contains("roll1") || bName.Contains("once"))
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnRollOnce);
                }
                else if (fullText.Contains("확인") || fullText.Contains("confirm") || bName.Contains("confirm") || bName.Contains("확인"))
                {
                    if (confirmBtn == null) confirmBtn = b.gameObject;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(ConfirmResults);
                }
                else if (fullText.Contains("스킵") || fullText.Contains("skip") || bName.Contains("skip") || bName.Contains("스킵"))
                {
                    if (skipBtn == null) skipBtn = b.gameObject;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(SkipAnimation);
                }
                else if (bName.Contains("card_list_test") || fullText.Contains("card_list_test") || fullText.Contains("카드목록테스트"))
                {
                    cardListTestBtn = b;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnCardListTestClicked);
                }
                else if (fullText.Contains("✕") || fullText.Contains("닫기") || fullText.Contains("close") || bName.Contains("close") || bName.Contains("닫기"))
                {
                    if (closeBtn == null) closeBtn = b.gameObject;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(ClosePanel);
                }
            }

            if (closeBtn != null)
            {
                var btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ClosePanel);
                }
            }
        }

        private void OnEnable()
        {
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.StateChanged += RefreshCosts;
                gm.AddTestMoney(100000d);
            }
            SelectType(GachaType.Normal);
            if (resultObj != null) resultObj.SetActive(false);
            if (typeSelectionObj != null) typeSelectionObj.SetActive(true);
            if (closeBtn != null) closeBtn.SetActive(true);
        }

        private void OnDisable()
        {
            if (gm != null) gm.StateChanged -= RefreshCosts;
            StopAllCoroutines();
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

        public void EnsureGachaUIPartsBuilt()
        {
            var typeSelSanitize = typeSelectionObj != null ? typeSelectionObj.transform : transform.Find("Panel/TypeSelection");
            if (typeSelSanitize != null)
            {
                var locs = typeSelSanitize.GetComponentsInChildren<LocalizeText>(true);
                foreach (var loc in locs)
                {
                    if (loc != null && (loc.gameObject.name.Contains("Label") || loc.gameObject.name.Contains("Gacha") || loc.Key == "hud_btn_gacha"))
                    {
                        DestroyImmediate(loc);
                    }
                }
            }

            if (resultObj == null) return;

            // Normalize Btn_1_Gacha / Btn_10_Gacha dimensions and prevent VerticalLayoutGroup height stretching
            if (typeSelectionObj != null)
            {
                var lg = typeSelectionObj.GetComponent<LayoutGroup>();
                if (lg != null) lg.enabled = false;

                foreach (Transform child in typeSelectionObj.transform)
                {
                    var childLg = child.GetComponent<LayoutGroup>();
                    if (childLg != null) childLg.enabled = false;

                    string cName = child.name.ToLower();
                    if (cName.Contains("1") && (cName.Contains("gacha") || cName.Contains("뽑기")) && !cName.Contains("10"))
                    {
                        var rt = child.GetComponent<RectTransform>();
                        if (rt != null) rt.sizeDelta = new Vector2(180f, 55f);
                    }
                    else if (cName.Contains("10"))
                    {
                        var rt = child.GetComponent<RectTransform>();
                        if (rt != null) rt.sizeDelta = new Vector2(180f, 55f);
                    }
                }
            }

            // Destroy redundant Btn_확인 directly under resultObj (from legacy baked prefab)
            var oldBtnTrans = resultObj.transform.Find("Btn_확인");
            if (oldBtnTrans != null)
            {
                SafeDestroy(oldBtnTrans.gameObject);
            }

            // Find or create animContainer
            var acTrans = resultObj.transform.Find("AnimContainer");
            if (acTrans != null)
            {
                animContainer = acTrans.gameObject;
                var viewTrans = animContainer.transform.Find("Viewport");
                if (viewTrans != null)
                {
                    var convTrans = viewTrans.Find("Conveyor");
                    if (convTrans != null) conveyor = convTrans.GetComponent<RectTransform>();
                }
                var sbTrans = animContainer.transform.Find("Btn_스킵");
                if (sbTrans != null) skipBtn = sbTrans.gameObject;
            }
            else
            {
                animContainer = new GameObject("AnimContainer");
                animContainer.transform.SetParent(resultObj.transform, false);
                var acRt = animContainer.AddComponent<RectTransform>();
                acRt.anchorMin = Vector2.zero; acRt.anchorMax = Vector2.one;
                acRt.offsetMin = Vector2.zero; acRt.offsetMax = Vector2.zero;

                var maskGo = new GameObject("Viewport");
                maskGo.transform.SetParent(animContainer.transform, false);
                var mRt = maskGo.AddComponent<RectTransform>();
                mRt.anchoredPosition = Vector2.zero;
                mRt.sizeDelta = new Vector2(1500, 900);
                var vpImg = maskGo.AddComponent<Image>();
                vpImg.color = new Color(0, 0, 0, 0.01f);
                maskGo.AddComponent<Mask>().showMaskGraphic = false;

                var convGo = new GameObject("Conveyor");
                convGo.transform.SetParent(maskGo.transform, false);
                conveyor = convGo.AddComponent<RectTransform>();
                conveyor.anchoredPosition = Vector2.zero;
                conveyor.sizeDelta = new Vector2(2000, 300);

                skipBtn = MakeButton(animContainer.transform, "스킵", new Vector2(0, -180), new Vector2(150, 48), BtnClose, SkipAnimation);
            }

            if (animContainer != null)
            {
                var animBtn = animContainer.GetComponent<Button>();
                if (animBtn != null) DestroyImmediate(animBtn);

                var vpTrans = animContainer.transform.Find("Viewport");
                if (vpTrans != null)
                {
                    var vpBtn = vpTrans.GetComponent<Button>();
                    if (vpBtn != null) DestroyImmediate(vpBtn);
                }
            }

            if (skipBtn != null)
            {
                var sbBtn = skipBtn.GetComponent<Button>();
                if (sbBtn == null) sbBtn = skipBtn.AddComponent<Button>();
                var sbImg = skipBtn.GetComponent<Image>();
                if (sbImg != null) sbImg.raycastTarget = true;
                sbBtn.onClick.RemoveAllListeners();
                sbBtn.onClick.AddListener(SkipAnimation);
            }

            // Find or create pre-placed CardBase template inside Conveyor for direct Scene Editor resizing
            if (conveyor != null)
            {
                var cbTrans = conveyor.Find("CardBase");
                if (cbTrans == null)
                {
                    var cbGo = new GameObject("CardBase");
                    cbGo.transform.SetParent(conveyor, false);
                    var cbRt = cbGo.AddComponent<RectTransform>();
                    cbRt.anchoredPosition = Vector2.zero;
                    cbRt.sizeDelta = animCardSize;

                    var backGo = new GameObject("Back");
                    backGo.transform.SetParent(cbGo.transform, false);
                    var backRt = backGo.AddComponent<RectTransform>();
                    backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one;
                    backRt.offsetMin = backRt.offsetMax = Vector2.zero;
                    var backImg = backGo.AddComponent<Image>();
                    backImg.color = new Color(0.12f, 0.15f, 0.23f);

                    var qText = MakeText(backGo.transform, "?", Vector2.zero, new Vector2(100, 100), 36, new Color(0.7f, 0.8f, 1f));
                    qText.alignment = TextAlignmentOptions.Center;
                    qText.fontStyle = FontStyles.Bold;

                    var frontGo = new GameObject("Front");
                    frontGo.transform.SetParent(cbGo.transform, false);
                    var frontRt = frontGo.AddComponent<RectTransform>();
                    frontRt.anchorMin = Vector2.zero; frontRt.anchorMax = Vector2.one;
                    frontRt.offsetMin = frontRt.offsetMax = Vector2.zero;
                    var frontImg = frontGo.AddComponent<Image>();
                    frontImg.color = new Color(0.6f, 0.2f, 0.8f);

                    var artGo = new GameObject("Art");
                    artGo.transform.SetParent(frontGo.transform, false);
                    artGo.name = "Art";
                    var artRt = artGo.AddComponent<RectTransform>();
                    artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
                    artRt.offsetMin = new Vector2(6, 36); artRt.offsetMax = new Vector2(-6, -6);
                    artGo.AddComponent<Image>().color = Color.white;

                    frontGo.SetActive(false);
                    cbTrans = cbGo.transform;
                }
                animCardTemplate = cbTrans.GetComponent<RectTransform>();
            }

            // Find or create summaryContainer & buttons
            var scTrans = resultObj.transform.Find("SummaryContainer");
            if (scTrans != null)
            {
                summaryContainer = scTrans.gameObject;
                var cbTrans = summaryContainer.transform.Find("Btn_확인") ?? summaryContainer.transform.Find("ConfirmBtn");
                if (cbTrans != null) confirmBtn = cbTrans.gameObject;
            }
            else
            {
                summaryContainer = new GameObject("SummaryContainer");
                summaryContainer.transform.SetParent(resultObj.transform, false);
                var scRt = summaryContainer.AddComponent<RectTransform>();
                scRt.anchorMin = Vector2.zero; scRt.anchorMax = Vector2.one;
                scRt.offsetMin = Vector2.zero; scRt.offsetMax = Vector2.zero;

                confirmBtn = MakeButton(summaryContainer.transform, "확인", new Vector2(-100, -220), new Vector2(160, 48), BtnGacha, ConfirmResults);
                MakeButton(summaryContainer.transform, "10회 다시뽑기", new Vector2(100, -220), new Vector2(160, 48), BtnGacha10, OnRollTen);
            }

            // Find or create pre-placed SummaryCardBase template inside SummaryContainer with exact CardBase structure
            if (summaryContainer != null)
            {
                var scbTrans = summaryContainer.transform.Find("SummaryCardBase");
                if (scbTrans == null)
                {
                    GameObject scbGo;
                    if (animCardTemplate != null)
                    {
                        scbGo = Instantiate(animCardTemplate.gameObject, summaryContainer.transform, false);
                        scbGo.name = "SummaryCardBase";
                    }
                    else
                    {
                        scbGo = new GameObject("SummaryCardBase");
                        scbGo.transform.SetParent(summaryContainer.transform, false);
                    }

                    var scbRt = scbGo.GetComponent<RectTransform>() ?? scbGo.AddComponent<RectTransform>();
                    scbRt.anchoredPosition = new Vector2(0, 30);
                    scbRt.sizeDelta = summaryCardSize;

                    var backTrans = scbGo.transform.Find("Back");
                    if (backTrans != null) backTrans.gameObject.SetActive(false);

                    var frontTrans = scbGo.transform.Find("Front");
                    if (frontTrans != null) frontTrans.gameObject.SetActive(true);

                    scbTrans = scbGo.transform;
                }
                summaryCardTemplate = scbTrans.GetComponent<RectTransform>();
            }

            if (skipBtn != null)
            {
                var btn = skipBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(SkipAnimation);
                }
            }
            if (confirmBtn != null)
            {
                var btn = confirmBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ConfirmResults);
                }
            }

            var font = moneyText?.font;
            if (font != null)
            {
                if (skipBtn != null)
                {
                    var txt = skipBtn.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.font = font;
                }
                if (confirmBtn != null)
                {
                    var txt = confirmBtn.GetComponentInChildren<TMP_Text>();
                    if (txt != null) txt.font = font;
                }
            }

            // Find close button from legacy panel or new parent
            if (closeBtn == null)
            {
                Transform cbTrans = transform.Find("Btn_✕ 닫기")
                                 ?? transform.Find("Panel/Btn_✕ 닫기")
                                 ?? transform.Find("CloseBtn")
                                 ?? transform.Find("Panel/CloseBtn")
                                 ?? transform.Find("Btn_Close")
                                 ?? transform.Find("Panel/Close_Btn")
                                 ?? transform.Find("Panel/Btn_Close")
                                 ?? transform.Find("Close")
                                 ?? transform.Find("Panel/Close");
                if (cbTrans != null) closeBtn = cbTrans.gameObject;
            }

            if (closeBtn != null)
            {
                var btn = closeBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ClosePanel);
                }
            }
        }

        public GameObject CreateCardBase(Transform parent)
        {
            EnsureGachaUIPartsBuilt();
            if (animCardTemplate != null)
            {
                var go = Instantiate(animCardTemplate.gameObject, parent, false);
                go.name = "CardBase";
                go.SetActive(true);
                return go;
            }
            return null;
        }

        private void BuildUI()
        {
            if (moneyText != null || transform.childCount > 0) return; 
            var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var overlay = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.85f);

            var panel = MakePanel(transform, Vector2.zero, new Vector2(850, 600));

            MakeText(panel.transform, "카드 뽑기", new Vector2(0, 260), new Vector2(400, 50), 24, Color.white).fontStyle = FontStyles.Bold;
            moneyText = MakeText(panel.transform, "보유 코인: 0", new Vector2(0, 220), new Vector2(400, 30), 16, new Color(1f, 0.9f, 0.4f));

            // Selection UI
            typeSelectionObj = new GameObject("TypeSelection");
            typeSelectionObj.transform.SetParent(panel.transform, false);
            var tsRt = typeSelectionObj.AddComponent<RectTransform>();
            tsRt.anchoredPosition = Vector2.zero;

            normalBtn = MakeButton(typeSelectionObj.transform, "일반 가챠", new Vector2(-200, 130), new Vector2(180, 50), BtnType, () => SelectType(GachaType.Normal)).GetComponent<Button>();
            rareBtn = MakeButton(typeSelectionObj.transform, "레어 가챠", new Vector2(0, 130), new Vector2(180, 50), BtnType, () => SelectType(GachaType.Rare)).GetComponent<Button>();
            superBtn = MakeButton(typeSelectionObj.transform, "슈퍼 가챠", new Vector2(200, 130), new Vector2(180, 50), BtnType, () => SelectType(GachaType.Super)).GetComponent<Button>();

            var once = MakeButton(typeSelectionObj.transform, "", new Vector2(-120, -20), new Vector2(180, 55), BtnGacha, OnRollOnce);
            rollOnceCostText = once.GetComponentInChildren<TMP_Text>();
            rollOnceCostText.text = "1회 뽑기\n0 코인";
            rollOnceCostText.fontSize = 14;

            var ten = MakeButton(typeSelectionObj.transform, "", new Vector2(120, -20), new Vector2(180, 55), BtnGacha10, OnRollTen);
            rollTenCostText = ten.GetComponentInChildren<TMP_Text>();
            rollTenCostText.text = "10회 뽑기\n0 코인";
            rollTenCostText.fontSize = 14;

            closeBtn = MakeButton(typeSelectionObj.transform, "✕ 닫기", new Vector2(0, -220), new Vector2(150, 40), BtnClose, () => gameObject.SetActive(false));

            // Result UI
            resultObj = new GameObject("ResultUI");
            resultObj.transform.SetParent(panel.transform, false);
            var rRt = resultObj.AddComponent<RectTransform>();
            rRt.anchoredPosition = Vector2.zero;
            resultObj.SetActive(false);
        }

        private void SelectType(GachaType type)
        {
            currentType = type;
            RefreshCosts();
        }

        private static Transform FindChildByNameRecursive(Transform parent, string targetName)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name == targetName) return child;
                var found = FindChildByNameRecursive(child, targetName);
                if (found != null) return found;
            }
            return null;
        }

        private void AutoWireCoinText()
        {
            Transform coinBg = transform.Find("CoinBg")
                            ?? transform.Find("Panel/CoinBg")
                            ?? transform.Find("TypeSelection/CoinBg")
                            ?? transform.Find("Panel/TypeSelection/CoinBg")
                            ?? FindChildByNameRecursive(transform, "CoinBg")
                            ?? FindChildByNameRecursive(transform, "Coin_Bg")
                            ?? FindChildByNameRecursive(transform, "CoinBG")
                            ?? FindChildByNameRecursive(transform, "MoneyBg")
                            ?? FindChildByNameRecursive(transform, "Coin");

            if (coinBg != null)
            {
                var tmp = coinBg.GetComponentInChildren<TMP_Text>(true);
                if (tmp != null)
                {
                    moneyText = tmp;
                }
            }
        }

        private void UpdateCoinText()
        {
            if (gm == null) return;

            AutoWireCoinText();

            string lang = gm != null ? gm.SelectedLanguage : "KR";
            bool isEN = lang == "EN";
            string formattedVal = GameManager.FormatNumber(gm.Money);

            if (moneyText != null)
            {
                moneyText.text = isEN ? $"Coins: {formattedVal}" : $"보유 코인: {formattedVal}";
            }

            Transform coinBg = transform.Find("CoinBg")
                            ?? transform.Find("Panel/CoinBg")
                            ?? transform.Find("TypeSelection/CoinBg")
                            ?? transform.Find("Panel/TypeSelection/CoinBg")
                            ?? FindChildByNameRecursive(transform, "CoinBg")
                            ?? FindChildByNameRecursive(transform, "Coin_Bg")
                            ?? FindChildByNameRecursive(transform, "CoinBG");

            if (coinBg != null)
            {
                foreach (var t in coinBg.GetComponentsInChildren<TMP_Text>(true))
                {
                    t.text = isEN ? $"Coins: {formattedVal}" : $"보유 코인: {formattedVal}";
                }

                foreach (var t in coinBg.GetComponentsInChildren<UnityEngine.UI.Text>(true))
                {
                    t.text = isEN ? $"Coins: {formattedVal}" : $"보유 코인: {formattedVal}";
                }
            }
        }

        [Header("Decorations / Paw Settings")]
        [SerializeField] private GameObject leftPaw;
        [SerializeField] private GameObject rightPaw;
        [SerializeField] private GameObject headObj;
        [SerializeField] private GameObject panelBodyObj; // GachaPanel > Panel (y: 0 ~ -117 as head rises)

        private void AutoWireDecorations()
        {
            if (leftPaw == null)
            {
                Transform lp = transform.Find("LeftPaw")
                            ?? transform.Find("Panel/LeftPaw")
                            ?? FindChildByNameRecursive(transform, "LeftPaw")
                            ?? FindChildByNameRecursive(transform, "leftPaw")
                            ?? FindChildByNameRecursive(transform, "Left_Paw")
                            ?? FindChildByNameRecursive(transform, "left_paw");
                if (lp != null) leftPaw = lp.gameObject;
            }

            if (rightPaw == null)
            {
                Transform rp = transform.Find("RightPaw")
                            ?? transform.Find("Panel/RightPaw")
                            ?? FindChildByNameRecursive(transform, "RightPaw")
                            ?? FindChildByNameRecursive(transform, "rightPaw")
                            ?? FindChildByNameRecursive(transform, "Right_Paw")
                            ?? FindChildByNameRecursive(transform, "right_paw");
                if (rp != null) rightPaw = rp.gameObject;
            }

            if (headObj == null)
            {
                Transform hd = transform.Find("Head")
                            ?? transform.Find("Panel/Head")
                            ?? FindChildByNameRecursive(transform, "Head")
                            ?? FindChildByNameRecursive(transform, "head")
                            ?? FindChildByNameRecursive(transform, "CatHead")
                            ?? FindChildByNameRecursive(transform, "cat_head");
                if (hd != null) headObj = hd.gameObject;
            }

            if (panelBodyObj == null)
            {
                Transform pb = transform.Find("Panel");
                if (pb != null) panelBodyObj = pb.gameObject;
            }
        }

        private void UpdateCatDecorations()
        {
            if (gm == null) return;

            AutoWireDecorations();

            float pct = gm.Completion01 * 100f; // Completion percentage (0.0 to 100.0)

            // 12.5% 이상일 때 LeftPaw 활성화
            if (leftPaw != null)
            {
                leftPaw.SetActive(pct >= 12.5f);
            }

            // 25.0% 이상일 때 RightPaw 활성화
            if (rightPaw != null)
            {
                rightPaw.SetActive(pct >= 25.0f);
            }

            // 40.0% 이상일 때 Head 활성화 & (40% ~ 80%) 구간에서 y 좌표 (0 ~ 338) 조정
            if (headObj != null)
            {
                bool showHead = pct >= 40.0f;
                headObj.SetActive(showHead);

                float headT = showHead ? Mathf.Clamp01((pct - 40.0f) / (80.0f - 40.0f)) : 0f;

                if (showHead)
                {
                    float targetY = Mathf.Lerp(0f, 338f, headT);

                    var headRt = headObj.GetComponent<RectTransform>();
                    if (headRt != null)
                    {
                        Vector2 pos = headRt.anchoredPosition;
                        headRt.anchoredPosition = new Vector2(pos.x, targetY);
                    }
                }

                // Panel을 head 상승 비율에 맞춰 y: 0 → -117 로 함께 내림
                if (panelBodyObj != null)
                {
                    var panelRt = panelBodyObj.GetComponent<RectTransform>();
                    if (panelRt != null)
                    {
                        Vector2 ppos = panelRt.anchoredPosition;
                        panelRt.anchoredPosition = new Vector2(ppos.x, Mathf.Lerp(0f, -117f, headT));
                    }
                }
            }
        }

        private void RefreshCosts()
        {
            if (gm == null) return;

            if (normalBtn == null) normalBtn = transform.Find("TypeSelection/Btn_Normal")?.GetComponent<Button>() ?? transform.Find("Panel/TypeSelection/Btn_일반 가챠")?.GetComponent<Button>();
            if (rareBtn == null) rareBtn = transform.Find("TypeSelection/Btn_Rare")?.GetComponent<Button>() ?? transform.Find("Panel/TypeSelection/Btn_레어 가챠")?.GetComponent<Button>();
            if (superBtn == null) superBtn = transform.Find("TypeSelection/Btn_Super")?.GetComponent<Button>() ?? transform.Find("Panel/TypeSelection/Btn_슈퍼 가챠")?.GetComponent<Button>();

            if (normalBtn != null)
            {
                normalBtn.interactable = true;
                var img = normalBtn.GetComponent<Image>();
                if (img != null) img.color = currentType == GachaType.Normal ? BtnTypeActive : BtnType;
            }

            if (rareBtn != null)
            {
                rareBtn.interactable = true;
                var txt = rareBtn.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "레어 가챠";
                var img = rareBtn.GetComponent<Image>();
                if (img != null) img.color = currentType == GachaType.Rare ? BtnTypeActive : BtnType;
            }

            if (superBtn != null)
            {
                superBtn.interactable = true;
                var txt = superBtn.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "슈퍼 가챠";
                var img = superBtn.GetComponent<Image>();
                if (img != null) img.color = currentType == GachaType.Super ? BtnTypeActive : BtnType;
            }

            double single = gm.GetCurrentGachaCost(currentType);
            double ten = single * 10f;

            string lang = gm != null ? gm.SelectedLanguage : "KR";
            bool isEN = lang == "EN";

            string singleStr = isEN ? $"1 Pull\n{single:0} Coins" : $"1회 뽑기\n{single:0} 코인";
            string tenStr = isEN ? $"10 Pulls\n{ten:0} Coins" : $"10회 뽑기\n{ten:0} 코인";

            if (rollOnceCostText != null) rollOnceCostText.text = singleStr;
            if (rollTenCostText != null) rollTenCostText.text = tenStr;

            var typeSel = typeSelectionObj != null ? typeSelectionObj.transform : transform.Find("Panel/TypeSelection");
            if (typeSel != null)
            {
                foreach (Transform child in typeSel.GetComponentsInChildren<Transform>(true))
                {
                    string cName = child.name.ToLower();
                    var txt = child.GetComponentInChildren<TMP_Text>();
                    if (txt == null) continue;

                    if (cName.Contains("10")) txt.text = tenStr;
                    else if (cName.Contains("1")) txt.text = singleStr;
                }
            }
            
            UpdateCoinText();
            UpdateCatDecorations();
        }

        private void OnRollOnce()
        {
            if (gm == null) return;
            double cost = gm.GetCurrentGachaCost(currentType);
            if (gm.Money < cost) return;

            var drawnCard = gm.RollOnce(currentType);
            if (drawnCard != null)
            {
                ShowResult(new List<CardEntry> { drawnCard });
            }
            RefreshCosts();
        }

        private void OnRollTen()
        {
            if (gm == null) return;
            double cost = gm.GetCurrentGachaCost(currentType);
            double totalCost = cost * 10f;
            if (gm.Money < totalCost) return;

            var drawnCards = gm.RollTen(currentType);
            if (drawnCards != null && drawnCards.Count > 0)
            {
                ShowResult(drawnCards);
            }
            RefreshCosts();
        }

        private void SkipAnimation()
        {
            isAnimSkipped = true;
        }

        private void StopActiveGachaCoroutines()
        {
            if (activeGachaSequence != null)
            {
                StopCoroutine(activeGachaSequence);
                activeGachaSequence = null;
            }
            if (activeShardConversion != null)
            {
                StopCoroutine(activeShardConversion);
                activeShardConversion = null;
            }
        }

        private void ConfirmResults()
        {
            StopActiveGachaCoroutines();
            resultObj.SetActive(false);
            typeSelectionObj.SetActive(true);
            if (closeBtn != null) closeBtn.SetActive(true);
            RefreshCosts();
            BindListeners();
        }

        public void ShowResult(List<CardEntry> drawnCards)
        {
            StopActiveGachaCoroutines();
            EnsureGachaUIPartsBuilt();
            BindListeners();
            typeSelectionObj.SetActive(false);
            resultObj.SetActive(true);
            if (closeBtn != null) closeBtn.SetActive(false);

            animContainer.SetActive(true);
            summaryContainer.SetActive(false);
            isAnimSkipped = false;

            currentIsShardDraw.Clear();
            currentShardsGained.Clear();

            var localCopies = new Dictionary<string, int>();
            var states = gm?.GetCardStates();
            if (states != null)
            {
                foreach (var kv in states)
                {
                    localCopies[kv.Key] = kv.Value.Copies;
                }
            }

            foreach (var card in drawnCards)
            {
                if (card == null) continue;
                if (!localCopies.ContainsKey(card.Id)) localCopies[card.Id] = 0;
                
                localCopies[card.Id]++;
                if (localCopies[card.Id] > card.MaxStacks)
                {
                    currentIsShardDraw.Add(true);
                    if (gm != null)
                    {
                        float refund = 1.5f + gm.GetUpgradeEffectValue("upg-shard-refund");
                        currentShardsGained.Add(Mathf.RoundToInt((int)card.ShardValue * refund));
                    }
                    else
                    {
                        currentShardsGained.Add((int)card.ShardValue);
                    }
                }
                else
                {
                    currentIsShardDraw.Add(false);
                    currentShardsGained.Add(0);
                }
            }

            // Clear conveyor children while preserving animCardTemplate
            if (conveyor != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (Transform child in conveyor)
                {
                    if (animCardTemplate == null || child != animCardTemplate)
                        toDestroy.Add(child.gameObject);
                }
                foreach (var obj in toDestroy)
                {
                    obj.transform.SetParent(null, false);
                    DestroyImmediate(obj);
                }
                if (animCardTemplate != null)
                {
                    animCardSize = animCardTemplate.sizeDelta;
                    animCardTemplate.gameObject.SetActive(false);
                }
            }

            activeGachaSequence = StartCoroutine(PlayGachaSequence(drawnCards));
        }

        private RarityTheme GetRarityTheme(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N: return normalRaritySprites;
                case CardRarity.R: return rareRaritySprites;
                case CardRarity.SR: return superRareRaritySprites;
                case CardRarity.SSR: return ssrRaritySprites;
                case CardRarity.UR: return urRaritySprites;
                case CardRarity.H: return hiddenRaritySprites;
                default: return normalRaritySprites;
            }
        }

        public void BindCardFrontData(Transform frontTrans, CardEntry card)
        {
            if (frontTrans == null || card == null) return;
            var theme = GetRarityTheme(card.Rarity);

            Sprite gachaBgSprite = card.GachaBgSprite != null ? card.GachaBgSprite : theme.frontSprite;

            // 0. CardBase root Image (if frontTrans is a child of CardBase)
            Transform rootTrans = (frontTrans.parent != null && (frontTrans.name.ToLower() == "front")) ? frontTrans.parent : frontTrans;
            var cardBaseImg = rootTrans.GetComponent<Image>();
            if (cardBaseImg != null && card.GachaBgSprite != null)
            {
                cardBaseImg.sprite = card.GachaBgSprite;
                cardBaseImg.color = Color.white;
            }

            // 1. Front (Card Background) Image
            var bgImg = frontTrans.GetComponent<Image>() ?? frontTrans.Find("Bg")?.GetComponent<Image>();
            if (bgImg != null)
            {
                if (gachaBgSprite != null)
                {
                    bgImg.sprite = gachaBgSprite;
                }
                bgImg.color = Color.white;
            }

            // 1.5 Frame (Card Outer Frame) Image
            var frameImg = frontTrans.Find("Frame")?.GetComponent<Image>()
                        ?? frontTrans.Find("CardFrame")?.GetComponent<Image>()
                        ?? frontTrans.Find("frame")?.GetComponent<Image>()
                        ?? (frontTrans.parent != null ? frontTrans.parent.Find("Frame")?.GetComponent<Image>() : null)
                        ?? (frontTrans.parent != null ? frontTrans.parent.Find("CardFrame")?.GetComponent<Image>() : null)
                        ?? (frontTrans.parent != null ? frontTrans.parent.Find("frame")?.GetComponent<Image>() : null);
            if (frameImg != null)
            {
                if (theme.frameSprite != null)
                {
                    frameImg.sprite = theme.frameSprite;
                }
                frameImg.color = Color.white;
            }

            // 2. Card Art Image (Art, CardArt, Image)
            var artImg = frontTrans.Find("Art")?.GetComponent<Image>()
                      ?? frontTrans.Find("CardArt")?.GetComponent<Image>()
                      ?? frontTrans.Find("Image")?.GetComponent<Image>();
            if (artImg != null)
            {
                artImg.sprite = card.CardSprite;
                artImg.color = card.CardSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);
            }

            // 3. Rare_Mark Image (Rare_Mark, RareMark, RarityMark, RarityBadge, Rarity)
            var rareMarkImg = frontTrans.Find("Rare_Mark")?.GetComponent<Image>()
                           ?? frontTrans.Find("RareMark")?.GetComponent<Image>()
                           ?? frontTrans.Find("RarityMark")?.GetComponent<Image>()
                           ?? frontTrans.Find("RarityBadge")?.GetComponent<Image>()
                           ?? frontTrans.Find("Rarity")?.GetComponent<Image>()
                           ?? (frontTrans.parent != null ? frontTrans.parent.Find("Rare_Mark")?.GetComponent<Image>() : null)
                           ?? (frontTrans.parent != null ? frontTrans.parent.Find("RareMark")?.GetComponent<Image>() : null);
            if (rareMarkImg != null)
            {
                Sprite sp = theme.rareMarkSprite;
                if (sp == null)
                {
                    var enc = EncyclopediaPanel.Instance ?? FindObjectOfType<EncyclopediaPanel>(true);
                    if (enc != null) sp = enc.GetMarkSpriteForRarity(card.Rarity);
                }
                if (sp != null) rareMarkImg.sprite = sp;
                rareMarkImg.color = Color.white;
            }

            // 4. NameLabel Image (NameLabel, NameBg, NameFrame)
            var nameLabelImg = frontTrans.Find("NameLabel")?.GetComponent<Image>()
                            ?? frontTrans.Find("NameBg")?.GetComponent<Image>()
                            ?? frontTrans.Find("NameFrame")?.GetComponent<Image>();
            if (nameLabelImg != null)
            {
                if (theme.nameLabelSprite != null)
                {
                    nameLabelImg.sprite = theme.nameLabelSprite;
                }
                nameLabelImg.color = Color.white;
            }

            // 5. Name Text (NameText, Name, Text_Name, Title)
            var nameTxt = frontTrans.Find("NameText")?.GetComponent<TMP_Text>()
                       ?? frontTrans.Find("Name")?.GetComponent<TMP_Text>()
                       ?? frontTrans.Find("Text_Name")?.GetComponent<TMP_Text>()
                       ?? frontTrans.GetComponentInChildren<TMP_Text>();
            if (nameTxt != null)
            {
                nameTxt.text = card.GetDisplayName();
            }

            // 6. Rarity Text (RarityText, Text_Rarity)
            var rarityTxt = frontTrans.Find("RarityText")?.GetComponent<TMP_Text>()
                         ?? frontTrans.Find("Text_Rarity")?.GetComponent<TMP_Text>();
            if (rarityTxt != null)
            {
                rarityTxt.text = card.Rarity.ToString();
                rarityTxt.color = GetRarityColor(card.Rarity);
            }
        }

        private IEnumerator PlayGachaSequence(List<CardEntry> cards)
        {
            var cardObjects = new List<GameObject>();
            var cardBacks = new List<GameObject>();
            var cardFronts = new List<GameObject>();

            Vector2 cardSize = animCardTemplate != null ? animCardTemplate.sizeDelta : animCardSize;
            float spacing = cardSize.x + 100f;
            var font = moneyText?.font;

            // Spawn conveyor cards
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                GameObject cardGo;
                if (animCardTemplate != null)
                {
                    cardGo = Instantiate(animCardTemplate.gameObject, conveyor, false);
                    cardGo.name = $"Card_{i}";
                    cardGo.SetActive(true);
                }
                else
                {
                    cardGo = new GameObject($"Card_{i}");
                    cardGo.transform.SetParent(conveyor, false);
                    var newRt = cardGo.AddComponent<RectTransform>();
                    newRt.sizeDelta = cardSize;
                }

                var rt = cardGo.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * spacing, 0f);
                rt.sizeDelta = cardSize;
                cardObjects.Add(cardGo);

                var backTrans = cardGo.transform.Find("Back");
                var frontTrans = cardGo.transform.Find("Front");

                if (backTrans == null)
                {
                    var backGo = new GameObject("Back");
                    backGo.transform.SetParent(cardGo.transform, false);
                    var backRt = backGo.AddComponent<RectTransform>();
                    backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one;
                    backRt.offsetMin = backRt.offsetMax = Vector2.zero;
                    var backImg = backGo.AddComponent<Image>();
                    backImg.color = new Color(0.12f, 0.15f, 0.23f);

                    var qText = MakeText(backGo.transform, "?", Vector2.zero, new Vector2(100, 100), 36, new Color(0.7f, 0.8f, 1f));
                    qText.alignment = TextAlignmentOptions.Center;
                    qText.fontStyle = FontStyles.Bold;
                    if (font != null) qText.font = font;
                    backTrans = backGo.transform;
                }
                backTrans.gameObject.SetActive(true);
                cardBacks.Add(backTrans.gameObject);

                if (frontTrans == null)
                {
                    var frontGo = new GameObject("Front");
                    frontGo.transform.SetParent(cardGo.transform, false);
                    var frontRt = frontGo.AddComponent<RectTransform>();
                    frontRt.anchorMin = Vector2.zero; frontRt.anchorMax = Vector2.one;
                    frontRt.offsetMin = frontRt.offsetMax = Vector2.zero;
                    var frontImg = frontGo.AddComponent<Image>();
                    if (card.GachaBgSprite != null) frontImg.sprite = card.GachaBgSprite;
                    frontImg.color = Color.white;

                    var artGo = new GameObject("Art");
                    artGo.transform.SetParent(frontGo.transform, false);
                    var artRt = artGo.AddComponent<RectTransform>();
                    artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
                    artRt.offsetMin = new Vector2(6, 36); artRt.offsetMax = new Vector2(-6, -6);
                    var fImg = artGo.AddComponent<Image>();
                    fImg.sprite = card.CardSprite;
                    fImg.color = card.CardSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);

                    var nameText = MakeText(frontGo.transform, card.GetDisplayName(), new Vector2(0, -78), new Vector2(120, 26), 11, Color.white);
                    nameText.alignment = TextAlignmentOptions.Center;
                    if (font != null) nameText.font = font;

                    frontTrans = frontGo.transform;
                }
                else
                {
                    BindCardFrontData(frontTrans, card);
                }

                frontTrans.gameObject.SetActive(false);
                cardFronts.Add(frontTrans.gameObject);
            }

            if (conveyor != null) conveyor.anchoredPosition = new Vector2(0f, 0f);

            // Sequence loop
            for (int i = 0; i < cards.Count; i++)
            {
                if (isAnimSkipped) break;

                // Slide conveyor X so card i is at the center
                float targetX = -i * spacing;
                float startX = conveyor.anchoredPosition.x;
                float t = 0f;
                while (t < 0.35f && !isAnimSkipped)
                {
                    t += Time.deltaTime;
                    conveyor.anchoredPosition = new Vector2(Mathf.Lerp(startX, targetX, t / 0.35f), 0f);
                    yield return null;
                }
                if (isAnimSkipped) break;
                conveyor.anchoredPosition = new Vector2(targetX, 0f);

                // Scale up active card
                var cardRT = cardObjects[i].GetComponent<RectTransform>();
                t = 0f;
                while (t < 0.2f && !isAnimSkipped)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1f, 1.25f, t / 0.2f);
                    cardRT.localScale = new Vector3(s, s, 1.0f);
                    yield return null;
                }
                if (isAnimSkipped) break;
                cardRT.localScale = new Vector3(1.25f, 1.25f, 1.0f);

                // Flip sequence (1-card draw flips 3 times for suspense, 10-card draw flips once)
                int totalFlips = cards.Count == 1 ? 3 : 1;

                for (int flipStep = 0; flipStep < totalFlips; flipStep++)
                {
                    if (isAnimSkipped) break;

                    bool isFinalFlip = (flipStep == totalFlips - 1);
                    float flipDuration = cards.Count == 1 ? 0.10f : 0.12f;

                    // Flip to 0 width (shrink X)
                    t = 0f;
                    while (t < flipDuration && !isAnimSkipped)
                    {
                        t += Time.deltaTime;
                        float s = Mathf.Lerp(1.25f, 0f, t / flipDuration);
                        cardRT.localScale = new Vector3(s, 1.25f, 1.0f);
                        yield return null;
                    }
                    if (isAnimSkipped) break;

                    // Reveal front on the final flip!
                    if (isFinalFlip)
                    {
                        cardBacks[i].SetActive(false);
                        cardFronts[i].SetActive(true);

                        // Special sound and visual flash for SR or higher
                        if (cards[i].Rarity >= CardRarity.SR)
                        {
                            effectPlayer?.PlayGachaEffect(cards[i].Rarity);
                            Color flashCol = cards[i].Rarity == CardRarity.UR ? new Color(1f, 0.2f, 0.2f) : (cards[i].Rarity == CardRarity.SSR ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.6f, 0.2f, 0.8f));
                            StartCoroutine(FlashScreen(flashCol));
                        }
                    }

                    // Flip expand to 1.25 width
                    t = 0f;
                    while (t < flipDuration && !isAnimSkipped)
                    {
                        t += Time.deltaTime;
                        float s = Mathf.Lerp(0f, 1.25f, t / flipDuration);
                        cardRT.localScale = new Vector3(s, 1.25f, 1.0f);
                        yield return null;
                    }
                    if (isAnimSkipped) break;
                    cardRT.localScale = new Vector3(1.25f, 1.25f, 1.0f);

                    // Pause briefly between suspense flips in 1-card draw
                    if (!isFinalFlip)
                    {
                        float pause = 0.08f;
                        while (pause > 0f && !isAnimSkipped)
                        {
                            pause -= Time.deltaTime;
                            yield return null;
                        }
                    }
                }

                // Wait 0.5 seconds to admire card
                float wait = 0.5f;
                while (wait > 0f && !isAnimSkipped)
                {
                    wait -= Time.deltaTime;
                    yield return null;
                }
                if (isAnimSkipped) break;

                // Scale down back to 1.0
                t = 0f;
                while (t < 0.12f && !isAnimSkipped)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1.25f, 1.0f, t / 0.12f);
                    cardRT.localScale = new Vector3(s, s, 1.0f);
                    yield return null;
                }
                if (isAnimSkipped) break;
                cardRT.localScale = Vector3.one;
            }

            ShowSummary(cards);
        }

        private void ShowSummary(List<CardEntry> cards)
        {
            animContainer.SetActive(false);
            summaryContainer.SetActive(true);
            BindListeners();

            if (summaryCardTemplate != null)
            {
                summaryCardTemplate.gameObject.SetActive(false);
            }

            // Disable any LayoutGroup component to prevent Unity layout overrides
            var lg1 = summaryContainer.GetComponent<LayoutGroup>();
            if (lg1 != null) lg1.enabled = false;

            foreach (Transform child in summaryContainer.transform)
            {
                var lg = child.GetComponent<LayoutGroup>();
                if (lg != null) lg.enabled = false;
            }

            Transform contentTrans = summaryContainer.transform.Find("SummaryContent") ?? summaryContainer.transform;
            var cardSlots = new List<GameObject>();

            // 1. Comprehensive search for all 10 slots (cardbase0~9 or cardbase1~10) anywhere under summaryContainer
            for (int i = 0; i < 10; i++)
            {
                int index0 = i;
                int index1 = i + 1;

                Transform slotTrans = FindChildByNameRecursive(summaryContainer.transform, $"cardbase{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"cardbase_{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase_{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"cardbase {index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase {index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"card_{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"Card_{index0}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"cardbase{index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase{index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"cardbase_{index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase_{index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"cardbase {index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"CardBase {index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"card_{index1}")
                                   ?? FindChildByNameRecursive(summaryContainer.transform, $"Card_{index1}");

                if (slotTrans != null && !cardSlots.Contains(slotTrans.gameObject))
                {
                    cardSlots.Add(slotTrans.gameObject);
                }
            }

            // 2. Fallback: If less than 10 slots were found, collect all child slots recursively until we have 10
            if (cardSlots.Count < 10)
            {
                void CollectSlotsFrom(Transform parentTrans)
                {
                    foreach (Transform child in parentTrans)
                    {
                        if (cardSlots.Count >= 10) break;
                        if (child.gameObject == confirmBtn || child.gameObject == skipBtn || child.gameObject == closeBtn) continue;
                        if (child.name.StartsWith("Btn_") || child.name == "CloseBtn" || child.name.Contains("Coin")) continue;
                        if (summaryCardTemplate != null && child == summaryCardTemplate) continue;
                        
                        if (child.name == "SummaryContent" || child.name == "Slots" || child.name == "Grid" || child.name == "CardGrid" || child.name.StartsWith("Row") || child.name.StartsWith("row"))
                        {
                            CollectSlotsFrom(child);
                            continue;
                        }

                        if (!cardSlots.Contains(child.gameObject))
                        {
                            cardSlots.Add(child.gameObject);
                        }
                    }
                }
                CollectSlotsFrom(summaryContainer.transform);
            }

            // 1. Save original positions once when slots are first collected
            if (!hasSavedOriginalPositions)
            {
                foreach (var slot in cardSlots)
                {
                    var rt = slot.GetComponent<RectTransform>();
                    if (rt != null && !slotOriginalPositions.ContainsKey(slot))
                    {
                        slotOriginalPositions[slot] = rt.anchoredPosition;
                    }
                }
                if (confirmBtn != null)
                {
                    var cRt = confirmBtn.GetComponent<RectTransform>();
                    if (cRt != null) confirmBtnOriginalPos = cRt.anchoredPosition;
                }
                hasSavedOriginalPositions = true;
            }

            // 2. Restore all slots to their original grid positions and scale
            foreach (var slot in cardSlots)
            {
                var rt = slot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    if (slotOriginalPositions.TryGetValue(slot, out Vector2 origPos))
                    {
                        rt.anchoredPosition = origPos;
                    }
                    rt.localScale = Vector3.one;
                    rt.localRotation = Quaternion.identity;
                }
            }

            // Stop any running shard conversion coroutine
            if (activeShardConversion != null)
            {
                StopCoroutine(activeShardConversion);
                activeShardConversion = null;
            }

            var spawnedCards = new List<GameObject>();

            // Populate cards directly into cardbase0 ~ cardbase9 without moving or destroying them
            for (int i = 0; i < cardSlots.Count; i++)
            {
                var slot = cardSlots[i];

                if (i < cards.Count)
                {
                    slot.SetActive(true);

                    // Ensure ShardOverlay is hidden initially before conversion animation starts!
                    var overlay = slot.transform.Find("ShardOverlay")
                               ?? slot.transform.Find("ShardConversion")
                               ?? slot.transform.Find("DuplicateOverlay");
                    if (overlay != null) overlay.gameObject.SetActive(false);

                    var back = slot.transform.Find("Back");
                    if (back != null) back.gameObject.SetActive(false);

                    var frameTrans = slot.transform.Find("Frame")
                                  ?? slot.transform.Find("CardFrame")
                                  ?? slot.transform.Find("frame");
                    if (frameTrans != null) frameTrans.gameObject.SetActive(true);

                    var front = slot.transform.Find("Front");
                    if (front != null)
                    {
                        front.gameObject.SetActive(true);
                        BindCardFrontData(front, cards[i]);
                    }
                    else
                    {
                        BindCardFrontData(slot.transform, cards[i]);
                    }
                    spawnedCards.Add(slot);
                }
                else
                {
                    slot.SetActive(false);
                }
            }

            // Always keep confirmBtn in its exact original position from Inspector
            if (confirmBtn != null)
            {
                confirmBtn.SetActive(true);
                var cRt = confirmBtn.GetComponent<RectTransform>();
                if (cRt != null) cRt.anchoredPosition = confirmBtnOriginalPos;
                confirmBtn.transform.SetAsLastSibling();
            }

            // Layout handling for 1-Pull vs 10-Pull
            if (cards.Count == 1)
            {
                // Single Pull: Position card[0] at exact center of screen with 1.3x scale
                if (cardSlots.Count > 0)
                {
                    var slot0 = cardSlots[0];
                    slot0.SetActive(true);
                    var slotRT = slot0.GetComponent<RectTransform>();
                    if (slotRT != null)
                    {
                        slotRT.anchoredPosition = new Vector2(0f, 30f);
                        slotRT.localScale = new Vector3(1.3f, 1.3f, 1.0f);
                    }
                }

                for (int i = 1; i < cardSlots.Count; i++)
                {
                    cardSlots[i].SetActive(false);
                }

                var rerollTrans = summaryContainer.transform.Find("Btn_10회 다시뽑기") ?? summaryContainer.transform.Find("Btn_다시뽑기");
                if (rerollTrans != null) rerollTrans.gameObject.SetActive(false);
            }
            else
            {
                // 10-Pull: Show reroll button
                var rerollTrans = summaryContainer.transform.Find("Btn_10회 다시뽑기") ?? summaryContainer.transform.Find("Btn_다시뽑기");
                if (rerollTrans != null)
                {
                    rerollTrans.gameObject.SetActive(true);
                    rerollTrans.SetAsLastSibling();
                }
            }

            var isShardCopy = new List<bool>(currentIsShardDraw);
            var shardsGainedCopy = new List<int>(currentShardsGained);
            activeShardConversion = StartCoroutine(PlayShardConversionAnim(spawnedCards, isShardCopy, shardsGainedCopy));
        }

        private IEnumerator PlayShardConversionAnim(List<GameObject> summaryCards, List<bool> isShardDraw, List<int> shardsGained)
        {
            yield return new WaitForSeconds(0.8f);

            for (int i = 0; i < summaryCards.Count; i++)
            {
                if (i >= isShardDraw.Count || !isShardDraw[i]) continue;

                var cardGo = summaryCards[i];
                if (cardGo == null) continue;
                var cardRT = cardGo.GetComponent<RectTransform>();
                if (cardRT == null) continue;
                int gained = shardsGained[i];

                // Flip shrink
                float t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1f, 0f, t / 0.15f);
                    cardRT.localScale = new Vector3(s, 1f, 1f);
                    yield return null;
                }
                cardRT.localScale = new Vector3(0f, 1f, 1f);

                // Disable Front & Frame containers when card converts into shards
                var frontTrans = cardGo.transform.Find("Front");
                if (frontTrans != null) frontTrans.gameObject.SetActive(false);

                var frameTrans = cardGo.transform.Find("Frame")
                              ?? cardGo.transform.Find("CardFrame")
                              ?? cardGo.transform.Find("frame");
                if (frameTrans != null) frameTrans.gameObject.SetActive(false);

                // Morph to Shard representation (using pre-placed ShardOverlay if available)
                var shardOverlayTrans = cardGo.transform.Find("ShardOverlay")
                                      ?? cardGo.transform.Find("ShardConversion")
                                      ?? cardGo.transform.Find("DuplicateOverlay");

                if (shardOverlayTrans != null)
                {
                    shardOverlayTrans.gameObject.SetActive(true);

                    // Remove Image component on ShardOverlay container if present
                    var bgImg = shardOverlayTrans.GetComponent<Image>();
                    if (bgImg != null) Destroy(bgImg);

                    // Update Shard Icon Sprite Image
                    var iconImg = shardOverlayTrans.Find("ShardIcon")?.GetComponent<Image>()
                               ?? shardOverlayTrans.Find("ShardImage")?.GetComponent<Image>()
                               ?? shardOverlayTrans.Find("Icon")?.GetComponent<Image>();
                    
                    if (iconImg == null)
                    {
                        foreach (var img in shardOverlayTrans.GetComponentsInChildren<Image>(true))
                        {
                            if (img.gameObject != shardOverlayTrans.gameObject)
                            {
                                iconImg = img;
                                break;
                            }
                        }
                    }

                    if (iconImg != null)
                    {
                        if (shardSprite != null)
                        {
                            iconImg.sprite = shardSprite;
                        }
                        iconImg.color = Color.white;
                    }

                    // Update Shard Value Text (+amount) with 40 font size
                    var shardTxt = shardOverlayTrans.Find("ShardValueText")?.GetComponent<TMP_Text>()
                                ?? shardOverlayTrans.Find("ValueText")?.GetComponent<TMP_Text>()
                                ?? shardOverlayTrans.GetComponentInChildren<TMP_Text>();
                    if (shardTxt != null)
                    {
                        shardTxt.text = $"+{gained}";
                        shardTxt.fontSize = 40;
                    }
                }
                else
                {
                    // Fallback dynamic creation if user hasn't pre-placed ShardOverlay
                    var overlayGo = new GameObject("ShardOverlay");
                    overlayGo.transform.SetParent(cardGo.transform, false);
                    var oRt = overlayGo.AddComponent<RectTransform>();
                    oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
                    oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;

                    // Shard Icon Image Component (using shardSprite!)
                    var iconGo = new GameObject("ShardIcon");
                    iconGo.transform.SetParent(overlayGo.transform, false);
                    var iconRt = iconGo.AddComponent<RectTransform>();
                    iconRt.anchoredPosition = new Vector2(0, 20);
                    iconRt.sizeDelta = new Vector2(80, 80);
                    var iconImg = iconGo.AddComponent<Image>();
                    if (shardSprite != null)
                    {
                        iconImg.sprite = shardSprite;
                    }
                    iconImg.color = Color.white;

                    var font = moneyText?.font;
                    var shardValueText = MakeText(overlayGo.transform, $"+{gained}", new Vector2(0, -45), new Vector2(140, 50), 40, new Color(0.4f, 0.9f, 1f));
                    shardValueText.alignment = TextAlignmentOptions.Center;
                    shardValueText.fontStyle = FontStyles.Bold;
                    if (font != null) shardValueText.font = font;
                }

                // Flip back
                t = 0f;
                while (t < 0.15f)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(0f, 1f, t / 0.15f);
                    cardRT.localScale = new Vector3(s, 1f, 1f);
                    yield return null;
                }
                cardRT.localScale = Vector3.one;

                effectPlayer?.PlayGachaEffect(CardRarity.N);

                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator FlashScreen(Color col)
        {
            var flashGO = new GameObject("FlashOverlay");
            flashGO.transform.SetParent(resultObj.transform, false);
            var rt = flashGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            flashGO.transform.SetAsLastSibling();
            
            var img = flashGO.AddComponent<Image>();
            img.color = col;
            
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                img.color = new Color(col.r, col.g, col.b, Mathf.Lerp(0.7f, 0f, t / 0.25f));
                yield return null;
            }
            SafeDestroy(flashGO);
        }

        private Color GetRarityColor(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N: return new Color(0.6f, 0.6f, 0.6f);
                case CardRarity.R: return new Color(0.2f, 0.4f, 0.8f);
                case CardRarity.SR: return new Color(0.6f, 0.2f, 0.8f);
                case CardRarity.SSR: return new Color(0.9f, 0.8f, 0.2f);
                case CardRarity.UR: return new Color(1f, 0.4f, 0.4f);
                default: return Color.white;
            }
        }

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
            cs.normalColor = Color.white;
            cs.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cs.pressedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
            cs.selectedColor = Color.white;
            cs.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            cs.colorMultiplier = 1f;
            cs.fadeDuration = 0.08f;
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

        private static void SafeDestroy(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Create CardBase In Scene Hierarchy")]
        public void CreateCardBaseInScene()
        {
            if (resultObj == null)
            {
                var rTrans = transform.Find("Panel/ResultUI") ?? transform.Find("ResultUI");
                if (rTrans != null) resultObj = rTrans.gameObject;
            }

            if (resultObj == null)
            {
                var panelTrans = transform.Find("Panel") ?? transform;
                resultObj = new GameObject("ResultUI");
                resultObj.transform.SetParent(panelTrans, false);
                var rRt = resultObj.AddComponent<RectTransform>();
                rRt.anchorMin = Vector2.zero; rRt.anchorMax = Vector2.one;
                rRt.offsetMin = Vector2.zero; rRt.offsetMax = Vector2.zero;
            }

            EnsureGachaUIPartsBuilt();

            if (animCardTemplate != null)
            {
                animCardTemplate.gameObject.SetActive(true);
                UnityEditor.Undo.RegisterCreatedObjectUndo(animCardTemplate.gameObject, "Create CardBase");
                UnityEditor.EditorUtility.SetDirty(animCardTemplate.gameObject);
            }

            if (summaryCardTemplate != null)
            {
                summaryCardTemplate.gameObject.SetActive(true);
                UnityEditor.Undo.RegisterCreatedObjectUndo(summaryCardTemplate.gameObject, "Create SummaryCardBase");
                UnityEditor.EditorUtility.SetDirty(summaryCardTemplate.gameObject);
            }

            if (resultObj != null)
            {
                resultObj.SetActive(true);
                UnityEditor.EditorUtility.SetDirty(resultObj);
            }

            if (animContainer != null) animContainer.SetActive(true);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GachaPanel] ✅ CardBase 및 SummaryCardBase가 씬 계층 구조에 즉시 생성되어 저장되었습니다!");
        }

        [ContextMenu("Recreate SummaryCardBase From CardBase")]
        public void RecreateSummaryCardBaseFromCardBase()
        {
            EnsureGachaUIPartsBuilt();
            if (summaryContainer == null || animCardTemplate == null) return;

            var old = summaryContainer.transform.Find("SummaryCardBase");
            if (old != null) UnityEditor.Undo.DestroyObjectImmediate(old.gameObject);

            var scbGo = Instantiate(animCardTemplate.gameObject, summaryContainer.transform, false);
            scbGo.name = "SummaryCardBase";
            var scbRt = scbGo.GetComponent<RectTransform>();
            scbRt.anchoredPosition = new Vector2(0, 30);
            scbRt.sizeDelta = summaryCardSize;

            var backTrans = scbGo.transform.Find("Back");
            if (backTrans != null) backTrans.gameObject.SetActive(false);

            var frontTrans = scbGo.transform.Find("Front");
            if (frontTrans != null) frontTrans.gameObject.SetActive(true);

            summaryCardTemplate = scbRt;
            UnityEditor.Undo.RegisterCreatedObjectUndo(scbGo, "Recreate SummaryCardBase");
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GachaPanel] ✅ SummaryCardBase가 CardBase 하위 구성을 100% 복사하여 새롭게 생성되었습니다!");
        }

        [ContextMenu("Preview AnimContainer (Animation Card Size Preview)")]
        public void ShowAnimContainerPreviewInScene()
        {
            CreateCardBaseInScene();
            if (typeSelectionObj != null) typeSelectionObj.SetActive(false);
            if (resultObj != null) resultObj.SetActive(true);
            if (summaryContainer != null) summaryContainer.SetActive(false);
            if (animContainer != null) animContainer.SetActive(true);

            if (conveyor != null)
            {
                foreach (Transform child in conveyor)
                    UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);

                float spacing = animCardSize.x + 100f;
                var rarities = new[] { CardRarity.N, CardRarity.R, CardRarity.SR, CardRarity.SSR, CardRarity.UR };
                var names = new[] { "N-기본냥", "R-레어냥", "SR-우주냥", "SSR-황금냥", "UR-신급냥" };

                for (int i = 0; i < 5; i++)
                {
                    var cardGo = new GameObject($"PreviewAnimCard_{i}");
                    cardGo.transform.SetParent(conveyor, false);
                    var rt = cardGo.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2((i - 2) * spacing, 0f);
                    rt.sizeDelta = animCardSize;

                    var frontGo = new GameObject("Front");
                    frontGo.transform.SetParent(cardGo.transform, false);
                    var frontRt = frontGo.AddComponent<RectTransform>();
                    frontRt.anchorMin = Vector2.zero; frontRt.anchorMax = Vector2.one;
                    frontRt.offsetMin = frontRt.offsetMax = Vector2.zero;
                    var frontImg = frontGo.AddComponent<UnityEngine.UI.Image>();
                    frontImg.color = GetRarityColor(rarities[i]);

                    var nameText = MakeText(frontGo.transform, names[i], new Vector2(0, -animCardSize.y * 0.4f), new Vector2(animCardSize.x - 10f, 26f), 12, Color.white);
                    nameText.alignment = TextAlignmentOptions.Center;
                }
            }

            if (skipBtn != null) skipBtn.SetActive(true);

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[GachaPanel] ✅ AnimContainer 연출창 미리보기가 씬 뷰에 활성화되었습니다. (카드 크기: {animCardSize.x}x{animCardSize.y})");
        }

        [ContextMenu("Preview SummaryContainer (Result Card Size Preview)")]
        public void ShowResultUIPreviewInScene()
        {
            EnsureGachaUIPartsBuilt();
            if (typeSelectionObj != null) typeSelectionObj.SetActive(false);
            if (resultObj != null) resultObj.SetActive(true);
            if (animContainer != null) animContainer.SetActive(false);
            if (summaryContainer != null) summaryContainer.SetActive(true);

            // Populate sample data into user-placed slots in Scene view without creating new containers
            var cardSlots = new List<GameObject>();
            void CollectSlotsFrom(Transform parentTrans)
            {
                foreach (Transform child in parentTrans)
                {
                    if (child.gameObject == confirmBtn || child.gameObject == skipBtn || child.gameObject == closeBtn) continue;
                    if (child.name.StartsWith("Btn_") || child.name == "CloseBtn") continue;
                    if (child.name == "SummaryContent" || child.name == "Slots" || child.name == "Grid")
                    {
                        CollectSlotsFrom(child);
                        continue;
                    }
                    if (!cardSlots.Contains(child.gameObject))
                        cardSlots.Add(child.gameObject);
                }
            }
            CollectSlotsFrom(summaryContainer.transform);

            var rarities = new[] { CardRarity.N, CardRarity.N, CardRarity.R, CardRarity.R, CardRarity.SR, CardRarity.SR, CardRarity.SSR, CardRarity.SSR, CardRarity.UR, CardRarity.N };
            var names = new[] { "N-고양이 1", "N-고양이 2", "R-고양이 1", "R-고양이 2", "SR-우주냥 1", "SR-우주냥 2", "SSR-황금냥", "SSR-은하냥", "UR-신급냥", "N-고양이 3" };

            for (int i = 0; i < cardSlots.Count; i++)
            {
                var slot = cardSlots[i];
                var card = new CardEntry { Id = $"preview_{i}", DisplayName = names[i % names.Length], Rarity = rarities[i % rarities.Length] };
                slot.SetActive(true);
                var front = slot.transform.Find("Front") ?? slot.transform;
                front.gameObject.SetActive(true);
                BindCardFrontData(front, card);
            }

            if (confirmBtn != null)
            {
                confirmBtn.SetActive(true);
                confirmBtn.transform.SetAsLastSibling();
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log($"[GachaPanel] ✅ SummaryContainer 결과창 미리보기가 씬 뷰에 활성화되었습니다. (카드 크기: {summaryCardSize.x}x{summaryCardSize.y})");
        }

        [ContextMenu("Create ShardOverlay In Card Slots")]
        public void CreateShardOverlayInCardSlots()
        {
            EnsureGachaUIPartsBuilt();
            Transform contentTrans = summaryContainer != null ? (summaryContainer.transform.Find("SummaryContent") ?? summaryContainer.transform) : transform;

            for (int i = 0; i < 10; i++)
            {
                Transform slotTrans = contentTrans.Find($"cardbase{i}")
                                   ?? contentTrans.Find($"cardbase_{i}")
                                   ?? contentTrans.Find($"cardbase {i}")
                                   ?? contentTrans.Find($"CardBase{i}")
                                   ?? contentTrans.Find($"CardBase_{i}")
                                   ?? contentTrans.Find($"CardBase {i}");
                if (slotTrans == null) continue;

                var overlayTrans = slotTrans.Find("ShardOverlay");
                if (overlayTrans == null)
                {
                    var overlayGo = new GameObject("ShardOverlay");
                    overlayGo.transform.SetParent(slotTrans, false);
                    var oRt = overlayGo.AddComponent<RectTransform>();
                    oRt.anchorMin = Vector2.zero; oRt.anchorMax = Vector2.one;
                    oRt.offsetMin = Vector2.zero; oRt.offsetMax = Vector2.zero;

                    var bgImg = overlayGo.AddComponent<Image>();
                    bgImg.color = new Color(0.12f, 0.14f, 0.20f, 0.95f);

                    // Shard Icon Image Component (using shardSprite!)
                    var iconGo = new GameObject("ShardIcon");
                    iconGo.transform.SetParent(overlayGo.transform, false);
                    var iconRt = iconGo.AddComponent<RectTransform>();
                    iconRt.anchoredPosition = new Vector2(0, 15);
                    iconRt.sizeDelta = new Vector2(64, 64);
                    var iconImg = iconGo.AddComponent<Image>();
                    if (shardSprite != null)
                    {
                        iconImg.sprite = shardSprite;
                        iconImg.color = Color.white;
                    }
                    else
                    {
                        iconImg.color = new Color(0.3f, 0.8f, 1f);
                    }

                    var font = moneyText?.font;
                    var shardValueText = MakeText(overlayGo.transform, "+10", new Vector2(0, -40), new Vector2(100, 30), 14, new Color(0.4f, 0.9f, 1f));
                    shardValueText.name = "ShardValueText";
                    shardValueText.alignment = TextAlignmentOptions.Center;
                    shardValueText.fontStyle = FontStyles.Bold;
                    if (font != null) shardValueText.font = font;

                    overlayGo.SetActive(false);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(overlayGo, "Create ShardOverlay");
                }
            }

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GachaPanel] ✅ cardbase0~9 슬롯에 조각 연출용 ShardOverlay(스프라이트 이미지 기반)가 생성되었습니다!");
        }

        [ContextMenu("Preview ResultUI In Scene (Hide)")]
        public void HideResultUIPreviewInScene()
        {
            if (resultObj != null) resultObj.SetActive(false);
            if (typeSelectionObj != null) typeSelectionObj.SetActive(true);
            if (closeBtn != null) closeBtn.SetActive(true);

            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            Debug.Log("[GachaPanel] ✅ ResultUI 미리보기가 숨겨지고 원래 탭 선택 화면으로 복원되었습니다.");
        }
#endif

        // ── Card List Test Button & AnimContainer Handler ──────────────────────
        [Header("Card List Test Button & AnimContainer")]
        [SerializeField] private Button cardListTestBtn;
        [SerializeField] private GameObject cardListTestAnimContainer;

        public void OnCardListTestClicked()
        {
            var testHandler = FindObjectOfType<CardListTestButton>(true);
            if (testHandler == null)
            {
                var testGO = new GameObject("CardListTestAutoWire", typeof(CardListTestButton));
                testHandler = testGO.GetComponent<CardListTestButton>();
            }

            if (testHandler != null)
            {
                testHandler.OnTestButtonClicked();
            }
        }

        private void PopulateCardListTestView(GameObject container, IReadOnlyList<CardEntry> cards)
        {
            if (container == null || cards == null) return;

            // Find or create TestScrollView inside container
            Transform scrollTrans = container.transform.Find("TestScrollView");
            GameObject scrollObj = null;
            if (scrollTrans == null)
            {
                scrollObj = new GameObject("TestScrollView", typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(ScrollRect));
                scrollObj.transform.SetParent(container.transform, false);
                var sRt = scrollObj.GetComponent<RectTransform>();
                sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
                sRt.offsetMin = new Vector2(20f, 60f); sRt.offsetMax = new Vector2(-20f, -20f);

                var sImg = scrollObj.GetComponent<Image>();
                sImg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);

                var viewObj = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
                viewObj.transform.SetParent(scrollObj.transform, false);
                var vRt = viewObj.GetComponent<RectTransform>();
                vRt.anchorMin = Vector2.zero; vRt.anchorMax = Vector2.one;
                vRt.offsetMin = vRt.offsetMax = Vector2.zero;
                viewObj.GetComponent<Image>().color = Color.white;

                var contentObj = new GameObject("Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
                contentObj.transform.SetParent(viewObj.transform, false);
                var cRt = contentObj.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0f, 1f); cRt.anchorMax = new Vector2(1f, 1f);
                cRt.pivot = new Vector2(0.5f, 1f);
                cRt.offsetMin = cRt.offsetMax = Vector2.zero;

                var grid = contentObj.GetComponent<GridLayoutGroup>();
                grid.cellSize = new Vector2(130f, 190f);
                grid.spacing = new Vector2(14f, 14f);
                grid.padding = new RectOffset(20, 20, 20, 20);
                grid.childAlignment = TextAnchor.UpperLeft;
                grid.constraint = GridLayoutGroup.Constraint.Flexible;

                var csf = contentObj.GetComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                var sr = scrollObj.GetComponent<ScrollRect>();
                sr.content = cRt;
                sr.viewport = vRt;
                sr.horizontal = false;
                sr.vertical = true;
            }
            else
            {
                scrollObj = scrollTrans.gameObject;
            }

            scrollObj.SetActive(true);
            Transform contentTrans = scrollObj.transform.Find("Viewport/Content") ?? scrollObj.transform;

            // Clear previous items
            var toDestroy = new List<GameObject>();
            foreach (Transform child in contentTrans)
                toDestroy.Add(child.gameObject);
            foreach (var obj in toDestroy)
                DestroyImmediate(obj);

            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                GameObject cardObj = null;
                if (animCardTemplate != null)
                {
                    cardObj = Instantiate(animCardTemplate.gameObject, contentTrans, false);
                }
                else
                {
                    cardObj = new GameObject($"TestCard_{card.Id}", typeof(RectTransform));
                    cardObj.transform.SetParent(contentTrans, false);
                    var rt = cardObj.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(130f, 190f);
                }

                cardObj.name = $"TestCard_{card.Id}";
                cardObj.SetActive(true);

                // Show Front, Hide Back
                var backTrans = cardObj.transform.Find("Back");
                if (backTrans != null) backTrans.gameObject.SetActive(false);

                var frontTrans = cardObj.transform.Find("Front");
                if (frontTrans == null) frontTrans = cardObj.transform;
                frontTrans.gameObject.SetActive(true);

                // Bind Front Face Data (Background, Frame, Art, Rarity Mark)
                BindCardFrontData(frontTrans, card);

                // Set Name & Card Number Text (No.X CardName)
                var nameTx = frontTrans.Find("Name")?.GetComponent<TMP_Text>()
                          ?? frontTrans.GetComponentInChildren<TMP_Text>(true);
                if (nameTx != null)
                {
                    nameTx.text = $"No.{card.Id}\n{card.GetDisplayName()}";
                    nameTx.enableWordWrapping = true;
                    nameTx.alignment = TextAlignmentOptions.Center;
                }
            }

            // Ensure close button at bottom of container or scrollObj
            Transform closeBtnTrans = container.transform.Find("TestCloseBtn");
            if (closeBtnTrans == null)
            {
                var closeObj = new GameObject("TestCloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                closeObj.transform.SetParent(container.transform, false);
                var cRt = closeObj.GetComponent<RectTransform>();
                cRt.anchorMin = new Vector2(0.5f, 0f); cRt.anchorMax = new Vector2(0.5f, 0f);
                cRt.anchoredPosition = new Vector2(0f, 25f);
                cRt.sizeDelta = new Vector2(140f, 36f);

                closeObj.GetComponent<Image>().color = new Color(0.80f, 0.20f, 0.20f, 1f);
                var closeBtn = closeObj.GetComponent<Button>();
                closeBtn.onClick.AddListener(() =>
                {
                    scrollObj.SetActive(false);
                    closeObj.SetActive(false);
                    container.SetActive(false);
                    if (resultObj != null) resultObj.SetActive(false);
                    if (typeSelectionObj != null) typeSelectionObj.SetActive(true);
                });

                var txObj = new GameObject("Text", typeof(RectTransform));
                txObj.transform.SetParent(closeObj.transform, false);
                var tRt = txObj.GetComponent<RectTransform>();
                tRt.anchorMin = Vector2.zero; tRt.anchorMax = Vector2.one;
                tRt.offsetMin = tRt.offsetMax = Vector2.zero;

                var txt = txObj.AddComponent<TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = "닫기 (Close)";
                    txt.fontSize = 16;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = Color.white;
                }

                closeBtnTrans = closeObj.transform;
            }
            closeBtnTrans.gameObject.SetActive(true);
            closeBtnTrans.SetAsLastSibling();
        }
    }
}
