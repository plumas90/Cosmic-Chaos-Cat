using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "CardCatalog", menuName = "CosmicChaosCat/Card Catalog")]
    public sealed class CardCatalogSO : ScriptableObject
    {
        [SerializeField] private List<CardEntry> cards = new List<CardEntry>();

        public IReadOnlyList<CardEntry> Cards => cards;
        public List<CardEntry> CardsList => cards;

        public CardEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || string.IsNullOrEmpty(cards[i].Id)) continue;
                if (cards[i].Id == id) return cards[i];
                if (int.TryParse(cards[i].Id, out int a) && int.TryParse(id, out int b) && a == b) return cards[i];
            }
            return null;
        }
    }
}
