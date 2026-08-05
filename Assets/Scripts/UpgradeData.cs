using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
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
        GachaDiscount,
        UnlockRCard,
        UnlockSRCard,
        UnlockSSRCard,
        UnlockURCard
    }

    [Serializable]
    public sealed class UpgradeEntry
    {
        public string UpgradeId;
        public string DisplayName;
        public string DisplayName_EN;
        [TextArea(2, 4)] public string Description;
        [TextArea(2, 4)] public string Description_EN;
        public UpgradeCategory Category;
        public int MaxLevel;
        public double[] CostPerLevel;
        public UpgradeEffectType EffectType;
        public float[] EffectValuePerLevel;

        public string GetDisplayName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(DisplayName_EN)) return DisplayName_EN;
            return !string.IsNullOrEmpty(DisplayName) ? DisplayName : UpgradeId;
        }

        public string GetDescription(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(Description_EN)) return Description_EN;
            return !string.IsNullOrEmpty(Description) ? Description : string.Empty;
        }
    }
}
