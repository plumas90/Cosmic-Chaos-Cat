using System;
using UnityEngine;

namespace CosmicChaosCat
{
    [Serializable]
    public sealed class BreakthroughStageSpriteSet
    {
        public int Stage;
        public Sprite[] Sprites;
    }

    public enum CardRarity { N, R, SR, SSR, UR, H }

    public enum CardShardValue
    {
        [InspectorName("1")]   Value_1   = 1,
        [InspectorName("3")]   Value_3   = 3,
        [InspectorName("5")]   Value_5   = 5,
        [InspectorName("10")]  Value_10  = 10,
        [InspectorName("50")]  Value_50  = 50,
        [InspectorName("100")] Value_100 = 100
    }

    public enum CardSpecialEffect
    {
        None,
        CriticalChanceBonus,
        DuplicateBonusBonus,
        ShardRefundBonus
    }

    public enum CardShopCurrency
    {
        Coin,
        Shard
    }

    public class SetIdAttribute : PropertyAttribute { }

    [Serializable]
    public sealed class CardEntry
    {
        public string Id;
        public string DisplayName;
        public string DisplayName_EN;
        public CardRarity Rarity;
        public float BaseWeight = 100f;

        [UnityEngine.Serialization.FormerlySerializedAs("ClickMultiplier")]
        [Tooltip("장착 시 클릭 당 기본 획득 골드 (Click/Gold)")]
        public float ClickGold = 1f;

        public float ClickMultiplier
        {
            get => ClickGold;
            set => ClickGold = value;
        }

        public CardShardValue ShardValue = CardShardValue.Value_1;
        public int MaxStacks = 6;
        [SetId] public string SetId;
        public bool IsHidden;
        public bool IsShop; // If true, card only appears in Shop and is excluded from Gacha
        public CardShopCurrency ShopCurrency = CardShopCurrency.Coin;
        public double ShopPrice = 1000;
        public Sprite CardSprite;
        [Tooltip("가챠 연출의 cardbase 및 상점 ShardContent Front 배경 이미지")]
        public Sprite GachaBgSprite;

        public Sprite GachaBg
        {
            get => GachaBgSprite != null ? GachaBgSprite : CardSprite;
            set => GachaBgSprite = value;
        }

        public int[] BreakthroughVariantStages; // e.g. {1, 2, 3, 4, 5} or {1, 3, 5}
        public Sprite[] BreakthroughSprites;     // Variant sprites corresponding to stages 1..5
        [Tooltip("클릭 연출 등에 사용하는 별도 효과 스프라이트")]
        public Sprite[] EffectSprites;
        [Tooltip("EffectSprites 안에서 첫 번째 효과 묶음이 끝나는 위치. 0이면 묶음 구분 없음")]
        public int EffectSpriteGroupSplit;
        [Tooltip("한 단계에 여러 이미지가 있을 때 사용하는 단계별 이미지 묶음")]
        public BreakthroughStageSpriteSet[] BreakthroughSpriteVariants;

        [Tooltip("한계돌파 단계별 설명문 사용 여부")]
        public bool UseBreakthroughDescriptions;

        [TextArea(2, 4)]
        [Tooltip("한계돌파 단계별 한국어 설명문 (BreakthroughVariantStages 순서와 1:1 대응)")]
        public string[] BreakthroughDescriptions;

        [TextArea(2, 4)]
        [Tooltip("한계돌파 단계별 영어 설명문 (BreakthroughVariantStages 순서와 1:1 대응)")]
        public string[] BreakthroughDescriptions_EN;
        public CardSpecialEffect SpecialEffect;
        public float SpecialEffectValue;
        [TextArea(3, 5)] public string Description;
        [TextArea(3, 5)] public string Description_EN;

        public System.Collections.Generic.List<int> GetBreakthroughStages()
        {
            if (IsBallOfYarnTouch())
                return new System.Collections.Generic.List<int>();

            if (int.TryParse(Id, out int cardNumber) && cardNumber == 169)
                return new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };
            if (int.TryParse(Id, out cardNumber) && cardNumber == 236)
                return new System.Collections.Generic.List<int> { 1, 2, 3, 4, 5 };

