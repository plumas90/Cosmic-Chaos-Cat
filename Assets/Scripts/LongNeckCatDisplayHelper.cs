using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    public static class LongNeckCatDisplayHelper
    {
        private static Sprite cachedHeadSprite;
        private static Sprite cachedBodySprite;
        private static Sprite cachedNeckSprite;

        public static void SetSprites(Sprite head, Sprite body, Sprite neck)
        {
            cachedHeadSprite = head;
            cachedBodySprite = body;
            cachedNeckSprite = neck;
        }

        public static void EnsureSpritesLoaded()
        {
            if (cachedHeadSprite != null && cachedBodySprite != null && cachedNeckSprite != null) return;

#if UNITY_EDITOR
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_No/169_174meme_cat/171_SR_long_neck_cat.png");
            foreach (var a in assets)
            {
                if (a is Sprite s)
                {
                    if (s.name.Contains("head")) cachedHeadSprite = s;
                    else if (s.name.Contains("body")) cachedBodySprite = s;
                    else if (s.name.Contains("neck")) cachedNeckSprite = s;
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

        public static Sprite GetNeckSprite()
        {
            EnsureSpritesLoaded();
            return cachedNeckSprite;
        }

        public static void BindLongNeckCardSlot(Transform frontTrans, CardEntry card)
        {
            if (frontTrans == null) return;

            Transform artTrans = frontTrans.Find("Art_image")
                        ?? frontTrans.Find("art_image")
                        ?? frontTrans.Find("Art")
                        ?? frontTrans.Find("CardArt")
                        ?? frontTrans.Find("Image")
                        ?? frontTrans;

            Transform headTrans = artTrans.Find("LN_Head");
            Transform bodyTrans = artTrans.Find("LN_Body");

            // Also check for any LN_Head / LN_Body that were attached to parent/children previously
            if (artTrans.parent != null)
            {
                var parentH = artTrans.parent.Find("LN_Head"); if (parentH != null && parentH != headTrans) parentH.gameObject.SetActive(false);
                var parentB = artTrans.parent.Find("LN_Body"); if (parentB != null && parentB != bodyTrans) parentB.gameObject.SetActive(false);
            }

            var mainImg = artTrans.GetComponent<Image>();

            bool is171 = card != null && (card.Id == "0171" || card.Id == "171" || (int.TryParse(card.Id, out int num) && num == 171));
            if (is171)
            {
                EnsureSpritesLoaded();

                // 1. Set Image component opacity (alpha) to 0 for composite card (171) & clear sprite
                if (mainImg != null)
                {
                    mainImg.sprite = null;
                    mainImg.color = new Color(1f, 1f, 1f, 0f); // Opacity 0 (transparent)
                }

                // 1.5 Unconditionally clear outer parent Image sprite so outer frame never draws head sprite
                if (artTrans.parent != null)
                {
                    var parentImg = artTrans.parent.GetComponent<Image>();
                    if (parentImg != null && parentImg.gameObject.name.ToLower() == "image")
                    {
                        parentImg.sprite = null;
                    }
                }

                // 2. Ensure Head child
                if (headTrans == null)
                {
                    var headGo = new GameObject("LN_Head", typeof(RectTransform), typeof(Image));
                    headGo.transform.SetParent(artTrans, false);
                    headTrans = headGo.transform;
                }
                headTrans.gameObject.SetActive(true);
                var headImg = headTrans.GetComponent<Image>();
                if (headImg != null)
                {
                    if (cachedHeadSprite != null) headImg.sprite = cachedHeadSprite;
                    headImg.preserveAspect = true;
                    headImg.color = Color.white;
                    headImg.raycastTarget = false;
                }
                var headRt = headTrans.GetComponent<RectTransform>();
                if (headRt != null)
                {
                    headRt.anchorMin = new Vector2(0f, 0.4707f);
                    headRt.anchorMax = new Vector2(1f, 1f);
                    headRt.offsetMin = Vector2.zero;
                    headRt.offsetMax = Vector2.zero;
                }

                // 3. Ensure Body child
                if (bodyTrans == null)
                {
                    var bodyGo = new GameObject("LN_Body", typeof(RectTransform), typeof(Image));
                    bodyGo.transform.SetParent(artTrans, false);
                    bodyTrans = bodyGo.transform;
                }
                bodyTrans.gameObject.SetActive(true);
                var bodyImg = bodyTrans.GetComponent<Image>();
                if (bodyImg != null)
                {
                    if (cachedBodySprite != null) bodyImg.sprite = cachedBodySprite;
                    bodyImg.preserveAspect = true;
                    bodyImg.color = Color.white;
                    bodyImg.raycastTarget = false;
                }
                var bodyRt = bodyTrans.GetComponent<RectTransform>();
                if (bodyRt != null)
                {
                    bodyRt.anchorMin = new Vector2(0f, 0f);
                    bodyRt.anchorMax = new Vector2(1f, 0.4707f);
                    bodyRt.offsetMin = Vector2.zero;
                    bodyRt.offsetMax = Vector2.zero;
                }
            }
            else
            {
                if (headTrans != null) headTrans.gameObject.SetActive(false);
                if (bodyTrans != null) bodyTrans.gameObject.SetActive(false);

                if (mainImg != null)
                {
                    mainImg.color = Color.white;
                    mainImg.preserveAspect = true;
                }
            }
        }
    }
}
