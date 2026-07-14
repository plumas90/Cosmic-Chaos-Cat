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
        public List<string> CardIds = new List<string>();
        public float CriticalChanceBonus = 0f;
        public double FlatIncomeBonus = 0d;
        public float CriticalDamageBonus = 0f;
        public float GachaDiscountBonus = 0f;
        public float ShardBonusMultiplier = 1.0f;
        [TextArea(2, 4)]
        public string EffectDesc;   // 세트 보상 효과 설명 (비어있으면 "아무 효과 없음" 표시)
    }
}
