using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    /// <summary>
    /// 게임 중앙의 클릭 대상 카드 이미지를 관리합니다.
    /// 장착된 카드가 바뀔 때마다 이미지와 배경색이 자동으로 갱신됩니다.
    /// Clicker 컴포넌트와 같은 오브젝트(또는 자식)에 배치하세요.
    /// </summary>
    public sealed class CardImageDisplay : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image       cardImage;       // 카드 아트 이미지
        [SerializeField] private Image       frameImage;      // 등급 색상 테두리
        [SerializeField] private Image       glowImage;       // 배경 글로우 (선택사항)
        [SerializeField] private TMP_Text    cardNameText;    // 카드 이름 (선택사항)
        [SerializeField] private TMP_Text    rarityText;      // 등급 텍스트 (선택사항)
        [SerializeField] private TMP_Text    stackText;       // 중첩 수 (선택사항)

        [Header("Default Sprites")]
        [SerializeField] private Sprite defaultCardSprite;

        [Header("Rarity Colors")]
        [SerializeField] private Color colorN   = new Color(0.70f, 0.70f, 0.70f);
        [SerializeField] private Color colorR   = new Color(0.31f, 0.58f, 1.00f);
        [SerializeField] private Color colorSR  = new Color(0.70f, 0.31f, 1.00f);
        [SerializeField] private Color colorSSR = new Color(1.00f, 0.78f, 0.00f);
        [SerializeField] private Color colorUR  = new Color(1.00f, 0.30f, 0.10f);

        [Header("Pulse on New Card")]
        [SerializeField] private float pulseScale    = 1.2f;
        [SerializeField] private float pulseDuration = 0.15f;

        private string   lastEquippedId;
        private Vector3  originalScale;
        private Vector2  originalSizeDelta;
        private bool     hasOriginalRectSize;
        private float    pulseTimer;
        private bool     isPulsing;

        // ── 소켓 슬롯 감지 ─────────────────────────────────────────────────
        private ClickSocketSlot mySocket = ClickSocketSlot.Center;
        private bool socketDetected = false;
        private int currentSpriteIndex = 0;

        // ── 152_missing_cat 자동 연속 루프 애니메이션 ───────────────────────────
        private float autoIdleTimer = 0f;
        private int autoIdleFrameIndex = 0;
        private List<Sprite> autoIdleSprites = null;
        private bool isAutoIdleActive = false;
        private bool isSchrodingerActive = false;
        private bool isSchrodingerSpecialSequence = false;

        public void CycleMemeCatImage()
        {
            if (isAutoIdleActive) return; // 152번 자동 연속 회전 카드는 클릭과 무관하게 자동 애니메이션 수행

            string socketCardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
            if (string.IsNullOrEmpty(socketCardId)) return;

            var entry = gameManager.CardCatalog?.FindById(socketCardId);
            int selectedStage = gameManager.GetSocketSelectedStage(mySocket);
            var sprites = MemeCatToggleDisplayHelper.GetSpriteListForCard(socketCardId, entry, selectedStage);
            if (sprites != null && sprites.Count >= 2)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Count;
                Sprite targetSp = sprites[currentSpriteIndex];
                if (cardImage != null && targetSp != null)
                {
                    cardImage.sprite = targetSp;
                    FitSubClickSpriteToBounds(targetSp);
                }
            }
        }

        public void ToggleMemeCatImage()
        {
            CycleMemeCatImage();
        }

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (cardImage == null) cardImage = GetComponent<Image>();
            DetectMySocket();

            originalScale = transform.localScale;
            if (transform is RectTransform rectTransform)
            {
                originalSizeDelta = rectTransform.sizeDelta;
                hasOriginalRectSize = true;
            }
        }

        private void DetectMySocket()
        {
            if (socketDetected) return;
            switch (gameObject.name)
            {
                case "LeftUpSubClick":    mySocket = ClickSocketSlot.LeftUp;    break;
                case "RightUpSubClick":   mySocket = ClickSocketSlot.RightUp;   break;
                case "LeftDownSubClick":  mySocket = ClickSocketSlot.LeftDown;  break;
                case "RightDownSubClick": mySocket = ClickSocketSlot.RightDown; break;
                default:                  mySocket = ClickSocketSlot.Center;    break;
            }
            socketDetected = true;
        }

        private void Start()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
                gameManager.StateChanged += Refresh;
            }

            if (mySocket != ClickSocketSlot.Center)
            {
                breathPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
            }
        }

        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.CardDrawn    -= OnCardDrawn;
                gameManager.CardDrawn    += OnCardDrawn;
                gameManager.CardClicked  -= TriggerClickBounce;
                gameManager.CardClicked  += TriggerClickBounce;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.CardDrawn    -= OnCardDrawn;
                gameManager.CardClicked  -= TriggerClickBounce;
            }
        }

        private float breathPhaseOffset;

        private void Update()
        {
            if (isPulsing)
            {
                pulseTimer += Time.unscaledDeltaTime;
                float t = pulseTimer / pulseDuration;
                float s = 1f + (pulseScale - 1f) * Mathf.Sin(t * Mathf.PI);
                transform.localScale = originalScale * s;
                if (pulseTimer >= pulseDuration)
                {
                    transform.localScale = originalScale;
                    isPulsing = false;
                    pulseTimer = 0f;
                }
            }
            else if (mySocket != ClickSocketSlot.Center && clickBounceRoutine == null)
            {
                // 서브 소켓 숨쉬기 애니메이션 (클릭 바운스 중이 아닐 때)
                const float speed = 1.4f;
                const float amp = 0.04f;
                float s = 1f + amp * Mathf.Sin(Time.time * speed * Mathf.PI * 2f + breathPhaseOffset);
                transform.localScale = originalScale * s;
            }

            // 171 롱넥캣 콤보 중단 시 역방향 복귀 감지
            if (longNeckContainer != null && longNeckContainer.gameObject.activeSelf && !isRetracting)
            {
                if (spawnedNeckData.Count > 0 && gameManager != null)
                {
                    if (gameManager.ComboCount == 0)
                    {
                        StartReverseRetract();
                    }
                }
            }

            // 152번 missing_cat 등 클릭 무관 자동 연속 프레임 루프 애니메이션
            UpdateAutoIdleAnimation();
        }

        private void UpdateAutoIdleAnimation()
        {
            if (!isAutoIdleActive || autoIdleSprites == null || autoIdleSprites.Count < 2) return;

            autoIdleTimer += Time.deltaTime;
            const float frameRate = 0.12f; // ~8 FPS 자동 회전
            if (autoIdleTimer >= frameRate)
            {
                autoIdleTimer = 0f;
                if (isSchrodingerActive && autoIdleSprites.Count >= 5)
                {
                    if (isSchrodingerSpecialSequence)
                    {
                        if (autoIdleFrameIndex == 2) autoIdleFrameIndex = 3;
                        else if (autoIdleFrameIndex == 3) autoIdleFrameIndex = 4;
                        else if (autoIdleFrameIndex == 4) autoIdleFrameIndex = 1;
                        else
                        {
                            autoIdleFrameIndex = 0;
                            isSchrodingerSpecialSequence = false;
                        }
                    }
                    else if (autoIdleFrameIndex == 0)
                    {
                        isSchrodingerSpecialSequence = Random.value < 0.01f;
                        autoIdleFrameIndex = isSchrodingerSpecialSequence ? 2 : 1;
                    }
                    else
                    {
                        autoIdleFrameIndex = 0;
                    }
                }
                else
                {
                    autoIdleFrameIndex = (autoIdleFrameIndex + 1) % autoIdleSprites.Count;
                }
                if (cardImage != null && autoIdleSprites[autoIdleFrameIndex] != null)
                {
                    cardImage.sprite = autoIdleSprites[autoIdleFrameIndex];
                    FitSubClickSpriteToBounds(autoIdleSprites[autoIdleFrameIndex]);
                }
            }
        }

        private void Refresh()
        {
            if (gameManager == null) return;

            DetectMySocket();

            if (mySocket != ClickSocketSlot.Center)
            {
                // A previously displayed sprite must never leave a sub-click at
                // its PNG native dimensions. Keep the scene's fixed slot bounds.
                if (hasOriginalRectSize && transform is RectTransform rectTransform)
                    rectTransform.sizeDelta = originalSizeDelta;

                bool isUnlocked = gameManager.IsSocketUnlocked(mySocket);
                string socketCardId = gameManager.GetSocketCardId(mySocket);
                bool hasCard = !string.IsNullOrEmpty(socketCardId);
                bool shouldShow = isUnlocked && hasCard;

                var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                foreach (var g in graphics) g.enabled = shouldShow;

                var renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) r.enabled = shouldShow;

                var clicker = GetComponent<Clicker>();
                if (clicker != null) clicker.enabled = shouldShow;

                if (!shouldShow) return;
            }

            // 이 GO가 속한 소켓의 장착 카드를 읽음
            CardEntry card = null;
            if (mySocket == ClickSocketSlot.Center)
            {
                card = gameManager.GetEquippedCard();
            }
            else
            {
                string socketCardId = gameManager.GetSocketCardId(mySocket);
                if (!string.IsNullOrEmpty(socketCardId))
                    card = gameManager.CardCatalog?.FindById(socketCardId);
            }

            var states = gameManager.GetCardStates();

            string curCardId = card != null ? card.Id : null;
            if (lastEquippedId != curCardId)
            {
                lastEquippedId = curCardId;
                currentSpriteIndex = 0;
                autoIdleFrameIndex = 0;
                autoIdleTimer = 0f;
                isSchrodingerSpecialSequence = false;
            }

            if (curCardId != null && AutoIdleCatAnimationHelper.IsAutoIdleCat(curCardId))
            {
                isAutoIdleActive = true;
                autoIdleSprites = AutoIdleCatAnimationHelper.GetAutoIdleSprites(curCardId, card);
                isSchrodingerActive = int.TryParse(curCardId, out int autoCardNumber) && autoCardNumber == 195;
            }
            else
            {
                isAutoIdleActive = false;
                autoIdleSprites = null;
                isSchrodingerActive = false;
                isSchrodingerSpecialSequence = false;
            }

            // 스프라이트 및 색상
            if (cardImage != null)
            {
                cardImage.preserveAspect = true;
                if (isAutoIdleActive && autoIdleSprites != null && autoIdleSprites.Count > 0)
                {
                    if (autoIdleFrameIndex >= autoIdleSprites.Count) autoIdleFrameIndex = 0;
                    Sprite currentSp = autoIdleSprites[autoIdleFrameIndex];
                    if (currentSp == null) currentSp = defaultCardSprite;
                    cardImage.sprite = currentSp;
                    FitSubClickSpriteToBounds(currentSp);
                    cardImage.color  = Color.white;
                }
                else
                {
                    int selectedStage = gameManager.GetSocketSelectedStage(mySocket);
                    var sprites = MemeCatToggleDisplayHelper.GetSpriteListForCard(curCardId, card, selectedStage);
                    if (sprites != null && sprites.Count >= 2)
                    {
                        if (currentSpriteIndex >= sprites.Count) currentSpriteIndex = 0;
                        Sprite currentSp = sprites[currentSpriteIndex];
                        if (currentSp == null) currentSp = defaultCardSprite;
                        cardImage.sprite = currentSp;
                        FitSubClickSpriteToBounds(currentSp);
                        cardImage.color  = Color.white;
                    }
                    else
                    {
                        Sprite sprite = card != null
                            ? card.GetSpriteForStage(selectedStage)
                            : defaultCardSprite;
                        if (sprite == null) sprite = defaultCardSprite;
                        
                        bool spriteChanged = cardImage.sprite != sprite;
                        cardImage.sprite = sprite;
                        FitSubClickSpriteToBounds(sprite);
                        if (sprite != null)
                        {
                            cardImage.color = Color.white;
                            if (spriteChanged && mySocket == ClickSocketSlot.Center)
                            {
                                cardImage.SetNativeSize();
                            }
                        }
                    }
                }
            }

            // 171 롱넥캣 전용 커스텀 디스플레이 토글
            RefreshLongNeckDisplay(card != null && card.Id == "0171");

            // 등급 컬러
            Color rarityColor = card != null ? GetRarityColor(card.Rarity) : colorN;
            if (frameImage != null) frameImage.color = rarityColor;
            if (glowImage  != null)
            {
                var c = rarityColor;
                c.a = 0.4f;
                glowImage.color = c;
            }

            // 텍스트
            if (cardNameText != null)
                cardNameText.text = card != null ? card.DisplayName : "기본 카드";

            if (rarityText != null)
                rarityText.text = card != null ? $"[{card.Rarity}]" : string.Empty;

            if (stackText != null && card != null &&
                states.TryGetValue(card.Id, out var progress) && progress.Copies > 1)
                stackText.text = $"×{progress.Copies}";
            else if (stackText != null)
                stackText.text = string.Empty;
        }

        /// <summary>
        /// Fits a sub-click illustration inside its scene-defined bounds while
        /// preserving the sprite's native aspect ratio (equivalent to CSS contain).
        /// Example: a 200x500 sprite in a 400x400 slot becomes 160x400.
        /// </summary>
        private void FitSubClickSpriteToBounds(Sprite sprite)
        {
            if (mySocket == ClickSocketSlot.Center || cardImage == null || sprite == null || !hasOriginalRectSize)
                return;

            float maxWidth = Mathf.Abs(originalSizeDelta.x);
            float maxHeight = Mathf.Abs(originalSizeDelta.y);
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            if (maxWidth <= 0f || maxHeight <= 0f || spriteWidth <= 0f || spriteHeight <= 0f)
                return;

            float scale = Mathf.Min(maxWidth / spriteWidth, maxHeight / spriteHeight);
            cardImage.rectTransform.sizeDelta = new Vector2(spriteWidth * scale, spriteHeight * scale);
            cardImage.preserveAspect = true;
        }

        private void OnCardDrawn(string cardId, CardRarity rarity)
        {
            // 새 카드가 장착될 때 펄스 애니메이션
            if (cardId != lastEquippedId)
            {
                lastEquippedId = cardId;
                isPulsing  = true;
                pulseTimer = 0f;
            }
        }

        private Color GetRarityColor(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.R:   return colorR;
                case CardRarity.SR:  return colorSR;
                case CardRarity.SSR: return colorSSR;
                case CardRarity.UR:  return colorUR;
                default:             return colorN;
            }
        }

        [Header("Long Neck Cat (0171) Special Setup")]
        [SerializeField] private Sprite longNeckHeadSprite;
        [SerializeField] private Sprite longNeckBodySprite;
        [SerializeField] private Sprite longNeckNeckSprite;
        
        private struct NeckSegmentData
        {
            public GameObject go;
            public Vector2 startPos;
            public float angle;
        }

        private List<NeckSegmentData> spawnedNeckData = new List<NeckSegmentData>();
        private Vector2 currentNeckTipPos = new Vector2(0f, -10f);
        private float currentNeckAngle = 0f;
        private const float SINGLE_NECK_HEIGHT = 93.33f; // 원본 비율(600x200)에 정확히 맞춘 1개 목 마디 높이
        private bool isRetracting = false;
        private Coroutine retractRoutine;

        private RectTransform longNeckContainer;
        private Image         longNeckHeadImage;
        private Image         longNeckBodyImage;
        private Image         longNeckNeckImage;
        private Coroutine     longNeckRoutine;

        private void StartReverseRetract()
        {
            if (isRetracting) return;
            if (retractRoutine != null) StopCoroutine(retractRoutine);
            retractRoutine = StartCoroutine(ReverseRetractRoutine());
        }

        private System.Collections.IEnumerator ReverseRetractRoutine()
        {
            isRetracting = true;

            int initialSegments = spawnedNeckData.Count;
            if (initialSegments > 0)
            {
                // 100 콤보 이하는 마디당 0.01초 (예: 50마디 = 0.5초, 100마디 = 1.0초)
                // 100 콤보 초과는 무조건 실제 벽시계 시간(Time.unscaledDeltaTime) 기준 1.0초 정밀 고정!
                float totalRetractDuration = (initialSegments <= 100)
                    ? (initialSegments * 0.01f)
                    : 1.0f;

                float elapsed = 0f;

                while (elapsed < totalRetractDuration && spawnedNeckData.Count > 0)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / totalRetractDuration);
                    int targetRemaining = Mathf.RoundToInt(Mathf.Lerp(initialSegments, 0, progress));

                    // 유니티 프레임 레이트(60FPS 등)에 상관없이 1개 프레임에 필요한 개수만큼 동시 파괴하여 시간 정확도 100% 보장
                    while (spawnedNeckData.Count > targetRemaining)
                    {
                        int lastIdx = spawnedNeckData.Count - 1;
                        var lastData = spawnedNeckData[lastIdx];

                        if (lastData.go != null) Destroy(lastData.go);
                        spawnedNeckData.RemoveAt(lastIdx);

                        if (spawnedNeckData.Count > 0)
                        {
                            var prevData = spawnedNeckData[spawnedNeckData.Count - 1];
                            Vector2 dir = Quaternion.Euler(0f, 0f, prevData.angle) * Vector2.up;
                            currentNeckTipPos = prevData.startPos + dir * SINGLE_NECK_HEIGHT;
                            currentNeckAngle = prevData.angle;
                        }
                        else
                        {
                            currentNeckTipPos = new Vector2(0f, -10f);
                            currentNeckAngle = 0f;
                        }

                        if (longNeckHeadImage != null)
                        {
                            longNeckHeadImage.rectTransform.anchoredPosition = currentNeckTipPos;
                            longNeckHeadImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentNeckAngle);
                            longNeckHeadImage.transform.SetAsLastSibling();
                        }
                    }

                    yield return null;
                }
            }

            ClearSpawnedNecks();
            isRetracting = false;
            retractRoutine = null;
        }

        private void EnsureLongNeckSetup()
        {
            if (longNeckContainer != null) return;

            GameObject containerGO = new GameObject("LongNeckContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);
            longNeckContainer = containerGO.GetComponent<RectTransform>();
            longNeckContainer.anchorMin = Vector2.zero;
            longNeckContainer.anchorMax = Vector2.one;
            longNeckContainer.offsetMin = Vector2.zero;
            longNeckContainer.offsetMax = Vector2.zero;

            // 상단 UI 버튼 및 메뉴를 가리지 않도록 overrideSorting Canvas를 제거하고 Canvas 표준 계층 순서 적용

            if (longNeckHeadSprite == null) longNeckHeadSprite = LongNeckCatDisplayHelper.GetHeadSprite();
            if (longNeckBodySprite == null) longNeckBodySprite = LongNeckCatDisplayHelper.GetBodySprite();
            if (longNeckNeckSprite == null) longNeckNeckSprite = LongNeckCatDisplayHelper.GetNeckSprite();

            // 1. Body (Original rect 600x377 -> 280x176)
            GameObject bodyGO = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGO.transform.SetParent(longNeckContainer, false);
            longNeckBodyImage = bodyGO.GetComponent<Image>();
            longNeckBodyImage.preserveAspect = true;
            longNeckBodyImage.raycastTarget = false;
            if (longNeckBodySprite != null) longNeckBodyImage.sprite = longNeckBodySprite;

            RectTransform bodyRt = bodyGO.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot = new Vector2(0.5f, 0f);
            bodyRt.anchoredPosition = new Vector2(0f, -186f);
            bodyRt.sizeDelta = new Vector2(280f, 176f);

            // 2. Head (Original rect 600x424 -> 280x198, moves UPWARD as necks spawn)
            GameObject headGO = new GameObject("Head", typeof(RectTransform), typeof(Image));
            headGO.transform.SetParent(longNeckContainer, false);
            longNeckHeadImage = headGO.GetComponent<Image>();
            longNeckHeadImage.preserveAspect = true;
            longNeckHeadImage.raycastTarget = false;
            if (longNeckHeadSprite != null) longNeckHeadImage.sprite = longNeckHeadSprite;

            RectTransform headRt = headGO.GetComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0.5f, 0.5f);
            headRt.anchorMax = new Vector2(0.5f, 0.5f);
            headRt.pivot = new Vector2(0.5f, 0f);
            headRt.anchoredPosition = new Vector2(0f, -10f);
            headRt.sizeDelta = new Vector2(280f, 198f);

            longNeckContainer.gameObject.SetActive(false);
        }

        private void SpawnNeckSegment()
        {
            EnsureLongNeckSetup();
            if (longNeckContainer == null) return;

            int index = spawnedNeckData.Count + 1;
            string neckName = $"neck{index}";

            // Calculate camera screen bounds relative to longNeckContainer's current position
            Canvas canvas = GetComponentInParent<Canvas>();
            float maxHalfW = 400f;
            float maxHalfH = 500f;
            Vector2 selfPosInCanvas = Vector2.zero;

            if (canvas != null)
            {
                var canvasRt = canvas.GetComponent<RectTransform>();
                if (canvasRt != null && canvasRt.rect.width > 100f)
                {
                    maxHalfW = canvasRt.rect.width * 0.50f;
                    maxHalfH = canvasRt.rect.height * 0.50f;
                    selfPosInCanvas = canvasRt.InverseTransformPoint(longNeckContainer.position);
                }
            }

            // Calculate screen edges relative to longNeckContainer's local origin
            float localTop    =  maxHalfH - selfPosInCanvas.y;
            float localBottom = -maxHalfH - selfPosInCanvas.y;
            float localRight  =  maxHalfW - selfPosInCanvas.x;
            float localLeft   = -maxHalfW - selfPosInCanvas.x;

            float margin = 100f;
            float outBoundTop    = localTop + margin;
            float outBoundBottom = localBottom - margin;
            float outBoundRight  = localRight + margin;
            float outBoundLeft   = localLeft - margin;

            // 1. Spawn current neck segment at current tip pos
            GameObject neckGO = new GameObject(neckName, typeof(RectTransform), typeof(Image));
            neckGO.transform.SetParent(longNeckContainer, false);

            var img = neckGO.GetComponent<Image>();
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (longNeckNeckSprite == null) longNeckNeckSprite = LongNeckCatDisplayHelper.GetNeckSprite();
            if (longNeckNeckSprite != null) img.sprite = longNeckNeckSprite;

            RectTransform neckRt = neckGO.GetComponent<RectTransform>();
            neckRt.anchorMin = new Vector2(0.5f, 0.5f);
            neckRt.anchorMax = new Vector2(0.5f, 0.5f);
            neckRt.pivot = new Vector2(0.5f, 0f);
            neckRt.anchoredPosition = currentNeckTipPos;
            neckRt.localRotation = Quaternion.Euler(0f, 0f, currentNeckAngle);
            neckRt.sizeDelta = new Vector2(280f, SINGLE_NECK_HEIGHT + 2f);

            spawnedNeckData.Add(new NeckSegmentData
            {
                go = neckGO,
                startPos = currentNeckTipPos,
                angle = currentNeckAngle
            });

            // 2. Advance tip position along current neck direction
            Vector2 dir = Quaternion.Euler(0f, 0f, currentNeckAngle) * Vector2.up;
            Vector2 nextTipPos = currentNeckTipPos + dir * SINGLE_NECK_HEIGHT;

            // 3. Check if neck tip has gone COMPLETELY outside screen boundary relative to current position
            bool isNeckAtEdge = nextTipPos.y > outBoundTop || nextTipPos.y < outBoundBottom ||
                                nextTipPos.x > outBoundRight || nextTipPos.x < outBoundLeft;

            if (isNeckAtEdge)
            {
                // Teleport start position FULLY OUTSIDE the screen boundary relative to current object!
                int edgeIndex = UnityEngine.Random.Range(0, 4); // 0: Top, 1: Bottom, 2: Left, 3: Right
                switch (edgeIndex)
                {
                    case 0: // Top edge (starts outside top screen), facing downwards
                        currentNeckTipPos = new Vector2(UnityEngine.Random.Range(localLeft + 50f, localRight - 50f), outBoundTop);
                        currentNeckAngle = 180f + UnityEngine.Random.Range(-35f, 35f);
                        break;
                    case 1: // Bottom edge (starts outside bottom screen), facing upwards
                        currentNeckTipPos = new Vector2(UnityEngine.Random.Range(localLeft + 50f, localRight - 50f), outBoundBottom);
                        currentNeckAngle = 0f + UnityEngine.Random.Range(-35f, 35f);
                        break;
                    case 2: // Left edge (starts outside left screen), facing rightwards
                        currentNeckTipPos = new Vector2(outBoundLeft, UnityEngine.Random.Range(localBottom + 50f, localTop - 50f));
                        currentNeckAngle = -90f + UnityEngine.Random.Range(-35f, 35f);
                        break;
                    case 3: // Right edge (starts outside right screen), facing leftwards
                        currentNeckTipPos = new Vector2(outBoundRight, UnityEngine.Random.Range(localBottom + 50f, localTop - 50f));
                        currentNeckAngle = 90f + UnityEngine.Random.Range(-35f, 35f);
                        break;
                }
            }
            else
            {
                currentNeckTipPos = nextTipPos;
            }

            // 4. Update Head position & rotation to current tip position (or new edge position)
            if (longNeckHeadImage != null)
            {
                longNeckHeadImage.rectTransform.anchoredPosition = currentNeckTipPos;
                longNeckHeadImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentNeckAngle);
                longNeckHeadImage.transform.SetAsLastSibling(); // 머리는 항상 최상단 목 위에 배치!
            }
        }

        private void ClearSpawnedNecks()
        {
            foreach (var data in spawnedNeckData)
            {
                if (data.go != null) Destroy(data.go);
            }
            spawnedNeckData.Clear();
            currentNeckTipPos = new Vector2(0f, -10f);
            currentNeckAngle = 0f;
            if (longNeckHeadImage != null)
            {
                longNeckHeadImage.rectTransform.anchoredPosition = new Vector2(0f, -10f);
                longNeckHeadImage.rectTransform.localRotation = Quaternion.identity;
            }
        }

        private void RefreshLongNeckDisplay(bool isLongNeck)
        {
            EnsureLongNeckSetup();
            if (longNeckContainer != null)
            {
                longNeckContainer.gameObject.SetActive(isLongNeck);
                if (cardImage != null)
                {
                    cardImage.enabled = true;
                    cardImage.color = isLongNeck ? new Color(1f, 1f, 1f, 0f) : Color.white;
                    cardImage.raycastTarget = true;
                }
                
                if (!isLongNeck)
                {
                    ClearSpawnedNecks();
                }
            }
        }

        private Vector3 defaultTargetScale = Vector3.zero;

        private void EnsureBaseScale(Transform targetTf)
        {
            if (defaultTargetScale.sqrMagnitude < 0.01f)
            {
                defaultTargetScale = targetTf != null && targetTf.localScale.sqrMagnitude >= 0.01f
                    ? targetTf.localScale
                    : Vector3.one;
            }
        }

        private Coroutine clickBounceRoutine;

        public void TriggerClickBounce()
        {
            if (isRetracting) return; // 콤보가 끊겨 역방향 복귀 중일 때는 클릭 입력 잠금!

            // 메인 클릭 시 공명하여 170, 174 등 밈 카드의 이미지 루프 전환
            CycleMemeCatImage();

            Transform targetTf = cardImage != null ? cardImage.transform : transform;
            EnsureBaseScale(targetTf);

            string socketCardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;

            bool is171Active = (longNeckContainer != null && longNeckContainer.gameObject.activeSelf);
            if (!is171Active && !string.IsNullOrEmpty(socketCardId))
            {
                if (socketCardId == "0171" || socketCardId == "171" || (int.TryParse(socketCardId, out int num) && num == 171))
                    is171Active = true;
            }

            if (is171Active)
            {
                EnsureLongNeckSetup();
                if (!longNeckContainer.gameObject.activeSelf) longNeckContainer.gameObject.SetActive(true);
                if (cardImage != null)
                {
                    cardImage.enabled = true;
                    cardImage.color = new Color(1f, 1f, 1f, 0f);
                    cardImage.raycastTarget = true;
                }

                // 클릭할 때마다 하이어라키 맵에 neck1, neck2, neck3... 게임오브젝트를 즉시 새로 생성!
                SpawnNeckSegment();
            }

            // Reset to clean default scale before starting a new click bounce
            if (targetTf != null) targetTf.localScale = defaultTargetScale;
            transform.localScale = defaultTargetScale;

            if (clickBounceRoutine != null) StopCoroutine(clickBounceRoutine);
            clickBounceRoutine = StartCoroutine(ClickBounceRoutine(targetTf));
        }

        private System.Collections.IEnumerator ClickBounceRoutine(Transform targetTf)
        {
            if (targetTf == null) yield break;
            EnsureBaseScale(targetTf);
            Vector3 baseScale = defaultTargetScale;

            float duration = 0.14f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float mult;
                if (t < 0.35f)
                {
                    float subT = t / 0.35f;
                    mult = Mathf.Lerp(1.0f, 0.94f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else if (t < 0.70f)
                {
                    float subT = (t - 0.35f) / 0.35f;
                    mult = Mathf.Lerp(0.94f, 1.06f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else
                {
                    float subT = (t - 0.70f) / 0.30f;
                    mult = Mathf.Lerp(1.06f, 1.0f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                targetTf.localScale = baseScale * mult;
                if (targetTf != transform) transform.localScale = baseScale * mult;
                yield return null;
            }

            targetTf.localScale = baseScale;
            transform.localScale = baseScale;
            clickBounceRoutine = null;
        }
    }
}
