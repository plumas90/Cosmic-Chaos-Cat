#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CosmicChaosCat.Editor
{
    /// <summary>
    /// Editor menu tool to auto-populate ScriptableObject catalogs.
    /// Run via  CosmicChaosCat ▶ Build All Catalogs  in the Unity menu bar.
    /// This runs only in the Unity Editor — never at game runtime.
    /// </summary>
    public static class CatalogBuilder
    {
        private const string SoDir = "Assets/ScriptableObjects";

        // [MenuItem("CosmicChaosCat/Build All Catalogs %#b")]
        public static void BuildAll()
        {
            EnsureDirectory(SoDir);
            BuildCardCatalog();
            BuildSetCatalog();
            BuildUpgradeCatalog();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog(
                "완료",
                "카드 카탈로그, 세트 카탈로그, 업그레이드 카탈로그가 생성되었습니다.\n" +
                $"경로: {SoDir}",
                "확인");
        }

        // ── Card Catalog ───────────────────────────────────────────────────────

        // [MenuItem("CosmicChaosCat/Build Card Catalog Only")]
        public static void BuildCardCatalog()
        {
            EnsureDirectory(SoDir);
            string path = $"{SoDir}/CardCatalog.asset";
            var catalog = LoadOrCreate<CardCatalogSO>(path);

            var so = new SerializedObject(catalog);
            var cardsProp = so.FindProperty("cards");
            cardsProp.ClearArray();

            var list = GetAllCards();
            for (int i = 0; i < list.Count; i++)
            {
                cardsProp.InsertArrayElementAtIndex(i);
                var elem = cardsProp.GetArrayElementAtIndex(i);
                WriteCardEntry(elem, list[i]);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            Debug.Log($"[CatalogBuilder] CardCatalog: {list.Count}장 작성 완료 → {path}");
        }

        // ── Set Catalog ────────────────────────────────────────────────────────

        [MenuItem("CosmicChaosCat/Build Set Catalog Only")]
        public static void BuildSetCatalog()
        {
            EnsureDirectory(SoDir);
            string path = $"{SoDir}/SetCatalog.asset";
            var catalog = LoadOrCreate<SetCatalogSO>(path);

            var so = new SerializedObject(catalog);
            var setsProp = so.FindProperty("sets");
            setsProp.ClearArray();

            var list = GetAllSets();
            for (int i = 0; i < list.Count; i++)
            {
                setsProp.InsertArrayElementAtIndex(i);
                var elem = setsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("SetId").stringValue            = list[i].SetId;
                elem.FindPropertyRelative("SetName").stringValue          = list[i].SetName;
                elem.FindPropertyRelative("SetCardWeightBonus").floatValue = list[i].WeightBonus;
                elem.FindPropertyRelative("StackEffectBonus").floatValue  = list[i].StackBonus;
                elem.FindPropertyRelative("ShardBonusMultiplier").floatValue = 1.2f;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            Debug.Log($"[CatalogBuilder] SetCatalog: {list.Count}세트 작성 완료 → {path}");
        }

        // ── Upgrade Catalog ────────────────────────────────────────────────────

        [MenuItem("CosmicChaosCat/Build Upgrade Catalog Only")]
        public static void BuildUpgradeCatalog()
        {
            EnsureDirectory(SoDir);
            string path = $"{SoDir}/UpgradeCatalog.asset";
            var catalog = LoadOrCreate<UpgradeCatalogSO>(path);

            var so = new SerializedObject(catalog);
            var upgProp = so.FindProperty("upgrades");
            upgProp.ClearArray();

            var list = GetAllUpgrades();
            for (int i = 0; i < list.Count; i++)
            {
                upgProp.InsertArrayElementAtIndex(i);
                var elem = upgProp.GetArrayElementAtIndex(i);
                var u = list[i];

                elem.FindPropertyRelative("UpgradeId").stringValue    = u.Id;
                elem.FindPropertyRelative("DisplayName").stringValue  = u.DisplayName;
                elem.FindPropertyRelative("Description").stringValue  = u.Description;
                elem.FindPropertyRelative("Category").enumValueIndex  = (int)u.Category;
                elem.FindPropertyRelative("MaxLevel").intValue        = u.MaxLevel;
                elem.FindPropertyRelative("EffectType").enumValueIndex= (int)u.EffectType;

                WriteDblArray(elem.FindPropertyRelative("CostPerLevel"),    u.Costs);
                WriteFloatArray(elem.FindPropertyRelative("EffectValuePerLevel"), u.Effects);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            Debug.Log($"[CatalogBuilder] UpgradeCatalog: {list.Count}개 업그레이드 작성 완료 → {path}");
        }

        // ── Data Definitions ──────────────────────────────────────────────────

        private static List<CardDef> GetAllCards()
        {
            // All R rarity for now — adjust Rarity, ClickMultiplier etc. in the Inspector later.
            return new List<CardDef>
            {
                // ── 일반 카드 ──────────────────────────────────────────────
                R("cat-tophat",           "중절모쓴 흰고양이",          "meme"),
                R("cat-calico",           "삼색고양이",                 "basic"),
                R("cat-cheese",           "치즈냥이",                   "food"),
                R("cat-black",            "검정냥이",                   "basic"),
                R("cat-thunder",          "썬더냥이",                   "fantasy"),
                R("cat-chimera",          "키메라 냥이",                "fantasy"),
                R("cat-puss-in-boots",    "장화신은고양이",             "fantasy"),
                R("cat-cerberus",         "케로베로스 셰버",             "fantasy"),
                R("cat-chapi-chapo",      "치피치피차파차파",            "meme"),
                R("cat-popcat",           "팝캣",                       "meme"),
                R("cat-nyancat",          "냥캣",                       "internet"),
                R("cat-nabitang",         "나비탕",                     "meme"),
                R("cat-nyanpunch",        "냥냥 펀치",                  "internet"),
                R("cat-bongocat",         "봉고캣",                     "internet"),
                R("cat-gaejugi",          "개죽이",                     "meme"),
                R("cat-buttertoast",      "버터토스트 무한동력",         "internet"),
                R("cat-loaf",             "식빵",                       "food"),
                R("cat-breadcat",         "식빵캣",                     "food"),
                R("cat-spinning",         "Flying Spinning Cat",         "internet"),
                R("cat-crying",           "우는 고양이",                "meme"),
                R("cat-sad-dance",        "슬픈 고양이 댄스",           "meme"),
                R("cat-woman-yelling",    "Woman Yelling at a Cat",      "meme"),
                R("cat-meowl",            "Meowl",                      "internet"),
                R("cat-frozen-river",     "꽁꽁얼어붙은항강위로",        "meme"),
                R("cat-minecraft",        "마크 네모네모 고양이",        "game"),
                R("cat-round",            "동글동글고양이",              "basic"),
                R("cat-hug",              "안아줘요 냥이",               "basic"),
                R("cat-desert-frog",      "사막비개구리",                "meme"),
                R("cat-thunder2",         "고양이썬더",                  "fantasy"),
                R("cat-person-costume",   "고양이 탈 사람",              "meme"),
                R("cat-dark-eyes",        "검정 눈만 고양이",            "basic"),
                R("cat-murphy",           "머피의 법칙 사다리 고양이",   "meme"),
                R("cat-martial-arts",     "AI 무술 고양이",              "internet"),
                R("cat-cone",             "동물병원 콘 고양이",          "basic"),
                R("cat-littleprince-box", "어린 왕자 상자",              "fantasy"),
                R("cat-littleprince-hat", "어린 왕자 모자",              "fantasy"),
                R("cat-snowleopard-tail", "자기 꼬리 문 설표",           "basic"),
                R("cat-box-full",         "박스 꽉 채운 고양이",         "basic"),
                R("cat-glass-bottom",     "유리 밑에서 본 고양이",       "basic"),
                R("cat-tiger-shadow",     "호랑이 그림자 짤",            "meme"),
                R("cat-spiky",            "스피키냥이",                  "internet"),
                R("cat-idw",              "아이디다브류",                "internet"),
                R("cat-letters",          "CAT 글자 고양이",             "internet"),
                R("cat-lonely-person",    "양옆 고양이 혼자인 사람",     "meme"),
                R("cat-glass-push",       "식탁 유리컵 떨구기",          "basic"),
                R("cat-drawn",            "내가 그린 고양이",            "meme"),
                R("cat-broad-shoulders",  "어깨 넓은 고양이",            "meme"),
                R("cat-bus",              "고양이 버스",                 "fantasy"),
                R("cat-trojan",           "트로이 목마 캣",              "fantasy"),
                R("cat-constellation",    "우주 별자리 캣",              "fantasy"),
                R("cat-four-monster",     "Four Cat Monster",            "fantasy"),
                R("cat-food-countries",   "나라별 음식 냥이",            "food"),
                R("cat-hotdog",           "통모짜핫도그",                "food"),
                R("cat-sleeping-cool",    "요즘잘자쿨냥이",              "internet"),
                R("cat-maneki-neko",      "마네키네코",                  "fantasy"),
                R("cat-burger",           "햄버거 고양이",               "food"),
                R("cat-sofa",             "쇼파에 고양이",               "basic"),
                R("cat-product",          "고양이 제품",                 "internet"),
                R("cat-river-bag",        "강에 버리는 자루 고양이",     "meme"),
                R("cat-box-paws",         "박스 발만 튀어나온 고양이",   "basic"),
                R("cat-mural",            "고양이 벽화",                 "internet"),
                R("cat-anubis",           "아누비스 고양이",             "fantasy"),
                R("cat-roomba",           "로봇 청소기 위 고양이",       "basic"),
                R("cat-rainbow-monitor",  "무지개 모니터 고양이",        "internet"),
                R("cat-bonobono-rainbow", "보노보노 무지개 고양이",      "internet"),
                R("cat-pawprints",        "고양이 발자국",               "basic"),
                R("cat-yuno",             "미래일기 유노짤",             "meme"),
                R("cat-bremen",           "브레멘음악대",                "fantasy"),
                R("cat-bell",             "고양이 목에 방울 달기",       "meme"),
                R("cat-liquid",           "고양이 액체설",               "internet"),
                R("cat-witch",            "마녀와 고양이",               "fantasy"),
                R("cat-scratch-move",     "고양이 긁기 발자국",          "basic"),
                R("cat-tower",            "캣타워",                      "basic"),
                R("cat-microwave",        "고양이 전자레인지",           "meme"),
                R("cat-yugioh",           "유희왕 카드 고양이",          "game"),
                R("cat-emergency-exit",   "비상탈출구",                  "meme"),
                R("cat-sleeping-chess",   "잠자는 체스 고양이",          "game"),
                R("cat-gingerbread",      "진저브레드 쿠키 캣",          "food"),
                R("cat-hwatu",            "화투",                        "game"),
                R("cat-tarot",            "타로",                        "game"),
                R("cat-wheel",            "고양이 캣휠",                 "basic"),
                R("cat-tail-chase",       "꼬리 잡으려는 고양이",        "basic"),
                R("cat-miku",             "미쿠 모니터링",               "internet"),
                R("cat-sprigatito",       "냐오하",                      "game"),
                R("cat-gurren-lagann",    "그렌라간 선글라스",            "game"),
                R("cat-digimon-goggles",  "디지몬 고글",                 "game"),
                R("cat-demian-eyepatch",  "데미안 안대",                 "game"),
                R("cat-scouter",          "스카우터 드래곤볼",            "game"),
                R("cat-monocle",          "모노클",                      "basic"),
                R("cat-isaac-tech",       "아이작 테크 X",               "game"),
                R("cat-eyepatch",         "안대",                        "basic"),
                R("cat-red-riding-hood",  "빨간망토 두건",               "fantasy"),
                R("cat-robin-hood",       "로빈 후드 깃털모자",          "fantasy"),
                R("cat-bandage",          "반창고 붙이기",               "basic"),
                R("cat-mermaid",          "인어공주",                    "fantasy"),
                R("cat-genie",            "램프의요정",                  "fantasy"),
                R("cat-cannon",           "고양이대포",                  "fantasy"),
                R("cat-human-tower",      "인간탑",                      "meme"),
                R("cat-eaten",            "먹히는 고양이",               "meme"),
                R("cat-hairball",         "털뭉치",                      "basic"),
                R("cat-fish-tank",        "어항에 비친 고양이",          "basic"),
                R("cat-stretch",          "늘어나는 고양이",             "internet"),
                R("cat-furry-stages",     "퍼리의 5단계",                "internet"),
                R("cat-rich-furry",       "수상하게 돈이 많은 퍼리",     "internet"),

                // ── 숨겨진 카드 (IsHidden = true) ────────────────────────
                Hidden("hidden-speed",   "초고속 냥이",  "1초에 20번 클릭으로 해금"),
                Hidden("hidden-veteran", "베테랑 냥이",  "총 1000번 클릭으로 해금"),
            };
        }

        private static List<SetDef> GetAllSets()
        {
            return new List<SetDef>
            {
                new SetDef("basic",    "기본 냥이 세트",  1.15f, 0.3f),
                new SetDef("internet", "인터넷 밈 세트",  1.2f,  0.4f),
                new SetDef("meme",     "밈 카드 세트",    1.2f,  0.4f),
                new SetDef("food",     "음식 고양이 세트", 1.15f, 0.3f),
                new SetDef("fantasy",  "판타지 냥이 세트", 1.25f, 0.5f),
                new SetDef("game",     "게임 밈 세트",    1.2f,  0.4f),
                new SetDef("internet2","인터넷 이름 세트", 1.15f, 0.3f),
            };
        }

        private static List<UpgradeDef> GetAllUpgrades()
        {
            return new List<UpgradeDef>
            {
                // ── Click 계열 ──────────────────────────────────────────────
                new UpgradeDef("upg-crit-chance",  "크리티컬 확률 증가",
                    "클릭 크리티컬 발동 확률을 높입니다.",
                    UpgradeCategory.Click, UpgradeEffectType.CriticalChance,
                    3,
                    new double[]{100, 300, 800},
                    new float[] {0.05f, 0.10f, 0.15f}),

                new UpgradeDef("upg-crit-mult",    "크리티컬 배율 증가",
                    "크리티컬 발동 시 수익 배율을 높입니다.",
                    UpgradeCategory.Click, UpgradeEffectType.CriticalMultiplier,
                    3,
                    new double[]{200, 600, 2000},
                    new float[] {1f, 2f, 4f}),

                new UpgradeDef("upg-combo",        "연타 보너스 강화",
                    "연속 클릭 콤보 수익 보너스를 증가시킵니다.",
                    UpgradeCategory.Click, UpgradeEffectType.ComboBonus,
                    3,
                    new double[]{150, 500, 1500},
                    new float[] {0.2f, 0.5f, 1.0f}),

                // ── Gacha 계열 ──────────────────────────────────────────────
                new UpgradeDef("upg-n-weight",     "N등급 확률 감소 I",
                    "N등급 카드 등장 가중치를 줄여 상위 등급 확률을 올립니다.",
                    UpgradeCategory.Gacha, UpgradeEffectType.NWeightReduction,
                    3,
                    new double[]{500, 2000, 8000},
                    new float[] {0.01f, 0.03f, 0.05f}),

                new UpgradeDef("upg-r-weight",     "R등급 확률 감소 I",
                    "R등급 카드 등장 가중치를 줄여 SR 이상 확률을 올립니다.",
                    UpgradeCategory.Gacha, UpgradeEffectType.RWeightReduction,
                    3,
                    new double[]{1000, 4000, 15000},
                    new float[] {0.01f, 0.03f, 0.05f}),

                new UpgradeDef("upg-extra-pull",   "10+1 뽑기 해금",
                    "10회 뽑기 시 1회를 추가로 뽑습니다 (11연차).",
                    UpgradeCategory.Gacha, UpgradeEffectType.ExtraGachaPull,
                    1,
                    new double[]{3000},
                    new float[] {1f}),

                new UpgradeDef("upg-gacha-disc",   "가챠 비용 할인",
                    "가챠 비용을 단계적으로 할인합니다.",
                    UpgradeCategory.Gacha, UpgradeEffectType.GachaDiscount,
                    3,
                    new double[]{800, 3000, 10000},
                    new float[] {0.01f, 0.03f, 0.05f}),

                // ── Economy 계열 ────────────────────────────────────────────
                new UpgradeDef("upg-shard-refund", "카드 조각 환급 증가",
                    "중복 카드 분해 시 획득하는 조각 수를 늘립니다.",
                    UpgradeCategory.Economy, UpgradeEffectType.ShardRefundBonus,
                    3,
                    new double[]{400, 1500, 5000},
                    new float[] {0.2f, 0.5f, 1.0f}),
            };
        }

        // ── Helper Methods ─────────────────────────────────────────────────────

        private static CardDef R(string id, string name, string setId)
            => new CardDef
            {
                Id = id, DisplayName = name, SetId = setId,
                Rarity = CardRarity.R, BaseWeight = 100f,
                ClickMultiplier = 1.4f, ShardValue = CardShardValue.Value_5, MaxStacks = 5,
                IsHidden = false
            };

        private static CardDef Hidden(string id, string name, string hint)
            => new CardDef
            {
                Id = id, DisplayName = name, SetId = string.Empty,
                Rarity = CardRarity.SR, BaseWeight = 0f,
                ClickMultiplier = 2.0f, ShardValue = CardShardValue.Value_10, MaxStacks = 1,
                IsHidden = true
            };

        private static void WriteCardEntry(SerializedProperty elem, CardDef c)
        {
            elem.FindPropertyRelative("Id").stringValue            = c.Id;
            elem.FindPropertyRelative("DisplayName").stringValue   = c.DisplayName;
            elem.FindPropertyRelative("Rarity").enumValueIndex     = (int)c.Rarity;
            elem.FindPropertyRelative("BaseWeight").floatValue     = c.BaseWeight;
            elem.FindPropertyRelative("ClickMultiplier").floatValue= c.ClickMultiplier;
            elem.FindPropertyRelative("ShardValue").intValue       = (int)c.ShardValue;
            elem.FindPropertyRelative("MaxStacks").intValue        = c.MaxStacks;
            elem.FindPropertyRelative("SetId").stringValue         = c.SetId ?? string.Empty;
            elem.FindPropertyRelative("IsHidden").boolValue        = c.IsHidden;
            elem.FindPropertyRelative("SpecialEffect").enumValueIndex = 0;
            elem.FindPropertyRelative("SpecialEffectValue").floatValue = 0f;
        }

        private static void WriteDblArray(SerializedProperty prop, double[] values)
        {
            prop.ClearArray();
            for (int i = 0; i < values.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).doubleValue = values[i];
            }
        }

        private static void WriteFloatArray(SerializedProperty prop, float[] values)
        {
            prop.ClearArray();
            for (int i = 0; i < values.Length; i++)
            {
                prop.InsertArrayElementAtIndex(i);
                prop.GetArrayElementAtIndex(i).floatValue = values[i];
            }
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void EnsureDirectory(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        // ── Inner Data Classes (Editor-only) ───────────────────────────────────

        private class CardDef
        {
            public string         Id, DisplayName, SetId;
            public CardRarity     Rarity;
            public float          BaseWeight, ClickMultiplier;
            public CardShardValue ShardValue;
            public int            MaxStacks;
            public bool           IsHidden;
        }

        private class SetDef
        {
            public string       SetId, SetName;
            public float        WeightBonus, StackBonus;
            public SetDef(string id, string name, float wb, float sb)
            { SetId=id; SetName=name; WeightBonus=wb; StackBonus=sb; }
        }

        private class UpgradeDef
        {
            public string          Id, DisplayName, Description;
            public UpgradeCategory Category;
            public UpgradeEffectType EffectType;
            public int             MaxLevel;
            public double[]        Costs;
            public float[]         Effects;
            public UpgradeDef(string id, string name, string desc,
                UpgradeCategory cat, UpgradeEffectType et,
                int maxLv, double[] costs, float[] effects)
            {
                Id=id; DisplayName=name; Description=desc;
                Category=cat; EffectType=et;
                MaxLevel=maxLv; Costs=costs; Effects=effects;
            }
        }
    }
}
#endif
