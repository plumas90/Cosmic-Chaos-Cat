using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GachaService
    {
        // Soft pity: SSR/UR weight starts boosting after this many consecutive non-SSR pulls
        private const int SoftPityStart = 80;
        private const float SoftPityBoostPerRoll = 0.1f;

        public CardEntry DrawCard(
            IReadOnlyList<CardEntry> allCards,
            Dictionary<string, CardProgress> stateById,
            float completion01,
            int pityCounter,
            float nWeightReduction = 0f,
            float rWeightReduction = 0f)
        {
            var candidates = new List<CardEntry>(allCards.Count);
            var weights = new List<float>(allCards.Count);
            float totalWeight = 0f;

            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                if (card == null || card.IsHidden) continue;
                if (!IsRarityUnlocked(card.Rarity, completion01)) continue;
                if (!stateById.TryGetValue(card.Id, out var progress)) continue;

                float weight = card.BaseWeight;

                // Rarity-specific weight reductions from upgrades
                if (card.Rarity == CardRarity.N) weight *= Mathf.Max(0.01f, 1f - nWeightReduction);
                if (card.Rarity == CardRarity.R) weight *= Mathf.Max(0.01f, 1f - rWeightReduction);

                // 5중첩 초과 카드는 등장 가중치 감소 (이미 조각 교환 가능하므로)
                if (progress.Copies >= card.MaxStacks)
                    weight *= 0.3f;

                // Soft pity: boost SSR/UR after many dry pulls
                if (pityCounter >= SoftPityStart && (card.Rarity == CardRarity.SSR || card.Rarity == CardRarity.UR))
                {
                    float boost = 1f + (pityCounter - SoftPityStart) * SoftPityBoostPerRoll;
                    weight *= boost;
                }

                if (weight <= 0f) continue;

                candidates.Add(card);
                weights.Add(weight);
                totalWeight += weight;
            }

            if (totalWeight <= 0f || candidates.Count == 0) return null;

            float pick = Random.value * totalWeight;
            float cursor = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cursor += weights[i];
                if (pick <= cursor) return candidates[i];
            }

            return candidates[candidates.Count - 1];
        }

        private static bool IsRarityUnlocked(CardRarity rarity, float completion01)
        {
            switch (rarity)
            {
                case CardRarity.N:
                case CardRarity.R:   return true;
                case CardRarity.SR:  return completion01 >= 0.2f;
                case CardRarity.SSR: return completion01 >= 0.5f;
                case CardRarity.UR:  return completion01 >= 0.8f;
                default:             return false;
            }
        }
    }
}
