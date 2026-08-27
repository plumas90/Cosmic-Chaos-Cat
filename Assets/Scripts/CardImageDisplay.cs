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
        private Sprite   lastSizedSprite;
        private string   lastVisualCardId;
        private int      lastVisualStage = -1;
        private bool     lastHiddenReplacementActive;
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
        private int tailClickSequenceStep = 0;
        private Coroutine spadeDeckShuffleRoutine;
        private Coroutine fistClashRoutine;
        private readonly List<Image> spadeDeckLayers = new List<Image>();
        private RectTransform misfortuneContainer;
        private Image misfortuneRearLadder;
        private Image misfortuneCat;
        private Image misfortuneFrontLadder;
        private Sprite misfortuneCatA;
        private Sprite misfortuneCatB;
        private float misfortuneCatX;
        private bool misfortuneUsesSecondFrame;
        private readonly List<GameObject> hungryBiscuits = new List<GameObject>();
        private Vector2 originalAnchorMin;
        private Vector2 originalAnchorMax;
        private Vector2 originalPivot;
        private Vector2 originalAnchoredPosition;
        private Sprite hungryBaseSprite;
        private Sprite hungryBiteSprite;
        private Coroutine hungryBiteRoutine;
        private bool hungryWasActive;
        private readonly List<GameObject> rainbowCats = new List<GameObject>();
        private int rainbowCatSequenceIndex;
        private float rainbowButtonPressedUntil;
        private bool rainbowWasActive;
        private Image portalLeftImage;
        private Image portalRightImage;
        private List<Sprite> portalCatSprites;
        private bool portalWasActive;
        private Sprite portalCurrentCatSprite;
        private Image punchDoorImage;
        private readonly List<GameObject> punchHoleObjects = new List<GameObject>();
        private int punchPhase;
        private bool punchSequenceCompleted;
        private Coroutine punchDoorRoutine;
        private Sprite punchCurrentCatSprite;
        private Material punchDoorMaterial;
        private Material thunderLightningMaterial;
        private Image ooRollingImage;
        private bool ooRollingActive;
        private float ooRollingX;
        private float ooRollingY;
        private RectTransform ooRollingButtonRect;
        private Vector2 ooRollingButtonOriginalPosition;
        private Quaternion ooRollingButtonOriginalRotation;
        private bool ooRollingButtonCaptured;
        private int flyCatStageFiveSequenceIndex;
        private Image blackEyesOverlay;
        private Coroutine blackEyesBlinkRoutine;
        private bool blackEyesWasActive;
        private Coroutine perfectWorldFlashRoutine;
        private int ticklingSequenceStep;
        private GameObject burgerMakerCenterStack;
        private readonly List<GameObject> spawnedBurgerStacks = new List<GameObject>();
        private readonly List<GameObject> floatingOutBurgerStacks = new List<GameObject>();
        private GameObject activeBurgerStack;
        private int nextBurgerIngredientIndex;
        private static readonly int[] TicklingSpriteSequence =
        {
            0,
            1, 2, 1, 2, 1, 2, 1, 2, 1, 2,
            3
        };
        private const float SeaCatBubbleRiseSpeed = 150f;

        public void CycleMemeCatImage()
        {
            if (isAutoIdleActive) return; // 152번 자동 연속 회전 카드는 클릭과 무관하게 자동 애니메이션 수행

            string socketCardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
            if (string.IsNullOrEmpty(socketCardId)) return;

            var entry = gameManager.CardCatalog?.FindById(socketCardId);
            int selectedStage = gameManager.GetSocketSelectedStage(mySocket);
            var sprites = GetActiveSpriteList(socketCardId, entry, selectedStage);

            if (IsPortalCat(socketCardId))
            {
                TeleportPortalCat(entry);
                return;
            }

            if (IsPunchCat(socketCardId))
            {
                AdvancePunchCat(entry);
                return;
            }

            if (IsHungry(socketCardId))
            {
                SpawnHungryBiscuit(entry);
                return;
            }

            if (IsMisfortune(socketCardId))
            {
                MoveMisfortuneCat();
                return;
            }

            if (IsFlyCat(socketCardId))
            {
                SpawnFlyCatProjectiles(entry);
                return;
            }

            if (IsPerfectWorldCat(socketCardId))
            {
                TryFlashPerfectWorld(entry);
                return;
            }

            if (IsTicklingCat(socketCardId))
            {
                AdvanceTicklingSequence(sprites);
                return;
            }

            if (IsBurgerMaker(socketCardId))
            {
                DropNextBurgerIngredient(entry);
                return;
            }

            if (IsCatInTheBox(socketCardId))
            {
                AdvanceCatInTheBox(entry);
                return;
            }

            if (IsCatonato(socketCardId))
            {
                AdvanceCatonato(entry, sprites);
                return;
            }

            if (IsTailClickAnimation(socketCardId))
            {
                AdvanceTailClickAnimation(entry);
                return;
            }

            if (IsRandomClickSingle(socketCardId))
            {
                if (sprites != null && sprites.Count >= 2 && cardImage != null)
                {
                    currentSpriteIndex = Random.Range(0, sprites.Count);
                    cardImage.sprite = sprites[currentSpriteIndex];
                    FitSubClickSpriteToBounds(cardImage.sprite);
                }
                return;
            }

            if (IsSpadeDeck(socketCardId))
            {
                if (sprites != null && sprites.Count >= 2 && spadeDeckShuffleRoutine == null)
                    spadeDeckShuffleRoutine = StartCoroutine(ShuffleSpadeDeck(sprites));
                return;
            }

            if (IsFistClashReward(socketCardId))
            {
                if (entry != null && entry.BreakthroughSprites != null &&
                    entry.BreakthroughSprites.Length >= 2 && fistClashRoutine == null)
                    fistClashRoutine = StartCoroutine(PlayFistClash(entry.BreakthroughSprites[0], entry.BreakthroughSprites[1], entry.CardSprite));
                return;
            }

            if (IsRainbowButton(socketCardId))
            {
                SpawnRainbowCat(entry);
                return;
            }

            if (sprites != null && sprites.Count >= 2)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Count;
                Sprite targetSp = sprites[currentSpriteIndex];
                if (cardImage != null && targetSp != null)
                {
                    cardImage.sprite = targetSp;
                    FitSubClickSpriteToBounds(targetSp);
                }
                if (IsSeaCat(socketCardId) && (currentSpriteIndex == 1 || currentSpriteIndex == 2))
                    SpawnSeaCatBubble(entry);
            }
        }

        public void ToggleMemeCatImage()
        {
            CycleMemeCatImage();
        }

        private static bool IsSpadeDeck(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 215;
        }

        private static bool IsFistClashReward(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 224;
        }

        private static bool IsMisfortune(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 232;
        }

        private static bool IsOORollingCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 279;
        }

        private static bool IsFlyCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 281;
        }

        private static bool IsRandomClickSingle(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber >= 289 && cardNumber <= 291;
        }

        private static bool IsBlackEyes(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 319;
        }

        private static bool IsPerfectWorldCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 321;
        }

        private static bool IsTicklingCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 320;
        }

        private static bool IsBurgerMaker(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 326;
        }

        private static bool IsCatInTheBox(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 327;
        }

        private static bool IsCatonato(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 336;
        }

        private static bool IsTailClickAnimation(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 339;
        }

        private void AdvanceTailClickAnimation(CardEntry card)
        {
            if (cardImage == null || card?.BreakthroughSprites == null ||
                card.BreakthroughSprites.Length < 5) return;

            int frameIndex;
            if (tailClickSequenceStep < 6)
            {
                tailClickSequenceStep++;
                frameIndex = tailClickSequenceStep % 2 == 1 ? 1 : 2;
            }
            else if (tailClickSequenceStep == 6)
            {
                tailClickSequenceStep = 7;
                frameIndex = Random.value < 0.5f ? 3 : 4;
            }
            else
            {
                tailClickSequenceStep = 0;
                frameIndex = 0;
            }

            Sprite frame = card.BreakthroughSprites[frameIndex];
            if (frame == null) return;
            cardImage.sprite = frame;
            FitSubClickSpriteToBounds(frame);
        }

        private void AdvanceCatonato(CardEntry card, List<Sprite> bodySprites)
        {
            if (cardImage != null && bodySprites != null && bodySprites.Count > 0)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % bodySprites.Count;
                Sprite bodySprite = bodySprites[currentSpriteIndex];
                if (bodySprite != null)
                {
                    cardImage.sprite = bodySprite;
                    FitSubClickSpriteToBounds(bodySprite);
                }
            }

            if (card?.EffectSprites == null || card.EffectSprites.Length == 0) return;
            Sprite sharkSprite = card.EffectSprites[Random.Range(0, card.EffectSprites.Length)];
            if (sharkSprite != null) LaunchCatonatoShark(sharkSprite);
        }

        private void LaunchCatonatoShark(Sprite sharkSprite)
        {
            Canvas canvas = cardImage != null ? cardImage.canvas : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null || cardImage == null || sharkSprite == null) return;

            RectTransform cardRect = cardImage.rectTransform;
            Vector2 origin = GetCanvasPointFromCardNormalized(cardRect, canvasRect, cardRect.rect, 0.5f, 0.5f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Rect bounds = canvasRect.rect;
            float size = mySocket == ClickSocketSlot.Center ? 420f : 315f;
            float distanceX = Mathf.Abs(direction.x) > 0.001f
                ? ((direction.x > 0f ? bounds.xMax : bounds.xMin) - origin.x) / direction.x
                : float.PositiveInfinity;
            float distanceY = Mathf.Abs(direction.y) > 0.001f
                ? ((direction.y > 0f ? bounds.yMax : bounds.yMin) - origin.y) / direction.y
                : float.PositiveInfinity;
            float distance = Mathf.Min(distanceX, distanceY) + size;
            Vector2 target = origin + direction * distance;

            Image flyingShark = CreateEffectImage("CatonatoFlyingShark", canvasRect, sharkSprite);
            RectTransform rect = flyingShark.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = origin;
            rect.sizeDelta = new Vector2(size, size * sharkSprite.rect.height / sharkSprite.rect.width);
            // Source illustrations face left. Mirror only when travelling right.
            rect.localScale = new Vector3(direction.x > 0f ? -1f : 1f, 1f, 1f);
            rect.SetAsLastSibling();
            StartCoroutine(AnimateCatonatoShark(rect, direction, target));
        }

        private System.Collections.IEnumerator AnimateCatonatoShark(
            RectTransform flyingShark, Vector2 direction, Vector2 target)
        {
            const float duration = 0.8f;
            float elapsed = 0f;
            Vector2 origin = flyingShark != null ? flyingShark.anchoredPosition : Vector2.zero;
            float rotation = Random.Range(-18f, 18f);
            while (flyingShark != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                flyingShark.anchoredPosition = Vector2.LerpUnclamped(origin, target, t * t);
                flyingShark.localRotation = Quaternion.Euler(0f, 0f, rotation * t + direction.y * 8f);
                yield return null;
            }
            if (flyingShark != null) Destroy(flyingShark.gameObject);
        }

        private void AdvanceCatInTheBox(CardEntry card)
        {
            if (cardImage == null || card?.BreakthroughSprites == null ||
                card.BreakthroughSprites.Length < 5) return;

            currentSpriteIndex = (currentSpriteIndex + 1) % 5;
            Sprite frame = card.BreakthroughSprites[currentSpriteIndex];
            if (frame != null)
            {
                cardImage.sprite = frame;
                FitSubClickSpriteToBounds(frame);
            }

            if (currentSpriteIndex == 4 && card.EffectSprites != null &&
                card.EffectSprites.Length > 0 && card.EffectSprites[0] != null)
                LaunchCatFromBox(card.EffectSprites[0]);
        }

        private void LaunchCatFromBox(Sprite catSprite)
        {
            Canvas canvas = cardImage != null ? cardImage.canvas : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null || cardImage == null || catSprite == null) return;

            RectTransform cardRect = cardImage.rectTransform;
            Vector2 origin = GetCanvasPointFromCardNormalized(cardRect, canvasRect, cardRect.rect, 0.5f, 0.62f);
            bool flyLeft = Random.value < 0.5f;
            float size = mySocket == ClickSocketSlot.Center ? 540f : 405f;
            Rect bounds = canvasRect.rect;
            float exitMargin = size * 0.5f + 20f;
            float verticalMargin = Mathf.Min(size * 0.35f, bounds.height * 0.25f);
            Vector2 target = new Vector2(
                flyLeft ? bounds.xMin - exitMargin : bounds.xMax + exitMargin,
                Random.Range(bounds.yMin + verticalMargin, bounds.yMax - verticalMargin));

            Image flyingCat = CreateEffectImage("CatInTheBoxFlyingCat", canvasRect, catSprite);
            RectTransform rect = flyingCat.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = origin;
            rect.sizeDelta = new Vector2(size, size * catSprite.rect.height / catSprite.rect.width);
            rect.localScale = new Vector3(flyLeft ? -1f : 1f, 1f, 1f);
            rect.SetAsLastSibling();
            StartCoroutine(AnimateCatFlyingAway(rect, origin, target));
        }

        private System.Collections.IEnumerator AnimateCatFlyingAway(
            RectTransform flyingCat, Vector2 origin, Vector2 target)
        {
            const float duration = 0.75f;
            const float arcHeight = 170f;
            float elapsed = 0f;
            while (flyingCat != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 position = Vector2.LerpUnclamped(origin, target, t);
                position.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                flyingCat.anchoredPosition = position;
                yield return null;
            }
            if (flyingCat != null) Destroy(flyingCat.gameObject);
        }

        private void AdvanceTicklingSequence(List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count < 4 || cardImage == null) return;

            ticklingSequenceStep = (ticklingSequenceStep + 1) % TicklingSpriteSequence.Length;
            currentSpriteIndex = TicklingSpriteSequence[ticklingSequenceStep];
            Sprite targetSprite = sprites[currentSpriteIndex];
            if (targetSprite == null) return;

            cardImage.sprite = targetSprite;
            FitSubClickSpriteToBounds(targetSprite);
        }

        private void TryFlashPerfectWorld(CardEntry card)
        {
            if (card == null || card.EffectSprites == null || card.EffectSprites.Length == 0 ||
                card.EffectSprites[0] == null || Random.value >= 0.01f) return;
            if (perfectWorldFlashRoutine != null) StopCoroutine(perfectWorldFlashRoutine);
            perfectWorldFlashRoutine = StartCoroutine(FlashPerfectWorld(card));
        }

        private System.Collections.IEnumerator FlashPerfectWorld(CardEntry card)
        {
            if (cardImage != null) cardImage.sprite = card.EffectSprites[0];
            yield return new WaitForSecondsRealtime(0.5f);
            string cardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
            if (cardImage != null && IsPerfectWorldCat(cardId)) cardImage.sprite = card.CardSprite;
            perfectWorldFlashRoutine = null;
        }

        private void TryBlinkBlackEyes(CardEntry card)
        {
            if (card == null || card.EffectSprites == null || card.EffectSprites.Length == 0 ||
                Random.value >= 0.01f) return;
            if (blackEyesBlinkRoutine != null) StopCoroutine(blackEyesBlinkRoutine);
            blackEyesBlinkRoutine = StartCoroutine(BlinkBlackEyes(card.EffectSprites[0]));
        }

        private System.Collections.IEnumerator BlinkBlackEyes(Sprite openSprite)
        {
            if (blackEyesOverlay != null && openSprite != null) blackEyesOverlay.sprite = openSprite;
            yield return new WaitForSecondsRealtime(0.5f);
            string cardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
            CardEntry card = gameManager != null ? gameManager.CardCatalog?.FindById(cardId) : null;
            if (blackEyesOverlay != null && IsBlackEyes(cardId) && card != null)
                blackEyesOverlay.sprite = card.CardSprite;
            blackEyesBlinkRoutine = null;
        }

        private void RefreshBlackEyesDisplay(bool active, CardEntry card)
        {
            if (active && card != null && card.CardSprite != null && cardImage != null)
            {
                blackEyesWasActive = true;
                Canvas canvas = cardImage.canvas;
                RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
                if (canvasRect == null) return;
                if (blackEyesOverlay == null)
                {
                    blackEyesOverlay = CreateEffectImage("BlackEyesBackgroundCover", canvasRect, card.CardSprite);
                    blackEyesOverlay.raycastTarget = false;
                    RectTransform overlayRect = blackEyesOverlay.rectTransform;
                    overlayRect.anchorMin = Vector2.zero;
                    overlayRect.anchorMax = Vector2.one;
                    overlayRect.offsetMin = Vector2.zero;
                    overlayRect.offsetMax = Vector2.zero;
                    overlayRect.localScale = Vector3.one;
                    overlayRect.localRotation = Quaternion.identity;
                    int buttonSibling = transform.parent == canvasRect ? transform.GetSiblingIndex() : canvasRect.childCount - 1;
                    overlayRect.SetSiblingIndex(Mathf.Max(0, buttonSibling));
                }
                blackEyesOverlay.sprite = blackEyesBlinkRoutine == null ? card.CardSprite : blackEyesOverlay.sprite;
                blackEyesOverlay.color = Color.white;
                blackEyesOverlay.preserveAspect = false;
                blackEyesOverlay.enabled = true;
                // Keep this transparent Image enabled so it remains the full-screen art's click target.
                cardImage.color = new Color(1f, 1f, 1f, 0f);
            }
            else if (blackEyesWasActive)
            {
                if (blackEyesBlinkRoutine != null) StopCoroutine(blackEyesBlinkRoutine);
                blackEyesBlinkRoutine = null;
                if (blackEyesOverlay != null) Destroy(blackEyesOverlay.gameObject);
                blackEyesOverlay = null;
                if (cardImage != null) cardImage.color = Color.white;
                blackEyesWasActive = false;
            }
        }

        private void SpawnFlyCatProjectiles(CardEntry card)
        {
            if (card == null || card.EffectSprites == null || gameManager == null || cardImage == null)
                return;
            if (!gameManager.TryGetCardProgress(card.Id, out var progress) || progress == null)
                return;

            int stage = Mathf.Clamp(progress.BreakthroughCount + 1, 1, 5);
            int split = Mathf.Clamp(card.EffectSpriteGroupSplit, 0, card.EffectSprites.Length);
            if (stage < 3 || split <= 0) return;

            Sprite randomStageThree = card.EffectSprites[Random.Range(0, split)];
            SpawnFlyCatProjectile(randomStageThree);

            int stageFiveCount = card.EffectSprites.Length - split;
            if (stage >= 5 && stageFiveCount > 0)
            {
                Sprite sequential = card.EffectSprites[split + flyCatStageFiveSequenceIndex];
                flyCatStageFiveSequenceIndex = (flyCatStageFiveSequenceIndex + 1) % stageFiveCount;
                SpawnFlyCatProjectile(sequential);
            }
        }

        private void SpawnFlyCatProjectile(Sprite sprite)
        {
            if (sprite == null || cardImage == null) return;
            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            Image projectile = CreateEffectImage("FlyCatProjectile", canvasRect, sprite);
            RectTransform rect = projectile.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            float width = Mathf.Clamp(canvasRect.rect.width * 0.16f, 130f, 310f);
            float aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            rect.sizeDelta = new Vector2(width, width / Mathf.Max(0.15f, aspect));
            rect.anchoredPosition = new Vector2(
                Random.Range(
                    canvasRect.rect.xMin + width * 0.5f,
                    canvasRect.rect.xMax + width * 1.5f),
                canvasRect.rect.yMax + rect.sizeDelta.y * 0.55f);

            float downLeftAngle = 225f + Random.Range(-15f, 15f);
            float radians = downLeftAngle * Mathf.Deg2Rad;
            float speed = canvasRect.rect.height * Random.Range(0.48f, 0.68f);
            Vector2 velocity = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * speed;
            rect.localEulerAngles = new Vector3(0f, 0f, downLeftAngle - 180f);
            rect.SetAsLastSibling();
            StartCoroutine(FallFlyCatProjectile(rect, canvasRect, velocity));
        }

        private System.Collections.IEnumerator FallFlyCatProjectile(
            RectTransform projectile, RectTransform canvasRect, Vector2 velocity)
        {
            while (projectile != null)
            {
                projectile.anchoredPosition += velocity * Time.unscaledDeltaTime;
                float margin = Mathf.Max(projectile.rect.width, projectile.rect.height);
                Vector2 position = projectile.anchoredPosition;
                if (position.x < canvasRect.rect.xMin - margin ||
                    position.y < canvasRect.rect.yMin - margin)
                    break;
                yield return null;
            }
            if (projectile != null) Destroy(projectile.gameObject);
        }

        private static bool IsHungry(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 233;
        }

        private static bool IsSeaCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 266;
        }

        private static bool IsRainbowButton(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 268;
        }

        private static float GetCardBaseScale(string cardId)
        {
            if (IsRainbowButton(cardId)) return 0.5f;
            if (IsPortalCat(cardId)) return 0.75f;
            if (int.TryParse(cardId, out int cardNumber) && cardNumber == 324) return 0.75f;
            return 1f;
        }

        private void SpawnRainbowCat(CardEntry card)
        {
            if (cardImage == null || card == null || card.EffectSprites == null ||
                card.EffectSprites.Length < 8 || gameManager == null)
                return;
            if (!gameManager.TryGetCardProgress(card.Id, out var progress) || progress == null) return;

            rainbowCats.RemoveAll(go => go == null);
            int breakthroughStage = Mathf.Clamp(progress.BreakthroughCount + 1, 1, 5);
            int maxRainbowCats = breakthroughStage * 3;
            rainbowButtonPressedUntil = Time.unscaledTime + 0.12f;
            if (rainbowCats.Count >= maxRainbowCats) return;

            Sprite catSprite = card.EffectSprites[1 + rainbowCatSequenceIndex];
            rainbowCatSequenceIndex = (rainbowCatSequenceIndex + 1) % 7;
            if (catSprite == null) return;

            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            Image projectile = CreateEffectImage("RainbowCatProjectile", canvasRect, catSprite);
            RectTransform projectileRect = projectile.rectTransform;
            projectileRect.anchorMin = projectileRect.anchorMax = new Vector2(0.5f, 0.5f);
            projectileRect.pivot = new Vector2(0.5f, 0.5f);
            float targetWidth = Mathf.Clamp(canvasRect.rect.width * 0.14f, 110f, 260f);
            float aspect = catSprite.rect.height > 0f ? catSprite.rect.width / catSprite.rect.height : 1f;
            projectileRect.sizeDelta = new Vector2(targetWidth, targetWidth / Mathf.Max(0.1f, aspect));
            float margin = projectileRect.rect.height * 0.5f;
            float spawnY = Random.Range(
                canvasRect.rect.yMin + canvasRect.rect.height * 0.15f + margin,
                canvasRect.rect.yMin + canvasRect.rect.height * 0.75f - margin);
            projectileRect.anchoredPosition = new Vector2(
                canvasRect.rect.xMin - projectileRect.rect.width * 0.55f,
                spawnY);
            projectileRect.localEulerAngles = new Vector3(0f, 0f, Random.Range(-18f, 18f));
            projectile.raycastTarget = false;
            projectile.color = Color.white;
            projectileRect.SetAsLastSibling();

            float launchAngle = Random.Range(12f, 38f) * Mathf.Deg2Rad;
            float launchForce = canvasRect.rect.width * Random.Range(0.48f, 0.78f);
            Vector2 velocity = new Vector2(Mathf.Cos(launchAngle), Mathf.Sin(launchAngle)) * launchForce;
            float angularVelocity = Random.Range(-110f, 110f);
            rainbowCats.Add(projectile.gameObject);
            StartCoroutine(FlyRainbowCat(projectileRect, canvasRect, velocity, angularVelocity));
        }

        private System.Collections.IEnumerator FlyRainbowCat(
            RectTransform projectile,
            RectTransform canvasRect,
            Vector2 velocity,
            float angularVelocity)
        {
            float gravity = canvasRect.rect.height * 0.62f;
            while (projectile != null)
            {
                float dt = Time.deltaTime;
                projectile.anchoredPosition += velocity * dt;
                velocity.y -= gravity * dt;
                projectile.Rotate(0f, 0f, angularVelocity * dt);

                Vector2 p = projectile.anchoredPosition;
                float marginX = projectile.rect.width;
                float marginY = projectile.rect.height;
                if (p.x > canvasRect.rect.xMax + marginX ||
                    p.y < canvasRect.rect.yMin - marginY ||
                    p.y > canvasRect.rect.yMax + marginY)
                    break;
                yield return null;
            }
            if (projectile != null)
            {
                rainbowCats.Remove(projectile.gameObject);
                Destroy(projectile.gameObject);
            }
        }

        private void RefreshRainbowButton(bool active, CardEntry card)
        {
            if (active)
            {
                bool enteringRainbow = !rainbowWasActive;
                rainbowWasActive = true;
                Vector3 rainbowScale = originalScale * 0.5f;
                if (enteringRainbow && clickBounceRoutine == null)
                {
                    transform.localScale = rainbowScale;
                    if (cardImage != null && cardImage.transform != transform)
                        cardImage.transform.localScale = rainbowScale;
                }
                defaultTargetScale = rainbowScale;
                if (cardImage != null && card?.EffectSprites != null && card.EffectSprites.Length > 0 &&
                    Time.unscaledTime < rainbowButtonPressedUntil && card.EffectSprites[0] != null)
                    cardImage.sprite = card.EffectSprites[0];
            }
            else if (rainbowWasActive)
            {
                foreach (GameObject rainbowCat in rainbowCats)
                    if (rainbowCat != null) Destroy(rainbowCat);
                rainbowCats.Clear();
                rainbowCatSequenceIndex = 0;
                Vector3 nextCardScale = originalScale * GetCardBaseScale(card != null ? card.Id : null);
                if (clickBounceRoutine == null)
                {
                    transform.localScale = nextCardScale;
                    if (cardImage != null && cardImage.transform != transform)
                        cardImage.transform.localScale = nextCardScale;
                }
                defaultTargetScale = nextCardScale;
                rainbowWasActive = false;
            }
        }

        private void SpawnSeaCatBubble(CardEntry entry)
        {
            if (cardImage == null || entry?.EffectSprites == null ||
                entry.EffectSprites.Length == 0 || entry.EffectSprites[0] == null)
                return;

            Canvas canvas = cardImage.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            RectTransform cardRect = cardImage.rectTransform;
            Rect bounds = cardRect.rect;
            // All four Sea Cat slices share this mouth position within their local bounds.
            Vector3 mouthLocal = new Vector3(
                bounds.xMin + bounds.width * 0.24f,
                bounds.yMin + bounds.height * 0.46f,
                0f);
            Vector3 mouthWorld = cardRect.TransformPoint(mouthLocal);

            var bubbleObject = new GameObject("SeaCatBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
            bubbleRect.SetParent(canvas.transform, false);
            bubbleRect.position = mouthWorld;
            bubbleRect.SetAsLastSibling();
            float size = Mathf.Clamp(Mathf.Min(bounds.width, bounds.height) * 0.18f, 42f, 110f);
            bubbleRect.sizeDelta = new Vector2(size, size);
            bubbleRect.localScale = Vector3.one * 2f;

            Image bubbleImage = bubbleObject.GetComponent<Image>();
            bubbleImage.sprite = entry.EffectSprites[0];
            bubbleImage.preserveAspect = true;
            bubbleImage.raycastTarget = false;
            bubbleImage.color = Color.white;
            StartCoroutine(RiseSeaCatBubble(bubbleRect, canvas));
        }

        private System.Collections.IEnumerator RiseSeaCatBubble(RectTransform bubble, Canvas canvas)
        {
            float elapsed = 0f;
            float horizontalPhase = Random.Range(0f, Mathf.PI * 2f);
            while (bubble != null && elapsed < 12f)
            {
                float dt = Time.deltaTime;
                elapsed += dt;
                float drift = Mathf.Sin(elapsed * 2.4f + horizontalPhase) * 22f;
                bubble.anchoredPosition += new Vector2(drift * dt, SeaCatBubbleRiseSpeed * dt);

                Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    bubble.position);
                if (screenPoint.y > Screen.height + bubble.rect.height) break;
                yield return null;
            }
            if (bubble != null) Destroy(bubble.gameObject);
        }

        private static bool IsPortalCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 236;
        }

        private static bool IsPunchCat(string cardId)
        {
            return int.TryParse(cardId, out int cardNumber) && cardNumber == 238;
        }

        private System.Collections.IEnumerator PlayFistClash(Sprite dog, Sprite cat, Sprite combined)
        {
            if (cardImage == null || dog == null || cat == null)
            {
                fistClashRoutine = null;
                yield break;
            }

            RectTransform front = cardImage.rectTransform;
            Transform parent = front.parent;
            Image left = CreateEffectImage("DogFistClashHalf", parent, dog);
            Image right = CreateEffectImage("CatFistClashHalf", parent, cat);
            RectTransform leftRect = left.rectTransform;
            RectTransform rightRect = right.rectTransform;
            CopyImageLayout(cardImage, left);
            CopyImageLayout(cardImage, right);
            leftRect.SetSiblingIndex(front.GetSiblingIndex() + 1);
            rightRect.SetSiblingIndex(front.GetSiblingIndex() + 2);

            Vector2 center = front.anchoredPosition;
            float textureWidth = dog.texture != null ? dog.texture.width : 1536f;
            float textureHeight = dog.texture != null ? dog.texture.height : 1024f;
            float fitScale = Mathf.Min(front.rect.width / textureWidth, front.rect.height / textureHeight);
            float fittedWidth = textureWidth * fitScale;

            // Rebuild both cropped sprites at their exact source-PNG coordinates.
            // Then close the source's 10 px transparent seam equally from both sides,
            // so the two inner sprite edges (the fist tips) meet at one point.
            Vector2 leftTarget = center + new Vector2(
                (dog.rect.x + dog.rect.width * 0.5f - textureWidth * 0.5f) * fitScale,
                (dog.rect.y + dog.rect.height * 0.5f - textureHeight * 0.5f) * fitScale);
            Vector2 rightTarget = center + new Vector2(
                (cat.rect.x + cat.rect.width * 0.5f - textureWidth * 0.5f) * fitScale,
                (cat.rect.y + cat.rect.height * 0.5f - textureHeight * 0.5f) * fitScale);
            float sourceGap = Mathf.Max(0f, cat.rect.xMin - dog.rect.xMax) * fitScale;
            leftTarget.x += sourceGap * 0.5f;
            rightTarget.x -= sourceGap * 0.5f;

            leftRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, dog.rect.width * fitScale);
            leftRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, dog.rect.height * fitScale);
            rightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cat.rect.width * fitScale);
            rightRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cat.rect.height * fitScale);

            // Define the opening pose by the visible gap between the two fist tips.
            // Each half moves outward by half the requested gap, keeping the pose centered.
            float openingGap = Mathf.Max(130f, fittedWidth * 0.55f);
            float halfOpeningGap = openingGap * 0.5f;
            Vector2 leftStart = leftTarget + Vector2.left * halfOpeningGap;
            Vector2 rightStart = rightTarget + Vector2.right * halfOpeningGap;
            leftRect.anchoredPosition = leftStart;
            rightRect.anchoredPosition = rightStart;
            leftRect.localScale = front.localScale;
            rightRect.localScale = front.localScale;
            // Remove the original button art entirely while the two animation
            // pieces are visible, preventing any combined-image overlap.
            cardImage.enabled = false;

            const float openingHoldDuration = 0.10f;
            float openingHoldElapsed = 0f;
            while (openingHoldElapsed < openingHoldDuration)
            {
                openingHoldElapsed += Time.unscaledDeltaTime;
                leftRect.anchoredPosition = leftStart;
                rightRect.anchoredPosition = rightStart;
                cardImage.enabled = false;
                yield return null;
            }

            const float approachDuration = 0.16f;
            float elapsed = 0f;
            while (elapsed < approachDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = SmoothStep(elapsed / approachDuration);
                leftRect.anchoredPosition = Vector2.Lerp(leftStart, leftTarget, t);
                rightRect.anchoredPosition = Vector2.Lerp(rightStart, rightTarget, t);
                cardImage.enabled = false;
                yield return null;
            }

            // Contact: start the explosion now, while both fists continue through
            // one another instead of stopping at the collision point.
            Vector2 impactPoint = center + new Vector2(
                ((dog.rect.xMax + cat.rect.xMin) * 0.5f - textureWidth * 0.5f) * fitScale,
                (544f - textureHeight * 0.5f) * fitScale);

            Image explosion = CreateEffectImage("FistClashShaderExplosion", parent, null);
            RectTransform explosionRect = explosion.rectTransform;
            explosionRect.anchorMin = front.anchorMin;
            explosionRect.anchorMax = front.anchorMax;
            explosionRect.pivot = new Vector2(0.5f, 0.5f);
            float explosionSize = Mathf.Max(240f, Mathf.Min(front.rect.width, front.rect.height) * 1.25f);
            explosionRect.sizeDelta = new Vector2(explosionSize, explosionSize);
            explosionRect.anchoredPosition = impactPoint;
            explosionRect.SetSiblingIndex(front.GetSiblingIndex() + 3);

            Shader explosionShader = Resources.Load<Shader>("Shaders/UIFistClashExplosion");
            if (explosionShader == null) explosionShader = Shader.Find("CosmicChaosCat/UIFistClashExplosion");
            Material explosionMaterial = explosionShader != null ? new Material(explosionShader) : null;
            if (explosionMaterial != null) explosion.material = explosionMaterial;
            explosion.color = Color.white;

            const float followThroughDuration = 0.22f;
            float passDistance = Mathf.Max(75f, fittedWidth * 0.18f);
            Vector2 leftPassed = leftTarget + Vector2.right * passDistance;
            Vector2 rightPassed = rightTarget + Vector2.left * passDistance;
            elapsed = 0f;
            while (elapsed < followThroughDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / followThroughDuration);
                float moveT = 1f - Mathf.Pow(1f - t, 3f);
                leftRect.anchoredPosition = Vector2.Lerp(leftTarget, leftPassed, moveT);
                rightRect.anchoredPosition = Vector2.Lerp(rightTarget, rightPassed, moveT);
                cardImage.enabled = false;
                if (explosionMaterial != null) explosionMaterial.SetFloat("_Progress", t);
                explosionRect.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.35f, Mathf.Sin(t * Mathf.PI * 0.5f));
                yield return null;
            }

            // Let the expanding shock ring remain briefly after the fists pass.
            const float burstDuration = 0.10f;
            elapsed = 0f;
            while (elapsed < burstDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / burstDuration);
                cardImage.enabled = false;
                Color c = explosion.color;
                c.a = 1f - t;
                explosion.color = c;
                yield return null;
            }

            if (explosionMaterial != null) Destroy(explosionMaterial);
            Destroy(explosion.gameObject);
            Destroy(left.gameObject);
            Destroy(right.gameObject);
            cardImage.sprite = combined != null ? combined : cardImage.sprite;
            cardImage.color = Color.white;
            cardImage.enabled = true;
            FitSubClickSpriteToBounds(cardImage.sprite);
            fistClashRoutine = null;
        }

        private static Image CreateEffectImage(string objectName, Transform parent, Sprite sprite)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = sprite != null;
            image.raycastTarget = false;
            return image;
        }

        // CenterClick의 400x400 기준 완성 버거 좌표. 다른 크기는 width / 400 비율로 환산한다.
        private static readonly float[] BurgerLayerY = { -62f, -22f, -10f, 10f, 82f, 95f, 136f, 185f };

        private void RefreshBurgerMakerDisplay(bool active, CardEntry card)
        {
            if (!active || card?.EffectSprites == null ||
                card.EffectSprites.Length < 8 || cardImage == null)
            {
                if (burgerMakerCenterStack != null) Destroy(burgerMakerCenterStack);
                burgerMakerCenterStack = null;
                if (cardImage != null)
                {
                    BurgerMakerDisplayHelper.Bind(cardImage, null, false);
                    cardImage.color = Color.white;
                }
                if (!active) ClearSpawnedBurgers();
                return;
            }

            if (mySocket != ClickSocketSlot.Center)
            {
                if (burgerMakerCenterStack != null) Destroy(burgerMakerCenterStack);
                burgerMakerCenterStack = null;
                BurgerMakerDisplayHelper.Bind(cardImage, card, true);
                return;
            }

            BurgerMakerDisplayHelper.Bind(cardImage, null, false);
            if (burgerMakerCenterStack == null)
            {
                cardImage.rectTransform.sizeDelta = new Vector2(
                    Mathf.Max(400f, Mathf.Abs(originalSizeDelta.x)),
                    Mathf.Max(400f, Mathf.Abs(originalSizeDelta.y)));
                burgerMakerCenterStack = CreateBurgerStack(
                    "BurgerMakerCenterStack", cardImage.transform, card.EffectSprites, 400f, Vector2.zero, true);
                burgerMakerCenterStack.transform.SetAsLastSibling();
            }
            cardImage.color = new Color(1f, 1f, 1f, 0f);
        }

        private GameObject CreateBurgerStack(
            string objectName, Transform parent, Sprite[] ingredients, float width, Vector2 position, bool showAll)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = position;
            rootRect.sizeDelta = new Vector2(width, width * 0.82f);

            float scale = width / 400f;
            for (int i = 0; i < 8; i++)
            {
                Sprite sprite = ingredients[i];
                if (sprite == null) continue;
                Image layer = CreateEffectImage($"BurgerIngredient_{i}", rootRect, sprite);
                RectTransform rect = layer.rectTransform;
                float layerWidth = width * Mathf.Clamp(sprite.rect.width / 808f, 0.7f, 1f);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(layerWidth, layerWidth * sprite.rect.height / sprite.rect.width);
                rect.anchoredPosition = new Vector2(0f, BurgerLayerY[i] * scale);
                layer.gameObject.SetActive(showAll);
            }
            return root;
        }

        private void DropNextBurgerIngredient(CardEntry card)
        {
            if (card?.EffectSprites == null || card.EffectSprites.Length < 8 || cardImage == null) return;
            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            if (activeBurgerStack == null)
            {
                float width = mySocket == ClickSocketSlot.Center ? 220f : 165f;
                float halfWidth = width * 0.55f;
                float halfHeight = width * 0.6f;
                Rect bounds = canvasRect.rect;
                Vector2 target = new Vector2(
                    Random.Range(bounds.xMin + halfWidth, bounds.xMax - halfWidth),
                    Random.Range(bounds.yMin + halfHeight, bounds.yMax - halfHeight));
                activeBurgerStack = CreateBurgerStack(
                    "BurgerMakerSpawnedStack", canvasRect, card.EffectSprites, width, target, false);
                activeBurgerStack.transform.SetAsLastSibling();
                spawnedBurgerStacks.Add(activeBurgerStack);
                nextBurgerIngredientIndex = 0;
            }

            RectTransform root = activeBurgerStack.GetComponent<RectTransform>();
            int ingredientIndex = nextBurgerIngredientIndex;
            StartCoroutine(DropBurgerIngredient(root, canvasRect, ingredientIndex));
            nextBurgerIngredientIndex++;

            // 7번 재료가 낙하를 시작한 순간 이 버거는 완성으로 간주한다.
            if (nextBurgerIngredientIndex >= 8)
            {
                activeBurgerStack = null;
                nextBurgerIngredientIndex = 0;
            }
        }

        private System.Collections.IEnumerator DropBurgerIngredient(
            RectTransform root, RectTransform canvasRect, int ingredientIndex)
        {
            if (root == null || canvasRect == null || ingredientIndex < 0 ||
                ingredientIndex >= root.childCount) yield break;

            RectTransform layer = root.GetChild(ingredientIndex) as RectTransform;
            if (layer == null) yield break;
            Vector2 target = layer.anchoredPosition;
            float topInRoot = canvasRect.rect.yMax - root.anchoredPosition.y + 180f;
            layer.anchoredPosition = new Vector2(target.x, topInRoot);
            layer.gameObject.SetActive(true);

            const float fallDuration = 0.28f;
            Vector2 start = layer.anchoredPosition;
            float elapsed = 0f;
            while (root != null && layer != null && elapsed < fallDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                float eased = 1f - (1f - t) * (1f - t);
                layer.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
                yield return null;
            }
            if (layer != null) layer.anchoredPosition = target;
        }

        private void ClearSpawnedBurgers(bool floatOut = false)
        {
            if (!floatOut)
            {
                foreach (GameObject burger in floatingOutBurgerStacks)
                    if (burger != null) Destroy(burger);
                floatingOutBurgerStacks.Clear();
            }

            foreach (GameObject burger in spawnedBurgerStacks)
            {
                if (burger == null) continue;
                if (floatOut)
                {
                    floatingOutBurgerStacks.Add(burger);
                    StartCoroutine(FloatBurgerOutOfScreen(burger.GetComponent<RectTransform>()));
                }
                else
                    Destroy(burger);
            }
            spawnedBurgerStacks.Clear();
            activeBurgerStack = null;
            nextBurgerIngredientIndex = 0;
        }

        private System.Collections.IEnumerator FloatBurgerOutOfScreen(RectTransform burger)
        {
            if (burger == null) yield break;
            RectTransform canvasRect = burger.GetComponentInParent<Canvas>()?.transform as RectTransform;
            if (canvasRect == null)
            {
                Destroy(burger.gameObject);
                yield break;
            }

            Vector2 start = burger.anchoredPosition;
            // 재료 레이어가 루트 영역 위로 돌출되므로 루트 높이만큼 여유를 더해 완전히 퇴장시킨다.
            float targetY = canvasRect.rect.yMax + burger.rect.height + 100f;
            Vector2 target = new Vector2(start.x, targetY);
            const float duration = 1.5f;
            float elapsed = 0f;
            while (burger != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                burger.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);
                yield return null;
            }

            if (burger != null)
            {
                floatingOutBurgerStacks.Remove(burger.gameObject);
                Destroy(burger.gameObject);
            }
        }

        private System.Collections.IEnumerator ShuffleSpadeDeck(List<Sprite> sprites)
        {
            if (cardImage == null) { spadeDeckShuffleRoutine = null; yield break; }

            EnsureSpadeDeckLayers(sprites);
            int frontIndex = UnityEngine.Random.Range(0, sprites.Count);
            Sprite nextFront = sprites[frontIndex];
            RectTransform frontRect = cardImage.rectTransform;
            Transform parent = frontRect.parent;

            GameObject movingObject = new GameObject("SpadeDeckDrawCard", typeof(RectTransform), typeof(Image));
            movingObject.transform.SetParent(parent, false);
            Image movingImage = movingObject.GetComponent<Image>();
            CopyImageLayout(cardImage, movingImage);
            movingImage.sprite = nextFront;
            movingImage.raycastTarget = false;

            RectTransform movingRect = movingImage.rectTransform;
            Vector2 frontPosition = frontRect.anchoredPosition;
            Vector2 backPosition = frontPosition + new Vector2(15f, -15f);
            movingRect.anchoredPosition = backPosition;
            movingRect.localRotation = Quaternion.Euler(0f, 0f, -4f);
            movingRect.SetSiblingIndex(Mathf.Max(0, frontRect.GetSiblingIndex() - spadeDeckLayers.Count));

            // The rear card first slides out to the side, then arcs over the deck to the front.
            const float duration = 0.11f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                Vector2 position;
                float angle;
                if (t < 0.4f)
                {
                    float u = t / 0.4f;
                    position = Vector2.Lerp(backPosition, frontPosition + new Vector2(-92f, 12f), SmoothStep(u));
                    angle = Mathf.Lerp(-4f, -17f, u);
                }
                else
                {
                    float u = (t - 0.4f) / 0.6f;
                    Vector2 from = frontPosition + new Vector2(-92f, 12f);
                    position = Vector2.Lerp(from, frontPosition, SmoothStep(u));
                    position.y += Mathf.Sin(u * Mathf.PI) * 38f;
                    angle = Mathf.Lerp(-17f, 0f, u);
                    if (movingRect.GetSiblingIndex() < frontRect.GetSiblingIndex())
                        movingRect.SetSiblingIndex(frontRect.GetSiblingIndex() + 1);
                }
                movingRect.anchoredPosition = position;
                movingRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                yield return null;
            }

            currentSpriteIndex = frontIndex;
            cardImage.sprite = nextFront;
            FitSubClickSpriteToBounds(nextFront);
            UpdateSpadeDeckLayerSprites(sprites);
            Destroy(movingObject);
            spadeDeckShuffleRoutine = null;
        }

        private static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private void EnsureSpadeDeckLayers(List<Sprite> sprites)
        {
            if (cardImage == null || cardImage.transform.parent == null) return;
            const int layerCount = 4;
            while (spadeDeckLayers.Count < layerCount)
            {
                int layerIndex = spadeDeckLayers.Count;
                GameObject layerObject = new GameObject($"SpadeDeckLayer{layerIndex + 1}", typeof(RectTransform), typeof(Image));
                layerObject.transform.SetParent(cardImage.transform.parent, false);
                Image layer = layerObject.GetComponent<Image>();
                CopyImageLayout(cardImage, layer);
                layer.raycastTarget = false;
                spadeDeckLayers.Add(layer);
            }

            int frontSiblingIndex = cardImage.rectTransform.GetSiblingIndex();
            for (int i = 0; i < spadeDeckLayers.Count; i++)
            {
                Image layer = spadeDeckLayers[i];
                if (layer == null) continue;
                layer.gameObject.SetActive(true);
                RectTransform rect = layer.rectTransform;
                float depth = spadeDeckLayers.Count - i;
                rect.anchoredPosition = cardImage.rectTransform.anchoredPosition + new Vector2(depth * 4f, depth * -4f);
                rect.localRotation = Quaternion.Euler(0f, 0f, depth * -0.65f);
                rect.SetSiblingIndex(Mathf.Max(0, frontSiblingIndex - spadeDeckLayers.Count + i));
            }
            UpdateSpadeDeckLayerSprites(sprites);
        }

        private void UpdateSpadeDeckLayerSprites(List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0) return;
            for (int i = 0; i < spadeDeckLayers.Count; i++)
            {
                Image layer = spadeDeckLayers[i];
                if (layer != null) layer.sprite = sprites[(currentSpriteIndex + i + 1) % sprites.Count];
            }
        }

        private void SyncSpadeDeckLayerMotion()
        {
            if (cardImage == null || spadeDeckLayers.Count == 0) return;
            RectTransform frontRect = cardImage.rectTransform;
            for (int i = 0; i < spadeDeckLayers.Count; i++)
            {
                Image layer = spadeDeckLayers[i];
                if (layer == null || !layer.gameObject.activeSelf) continue;
                float depth = spadeDeckLayers.Count - i;
                RectTransform rect = layer.rectTransform;
                rect.anchoredPosition = frontRect.anchoredPosition + new Vector2(depth * 4f, depth * -4f);
                rect.localScale = frontRect.localScale;
                rect.localRotation = frontRect.localRotation * Quaternion.Euler(0f, 0f, depth * -0.65f);
            }
        }

        private static void CopyImageLayout(Image source, Image target)
        {
            RectTransform src = source.rectTransform;
            RectTransform dst = target.rectTransform;
            dst.anchorMin = src.anchorMin;
            dst.anchorMax = src.anchorMax;
            dst.pivot = src.pivot;
            dst.sizeDelta = src.sizeDelta;
            dst.anchoredPosition = src.anchoredPosition;
            dst.localScale = src.localScale;
            target.preserveAspect = true;
            target.color = Color.white;
            target.material = source.material;
        }

        private void SetSpadeDeckLayersVisible(bool visible, List<Sprite> sprites = null)
        {
            if (visible) EnsureSpadeDeckLayers(sprites);
            else
                foreach (Image layer in spadeDeckLayers)
                    if (layer != null) layer.gameObject.SetActive(false);
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
                originalAnchorMin = rectTransform.anchorMin;
                originalAnchorMax = rectTransform.anchorMax;
                originalPivot = rectTransform.pivot;
                originalAnchoredPosition = rectTransform.anchoredPosition;
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
            lastVisualStage = -1;
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.CardDrawn    -= OnCardDrawn;
                gameManager.CardDrawn    += OnCardDrawn;
                gameManager.CardClicked  -= TriggerClickBounce;
                gameManager.CardClicked  += TriggerClickBounce;
                gameManager.CardClickedAt -= PlayThunderCatLightning;
                gameManager.CardClickedAt += PlayThunderCatLightning;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.CardDrawn    -= OnCardDrawn;
                gameManager.CardClicked  -= TriggerClickBounce;
                gameManager.CardClickedAt -= PlayThunderCatLightning;
            }
            StopOORollingDisplay();
            ClearSpawnedBurgers();
        }

        private void PlayThunderCatLightning(Vector2 screenPosition)
        {
            if (gameManager == null || cardImage == null)
                return;

            string socketCardId = gameManager.GetSocketCardId(mySocket);
            if (!int.TryParse(socketCardId, out int cardNumber) || cardNumber != 271)
                return;

            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            Shader shader = Shader.Find("CosmicChaosCat/UIThunderCatLightning");
            if (shader == null) shader = Resources.Load<Shader>("Shaders/UIThunderCatLightning");
            if (shader == null) return;
            if (thunderLightningMaterial == null || thunderLightningMaterial.shader != shader)
                thunderLightningMaterial = new Material(shader) { name = "Thunder Cat Lightning (Runtime)" };

            // Thunder Cat's raised paws sit at these normalized points in its illustration.
            // The click position is intentionally ignored: every click discharges both paws.
            RectTransform cardRect = cardImage.rectTransform;
            Rect cardBounds = cardRect.rect;
            Vector2 leftPaw = GetCanvasPointFromCardNormalized(cardRect, canvasRect, cardBounds, 0.15f, 0.83f);
            Vector2 rightPaw = GetCanvasPointFromCardNormalized(cardRect, canvasRect, cardBounds, 0.85f, 0.80f);
            SpawnThunderCatLightning(canvasRect, leftPaw);
            SpawnThunderCatLightning(canvasRect, rightPaw);
        }

        private static Vector2 GetCanvasPointFromCardNormalized(
            RectTransform cardRect, RectTransform canvasRect, Rect bounds, float normalizedX, float normalizedY)
        {
            Vector3 cardLocal = new Vector3(
                Mathf.Lerp(bounds.xMin, bounds.xMax, normalizedX),
                Mathf.Lerp(bounds.yMin, bounds.yMax, normalizedY),
                0f);
            Vector3 world = cardRect.TransformPoint(cardLocal);
            return canvasRect.InverseTransformPoint(world);
        }

        private void SpawnThunderCatLightning(RectTransform canvasRect, Vector2 origin)
        {
            Image lightning = CreateEffectImage("ThunderCatLightning", canvasRect, null);
            lightning.material = new Material(thunderLightningMaterial);
            lightning.color = Color.white;
            lightning.raycastTarget = false;
            RectTransform rect = lightning.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = origin;
            float size = Mathf.Clamp(Mathf.Min(canvasRect.rect.width, canvasRect.rect.height) * 0.52f, 320f, 780f);
            rect.sizeDelta = new Vector2(size, size);
            rect.SetAsLastSibling();
            StartCoroutine(AnimateThunderCatLightning(lightning));
        }

        private System.Collections.IEnumerator AnimateThunderCatLightning(Image lightning)
        {
            const float duration = 0.42f;
            float elapsed = 0f;
            Material material = lightning != null ? lightning.material : null;
            if (material != null) material.SetFloat("_Seed", Random.Range(0f, 100f));
            while (lightning != null && elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (material != null) material.SetFloat("_Progress", t);
                lightning.color = new Color(1f, 1f, 1f, 1f - t * t);
                yield return null;
            }
            if (lightning != null)
            {
                if (lightning.material != null) Destroy(lightning.material);
                Destroy(lightning.gameObject);
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
                string pulseCardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
                float pulseBaseScale = GetCardBaseScale(pulseCardId);
                transform.localScale = originalScale * pulseBaseScale * s;
                if (pulseTimer >= pulseDuration)
                {
                    transform.localScale = originalScale * pulseBaseScale;
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
                string breathingCardId = gameManager != null ? gameManager.GetSocketCardId(mySocket) : null;
                float cardScale = GetCardBaseScale(breathingCardId);
                transform.localScale = originalScale * cardScale * s;
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
            UpdateOORollingAnimation();

            // Spade Deck rear cards share the same breathing/click motion as the front card.
            SyncSpadeDeckLayerMotion();

            if (spawnedBurgerStacks.Count > 0 && gameManager != null && gameManager.ComboCount == 0)
                ClearSpawnedBurgers(true);
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
                foreach (var g in graphics)
                {
                    g.enabled = shouldShow;
                    g.raycastTarget = false;
                }

                var renderers = GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers) r.enabled = shouldShow;

                var clicker = GetComponent<Clicker>();
                if (clicker != null) clicker.enabled = false;

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
            int selectedStage = gameManager.GetSocketSelectedStage(mySocket);
            bool hiddenReplacementActive = !string.IsNullOrEmpty(curCardId) &&
                                           gameManager.GetHiddenReplacementSprites(curCardId) != null;
            bool visualChanged = lastVisualCardId != curCardId ||
                                 lastVisualStage != selectedStage ||
                                 lastHiddenReplacementActive != hiddenReplacementActive;
            bool equippedCardChanged = lastEquippedId != curCardId;
            if (equippedCardChanged)
            {
                lastEquippedId = curCardId;
                lastSizedSprite = null;
                currentSpriteIndex = 0;
                ticklingSequenceStep = 0;
                autoIdleFrameIndex = 0;
                autoIdleTimer = 0f;
                isSchrodingerSpecialSequence = false;
                tailClickSequenceStep = 0;
                portalCurrentCatSprite = null;
                flyCatStageFiveSequenceIndex = 0;
                ResetPunchCatDisplay();

                // 이전 특수 카드가 남긴 0.5/0.75 배율을 일반 카드가 이어받지 않게 한다.
                if (mySocket == ClickSocketSlot.Center && clickBounceRoutine == null)
                {
                    transform.localScale = originalScale;
                    if (cardImage != null && cardImage.transform != transform)
                        cardImage.transform.localScale = originalScale;
                    defaultTargetScale = originalScale;
                }
            }

            if (visualChanged)
            {
                lastVisualCardId = curCardId;
                lastVisualStage = selectedStage;
                lastHiddenReplacementActive = hiddenReplacementActive;

                if (curCardId != null && AutoIdleCatAnimationHelper.IsAutoIdleCat(curCardId))
                {
                    isAutoIdleActive = true;
                    autoIdleSprites = AutoIdleCatAnimationHelper.GetAutoIdleSprites(curCardId, card);
                    isSchrodingerActive = int.TryParse(curCardId, out int autoCardNumber) && autoCardNumber == 201;
                }
                else
                {
                    isAutoIdleActive = false;
                    autoIdleSprites = null;
                    isSchrodingerActive = false;
                    isSchrodingerSpecialSequence = false;
                }

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
                        var sprites = GetActiveSpriteList(curCardId, card, selectedStage);
                        if (sprites != null && sprites.Count >= 2)
                        {
                            if (currentSpriteIndex >= sprites.Count) currentSpriteIndex = 0;
                            Sprite currentSp = sprites[currentSpriteIndex];
                            if (currentSp == null) currentSp = defaultCardSprite;
                            cardImage.sprite = currentSp;
                            FitSubClickSpriteToBounds(currentSp);
                            cardImage.color = Color.white;
                        }
                        else
                        {
                            Sprite sprite = card != null
                                ? card.GetSpriteForStage(selectedStage)
                                : defaultCardSprite;
                            if (sprite == null) sprite = defaultCardSprite;

                            cardImage.sprite = sprite;
                            FitSubClickSpriteToBounds(sprite);
                            if (sprite != null) cardImage.color = Color.white;
                        }
                    }
                }

                if (IsSpadeDeck(curCardId))
                {
                    SetSpadeDeckLayersVisible(true,
                        MemeCatToggleDisplayHelper.GetSpriteListForCard(curCardId, card, selectedStage));
                }
                else
                {
                    SetSpadeDeckLayersVisible(false);
                }

                // 카드/단계가 바뀔 때만 특수 레이아웃을 재구성한다.
                RefreshLongNeckDisplay(card != null && card.Id == "0171");

                RefreshMisfortuneDisplay(IsMisfortune(curCardId), card);
                RefreshHungryDisplay(IsHungry(curCardId), card);
                RefreshPortalCatDisplay(IsPortalCat(curCardId), card);
                RefreshPunchCatDisplay(IsPunchCat(curCardId), card);
                RefreshRainbowButton(IsRainbowButton(curCardId), card);
                RefreshOORollingDisplay(IsOORollingCat(curCardId), card);
                RefreshBlackEyesDisplay(IsBlackEyes(curCardId), card);
                RefreshBurgerMakerDisplay(IsBurgerMaker(curCardId), card);
            }

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
        /// 중앙 이미지는 최소 크기를 보정한 native size를 적용하고, 서브 슬롯 이미지는
        /// 씬에 지정된 영역 안에 종횡비를 유지하며 맞춘다(CSS contain과 동일).
        /// 예: 서브 슬롯의 200x500 이미지는 400x400 영역에서 160x400이 된다.
        /// </summary>
        private void FitSubClickSpriteToBounds(Sprite sprite)
        {
            if (cardImage == null || sprite == null || !hasOriginalRectSize)
                return;

            if (mySocket == ClickSocketSlot.Center)
            {
                ApplyCenterNativeSizeWithMinimum();
                return;
            }

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

        /// <summary>
        /// 중앙 카드는 native size를 사용하되, 원본의 가로/세로가 모두 씬의
        /// 기본 크기(현재 400x400)보다 작으면 종횡비를 유지하며 최소 크기까지 확대한다.
        /// </summary>
        private void ApplyCenterNativeSizeWithMinimum()
        {
            if (cardImage == null || cardImage.sprite == null || !hasOriginalRectSize) return;

            Sprite sprite = cardImage.sprite;
            if (lastSizedSprite == sprite) return;

            cardImage.SetNativeSize();

            RectTransform rect = cardImage.rectTransform;
            float nativeWidth = Mathf.Abs(rect.sizeDelta.x);
            float nativeHeight = Mathf.Abs(rect.sizeDelta.y);
            float minWidth = Mathf.Abs(originalSizeDelta.x);
            float minHeight = Mathf.Abs(originalSizeDelta.y);

            if (nativeWidth <= 0f || nativeHeight <= 0f || minWidth <= 0f || minHeight <= 0f)
                return;

            if (nativeWidth < minWidth && nativeHeight < minHeight)
            {
                float scale = Mathf.Max(minWidth / nativeWidth, minHeight / nativeHeight);
                rect.sizeDelta = new Vector2(nativeWidth * scale, nativeHeight * scale);
            }

            cardImage.preserveAspect = true;
            lastSizedSprite = sprite;
        }

        private List<Sprite> GetActiveSpriteList(string cardId, CardEntry entry, int stage)
        {
            List<Sprite> hiddenSprites = gameManager != null
                ? gameManager.GetHiddenReplacementSprites(cardId)
                : null;
            if (hiddenSprites != null && hiddenSprites.Count >= 2) return hiddenSprites;
            return MemeCatToggleDisplayHelper.GetSpriteListForCard(cardId, entry, stage);
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
                    cardImage.raycastTarget = mySocket == ClickSocketSlot.Center;
                }
                
                if (!isLongNeck)
                {
                    ClearSpawnedNecks();
                }
            }
        }

        private void RefreshOORollingDisplay(bool active, CardEntry card)
        {
            if (!active || cardImage == null || card?.CardSprite == null)
            {
                StopOORollingDisplay();
                return;
            }

            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            if (ooRollingImage == null)
            {
                RectTransform sourceRect = cardImage.rectTransform;
                Clicker clicker = GetComponent<Clicker>() ?? GetComponentInParent<Clicker>();
                ooRollingButtonRect = clicker != null ? clicker.transform as RectTransform : sourceRect;
                if (ooRollingButtonRect != null && !ooRollingButtonCaptured)
                {
                    ooRollingButtonOriginalPosition = ooRollingButtonRect.anchoredPosition;
                    ooRollingButtonOriginalRotation = ooRollingButtonRect.localRotation;
                    ooRollingButtonCaptured = true;
                }
                Vector2 sourceCenter = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(sourceRect.rect.center));
                Vector3 left = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector3(sourceRect.rect.xMin, sourceRect.rect.center.y)));
                Vector3 right = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector3(sourceRect.rect.xMax, sourceRect.rect.center.y)));
                Vector3 bottom = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector3(sourceRect.rect.center.x, sourceRect.rect.yMin)));
                Vector3 top = canvasRect.InverseTransformPoint(sourceRect.TransformPoint(new Vector3(sourceRect.rect.center.x, sourceRect.rect.yMax)));

                ooRollingImage = CreateEffectImage("OOCatRolling", canvasRect, card.CardSprite);
                RectTransform rollingRect = ooRollingImage.rectTransform;
                rollingRect.anchorMin = rollingRect.anchorMax = new Vector2(0.5f, 0.5f);
                rollingRect.pivot = new Vector2(0.5f, 0.5f);
                rollingRect.sizeDelta = new Vector2(
                    Mathf.Max(1f, Mathf.Abs(right.x - left.x)),
                    Mathf.Max(1f, Mathf.Abs(top.y - bottom.y)));
                ooRollingX = sourceCenter.x;
                ooRollingY = sourceCenter.y;
                rollingRect.anchoredPosition = new Vector2(ooRollingX, ooRollingY);
                rollingRect.SetAsLastSibling();
            }

            ooRollingImage.sprite = card.CardSprite;
            ooRollingImage.color = Color.white;
            ooRollingImage.raycastTarget = false;
            ooRollingActive = true;
            cardImage.enabled = true;
            cardImage.color = new Color(1f, 1f, 1f, 0f);
            cardImage.raycastTarget = mySocket == ClickSocketSlot.Center;
        }

        private void UpdateOORollingAnimation()
        {
            if (!ooRollingActive || ooRollingImage == null) return;
            Canvas canvas = ooRollingImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;

            RectTransform rollingRect = ooRollingImage.rectTransform;
            // UI gameplay can run while Time.timeScale is zero, so this autonomous
            // card must use unscaled time or it appears completely stationary.
            float dt = Time.unscaledDeltaTime;
            ooRollingX += 180f * dt;
            rollingRect.localEulerAngles += new Vector3(0f, 0f, -220f * dt);
            float halfWidth = rollingRect.rect.width * 0.5f;
            if (ooRollingX - halfWidth > canvasRect.rect.xMax)
                ooRollingX = canvasRect.rect.xMin - halfWidth;
            rollingRect.anchoredPosition = new Vector2(ooRollingX, ooRollingY);
            if (ooRollingButtonRect != null)
                ooRollingButtonRect.position = rollingRect.position;
        }

        private void StopOORollingDisplay()
        {
            ooRollingActive = false;
            if (ooRollingButtonCaptured && ooRollingButtonRect != null)
            {
                ooRollingButtonRect.anchoredPosition = ooRollingButtonOriginalPosition;
                ooRollingButtonRect.localRotation = ooRollingButtonOriginalRotation;
            }
            ooRollingButtonCaptured = false;
            ooRollingButtonRect = null;
            if (ooRollingImage != null) Destroy(ooRollingImage.gameObject);
            ooRollingImage = null;
        }

        private void RefreshMisfortuneDisplay(bool active, CardEntry card)
        {
            if (!active)
            {
                if (misfortuneContainer != null) misfortuneContainer.gameObject.SetActive(false);
                return;
            }

            EnsureMisfortuneSetup(card);
            if (misfortuneContainer == null) return;
            misfortuneContainer.gameObject.SetActive(true);
            if (cardImage != null)
            {
                cardImage.enabled = true;
                cardImage.color = new Color(1f, 1f, 1f, 0f);
                cardImage.raycastTarget = mySocket == ClickSocketSlot.Center;
            }
        }

        private void EnsureMisfortuneSetup(CardEntry card)
        {
            if (cardImage == null || card == null) return;

            var sprites = card.BreakthroughSprites;
            if (sprites == null || sprites.Length < 4) return;

            // Catalog order: rear ladder, cat frame 1, cat frame 2, front ladder.
            Sprite rear = sprites[0];
            misfortuneCatA = sprites[1];
            misfortuneCatB = sprites[2];
            Sprite front = sprites[3];
            if (rear == null || misfortuneCatA == null || misfortuneCatB == null || front == null) return;

            if (misfortuneContainer == null)
            {
                var containerObject = new GameObject("MisfortuneLayers", typeof(RectTransform));
                containerObject.transform.SetParent(cardImage.transform, false);
                misfortuneContainer = containerObject.GetComponent<RectTransform>();
                misfortuneContainer.anchorMin = Vector2.zero;
                misfortuneContainer.anchorMax = Vector2.one;
                misfortuneContainer.offsetMin = Vector2.zero;
                misfortuneContainer.offsetMax = Vector2.zero;

                misfortuneRearLadder = CreateMisfortuneLayer("MisfortuneRearLadder", misfortuneContainer);
                misfortuneCat = CreateMisfortuneLayer("MisfortuneCat", misfortuneContainer);
                misfortuneFrontLadder = CreateMisfortuneLayer("MisfortuneFrontLadder", misfortuneContainer);
                misfortuneCatX = 0f;
                misfortuneUsesSecondFrame = false;
            }

            SetMisfortuneSprite(misfortuneRearLadder, rear);
            SetMisfortuneSprite(misfortuneCat, misfortuneUsesSecondFrame ? misfortuneCatB : misfortuneCatA);
            SetMisfortuneSprite(misfortuneFrontLadder, front);

            // ladder_1 is the left/front half and ladder_2 is the right/rear half.
            // Place their inner crop edges directly next to each other to rebuild
            // the original ladder instead of stacking both halves at the center.
            float frontWidth = front.rect.width;
            float rearWidth = rear.rect.width;
            misfortuneFrontLadder.rectTransform.anchoredPosition = new Vector2(-rearWidth * 0.5f, 0f);
            misfortuneRearLadder.rectTransform.anchoredPosition = new Vector2(frontWidth * 0.5f, 0f);
            misfortuneCat.rectTransform.anchoredPosition = new Vector2(misfortuneCatX, -70f);
        }

        private static Image CreateMisfortuneLayer(string objectName, Transform parent)
        {
            var layerObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(parent, false);
            Image layer = layerObject.GetComponent<Image>();
            layer.preserveAspect = true;
            layer.raycastTarget = false;
            return layer;
        }

        private static void SetMisfortuneSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null) return;
            image.sprite = sprite;
            image.rectTransform.sizeDelta = sprite.rect.size;
            image.color = Color.white;
        }

        private void MoveMisfortuneCat()
        {
            if (misfortuneCat == null || misfortuneContainer == null) return;

            misfortuneUsesSecondFrame = !misfortuneUsesSecondFrame;
            Sprite nextFrame = misfortuneUsesSecondFrame ? misfortuneCatB : misfortuneCatA;
            SetMisfortuneSprite(misfortuneCat, nextFrame);
            misfortuneCatX -= 100f;

            float halfCatWidth = misfortuneCat.rectTransform.rect.width * 0.5f;
            GetMisfortuneHorizontalBounds(out float viewportLeft, out float viewportRight);
            if (misfortuneCatX + halfCatWidth < viewportLeft)
            {
                // Re-enter already straddling the right edge instead of teleporting fully on-screen.
                misfortuneCatX = viewportRight + halfCatWidth * 0.5f;
            }
            misfortuneCat.rectTransform.anchoredPosition = new Vector2(misfortuneCatX, -70f);
        }

        private void GetMisfortuneHorizontalBounds(out float left, out float right)
        {
            left = misfortuneContainer != null ? misfortuneContainer.rect.xMin : -Screen.width * 0.5f;
            right = misfortuneContainer != null ? misfortuneContainer.rect.xMax : Screen.width * 0.5f;
            if (misfortuneContainer == null) return;

            Canvas canvas = cardImage != null ? cardImage.canvas : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null || canvasRect.rect.width <= 0f) return;

            // Convert the real canvas edges into this card's local coordinates.
            // Sub-clicks are offset from screen center, so a symmetric width alone
            // would detect the wrong exit/re-entry points for their moving cats.
            Vector3 worldLeft = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMin, 0f, 0f));
            Vector3 worldRight = canvasRect.TransformPoint(new Vector3(canvasRect.rect.xMax, 0f, 0f));
            float localLeft = misfortuneContainer.InverseTransformPoint(worldLeft).x;
            float localRight = misfortuneContainer.InverseTransformPoint(worldRight).x;
            left = Mathf.Min(localLeft, localRight);
            right = Mathf.Max(localLeft, localRight);
        }

        private void RefreshHungryDisplay(bool active, CardEntry card)
        {
            if (cardImage == null || !(cardImage.transform is RectTransform rect)) return;

            if (active)
            {
                bool enteringHungry = !hungryWasActive;
                hungryWasActive = true;
                hungryBaseSprite = card != null ? card.CardSprite : null;
                hungryBiteSprite = card != null && card.BreakthroughSprites != null &&
                    card.BreakthroughSprites.Length > 0 ? card.BreakthroughSprites[0] : null;
                if (hungryBiteRoutine == null && hungryBaseSprite != null)
                    cardImage.sprite = hungryBaseSprite;

                if (enteringHungry && mySocket == ClickSocketSlot.Center)
                {
                    lastSizedSprite = null;
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = Vector2.zero;
                    if (cardImage.sprite != null) rect.sizeDelta = cardImage.sprite.rect.size;
                }
            }
            else
            {
                if (hungryWasActive && mySocket == ClickSocketSlot.Center)
                {
                    lastSizedSprite = null;
                    rect.anchorMin = originalAnchorMin;
                    rect.anchorMax = originalAnchorMax;
                    rect.pivot = originalPivot;
                    rect.anchoredPosition = originalAnchoredPosition;
                    if (hasOriginalRectSize) rect.sizeDelta = originalSizeDelta;
                }
                if (hungryWasActive)
                {
                    ClearHungryBiscuits();
                    hungryWasActive = false;
                }
            }
        }

        private void SpawnHungryBiscuit(CardEntry card)
        {
            if (cardImage == null || card == null) return;
            var states = gameManager != null ? gameManager.GetCardStates() : null;
            if (states == null || !states.TryGetValue(card.Id, out var progress)) return;

            hungryBiscuits.RemoveAll(go => go == null);
            // BreakthroughCount is zero-based internally: the unevolved card is
            // visual stage 1, so it must already allow two biscuits.
            int breakthroughStage = Mathf.Clamp(progress.BreakthroughCount + 1, 1, 5);
            int maxBiscuits = breakthroughStage * 2;
            if (card.BreakthroughSprites == null || card.BreakthroughSprites.Length < 2) return;
            Sprite biscuitSprite = card.BreakthroughSprites[1];
            if (biscuitSprite == null) return;

            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null) return;
            // Every click socket owns and limits its own biscuit collection.
            if (hungryBiscuits.Count >= maxBiscuits) return;

            Image biscuit = CreateEffectImage("HungryBiscat", canvasRect, biscuitSprite);
            RectTransform biscuitRect = biscuit.rectTransform;
            biscuitRect.anchorMin = biscuitRect.anchorMax = new Vector2(0.5f, 0.5f);
            biscuitRect.pivot = new Vector2(0.5f, 0.5f);

            RectTransform hungryRect = cardImage.rectTransform;
            float hungryScale = card.CardSprite != null && card.CardSprite.rect.width > 0f
                ? hungryRect.rect.width / card.CardSprite.rect.width
                : 1f;
            biscuitRect.sizeDelta = biscuitSprite.rect.size * Mathf.Max(0.05f, hungryScale);
            Vector3 mouthWorld = hungryRect.TransformPoint(new Vector3(
                hungryRect.rect.xMin + hungryRect.rect.width * 0.95f,
                hungryRect.rect.yMin + hungryRect.rect.height * 0.70f,
                0f));
            Vector2 mouthLocal = canvasRect.InverseTransformPoint(mouthWorld);
            float spawnY = canvasRect.rect.yMax + biscuitRect.rect.height * 0.35f;
            biscuitRect.anchoredPosition = new Vector2(mouthLocal.x, spawnY);
            hungryBiscuits.Add(biscuit.gameObject);
            StartCoroutine(DropHungryBiscuit(biscuitRect, mouthLocal.y));
        }

        private System.Collections.IEnumerator DropHungryBiscuit(RectTransform biscuit, float targetY)
        {
            const float fallSpeed = 950f;
            while (biscuit != null && biscuit.anchoredPosition.y > targetY)
            {
                Vector2 position = biscuit.anchoredPosition;
                position.y = Mathf.Max(targetY, position.y - fallSpeed * Time.unscaledDeltaTime);
                biscuit.anchoredPosition = position;
                biscuit.Rotate(0f, 0f, 240f * Time.unscaledDeltaTime);
                yield return null;
            }
            if (biscuit != null)
            {
                hungryBiscuits.Remove(biscuit.gameObject);
                Destroy(biscuit.gameObject);
                if (hungryBiteRoutine != null) StopCoroutine(hungryBiteRoutine);
                hungryBiteRoutine = StartCoroutine(PlayHungryBiteReaction());
            }
        }

        private System.Collections.IEnumerator PlayHungryBiteReaction()
        {
            if (cardImage != null && hungryBiteSprite != null)
                cardImage.sprite = hungryBiteSprite;
            yield return new WaitForSecondsRealtime(0.14f);
            if (cardImage != null && hungryBaseSprite != null)
                cardImage.sprite = hungryBaseSprite;
            hungryBiteRoutine = null;
        }

        private void ClearHungryBiscuits()
        {
            foreach (GameObject biscuit in hungryBiscuits)
                if (biscuit != null) Destroy(biscuit);
            hungryBiscuits.Clear();
            if (hungryBiteRoutine != null)
            {
                StopCoroutine(hungryBiteRoutine);
                hungryBiteRoutine = null;
            }
        }

        private void RefreshPortalCatDisplay(bool active, CardEntry card)
        {
            if (!active)
            {
                if (portalLeftImage != null) portalLeftImage.gameObject.SetActive(false);
                if (portalRightImage != null) portalRightImage.gameObject.SetActive(false);
                if (portalWasActive)
                {
                    lastSizedSprite = null;
                    RectTransform rect = cardImage != null ? cardImage.rectTransform : null;
                    if (rect != null)
                    {
                        rect.anchorMin = originalAnchorMin;
                        rect.anchorMax = originalAnchorMax;
                        rect.pivot = originalPivot;
                        rect.anchoredPosition = originalAnchoredPosition;
                        if (hasOriginalRectSize) rect.sizeDelta = originalSizeDelta;
                    }
                    if (clickBounceRoutine == null) transform.localScale = originalScale;
                    defaultTargetScale = originalScale;
                    portalWasActive = false;
                }
                return;
            }
            if (cardImage == null || card == null || card.BreakthroughSprites == null ||
                card.BreakthroughSprites.Length < 7) return;

            portalCatSprites = new List<Sprite>();
            for (int i = 0; i < 5; i++)
                if (card.BreakthroughSprites[i] != null) portalCatSprites.Add(card.BreakthroughSprites[i]);
            if (portalCatSprites.Count == 0) return;

            if (portalCurrentCatSprite == null || !portalCatSprites.Contains(portalCurrentCatSprite))
                portalCurrentCatSprite = portalCatSprites[0];
            cardImage.sprite = portalCurrentCatSprite;
            FitSubClickSpriteToBounds(portalCurrentCatSprite);

            if (portalLeftImage == null)
                portalLeftImage = CreateEffectImage("PortalCatLeft", cardImage.transform, card.BreakthroughSprites[5]);
            if (portalRightImage == null)
                portalRightImage = CreateEffectImage("PortalCatRight", cardImage.transform, card.BreakthroughSprites[6]);
            portalLeftImage.gameObject.SetActive(true);
            portalRightImage.gameObject.SetActive(true);
            bool enteringPortal = !portalWasActive;
            portalWasActive = true;
            Vector3 portalScale = originalScale * 0.75f;
            if (enteringPortal && clickBounceRoutine == null) transform.localScale = portalScale;
            defaultTargetScale = portalScale;
            SetPortalLayout(portalLeftImage, card.BreakthroughSprites[5], -1f);
            SetPortalLayout(portalRightImage, card.BreakthroughSprites[6], 1f);
        }

        private void SetPortalLayout(Image portal, Sprite sprite, float direction)
        {
            if (portal == null || sprite == null || cardImage == null) return;
            portal.sprite = sprite;
            RectTransform rect = portal.rectTransform;
            float catWidth = cardImage.rectTransform.rect.width;
            float scale = catWidth > 0f && cardImage.sprite != null && cardImage.sprite.rect.width > 0f
                ? catWidth / cardImage.sprite.rect.width : 1f;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sprite.rect.size * scale;
            rect.anchoredPosition = new Vector2(direction * (catWidth * 0.5f + rect.sizeDelta.x * 0.62f), 0f);
            portal.color = Color.white;
            portal.raycastTarget = false;
        }

        private void TeleportPortalCat(CardEntry card)
        {
            if (cardImage == null || card == null || portalCatSprites == null || portalCatSprites.Count == 0) return;
            var states = gameManager != null ? gameManager.GetCardStates() : null;
            int stage = 1;
            if (states != null && states.TryGetValue(card.Id, out var progress))
                stage = Mathf.Clamp(progress.BreakthroughCount + 1, 1, 5);
            if (gameManager != null)
                stage = Mathf.Max(stage, gameManager.GetSocketSelectedStage(mySocket));
            int candidateCount = Mathf.Min(stage, portalCatSprites.Count);
            int previousIndex = portalCurrentCatSprite != null
                ? portalCatSprites.IndexOf(portalCurrentCatSprite)
                : -1;
            int nextIndex = UnityEngine.Random.Range(0, candidateCount);
            if (candidateCount > 1 && nextIndex == previousIndex)
                nextIndex = (nextIndex + UnityEngine.Random.Range(1, candidateCount)) % candidateCount;
            currentSpriteIndex = nextIndex;
            portalCurrentCatSprite = portalCatSprites[nextIndex];
            cardImage.sprite = portalCurrentCatSprite;
            FitSubClickSpriteToBounds(portalCurrentCatSprite);
            RefreshPortalCatDisplay(true, card);

            RectTransform rect = cardImage.rectTransform;
            RectTransform parentRect = rect.parent as RectTransform;
            Canvas canvas = cardImage.canvas;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (parentRect == null || canvasRect == null) return;
            float halfWidth = Mathf.Max((rect.rect.width * 0.5f + 180f) * 0.75f, 40f);
            float halfHeight = Mathf.Max(rect.rect.height * 0.5f * 0.75f, 40f);
            Rect bounds = canvasRect.rect;
            float minX = bounds.xMin + halfWidth;
            float maxX = bounds.xMax - halfWidth;
            float minY = bounds.yMin + halfHeight;
            float maxY = bounds.yMax - halfHeight;
            if (minX <= maxX && minY <= maxY)
            {
                Vector2 canvasPosition = new Vector2(
                    UnityEngine.Random.Range(minX, maxX),
                    UnityEngine.Random.Range(minY, maxY));
                Vector3 worldPosition = canvasRect.TransformPoint(canvasPosition);
                Vector3 parentPosition = parentRect.InverseTransformPoint(worldPosition);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                Vector2 parentCenter = parentRect.rect.center;
                rect.anchoredPosition = new Vector2(
                    parentPosition.x - parentCenter.x,
                    parentPosition.y - parentCenter.y);
            }
        }

        private void RefreshPunchCatDisplay(bool active, CardEntry card)
        {
            if (!active || cardImage == null || card == null) return;
            if (card.BreakthroughSprites == null || card.BreakthroughSprites.Length < 5) return;
            if (punchCurrentCatSprite == null) punchCurrentCatSprite = card.BreakthroughSprites[0];
            cardImage.sprite = punchCurrentCatSprite;
            FitSubClickSpriteToBounds(punchCurrentCatSprite);
        }

        private void AdvancePunchCat(CardEntry card)
        {
            if (cardImage == null || card == null || card.BreakthroughSprites == null ||
                card.BreakthroughSprites.Length < 5 || punchDoorRoutine != null) return;

            if (punchSequenceCompleted)
            {
                SetPunchCatSprite(card.BreakthroughSprites[0]);
                punchSequenceCompleted = false;
                punchPhase = 0;
                return;
            }

            if (punchDoorImage == null)
            {
                EnsurePunchDoor(card.BreakthroughSprites[3]);
                SetPunchCatSprite(card.BreakthroughSprites[0]);
                punchPhase = 1;
                punchDoorRoutine = StartCoroutine(MovePunchDoor(entering: true));
                return;
            }

            if (punchPhase == 1)
            {
                SetPunchCatSprite(card.BreakthroughSprites[1]);
                punchPhase = 2;
                return;
            }

            int maxHoles = GetPunchMaxHoles(card.Id);
            if (punchHoleObjects.Count >= maxHoles)
            {
                SetPunchCatSprite(card.BreakthroughSprites[2]);
                punchDoorRoutine = StartCoroutine(MovePunchDoor(entering: false));
                return;
            }

            SetPunchCatSprite(card.BreakthroughSprites[2]);
            CreatePunchHole(card.BreakthroughSprites[4]);
            punchPhase = 1;
        }

        private int GetPunchMaxHoles(string cardId)
        {
            var states = gameManager != null ? gameManager.GetCardStates() : null;
            int stage = 1;
            if (states != null && states.TryGetValue(cardId, out var progress))
                stage = Mathf.Clamp(progress.BreakthroughCount + 1, 1, 5);
            return stage * 4;
        }

        private void SetPunchCatSprite(Sprite sprite)
        {
            if (sprite == null || cardImage == null) return;
            punchCurrentCatSprite = sprite;
            cardImage.sprite = sprite;
            FitSubClickSpriteToBounds(sprite);
        }

        private void EnsurePunchDoor(Sprite doorSprite)
        {
            punchDoorImage = CreateEffectImage("PunchCatDoor", cardImage.transform, doorSprite);
            RectTransform doorRect = punchDoorImage.rectTransform;
            doorRect.anchorMin = doorRect.anchorMax = new Vector2(0.5f, 0.5f);
            doorRect.pivot = new Vector2(0.5f, 0.5f);
            float targetHeight = Mathf.Max(cardImage.rectTransform.rect.height * 1.12f, 1f);
            float scale = targetHeight / doorSprite.rect.height;
            doorRect.sizeDelta = doorSprite.rect.size * scale;
            doorRect.anchoredPosition = new Vector2(0f, GetPunchDoorTravel());
            punchDoorImage.raycastTarget = false;
            Shader holeShader = Resources.Load<Shader>("Shaders/UIPunchDoorHoles");
            if (holeShader == null) holeShader = Shader.Find("CosmicChaosCat/UIPunchDoorHoles");
            if (holeShader != null)
            {
                punchDoorMaterial = new Material(holeShader);
                punchDoorImage.material = punchDoorMaterial;
                for (int i = 0; i < 20; i++) punchDoorMaterial.SetVector("_Hole" + i, Vector4.zero);
            }
        }

        private float GetPunchDoorTravel()
        {
            Canvas canvas = cardImage != null ? cardImage.canvas : null;
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvasRect == null || cardImage == null) return 1000f;
            float scaleY = Mathf.Max(Mathf.Abs(cardImage.rectTransform.lossyScale.y), 0.001f);
            return canvasRect.rect.height * Mathf.Abs(canvasRect.lossyScale.y) / scaleY;
        }

        private System.Collections.IEnumerator MovePunchDoor(bool entering)
        {
            if (punchDoorImage == null) { punchDoorRoutine = null; yield break; }
            if (!entering)
            {
                yield return ShatterPunchDoor();
                punchSequenceCompleted = true;
                punchPhase = 0;
                punchDoorRoutine = null;
                yield break;
            }
            RectTransform rect = punchDoorImage.rectTransform;
            Vector2 from = rect.anchoredPosition;
            Vector2 to = Vector2.zero;
            const float duration = 0.38f;
            float elapsed = 0f;
            while (elapsed < duration && rect != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = SmoothStep(elapsed / duration);
                rect.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }
            punchDoorRoutine = null;
        }

        private System.Collections.IEnumerator ShatterPunchDoor()
        {
            if (punchDoorImage == null || punchDoorImage.sprite == null) yield break;
            RectTransform doorRect = punchDoorImage.rectTransform;
            Sprite source = punchDoorImage.sprite;
            const int columns = 4;
            const int rows = 5;
            int count = columns * rows;
            var pieceObjects = new GameObject[count];
            var pieceRects = new RectTransform[count];
            var pieceImages = new Image[count];
            var pieceSprites = new Sprite[count];
            var velocities = new Vector2[count];
            var angularSpeeds = new float[count];

            float displayWidth = doorRect.rect.width / columns;
            float displayHeight = doorRect.rect.height / rows;
            float sourceWidth = source.rect.width / columns;
            float sourceHeight = source.rect.height / rows;
            Vector2 doorCenter = doorRect.anchoredPosition;

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = row * columns + column;
                    Rect sourceRect = new Rect(
                        source.rect.x + column * sourceWidth,
                        source.rect.y + row * sourceHeight,
                        sourceWidth,
                        sourceHeight);
                    Sprite pieceSprite = Sprite.Create(source.texture, sourceRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
                    pieceSprites[index] = pieceSprite;

                    Image piece = CreateEffectImage("PunchDoorShard", doorRect.parent, pieceSprite);
                    RectTransform pieceRect = piece.rectTransform;
                    pieceRect.anchorMin = pieceRect.anchorMax = doorRect.anchorMin;
                    pieceRect.pivot = new Vector2(0.5f, 0.5f);
                    pieceRect.sizeDelta = new Vector2(displayWidth + 1f, displayHeight + 1f);
                    Vector2 localOffset = new Vector2(
                        (column + 0.5f - columns * 0.5f) * displayWidth,
                        (row + 0.5f - rows * 0.5f) * displayHeight);
                    pieceRect.anchoredPosition = doorCenter + localOffset;
                    pieceRect.localScale = doorRect.localScale;
                    piece.transform.SetSiblingIndex(doorRect.GetSiblingIndex() + 1);

                    Vector2 outward = localOffset.sqrMagnitude > 0.01f ? localOffset.normalized : UnityEngine.Random.insideUnitCircle.normalized;
                    velocities[index] = outward * UnityEngine.Random.Range(180f, 390f) +
                        new Vector2(UnityEngine.Random.Range(-90f, 90f), UnityEngine.Random.Range(120f, 330f));
                    angularSpeeds[index] = UnityEngine.Random.Range(-520f, 520f);
                    pieceObjects[index] = piece.gameObject;
                    pieceRects[index] = pieceRect;
                    pieceImages[index] = piece;
                }
            }

            punchDoorImage.enabled = false;
            foreach (GameObject hole in punchHoleObjects)
                if (hole != null) hole.SetActive(false);

            const float duration = 0.72f;
            const float gravity = 1050f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                float fade = 1f - Mathf.Clamp01((elapsed - duration * 0.42f) / (duration * 0.58f));
                for (int i = 0; i < count; i++)
                {
                    if (pieceRects[i] == null) continue;
                    velocities[i] += Vector2.down * gravity * dt;
                    pieceRects[i].anchoredPosition += velocities[i] * dt;
                    pieceRects[i].Rotate(0f, 0f, angularSpeeds[i] * dt);
                    Color color = pieceImages[i].color;
                    color.a = fade;
                    pieceImages[i].color = color;
                }
                yield return null;
            }

            for (int i = 0; i < count; i++)
            {
                if (pieceObjects[i] != null) Destroy(pieceObjects[i]);
                if (pieceSprites[i] != null) Destroy(pieceSprites[i]);
            }
            foreach (GameObject hole in punchHoleObjects)
                if (hole != null) Destroy(hole);
            punchHoleObjects.Clear();
            if (punchDoorImage != null) Destroy(punchDoorImage.gameObject);
            punchDoorImage = null;
            if (punchDoorMaterial != null) Destroy(punchDoorMaterial);
            punchDoorMaterial = null;
        }

        private void CreatePunchHole(Sprite rimSprite)
        {
            if (punchDoorImage == null || rimSprite == null) return;
            RectTransform doorRect = punchDoorImage.rectTransform;
            float radius = Mathf.Max(34f, Mathf.Min(doorRect.rect.width, doorRect.rect.height) * 0.09f);
            float margin = radius * 1.4f;
            Vector2 position = new Vector2(
                UnityEngine.Random.Range(doorRect.rect.xMin + margin, doorRect.rect.xMax - margin),
                UnityEngine.Random.Range(doorRect.rect.yMin + margin, doorRect.rect.yMax - margin));

            Image hole = CreateEffectImage("PunchCatHole", doorRect, rimSprite);
            RectTransform holeRect = hole.rectTransform;
            holeRect.anchorMin = holeRect.anchorMax = new Vector2(0.5f, 0.5f);
            holeRect.pivot = new Vector2(0.5f, 0.5f);
            holeRect.sizeDelta = Vector2.one * radius * 2.65f;
            holeRect.anchoredPosition = position;
            // The source sprite is used only as shader mask data. Rendering it
            // here would cover the exact pixels that were cut from the door.
            hole.color = new Color(1f, 1f, 1f, 0f);
            hole.raycastTarget = false;
            if (punchDoorMaterial != null && punchHoleObjects.Count < 20)
            {
                float u = Mathf.InverseLerp(doorRect.rect.xMin, doorRect.rect.xMax, position.x);
                float v = Mathf.InverseLerp(doorRect.rect.yMin, doorRect.rect.yMax, position.y);
                float halfWidth = holeRect.sizeDelta.x * 0.5f / Mathf.Max(doorRect.rect.width, 1f);
                float halfHeight = holeRect.sizeDelta.y * 0.5f / Mathf.Max(doorRect.rect.height, 1f);
                punchDoorMaterial.SetTexture("_HoleTex", rimSprite.texture);
                punchDoorMaterial.SetVector("_Hole" + punchHoleObjects.Count,
                    new Vector4(u, v, halfWidth, halfHeight));
            }
            punchHoleObjects.Add(hole.gameObject);
        }

        private void ResetPunchCatDisplay()
        {
            if (punchDoorRoutine != null) StopCoroutine(punchDoorRoutine);
            punchDoorRoutine = null;
            foreach (GameObject hole in punchHoleObjects)
                if (hole != null) Destroy(hole);
            punchHoleObjects.Clear();
            if (punchDoorImage != null) Destroy(punchDoorImage.gameObject);
            punchDoorImage = null;
            if (punchDoorMaterial != null) Destroy(punchDoorMaterial);
            punchDoorMaterial = null;
            punchPhase = 0;
            punchSequenceCompleted = false;
            punchCurrentCatSprite = null;
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

            if (IsBlackEyes(socketCardId))
            {
                TryBlinkBlackEyes(gameManager.CardCatalog?.FindById(socketCardId));
                return;
            }

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
                    cardImage.raycastTarget = mySocket == ClickSocketSlot.Center;
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
