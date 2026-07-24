using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 도감 그리드의 단일 카드 슬롯.
    /// 구조: Frame(Image) / Art(Image) / NameText(TMP_Text)
    ///   - 미해금: Frame = card_locked 스프라이트, Art = 투명, Name = "???"
    ///   - 보유중: Frame = 등급 프레임 스프라이트, Art = 카드 아트, Name = "No.X 이름"
    /// rarityText / unknownOverlay 는 선택사항 (있으면 사용, 없어도 무방)
    /// </summary>
    public sealed class CardSlotUI : MonoBehaviour
    {
        [SerializeField] private Image      frameImage;
        [SerializeField] private Image      cardArtImage;
        [SerializeField] private TMP_Text   nameText;
        [SerializeField] private TMP_Text   rarityText;        // 선택사항
        [SerializeField] private GameObject unknownOverlay;    // 선택사항 (구형 호환)
        [SerializeField] private Button     equipButton;       // 선택사항
        [SerializeField] private TMP_Text   lockText;          // 선택사항 (미해금 시 노출되는 텍스트)

        // Rarity border colors (frame sprite 없을 때 색상 대체)
        private static readonly Color32 ColN   = new Color32(180, 180, 180, 255);
        private static readonly Color32 ColR   = new Color32( 80, 150, 255, 255);
        private static readonly Color32 ColSR  = new Color32(180,  80, 255, 255);
        private static readonly Color32 ColSSR = new Color32(255, 200,   0, 255);
        private static readonly Color32 ColUR  = new Color32(255,  80,  30, 255);

        private GameManager gm;
        private string      cardId;

        private bool initialized        = false;
        private Color originalRarityColor = Color.white;

        // ── 런타임에 EncyclopediaPanel이 전달하는 스프라이트 ─────────────────
        private Sprite _rarityFrameSprite;   // 보유 시 사용할 등급 프레임
        private Sprite _lockedSprite;        // 미해금 시 frame에 표시

        private void Awake() => EnsureInit();

        public void EnsureInit()
        {
            if (initialized) return;
            initialized = true;

            var txts = GetComponentsInChildren<TMP_Text>(true);
            if (txts == null) return;

            TMP_Text foundName   = null;
            TMP_Text foundRarity = null;
            TMP_Text foundLock   = null;

            foreach (var t in txts)
            {
                // Unknown / Unk 오버레이 자식 제외
                if (t.transform.parent != null &&
                    (t.transform.parent.name == "Unknown" || t.transform.parent.name == "Unk"))
                    continue;

                string n = t.name.ToLower();
                if (n.Contains("name"))        { if (foundName   == null) foundName   = t; }
                else if (n.Contains("rarity") || n.Contains("rare"))
                                               { if (foundRarity == null) foundRarity = t; }
                else if (n.Contains("lock"))   { if (foundLock   == null) foundLock   = t; }
            }

            if (foundName   != null) nameText   = foundName;
            if (foundRarity != null) { rarityText = foundRarity; originalRarityColor = foundRarity.color; }
            if (foundLock   != null) lockText   = foundLock;

            // 이름 매핑 실패 시 인덱스 순서 사용 (Unknown 제외)
            var clean = new System.Collections.Generic.List<TMP_Text>();
            foreach (var t in txts)
            {
                if (t.transform.parent != null &&
                    t.transform.parent.name != "Unknown" && t.transform.parent.name != "Unk" &&
                    !t.name.ToLower().Contains("lock"))
                    clean.Add(t);
            }
            if (nameText   == null && clean.Count >= 1) nameText   = clean[0];
            if (rarityText == null && clean.Count >= 2) { rarityText = clean[1]; originalRarityColor = rarityText.color; }
        }

        // ── 스프라이트를 외부(EncyclopediaPanel)에서 전달 ────────────────────
        public void SetSprites(Sprite rarityFrame, Sprite locked)
        {
            _rarityFrameSprite = rarityFrame;
            _lockedSprite      = locked;
        }

        // ── 메인 데이터 적용 ─────────────────────────────────────────────────
        public void SetData(CardEntry card, int cardIndex, CardProgress progress,
            GameManager gameManager, System.Action<string> onSlotClicked)
        {
            EnsureInit();

            gm     = gameManager;
            cardId = card.Id;

            bool unlocked = progress != null && progress.Unlocked;

            // ── Frame ────────────────────────────────────────────────────────
            if (frameImage != null)
            {
                frameImage.raycastTarget = true;   // 클릭 수신 필수

                if (unlocked)
                {
                    if (_rarityFrameSprite != null)
                    {
                        frameImage.sprite = _rarityFrameSprite;
                        frameImage.color  = Color.white;
                        frameImage.type   = Image.Type.Sliced;
                    }
                    else
                    {
                        // 스프라이트 없으면 등급 색상 단색으로 대체
                        frameImage.sprite = null;
                        frameImage.color  = (Color)RarityToColor(card.Rarity);
                    }
                }
                else
                {
                    // 미해금 → card_locked 스프라이트
                    if (_lockedSprite != null)
                    {
                        frameImage.sprite = _lockedSprite;
                        frameImage.color  = Color.white;
                        frameImage.type   = Image.Type.Sliced;
                    }
                    else
                    {
                        frameImage.sprite = null;
                        frameImage.color  = new Color(0.18f, 0.14f, 0.10f, 1f);
                    }
                }
            }

            // ── Art ──────────────────────────────────────────────────────────
            if (cardArtImage != null)
            {
                cardArtImage.raycastTarget = false;

                if (unlocked)
                {
                    if (card.CardSprite != null)
                    {
                        cardArtImage.sprite = card.CardSprite;
                        cardArtImage.color  = Color.white;
                    }
                    else
                    {
                        cardArtImage.sprite = null;
                        cardArtImage.color  = (Color)RarityToColor(card.Rarity) * new Color(1, 1, 1, 0.5f);
                    }
                }
                else
                {
                    // 미해금 → 투명 (보이지 않음)
                    cardArtImage.sprite = null;
                    cardArtImage.color  = new Color(0, 0, 0, 0);
                }
            }

            // ── Name Text ────────────────────────────────────────────────────
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.enabled          = true;
                nameText.enableWordWrapping = false;
                nameText.overflowMode     = TextOverflowModes.Ellipsis;
                nameText.text             = unlocked
                    ? $"No.{cardIndex} {card.DisplayName}"
                    : $"No.{cardIndex} ???";
                nameText.raycastTarget    = false;
            }

            // ── Rarity Text (선택사항) ────────────────────────────────────────
            if (rarityText != null)
            {
                rarityText.gameObject.SetActive(unlocked);
                if (unlocked)
                {
                    rarityText.text  = card.Rarity.ToString();
                    rarityText.color = card.Rarity == CardRarity.N
                        ? originalRarityColor
                        : (Color)RarityToColor(card.Rarity);
                    rarityText.raycastTarget = false;
                }
            }

            // ── Lock Text ────────────────────────────────────────────────────
            if (lockText != null)
            {
                lockText.gameObject.SetActive(!unlocked);
                lockText.raycastTarget = false;
            }

            // ── Unknown Overlay (선택사항 – 구형 호환) ───────────────────────
            // 새 구조에서는 Frame 자체가 잠김 표시이므로 오버레이는 항상 숨김
            if (unknownOverlay != null)
                unknownOverlay.SetActive(false);

            // ── Equip Button (선택사항) ───────────────────────────────────────
            if (equipButton != null)
                equipButton.gameObject.SetActive(false);

            // ── Slot Click ───────────────────────────────────────────────────
            var slotBtn = GetComponent<Button>();
            if (slotBtn == null) slotBtn = gameObject.AddComponent<Button>();
            slotBtn.onClick.RemoveAllListeners();
            slotBtn.onClick.AddListener(() => onSlotClicked?.Invoke(cardId));
        }

        private void OnEquip() => gm?.EquipCard(cardId);

        /// <summary>코드 빌드 시 EncyclopediaPanel에서 직접 레퍼런스 주입.</summary>
        public void InitUI(Image frame, Image art, TMP_Text nameTx,
                           TMP_Text rarityTx, GameObject unknown)
        {
            frameImage      = frame;
            cardArtImage    = art;
            nameText        = nameTx;
            rarityText      = rarityTx;
            unknownOverlay  = unknown;
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
