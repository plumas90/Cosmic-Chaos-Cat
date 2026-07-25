using System;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public enum GachaType
    {
        Normal,
        Rare,
        Super
    }

    [Serializable]
    public sealed class CardProgress
    {
        public string CardId;
        public int Copies;
        public bool Unlocked;
        public int BreakthroughCount;
        public int SelectedStage = 1;
    }

    [Serializable]
    public sealed class UpgradeProgress
    {
        public string UpgradeId;
        public int Level;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public double Money;
        public int Shards;
        public float ElapsedSeconds;
        public string EquippedCardId;
        public string EquippedBackgroundId;
        public string EquippedDecorationId;
        public int TotalRolls;
        public long TotalClicks;
        public List<CardProgress> Cards = new List<CardProgress>();
        public List<UpgradeProgress> Upgrades = new List<UpgradeProgress>();
        public List<string> UnlockedHiddenCards = new List<string>();
        public List<string> CompletedSets = new List<string>();
        public List<string> ClaimedSetRewards = new List<string>();

        public bool UnlockedRareGacha;
        public bool UnlockedSuperGacha;

        public int nExchangeCount;
        public int rExchangeCount;
        public int srExchangeCount;
        public int ssrExchangeCount;
        public int urExchangeCount;
    }
}
