using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// A single upgrade button row. Pre-place in the UpgradePanel — one per upgrade.
    /// Assign the UpgradeId in Inspector to link it to the catalog entry.
    /// </summary>
    public sealed class UpgradeEntryUI : MonoBehaviour
    {
        [Header("Link to catalog")]
        [SerializeField] private string upgradeId;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button   buyButton;
        [SerializeField] private Image    bgImage;

        [Header("Colors")]
        [SerializeField] private Color affordableColor   = new Color(0.18f, 0.62f, 0.30f);
        [SerializeField] private Color unaffordableColor = new Color(0.40f, 0.40f, 0.40f);
        [SerializeField] private Color maxLevelColor     = new Color(0.85f, 0.70f, 0.10f);

        private GameManager gm;

        private void OnEnable()
        {
            if (buyButton != null)
                buyButton.onClick.AddListener(OnBuy);
        }

        private void OnDisable()
        {
            if (buyButton != null)
                buyButton.onClick.RemoveListener(OnBuy);
        }

        public void Bind(GameManager gameManager)
        {
            gm = gameManager;
            Refresh();
        }

        public void Refresh()
        {
            if (gm == null || gm.UpgradeCatalog == null) return;
            var entry = gm.UpgradeCatalog.FindById(upgradeId);
            if (entry == null) return;

            int  level  = gm.GetUpgradeLevel(upgradeId);
            bool maxed  = level >= entry.MaxLevel;

            if (nameText  != null) nameText.text  = entry.DisplayName;
            if (descText  != null) descText.text  = entry.Description;
            if (levelText != null) levelText.text = $"Lv.{level} / {entry.MaxLevel}";

            if (maxed)
            {
                if (costText  != null) costText.text       = "MAX";
                if (bgImage   != null) bgImage.color       = maxLevelColor;
                if (buyButton != null) buyButton.interactable = false;
            }
            else
            {
                bool canAfford = gm.CanAffordUpgrade(upgradeId);
                double cost    = entry.CostPerLevel != null && level < entry.CostPerLevel.Length
                    ? entry.CostPerLevel[level] : 0;

                if (costText  != null) costText.text         = $"{GameManager.FormatNumber(cost)} 코인";
                if (bgImage   != null) bgImage.color         = canAfford ? affordableColor : unaffordableColor;
                if (buyButton != null) buyButton.interactable = canAfford;
            }
        }

        private void OnBuy()
        {
            gm?.BuyUpgrade(upgradeId);
            Refresh();
        }
    }
}
