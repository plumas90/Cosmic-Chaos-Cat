using UnityEngine;
using UnityEditor;
using TMPro;

namespace CosmicChaosCat.Editor
{
    /// <summary>
    /// 도감 패널 UI를 에디터 모드에서 씬에 미리 구워넣는 도구.
    /// Tools → Encyclopedia → 씬에 UI 빌드
    /// </summary>
    public static class BakeEncyclopediaUI
    {
        // [MenuItem("Tools/Encyclopedia/씬에 UI 빌드 (에디터 전용)")]
        public static void Execute()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("도감 UI 빌드",
                    "Play 모드 중에는 실행할 수 없습니다.\nPlay를 중단 후 다시 시도하세요.", "확인");
                return;
            }

            var encyPanel = Object.FindObjectOfType<EncyclopediaPanel>(true);
            if (encyPanel == null)
            {
                EditorUtility.DisplayDialog("도감 UI 빌드",
                    "씬에서 EncyclopediaPanel을 찾을 수 없습니다.", "확인");
                return;
            }

            // 1. 폰트 로드
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Font/Galmuri9 SDF.asset");
            if (font == null)
                Debug.LogWarning("[BakeEncyclopediaUI] Assets/Font/Galmuri9 SDF.asset 를 찾지 못했습니다. 기본 폰트로 진행합니다.");

            // 2. BakeUIToScene 호출 (ContextMenu 공개 메서드)
            encyPanel.BakeUIToScene();

            // 3. 전체 TMP Text에 폰트 적용
            if (font != null)
            {
                var allTexts = encyPanel.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var t in allTexts)
                {
                    t.font = font;
                    EditorUtility.SetDirty(t);
                }
            }

            // 4. 씬 저장
            EditorUtility.SetDirty(encyPanel.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[BakeEncyclopediaUI] 완료! 계층창에서 EncyclopediaPanel 하위에 UI가 생성되었습니다.\n이제 Field Guide Sprites 섹션에 스프라이트를 드래그 연결하고 Ctrl+S로 저장하세요.");

            EditorUtility.DisplayDialog("도감 UI 빌드 완료",
                "계층창에 EncyclopediaPanel 하위로 UI가 생성됐습니다!\n\n" +
                "다음 단계:\n" +
                "1. EncyclopediaPanel 인스펙터에서\n" +
                "   Field Guide Sprites 섹션에 스프라이트 연결\n" +
                "2. Ctrl+S 로 씬 저장",
                "확인");
        }

        // [MenuItem("Tools/Encyclopedia/씬에 UI 빌드 (에디터 전용)", true)]
        private static bool ValidateExecute() => !EditorApplication.isPlaying;
    }
}
