using System.Collections.Generic;
using System.Linq;
using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>Consumable - одноразовый предмет, используется в бою из панели предметов. Passive - реликвия (RelicSystem).</summary>
public enum ItemKind { Consumable, Passive }

/// <summary>Что делает расходник (логика - ItemSystem, Scrpits/Combat).</summary>
public enum ItemEffect
{
    None,
    Heal,        // +value HP игроку
    Shield,      // +value щита игроку
    Mana,        // +value маны в этом ходу
    Draw,        // добрать value карт
    BossDamage,  // value урона боссу
    Bomb,        // value урона всем вражеским картам
    Poison,      // value яда боссу + 2 яда каждой вражеской карте
    Rally,       // +value ATK всем твоим картам (рука и поле)
    Cleanse,     // снять яд/кровотечение с тебя и твоих карт, +value HP
    FullHeal,    // полное лечение + value щита
}

/// <summary>
/// Предмет магазина / кейса / награды. Иконка рисуется процедурно (<see cref="ItemIcons"/>) по <see cref="icon"/>.
/// </summary>
public class ItemDef
{
    public string id;
    public string name;
    public string description;
    public CardRarity rarity;
    public ItemKind kind;
    public int price;
    public string icon;
    public Color color;
    public ItemEffect effect;
    public int value;

    public bool IsPassive => kind == ItemKind.Passive;
    public string KindLabel => kind == ItemKind.Passive ? "Passive" : "Consumable";

    public ItemDef(string id, string name, string description, CardRarity rarity, ItemKind kind, int price, string icon, Color color,
        ItemEffect effect = ItemEffect.None, int value = 0)
    {
        this.id = id; this.name = name; this.description = description; this.rarity = rarity; this.kind = kind;
        this.price = price; this.icon = icon; this.color = color; this.effect = effect; this.value = value;
    }
}

/// <summary>
/// Каталог предметов: расходники (здесь) + пассивки (= реликвии <see cref="RelicDatabase"/>, эффекты в RelicSystem).
/// Инвентарь расходников - <see cref="RunState.Items"/>, пассивки - <see cref="RunState.Relics"/>.
/// Eval: ItemDatabase.Describe()
/// </summary>
public static class ItemDatabase
{
    public static readonly List<ItemDef> Consumables = new List<ItemDef>
    {
        new ItemDef("heal_potion", "Health Potion", "Heal 8 HP.", CardRarity.Common, ItemKind.Consumable, 25, ItemIcons.Potion, new Color32(0xE8, 0x3A, 0x4A, 0xFF), ItemEffect.Heal, 8),
        new ItemDef("iron_skin", "Iron Skin", "Gain 6 Shield.", CardRarity.Common, ItemKind.Consumable, 25, ItemIcons.Shield, new Color32(0x7F, 0xB8, 0xE8, 0xFF), ItemEffect.Shield, 6),
        new ItemDef("insight_scroll", "Scroll of Insight", "Draw 2 cards.", CardRarity.Common, ItemKind.Consumable, 30, ItemIcons.Scroll, new Color32(0xE8, 0xD2, 0x9A, 0xFF), ItemEffect.Draw, 2),
        new ItemDef("holy_water", "Holy Water", "Remove Poison and Bleed from you and your cards. Heal 3 HP.", CardRarity.Common, ItemKind.Consumable, 20, ItemIcons.Drop, new Color32(0x9A, 0xE6, 0xFF, 0xFF), ItemEffect.Cleanse, 3),
        new ItemDef("mana_crystal", "Mana Crystal", "+2 mana this turn.", CardRarity.Rare, ItemKind.Consumable, 40, ItemIcons.Crystal, new Color32(0x4D, 0xA6, 0xFF, 0xFF), ItemEffect.Mana, 2),
        new ItemDef("throwing_dagger", "Throwing Dagger", "Deal 8 damage to the boss.", CardRarity.Rare, ItemKind.Consumable, 40, ItemIcons.Dagger, new Color32(0xD8, 0xDE, 0xE8, 0xFF), ItemEffect.BossDamage, 8),
        new ItemDef("powder_bomb", "Powder Bomb", "Deal 3 damage to every enemy card.", CardRarity.Rare, ItemKind.Consumable, 50, ItemIcons.Bomb, new Color32(0x5A, 0x52, 0x66, 0xFF), ItemEffect.Bomb, 3),
        new ItemDef("venom_flask", "Venom Flask", "Apply 5 Poison to the boss and 2 Poison to every enemy card.", CardRarity.Rare, ItemKind.Consumable, 45, ItemIcons.Flask, new Color32(0x8C, 0xE0, 0x3C, 0xFF), ItemEffect.Poison, 5),
        new ItemDef("battle_cry", "Battle Cry", "All your cards (hand and board) get +2 ATK.", CardRarity.Epic, ItemKind.Consumable, 70, ItemIcons.Star, new Color32(0xFF, 0x8A, 0x3D, 0xFF), ItemEffect.Rally, 2),
        new ItemDef("phoenix_elixir", "Phoenix Elixir", "Heal to full HP and gain 5 Shield.", CardRarity.Legendary, ItemKind.Consumable, 120, ItemIcons.Heart, new Color32(0xFF, 0xB3, 0x1F, 0xFF), ItemEffect.FullHeal, 5),
    };

