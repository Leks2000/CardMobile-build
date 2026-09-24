using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>
/// Прокачка карты колоды забега в «Veteran»-версию: +1 ATK, +1 HP, золотая рамка, имя «Veteran ...».
/// Где: узел «Отдых» (бесплатно, вместо лечения) и магазин (за монеты).
/// </summary>
public static class CardUpgrade
{
    public const int ShopPrice = 60;
    public static readonly Color Gold = new Color32(0xFF, 0xC4, 0x3D, 0xFF);

    public static bool CanUpgrade(CardData c) => c != null && !c.upgraded && !c.isEnemy;

    public static bool AnyUpgradable()
    {
        foreach (var c in RunState.Deck) if (CanUpgrade(c)) return true;
        return false;
    }

    /// <summary>Прокачать карту колоды по индексу. Возвращает новую карту (или null).</summary>
    public static CardData UpgradeDeckCard(int index)
    {
        if (index < 0 || index >= RunState.Deck.Count || !CanUpgrade(RunState.Deck[index])) return null;
        var src = RunState.Deck[index];
        var c = MakeVeteran(src);
        RunState.Deck[index] = c;
        RunState.Save();
        Debug.Log($"[UPGRADE] {src.Title} -> {c.Title} ({c.Damage}/{c.HP})");
        return c;
    }

    /// <summary>Veteran-копия карты (+1 ATK, +1 HP, золотая рамка). Также при загрузке сохранённого забега.</summary>
    public static CardData MakeVeteran(CardData src)
    {
        var c = Object.Instantiate(src);
        c.name = src.name;
        c.id = src.Id;
        c.upgraded = true;
        c.HP += 1;
        c.Damage += 1;
        c.displayName = "Veteran " + src.Title;
        c.hideFlags = HideFlags.DontUnloadUnusedAsset; // живёт между сценами, пока идёт забег
        return c;
    }
}
