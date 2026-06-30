using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// Gacha pull panel — 1회 / 10회 버튼과 결과 표시.
    /// Placed in scene, activated by GameHud.
    /// </summary>
    public sealed class GachaPanel : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ClickEffectPlayer effectPlayer;

        [Header("Buttons")]
        [SerializeField] private Button  rollOnceButton;
        [SerializeField] private Button  rollTenButton;
        [SerializeField] private Button  closeButton;

        [Header("Cost Labels")]
        [SerializeField] private TMP_Text rollOnceCostText;
        [SerializeField] private TMP_Text rollTenCostText;
        [SerializeField] private TMP_Text moneyText;

        [Header("Result Area")]
        [SerializeField] private TMP_Text  resultLogText;
        [SerializeField] private Animator  panelAnimator;
        [SerializeField] private string    openTriggerName = "Open";

        private void OnEnable()
        {
            if (rollOnceButton != null) rollOnceButton.onClick.AddListener(OnRollOnce);
            if (rollTenButton  != null) rollTenButton.onClick.AddListener(OnRollTen);
            if (closeButton    != null) closeButton.onClick.AddListener(OnClose);

            if (gameManager != null)
            {
                gameManager.StateChanged += RefreshCosts;
                gameManager.LogUpdated   += OnLog;
                gameManager.CardDrawn    += OnCardDrawn;
            }

            if (panelAnimator != null && !string.IsNullOrEmpty(openTriggerName))
                panelAnimator.SetTrigger(openTriggerName);

            RefreshCosts();
        }

        private void OnDisable()
        {
            if (rollOnceButton != null) rollOnceButton.onClick.RemoveListener(OnRollOnce);
            if (rollTenButton  != null) rollTenButton.onClick.RemoveListener(OnRollTen);
            if (closeButton    != null) closeButton.onClick.RemoveListener(OnClose);

            if (gameManager != null)
            {
                gameManager.StateChanged -= RefreshCosts;
                gameManager.LogUpdated   -= OnLog;
                gameManager.CardDrawn    -= OnCardDrawn;
            }
        }

        private void RefreshCosts()
        {
            if (gameManager == null) return;
            double singleCost = gameManager.GetCurrentGachaCost();
            double tenCost    = singleCost * 9;   // 10회 가격은 GameManager.RollTen()에서 정확히 계산

            if (rollOnceCostText != null) rollOnceCostText.text = $"1회 뽑기\n{singleCost:0} 코인";
            if (rollTenCostText  != null) rollTenCostText.text  = $"10회 뽑기\n{tenCost:0} 코인";
            if (moneyText        != null) moneyText.text        = $"💰 {gameManager.Money:0}";
        }

        private void OnLog(string msg)
        {
            if (resultLogText != null) resultLogText.text = msg;
        }

        private void OnCardDrawn(string cardId, CardRarity rarity)
        {
            effectPlayer?.PlayGachaEffect(rarity);
        }

        private void OnRollOnce()
        {
            gameManager?.RollOnce();
            RefreshCosts();
        }

        private void OnRollTen()
        {
            gameManager?.RollTen();
            RefreshCosts();
        }

        private void OnClose() => gameObject.SetActive(false);
    }
}
