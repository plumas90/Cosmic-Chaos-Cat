using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        // ── Constants ──────────────────────────────────────────────────────────
        private const string SaveKey            = "ccc_save_v3";
        private const double BaseClickIncome    = 1d;
        private const double BaseGachaCost      = 10d;
        private const float  ComboWindowSeconds = 1.0f;
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
        [SerializeField] private CardCatalogSO       cardCatalog;
        [SerializeField] private SetCatalogSO        setCatalog;
        [SerializeField] private UpgradeCatalogSO    upgradeCatalog;
        [SerializeField] private BackgroundCatalogSO backgroundCatalog;
        [SerializeField] private DecorationCatalogSO decorationCatalog;

        [Header("Base Settings")]
        [SerializeField] private float baseCriticalChance     = 0f;
        [SerializeField] private float baseCriticalMultiplier = 3f;

        // ── Private State ──────────────────────────────────────────────────────
        private readonly GachaService gachaService = new GachaService();
        private readonly Dictionary<string, CardProgress>    cardState    = new Dictionary<string, CardProgress>();
        private readonly Dictionary<string, UpgradeProgress> upgradeState = new Dictionary<string, UpgradeProgress>();
        private readonly HashSet<string> completedSets       = new HashSet<string>();
        private readonly HashSet<string> claimedSetRewards   = new HashSet<string>();
        private readonly HashSet<string> unlockedHiddenCards = new HashSet<string>();
        private readonly HashSet<string> unlockedBackgrounds = new HashSet<string> { "bg-none" };
        private readonly HashSet<string> unlockedDecorations = new HashSet<string> { "deco-none" };

        private float lastClickTime = -999f;
        private int   comboCount;
        private float clickWindowTimer;
        private int   clicksInWindow;
        private TMPro.TMP_FontAsset cachedFont;

        // ── Public State ───────────────────────────────────────────────────────
        public double Money          { get; private set; }
        public int    Shards         { get; private set; }
        public float  ElapsedSeconds { get; private set; }
        public int    TotalRolls     { get; private set; }
        public long   TotalClicks    { get; private set; }
        public bool   IsGameEnded    { get; private set; }
        public bool   UnlockedRareGacha  { get; private set; }
        public bool   UnlockedSuperGacha { get; private set; }

        public float BgmVolume { get; private set; } = 1f;
        public float SfxVolume { get; private set; } = 1f;
        public bool IsMuted { get; private set; } = false;
        public string SelectedLanguage { get; private set; } = "KR";
        public int    NExchangeCount     { get; private set; }
        public int    RExchangeCount     { get; private set; }
        public int    SRExchangeCount    { get; private set; }
        public int    SSRExchangeCount   { get; private set; }
        public int    URExchangeCount    { get; private set; }
        public string EquippedCardId { get; private set; }
        public string EquippedBackgroundId { get; private set; }
        public string EquippedDecorationId { get; private set; }
        public int    ComboCount     => comboCount;
        public int    ClicksPerSecond => clicksInWindow;

        public CardCatalogSO       CardCatalog       => cardCatalog;
        public SetCatalogSO        SetCatalog        => setCatalog;
        public UpgradeCatalogSO    UpgradeCatalog    => upgradeCatalog;
        public BackgroundCatalogSO BackgroundCatalog => backgroundCatalog;
        public DecorationCatalogSO DecorationCatalog => decorationCatalog;

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
            Instance = this;
#if UNITY_EDITOR
            // 인스펙터에 깨진 바인딩이 들어있을 수 있으므로 에디터 환경에서는 무조건 강제 새로고침
            cardCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<CardCatalogSO>("Assets/ScriptableObjects/CardCatalog.asset");
            setCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<SetCatalogSO>("Assets/ScriptableObjects/SetCatalog.asset");
            upgradeCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCatalogSO>("Assets/ScriptableObjects/UpgradeCatalog.asset");
            backgroundCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<BackgroundCatalogSO>("Assets/ScriptableObjects/BackgroundCatalog.asset");
            decorationCatalog = UnityEditor.AssetDatabase.LoadAssetAtPath<DecorationCatalogSO>("Assets/ScriptableObjects/DecorationCatalog.asset");
