using System.Collections;
using UnityEngine;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the card image object in the scene.
    /// Handles visual feedback for clicks and gacha draws.
    /// All particle systems must be placed in the scene and assigned in Inspector.
    /// </summary>
    public sealed class ClickEffectPlayer : MonoBehaviour
    {
        [Header("Card Transform (for scale bounce)")]
        [SerializeField] private Transform cardTransform;
        [SerializeField] private float shrinkScaleAmount = 0.85f;
        [SerializeField] private float overshootScaleAmount = 1.15f;
        [SerializeField] private float clickScaleDuration = 0.15f;

        public Transform CardTransform
        {
            get => cardTransform;
            set
            {
                cardTransform = value;
                if (cardTransform != null && cardTransform.localScale.sqrMagnitude >= 0.01f)
                    originalScale = cardTransform.localScale;
            }
        }

        [Header("Particles — place in scene, assign here")]
        [SerializeField] private ParticleSystem normalClickParticle;
        [SerializeField] private ParticleSystem criticalParticle;
        [SerializeField] private ParticleSystem srParticle;
        [SerializeField] private ParticleSystem ssrParticle;
        [SerializeField] private ParticleSystem urParticle;

        [Header("SR Flip Effect")]
        [SerializeField] private Animator srFlipAnimator;
        [SerializeField] private string srFlipTriggerName = "Flip";

        [Header("SSR Lightning Overlay — place in scene, start inactive")]
        [SerializeField] private GameObject ssrLightningOverlay;

        [Header("UR Fake Loading Panel — place in scene, start inactive")]
        [SerializeField] private GameObject urFakeLoadingPanel;

        private Vector3 originalScale = Vector3.one;
        private Coroutine scaleRoutine;

        private void Awake()
        {
            ResolveCardTransform();
        }

        private void OnEnable()
        {
            ResolveCardTransform();
            var gm = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.CardClicked -= PlayNormalClick;
                gm.CardClicked += PlayNormalClick;
            }
        }

        private void OnDisable()
        {
            var gm = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                gm.CardClicked -= PlayNormalClick;
            }
        }

        private Transform activeTargetTransform;

        private Transform GetTargetTransform()
        {
            Transform t = cardTransform;

            // 1. If explicitly set in Inspector and has visible renderer/image, use it!
            if (t == null)
            {
                var disp = FindObjectOfType<CardImageDisplay>(true);
                if (disp != null) t = disp.transform;
            }

            if (t == null)
            {
                var clicker = FindObjectOfType<Clicker>(true);
                if (clicker != null) t = clicker.transform;
            }

            if (t == null) t = transform;

            // 2. If t points to an invisible overlay button (alpha == 0), automatically redirect to the visible cat image!
            if (t != null)
            {
                var img = t.GetComponent<UnityEngine.UI.Image>();
                if (img != null && img.color.a < 0.05f)
                {
                    var disp = t.transform.parent != null ? t.transform.parent.GetComponentInChildren<CardImageDisplay>(true) : null;
                    if (disp != null) return disp.transform;

                    var visibleImgs = t.transform.parent != null ? t.transform.parent.GetComponentsInChildren<UnityEngine.UI.Image>(true) : null;
                    if (visibleImgs != null)
                    {
                        foreach (var vi in visibleImgs)
                        {
                            if (vi != null && vi != img && vi.enabled && vi.color.a > 0.05f)
                                return vi.transform;
                        }
                    }
                }
            }

            return t;
        }

        private void ResolveCardTransform()
        {
            Transform target = GetTargetTransform();
            if (target != null)
            {
                if (activeTargetTransform != target || originalScale.sqrMagnitude < 0.01f)
                {
                    activeTargetTransform = target;
                    originalScale = target.localScale.sqrMagnitude >= 0.01f ? target.localScale : Vector3.one;
                }
            }
        }

        // Called on every normal click (N/R) via Clicker
        public void PlayNormalClick()
        {
            BounceScale();
            if (normalClickParticle != null) normalClickParticle.Play();
        }

        // Called when GameManager fires CriticalHit event
        public void PlayCriticalEffect()
        {
            if (criticalParticle != null) criticalParticle.Play();
        }

        // Called when a gacha card is drawn (rarity determines effect)
        public void PlayGachaEffect(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N:
                case CardRarity.R:
                    BounceScale();
                    if (normalClickParticle != null) normalClickParticle.Play();
                    break;
                case CardRarity.SR:
                    StartCoroutine(SREffectRoutine());
                    break;
                case CardRarity.SSR:
                    StartCoroutine(SSREffectRoutine());
                    break;
                case CardRarity.UR:
                    StartCoroutine(UREffectRoutine());
                    break;
            }
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void BounceScale()
        {
            Transform target = GetTargetTransform();
            if (target == null) return;

            if (target != activeTargetTransform || originalScale.sqrMagnitude < 0.01f)
            {
                activeTargetTransform = target;
                if (target.localScale.sqrMagnitude >= 0.01f)
                    originalScale = target.localScale;
                else
                    originalScale = Vector3.one;
            }

            // Always reset scale back to originalScale before starting a new click bounce
            if (activeTargetTransform != null)
                activeTargetTransform.localScale = originalScale;

            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleBounceRoutine(target));
        }

        private IEnumerator ScaleBounceRoutine(Transform target)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            while (elapsed < clickScaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / clickScaleDuration);

                float scaleMultiplier;
                if (t < 0.35f)
                {
                    // 0% ~ 35%: 1.0 -> shrinkScaleAmount (0.85f) [확실히 작아짐]
                    float subT = t / 0.35f;
                    scaleMultiplier = Mathf.Lerp(1.0f, shrinkScaleAmount, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else if (t < 0.70f)
                {
                    // 35% ~ 70%: shrinkScaleAmount (0.85f) -> overshootScaleAmount (1.15f) [확실히 커짐]
                    float subT = (t - 0.35f) / 0.35f;
                    scaleMultiplier = Mathf.Lerp(shrinkScaleAmount, overshootScaleAmount, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }
                else
                {
                    // 70% ~ 100%: overshootScaleAmount (1.15f) -> 1.0f [원래대로 복구]
                    float subT = (t - 0.70f) / 0.30f;
                    scaleMultiplier = Mathf.Lerp(overshootScaleAmount, 1.0f, Mathf.Sin(subT * Mathf.PI * 0.5f));
                }

                target.localScale = originalScale * scaleMultiplier;
                yield return null;
            }
            target.localScale = originalScale;
            scaleRoutine = null;
        }

        private IEnumerator SREffectRoutine()
        {
            if (srFlipAnimator != null) srFlipAnimator.SetTrigger(srFlipTriggerName);
            if (srParticle     != null) srParticle.Play();
            yield return null;
        }

        private IEnumerator SSREffectRoutine()
        {
            if (ssrLightningOverlay != null) ssrLightningOverlay.SetActive(true);
            if (ssrParticle         != null) ssrParticle.Play();
            yield return new WaitForSecondsRealtime(1.8f);
            if (ssrLightningOverlay != null) ssrLightningOverlay.SetActive(false);
        }

        private IEnumerator UREffectRoutine()
        {
            if (urFakeLoadingPanel != null) urFakeLoadingPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(2.5f);
            if (urFakeLoadingPanel != null) urFakeLoadingPanel.SetActive(false);
            if (urParticle         != null) urParticle.Play();
        }
    }
}
