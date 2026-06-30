using System;
using UnityEngine;

namespace CosmicChaosCat
{
    public enum CardRarity { N, R, SR, SSR, UR }

    public enum CardSpecialEffect
    {
        None,
        CriticalChanceBonus,
        DuplicateBonusBonus,
        ShardRefundBonus
    }

    [Serializable]
    public sealed class CardEntry
    {
        public string Id;
        public string DisplayName;
        public CardRarity Rarity;
        public float BaseWeight = 100f;
        public float ClickMultiplier = 1f;
        public int ShardValue = 3;
        public int MaxStacks = 5;
        public string SetId;
        public bool IsHidden;
        public Sprite CardSprite;
        public CardSpecialEffect SpecialEffect;
        public float SpecialEffectValue;
    }
}
