using UnityEngine;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to a decoration object that is pre-placed in the scene (but starts hidden).
    /// Automatically shows when the linked set is completed.
    ///
    /// The parent object stays active so the script keeps running.
    /// The [visuals] child is what actually appears/disappears.
    /// </summary>
    public sealed class DecorationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private string      setId;          // Must match a SetEntry.SetId in SetCatalog
        [SerializeField] private GameObject  visuals;        // Child object to show/hide

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged += Refresh;
                gameManager.SetCompleted += OnSetCompleted;
            }
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
                gameManager.SetCompleted -= OnSetCompleted;
            }
        }

        private void OnSetCompleted(string completedSetId)
        {
            if (completedSetId == setId) Refresh();
        }

        private void Refresh()
        {
            if (visuals == null || gameManager == null) return;
            bool show = gameManager.IsSetCompleted(setId);
            if (visuals.activeSelf != show)
                visuals.SetActive(show);
        }
    }
}
