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
            if (gameManager == null || bgImage == null) return;

            string bgId = gameManager.EquippedBackgroundId;
            if (string.IsNullOrEmpty(bgId)) return;

            if (CollectionPanel.Instance != null)
            {
                var sprite = CollectionPanel.Instance.GetBackgroundSprite(bgId);
                if (sprite != null)
                {
                    bgImage.sprite = sprite;
                    bgImage.color = Color.white;
                }
            }
        }
    }
}
