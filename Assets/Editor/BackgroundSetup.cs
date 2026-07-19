using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    [InitializeOnLoad]
    public static class BackgroundSetup
    {
        static BackgroundSetup()
        {
            EditorApplication.delayCall += SetupBackground;
        }

        private static void SetupBackground()
        {
            if (EditorApplication.isPlaying) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "GameScene") return;

            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            bool dirty = false;

            // 1. Setup MainBackground if missing
            var existingBg = canvas.transform.Find("MainBackground");
            if (existingBg == null)
            {
                var bgGO = new GameObject("MainBackground", typeof(RectTransform));
                bgGO.transform.SetParent(canvas.transform, false);
                bgGO.transform.SetSiblingIndex(0); // Place at the very top (drawn at the back)

                var rt = bgGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);

                var img = bgGO.AddComponent<Image>();
                img.color = new Color(0.08f, 0.10f, 0.16f, 1f);

                bgGO.AddComponent<MainBackgroundController>();
                dirty = true;
                Debug.Log("[BackgroundSetup] Created MainBackground in GameScene hierarchy!");
            }

            // 2. Setup MainDecoration if missing
            var existingDeco = canvas.transform.Find("MainDecoration");
            if (existingDeco == null)
            {
                var decoGO = new GameObject("MainDecoration", typeof(RectTransform));
                decoGO.transform.SetParent(canvas.transform, false);
                // Place it right after MainBackground (index 1) so it sits behind panels but in front of bg
                decoGO.transform.SetSiblingIndex(1);

                var rt = decoGO.GetComponent<RectTransform>();
                // Anchor to bottom-right of screen
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(250f, 250f);
                rt.anchoredPosition = new Vector2(-50f, 50f); // Slight offset from corner

                var img = decoGO.AddComponent<Image>();
                img.color = Color.white;
                img.preserveAspect = true;
                img.raycastTarget = false; // By default, let's not block clicks unless needed

                decoGO.AddComponent<MainDecorationController>();
                dirty = true;
                Debug.Log("[BackgroundSetup] Created MainDecoration in GameScene hierarchy!");
            }

            if (dirty)
            {
                EditorUtility.SetDirty(canvas.gameObject);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
    }
}
