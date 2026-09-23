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

        public static bool IsActive { get; private set; }
        /// <summary>Узел, в который игрок зашёл последним (-1 = ещё не выбран).</summary>
        public static int CurrentNodeId { get; private set; } = -1;
        public static readonly HashSet<int> CompletedNodes = new HashSet<int>();
        public static RunOutcome Outcome { get; private set; }

        /// <summary>HP игрока между боями (-1 = не задано, берётся из PlayerData).</summary>
        public static int PlayerHP = -1;
        public static int PlayerHPMax = -1;

        /// <summary>Множители HP босса по типу боя (базовое HP берётся из BossData).</summary>
        public static float BattleHpMultiplier = 0.5f;
        public static float EliteHpMultiplier = 0.75f;
        public static float BossHpMultiplier = 1f;

        public static MapNodeType CurrentNodeType =>
            MapGraph.TryGet(CurrentNodeId, out var n) ? n.type : MapNodeType.Battle;

        public static bool IsCurrentNodeBattle => MapGraph.IsBattle(CurrentNodeType) && CurrentNodeId >= 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Reset();

        public static void StartNewRun()
        {
            Reset();
            IsActive = true;
            Debug.Log("[RUN] New run started");
        }

        public static void Reset()
        {
            IsActive = false;
            CurrentNodeId = -1;
            CompletedNodes.Clear();
            Outcome = RunOutcome.None;
            PlayerHP = -1;
            PlayerHPMax = -1;
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
            CompletedNodes.Add(CurrentNodeId);
            if (CurrentNodeType == MapNodeType.Boss)
            {
                Outcome = RunOutcome.Won;
            }
            Debug.Log($"[RUN] Node {CurrentNodeId} completed. Outcome={Outcome}");
        }

        public static void FailRun()
        {
            Outcome = RunOutcome.Failed;
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
        public static void LoadMap() => SceneManager.LoadScene(MapSceneName);
        public static void LoadBattle() => SceneManager.LoadScene(BattleSceneName);

        public static void ReturnToMainMenu()
        {
            Reset();
            Time.timeScale = 1f;
            SceneManager.LoadScene(MainMenuSceneName);
        }

        public static string Describe()
        {
            return $"active={IsActive} current={CurrentNodeId}({CurrentNodeType}) completed=[{string.Join(",", CompletedNodes)}] " +
                   $"available=[{string.Join(",", GetAvailableNodes())}] outcome={Outcome} hp={PlayerHP}/{PlayerHPMax} " +
                   $"scene={SceneManager.GetActiveScene().name}";
        }
    }
}
