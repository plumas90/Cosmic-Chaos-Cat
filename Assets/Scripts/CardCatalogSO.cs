using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "CardCatalog", menuName = "CosmicChaosCat/Card Catalog")]
    public sealed class CardCatalogSO : ScriptableObject
    {
        [SerializeField] private List<CardEntry> cards = new List<CardEntry>();

        public IReadOnlyList<CardEntry> Cards => cards;

        public CardEntry FindById(string id)
        {
            for (int i = 0; i < cards.Count; i++)
                if (cards[i] != null && cards[i].Id == id) return cards[i];
            return null;
        }
    }
}
