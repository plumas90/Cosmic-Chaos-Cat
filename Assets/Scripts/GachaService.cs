using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GachaService
    {
        public CardEntry DrawCard(
            IReadOnlyList<CardEntry> allCards,
            Dictionary<string, CardProgress> stateById,
            float completion01,
            float nWeightReduction = 0f,
            float rWeightReduction = 0f,
            GachaType type = GachaType.Normal)
        {
            var candidates = new List<CardEntry>(allCards.Count);
            var weights = new List<float>(allCards.Count);
            float totalWeight = 0f;

            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                if (card == null || card.IsHidden) continue;
                if (!IsRarityUnlocked(card.Rarity, completion01, type)) continue;
                if (!stateById.TryGetValue(card.Id, out var progress)) continue;

                float weight = card.BaseWeight;

                // Rarity-specific weight reductions from upgrades
                if (card.Rarity == CardRarity.N) weight *= Mathf.Max(0.01f, 1f - nWeightReduction);
                if (card.Rarity == CardRarity.R) weight *= Mathf.Max(0.01f, 1f - rWeightReduction);

                // 가챠 타입별 가중치 조절
                if (type == GachaType.Rare)
                {
                    if (card.Rarity == CardRarity.N) weight *= 0.5f;
                    if (card.Rarity == CardRarity.SR) weight *= 2.0f;
                }
                else if (type == GachaType.Super)
                {
                    if (card.Rarity == CardRarity.N) weight *= 0.1f;
                    if (card.Rarity == CardRarity.R) weight *= 0.5f;
                    if (card.Rarity == CardRarity.SR) weight *= 2.0f;
                    if (card.Rarity == CardRarity.SSR) weight *= 3.0f;
                    if (card.Rarity == CardRarity.UR) weight *= 1.5f;
                }

                // 5중첩 초과 카드는 등장 가중치 감소 (이미 조각 교환 가능하므로)
                if (progress.Copies >= card.MaxStacks)
                    weight *= 0.3f;


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

        private static bool IsRarityUnlocked(CardRarity rarity, float completion01, GachaType type)
        {
            float bonus = type == GachaType.Super ? 0.3f : (type == GachaType.Rare ? 0.1f : 0f);
            float comp = completion01 + bonus;

            switch (rarity)
            {
                case CardRarity.N:
                case CardRarity.R:   return true;
                case CardRarity.SR:  return comp >= 0.2f;
                case CardRarity.SSR: return comp >= 0.5f;
                case CardRarity.UR:  return comp >= 0.8f;
                default:             return false;
            }
        }
    }
}
