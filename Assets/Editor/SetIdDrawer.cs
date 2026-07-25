using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace CosmicChaosCat.Editor
{
    [CustomPropertyDrawer(typeof(SetIdAttribute))]
    public class SetIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            List<string> displayOptions = new List<string> { "None (세트 없음)" };
            List<string> idValues = new List<string> { "" };

            // Find all SetCatalogSO assets in the project
            string[] guids = AssetDatabase.FindAssets("t:SetCatalogSO");
            if (guids != null && guids.Length > 0)
            {
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var setCatalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>(path);
                    if (setCatalog != null && setCatalog.Sets != null)
                    {
                        foreach (var set in setCatalog.Sets)
                        {
                            if (set != null && !string.IsNullOrEmpty(set.SetId))
                            {
                                if (!idValues.Contains(set.SetId))
                                {
                                    string nameText = string.IsNullOrEmpty(set.SetName)
                                        ? set.SetId
                                        : $"{set.SetId} ({set.SetName})";
                                    displayOptions.Add(nameText);
                                    idValues.Add(set.SetId);
                                }
                            }
                        }
                    }
                }
            }

            string currentId = property.stringValue ?? "";
            int selectedIndex = idValues.IndexOf(currentId);

            if (selectedIndex < 0 && !string.IsNullOrEmpty(currentId))
            {
                // If current SetId isn't found in SetCatalogSO, keep it as unlisted option
                displayOptions.Add($"{currentId} (미등록 세트)");
                idValues.Add(currentId);
                selectedIndex = idValues.Count - 1;
            }
            else if (selectedIndex < 0)
            {
                selectedIndex = 0; // Default to None
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                property.stringValue = idValues[newIndex];
            }
        }
    }
}
