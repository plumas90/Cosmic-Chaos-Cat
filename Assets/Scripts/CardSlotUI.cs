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

        // Original state of cardArtImage
        private Sprite _originalArtSprite;
        private Color  _originalArtColor = Color.white;
        private bool   _artOriginalSaved = false;

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
            // ── Image 컴포넌트 연결 (정확한 이름 기준, 항상 덮어쓰기) ────────
            // 구조: Frame = 배경프레임, Art = 고양이 이미지, Image = 이름 배경 (건드리지 않음)
            var imgs = GetComponentsInChildren<Image>(true);
            frameImage   = null;
            cardArtImage = null;
            foreach (var img in imgs)
            {
                if (img == null) continue;
                string n = img.gameObject.name;

                if (n == "Frame" || n == "frame")
                {
                    if (frameImage == null) frameImage = img;
                }
                else if (n == "Art" || n == "art")
                {
                    if (cardArtImage == null) cardArtImage = img;
                }
            }

            // 이름 매칭 실패 시 Contains 폴백
            if (frameImage == null || cardArtImage == null)
            {
                foreach (var img in imgs)
                {
                    if (img == null) continue;
                    string nl = img.gameObject.name.ToLower();
                    if (frameImage == null && nl.Contains("frame"))
                        frameImage = img;
                    else if (cardArtImage == null &&
                             (nl.Contains("cardart") || nl.Contains("card_art") ||
                              (nl.Contains("art") && !nl.Contains("start") && !nl.Contains("heart"))))
                        cardArtImage = img;
                }
            }

            // Save cardArtImage original state (only once)
            _artOriginalSaved  = false;
            if (cardArtImage != null)
            {
                try
                {
                    _originalArtSprite = cardArtImage.sprite;
                    _originalArtColor  = cardArtImage.color;
                }
                catch { _originalArtSprite = null; _originalArtColor = Color.white; }
                _artOriginalSaved = true;
            }
        }

        // ── 스프라이트를 외부(EncyclopediaPanel)에서 전달 ────────────────────
        public void SetSprites(Sprite rarityFrame, Sprite locked)
        {
            _rarityFrameSprite = rarityFrame;
            _lockedSprite      = locked;

            // Save cardArtImage original state on first assignment
            if (cardArtImage != null && !_artOriginalSaved)
            {
                _originalArtSprite = cardArtImage.sprite;
                _originalArtColor  = cardArtImage.color;
                _artOriginalSaved  = true;
            }
        }

        public void SetArtSprite(Sprite sp)
        {
            EnsureInit();
            if (cardArtImage != null)
            {
                if (sp != null)
                {
                    cardArtImage.gameObject.SetActive(true);
                    cardArtImage.sprite = sp;
                    cardArtImage.color  = Color.white;
                }
            }
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
                    cardArtImage.gameObject.SetActive(true);
                    int selStage = (progress != null && progress.SelectedStage > 0) ? progress.SelectedStage : 1;
                    Sprite stageSprite = card != null ? card.GetSpriteForStage(selStage) : null;
                    if (stageSprite == null && card != null) stageSprite = card.CardSprite;
                    if (stageSprite == null) stageSprite = _originalArtSprite;

                    if (stageSprite != null)
                    {
                        cardArtImage.sprite = stageSprite;
                        cardArtImage.color  = Color.white;
                    }
                    else
                    {
                        cardArtImage.color  = Color.white;
                    }
                }
                else
                {
                    // 미해금 → 투명 및 비활성화 처리 (이미지 안 보임)
                    cardArtImage.sprite = null;
                    cardArtImage.color  = new Color(0, 0, 0, 0);
                    cardArtImage.gameObject.SetActive(false);
                }
            }

            // ── Name Text ────────────────────────────────────────────────────
            if (nameText != null)
            {
                var t = nameText.transform;
                while (t != null && t != transform)
                {
                    if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);
                    t = t.parent;
                }

                if (!nameText.gameObject.activeSelf) nameText.gameObject.SetActive(true);
                nameText.enabled          = true;
                nameText.enableWordWrapping = false;
                nameText.overflowMode     = TextOverflowModes.Ellipsis;
                
                string targetName = unlocked
                    ? $"No.{cardIndex} {card.DisplayName}"
                    : $"No.{cardIndex} ???";
                if (nameText.text != targetName) nameText.text = targetName;
                nameText.raycastTarget    = false;
            }

            // ── Rarity Text (선택사항) ────────────────────────────────────────
            if (rarityText != null)
            {
                if (rarityText.gameObject.activeSelf != unlocked)
                    rarityText.gameObject.SetActive(unlocked);

                if (unlocked)
                {
                    string targetRarity = card.Rarity.ToString();
                    if (rarityText.text != targetRarity) rarityText.text = targetRarity;

                    Color targetColor = card.Rarity == CardRarity.N
                        ? originalRarityColor
                        : (Color)RarityToColor(card.Rarity);
                    if (rarityText.color != targetColor) rarityText.color = targetColor;
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

        #if UNITY_EDITOR
        public void EditorWireFields()
        {
            var txts = GetComponentsInChildren<TMP_Text>(true);
            TMP_Text foundName = null;
            TMP_Text foundRarity = null;
            TMP_Text foundLock = null;

            foreach (var t in txts)
            {
                string n = t.name.ToLower();
                if (n.Contains("name")) foundName = t;
                else if (n.Contains("rarity") || n.Contains("rare")) foundRarity = t;
                else if (n.Contains("lock")) foundLock = t;
            }

            var imgs = GetComponentsInChildren<Image>(true);
            Image frame = null;
            Image art = null;
            foreach (var img in imgs)
            {
                string n = img.name.ToLower();
                if (n.Equals("art", System.StringComparison.OrdinalIgnoreCase) ||
                    n.Contains("cardart") || n.Contains("card_art") ||
                    (n.Contains("art") && !n.Contains("start") && !n.Contains("heart")))
                {
                    art = img;
                }
                else if (n.Contains("frame") || n.Equals("image", System.StringComparison.OrdinalIgnoreCase))
                {
                    if (frame == null) frame = img;
                }
            }

            if (frame != null) frameImage = frame;
            if (art != null) cardArtImage = art;
            if (foundName != null) nameText = foundName;
            if (foundRarity != null) rarityText = foundRarity;
            if (foundLock != null) lockText = foundLock;

            var unk = transform.Find("Unknown") ?? transform.Find("Unk");
            if (unk != null) unknownOverlay = unk.gameObject;

            UnityEditor.EditorUtility.SetDirty(this);
        }
        #endif
    }
}
