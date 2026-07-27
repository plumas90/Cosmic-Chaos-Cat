using System;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class DecoEntry
    {
        public string Id;
        public string DisplayName;
        public bool IsHidden;
        public bool IsShop;
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 5000;
        public Sprite IconSprite;
        [TextArea(3, 5)] public string Description;

        public string GetDescription()
        {
            if (!string.IsNullOrEmpty(Description)) return Description;
            return $"{DisplayName} 장식 오브제입니다.";
        }
    }
}
