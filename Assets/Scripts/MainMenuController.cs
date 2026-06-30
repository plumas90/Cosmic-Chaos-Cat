using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button  startButton;
        [SerializeField] private Button  continueButton;
        [SerializeField] private TMP_Text savedTimeText;

        private void Awake()
        {
            // Show saved time if save data exists
            bool hasSave = PlayerPrefs.HasKey("ccc_save_v3");
            if (continueButton != null) continueButton.gameObject.SetActive(hasSave);
            if (savedTimeText  != null)
            {
                if (hasSave)
                {
                    var raw  = PlayerPrefs.GetString("ccc_save_v3", string.Empty);
                    var data = string.IsNullOrEmpty(raw) ? null : JsonUtility.FromJson<GameSaveData>(raw);
                    if (data != null)
                    {
                        var t = System.TimeSpan.FromSeconds(data.ElapsedSeconds);
                        savedTimeText.text = $"이어하기  {t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}";
                    }
                }
                else
                {
                    savedTimeText.text = string.Empty;
                }
            }
        }

        private void OnEnable()
        {
            if (startButton    != null) startButton.onClick.AddListener(NewGame);
            if (continueButton != null) continueButton.onClick.AddListener(ContinueGame);
        }

        private void OnDisable()
        {
            if (startButton    != null) startButton.onClick.RemoveListener(NewGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(ContinueGame);
        }

        private void NewGame()
        {
            PlayerPrefs.DeleteKey("ccc_save_v3");
            PlayerPrefs.Save();
            SceneManager.LoadScene("GameScene");
        }

        private void ContinueGame() => SceneManager.LoadScene("GameScene");
    }
}
