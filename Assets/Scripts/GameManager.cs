using System;
using System.Collections.Generic;
using UnityEngine;

namespace CosmicChaosCat
{
    public sealed class GameManager : MonoBehaviour
    {
        private const string SaveKey = "cosmic_chaos_cat_save_v2";
        private const double BaseClickIncome = 1d;
        private const double BaseGachaCost = 10d;
        private const float ComboWindowSeconds = 0.8f;

        private readonly GachaService gachaService = new GachaService();
        private readonly Dictionary<string, CardProgress> cardStateById = new Dictionary<string, CardProgress>();

        [SerializeField] private float criticalChance = 0.1f;
        [SerializeField] private float criticalMultiplier = 3f;

        private List<CardDefinition> cardDefinitions = new List<CardDefinition>();
        private float lastClickTime = -999f;
        private int comboCount;

        public event Action StateChanged;
        public event Action<string> LogUpdated;

        public double Money { get; private set; }
        public int Shards { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public int TotalRolls { get; private set; }
        public bool IsGameEnded { get; private set; }
        public string EquippedCardId { get; private set; }
        public int ComboCount => comboCount;

        public float Completion01
        {
            get
            {
                if (cardDefinitions.Count == 0)
                {
                    return 0f;
                }

                var unlocked = 0;
                for (var i = 0; i < cardDefinitions.Count; i++)
                {
                    var card = cardDefinitions[i];
                    if (cardStateById.TryGetValue(card.Id, out var state) && state.Unlocked)
                    {
                        unlocked++;
                    }
                }

                return unlocked / (float)cardDefinitions.Count;
            }
        }

        public int UnlockedCards
        {
            get { return Mathf.RoundToInt(Completion01 * cardDefinitions.Count); }
        }

        public int TotalCards
        {
            get { return cardDefinitions.Count; }
        }

        private void Awake()
        {
            cardDefinitions = DefaultCardCatalog.Create();
            InitializeCardState();
            Load();
            NotifyState();
        }

        private void Update()
        {
            if (IsGameEnded)
            {
                return;
            }

            ElapsedSeconds += Time.unscaledDeltaTime;
            NotifyState();
        }

        public void HandleCardClicked()
        {
            if (IsGameEnded)
            {
                return;
            }

            if (Time.unscaledTime - lastClickTime <= ComboWindowSeconds)
            {
                comboCount++;
            }
            else
            {
                comboCount = 1;
            }

            lastClickTime = Time.unscaledTime;

            var comboMultiplier = 1d + Math.Log(comboCount + 1d);
            var critBonus = UnityEngine.Random.value <= criticalChance ? criticalMultiplier : 1f;
            var income = BaseClickIncome * GetEquippedIncomeMultiplier() * comboMultiplier * critBonus;

            Money += income;
            Save();
            NotifyState();
        }

        public void RollOnce()
        {
            if (IsGameEnded)
            {
                return;
            }

            var cost = GetCurrentGachaCost();
            if (Money < cost)
            {
                PublishLog("돈이 부족해요.");
                return;
            }

            var card = gachaService.DrawCard(cardDefinitions, cardStateById, Completion01);
            if (card == null)
            {
                PublishLog("가챠 대상 카드가 없어요.");
                return;
            }

            Money -= cost;
            TotalRolls++;
            ApplyDraw(card);
            CheckEnding();
            Save();
            NotifyState();
        }

        public string GetTimerText()
        {
            var time = TimeSpan.FromSeconds(ElapsedSeconds);
            return $"{time.Hours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        public string GetMoneyText()
        {
            return $"돈 {Money:0} | 조각 {Shards}";
        }

        public string GetProgressText()
        {
            return $"도감 {UnlockedCards}/{TotalCards} ({Completion01 * 100f:0.0}%)";
        }

        public string GetEquippedCardText()
        {
            if (string.IsNullOrEmpty(EquippedCardId))
            {
                return "기본 카드";
            }

            var card = FindCard(EquippedCardId);
            if (card == null || !cardStateById.TryGetValue(EquippedCardId, out var state))
            {
                return "기본 카드";
            }

            return $"{card.Name} x{Mathf.Max(state.Copies, 1)}";
        }

        public double GetCurrentGachaCost()
        {
            var stage = Mathf.FloorToInt(Completion01 * 20f);
            var scaled = BaseGachaCost * Math.Pow(1.15d, stage);
            var milestone = 0d;

            if (Completion01 >= 0.1f)
            {
                milestone += 5d;
            }

            if (Completion01 >= 0.3f)
            {
                milestone += 20d;
            }

            if (Completion01 >= 0.6f)
            {
                milestone += 100d;
            }

            return Math.Round(scaled + milestone, 0, MidpointRounding.AwayFromZero);
        }

        private void InitializeCardState()
        {
            cardStateById.Clear();
            for (var i = 0; i < cardDefinitions.Count; i++)
            {
                var card = cardDefinitions[i];
                cardStateById[card.Id] = new CardProgress
                {
                    CardId = card.Id,
                    Copies = 0,
                    Unlocked = false
                };
            }
        }

        private void ApplyDraw(CardDefinition card)
        {
            var state = cardStateById[card.Id];
            state.Unlocked = true;
            state.Copies++;

            if (state.Copies > card.MaxStacks)
            {
                state.Copies = card.MaxStacks;
                var shards = Mathf.RoundToInt(card.ShardValue * 1.5f);
                Shards += shards;
                PublishLog($"{card.Name} 초과 중복! 카드 조각 +{shards}");
            }
            else if (state.Copies == 1)
            {
                PublishLog($"신규 카드 획득: {card.Name} [{card.Rarity}]");
            }
            else
            {
                PublishLog($"중복 강화: {card.Name} x{state.Copies}");
            }

            EquippedCardId = card.Id;
        }

        private double GetEquippedIncomeMultiplier()
        {
            if (string.IsNullOrEmpty(EquippedCardId))
            {
                return 1d;
            }

            if (!cardStateById.TryGetValue(EquippedCardId, out var state))
            {
                return 1d;
            }

            var card = FindCard(EquippedCardId);
            if (card == null)
            {
                return 1d;
            }

            var stack = Mathf.Max(1, state.Copies);
            var enhancement = 1d + Math.Pow(stack, 1.3d);
            return card.ClickMultiplier * enhancement;
        }

        private CardDefinition FindCard(string cardId)
        {
            for (var i = 0; i < cardDefinitions.Count; i++)
            {
                if (cardDefinitions[i].Id == cardId)
                {
                    return cardDefinitions[i];
                }
            }

            return null;
        }

        private void CheckEnding()
        {
            if (Completion01 < 1f || IsGameEnded)
            {
                return;
            }

            IsGameEnded = true;
            PublishLog($"도감 100% 달성! 최종 플레이 타임 {GetTimerText()}");
        }

        private void PublishLog(string message)
        {
            LogUpdated?.Invoke(message);
        }

        private void NotifyState()
        {
            StateChanged?.Invoke();
        }

        private void Save()
        {
            var saveData = new GameSaveData
            {
                Money = Money,
                Shards = Shards,
                ElapsedSeconds = ElapsedSeconds,
                EquippedCardId = EquippedCardId,
                TotalRolls = TotalRolls
            };

            foreach (var entry in cardStateById)
            {
                saveData.Cards.Add(new CardProgress
                {
                    CardId = entry.Value.CardId,
                    Copies = entry.Value.Copies,
                    Unlocked = entry.Value.Unlocked
                });
            }

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
        }

        private void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey))
            {
                return;
            }

            var raw = PlayerPrefs.GetString(SaveKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            var saveData = JsonUtility.FromJson<GameSaveData>(raw);
            if (saveData == null)
            {
                return;
            }

            Money = Math.Max(0d, saveData.Money);
            Shards = Math.Max(0, saveData.Shards);
            ElapsedSeconds = Math.Max(0f, saveData.ElapsedSeconds);
            EquippedCardId = saveData.EquippedCardId;
            TotalRolls = Math.Max(0, saveData.TotalRolls);

            if (saveData.Cards == null)
            {
                return;
            }

            for (var i = 0; i < saveData.Cards.Count; i++)
            {
                var saved = saveData.Cards[i];
                if (saved == null || string.IsNullOrEmpty(saved.CardId) || !cardStateById.ContainsKey(saved.CardId))
                {
                    continue;
                }

                cardStateById[saved.CardId].Copies = Math.Max(0, saved.Copies);
                cardStateById[saved.CardId].Unlocked = saved.Unlocked;
            }
        }
    }
}
