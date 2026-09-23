using System.Reflection;
using Assets.Scrpits.Map;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scrpits.Run
{
    /// <summary>
    /// Настраивает бой при загрузке EnemyScene в рамках забега:
    /// масштабирует HP босса по типу узла и переносит HP игрока между боями.
    /// Работает через рефлексию, чтобы не трогать Boss.cs / Player.cs.
    /// </summary>
    public static class RunBattleSetup
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != RunState.BattleSceneName || !RunState.IsActive || !RunState.IsCurrentNodeBattle) return;
            Time.timeScale = 1f;
            ApplyBossHp();
            ApplyPlayerHp();
        }

        public static void ApplyBossHp()
        {
            var boss = Object.FindFirstObjectByType<Boss>();
            if (boss == null) { Debug.LogWarning("[RUN] Boss not found in battle scene"); return; }

            var data = typeof(Boss).GetField("bossData", Flags)?.GetValue(boss) as BossData;
            // Awake клонирует данные; если Awake не отработал - это ассет, его не трогаем.
            if (data == null || !boss.gameObject.activeInHierarchy) { Debug.LogWarning("[RUN] Boss data unavailable, HP not scaled"); return; }

            float mult = RunState.CurrentNodeType switch
            {
                MapNodeType.Elite => RunState.EliteHpMultiplier,
                MapNodeType.Boss => RunState.BossHpMultiplier,
                _ => RunState.BattleHpMultiplier,
            };
            float baseHp = data.bossHP;
            data.bossHP = Mathf.Max(1f, Mathf.Round(baseHp * mult));
            boss.UpdateCardDisplay();
            Debug.Log($"[RUN] Battle '{RunState.CurrentNodeType}' boss HP {baseHp} x{mult} -> {data.bossHP}");
        }

        public static void ApplyPlayerHp()
        {
            var player = Object.FindFirstObjectByType<Player>();
            if (player == null) return;
            var data = typeof(Player).GetField("playerData", Flags)?.GetValue(player) as PlayerData;
            if (data == null || !player.gameObject.activeInHierarchy) return;

            if (RunState.PlayerHP < 0)
            {
                RunState.PlayerHP = data.playerHP;
                RunState.PlayerHPMax = data.playerHPMAX;
            }
            else
            {
                data.playerHP = Mathf.Clamp(RunState.PlayerHP, 1, data.playerHPMAX);
                RunState.PlayerHPMax = data.playerHPMAX;
                player.UpdatePlayerDisplay();
            }
        }

        /// <summary>Сохранить HP игрока после победы.</summary>
        public static void StorePlayerHp()
        {
            var player = Object.FindFirstObjectByType<Player>();
            if (player == null) return;
            var data = typeof(Player).GetField("playerData", Flags)?.GetValue(player) as PlayerData;
            if (data == null) return;
            RunState.PlayerHP = Mathf.Max(1, data.playerHP);
            RunState.PlayerHPMax = data.playerHPMAX;
        }
    }
}
