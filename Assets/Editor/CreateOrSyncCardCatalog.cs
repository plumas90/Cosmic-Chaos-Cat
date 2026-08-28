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
            addedCount += EnsureReadySingleCards(cardsProp, existingCards);
            addedCount += EnsureReadySplitCards(cardsProp, existingCards);
            addedCount += EnsureOOCards(cardsProp, existingCards);
            addedCount += EnsureFlyCatCard(cardsProp, existingCards);
            addedCount += EnsureAppleHeadAndSoupCards(cardsProp, existingCards);
            addedCount += EnsureClickSingleCards(cardsProp, existingCards);
            addedCount += EnsureFurryCards(cardsProp, existingCards);
            addedCount += EnsureReadyRCardBatch(cardsProp, existingCards);
            addedCount += EnsureCatMonsterSetCards(cardsProp, existingCards);
            addedCount += EnsureReadyCards312To318(cardsProp, existingCards);
            addedCount += EnsureBlackEyesCard(cardsProp, existingCards);
            // Ready batch 0320-0322 is synced after its sliced sprites finish importing.
            addedCount += EnsureReadyCards320To322(cardsProp, existingCards);
            addedCount += EnsureCatClawUpgradeCard(cardsProp, existingCards);
            addedCount += EnsureCatTowerCard(cardsProp, existingCards);
            addedCount += EnsureBallOfYarnTouchCard(cardsProp, existingCards);
            addedCount += EnsureBurgerMakerCard(cardsProp, existingCards);
            addedCount += EnsureCatInTheBoxCard(cardsProp, existingCards);
            addedCount += EnsureCatonatoSetCards(cardsProp, existingCards);
            addedCount += EnsureFiftyBillionRainbowsCard(cardsProp, existingCards);
            addedCount += EnsureSurpriseCatCard(cardsProp, existingCards);
            addedCount += EnsureTailAnimationCard(cardsProp, existingCards);
            addedCount += EnsureRedFishHeadCatCard(cardsProp, existingCards);
            addedCount += EnsureTrinitySetCards(cardsProp, existingCards);
            addedCount += EnsureHairballCard(cardsProp, existingCards);
            addedCount += EnsureWarigari2Card(cardsProp, existingCards);
            addedCount += EnsureTasteCard(cardsProp, existingCards);
            addedCount += EnsureThrowCard(cardsProp, existingCards);
            addedCount += EnsureHumunStreetEarthCard(cardsProp, existingCards);
            addedCount += EnsureCatIsWaterSetCards(cardsProp, existingCards);
            addedCount += EnsureCheersCard(cardsProp, existingCards);
            addedCount += EnsureCthulhuCard(cardsProp, existingCards);
            addedCount += EnsureMatryosikaCard(cardsProp, existingCards);
            addedCount += EnsureServalCard(cardsProp, existingCards);
            addedCount += EnsureWalkingWalkwalkCard(cardsProp, existingCards);
            addedCount += EnsureStretchCard(cardsProp, existingCards);
            EnsureSeaSetCatalog();
            EnsureCatMonsterSetCatalog();
            EnsureCatonatoSetCatalog();
            EnsureTrinitySetCatalog();
            EnsureCatIsWaterSetCatalog();
            EnsureCatWheelDecoration();
            EnsureCatTowerDecoration();
            EnsureSharkDecorations();
            EnsureThrowObjectDecorations();

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CreateOrSyncCardCatalog] ✅ CardCatalog.asset successfully updated! Total cards: {cardsProp.arraySize} (Newly added: {addedCount})");
        }

        private static int EnsureReadySingleCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            var cards = new[]
            {
                new[] { "0269", "saba.png",              "고등어 고양이",        "Mackerel Cat" },
                new[] { "0270", "three_color.png",       "삼색 고양이",          "Calico Cat" },
                new[] { "0271", "thunder_cat1.png",      "선더캣",                "Thunder Cat" },
                new[] { "0272", "saba_wheel.png",        "동그란 고등어 고양이", "Curled Mackerel Cat" },
                new[] { "0273", "three_color_wheel.png", "동그란 삼색 고양이",   "Curled Calico Cat" }
            };

            const string directory = "Assets/image/A_No/269_273_single_cards";
            int added = 0;
            foreach (string[] card in cards)
            {
                Sprite sprite = LoadFirstSprite($"{directory}/{card[1]}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing ready-card sprite: {card[1]}");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(card[0], out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[card[0]] = elem;
                    added++;
                }

                bool isThunderCat = card[0] == "0271";
                elem.FindPropertyRelative("Id").stringValue = card[0];
                elem.FindPropertyRelative("DisplayName").stringValue = card[2];
                elem.FindPropertyRelative("DisplayName_EN").stringValue = card[3];
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)(isThunderCat ? CardRarity.SR : CardRarity.R);
                elem.FindPropertyRelative("BaseWeight").floatValue = isThunderCat ? 10f : 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = isThunderCat ? 25f : 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)(isThunderCat ? CardShardValue.Value_5 : CardShardValue.Value_3);
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = isThunderCat
                    ? "클릭한 발끝에서 번개가 사방으로 뻗는 SR 등급 선더캣입니다."
                    : $"진화 일러스트가 없는 R 등급 싱글 카드, {card[2]}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = isThunderCat
                    ? "An SR-grade Thunder Cat that fires lightning in every direction from the clicked paw."
                    : $"{card[3]}, an R-grade single-illustration card without evolution art.";
                ClearCardVariants(elem);
                elem.FindPropertyRelative("EffectSprites").ClearArray();
            }
            return added;
        }

        private static int EnsureReadySplitCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            var cards = new[]
            {
                new[] { "0274", "long_cat_ori.png",         "long_cat_ori_0",                 "롱캣",          "Long Cat" },
                new[] { "0275", "bread_and_cat.png",        "bread_cat",                      "식빵 고양이",   "Bread Cat" },
                new[] { "0276", "bread_and_cat.png",        "bread_bread",                    "식빵",          "Bread" },
                new[] { "0277", "muffin_and_chihuahua.png", "muffin_and_chihuahua_chihuahua", "치와와",        "Chihuahua" },
                new[] { "0278", "muffin_and_chihuahua.png", "muffin_and_chihuahua_muffin",    "머핀",          "Muffin" }
            };

            const string directory = "Assets/image/A_No/274_278_single_cards";
            int added = 0;
            foreach (string[] card in cards)
            {
                Sprite sprite = LoadNamedSprite($"{directory}/{card[1]}", card[2]);
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing split-card sprite: {card[1]} / {card[2]}");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(card[0], out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[card[0]] = elem;
                    added++;
                }

                elem.FindPropertyRelative("Id").stringValue = card[0];
                elem.FindPropertyRelative("DisplayName").stringValue = card[3];
                elem.FindPropertyRelative("DisplayName_EN").stringValue = card[4];
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = string.Empty;
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"진화 일러스트가 없는 R 등급 싱글 카드, {card[3]}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"{card[4]}, an R-grade single-illustration card without evolution art.";
                ClearCardVariants(elem);
            }
            return added;
        }

        private static int EnsureOOCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/279_280_oo_cats";
            int added = 0;

            Sprite rollingSprite = LoadNamedSprite(directory + "/OO_cat.png", "OO_cat_1");
            if (rollingSprite != null)
            {
                const string id = "0279";
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                WriteOOSingleCard(elem, id, "오오 고양이", "OO Cat", CardRarity.SR, rollingSprite);
                elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
                elem.FindPropertyRelative("ClickGold").floatValue = 25f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
                elem.FindPropertyRelative("Description").stringValue = "자동으로 시계 방향 회전하며 화면 오른쪽으로 굴러가는 SR 등급 고양이입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade OO Cat that automatically rolls clockwise toward the right side of the screen.";
            }
            else Debug.LogWarning("[CreateOrSyncCardCatalog] Missing OO_cat_1 sprite.");

            Sprite baseSprite = LoadNamedSprite(directory + "/OO_cat_ss.png", "OO_cat_ss_0");
            Sprite stageThree = LoadNamedSprite(directory + "/OO_cat_ss_three.png", "OO_cat_ss_three_0");
            Sprite stageFive = LoadNamedSprite(directory + "/OO_cat_ss_genga.png", "OO_cat_ss_genga_0");
            if (baseSprite != null && stageThree != null && stageFive != null)
            {
                const string id = "0280";
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                WriteOOSingleCard(elem, id, "오오 고양이 SS", "OO Cat SS", CardRarity.R, baseSprite);
                var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
                stages.arraySize = 3;
                stages.GetArrayElementAtIndex(0).intValue = 1;
                stages.GetArrayElementAtIndex(1).intValue = 3;
                stages.GetArrayElementAtIndex(2).intValue = 5;
                var sprites = elem.FindPropertyRelative("BreakthroughSprites");
                sprites.arraySize = 5;
                sprites.GetArrayElementAtIndex(0).objectReferenceValue = baseSprite;
                sprites.GetArrayElementAtIndex(1).objectReferenceValue = baseSprite;
                sprites.GetArrayElementAtIndex(2).objectReferenceValue = stageThree;
                sprites.GetArrayElementAtIndex(3).objectReferenceValue = stageThree;
                sprites.GetArrayElementAtIndex(4).objectReferenceValue = stageFive;
                elem.FindPropertyRelative("Description").stringValue = "3단계에서 Three, 5단계에서 Genga 모습으로 진화하는 R 등급 고양이입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade OO Cat SS that evolves to Three at stage 3 and Genga at stage 5.";
            }
            else Debug.LogWarning("[CreateOrSyncCardCatalog] Missing one or more OO Cat SS evolution sprites.");

            return added;
        }

        private static void WriteOOSingleCard(
            SerializedProperty elem, string id, string koreanName, string englishName,
            CardRarity rarity, Sprite sprite)
        {
            elem.FindPropertyRelative("Id").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = koreanName;
            elem.FindPropertyRelative("DisplayName_EN").stringValue = englishName;
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)rarity;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            ClearCardVariants(elem);
        }

        private static int EnsureFlyCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0281";
            const string directory = "Assets/image/A_No/281_fly_cat";
            Sprite stageOne = LoadNamedSprite(directory + "/fly_cat_shoot1.png", "fly_cat_shoot1_0");
            Sprite stageThree = LoadNamedSprite(directory + "/fly_cat_shoot2.png", "fly_cat_shoot2_0");
            Sprite stageFive = LoadNamedSprite(directory + "/fly_cat_shoot3.png", "fly_cat_shoot3_0");
            var shootFour = LoadSpritesSorted(directory + "/fly_cat_shoot4.png");
            var shootFive = LoadSpritesSorted(directory + "/fly_cat_shoot5.png");
            if (stageOne == null || stageThree == null || stageFive == null ||
                shootFour.Count == 0 || shootFive.Count == 0)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Fly Cat stage or projectile sprites.");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "플라이 캣";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Fly Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SSR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 4f;
            elem.FindPropertyRelative("ClickGold").floatValue = 125f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = stageOne;
            elem.FindPropertyRelative("Description").stringValue = "3단계부터 클릭할 때 무작위 고양이가 좌하단으로 떨어지고, 5단계부터 순차 색상 고양이가 추가로 떨어지는 SSR 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SSR Fly Cat that rains random cats from stage 3 and adds sequential colored cats from stage 5.";
            ClearCardVariants(elem);

            var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
            stages.arraySize = 3;
            stages.GetArrayElementAtIndex(0).intValue = 1;
            stages.GetArrayElementAtIndex(1).intValue = 3;
            stages.GetArrayElementAtIndex(2).intValue = 5;
            var stageSprites = elem.FindPropertyRelative("BreakthroughSprites");
            stageSprites.arraySize = 5;
            stageSprites.GetArrayElementAtIndex(0).objectReferenceValue = stageOne;
            stageSprites.GetArrayElementAtIndex(1).objectReferenceValue = stageOne;
            stageSprites.GetArrayElementAtIndex(2).objectReferenceValue = stageThree;
            stageSprites.GetArrayElementAtIndex(3).objectReferenceValue = stageThree;
            stageSprites.GetArrayElementAtIndex(4).objectReferenceValue = stageFive;

            var effects = elem.FindPropertyRelative("EffectSprites");
            effects.arraySize = shootFour.Count + shootFive.Count;
            for (int i = 0; i < shootFour.Count; i++)
                effects.GetArrayElementAtIndex(i).objectReferenceValue = shootFour[i];
            for (int i = 0; i < shootFive.Count; i++)
                effects.GetArrayElementAtIndex(shootFour.Count + i).objectReferenceValue = shootFive[i];
            elem.FindPropertyRelative("EffectSpriteGroupSplit").intValue = shootFour.Count;
            return isNew ? 1 : 0;
        }

        private static List<Sprite> LoadSpritesSorted(string path)
        {
            var sprites = new List<Sprite>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite) sprites.Add(sprite);
            sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return sprites;
        }

        private static int EnsureAppleHeadAndSoupCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/282_287_apple_soup";
            int added = 0;
            var appleSprites = LoadSpritesSorted(directory + "/apple head.png");
            for (int i = 0; i < appleSprites.Count; i++)
            {
                string id = (282 + i).ToString("D4");
                bool isNew = !existingCards.TryGetValue(id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = elem;
                    added++;
                }
                string koreanName = $"애플 헤드 {i + 1}";
                string englishName = $"Apple Head {i + 1}";
                WriteOOSingleCard(elem, id, koreanName, englishName, CardRarity.R, appleSprites[i]);
                elem.FindPropertyRelative("Description").stringValue = $"과일 모자를 쓴 R 등급 싱글 카드, {koreanName}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"{englishName}, an R-grade single fruit-hat card.";
            }
            if (appleSprites.Count != 5)
                Debug.LogWarning($"[CreateOrSyncCardCatalog] Expected 5 Apple Head sprites, found {appleSprites.Count}.");

            Sprite[] stages =
            {
                LoadNamedSprite(directory + "/cat_shower_not_soup.png", "cat_shower_not_soup_0"),
                LoadNamedSprite(directory + "/cat_shower_not_soup.png", "cat_shower_not_soup_1"),
                LoadNamedSprite(directory + "/cat_shower_not_soup.png", "cat_shower_not_soup_2"),
                LoadNamedSprite(directory + "/cat_shower_not_soup.png", "cat_shower_not_soup_3"),
                LoadNamedSprite(directory + "/cat_shower_not_soup2.png", "cat_shower_not_soup2_0")
            };
            Sprite finalToggle = LoadNamedSprite(directory + "/cat_shower_not_soup2.png", "cat_shower_not_soup2_1");
            bool soupComplete = finalToggle != null;
            foreach (Sprite sprite in stages) soupComplete &= sprite != null;
            if (!soupComplete)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Cat Shower stage or final toggle sprite.");
                return added;
            }

            const string soupId = "0287";
            bool soupIsNew = !existingCards.TryGetValue(soupId, out var soup);
            if (soupIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                soup = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[soupId] = soup;
                added++;
            }
            WriteOOSingleCard(soup, soupId, "수프 아닌 고양이 샤워", "Cat Shower, Not Soup", CardRarity.SSR, stages[0]);
            soup.FindPropertyRelative("BaseWeight").floatValue = 4f;
            soup.FindPropertyRelative("ClickGold").floatValue = 125f;
            soup.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            soup.FindPropertyRelative("Description").stringValue = "단계마다 수프 재료가 늘어나며, 5단계에서 클릭할 때마다 두 완성 모습이 반복되는 SSR 카드입니다.";
            soup.FindPropertyRelative("Description_EN").stringValue = "An SSR cat whose ingredients increase each stage and whose two final forms alternate on click at stage 5.";
            var variantStages = soup.FindPropertyRelative("BreakthroughVariantStages");
            variantStages.arraySize = 5;
            var breakthroughSprites = soup.FindPropertyRelative("BreakthroughSprites");
            breakthroughSprites.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                variantStages.GetArrayElementAtIndex(i).intValue = i + 1;
                breakthroughSprites.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
            }
            var groups = soup.FindPropertyRelative("BreakthroughSpriteVariants");
            groups.arraySize = 1;
            var finalGroup = groups.GetArrayElementAtIndex(0);
            finalGroup.FindPropertyRelative("Stage").intValue = 5;
            var finalSprites = finalGroup.FindPropertyRelative("Sprites");
            finalSprites.arraySize = 2;
            finalSprites.GetArrayElementAtIndex(0).objectReferenceValue = stages[4];
            finalSprites.GetArrayElementAtIndex(1).objectReferenceValue = finalToggle;
            return added;
        }

        private static int EnsureClickSingleCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/288_289_click_singles";
            var definitions = new[]
            {
                new { Id = "0288", File = "Elizabethan collar_sr.png", Korean = "넥카라 고양이 SR", English = "Elizabethan Collar Cat SR", Rarity = CardRarity.SR },
                new { Id = "0289", File = "fly_toast.png", Korean = "플라이 토스트", English = "Fly Toast", Rarity = CardRarity.SSR }
            };
            int added = 0;
            foreach (var definition in definitions)
            {
                var sprites = LoadSpritesSorted($"{directory}/{definition.File}");
                if (sprites.Count < 2)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing click sprites for {definition.File}.");
                    continue;
                }

                bool isNew = !existingCards.TryGetValue(definition.Id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[definition.Id] = elem;
                    added++;
                }
                WriteOOSingleCard(elem, definition.Id, definition.Korean, definition.English, definition.Rarity, sprites[0]);
                bool isSsr = definition.Rarity == CardRarity.SSR;
                elem.FindPropertyRelative("BaseWeight").floatValue = isSsr ? 4f : 10f;
                elem.FindPropertyRelative("ClickGold").floatValue = isSsr ? 125f : 25f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)(isSsr ? CardShardValue.Value_10 : CardShardValue.Value_5);
                elem.FindPropertyRelative("Description").stringValue = isSsr
                    ? "진화 없이 클릭할 때마다 모습이 무작위로 바뀌는 SSR 등급 플라이 토스트입니다."
                    : $"진화 없이 클릭할 때마다 {sprites.Count}가지 모습이 순서대로 반복되는 SR 등급 넥카라 고양이입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = isSsr
                    ? "An SSR Fly Toast whose appearance changes randomly on every click without evolution."
                    : $"An SR Elizabethan Collar Cat whose {sprites.Count} appearances cycle on click without evolution.";
                var clickSprites = elem.FindPropertyRelative("BreakthroughSprites");
                clickSprites.arraySize = sprites.Count;
                for (int i = 0; i < sprites.Count; i++)
                    clickSprites.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
            return added;
        }

        private static int EnsureFurryCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/290_291_furry";
            var furryNot = LoadSpritesSorted(directory + "/furry_not.png");
            furryNot.AddRange(LoadSpritesSorted(directory + "/furry_not_2.png"));
            var furryYes = LoadSpritesSorted(directory + "/furry_yes.png");
            var definitions = new[]
            {
                new { Id = "0290", Korean = "복슬복슬 아님", English = "Furry Not", Sprites = furryNot },
                new { Id = "0291", Korean = "복슬복슬 맞음", English = "Furry Yes", Sprites = furryYes }
            };

            int added = 0;
            foreach (var definition in definitions)
            {
                if (definition.Sprites.Count < 2)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing random sprites for {definition.English}.");
                    continue;
                }
                bool isNew = !existingCards.TryGetValue(definition.Id, out var elem);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[definition.Id] = elem;
                    added++;
                }
                WriteOOSingleCard(elem, definition.Id, definition.Korean, definition.English, CardRarity.SSR, definition.Sprites[0]);
                elem.FindPropertyRelative("BaseWeight").floatValue = 4f;
                elem.FindPropertyRelative("ClickGold").floatValue = 125f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
                elem.FindPropertyRelative("Description").stringValue = $"진화 없이 클릭할 때마다 모습이 무작위로 바뀌는 SSR 등급 {definition.Korean} 카드입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"An SSR {definition.English} card whose appearance changes randomly on every click without evolution.";
                var clickSprites = elem.FindPropertyRelative("BreakthroughSprites");
                clickSprites.arraySize = definition.Sprites.Count;
                for (int i = 0; i < definition.Sprites.Count; i++)
                    clickSprites.GetArrayElementAtIndex(i).objectReferenceValue = definition.Sprites[i];
            }
            return added;
        }

        private static int EnsureReadyRCardBatch(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/292_306_ready_r_cards";
            var singles = new[]
            {
                new[] { "0292", "basket_in_cat.png",          "바구니 속 고양이",       "Basket Cat" },
                new[] { "0293", "birman_cat.png",             "버만 고양이",            "Birman Cat" },
                new[] { "0294", "blue_rush.png",              "블루 러시",              "Blue Rush" },
                new[] { "0295", "cat_in_the_box.png",         "상자 속 고양이",         "Cat in the Box" },
                new[] { "0296", "cerberus_cat.png",           "케르베로스 고양이",       "Cerberus Cat" },
                new[] { "0297", "donut_munchkin_cat.png",     "도넛 먼치킨 고양이",      "Donut Munchkin Cat" },
                new[] { "0298", "eng_cat.png",                "잉글리시 고양이",         "English Cat" },
                new[] { "0299", "gingerbread_cookie_cat.png", "진저브레드 고양이",       "Gingerbread Cookie Cat" },
                new[] { "0301", "paper_pattern_cat.png",      "종이 무늬 고양이",        "Paper Pattern Cat" },
                new[] { "0302", "savannah_cat.png",           "사바나 고양이",           "Savannah Cat" },
                new[] { "0303", "siamese_cat.png",            "샴 고양이",               "Siamese Cat" },
                new[] { "0304", "sleeping_cat.png",           "잠자는 고양이",           "Sleeping Cat" },
                new[] { "0305", "sphynx_cat.png",             "스핑크스 고양이",         "Sphynx Cat" }
            };

            int added = 0;
            foreach (string[] card in singles)
            {
                Sprite sprite = LoadFirstSprite($"{directory}/{card[1]}");
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing ready R sprite: {card[1]}");
                    continue;
                }
                added += WriteReadyRCard(cardsProp, existingCards, card[0], card[2], card[3], sprite);
            }

            var burgerSprites = LoadSpritesSorted(directory + "/nyanburger.png");
            if (burgerSprites.Count == 5)
            {
                const string id = "0300";
                bool isNew = !existingCards.TryGetValue(id, out var burger);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    burger = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = burger;
                    added++;
                }
                WriteOOSingleCard(burger, id, "냥버거", "Nyanburger", CardRarity.R, burgerSprites[0]);
                var stages = burger.FindPropertyRelative("BreakthroughVariantStages");
                var sprites = burger.FindPropertyRelative("BreakthroughSprites");
                stages.arraySize = sprites.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    stages.GetArrayElementAtIndex(i).intValue = i + 1;
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = burgerSprites[i];
                }
                burger.FindPropertyRelative("Description").stringValue = "한계돌파 1~5단계마다 햄버거가 커지는 R 등급 냥버거입니다.";
                burger.FindPropertyRelative("Description_EN").stringValue = "An R-grade Nyanburger with a distinct illustration at every breakthrough stage.";
            }
            else Debug.LogWarning($"[CreateOrSyncCardCatalog] Expected 5 Nyanburger sprites, found {burgerSprites.Count}.");

            Sprite whoBase = LoadFirstSprite(directory + "/who_am_i_1.png");
            Sprite whoStageThree = LoadFirstSprite(directory + "/who_am_i_2.png");
            if (whoBase != null && whoStageThree != null)
            {
                const string id = "0306";
                bool isNew = !existingCards.TryGetValue(id, out var who);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    who = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = who;
                    added++;
                }
                WriteOOSingleCard(who, id, "나는 누구", "Who Am I", CardRarity.R, whoBase);
                var stages = who.FindPropertyRelative("BreakthroughVariantStages");
                stages.arraySize = 1;
                stages.GetArrayElementAtIndex(0).intValue = 3;
                var sprites = who.FindPropertyRelative("BreakthroughSprites");
                sprites.arraySize = 5;
                sprites.GetArrayElementAtIndex(0).objectReferenceValue = whoBase;
                sprites.GetArrayElementAtIndex(1).objectReferenceValue = whoBase;
                sprites.GetArrayElementAtIndex(2).objectReferenceValue = whoStageThree;
                sprites.GetArrayElementAtIndex(3).objectReferenceValue = whoStageThree;
                sprites.GetArrayElementAtIndex(4).objectReferenceValue = whoStageThree;
                who.FindPropertyRelative("Description").stringValue = "한계돌파 3단계부터 두 번째 모습으로 변하는 R 등급 고양이입니다.";
                who.FindPropertyRelative("Description_EN").stringValue = "An R-grade cat that changes to its second form at breakthrough stage 3.";
            }
            else Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Who Am I stage sprite.");
            return added;
        }

        private static int WriteReadyRCard(
            SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards,
            string id, string koreanName, string englishName, Sprite sprite)
        {
            bool isNew = !existingCards.TryGetValue(id, out var elem);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                elem = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = elem;
            }
            WriteOOSingleCard(elem, id, koreanName, englishName, CardRarity.R, sprite);
            elem.FindPropertyRelative("Description").stringValue = $"진화 일러스트가 없는 R 등급 싱글 카드, {koreanName}입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = $"{englishName}, an R-grade single-illustration card without evolution art.";
            return isNew ? 1 : 0;
        }

        private static int EnsureCatMonsterSetCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/307_311_cat_monsters";
            var members = new[]
            {
                new[] { "0307", "four_cat_monster_eyes",   "눈알 몬스터 고양이", "Eye Monster Cat" },
                new[] { "0308", "four_cat_monster_poison", "독 몬스터 고양이",   "Poison Monster Cat" },
                new[] { "0309", "four_cat_monster_rock",   "바위 몬스터 고양이", "Rock Monster Cat" },
                new[] { "0310", "four_cat_monster_ghost",  "유령 몬스터 고양이", "Ghost Monster Cat" }
            };
            int added = 0;
            foreach (string[] member in members)
            {
                Sprite sprite = LoadNamedSprite(directory + "/four_cat_monster.png", member[1]);
                if (sprite == null) continue;
                added += WriteReadyRCard(cardsProp, existingCards, member[0], member[2], member[3], sprite);
                existingCards[member[0]].FindPropertyRelative("SetId").stringValue = "16";
            }

            Sprite rewardSprite = LoadFirstSprite(directory + "/pocket_monster.png");
            if (rewardSprite == null) return added;
            const string rewardId = "0311";
            bool isNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            WriteOOSingleCard(reward, rewardId, "포켓 몬스터", "Pocket Monster", CardRarity.R, rewardSprite);
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            reward.FindPropertyRelative("Description").stringValue = "네 종류의 몬스터 고양이를 모두 모으면 해금되는 R 등급 세트 보상입니다.";
            reward.FindPropertyRelative("Description_EN").stringValue = "An R-grade set reward unlocked by collecting all four Monster Cats.";
            return added;
        }

        private static int EnsureReadyCards312To318(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards)
        {
            const string dir = "Assets/image/A_No/312_318_ready_cards";
            int added = 0;
            added += WriteClickCycleCard(cardsProp, existingCards, "0312", "캣 휠", "Cat Wheel", LoadSpritesSorted(dir + "/cat_wheel.png"));

            var boxSprites = LoadSpritesSorted(dir + "/i_have_box.png");
            if (boxSprites.Count >= 3)
            {
                bool isNew = !existingCards.TryGetValue("0313", out var box);
                if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); box = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards["0313"] = box; added++; }
                WriteOOSingleCard(box, "0313", "상자가 있어", "I Have a Box", CardRarity.R, boxSprites[0]);
                var stages = box.FindPropertyRelative("BreakthroughVariantStages");
                var variants = box.FindPropertyRelative("BreakthroughSpriteVariants");
                stages.arraySize = variants.arraySize = 4;
                for (int i = 0; i < 4; i++)
                {
                    stages.GetArrayElementAtIndex(i).intValue = i + 2;
                    var variant = variants.GetArrayElementAtIndex(i);
                    variant.FindPropertyRelative("Stage").intValue = i + 2;
                    var group = variant.FindPropertyRelative("Sprites");
                    group.arraySize = 2;
                    group.GetArrayElementAtIndex(0).objectReferenceValue = boxSprites[1];
                    group.GetArrayElementAtIndex(1).objectReferenceValue = boxSprites[2];
                }
                var reps = box.FindPropertyRelative("BreakthroughSprites");
                reps.arraySize = 5;
                reps.GetArrayElementAtIndex(0).objectReferenceValue = boxSprites[0];
                for (int i = 1; i < 5; i++) reps.GetArrayElementAtIndex(i).objectReferenceValue = boxSprites[1];
                box.FindPropertyRelative("Description").stringValue = "한계돌파 2단계부터 진화하며 클릭할 때 두 모습을 번갈아 보여 줍니다.";
            }

            added += WriteClickCycleCard(cardsProp, existingCards, "0314", "차가 있어", "I Have a Car", LoadSpritesSorted(dir + "/i_have_car.png"));
            added += WriteClickCycleCard(cardsProp, existingCards, "0315", "긴 꼬리가 있어", "I Have a Long Tail", LoadSpritesSorted(dir + "/i_have_long_tale.png"));
            Sprite kite = LoadFirstSprite(dir + "/sim_kite.png");
            if (kite != null) added += WriteReadyRCard(cardsProp, existingCards, "0316", "심 연", "Sim Kite", kite);

            Sprite rich = LoadFirstSprite(dir + "/suspiciously_rich_cat.png");
            if (rich != null)
            {
                bool isNew = !existingCards.TryGetValue("0317", out var card);
                if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards["0317"] = card; added++; }
                WriteOOSingleCard(card, "0317", "수상하게 부유한 고양이", "Suspiciously Rich Cat", CardRarity.SR, rich);
                card.FindPropertyRelative("ClickGold").floatValue = 25f;
                card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
                card.FindPropertyRelative("IsShop").boolValue = true;
                card.FindPropertyRelative("ShopCurrency").enumValueIndex = (int)CardShopCurrency.Coin;
                card.FindPropertyRelative("ShopPrice").doubleValue = 1000d;
                card.FindPropertyRelative("BaseWeight").floatValue = 0f;
                card.FindPropertyRelative("Description").stringValue = "상점에서 구매할 수 있는 SR 등급 고양이입니다.";
            }

            var taco = LoadSpritesSorted(dir + "/taco_cat_goat_cheese_pizza.png");
            if (taco.Count >= 5)
            {
                bool isNew = !existingCards.TryGetValue("0318", out var card);
                if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards["0318"] = card; added++; }
                WriteOOSingleCard(card, "0318", "타코 캣 고트 치즈 피자", "Taco Cat Goat Cheese Pizza", CardRarity.R, taco[0]);
                var reps = card.FindPropertyRelative("BreakthroughSprites");
                var stages = card.FindPropertyRelative("BreakthroughVariantStages");
                reps.arraySize = stages.arraySize = 5;
                for (int i = 0; i < 5; i++) { reps.GetArrayElementAtIndex(i).objectReferenceValue = taco[i]; stages.GetArrayElementAtIndex(i).intValue = i + 1; }
                card.FindPropertyRelative("Description").stringValue = "한계돌파 1~5단계마다 모습이 변하는 R 등급 고양이입니다.";
            }
            return added;
        }

        private static int EnsureBlackEyesCard(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards)
        {
            const string dir = "Assets/image/A_No/319_black_eyes";
            Sprite closed = LoadFirstSprite(dir + "/the_black_eyes_close.png");
            Sprite open = LoadFirstSprite(dir + "/the_black_eyes_open.png");
            if (closed == null || open == null) return 0;
            const string id = "0319";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards[id] = card; }
            WriteOOSingleCard(card, id, "검은 눈", "The Black Eyes", CardRarity.R, closed);
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = open;
            card.FindPropertyRelative("Description").stringValue = "배경을 완전히 덮으며, 클릭 시 1% 확률로 0.5초 동안 눈을 뜹니다. 배경형 카드이므로 서브 소켓에는 장착할 수 없습니다.";
            card.FindPropertyRelative("Description_EN").stringValue = "Covers the background and has a 1% chance to open its eyes for 0.5 seconds on click. As a background-type card, it cannot be equipped in a sub socket.";
            return isNew ? 1 : 0;
        }

        private static int EnsureReadyCards320To322(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards)
        {
            const string dir = "Assets/image/A_No/320_322_ready_cards";
            int added = 0;
            added += WriteClickCycleCard(cardsProp, existingCards, "0320", "간지럼", "Tickling", LoadSpritesSorted(dir + "/tickling.png"));

            var perfect = LoadSpritesSorted(dir + "/World_of_perfect_cat.png");
            if (perfect.Count >= 2)
            {
                const string id = "0321";
                bool isNew = !existingCards.TryGetValue(id, out var card);
                if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards[id] = card; added++; }
                WriteOOSingleCard(card, id, "완벽한 고양이의 세계", "World of Perfect Cat", CardRarity.SSR, perfect[0]);
                card.FindPropertyRelative("BaseWeight").floatValue = 4f;
                card.FindPropertyRelative("ClickGold").floatValue = 125f;
                card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
                var effects = card.FindPropertyRelative("EffectSprites");
                effects.arraySize = 1;
                effects.GetArrayElementAtIndex(0).objectReferenceValue = perfect[1];
                card.FindPropertyRelative("Description").stringValue = "클릭 시 1% 확률로 두 번째 모습이 0.5초 동안 나타나는 SSR 등급 고양이입니다.";
                card.FindPropertyRelative("Description_EN").stringValue = "An SSR-grade cat with a 1% chance to show its second sprite for 0.5 seconds on click.";
            }

            added += WriteClickCycleCard(cardsProp, existingCards, "0322", "불친절한 이웃", "Your Unfriendly Neighborhood",
                LoadSpritesSorted(dir + "/your_Unfriendly_Neighborhood.png"));
            return added;
        }

        private static int WriteClickCycleCard(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards,
            string id, string korean, string english, List<Sprite> sprites)
        {
            if (sprites == null || sprites.Count == 0) return 0;
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew) { cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize); card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1); existingCards[id] = card; }
            WriteOOSingleCard(card, id, korean, english, CardRarity.R, sprites[0]);
            var frames = card.FindPropertyRelative("BreakthroughSprites");
            frames.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++) frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            card.FindPropertyRelative("Description").stringValue = "진화 없이 클릭할 때마다 스프라이트가 순서대로 바뀌는 R 등급 고양이입니다.";
            card.FindPropertyRelative("Description_EN").stringValue = "An R-grade cat whose sprites cycle in order with every click, without evolution.";
            return isNew ? 1 : 0;
        }

        private static int EnsureCatClawUpgradeCard(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards)
        {
            return WriteClickCycleCard(cardsProp, existingCards, "0323", "고양이 발톱 강화", "Cat Claw Upgrade",
                LoadSpritesSorted("Assets/image/A_No/323_cat_claw_upgrade/cat_claw_upgrade.png"));
        }

        private static int EnsureCatTowerCard(SerializedProperty cardsProp, Dictionary<string, SerializedProperty> existingCards)
        {
            const string dir = "Assets/image/A_No/324_cat_tower";
            var firstStages = LoadSpritesSorted(dir + "/tower_1_2.png");
            Sprite stageThree = LoadFirstSprite(dir + "/tower_3.png");
            Sprite stageFour = LoadFirstSprite(dir + "/tower_4.png");
            Sprite stageFive = LoadFirstSprite(dir + "/tower_5.png");
            if (firstStages.Count < 2 || stageThree == null || stageFour == null || stageFive == null) return 0;

            const string id = "0324";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "고양이 타워", "Cat Tower", CardRarity.R, firstStages[0]);
            var sprites = card.FindPropertyRelative("BreakthroughSprites");
            var stages = card.FindPropertyRelative("BreakthroughVariantStages");
            sprites.arraySize = stages.arraySize = 5;
            Sprite[] stageSprites = { firstStages[0], firstStages[1], stageThree, stageFour, stageFive };
            for (int i = 0; i < stageSprites.Length; i++)
            {
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = stageSprites[i];
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
            }
            card.FindPropertyRelative("Description").stringValue = "한계돌파 1~5단계마다 타워가 성장하는 R 등급 고양이입니다.";
            card.FindPropertyRelative("Description_EN").stringValue = "An R-grade cat whose tower grows at every breakthrough stage from 1 to 5.";
            return isNew ? 1 : 0;
        }

        private static int EnsureBallOfYarnTouchCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            return WriteClickCycleCard(
                cardsProp,
                existingCards,
                "0325",
                "털실 공",
                "Ball of Yarn",
                LoadSpritesSorted("Assets/image/A_No/325_ball_of_yarn_touch/Ball_of_yarn_touch.png"));
        }

        private static int EnsureBurgerMakerCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            var ingredients = LoadSpritesSorted("Assets/image/A_No/326_burger_maker/burger_maker.png");
            if (ingredients.Count < 8) return 0;

            const string id = "0326";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "버거 메이커", "Burger Maker", CardRarity.SSR, ingredients[0]);
            card.FindPropertyRelative("BaseWeight").floatValue = 4f;
            card.FindPropertyRelative("ClickGold").floatValue = 125f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 8;
            for (int i = 0; i < 8; i++)
                effects.GetArrayElementAtIndex(i).objectReferenceValue = ingredients[i];
            card.FindPropertyRelative("Description").stringValue =
                "중앙에는 완성된 버거가 표시됩니다. 클릭할 때마다 무작위 위치의 버거에 재료가 하나씩 떨어지며, 7번 재료가 낙하를 시작하면 다음 버거를 만들 수 있습니다. 콤보가 끝나면 생성된 버거는 사라집니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SSR-grade cat with a completed center burger. Each click drops one ingredient onto a burger at a random position; the next burger starts after ingredient 7 begins falling. Spawned burgers vanish when the combo ends.";
            return isNew ? 1 : 0;
        }

        private static int EnsureCatInTheBoxCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/327_cat_in_the_box/cat_in_the_box1.png";
            Sprite frame0 = LoadNamedSprite(path, "cat_in_the_box_0");
            Sprite frame1 = LoadNamedSprite(path, "cat_in_the_box_1");
            Sprite frame2 = LoadNamedSprite(path, "cat_in_the_box_2");
            Sprite frame3 = LoadNamedSprite(path, "cat_in_the_box_3");
            Sprite frame4Body = LoadNamedSprite(path, "cat_in_the_box_4_body");
            Sprite frame4Cat = LoadNamedSprite(path, "cat_in_the_box_4_cat");
            if (frame0 == null || frame1 == null || frame2 == null || frame3 == null ||
                frame4Body == null || frame4Cat == null) return 0;

            const string id = "0327";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "상자 속 고양이", "Cat in the Box", CardRarity.SR, frame0);
            card.FindPropertyRelative("BaseWeight").floatValue = 10f;
            card.FindPropertyRelative("ClickGold").floatValue = 25f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            Sprite[] frames = { frame0, frame1, frame2, frame3, frame4Body };
            var stored = card.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = frame4Cat;
            card.FindPropertyRelative("Description").stringValue =
                "클릭할 때마다 상자 속 모습이 바뀝니다. 4번 모습에서는 열린 상자만 남고 고양이가 무작위 방향으로 날아가며, 왼쪽으로 날 때는 좌우가 반전되는 SR 등급 고양이입니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SR-grade cat whose box changes on every click. On frame 4, the empty box remains while the cat flies away, flipping horizontally when it travels left.";
            return isNew ? 1 : 0;
        }

        private static int EnsureCatonatoSetCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/328_336_catonato_set";
            const string memberPath = directory + "/catonato_shark2.png";
            int added = 0;
            for (int type = 1; type <= 8; type++)
            {
                var images = new List<Sprite>();
                for (int variant = 1; variant <= 3; variant++)
                {
                    Sprite sprite = LoadNamedSprite(memberPath, $"catonato_shark_cat_type{type}_{variant}");
                    if (sprite != null) images.Add(sprite);
                }
                if (images.Count == 0)
                {
                    Sprite single = LoadNamedSprite(memberPath, $"catonato_shark_cat_type{type}");
                    if (single != null) images.Add(single);
                }
                if (images.Count == 0) continue;

                string id = (327 + type).ToString("D4");
                bool isNew = !existingCards.TryGetValue(id, out var card);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[id] = card;
                    added++;
                }

                WriteOOSingleCard(card, id, $"카토나토 상어 타입 {type}", $"Catonato Shark Type {type}", CardRarity.SR, images[0]);
                card.FindPropertyRelative("BaseWeight").floatValue = 10f;
                card.FindPropertyRelative("ClickGold").floatValue = 25f;
                card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
                card.FindPropertyRelative("SetId").stringValue = "17";
                if (images.Count > 1)
                {
                    var stages = card.FindPropertyRelative("BreakthroughVariantStages");
                    var sprites = card.FindPropertyRelative("BreakthroughSprites");
                    stages.arraySize = sprites.arraySize = images.Count;
                    for (int imageIndex = 0; imageIndex < images.Count; imageIndex++)
                    {
                        int activationStage = images.Count == 2
                            ? (imageIndex == 0 ? 1 : 5)
                            : (imageIndex == 0 ? 1 : imageIndex == 1 ? 3 : 5);
                        stages.GetArrayElementAtIndex(imageIndex).intValue = activationStage;
                        sprites.GetArrayElementAtIndex(imageIndex).objectReferenceValue = images[imageIndex];
                    }
                }
                card.FindPropertyRelative("Description").stringValue =
                    $"카토나토 세트를 구성하는 SR 등급 상어 고양이 타입 {type}입니다.";
                card.FindPropertyRelative("Description_EN").stringValue =
                    $"Catonato Shark Type {type}, an SR-grade member of the Catonato set.";
            }

            const string rewardId = "0336";
            Sprite reward0 = LoadNamedSprite(directory + "/catonato.png", "catonato_0");
            Sprite reward1 = LoadNamedSprite(directory + "/catonato.png", "catonato_");
            Sprite reward2 = LoadNamedSprite(directory + "/catonato.png", "catonato_2");
            Sprite reward3 = LoadNamedSprite(directory + "/catonato.png", "catonato_3");
            var flyingSharks = LoadSpritesSorted(directory + "/catonato_shark1.png");
            if (reward0 == null || reward1 == null || reward2 == null || reward3 == null) return added;
            bool rewardIsNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (rewardIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            WriteOOSingleCard(reward, rewardId, "카토나토", "Catonato", CardRarity.SSR, reward0);
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("ClickGold").floatValue = 125f;
            reward.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            Sprite[] rewardFrames = { reward0, reward1, reward2, reward3 };
            var rewardSprites = reward.FindPropertyRelative("BreakthroughSprites");
            rewardSprites.arraySize = rewardFrames.Length;
            for (int i = 0; i < rewardFrames.Length; i++)
                rewardSprites.GetArrayElementAtIndex(i).objectReferenceValue = rewardFrames[i];
            var rewardEffects = reward.FindPropertyRelative("EffectSprites");
            rewardEffects.arraySize = flyingSharks.Count;
            for (int i = 0; i < flyingSharks.Count; i++)
                rewardEffects.GetArrayElementAtIndex(i).objectReferenceValue = flyingSharks[i];
            reward.FindPropertyRelative("Description").stringValue =
                "카토나토 상어 타입 8종을 모두 모으면 해금됩니다. 클릭할 때마다 본체 모습이 바뀌고, 무작위 상어 고양이가 무작위 방향으로 날아가는 SSR 등급 카토나토입니다.";
            reward.FindPropertyRelative("Description_EN").stringValue =
                "An SSR-grade Catonato unlocked by collecting all eight Catonato Shark types. Each click changes its sprite and launches a random shark cat in a random direction.";
            return added;
        }

        private static void EnsureCatonatoSetCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var sets = so.FindProperty("sets");
            SerializedProperty entry = null;
            for (int i = 0; i < sets.arraySize; i++)
            {
                var candidate = sets.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("SetId").stringValue == "17") { entry = candidate; break; }
            }
            if (entry == null)
            {
                sets.InsertArrayElementAtIndex(sets.arraySize);
                entry = sets.GetArrayElementAtIndex(sets.arraySize - 1);
            }
            entry.FindPropertyRelative("SetId").stringValue = "17";
            entry.FindPropertyRelative("SetName").stringValue = "카토나토 상어 8종";
            entry.FindPropertyRelative("SetName_EN").stringValue = "Eight Catonato Sharks";
            entry.FindPropertyRelative("RewardGold").doubleValue = 0d;
            entry.FindPropertyRelative("RewardShards").intValue = 0;
            entry.FindPropertyRelative("CriticalChanceBonus").floatValue = 0f;
            entry.FindPropertyRelative("FlatIncomeBonus").doubleValue = 0d;
            entry.FindPropertyRelative("CriticalDamageBonus").floatValue = 0f;
            entry.FindPropertyRelative("GachaDiscountBonus").floatValue = 0f;
            entry.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1f;
            entry.FindPropertyRelative("RewardBackgroundId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardDecorationId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardCardId").stringValue = "0336";
            entry.FindPropertyRelative("EffectDesc").stringValue = "상어 고양이 타입 8종을 모두 모으면 SSR 카토나토를 획득합니다.";
            entry.FindPropertyRelative("EffectDesc_EN").stringValue = "Collect all eight Shark Cat types to unlock the SSR Catonato.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static int EnsureFiftyBillionRainbowsCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/337_fifty_billion_rainbows/Fifty_billion_rainbows.png";
            Sprite image1 = LoadNamedSprite(path, "Fifty_billion rainbows_0");
            Sprite image2 = LoadNamedSprite(path, "Fifty_billion rainbows_1");
            Sprite image3 = LoadNamedSprite(path, "Fifty_billion rainbows_2");
            Sprite image4 = LoadNamedSprite(path, "Fifty_billion rainbows_3");
            if (image1 == null || image2 == null || image3 == null || image4 == null) return 0;

            const string id = "0337";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "500억개의 무지개", "Fifty Billion Rainbows", CardRarity.R, image1);
            Sprite[] images = { image1, image2, image3, image4 };
            var stages = card.FindPropertyRelative("BreakthroughVariantStages");
            var sprites = card.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = sprites.arraySize = images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
            card.FindPropertyRelative("Description").stringValue =
                "한계돌파 1~4단계마다 무지개가 늘어나는 R 등급 고양이입니다. 5단계에서는 4단계 모습을 유지합니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An R-grade cat whose rainbows increase at breakthrough stages 1 through 4. Stage 5 keeps the stage 4 appearance.";
            return isNew ? 1 : 0;
        }

        private static int EnsureSurpriseCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/338_surprise_cat/Surprise_cat.png";
            Sprite image1 = LoadNamedSprite(path, "ChatGPT Image Aug 16, 2026, 12_46_22 PM_0");
            Sprite image2 = LoadNamedSprite(path, "Surprise_cat_0");
            Sprite image3 = LoadNamedSprite(path, "ChatGPT Image Aug 16, 2026, 12_46_22 PM_6");
            Sprite image4 = LoadNamedSprite(path, "ChatGPT Image Aug 16, 2026, 12_46_22 PM_3");
            if (image1 == null || image2 == null || image3 == null || image4 == null) return 0;

            const string id = "0338";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "깜짝 고양이", "Surprise Cat", CardRarity.R, image1);
            Sprite[] images = { image1, image2, image3, image4 };
            var stages = card.FindPropertyRelative("BreakthroughVariantStages");
            var sprites = card.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = sprites.arraySize = images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
            card.FindPropertyRelative("Description").stringValue =
                "한계돌파 1~4단계마다 더욱 크게 놀라는 R 등급 고양이입니다. 5단계에서는 4단계 모습을 유지합니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An R-grade cat that becomes increasingly surprised at breakthrough stages 1 through 4. Stage 5 keeps the stage 4 appearance.";
            return isNew ? 1 : 0;
        }

        private static int EnsureTailAnimationCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/339_tail_animation/tail1.png";
            Sprite start = LoadNamedSprite(path, "tail1_start");
            Sprite ing1 = LoadNamedSprite(path, "tail1_ing1");
            Sprite ing2 = LoadNamedSprite(path, "tail1_ing2");
            Sprite end1 = LoadNamedSprite(path, "tail1_end1");
            Sprite end2 = LoadNamedSprite(path, "tail1_end2");
            if (start == null || ing1 == null || ing2 == null || end1 == null || end2 == null) return 0;

            const string id = "0339";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "꼬리 고양이", "Tail Cat", CardRarity.SR, start);
            card.FindPropertyRelative("BaseWeight").floatValue = 10f;
            card.FindPropertyRelative("ClickGold").floatValue = 25f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            Sprite[] frames = { start, ing1, ing2, end1, end2 };
            var stored = card.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            card.FindPropertyRelative("Description").stringValue =
                "클릭할 때마다 Start 다음 Ing 1·2를 세 번 반복하고, End 1 또는 End 2를 각각 50% 확률로 표시한 뒤 다음 클릭에 Start부터 반복하는 SR 등급 고양이입니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SR-grade cat whose clicks advance from Start through three Ing 1/Ing 2 pairs, choose End 1 or End 2 with equal probability, then return to Start on the next click.";
            return isNew ? 1 : 0;
        }

        private static int EnsureRedFishHeadCatCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/340_red_fish_evolution/red_fish_head_cat.png";
            Sprite stage1 = LoadNamedSprite(path, "The_cat_whose_head_is_eating_a_red_fish_5");
            Sprite stage2 = LoadNamedSprite(path, "The_cat_whose_head_is_eating_a_red_fish_6");
            Sprite stage3 = LoadNamedSprite(path, "The_cat_whose_head_is_eating_a_red_fish_0");
            Sprite stage4 = LoadNamedSprite(path, "The_cat_whose_head_is_eating_a_red_fish_2");
            Sprite stage5 = LoadNamedSprite(path, "The_cat_whose_head_is_eating_a_red_fish_3");
            if (stage1 == null || stage2 == null || stage3 == null || stage4 == null || stage5 == null) return 0;

            const string id = "0340";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "머리가 빨간 생선을 먹는 고양이", "The Cat Whose Head Is Eating a Red Fish", CardRarity.R, stage1);
            Sprite[] stagesInOrder = { stage1, stage2, stage3, stage4, stage5 };
            var stages = card.FindPropertyRelative("BreakthroughVariantStages");
            var sprites = card.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = sprites.arraySize = stagesInOrder.Length;
            for (int i = 0; i < stagesInOrder.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = stagesInOrder[i];
            }
            card.FindPropertyRelative("Description").stringValue =
                "한계돌파 1~5단계마다 머리와 물고기의 모습이 변하는 R 등급 고양이입니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An R-grade cat whose head and fish change at every breakthrough stage from 1 through 5.";
            return isNew ? 1 : 0;
        }

        private static int EnsureTrinitySetCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/341_344_trinity_set";
            var definitions = new[]
            {
                new
                {
                    Id = "0341", Korean = "트리니티 분노", English = "Trinity Angry",
                    Path = directory + "/trinity_angry.png",
                    Names = new[] { "trinity_angry_0", "trinity_angry_1", "trinity_angry_2", "trinity_angry_3", "trinity_angry_4" }
                },
                new
                {
                    Id = "0342", Korean = "트리니티 행복", English = "Trinity Happy",
                    Path = directory + "/trinity_happy.png",
                    Names = new[]
                    {
                        "ChatGPT Image Aug 16, 2026, 10_23_27 AM_2",
                        "ChatGPT Image Aug 16, 2026, 10_23_27 AM_0",
                        "ChatGPT Image Aug 16, 2026, 10_23_27 AM_3",
                        "ChatGPT Image Aug 16, 2026, 10_23_27 AM_6",
                        "ChatGPT Image Aug 16, 2026, 10_23_27 AM_8"
                    }
                },
                new
                {
                    Id = "0343", Korean = "트리니티 슬픔", English = "Trinity Sad",
                    Path = directory + "/trinity_sad.png",
                    Names = new[]
                    {
                        "ChatGPT Image Aug 16, 2026, 10_21_40 AM_0",
                        "ChatGPT Image Aug 16, 2026, 10_21_40 AM_1",
                        "ChatGPT Image Aug 16, 2026, 10_21_40 AM_4",
                        "ChatGPT Image Aug 16, 2026, 10_21_40 AM_10",
                        "ChatGPT Image Aug 16, 2026, 10_21_40 AM_11"
                    }
                }
            };

            int added = 0;
            foreach (var definition in definitions)
            {
                var images = new List<Sprite>();
                foreach (string spriteName in definition.Names)
                {
                    Sprite sprite = LoadNamedSprite(definition.Path, spriteName);
                    if (sprite != null) images.Add(sprite);
                }
                if (images.Count != 5) continue;

                bool isNew = !existingCards.TryGetValue(definition.Id, out var card);
                if (isNew)
                {
                    cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                    card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                    existingCards[definition.Id] = card;
                    added++;
                }
                WriteOOSingleCard(card, definition.Id, definition.Korean, definition.English, CardRarity.SR, images[0]);
                card.FindPropertyRelative("BaseWeight").floatValue = 10f;
                card.FindPropertyRelative("ClickGold").floatValue = 25f;
                card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
                card.FindPropertyRelative("SetId").stringValue = "18";
                var stages = card.FindPropertyRelative("BreakthroughVariantStages");
                var sprites = card.FindPropertyRelative("BreakthroughSprites");
                stages.arraySize = sprites.arraySize = 5;
                for (int i = 0; i < 5; i++)
                {
                    stages.GetArrayElementAtIndex(i).intValue = i + 1;
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
                }
                card.FindPropertyRelative("Description").stringValue =
                    $"한계돌파 1~5단계마다 모습이 변하는 SR 등급 {definition.Korean} 카드입니다.";
                card.FindPropertyRelative("Description_EN").stringValue =
                    $"An SR-grade {definition.English} card whose appearance changes at every breakthrough stage from 1 through 5.";
            }

            Sprite reward0 = LoadNamedSprite(directory + "/trinity.png", "trinity_0");
            Sprite reward1 = LoadNamedSprite(directory + "/trinity.png", "trinity_2");
            Sprite reward2 = LoadNamedSprite(directory + "/trinity.png", "trinity_6");
            if (reward0 == null || reward1 == null || reward2 == null) return added;

            const string rewardId = "0344";
            bool rewardIsNew = !existingCards.TryGetValue(rewardId, out var reward);
            if (rewardIsNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                reward = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[rewardId] = reward;
                added++;
            }
            WriteOOSingleCard(reward, rewardId, "트리니티", "Trinity", CardRarity.SSR, reward0);
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("ClickGold").floatValue = 125f;
            reward.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            Sprite[] rewardFrames = { reward0, reward1, reward2 };
            var stored = reward.FindPropertyRelative("BreakthroughSprites");
            stored.arraySize = rewardFrames.Length;
            for (int i = 0; i < rewardFrames.Length; i++)
                stored.GetArrayElementAtIndex(i).objectReferenceValue = rewardFrames[i];
            reward.FindPropertyRelative("Description").stringValue =
                "분노·행복·슬픔 트리니티 3종을 모두 모으면 해금되며, 클릭할 때마다 세 모습이 순서대로 바뀌는 SSR 등급 카드입니다.";
            reward.FindPropertyRelative("Description_EN").stringValue =
                "An SSR-grade reward unlocked by collecting Trinity Angry, Happy, and Sad. Its three appearances cycle on click.";
            return added;
        }

        private static void EnsureTrinitySetCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            if (catalog == null) return;
            var so = new SerializedObject(catalog);
            var sets = so.FindProperty("sets");
            SerializedProperty entry = null;
            for (int i = 0; i < sets.arraySize; i++)
            {
                var candidate = sets.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("SetId").stringValue == "18") { entry = candidate; break; }
            }
            if (entry == null)
            {
                sets.InsertArrayElementAtIndex(sets.arraySize);
                entry = sets.GetArrayElementAtIndex(sets.arraySize - 1);
            }
            entry.FindPropertyRelative("SetId").stringValue = "18";
            entry.FindPropertyRelative("SetName").stringValue = "트리니티 감정 3종";
            entry.FindPropertyRelative("SetName_EN").stringValue = "Three Trinity Emotions";
            entry.FindPropertyRelative("RewardGold").doubleValue = 0d;
            entry.FindPropertyRelative("RewardShards").intValue = 0;
            entry.FindPropertyRelative("CriticalChanceBonus").floatValue = 0f;
            entry.FindPropertyRelative("FlatIncomeBonus").doubleValue = 0d;
            entry.FindPropertyRelative("CriticalDamageBonus").floatValue = 0f;
            entry.FindPropertyRelative("GachaDiscountBonus").floatValue = 0f;
            entry.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1f;
            entry.FindPropertyRelative("RewardBackgroundId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardDecorationId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardCardId").stringValue = "0344";
            entry.FindPropertyRelative("EffectDesc").stringValue = "트리니티 감정 3종을 모두 모으면 SSR 트리니티를 획득합니다.";
            entry.FindPropertyRelative("EffectDesc_EN").stringValue = "Collect all three Trinity emotions to unlock the SSR Trinity.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static int EnsureHairballCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/345_hairball/hairball.png";
            Sprite cat1 = LoadNamedSprite(path, "hairball_cat1");
            Sprite cat2 = LoadNamedSprite(path, "hairball_cat2");
            Sprite hairball = LoadNamedSprite(path, "hairball_hairball");
            if (cat1 == null || cat2 == null || hairball == null) return 0;

            const string id = "0345";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "헤어볼", "Hairball", CardRarity.SR, cat1);
            card.FindPropertyRelative("BaseWeight").floatValue = 10f;
            card.FindPropertyRelative("ClickGold").floatValue = 25f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 2;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = cat2;
            effects.GetArrayElementAtIndex(1).objectReferenceValue = hairball;
            card.FindPropertyRelative("Description").stringValue =
                "기본 모습은 cat1이며, 클릭하면 잠시 cat2로 바뀌고 입에서 화면 왼쪽 아래 110도 방향으로 헤어볼을 발사하는 SR 등급 카드입니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SR-grade card that briefly changes from cat1 to cat2 on click and launches a hairball from its mouth down-left at 110 degrees.";
            return isNew ? 1 : 0;
        }

        private static int EnsureWarigari2Card(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/346_warigari2/warigari2.png";
            var images = new Sprite[5];
            for (int i = 0; i < images.Length; i++)
            {
                images[i] = LoadNamedSprite(path, $"warigari2_{i}");
                if (images[i] == null) return 0;
            }

            const string id = "0346";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "와리가리 2", "Warigari 2", CardRarity.R, images[0]);
            var stages = card.FindPropertyRelative("BreakthroughVariantStages");
            var sprites = card.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = sprites.arraySize = images.Length;
            for (int i = 0; i < images.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
            card.FindPropertyRelative("Description").stringValue =
                "한계돌파할 때마다 더 격렬하게 뛰는 모습으로 변합니다. 클릭할 때마다 왼쪽과 오른쪽으로 번갈아 빠르게 이동하며, 단계가 높을수록 이동 거리가 늘고 오른쪽으로 달릴 때는 이미지가 좌우 반전됩니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "Its running pose grows more intense at every breakthrough. Each click alternates a quick dash left or right; higher stages travel farther, and rightward dashes flip the image horizontally.";
            return isNew ? 1 : 0;
        }

        private static int EnsureTasteCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/347_taste";
            var cats = new Sprite[4];
            var fish = new Sprite[4];
            var meat = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                cats[i] = LoadNamedSprite(directory + "/taste1.png", $"taste1_{i}");
                fish[i] = LoadNamedSprite(directory + "/taste_fish.png", $"taste_fish_{i}");
                meat[i] = LoadNamedSprite(directory + "/taste_meat.png", $"taste_meat_{i}");
                if (cats[i] == null || fish[i] == null || meat[i] == null) return 0;
            }

            const string id = "0347";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "맛보기", "Taste", CardRarity.SR, cats[0]);
            card.FindPropertyRelative("BaseWeight").floatValue = 10f;
            card.FindPropertyRelative("ClickGold").floatValue = 25f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 12;
            for (int i = 0; i < 4; i++)
            {
                effects.GetArrayElementAtIndex(i).objectReferenceValue = cats[i];
                effects.GetArrayElementAtIndex(4 + i).objectReferenceValue = fish[i];
                effects.GetArrayElementAtIndex(8 + i).objectReferenceValue = meat[i];
            }
            card.FindPropertyRelative("EffectSpriteGroupSplit").intValue = 4;
            card.FindPropertyRelative("Description").stringValue =
                "식사가 시작될 때 생선과 고기 중 하나가 무작위로 정해집니다. 클릭할 때마다 고양이와 음식이 1~4단계로 함께 바뀌며, 4단계에서 모두 먹은 뒤 새 식사를 시작합니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "Each meal randomly begins with fish or meat. Every click advances both the cat and its food through eating frames 1 to 4, then starts a new meal after the food is finished.";
            return isNew ? 1 : 0;
        }

        private static int EnsureThrowCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/348_throw";
            Sprite watching = LoadNamedSprite(directory + "/throw2.png", "throw2.png_0");
            Sprite pushing = LoadNamedSprite(directory + "/throw2.png", "throw2.png_2");
            if (watching == null || pushing == null) return 0;

            var objects = new Sprite[14];
            for (int i = 0; i < objects.Length; i++)
            {
                objects[i] = LoadNamedSprite(directory + "/throw1.png", $"throw1_{i}");
                if (objects[i] == null) return 0;
            }

            const string id = "0348";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "밀어 떨어뜨리기", "Push It Off", CardRarity.SSR, watching);
            card.FindPropertyRelative("BaseWeight").floatValue = 2f;
            card.FindPropertyRelative("ClickGold").floatValue = 125f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = 16;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = watching;
            effects.GetArrayElementAtIndex(1).objectReferenceValue = pushing;
            for (int i = 0; i < objects.Length; i++)
                effects.GetArrayElementAtIndex(i + 2).objectReferenceValue = objects[i];
            card.FindPropertyRelative("EffectSpriteGroupSplit").intValue = 2;
            card.FindPropertyRelative("Description").stringValue =
                "탁자 위의 무작위 물건을 바라보다가 클릭하면 앞발로 쳐서 왼쪽 아래로 떨어뜨립니다. 물건이 화면 아래로 사라진 뒤 다음 클릭에서 새 물건을 고릅니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SSR cat watches a random object on the table. Click to swat it down-left and off the bottom of the screen; the next click prepares a new object.";
            return isNew ? 1 : 0;
        }

        private static int EnsureHumunStreetEarthCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string path = "Assets/image/A_No/349_humun_street_earth/humun_street_earth2.png";
            Sprite earth = LoadNamedSprite(path, "humun_street_earth_earth");
            var cats = new Sprite[12];
            cats[0] = LoadNamedSprite(path, "humun_street_earth_cat_base");
            for (int i = 1; i < cats.Length; i++)
                cats[i] = LoadNamedSprite(path, $"humun_street_earth_cat_{i}");
            if (earth == null) return 0;
            for (int i = 0; i < cats.Length; i++) if (cats[i] == null) return 0;

            const string id = "0349";
            bool isNew = !existingCards.TryGetValue(id, out var card);
            if (isNew)
            {
                cardsProp.InsertArrayElementAtIndex(cardsProp.arraySize);
                card = cardsProp.GetArrayElementAtIndex(cardsProp.arraySize - 1);
                existingCards[id] = card;
            }

            WriteOOSingleCard(card, id, "휴먼 스트리트 어스", "Humun Street Earth", CardRarity.SSR, earth);
            card.FindPropertyRelative("BaseWeight").floatValue = 2f;
            card.FindPropertyRelative("ClickGold").floatValue = 125f;
            card.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            var effects = card.FindPropertyRelative("EffectSprites");
            effects.arraySize = cats.Length;
            for (int i = 0; i < cats.Length; i++)
                effects.GetArrayElementAtIndex(i).objectReferenceValue = cats[i];
            card.FindPropertyRelative("Description").stringValue =
                "지구 위에 고양이가 서 있으며, 클릭할 때마다 기본 모습부터 11가지 동작까지 순서대로 바뀌는 SSR 등급 카드입니다.";
            card.FindPropertyRelative("Description_EN").stringValue =
                "An SSR card with a cat layered over Earth. Each click cycles from the base pose through eleven cat poses in order.";
            return isNew ? 1 : 0;
        }

        private static void EnsureCatTowerDecoration()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
            Sprite sprite = LoadFirstSprite("Assets/image/A_Deco/Deco_tower.png");
            if (catalog == null || sprite == null) return;

            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("decorations");
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var candidate = entries.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("Id").stringValue == "deco-cat-tower") { entry = candidate; break; }
            }
            if (entry == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            entry.FindPropertyRelative("Id").stringValue = "deco-cat-tower";
            entry.FindPropertyRelative("DisplayName").stringValue = "고양이 타워";
            entry.FindPropertyRelative("DisplayName_EN").stringValue = "Cat Tower";
            entry.FindPropertyRelative("DecorationSprite").objectReferenceValue = sprite;
            entry.FindPropertyRelative("AnimationSprites").ClearArray();
            entry.FindPropertyRelative("AnimationFrameDuration").floatValue = 0.25f;
            entry.FindPropertyRelative("IsHidden").boolValue = false;
            entry.FindPropertyRelative("IsShop").boolValue = false;
            entry.FindPropertyRelative("ShopCurrency").enumValueIndex = (int)CardShopCurrency.Coin;
            entry.FindPropertyRelative("ShopPrice").doubleValue = 1000d;
            entry.FindPropertyRelative("SetId").stringValue = string.Empty;
            entry.FindPropertyRelative("Description").stringValue = "고양이 타워 장식입니다.";
            entry.FindPropertyRelative("Description_EN").stringValue = "A cat tower decoration.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureSharkDecorations()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
            Sprite staticShark = LoadNamedSprite("Assets/image/A_Deco/Deco_Shark.png", "deco_shark_0");
            var animatedSharks = LoadSpritesSorted("Assets/image/A_Deco/Deco_Shark_Animated.png");
            if (catalog == null || staticShark == null || animatedSharks.Count != 4) return;

            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("decorations");
            SerializedProperty staticEntry = null;
            SerializedProperty animatedEntry = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var candidate = entries.GetArrayElementAtIndex(i);
                string id = candidate.FindPropertyRelative("Id").stringValue;
                if (id == "deco-shark") staticEntry = candidate;
                else if (id == "deco-shark-animated") animatedEntry = candidate;
            }
            if (staticEntry == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                staticEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            WriteSharkDecoration(staticEntry, "deco-shark", "상어 장식", "Shark Decoration", staticShark, null);

            // Array insertion can invalidate a previously captured SerializedProperty.
            animatedEntry = null;
            for (int i = 0; i < entries.arraySize; i++)
            {
                var candidate = entries.GetArrayElementAtIndex(i);
                if (candidate.FindPropertyRelative("Id").stringValue == "deco-shark-animated")
                {
                    animatedEntry = candidate;
                    break;
                }
            }
            if (animatedEntry == null)
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                animatedEntry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            WriteSharkDecoration(animatedEntry, "deco-shark-animated", "움직이는 상어 장식", "Animated Shark Decoration",
                animatedSharks[0], animatedSharks);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureThrowObjectDecorations()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
            if (catalog == null) return;

            var definitions = new[]
            {
                new { Id = "deco-throw-yellow-cat-mug", Korean = "노란 고양이 머그", English = "Yellow Cat Mug" },
                new { Id = "deco-throw-bubble-tea", Korean = "버블티", English = "Bubble Tea" },
                new { Id = "deco-throw-potted-plant", Korean = "고양이 화분", English = "Cat Potted Plant" },
                new { Id = "deco-throw-red-yarn", Korean = "빨간 실뭉치", English = "Red Yarn Ball" },
                new { Id = "deco-throw-fish-can", Korean = "파란 생선 캔", English = "Blue Fish Can" },
                new { Id = "deco-throw-snow-globe", Korean = "고양이 스노우글로브", English = "Cat Snow Globe" },
                new { Id = "deco-throw-book-stack", Korean = "고양이 책 더미", English = "Cat Book Stack" },
                new { Id = "deco-throw-rubber-duck", Korean = "고무 오리", English = "Rubber Duck" },
                new { Id = "deco-throw-fish-plush", Korean = "생선 인형", English = "Fish Plush" },
                new { Id = "deco-throw-alarm-clock", Korean = "고양이 알람시계", English = "Cat Alarm Clock" },
                new { Id = "deco-throw-crumpled-paper", Korean = "구겨진 종이", English = "Crumpled Paper" },
                new { Id = "deco-throw-cactus", Korean = "꽃 선인장", English = "Flowering Cactus" },
                new { Id = "deco-throw-paw-mug", Korean = "발바닥 머그", English = "Paw Mug" },
                new { Id = "deco-throw-black-cat-cushion", Korean = "검은 고양이 쿠션", English = "Black Cat Cushion" }
            };

            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("decorations");
            for (int definitionIndex = 0; definitionIndex < definitions.Length; definitionIndex++)
            {
                var definition = definitions[definitionIndex];
                Sprite sprite = LoadNamedSprite(
                    "Assets/image/A_No/348_throw/throw1.png", $"throw1_{definitionIndex}");
                if (sprite == null) continue;

                SerializedProperty entry = null;
                for (int i = 0; i < entries.arraySize; i++)
                {
                    var candidate = entries.GetArrayElementAtIndex(i);
                    if (candidate.FindPropertyRelative("Id").stringValue == definition.Id)
                    {
                        entry = candidate;
                        break;
                    }
                }
                if (entry == null)
                {
                    entries.InsertArrayElementAtIndex(entries.arraySize);
                    entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                }

                entry.FindPropertyRelative("Id").stringValue = definition.Id;
                entry.FindPropertyRelative("DisplayName").stringValue = definition.Korean;
                entry.FindPropertyRelative("DisplayName_EN").stringValue = definition.English;
                entry.FindPropertyRelative("DecorationSprite").objectReferenceValue = sprite;
                entry.FindPropertyRelative("AnimationSprites").ClearArray();
                entry.FindPropertyRelative("AnimationFrameDuration").floatValue = 0.25f;
                entry.FindPropertyRelative("IsHidden").boolValue = false;
                entry.FindPropertyRelative("IsShop").boolValue = false;
                entry.FindPropertyRelative("ShopCurrency").enumValueIndex = (int)CardShopCurrency.Coin;
                entry.FindPropertyRelative("ShopPrice").doubleValue = 1000d;
                entry.FindPropertyRelative("SetId").stringValue = string.Empty;
                entry.FindPropertyRelative("Description").stringValue = $"{definition.Korean} 장식입니다.";
                entry.FindPropertyRelative("Description_EN").stringValue = $"A {definition.English.ToLowerInvariant()} decoration.";
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void WriteSharkDecoration(
            SerializedProperty entry, string id, string koreanName, string englishName,
            Sprite representative, List<Sprite> animationFrames)
        {
            entry.FindPropertyRelative("Id").stringValue = id;
            entry.FindPropertyRelative("DisplayName").stringValue = koreanName;
            entry.FindPropertyRelative("DisplayName_EN").stringValue = englishName;
            entry.FindPropertyRelative("DecorationSprite").objectReferenceValue = representative;
            var animation = entry.FindPropertyRelative("AnimationSprites");
            int frameCount = animationFrames != null ? animationFrames.Count : 0;
            animation.arraySize = frameCount;
            for (int i = 0; i < frameCount; i++)
                animation.GetArrayElementAtIndex(i).objectReferenceValue = animationFrames[i];
            entry.FindPropertyRelative("AnimationFrameDuration").floatValue = 0.25f;
            entry.FindPropertyRelative("IsHidden").boolValue = false;
            entry.FindPropertyRelative("IsShop").boolValue = false;
            entry.FindPropertyRelative("ShopCurrency").enumValueIndex = (int)CardShopCurrency.Coin;
            entry.FindPropertyRelative("ShopPrice").doubleValue = 1000d;
            entry.FindPropertyRelative("SetId").stringValue = string.Empty;
            entry.FindPropertyRelative("Description").stringValue = animationFrames == null
                ? "상어 모습의 정적 장식입니다."
                : "네 개의 스프라이트가 자동으로 순서대로 반복되는 상어 장식입니다.";
            entry.FindPropertyRelative("Description_EN").stringValue = animationFrames == null
                ? "A static shark decoration."
                : "A shark decoration whose four sprites loop automatically in sequence.";
        }

        private static void EnsureCatWheelDecoration()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
            if (catalog == null) return;
            var sprites = LoadSpritesSorted("Assets/image/A_No/312_318_ready_cards/cat_wheel.png");
            if (sprites.Count == 0) return;
            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("decorations");
            SerializedProperty entry = null;
            for (int i = 0; i < entries.arraySize; i++) { var e = entries.GetArrayElementAtIndex(i); if (e.FindPropertyRelative("Id").stringValue == "deco-cat-wheel") { entry = e; break; } }
            if (entry == null) { entries.InsertArrayElementAtIndex(entries.arraySize); entry = entries.GetArrayElementAtIndex(entries.arraySize - 1); }
            entry.FindPropertyRelative("Id").stringValue = "deco-cat-wheel";
            entry.FindPropertyRelative("DisplayName").stringValue = "캣 휠";
            entry.FindPropertyRelative("DisplayName_EN").stringValue = "Cat Wheel";
            entry.FindPropertyRelative("DecorationSprite").objectReferenceValue = sprites[0];
            entry.FindPropertyRelative("IsHidden").boolValue = false;
            entry.FindPropertyRelative("IsShop").boolValue = false;
            entry.FindPropertyRelative("SetId").stringValue = string.Empty;
            entry.FindPropertyRelative("Description").stringValue = "장착하면 스프라이트가 자동으로 바뀌는 캣 휠 장식입니다.";
            var animation = entry.FindPropertyRelative("AnimationSprites");
            animation.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++) animation.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            entry.FindPropertyRelative("AnimationFrameDuration").floatValue = 0.25f;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureCatMonsterSetCatalog()
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
                if (candidate.FindPropertyRelative("SetId").stringValue == "16") { entry = candidate; break; }
            }
            if (entry == null)
            {
                sets.InsertArrayElementAtIndex(sets.arraySize);
                entry = sets.GetArrayElementAtIndex(sets.arraySize - 1);
            }
            entry.FindPropertyRelative("SetId").stringValue = "16";
            entry.FindPropertyRelative("SetName").stringValue = "네 마리 몬스터 고양이";
            entry.FindPropertyRelative("SetName_EN").stringValue = "Four Monster Cats";
            entry.FindPropertyRelative("RewardGold").doubleValue = 0d;
            entry.FindPropertyRelative("RewardShards").intValue = 0;
            entry.FindPropertyRelative("CriticalChanceBonus").floatValue = 0f;
            entry.FindPropertyRelative("FlatIncomeBonus").doubleValue = 0d;
            entry.FindPropertyRelative("CriticalDamageBonus").floatValue = 0f;
            entry.FindPropertyRelative("GachaDiscountBonus").floatValue = 0f;
            entry.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1f;
            entry.FindPropertyRelative("RewardBackgroundId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardDecorationId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardCardId").stringValue = "0311";
            entry.FindPropertyRelative("EffectDesc").stringValue = "몬스터 고양이 네 장을 모두 모으면 R 등급 포켓 몬스터를 획득합니다.";
            entry.FindPropertyRelative("EffectDesc_EN").stringValue = "Collect all four Monster Cats to unlock the R-grade Pocket Monster.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
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

        private static int EnsureCatIsWaterSetCards(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string directory = "Assets/image/A_No/350_354_cat_is_water_set";
            const string memberPath = directory + "/0350_0353_R_cat_is_water.png";
            int added = 0;

            for (int i = 0; i < 4; i++)
            {
                string id = (350 + i).ToString("D4");
                Sprite sprite = LoadNamedSprite(memberPath, "cat_is_water-Photoroom_" + i);
                if (sprite == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Cat Is Water member frame {i}.");
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
                elem.FindPropertyRelative("DisplayName").stringValue = $"고양이는 물 {i + 1}";
                elem.FindPropertyRelative("DisplayName_EN").stringValue = $"Cat Is Water {i + 1}";
                elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
                elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
                elem.FindPropertyRelative("ClickGold").floatValue = 5f;
                elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
                elem.FindPropertyRelative("MaxStacks").intValue = 6;
                elem.FindPropertyRelative("SetId").stringValue = "19";
                elem.FindPropertyRelative("IsHidden").boolValue = false;
                elem.FindPropertyRelative("IsShop").boolValue = false;
                elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
                elem.FindPropertyRelative("Description").stringValue = $"고양이는 물 세트를 구성하는 R 등급 카드 {i + 1}입니다.";
                elem.FindPropertyRelative("Description_EN").stringValue = $"R-grade card {i + 1} from the Cat Is Water set.";
                ClearCardVariants(elem);
                elem.FindPropertyRelative("EffectSprites").ClearArray();
            }

            const string rewardId = "0354";
            const string rewardPath = directory + "/0354_SR_cat_is_real_water.png";
            var rewardSprites = new Sprite[4];
            for (int i = 0; i < rewardSprites.Length; i++)
            {
                rewardSprites[i] = LoadNamedSprite(rewardPath, "cat_is_real_water_" + i);
                if (rewardSprites[i] == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Cat Is Real Water frame {i}.");
                    return added;
                }
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
            reward.FindPropertyRelative("DisplayName").stringValue = "고양이는 진짜 물";
            reward.FindPropertyRelative("DisplayName_EN").stringValue = "Cat Is Real Water";
            reward.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            reward.FindPropertyRelative("BaseWeight").floatValue = 0f;
            reward.FindPropertyRelative("ClickGold").floatValue = 25f;
            reward.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            reward.FindPropertyRelative("MaxStacks").intValue = 6;
            reward.FindPropertyRelative("SetId").stringValue = string.Empty;
            reward.FindPropertyRelative("IsHidden").boolValue = true;
            reward.FindPropertyRelative("IsShop").boolValue = false;
            reward.FindPropertyRelative("CardSprite").objectReferenceValue = rewardSprites[0];
            reward.FindPropertyRelative("Description").stringValue = "고양이는 물 세트 보상입니다. 진화 없이 클릭할 때마다 네 가지 모습이 순서대로 반복됩니다.";
            reward.FindPropertyRelative("Description_EN").stringValue = "The Cat Is Water set reward. Each click cycles through four sprites in order, without evolution art.";
            ClearCardVariants(reward);
            var frames = reward.FindPropertyRelative("BreakthroughSprites");
            frames.arraySize = rewardSprites.Length;
            for (int i = 0; i < rewardSprites.Length; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue = rewardSprites[i];
            reward.FindPropertyRelative("EffectSprites").ClearArray();
            return added;
        }

        private static void EnsureCatIsWaterSetCatalog()
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
                if (candidate.FindPropertyRelative("SetId").stringValue == "19") { entry = candidate; break; }
            }
            if (entry == null)
            {
                sets.InsertArrayElementAtIndex(sets.arraySize);
                entry = sets.GetArrayElementAtIndex(sets.arraySize - 1);
            }
            entry.FindPropertyRelative("SetId").stringValue = "19";
            entry.FindPropertyRelative("SetName").stringValue = "고양이는 물";
            entry.FindPropertyRelative("SetName_EN").stringValue = "Cat Is Water";
            entry.FindPropertyRelative("RewardGold").doubleValue = 0d;
            entry.FindPropertyRelative("RewardShards").intValue = 0;
            entry.FindPropertyRelative("CriticalChanceBonus").floatValue = 0f;
            entry.FindPropertyRelative("FlatIncomeBonus").doubleValue = 0d;
            entry.FindPropertyRelative("CriticalDamageBonus").floatValue = 0f;
            entry.FindPropertyRelative("GachaDiscountBonus").floatValue = 0f;
            entry.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1f;
            entry.FindPropertyRelative("RewardBackgroundId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardDecorationId").stringValue = string.Empty;
            entry.FindPropertyRelative("RewardCardId").stringValue = "0354";
            entry.FindPropertyRelative("EffectDesc").stringValue = "고양이는 물 카드 네 장을 모두 모으면 SR 등급 고양이는 진짜 물을 획득합니다.";
            entry.FindPropertyRelative("EffectDesc_EN").stringValue = "Collect all four Cat Is Water cards to unlock the SR Cat Is Real Water.";
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
        }

        private static int EnsureCheersCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0355";
            const string path = "Assets/image/A_No/355_cheers/0355_SR_cheers.png";
            Sprite baseSprite = LoadNamedSprite(path, "cheers_0");
            Sprite clickSprite = LoadNamedSprite(path, "cheers_2");
            if (baseSprite == null || clickSprite == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Cheers base or click sprite.");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "건배";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Cheers";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 10f;
            elem.FindPropertyRelative("ClickGold").floatValue = 25f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_5;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = baseSprite;
            elem.FindPropertyRelative("Description").stringValue = "클릭하면 잠깐 두 번째 모습으로 변했다가 돌아오는 SR 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SR-grade card that briefly changes to its second sprite when clicked, then returns to normal.";
            ClearCardVariants(elem);
            var effects = elem.FindPropertyRelative("EffectSprites");
            effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = clickSprite;
            return isNew ? 1 : 0;
        }

        private static int EnsureCthulhuCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0356";
            const string directory = "Assets/image/A_No/356_cthulhu";
            const string normalPath = directory + "/0356_SSR_Cthulhu_normal.png";
            const string fifthPath = directory + "/0356_SSR_Cthulhu_sprite_five.png";
            const string truthPath = directory + "/0356_SSR_Cthulhu_truth.png";
            var normalSprites = new Sprite[5];
            var truthSprites = new Sprite[5];
            for (int i = 0; i < 4; i++)
            {
                normalSprites[i] = LoadNamedSprite(normalPath, "Cthulhu_normal_" + i);
                int truthIndex = i == 1 ? 3 : (i == 3 ? 1 : i);
                truthSprites[i] = LoadNamedSprite(truthPath, "Cthulhu_Cthulhu_" + truthIndex);
            }
            normalSprites[4] = LoadNamedSprite(fifthPath, "Cthulhu_sprite_five_normal");
            truthSprites[4] = LoadNamedSprite(fifthPath, "Cthulhu_sprite_five_trans");
            for (int i = 0; i < normalSprites.Length; i++)
            {
                if (normalSprites[i] == null || truthSprites[i] == null)
                {
                    Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Cthulhu stage {i + 1} sprite pair.");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "평범한 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Ordinary Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SSR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 2f;
            elem.FindPropertyRelative("ClickGold").floatValue = 125f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = normalSprites[0];
            elem.FindPropertyRelative("Description").stringValue = "어느 단계에서도 평범한 고양이처럼 보이는 SSR 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An SSR-grade card that looks like an ordinary cat at every stage.";
            ClearCardVariants(elem);
            var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
            var sprites = elem.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = normalSprites.Length;
            sprites.arraySize = normalSprites.Length;
            for (int i = 0; i < normalSprites.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = normalSprites[i];
            }
            var effects = elem.FindPropertyRelative("EffectSprites");
            effects.arraySize = truthSprites.Length;
            for (int i = 0; i < truthSprites.Length; i++)
                effects.GetArrayElementAtIndex(i).objectReferenceValue = truthSprites[i];
            return isNew ? 1 : 0;
        }

        private static int EnsureMatryosikaCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0357";
            const string path = "Assets/image/A_No/357_matryosika/0357_SSR_matryosika.png";
            string[] spriteNames =
            {
                "0357_SSR_matryosika_0_head", "0357_SSR_matryosika_0_body",
                "0357_SSR_matryosika_1_head", "0357_SSR_matryosika_1_body",
                "0357_SSR_matryosika_2_head", "0357_SSR_matryosika_2_body",
                "0357_SSR_matryosika_3_head", "0357_SSR_matryosika_3_body",
                "0357_SSR_matryosika_4_head", "0357_SSR_matryosika_4_body",
                "0357_SSR_matryosika_0_body_back",
                "0357_SSR_matryosika_1_body_back",
                "0357_SSR_matryosika_2_body_back",
                "0357_SSR_matryosika_3_body_back"
            };
            var sprites = new Sprite[spriteNames.Length];
            for (int i = 0; i < spriteNames.Length; i++)
            {
                sprites[i] = LoadNamedSprite(path, spriteNames[i]);
                if (sprites[i] != null) continue;
                Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Matryosika sprite: {spriteNames[i]}");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "마트료시카 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Matryosika Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.SSR;
            elem.FindPropertyRelative("BaseWeight").floatValue = 2f;
            elem.FindPropertyRelative("ClickGold").floatValue = 125f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_10;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[0];
            elem.FindPropertyRelative("Description").stringValue = "클릭할 때마다 바깥 인형의 머리가 열려 안쪽 인형이 차례로 드러나며, 마지막에는 진짜 고양이가 나타납니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "Each click opens the next outer doll, revealing the nested dolls in order and finally the real cat.";
            ClearCardVariants(elem);
            var effects = elem.FindPropertyRelative("EffectSprites");
            effects.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                effects.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            return isNew ? 1 : 0;
        }

        private static int EnsureServalCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0358";
            const string path = "Assets/image/A_No/358_serval/0358_R_serval.png";
            Sprite sprite = LoadNamedSprite(path, "serval_1");
            if (sprite == null)
            {
                Debug.LogWarning("[CreateOrSyncCardCatalog] Missing Serval main sprite: serval_1");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "서벌";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Serval";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprite;
            elem.FindPropertyRelative("Description").stringValue = "진화 및 클릭 특수효과가 없는 단일 R 등급 서벌 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "A single R-grade Serval card without evolution art or a click effect.";
            ClearCardVariants(elem);
            elem.FindPropertyRelative("EffectSprites").ClearArray();
            return isNew ? 1 : 0;
        }

        private static int EnsureWalkingWalkwalkCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0359";
            const string path = "Assets/image/A_No/359_walking_walkwalk/0359_R_walking_walkwalk.png";
            var sprites = new Sprite[4];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = LoadNamedSprite(path, "walking_walkwalk_" + i);
                if (sprites[i] != null) continue;
                Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Walking Walkwalk frame {i}.");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "워킹 워크워크";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Walking Walkwalk";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[0];
            elem.FindPropertyRelative("Description").stringValue = "클릭할 때마다 네 가지 걷는 모습이 순서대로 반복되는 R 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade card that cycles through four walking sprites in order with each click.";
            ClearCardVariants(elem);
            var frames = elem.FindPropertyRelative("BreakthroughSprites");
            frames.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                frames.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            elem.FindPropertyRelative("EffectSprites").ClearArray();
            return isNew ? 1 : 0;
        }

        private static int EnsureStretchCard(
            SerializedProperty cardsProp,
            Dictionary<string, SerializedProperty> existingCards)
        {
            const string id = "0360";
            const string path = "Assets/image/A_No/360_stretch/0360_R_stretch.png";
            var sprites = new Sprite[5];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = LoadNamedSprite(path, "stretch_needfix_" + i);
                if (sprites[i] != null) continue;
                Debug.LogWarning($"[CreateOrSyncCardCatalog] Missing Stretch frame {i}.");
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
            elem.FindPropertyRelative("DisplayName").stringValue = "쭉쭉 고양이";
            elem.FindPropertyRelative("DisplayName_EN").stringValue = "Stretch Cat";
            elem.FindPropertyRelative("Rarity").enumValueIndex = (int)CardRarity.R;
            elem.FindPropertyRelative("BaseWeight").floatValue = 40f;
            elem.FindPropertyRelative("ClickGold").floatValue = 5f;
            elem.FindPropertyRelative("ShardValue").intValue = (int)CardShardValue.Value_3;
            elem.FindPropertyRelative("MaxStacks").intValue = 6;
            elem.FindPropertyRelative("SetId").stringValue = string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue = false;
            elem.FindPropertyRelative("IsShop").boolValue = false;
            elem.FindPropertyRelative("CardSprite").objectReferenceValue = sprites[0];
            elem.FindPropertyRelative("Description").stringValue = "한계돌파할 때마다 모습이 변하며, 최대 한계돌파 후에는 클릭으로 다섯 모습을 순서대로 반복하는 R 등급 카드입니다.";
            elem.FindPropertyRelative("Description_EN").stringValue = "An R-grade card that evolves through five sprites and cycles through them on click after reaching maximum breakthrough.";
            ClearCardVariants(elem);
            var stages = elem.FindPropertyRelative("BreakthroughVariantStages");
            var variants = elem.FindPropertyRelative("BreakthroughSprites");
            stages.arraySize = sprites.Length;
            variants.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                stages.GetArrayElementAtIndex(i).intValue = i + 1;
                variants.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
            elem.FindPropertyRelative("EffectSprites").ClearArray();
            return isNew ? 1 : 0;
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
            elem.FindPropertyRelative("EffectSprites").ClearArray();
            elem.FindPropertyRelative("EffectSpriteGroupSplit").intValue = 0;
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
