using System;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public enum CardRarity
    {
        N,
        R,
        SR,
        SSR,
        UR
    }

    [Serializable]
    public sealed class CardDefinition
    {
        public string Id;
        public string Name;
        public CardRarity Rarity;
        public float BaseWeight;
        public float ClickMultiplier;
        public int ShardValue;
        public int MaxStacks;
    }

    [Serializable]
    public sealed class CardProgress
    {
        public string CardId;
        public int Copies;
        public bool Unlocked;
    }

    [Serializable]
    public sealed class GameSaveData
    {
        public double Money;
        public int Shards;
        public float ElapsedSeconds;
        public string EquippedCardId;
        public int TotalRolls;
        public List<CardProgress> Cards = new List<CardProgress>();
    }

    public static class DefaultCardCatalog
    {
        public static List<CardDefinition> Create()
        {
            return new List<CardDefinition>
            {
                new CardDefinition
                {
                    Id = "n-tabby",
                    Name = "길냥이 새싹",
                    Rarity = CardRarity.N,
                    BaseWeight = 900f,
                    ClickMultiplier = 1f,
                    ShardValue = 1,
                    MaxStacks = 5
                },
                new CardDefinition
                {
                    Id = "r-student",
                    Name = "교복 고양이",
                    Rarity = CardRarity.R,
                    BaseWeight = 100f,
                    ClickMultiplier = 1.4f,
                    ShardValue = 3,
                    MaxStacks = 5
                },
                new CardDefinition
                {
                    Id = "sr-winter",
                    Name = "겨울 코트 캣",
                    Rarity = CardRarity.SR,
                    BaseWeight = 70f,
                    ClickMultiplier = 2f,
                    ShardValue = 10,
                    MaxStacks = 5
                },
                new CardDefinition
                {
                    Id = "ssr-cosmos",
                    Name = "코스믹 전령",
                    Rarity = CardRarity.SSR,
                    BaseWeight = 9f,
                    ClickMultiplier = 3f,
                    ShardValue = 30,
                    MaxStacks = 5
                },
                new CardDefinition
                {
                    Id = "ur-chaos",
                    Name = "혼돈의 고양신",
                    Rarity = CardRarity.UR,
                    BaseWeight = 1f,
                    ClickMultiplier = 5f,
                    ShardValue = 100,
                    MaxStacks = 5
                }
            };
        }
    }
}
