using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Clicker clicker;
        [SerializeField] private Button gachaButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button encyclopediaButton;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private TMP_Text shardText;
        [SerializeField] private TMP_Text gachaButtonText;
        [SerializeField] private TMP_Text encyclopediaButtonText;
        [SerializeField] private GameObject encyclopediaPanel;
        [SerializeField] private KeyCode encyclopediaToggleKey = KeyCode.Tab;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<GameManager>();
            }
        }

        private void OnEnable()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.StateChanged += Refresh;
            if (clicker != null)
            {
                clicker.Clicked += gameManager.HandleCardClicked;
            }

            if (gachaButton != null)
            {
                gachaButton.onClick.AddListener(gameManager.RollOnce);
            }

            if (menuButton != null)
            {
                menuButton.onClick.AddListener(LoadMainMenu);
            }

            if (encyclopediaButton != null)
            {
                encyclopediaButton.onClick.AddListener(ToggleEncyclopedia);
            }

            if (encyclopediaPanel != null)
            {
                encyclopediaPanel.SetActive(false);
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
            }

            if (clicker != null && gameManager != null)
            {
                clicker.Clicked -= gameManager.HandleCardClicked;
            }

            if (gachaButton != null && gameManager != null)
            {
                gachaButton.onClick.RemoveListener(gameManager.RollOnce);
            }

            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(LoadMainMenu);
            }

            if (encyclopediaButton != null)
            {
                encyclopediaButton.onClick.RemoveListener(ToggleEncyclopedia);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(encyclopediaToggleKey))
            {
                ToggleEncyclopedia();
            }
        }

        private void Refresh()
        {
            timerText.text = gameManager.GetTimerText();
            coinText.text = $"코인 {gameManager.Money:0}";
            shardText.text = $"리소스 {gameManager.Shards}";
            gachaButtonText.text = $"가챠 1회 ({gameManager.GetCurrentGachaCost():0})";

            if (encyclopediaButtonText != null)
            {
                encyclopediaButtonText.text = $"도감 {gameManager.GetProgressText()}";
            }
        }

        private void ToggleEncyclopedia()
        {
            if (encyclopediaPanel == null)
            {
                return;
            }

            encyclopediaPanel.SetActive(!encyclopediaPanel.activeSelf);
        }

        private static void LoadMainMenu()
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}
