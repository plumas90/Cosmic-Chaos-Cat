using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    /// <summary>
    /// UI static & dynamic text localization manager.
    /// Supports KR (Korean) and EN (English), easily expandable to 5+ languages.
    /// Dynamically loads and reads from LocalizationDataSO ScriptableObject.
    /// </summary>
    public static class LocalizationManager
    {
        public static event Action OnLanguageChanged;

        private static LocalizationDataSO dataSO;
        private static Dictionary<string, Dictionary<string, string>> runtimeTable;

        public static LocalizationDataSO DataSO
        {
            get
            {
                if (dataSO == null) LoadSO();
                return dataSO;
            }
        }

        public static void SetData(LocalizationDataSO data)
        {
            dataSO = data;
            RefreshRuntimeTable();
        }

        private static void LoadSO()
        {
#if UNITY_EDITOR
            dataSO = UnityEditor.AssetDatabase.LoadAssetAtPath<LocalizationDataSO>("Assets/ScriptableObjects/LocalizationData.asset");
#endif
            if (dataSO == null)
            {
                dataSO = Resources.Load<LocalizationDataSO>("LocalizationData");
            }
            RefreshRuntimeTable();
        }

        public static void RefreshRuntimeTable()
        {
            if (dataSO != null)
            {
                runtimeTable = dataSO.ToTable();
            }
        }

        public static string Get(string key, string lang = null)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }

            if (dataSO == null || runtimeTable == null)
            {
                LoadSO();
            }
            else
            {
#if UNITY_EDITOR
                // In Editor, ensure real-time inspector updates reflect immediately
                runtimeTable = dataSO.ToTable();
#endif
            }

            if (runtimeTable != null && runtimeTable.TryGetValue(key, out var dict))
            {
                if (dict.TryGetValue(lang, out var val) && !string.IsNullOrEmpty(val))
                    return val;
                if (dict.TryGetValue("KR", out var fallback))
                    return fallback;
            }
            return key;
        }

        public static void NotifyLanguageChanged()
        {
            RefreshRuntimeTable();
            OnLanguageChanged?.Invoke();
        }
    }
}
