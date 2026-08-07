#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CosmicChaosCat.EditorTools
{
    [InitializeOnLoad]
    public static class CreateOrSyncCardCatalog
    {
        static CreateOrSyncCardCatalog()
        {
            EditorApplication.delayCall += EnsureAllCardsInCatalog;
        }

        [MenuItem("CosmicChaosCat/Sync All 174 Cards to CardCatalog.asset")]
        public static void EnsureAllCardsInCatalog()
        {
            string path = "Assets/ScriptableObjects/CardCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>(path);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<CardCatalogSO>();
                AssetDatabase.CreateAsset(catalog, path);
            }

            // Collect all card image assets from Assets/image/A_No/
            var cardImageFiles = new List<string>();
            string aNoDir = "Assets/image/A_No";
            if (Directory.Exists(aNoDir))
            {
                var files = Directory.GetFiles(aNoDir, "*.png", SearchOption.AllDirectories);
                foreach (var f in files)
                {
                    string name = Path.GetFileName(f);
                    // Match prefix like 0001_, 0152_, 170_, 171_, etc.
                    if (Regex.IsMatch(name, @"^\d{1,4}_"))
                    {
                        cardImageFiles.Add(f.Replace('\\', '/'));
                    }
                }
            }

            // Map card ID -> List of Sprites
            var cardSpritesMap = new Dictionary<string, List<Sprite>>();
            foreach (var filePath in cardImageFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                var match = Regex.Match(fileName, @"^(\d{1,4})_");
                if (!match.Success) continue;

                int num = int.Parse(match.Groups[1].Value);
                string cardId = num.ToString("D4");

                // Load all sub-sprites (in case of sprite sheet like 171_SR_long_neck_cat)
                var assets = AssetDatabase.LoadAllAssetsAtPath(filePath);
                var sprites = new List<Sprite>();
                foreach (var a in assets)
                {
                    if (a is Sprite sp) sprites.Add(sp);
                }

                if (sprites.Count == 0)
                {
                    var mainSp = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                    if (mainSp != null) sprites.Add(mainSp);
                }

                if (sprites.Count > 0)
                {
                    if (!cardSpritesMap.ContainsKey(cardId)) cardSpritesMap[cardId] = new List<Sprite>();
                    cardSpritesMap[cardId].AddRange(sprites);
                }
            }

            // Also load background sprites from Assets/image/A_Frame_bg
            var bgSpriteMap = new Dictionary<string, Sprite>();
            string aBgDir = "Assets/image/A_Frame_bg";
            if (Directory.Exists(aBgDir))
            {
                var bgFiles = Directory.GetFiles(aBgDir, "*.png", SearchOption.AllDirectories);
                foreach (var f in bgFiles)
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    var match = Regex.Match(name, @"^(\d{1,4})_");
                    if (match.Success)
                    {
                        int num = int.Parse(match.Groups[1].Value);
                        string cardId = num.ToString("D4");
                        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(f.Replace('\\', '/'));
                        if (sp != null && !bgSpriteMap.ContainsKey(cardId))
                            bgSpriteMap[cardId] = sp;
                    }
                }
            }

            var so = new SerializedObject(catalog);
            var cardsProp = so.FindProperty("cards");

            // Build map of existing cards in catalog
            var existingCards = new Dictionary<string, SerializedProperty>();
            for (int i = 0; i < cardsProp.arraySize; i++)
            {
                var elem = cardsProp.GetArrayElementAtIndex(i);
                string id = elem.FindPropertyRelative("Id").stringValue;
                if (!string.IsNullOrEmpty(id))
                {
                    // Normalize ID to 4 digits
                    if (int.TryParse(id, out int n)) id = n.ToString("D4");
                    existingCards[id] = elem;
                }
            }

            int addedCount = 0;

            // Ensure cards 0001 through 0174 exist
            for (int num = 1; num <= 174; num++)
            {
                string cardId = num.ToString("D4");
                if (!existingCards.ContainsKey(cardId))
                {
                    int newIdx = cardsProp.arraySize;
                    cardsProp.InsertArrayElementAtIndex(newIdx);
                    var elem = cardsProp.GetArrayElementAtIndex(newIdx);
                    elem.FindPropertyRelative("Id").stringValue = cardId;
                    
                    // Name & Rarity heuristics
                    string name = GetDefaultCardName(num);
                    int rarity = GetDefaultCardRarity(num);

                    elem.FindPropertyRelative("DisplayName").stringValue = name;
                    elem.FindPropertyRelative("DisplayName_EN").stringValue = name;
                    elem.FindPropertyRelative("Rarity").enumValueIndex = rarity;
                    elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
                    elem.FindPropertyRelative("ClickGold").doubleValue = 25d;
                    elem.FindPropertyRelative("ShardValue").intValue = 10;
                    elem.FindPropertyRelative("MaxStacks").intValue = 6;
                    elem.FindPropertyRelative("SetId").stringValue = GetSetIdForNumber(num);
                    elem.FindPropertyRelative("IsHidden").boolValue = false;
                    elem.FindPropertyRelative("IsShop").boolValue = false;
                    elem.FindPropertyRelative("Description").stringValue = $"{name} 고양이입니다.";
                    elem.FindPropertyRelative("Description_EN").stringValue = $"A {name} cat.";

                    existingCards[cardId] = elem;
                    addedCount++;
                }

                // Link Sprites for the card
                var cardElem = existingCards[cardId];
                if (cardSpritesMap.TryGetValue(cardId, out var sprites) && sprites.Count > 0)
                {
                    var mainSp = sprites[0];
                    cardElem.FindPropertyRelative("CardSprite").objectReferenceValue = mainSp;
                    
                    if (bgSpriteMap.TryGetValue(cardId, out var bgSp))
                        cardElem.FindPropertyRelative("GachaBgSprite").objectReferenceValue = bgSp;
                    else
                        cardElem.FindPropertyRelative("GachaBgSprite").objectReferenceValue = null;

                    // Breakthrough sprites setup
                    var btProp = cardElem.FindPropertyRelative("BreakthroughSprites");
                    btProp.ClearArray();
                    for (int s = 0; s < 5; s++)
                    {
                        btProp.InsertArrayElementAtIndex(s);
                        // Special rule for 170: 5th stage uses 2nd sprite if available
                        if (num == 170 && s == 4 && sprites.Count > 1)
                        {
                            btProp.GetArrayElementAtIndex(s).objectReferenceValue = sprites[1];
                        }
                        else
                        {
                            btProp.GetArrayElementAtIndex(s).objectReferenceValue = mainSp;
                        }
                    }
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateOrSyncCardCatalog] ✅ CardCatalog.asset successfully updated! Total cards: {cardsProp.arraySize} (Newly added: {addedCount})");
        }

        private static string GetDefaultCardName(int num)
        {
            switch (num)
            {
                case 152: return "Missing Cat";
                case 169: return "Buff Half Cat";
                case 170: return "Huh Cat";
                case 171: return "Long Neck Cat";
                case 172: return "OIIA OIIA Cat";
                case 173: return "Polite Cat Ollie";
                case 174: return "Pop Cat";
                default: return $"Card {num}";
            }
        }

        private static int GetDefaultCardRarity(int num)
        {
            if (num >= 169 && num <= 174)
            {
                if (num == 172 || num == 173) return 1; // R
                return 2; // SR
            }
            if (num == 152) return 2; // SR
            return 1;
        }

        private static string GetSetIdForNumber(int num)
        {
            if (num >= 1 && num <= 14) return "1";
            if (num >= 15 && num <= 28) return "2";
            if (num >= 29 && num <= 42) return "3";
            if (num >= 43 && num <= 56) return "4";
            if (num >= 57 && num <= 70) return "5";
            if (num >= 71 && num <= 84) return "6";
            if (num >= 85 && num <= 98) return "7";
            if (num >= 99 && num <= 112) return "8";
            if (num >= 113 && num <= 126) return "9";
            if (num >= 127 && num <= 139) return "10";
            if (num >= 140 && num <= 151) return "11";
            if (num >= 153 && num <= 154) return "11";
            if (num >= 155 && num <= 168) return "12";
            return "";
        }
    }
}
#endif
