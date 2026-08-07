using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace CosmicChaosCat.Editor
{
    public static class CreateCharacterSet1
    {
        // [MenuItem("Tools/Build Character Set 1 Cards")]
        public static void BuildSet1()
        {
            var cardCatalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            if (cardCatalog == null)
            {
                Debug.LogError("[CreateCharacterSet1] CardCatalog.asset not found at Assets/ScriptableObjects/CardCatalog.asset");
                return;
            }

            var setCatalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            if (setCatalog == null)
            {
                Debug.LogError("[CreateCharacterSet1] SetCatalog.asset not found at Assets/ScriptableObjects/SetCatalog.asset");
                return;
            }

            // 1. Ensure TextureImporter is set to Sprite for all PNGs in characters_set1
            string folderPath = "Assets/image/No/characters_set1";
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.SaveAndReimport();
                }
            }

            // 2. Prepare card entries 01 to 10
            var newCards = new List<CardEntry>();

            // 01: N - Cat in the Box
            newCards.Add(MakeCard(
                id: "01",
                name: "Cat in the Box",
                rarity: CardRarity.N,
                shardValue: CardShardValue.Value_1,
                setId: "1",
                spritePath: $"{folderPath}/01_N_Cat_in_the_Box.png"
            ));

            // 02: N - Tin Helmet Cat
            newCards.Add(MakeCard(
                id: "02",
                name: "Tin Helmet Cat",
                rarity: CardRarity.N,
                shardValue: CardShardValue.Value_1,
                setId: "1",
                spritePath: $"{folderPath}/02_N_tin_helmet_cat.png"
            ));

            // 03: N - Umbrella Cat
            newCards.Add(MakeCard(
                id: "03",
                name: "Umbrella Cat",
                rarity: CardRarity.N,
                shardValue: CardShardValue.Value_1,
                setId: "1",
                spritePath: $"{folderPath}/03_N_umbrella_cat.png"
            ));

            // 04: N - Fishbone Cat
            newCards.Add(MakeCard(
                id: "04",
                name: "Fishbone Cat",
                rarity: CardRarity.N,
                shardValue: CardShardValue.Value_1,
                setId: "1",
                spritePath: $"{folderPath}/04_N_fishbone_cat.png"
            ));

            // 05: R - Spray Cat
            newCards.Add(MakeCard(
                id: "05",
                name: "Spray Cat",
                rarity: CardRarity.R,
                shardValue: CardShardValue.Value_5,
                setId: "1",
                spritePath: $"{folderPath}/05_R_spray_cat.png"
            ));

            // 06: R - Midnight Delivery Cat
            newCards.Add(MakeCard(
                id: "06",
                name: "Midnight Delivery Cat",
                rarity: CardRarity.R,
                shardValue: CardShardValue.Value_5,
                setId: "1",
                spritePath: $"{folderPath}/06_R_midnight_delivery_cat.png"
            ));

            // 07: R - Padlock Cat
            newCards.Add(MakeCard(
                id: "07",
                name: "Padlock Cat",
                rarity: CardRarity.R,
                shardValue: CardShardValue.Value_5,
                setId: "1",
                spritePath: $"{folderPath}/07_R_padlock_cat.png"
            ));

            // 08: R - Pickpocket Cat
            newCards.Add(MakeCard(
                id: "08",
                name: "Pickpocket Cat",
                rarity: CardRarity.R,
                shardValue: CardShardValue.Value_5,
                setId: "1",
                spritePath: $"{folderPath}/08_R_pickpocket_cat.png"
            ));

            // 09: SR - Neon Hacker Cat (Stage 1, 2, 3 -> Breakthrough 1, 3, 5)
            newCards.Add(MakeBreakthroughCard(
                id: "09",
                name: "Neon Hacker Cat",
                rarity: CardRarity.SR,
                shardValue: CardShardValue.Value_10,
                setId: "1",
                spritePaths: new[]
                {
                    $"{folderPath}/09_SR_neon_hacker_cat_stage1.png",
                    $"{folderPath}/09_SR_neon_hacker_cat_stage2.png",
                    $"{folderPath}/09_SR_neon_hacker_cat_stage3.png"
                }
            ));

            // 10: SR - Pipe Boss Cat (Stage 1, 2, 3 -> Breakthrough 1, 3, 5)
            newCards.Add(MakeBreakthroughCard(
                id: "10",
                name: "Pipe Boss Cat",
                rarity: CardRarity.SR,
                shardValue: CardShardValue.Value_10,
                setId: "1",
                spritePaths: new[]
                {
                    $"{folderPath}/10_SR_pipe_boss_cat_stage1.png",
                    $"{folderPath}/10_SR_pipe_boss_cat_stage2.png",
                    $"{folderPath}/10_SR_pipe_boss_cat_stage3.png"
                }
            ));

            // Save to CardCatalogSO via SerializedObject
            var so = new SerializedObject(cardCatalog);
            var cardsProp = so.FindProperty("cards");
            cardsProp.ClearArray();

            for (int i = 0; i < newCards.Count; i++)
            {
                cardsProp.InsertArrayElementAtIndex(i);
                var elem = cardsProp.GetArrayElementAtIndex(i);
                var c = newCards[i];

                elem.FindPropertyRelative("Id").stringValue            = c.Id;
                elem.FindPropertyRelative("DisplayName").stringValue   = c.DisplayName;
                elem.FindPropertyRelative("Rarity").enumValueIndex     = (int)c.Rarity;
                elem.FindPropertyRelative("BaseWeight").floatValue     = c.BaseWeight;
                elem.FindPropertyRelative("ClickMultiplier").floatValue= c.ClickMultiplier;
                elem.FindPropertyRelative("ShardValue").intValue       = (int)c.ShardValue;
                elem.FindPropertyRelative("MaxStacks").intValue        = c.MaxStacks;
                elem.FindPropertyRelative("SetId").stringValue         = c.SetId;
                elem.FindPropertyRelative("IsHidden").boolValue        = c.IsHidden;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = c.CardSprite;

                var stagesProp = elem.FindPropertyRelative("BreakthroughVariantStages");
                stagesProp.ClearArray();
                if (c.BreakthroughVariantStages != null)
                {
                    for (int st = 0; st < c.BreakthroughVariantStages.Length; st++)
                    {
                        stagesProp.InsertArrayElementAtIndex(st);
                        stagesProp.GetArrayElementAtIndex(st).intValue = c.BreakthroughVariantStages[st];
                    }
                }

                var spritesProp = elem.FindPropertyRelative("BreakthroughSprites");
                spritesProp.ClearArray();
                if (c.BreakthroughSprites != null)
                {
                    for (int sp = 0; sp < c.BreakthroughSprites.Length; sp++)
                    {
                        spritesProp.InsertArrayElementAtIndex(sp);
                        spritesProp.GetArrayElementAtIndex(sp).objectReferenceValue = c.BreakthroughSprites[sp];
                    }
                }

                elem.FindPropertyRelative("SpecialEffect").enumValueIndex = (int)c.SpecialEffect;
                elem.FindPropertyRelative("SpecialEffectValue").floatValue = c.SpecialEffectValue;
                elem.FindPropertyRelative("Description").stringValue = c.Description;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cardCatalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateCharacterSet1] 01부터 10까지 총 {newCards.Count}개 카드가 CardCatalog.asset에 성공적으로 생성/등록되었습니다!");
        }

        private static Sprite LoadSprite(string path)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var asset in assets)
                {
                    if (asset is Sprite sprite) return sprite;
                }
            }
            return null;
        }

        private static CardEntry MakeCard(string id, string name, CardRarity rarity, CardShardValue shardValue, string setId, string spritePath)
        {
            var sprite = LoadSprite(spritePath);
            return new CardEntry
            {
                Id = id,
                DisplayName = name,
                Rarity = rarity,
                BaseWeight = 100f,
                ClickMultiplier = 1f,
                ShardValue = shardValue,
                MaxStacks = 6,
                SetId = setId,
                IsHidden = false,
                CardSprite = sprite,
                BreakthroughVariantStages = null,
                BreakthroughSprites = null,
                SpecialEffect = CardSpecialEffect.None,
                SpecialEffectValue = 0f,
                Description = "테스트1234"
            };
        }

        private static CardEntry MakeBreakthroughCard(string id, string name, CardRarity rarity, CardShardValue shardValue, string setId, string[] spritePaths)
        {
            var sprites = new Sprite[spritePaths.Length];
            for (int i = 0; i < spritePaths.Length; i++)
            {
                sprites[i] = LoadSprite(spritePaths[i]);
            }

            return new CardEntry
            {
                Id = id,
                DisplayName = name,
                Rarity = rarity,
                BaseWeight = 100f,
                ClickMultiplier = 1f,
                ShardValue = shardValue,
                MaxStacks = 6,
                SetId = setId,
                IsHidden = false,
                CardSprite = sprites.Length > 0 ? sprites[0] : null,
                BreakthroughVariantStages = new int[] { 1, 3, 5 },
                BreakthroughSprites = sprites,
                SpecialEffect = CardSpecialEffect.None,
                SpecialEffectValue = 0f,
                Description = "테스트1234"
            };
        }
    }
}
