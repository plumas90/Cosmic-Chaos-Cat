using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the clickable card image in the scene.
    /// Uses IPointerDownHandler for zero-delay instant response on every click (10+ clicks/sec supported).
    /// </summary>
    public sealed class Clicker : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ClickEffectPlayer effectPlayer;

        public event Action Clicked;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (effectPlayer == null) effectPlayer = FindObjectOfType<ClickEffectPlayer>(true);
            if (GetComponent<CardImageDisplay>() == null)
                gameObject.AddComponent<CardImageDisplay>();

            // Button 컴포넌트가 붙어있으면 딜레이/클릭 씹힘 원인이 되므로 제거
            var btn = GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                Destroy(btn);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Vector2 clickPos = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
            gameManager?.HandleCardClicked(clickPos);
            effectPlayer?.PlayNormalClick();
            Clicked?.Invoke();
        }

        public void TriggerClick()
        {
            gameManager?.HandleCardClicked(Input.mousePosition);
            effectPlayer?.PlayNormalClick();
            Clicked?.Invoke();
        }
    }
}
