using System;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class BGEntry
    {
        public string Id;
        public string DisplayName;
        public string Category = "테마"; // e.g. "자연", "테마", "시즌", "이벤트"
        public bool IsHidden;
        public bool IsShop;
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 10000;
        public Sprite BGSprite;
        [TextArea(3, 5)] public string Description;

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Description)) return Description;
            return $"{DisplayName} 배경 테마입니다.";
        }
    }
}
