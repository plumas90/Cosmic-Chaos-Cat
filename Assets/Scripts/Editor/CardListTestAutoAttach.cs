#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CosmicChaosCat.Editor
{
    /// <summary>
    /// Editor initializer that automatically attaches CardListTestButton component
    /// directly to card_list_test_btn in the scene, and pre-places control buttons in Editor!
    /// Safely guarded against Play Mode execution.
    /// </summary>
    [InitializeOnLoad]
    public static class CardListTestAutoAttach
    {
        static CardListTestAutoAttach()
        {
            EditorApplication.delayCall += EnsureAttachedInActiveScene;
            EditorSceneManager.sceneOpened += (scene, mode) => EnsureAttachedInActiveScene();
        }

        [MenuItem("Tools/CosmicChaosCat/Attach CardListTestButton To Scene Btn")]
        public static void EnsureAttachedInActiveScene()
        {
            // Do NOT execute scene dirty operations during Play Mode
            if (Application.isPlaying || EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;

            var allBtns = Object.FindObjectsOfType<Button>(true);
            GameObject testBtnGo = null;

            foreach (var b in allBtns)
            {
                if (b != null && b.name.ToLower().Contains("card_list_test"))
                {
                    testBtnGo = b.gameObject;
                    break;
                }
            }

            if (testBtnGo == null)
            {
                var allGos = Object.FindObjectsOfType<GameObject>(true);
                foreach (var go in allGos)
                {
                    if (go != null && go.name.ToLower().Contains("card_list_test"))
                    {
                        testBtnGo = go;
                        break;
                    }
                }
            }

            if (testBtnGo != null)
            {
                var component = testBtnGo.GetComponent<CardListTestButton>();
                if (component == null)
                {
                    component = Undo.AddComponent<CardListTestButton>(testBtnGo);
                    Debug.Log($"[CardListTestAutoAttach] ✅ CardListTestButton 인스펙터 컴포넌트가 '{testBtnGo.name}' 에디터 오브젝트에 자동으로 부착되었습니다!");
                }

                // Auto-wire AnimContainer
                var animContainerTrans = testBtnGo.transform.Find("AnimContainer")
                                      ?? testBtnGo.transform.Find("animcontainer")
                                      ?? testBtnGo.transform.Find("Animcontainer");

                if (animContainerTrans != null)
                {
                    var animContainerField = typeof(CardListTestButton).GetField("animContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (animContainerField != null)
                    {
                        animContainerField.SetValue(component, animContainerTrans.gameObject);
                    }
                }

                // Pre-place control buttons if needed (only in edit mode)
                component.CreatePreplacedControlButtonsInScene();

                EditorUtility.SetDirty(testBtnGo);
                EditorUtility.SetDirty(component);
                EditorSceneManager.MarkSceneDirty(testBtnGo.scene);
            }
        }
    }
}
#endif
