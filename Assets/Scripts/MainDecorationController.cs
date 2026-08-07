using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the MainDecoration Image component in the scene.
    /// Automatically updates the decoration sprite based on the equipped decoration.
    /// Supports continuous 8-frame sprite animations for Mouse_Toy and Cat_Wheel.
    /// </summary>
    public sealed class MainDecorationController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Image decoImage;

        private RectTransform rectTransform;
        private Canvas rootCanvas;

        // Sub-sprite frame animation fields
        private Sprite[] wheelSprites;
        private Sprite[] mouseSprites;
        private Sprite[] fishSprites;
        private float frameTimer = 0f;
        private int frameIndex = 0;
        private const float FRAME_DURATION = 0.12f; // ~8 FPS continuous frame animation

        // Movement parameters
        private float moveX = 0f;
        private float mouseSpeed = 220f; // Horizontal move speed for Mouse_Toy / Fish_Toy (pixels/sec)
        private string currentDecoId = "";

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
            if (wheelSprites == null || wheelSprites.Length == 0)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_Deco/Cat_Wheel.png");
                var list = new List<Sprite>();
                foreach (var a in assets)
                    if (a is Sprite s) list.Add(s);
                list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
                wheelSprites = list.ToArray();
            }

            if (mouseSprites == null || mouseSprites.Length == 0)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_Deco/Mouse_Toy.png");
                var list = new List<Sprite>();
                foreach (var a in assets)
                    if (a is Sprite s) list.Add(s);
                list.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));
                mouseSprites = list.ToArray();
            }

            if (fishSprites == null || fishSprites.Length == 0)
            {
                var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/image/A_Deco/fish_toy_spritesheet_200x100.png");
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

            // 8프레임 연속 스프라이트 애니메이션 타이머
            frameTimer += Time.deltaTime;
            if (frameTimer >= FRAME_DURATION)
            {
                frameTimer -= FRAME_DURATION;
                frameIndex++;
            }

            // 1. Mouse Toy (8프레임 쥐 달리기 연출 + 화면 아래 오른쪽 이동 & 무한 재등장)
            if (IsMouseToy(decoId))
            {
                LoadDecoSubSprites();
                if (mouseSprites != null && mouseSprites.Length > 0)
                {
                    decoImage.sprite = mouseSprites[frameIndex % mouseSprites.Length];
                }
                rectTransform.localRotation = Quaternion.identity;
                UpdateMouseToyMovement();
            }
            // 2. Fish Toy (8프레임 생선 튀기기 연출 + 화면 아래 오른쪽 이동 & 무한 재등장)
            else if (IsFishToy(decoId))
            {
                LoadDecoSubSprites();
                if (fishSprites != null && fishSprites.Length > 0)
                {
                    decoImage.sprite = fishSprites[frameIndex % fishSprites.Length];
                }
                rectTransform.localRotation = Quaternion.identity;
                UpdateMouseToyMovement();
            }
            // 3. Cat Wheel (8프레임 캣휠 자체 회전 프레임 연출 + 트랜스폼 회전 없이 화면 경계 자동 보정)
            else if (IsCatWheel(decoId))
            {
                LoadDecoSubSprites();
                if (wheelSprites != null && wheelSprites.Length > 0)
                {
                    decoImage.sprite = wheelSprites[frameIndex % wheelSprites.Length];
                }
                rectTransform.localRotation = Quaternion.identity; // 오브젝트 트랜스폼 직접 회전 방지
                UpdateCatWheelBehavior();
            }
        }

        private bool IsMouseToy(string id) =>
            !string.IsNullOrEmpty(id) && (id.Equals("deco-mouse-toy", System.StringComparison.OrdinalIgnoreCase) || id.Equals("Mouse_Toy", System.StringComparison.OrdinalIgnoreCase) || id.ToLower().Contains("mouse"));

        private bool IsFishToy(string id) =>
            !string.IsNullOrEmpty(id) && (id.Equals("deco-fish-toy", System.StringComparison.OrdinalIgnoreCase) || id.Equals("Fish_Toy", System.StringComparison.OrdinalIgnoreCase) || id.ToLower().Contains("fish"));

        private bool IsCatWheel(string id) =>
            !string.IsNullOrEmpty(id) && (id.Equals("deco-cat-wheel", System.StringComparison.OrdinalIgnoreCase) || id.Equals("Cat_Wheel", System.StringComparison.OrdinalIgnoreCase) || id.ToLower().Contains("wheel"));

        private void UpdateMouseToyMovement()
        {
            if (rectTransform == null) return;
            var canvasRT = GetCanvasRectTransform();
            if (canvasRT == null) return;

            float canvasWidth = canvasRT.rect.width;
            float canvasHeight = canvasRT.rect.height;
            float halfWidth = canvasWidth * 0.5f;
            float halfHeight = canvasHeight * 0.5f;

            // 앵커 중앙 정렬
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            float itemWidth = rectTransform.rect.width * rectTransform.localScale.x;
            if (itemWidth < 10f) itemWidth = 120f;

            moveX += mouseSpeed * Time.deltaTime;

            float rightLimit = halfWidth + itemWidth * 0.6f;
            float leftLimit = -halfWidth - itemWidth * 0.6f;

            if (moveX > rightLimit)
            {
                moveX = leftLimit;
            }

            // 화면 맨 아래 위치 (하단에서 35px 띄움)
            float bottomY = -halfHeight + (rectTransform.rect.height * rectTransform.localScale.y * 0.5f) + 35f;
            rectTransform.anchoredPosition = new Vector2(moveX, bottomY);
            rectTransform.localRotation = Quaternion.identity;
        }

        private void UpdateCatWheelBehavior()
        {
            if (rectTransform == null) return;
            var canvasRT = GetCanvasRectTransform();
            if (canvasRT == null) return;

            // 오브젝트 자체 회전은 하지 않고, 이미지 일부가 화면 밖으로 나가는 경우 화면 안으로 위치 및 크기 자동 조정
            ClampRectTransformToScreen(canvasRT);
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
            currentDecoId = decoId;

            if (string.IsNullOrEmpty(decoId) || decoId == "deco-none" || decoId == "deco-00" || decoId.Equals("deco-none", System.StringComparison.OrdinalIgnoreCase))
            {
                decoImage.sprite = null;
                decoImage.enabled = false;
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
                else if (IsCatWheel(decoId) && wheelSprites != null && wheelSprites.Length > 0)
                {
                    sprite = wheelSprites[0];
                }
            }

            if (sprite != null)
            {
                decoImage.sprite = sprite;
                decoImage.enabled = true;
                decoImage.color = Color.white;
                decoImage.preserveAspect = true;
                decoImage.SetNativeSize(); // 네이티브 사이즈 적용

                var canvasRT = GetCanvasRectTransform();
                if (IsMouseToy(decoId))
                {
                    if (canvasRT != null)
                    {
                        moveX = -canvasRT.rect.width * 0.5f - 100f;
                    }
                }
                else if (IsCatWheel(decoId))
                {
                    // 화면 중앙(0,0)에 강제 고정하지 않고, 기존 배치 위치를 유지하되 화면 가장자리(외곽)를 벗어날 경우에만 안쪽으로 자동 클램핑 보정
                    rectTransform.localRotation = Quaternion.identity;
                    ClampRectTransformToScreen(canvasRT);
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
