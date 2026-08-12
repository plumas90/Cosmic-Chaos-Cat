using System;
using System.Collections.Generic;

// Socket slot identifiers for the 5-socket click system
public enum ClickSocketSlot { Center = 0, LeftUp = 1, RightUp = 2, LeftDown = 3, RightDown = 4 }

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
        public int SaveVersion;
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
        public List<string> UnlockedBackgrounds = new List<string>();
        public List<string> UnlockedDecorations = new List<string>();

        // Socket unlock flags (Center is always unlocked; sub-sockets purchased from shop)
        public bool SocketLeftUpUnlocked   = false;
        public bool SocketRightUpUnlocked  = false;
        public bool SocketLeftDownUnlocked = false;
        public bool SocketRightDownUnlocked = false;

        // Card ID equipped in each socket slot (Center = EquippedCardId for backward compat)
        public string SocketLeftUpCardId    = "";
        public string SocketRightUpCardId   = "";
        public string SocketLeftDownCardId  = "";
        public string SocketRightDownCardId = "";

        // Illustration stage assigned independently to each click socket.
        public int SocketCenterStage    = 1;
        public int SocketLeftUpStage    = 1;
        public int SocketRightUpStage   = 1;
        public int SocketLeftDownStage  = 1;
        public int SocketRightDownStage = 1;

        public bool UnlockedRareGacha;
        public bool UnlockedSuperGacha;

        public int nExchangeCount;
        public int rExchangeCount;
        public int srExchangeCount;
        public int ssrExchangeCount;
        public int urExchangeCount;

        // Settings
        public float BgmVolume = 1f;
        public float SfxVolume = 1f;
        public bool IsMuted = false;
        public string SelectedLanguage = "KR";
    }
}
