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

        // ─── Style ──────────────────────────────────────────────────────────
        private static readonly Color BtnNew      = new Color(0.20f, 0.55f, 0.90f, 1.00f);
        private static readonly Color BtnContinue = new Color(0.18f, 0.52f, 0.28f, 1.00f);

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
            Transform uiParent = canvas != null ? canvas.transform : transform;

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

                var contGO = MakeButton(uiParent, "이어하기", contPos, startSize, BtnContinue);
                continueButton = contGO.GetComponent<Button>();

                // 시작버튼의 텍스트 컴포넌트에서 폰트 및 글자 크기 복사
                if (startButton != null)
                {
                    var startTxt = startButton.GetComponentInChildren<TMP_Text>();
                    var contTxt = contGO.GetComponentInChildren<TMP_Text>();
                    if (startTxt != null && contTxt != null)
                    {
                        contTxt.font = startTxt.font;
                        contTxt.fontSize = startTxt.fontSize;
                    }
                }
            }

            // startButton 색상 세팅 (씬에서 직접 바꾸기 어려우므로 코드로)
            if (startButton != null)
            {
                var img = startButton.GetComponent<Image>();
                if (img != null) img.color = BtnNew;
            }
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
