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
        
        [SerializeField] private Button normalBtn;
        [SerializeField] private Button rareBtn;
        [SerializeField] private Button superBtn;

        [SerializeField] private GameObject typeSelectionObj;
        [SerializeField] private GameObject resultObj;
        [SerializeField] private Transform cardGrid; // Keep for inspector compatibility

        private GameObject animContainer;
        private GameObject summaryContainer;
        private RectTransform conveyor;
        private GameObject skipBtn;
        private GameObject confirmBtn;
        private GameObject closeBtn;
        private bool isAnimSkipped;
        private readonly List<bool> currentIsShardDraw = new List<bool>();
        private readonly List<int> currentShardsGained = new List<int>();

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
            BuildUI();
            EnsureGachaUIPartsBuilt();
            if (moneyText != null) BindListeners();
            effectPlayer = FindObjectOfType<ClickEffectPlayer>();
        }

        private void BindListeners()
        {
            if (normalBtn != null) { normalBtn.onClick.RemoveAllListeners(); normalBtn.onClick.AddListener(() => SelectType(GachaType.Normal)); }
            if (rareBtn != null) { rareBtn.onClick.RemoveAllListeners(); rareBtn.onClick.AddListener(() => SelectType(GachaType.Rare)); }
            if (superBtn != null) { superBtn.onClick.RemoveAllListeners(); superBtn.onClick.AddListener(() => SelectType(GachaType.Super)); }

            var closeBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in closeBtns)
            {
                var txt = b.GetComponentInChildren<TMP_Text>();
                if (txt != null && txt.text.Contains("1회 뽑기")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnRollOnce); }
                else if (txt != null && txt.text.Contains("10회 뽑기")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(OnRollTen); }
                else if (txt != null && txt.text.Contains("✕")) { b.onClick.RemoveAllListeners(); b.onClick.AddListener(() => gameObject.SetActive(false)); }
            }
        }

        private void OnEnable()
        {
            if (gm == null) gm = FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.StateChanged += RefreshCosts;
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
            if (resultObj == null) return;

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
                mRt.anchoredPosition = new Vector2(0, 40);
                mRt.sizeDelta = new Vector2(750, 320);
                maskGo.AddComponent<Image>().color = new Color(0,0,0,0.01f);
                maskGo.AddComponent<Mask>().showMaskGraphic = false;

                var convGo = new GameObject("Conveyor");
                convGo.transform.SetParent(maskGo.transform, false);
                conveyor = convGo.AddComponent<RectTransform>();
                conveyor.anchoredPosition = Vector2.zero;
                conveyor.sizeDelta = new Vector2(2000, 300);

                skipBtn = MakeButton(animContainer.transform, "스킵", new Vector2(0, -180), new Vector2(150, 48), BtnClose, SkipAnimation);
            }

            // Find or create summaryContainer
            var scTrans = resultObj.transform.Find("SummaryContainer");
            if (scTrans != null)
            {
                summaryContainer = scTrans.gameObject;
                var cbTrans = summaryContainer.transform.Find("Btn_확인");
                if (cbTrans != null) confirmBtn = cbTrans.gameObject;
            }
            else
            {
                summaryContainer = new GameObject("SummaryContainer");
                summaryContainer.transform.SetParent(resultObj.transform, false);
                var scRt = summaryContainer.AddComponent<RectTransform>();
                scRt.anchorMin = Vector2.zero; scRt.anchorMax = Vector2.one;
                scRt.offsetMin = Vector2.zero; scRt.offsetMax = Vector2.zero;

                confirmBtn = MakeButton(summaryContainer.transform, "확인", new Vector2(0, -220), new Vector2(160, 48), BtnGacha, ConfirmResults);
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
                var panelTrans = transform.Find("Panel");
                if (panelTrans != null)
                {
                    var cbTrans = panelTrans.Find("Btn_✕ 닫기");
                    if (cbTrans != null) closeBtn = cbTrans.gameObject;
                }
            }
        }

        private void BuildUI()
        {
            if (moneyText != null) return; 
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

            var once = MakeButton(typeSelectionObj.transform, "", new Vector2(-150, -20), new Vector2(200, 80), BtnGacha, OnRollOnce);
            rollOnceCostText = once.GetComponentInChildren<TMP_Text>();
            rollOnceCostText.text = "1회 뽑기\n0 코인";
            rollOnceCostText.fontSize = 16;

            var ten = MakeButton(typeSelectionObj.transform, "", new Vector2(150, -20), new Vector2(200, 80), BtnGacha10, OnRollTen);
            rollTenCostText = ten.GetComponentInChildren<TMP_Text>();
            rollTenCostText.text = "10회 뽑기\n0 코인";
            rollTenCostText.fontSize = 16;

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
            if (gm == null) return;
            if (type == GachaType.Rare && !gm.UnlockedRareGacha) type = GachaType.Normal;
            if (type == GachaType.Super && !gm.UnlockedSuperGacha) type = GachaType.Normal;

            currentType = type;
            RefreshCosts();
        }

        private void RefreshCosts()
        {
            if (gm == null) return;
            
            normalBtn.interactable = true;
            normalBtn.GetComponent<Image>().color = currentType == GachaType.Normal ? BtnTypeActive : BtnType;

            rareBtn.interactable = gm.UnlockedRareGacha;
            rareBtn.GetComponentInChildren<TMP_Text>().text = gm.UnlockedRareGacha ? "레어 가챠" : "레어 가챠\n(잠김)";
            rareBtn.GetComponent<Image>().color = gm.UnlockedRareGacha ? (currentType == GachaType.Rare ? BtnTypeActive : BtnType) : new Color(0.3f, 0.3f, 0.3f);

            superBtn.interactable = gm.UnlockedSuperGacha;
            superBtn.GetComponentInChildren<TMP_Text>().text = gm.UnlockedSuperGacha ? "슈퍼 가챠" : "슈퍼 가챠\n(잠김)";
            superBtn.GetComponent<Image>().color = gm.UnlockedSuperGacha ? (currentType == GachaType.Super ? BtnTypeActive : BtnType) : new Color(0.3f, 0.3f, 0.3f);

            double single = gm.GetCurrentGachaCost(currentType);
            double ten = single * 10f;

            if (rollOnceCostText != null) rollOnceCostText.text = $"1회 뽑기\n{single:0} 코인";
            if (rollTenCostText != null) rollTenCostText.text = $"10회 뽑기\n{ten:0} 코인";
            if (moneyText != null) moneyText.text = $"보유 코인: {gm.Money:F1}";
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

        private void ConfirmResults()
        {
            resultObj.SetActive(false);
            typeSelectionObj.SetActive(true);
            if (closeBtn != null) closeBtn.SetActive(true);
        }

        public void ShowResult(List<CardEntry> drawnCards)
        {
            EnsureGachaUIPartsBuilt();
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
                    float shardMult = 1.5f + (gm != null ? gm.GetUpgradeEffectValue("upg-gacha-disc") : 0f); // wait, upg-shard-refund!
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

            // Clear old children
            if (conveyor != null)
            {
                foreach (Transform child in conveyor) SafeDestroy(child.gameObject);
            }
            if (summaryContainer != null)
            {
                foreach (Transform child in summaryContainer.transform)
                {
                    if (child.gameObject != confirmBtn) SafeDestroy(child.gameObject);
                }
            }

            StartCoroutine(PlayGachaSequence(drawnCards));
        }

        private IEnumerator PlayGachaSequence(List<CardEntry> cards)
        {
            var cardObjects = new List<GameObject>();
            var cardBacks = new List<GameObject>();
            var cardFronts = new List<GameObject>();

            float spacing = 200f;
            var font = moneyText?.font;

            // Spawn conveyor cards
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var cardGo = new GameObject($"Card_{i}");
                cardGo.transform.SetParent(conveyor, false);
                var rt = cardGo.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(i * spacing, 0f);
                rt.sizeDelta = new Vector2(130, 190);
                cardObjects.Add(cardGo);

                // Back side
                var backGo = new GameObject("Back");
                backGo.transform.SetParent(cardGo.transform, false);
                var backRt = backGo.AddComponent<RectTransform>();
                backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one;
                backRt.offsetMin = Vector2.zero; backRt.offsetMax = Vector2.zero;
                var backImg = backGo.AddComponent<Image>();
                backImg.color = new Color(0.12f, 0.15f, 0.23f);

                var backBorder = new GameObject("Border");
                backBorder.transform.SetParent(backGo.transform, false);
                var bbRt = backBorder.AddComponent<RectTransform>();
                bbRt.anchorMin = Vector2.zero; bbRt.anchorMax = Vector2.one;
                bbRt.offsetMin = new Vector2(4, 4); bbRt.offsetMax = new Vector2(-4, -4);
                backBorder.AddComponent<Image>().color = new Color(0.30f, 0.40f, 0.60f);

                var qText = MakeText(backGo.transform, "?", Vector2.zero, new Vector2(100, 100), 36, new Color(0.7f, 0.8f, 1f));
                qText.alignment = TextAlignmentOptions.Center;
                qText.fontStyle = FontStyles.Bold;
                if (font != null) qText.font = font;

                cardBacks.Add(backGo);

                // Front side
                var frontGo = new GameObject("Front");
                frontGo.transform.SetParent(cardGo.transform, false);
                var frontRt = frontGo.AddComponent<RectTransform>();
                frontRt.anchorMin = Vector2.zero; frontRt.anchorMax = Vector2.one;
                frontRt.offsetMin = Vector2.zero; frontRt.offsetMax = Vector2.zero;
                var frontImg = frontGo.AddComponent<Image>();
                frontImg.color = GetRarityColor(card.Rarity);

                var artGo = new GameObject("Art");
                artGo.transform.SetParent(frontGo.transform, false);
                var artRt = artGo.AddComponent<RectTransform>();
                artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
                artRt.offsetMin = new Vector2(6, 36); artRt.offsetMax = new Vector2(-6, -6);
                var fImg = artGo.AddComponent<Image>();
                fImg.sprite = card.CardSprite;
                fImg.color = card.CardSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);

                var nameText = MakeText(frontGo.transform, card.DisplayName, new Vector2(0, -78), new Vector2(120, 26), 11, Color.white);
                nameText.alignment = TextAlignmentOptions.Center;
                if (font != null) nameText.font = font;

                frontGo.SetActive(false);
                cardFronts.Add(frontGo);
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

                // Flip to 0 width
                t = 0f;
                while (t < 0.12f && !isAnimSkipped)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(1.25f, 0f, t / 0.12f);
                    cardRT.localScale = new Vector3(s, 1.25f, 1.0f);
                    yield return null;
                }
                if (isAnimSkipped) break;

                // Toggle visibility
                cardBacks[i].SetActive(false);
                cardFronts[i].SetActive(true);

                // Special sound and visual flash for SR or higher
                if (cards[i].Rarity >= CardRarity.SR)
                {
                    effectPlayer?.PlayGachaEffect(cards[i].Rarity);
                    Color flashCol = cards[i].Rarity == CardRarity.UR ? new Color(1f, 0.2f, 0.2f) : (cards[i].Rarity == CardRarity.SSR ? new Color(0.9f, 0.8f, 0.2f) : new Color(0.6f, 0.2f, 0.8f));
                    StartCoroutine(FlashScreen(flashCol));
                }

                // Flip back to 1.25 width
                t = 0f;
                while (t < 0.12f && !isAnimSkipped)
                {
                    t += Time.deltaTime;
                    float s = Mathf.Lerp(0f, 1.25f, t / 0.12f);
                    cardRT.localScale = new Vector3(s, 1.25f, 1.0f);
                    yield return null;
                }
                if (isAnimSkipped) break;
                cardRT.localScale = new Vector3(1.25f, 1.25f, 1.0f);

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

            var spawnedCards = new List<GameObject>();

            if (cards.Count == 1)
            {
                var cardGo = CreateSummaryCard(summaryContainer.transform, cards[0], Vector2.zero, new Vector2(150, 220), 13);
                spawnedCards.Add(cardGo);
            }
            else
            {
                var gridGo = new GameObject("SummaryGrid");
                gridGo.transform.SetParent(summaryContainer.transform, false);
                var gRt = gridGo.AddComponent<RectTransform>();
                gRt.anchoredPosition = new Vector2(0f, 35f);
                gRt.sizeDelta = new Vector2(750, 360);

                var layout = gridGo.AddComponent<GridLayoutGroup>();
                layout.cellSize = new Vector2(110, 160);
                layout.spacing = new Vector2(15, 15);
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                layout.constraintCount = 5; // 5x2 grid

                for (int i = 0; i < cards.Count; i++)
                {
                    var cardGo = CreateSummaryCard(gridGo.transform, cards[i], Vector2.zero, new Vector2(110, 160), 10);
                    spawnedCards.Add(cardGo);
                }
            }

            confirmBtn.SetActive(true);
            confirmBtn.transform.SetAsLastSibling();

            StartCoroutine(PlayShardConversionAnim(spawnedCards, currentIsShardDraw, currentShardsGained));
        }

        private GameObject CreateSummaryCard(Transform parent, CardEntry card, Vector2 pos, Vector2 size, int fontSize)
        {
            var cardGo = new GameObject("SummaryCard");
            cardGo.transform.SetParent(parent, false);
            var rt = cardGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var bgImg = cardGo.AddComponent<Image>();
            bgImg.color = GetRarityColor(card.Rarity);

            var artGo = new GameObject("Art");
            artGo.transform.SetParent(cardGo.transform, false);
            artGo.name = "Art";
            var artRt = artGo.AddComponent<RectTransform>();
            artRt.anchorMin = Vector2.zero; artRt.anchorMax = Vector2.one;
            
            float padTop = size.y * 0.18f;
            float padSide = size.x * 0.05f;
            artRt.offsetMin = new Vector2(padSide, padTop); 
            artRt.offsetMax = new Vector2(-padSide, -padSide);
            var fImg = artGo.AddComponent<Image>();
            fImg.sprite = card.CardSprite;
            fImg.color = card.CardSprite != null ? Color.white : new Color(0.2f, 0.2f, 0.2f);

            var font = moneyText?.font;
            var nameText = MakeText(cardGo.transform, card.DisplayName, 
                new Vector2(0f, -size.y/2f + padTop/2f), 
                new Vector2(size.x - 10f, padTop), fontSize, Color.white);
            nameText.alignment = TextAlignmentOptions.Center;
            if (font != null) nameText.font = font;

            return cardGo;
        }

        private IEnumerator PlayShardConversionAnim(List<GameObject> summaryCards, List<bool> isShardDraw, List<int> shardsGained)
        {
            yield return new WaitForSeconds(0.8f);

            for (int i = 0; i < summaryCards.Count; i++)
            {
                if (i >= isShardDraw.Count || !isShardDraw[i]) continue;

                var cardGo = summaryCards[i];
                var cardRT = cardGo.GetComponent<RectTransform>();
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

                // Morph to Shard representation
                var art = cardGo.transform.Find("Art");
                if (art != null) SafeDestroy(art.gameObject);
                
                var nameText = cardGo.transform.Find("Text");
                if (nameText != null) SafeDestroy(nameText.gameObject);

                var bgImg = cardGo.GetComponent<Image>();
                bgImg.color = new Color(0.12f, 0.14f, 0.20f);

                var glowBorder = new GameObject("GlowBorder");
                glowBorder.transform.SetParent(cardGo.transform, false);
                var gbRt = glowBorder.AddComponent<RectTransform>();
                gbRt.anchorMin = Vector2.zero; gbRt.anchorMax = Vector2.one;
                gbRt.offsetMin = new Vector2(3, 3); gbRt.offsetMax = new Vector2(-3, -3);
                var glowImg = glowBorder.AddComponent<Image>();
                glowImg.color = new Color(0.12f, 0.14f, 0.20f);
                
                var outerGlow = cardGo.AddComponent<Outline>();
                if (outerGlow != null)
                {
                    outerGlow.effectColor = new Color(0.3f, 0.8f, 1f, 0.8f);
                    outerGlow.effectDistance = new Vector2(3, 3);
                }

                var font = moneyText?.font;
                var shardIcon = MakeText(cardGo.transform, "✦", new Vector2(0, 15), new Vector2(80, 80), 32, new Color(0.3f, 0.8f, 1f));
                shardIcon.alignment = TextAlignmentOptions.Center;
                shardIcon.fontStyle = FontStyles.Bold;
                if (font != null) shardIcon.font = font;

                var shardValueText = MakeText(cardGo.transform, $"+{gained}", new Vector2(0, -40), new Vector2(100, 30), 14, new Color(0.4f, 0.9f, 1f));
                shardValueText.alignment = TextAlignmentOptions.Center;
                shardValueText.fontStyle = FontStyles.Bold;
                if (font != null) shardValueText.font = font;

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
    }
}
