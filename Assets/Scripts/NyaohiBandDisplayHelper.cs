using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    public static class NyaohiBandDisplayHelper
    {
        private const string RootName = "NyaohiBandOverlay";

        public static void Bind(Image artImage, CardEntry card, int stage, bool visible = true)
        {
            if (artImage == null) return;

            Transform existing = artImage.transform.Find(RootName);
            bool active = visible && card != null && card.Id == "0364" && stage == 2 &&
                          card.EffectSprites != null && card.EffectSprites.Length > 0 &&
                          card.EffectSprites[0] != null;
            if (!active)
            {
                if (existing != null) existing.gameObject.SetActive(false);
                return;
            }

            RectTransform root;
            if (existing == null)
            {
                var rootObject = new GameObject(RootName, typeof(RectTransform));
                rootObject.transform.SetParent(artImage.transform, false);
                root = rootObject.GetComponent<RectTransform>();
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;

                CreateBand(root, "FrontPawBand", new Vector2(0.20f, 0.09f), -18f);
                CreateBand(root, "RearPawBand", new Vector2(0.84f, 0.09f), 14f);
            }
            else
            {
                root = (RectTransform)existing;
                root.gameObject.SetActive(true);
            }

            FitRootToDisplayedSprite(root, artImage);

            Sprite band = card.EffectSprites[0];
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                image.sprite = band;
                image.preserveAspect = true;
                image.color = Color.white;
            }
            root.SetAsLastSibling();
        }

        private static void FitRootToDisplayedSprite(RectTransform root, Image artImage)
        {
            Rect parentRect = artImage.rectTransform.rect;
            Sprite sprite = artImage.sprite;
            if (sprite == null || parentRect.width <= 0f || parentRect.height <= 0f || sprite.rect.height <= 0f)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                return;
            }

            float spriteAspect = sprite.rect.width / sprite.rect.height;
            float parentAspect = parentRect.width / parentRect.height;
            float width = parentRect.width;
            float height = parentRect.height;
            if (parentAspect > spriteAspect) width = height * spriteAspect;
            else height = width / spriteAspect;

            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(width, height);
            root.anchoredPosition = Vector2.zero;
        }

        private static void CreateBand(RectTransform parent, string name, Vector2 center, float rotation)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            Vector2 halfSize = new Vector2(0.13f, 0.105f);
            rect.anchorMin = center - halfSize;
            rect.anchorMax = center + halfSize;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            go.GetComponent<Image>().raycastTarget = false;
        }
    }
}
