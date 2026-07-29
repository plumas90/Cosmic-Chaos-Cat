using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [CreateAssetMenu(fileName = "DecorationCatalog", menuName = "CosmicChaosCat/Decoration Catalog")]
    public sealed class DecorationCatalogSO : ScriptableObject
    {
        [SerializeField] private List<DecorationEntry> decorations = new List<DecorationEntry>();

        public IReadOnlyList<DecorationEntry> Decorations => decorations;
        public List<DecorationEntry> DecorationsList => decorations;

        public DecorationEntry FindById(string id)
        {
            if (string.IsNullOrEmpty(id) || decorations == null) return null;
            for (int i = 0; i < decorations.Count; i++)
                if (decorations[i] != null && decorations[i].Id == id) return decorations[i];
            return null;
        }
    }
}
