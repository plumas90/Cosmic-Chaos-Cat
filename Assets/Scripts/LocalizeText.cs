using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    public class LocalizeText : MonoBehaviour
    {
        [SerializeField] private string key;

        public string Key
        {
            get => key;
            set
            {
                key = value;
                Refresh();
            }
        }

        private TMP_Text tmpText;
        private Text legacyText;

        private void Awake()
        {
            tmpText = GetComponent<TMP_Text>();
            legacyText = GetComponent<Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.OnLanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= Refresh;
        }

        public void Refresh()
        {
            if (tmpText == null) tmpText = GetComponent<TMP_Text>();
            if (legacyText == null) legacyText = GetComponent<Text>();

            if (!string.IsNullOrEmpty(key))
            {
                string localizedStr = LocalizationManager.Get(key);
                if (tmpText != null)
                {
                    tmpText.text = localizedStr;
                    tmpText.SetAllDirty();
                }
                if (legacyText != null)
                {
                    legacyText.text = localizedStr;
                    legacyText.SetAllDirty();
                }
            }
        }
    }
}
