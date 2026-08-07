using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    public static class LongNeckCatDisplayHelper
    {
        private static Sprite cachedHeadSprite;
        private static Sprite cachedBodySprite;

        public static void EnsureSpritesLoaded()
        {
            if (cachedHeadSprite != null && cachedBodySprite != null) return;

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/171_SR_long_neck_cat.png");
            foreach (var a in assets)
            {
                if (a is Sprite s)
                {
                    if (s.name.Contains("head")) cachedHeadSprite = s;
                    else if (s.name.Contains("body")) cachedBodySprite = s;
                }
            }
#endif
        }

        public static Sprite GetHeadSprite()
        {
            EnsureSpritesLoaded();
            return cachedHeadSprite;
        }

        public static Sprite GetBodySprite()
        {
            EnsureSpritesLoaded();
            return cachedBodySprite;
        }

        public static void BindLongNeckCardSlot(Transform frontTrans, CardEntry card)
        {
            if (frontTrans == null) return;

            Transform artTrans = frontTrans.Find("Art")
                       ?? frontTrans.Find("CardArt")
                       ?? frontTrans.Find("Image");
            if (artTrans == null) return;

            var artRt = artTrans.GetComponent<RectTransform>();
            Transform parentTrans = artTrans.parent != null ? artTrans.parent : frontTrans;
            Transform bodyTrans = parentTrans.Find("Art (1)") ?? parentTrans.Find("Body");

            if (card != null && card.Id == "0171")
            {
                EnsureSpritesLoaded();

                var headImg = artTrans.GetComponent<Image>();
                if (headImg != null)
                {
                    if (cachedHeadSprite != null) headImg.sprite = cachedHeadSprite;
                    headImg.preserveAspect = false;
                    headImg.color = Color.white;
                }

                if (bodyTrans == null)
                {
                    var bodyGo = new GameObject("Art (1)", typeof(RectTransform), typeof(Image));
                    bodyGo.transform.SetParent(parentTrans, false);
                    bodyTrans = bodyGo.transform;
                }

                bodyTrans.gameObject.SetActive(true);
                var bodyImg = bodyTrans.GetComponent<Image>();
                if (bodyImg != null)
                {
                    if (cachedBodySprite != null) bodyImg.sprite = cachedBodySprite;
                    bodyImg.preserveAspect = false;
                    bodyImg.color = Color.white;
                }

                // Mathematical ratio: Head height = 424, Body height = 377, Total = 801
                // Head ratio = 424 / 801 = 0.5293 (top 52.93%), Body ratio = 377 / 801 = 0.4707 (bottom 47.07%)
                // Head bottom line & Body top line touch exactly at y = 0.4707 with ZERO GAP!
                if (artRt != null)
                {
                    artRt.anchorMin = new Vector2(0f, 0.4707f);
                    artRt.anchorMax = new Vector2(1f, 1f);
                    artRt.offsetMin = Vector2.zero;
                    artRt.offsetMax = Vector2.zero;

                    var bodyRt = bodyTrans.GetComponent<RectTransform>();
                    if (bodyRt != null)
                    {
                        bodyRt.anchorMin = new Vector2(0f, 0f);
                        bodyRt.anchorMax = new Vector2(1f, 0.4707f);
                        bodyRt.offsetMin = Vector2.zero;
                        bodyRt.offsetMax = Vector2.zero;
                    }
                }
            }
            else
            {
                if (bodyTrans != null)
                {
                    bodyTrans.gameObject.SetActive(false);
                }

                if (artRt != null)
                {
                    artRt.anchorMin = Vector2.zero;
                    artRt.anchorMax = Vector2.one;
                    artRt.offsetMin = Vector2.zero;
                    artRt.offsetMax = Vector2.zero;

                    var headImg = artTrans.GetComponent<Image>();
                    if (headImg != null) headImg.preserveAspect = true;
                }
            }
        }
    }
}
