using System.Reflection;
using Assets.Scrpits.Map;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Assets.Scrpits.Run
{
    /// <summary>
    /// Настраивает бой при загрузке EnemyScene: [D] состав боя по типу узла (Encounters: BossData +
    /// враги), HP игрока между боями, реликвии на старте боя. Вне забега - обычный Battle.
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
            if (scene.name != RunState.BattleSceneName) return;
            Time.timeScale = 1f;
            var inRun = RunState.IsActive && RunState.IsCurrentNodeBattle;
            Encounters.Setup(inRun ? RunState.CurrentNodeType : MapNodeType.Battle);
            if (inRun) ApplyPlayerHp();
            RelicSystem.OnBattleStart();
            BattleSceneDresser.Install(); // [V] HUD / фон / босс / панель предметов (кодом, сцену не трогаем)
        }

        public static void ApplyPlayerHp()
        {
            var player = Object.FindFirstObjectByType<Player>();
            if (player == null) return;
            var data = typeof(Player).GetField("playerData", Flags)?.GetValue(player) as PlayerData;
            if (data == null || !player.gameObject.activeInHierarchy) return;

            // [Items] бонус к макс. HP от реликвий (Heart Amulet); data - свежий клон ассета на каждый бой
            data.playerHPMAX += RelicSystem.BonusMaxHp;
            data.playerHP = RunState.PlayerHP < 0
                ? Mathf.Min(data.playerHP, data.playerHPMAX)
                : Mathf.Clamp(RunState.PlayerHP, 1, data.playerHPMAX);
            RunState.PlayerHP = data.playerHP;
            RunState.PlayerHPMax = data.playerHPMAX;
            player.UpdatePlayerDisplay();
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
