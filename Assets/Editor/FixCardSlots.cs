using UnityEngine;
using UnityEditor;
using TMPro;
using System.Collections.Generic;
using CosmicChaosCat;

namespace CosmicChaosCat.Editor
{
    public class FixCardSlots : MonoBehaviour
    {
        [MenuItem("Tools/Fix Card Slots (Rename RareText & Delete StackText)")]
        public static void FixSlots()
        {
            var slots = Object.FindObjectsOfType<CardSlotUI>(true);
            int modifiedCount = 0;

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

                // Rename rarity text to RareText
                if (rarityText != null && rarityText.name != "RareText")
                {
                    rarityText.gameObject.name = "RareText";
                    EditorUtility.SetDirty(rarityText.gameObject);
                    changed = true;
                }

                // Delete stack text
                if (stackText != null && stackText.gameObject != null)
                {
                    Object.DestroyImmediate(stackText.gameObject);
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
    }
}
