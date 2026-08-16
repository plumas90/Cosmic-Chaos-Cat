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

        [MenuItem("CosmicChaosCat/Sync All Cards to CardCatalog.asset")]
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

            // Map card ID -> Stage Dict (stageNum -> Sprite)
            var cardStageMap = new Dictionary<string, SortedDictionary<int, Sprite>>();
            foreach (var filePath in cardImageFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                var match = Regex.Match(fileName, @"^(\d{1,4})_");
                if (!match.Success) continue;

                int num = int.Parse(match.Groups[1].Value);
                string cardId = num.ToString("D4");

                // 169's 2_3 / 4_5 sheets contain breakthrough animation slices.
                // They do not have a "stage" token in the filename, so the generic
                // parser would otherwise classify them as stage 1 and overwrite the
                // actual unevolved card image, depending on filesystem enumeration order.
                if (num == 169 &&
                    !fileName.Equals("169_SR_Buff_Half_Cat", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int stageNum = 1;
                var stageMatch = Regex.Match(fileName, @"stage_?(\d+)", RegexOptions.IgnoreCase);
                if (stageMatch.Success)
                {
                    stageNum = int.Parse(stageMatch.Groups[1].Value);
                }

                var assets = AssetDatabase.LoadAllAssetsAtPath(filePath);
                Sprite sprite = null;
                foreach (var a in assets)
                {
                    if (a is Sprite sp)
                    {
                        sprite = sp;
                        break;
                    }
                }
                if (sprite == null)
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(filePath);
                }

                if (sprite != null)
                {
                    if (!cardStageMap.ContainsKey(cardId))
                        cardStageMap[cardId] = new SortedDictionary<int, Sprite>();
                    
                    cardStageMap[cardId][stageNum] = sprite;
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
                if (cardStageMap.TryGetValue(cardId, out var stageDict) && stageDict.Count > 0)
                {
                    Sprite mainSp = stageDict.ContainsKey(1) ? stageDict[1] : null;
                    if (mainSp == null)
                    {
                        foreach (var kvp in stageDict) { mainSp = kvp.Value; break; }
                    }

                    cardElem.FindPropertyRelative("CardSprite").objectReferenceValue = mainSp;
                    
                    if (bgSpriteMap.TryGetValue(cardId, out var bgSp))
                        cardElem.FindPropertyRelative("GachaBgSprite").objectReferenceValue = bgSp;
                    else
                        cardElem.FindPropertyRelative("GachaBgSprite").objectReferenceValue = null;

                    // BreakthroughVariantStages setup (stages that have distinct sprites)
                    var varStagesProp = cardElem.FindPropertyRelative("BreakthroughVariantStages");
                    varStagesProp.ClearArray();
                    int stageIdx = 0;
                    foreach (var kvp in stageDict)
                    {
                        varStagesProp.InsertArrayElementAtIndex(stageIdx);
                        varStagesProp.GetArrayElementAtIndex(stageIdx).intValue = kvp.Key;
                        stageIdx++;
                    }

                    // BreakthroughSprites setup (5 elements for stages 1..5)
                    var btProp = cardElem.FindPropertyRelative("BreakthroughSprites");
                    btProp.ClearArray();
                    Sprite currentSp = mainSp;
                    for (int s = 0; s < 5; s++)
                    {
                        int targetStage = s + 1;
                        if (stageDict.TryGetValue(targetStage, out var stSp) && stSp != null)
                        {
                            currentSp = stSp;
                        }
                        btProp.InsertArrayElementAtIndex(s);
                        btProp.GetArrayElementAtIndex(s).objectReferenceValue = currentSp;
                    }
                }
            }

            addedCount += EnsureSpadeDeckCards(cardsProp, existingCards);
            addedCount += EnsureHwatuCards(cardsProp, existingCards);
            addedCount += EnsureHatCard(cardsProp, existingCards);
            addedCount += EnsureDogCatFistClashCards(cardsProp, existingCards);
            addedCount += EnsureChessCards(cardsProp, existingCards);
            addedCount += EnsureItIsCatCard(cardsProp, existingCards);
            addedCount += EnsureMisfortuneCard(cardsProp, existingCards);
            addedCount += EnsureHungryCard(cardsProp, existingCards);
            addedCount += EnsureYouIsCatCard(cardsProp, existingCards);
            addedCount += EnsureSoBrightThatCard(cardsProp, existingCards);
            addedCount += EnsurePortalCatCard(cardsProp, existingCards);
            addedCount += EnsurePacmanCatCard(cardsProp, existingCards);
            addedCount += EnsurePunchCatCard(cardsProp, existingCards);
            addedCount += EnsureSingleIllustrationCards(cardsProp, existingCards);
            addedCount += EnsureGiftCatCard(cardsProp, existingCards);
            addedCount += EnsureSeaSetCards(cardsProp, existingCards);
            addedCount += EnsureMicrowaveAndRainbowCards(cardsProp, existingCards);
            EnsureSeaSetCatalog();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateOrSyncCardCatalog] ✅ CardCatalog.asset successfully updated! Total cards: {cardsProp.arraySize} (Newly added: {addedCount})");
        }

        private static int EnsureSingleIllustrationCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            var cards = new[]
            {
                new[] { "0239", "Cheese",              "치즈 고양이",             "Cheese Cat" },
                new[] { "0240", "Elizabethan_collar", "넥카라 고양이",           "Elizabethan Collar Cat" },
                new[] { "0241", "cat_cat_cat",        "고양이 고양이 고양이",    "Cat Cat Cat" },
                new[] { "0242", "cheese_color",       "치즈색 고양이",           "Cheese-Colored Cat" },
                new[] { "0243", "cool_cat",           "쿨 고양이",               "Cool Cat" },
                new[] { "0244", "cool_dog",           "쿨 강아지",               "Cool Dog" },
                new[] { "0245", "half_cat",           "반쪽 고양이",             "Half Cat" },
                new[] { "0246", "hat_white",          "하얀 모자 고양이",        "White Hat Cat" },
                new[] { "0247", "hoeeee1",            "호에에 고양이 1",         "Hoeeee Cat 1" },
                new[] { "0248", "hoeeee2",            "호에에 고양이 2",         "Hoeeee Cat 2" },
                new[] { "0249", "honjong",            "혼종 고양이",             "Hybrid Cat" },
                new[] { "0250", "hot_cat",            "핫 캣",                   "Hot Cat" },
                new[] { "0251", "hot_dog",            "핫 도그",                 "Hot Dog" },
                new[] { "0252", "hug_me",             "안아줘 고양이",           "Hug Me Cat" },
                new[] { "0253", "hug_together",       "함께 안아 고양이",        "Hug Together Cat" },
                new[] { "0254", "long_nose",          "긴 코 고양이",            "Long-Nosed Cat" },
                new[] { "0255", "cloud_cat",          "구름 고양이",             "Cloud Cat" },
                new[] { "0256", "monocle_cat",        "외알 안경 고양이",        "Monocle Cat" },
                new[] { "0257", "neck_Black",         "검은 목 고양이",          "Black Neck Cat" },
                new[] { "0258", "nest_cat",           "둥지 고양이",             "Nest Cat" },
                new[] { "0259", "scary_cat",          "무서운 고양이",           "Scary Cat" },
                new[] { "0260", "skateboard_cat",     "스케이트보드 고양이",     "Skateboard Cat" }
            };

            const string directory = "Assets/image/A_No/239_260_single_cards";
            int added = 0;
            foreach (string[] card in cards)
            {
                string id = card[0];
                string imageName = card[1];
                Sprite sprite = LoadFirstSprite($"{directory}/{id}_R_{imageName}.png");
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing single-card sprite: {id}_R_{imageName}");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }

                elem.FindPropertyRelative("Id").stringValue = id;
                elem.FindPropertyRelative("DisplayName").stringValue = card[2];
                elem.FindPropertyRelative("DisplayName_EN").stringValue = card[3];
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"진화 일러스트가 없는 R 등급 싱글 카드, {card[2]}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"{card[3]}, an R-grade single-illustration card without evolution art.";
                ClearCardVariants(elem);
            }
            return added;
        }

        private static int EnsureSpadeDeckCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string spritePath = "Assets/image/A_No/202_215_spade_deck/202_215_R_SR_cat_playing_cards_Spade.png";
            var spriteByName = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                if (asset is Sprite sprite) spriteByName[sprite.name] = sprite;

            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            var deckSprites = new List<Sprite>();
            int added = 0;

            for (int i = 0; i < ranks.Length; i++)
            {
                string spriteRank = ranks[i] == "J" ? "11" : ranks[i];
                string spriteName = "cat_playing_cards_spade_" + spriteRank;
                if (!spriteByName.TryGetValue(spriteName, out var sprite))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Spade sprite: {spriteName}");
                    continue;
                }
                deckSprites.Add(sprite);

                string id = (202 + i).ToString("D4");
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                WriteSpadeCard(elem, id, ranks[i], sprite);
            }

            const string rewardId = "0215";
            bool rewardIsNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (rewardIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            WriteSpadeDeckReward(reward, deckSprites);
            return added;
        }

        private static int EnsureGiftCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0261";
            const string path = "Assets/image/A_No/261_gift_cat/0261_SR_gift_cat.png";
            // gift_cat_ is the second frame (its intended "1" suffix is absent in the slice name).
            string[] required = { "gift_cat_0", "gift_cat_", "gift_cat_2", "gift_cat_3" };
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;

            foreach (string spriteName in required)
                if (!sprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Gift Cat sprite: {spriteName}");
                    return 0;
                }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "선물 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Gift Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[required[0]];
            elem.FindPropertyRelative("Description").stringValue = "진화 없이 클릭할 때마다 네 가지 모습이 순서대로 반복되는 SR 등급 선물 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade Gift Cat that cycles through four sprites on click without evolution art.";
            ClearCardVariants(elem);
            var stored = elem.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = required.Length;
            for (int i = 0; i < required.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = sprites[required[i]];
            return isNew ? 1 : 0;
        }

        private static int EnsureHwatuCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string spritePath = "Assets/image/A_No/216_220_hawtu/216_220_R_hawtu.png";
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(spritePath))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;

            int added = 0;
            for (int number = 216; number <= 220; number++)
            {
                string id = number.ToString("D4");
                if (!sprites.TryGetValue("hawtu_" + number, out var sprite))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Hwatu sprite: hawtu_{number}");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }

                int index = number - 215;
                elem.FindPropertyRelative("Id").stringValue = id;
                elem.FindPropertyRelative("DisplayName").stringValue = $"화투 {index}";
                elem.FindPropertyRelative("DisplayName_EN").stringValue = $"Hwatu {index}";
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"화투 그림을 담은 R 등급 고양이 카드 {index}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"R-grade Hwatu cat card {index}.";
                ClearCardVariants(elem);
            }
            return added;
        }

        private static int EnsureSeaSetCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/262_266_sea_set";
            var members = new[]
            {
                new[] { "0262", "sea_cow",    "바다소",   "Sea Cow" },
                new[] { "0263", "sea_otter",  "해달",     "Sea Otter" },
                new[] { "0264", "sea_turtle", "바다거북", "Sea Turtle" },
                new[] { "0265", "seahorse",   "해마",     "Seahorse" }
            };

            int added = 0;
            foreach (string[] member in members)
            {
                string id = member[0];
                Sprite sprite = LoadNamedSprite($"{directory}/{id}_R_{member[1]}.png", member[1] + "_0");
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing sea-set sprite: {member[1]}_0");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                elem.FindPropertyRelative("Id").stringValue = id;
                elem.FindPropertyRelative("DisplayName").stringValue = member[2];
                elem.FindPropertyRelative("DisplayName_EN").stringValue = member[3];
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = "15";
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"바다 친구들 세트의 R 등급 카드, {member[2]}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"{member[3]}, an R-grade member of the Sea Friends set.";
                ClearCardVariants(elem);
                elem.FindPropertyRelative("EffectSprites").ClearArray();
            }

            const string rewardId = "0266";
            const string rewardPath = directory + "/0266_SR_sea_cat.png";
            string[] frameNames = { "sea_cat_0", "sea_cat_", "sea_cat_2", "sea_cat_3" };
            var rewardSprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(rewardPath))
                if (asset is Sprite sprite) rewardSprites[sprite.name] = sprite;
            foreach (string spriteName in frameNames)
                if (!rewardSprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Sea Cat frame: {spriteName}");
                    return added;
                }
            if (!rewardSprites.TryGetValue("sea_cat_bubble", out Sprite bubbleSprite))
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Sea Cat bubble sprite.");
                return added;
            }

            bool rewardIsNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (rewardIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            reward.FindPropertyRelative("Id").stringValue = rewardId;
            reward.FindPropertyRelative("DisplayName").stringValue = "바다 고양이";
            reward.FindPropertyRelative("DisplayName_EN").stringValue = "Sea Cat";
            reward.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("ClickGold").floatValue = 25f;
            reward.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            reward.FindPropertyRelative("MaxStacks").intValue = 6;
            reward.FindPropertyRelative("SetId").stringValue = string.Empty;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            reward.FindPropertyRelative("IsShop").boolValue = false;
            reward.FindPropertyRelative("CardSprite").objectReferenceValue = rewardSprites[frameNames[0]];
            reward.FindPropertyRelative("Description").stringValue = "바다 친구들 세트 보상입니다. 진화 없이 클릭할 때마다 네 가지 모습이 반복되며 입에서 버블을 내뿜습니다.";
            reward.FindPropertyRelative("Description_EN").stringValue = "The Sea Friends set reward. It cycles through four click frames and blows bubbles without evolution art.";
            ClearCardVariants(reward);
            var frames = reward.FindPropertyRelative("BreakthroughSprites");
            frames.arraySize = frameNames.Length;
            for (int i = 0; i < frameNames.Length; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue = rewardSprites[frameNames[i]];
            var effects = reward.FindPropertyRelative("EffectSprites");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = bubbleSprite;
            return added;
        }

        private static void EnsureSeaSetCatalog()
        {
            const string path = "Assets/ScriptableObjects/SetCatalog.asset";
            var catalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>(path);
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var sets = so.FindProperty("sets");
            SerializedProperty entry = null;
            for (int i = 0; i < sets.arraySize; i++)
            {
                var candidate = sets.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("SetId").stringValue == "15") { entry = candidate; break; }
            }
            if (entry == null)
            {
                sets.InsertArrayElementAtIndex(sets.arraySize);
                entry = sets.GetArrayElementAtIndex(sets.arraySize - 1);
            }
            entry.FindPropertyRelative("SetId").stringValue = "15";
            entry.FindPropertyRelative("SetName").stringValue = "바다 친구들";
            entry.FindPropertyRelative("SetName_EN").stringValue = "Sea Friends";
            entry.FindPropertyRelative("RewardGold").doubleValue = 0d;
            entry.FindPropertyRelative("RewardShards").intValue = 0;
            entry.FindPropertyRelative("CriticalChanceBonus").floatValue = 0f;
            entry.FindPropertyRelative("FlatIncomeBonus").doubleValue = 0d;
            entry.FindPropertyRelative("CriticalDamageBonus").floatValue = 0f;
            entry.FindPropertyRelative("GachaDiscountBonus").floatValue = 0f;
            entry.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1f;
            entry.FindPropertyRelative("RewardBackgroundId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardDecorationId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardCardId").stringValue = "0266";
            entry.FindPropertyRelative("EffectDesc").stringValue = "바다 친구 네 장을 모두 모으면 SR 등급 바다 고양이를 획득합니다.";
            entry.FindPropertyRelative("EffectDesc_EN").stringValue = "Collect all four Sea Friends to unlock the SR Sea Cat.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static int EnsureMicrowaveAndRainbowCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/267_268_rainbow";
            int added = 0;

            Sprite microwave = LoadNamedSprite(directory + "/0267_R_microwave.png", "microwave_0");
            if (microwave != null)
            {
                const string id = "0267";
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                elem.FindPropertyRelative("Id").stringValue = id;
                elem.FindPropertyRelative("DisplayName").stringValue = "전자레인지";
                elem.FindPropertyRelative("DisplayName_EN").stringValue = "Microwave";
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = microwave;
                elem.FindPropertyRelative("Description").stringValue = "진화 일러스트가 없는 일반 R 등급 전자레인지 카드입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = "A standard R-grade Microwave card without evolution art.";
                ClearCardVariants(elem);
                elem.FindPropertyRelative("EffectSprites").ClearArray();
            }

            const string rainbowId = "0268";
            const string buttonPath = directory + "/0268_SSR_rainbow_btn.png";
            const string catPath = directory + "/0268_SSR_rainbow_cat.png";
            Sprite buttonUp = LoadNamedSprite(buttonPath, "rainbow_btn_0");
            Sprite buttonDown = LoadNamedSprite(buttonPath, "rainbow_btn_1");
            var cats = new Sprite[7];
            for (int i = 0; i < cats.Length; i++)
                cats[i] = LoadNamedSprite(catPath, "rainbow_cat_" + i);
            bool complete = buttonUp != null && buttonDown != null;
            foreach (Sprite cat in cats) complete &= cat != null;
            if (!complete)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Rainbow Button or Rainbow Cat sprite.");
                return added;
            }

            bool rainbowIsNew = !existingCards.TryGetValue(rainbowId, out var rainbow);
            if (rainbowIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                rainbow = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rainbowId] = rainbow;
                added++;
            }
            rainbow.FindPropertyRelative("Id").stringValue = rainbowId;
            rainbow.FindPropertyRelative("DisplayName").stringValue = "무지개 버튼";
            rainbow.FindPropertyRelative("DisplayName_EN").stringValue = "Rainbow Button";
            rainbow.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SSR;
            rainbow.FindPropertyRelative("BaseWeight").floatValue = 4f;
            rainbow.FindPropertyRelative("ClickGold").floatValue = 125f;
            rainbow.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            rainbow.FindPropertyRelative("MaxStacks").intValue = 6;
            rainbow.FindPropertyRelative("SetId").stringValue = string.Empty;
            rainbow.FindPropertyRelative("IsHidden").boolValue = false;
            rainbow.FindPropertyRelative("IsShop").boolValue = false;
            rainbow.FindPropertyRelative("CardSprite").objectReferenceValue = buttonUp;
            rainbow.FindPropertyRelative("Description").stringValue = "클릭하면 왼쪽에서 무지개 고양이를 순서대로 발사하는 SSR 등급 버튼입니다. 화면 내 고양이 수는 한계돌파 단계마다 3마리씩 증가합니다.";
            rainbow.FindPropertyRelative("Description_EN").stringValue = "An SSR button that launches Rainbow Cats from the left in sequence. The on-screen limit increases by three per breakthrough stage.";
            ClearCardVariants(rainbow);
            var effects = rainbow.FindPropertyRelative("EffectSprites");
            effects.arraySize = 1 + cats.Length;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = buttonDown;
            for (int i = 0; i < cats.Length; i++)
                effects.GetArrayElementAtIndex(i + 1).objectReferenceValue = cats[i];
            return added;
        }

        private static int EnsureHatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0221";
            const string basePath = "Assets/image/A_No/221_hat/221_R_Hat.png";
            const string stage5Path = "Assets/image/A_No/221_hat/221_R_Hat_stage5.png";
            Sprite baseSprite = LoadFirstSprite(basePath);
            Sprite stage5Sprite = LoadFirstSprite(stage5Path);

            if (baseSprite == null || stage5Sprite == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Hat card sprite(s).");
                return 0;
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "모자 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Hat Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = baseSprite;
            elem.FindPropertyRelative("Description").stringValue = "한계돌파 5단계에서 모습이 바뀌는 R 등급 모자 고양이 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade Hat Cat card whose appearance changes at breakthrough stage 5.";
            ClearCardVariants(elem);

            var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
            stages.arraySize = 5;
            var sprites = elem.FindPropertyRelative("BreakthroughSprites");
            sprites.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = i == 4 ? stage5Sprite : baseSprite;
            }
            return isNew ? 1 : 0;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) return sprite;
            return null;
        }

        private static int EnsureDogCatFistClashCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/222_224_dog_cat_fist_clash/222_224_R_SR_dog_cat_fist_clash.png";
            var byName = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) byName[sprite.name] = sprite;

            if (!byName.TryGetValue("dog_cat_fist_clash_dog", out var dog) ||
                !byName.TryGetValue("dog_cat_fist_clash_cat", out var cat) ||
                !byName.TryGetValue("dog_cat_fist_clash_all", out var combined))
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Dog/Cat Fist Clash sprite(s).");
                return 0;
            }

            int added = 0;
            added += WriteFistClashCard(cardsProp, existingCards, "0222", "개의 주먹", "Dog Fist", dog);
            added += WriteFistClashCard(cardsProp, existingCards, "0223", "고양이의 주먹", "Cat Fist", cat);

            const string rewardId = "0224";
            bool rewardIsNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (rewardIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            reward.FindPropertyRelative("Id").stringValue = rewardId;
            reward.FindPropertyRelative("DisplayName").stringValue = "개와 고양이의 주먹 충돌";
            reward.FindPropertyRelative("DisplayName_EN").stringValue = "Dog & Cat Fist Clash";
            reward.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("ClickGold").floatValue = 25f;
            reward.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            reward.FindPropertyRelative("MaxStacks").intValue = 6;
            reward.FindPropertyRelative("SetId").stringValue = string.Empty;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            reward.FindPropertyRelative("IsShop").boolValue = false;
            reward.FindPropertyRelative("CardSprite").objectReferenceValue = combined;
            reward.FindPropertyRelative("Description").stringValue = "개와 고양이의 주먹 카드를 모아 완성한 SR 카드입니다. 클릭하면 두 주먹이 중앙에서 충돌합니다.";
            reward.FindPropertyRelative("Description_EN").stringValue = "An SR reward completed from the Dog Fist and Cat Fist cards. Click it to clash both fists at the center.";
            ClearCardVariants(reward);
            var rewardSprites = reward.FindPropertyRelative("BreakthroughSprites");
            rewardSprites.arraySize = 2;
            rewardSprites.GetArrayElementAtIndex(0).objectReferenceValue = dog;
            rewardSprites.GetArrayElementAtIndex(1).objectReferenceValue = cat;
            return added;
        }

        private static int WriteFistClashCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards,
            string id, string koName, string enName, Sprite sprite)
        {
            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = koName;
            elem.FindPropertyRelative("DisplayName_EN").stringValue = enName;
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = "14";
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            elem.FindPropertyRelative("Description").stringValue = "개와 고양이의 주먹 충돌 세트를 이루는 R 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade card from the Dog & Cat Fist Clash set.";
            ClearCardVariants(elem);
            return isNew ? 1 : 0;
        }

        private static int EnsureChessCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/225_230_chess/225_230_R_chess.png";
            string[] spriteNames = { "chess_pawn", "chess_knight", "chess_rock", "chess_king", "chess_bishop", "chess_queen" };
            string[] koNames = { "체스 폰", "체스 나이트", "체스 룩", "체스 킹", "체스 비숍", "체스 퀸" };
            string[] enNames = { "Chess Pawn", "Chess Knight", "Chess Rook", "Chess King", "Chess Bishop", "Chess Queen" };
            var byName = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) byName[sprite.name] = sprite;

            int added = 0;
            for (int i = 0; i < spriteNames.Length; i++)
            {
                if (!byName.TryGetValue(spriteNames[i], out var sprite))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Chess sprite: {spriteNames[i]}");
                    continue;
                }

                string id = (225 + i).ToString("D4");
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                elem.FindPropertyRelative("Id").stringValue = id;
                elem.FindPropertyRelative("DisplayName").stringValue = koNames[i];
                elem.FindPropertyRelative("DisplayName_EN").stringValue = enNames[i];
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"체스의 {koNames[i].Replace("체스 ", string.Empty)}을 표현한 R 등급 카드입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"An R-grade {enNames[i]} card.";
                ClearCardVariants(elem);
            }
            return added;
        }

        private static int EnsureItIsCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0231";
            Sprite sprite = LoadFirstSprite("Assets/image/A_No/231_it_is_cat/231_R_it_is_cat.png");
            if (sprite == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing It Is Cat sprite.");
                return 0;
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "고양이입니다";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "It Is Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            elem.FindPropertyRelative("Description").stringValue = "분명히 고양이인 R 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade card. It is definitely a cat.";
            ClearCardVariants(elem);
            return isNew ? 1 : 0;
        }

        private static int EnsureMisfortuneCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0232";
            const string path = "Assets/image/A_No/232_misfortune/232_SR_Misfortune.png";
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;

            string[] required = { "Misfortune_ladder_2", "Misfortune_cat", "Misfortune_cat2", "Misfortune_ladder_1" };
            foreach (string spriteName in required)
            {
                if (!sprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Misfortune sprite: {spriteName}");
                    return 0;
                }
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "불운";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Misfortune";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites["Misfortune_cat"];
            elem.FindPropertyRelative("Description").stringValue = "사다리 사이를 지나 화면을 순환하는 SR 등급 불운의 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade unlucky cat that walks between the ladder layers and loops across the screen.";
            ClearCardVariants(elem);

            var breakthroughSprites = elem.FindPropertyRelative("BreakthroughSprites");
            breakthroughSprites.arraySize = required.Length;
            for (int i = 0; i < required.Length; i++)
                breakthroughSprites.GetArrayElementAtIndex(i).objectReferenceValue = sprites[required[i]];
            return isNew ? 1 : 0;
        }

        private static int EnsureHungryCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0233";
            Sprite hungry1 = LoadFirstSprite("Assets/image/A_No/233_hungry/233_SR_Hungry_1.png");
            Sprite hungry2 = LoadFirstSprite("Assets/image/A_No/233_hungry/233_SR_Hungry_2.png");
            Sprite biscuit = LoadFirstSprite("Assets/image/A_No/233_hungry/233_SR_biscat.png");
            if (hungry1 == null || hungry2 == null || biscuit == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Hungry_1, Hungry_2, or biscat sprite.");
                return 0;
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "헝그리";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Hungry";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = hungry1;
            elem.FindPropertyRelative("Description").stringValue = "클릭하면 입 앞으로 비스켓이 떨어지고, 먹는 순간 표정이 변하는 SR 등급 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade cat that drops biscuits toward its mouth and reacts briefly when it eats one.";
            ClearCardVariants(elem);

            elem.FindPropertyRelative("BreakthroughVariantStages").ClearArray();
            var sprites = elem.FindPropertyRelative("BreakthroughSprites");
            sprites.arraySize = 2;
            sprites.GetArrayElementAtIndex(0).objectReferenceValue = hungry2;
            sprites.GetArrayElementAtIndex(1).objectReferenceValue = biscuit;
            return isNew ? 1 : 0;
        }

        private static int EnsureYouIsCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0234";
            const string path = "Assets/image/A_No/234_you_is_cat/234_R_You_is_cat.png";
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;

            string[] required = { "You_is_Cat1", "You_is_Cat2", "You_is_Cat3" };
            foreach (string spriteName in required)
            {
                if (!sprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing You Is Cat sprite: {spriteName}");
                    return 0;
                }
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "너는 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "You Is Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[required[0]];
            elem.FindPropertyRelative("Description").stringValue = "3단계와 5단계에서 모습이 변하는 R 등급 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade cat whose appearance changes at stages 3 and 5.";
            ClearCardVariants(elem);

            var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
            stages.arraySize = 3;
            stages.GetArrayElementAtIndex(0).intValue = 1;
            stages.GetArrayElementAtIndex(1).intValue = 3;
            stages.GetArrayElementAtIndex(2).intValue = 5;
            var breakthroughSprites = elem.FindPropertyRelative("BreakthroughSprites");
            breakthroughSprites.arraySize = 3;
            breakthroughSprites.GetArrayElementAtIndex(0).objectReferenceValue = sprites[required[0]];
            breakthroughSprites.GetArrayElementAtIndex(1).objectReferenceValue = sprites[required[1]];
            breakthroughSprites.GetArrayElementAtIndex(2).objectReferenceValue = sprites[required[2]];
            return isNew ? 1 : 0;
        }

        private static int EnsureSoBrightThatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0235";
            Sprite sprite = LoadFirstSprite("Assets/image/A_No/235_so_bright_that/235_R_so_bright_that.png");
            if (sprite == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing So Bright That sprite.");
                return 0;
            }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }

            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "너무 눈부셔서";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "So Bright That";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            elem.FindPropertyRelative("Description").stringValue = "너무 눈부셔서 바라볼 수밖에 없는 R 등급 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "A dazzling single-illustration R-grade cat.";
            ClearCardVariants(elem);
            return isNew ? 1 : 0;
        }

        private static int EnsurePortalCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0236";
            const string path = "Assets/image/A_No/236_portal_cat/236_SSR_Portal_cat.png";
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;

            string[] required =
            {
                "Portal_cat_0", "Portal_cat_1", "Portal_cat_2", "Portal_cat_3", "Portal_cat_4",
                "Portal_cat_portal_left", "Portal_cat_portal_right"
            };
            foreach (string spriteName in required)
                if (!sprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Portal Cat sprite: {spriteName}");
                    return 0;
                }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "포털 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Portal Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SSR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 4f;
            elem.FindPropertyRelative("ClickGold").floatValue = 125f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[required[0]];
            elem.FindPropertyRelative("Description").stringValue = "클릭할 때마다 포털과 함께 이동하며 현재 한계돌파 단계의 모습 중 하나로 변하는 SSR 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SSR card that teleports on every click and randomly assumes an appearance unlocked at its current breakthrough stage.";
            ClearCardVariants(elem);
            var stored = elem.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = required.Length;
            for (int i = 0; i < required.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = sprites[required[i]];
            return isNew ? 1 : 0;
        }

        private static int EnsurePacmanCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0237";
            const string path = "Assets/image/A_No/237_pacman_cat/237_SR_pacman_cat_made_by_me.png";
            var sprites = new Dictionary<string, Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites[sprite.name] = sprite;
            string[] required = { "pacman_cat_made_by_me1", "pacman_cat_made_by_me2" };
            foreach (string spriteName in required)
                if (!sprites.ContainsKey(spriteName))
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Pacman Cat sprite: {spriteName}");
                    return 0;
                }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "팩맨 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Pacman Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[required[0]];
            elem.FindPropertyRelative("Description").stringValue = "클릭할 때마다 두 모습이 반복되는 SR 등급 팩맨 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade Pacman Cat that alternates between two frames on every click.";
            ClearCardVariants(elem);
            var stored = elem.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = 2;
            stored.GetArrayElementAtIndex(0).objectReferenceValue = sprites[required[0]];
            stored.GetArrayElementAtIndex(1).objectReferenceValue = sprites[required[1]];
            return isNew ? 1 : 0;
        }

        private static int EnsurePunchCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0238";
            Sprite[] sprites =
            {
                LoadNamedSprite("Assets/image/A_No/238_punch_cat/238_SR_punch_cat_cat.png", "punch_cat_cat0"),
                LoadNamedSprite("Assets/image/A_No/238_punch_cat/238_SR_punch_cat_cat.png", "punch_cat_cat1"),
                LoadNamedSprite("Assets/image/A_No/238_punch_cat/238_SR_punch_cat_cat.png", "punch_cat_cat2"),
                LoadFirstSprite("Assets/image/A_No/238_punch_cat/238_SR_punch_cat_door.png"),
                LoadFirstSprite("Assets/image/A_No/238_punch_cat/238_SR_punch_cat_hole.png")
            };
            foreach (Sprite sprite in sprites)
                if (sprite == null)
                {
                    Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Punch Cat sprite.");
                    return 0;
                }

            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = "펀치 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Punch Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[0];
            elem.FindPropertyRelative("Description").stringValue = "문을 주먹으로 뚫는 SR 등급 고양이입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade cat that punches holes through a door.";
            ClearCardVariants(elem);
            var stored = elem.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            return isNew ? 1 : 0;
        }

        private static Sprite LoadNamedSprite(string path, string spriteName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite && sprite.name == spriteName) return sprite;
            return null;
        }

        private static void WriteSpadeCard(SerializedProperty elem, string id, string rank, Sprite sprite)
        {
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = $"스페이드 {rank}";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = $"Spade {rank}";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = "13";
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            elem.FindPropertyRelative("Description").stringValue = $"스페이드 덱을 이루는 R 등급 {rank} 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = $"The R-grade Spade {rank} card from the Spade Deck.";
            ClearCardVariants(elem);
        }

        private static void WriteSpadeDeckReward(SerializedProperty elem, List<Sprite> sprites)
        {
            elem.FindPropertyRelative("Id").stringValue = "0215";
            elem.FindPropertyRelative("DisplayName").stringValue = "스페이드 덱";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Spade Deck";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 0f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = true;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites.Count > 0 ? sprites[0] : null;
            elem.FindPropertyRelative("Description").stringValue = "스페이드 A부터 K까지 모두 모아 완성한 SR 등급 덱입니다. 클릭하면 덱을 섞고 맨 앞 카드가 무작위로 바뀝니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR deck completed by collecting every Spade card from A to K. Click it to shuffle and reveal a random card on top.";
            ClearCardVariants(elem);
            var breakthroughSprites = elem.FindPropertyRelative("BreakthroughSprites");
            breakthroughSprites.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                breakthroughSprites.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void ClearCardVariants(SerializedProperty elem)
        {
            elem.FindPropertyRelative("GachaBgSprite").objectReferenceValue = null;
            elem.FindPropertyRelative("BreakthroughVariantStages").ClearArray();
            elem.FindPropertyRelative("BreakthroughSprites").ClearArray();
            elem.FindPropertyRelative("BreakthroughSpriteVariants").ClearArray();
            elem.FindPropertyRelative("UseBreakthroughDescriptions").boolValue = false;
            elem.FindPropertyRelative("BreakthroughDescriptions").ClearArray();
            elem.FindPropertyRelative("BreakthroughDescriptions_EN").ClearArray();
            elem.FindPropertyRelative("SpecialEffect").enumValueIndex = (int)CardSpecialEffect.None;
            elem.FindPropertyRelative("SpecialEffectValue").floatValue = 0f;
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
