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
            EnsureParentedToRootCanvas();
            EnsureUIBuilt();
            gameObject.SetActive(false);
        }

        public void EnsureParentedToRootCanvas()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
                if (transform.parent != canvas.transform) transform.SetParent(canvas.transform, false);
            }
            else if (transform.parent != null && !transform.parent.gameObject.activeInHierarchy)
            {
                transform.SetParent(null, false);
            }
            transform.SetAsLastSibling();
        }

        public static BreakthroughCutsceneUI GetOrCreate()
        {
            if (Instance != null)
            {
                Instance.EnsureParentedToRootCanvas();
                return Instance;
            }

            var existing = FindObjectOfType<BreakthroughCutsceneUI>(true);
            if (existing != null)
            {
                Instance = existing;
                Instance.EnsureParentedToRootCanvas();
                return Instance;
            }

            var canvas = FindObjectOfType<Canvas>();
            var root = new GameObject("BreakthroughCutsceneUI");
            if (canvas != null) root.transform.SetParent(canvas.transform, false);

            Instance = root.AddComponent<BreakthroughCutsceneUI>();
            Instance.EnsureParentedToRootCanvas();
            return Instance;
        }

        public void EnsureUIBuilt()
        {
            if (canvasGroup != null && bgOverlay != null && cardImage != null)
            {
                ApplyGameFont();
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

        // ── UI Native Star Burst Effect ──────────────────────────────────────
        private static Sprite cachedStarSprite;
        private RectTransform starContainer;

        private static Sprite GetOrCreateStarSprite()
        {
            if (cachedStarSprite != null) return cachedStarSprite;

            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] cols = new Color[size * size];
            Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float maxR = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 pos = new Vector2(x, y) - center;
                    float dist = pos.magnitude;
                    float angle = Mathf.Atan2(pos.y, pos.x);

                    // 4-point sharp diamond star formula
                    float starFactor = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2f)), 5f);
                    float currentMaxR = maxR * (0.20f + 0.80f * starFactor);

                    if (dist > currentMaxR)
                    {
                        cols[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float normDist = dist / currentMaxR;
                        float alpha = Mathf.Clamp01(1f - normDist);
                        alpha = Mathf.Pow(alpha, 1.2f); // Sharp glowing core

                        Color c = Color.Lerp(Color.white, new Color(1f, 0.92f, 0.4f, 1f), normDist * 0.5f);
                        c.a = alpha;
                        cols[y * size + x] = c;
                    }
                }
            }

            tex.SetPixels(cols);
            tex.Apply();
            cachedStarSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return cachedStarSprite;
        }

        private void EnsureStarContainerBuilt()
        {
            if (starContainer != null) return;

            var scGO = new GameObject("UIStarContainer", typeof(RectTransform));
            scGO.transform.SetParent(cardContainer, false);
            scGO.transform.SetSiblingIndex(0); // Card 이미지 바로 뒤쪽에 배치

            starContainer = scGO.GetComponent<RectTransform>();
            starContainer.anchorMin = new Vector2(0.5f, 0.5f);
            starContainer.anchorMax = new Vector2(0.5f, 0.5f);
            starContainer.sizeDelta = Vector2.zero;
            starContainer.anchoredPosition = Vector2.zero;
        }

        private void SpawnUIStarBurst(int count, float minSpeed, float maxSpeed, float minSize = 40f, float maxSize = 90f)
        {
            EnsureStarContainerBuilt();
            Sprite starSprite = GetOrCreateStarSprite();

            Color[] starColors = new Color[]
            {
                new Color(1f, 0.92f, 0.3f, 1f), // Bright Gold
                new Color(1f, 1f, 1f, 1f),      // Diamond White
                new Color(1f, 0.75f, 0.2f, 1f), // Deep Amber
                new Color(0.6f, 0.95f, 1f, 1f)  // Cyan Sparkle
            };

            for (int i = 0; i < count; i++)
            {
                var starGO = new GameObject("StarParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starGO.transform.SetParent(starContainer, false);

                var img = starGO.GetComponent<Image>();
                img.sprite = starSprite;
                img.raycastTarget = false;
                img.color = starColors[UnityEngine.Random.Range(0, starColors.Length)];

                var sRT = starGO.GetComponent<RectTransform>();
                float sz = UnityEngine.Random.Range(minSize, maxSize);
                sRT.sizeDelta = new Vector2(sz, sz);
                sRT.anchoredPosition = Vector2.zero;

                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float speed = UnityEngine.Random.Range(minSpeed, maxSpeed);
                float rotSpeed = UnityEngine.Random.Range(-360f, 360f);
                float lifetime = UnityEngine.Random.Range(0.7f, 1.3f);

                StartCoroutine(AnimateUIStar(starGO, sRT, img, dir, speed, rotSpeed, lifetime));
            }
        }

        private IEnumerator AnimateUIStar(GameObject starGO, RectTransform rt, Image img, Vector2 dir, float speed, float rotSpeed, float duration)
        {
            float elapsed = 0f;
            Vector2 pos = Vector2.zero;
            Color baseColor = img.color;

            while (elapsed < duration)
            {
                if (starGO == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                float currentSpeed = speed * (1f - Mathf.Pow(t, 0.7f) * 0.6f);
                pos += dir * currentSpeed * Time.unscaledDeltaTime;
                rt.anchoredPosition = pos;

                rt.Rotate(0, 0, rotSpeed * Time.unscaledDeltaTime);

                float scale = Mathf.Sin(t * Mathf.PI) * 1.2f;
                rt.localScale = new Vector3(scale, scale, 1f);

                float alpha = Mathf.Clamp01(1f - Mathf.Pow(t, 2f));
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                yield return null;
            }

            if (starGO != null) Destroy(starGO);
        }

        private void SpawnUIStarImplosion(int count, float duration = 2.0f, float minSize = 50f, float maxSize = 120f)
        {
            EnsureStarContainerBuilt();
            Sprite starSprite = GetOrCreateStarSprite();

            Color[] starColors = new Color[]
            {
                new Color(1f, 0.95f, 0.4f, 1f), // Glowing Gold
                new Color(1f, 0.5f, 0.85f, 1f), // SSR Magenta Sparkle
                new Color(0.4f, 0.9f, 1f, 1f),  // Celestial Cyan
                new Color(1f, 1f, 1f, 1f)       // Pure Starlight
            };

            for (int i = 0; i < count; i++)
            {
                var starGO = new GameObject("StarImplosionParticle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                starGO.transform.SetParent(starContainer, false);

                var img = starGO.GetComponent<Image>();
                img.sprite = starSprite;
                img.raycastTarget = false;
                img.color = starColors[UnityEngine.Random.Range(0, starColors.Length)];

                var sRT = starGO.GetComponent<RectTransform>();
                float sz = UnityEngine.Random.Range(minSize, maxSize);
                sRT.sizeDelta = new Vector2(sz, sz);

                // Spawn position outside screen bounds (radius 700 to 1300)
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float startRadius = UnityEngine.Random.Range(700f, 1300f);
                Vector2 startPos = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * startRadius;
                sRT.anchoredPosition = startPos;

                float rotSpeed = UnityEngine.Random.Range(-500f, 500f);
                float delay = UnityEngine.Random.Range(0f, duration * 0.5f);
                float flyDuration = UnityEngine.Random.Range(0.6f, 1.2f);

                StartCoroutine(AnimateUIStarImplosion(starGO, sRT, img, startPos, rotSpeed, delay, flyDuration));
            }
        }

        private IEnumerator AnimateUIStarImplosion(GameObject starGO, RectTransform sRT, Image img, Vector2 startPos, float rotSpeed, float delay, float flyDuration)
        {
            if (delay > 0f)
            {
                if (img != null) img.color = Color.clear;
                yield return new WaitForSecondsRealtime(delay);
            }

            if (starGO == null || sRT == null || img == null) yield break;

            Color baseColor = img.color == Color.clear ? Color.white : img.color;
            float elapsed = 0f;

            while (elapsed < flyDuration)
            {
                if (starGO == null || sRT == null) yield break;

                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / flyDuration);
                float easeT = t * t * t; // Accelerate poured in towards center

                sRT.anchoredPosition = Vector2.Lerp(startPos, Vector2.zero, easeT);
                sRT.Rotate(0f, 0f, rotSpeed * Time.unscaledDeltaTime);

                float scale = (1f - t * 0.6f);
                sRT.localScale = new Vector3(scale, scale, 1f);

                float alpha = t < 0.2f ? (t / 0.2f) : (1f - (t - 0.2f) / 0.8f);
                if (img != null) img.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                yield return null;
            }

            if (starGO != null) Destroy(starGO);
        }

        private IEnumerator DoScreenShake(RectTransform target, float duration, float magnitude)
        {
            if (target == null) yield break;
            Vector2 initialPos = target.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float currentMag = magnitude * (1f - (elapsed / duration));
                Vector2 randomOffset = new Vector2(
                    UnityEngine.Random.Range(-currentMag, currentMag),
                    UnityEngine.Random.Range(-currentMag, currentMag)
                );
                target.anchoredPosition = initialPos + randomOffset;
                yield return null;
            }

            if (target != null) target.anchoredPosition = initialPos;
        }

        public void PlayCutscene(Sprite oldSprite, Sprite newSprite, string cardName, int newStage, Action onComplete, bool enableStarParticles = true, bool isSSR = false)
        {
            EnsureParentedToRootCanvas();
            gameObject.SetActive(true);

            EnsureUIBuilt();
            if (enableStarParticles) EnsureStarContainerBuilt();
            onCutsceneFinished = onComplete;

            if (!gameObject.activeInHierarchy)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    canvas.gameObject.SetActive(true);
                    transform.SetParent(canvas.transform, false);
                }
                else
                {
                    transform.SetParent(null, false);
                }
                gameObject.SetActive(true);
            }

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(RunCutsceneAnimation(oldSprite, newSprite, cardName, newStage, enableStarParticles, isSSR));
        }

        private IEnumerator RunCutsceneAnimation(Sprite oldSprite, Sprite newSprite, string cardName, int newStage, bool enableStarParticles, bool isSSR)
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
            float phase1Duration = isSSR ? 0.7f : 0.5f;

            if (enableStarParticles) SpawnUIStarBurst(12, 200f, 400f, 35f, 65f);
            if (isSSR) SpawnUIStarImplosion(36, 2.5f, 40f, 90f);

            while (elapsed < phase1Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Duration);

                cardContainer.localScale = Vector3.Lerp(Vector3.one * 0.7f, Vector3.one * 1.0f, EaseOutBack(t));
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, t * 0.3f);
                yield return null;
            }

            // ── Phase 2: Rapid Alternating Silhouettes (Old <-> New) ─────────
            int flickers = isSSR ? 14 : 8;
            float flickerInterval = isSSR ? 0.14f : 0.12f;

            for (int i = 0; i < flickers; i++)
            {
                bool isNew = (i % 2 == 1);
                cardImage.sprite = isNew ? newSprite : (oldSprite != null ? oldSprite : newSprite);

                // White flash burst on each swap
                SetShaderProperties(0f, 0.8f, Color.black);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0.7f);
                if (enableStarParticles) SpawnUIStarBurst(5, 250f, 500f, 30f, 60f);

                if (isSSR && i % 2 == 0) StartCoroutine(DoScreenShake(cardContainer, 0.12f, 15f));
                if (isSSR && i % 3 == 0) SpawnUIStarImplosion(12, 1.5f, 35f, 80f);

                yield return new WaitForSecondsRealtime(flickerInterval * 0.4f);

                SetShaderProperties(0f, 0.1f, Color.black);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 1f, 1f, 0.3f);

                yield return new WaitForSecondsRealtime(flickerInterval * 0.6f);
            }

            // ── Phase 3: New Silhouette Fixed & Charging Aura ──────────────
            cardImage.sprite = newSprite;
            SetShaderProperties(0f, 1f, Color.black); // Intense white flash
            if (enableStarParticles) SpawnUIStarBurst(16, 300f, 600f, 40f, 75f);
            if (isSSR) SpawnUIStarImplosion(60, 2.0f, 60f, 110f);

            yield return new WaitForSecondsRealtime(0.15f);

            SetShaderProperties(0f, 0f, Color.black);
            cardContainer.localScale = Vector3.one * 1.15f;

            elapsed = 0f;
            float phase3Duration = isSSR ? 0.9f : 0.4f;

            if (isSSR) StartCoroutine(DoScreenShake(cardContainer, phase3Duration, 18f));

            while (elapsed < phase3Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / phase3Duration);

                cardContainer.localScale = Vector3.Lerp(Vector3.one * 1.15f, Vector3.one * 1.0f, t);
                if (auraGlowImage != null) auraGlowImage.color = new Color(1f, 0.9f, 0.5f, 0.4f + t * 0.4f);
                yield return null;
            }

            // ── Phase 4: Silhouette Reveals Full Color Illustration + UI STAR BURST (if enabled) ──
            if (enableStarParticles) SpawnUIStarBurst(isSSR ? 56 : 36, 450f, isSSR ? 1100f : 900f, 50f, isSSR ? 130f : 100f);
            if (isSSR)
            {
                SpawnUIStarImplosion(48, 1.5f, 50f, 100f);
                StartCoroutine(DoScreenShake(cardContainer, 1.2f, 26f));
            }

            elapsed = 0f;
            float phase4Duration = isSSR ? 1.4f : 0.9f;

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
            yield return new WaitForSecondsRealtime(isSSR ? 0.8f : 0.6f);

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
