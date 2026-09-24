using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scrpits.Run
{
    /// <summary>Снимок забега для сохранения (JsonUtility).</summary>
    [Serializable]
    public class RunSaveData
    {
        public int version = 1;
        public int currentNodeId = -1;
        public List<int> completed = new List<int>();
        public int act = 1;
        public int mapSeed;
        public bool pendingActAdvance;
        public bool needsDeckChoice;
        public string starterDeckId;
        public int nodesCleared, elitesKilled, bossesKilled;
        public int playerHp = -1, playerHpMax = -1;
        public List<string> deck = new List<string>();
        /// <summary>Параллельно deck: карта прокачана до Veteran.</summary>
        public List<bool> deckVeteran = new List<bool>();
        public List<string> relics = new List<string>();
        public List<string> itemIds = new List<string>();
        public List<int> itemCounts = new List<int>();
        public int coinsEarned;
    }

    /// <summary>
    /// Сохранение забега в PlayerPrefs: пишется при входе в узел, завершении узла, изменении колоды/предметов,
    /// на карте и при сворачивании приложения. Загружается при запуске игры - START в меню продолжает забег.
    /// Бой не сохраняется посередине: продолжение начинает бой узла заново (HP - как при входе).
    /// Eval: Assets.Scrpits.Run.RunSave.Describe()
    /// </summary>
    public static class RunSave
    {
        private const string Key = "RunSaveV1";
        private static bool loading;

        public static bool HasSave => PlayerPrefs.HasKey(Key);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // до Awake карты, иначе она начнёт новый забег
        private static void Init()
        {
            loading = false;
            TryLoad();
            // сворачивание / закрытие на телефоне - сохранить
            var go = new GameObject("[RunSave]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideInHierarchy;
            go.AddComponent<RunSaveHook>();
        }

        public static void Save()
        {
            if (loading) return;
            try
            {
                if (!RunState.IsActive || RunState.Outcome != RunOutcome.None) { Clear(); return; }
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(RunState.Export()));
                PlayerPrefs.Save();
            }
            catch (Exception e) { Debug.LogWarning("[SAVE] Run save failed: " + e.Message); }
        }

        public static void Clear()
        {
            if (!PlayerPrefs.HasKey(Key)) return;
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        /// <summary>Поднять сохранённый забег, если сейчас забега нет. true - забег загружен.</summary>
        public static bool TryLoad()
        {
            if (RunState.IsActive || !HasSave) return false;
            try
            {
                var data = JsonUtility.FromJson<RunSaveData>(PlayerPrefs.GetString(Key));
                if (data == null || data.deck == null || data.deck.Count == 0) { Clear(); return false; }
                loading = true;
                RunState.Import(data);
                Debug.Log($"[SAVE] Run loaded: act {data.act}, node {data.currentNodeId}, deck {data.deck.Count}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SAVE] Broken run save, discarded: " + e.Message);
                Clear();
                return false;
            }
            finally { loading = false; }
        }

        public static string Describe() => HasSave ? PlayerPrefs.GetString(Key) : "no save";
    }

    /// <summary>Сохраняет забег при сворачивании/закрытии приложения.</summary>
    public class RunSaveHook : MonoBehaviour
    {
        private void OnApplicationPause(bool paused) { if (paused) RunSave.Save(); }
        private void OnApplicationQuit() => RunSave.Save();
    }
}
