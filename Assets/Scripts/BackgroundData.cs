using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class BackgroundEntry
    {
        public string Id;
        public string DisplayName;
        public Sprite BackgroundSprite;
        public bool IsHidden;
        public bool IsShop;
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 1000;
        public string SetId;
        [TextArea(2, 4)]
        public string Description;
    }

    [Serializable]
    [CreateAssetMenu(fileName = "BackgroundCatalog", menuName = "CosmicChaosCat/Background Catalog")]
    public sealed class BackgroundCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BackgroundEntry> backgrounds = new List<BackgroundEntry>();

        public IReadOnlyList<BackgroundEntry> Backgrounds => backgrounds;
        public List<BackgroundEntry> BackgroundsList => backgrounds;

        public BackgroundEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id) || backgrounds == null) return null;
            for (int i = 0; i < backgrounds.Count; i++)
                if (backgrounds[i] != null && backgrounds[i].Id == id) return backgrounds[i];
            return null;
        }
    }
}