    private static List<ItemDef> passives;

    /// <summary>Пассивки = реликвии (цена по редкости).</summary>
    public static List<ItemDef> Passives
    {
        get
        {
            if (passives == null)
            {
                passives = RelicDatabase.All.Select(r => new ItemDef(r.id, r.name, r.description, r.rarity, ItemKind.Passive,
                    PassivePrice(r.rarity), RelicSystem.IconOf(r.id), RelicSystem.ColorOf(r.id))).ToList();
            }
            return passives;
        }
    }

    public static IEnumerable<ItemDef> All => Consumables.Concat(Passives);

    public static ItemDef Get(string id) => All.FirstOrDefault(i => i.id == id);

    public static int PassivePrice(CardRarity r) => r switch
    {
        CardRarity.Rare => 100,
        CardRarity.Epic => 150,
        CardRarity.Legendary => 220,
        _ => 60,
    };

    public static bool Owns(ItemDef item) => item != null && (item.IsPassive ? RunState.Relics.Contains(item.id) : RunState.ItemCount(item.id) > 0);

    /// <summary>Выдать предмет: расходник в инвентарь, пассивку - как реликвию. false - пассивка уже есть.</summary>
    public static bool Grant(ItemDef item)
    {
        if (item == null) return false;
        if (item.IsPassive) return RelicDatabase.Grant(item.id);
        RunState.AddItem(item.id);
        return true;
    }

    /// <summary>
    /// Случайный предмет нужной редкости: расходники этой редкости + ещё не собранные пассивки
    /// (<paramref name="passiveWeight"/> - шанс, что выпадет именно пассивка, если она доступна).
    /// Если на этой редкости ничего нет - редкость ниже.
    /// </summary>
    public static ItemDef Roll(CardRarity rarity, float passiveWeight = 0.35f)
    {
        for (int r = (int)rarity; r >= 0; r--)
        {
            var cons = Consumables.Where(i => i.rarity == (CardRarity)r).ToList();
            var pas = Passives.Where(i => i.rarity == (CardRarity)r && !RunState.Relics.Contains(i.id)).ToList();
            if (cons.Count == 0 && pas.Count == 0) continue;
            bool pickPassive = pas.Count > 0 && (cons.Count == 0 || Random.value < passiveWeight);
            var pool = pickPassive ? pas : cons;
            return pool[Random.Range(0, pool.Count)];
        }
        return Consumables[Random.Range(0, Consumables.Count)];
    }

    /// <summary>Случайный расходник (награда за бой).</summary>
    public static ItemDef RollConsumable(CardRarity maxRarity)
    {
        var pool = Consumables.Where(i => i.rarity <= maxRarity).ToList();
        // чем реже - тем меньше шанс: вес 6/3/2/1
        float total = pool.Sum(Weight);
        float x = Random.value * total;
        foreach (var i in pool)
        {
            x -= Weight(i);
            if (x <= 0f) return i;
        }
        return pool[pool.Count - 1];
    }

    private static float Weight(ItemDef i) => i.rarity switch
    {
        CardRarity.Rare => 3f,
        CardRarity.Epic => 2f,
        CardRarity.Legendary => 1f,
        _ => 6f,
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => passives = null;

    public static string Describe() =>
        string.Join(", ", All.Select(i => $"{i.id}[{i.kind},{i.rarity},{i.price}c]")) +
        $" | inventory: {string.Join(",", RunState.Items.Select(kv => kv.Key + "x" + kv.Value))}";
}
