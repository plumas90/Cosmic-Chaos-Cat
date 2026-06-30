using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GameManager : MonoBehaviour
    {
        // ── Constants ──────────────────────────────────────────────────────────
        private const string SaveKey            = "ccc_save_v3";
        private const double BaseClickIncome    = 1d;
        private const double BaseGachaCost      = 10d;
        private const float  ComboWindowSeconds = 0.8f;
        private const float  ClickWindowSeconds = 1f;
        private const int    SoftPityStart      = 80;
        private const int    HardPityAt         = 100;   // 100연 보장

        // Upgrade IDs — must match UpgradeCatalogSO entries
        private const string UPG_CRIT_CHANCE  = "upg-crit-chance";
        private const string UPG_CRIT_MULT    = "upg-crit-mult";
        private const string UPG_COMBO        = "upg-combo";
        private const string UPG_N_WEIGHT     = "upg-n-weight";
        private const string UPG_R_WEIGHT     = "upg-r-weight";
        private const string UPG_SHARD_REFUND = "upg-shard-refund";
        private const string UPG_EXTRA_PULL   = "upg-extra-pull";
        private const string UPG_GACHA_DISC   = "upg-gacha-disc";

        // Hidden card IDs — must match CardCatalog entries
        private const string HIDDEN_SPEED   = "hidden-speed";
        private const string HIDDEN_VETERAN = "hidden-veteran";

        // ── Inspector ──────────────────────────────────────────────────────────
        [Header("Data — assign in Inspector")]
        [SerializeField] private CardCatalogSO    cardCatalog;
        [SerializeField] private SetCatalogSO     setCatalog;
        [SerializeField] private UpgradeCatalogSO upgradeCatalog;

        [Header("Base Settings")]
        [SerializeField] private float baseCriticalChance     = 0.1f;
        [SerializeField] private float baseCriticalMultiplier = 3f;

        // ── Private State ──────────────────────────────────────────────────────
        private readonly GachaService gachaService = new GachaService();
        private readonly Dictionary<string, CardProgress>    cardState    = new Dictionary<string, CardProgress>();
        private readonly Dictionary<string, UpgradeProgress> upgradeState = new Dictionary<string, UpgradeProgress>();
        private readonly HashSet<string> completedSets       = new HashSet<string>();
        private readonly HashSet<string> unlockedHiddenCards = new HashSet<string>();

        private float lastClickTime = -999f;
        private int   comboCount;
        private float clickWindowTimer;
        private int   clicksInWindow;
        private int   pityCounter;

        // ── Public State ───────────────────────────────────────────────────────
        public double Money          { get; private set; }
        public int    Shards         { get; private set; }
        public float  ElapsedSeconds { get; private set; }
        public int    TotalRolls     { get; private set; }
        public long   TotalClicks    { get; private set; }
        public bool   IsGameEnded    { get; private set; }
        public string EquippedCardId { get; private set; }
        public int    ComboCount     => comboCount;
        public int    ClicksPerSecond => clicksInWindow;
        public int    PityCounter    => pityCounter;
        public int    HardPityThreshold => HardPityAt;

        public CardCatalogSO    CardCatalog    => cardCatalog;
        public SetCatalogSO     SetCatalog     => setCatalog;
        public UpgradeCatalogSO UpgradeCatalog => upgradeCatalog;

        public float Completion01
        {
            get
            {
                if (cardCatalog == null || cardCatalog.Cards.Count == 0) return 0f;
                int unlocked = 0;
                for (int i = 0; i < cardCatalog.Cards.Count; i++)
                    if (cardState.TryGetValue(cardCatalog.Cards[i].Id, out var s) && s.Unlocked)
                        unlocked++;
                return unlocked / (float)cardCatalog.Cards.Count;
            }
        }

        public int UnlockedCount
        {
            get { int n = 0; foreach (var s in cardState.Values) if (s.Unlocked) n++; return n; }
        }

        public int TotalCardCount => cardCatalog != null ? cardCatalog.Cards.Count : 0;

        // ── Events ─────────────────────────────────────────────────────────────
        public event Action          StateChanged;
        public event Action<string>  LogUpdated;
        public event Action<string, CardRarity> CardDrawn;   // (cardId, rarity)
        public event Action          CriticalHit;
        public event Action<string>  SetCompleted;           // setId
        public event Action          GameEnded;
        public event Action          HardPityFired;          // 100연 보장 발동

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
            if (cardCatalog == null)
            {
                Debug.LogError("[GameManager] CardCatalogSO가 연결되지 않았습니다!");
                return;
            }
            InitCardState();
            Load();
            RebuildSetState();
            NotifyState();
        }

        private void Update()
        {
            if (IsGameEnded) return;
            ElapsedSeconds += Time.unscaledDeltaTime;

            clickWindowTimer += Time.unscaledDeltaTime;
            if (clickWindowTimer >= ClickWindowSeconds)
            {
                clickWindowTimer = 0f;
                clicksInWindow   = 0;
            }

            NotifyState();
        }

        // ── Public Actions ─────────────────────────────────────────────────────

        public void HandleCardClicked()
        {
            if (IsGameEnded) return;

            if (Time.unscaledTime - lastClickTime <= ComboWindowSeconds)
                comboCount++;
            else
                comboCount = 1;
            lastClickTime = Time.unscaledTime;

            TotalClicks++;
            clicksInWindow++;

            double comboBonus = GetUpgradeEffectValue(UPG_COMBO);
            double comboMult  = 1d + Math.Log(comboCount + 1d) * (1d + comboBonus);
            bool   isCrit     = UnityEngine.Random.value <= GetEffectiveCritChance();
            float  critMult   = isCrit ? GetEffectiveCritMult() : 1f;
            double income     = BaseClickIncome * GetEquippedIncomeMultiplier() * comboMult * critMult;

            Money += income;
            if (isCrit) CriticalHit?.Invoke();

            CheckHiddenConditions();
            Save();
            NotifyState();
        }

        public void RollOnce()
        {
            if (IsGameEnded) return;
            double cost = GetCurrentGachaCost();
            if (Money < cost) { Log("돈이 부족해요."); return; }

            Money -= cost;
            TotalRolls++;

            // ── 하드 피티: 100연마다 최고 등급 보장 ──────────────────────────
            if (pityCounter >= HardPityAt)
            {
                pityCounter = 0;
                var pityCard = GetHardPityCard();
                if (pityCard != null)
                {
                    Log($"⭐ 100연 보장 발동! {pityCard.DisplayName}");
                    HardPityFired?.Invoke();
                    ApplyDraw(pityCard);
                    CheckEnding();
                    Save();
                    NotifyState();
                    return;
                }
            }

            var card = DrawOneCard();
            if (card == null) { Log("뽑을 카드가 없어요."); Save(); NotifyState(); return; }

            ApplyDraw(card);
            CheckEnding();
            Save();
            NotifyState();
        }

        public void RollTen()
        {
            if (IsGameEnded) return;
            double cost      = GetCurrentGachaCost();
            int    extraPull = Mathf.RoundToInt(GetUpgradeEffectValue(UPG_EXTRA_PULL));
            int    pullCount = 10 + extraPull;
            float  discount  = GetUpgradeEffectValue(UPG_GACHA_DISC);
            double totalCost = cost * (pullCount - 1) * (1f - discount);   // 1장 무료

            if (Money < totalCost) { Log("돈이 부족해요."); return; }

            Money -= totalCost;
            TotalRolls += pullCount;

            for (int i = 0; i < pullCount; i++)
            {
                // 하드 피티 체크 (10연 도중 발동 가능)
                if (pityCounter >= HardPityAt)
                {
                    pityCounter = 0;
                    var pityCard = GetHardPityCard();
                    if (pityCard != null)
                    {
                        Log($"⭐ 100연 보장 발동! {pityCard.DisplayName}");
                        HardPityFired?.Invoke();
                        ApplyDraw(pityCard);
                        continue;
                    }
                }

                var card = DrawOneCard();
                if (card != null) ApplyDraw(card);
            }

            CheckEnding();
            Save();
            NotifyState();
        }

        /// <summary>조각을 소모해 특정 카드를 확정 획득합니다.</summary>
        public void ExchangeWithShards(string cardId)
        {
            if (IsGameEnded) return;
            var card = cardCatalog?.FindById(cardId);
            if (card == null || card.IsHidden) { Log("교환할 수 없는 카드입니다."); return; }

            int cost = GetShardExchangeCost(card.Rarity);
            if (Shards < cost) { Log($"조각이 부족해요. (필요: {cost})"); return; }

            Shards -= cost;
            ApplyDraw(card);
            CheckEnding();
            Save();
            NotifyState();
        }

        /// <summary>등급별 조각 교환 비용 (분해 가치의 10배)</summary>
        public static int GetShardExchangeCost(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N:   return 10;
                case CardRarity.R:   return 30;
                case CardRarity.SR:  return 100;
                case CardRarity.SSR: return 300;
                case CardRarity.UR:  return 1000;
                default:             return 30;
            }
        }

        public void BuyUpgrade(string upgradeId)
        {
            if (upgradeCatalog == null) return;
            var entry = upgradeCatalog.FindById(upgradeId);
            if (entry == null) return;

            if (!upgradeState.TryGetValue(upgradeId, out var progress))
            {
                progress = new UpgradeProgress { UpgradeId = upgradeId, Level = 0 };
                upgradeState[upgradeId] = progress;
            }

            if (progress.Level >= entry.MaxLevel) { Log("이미 최대 레벨입니다."); return; }
            if (entry.CostPerLevel == null || progress.Level >= entry.CostPerLevel.Length) return;

            double cost = entry.CostPerLevel[progress.Level];
            if (Money < cost) { Log("돈이 부족해요."); return; }

            Money -= cost;
            progress.Level++;
            Log($"{entry.DisplayName} Lv.{progress.Level} 업그레이드!");
            Save();
            NotifyState();
        }

        public void EquipCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;
            if (!cardState.TryGetValue(cardId, out var s) || !s.Unlocked) return;
            EquippedCardId = cardId;
            Save();
            NotifyState();
        }

        // ── Queries ────────────────────────────────────────────────────────────

        public int  GetUpgradeLevel(string upgradeId) =>
            upgradeState.TryGetValue(upgradeId, out var p) ? p.Level : 0;

        public bool CanAffordUpgrade(string upgradeId)
        {
            var entry = upgradeCatalog?.FindById(upgradeId);
            if (entry == null) return false;
            int lv = GetUpgradeLevel(upgradeId);
            if (lv >= entry.MaxLevel) return false;
            if (entry.CostPerLevel == null || lv >= entry.CostPerLevel.Length) return false;
            return Money >= entry.CostPerLevel[lv];
        }

        public bool   IsSetCompleted(string setId) => completedSets.Contains(setId);
        public IReadOnlyDictionary<string, CardProgress> GetCardStates() => cardState;

        public CardEntry GetEquippedCard() =>
            string.IsNullOrEmpty(EquippedCardId) ? null : cardCatalog?.FindById(EquippedCardId);

        public double GetCurrentGachaCost()
        {
            int    stage     = Mathf.FloorToInt(Completion01 * 20f);
            double scaled    = BaseGachaCost * Math.Pow(1.15d, stage);
            double milestone = 0;
            if (Completion01 >= 0.1f) milestone += 5;
            if (Completion01 >= 0.3f) milestone += 20;
            if (Completion01 >= 0.6f) milestone += 100;
            float  discount  = GetUpgradeEffectValue(UPG_GACHA_DISC);
            return Math.Round((scaled + milestone) * (1f - discount), 0, MidpointRounding.AwayFromZero);
        }

        public string GetTimerText()
        {
            var t = TimeSpan.FromSeconds(ElapsedSeconds);
            return $"{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        public int GetClicksInLastSecond() => clicksInWindow;

        // ── Private Helpers ────────────────────────────────────────────────────

        private CardEntry DrawOneCard()
        {
            float nRed = GetUpgradeEffectValue(UPG_N_WEIGHT);
            float rRed = GetUpgradeEffectValue(UPG_R_WEIGHT);
            var   card = gachaService.DrawCard(cardCatalog.Cards, cardState, Completion01,
                                               pityCounter, nRed, rRed);
            if (card == null) return null;

            if (card.Rarity >= CardRarity.SSR) pityCounter = 0;
            else                               pityCounter++;

            return card;
        }

        /// <summary>100연 보장: 현재 해금된 가장 높은 등급의 카드 중 랜덤 반환.</summary>
        private CardEntry GetHardPityCard()
        {
            CardRarity best;
            if      (Completion01 >= 0.8f) best = CardRarity.UR;
            else if (Completion01 >= 0.5f) best = CardRarity.SSR;
            else if (Completion01 >= 0.2f) best = CardRarity.SR;
            else                           best = CardRarity.R;

            var candidates = new List<CardEntry>();
            for (int i = 0; i < cardCatalog.Cards.Count; i++)
            {
                var c = cardCatalog.Cards[i];
                if (c == null || c.IsHidden || c.Rarity != best) continue;
                if (cardState.TryGetValue(c.Id, out var state) && state.Copies < c.MaxStacks)
                    candidates.Add(c);
            }

            if (candidates.Count == 0)
            {
                // 만약 최고 등급이 다 꽉 찼으면 아래 등급으로 내려감
                for (int i = 0; i < cardCatalog.Cards.Count; i++)
                {
                    var c = cardCatalog.Cards[i];
                    if (c == null || c.IsHidden) continue;
                    if (cardState.TryGetValue(c.Id, out var state) && state.Copies < c.MaxStacks)
                        candidates.Add(c);
                }
            }

            if (candidates.Count == 0) return null;
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        private void ApplyDraw(CardEntry card)
        {
            if (!cardState.TryGetValue(card.Id, out var state)) return;

            state.Unlocked = true;
            state.Copies++;

            CardDrawn?.Invoke(card.Id, card.Rarity);

            if (state.Copies > card.MaxStacks)
            {
                state.Copies = card.MaxStacks;
                float shardMult = 1.5f + GetUpgradeEffectValue(UPG_SHARD_REFUND);
                int   shards    = Mathf.RoundToInt(card.ShardValue * shardMult);
                Shards += shards;
                Log($"{card.DisplayName} 초과 중복! 조각 +{shards}");
            }
            else if (state.Copies == 1)
            {
                Log($"✨ 신규 카드 획득! {card.DisplayName} [{card.Rarity}]");
                CheckSetCompletion(card);
            }
            else
            {
                Log($"💪 중복 강화: {card.DisplayName} x{state.Copies}");
            }

            EquippedCardId = card.Id;
        }

        private void CheckSetCompletion(CardEntry newCard)
        {
            if (setCatalog == null || string.IsNullOrEmpty(newCard.SetId)) return;
            var set = setCatalog.FindById(newCard.SetId);
            if (set == null || completedSets.Contains(set.SetId)) return;

            foreach (var cardId in set.CardIds)
                if (!cardState.TryGetValue(cardId, out var s) || !s.Unlocked) return;

            completedSets.Add(set.SetId);
            Log($"🎉 세트 완성! [{set.SetName}]");
            SetCompleted?.Invoke(set.SetId);
        }

        private void RebuildSetState()
        {
            if (setCatalog == null) return;
            for (int i = 0; i < setCatalog.Sets.Count; i++)
            {
                var set = setCatalog.Sets[i];
                if (completedSets.Contains(set.SetId)) continue;
                bool allOwned = true;
                foreach (var cardId in set.CardIds)
                    if (!cardState.TryGetValue(cardId, out var s) || !s.Unlocked)
                    { allOwned = false; break; }
                if (allOwned) completedSets.Add(set.SetId);
            }
        }

        private void CheckHiddenConditions()
        {
            if (clicksInWindow >= 20 && !unlockedHiddenCards.Contains(HIDDEN_SPEED))
                UnlockHiddenCard(HIDDEN_SPEED);
            if (TotalClicks >= 1000 && !unlockedHiddenCards.Contains(HIDDEN_VETERAN))
                UnlockHiddenCard(HIDDEN_VETERAN);
        }

        private void UnlockHiddenCard(string cardId)
        {
            var card = cardCatalog?.FindById(cardId);
            if (card == null) return;

            unlockedHiddenCards.Add(cardId);
            if (cardState.TryGetValue(cardId, out var state))
            {
                state.Unlocked = true;
                state.Copies   = Mathf.Max(state.Copies, 1);
            }

            Log($"🔓 숨겨진 카드 해금! {card.DisplayName}");
            CardDrawn?.Invoke(cardId, card.Rarity);
            CheckEnding();
        }

        private void CheckEnding()
        {
            if (IsGameEnded || Completion01 < 1f) return;
            IsGameEnded = true;
            Log($"🏆 도감 100% 달성! 최종 타임: {GetTimerText()}");
            GameEnded?.Invoke();
        }

        private double GetEquippedIncomeMultiplier()
        {
            var card = GetEquippedCard();
            if (card == null) return 1d;
            if (!cardState.TryGetValue(card.Id, out var state)) return 1d;

            int    stack       = Mathf.Max(1, state.Copies);
            double enhancement = 1d + Math.Pow(stack, 1.3d);
            double multiplier  = card.ClickMultiplier * enhancement;

            if (card.SpecialEffect == CardSpecialEffect.DuplicateBonusBonus)
                multiplier *= (1f + card.SpecialEffectValue);

            return multiplier;
        }

        private float GetEffectiveCritChance() => baseCriticalChance  + GetUpgradeEffectValue(UPG_CRIT_CHANCE);
        private float GetEffectiveCritMult()   => baseCriticalMultiplier + GetUpgradeEffectValue(UPG_CRIT_MULT);

        private float GetUpgradeEffectValue(string upgradeId)
        {
            if (upgradeCatalog == null) return 0f;
            var entry = upgradeCatalog.FindById(upgradeId);
            if (entry == null) return 0f;
            int lv = GetUpgradeLevel(upgradeId);
            if (lv <= 0 || entry.EffectValuePerLevel == null || lv > entry.EffectValuePerLevel.Length) return 0f;
            return entry.EffectValuePerLevel[lv - 1];
        }

        private void InitCardState()
        {
            cardState.Clear();
            for (int i = 0; i < cardCatalog.Cards.Count; i++)
            {
                var card = cardCatalog.Cards[i];
                if (card == null) continue;
                cardState[card.Id] = new CardProgress { CardId = card.Id, Copies = 0, Unlocked = false };
            }
        }

        private void Log(string msg)  => LogUpdated?.Invoke(msg);
        private void NotifyState()    => StateChanged?.Invoke();

        // ── Save / Load ────────────────────────────────────────────────────────

        private void Save()
        {
            var data = new GameSaveData
            {
                Money = Money, Shards = Shards, ElapsedSeconds = ElapsedSeconds,
                EquippedCardId = EquippedCardId, TotalRolls = TotalRolls,
                PityCounter = pityCounter, TotalClicks = TotalClicks
            };
            foreach (var kv in cardState)
                data.Cards.Add(new CardProgress
                    { CardId = kv.Value.CardId, Copies = kv.Value.Copies, Unlocked = kv.Value.Unlocked });
            foreach (var kv in upgradeState)
                data.Upgrades.Add(new UpgradeProgress
                    { UpgradeId = kv.Value.UpgradeId, Level = kv.Value.Level });
            data.UnlockedHiddenCards.AddRange(unlockedHiddenCards);
            data.CompletedSets.AddRange(completedSets);

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            string raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            var data = JsonUtility.FromJson<GameSaveData>(raw);
            if (data == null) return;

            Money          = Math.Max(0d, data.Money);
            Shards         = Math.Max(0, data.Shards);
            ElapsedSeconds = Math.Max(0f, data.ElapsedSeconds);
            EquippedCardId = data.EquippedCardId;
            TotalRolls     = Math.Max(0, data.TotalRolls);
            pityCounter    = Math.Max(0, data.PityCounter);
            TotalClicks    = Math.Max(0, data.TotalClicks);

            if (data.Cards != null)
                foreach (var c in data.Cards)
                {
                    if (c == null || !cardState.ContainsKey(c.CardId)) continue;
                    cardState[c.CardId].Copies   = Math.Max(0, c.Copies);
                    cardState[c.CardId].Unlocked = c.Unlocked;
                }
            if (data.Upgrades != null)
                foreach (var u in data.Upgrades)
                {
                    if (u == null || string.IsNullOrEmpty(u.UpgradeId)) continue;
                    upgradeState[u.UpgradeId] = new UpgradeProgress { UpgradeId = u.UpgradeId, Level = u.Level };
                }
            if (data.UnlockedHiddenCards != null) unlockedHiddenCards.UnionWith(data.UnlockedHiddenCards);
            if (data.CompletedSets != null)       completedSets.UnionWith(data.CompletedSets);
        }
    }
}
