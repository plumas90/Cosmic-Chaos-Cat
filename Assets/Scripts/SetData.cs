using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    [CreateAssetMenu(fileName = "SetCatalog", menuName = "CosmicChaosCat/Set Catalog")]
    public sealed class SetCatalogSO : ScriptableObject
    {
        [SerializeField] private List<SetEntry> sets = new List<SetEntry>();

        public IReadOnlyList<SetEntry> Sets => sets;

        public SetEntry FindById(string setId)
        {
            for (int i = 0; i < sets.Count; i++)
                if (sets[i] != null && sets[i].SetId == setId) return sets[i];
            return null;
        }
    }

    [Serializable]
    public sealed class SetEntry
    {
        public string SetId;
        public string SetName;
        public double RewardGold = 0d;
        public int RewardShards = 0;
        public float CriticalChanceBonus = 0f;
        public double FlatIncomeBonus = 0d;
        public float CriticalDamageBonus = 0f;
        public float GachaDiscountBonus = 0f;
        public float ShardBonusMultiplier = 1.0f;
        [TextArea(2, 4)]
        public string EffectDesc;   // 세트 보상 효과 설명 (비어있으면 "아무 효과 없음" 표시)

        public string GetRewardSummary()
        {
            var parts = new List<string>();
            if (RewardGold > 0d) parts.Add($"골드 +{RewardGold:N0}");
            if (RewardShards > 0) parts.Add($"조각 +{RewardShards:N0}");
            if (CriticalChanceBonus > 0f) parts.Add($"크리티컬 확률 +{CriticalChanceBonus * 100:F0}%");
            if (FlatIncomeBonus > 0d) parts.Add($"골드 생산 +{FlatIncomeBonus:N0}");
            if (CriticalDamageBonus > 0f) parts.Add($"크리티컬 데미지 +{CriticalDamageBonus * 100:F0}%");
            if (GachaDiscountBonus > 0f) parts.Add($"뽑기 할인 +{GachaDiscountBonus * 100:F0}%");

            if (parts.Count > 0) return string.Join(", ", parts);
            return !string.IsNullOrWhiteSpace(EffectDesc) ? EffectDesc : "없음";
        }

        /// <summary>
        /// 카드 카탈로그에서 이 세트에 속한 카드들(SetId 일치)을 동적으로 가져옵니다.
        /// </summary>
        public List<CardEntry> GetCardsInSet(IReadOnlyList<CardEntry> allCards)
        {
            var result = new List<CardEntry>();
            if (allCards == null || string.IsNullOrEmpty(SetId)) return result;
            for (int i = 0; i < allCards.Count; i++)
            {
                var card = allCards[i];
                if (card != null && card.SetId == SetId)
                    result.Add(card);
            }
            return result;
        }
    }
}
