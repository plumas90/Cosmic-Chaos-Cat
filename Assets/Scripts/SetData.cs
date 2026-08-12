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

        public static bool IsSetAllowed(string setIdStr)
        {
            if (string.IsNullOrEmpty(setIdStr)) return true;
            string digits = System.Text.RegularExpressions.Regex.Match(setIdStr, @"\d+").Value;
            if (int.TryParse(digits, out int num))
            {
                return num <= 2 || num == 9 || num == 13 || num == 14; // 기존 공개 세트와 특수 카드 세트
            }
            return true;
        }

        public IReadOnlyList<SetEntry> Sets
        {
            get
            {
                var allowed = new List<SetEntry>();
                if (sets != null)
                {
                    foreach (var s in sets)
                    {
                        if (s != null && IsSetAllowed(s.SetId)) allowed.Add(s);
                    }
                }
                return allowed;
            }
        }

        public List<SetEntry> SetsList
        {
            get
            {
                var allowed = new List<SetEntry>();
                if (sets != null)
                {
                    foreach (var s in sets)
                    {
                        if (s != null && IsSetAllowed(s.SetId)) allowed.Add(s);
                    }
                }
                return allowed;
            }
        }

        public SetEntry FindById(string setId)
        {
            if (!IsSetAllowed(setId)) return null;
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
        public string SetName_EN;
        public double RewardGold = 0d;
        public int RewardShards = 0;
        public float CriticalChanceBonus = 0f;
        public double FlatIncomeBonus = 0d;
        public float CriticalDamageBonus = 0f;
        public float GachaDiscountBonus = 0f;
        public float ShardBonusMultiplier = 1.0f;
        public string RewardBackgroundId;
        public string RewardDecorationId;
        public string RewardCardId;
        [TextArea(2, 4)]
        public string EffectDesc;   // 세트 보상 효과 설명 (비어있으면 "아무 효과 없음" 표시)
        [TextArea(2, 4)]
        public string EffectDesc_EN;

        public string GetSetName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(SetName_EN)) return SetName_EN;
            return !string.IsNullOrEmpty(SetName) ? SetName : $"Set {SetId}";
        }

        public string GetRewardSummary(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            bool isEN = lang == "EN";
            if (SetId == "9" && RewardCardId == "0200")
                return isEN ? "Unlock Ophiuchus Cat" : "뱀주인자리 고양이 해금";

            var parts = new List<string>();
            if (RewardGold > 0d) parts.Add(isEN ? $"Gold +{RewardGold:N0}" : $"골드 +{RewardGold:N0}");
            if (RewardShards > 0) parts.Add(isEN ? $"Shards +{RewardShards:N0}" : $"조각 +{RewardShards:N0}");
            if (CriticalChanceBonus > 0f) parts.Add(isEN ? $"Crit Rate +{CriticalChanceBonus * 100:F0}%" : $"크리티컬 확률 +{CriticalChanceBonus * 100:F0}%");
            if (FlatIncomeBonus > 0d) parts.Add(isEN ? $"Gold Prod +{FlatIncomeBonus:N0}" : $"골드 생산 +{FlatIncomeBonus:N0}");
            if (CriticalDamageBonus > 0f) parts.Add(isEN ? $"Crit Dmg +{CriticalDamageBonus * 100:F0}%" : $"크리티컬 데미지 +{CriticalDamageBonus * 100:F0}%");
            if (GachaDiscountBonus > 0f) parts.Add(isEN ? $"Gacha Discount +{GachaDiscountBonus * 100:F0}%" : $"뽑기 할인 +{GachaDiscountBonus * 100:F0}%");
            bool showCollectionUnlocks = SetId != "1" && SetId != "2";
            if (showCollectionUnlocks && !string.IsNullOrEmpty(RewardBackgroundId)) parts.Add(isEN ? "Unlock BG" : "배경 해금");
            if (showCollectionUnlocks && !string.IsNullOrEmpty(RewardDecorationId)) parts.Add(isEN ? "Unlock Deco" : "데코 해금");
            if (!string.IsNullOrEmpty(RewardCardId)) parts.Add(isEN ? "Reward Card" : "보상 카드");

            if (parts.Count > 0) return string.Join(", ", parts);
            if (isEN && !string.IsNullOrWhiteSpace(EffectDesc_EN)) return EffectDesc_EN;
            return !string.IsNullOrWhiteSpace(EffectDesc) ? EffectDesc : (isEN ? "None" : "없음");
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
                if (card != null && card.CardSprite != null && card.SetId == SetId)
                    result.Add(card);
            }
            return result;
        }
    }
}
