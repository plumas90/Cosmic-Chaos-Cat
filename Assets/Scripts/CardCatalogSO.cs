using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "CardCatalog", menuName = "CosmicChaosCat/Card Catalog")]
    public sealed class CardCatalogSO : ScriptableObject
    {
        [SerializeField] private List<CardEntry> cards = new List<CardEntry>();

        public IReadOnlyList<CardEntry> Cards
        {
            get
            {
                var allowed = new List<CardEntry>();
                if (cards != null)
                {
                    foreach (var c in cards)
                    {
                        if (c != null && SetCatalogSO.IsSetAllowed(c.SetId)) allowed.Add(c);
                    }
                }
                return allowed;
            }
        }

        public List<CardEntry> CardsList
        {
            get
            {
                var allowed = new List<CardEntry>();
                if (cards != null)
                {
                    foreach (var c in cards)
                    {
                        if (c != null && SetCatalogSO.IsSetAllowed(c.SetId)) allowed.Add(c);
                    }
                }
                return allowed;
            }
        }

        public CardEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || string.IsNullOrEmpty(cards[i].Id)) continue;
                if (!SetCatalogSO.IsSetAllowed(cards[i].SetId)) continue;
                if (cards[i].Id == id) return cards[i];
                if (int.TryParse(cards[i].Id, out int a) && int.TryParse(id, out int b) && a == b) return cards[i];
            }
            return null;
        }
    }
}