#endif

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

        private void Start()
        {
            LocalizationManager.NotifyLanguageChanged();
        }


        [ContextMenu("Add 100,000 Gold")]
        public void AddTestMoney(double amount = 100000d)
        {
            Money += amount;
            Save();
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

            // 1초 동안 추가 입력이 없으면 콤보 리셋
            if (comboCount > 0 && Time.unscaledTime - lastClickTime > ComboWindowSeconds)
            {
                comboCount = 0;
                NotifyState();
            }
        }

        // ── Public Actions ─────────────────────────────────────────────────────

        public void HandleCardClicked()
        {
            HandleCardClicked(Input.mousePosition);
        }

        public void HandleCardClicked(Vector2 screenPos)
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
            
            // Check combo milestones for instant gold rewards and reset combo to 0
            double comboReward = 0d;
            if (comboCount == 100)
            {
                comboReward = 10d * (1.0 + comboBonus);
                Money += comboReward;
                SpawnComboRewardFloatingText(comboReward);
                Log($"[100 콤보 보상] +{comboReward:F0} 골드 획득!");
            }
            else if (comboCount == 500)
            {
                comboReward = 100d * (1.0 + comboBonus);
                Money += comboReward;
                SpawnComboRewardFloatingText(comboReward);
                Log($"[500 콤보 보상] +{comboReward:F0} 골드 획득!");
            }
            else if (comboCount >= 999)
            {
                comboReward = 1000d * (1.0 + comboBonus);
                Money += comboReward;
                SpawnComboRewardFloatingText(comboReward);
                comboCount = 0;
                Log($"[999 콤보 달성!] +{comboReward:F0} 골드 획득 및 콤보 초기화!");
            }

            bool   isCrit     = UnityEngine.Random.value <= GetEffectiveCritChance();
            float  critMult   = isCrit ? GetEffectiveCritMult() : 1f;
            
            double flatSetIncome = 0d;
            if (setCatalog != null)
            {
                foreach (var setId in completedSets)
                {
                    var set = setCatalog.FindById(setId);
                    if (set != null) flatSetIncome += set.FlatIncomeBonus;
                }
            }

            double income     = (BaseClickIncome * GetEquippedIncomeMultiplier() * critMult) + flatSetIncome;

            Money += income + comboReward;
            if (isCrit) CriticalHit?.Invoke();

            // Spawn floating text on the click position
            SpawnFloatingText(screenPos, income + comboReward, isCrit);

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
            double totalCost = cost * 10f;

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
            var card = cardCatalog?.FindById(cardId);
            if (card == null || card.IsHidden) { Log("교환할 수 없는 카드입니다."); return; }

            int cost = GetShardExchangeCost(card.Rarity);
            if (Shards < cost) { Log($"조각이 부족해요. (필요: {cost})"); return; }

            Shards -= cost;
            
            // Increment the purchase count for this rarity tier
            if (card.Rarity == CardRarity.N) NExchangeCount++;
            else if (card.Rarity == CardRarity.R) RExchangeCount++;
            else if (card.Rarity == CardRarity.SR) SRExchangeCount++;
            else if (card.Rarity == CardRarity.SSR) SSRExchangeCount++;
            else if (card.Rarity == CardRarity.UR) URExchangeCount++;

            ApplyDraw(card);
            CheckEnding();
            Save();
            NotifyState();
        }

        public int GetShardExchangeCost(CardRarity rarity)
        {
            int baseCost;
            int count = 0;
            switch (rarity)
            {
                case CardRarity.N:
                    baseCost = 100;
                    count = NExchangeCount;
                    break;
                case CardRarity.R:
                    baseCost = 500;
                    count = RExchangeCount;
                    break;
                case CardRarity.SR:
                    baseCost = 5000;
                    count = SRExchangeCount;
                    break;
                case CardRarity.SSR:
                    baseCost = 10000;
                    count = SSRExchangeCount;
                    break;
                case CardRarity.UR:
                    baseCost = 10000;
                    count = SSRExchangeCount;
                    break;
                default:
                    baseCost = 100;
                    break;
            }

            // Linear price scaling: BaseCost * (PurchaseCount + 1)
            // (e.g. N: 100 -> 200 -> 300 -> 400...)
            return baseCost * (count + 1);
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
            bool isShardUpgrade = entry.UpgradeId.StartsWith("upg-unlock-");

            if (isShardUpgrade)
            {
                if (Shards < (int)cost) { Log("조각이 부족해요."); return; }
                Shards -= (int)cost;
            }
            else
            {
                if (Money < cost) { Log("돈이 부족해요."); return; }
                Money -= cost;
            }

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

        public void EquipBackground(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            EquippedBackgroundId = id;
            Save();
            NotifyState();
        }

        public void EquipDecoration(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            EquippedDecorationId = id;
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

            if (state.BreakthroughCount >= 4) return false;
            if (state.Copies <= state.BreakthroughCount + 1) return false;

            state.BreakthroughCount++;
            Log($"🌟 {card.DisplayName} 한계 돌파! ({state.BreakthroughCount + 1}강 달성)");
            
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

        public bool IsBackgroundUnlocked(string id) =>
            string.IsNullOrEmpty(id) || id == "bg-none" || unlockedBackgrounds.Contains(id);

        public bool IsDecorationUnlocked(string id) =>
            string.IsNullOrEmpty(id) || id == "deco-none" || unlockedDecorations.Contains(id);

        public void UnlockBackground(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!unlockedBackgrounds.Contains(id))
            {
                unlockedBackgrounds.Add(id);
                var bg = backgroundCatalog?.FindById(id);
                string name = bg != null ? bg.DisplayName : id;
                Log($"🖼️ 새로운 배경 [{name}] 해금!");
                Save();
                NotifyState();
            }
        }

        public void UnlockDecoration(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (!unlockedDecorations.Contains(id))
            {
                unlockedDecorations.Add(id);
                var deco = decorationCatalog?.FindById(id);
                string name = deco != null ? deco.DisplayName : id;
                Log($"🎀 새로운 데코 [{name}] 해금!");
                Save();
                NotifyState();
            }
        }

        public void ResetBackgroundsAndDecorationsForTest()
        {
            unlockedBackgrounds.Clear();
            unlockedBackgrounds.Add("bg");
            unlockedBackgrounds.Add("bg-none");
            EquippedBackgroundId = "bg";

            unlockedDecorations.Clear();
            unlockedDecorations.Add("deco-none");
            EquippedDecorationId = "deco-none";

            claimedSetRewards.Clear();

            // Grant all cards with 5 copies to complete sets
            if (cardCatalog != null && cardCatalog.Cards != null)
            {
                foreach (var card in cardCatalog.Cards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.Id))
                    {
                        if (!cardState.TryGetValue(card.Id, out var state))
                        {
                            cardState[card.Id] = new CardProgress { CardId = card.Id, Copies = 5, Unlocked = true };
                        }
                        else
                        {
                            state.Unlocked = true;
                            state.Copies = 5;
                        }
                    }
                }
            }

            // Mark all sets completed
            if (setCatalog != null && setCatalog.Sets != null)
            {
                foreach (var s in setCatalog.Sets)
                {
                    if (s != null && !completedSets.Contains(s.SetId))
                    {
                        completedSets.Add(s.SetId);
                    }
                }
            }

            Save();
            NotifyState();
            Debug.Log("[GameManager] ResetBackgroundsAndDecorationsForTest completed: All cards unlocked with 5 copies, Sets completed, Rewards unclaimed, BGs & Decos locked!");
        }

        public bool   IsSetCompleted(string setId) => completedSets.Contains(setId);
        public bool   IsSetRewardClaimed(string setId) => claimedSetRewards.Contains(setId);

        public void ClaimSetReward(string setId)
        {
            if (setCatalog == null) return;
            var set = setCatalog.FindById(setId);
            if (set == null) return;

            if (IsSetCompleted(setId) && !IsSetRewardClaimed(setId))
            {
                claimedSetRewards.Add(setId);

                if (set.RewardGold > 0d) Money += set.RewardGold;
                if (set.RewardShards > 0) Shards += set.RewardShards;

                if (!string.IsNullOrEmpty(set.RewardBackgroundId))
                    UnlockBackground(set.RewardBackgroundId);
                if (!string.IsNullOrEmpty(set.RewardDecorationId))
                    UnlockDecoration(set.RewardDecorationId);

                // Fallback for sets with no explicit rewards specified
                if (set.RewardGold <= 0d && set.RewardShards <= 0 && string.IsNullOrEmpty(set.RewardBackgroundId) && string.IsNullOrEmpty(set.RewardDecorationId))
                {
                    Shards += 1000;
                }

                string msg = $"🎁 [{set.SetName}] 세트 완료 보상 수령!";
                if (set.RewardGold > 0d) msg += $" 골드 +{set.RewardGold:N0}";
                if (set.RewardShards > 0) msg += $" 조각 +{set.RewardShards:N0}";
                if (!string.IsNullOrEmpty(set.RewardBackgroundId)) msg += $" 배경 해금({set.RewardBackgroundId})";
                if (!string.IsNullOrEmpty(set.RewardDecorationId)) msg += $" 데코 해금({set.RewardDecorationId})";
                if (set.RewardGold <= 0d && set.RewardShards <= 0 && string.IsNullOrEmpty(set.RewardBackgroundId) && string.IsNullOrEmpty(set.RewardDecorationId)) msg += " 조각 +1,000";

                Log(msg);
                Save();
                NotifyState();
            }
        }

        private static readonly string[] MoneySuffixes = new string[]
        {
            "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc"
        };

        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return "0";
            if (value < 0) return "-" + FormatNumber(-value);
            if (value < 1000d)
            {
                return Math.Floor(value).ToString("F0");
            }

            int suffixIndex = 0;
            double num = value;
            while (num >= 1000d && suffixIndex < MoneySuffixes.Length - 1)
            {
                num /= 1000d;
                suffixIndex++;
            }

            double floored = Math.Floor(num * 100d) / 100d;
            return floored.ToString("0.##") + MoneySuffixes[suffixIndex];
        }

        public IReadOnlyDictionary<string, CardProgress> GetCardStates() => cardState;

        public CardEntry GetEquippedCard() =>
            string.IsNullOrEmpty(EquippedCardId) ? null : cardCatalog?.FindById(EquippedCardId);

        public void SetCardSelectedStage(string cardId, int stage)
        {
            if (string.IsNullOrEmpty(cardId) || !cardState.ContainsKey(cardId)) return;
            cardState[cardId].SelectedStage = stage;
            Save();
            NotifyState();
        }

        public Sprite GetCardSpriteForDisplay(string cardId)
        {
            if (string.IsNullOrEmpty(cardId) || cardCatalog == null) return null;
            var card = cardCatalog.FindById(cardId);
            if (card == null) return null;
            if (cardState.TryGetValue(cardId, out var prog))
            {
                int stage = prog.SelectedStage > 0 ? prog.SelectedStage : 1;
                return card.GetSpriteForStage(stage);
            }
            return card.CardSprite;
        }

        public double GetCurrentGachaCost(GachaType type = GachaType.Normal)
        {
            double baseTypeCost = 100d;
            if (type == GachaType.Rare) baseTypeCost = 1000d;
            if (type == GachaType.Super) baseTypeCost = 10000d;

            float  discount  = GetUpgradeEffectValue(UPG_GACHA_DISC);
            float  setDiscount = 0f;
            if (setCatalog != null)
            {
                foreach (var setId in completedSets)
                {
                    var set = setCatalog.FindById(setId);
                    if (set != null) setDiscount += set.GachaDiscountBonus;
                }
            }
            float totalDiscount = Mathf.Clamp(discount + setDiscount, 0f, 0.9f); // Cap discount at 90%
            
            return Math.Round(baseTypeCost * (1f - totalDiscount), 0, MidpointRounding.AwayFromZero);
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
            float nToRMod = 0f;
            float rToSRMod = 0f;

            if (type == GachaType.Normal)
            {
                nToRMod = GetUpgradeEffectValue("upg-normal-prob-1");
                rToSRMod = GetUpgradeEffectValue("upg-normal-prob-2");
            }
            else if (type == GachaType.Rare)
            {
                nToRMod = GetUpgradeEffectValue("upg-rare-prob-1");
                rToSRMod = GetUpgradeEffectValue("upg-rare-prob-2");
            }
            else if (type == GachaType.Super)
            {
                nToRMod = GetUpgradeEffectValue("upg-super-prob-1");
                rToSRMod = GetUpgradeEffectValue("upg-super-prob-2");
            }

            bool rUnlocked = GetUpgradeLevel("upg-unlock-r") >= 1;
            bool srUnlocked = GetUpgradeLevel("upg-unlock-sr") >= 1;
            bool ssrUnlocked = GetUpgradeLevel("upg-unlock-ssr") >= 1;
            bool urUnlocked = GetUpgradeLevel("upg-unlock-ur") >= 1;

            var card = gachaService.DrawCard(cardCatalog.Cards, cardState,
                                             nToRMod, rToSRMod,
                                             rUnlocked, srUnlocked, ssrUnlocked, urUnlocked, type);
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
                int   shards    = Mathf.RoundToInt((int)card.ShardValue * shardMult);
                Shards += shards;
                Log($"{card.DisplayName} 초과 중복! 조각 +{shards}");
            }
            else if (state.Copies == 1)
            {
                Log($"✨ 신규 카드 획득! {card.DisplayName} [{card.Rarity}]");
                CheckSetCompletion(card);
                if (card.IsHidden)
                {
                    SurprisePopUp.ShowCard(card);
                }
            }
            else
            {
                Log($"💪 중복 강화: {card.DisplayName} x{state.Copies}");
            }
        }

        private void CheckSetCompletion(CardEntry newCard)
        {
            if (setCatalog == null || cardCatalog == null || string.IsNullOrEmpty(newCard.SetId)) return;
            var set = setCatalog.FindById(newCard.SetId);
            if (set == null || completedSets.Contains(set.SetId)) return;

            var setCards = set.GetCardsInSet(cardCatalog.Cards);
            if (setCards.Count == 0) return;

            foreach (var card in setCards)
                if (!cardState.TryGetValue(card.Id, out var s) || !s.Unlocked) return;

            completedSets.Add(set.SetId);
            Log($"🎉 세트 완성! [{set.SetName}]");
            SetCompleted?.Invoke(set.SetId);
        }

        private void RebuildSetState()
        {
            if (setCatalog == null || cardCatalog == null) return;
            for (int i = 0; i < setCatalog.Sets.Count; i++)
            {
                var set = setCatalog.Sets[i];
                if (completedSets.Contains(set.SetId)) continue;
                var setCards = set.GetCardsInSet(cardCatalog.Cards);
                if (setCards.Count == 0) continue;
                bool allOwned = true;
                foreach (var card in setCards)
                    if (!cardState.TryGetValue(card.Id, out var s) || !s.Unlocked)
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

            SurprisePopUp.ShowCard(card);
        }

        private void CheckEnding()
        {
            if (IsGameEnded || Completion01 < 1f) return;
            IsGameEnded = true;
            Log($"🏆 도감 100% 달성! 최종 타임: {GetTimerText()}");
            GameEnded?.Invoke();
        }

        public double GetClickIncome(CardEntry card, CardProgress state)
        {
            if (card == null) return 1d;
            int breakthrough = state != null ? state.BreakthroughCount : 0;
            double multiplier = card.ClickMultiplier * (1 + breakthrough);
            if (breakthrough >= 5)
            {
                multiplier += card.ClickMultiplier;
            }

            if (card.SpecialEffect == CardSpecialEffect.DuplicateBonusBonus)
                multiplier *= (1f + card.SpecialEffectValue);

            return multiplier;
        }

        private double GetEquippedIncomeMultiplier()
        {
            var card = GetEquippedCard();
            if (card == null) return 1d;
            if (!cardState.TryGetValue(card.Id, out var state)) return 1d;

            return GetClickIncome(card, state);
        }

        private float GetEffectiveCritChance()
        {
            float chance = baseCriticalChance + GetUpgradeEffectValue(UPG_CRIT_CHANCE);
            if (setCatalog != null)
            {
                foreach (var setId in completedSets)
                {
                    var set = setCatalog.FindById(setId);
                    if (set != null) chance += set.CriticalChanceBonus;
                }
            }
            return chance;
        }

        private float GetEffectiveCritMult()
        {
            float mult = baseCriticalMultiplier * (1f + GetUpgradeEffectValue(UPG_CRIT_MULT));
            float setBonus = 0f;
            if (setCatalog != null)
            {
                foreach (var setId in completedSets)
                {
                    var set = setCatalog.FindById(setId);
                    if (set != null) setBonus += set.CriticalDamageBonus;
                }
            }
            return mult + setBonus;
        }

        public float GetUpgradeEffectValue(string upgradeId)
        {
            if (upgradeCatalog == null) return 0f;
            var entry = upgradeCatalog.FindById(upgradeId);
            if (entry == null) return 0f;
            int lv = GetUpgradeLevel(upgradeId);
            if (lv <= 0 || entry.EffectValuePerLevel == null || lv > entry.EffectValuePerLevel.Length) return 0f;
            return entry.EffectValuePerLevel[lv - 1];
        }

        private string GetDefaultFirstCardId()
        {
            if (cardCatalog != null && cardCatalog.Cards != null && cardCatalog.Cards.Count > 0)
            {
                if (cardCatalog.Cards[0] != null && !string.IsNullOrEmpty(cardCatalog.Cards[0].Id))
                    return cardCatalog.Cards[0].Id;
            }
            return "01";
        }

        private void InitCardState()
        {
            cardState.Clear();
            if (cardCatalog == null || cardCatalog.Cards == null) return;
            string defaultCardId = GetDefaultFirstCardId();

            for (int i = 0; i < cardCatalog.Cards.Count; i++)
            {
                var card = cardCatalog.Cards[i];
                if (card == null) continue;
                bool isDefault = card.Id == defaultCardId;
                cardState[card.Id] = new CardProgress
                {
                    CardId = card.Id,
                    Copies = isDefault ? 1 : 0,
                    Unlocked = isDefault
                };
            }
        }

        public bool DeductMoney(double amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        public bool DeductShards(int amount)
        {
            if (Shards < amount) return false;
            Shards -= amount;
            return true;
        }

        public void GrantCard(string cardId)
        {
            if (cardCatalog == null) return;
            var card = cardCatalog.FindById(cardId);
            if (card == null) return;

            if (!cardState.TryGetValue(card.Id, out var state))
            {
                state = new CardProgress { CardId = card.Id, Unlocked = true, Copies = 1 };
                cardState[card.Id] = state;
            }
            else
            {
                state.Unlocked = true;
                state.Copies++;
            }

            CheckSetCompletion(card);
            Save();
            NotifyState();
        }

        public void IncrementExchangeCount(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.N:   NExchangeCount++; break;
                case CardRarity.R:   RExchangeCount++; break;
                case CardRarity.SR:  SRExchangeCount++; break;
                case CardRarity.SSR: SSRExchangeCount++; break;
                case CardRarity.UR:  URExchangeCount++; break;
            }
        }

        public void NotifyStateChange() => NotifyState();

        private void Log(string msg)  => LogUpdated?.Invoke(msg);
        public void NotifyState()    => StateChanged?.Invoke();

        // ── Public Save Hook ────────────────────────────────────────────────────
        /// <summary>외부(GameHud 등)에서 명시적으로 저장을 호출할 때 사용합니다.</summary>
        public void SaveGame() => Save();

        private void OnApplicationQuit()              => Save();
        private void OnApplicationPause(bool pausing) { if (pausing) Save(); }
        private void OnDisable()                      => Save();
        private void OnDestroy()                      => Save();

        public void SetBgmVolume(float vol) { BgmVolume = Mathf.Clamp01(vol); Save(); }
        public void SetSfxVolume(float vol) { SfxVolume = Mathf.Clamp01(vol); Save(); }
        public void SetMuted(bool mute)     { IsMuted = mute; Save(); }
        public void SetLanguage(string lang)
        {
            SelectedLanguage = lang;
            Save();
            NotifyState();
            LocalizationManager.NotifyLanguageChanged();
        }

        public void Save()
        {
            var data = new GameSaveData
            {
                Money = Money, Shards = Shards, ElapsedSeconds = ElapsedSeconds,
                EquippedCardId = EquippedCardId,
                EquippedBackgroundId = EquippedBackgroundId,
                EquippedDecorationId = EquippedDecorationId,
                TotalRolls = TotalRolls,
                TotalClicks = TotalClicks,
                UnlockedRareGacha = UnlockedRareGacha,
                UnlockedSuperGacha = UnlockedSuperGacha,
                nExchangeCount = NExchangeCount,
                rExchangeCount = RExchangeCount,
                srExchangeCount = SRExchangeCount,
                ssrExchangeCount = SSRExchangeCount,
                urExchangeCount = URExchangeCount,
                BgmVolume = BgmVolume,
                SfxVolume = SfxVolume,
                IsMuted = IsMuted,
                SelectedLanguage = SelectedLanguage
            };
            foreach (var kv in cardState)
                data.Cards.Add(new CardProgress
                {
                    CardId = kv.Value.CardId,
                    Copies = kv.Value.Copies,
                    Unlocked = kv.Value.Unlocked,
                    BreakthroughCount = kv.Value.BreakthroughCount
                });
            foreach (var kv in upgradeState)
                data.Upgrades.Add(new UpgradeProgress
                    { UpgradeId = kv.Value.UpgradeId, Level = kv.Value.Level });
            data.UnlockedHiddenCards.AddRange(unlockedHiddenCards);
            data.CompletedSets.AddRange(completedSets);
            data.ClaimedSetRewards.AddRange(claimedSetRewards);
            data.UnlockedBackgrounds.AddRange(unlockedBackgrounds);
            data.UnlockedDecorations.AddRange(unlockedDecorations);

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            string defaultCardId = GetDefaultFirstCardId();
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                Money = 1000d;
                EquippedCardId = defaultCardId;
                EquippedBackgroundId = "bg";
                EquippedDecorationId = "deco-none";
                if (cardState.ContainsKey(defaultCardId))
                {
                    cardState[defaultCardId].Unlocked = true;
                    cardState[defaultCardId].Copies = 1;
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
            EquippedBackgroundId = string.IsNullOrEmpty(data.EquippedBackgroundId) ? "bg" : data.EquippedBackgroundId;
            EquippedDecorationId = string.IsNullOrEmpty(data.EquippedDecorationId) ? "deco-none" : data.EquippedDecorationId;
            TotalRolls     = Math.Max(0, data.TotalRolls);
            TotalClicks    = Math.Max(0, data.TotalClicks);
            UnlockedRareGacha  = data.UnlockedRareGacha;
            UnlockedSuperGacha = data.UnlockedSuperGacha;
            NExchangeCount     = Math.Max(0, data.nExchangeCount);
            RExchangeCount     = Math.Max(0, data.rExchangeCount);
            SRExchangeCount    = Math.Max(0, data.srExchangeCount);
            SSRExchangeCount   = Math.Max(0, data.ssrExchangeCount);
            URExchangeCount    = Math.Max(0, data.urExchangeCount);

            if (data.Cards != null)
                foreach (var c in data.Cards)
                {
                    if (c == null || !cardState.ContainsKey(c.CardId)) continue;
                    cardState[c.CardId].Copies            = Math.Max(0, c.Copies);
                    cardState[c.CardId].Unlocked          = c.Unlocked;
                    cardState[c.CardId].BreakthroughCount = c.BreakthroughCount;
                    cardState[c.CardId].SelectedStage     = c.SelectedStage > 0 ? c.SelectedStage : 1;
                }
            if (data.Upgrades != null)
                foreach (var u in data.Upgrades)
                {
                    if (u == null || string.IsNullOrEmpty(u.UpgradeId)) continue;
                    upgradeState[u.UpgradeId] = new UpgradeProgress { UpgradeId = u.UpgradeId, Level = u.Level };
                }
            if (data.UnlockedHiddenCards != null) unlockedHiddenCards.UnionWith(data.UnlockedHiddenCards);
            if (data.CompletedSets != null)       completedSets.UnionWith(data.CompletedSets);
            if (data.ClaimedSetRewards != null)   claimedSetRewards.UnionWith(data.ClaimedSetRewards);
            if (data.UnlockedBackgrounds != null) unlockedBackgrounds.UnionWith(data.UnlockedBackgrounds);
            if (data.UnlockedDecorations != null) unlockedDecorations.UnionWith(data.UnlockedDecorations);

            BgmVolume = data.BgmVolume;
            SfxVolume = data.SfxVolume;
            IsMuted = data.IsMuted;
            SelectedLanguage = string.IsNullOrEmpty(data.SelectedLanguage) ? "KR" : data.SelectedLanguage;
            NotifyState();
        }

        [ContextMenu("Reset To Default Card Only")]
        public void ResetToDefaultCardOnly()
        {
            if (cardCatalog == null || cardCatalog.Cards == null) return;
            string defaultCardId = GetDefaultFirstCardId();
            foreach (var card in cardCatalog.Cards)
            {
                if (card == null) continue;
                if (!cardState.TryGetValue(card.Id, out var state))
                {
                    state = new CardProgress { CardId = card.Id };
                    cardState[card.Id] = state;
                }

                if (card.Id == defaultCardId || card.Id == "c001" || card.Id == cardCatalog.Cards[0].Id)
                {
                    state.Unlocked = true;
                    state.Copies = 1;
                    state.BreakthroughCount = 0;
                    state.SelectedStage = 1;
                }
                else
                {
                    state.Unlocked = false;
                    state.Copies = 0;
                    state.BreakthroughCount = 0;
                    state.SelectedStage = 1;
                }
            }

            completedSets.Clear();
            claimedSetRewards.Clear();
            unlockedBackgrounds.Clear();
            unlockedBackgrounds.Add("bg");
            unlockedDecorations.Clear();

            EquippedCardId = defaultCardId;
            EquippedBackgroundId = "bg";
            EquippedDecorationId = "deco-none";

            RebuildSetState();
            Save();
            NotifyState();
        }

        public void EnsureTestCardsFirst12()
        {
            // Do not wipe user save data on launch
        }

        private void SpawnComboRewardFloatingText(double amount)
        {
            Vector2 screenPos = Vector2.zero;
            bool foundPos = false;

            var hud = FindObjectOfType<GameHud>(true);
            Transform comboTransform = null;
            if (hud != null)
            {
                var comboComp = hud.GetComboTextComponent();
                if (comboComp != null) comboTransform = comboComp.transform;
            }

            if (comboTransform == null)
            {
                var found = GameObject.Find("ComboText") ?? GameObject.Find("Text_Combo") ?? GameObject.Find("Combo_Text") ?? GameObject.Find("Combo");
                if (found != null) comboTransform = found.transform;
            }

            if (comboTransform != null)
            {
                Canvas canvas = comboTransform.GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
                screenPos = RectTransformUtility.WorldToScreenPoint(cam, comboTransform.position);
                foundPos = true;
            }

            if (!foundPos)
            {
                screenPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            }

            SpawnFloatingText(screenPos, amount, isCrit: true);
        }

        private void SpawnFloatingText(Vector2 screenPos, double amount, bool isCrit)
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("FloatingText", typeof(RectTransform));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out Vector2 localPos
            );

            // Add slight random offset to prevent stacked overlaps on fast clicks
            float randomX = UnityEngine.Random.Range(-25f, 25f);
            float randomY = UnityEngine.Random.Range(-15f, 15f);
            rt.anchoredPosition = localPos + new Vector2(randomX, randomY);

            var textComp = go.AddComponent<TMPro.TextMeshProUGUI>();
            textComp.raycastTarget = false;
            textComp.text = $"+{amount:F0}";

            // Cache font component search to prevent runtime serialization stuttering
            if (cachedFont == null)
            {
                var anyText = FindObjectOfType<TMPro.TextMeshProUGUI>(true);
                if (anyText != null) cachedFont = anyText.font;
            }
            if (cachedFont != null) textComp.font = cachedFont;

            textComp.alignment = TMPro.TextAlignmentOptions.Center;

            if (isCrit)
            {
                textComp.color = Color.red;
                textComp.fontSize = 44;
                textComp.fontStyle = TMPro.FontStyles.Bold;
            }
            else
            {
                textComp.color = new Color(1f, 0.85f, 0f);
                textComp.fontSize = 40;
                textComp.fontStyle = TMPro.FontStyles.Bold;
            }

            textComp.outlineWidth = 0.25f;
            textComp.outlineColor = Color.black;

            StartCoroutine(AnimateFloatingText(go, textComp, rt));
        }

        private System.Collections.IEnumerator AnimateFloatingText(GameObject obj, TMPro.TextMeshProUGUI text, RectTransform rt)
        {
            float duration = 0.6f;
            float elapsed = 0f;
            Vector2 startPos = rt.anchoredPosition;
            Color baseColor = text.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                rt.anchoredPosition = startPos + new Vector2(0f, t * 70f);
                text.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);

                yield return null;
            }

            Destroy(obj);
        }
    }
}
