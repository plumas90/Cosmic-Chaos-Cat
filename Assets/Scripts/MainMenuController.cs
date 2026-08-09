using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 메인 메뉴 컨트롤러 – 완전 자급자족형.
    /// 씬에 있는 StartButton은 찾아서 연결하고, 이어하기 버튼과 저장 정보 텍스트는 코드로 생성.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        private Button   continueButton;
        private const string SaveKey = "ccc_save_v3";

        // ─── Lifecycle ───────────────────────────────────────────────────────
        private void Awake()
        {
            // startButton을 씬에서 찾지 못했으면 직접 탐색
            if (startButton == null)
            {
                var go = GameObject.Find("StartButton");
                if (go != null) startButton = go.GetComponent<Button>();
            }

            BuildContinueUI();
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

        // ─── Continue UI Builder ─────────────────────────────────────────────
        private void BuildContinueUI()
        {
            bool hasSave = PlayerPrefs.HasKey(SaveKey);

            // startButton이 있으면 그 부모 캔버스에 붙임, 없으면 자신의 캔버스
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            Transform uiParent = startButton != null
                ? startButton.transform.parent
                : (canvas != null ? canvas.transform : transform);

            // ── 이어하기 버튼 ──────────────────────────────────────────────
            if (hasSave)
            {
                // startSize 구하기 (시작버튼과 크기 똑같이 맞춤)
                Vector2 startSize = startButton != null
                    ? startButton.GetComponent<RectTransform>().sizeDelta
                    : new Vector2(240, 62);

                // startButton 위치를 기준으로 이어하기를 더 아래에 배치 (겹치지 않게 여백 조정: 65px로 변경)
                Vector2 contPos = startButton != null
                    ? GetAnchoredPos(startButton) + new Vector2(0, -(startSize.y + 65f))
                    : new Vector2(0, -120);

                GameObject contGO;
                if (startButton != null)
                {
                    contGO = Instantiate(startButton.gameObject, uiParent, false);
                    contGO.name = "Btn_이어하기";
                    var contRT = contGO.GetComponent<RectTransform>();
                    if (contRT != null)
                    {
                        contRT.anchoredPosition = contPos;
                        contRT.sizeDelta = startSize;
                    }
                    var copiedText = contGO.GetComponentInChildren<TMP_Text>(true);
                    if (copiedText != null) copiedText.text = "이어하기";
                }
                else
                {
                    contGO = MakeButton(uiParent, "이어하기", contPos, startSize, Color.white);
                }
                continueButton = contGO.GetComponent<Button>();
                ApplyDarkButtonTransitions(continueButton);
            }

            // Keep the scene-authored background sprite/image tint and only set interaction colors.
            ApplyDarkButtonTransitions(startButton);
        }

        // ─── Game Logic ──────────────────────────────────────────────────────
        private void NewGame()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            SceneManager.LoadScene("GameScene");
        }

        private void ContinueGame() => SceneManager.LoadScene("GameScene");

        // ─── Helpers ────────────────────────────────────────────────────────
        private static Vector2 GetAnchoredPos(Button btn)
        {
            var rt = btn.GetComponent<RectTransform>();
            return rt != null ? rt.anchoredPosition : Vector2.zero;
        }

        private static void ApplyDarkButtonTransitions(Button button)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.32f, 0.32f, 0.32f, 1f);
            colors.pressedColor = new Color(0.08f, 0.08f, 0.08f, 1f);
            colors.selectedColor = new Color(0.20f, 0.20f, 0.20f, 1f);
            colors.disabledColor = new Color(0.10f, 0.10f, 0.10f, 0.5f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
        }

        private static GameObject MakeButton(Transform parent, string label, Vector2 pos, Vector2 size, Color bg)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            var cs  = btn.colors;
            cs.highlightedColor = bg * 1.3f;
            cs.pressedColor     = bg * 0.7f;
            btn.colors          = cs;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            tx.text      = label;
            tx.fontSize  = 18;
            tx.alignment = TextAlignmentOptions.Center;
            tx.color     = Color.white;

            return go;
        }
    }
}
