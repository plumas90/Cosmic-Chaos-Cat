using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the MainDecoration Image component in the scene.
    /// Automatically updates the decoration sprite based on the equipped decoration.
    /// Animates the fish and mouse decorations while they cross the screen.
    /// </summary>
    public sealed class MainDecorationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image decoImage;

        private RectTransform rectTransform;
        private Canvas rootCanvas;

        // Sub-sprite frame animation fields
        [SerializeField] private Sprite[] mouseSprites;
        [SerializeField] private Sprite[] fishSprites;
        private float frameTimer = 0f;
        private int frameIndex = 0;
        private const float FRAME_DURATION = 0.25f;
        private static readonly int[] FRAME_SEQUENCE = { 0, 1, 2, 1 };
        private static readonly Vector3 DECORATION_SCALE = new Vector3(0.5f, 0.5f, 1f);

        // Movement parameters
        private float moveX = 0f;
        [SerializeField] private float moveSpeed = 220f;
        private string currentDecoId = string.Empty;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (decoImage == null) decoImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();
            LoadDecoSubSprites();
        }

        private void LoadDecoSubSprites()
        {
#if UNITY_EDITOR
            if (mouseSprites == null || mouseSprites.Length == 0)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_Deco/Deco_Mouse.png");
                var list = new List<Sprite>();
                foreach (var a in assets)
                    if (a is Sprite s) list.Add(s);
                list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
                mouseSprites = list.ToArray();
            }

            if (fishSprites == null || fishSprites.Length == 0)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_Deco/Deco_Fish.png");
                var list = new List<Sprite>();
                foreach (var a in assets)
                    if (a is Sprite s) list.Add(s);
                list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
                fishSprites = list.ToArray();
            }
