using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// Represents a single card slot in the encyclopedia grid.
    /// Must be pre-placed as a prefab; set data is populated by EncyclopediaPanel.
    /// </summary>
    public sealed class CardSlotUI : MonoBehaviour
    {
        [SerializeField] private Image      frameImage;
        [SerializeField] private Image      cardArtImage;
        [SerializeField] private TMP_Text   nameText;
        [SerializeField] private TMP_Text   rarityText;
        [SerializeField] private TMP_Text   stackText;
        [SerializeField] private GameObject unknownOverlay;   // "???" cover
        [SerializeField] private Button     equipButton;

        // Rarity border colors
        private static readonly Color32 ColN   = new Color32(180, 180, 180, 255);
        private static readonly Color32 ColR   = new Color32( 80, 150, 255, 255);
        private static readonly Color32 ColSR  = new Color32(180,  80, 255, 255);
        private static readonly Color32 ColSSR = new Color32(255, 200,   0, 255);
        private static readonly Color32 ColUR  = new Color32(255,  80,  30, 255);

        private GameManager gm;
        private string      cardId;

        public void SetData(CardEntry card, CardProgress progress, GameManager gameManager)
        {
            gm     = gameManager;
            cardId = card.Id;

            bool unlocked = progress != null && progress.Unlocked;

            if (unknownOverlay != null) unknownOverlay.SetActive(!unlocked);

            if (nameText  != null) nameText.text  = unlocked ? card.DisplayName : "???";
            if (rarityText != null) rarityText.text = unlocked ? card.Rarity.ToString() : "?";
            if (stackText  != null)
                stackText.text = (unlocked && progress != null && progress.Copies > 1)
                    ? $"x{progress.Copies}" : string.Empty;

            // Art
            if (cardArtImage != null)
            {
                if (unlocked && card.CardSprite != null)
                {
                    cardArtImage.sprite  = card.CardSprite;
                    cardArtImage.color   = Color.white;
                }
                else
                {
                    cardArtImage.sprite = null;
                    cardArtImage.color  = unlocked ? RarityToColor(card.Rarity) : new Color(0.2f, 0.2f, 0.2f);
                }
            }

            // Frame border color
            if (frameImage != null)
                frameImage.color = unlocked ? (Color)RarityToColor(card.Rarity) : (Color)ColN;

            // Equip button
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(unlocked);
                equipButton.onClick.RemoveAllListeners();
                if (unlocked) equipButton.onClick.AddListener(OnEquip);
            }
        }

        private void OnEquip() => gm?.EquipCard(cardId);

        private static Color32 RarityToColor(CardRarity r)
        {
            switch (r)
            {
                case CardRarity.R:   return ColR;
                case CardRarity.SR:  return ColSR;
                case CardRarity.SSR: return ColSSR;
                case CardRarity.UR:  return ColUR;
                default:             return ColN;
            }
        }
    }
}
