using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Стартовая колода (открывается за очки славы).</summary>
public class StarterDeckDef
{
    public string id;
    public string name;
    public string description;
    public int cost;
    /// <summary>id карт (повтор = несколько копий). null - стандартная стартовая колода (CardData.isStarter).</summary>
    public string[] cards;

    public StarterDeckDef(string id, string name, string description, int cost, params string[] cards)
    {
        this.id = id; this.name = name; this.description = description; this.cost = cost; this.cards = cards;
    }
}

/// <summary>
/// Мета-прогрессия между забегами (PlayerPrefs): очки славы (Renown) за каждый забег,
/// за них открываются стартовые колоды и новые карты (закрытые карты не выпадают в магазине/кейсах).
/// Eval: MetaProgress.Describe()
/// </summary>
public static class MetaProgress
{
    private const string PointsKey = "MetaRenown";
    private const string UnlocksKey = "MetaUnlocks";
    public const string DefaultDeck = "office";

    public static readonly List<StarterDeckDef> Decks = new List<StarterDeckDef>
    {
        new StarterDeckDef(DefaultDeck, "Open Space", "The classic office crew. Balanced.", 0, null),
        new StarterDeckDef("it_dept", "IT Department", "Snipers and poison: kill from the back row.", 60,
            "StaplerSniper", "StaplerSniper", "ITWizard", "Intern", "Intern", "Accountant", "MotivationalCoach", "SecurityGuard"),
        new StarterDeckDef("security", "Security Team", "Shields, guards and thorns. Hard to break.", 90,
            "CompliancePaladin", "SysadminWarden", "SecurityGuard", "SecurityGuard", "CactusKeeper", "Intern", "Intern", "HRNurse"),
    };

    /// <summary>Карты, которые нужно открыть, прежде чем они появятся в магазине и кейсах.</summary>
    public static readonly Dictionary<string, int> LockedCards = new Dictionary<string, int>
    {
        ["ScrumMaster"] = 30,
        ["HRNurse"] = 30,
        ["ITWizard"] = 45,
        ["SysadminWarden"] = 45,
    };

    public static int Points => PlayerPrefs.GetInt(PointsKey, 0);

    public static void AddPoints(int amount)
    {
        if (amount <= 0) return;
        PlayerPrefs.SetInt(PointsKey, Points + amount);
        PlayerPrefs.Save();
    }

    private static HashSet<string> Unlocks()
    {
        var raw = PlayerPrefs.GetString(UnlocksKey, "");
        return new HashSet<string>(raw.Split(',').Where(x => !string.IsNullOrEmpty(x)));
    }

    public static bool IsUnlocked(string key) => key == "deck:" + DefaultDeck || Unlocks().Contains(key);

    public static bool IsDeckUnlocked(string deckId) => deckId == DefaultDeck || IsUnlocked("deck:" + deckId);

    public static bool IsCardUnlocked(string cardId) => !LockedCards.ContainsKey(cardId) || IsUnlocked("card:" + cardId);

    /// <summary>Купить открытие за очки. false - не хватает очков или уже открыто.</summary>
    public static bool TryUnlock(string key, int cost)
    {
        if (IsUnlocked(key) || Points < cost) return false;
        PlayerPrefs.SetInt(PointsKey, Points - cost);
        var set = Unlocks();
        set.Add(key);
        PlayerPrefs.SetString(UnlocksKey, string.Join(",", set));
        PlayerPrefs.Save();
        Debug.Log($"[META] Unlocked {key} for {cost}. Renown left {Points}");
        return true;
    }

    public static List<StarterDeckDef> UnlockedDecks() => Decks.Where(d => IsDeckUnlocked(d.id)).ToList();

    public static StarterDeckDef Deck(string id) => Decks.FirstOrDefault(d => d.id == id) ?? Decks[0];

    /// <summary>Карты стартовой колоды.</summary>
    public static List<CardData> DeckCards(string deckId)
    {
        var def = Deck(deckId);
        if (def.cards == null) return CardDatabase.StarterDeck.ToList();
        var list = def.cards.Select(CardDatabase.Get).Where(c => c != null).ToList();
        return list.Count > 0 ? list : CardDatabase.StarterDeck.ToList();
    }

    public static string Describe() =>
        $"renown={Points} decks=[{string.Join(",", UnlockedDecks().Select(d => d.id))}] cards=[{string.Join(",", LockedCards.Keys.Where(IsCardUnlocked))}]";
}
