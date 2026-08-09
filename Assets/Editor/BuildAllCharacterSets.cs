using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace CosmicChaosCat.Editor
{
    public static class BuildAllCharacterSets
    {
        // [MenuItem("Tools/Build All 8 Character Sets Cards & Catalogs")]
        public static void BuildAllSets()
        {
            var cardCatalog = AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            if (cardCatalog == null)
            {
                Debug.LogError("[BuildAllCharacterSets] CardCatalog.asset not found at Assets/ScriptableObjects/CardCatalog.asset");
                return;
            }

            var setCatalog = AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            if (setCatalog == null)
            {
                Debug.LogError("[BuildAllCharacterSets] SetCatalog.asset not found at Assets/ScriptableObjects/SetCatalog.asset");
                return;
            }

            // 1. Ensure TextureImporter is set to Sprite for all PNGs in characters_set1..8
            for (int s = 1; s <= 8; s++)
            {
                string folder = $"Assets/image/No/characters_set{s}";
                if (!Directory.Exists(folder)) continue;
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
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
            }

            // Set definitions (Set 1 to Set 8)
            var setDefs = new (string setId, string setName, double gold, int shards, string desc)[]
            {
                ("1", "골목길의 수호자", 500d, 10, "골목길 고양이들을 모두 모았습니다."),
                ("2", "사막 오아시스 탐험대", 1500d, 25, "사막 유적지의 모든 고양이를 해금했습니다."),
                ("3", "달콤한 디저트 왕국", 3000d, 50, "달콤한 디저트 고양이들을 모두 수집했습니다."),
                ("4", "신비한 심해 수족관", 5000d, 100, "깊은 바닷속 해저 고양이들을 모두 수집했습니다."),
                ("5", "민속 저자거리 축제", 10000d, 200, "전통 저자거리의 고양이들을 모두 모았습니다."),
                ("6", "우주 레인저 히어로", 25000d, 350, "우주를 지키는 레인저 히어로 고양이들을 모두 모았습니다."),
                ("7", "스팀펑크 비행선단", 50000d, 500, "하늘을 누비는 스팀펑크 비행선단 고양이들을 모두 모았습니다."),
                ("8", "사계절의 정령들", 100000d, 1000, "사계절의 아름다움을 품은 정령 고양이들을 모두 수집했습니다.")
            };

            var newSets = new List<SetEntry>();
            foreach (var sd in setDefs)
            {
                newSets.Add(new SetEntry
                {
                    SetId = sd.setId,
                    SetName = sd.setName,
                    RewardGold = sd.gold,
                    RewardShards = sd.shards,
                    EffectDesc = sd.desc
                });
            }

            // Card metadata lookup table
            var cardInfo = GetCardMetadata();

            var newCards = new List<CardEntry>();
            int globalCardNum = 1;

            for (int s = 1; s <= 8; s++)
            {
                string setId = s.ToString();
                string folder = $"Assets/image/No/characters_set{s}";
                if (!Directory.Exists(folder)) continue;

                // Group files by slot number 01..14
                var slotFiles = new Dictionary<int, List<string>>();
                string[] files = Directory.GetFiles(folder, "*.png");
                System.Array.Sort(files);

                foreach (var f in files)
                {
                    string fileName = Path.GetFileName(f);
                    var match = Regex.Match(fileName, @"^(\d+)_([A-Z]+)_(.+?)(?:_stage(\d+))?\.png$");
                    if (match.Success)
                    {
                        int slotNum = int.Parse(match.Groups[1].Value);
                        if (!slotFiles.ContainsKey(slotNum)) slotFiles[slotNum] = new List<string>();
                        slotFiles[slotNum].Add(f);
                    }
                }

                for (int slot = 1; slot <= 14; slot++)
                {
                    if (!slotFiles.ContainsKey(slot)) continue;
                    var flist = slotFiles[slot];
                    string firstFile = Path.GetFileName(flist[0]);
                    var match = Regex.Match(firstFile, @"^(\d+)_([A-Z]+)_(.+?)(?:_stage(\d+))?\.png$");
                    if (!match.Success) continue;

                    string rarityStr = match.Groups[2].Value;
                    string rawName   = match.Groups[3].Value;

                    CardRarity rarity = CardRarity.N;
                    float clickGold = 1f;
                    CardShardValue shardVal = CardShardValue.Value_1;

                    if (rarityStr == "R")
                    {
                        rarity = CardRarity.R;
                        clickGold = 2f;
                        shardVal = CardShardValue.Value_3;
                    }
                    else if (rarityStr == "SR")
                    {
                        rarity = CardRarity.SR;
                        clickGold = 5f;
                        shardVal = CardShardValue.Value_50;
                    }
                    else if (rarityStr == "SSR")
                    {
                        rarity = CardRarity.SSR;
                        clickGold = 10f;
                        shardVal = CardShardValue.Value_100;
                    }
                    else
                    {
                        rarity = CardRarity.N;
                        clickGold = 1f;
                        shardVal = CardShardValue.Value_1;
                    }

                    string idStr = globalCardNum.ToString("D4"); // 0001, 0002...

                    string displayName = rawName;
                    string desc = $"{rawName} 고양이입니다.";
                    if (cardInfo.TryGetValue(rawName, out var info))
                    {
                        displayName = info.name;
                        desc = info.desc;
                    }

                    // Stage sprites for SR (1..3) & SSR (1..5)
                    Sprite baseSprite = null;
                    int[] variantStages = null;
                    Sprite[] variantSprites = null;

                    if (rarity == CardRarity.N || rarity == CardRarity.R)
                    {
                        baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>(flist[0]);
                    }
                    else if (rarity == CardRarity.SR)
                    {
                        variantStages = new int[] { 1, 2, 3 };
                        variantSprites = new Sprite[3];
                        foreach (var fp in flist)
                        {
                            var m = Regex.Match(Path.GetFileName(fp), @"_stage(\d+)\.png$");
                            if (m.Success)
                            {
                                int st = int.Parse(m.Groups[1].Value);
                                if (st >= 1 && st <= 3)
                                {
                                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(fp);
                                    variantSprites[st - 1] = sp;
                                    if (st == 1) baseSprite = sp;
                                }
                            }
                        }
                        if (baseSprite == null && variantSprites.Length > 0) baseSprite = variantSprites[0];
                    }
                    else if (rarity == CardRarity.SSR)
                    {
                        variantStages = new int[] { 1, 2, 3, 4, 5 };
                        variantSprites = new Sprite[5];
                        foreach (var fp in flist)
                        {
                            var m = Regex.Match(Path.GetFileName(fp), @"_stage(\d+)\.png$");
                            if (m.Success)
                            {
                                int st = int.Parse(m.Groups[1].Value);
                                if (st >= 1 && st <= 5)
                                {
                                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(fp);
                                    variantSprites[st - 1] = sp;
                                    if (st == 1) baseSprite = sp;
                                }
                            }
                        }
                        if (baseSprite == null && variantSprites.Length > 0) baseSprite = variantSprites[0];
                    }

                    // Set 1/2 balancing: completed three-stage interaction cards are
                    // promoted from SR to SSR; the former SSR cards are lowered to R.
                    // Apply this after loading variants so each card keeps its authored stages.
                    if (s <= 2)
                    {
                        if (rarity == CardRarity.SR)
                            rarity = CardRarity.SSR;
                        else if (rarity == CardRarity.SSR)
                            rarity = CardRarity.R;
                    }

                    var cardEntry = new CardEntry
                    {
                        Id = idStr,
                        DisplayName = displayName,
                        Rarity = rarity,
                        ClickGold = clickGold,
                        ShardValue = shardVal,
                        SetId = setId,
                        CardSprite = baseSprite,
                        BreakthroughVariantStages = variantStages,
                        BreakthroughSprites = variantSprites,
                        Description = desc
                    };

                    newCards.Add(cardEntry);
                    globalCardNum++;
                }
            }

            cardCatalog.CardsList.Clear();
            cardCatalog.CardsList.AddRange(newCards);
            EditorUtility.SetDirty(cardCatalog);

            setCatalog.SetsList.Clear();
            setCatalog.SetsList.AddRange(newSets);
            EditorUtility.SetDirty(setCatalog);

            AssetDatabase.SaveAssets();
            Debug.Log($"[BuildAllCharacterSets] Successfully built {newCards.Count} cards across {newSets.Count} sets! Cards numbered 0001 to {globalCardNum - 1:D4}.");
        }

        private static Dictionary<string, (string name, string desc)> GetCardMetadata()
        {
            return new Dictionary<string, (string name, string desc)>
            {
                // Set 1
                { "Cat_in_the_Box", ("상자 속 고양이", "상자 속에 쏙 들어가 세상을 구경하는 아늑한 고양이입니다.") },
                { "tin_helmet_cat", ("양철 모자 고양이", "양철 냄비를 투구처럼 쓰고 용맹하게 골목을 누빕니다.") },
                { "umbrella_cat", ("우산 고양이", "비 오는 날에도 우산을 쓰고 산책을 즐기는 감성 고양이입니다.") },
                { "fishbone_cat", ("생선가시 고양이", "맛있는 생선가시를 사수하고 만족스러운 표정을 짓습니다.") },
                { "spray_cat", ("스프레이 고양이", "벽면에 멋진 그래피티 아트를 그리는 스트리트 아티스트입니다.") },
                { "midnight_delivery_cat", ("야간 배달 고양이", "밤하늘을 가르며 비밀스러운 소물을 배달합니다.") },
                { "cardboard_fort_cat", ("박스 요새 고양이", "튼튼한 종이 상자로 누구도 뚫을 수 없는 요새를 구축했습니다.") },
                { "alley_dj_cat", ("골목길 DJ 고양이", "신나는 비트로 골목길 고양이들의 밤 파티를 책임집니다.") },
                { "street_skater_cat", ("스트리트 스케이터 고양이", "멋진 점프 기술로 골목길 장애물을 자유롭게 넘어다닙니다.") },
                { "neon_graffiti_cat", ("네온 그래피티 고양이", "화려한 네온 빛으로 밤의 도시를 아름답게 물들입니다.") },
                { "rooftop_vigilante_cat", ("지붕 위 자객 고양이", "어둠 속 지붕 위에서 도시의 평화를 감시하는 비밀 영웅입니다.") },
                { "alley_king_cat", ("골목길 국왕 고양이", "골목길의 모든 고양이들이 우러러보는 위엄 있는 왕입니다.") },
                { "cyber_punk_cat", ("사이버펑크 고양이", "첨단 해킹 기술과 최첨단 장비로 미래 도시를 지배합니다.") },
                { "phantom_thief_cat", ("괴도 고양이", "예고장을 남기고 밤하늘로 사라지는 전설의 대괴도 고양이입니다.") },
                // Set 2
                { "sand_paw_cat", ("모래 발자국 고양이", "뜨거운 사막 모래 위를 사뿐사뿐 걸어가는 고양이입니다.") },
                { "papyrus_cat", ("파피루스 고양이", "고대 기록이 담긴 파피루스 두루마리를 소중히 품고 있습니다.") },
                { "jar_cat", ("항아리 고양이", "서늘한 도자기 항아리 안에 들어가 꿀잠을 청합니다.") },
                { "scarab_cat", ("스카라베 고양이", "행운을 가져다주는 신비한 스카라베 장식을 차고 있습니다.") },
                { "temple_scribe_cat", ("신전 필경사 고양이", "신전 벽면에 비밀 문자를 정성스레 기록합니다.") },
                { "desert_guide_cat", ("사막 길잡이 고양이", "광활한 사막에서 오아시스로 향하는 길을 안내해 줍니다.") },
                { "oasis_merchant_cat", ("오아시스 상인 고양이", "신비한 사막의 보물과 향신료를 거래하는 거상입니다.") },
                { "pyramid_guard_cat", ("피라미드 수호병 고양이", "피라미드의 입구를 지키며 굳건한 창을 들고 서 있습니다.") },
                { "sphinx_cat", ("스핑크스 고양이", "수수께끼를 던지며 유적지를 탐험하는 신비로운 수호자입니다.") },
                { "sun_priest_cat", ("태양 사제 고양이", "붉은 태양의 기운을 빌려 가뭄을 물리치는 의식을 올립니다.") },
                { "pharaoh_guard_cat", ("파라오 근위대장 고양이", "왕을 호위하는 최정예 근위대의 용맹한 대장입니다.") },
                { "cleopatra_cat", ("클레오파트라 고양이", "치명적인 매력과 지혜로 사막의 제국을 다스리는 여왕입니다.") },
                { "eye_of_ra_cat", ("라의 눈 고양이", "태양신 라의 눈동자를 통해 세상을 꿰뚫어보는 신성한 고양이입니다.") },
                { "anubis_judgement_cat", ("아누비스 심판관 고양이", "영혼의 무게를 측정하여 영원의 고요로 인도하는 신성한 심판관입니다.") },
                // Set 3
                { "flour_cat", ("밀가루 고양이", "온몸에 고소한 밀가루를 묻히고 빵집을 거니는 고양이입니다.") },
                { "sugar_cat", ("설탕 고양이", "달콤한 설탕가루처럼 사르르 녹아내리는 애교쟁이입니다.") },
                { "dough_cat", ("반죽 고양이", "푹신한 반죽 위에서 식빵 자세를 취하고 있습니다.") },
                { "cookie_cat", ("쿠키 고양이", "바삭하게 구워진 달콤한 쿠키 냄새를 풍깁니다.") },
                { "macaron_cat", ("마카롱 고양이", "알록달록 쫀득한 마카롱 꼬깔을 쓴 사랑스러운 고양이입니다.") },
                { "cupcake_cat", ("컵케이크 고양이", "머리 위에 부드러운 생크림과 체리를 올리고 있습니다.") },
                { "chocolate_fountain_cat", ("초콜릿 분수 고양이", "달콤하게 흘러내리는 초콜릿으로 가득한 퐁듀를 즐깁니다.") },
                { "jelly_delivery_cat", ("젤리 배달부 고양이", "탱글탱글한 알록달록 젤리를 이웃들에게 배달합니다.") },
                { "croissant_knight_cat", ("크로아상 기사 고양이", "바삭한 겹겹의 크로아상 갑옷을 입은 굳건한 기사입니다.") },
                { "cake_mage_cat", ("케이크 마법사 고양이", "달콤한 마법 지팡이로 모두를 행복하게 만드는 주문을 겁니다.") },
                { "icecream_alchemist_cat", ("아이스크림 연금술사 고양이", "시원한 아이스크림 재료를 조합하여 연금술 조합을 만듭니다.") },
                { "confectionery_queen_cat", ("제과 여왕 고양이", "달콤한 디저트 성의 제과 제국을 총괄하는 우아한 여왕입니다.") },
                { "star_candy_angel_cat", ("별사탕 천사 고양이", "밤하늘의 별사탕을 모아 사람들에게 기쁨의 축복을 내립니다.") },
                { "infinite_dessert_dragon_cat", ("무한 디저트 드래곤 고양이", "마르지 않는 디저트 샘을 수호하는 전설의 디저트 드래곤 고양이입니다.") },
                // Set 4
                { "bubble_scout_cat", ("방울 정찰 고양이", "바닷속 물방울을 타고 유유히 헤엄치는 정찰 고양이입니다.") },
                { "shell_collector_cat", ("조개 수집가 고양이", "예쁜 조개껍데기를 모아 바닷가에 모아둡니다.") },
                { "coral_gardener_cat", ("산호 정원사 고양이", "알록달록 산호초를 다듬어 쾌적한 보금자리를 만듭니다.") },
                { "jellyfish_lantern_cat", ("해파리 등불 고양이", "은은하게 빛나는 해파리를 등불 삼아 깊은 바다를 밝깁니다.") },
                { "golden_seahorse_courier_cat", ("황금 해마 전령 고양이", "황금 해마를 타고 바다 왕국의 기쁜 소식을 전합니다.") },
                { "crab_armor_cat", ("게 갑주 고양이", "단단한 게 집게발과 갑옷으로 무장한 용감한 파수꾼입니다.") },
                { "pearl_harp_cat", ("진주 하프 고양이", "빛나는 진주 하프를 켜며 조용한 바닷속 멜로디를 만듭니다.") },
                { "anchor_sailor_cat", ("닻 수영 고양이", "묵직한 닻을 가뿐히 들고 해저 탐사를 지휘하는 고양이입니다.") },
                { "submarine_captain_cat", ("잠수함 선장 고양이", "첨단 잠수함을 조종하여 수중 대탐사를 이끄는 리더입니다.") },
                { "sea_mage_cat", ("해양 마도사 고양이", "푸른 소용돌이와 바다의 원소 마법을 자유자재로 다룹니다.") },
                { "coral_kingdom_knight_cat", ("산호 왕국 수호기사 고양이", "산호 검을 휘두르며 해저 제국의 성벽을 수호합니다.") },
                { "mermaid_empress_cat", ("인어 여황제 고양이", "맑고 영롱한 목소리로 해저 모든 생물들을 다스리는 여황제입니다.") },
                { "sapphire_dragon_cat", ("사파이어 드래곤 고양이", "심해 깊은 곳 사파이어 광산을 수호하는 해저 용 고양이입니다.") },
                { "sea_god_cat", ("해신 포세이돈 고양이", "삼지창을 거머쥐고 칠해의 파도를 자유자재로 호령하는 해신 고양이입니다.") },
                // Set 5
                { "mask_dance_cat", ("탈춤 고양이", "봉산탈을 쓰고 덩실덩실 춤을 추며 복을 기원하는 고양이입니다.") },
                { "mackerel_vendor_cat", ("고등어 장수 고양이", "신선한 간고등어를 지게에 짊어지고 저자거리를 다닙니다.") },
                { "kite_festival_cat", ("방패연 고양이", "시원한 바람을 타는 알록달록 방패연을 하늘 높이 날립니다.") },
                { "blacksmith_cat", ("대장장이 고양이", "붉은 화로 앞에서 뚝딱뚝딱 호미와 낫을 만듭니다.") },
                { "straw_hat_farmer_cat", ("삿갓 농부 고양이", "벼 이삭이 영그는 들판에서 풍년을 기원하며 땀을 흘립니다.") },
                { "harvest_manager_cat", ("풍년 관리인 고양이", "가을철 곡식을 곳간에 가득 쌓고 쌀가마니를 검수합니다.") },
                { "market_guardian_cat", ("저자거리 포돌이 고양이", "왁자지껄한 장터의 치안을 지키는 늠름한 파수꾼입니다.") },
                { "herb_tea_cat", ("쌍화탕 한의사 고양이", "따스한 한약재를 정성껏 달여 마을 이웃들의 건강을 보살핍니다.") },
                { "moonlight_storyteller_cat", ("달빛 전기수 고양이", "밤마다 촛불 아래에서 흥미진진한 옛날이야기를 풀어놓습니다.") },
                { "quiet_mountain_spirit_cat", ("산신령 고양이", "하얀 수염을 휘날리며 깊은 산속에서 은둔하는 산신령입니다.") },
                { "rice_cake_seller_cat", ("떡메 치기 고양이", "쫄깃한 인절미를 쿵덕쿵덕 찧어 달콤한 떡을 만드는 장인입니다.") },
                { "festival_drum_cat", ("상모 사물놀이 고양이", "웅장한 북소리와 상모돌리기로 축제의 흥을 최고조로 끌어올립니다.") },
                { "tiger_doll_cat", ("호랑이 탈 고양이", "액운을 쫓아내고 기운을 북돋아주는 영험한 호랑이 탈을 쓰고 있습니다.") },
                { "white_tiger_guardian_cat", ("백호 신수 고양이", "사방신 중 신령스러운 백호의 신통력을 계승한 수호신 고양이입니다.") },
                // Set 6
                { "red_transform_hero_cat", ("레드 변신 히어로 고양이", "정의감 넘치는 레인저 팀의 열정적인 리더입니다.") },
                { "cobalt_tech_hero_cat", ("코발트 브레인 히어로 고양이", "명석한 두뇌와 첨단 바이저로 전술을 분석합니다.") },
                { "yellow_speed_hero_cat", ("옐로우 스피드 히어로 고양이", "바람보다 빠른 번개 같은 스피드로 악당을 뒤쫓습니다.") },
                { "emerald_rescue_hero_cat", ("에메랄드 리스큐 히어로 고양이", "굳건한 방패와 치유 슈트로 대원들을 보호합니다.") },
                { "rose_flight_hero_cat", ("로즈 비행 히어로 고양이", "화려한 날개 엔진을 장착하고 공중전을 담당합니다.") },
                { "black_stealth_hero_cat", ("블랙 은신 히어로 고양이", "밤그림자 속으로 사라지는 은밀한 침투 전문가입니다.") },
                { "orange_blast_hero_cat", ("오렌지 버스트 히어로 고양이", "강력한 화염 브래스터로 적의 진지를 타격합니다.") },
                { "crimson_claw_captain_cat", ("크림슨 크로 캡틴 고양이", "날카로운 에너제틱 클로를 휘두르는 캡틴입니다.") },
                { "blue_shark_marine_cat", ("블루 샤크 마린 히어로 고양이", "수중 기지를 지키는 유선형 해양 전투용 히어로입니다.") },
                { "white_blizzard_cat", ("화이트 블리자드 히어로 고양이", "얼음의 냉기로 적들의 움직임을 순간 얼려버립니다.") },
                { "purple_psychic_cat", ("퍼플 초능력 히어로 고양이", "염동력 빔으로 사물을 자유자재로 움직이는 초능력자입니다.") },
                { "golden_lion_commander_cat", ("골든 라이온 사령관 고양이", "황금 사자 로봇에 탑승하여 전 함대를 지휘하는 사령관입니다.") },
                { "silver_scarlet_hero_cat", ("실버 스카렛 제트 히어로 고양이", "초음속 제트 변신 기체로 우주 공간을 누비는 영웅입니다.") },
                { "prism_galaxy_supreme_cat", ("프리즘 은하 최강자 고양이", "우주 프리즘 에너지를 집속하여 악을 멸하는 은하의 절대 수호자입니다.") },
                // Set 7
                { "steampunk_sailor_cat", ("스팀펑크 수습 선원 고양이", "톱니바퀴 모자를 쓰고 비행선 갑판을 청소하는 고양이입니다.") },
                { "coal_shoveler_cat", ("석탄 보일러 고양이", "거대한 증기 보일러에 석탄을 쉴 새 없이 넣습니다.") },
                { "airship_scout_cat", ("비행선 렌즈 정찰 고양이", "망원경 정찰 장비로 구름 너머 항로를 확인합니다.") },
                { "scrap_collector_cat", ("고물 톱니 수집가 고양이", "굴러다니는 톱니바퀴와 기어를 모아 요긴하게 재활용합니다.") },
                { "sturdy_welder_cat", ("강철 용접공 고양이", "푸른 불꽃을 내뿜으며 비행선의 동체를 튼튼하게 용접합니다.") },
                { "cloud_surveyor_cat", ("구름 측정사 고양이", "고도계와 기압계를 들고 하늘의 기상을 연구합니다.") },
                { "propeller_duelist_cat", ("프로펠러 결투사 고양이", "등 뒤의 고속 프로펠러로 공중 결투를 벌이는 고수입니다.") },
                { "steam_medic_cat", ("증기 위생병 고양이", "소독용 증기 팩으로 대원들의 상처를 치료해 줍니다.") },
                { "storm_tracker_cat", ("폭풍 추적자 고양이", "거친 뇌우 속에서도 안전하게 비행선을 운항하는 베테랑 항해사입니다.") },
                { "ironwing_ace_cat", ("아이언윙 파일럿 고양이", "강철 날개 비행기를 타고 공중 묘기를 부리는 에이스입니다.") },
                { "airship_mechanic_cat", ("증기기관 엔지니어 고양이", "복잡한 증기 기관의 원리를 완벽하게 파악하고 있는 천재 정비사입니다.") },
                { "sky_pirate_empress_cat", ("하늘 해적 여왕 고양이", "하늘을 누비며 보물을 탐하는 스팀펑크 비행선단의 제왕입니다.") },
                { "golden_airship_captain_cat", ("황금 비행선 함장 고양이", "거대한 기함의 키를 잡고 전설의 유토피아로 향하는 함장입니다.") },
                { "eternal_clockwork_cat", ("영원의 태엽 시계 고양이", "시간과 톱니바퀴의 차원을 넘어 멈추지 않는 시간을 주관하는 존재입니다.") },
                // Set 8
                { "dandelion_flower_cat", ("민들레 씨앗 고양이", "민들레 홀씨를 후 불어 봄바람을 타고 여행을 떠납니다.") },
                { "raincoat_cat", ("노란 우비 고양이", "노란 장화와 우비를 입고 빗물 웅덩이를 짝짝 땁니다.") },
                { "silver_daisy_cat", ("은빛 데이지 고양이", "향기로운 데이지 꽃밭에서 은은하게 낮잠을 청합니다.") },
                { "maple_cat", ("단풍잎 고양이", "붉은 단풍잎 낙엽을 모아 따스한 보금자리를 짓습니다.") },
                { "cherry_blossom_calico_cat", ("벚꽃 삼색 고양이", "흩날리는 분홍빛 벚꽃 잎을 맞으며 낭만을 즐깁니다.") },
                { "indigo_necklace_black_cat", ("남색 목걸이 검은 고양이", "신비로운 푸른 빛 목걸이를 한 매력적인 검은 고양이입니다.") },
                { "summer_thunder_guardian_cat", ("한여름 뇌우 고양이", "소나기가 그친 뒤 들판을 가로지르며 무지개를 찾아 나섭니다.") },
                { "harvest_moon_cat", ("한가위 보름달 고양이", "휘영청 밝은 보름달 아래에서 이웃들과 따뜻한 마음을 나눕니다.") },
                { "spring_wind_guardian_cat", ("봄바람 정령 고양이", "포근한 봄바람을 몰고 와 겨울 산을 꽃밭으로 바꿉니다.") },
                { "first_snow_cat", ("첫눈 수호 고양이", "사뿐사뿐 첫눈 위를 걸으며 눈사람을 함께 만드는 고양이입니다.") },
                { "winter_aurora_empress_cat", ("겨울 오로라 여왕 고양이", "밤하늘을 일렁이는 영롱한 오로라 장막을 두르고 있습니다.") },
                { "autumn_mountain_guardian_cat", ("단풍 산맥 신령 고양이", "온 산을 불타는 붉은빛 단풍으로 장엄하게 단장하는 신령입니다.") },
                { "eternal_sky_spirit_cat", ("영원의 하늘 정령 고양이", "푸른 하늘의 기운을 담아 세상의 절기를 평화롭게 순환시킵니다.") },
                { "four_season_sacred_cat", ("사계절 성수 고양이", "봄·여름·가을·겨울 삼라만상의 자연을 지키는 숭고한 성수 고양이입니다.") }
            };
        }
    }
}
