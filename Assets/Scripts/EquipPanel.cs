using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// EquipPanel — 5개 소켓 슬롯 장착 팝업.
    ///
    /// EquipPanel 하위 씬 오브젝트 구조 (자동 인식):
    ///   SenterBatchSlot    → Center 소켓   (CardSlotUI + Button 있음)
    ///   LeftUpBatchSlot    → 좌상단 소켓   (CardSlotUI + Button 있음)
    ///   RightUpBatchSlot   → 우상단 소켓   (CardSlotUI + Button 있음)
    ///   LeftDownBatchSlot  → 좌하단 소켓   (CardSlotUI + Button 있음)
    ///   RightDownBatchSlot → 우하단 소켓   (CardSlotUI + Button 있음)
    ///   Change_Btn         → 교체 버튼
    ///   Btn_✕              → 닫기 버튼
    ///
    /// 실제 게임 씬의 CenterClick / LeftUpSubClick 등은
    /// CardImageDisplay 컴포넌트가 GameManager.StateChanged를 구독하여
    /// 소켓 장착 카드를 자동 갱신합니다 (별도 연결 불필요).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EquipPanel : MonoBehaviour
    {
        // ── 소켓 슬롯 GO 참조 (Inspector 또는 자동 검색) ──────────────────────
        [Header("Batch Slot GOs (auto-found by name if null)")]
        [SerializeField] private GameObject centerSlotGO;
        [SerializeField] private GameObject leftUpSlotGO;
        [SerializeField] private GameObject rightUpSlotGO;
        [SerializeField] private GameObject leftDownSlotGO;
        [SerializeField] private GameObject rightDownSlotGO;

        [Header("Buttons (auto-found by name if null)")]
        [SerializeField] private Button changeBtn;
        [SerializeField] private Button unchangeBtn;
        [SerializeField] private Button closeBtn;

        [Header("Lock Icon Sprite (assign in Inspector)")]
        [SerializeField] private Sprite lockIconSprite;

        // ── 내부 상태 ─────────────────────────────────────────────────────────
        private GameManager gm;
        private EncyclopediaPanel encPanel;
        private string pendingCardId;
        private ClickSocketSlot selectedSlot = ClickSocketSlot.Center;

        private GameObject[] allSlotGOs;       // Center, LeftUp, RightUp, LeftDown, RightDown 순
        private ClickSocketSlot[] allSlotKeys;
        private Vector3[] slotBaseScales;

        // 색상
        private static readonly Color SelectedTint   = Color.white;
        private static readonly Color UnlockedTint   = Color.white;
        private static readonly Color LockedTint     = Color.white;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            EnsureSetup();
            LocalizationManager.OnLanguageChanged += OnLanguageChanged;
            Refresh();
        }

        private void OnDisable()
        {
            LocalizationManager.OnLanguageChanged -= OnLanguageChanged;
            StopAllCoroutines();
        }

        private void OnLanguageChanged()
        {
            Refresh();
        }

        // ── 공개 API ──────────────────────────────────────────────────────────

        /// <summary>
        /// EncyclopediaPanel의 "대표 설정" 버튼 클릭 시 호출.
        /// </summary>
        public void Open(string cardId, EncyclopediaPanel encyclopedia, GameManager gameManager)
        {
            pendingCardId = cardId;
            encPanel      = encyclopedia;
            gm            = gameManager;
            selectedSlot  = ClickSocketSlot.Center;
            gameObject.SetActive(true);
        }

        // ── 셋업 ─────────────────────────────────────────────────────────────

        private void EnsureSetup()
        {
            if (gm == null) gm = GameManager.Instance ?? FindObjectOfType<GameManager>(true);

            // 슬롯 GO 자동 탐색
            if (centerSlotGO   == null) centerSlotGO   = FindChildGO("SenterBatchSlot");
            if (leftUpSlotGO   == null) leftUpSlotGO   = FindChildGO("LeftUpBatchSlot");
            if (rightUpSlotGO  == null) rightUpSlotGO  = FindChildGO("RightUpBatchSlot");
            if (leftDownSlotGO == null) leftDownSlotGO = FindChildGO("LeftDownBatchSlot");
            if (rightDownSlotGO == null) rightDownSlotGO = FindChildGO("RightDownBatchSlot");

            allSlotGOs = new[]
            {
                centerSlotGO, leftUpSlotGO, rightUpSlotGO, leftDownSlotGO, rightDownSlotGO
            };
            allSlotKeys = new[]
            {
                ClickSocketSlot.Center, ClickSocketSlot.LeftUp, ClickSocketSlot.RightUp,
                ClickSocketSlot.LeftDown, ClickSocketSlot.RightDown
            };

            if (slotBaseScales == null || slotBaseScales.Length != allSlotGOs.Length)
            {
                slotBaseScales = new Vector3[allSlotGOs.Length];
                for (int i = 0; i < allSlotGOs.Length; i++)
                {
                    if (allSlotGOs[i] != null && allSlotGOs[i].transform.localScale.sqrMagnitude >= 0.01f)
                    {
                        slotBaseScales[i] = allSlotGOs[i].transform.localScale;
                    }
                    else
                    {
                        slotBaseScales[i] = new Vector3(1.5f, 1.5f, 1f);
                    }
                }
            }

            // 버튼 자동 탐색
            if (changeBtn == null)
            {
                var t = FindChildTransform("Change_Btn");
                if (t != null) changeBtn = t.GetComponent<Button>();
            }
            if (unchangeBtn == null)
            {
                var t = FindChildTransform("UnChange_Btn") ?? FindChildTransform("Unchange_Btn") ?? FindChildTransform("UnChangeBtn") ?? FindChildTransform("UnchangeBtn");
                if (t != null) unchangeBtn = t.GetComponent<Button>();
            }
            if (closeBtn == null)
            {
                var t = FindChildTransform("Btn_✕") ?? FindChildTransform("Btn_X") ?? FindChildTransform("CloseBtn");
                if (t != null) closeBtn = t.GetComponent<Button>();
            }

            // 버튼 이벤트
            if (changeBtn   != null) { changeBtn.onClick.RemoveAllListeners();   changeBtn.onClick.AddListener(OnChangeClicked); }
            if (unchangeBtn != null) { unchangeBtn.onClick.RemoveAllListeners(); unchangeBtn.onClick.AddListener(OnUnchangeClicked); }
            if (closeBtn    != null) { closeBtn.onClick.RemoveAllListeners();    closeBtn.onClick.AddListener(OnCloseClicked); }

            // 각 슬롯 버튼에 클릭 이벤트 등록
            BindSlotButtonListeners();
        }

        private void BindSlotButtonListeners()
        {
            if (allSlotGOs == null) return;
            for (int i = 0; i < allSlotGOs.Length; i++)
            {
                var go   = allSlotGOs[i];
                var slot = allSlotKeys[i];
                if (go == null) continue;

                // CardSlotUI 컴포넌트가 슬롯에 있으면 파괴하여 EncyclopediaPanel의 카드 슬롯 풀 수집 대상에서 제외
                var slotUI = go.GetComponent<CardSlotUI>();
                if (slotUI != null) Destroy(slotUI);

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();

                var capturedSlot = slot;
                btn.onClick.AddListener(() => OnSlotClicked(capturedSlot));
                btn.interactable = true;
            }
        }

        // ── 갱신 ─────────────────────────────────────────────────────────────

        private void Refresh()
        {
            BindSlotButtonListeners();

            for (int i = 0; i < allSlotGOs.Length; i++)
            {
                if (allSlotGOs[i] != null)
                {
                    if (!allSlotGOs[i].activeSelf) allSlotGOs[i].SetActive(true);
                    RefreshSlotVisual(allSlotGOs[i], allSlotKeys[i]);
                }
            }

            ApplyHighlightAll();
            UpdateChangeBtnLabel();
        }

        private void RefreshSlotVisual(GameObject slotGO, ClickSocketSlot slot)
        {
            if (slotGO == null || gm == null) return;

            // 슬롯 자체 Image는 raycastTarget = true 유지
            var bg = slotGO.GetComponent<Image>();
            if (bg != null) bg.raycastTarget = true;

            // 자식 Graphic(Art, Frame, NameText 등)은 레이캐스트 차단 해제하여 클릭이 버튼으로 바로 전달되게 함
            var childGraphics = slotGO.GetComponentsInChildren<Graphic>(true);
            foreach (var g in childGraphics)
            {
                if (g.gameObject != slotGO)
                    g.raycastTarget = false;
            }

            bool unlocked    = gm.IsSocketUnlocked(slot);
            string cardId    = gm.GetSocketCardId(slot);
            bool hasCard     = !string.IsNullOrEmpty(cardId);

            var ep = encPanel != null ? encPanel : EncyclopediaPanel.Instance;
            var frameImg = FindFrameImage(slotGO);
            var markImg  = FindRarityMarkImage(slotGO);
            var artImg   = FindChildImage(slotGO, "Art");
            var nameTxt  = FindChildText(slotGO, "Name"); // NameText (1) 등을 찾음

            // Lock 아이콘 (Art 이미지에 잠금 스프라이트로 표현)
            if (!unlocked)
            {
                if (frameImg != null)
                {
                    frameImg.color = LockedTint;
                    if (ep != null && ep.SpriteCardLocked != null)
                        frameImg.sprite = ep.SpriteCardLocked;
                }
                if (markImg != null) markImg.gameObject.SetActive(false);

                if (artImg != null)
                {
                    artImg.color  = LockedTint;
                    if (lockIconSprite == null && ep != null)
                        lockIconSprite = ep.SpriteCardLocked;
                    
                    if (lockIconSprite != null) artImg.sprite = lockIconSprite;
                    else artImg.sprite = null;

                    LongNeckCatDisplayHelper.BindLongNeckCardSlot(artImg.transform, null);
                }
                if (nameTxt != null) nameTxt.text = "???";
                return;
            }

            // 잠금 해제 — 장착 카드 표시
            if (artImg != null)
            {
                if (hasCard)
                {
                    var entry = gm.CardCatalog?.FindById(cardId);
                    if (entry != null)
                    {
                        if (frameImg != null)
                        {
                            frameImg.color = UnlockedTint;
                            if (ep != null)
                            {
                                Sprite fSp = ep.GetFrameSpriteForRarity(entry.Rarity);
                                if (fSp != null) frameImg.sprite = fSp;
                            }
                        }

                        if (markImg != null)
                        {
                            if (ep != null)
                            {
                                Sprite mSp = ep.GetMarkSpriteForRarity(entry.Rarity);
                                if (mSp != null)
                                {
                                    markImg.sprite = mSp;
                                    markImg.color = Color.white;
                                    if (!markImg.gameObject.activeSelf) markImg.gameObject.SetActive(true);
                                }
                                else
                                {
                                    markImg.gameObject.SetActive(false);
                                }
                            }
                        }

                        if (artImg != null)
                        {
                            if (!artImg.gameObject.activeSelf) artImg.gameObject.SetActive(true);
                            Sprite stageSprite = gm.GetCardSpriteForDisplay(cardId);
                            if (stageSprite == null) stageSprite = entry.CardSprite ?? entry.GachaBgSprite;

                            artImg.sprite = stageSprite;
                            artImg.color  = Color.white;
                            artImg.preserveAspect = true;

                            LongNeckCatDisplayHelper.BindLongNeckCardSlot(artImg.transform, entry);
                        }
                        
                        if (nameTxt != null) nameTxt.text = entry.GetDisplayName();
                    }
                }
                else
                {
                    // 비어있는 슬롯 (UnChange 해제 상태)
                    if (frameImg != null)
                    {
                        frameImg.color = UnlockedTint;
                        if (ep != null)
                        {
                            Sprite fSp = ep.GetFrameSpriteForRarity(CardRarity.N); // card_frame_n으로 변경
                            if (fSp != null) frameImg.sprite = fSp;
                        }
                    }
                    if (markImg != null) markImg.gameObject.SetActive(false); // rareMark 꺼둠

                    if (artImg != null)
                    {
                        artImg.sprite = null;
                        artImg.gameObject.SetActive(false); // Art 이미지 꺼둠
                        LongNeckCatDisplayHelper.BindLongNeckCardSlot(artImg.transform, null);
                    }
                    if (nameTxt != null) nameTxt.text = "";
                }
            }
        }

        private void ApplyHighlightAll()
        {
            for (int i = 0; i < allSlotGOs.Length; i++)
                ApplySlotHighlight(allSlotGOs[i], allSlotKeys[i]);
        }

        private void ApplySlotHighlight(GameObject slotGO, ClickSocketSlot slot)
        {
            if (slotGO == null) return;
            bool isSelected = slot == selectedSlot;

            int index = System.Array.IndexOf(allSlotGOs, slotGO);
            Vector3 baseScale = (index >= 0 && slotBaseScales != null && index < slotBaseScales.Length)
                ? slotBaseScales[index]
                : (slotGO.transform.localScale.sqrMagnitude >= 0.01f ? slotGO.transform.localScale : new Vector3(1.5f, 1.5f, 1f));

            // 1. 선택된 슬롯 원래 크기(예: 1.5배)의 1.12배 확대 강조 (1.5 * 1.12 = 1.68배)
            slotGO.transform.localScale = isSelected ? baseScale * 1.12f : baseScale;

            // 2. 슬롯 배경 및 테두리 이미지 황금빛 강조 색상 적용
            var bg = slotGO.GetComponent<Image>();
            if (bg != null) bg.color = isSelected ? SelectedTint : Color.white;

            var frameImg = FindFrameImage(slotGO);
            if (frameImg != null && gm != null && gm.IsSocketUnlocked(slot))
            {
                frameImg.color = isSelected ? SelectedTint : Color.white;
            }

            // 3. Highlight / Select / Outline 자식 오브젝트가 있다면 켜고 끔
            var hl = FindChildImage(slotGO, "Highlight") ?? FindChildImage(slotGO, "Select") ?? FindChildImage(slotGO, "Outline");
            if (hl != null) hl.gameObject.SetActive(isSelected);
        }

        private void UpdateChangeBtnLabel()
        {
            // 1. 교체 / 장착 버튼 텍스트 언어 갱신
            if (changeBtn != null)
            {
                var lbl = changeBtn.GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                {
                    if (gm != null && !gm.IsSocketUnlocked(selectedSlot))
                    {
                        lbl.text = LocalizationManager.Get("equip_btn_unlock_shop");
                        changeBtn.interactable = false;
                    }
                    else
                    {
                        lbl.text = LocalizationManager.Get("equip_btn_change");
                        changeBtn.interactable = true;
                    }
                }
            }

            // 2. 장착 해제 버튼 텍스트 언어 갱신
            if (unchangeBtn != null)
            {
                var lbl = unchangeBtn.GetComponentInChildren<TMP_Text>();
                if (lbl != null)
                {
                    lbl.text = LocalizationManager.Get("equip_btn_unequip");
                }
            }

            // 3. 팝업 타이틀 및 닫기 버튼 텍스트 언어 갱신
            var titleTxt = FindChildText(gameObject, "Title") ?? FindChildText(gameObject, "Header");
            if (titleTxt != null)
            {
                titleTxt.text = LocalizationManager.Get("equip_panel_title");
            }

            if (closeBtn != null)
            {
                var closeTxt = closeBtn.GetComponentInChildren<TMP_Text>();
                if (closeTxt != null && closeTxt.text != "✕")
                {
                    closeTxt.text = LocalizationManager.Get("common_btn_close");
                }
            }
        }

        // ── 클릭 핸들러 ───────────────────────────────────────────────────────

        private void OnSlotClicked(ClickSocketSlot slot)
        {
            selectedSlot = slot;
            ApplyHighlightAll();
            UpdateChangeBtnLabel();
        }

        private void OnChangeClicked()
        {
            if (gm == null || string.IsNullOrEmpty(pendingCardId)) return;

            if (!gm.IsSocketUnlocked(selectedSlot))
            {
                return; // 해금은 상점에서만 가능
            }

            gm.EquipCardToSocket(selectedSlot, pendingCardId);
            Refresh();
            encPanel?.RefreshAfterEquip(pendingCardId);
            // GameManager.StateChanged가 발생하므로 CenterClick/SubClick의 CardImageDisplay가 자동 갱신됨
        }

        private void OnUnchangeClicked()
        {
            if (gm == null) return;

            if (!gm.IsSocketUnlocked(selectedSlot))
            {
                return;
            }

            // 해제 버튼 클릭 시 해당 소켓을 비움("")
            gm.EquipCardToSocket(selectedSlot, "");
            Refresh();
            encPanel?.RefreshAfterEquip(pendingCardId);
        }

        private void OnCloseClicked()
        {
            gameObject.SetActive(false);
        }


        // ── 유틸 ─────────────────────────────────────────────────────────────

        private GameObject FindChildGO(string childName)
        {
            var t = FindChildTransform(childName);
            return t != null ? t.gameObject : null;
        }

        private Transform FindChildTransform(string childName)
        {
            return FindRecursive(transform, childName);
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var f = FindRecursive(child, name);
                if (f != null) return f;
            }
            return null;
        }

        private static Image FindChildImage(GameObject parent, string childName)
        {
            var t = FindRecursive(parent.transform, childName);
            return t != null ? t.GetComponent<Image>() : null;
        }

        private static Image FindFrameImage(GameObject parent)
        {
            if (parent == null) return null;
            var imgs = parent.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.gameObject == parent) continue;
                string n = img.name.ToLower();
                if (n.Contains("frame")) return img;
            }
            return FindChildImage(parent, "Frame");
        }

        private static Image FindRarityMarkImage(GameObject parent)
        {
            if (parent == null) return null;
            var imgs = parent.GetComponentsInChildren<Image>(true);
            foreach (var img in imgs)
            {
                if (img.gameObject == parent) continue;
                string n = img.name.ToLower();
                if (n.Contains("rare") || n.Contains("mark") || n.Contains("badge") || n.Contains("rarity"))
                {
                    return img;
                }
            }
            return null;
        }

        private static TMP_Text FindChildText(GameObject parent, string childNameContains)
        {
            var txts = parent.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in txts)
            {
                if (t.name.IndexOf(childNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return t;
            }
            return null;
        }
    }
}
