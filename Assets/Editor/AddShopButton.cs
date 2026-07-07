using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace CosmicChaosCat.Editor
{
    public static class AddShopButton
    {
        [MenuItem("Tools/Add Shop Button and Panel")]
        public static void Execute()
        {
            var hud = Object.FindObjectOfType<GameHud>(true);
            if (hud == null)
            {
                Debug.LogError("GameHud not found.");
                return;
            }

            var canvas = hud.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 1. Create ShopPanel GameObject
            var shopPanelGO = new GameObject("ShopPanel");
            shopPanelGO.transform.SetParent(canvas.transform, false);
            var shopPanel = shopPanelGO.AddComponent<ShopPanel>();
            
            // Link to GameHud using SerializedObject
            var so = new SerializedObject(hud);
            so.Update();
            so.FindProperty("shopPanel").objectReferenceValue = shopPanel;
            so.ApplyModifiedProperties();

            shopPanelGO.SetActive(false);

            // 2. Create Shop Button at original bottom right
            // Find existing MenuButton (it was moved to TopLeft, so we just duplicate it or create a new one)
            var menuBtn = GameObject.Find("MenuButton");
            if (menuBtn != null)
            {
                var shopBtnGO = Object.Instantiate(menuBtn, menuBtn.transform.parent);
                shopBtnGO.name = "ShopButton";
                var rt = shopBtnGO.GetComponent<RectTransform>();
                // Bottom right anchoring
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-10, 10); // Standard padding

                var txt = shopBtnGO.GetComponentInChildren<TMP_Text>();
                if (txt != null) txt.text = "상점";

                var btn = shopBtnGO.GetComponent<Button>();
                if (btn != null)
                {
                    // Remove old listeners and add ToggleShop via UnityEvent
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                    UnityAction action = new UnityAction(hud.ToggleShop);
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
                }
            }

            // Also, update GachaPanel layout if needed, but it builds its UI dynamically anyway.
            // Same for ShopPanel, it builds its UI dynamically.

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("Shop Button and Panel added successfully!");
        }
    }
}