#endif
        }

        private void OnEnable()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (gameManager != null)
            {
                gameManager.StateChanged += Refresh;
            }
            LoadDecoSubSprites();
            Refresh();
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.StateChanged -= Refresh;
            }
        }

        private void Update()
        {
            if (gameManager == null || decoImage == null || !decoImage.enabled) return;

            string decoId = gameManager.EquippedDecorationId;
            if (string.IsNullOrEmpty(decoId)) return;

            // 세 프레임을 1 → 2 → 3 → 2 → 1 순서로 왕복 재생한다.
            frameTimer += Time.deltaTime;
            if (frameTimer >= FRAME_DURATION)
            {
                frameTimer -= FRAME_DURATION;
                frameIndex++;
            }

            if (IsMouseToy(decoId))
            {
                if (mouseSprites != null && mouseSprites.Length > 0)
                {
                    Sprite sprite = mouseSprites[GetSequenceFrame(frameIndex, mouseSprites.Length)];
                    if (decoImage.sprite != sprite) decoImage.sprite = sprite;
                }
                UpdateLeftwardMovement();
            }
            else if (IsFishToy(decoId))
            {
                if (fishSprites != null && fishSprites.Length > 0)
                {
                    Sprite sprite = fishSprites[GetSequenceFrame(frameIndex, fishSprites.Length)];
                    if (decoImage.sprite != sprite) decoImage.sprite = sprite;
                }
                UpdateLeftwardMovement();
            }
            else
            {
                var entry = gameManager.DecorationCatalog?.FindById(decoId);
                if (entry != null && entry.AnimationSprites != null && entry.AnimationSprites.Length > 0)
                {
                    int index = frameIndex % entry.AnimationSprites.Length;
                    Sprite sprite = entry.AnimationSprites[index];
                    if (sprite != null && decoImage.sprite != sprite) decoImage.sprite = sprite;
                }
            }
        }

        private static int GetSequenceFrame(int index, int frameCount)
        {
            if (frameCount <= 1) return 0;
            return Mathf.Min(FRAME_SEQUENCE[index % FRAME_SEQUENCE.Length], frameCount - 1);
        }

        private bool IsMouseToy(string id) =>
            !string.IsNullOrEmpty(id) && id.Equals("deco-mouse", System.StringComparison.OrdinalIgnoreCase);

        private bool IsFishToy(string id) =>
            !string.IsNullOrEmpty(id) && id.Equals("deco-fish", System.StringComparison.OrdinalIgnoreCase);

        private void UpdateLeftwardMovement()
        {
            if (rectTransform == null) return;
            var canvasRT = GetCanvasRectTransform();
            if (canvasRT == null) return;

            float canvasWidth = canvasRT.rect.width;
            float canvasHeight = canvasRT.rect.height;
            float halfWidth = canvasWidth * 0.5f;
            float halfHeight = canvasHeight * 0.5f;

            float itemWidth = rectTransform.rect.width * rectTransform.localScale.x;
            if (itemWidth < 10f) itemWidth = 120f;

            moveX -= moveSpeed * Time.deltaTime;

            float rightLimit = halfWidth + itemWidth * 0.6f;
            float leftLimit = -halfWidth - itemWidth * 0.6f;

            if (moveX < leftLimit)
            {
                moveX = rightLimit;
            }

            // 화면 맨 아래 위치 (하단에서 35px 띄움)
            float bottomY = -halfHeight + (rectTransform.rect.height * rectTransform.localScale.y * 0.5f) + 35f;
            rectTransform.anchoredPosition = new Vector2(moveX, bottomY);
        }

        private void ClampRectTransformToScreen(RectTransform canvasRT)
        {
            if (canvasRT == null || rectTransform == null) return;

            Vector3[] canvasCorners = new Vector3[4];
            canvasRT.GetWorldCorners(canvasCorners);
            float minX = canvasCorners[0].x;
            float maxX = canvasCorners[2].x;
            float minY = canvasCorners[0].y;
            float maxY = canvasCorners[2].y;

            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            float itemMinX = corners[0].x;
            float itemMaxX = corners[2].x;
            float itemMinY = corners[0].y;
            float itemMaxY = corners[2].y;

            for (int i = 1; i < 4; i++)
            {
                if (corners[i].x < itemMinX) itemMinX = corners[i].x;
                if (corners[i].x > itemMaxX) itemMaxX = corners[i].x;
                if (corners[i].y < itemMinY) itemMinY = corners[i].y;
                if (corners[i].y > itemMaxY) itemMaxY = corners[i].y;
            }

            float itemWidth = itemMaxX - itemMinX;
            float itemHeight = itemMaxY - itemMinY;
            float screenWidth = maxX - minX;
            float screenHeight = maxY - minY;

            // 화면 크기를 초과할 경우 비율 유지하며 스케일 축소
            if (itemWidth > screenWidth * 0.82f || itemHeight > screenHeight * 0.82f)
            {
                float scaleFactor = Mathf.Min((screenWidth * 0.82f) / itemWidth, (screenHeight * 0.82f) / itemHeight);
                rectTransform.localScale *= scaleFactor;
            }

            // 스케일 변경 후 모서리 재계산
            rectTransform.GetWorldCorners(corners);
            itemMinX = corners[0].x; itemMaxX = corners[2].x;
            itemMinY = corners[0].y; itemMaxY = corners[2].y;
            for (int i = 1; i < 4; i++)
            {
                if (corners[i].x < itemMinX) itemMinX = corners[i].x;
                if (corners[i].x > itemMaxX) itemMaxX = corners[i].x;
                if (corners[i].y < itemMinY) itemMinY = corners[i].y;
                if (corners[i].y > itemMaxY) itemMaxY = corners[i].y;
            }

            Vector3 shift = Vector3.zero;
            if (itemMinX < minX) shift.x += (minX - itemMinX);
            if (itemMaxX > maxX) shift.x -= (itemMaxX - maxX);
            if (itemMinY < minY) shift.y += (minY - itemMinY);
            if (itemMaxY > maxY) shift.y -= (itemMaxY - maxY);

            if (shift.sqrMagnitude > 0.0001f)
            {
                rectTransform.position += shift;
            }
        }

        private RectTransform GetCanvasRectTransform()
        {
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            return rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null;
        }

        private void Refresh()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (decoImage == null) return;

            if (gameManager == null)
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
                return;
            }

            string decoId = gameManager.EquippedDecorationId;
            bool decorationChanged = !string.Equals(currentDecoId, decoId, System.StringComparison.OrdinalIgnoreCase);
            currentDecoId = decoId;
            if (decorationChanged)
            {
                frameIndex = 0;
                frameTimer = 0f;
            }
            if (string.IsNullOrEmpty(decoId) || decoId == "deco-none" || decoId == "deco-00" || decoId.Equals("deco-none", System.StringComparison.OrdinalIgnoreCase))
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
                return;
            }

            // StateChanged is raised for many unrelated game updates. Do not overwrite
            // an animated decoration with its catalog thumbnail unless the deco changed.
            if (!decorationChanged && decoImage.enabled)
            {
                return;
            }

            Sprite sprite = null;
            if (CollectionPanel.Instance != null)
            {
                sprite = CollectionPanel.Instance.GetDecorationSprite(decoId);
            }

            if (sprite == null)
            {
                if (IsMouseToy(decoId) && mouseSprites != null && mouseSprites.Length > 0)
                {
                    sprite = mouseSprites[0];
                }
                else if (IsFishToy(decoId) && fishSprites != null && fishSprites.Length > 0)
                {
                    sprite = fishSprites[0];
                }
            }

            if (sprite != null)
            {
                decoImage.sprite = sprite;
                decoImage.enabled = true;
                decoImage.color = Color.white;
                decoImage.preserveAspect = true;
                decoImage.SetNativeSize(); // 네이티브 사이즈 적용

                if (IsMouseToy(decoId) || IsFishToy(decoId))
                {
                    rectTransform.localScale = DECORATION_SCALE;
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.localRotation = Quaternion.identity;
                }

                var canvasRT = GetCanvasRectTransform();
                if (decorationChanged && (IsMouseToy(decoId) || IsFishToy(decoId)))
                {
                    if (canvasRT != null)
                    {
                        moveX = canvasRT.rect.width * 0.5f + 100f;
                    }
                }
            }
            else
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
            }
        }
    }
}
