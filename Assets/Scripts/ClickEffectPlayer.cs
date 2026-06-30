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
        [SerializeField] private float clickScaleAmount = 1.15f;
        [SerializeField] private float clickScaleDuration = 0.12f;

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

        private Vector3 originalScale;
        private Coroutine scaleRoutine;

        private void Awake()
        {
            if (cardTransform != null)
                originalScale = cardTransform.localScale;
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
            if (cardTransform == null) return;
            if (scaleRoutine != null) StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScaleBounceRoutine());
        }

        private IEnumerator ScaleBounceRoutine()
        {
            float elapsed = 0f;
            while (elapsed < clickScaleDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = elapsed / clickScaleDuration;
                float scale = 1f + (clickScaleAmount - 1f) * Mathf.Sin(t * Mathf.PI);
                cardTransform.localScale = originalScale * scale;
                yield return null;
            }
            cardTransform.localScale = originalScale;
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
