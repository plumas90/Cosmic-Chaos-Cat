using System;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class DecorationEntry
    {
        public string Id;
        public string DisplayName;
        public string DisplayName_EN;
        public Sprite DecorationSprite;
        public bool IsHidden;
        public bool IsShop;
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 1000;
        public string SetId;
        [TextArea(2, 4)]
        public string Description;
        [TextArea(2, 4)]
        public string Description_EN;

        public string GetDisplayName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(DisplayName_EN)) return DisplayName_EN;
            return !string.IsNullOrEmpty(DisplayName) ? DisplayName : Id;
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
