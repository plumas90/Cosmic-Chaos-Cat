using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class HiddenSpriteReplacement
    {
        public string CardId;
        public Sprite[] Sprites;
    }

    [Serializable]
    public sealed class HiddenEvolutionEntry
    {
        public string Id;
        public string DisplayName;
        public string Description;
        public string[] RequiredCardIds;
        public Sprite PopupSprite;
        public HiddenSpriteReplacement[] Replacements;

        public List<Sprite> GetSprites(string cardId)
        {
            var result = new List<Sprite>();
            if (Replacements == null) return result;
            foreach (var replacement in Replacements)
            {
                if (replacement == null || !SameCardId(replacement.CardId, cardId) || replacement.Sprites == null) continue;
                foreach (Sprite sprite in replacement.Sprites)
                    if (sprite != null) result.Add(sprite);
                break;
            }
            return result;
        }

        private static bool SameCardId(string a, string b)
        {
            if (a == b) return true;
            return int.TryParse(a, out int ai) && int.TryParse(b, out int bi) && ai == bi;
        }
    }

    [CreateAssetMenu(fileName = "HiddenEvolutionCatalog", menuName = "CosmicChaosCat/Hidden Evolution Catalog")]
    public sealed class HiddenEvolutionCatalogSO : ScriptableObject
    {
        [SerializeField] private List<HiddenEvolutionEntry> entries = new List<HiddenEvolutionEntry>();
        public IReadOnlyList<HiddenEvolutionEntry> Entries => entries;

        public HiddenEvolutionEntry FindById(string id)
        {
            return entries.Find(entry => entry != null && entry.Id == id);
        }
    }
}
