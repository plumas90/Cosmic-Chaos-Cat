using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 게임 씬 메인 HUD.
    /// 모든 UI 요소를 인스펙터에서 미리 배치하고 연결합니다.
    /// 런타임 오브젝트 생성 없음.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Core References")]
        [SerializeField] private GameManager       gameManager;
        [SerializeField] private ClickEffectPlayer effectPlayer;

        [Header("HUD Labels")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text comboText;
        [SerializeField] private TMP_Text equippedText;
        [SerializeField] private TMP_Text logText;
        [SerializeField] private TMP_Text pityText;       // "피티 XX / 100"

        [Header("Navigation Buttons")]
        [SerializeField] private Button gachaButton;
        [SerializeField] private Button encyclopediaButton;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private Button exchangeButton;   // 조각 교환
        [SerializeField] private Button menuButton;

        [Header("Panels — pre-placed, start inactive")]
        [SerializeField] private GachaPanel         gachaPanel;
        [SerializeField] private EncyclopediaPanel  encyclopediaPanel;
        [SerializeField] private UpgradePanel       upgradePanel;
        [SerializeField] private ShardExchangePanel exchangePanel;

        [Header("Ending Screen — pre-placed, starts inactive")]
        [SerializeField] private GameObject endingScreen;
        [SerializeField] private TMP_Text   endingTimerText;
        [SerializeField] private TMP_Text   endingMessageText;

        [Header("Hard Pity Notification — pre-placed, starts inactive")]
        [SerializeField] private GameObject pityNotification;   // "100연 보장 발동!" 팝업

        private void Awake()
        {
            // 씬 로드 시 모든 패널 비활성화 보장
            SetPanelActive(gachaPanel,         false);
            SetPanelActive(encyclopediaPanel,  false);
            SetPanelActive(upgradePanel,       false);
            SetPanelActive(exchangePanel,      false);
            if (endingScreen      != null) endingScreen.SetActive(false);
            if (pityNotification  != null) pityNotification.SetActive(false);
        }

        private void OnEnable()
        {
            if (gameManager == null) return;

            gameManager.StateChanged  += Refresh;
            gameManager.LogUpdated    += OnLog;
            gameManager.CriticalHit   += OnCriticalHit;
            gameManager.GameEnded     += OnGameEnded;
            gameManager.HardPityFired += OnHardPityFired;

            if (gachaButton       != null) gachaButton.onClick.AddListener(OpenGacha);
            if (encyclopediaButton != null) encyclopediaButton.onClick.AddListener(ToggleEncyclopedia);
            if (upgradeButton     != null) upgradeButton.onClick.AddListener(ToggleUpgrade);
            if (exchangeButton    != null) exchangeButton.onClick.AddListener(ToggleExchange);
            if (menuButton        != null) menuButton.onClick.AddListener(GoToMenu);

            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager == null) return;

            gameManager.StateChanged  -= Refresh;
            gameManager.LogUpdated    -= OnLog;
            gameManager.CriticalHit   -= OnCriticalHit;
            gameManager.GameEnded     -= OnGameEnded;
            gameManager.HardPityFired -= OnHardPityFired;

            if (gachaButton       != null) gachaButton.onClick.RemoveListener(OpenGacha);
            if (encyclopediaButton != null) encyclopediaButton.onClick.RemoveListener(ToggleEncyclopedia);
            if (upgradeButton     != null) upgradeButton.onClick.RemoveListener(ToggleUpgrade);
            if (exchangeButton    != null) exchangeButton.onClick.RemoveListener(ToggleExchange);
            if (menuButton        != null) menuButton.onClick.RemoveListener(GoToMenu);
        }

        private void Refresh()
        {
            if (gameManager == null) return;

            if (timerText    != null) timerText.text    = gameManager.GetTimerText();
            if (coinText     != null) coinText.text     = $"💰  {gameManager.Money:0}";
            if (shardText    != null) shardText.text    = $"🔷  {gameManager.Shards}";
            if (progressText != null) progressText.text =
                $"📖  {gameManager.UnlockedCount} / {gameManager.TotalCardCount}" +
                $"  ({gameManager.Completion01 * 100f:0.0}%)";

            // 콤보
            if (comboText != null)
                comboText.text = gameManager.ComboCount > 1
                    ? $"🔥  ×{gameManager.ComboCount} 콤보!"
                    : string.Empty;

            // 장착 카드
            var card = gameManager.GetEquippedCard();
            if (equippedText != null)
                equippedText.text = card != null
                    ? $"장착: {card.DisplayName}  [{card.Rarity}]"
                    : "장착: 기본";

            // 피티 카운터
            if (pityText != null)
            {
                int pity  = gameManager.PityCounter;
                int total = gameManager.HardPityThreshold;
                pityText.text = $"⭐ 피티  {pity} / {total}";
                // 80연 이상이면 강조 색상
                if (pity >= 80)
                    pityText.color = new Color(1f, 0.8f, 0f);
                else
                    pityText.color = Color.white;
            }
        }

        private void OnLog(string msg)
        {
            if (logText != null) logText.text = msg;
        }

        private void OnCriticalHit()
        {
            effectPlayer?.PlayCriticalEffect();
        }

        private void OnGameEnded()
        {
            if (endingScreen != null) endingScreen.SetActive(true);
            if (endingTimerText != null)
                endingTimerText.text = $"클리어 타임\n{gameManager.GetTimerText()}";
            if (endingMessageText != null)
                endingMessageText.text =
                    $"총 뽑기 {gameManager.TotalRolls}회\n총 클릭 {gameManager.TotalClicks}번";
        }

        private void OnHardPityFired()
        {
            if (pityNotification == null) return;
            pityNotification.SetActive(true);
            // 3초 후 자동으로 닫기
            Invoke(nameof(HidePityNotification), 3f);
        }

        private void HidePityNotification()
        {
            if (pityNotification != null) pityNotification.SetActive(false);
        }

        private void OpenGacha()        => SetPanelActive(gachaPanel,        true);
        private void ToggleEncyclopedia() => Toggle(encyclopediaPanel);
        private void ToggleUpgrade()    => Toggle(upgradePanel);
        private void ToggleExchange()   => Toggle(exchangePanel);

        private static void GoToMenu() => SceneManager.LoadScene("MainMenuScene");

        private static void Toggle(MonoBehaviour panel)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(!panel.gameObject.activeSelf);
        }

        private static void SetPanelActive(MonoBehaviour panel, bool active)
        {
            if (panel != null) panel.gameObject.SetActive(active);
        }
    }
}
