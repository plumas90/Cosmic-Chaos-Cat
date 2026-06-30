using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CosmicChaosCat
{
    /// <summary>
    /// 조각 교환 패널.
    /// 모든 카드(숨겨진 카드 제외)를 목록으로 보여주고,
    /// 조각을 소모해 원하는 카드를 확정 획득할 수 있습니다.
    ///
    /// 피티 100연 보장과는 별개로 언제든지 사용 가능합니다.
    /// </summary>
    public sealed class ShardExchangePanel : MonoBehaviour
    {
        [SerializeField] private GameManager      gameManager;
        [SerializeField] private Transform        gridContent;      // ScrollRect Content
        [SerializeField] private ExchangeSlotUI   slotPrefab;       // 프리팹
        [SerializeField] private Button           closeButton;
        [SerializeField] private TMP_Text         shardText;
        [SerializeField] private TMP_Text         titleText;
        [SerializeField] private TMP_InputField   searchField;
        [SerializeField] private TMP_Text         hintText;

        // 등급 필터 버튼 (선택사항 — 연결 안 해도 됨)
        [SerializeField] private Button filterAllButton;
        [SerializeField] private Button filterRButton;
        [SerializeField] private Button filterSRButton;
        [SerializeField] private Button filterSSRButton;

        private readonly List<ExchangeSlotUI> slots = new List<ExchangeSlotUI>();
        private bool   poolBuilt;
        private string rarityFilter = string.Empty;   // "" = 전체

        private void OnEnable()
        {
            if (!poolBuilt) BuildPool();

            if (closeButton     != null) closeButton.onClick.AddListener(OnClose);
            if (filterAllButton != null) filterAllButton.onClick.AddListener(() => SetFilter(string.Empty));
            if (filterRButton   != null) filterRButton.onClick.AddListener(()   => SetFilter("R"));
            if (filterSRButton  != null) filterSRButton.onClick.AddListener(()  => SetFilter("SR"));
            if (filterSSRButton != null) filterSSRButton.onClick.AddListener(() => SetFilter("SSR"));
            if (searchField     != null) searchField.onValueChanged.AddListener(_ => Refresh());

            if (gameManager != null) gameManager.StateChanged += Refresh;

            Refresh();
        }

        private void OnDisable()
        {
            if (closeButton     != null) closeButton.onClick.RemoveListener(OnClose);
            if (filterAllButton != null) filterAllButton.onClick.RemoveAllListeners();
            if (filterRButton   != null) filterRButton.onClick.RemoveAllListeners();
            if (filterSRButton  != null) filterSRButton.onClick.RemoveAllListeners();
            if (filterSSRButton != null) filterSSRButton.onClick.RemoveAllListeners();
            if (searchField     != null) searchField.onValueChanged.RemoveAllListeners();

            if (gameManager != null) gameManager.StateChanged -= Refresh;
        }

        private void BuildPool()
        {
            poolBuilt = true;
            if (gameManager?.CardCatalog == null || slotPrefab == null || gridContent == null) return;

            var cards = gameManager.CardCatalog.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || cards[i].IsHidden) continue;
                var slot = Instantiate(slotPrefab, gridContent);
                slots.Add(slot);
            }
        }

        private void Refresh()
        {
            if (!poolBuilt || gameManager?.CardCatalog == null) return;

            // 재화 표시
            if (shardText != null) shardText.text = $"🔷 보유 조각: {gameManager.Shards}";

            // 힌트
            if (hintText != null)
                hintText.text = "조각을 소모해 원하는 카드를 확정 획득하세요.\n" +
                                $"R: 30  SR: 100  SSR: 300  UR: 1000";

            var catalog = gameManager.CardCatalog;
            var states  = gameManager.GetCardStates();
            string search = searchField != null ? searchField.text.ToLower() : string.Empty;

            int slotIdx = 0;
            for (int i = 0; i < catalog.Cards.Count; i++)
            {
                var card = catalog.Cards[i];
                if (card == null || card.IsHidden) continue;

                if (slotIdx >= slots.Count) break;

                bool matchRarity = string.IsNullOrEmpty(rarityFilter)
                                || card.Rarity.ToString() == rarityFilter;
                bool matchSearch = string.IsNullOrEmpty(search)
                                || card.DisplayName.ToLower().Contains(search);

                bool show = matchRarity && matchSearch;
                slots[slotIdx].gameObject.SetActive(show);

                if (show)
                {
                    states.TryGetValue(card.Id, out var progress);
                    slots[slotIdx].SetData(card, progress, gameManager);
                }

                slotIdx++;
            }
        }

        private void SetFilter(string filter)
        {
            rarityFilter = filter;
            Refresh();
        }

        private void OnClose() => gameObject.SetActive(false);
    }
}
