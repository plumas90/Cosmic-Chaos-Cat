using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "BGCatalog", menuName = "CosmicChaosCat/BG Catalog")]
    public sealed class BGCatalogSO : ScriptableObject
    {
        [SerializeField] private List<BGEntry> backgrounds = new List<BGEntry>();

        public IReadOnlyList<BGEntry> Backgrounds => backgrounds;
        public List<BGEntry> BackgroundsList => backgrounds;

        public BGEntry FindById(string id)
        {
            for (int i = 0; i < backgrounds.Count; i++)
                if (backgrounds[i] != null && backgrounds[i].Id == id) return backgrounds[i];
            return null;
        }
    }
}
