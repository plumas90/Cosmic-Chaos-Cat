using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace CosmicChaosCat
{
    [InitializeOnLoad]
    public static class SceneInitializer
    {
        static SceneInitializer()
        {
            EditorApplication.delayCall += InitializeScene;
        }

        private static void InitializeScene()
        {
            // Only run if not in play mode
            if (EditorApplication.isPlaying) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "GameScene") return;

            var hud = Object.FindObjectOfType<GameHud>();
            if (hud == null) return;

            var canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Check if CollectionPanel already exists under Canvas
            var existing = canvas.transform.Find("CollectionPanel");
            if (existing != null)
            {
                // Ensure it is assigned to GameHud if not already
                var serializedHud = new SerializedObject(hud);
                var prop = serializedHud.FindProperty("collectionPanel");
                if (prop != null && prop.objectReferenceValue == null)
                {
                    var existPanel = existing.GetComponent<CollectionPanel>();
                    if (existPanel != null)
                    {
                        prop.objectReferenceValue = existPanel;
                        serializedHud.ApplyModifiedProperties();
                        EditorUtility.SetDirty(hud);
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                        Debug.Log("[SceneInitializer] CollectionPanel reassigned to GameHud.");
                    }
                }
                return;
            }

            // Create CollectionPanel GameObject under Canvas
            var colGO = new GameObject("CollectionPanel", typeof(RectTransform));
            colGO.transform.SetParent(canvas.transform, false);
            colGO.SetActive(false); // Starts inactive

            // Setup RectTransform to be full screen
            var rt = colGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var colPanel = colGO.AddComponent<CollectionPanel>();
            colPanel.BuildUI();

            // Assign to GameHud serialization field
            var serHud = new SerializedObject(hud);
            var p = serHud.FindProperty("collectionPanel");
            if (p != null)
            {
                p.objectReferenceValue = colPanel;
                serHud.ApplyModifiedProperties();
            }

            // Mark dirty and save
            EditorUtility.SetDirty(hud);
            EditorUtility.SetDirty(canvas.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[SceneInitializer] Successfully created and wired CollectionPanel in the GameScene hierarchy!");
        }
    }
}
