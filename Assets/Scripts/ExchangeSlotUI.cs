using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 조각 교환 패널의 카드 한 칸.
    /// ExchangeSlotUI 프리팹으로 만들어 ShardExchangePanel에 연결하세요.
    /// </summary>
    public sealed class ExchangeSlotUI : MonoBehaviour
    {
        [SerializeField] private Image    frameImage;
        [SerializeField] private Image    cardArtImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text stackText;
        [SerializeField] private Button   exchangeButton;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private Image    ownedCheckmark;   // 획득 완료 표시 (선택사항)

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

            bool owned   = progress != null && progress.Unlocked;
            int currentMax = progress != null ? card.MaxStacks + (progress.BreakthroughCount * 5) : card.MaxStacks;
            bool maxed   = owned && progress != null && progress.Copies >= currentMax;
            int cost = gameManager != null ? gameManager.GetShardExchangeCost(card.Rarity) : 500;
            bool canBuy  = !maxed && gameManager.Shards >= cost;

            // 이름 / 등급
            if (nameText   != null) nameText.text   = card.DisplayName;
            if (rarityText != null) rarityText.text = $"[{card.Rarity}]";

            // 중첩 수
            if (stackText  != null)
                stackText.text = owned && progress != null && progress.Copies > 0
                    ? $"{progress.Copies} / {currentMax}"
                    : "미획득";

            // 비용
            if (costText != null)
                costText.text = maxed ? "MAX" : $"🔷 {cost}";

            // 아트
            if (cardArtImage != null)
            {
                cardArtImage.sprite = card.CardSprite;
                cardArtImage.color  = card.CardSprite != null
                    ? Color.white
                    : (Color)RarityToColor(card.Rarity);
            }

            // 테두리
            if (frameImage != null)
                frameImage.color = (Color)RarityToColor(card.Rarity);

            // 체크마크 (5중첩 완료)
            if (ownedCheckmark != null) ownedCheckmark.gameObject.SetActive(maxed);

            // 버튼
            if (exchangeButton != null)
            {
                exchangeButton.interactable = canBuy;
                exchangeButton.onClick.RemoveAllListeners();
                if (canBuy) exchangeButton.onClick.AddListener(OnExchange);
            }
            if (buttonLabel != null)
                buttonLabel.text = maxed ? "완료" : "교환";
        }

        private void OnExchange() => gm?.ExchangeWithShards(cardId);

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
