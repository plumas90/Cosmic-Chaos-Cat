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

        public void SetData(CardEntry card, int cardIndex, CardProgress progress, GameManager gameManager, System.Action<string> onSlotClicked)
        {
            gm     = gameManager;
            cardId = card.Id;

            bool unlocked = progress != null && progress.Unlocked;

            if (unknownOverlay != null)
            {
                unknownOverlay.SetActive(!unlocked);
                var overlayImg = unknownOverlay.GetComponent<Image>();
                if (overlayImg != null) overlayImg.raycastTarget = false;
                var overlayTexts = unknownOverlay.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in overlayTexts) t.raycastTarget = false;
                var overlayImgs = unknownOverlay.GetComponentsInChildren<Image>(true);
                foreach (var img in overlayImgs) img.raycastTarget = false;
            }

            if (nameText  != null) { nameText.text  = unlocked ? $"No.{cardIndex}. {card.DisplayName}" : $"No.{cardIndex}. ???"; nameText.raycastTarget = false; }
            if (rarityText != null) { rarityText.text = unlocked ? card.Rarity.ToString() : "?"; rarityText.raycastTarget = false; }
            if (stackText  != null) { stackText.text = (unlocked && progress != null && progress.Copies > 1) ? $"x{progress.Copies}" : string.Empty; stackText.raycastTarget = false; }

            // Art
            if (cardArtImage != null)
            {
                cardArtImage.raycastTarget = false;
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
            {
                frameImage.raycastTarget = false;
                frameImage.color = unlocked ? (Color)RarityToColor(card.Rarity) : (Color)ColN;
            }

            // Hide direct equip button since it's moved to details popup
            if (equipButton != null)
                equipButton.gameObject.SetActive(false);

            // Add full slot click trigger
            var slotBtn = GetComponent<Button>();
            if (slotBtn == null) slotBtn = gameObject.AddComponent<Button>();
            slotBtn.onClick.RemoveAllListeners();
            slotBtn.onClick.AddListener(() => onSlotClicked?.Invoke(cardId));
        }

        private void OnEquip() => gm?.EquipCard(cardId);

        public void InitUI(Image frame, Image art, TMP_Text nameTx, TMP_Text rarityTx, TMP_Text stackTx, GameObject unknown)
        {
            frameImage = frame;
            cardArtImage = art;
            nameText = nameTx;
            rarityText = rarityTx;
            stackText = stackTx;
            unknownOverlay = unknown;
        }

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
