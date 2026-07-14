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
            if (EditorApplication.isPlaying || Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Bake UI Prefabs", "플레이 모드(Play Mode) 중에는 UI를 구울(Bake) 수 없습니다.\n유니티 재생(Play)을 중단한 후에 다시 시도해 주세요.", "확인");
                return;
            }
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
            UnpackIfPrefab(encyPanel);

            // ── Force build UI for each ──────────────────────────────────────
            ForceBuildUI(gachaPanel);
            ForceBuildEncyclopediaPanel(encyPanel);

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


            if (encyPanel != null)
            {
                encyPanel.EnsureBreakthroughButtonBuilt();
                encyPanel.EnsureSetPageChildrenBuilt();   // SetNameTitle / ClaimBtn 하이라키에 미리 생성
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

        private static void ForceBuildEncyclopediaPanel(EncyclopediaPanel encyPanel)
        {
            if (encyPanel == null) return;

            // 1. We NEVER destroy existing hierarchy to preserve user's manual layout designs (Panel, NoPanel, Slot backgrounds, close button)

            // 2. Trigger auto-wiring to automatically connect missing SerializeFields to hierarchy instances
            var autoWireMethod = encyPanel.GetType().GetMethod("AutoWireFields",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (autoWireMethod != null)
            {
                autoWireMethod.Invoke(encyPanel, null);
            }

            // 3. Read Set tab fields
            var setPanelField = typeof(EncyclopediaPanel).GetField("setPanel",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var setPanelGO = setPanelField?.GetValue(encyPanel) as GameObject;

            var leftSetPageField = typeof(EncyclopediaPanel).GetField("leftSetPage",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var leftSetPageGO = leftSetPageField?.GetValue(encyPanel) as GameObject;

            // If SetPanel is missing entirely, build it under Panel
            if (setPanelGO == null)
            {
                var noPanelField = typeof(EncyclopediaPanel).GetField("noPanel",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var noPanelGO = noPanelField?.GetValue(encyPanel) as GameObject;
                Transform bookParent = noPanelGO != null ? noPanelGO.transform.parent : encyPanel.transform.Find("Panel");

                if (bookParent != null)
                {
                    var buildSetTabMethod = encyPanel.GetType().GetMethod("BuildSetTabArea",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (buildSetTabMethod != null)
                    {
                        buildSetTabMethod.Invoke(encyPanel, new object[] { bookParent });
                    }
                }
            }
            else if (leftSetPageGO == null)
            {
                // If setPanel exists but leftSetPage is null (stale single page), destroy setPanel and rebuild it
                Object.DestroyImmediate(setPanelGO);
                setPanelField?.SetValue(encyPanel, null);

                var noPanelField = typeof(EncyclopediaPanel).GetField("noPanel",
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                var noPanelGO = noPanelField?.GetValue(encyPanel) as GameObject;
                Transform bookParent = noPanelGO != null ? noPanelGO.transform.parent : encyPanel.transform.Find("Panel");

                if (bookParent != null)
                {
                    var buildSetTabMethod = encyPanel.GetType().GetMethod("BuildSetTabArea",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (buildSetTabMethod != null)
                    {
                        buildSetTabMethod.Invoke(encyPanel, new object[] { bookParent });
                    }
                }
            }

            // 4. Update listeners & breakthrough popup linkages without altering any layouts
            encyPanel.EnsureBreakthroughButtonBuilt();
            encyPanel.EnsureShopButtonCleanedUp();

            var bindMethod = encyPanel.GetType().GetMethod("BindListeners",
                BindingFlags.NonPublic | BindingFlags.Instance);
            bindMethod?.Invoke(encyPanel, null);

            // 5. Mark components dirty to ensure prefab serialization
            EditorUtility.SetDirty(encyPanel);
            if (encyPanel.gameObject != null)
            {
                EditorUtility.SetDirty(encyPanel.gameObject);
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
