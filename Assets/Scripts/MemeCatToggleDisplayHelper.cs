using UnityEngine;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public static class MemeCatToggleDisplayHelper
    {
        private static Sprite sprite170_A;
        private static Sprite sprite170_B;
        private static Sprite sprite174_A;
        private static Sprite sprite174_B;

        public static void EnsureSpritesLoaded()
        {
            if (sprite170_A != null && sprite170_B != null && sprite174_A != null && sprite174_B != null) return;

#if UNITY_EDITOR
            var assets170_1 = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/170_SR_Huh_Cat.png");
            foreach (var a in assets170_1) if (a is Sprite s) sprite170_A = s;

            var assets170_2 = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/170_SR_Huh_Cat2.png");
            foreach (var a in assets170_2) if (a is Sprite s) sprite170_B = s;

            var assets174_1 = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/174_SR_pop-cat1.png");
            foreach (var a in assets174_1) if (a is Sprite s) sprite174_A = s;

            var assets174_2 = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/174_SR_pop-cat2.png");
            foreach (var a in assets174_2) if (a is Sprite s) sprite174_B = s;
#endif
        }

        public static bool IsLoopCat(string cardId, CardEntry entry)
        {
            var list = GetSpriteListForCard(cardId, entry);
            return list != null && list.Count >= 2;
        }

        public static List<Sprite> GetSpriteListForCard(string cardId, CardEntry entry, int stage = 0)
        {
            EnsureSpritesLoaded();
            List<Sprite> sprites = new List<Sprite>();

            int num = 0;
            if (int.TryParse(cardId, out int parsed)) num = parsed;
            bool isBremenClickToggle = num >= 175 && num <= 178;
            bool isEarthCatSocietyAngleToggle = num == 180;
            bool isSevenDeadlySinsToggle = num == 181;

            // A stage can contain multiple illustrations. Always keep that group
            // separate from the other breakthrough stages.
            if (entry != null && stage >= 1)
            {
                var stageSprites = entry.GetSpritesForStage(stage);
                if (stageSprites.Count > 0) return stageSprites;

                // 170 / 174 are legacy two-image toggle cards whose images are
                // stored in separate files rather than per-stage variant groups.
                if (num != 170 && num != 174 && !isBremenClickToggle &&
                    !isEarthCatSocietyAngleToggle && !isSevenDeadlySinsToggle)
                {
                    Sprite representative = entry.GetSpriteForStage(stage);
                    if (representative != null) sprites.Add(representative);
                    return sprites;
                }
            }

            // 1. Gather all sprites from entry.BreakthroughSprites
            if (entry != null && entry.BreakthroughSprites != null)
            {
                foreach (var sp in entry.BreakthroughSprites)
                {
                    if (sp != null && !sprites.Contains(sp))
                    {
                        sprites.Add(sp);
                    }
                }
            }

            // 2. Editor / Fallback overrides for 170 / 174
            if (num == 170 || cardId == "0170" || cardId == "170")
            {
                if (sprite170_A != null && !sprites.Contains(sprite170_A)) sprites.Insert(0, sprite170_A);
                if (sprite170_B != null && !sprites.Contains(sprite170_B)) sprites.Add(sprite170_B);
            }
            else if (num == 174 || cardId == "0174" || cardId == "174")
            {
                if (sprite174_A != null && !sprites.Contains(sprite174_A)) sprites.Insert(0, sprite174_A);
                if (sprite174_B != null && !sprites.Contains(sprite174_B)) sprites.Add(sprite174_B);
            }

            // 3. Fallback to entry.CardSprite if empty
            if (sprites.Count == 0 && entry != null && entry.CardSprite != null)
            {
                sprites.Add(entry.CardSprite);
            }

            return sprites;
        }

        public static (Sprite spriteA, Sprite spriteB) GetSpritesForCard(string cardId, CardEntry entry)
        {
            var list = GetSpriteListForCard(cardId, entry);
            Sprite a = list.Count > 0 ? list[0] : null;
            Sprite b = list.Count > 1 ? list[1] : a;
            return (a, b);
        }
    }
}
