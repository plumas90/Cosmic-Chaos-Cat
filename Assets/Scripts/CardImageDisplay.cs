using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        private float    pulseTimer;
        private bool     isPulsing;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (cardImage == null) cardImage = GetComponent<Image>();
            originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
                gameManager.StateChanged += Refresh;
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
                gameManager.StateChanged -= Refresh;
                gameManager.CardDrawn    -= OnCardDrawn;
                gameManager.CardClicked  -= TriggerClickBounce;
            }
        }

        private void Update()
        {
            if (!isPulsing) return;
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

        private void Refresh()
        {
            if (gameManager == null) return;
            var card   = gameManager.GetEquippedCard();
            var states = gameManager.GetCardStates();

            // 스프라이트 및 색상
            if (cardImage != null)
            {
                cardImage.preserveAspect = true;
                Sprite sprite = (card != null)
                    ? gameManager.GetCardSpriteForDisplay(card.Id)
                    : defaultCardSprite;
                if (sprite == null) sprite = defaultCardSprite;
                
                bool spriteChanged = cardImage.sprite != sprite;
                cardImage.sprite = sprite;
                if (sprite != null)
                {
                    cardImage.color = Color.white;
                    if (spriteChanged)
                    {
                        cardImage.SetNativeSize();
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
        
        private RectTransform longNeckContainer;
        private Image         longNeckHeadImage;
        private Image         longNeckBodyImage;
        private Image         longNeckNeckImage;
        private Coroutine     longNeckRoutine;

        private void EnsureLongNeckSetup()
        {
            if (longNeckContainer != null) return;

            GameObject containerGO = new GameObject("LongNeckContainer", typeof(RectTransform));
            containerGO.transform.SetParent(transform, false);
            longNeckContainer = containerGO.GetComponent<RectTransform>();
            longNeckContainer.anchorMin = new Vector2(0.5f, 0.5f);
            longNeckContainer.anchorMax = new Vector2(0.5f, 0.5f);
            longNeckContainer.sizeDelta = new Vector2(300, 300);

            // Body
            GameObject bodyGO = new GameObject("Body", typeof(Image));
            bodyGO.transform.SetParent(longNeckContainer, false);
            longNeckBodyImage = bodyGO.GetComponent<Image>();
            RectTransform bodyRt = bodyGO.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = new Vector2(0, -60);
            bodyRt.sizeDelta = new Vector2(240, 150);

            #if UNITY_EDITOR
            if (longNeckBodySprite == null || longNeckHeadSprite == null || longNeckNeckSprite == null)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/171_SR_long_neck_cat.png");
                foreach (var a in assets)
                {
                    if (a is Sprite s)
                    {
                        if (s.name.Contains("head")) longNeckHeadSprite = s;
                        else if (s.name.Contains("body")) longNeckBodySprite = s;
                        else if (s.name.Contains("neck")) longNeckNeckSprite = s;
                    }
                }
            }
            #endif

            if (longNeckBodySprite != null) longNeckBodyImage.sprite = longNeckBodySprite;

            // Neck
            GameObject neckGO = new GameObject("Neck", typeof(Image));
            neckGO.transform.SetParent(longNeckContainer, false);
            longNeckNeckImage = neckGO.GetComponent<Image>();
            RectTransform neckRt = neckGO.GetComponent<RectTransform>();
            neckRt.anchorMin = new Vector2(0.5f, 0.5f);
            neckRt.anchorMax = new Vector2(0.5f, 0.5f);
            neckRt.pivot = new Vector2(0.5f, 0.0f); // Grow upward
            neckRt.anchoredPosition = new Vector2(0, 0);
            neckRt.sizeDelta = new Vector2(240, 0);
            if (longNeckNeckSprite != null) longNeckNeckImage.sprite = longNeckNeckSprite;

            // Head
            GameObject headGO = new GameObject("Head", typeof(Image));
            headGO.transform.SetParent(longNeckContainer, false);
            longNeckHeadImage = headGO.GetComponent<Image>();
            RectTransform headRt = headGO.GetComponent<RectTransform>();
            headRt.anchorMin = new Vector2(0.5f, 0.5f);
            headRt.anchorMax = new Vector2(0.5f, 0.5f);
            headRt.anchoredPosition = new Vector2(0, 20);
            headRt.sizeDelta = new Vector2(240, 160);
            if (longNeckHeadSprite != null) longNeckHeadImage.sprite = longNeckHeadSprite;

            longNeckContainer.gameObject.SetActive(false);
        }

        private void RefreshLongNeckDisplay(bool isLongNeck)
        {
            EnsureLongNeckSetup();
            if (longNeckContainer != null)
            {
                longNeckContainer.gameObject.SetActive(isLongNeck);
                if (cardImage != null) cardImage.enabled = !isLongNeck;
                
                if (isLongNeck)
                {
                    // Reset head & neck position
                    RectTransform headRt = longNeckHeadImage.GetComponent<RectTransform>();
                    RectTransform neckRt = longNeckNeckImage.GetComponent<RectTransform>();
                    headRt.anchoredPosition = new Vector2(0, 20);
                    neckRt.sizeDelta = new Vector2(240, 0);
                    longNeckNeckImage.enabled = false;
                }
            }
        }

        private System.Collections.IEnumerator LongNeckStretchRoutine()
        {
            EnsureLongNeckSetup();
            if (longNeckContainer == null || longNeckHeadImage == null || longNeckNeckImage == null) yield break;

            longNeckNeckImage.enabled = true;
            RectTransform headRt = longNeckHeadImage.GetComponent<RectTransform>();
            RectTransform neckRt = longNeckNeckImage.GetComponent<RectTransform>();

            float duration = 0.28f;
            float maxStretch = 130f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float currentStretch;
                if (t < 0.40f)
                {
                    float subT = t / 0.40f;
                    currentStretch = Mathf.Lerp(0f, maxStretch, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else
                {
                    float subT = (t - 0.40f) / 0.60f;
                    currentStretch = Mathf.Lerp(maxStretch, 0f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                neckRt.sizeDelta = new Vector2(240, currentStretch);
                headRt.anchoredPosition = new Vector2(0, 20f + currentStretch);
                yield return null;
            }

            neckRt.sizeDelta = new Vector2(240, 0f);
            headRt.anchoredPosition = new Vector2(0, 20f);
            longNeckNeckImage.enabled = false;
            longNeckRoutine = null;
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
            Transform targetTf = cardImage != null ? cardImage.transform : transform;
            EnsureBaseScale(targetTf);

            // Check if 171 Long Neck Cat is equipped
            var equipped = gameManager != null ? gameManager.GetEquippedCard() : null;
            if (equipped != null && equipped.Id == "0171")
            {
                EnsureLongNeckSetup();
                if (longNeckRoutine != null) StopCoroutine(longNeckRoutine);
                longNeckRoutine = StartCoroutine(LongNeckStretchRoutine());
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
