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

        public void OnPointerClick(PointerEventData eventData)
        {
            gameManager?.HandleCardClicked();
            effectPlayer?.PlayNormalClick();
            Clicked?.Invoke();
        }

        public void TriggerClick()
        {
            gameManager?.HandleCardClicked();
            effectPlayer?.PlayNormalClick();
            Clicked?.Invoke();
        }
    }
}
