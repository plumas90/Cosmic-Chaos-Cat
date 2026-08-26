using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the background Image component in the scene.
    /// Automatically updates the background sprite based on the equipped background.
    /// </summary>
    public sealed class MainBackgroundController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image bgImage;
        private string lastAppliedBackgroundId;
        private bool hasAppliedBackground;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (bgImage == null) bgImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += Refresh;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager == null || bgImage == null) return;

            string bgId = gameManager.EquippedBackgroundId;
            if (hasAppliedBackground && lastAppliedBackgroundId == bgId) return;
            hasAppliedBackground = true;
            lastAppliedBackgroundId = bgId;

            if (string.IsNullOrEmpty(bgId) || bgId == "bg-none")
            {
                bgImage.sprite = null;
                bgImage.color = Color.white; // Solid white background
                return;
            }

            var entry = gameManager.BackgroundCatalog != null
                ? gameManager.BackgroundCatalog.FindById(bgId)
                : null;
            var sprite = entry != null ? entry.BackgroundSprite : null;

            if (sprite == null && CollectionPanel.Instance != null)
            {
                sprite = CollectionPanel.Instance.GetBackgroundSprite(bgId);
            }

            bgImage.sprite = sprite;
            bgImage.color = Color.white;
        }
    }
}
