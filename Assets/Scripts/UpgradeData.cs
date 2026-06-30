using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public enum UpgradeCategory { Click, Gacha, Economy }

    public enum UpgradeEffectType
    {
        CriticalChance,
        CriticalMultiplier,
        ComboBonus,
        NWeightReduction,
        RWeightReduction,
        ShardRefundBonus,
        ExtraGachaPull,
        GachaDiscount
    }

    [Serializable]
    public sealed class UpgradeEntry
    {
        public string UpgradeId;
        public string DisplayName;
        [TextArea(2, 4)] public string Description;
        public UpgradeCategory Category;
        public int MaxLevel;
        public double[] CostPerLevel;
        public UpgradeEffectType EffectType;
        public float[] EffectValuePerLevel;
    }

    [CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "CosmicChaosCat/Upgrade Catalog")]
    public sealed class UpgradeCatalogSO : ScriptableObject
    {
        [SerializeField] private List<UpgradeEntry> upgrades = new List<UpgradeEntry>();

        public IReadOnlyList<UpgradeEntry> Upgrades => upgrades;

        public UpgradeEntry FindById(string upgradeId)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i] != null && upgrades[i].UpgradeId == upgradeId) return upgrades[i];
            return null;
        }
    }
}
