using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    /// <summary>도감처럼 단일 Image만 지원하는 화면에 Burger Maker 완성 모습을 합성한다.</summary>
    public static class BurgerMakerDisplayHelper
    {
        private const string ContainerName = "BurgerMakerCompletedPreview";
        private static readonly float[] LayerY = { -62f, -22f, -10f, 10f, 82f, 95f, 136f, 185f };

        public static void Bind(Image host, CardEntry card, bool visible)
        {
            if (host == null) return;
            bool isBurgerMaker = visible && card != null && card.Id == "0326" &&
                                 card.EffectSprites != null && card.EffectSprites.Length >= 8;
            Transform existing = host.transform.Find(ContainerName);

            if (!isBurgerMaker)
            {
                if (existing != null) Object.Destroy(existing.gameObject);
                return;
            }

            if (existing == null)
            {
                GameObject root = new GameObject(ContainerName, typeof(RectTransform));
                root.transform.SetParent(host.transform, false);
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;
                rootRect.SetAsLastSibling();

                float width = Mathf.Abs(host.rectTransform.rect.width);
                if (width <= 0f) width = Mathf.Abs(host.rectTransform.sizeDelta.x);
                if (width <= 0f) width = 400f;
                float scale = width / 400f;

                for (int i = 0; i < 8; i++)
                {
                    Sprite sprite = card.EffectSprites[i];
                    if (sprite == null) continue;
                    GameObject layerObject = new GameObject($"BurgerIngredient_{i}", typeof(RectTransform), typeof(Image));
                    layerObject.transform.SetParent(rootRect, false);
                    Image layer = layerObject.GetComponent<Image>();
                    layer.sprite = sprite;
                    layer.preserveAspect = true;
                    layer.raycastTarget = false;
                    RectTransform rect = layer.rectTransform;
                    float layerWidth = width * Mathf.Clamp(sprite.rect.width / 808f, 0.7f, 1f);
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.sizeDelta = new Vector2(layerWidth, layerWidth * sprite.rect.height / sprite.rect.width);
                    rect.anchoredPosition = new Vector2(0f, LayerY[i] * scale);
                }
            }

            host.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
