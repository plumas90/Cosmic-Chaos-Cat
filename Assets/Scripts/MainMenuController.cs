using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private void OnEnable()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(StartGame);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }
        }

        public void StartGame()
        {
            SceneManager.LoadScene("GameScene");
        }
    }
}
