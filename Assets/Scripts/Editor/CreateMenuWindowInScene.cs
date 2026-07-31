#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat.Editor
{
    public static class CreateMenuWindowInScene
    {
        [MenuItem("Tools/Build Menu Window in GameScene")]
        public static void BuildMenuWindowInScene()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[CreateMenuWindowInScene] Canvas를 찾을 수 없습니다.");
                return;
            }

            // Existing MenuSettingsWindow Check
            var existing = canvas.transform.Find("MenuSettingsWindow");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            // 1. Root & Dark Overlay
            var menuWindow = new GameObject("MenuSettingsWindow");
            menuWindow.transform.SetParent(canvas.transform, false);
            var rt = menuWindow.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var blocker = menuWindow.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.65f);

            // 2. Panel Container
            var panel = new GameObject("MenuPanel");
            panel.transform.SetParent(menuWindow.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchoredPosition = Vector2.zero;
            prt.sizeDelta = new Vector2(460, 480);
            var panelImg = panel.AddComponent<Image>();
            panelImg.color = new Color(0.08f, 0.10f, 0.16f, 0.98f);

            var font = Object.FindObjectOfType<TextMeshProUGUI>()?.font;

            // 3. Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(panel.transform, false);
            var trt = titleGO.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, 200);
            trt.sizeDelta = new Vector2(400, 45);
            var titleTxt = titleGO.AddComponent<TextMeshProUGUI>();
            if (font != null) titleTxt.font = font;
            titleTxt.text = "⚙️ 설정 및 메뉴";
            titleTxt.fontSize = 22;
            titleTxt.fontStyle = FontStyles.Bold;
            titleTxt.alignment = TextAlignmentOptions.Center;
            titleTxt.color = Color.white;

            // 4. Sound Section
            var soundLabelGO = new GameObject("SoundLabel");
            soundLabelGO.transform.SetParent(panel.transform, false);
            var slrt = soundLabelGO.AddComponent<RectTransform>();
            slrt.anchoredPosition = new Vector2(-110, 135);
            slrt.sizeDelta = new Vector2(180, 35);
            var slTxt = soundLabelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) slTxt.font = font;
            slTxt.text = "🎵 사운드 설정";
            slTxt.fontSize = 16;
            slTxt.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            MakeMenuBtn(panel.transform, "Btn_SoundToggle", "🔊 소리 켬", new Vector2(80, 135), new Vector2(140, 38),
                new Color(0.2f, 0.45f, 0.7f, 1f), font);

            // 5. Language Section
            var langLabelGO = new GameObject("LangLabel");
            langLabelGO.transform.SetParent(panel.transform, false);
            var llrt = langLabelGO.AddComponent<RectTransform>();
            llrt.anchoredPosition = new Vector2(-110, 75);
            llrt.sizeDelta = new Vector2(180, 35);
            var llTxt = langLabelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) llTxt.font = font;
            llTxt.text = "🌐 언어 (Language)";
            llTxt.fontSize = 16;
            llTxt.color = new Color(0.9f, 0.9f, 0.9f, 1f);

            MakeMenuBtn(panel.transform, "Btn_LangKR", "🇰🇷 한국어", new Vector2(25, 75), new Vector2(95, 38),
                new Color(0.18f, 0.52f, 0.28f, 1f), font);
            MakeMenuBtn(panel.transform, "Btn_LangEN", "🇺🇸 English", new Vector2(130, 75), new Vector2(95, 38),
                new Color(0.25f, 0.3f, 0.4f, 1f), font);

            // Divider Line
            var divGO = new GameObject("Divider");
            divGO.transform.SetParent(panel.transform, false);
            var divRT = divGO.AddComponent<RectTransform>();
            divRT.anchoredPosition = new Vector2(0, 30);
            divRT.sizeDelta = new Vector2(400, 2);
            divGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            // 6. Action Buttons
            MakeMenuBtn(panel.transform, "Btn_Save", "💾 진행상황 저장하기", new Vector2(0, -25), new Vector2(380, 48),
                new Color(0.18f, 0.52f, 0.28f, 1f), font);

            MakeMenuBtn(panel.transform, "Btn_MainMenu", "🏠 메인 메뉴로 이동", new Vector2(0, -90), new Vector2(380, 48),
                new Color(0.70f, 0.35f, 0.15f, 1f), font);

            MakeMenuBtn(panel.transform, "Btn_Resume", "❌ 계속하기 (닫기)", new Vector2(0, -165), new Vector2(380, 48),
                new Color(0.35f, 0.35f, 0.40f, 1f), font);

            menuWindow.SetActive(false);

            EditorSceneManager.MarkSceneDirty(activeScene);
            Debug.Log("[CreateMenuWindowInScene] MenuSettingsWindow가 GameScene 계층 구조에 성공적으로 생성되었습니다!");
        }

        private static void MakeMenuBtn(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bg, TMP_FontAsset font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            var cs = btn.colors;
            cs.highlightedColor = bg * 1.3f;
            cs.pressedColor = bg * 0.7f;
            btn.colors = cs;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var lrt = labelGO.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var tx = labelGO.AddComponent<TextMeshProUGUI>();
            if (font != null) tx.font = font;
            tx.text = label;
            tx.fontSize = 15;
            tx.alignment = TextAlignmentOptions.Center;
            tx.color = Color.white;
        }
    }
}
#endif
