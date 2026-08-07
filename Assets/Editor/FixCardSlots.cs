using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using CosmicChaosCat;

namespace CosmicChaosCat.Editor
{
    public class FixCardSlots : MonoBehaviour
    {
        // [MenuItem("Tools/Fix Card Slots (Rename RareText & Delete StackText)")]
        public static void FixSlots()
        {
            var slots = Object.FindObjectsOfType<CardSlotUI>(true);
            int modifiedCount = 0;

            // 1. Find Slot_0 and its RareText as the template
            TMP_Text templateRarityText = null;
            RectTransform templateRT = null;
            foreach (var slot in slots)
            {
                if (slot.gameObject.name == "Slot_0" || slot.gameObject.name == "Slot_0 (1)")
                {
                    var txts = slot.GetComponentsInChildren<TMP_Text>(true);
                    foreach (var t in txts)
                    {
                        if (t.name == "RareText" || t.name.ToLower().Contains("rarity") || t.name.ToLower().Contains("rare"))
                        {
                            templateRarityText = t;
                            templateRT = t.GetComponent<RectTransform>();
                            break;
                        }
                    }
                    if (templateRarityText != null) break;
                }
            }

            if (templateRarityText == null)
            {
                Debug.LogWarning("[FixCardSlots] Could not find Slot_0 or its RareText to use as a template. Aborting.");
                return;
            }

            // 2. Apply to all slots
            foreach (var slot in slots)
            {
                var txts = slot.GetComponentsInChildren<TMP_Text>(true);
                var cleanTxts = new List<TMP_Text>();
                
                TMP_Text rarityText = null;
                TMP_Text stackText = null;

                foreach (var t in txts)
                {
                    if (t.transform.parent != null && (t.transform.parent.name == "Unknown" || t.transform.parent.name == "Unk"))
                        continue;

                    string nameLower = t.name.ToLower();
                    
                    if (nameLower.Contains("rarity") || nameLower.Contains("rare"))
                        rarityText = t;
                    else if (nameLower.Contains("stack") || nameLower.Contains("count") || nameLower.Contains("copies"))
                        stackText = t;

                    cleanTxts.Add(t);
                }

                // If not found by name, fallback to index
                if (rarityText == null && cleanTxts.Count >= 2) rarityText = cleanTxts[1];
                if (stackText == null && cleanTxts.Count >= 3) stackText = cleanTxts[2];

                bool changed = false;

                // Rename and apply properties to rarity text
                if (rarityText != null)
                {
                    if (rarityText.name != "RareText")
                    {
                        Undo.RecordObject(rarityText.gameObject, "Rename RareText");
                        rarityText.gameObject.name = "RareText";
                        changed = true;
                    }

                    // Copy RectTransform properties
                    var rt = rarityText.GetComponent<RectTransform>();
                    if (rt != null && templateRT != null && slot.gameObject != templateRarityText.transform.parent.gameObject)
                    {
                        Undo.RecordObject(rt, "Copy RectTransform Properties");
                        rt.anchoredPosition = templateRT.anchoredPosition;
                        rt.sizeDelta = templateRT.sizeDelta;
                        rt.anchorMin = templateRT.anchorMin;
                        rt.anchorMax = templateRT.anchorMax;
                        rt.pivot = templateRT.pivot;
                        EditorUtility.SetDirty(rt);
                        changed = true;
                    }

                    // Copy and set custom Text properties (Color 128, 128, 253 & Bold style)
                    Undo.RecordObject(rarityText, "Set Color & Bold Style");
                    rarityText.color = new Color32(128, 128, 253, 255);
                    rarityText.fontStyle = FontStyles.Bold;
                    
                    if (slot.gameObject != templateRarityText.transform.parent.gameObject)
                    {
                        rarityText.fontSize = templateRarityText.fontSize;
                        rarityText.alignment = templateRarityText.alignment;
                    }
                    
                    EditorUtility.SetDirty(rarityText);
                    changed = true;
                }

                // Delete stack text
                if (stackText != null && stackText.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(stackText.gameObject);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(slot.gameObject);
                    modifiedCount++;
                }
            }

            if (modifiedCount > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                Debug.Log($"[FixCardSlots] Successfully fixed {modifiedCount} slots!");
            }
            else
            {
                Debug.Log("[FixCardSlots] No slots needed fixing.");
            }
        }

