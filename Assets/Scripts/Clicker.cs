using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CosmicChaosCat
{
    public sealed class Clicker : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke();
        }

        public void TriggerClick()
        {
            Clicked?.Invoke();
        }
    }
}
