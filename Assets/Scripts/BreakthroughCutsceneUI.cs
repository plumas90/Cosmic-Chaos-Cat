using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 한계돌파 연출 (Cutscene UI)
    /// 연출 흐름:
    ///   1. 화면 가운데 카드 배치 & 검은색 실루엣 전환
    ///   2. 기존 이미지(실루엣)와 한계돌파 이미지(실루엣)가 흰색 쉐이더/플래시 효과와 함께 번갈아 교체
    ///   3. 다음 한계돌파 이미지 실루엣으로 고정
    ///   4. 검은색 실루엣이 원본 컬러 이미지로 서서히 밝아지며 해금 연출 완료
    ///   5. 연출 동안은 하단 [스킵] 버튼 외 모든 화면 상호작용 차단
    /// </summary>
    public sealed class BreakthroughCutsceneUI : MonoBehaviour
    {
        public static BreakthroughCutsceneUI Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private CanvasGroup     canvasGroup;
        [SerializeField] private Image           bgOverlay;
        [SerializeField] private RectTransform   cardContainer;
        [SerializeField] private Image           cardImage;
        [SerializeField] private Image           auraGlowImage;
        [SerializeField] private TMP_Text        titleText;
        [SerializeField] private TMP_Text        subtitleText;
        [SerializeField] private Button         skipButton;

        private Material    silhouetteMaterial;
        private Coroutine   animationCoroutine;
        private bool        isCutscenePlaying = false;
        private Action      onCutsceneFinished;

        // Cached shader property IDs
        private static readonly int PropColorProgress = Shader.PropertyToID("_ColorProgress");
        private static readonly int PropFlash         = Shader.PropertyToID("_Flash");
        private static readonly int PropSilhouetteColor = Shader.PropertyToID("_SilhouetteColor");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureUIBuilt();
            gameObject.SetActive(false);
        }

        public static BreakthroughCutsceneUI GetOrCreate()
        {
            if (Instance != null) return Instance;

            var existing = FindObjectOfType<BreakthroughCutsceneUI>(true);
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }

            var canvas = FindObjectOfType<Canvas>();
            var root = new GameObject("BreakthroughCutsceneUI");
            if (canvas != null) root.transform.SetParent(canvas.transform, false);

            Instance = root.AddComponent<BreakthroughCutsceneUI>();
            return Instance;
        }

        public void EnsureUIBuilt()
        {
            if (canvasGroup != null && bgOverlay != null && cardImage != null)
            {
                ApplyGameFont();
                gameObject.SetActive(false);
                return;
            }

            // 1. Root RectTransform & CanvasGroup
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 2. Dark Background Overlay
            if (bgOverlay == null)
            {
                var bgGO = new GameObject("BGOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGO.transform.SetParent(transform, false);
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = Vector2.zero;
                bgRT.anchorMax = Vector2.one;
                bgRT.offsetMin = Vector2.zero;
                bgRT.offsetMax = Vector2.zero;

                bgOverlay = bgGO.GetComponent<Image>();
                bgOverlay.color = new Color(0f, 0f, 0f, 0.88f);
                bgOverlay.raycastTarget = true; // Blocks clicks to behind UI
            }

            // 3. Card Container & Aura Glow
            if (cardContainer == null)
            {
                var contGO = new GameObject("CardContainer", typeof(RectTransform));
                contGO.transform.SetParent(transform, false);
                cardContainer = contGO.GetComponent<RectTransform>();
                cardContainer.anchorMin = new Vector2(0.5f, 0.55f);
                cardContainer.anchorMax = new Vector2(0.5f, 0.55f);
                cardContainer.sizeDelta = new Vector2(320f, 420f);
                cardContainer.anchoredPosition = Vector2.zero;
            }

            if (auraGlowImage == null)
            {
                var auraGO = new GameObject("AuraGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                auraGO.transform.SetParent(cardContainer, false);
                var auraRT = auraGO.GetComponent<RectTransform>();
                auraRT.anchorMin = Vector2.zero;
                auraRT.anchorMax = Vector2.one;
                auraRT.sizeDelta = new Vector2(100f, 100f);

                auraGlowImage = auraGO.GetComponent<Image>();
                auraGlowImage.color = new Color(1f, 1f, 1f, 0f);
                auraGlowImage.raycastTarget = false;
            }

            if (cardImage == null)
            {
                var imgGO = new GameObject("CardImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                imgGO.transform.SetParent(cardContainer, false);
                var imgRT = imgGO.GetComponent<RectTransform>();
                imgRT.anchorMin = Vector2.zero;
                imgRT.anchorMax = Vector2.one;
                imgRT.sizeDelta = Vector2.zero;

                cardImage = imgGO.GetComponent<Image>();
                cardImage.raycastTarget = false;
            }

            // 4. Shader Material
            var shader = Shader.Find("UI/BreakthroughSilhouette");
            if (shader != null)
            {
                silhouetteMaterial = new Material(shader);
                cardImage.material = silhouetteMaterial;
            }

            // 5. Title & Subtitle Texts
            if (titleText == null)
            {
                var txtGO = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(transform, false);
                var txtRT = txtGO.GetComponent<RectTransform>();
                txtRT.anchorMin = new Vector2(0.5f, 0.82f);
                txtRT.anchorMax = new Vector2(0.5f, 0.82f);
                txtRT.sizeDelta = new Vector2(600f, 80f);
                txtRT.anchoredPosition = Vector2.zero;

                titleText = txtGO.GetComponent<TextMeshProUGUI>();
                titleText.text = "⭐ 한계 돌파! ⭐";
                titleText.fontSize = 42;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = new Color(1f, 0.9f, 0.4f);
                titleText.fontStyle = FontStyles.Bold;
                titleText.raycastTarget = false;
            }

            if (subtitleText == null)
            {
                var subGO = new GameObject("SubtitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                subGO.transform.SetParent(transform, false);
                var subRT = subGO.GetComponent<RectTransform>();
                subRT.anchorMin = new Vector2(0.5f, 0.25f);
                subRT.anchorMax = new Vector2(0.5f, 0.25f);
                subRT.sizeDelta = new Vector2(600f, 60f);
                subRT.anchoredPosition = Vector2.zero;

                subtitleText = subGO.GetComponent<TextMeshProUGUI>();
                subtitleText.text = "새로운 모습이 해금되었습니다!";
                subtitleText.fontSize = 26;
                subtitleText.alignment = TextAlignmentOptions.Center;
                subtitleText.color = Color.white;
                subtitleText.raycastTarget = false;
            }

            // 6. Skip Button
            if (skipButton == null)
            {
                var btnGO = new GameObject("SkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(transform, false);
                var btnRT = btnGO.GetComponent<RectTransform>();
                btnRT.anchorMin = new Vector2(0.5f, 0.12f);
                btnRT.anchorMax = new Vector2(0.5f, 0.12f);
                btnRT.sizeDelta = new Vector2(160f, 50f);
                btnRT.anchoredPosition = Vector2.zero;

                var btnImg = btnGO.GetComponent<Image>();
                btnImg.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);

                skipButton = btnGO.GetComponent<Button>();
                skipButton.onClick.AddListener(OnSkipClicked);

                var btnTxtGO = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                btnTxtGO.transform.SetParent(btnGO.transform, false);
                var btnTxtRT = btnTxtGO.GetComponent<RectTransform>();
                btnTxtRT.anchorMin = Vector2.zero;
                btnTxtRT.anchorMax = Vector2.one;
                btnTxtRT.sizeDelta = Vector2.zero;

                var btnTxt = btnTxtGO.GetComponent<TextMeshProUGUI>();
                btnTxt.text = "스킵 >>";
                btnTxt.fontSize = 22;
                btnTxt.alignment = TextAlignmentOptions.Center;
                btnTxt.color = Color.white;
                btnTxt.fontStyle = FontStyles.Bold;
            }

            ApplyGameFont();
            gameObject.SetActive(false);
        }

        private void ApplyGameFont()
        {
            TMP_FontAsset gameFont = null;
            var fontCandidates = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var t in fontCandidates)
            {
                if (t != null && t.font != null && t.font.name != "LiberationSans SDF")
                {
                    gameFont = t.font;
                    break;
                }
            }
            if (gameFont == null && fontCandidates.Length > 0)
                gameFont = fontCandidates[0].font;

            if (gameFont != null)
            {
                if (titleText != null) titleText.font = gameFont;
                if (subtitleText != null) subtitleText.font = gameFont;
                if (skipButton != null)
                {
                    var lbl = skipButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (lbl != null) lbl.font = gameFont;
                }
            }
        }

        public void PlayCutscene(Sprite oldSprite, Sprite newSprite, string cardName, int newStage, Action onComplete)
        {
            EnsureUIBuilt();
            onCutsceneFinished = onComplete;

            gameObject.SetActive(true);

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(RunCutsceneAnimation(oldSprite, newSprite, cardName, newStage));
        }

        private IEnumerator RunCutsceneAnimation(Sprite oldSprite, Sprite newSprite, string cardName, int newStage)
        {
            isCutscenePlaying = true;

            // Enable canvas group blocking
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (subtitleText != null)
                subtitleText.text = $"{cardName} [{newStage}단계 해금]";

            // Shader Material properties init
            SetShaderProperties(0f, 0f, Color.black);
            if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0f);

            // ── Phase 1: Screen Darkens & Old Card enters as Silhouette ─────
            cardImage.sprite = oldSprite != null ? oldSprite : newSprite;
            cardContainer.localScale = Vector3.one * 0.7f;
            float elapsed = 0f;
            float phase1Duration = 0.5f;

            while (elapsed < phase1Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Duration);

                cardContainer.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one * 1.0f, EaseOutBack(t));
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, t * 0.3f);
                yield return null;
            }

            // ── Phase 2: Rapid Alternating Silhouettes (Old <-> New) ─────────
            int flickers = 8;
            float flickerInterval = 0.12f;

            for (int i = 0; i < flickers; i++)
            {
                bool isNew = (i % 2 == 1);
                cardImage.sprite = isNew ? newSprite : (oldSprite != null ? oldSprite : newSprite);

                // White flash burst on each swap
                SetShaderProperties(0f, 0.8f, Color.black);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0.7f);

                yield return new WaitForSecondsRealtime(flickerInterval * 0.4f);

                SetShaderProperties(0f, 0.1f, Color.black);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0.3f);

                yield return new WaitForSecondsRealtime(flickerInterval * 0.6f);
            }

            // ── Phase 3: New Silhouette Fixed & Charging Aura ──────────────
            cardImage.sprite = newSprite;
            SetShaderProperties(0f, 1f, Color.black); // Intense white flash
            yield return new WaitForSecondsRealtime(0.15f);

            SetShaderProperties(0f, 0f, Color.black);
            cardContainer.localScale = Vector3.one * 1.15f;

            elapsed = 0f;
            float phase3Duration = 0.4f;
            while (elapsed < phase3Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / phase3Duration);

                cardContainer.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 1.0f, t);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 0.9f, 0.5f, 0.4f + t * 0.4f);
                yield return null;
            }

            // ── Phase 4: Silhouette Reveals Full Color Illustration! ────────
            elapsed = 0f;
            float phase4Duration = 0.9f;

            while (elapsed < phase4Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / phase4Duration);

                float colorProgress = Mathf.SmoothStep(0f, 1f, t);
                float flash = (1f - t) * 0.3f;
                SetShaderProperties(colorProgress, flash, Color.black);

                if (auraGlowImage != null)
                {
                    auraGlowImage.color = new Color(1f, 0.95f, 0.6f, (1f - t) * 0.8f);
                }

                yield return null;
            }

            SetShaderProperties(1f, 0f, Color.black);

            // ── Phase 5: Hold brief moment & Finish ─────────────────────────
            yield return new WaitForSecondsRealtime(0.6f);

            CompleteAndClose();
        }

        private void OnSkipClicked()
        {
            if (!isCutscenePlaying) return;

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            CompleteAndClose();
        }

        private void CompleteAndClose()
        {
            isCutscenePlaying = false;
            SetShaderProperties(1f, 0f, Color.black);
            if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0f);

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);

            var cb = onCutsceneFinished;
            onCutsceneFinished = null;
            cb?.Invoke();
        }

        private void SetShaderProperties(float colorProgress, float flash, Color silhouetteColor)
        {
            if (cardImage != null && cardImage.material != null)
            {
                cardImage.material.SetFloat(PropColorProgress, colorProgress);
                cardImage.material.SetFloat(PropFlash, flash);
                cardImage.material.SetColor(PropSilhouetteColor, silhouetteColor);
            }
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
