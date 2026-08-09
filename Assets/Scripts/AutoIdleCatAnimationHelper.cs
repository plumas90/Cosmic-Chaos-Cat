using UnityEngine;
using System.Collections.Generic;

namespace CosmicChaosCat
{
    public static class AutoIdleCatAnimationHelper
    {
        private static List<Sprite> cached152Sprites = null;
        private static List<Sprite> cached195Sprites = null;

        public static bool IsAutoIdleCat(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            if (cardId == "0152" || cardId == "152" || cardId == "0195" || cardId == "195") return true;
            if (int.TryParse(cardId, out int num) && (num == 152 || num == 195)) return true;
            return false;
        }

        public static List<Sprite> GetAutoIdleSprites(string cardId, CardEntry entry)
        {
            if (!IsAutoIdleCat(cardId)) return null;

            int cardNumber = int.TryParse(cardId, out int parsed) ? parsed : 0;
            if (cardNumber == 195)
            {
                if (cached195Sprites != null && cached195Sprites.Count > 0)
                    return cached195Sprites;

                cached195Sprites = new List<Sprite>();
                if (entry != null && entry.BreakthroughSprites != null)
                {
                    foreach (var sprite in entry.BreakthroughSprites)
                        if (sprite != null && !cached195Sprites.Contains(sprite)) cached195Sprites.Add(sprite);
                }
                if (cached195Sprites.Count == 0 && entry != null && entry.CardSprite != null)
                    cached195Sprites.Add(entry.CardSprite);
                return cached195Sprites;
            }

            if (cached152Sprites != null && cached152Sprites.Count > 0)
                return cached152Sprites;

            cached152Sprites = new List<Sprite>();

#if UNITY_EDITOR
            // 유니티 에디터 상에서 152_missing_cat.png 슬라이스 스프라이트 전체 자동 수집
            string path = "Assets/image/A_No/152_missing_cat/152_missing_cat.png";
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets != null)
            {
                foreach (var a in assets)
                {
                    if (a is Sprite s && s != null)
                    {
                        if (!cached152Sprites.Contains(s))
                            cached152Sprites.Add(s);
                    }
                }
            }
#endif

            // BreakthroughSprites에 있는 스프라이트들도 추가 수집
            if (entry != null && entry.BreakthroughSprites != null)
            {
                foreach (var sp in entry.BreakthroughSprites)
                {
                    if (sp != null && !cached152Sprites.Contains(sp))
                    {
                        cached152Sprites.Add(sp);
                    }
                }
            }

            // 기본 CardSprite 추가
            if (cached152Sprites.Count == 0 && entry != null && entry.CardSprite != null)
            {
                cached152Sprites.Add(entry.CardSprite);
            }

            return cached152Sprites;
        }
    }
}
