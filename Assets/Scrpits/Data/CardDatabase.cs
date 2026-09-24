using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Все CardData проекта. Новые карты кладём в Assets/Resources/CardData/Cards/.
/// Eval: CardDatabase.Describe()
/// </summary>
public static class CardDatabase
{
    private static List<CardData> all;

    public static IReadOnlyList<CardData> All
    {
        get
        {
            if (all == null)
            {
                all = Resources.LoadAll<CardData>("CardData/Cards")
                    .Concat(Resources.LoadAll<CardData>("ScriptableObjects"))
                    .GroupBy(c => c.Id).Select(g => g.First())
                    .ToList();
            }
            return all;
        }
    }

    public static CardData Get(string id) => All.FirstOrDefault(c => c.Id == id);

    public static IEnumerable<CardData> PlayerCards => All.Where(c => !c.isEnemy);
    public static IEnumerable<CardData> EnemyCards => All.Where(c => c.isEnemy);
    /// <summary>Стартовая колода: каждая isStarter-карта в starterCopies экземплярах (одинаковые ссылки на ассет).</summary>
    public static IEnumerable<CardData> StarterDeck =>
        PlayerCards.Where(c => c.isStarter).SelectMany(c => Enumerable.Repeat(c, Mathf.Max(1, c.starterCopies))); // [D]
    /// <summary>Карты магазина/кейсов этой редкости (закрытые мета-прогрессией не выпадают).</summary>
    public static IEnumerable<CardData> ShopPool(CardRarity rarity) =>
        Obtainable.Where(c => c.rarity == rarity);

    /// <summary>Все карты, которые можно получить (в пуле магазина, открыты мета-прогрессией и с артом).</summary>
    public static IEnumerable<CardData> Obtainable =>
        PlayerCards.Where(c => c.inShopPool && c.ArtSprite != null && MetaProgress.IsCardUnlocked(c.Id));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => all = null;

    public static void Reload() => all = null;

    public static string Describe() =>
        string.Join(", ", All.Select(c => $"{c.Id}[{c.rarity}{(c.isEnemy ? ",enemy" : "")}{(c.isStarter ? ",starter" : "")}]"));
}
