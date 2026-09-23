using System.Collections.Generic;
using System.Linq;
using Assets.Scrpits.Run;

/// <summary>
/// Описание реликвии (пассивки). Эффект реализует RelicSystem (Subagent D, Scrpits/Combat).
/// </summary>
public class RelicDef
{
    public string id;
    public string name;
    public string description;
    public CardRarity rarity;

    public RelicDef(string id, string name, string description, CardRarity rarity)
    {
        this.id = id; this.name = name; this.description = description; this.rarity = rarity;
    }
}

/// <summary>
/// Каталог реликвий. Контракт: карта/магазин/награды вызывают только All, Get, Random, Grant.
/// Subagent D наполняет список и реализует эффекты.
/// </summary>
public static class RelicDatabase
{
    public static readonly List<RelicDef> All = new List<RelicDef>
    {
        // [D] эффекты - RelicSystem (Scrpits/Combat)
        new RelicDef(RelicSystem.Espresso, "Espresso Shot", "+1 max mana every round.", CardRarity.Epic),
        new RelicDef(RelicSystem.HardHat, "Hard Hat", "Start each battle with 4 Shield.", CardRarity.Common),
        new RelicDef(RelicSystem.Whetstone, "Whetstone", "The first card you play each round gets +2 ATK.", CardRarity.Rare),
        new RelicDef(RelicSystem.FirstAid, "First-Aid Kit", "Heal 6 HP after every won battle.", CardRarity.Common),
        new RelicDef(RelicSystem.PiggyBank, "Piggy Bank", "+10 coins after every won battle.", CardRarity.Common),
        new RelicDef(RelicSystem.VenomVial, "Venom Vial", "Your cards apply 1 Poison on hit.", CardRarity.Rare),
    };

    public static RelicDef Get(string id) => All.FirstOrDefault(r => r.id == id);

    /// <summary>Случайная реликвия, которой ещё нет в забеге (null если все собраны).</summary>
    public static RelicDef RandomNotOwned()
    {
        var pool = All.Where(r => !RunState.Relics.Contains(r.id)).ToList();
        return pool.Count == 0 ? null : pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    public static bool Grant(string id)
    {
        if (Get(id) == null || RunState.Relics.Contains(id)) return false;
        RunState.Relics.Add(id);
        UnityEngine.Debug.Log($"[RELIC] Granted {id}");
        return true;
    }
}

/// <summary>
/// [D] Награда за бой. Оверлей Victory вызывает Grant() один раз и показывает Last*.
/// База по типу узла (Battle 20 / Elite 35 / Boss 60) + бонусы; элита и босс дают реликвию.
/// </summary>
public static class BattleRewards
{
    public static int LastCoins;
    public static readonly List<(string label, int coins)> LastBreakdown = new List<(string label, int coins)>();
    /// <summary>Реликвия, выданная за этот бой (элита/босс), иначе null.</summary>
    public static RelicDef LastRelic;

    private static bool granted;

    /// <summary>Новый бой (Encounters.Setup): разрешает один Grant.</summary>
    public static void BeginBattle() => granted = false;

    /// <summary>Посчитать и начислить награду. Вызывается один раз при победе (повторный вызов ничего не делает).</summary>
    public static void Grant()
    {
        if (granted) return;
        granted = true;
        LastBreakdown.Clear();
        LastRelic = null;

        var type = Encounters.Type;
        switch (type)
        {
            case Assets.Scrpits.Map.MapNodeType.Elite: LastBreakdown.Add(("Elite defeated", 35)); break;
            case Assets.Scrpits.Map.MapNodeType.Boss: LastBreakdown.Add(("Boss defeated", 60)); break;
            default: LastBreakdown.Add(("Battle won", 20)); break;
        }
        if (CombatRules.DamageTaken == 0) LastBreakdown.Add(("No damage taken", 10));
        if (CombatRules.Round <= 3) LastBreakdown.Add(("Quick victory", 5));
        RelicSystem.OnBattleWon(LastBreakdown);

        LastCoins = 0;
        foreach (var (_, c) in LastBreakdown) LastCoins += c;
        Wallet.Add(LastCoins);
        RunState.CoinsEarned += LastCoins;

        if (type != Assets.Scrpits.Map.MapNodeType.Battle && RunState.IsActive)
        {
            LastRelic = RelicDatabase.RandomNotOwned();
            if (LastRelic != null) RelicDatabase.Grant(LastRelic.id);
        }
        UnityEngine.Debug.Log($"[D] Rewards {type}: +{LastCoins} ({string.Join(", ", LastBreakdown.Select(b => b.label + " " + b.coins))}) relic={LastRelic?.id}");
    }
}
