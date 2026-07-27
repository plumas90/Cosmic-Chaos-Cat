using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "DecoCatalog", menuName = "CosmicChaosCat/Deco Catalog")]
    public sealed class DecoCatalogSO : ScriptableObject
    {
        [SerializeField] private List<DecoEntry> decos = new List<DecoEntry>();

        public IReadOnlyList<DecoEntry> Decos => decos;
        public List<DecoEntry> DecosList => decos;

        public DecoEntry FindById(string id)
        {
            for (int i = 0; i < decos.Count; i++)
                if (decos[i] != null && decos[i].Id == id) return decos[i];
            return null;
        }
    }
}
