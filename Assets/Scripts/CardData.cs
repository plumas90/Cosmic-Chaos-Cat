using System;
using UnityEngine;

namespace CosmicChaosCat
{
    public enum CardRarity { N, R, SR, SSR, UR, H }

    public enum CardShardValue
    {
        [InspectorName("1")]   Value_1   = 1,
        [InspectorName("3")]   Value_3   = 3,
        [InspectorName("5")]   Value_5   = 5,
        [InspectorName("10")]  Value_10  = 10,
        [InspectorName("50")]  Value_50  = 50,
        [InspectorName("100")] Value_100 = 100
    }

    public enum CardSpecialEffect
    {
        None,
        CriticalChanceBonus,
        DuplicateBonusBonus,
        ShardRefundBonus
    }

    public enum CardShopCurrency
    {
        Coin,
        Shard
    }

    public class SetIdAttribute : PropertyAttribute { }

    [Serializable]
    public sealed class CardEntry
    {
        public string Id;
        public string DisplayName;
        public string DisplayName_EN;
        public CardRarity Rarity;
        public float BaseWeight = 100f;

        [UnityEngine.Serialization.FormerlySerializedAs("ClickMultiplier")]
        [Tooltip("장착 시 클릭 당 기본 획득 골드 (Click/Gold)")]
        public float ClickGold = 1f;

        public float ClickMultiplier
        {
            get => ClickGold;
            set => ClickGold = value;
        }

        public CardShardValue ShardValue = CardShardValue.Value_1;
        public int MaxStacks = 6;
        [SetId] public string SetId;
        public bool IsHidden;
        public bool IsShop; // If true, card only appears in Shop and is excluded from Gacha
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 1000;
        public Sprite CardSprite;
        [Tooltip("가챠 연출의 cardbase 및 상점 ShardContent Front 배경 이미지")]
        public Sprite GachaBgSprite;

        public Sprite GachaBg
        {
            get => GachaBgSprite != null ? GachaBgSprite : CardSprite;
            set => GachaBgSprite = value;
        }

        public int[] BreakthroughVariantStages; // e.g. {1, 2, 3, 4, 5} or {1, 3, 5}
        public Sprite[] BreakthroughSprites;     // Variant sprites corresponding to stages 1..5
        public CardSpecialEffect SpecialEffect;
        public float SpecialEffectValue;
        [TextArea(3, 5)] public string Description;
        [TextArea(3, 5)] public string Description_EN;

        public System.Collections.Generic.List<int> GetBreakthroughStages()
        {
            if (BreakthroughVariantStages != null && BreakthroughVariantStages.Length > 0 &&
                BreakthroughSprites != null && BreakthroughSprites.Length > 0)
            {
                var list = new System.Collections.Generic.List<int>();
                foreach (int st in BreakthroughVariantStages)
                {
                    if (st >= 1 && st <= 5 && !list.Contains(st)) list.Add(st);
                }
                if (list.Count > 0) return list;
            }
            return new System.Collections.Generic.List<int>();
        }

        public Sprite GetSpriteForStage(int stage)
        {
            if (BreakthroughSprites != null && BreakthroughSprites.Length > 0)
            {
                if (BreakthroughVariantStages != null && BreakthroughVariantStages.Length == BreakthroughSprites.Length)
                {
                    for (int i = 0; i < BreakthroughVariantStages.Length; i++)
                    {
                        if (BreakthroughVariantStages[i] == stage && BreakthroughSprites[i] != null)
                            return BreakthroughSprites[i];
                    }
                }
                int idx = stage - 1;
                if (idx >= 0 && idx < BreakthroughSprites.Length && BreakthroughSprites[idx] != null)
                    return BreakthroughSprites[idx];
            }
            return CardSprite;
        }

        public string GetDisplayName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(DisplayName_EN)) return DisplayName_EN;
            return !string.IsNullOrEmpty(DisplayName) ? DisplayName : Id;
        }

        public string GetDescription(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN")
            {
                if (!string.IsNullOrEmpty(Description_EN)) return Description_EN;
                return $"{GetDisplayName(lang)} is a {Rarity} grade cat card. Provides click multiplier of x{ClickMultiplier}.";
            }
            if (!string.IsNullOrEmpty(Description)) return Description;
            return $"{DisplayName}은(는) {Rarity} 등급의 고양이 카드입니다. 특수 클릭 수익 배율 {ClickMultiplier}배를 제공합니다.";
        }
    }
}
