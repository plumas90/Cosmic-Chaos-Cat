using UnityEngine;
using UnityEditor;
using System.Reflection;
using TMPro;
using UnityEngine.UI;

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

            // ── Load GameManager and its catalogs ────────────────────────────
            var gm = Object.FindObjectOfType<GameManager>(true);
            if (gm != null)
            {
                var awakeMethod = gm.GetType().GetMethod("Awake",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (awakeMethod != null) awakeMethod.Invoke(gm, null);
            }

            var gachaPanel = Object.FindObjectOfType<GachaPanel>(true);
            var shopPanel  = Object.FindObjectOfType<ShopPanel>(true);
            var encyPanel  = Object.FindObjectOfType<EncyclopediaPanel>(true);

            // ── Unpack existing prefabs to avoid recursive nested self-references ──
            UnpackIfPrefab(gachaPanel);
            UnpackIfPrefab(shopPanel);
            UnpackIfPrefab(encyPanel);

            // ── Create ShopPanel if missing ──────────────────────────────────
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

            // ── Inject GameManager into ShopPanel BEFORE building ────────────
            // This is critical: BuildUI() -> BuildUpgradesTab() needs gm to be set
            // because Awake() is not called in Edit Mode.
            if (shopPanel != null && gm != null)
            {
                var gmField = typeof(ShopPanel).GetField("gm",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                gmField?.SetValue(shopPanel, gm);
            }

            // ── Force build UI for each ──────────────────────────────────────
            ForceBuildUI(gachaPanel);
            ForceBuildUI(encyPanel);

            // ── Build Shop Button inside GameHud ─────────────────────────────
            var gameHud = Object.FindObjectOfType<GameHud>(true);
            if (gameHud != null)
            {
                var buildMethod = gameHud.GetType().GetMethod("EnsureShopButtonBuilt",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (buildMethod != null)
                {
                    buildMethod.Invoke(gameHud, null);
                    // Also bind button click to gameHud.ToggleShop for edit-time serialization
                    var shopBtnField = gameHud.GetType().GetField("shopButton",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    var shopBtn = shopBtnField?.GetValue(gameHud) as Button;
                    if (shopBtn != null)
                    {
                        shopBtn.onClick.RemoveAllListeners();
                        shopBtn.onClick.AddListener(gameHud.ToggleShop);
                        EditorUtility.SetDirty(shopBtn.gameObject);
                    }
                    EditorUtility.SetDirty(gameHud);
                    Debug.Log("[BakeUIPrefabs] ShopButton dynamically baked and saved into GameHud.");
                }
            }

            // ── Set ShopPanel Panel scale at bake time ───────────────────────
            // The Panel child holds the shop window. Scale is 1.5x so the shop
            // appears larger. Setting it here avoids any runtime scale changes.
            if (shopPanel != null)
            {
                var panelTrans = shopPanel.transform.Find("Panel");
                if (panelTrans != null)
                {
                    panelTrans.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    EditorUtility.SetDirty(panelTrans.gameObject);
                    Debug.Log("[BakeUIPrefabs] ShopPanel.Panel scale set to 1.5x.");
                }
                else
                {
                    Debug.LogWarning("[BakeUIPrefabs] ShopPanel 'Panel' child not found.");
                }
            }

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

            // ── Apply Galmuri9 font to all TMP texts in scene ────────────────
            var allTexts = Object.FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                txt.font = galmuriFont;
                EditorUtility.SetDirty(txt);
            }

            // ── Save as Prefabs ───────────────────────────────────────────────
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            SaveAsPrefab(gachaPanel?.gameObject, "Assets/Prefabs/GachaPanel.prefab");
            SaveAsPrefab(shopPanel?.gameObject,  "Assets/Prefabs/ShopPanel.prefab");
            SaveAsPrefab(encyPanel?.gameObject,  "Assets/Prefabs/EncyclopediaPanel.prefab");

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();

            Debug.Log("UI Prefabs baked and Font applied successfully!");
        }

        private static void ForceBuildUI(MonoBehaviour panel)
        {
            if (panel == null) return;
            var buildMethod = panel.GetType().GetMethod("BuildUI",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (buildMethod != null)
            {
                buildMethod.Invoke(panel, null);
                EditorUtility.SetDirty(panel);
            }
        }

        private static void SaveAsPrefab(GameObject go, string path)
        {
            if (go == null) return;
            PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.AutomatedAction);
        }

        private static void UnpackIfPrefab(MonoBehaviour panel)
        {
            if (panel == null) return;
            if (PrefabUtility.IsPartOfAnyPrefab(panel.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(panel.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log($"[BakeUIPrefabs] Unpacked {panel.gameObject.name} prefab instance to prevent nested corruption.");
            }
        }
    }
}
