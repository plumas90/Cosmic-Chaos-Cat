using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(1, 3)] public string kr;
        [TextArea(1, 3)] public string en;

        public LocalizationEntry() { }

        public LocalizationEntry(string key, string kr, string en)
        {
            this.key = key;
            this.kr = kr;
            this.en = en;
        }
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "CosmicChaosCat/Localization Data")]
    public class LocalizationDataSO : ScriptableObject
    {
        public List<LocalizationEntry> entries = new List<LocalizationEntry>();

        public Dictionary<string, Dictionary<string, string>> ToTable()
        {
            var dict = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return dict;

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.key)) continue;
                var langDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "KR", entry.kr ?? string.Empty },
                    { "EN", entry.en ?? string.Empty }
                };
                dict[entry.key] = langDict;
            }
            return dict;
        }
    }
}
