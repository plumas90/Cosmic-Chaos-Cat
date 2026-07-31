using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the MainDecoration Image component in the scene.
    /// Automatically updates the decoration sprite based on the equipped decoration.
    /// </summary>
    public sealed class MainDecorationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image decoImage;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (decoImage == null) decoImage = GetComponent<Image>();
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
            if (decoImage == null) return;

            if (gameManager == null)
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
                return;
            }

            string decoId = gameManager.EquippedDecorationId;
            if (string.IsNullOrEmpty(decoId) || decoId == "deco-none" || decoId == "deco-00" || decoId.Equals("deco-none", System.StringComparison.OrdinalIgnoreCase))
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
                return;
            }

            if (CollectionPanel.Instance != null)
            {
                var sprite = CollectionPanel.Instance.GetDecorationSprite(decoId);
                if (sprite != null)
                {
                    decoImage.sprite = sprite;
                    decoImage.enabled = true;
                    decoImage.color = Color.white;
                    decoImage.preserveAspect = true;
                }
                else
                {
                    decoImage.sprite = null;
                    decoImage.enabled = false;
                }
            }
            else
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
            }
        }
    }
}
