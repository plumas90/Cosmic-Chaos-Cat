using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// Encyclopedia (도감) panel.
    /// Card slots are created once and pooled — all data population happens at refresh,
    /// not at game start. The CardSlotUI prefab should be assigned in Inspector.
    /// </summary>
    public sealed class EncyclopediaPanel : MonoBehaviour
    {
        [SerializeField] private GameManager  gameManager;
        [SerializeField] private ScrollRect   scrollRect;
        [SerializeField] private Transform    gridContent;
        [SerializeField] private CardSlotUI   cardSlotPrefab;   // Prefab reference
        [SerializeField] private Button       closeButton;
        [SerializeField] private TMP_Text     progressText;
        [SerializeField] private TMP_InputField searchField;

        private readonly List<CardSlotUI> slots = new List<CardSlotUI>();
        private bool poolBuilt;

        private void OnEnable()
        {
            if (!poolBuilt) BuildSlotPool();
            Refresh();

            if (closeButton != null)
                closeButton.onClick.AddListener(OnClose);
            if (gameManager != null)
                gameManager.StateChanged += Refresh;
            if (searchField != null)
                searchField.onValueChanged.AddListener(_ => Refresh());
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(OnClose);
            if (gameManager != null)
                gameManager.StateChanged -= Refresh;
            if (searchField != null)
                searchField.onValueChanged.RemoveAllListeners();
        }

        private void BuildSlotPool()
        {
            poolBuilt = true;
            if (gameManager == null || cardCatalog == null || cardSlotPrefab == null || gridContent == null) return;

            int count = cardCatalog.Cards.Count;
            for (int i = 0; i < count; i++)
            {
                var slot = Instantiate(cardSlotPrefab, gridContent);
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            if (!poolBuilt || gameManager == null) return;

            var catalog = cardCatalog;
            if (catalog == null) return;

            var states = gameManager.GetCardStates();
            string filter = searchField != null ? searchField.text.ToLower() : string.Empty;
            bool hasFilter = !string.IsNullOrEmpty(filter);

            for (int i = 0; i < slots.Count && i < catalog.Cards.Count; i++)
            {
                var card    = catalog.Cards[i];
                var progress = states.TryGetValue(card.Id, out var p) ? p : null;

                bool show = !hasFilter || card.DisplayName.ToLower().Contains(filter)
                                       || card.Rarity.ToString().ToLower().Contains(filter);
                slots[i].gameObject.SetActive(show);

                if (show) slots[i].SetData(card, progress, gameManager);
            }

            if (progressText != null)
                progressText.text = $"도감  {gameManager.UnlockedCount} / {gameManager.TotalCardCount}" +
                                    $"  ({gameManager.Completion01 * 100f:0.0}%)";
        }

        private void OnClose() => gameObject.SetActive(false);

        private CardCatalogSO cardCatalog => gameManager?.CardCatalog;
    }
}
