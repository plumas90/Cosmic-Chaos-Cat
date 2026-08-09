using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    [RequireComponent(typeof(Image))]
    public sealed class NativeSizeOnSpriteChange : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 10f;

        private Image targetImage;
        private Sprite previousSprite;

        private void Awake()
        {
            targetImage = GetComponent<Image>();
            ApplyNativeSize();
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (targetImage.sprite != previousSprite)
                ApplyNativeSize();
        }

        private void ApplyNativeSize()
        {
            previousSprite = targetImage.sprite;

            if (previousSprite != null)
                targetImage.SetNativeSize();
        }
    }
}
