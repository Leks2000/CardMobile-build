using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scrpits.Map;
using TMPro;
using UnityEngine;

/// <summary>
/// [D] Состав боя по типу узла карты: босс (BossData) + пул вражеских карт + их количество.
/// Вызывается из RunBattleSetup при загрузке EnemyScene (вне забега - как обычный Battle).
/// </summary>
public static class Encounters
{
    public class Def
    {
        public string bossAsset;      // Resources/ScriptableObjects/<bossAsset>
        public string[] enemies;      // id вражеских CardData (повторы = чаще выпадает)
        public int enemyCount;        // всего врагов за бой
        public int openingWave;       // сколько врагов уже стоит на старте
    }

    private static readonly Dictionary<MapNodeType, Def> Defs = new Dictionary<MapNodeType, Def>
    {
        [MapNodeType.Battle] = new Def { bossAsset = "BossGuard", enemies = new[] { "RatCard", "RatCard", "MoldSlime", "StaplerBat", "PaperArcher" }, enemyCount = 6, openingWave = 1 },
        [MapNodeType.Elite] = new Def { bossAsset = "EliteEnemy", enemies = new[] { "RatCard", "MoldSlime", "StaplerBat", "FilingGolem", "PaperArcher" }, enemyCount = 8, openingWave = 2 },
        [MapNodeType.Boss] = new Def { bossAsset = "Boss", enemies = new[] { "MoldSlime", "StaplerBat", "FilingGolem", "FilingGolem", "RatCard", "PaperArcher", "PaperArcher" }, enemyCount = 10, openingWave = 2 },
    };

    public static MapNodeType Type { get; private set; } = MapNodeType.Battle;
    public static Def Current { get; private set; }
    /// <summary>Клон BossData текущего боя (тот же объект, что у Boss).</summary>
    public static BossData Boss { get; private set; }
    public static int BossAttack => Boss != null ? Mathf.RoundToInt(Boss.attackPower) : 0;

    public static void Setup(MapNodeType type)
    {
        Type = Defs.ContainsKey(type) ? type : MapNodeType.Battle;
        Current = Defs[Type];
        CombatRules.ResetBattle();
        BattleRewards.BeginBattle();

        SetupBoss();
        var spawner = Object.FindAnyObjectByType<EnemySpawnCardLogic>();
        if (spawner != null)
        {
            var pool = Current.enemies.Select(CardDatabase.Get).Where(c => c != null).ToList();
            spawner.Configure(pool, Current.enemyCount);
            spawner.SpawnWave(Current.openingWave);
        }
        Debug.Log($"[D] Encounter {Type}: boss={Boss?.displayName} hp={Boss?.bossHP} atk={BossAttack} enemies={Current.enemyCount} [{string.Join(",", Current.enemies)}]");
    }

    private static void SetupBoss()
    {
        var boss = Object.FindAnyObjectByType<Boss>();
        var asset = Resources.Load<BossData>("ScriptableObjects/" + Current.bossAsset);
        if (boss == null || asset == null)
        {
            Debug.LogWarning($"[D] Boss or BossData '{Current.bossAsset}' missing");
            return;
        }
        Boss = asset.Clone();
        if (Boss.maxHP <= 0) Boss.maxHP = Boss.bossHP;
        Boss.bossHP = Boss.maxHP;
        typeof(Boss).GetField("bossData", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(boss, Boss);
        boss.UpdateCardDisplay();
        boss.SendMessage("OnBossDataChanged", SendMessageOptions.DontRequireReceiver); // [V] может перерисовать имя/тинт
        ShowIntent(boss);
    }

    /// <summary>Телеграф атаки босса: подпись под HP босса.</summary>
    private static void ShowIntent(Boss boss)
    {
        var hp = boss.transform.Find("BossHP");
        var parent = hp != null ? hp : boss.transform;
        var existing = parent.Find("D_BossIntent");
        TMP_Text text;
        if (existing != null)
        {
            text = existing.GetComponent<TMP_Text>();
        }
        else
        {
            var go = new GameObject("D_BossIntent", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(80f, 26f);
            rt.anchoredPosition = new Vector2(0f, -2f);
            text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = 20;
            text.raycastTarget = false;
            UiTheme.Apply(text, UiTheme.Damage);
        }
        text.text = BossAttack > 0 ? $"{Boss.displayName}: hits you for {BossAttack} each round" : Boss.displayName;
    }
}
