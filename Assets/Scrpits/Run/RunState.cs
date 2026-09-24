using System.Collections.Generic;
using Assets.Scrpits.Map;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scrpits.Run
{
    public enum RunOutcome { None, Won, Failed }

    /// <summary>
    /// Состояние текущего забега. Статический класс - переживает загрузку сцен.
    /// Eval: Assets.Scrpits.Run.RunState.Describe()
    /// </summary>
    public static class RunState
    {
        public const string MainMenuSceneName = "MainMenuScene";
        public const string MapSceneName = "MapScene";
        public const string BattleSceneName = "EnemyScene";
        public const string ShopSceneName = "CardShopScene";

        public static bool IsActive { get; private set; }
        /// <summary>Узел, в который игрок зашёл последним (-1 = ещё не выбран).</summary>
        public static int CurrentNodeId { get; private set; } = -1;
        public static readonly HashSet<int> CompletedNodes = new HashSet<int>();
        public static RunOutcome Outcome { get; private set; }

        /// <summary>Текущий акт (1..MapGraph.Acts). Карта акта генерируется по MapSeed.</summary>
        public static int Act { get; private set; } = 1;
        public static int MapSeed { get; private set; }
        /// <summary>Босс акта побеждён - карта при возврате переключится на следующий акт.</summary>
        public static bool PendingActAdvance { get; private set; }
        /// <summary>В начале забега нужно выбрать стартовую колоду (если открыто больше одной).</summary>
        public static bool NeedsDeckChoice;
        public static string StarterDeckId = MetaProgress.DefaultDeck;
        // статистика забега (очки мета-прогрессии)
        public static int NodesCleared, ElitesKilled, BossesKilled;
        public static bool RenownAwarded;

        /// <summary>HP игрока между боями (-1 = не задано, берётся из PlayerData).</summary>
        public static int PlayerHP = -1;
        public static int PlayerHPMax = -1;

        /// <summary>Множители HP босса по типу боя (базовое HP берётся из BossData).</summary>
        public static float BattleHpMultiplier = 0.5f;
        public static float EliteHpMultiplier = 0.75f;
        public static float BossHpMultiplier = 1f;

        /// <summary>Колода забега (стартовая + выбитые/купленные карты). Бой добирает из неё.</summary>
        public static readonly List<CardData> Deck = new List<CardData>();
        /// <summary>Id реликвий/пассивок, собранных за забег.</summary>
        public static readonly List<string> Relics = new List<string>();
        /// <summary>Расходники забега: id предмета -> количество (ItemDatabase / ItemSystem).</summary>
        public static readonly Dictionary<string, int> Items = new Dictionary<string, int>();
        /// <summary>Инвентарь расходников изменился (HUD боя / карты).</summary>
        public static event System.Action ItemsChanged;
        /// <summary>Монеты, заработанные за текущий забег (для статистики; сами монеты в Wallet).</summary>
        public static int CoinsEarned;

        /// <summary>[M] Победа в бою -> карта покажет тост с BattleRewards.Last* при возврате.</summary>
        public static bool PendingBattleReward;
        /// <summary>[M] Тосты для карты от небоевых узлов (магазин и т.п.): текст + опционально карта.</summary>
        public static readonly List<(string text, CardData card)> PendingToasts = new List<(string, CardData)>();

        public static void QueueMapToast(string text, CardData card = null) => PendingToasts.Add((text, card));

        public static void AddCard(CardData card)
        {
            if (card != null) Deck.Add(card);
        }

        /// <summary>Сообщить подписчикам об изменении инвентаря; ошибка одного подписчика не ломает забег.</summary>
        private static void NotifyItems()
        {
            if (ItemsChanged == null) return;
            foreach (System.Action h in ItemsChanged.GetInvocationList())
            {
                try { h(); }
                catch (System.Exception e) { Debug.LogWarning("[RUN] ItemsChanged handler failed: " + e.Message); }
            }
        }

        public static int ItemCount(string id) => id != null && Items.TryGetValue(id, out var n) ? n : 0;

        public static int TotalItems
        {
            get
            {
                int n = 0;
                foreach (var kv in Items) n += kv.Value;
                return n;
            }
        }

        public static void AddItem(string id, int count = 1)
        {
            if (string.IsNullOrEmpty(id) || count <= 0) return;
            Items[id] = ItemCount(id) + count;
            NotifyItems();
        }

        /// <summary>Списать один расходник. false - такого нет.</summary>
        public static bool ConsumeItem(string id)
        {
            int n = ItemCount(id);
            if (n <= 0) return false;
            if (n == 1) Items.Remove(id);
            else Items[id] = n - 1;
            NotifyItems();
            return true;
        }

        public static MapNodeType CurrentNodeType =>
            MapGraph.TryGet(CurrentNodeId, out var n) ? n.type : MapNodeType.Battle;

        public static bool IsCurrentNodeBattle => MapGraph.IsBattle(CurrentNodeType) && CurrentNodeId >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ItemsChanged = null;
            Reset();
        }

        public static void StartNewRun()
        {
            Reset();
            IsActive = true;
            Act = 1;
            MapSeed = Random.Range(1, int.MaxValue);
            MapGraph.Generate(Act, MapSeed);
            StarterDeckId = MetaProgress.DefaultDeck;
            Deck.AddRange(MetaProgress.DeckCards(StarterDeckId));
            NeedsDeckChoice = MetaProgress.UnlockedDecks().Count > 1;
            // HP известно с начала забега (для HUD, событий и отдыха)
            var pd = Resources.Load<PlayerData>("ScriptableObjects/Player");
            if (pd != null && pd.playerHPMAX > 0)
            {
                PlayerHPMax = pd.playerHPMAX;
                PlayerHP = pd.playerHPMAX; // забег начинается с полным HP
            }
            Debug.Log($"[RUN] New run started, deck={Deck.Count}");
        }

        public static void Reset()
        {
            IsActive = false;
            CurrentNodeId = -1;
            CompletedNodes.Clear();
            Outcome = RunOutcome.None;
            Act = 1;
            PendingActAdvance = false;
            NeedsDeckChoice = false;
            NodesCleared = ElitesKilled = BossesKilled = 0;
            RenownAwarded = false;
            PlayerHP = -1;
            PlayerHPMax = -1;
            Deck.Clear();
            Relics.Clear();
            Items.Clear();
            NotifyItems();
            CoinsEarned = 0;
            PendingBattleReward = false;
            PendingToasts.Clear();
        }

        public static bool IsCompleted(int id) => CompletedNodes.Contains(id);

        /// <summary>Узлы, которые сейчас можно выбрать.</summary>
        public static List<int> GetAvailableNodes()
        {
            var result = new List<int>();
            if (!IsActive || Outcome != RunOutcome.None) return result;

            if (CurrentNodeId < 0)
            {
                result.AddRange(MapGraph.StartNodes);
            }
            else if (IsCompleted(CurrentNodeId) && MapGraph.TryGet(CurrentNodeId, out var cur))
            {
                result.AddRange(cur.next);
            }
            return result;
        }

        public static bool IsAvailable(int id) => GetAvailableNodes().Contains(id);

        public static void EnterNode(int id)
        {
            CurrentNodeId = id;
            Debug.Log($"[RUN] Enter node {id} ({CurrentNodeType})");
        }

        public static void CompleteCurrentNode()
        {
            if (CurrentNodeId < 0) return;
            if (CompletedNodes.Add(CurrentNodeId)) NodesCleared++;
            if (CurrentNodeType == MapNodeType.Elite) ElitesKilled++;
            if (CurrentNodeType == MapNodeType.Boss)
            {
                BossesKilled++;
                if (Act < MapGraph.Acts) PendingActAdvance = true;
                else Outcome = RunOutcome.Won;
            }
            Debug.Log($"[RUN] Node {CurrentNodeId} completed. Outcome={Outcome}");
        }

        /// <summary>Следующий акт: новая карта (новая локация), фишка в начале, лечение 25%.</summary>
        public static void AdvanceAct()
        {
            if (!PendingActAdvance) return;
            PendingActAdvance = false;
            Act++;
            CompletedNodes.Clear();
            CurrentNodeId = -1;
            MapGraph.Generate(Act, MapSeed);
            if (PlayerHPMax > 0) Heal(Mathf.CeilToInt(PlayerHPMax * 0.25f));
            Debug.Log($"[RUN] Act {Act} begins");
        }

        /// <summary>Сменить стартовую колоду (только до первого узла).</summary>
        public static void ChooseStarterDeck(string deckId)
        {
            NeedsDeckChoice = false;
            if (CurrentNodeId >= 0) return;
            StarterDeckId = deckId;
            Deck.Clear();
            Deck.AddRange(MetaProgress.DeckCards(deckId));
            Debug.Log($"[RUN] Starter deck {deckId}: {Deck.Count} cards");
        }

        /// <summary>Начислить очки славы за забег (один раз).</summary>
        public static void AwardRenown()
        {
            if (RenownAwarded) return;
            RenownAwarded = true;
            MetaProgress.AddPoints(RunScore);
        }

        /// <summary>Очки мета-прогрессии за этот забег.</summary>
        public static int RunScore => NodesCleared * 3 + ElitesKilled * 10 + BossesKilled * 30 + (Outcome == RunOutcome.Won ? 60 : 0);

        /// <summary>Карта на месте (после перезагрузки домена статика MapGraph пустая).</summary>
        public static void EnsureMap()
        {
            if (IsActive) MapGraph.Ensure(Act, MapSeed);
        }

        public static void FailRun()
        {
            Outcome = RunOutcome.Failed;
            AwardRenown();
            Debug.Log("[RUN] Run failed");
        }

        /// <summary>Хилит игрока (для Rest). Возвращает фактическое кол-во HP.</summary>
        public static int Heal(int amount)
        {
            if (PlayerHP < 0 || PlayerHPMax < 0) return 0;
            int before = PlayerHP;
            PlayerHP = Mathf.Min(PlayerHPMax, PlayerHP + amount);
            return PlayerHP - before;
        }

        // ---------- scene flow ----------
        // все переходы - через затемнение (SceneFade), без резких склеек
        public static void LoadMap() => LoadFaded(MapSceneName);
        public static void LoadBattle() => LoadFaded(BattleSceneName);
        public static void LoadShop() => LoadFaded(ShopSceneName);

        public static void LoadFaded(string scene) => SceneFade.Out(0.35f, () => SceneManager.LoadScene(scene));

        /// <summary>Потерять HP вне боя (события). Не опускает ниже 1. Возвращает фактическое кол-во.</summary>
        public static int Damage(int amount)
        {
            if (PlayerHP < 0 || amount <= 0) return 0;
            int before = PlayerHP;
            PlayerHP = Mathf.Max(1, PlayerHP - amount);
            return before - PlayerHP;
        }

        public static void ReturnToMainMenu()
        {
            Reset();
            Time.timeScale = 1f;
            LoadFaded(MainMenuSceneName);
        }

        public static string Describe()
        {
            return $"active={IsActive} current={CurrentNodeId}({CurrentNodeType}) completed=[{string.Join(",", CompletedNodes)}] " +
                   $"available=[{string.Join(",", GetAvailableNodes())}] outcome={Outcome} act={Act} hp={PlayerHP}/{PlayerHPMax} deck={Deck.Count} relics=[{string.Join(",", Relics)}] items=[{string.Join(",", Items.Keys)}] coins={Wallet.Coins} " +
                   $"scene={SceneManager.GetActiveScene().name}";
        }
    }
}