        // [MenuItem("Tools/Print Encyclopedia Hierarchy")]
        public static void PrintHierarchy()
        {
            var panel = Object.FindObjectOfType<EncyclopediaPanel>(true);
            if (panel == null)
            {
                Debug.LogWarning("[PrintHierarchy] Could not find EncyclopediaPanel in the scene.");
                return;
            }

            Debug.Log("[PrintHierarchy] Start printing hierarchy...");
            PrintChildrenRecursive(panel.transform, "");
            Debug.Log("[PrintHierarchy] Hierarchy print finished.");
        }

        // [MenuItem("Tools/Print Prefab Hierarchy")]
        public static void PrintPrefabHierarchy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EncyclopediaPanel.prefab");
            if (prefab == null)
            {
                Debug.LogWarning("[PrintHierarchy] Could not find Assets/Prefabs/EncyclopediaPanel.prefab.");
                return;
            }

            Debug.Log("[PrintHierarchy] Start printing PREFAB hierarchy...");
            PrintChildrenRecursive(prefab.transform, "");
            Debug.Log("[PrintHierarchy] PREFAB Hierarchy print finished.");
        }

        // [MenuItem("Tools/Print Detail Panel Children")]
        public static void PrintDetailPanelChildren()
        {
            var panel = Object.FindObjectOfType<EncyclopediaPanel>(true);
            if (panel == null) return;

            // Search for detail roots
            var noPanel = panel.transform.Find("Panel/NoPanel") ?? panel.transform.Find("NoPanel") ?? panel.transform.Find("Panel/NoTabRoot") ?? panel.transform.Find("NoTabRoot");
            var setPanel = panel.transform.Find("Panel/SetPanel") ?? panel.transform.Find("SetPanel");

            if (noPanel != null)
            {
                var detail = FindDetailPanelInTf(noPanel);
                if (detail != null)
                {
                    Debug.Log($"--- Children of NoPanel Detail Panel ({detail.name}) ---");
                    PrintChildrenDirect(detail.transform);
                }
            }

            if (setPanel != null)
            {
                var detail = FindDetailPanelInTf(setPanel);
                if (detail != null)
                {
                    Debug.Log($"--- Children of SetPanel Detail Panel ({detail.name}) ---");
                    PrintChildrenDirect(detail.transform);
                }
            }
        }

        private static Transform FindDetailPanelInTf(Transform container)
        {
            var t = FindChildRecursive(container, "DetailArt") ??
                    FindChildRecursive(container, "DetailImage") ??
                    FindChildRecursive(container, "DetailName") ??
                    FindChildRecursive(container, "DetailDesc") ??
                    FindChildRecursive(container, "DetailIncomeText") ??
                    FindChildRecursive(container, "Btn_장착하기") ??
                    FindChildRecursive(container, "Btn_한계 돌파");
            return t != null ? t.parent : null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void PrintChildrenDirect(Transform t)
        {
            foreach (Transform child in t)
            {
                var text = child.GetComponent<TMPro.TMP_Text>();
                var img = child.GetComponent<UnityEngine.UI.Image>();
                var btn = child.GetComponent<UnityEngine.UI.Button>();
                string compType = "";
                if (text != null) compType += $"TMP_Text(text={text.text}) ";
                if (img != null) compType += "Image ";
                if (btn != null) compType += "Button ";
                Debug.Log($"  - {child.name} (active={child.gameObject.activeSelf}) : {compType}");
                // print nested
                foreach (Transform sub in child)
                {
                    var stext = sub.GetComponent<TMPro.TMP_Text>();
                    string scomp = "";
                    if (stext != null) scomp += $"TMP_Text(text={stext.text}) ";
                    Debug.Log($"    - {sub.name} (active={sub.gameObject.activeSelf}) : {scomp}");
                }
            }
        }

        private static void PrintChildrenRecursive(Transform t, string indent)
        {
            Debug.Log($"{indent}- {t.name} ({t.gameObject.activeSelf})");
            foreach (Transform child in t)
            {
                PrintChildrenRecursive(child, indent + "  ");
            }
        }
    }
}
