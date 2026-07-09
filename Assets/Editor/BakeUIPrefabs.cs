using UnityEngine;
using UnityEditor;
using TMPro;

namespace CosmicChaosCat.Editor
{
    public static class BakeUIPrefabs
    {
        [MenuItem("Tools/Bake UI Prefabs and Apply Font")]
        public static void Execute()
        {
            var fontPath = "Assets/Font/Galmuri9 SDF.asset";
            var galmuriFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
            if (galmuriFont == null)
            {
                Debug.LogError($"Font not found at {fontPath}");
                return;
            }

            var gm = Object.FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                var awakeMethod = gm.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (awakeMethod != null) awakeMethod.Invoke(gm, null);
            }

            var gachaPanel = Object.FindObjectOfType<GachaPanel>(true);
            var shopPanel = Object.FindObjectOfType<ShopPanel>(true);
            var encyPanel = Object.FindObjectOfType<EncyclopediaPanel>(true);

            if (shopPanel == null)
            {
                var hud = Object.FindObjectOfType<GameHud>(true);
                if (hud != null)
                {
                    var canvas = hud.GetComponentInParent<Canvas>();
                    if (canvas == null) canvas = Object.FindObjectOfType<Canvas>();
                    if (canvas != null)
                    {
                        var go = new GameObject("ShopPanel", typeof(RectTransform));
                        go.transform.SetParent(canvas.transform, false);
                        shopPanel = go.AddComponent<ShopPanel>();
                        
                        var so = new SerializedObject(hud);
                        so.Update();
                        so.FindProperty("shopPanel").objectReferenceValue = shopPanel;
                        so.ApplyModifiedProperties();
                        
                        Debug.Log("[BakeUIPrefabs] Created and linked ShopPanel in scene dynamically.");
                    }
                }
            }

            // Force build UI for each
            ForceBuildUI(gachaPanel);
            ForceBuildUI(shopPanel);
            ForceBuildUI(encyPanel);

            if (encyPanel != null)
            {
                encyPanel.EnsureBreakthroughButtonBuilt();
                encyPanel.EnsureShopButtonCleanedUp();
                EditorUtility.SetDirty(encyPanel);
            }
            if (gachaPanel != null)
            {
                gachaPanel.EnsureGachaUIPartsBuilt();
                EditorUtility.SetDirty(gachaPanel);
            }

            // Apply font to everything in the active scene
            var allTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                txt.font = galmuriFont;
                EditorUtility.SetDirty(txt);
            }

            // Save as Prefabs
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            SaveAsPrefab(gachaPanel?.gameObject, "Assets/Prefabs/GachaPanel.prefab");
            SaveAsPrefab(shopPanel?.gameObject, "Assets/Prefabs/ShopPanel.prefab");
            SaveAsPrefab(encyPanel?.gameObject, "Assets/Prefabs/EncyclopediaPanel.prefab");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("UI Prefabs baked and Font applied successfully!");
        }

        private static void ForceBuildUI(MonoBehaviour panel)
        {
            if (panel == null) return;
            var buildMethod = panel.GetType().GetMethod("BuildUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (buildMethod != null)
            {
                buildMethod.Invoke(panel, null);
                
                // Now serialize the references using reflection, but wait!
                // It's much easier to just leave it as is. 
                // The fields are automatically serialized because we added [SerializeField], 
                // but since we assigned them via code, Unity might lose them if we don't mark dirty.
                EditorUtility.SetDirty(panel);
            }
        }

        private static void SaveAsPrefab(GameObject go, string path)
        {
            if (go == null) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }
    }
}
