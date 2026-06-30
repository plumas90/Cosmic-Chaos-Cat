using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// Upgrade shop panel.
    /// UpgradeEntryUI components should be placed as children of this panel in the scene.
    /// This script finds them and binds the GameManager reference.
    /// </summary>
    public sealed class UpgradePanel : MonoBehaviour
    {
        [SerializeField] private GameManager        gameManager;
        [SerializeField] private Button             closeButton;
        [SerializeField] private TMP_Text           moneyText;

        // Tab buttons (optional — hide categories behind tabs)
        [SerializeField] private Button             tabClickButton;
        [SerializeField] private Button             tabGachaButton;
        [SerializeField] private Button             tabEconomyButton;
        [SerializeField] private GameObject         clickGroup;
        [SerializeField] private GameObject         gachaGroup;
        [SerializeField] private GameObject         economyGroup;

        private readonly List<UpgradeEntryUI> entries = new List<UpgradeEntryUI>();

        private void Awake()
        {
            // Find all entry UIs in children (pre-placed in scene)
            GetComponentsInChildren<UpgradeEntryUI>(true, entries);
            foreach (var e in entries)
                e.Bind(gameManager);
        }

        private void OnEnable()
        {
            if (closeButton     != null) closeButton.onClick.AddListener(OnClose);
            if (tabClickButton  != null) tabClickButton.onClick.AddListener(() => ShowTab(0));
            if (tabGachaButton  != null) tabGachaButton.onClick.AddListener(() => ShowTab(1));
            if (tabEconomyButton!= null) tabEconomyButton.onClick.AddListener(() => ShowTab(2));

            if (gameManager != null) gameManager.StateChanged += Refresh;
            ShowTab(0);
            Refresh();
        }

        private void OnDisable()
        {
            if (closeButton     != null) closeButton.onClick.RemoveListener(OnClose);
            if (tabClickButton  != null) tabClickButton.onClick.RemoveAllListeners();
            if (tabGachaButton  != null) tabGachaButton.onClick.RemoveAllListeners();
            if (tabEconomyButton!= null) tabEconomyButton.onClick.RemoveAllListeners();
            if (gameManager     != null) gameManager.StateChanged -= Refresh;
        }

        private void Refresh()
        {
            if (moneyText != null && gameManager != null)
                moneyText.text = $"💰 {gameManager.Money:0}";

            foreach (var e in entries) e.Refresh();
        }

        private void ShowTab(int index)
        {
            if (clickGroup   != null) clickGroup.SetActive(index == 0);
            if (gachaGroup   != null) gachaGroup.SetActive(index == 1);
            if (economyGroup != null) economyGroup.SetActive(index == 2);
        }

        private void OnClose() => gameObject.SetActive(false);
    }
}
