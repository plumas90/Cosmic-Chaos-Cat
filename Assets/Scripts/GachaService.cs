using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GachaService
    {
        public CardEntry DrawCard(
            IReadOnlyList<CardEntry> allCards,
            Dictionary<string, CardProgress> stateById,
            float nWeightReduction,
            float rWeightReduction,
            bool rUnlocked,
            bool srUnlocked,
            bool ssrUnlocked,
            bool urUnlocked,
            GachaType type = GachaType.Normal)
        {
            // ── Step 1: Draw Rarity Tier based on gacha parameters & rate upgrades ──
            CardRarity chosenRarity = ChooseRarityTier(type, nWeightReduction, rWeightReduction);

            // ── Step 2: Fallback checks for locked tiers ──────────────────────────
            // If the rolled rarity tier isn't unlocked in the shop, fall back to N tier
            if (!IsRarityUnlocked(chosenRarity, rUnlocked, srUnlocked, ssrUnlocked, urUnlocked))
            {
                chosenRarity = CardRarity.N;
            }

            // ── Step 3: Draw card from the chosen tier pool flatly ─────────────────
            var candidates = new List<CardEntry>();
            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                if (card == null || card.IsHidden) continue;
                if (card.Rarity != chosenRarity) continue;
                if (!stateById.TryGetValue(card.Id, out var progress)) continue;

                candidates.Add(card);
            }

            // If selected pool is empty, fall back to N pool as safety guard
            if (candidates.Count == 0 && chosenRarity != CardRarity.N)
            {
                chosenRarity = CardRarity.N;
                for (int i = 0; i < allCards.Count; i++)
                {
                    var card = allCards[i];
                    if (card == null || card.IsHidden) continue;
                    if (card.Rarity != chosenRarity) continue;
                    if (!stateById.TryGetValue(card.Id, out var progress)) continue;

                    candidates.Add(card);
                }
            }

            if (candidates.Count == 0) return null;

            // Equal probability flat selection (no base weights bias)
            int randomIndex = Random.Range(0, candidates.Count);
            return candidates[randomIndex];
        }

        private static CardRarity ChooseRarityTier(GachaType type, float nRed, float rRed)
        {
            // Base Rarity Drop Rates per Gacha Type
            float pN = 0.90f, pR = 0.09f, pSR = 0.01f, pSSR = 0f, pUR = 0f;

            if (type == GachaType.Rare)
            {
                pN = 0.50f; pR = 0.40f; pSR = 0.08f; pSSR = 0.02f; pUR = 0f;
            }
            else if (type == GachaType.Super)
            {
                pN = 0.10f; pR = 0.30f; pSR = 0.40f; pSSR = 0.15f; pUR = 0.05f;
            }

            // Apply upg-n-weight (reduces N, adds to R)
            if (nRed > 0f)
            {
                float deduct = Mathf.Min(pN, nRed);
                pN -= deduct;
                pR += deduct;
            }

            // Apply upg-r-weight (reduces R, adds to SR)
            if (rRed > 0f)
            {
                float deduct = Mathf.Min(pR, rRed);
                pR -= deduct;
                pSR += deduct;
            }

            // Roulette wheel selection
            float total = pN + pR + pSR + pSSR + pUR;
            float roll = Random.value * total;
            float cursor = 0f;

            cursor += pN;   if (roll <= cursor) return CardRarity.N;
            cursor += pR;   if (roll <= cursor) return CardRarity.R;
            cursor += pSR;  if (roll <= cursor) return CardRarity.SR;
            cursor += pSSR; if (roll <= cursor) return CardRarity.SSR;
            return CardRarity.UR;
        }

        private static bool IsRarityUnlocked(CardRarity rarity, bool rUnlocked, bool srUnlocked, bool ssrUnlocked, bool urUnlocked)
        {
            switch (rarity)
            {
                case CardRarity.N:   return true;
                case CardRarity.R:   return rUnlocked;
                case CardRarity.SR:  return srUnlocked;
                case CardRarity.SSR: return ssrUnlocked;
                case CardRarity.UR:  return urUnlocked;
                default:             return false;
            }
        }
    }
}
