using System;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class CardProgress
    {
        public string CardId;
        public int Copies;
        public bool Unlocked;
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
        public int TotalRolls;
        public int PityCounter;
        public long TotalClicks;
        public List<CardProgress> Cards = new List<CardProgress>();
        public List<UpgradeProgress> Upgrades = new List<UpgradeProgress>();
        public List<string> UnlockedHiddenCards = new List<string>();
        public List<string> CompletedSets = new List<string>();
    }
}