            if (BreakthroughVariantStages != null && BreakthroughVariantStages.Length > 0)
            {
                var list = new System.Collections.Generic.List<int>();
                foreach (int st in BreakthroughVariantStages)
                {
                    if (st >= 1 && st <= 5 && !list.Contains(st)) list.Add(st);
                }
                if (list.Count > 0) return list;
            }
            return new System.Collections.Generic.List<int>();
        }

        public Sprite GetSpriteForStage(int stage)
        {
            // Ball_of_yarn_touch의 추가 프레임은 클릭 반복용이며 한계돌파 진화가 아니다.
            // 도감/장착/단계 표시는 항상 대표 CardSprite를 유지한다.
            if (IsBallOfYarnTouch())
                return CardSprite;

            if (int.TryParse(Id, out int clickVariantCardNumber))
            {
                // These multi-sprite cards change only through clicks, never through breakthrough.
                if (clickVariantCardNumber == 180)
                    return CardSprite;

                // Schrodinger shows frame 2 only as its 5-star encyclopedia illustration.
                // Its equipped auto animation still starts independently from frame 0.
                if (clickVariantCardNumber == 201)
                {
                    if (stage >= 5 && BreakthroughSprites != null && BreakthroughSprites.Length > 2 && BreakthroughSprites[2] != null)
                        return BreakthroughSprites[2];
                    return CardSprite;
                }

                // The two stored sprites are animation pieces. Encyclopedia and
                // normal card views always use the combined full illustration.
                if (clickVariantCardNumber == 224)
                    return CardSprite;

                // Misfortune stores ladder layers and walking frames as effect
                // pieces. Encyclopedia slots always show the representative cat.
                if (clickVariantCardNumber == 232)
                    return CardSprite;

                // Hungry_2 is a momentary bite reaction, not a breakthrough image.
                if (clickVariantCardNumber == 233)
                    return CardSprite;

                if (clickVariantCardNumber == 234)
                {
                    if (stage >= 5 && BreakthroughSprites != null && BreakthroughSprites.Length > 2 && BreakthroughSprites[2] != null)
                        return BreakthroughSprites[2];
                    if (stage >= 3 && BreakthroughSprites != null && BreakthroughSprites.Length > 1 && BreakthroughSprites[1] != null)
                        return BreakthroughSprites[1];
                    return CardSprite;
                }

                // Portal Cat stores five cumulative breakthrough appearances,
                // followed by two portal-effect sprites.
                if (clickVariantCardNumber == 236)
                {
                    int portalIndex = Mathf.Clamp(stage - 1, 0, 4);
                    if (BreakthroughSprites != null && BreakthroughSprites.Length > portalIndex &&
                        BreakthroughSprites[portalIndex] != null)
                        return BreakthroughSprites[portalIndex];
                    return CardSprite;
                }

                // These arrays contain click-effect frames and props, not
                // breakthrough illustrations. Catalog/detail art stays fixed.
                if (clickVariantCardNumber == 237 || clickVariantCardNumber == 238 ||
                    clickVariantCardNumber == 261 || clickVariantCardNumber == 266 || clickVariantCardNumber == 268 ||
                    clickVariantCardNumber == 288 || clickVariantCardNumber == 289 ||
                    clickVariantCardNumber == 290 || clickVariantCardNumber == 291 ||
                    clickVariantCardNumber == 312 || clickVariantCardNumber == 314 || clickVariantCardNumber == 315 ||
                    clickVariantCardNumber == 320 || clickVariantCardNumber == 322 || clickVariantCardNumber == 323 ||
                    clickVariantCardNumber == 326 || clickVariantCardNumber == 327 || clickVariantCardNumber == 336)
                    return CardSprite;
            }

            bool isBuffHalfCat = int.TryParse(Id, out int cardNumber) && cardNumber == 169;
            if (isBuffHalfCat && stage <= 1)
                return CardSprite;

            var stageSprites = GetSpritesForStage(stage);
            if (stageSprites.Count > 0 && stageSprites[0] != null)
                return stageSprites[0];

            if (BreakthroughSprites != null && BreakthroughSprites.Length > 0)
            {
                if (BreakthroughVariantStages != null && BreakthroughVariantStages.Length == BreakthroughSprites.Length)
                {
                    Sprite activeSprite = CardSprite;
                    int activeStage = int.MinValue;
                    for (int i = 0; i < BreakthroughVariantStages.Length; i++)
                    {
                        int mappedStage = BreakthroughVariantStages[i];
                        if (mappedStage <= stage && mappedStage > activeStage && BreakthroughSprites[i] != null)
                        {
                            activeStage = mappedStage;
                            activeSprite = BreakthroughSprites[i];
                        }
                    }
                    return activeSprite;
                }
                int idx = stage - 1;
                if (idx >= 0 && idx < BreakthroughSprites.Length && BreakthroughSprites[idx] != null)
                    return BreakthroughSprites[idx];
            }
            return CardSprite;
        }

