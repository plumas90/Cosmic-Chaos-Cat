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

            // Check if MainBackground already exists under canvas
            var existing = canvas.transform.Find("MainBackground");
            if (existing != null) return;

            // Create MainBackground GameObject
            var bgGO = new GameObject("MainBackground", typeof(RectTransform));
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetSiblingIndex(0); // Place at the very top (drawn at the back)

            // Setup RectTransform to stretch and fill screen
            var rt = bgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            // Add Image component
            var img = bgGO.AddComponent<Image>();
            img.color = new Color(0.08f, 0.10f, 0.16f, 1f); // Safe default dark color

            // Add MainBackgroundController component
            bgGO.AddComponent<MainBackgroundController>();

            // Mark scene dirty and save
            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[BackgroundSetup] Created and saved MainBackground in GameScene hierarchy!");
        }
    }
}
