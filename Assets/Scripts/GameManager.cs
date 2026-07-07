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
        [SerializeField] private float baseCriticalChance     = 0f;
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

        // ── Public State ───────────────────────────────────────────────────────
        public double Money          { get; private set; }
        public int    Shards         { get; private set; }
        public float  ElapsedSeconds { get; private set; }
        public int    TotalRolls     { get; private set; }
        public long   TotalClicks    { get; private set; }
        public bool   IsGameEnded    { get; private set; }
        public bool   UnlockedRareGacha  { get; private set; }
        public bool   UnlockedSuperGacha { get; private set; }
        public string EquippedCardId { get; private set; }
        public int    ComboCount     => comboCount;
        public int    ClicksPerSecond => clicksInWindow;

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

        // ── Lifecycle ──────────────────────────────────────────────────────────
        private void Awake()
        {
#if UNITY_EDITOR
            // 인스펙터에 깨진 바인딩이 들어있을 수 있으므로 에디터 환경에서는 무조건 강제 새로고침
            cardCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            setCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            upgradeCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalogSO>("Assets/ScriptableObjects/UpgradeCatalog.asset");
#endif

            if (cardCatalog == null)
            {
                Debug.LogError("[GameManager] CardCatalogSO가 연결되지 않았습니다!");
                return;
            }
            EnsureTestCards();
            EnsureTestSets();
            InitCardState();
            Load();
            RebuildSetState();
            NotifyState();
        }

        private void EnsureTestCards()
        {
            var field = typeof(CardCatalogSO).GetField("cards", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var list = field.GetValue(cardCatalog) as List<CardEntry>;
            if (list == null) return;

            list.Clear();
            var tex = Texture2D.whiteTexture;
            var defaultSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            for (int i = 1; i <= 10; i++)
            {
                CardRarity rarity = CardRarity.N;
                if (i >= 5 && i <= 7)       rarity = CardRarity.R;
                else if (i >= 8 && i <= 9)  rarity = CardRarity.SR;
                else if (i == 10)           rarity = CardRarity.SSR;

                list.Add(new CardEntry
                {
                    Id = $"cat-{i}",
                    DisplayName = $"고양이 {i}",
                    Description = $"고양이{i} 상세 설명입니다.",
                    Rarity = rarity,
                    BaseWeight = i == 10 ? 10f : (i >= 8 ? 30f : (i >= 5 ? 100f : 500f)),
                    ClickMultiplier = i,
                    ShardValue = rarity == CardRarity.SSR ? 30 : (rarity == CardRarity.SR ? 10 : (rarity == CardRarity.R ? 3 : 1)),
                    MaxStacks = 6,
                    SetId = i <= 5 ? "set-cat-a" : "set-cat-b",
                    IsHidden = false,
                    CardSprite = defaultSprite,
                    SpecialEffect = i % 3 == 1 ? CardSpecialEffect.CriticalChanceBonus : CardSpecialEffect.None,
                    SpecialEffectValue = 0.05f
                });
            }
        }

        private void EnsureTestSets()
        {
            if (setCatalog == null) return;
            var field = typeof(SetCatalogSO).GetField("sets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field == null) return;
            var list = field.GetValue(setCatalog) as List<SetEntry>;
            if (list == null) return;

            list.Clear();
            list.Add(new SetEntry
            {
                SetId = "set-cat-a",
                SetName = "고양이 A 세트 (1~5)",
                CardIds = new List<string> { "cat-1", "cat-2", "cat-3", "cat-4", "cat-5" },
                SetCardWeightBonus = 1.2f,
                StackEffectBonus = 0.5f,
                ShardBonusMultiplier = 1.2f
            });
            list.Add(new SetEntry
            {
                SetId = "set-cat-b",
                SetName = "고양이 B 세트 (6~10)",
                CardIds = new List<string> { "cat-6", "cat-7", "cat-8", "cat-9", "cat-10" },
                SetCardWeightBonus = 1.2f,
                StackEffectBonus = 0.5f,
                ShardBonusMultiplier = 1.2f
            });
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
            double comboMult  = 1.0;
            if (comboCount >= 100)
            {
                comboMult = 1.0 + (comboCount / 100.0) * 0.1 * (1.0 + comboBonus);
            }
            bool   isCrit     = UnityEngine.Random.value <= GetEffectiveCritChance();
            float  critMult   = isCrit ? GetEffectiveCritMult() : 1f;
            double income     = BaseClickIncome * GetEquippedIncomeMultiplier() * comboMult * critMult;

            Money += income;
            if (isCrit) CriticalHit?.Invoke();

            CheckHiddenConditions();
            Save();
            NotifyState();
        }

        public CardEntry RollOnce(GachaType type = GachaType.Normal)
        {
            if (IsGameEnded) return null;
            double cost = GetCurrentGachaCost(type);
            if (Money < cost) { Log("돈이 부족해요."); return null; }

            Money -= cost;
            TotalRolls++;

            var card = DrawOneCard(type);
            if (card == null) { Log("뽑을 카드가 없어요."); Save(); NotifyState(); return null; }

            ApplyDraw(card);
            CheckEnding();
            Save();
            NotifyState();
            return card;
        }

        public List<CardEntry> RollTen(GachaType type = GachaType.Normal)
        {
            if (IsGameEnded) return null;
            double cost      = GetCurrentGachaCost(type);
            int    pullCount = 10;
            float  discount  = GetUpgradeEffectValue(UPG_GACHA_DISC);
            double totalCost = cost * 10f * (1f - discount);

            if (Money < totalCost) { Log("돈이 부족해요."); return null; }

            Money -= totalCost;
            TotalRolls += pullCount;

            var drawnCards = new List<CardEntry>();
            for (int i = 0; i < pullCount; i++)
            {
                var card = DrawOneCard(type);
                if (card != null) 
                {
                    ApplyDraw(card);
                    drawnCards.Add(card);
                }
            }

            CheckEnding();
            Save();
            NotifyState();
            return drawnCards;
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

        public bool BreakthroughCard(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;
            if (cardCatalog == null) return false;
            var card = cardCatalog.FindById(cardId);
            if (card == null) return false;
            if (!cardState.TryGetValue(cardId, out var state) || !state.Unlocked) return false;

            if (state.BreakthroughCount >= 5) return false;
            if (state.Copies <= state.BreakthroughCount + 1) return false;

            state.BreakthroughCount++;
            Log($"🌟 {card.DisplayName} 한계 돌파! ({state.BreakthroughCount}강 달성)");
            
            Save();
            NotifyState();
            return true;
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

        public double GetCurrentGachaCost(GachaType type = GachaType.Normal)
        {
            int    stage     = Mathf.FloorToInt(Completion01 * 20f);
            double scaled    = BaseGachaCost * Math.Pow(1.15d, stage);
            double milestone = 0;
            if (Completion01 >= 0.1f) milestone += 5;
            if (Completion01 >= 0.3f) milestone += 20;
            if (Completion01 >= 0.6f) milestone += 100;
            float  discount  = GetUpgradeEffectValue(UPG_GACHA_DISC);
            
            double baseTypeCost = scaled + milestone;
            if (type == GachaType.Rare) baseTypeCost *= 5; // 레어가챠는 5배 비쌈
            if (type == GachaType.Super) baseTypeCost *= 20; // 슈퍼가챠는 20배 비쌈

            return Math.Round(baseTypeCost * (1f - discount), 0, MidpointRounding.AwayFromZero);
        }

        public void UnlockGachaType(GachaType type, double cost)
        {
            if (Money < cost) { Log("돈이 부족해요."); return; }
            if (type == GachaType.Rare && UnlockedRareGacha) return;
            if (type == GachaType.Super && UnlockedSuperGacha) return;

            Money -= cost;
            if (type == GachaType.Rare) UnlockedRareGacha = true;
            if (type == GachaType.Super) UnlockedSuperGacha = true;
            
            Log($"{type} 가챠가 해금되었습니다!");
            Save();
            NotifyState();
        }

        public string GetTimerText()
        {
            var t = TimeSpan.FromSeconds(ElapsedSeconds);
            return $"{t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}";
        }

        public int GetClicksInLastSecond() => clicksInWindow;

        // ── Private Helpers ────────────────────────────────────────────────────

        private CardEntry DrawOneCard(GachaType type)
        {
            float nRed = GetUpgradeEffectValue(UPG_N_WEIGHT);
            float rRed = GetUpgradeEffectValue(UPG_R_WEIGHT);
            var   card = gachaService.DrawCard(cardCatalog.Cards, cardState, Completion01,
                                               nRed, rRed, type);
            return card;
        }

        private void ApplyDraw(CardEntry card)
        {
            if (!cardState.TryGetValue(card.Id, out var state)) return;

            state.Unlocked = true;
            state.Copies++;

            CardDrawn?.Invoke(card.Id, card.Rarity);

            int maxStacks = card.MaxStacks;
            if (state.Copies > maxStacks)
            {
                state.Copies = maxStacks;
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

            double multiplier = card.ClickMultiplier * (1 + state.BreakthroughCount);
            if (state.BreakthroughCount >= 5)
            {
                multiplier += card.ClickMultiplier;
            }

            if (card.SpecialEffect == CardSpecialEffect.DuplicateBonusBonus)
                multiplier *= (1f + card.SpecialEffectValue);

            return multiplier;
        }

        private float GetEffectiveCritChance() => baseCriticalChance  + GetUpgradeEffectValue(UPG_CRIT_CHANCE);
        private float GetEffectiveCritMult()   => baseCriticalMultiplier + GetUpgradeEffectValue(UPG_CRIT_MULT);

        public float GetUpgradeEffectValue(string upgradeId)
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

        // ── Public Save Hook ────────────────────────────────────────────────────
        /// <summary>외부(GameHud 등)에서 명시적으로 저장을 호출할 때 사용합니다.</summary>
        public void SaveGame() => Save();

        private void OnApplicationQuit()    => Save();
        private void OnApplicationPause(bool pausing) { if (pausing) Save(); }

        private void Save()
        {
            var data = new GameSaveData
            {
                Money = Money, Shards = Shards, ElapsedSeconds = ElapsedSeconds,
                EquippedCardId = EquippedCardId, TotalRolls = TotalRolls,
                TotalClicks = TotalClicks,
                UnlockedRareGacha = UnlockedRareGacha,
                UnlockedSuperGacha = UnlockedSuperGacha
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
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Money = 1000d;
                EquippedCardId = "cat-1";
                if (cardState.ContainsKey("cat-1"))
                {
                    cardState["cat-1"].Unlocked = true;
                    cardState["cat-1"].Copies = 1;
                }
                Save();
                return;
            }
            string raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return;
            var data = JsonUtility.FromJson<GameSaveData>(raw);
            if (data == null) return;

            Money          = Math.Max(0d, data.Money);
            Shards         = Math.Max(0, data.Shards);
            ElapsedSeconds = Math.Max(0f, data.ElapsedSeconds);
            EquippedCardId = data.EquippedCardId;
            TotalRolls     = Math.Max(0, data.TotalRolls);
            TotalClicks    = Math.Max(0, data.TotalClicks);
            UnlockedRareGacha  = data.UnlockedRareGacha;
            UnlockedSuperGacha = data.UnlockedSuperGacha;

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

            // 기본 장착 보장
            if (string.IsNullOrEmpty(EquippedCardId) || !cardState.ContainsKey(EquippedCardId) || !cardState[EquippedCardId].Unlocked)
            {
                EquippedCardId = "cat-1";
                if (cardState.ContainsKey("cat-1"))
                {
                    cardState["cat-1"].Unlocked = true;
                    if (cardState["cat-1"].Copies == 0) cardState["cat-1"].Copies = 1;
                }
            }
        }
    }
}