        public bool IsBallOfYarnTouch()
        {
            if (CardSprite != null &&
                CardSprite.name.IndexOf("Ball_of_yarn_touch", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (BreakthroughSprites != null)
            {
                foreach (var sprite in BreakthroughSprites)
                    if (sprite != null &&
                        sprite.name.IndexOf("Ball_of_yarn_touch", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
            }

            return false;
        }

        public System.Collections.Generic.List<Sprite> GetSpritesForStage(int stage)
        {
            var result = new System.Collections.Generic.List<Sprite>();
            if (BreakthroughSpriteVariants != null)
            {
                foreach (var set in BreakthroughSpriteVariants)
                {
                    if (set == null || set.Stage != stage || set.Sprites == null) continue;
                    foreach (var sprite in set.Sprites)
                        if (sprite != null && !result.Contains(sprite)) result.Add(sprite);
                    break;
                }
            }

#if UNITY_EDITOR
            // Newly sliced 169 sprites can be temporarily unresolved after a meta/catalog edit.
            // Resolve them by their stable slice names so breakthrough preview and cutscene still work.
            if (result.Count == 0 && int.TryParse(Id, out int cardNumber) && cardNumber == 169 && stage >= 2)
            {
                string path = stage <= 3
                    ? "Assets/image/A_No/169_174meme_cat/169_SR_Buff_Half_Cat2_3.png"
                    : "Assets/image/A_No/169_174meme_cat/169_SR_Buff_Half_Cat4_5.png";
                string prefix = $"169_SR_Buff_Half_Cat{stage}-";
                var loaded = new System.Collections.Generic.List<Sprite>();
                foreach (var asset in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
                    if (asset is Sprite sprite && sprite.name.StartsWith(prefix)) loaded.Add(sprite);
                loaded.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                result.AddRange(loaded);
            }
#endif
            return result;
        }

        public string GetDisplayName(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN" && !string.IsNullOrEmpty(DisplayName_EN)) return DisplayName_EN;
            return !string.IsNullOrEmpty(DisplayName) ? DisplayName : Id;
        }

        public string GetDescription(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            if (lang == "EN")
            {
                if (!string.IsNullOrEmpty(Description_EN)) return Description_EN;
                return $"{GetDisplayName(lang)} is a {Rarity} grade cat card. Provides click multiplier of x{ClickMultiplier}.";
            }
            if (!string.IsNullOrEmpty(Description)) return Description;
            return $"{DisplayName}은(는) {Rarity} 등급의 고양이 카드입니다. 특수 클릭 수익 배율 {ClickMultiplier}배를 제공합니다.";
        }

        public string GetDescriptionForStage(int stage, string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
            {
                var gm = GameManager.Instance != null ? GameManager.Instance : UnityEngine.Object.FindObjectOfType<GameManager>(true);
                lang = gm != null ? gm.SelectedLanguage : "KR";
            }
            bool isEN = lang == "EN";

            if (UseBreakthroughDescriptions && BreakthroughVariantStages != null && BreakthroughVariantStages.Length > 0)
            {
                var descs = isEN ? BreakthroughDescriptions_EN : BreakthroughDescriptions;
                if (isEN && (descs == null || descs.Length == 0))
                {
                    descs = BreakthroughDescriptions;
                }

                if (descs != null && descs.Length > 0)
                {
                    string foundDesc = null;
                    int maxMatchingStage = -1;

                    for (int i = 0; i < BreakthroughVariantStages.Length; i++)
                    {
                        int st = BreakthroughVariantStages[i];
                        if (st <= stage && st > maxMatchingStage)
                        {
                            string d = (i < descs.Length) ? descs[i] : null;
                            if (!string.IsNullOrWhiteSpace(d))
                            {
                                foundDesc = d;
                                maxMatchingStage = st;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(foundDesc))
                    {
                        return foundDesc;
                    }
                }
            }

            return GetDescription(lang);
        }
    }
}
