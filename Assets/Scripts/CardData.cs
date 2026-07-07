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
        public int MaxStacks = 6;
        public string SetId;
        public bool IsHidden;
        public Sprite CardSprite;
        public CardSpecialEffect SpecialEffect;
        public float SpecialEffectValue;
        [TextArea(3, 5)] public string Description;

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Description)) return Description;
            return $"{DisplayName}은(는) {Rarity} 등급의 고양이 카드입니다. 특수 클릭 수익 배율 {ClickMultiplier}배를 제공합니다.";
        }
    }
}
