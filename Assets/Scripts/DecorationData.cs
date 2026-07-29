using System;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class DecorationEntry
    {
        public string Id;
        public string DisplayName;
        public Sprite DecorationSprite;
        public bool IsHidden;
        public bool IsShop;
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 1000;
        public string SetId;
        [TextArea(2, 4)]
        public string Description;
    }
}
