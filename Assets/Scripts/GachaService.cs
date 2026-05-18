using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GachaService
    {
        public CardDefinition DrawCard(List<CardDefinition> cards, Dictionary<string, CardProgress> stateById, float completion01)
        {
            var candidates = new List<CardDefinition>();
            var weights = new List<float>();
            var totalWeight = 0f;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (!IsRarityUnlocked(card.Rarity, completion01))
                {
                    continue;
                }

                if (!stateById.TryGetValue(card.Id, out var progress))
                {
                    continue;
                }

                var weight = card.BaseWeight;
                if (!progress.Unlocked)
                {
                    weight *= 1.5f;
                }
                else if (progress.Copies >= card.MaxStacks)
                {
                    weight *= 0.3f;
                }

                if (weight <= 0f)
                {
                    continue;
                }

                candidates.Add(card);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0f || candidates.Count == 0)
            {
                return null;
            }

            var pick = Random.value * totalWeight;
            var cursor = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                cursor += weights[i];
                if (pick <= cursor)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static bool IsRarityUnlocked(CardRarity rarity, float completion01)
        {
            switch (rarity)
            {
                case CardRarity.N:
                case CardRarity.R:
                    return true;
                case CardRarity.SR:
                    return completion01 >= 0.2f;
                case CardRarity.SSR:
                    return completion01 >= 0.5f;
                case CardRarity.UR:
                    return completion01 >= 0.8f;
                default:
                    return false;
            }
        }
    }
}
