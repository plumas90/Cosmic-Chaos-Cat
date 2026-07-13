using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace CosmicChaosCat
{
    /// <summary>
    /// Attach to the clickable card image in the scene.
    /// Fires Clicked event; connects to GameManager and ClickEffectPlayer.
    /// </summary>
    public sealed class Clicker : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ClickEffectPlayer effectPlayer;

        public event Action Clicked;

        private void Awake()
        {
            if (gameManager == null) gameManager = FindObjectOfType<GameManager>(true);
            if (effectPlayer == null) effectPlayer = FindObjectOfType<ClickEffectPlayer>(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData != null)
                gameManager?.HandleCardClicked(eventData.position);
            else
                gameManager?.HandleCardClicked(Input.mousePosition);

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
