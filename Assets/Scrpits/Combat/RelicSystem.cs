using System.Collections.Generic;
using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>
/// [D] Эффекты реликвий (каталог - <see cref="RelicDatabase"/>, в магазине они продаются как пассивные предметы).
/// Хуки: выдача, старт боя, старт раунда, карта сыграна, удар картой игрока, победа, цены магазина.
/// </summary>
public static class RelicSystem
{
    public const string Espresso = "espresso";
    public const string HardHat = "hard_hat";
    public const string Whetstone = "whetstone";
    public const string FirstAid = "first_aid";
    public const string PiggyBank = "piggy_bank";
    public const string VenomVial = "venom_vial";
    public const string DeckPouch = "deck_pouch";
    public const string LuckyClover = "lucky_clover";
    public const string HeartAmulet = "heart_amulet";
    public const string VampireFang = "vampire_fang";
    public const string GoldenRing = "golden_ring";

    public const int HeartAmuletHp = 10;

    private static bool firstCardPlayed;

    public static bool Has(string id) => RunState.Relics.Contains(id);

    /// <summary>Бонус к максимальному HP от реликвий (применяется в RunBattleSetup.ApplyPlayerHp).</summary>
    public static int BonusMaxHp => Has(HeartAmulet) ? HeartAmuletHp : 0;

    /// <summary>Дополнительные карты в стартовой руке боя.</summary>
    public static int ExtraStartingCards => Has(DeckPouch) ? 1 : 0;

    /// <summary>Цена в магазине с учётом скидок.</summary>
    public static int ShopPrice(int basePrice) => Has(GoldenRing) ? Mathf.CeilToInt(basePrice * 0.75f) : basePrice;

    /// <summary>Иконка пассивки (ItemIcons) для магазина / HUD.</summary>
    public static string IconOf(string id) => id switch
    {
        Espresso => ItemIcons.Cup,
        HardHat => ItemIcons.Helmet,
        Whetstone => ItemIcons.Crystal,
        FirstAid => ItemIcons.Cross,
        PiggyBank => ItemIcons.Coin,
        VenomVial => ItemIcons.Drop,
        DeckPouch => ItemIcons.Scroll,
        LuckyClover => ItemIcons.Clover,
        HeartAmulet => ItemIcons.Heart,
        VampireFang => ItemIcons.Fang,
        GoldenRing => ItemIcons.Ring,
        _ => ItemIcons.Star,
    };

    public static Color ColorOf(string id) => id switch
    {
        Espresso => new Color32(0xB0, 0x7A, 0x4A, 0xFF),
        HardHat => new Color32(0xFF, 0xC4, 0x3D, 0xFF),
        Whetstone => new Color32(0xA8, 0xB0, 0xBC, 0xFF),
        FirstAid => new Color32(0xF0, 0xF0, 0xF0, 0xFF),
        PiggyBank => new Color32(0xFF, 0xC4, 0x4D, 0xFF),
        VenomVial => new Color32(0x8C, 0xE0, 0x3C, 0xFF),
        DeckPouch => new Color32(0x8A, 0x6A, 0xD8, 0xFF),
        LuckyClover => new Color32(0x4C, 0xC8, 0x5A, 0xFF),
        HeartAmulet => new Color32(0xE8, 0x3A, 0x6A, 0xFF),
        VampireFang => new Color32(0xF4, 0xEC, 0xDD, 0xFF),
        GoldenRing => new Color32(0xFF, 0xB3, 0x1F, 0xFF),
        _ => new Color32(0xFF, 0xC4, 0x4D, 0xFF),
    };

    /// <summary>Реликвия только что выдана (RelicDatabase.Grant).</summary>
    public static void OnGranted(string id)
    {
        if (id != HeartAmulet) return;
        if (RunState.PlayerHPMax > 0)
        {
            RunState.PlayerHPMax += HeartAmuletHp;
            RunState.PlayerHP += HeartAmuletHp;
        }
        // выдали прямо в бою (награда за элиту/босса) - поднять и текущего игрока, иначе StorePlayerHp затрёт бонус
        var data = CombatRules.PlayerDataOf(Player.Instance);
        if (data != null)
        {
            data.playerHPMAX += HeartAmuletHp;
            data.playerHP += HeartAmuletHp;
            Player.Instance.UpdatePlayerDisplay();
        }
    }

    /// <summary>До первого добора (из RunBattleSetup, после Encounters.Setup).</summary>
    public static void OnBattleStart()
    {
        firstCardPlayed = false;
        var mana = Object.FindAnyObjectByType<CardCostStatus>();
        if (mana != null)
        {
            mana.maxMana = Has(Espresso) ? 4 : 3;
            mana.totalMana = mana.maxMana;
            mana.updateText();
        }
        if (Has(HardHat) && Player.Instance != null)
        {
            StatusHolder.Of(Player.Instance).Add(StatusType.Shield, 4);
        }
        if (RunState.Relics.Count > 0) Debug.Log($"[D] Relics active: {string.Join(",", RunState.Relics)}");
    }

    public static void OnRoundStart()
    {
        firstCardPlayed = false;
    }

    /// <summary>Карта игрока положена на поле.</summary>
    public static void OnCardPlayed(Card card)
    {
        if (card == null || card.CardData == null) return;
        if (Has(VampireFang) && card.Statuses != null)
        {
            card.Statuses.Add(StatusType.Lifesteal, 1);
        }
        if (firstCardPlayed) return;
        firstCardPlayed = true;
        if (Has(Whetstone))
        {
            card.CardData.Damage += 2;
            card.UpdateCardDisplay();
            StatusFx.Atk(card.transform, 2);
            CombatFx.Punch(card.transform, 0.2f, 0.3f);
        }
    }

    public static void OnPlayerCardHit(Card attacker, StatusHolder target)
    {
        if (Has(VenomVial) && target != null)
        {
            target.Add(StatusType.Poison, 1);
        }
    }

    /// <summary>Победа: бонусы к награде + лечение.</summary>
    public static void OnBattleWon(List<(string label, int coins)> breakdown)
    {
        if (Has(PiggyBank)) breakdown.Add(("Piggy Bank", 10));
        if (Has(LuckyClover))
        {
            int sum = 0;
            foreach (var (_, c) in breakdown) sum += c;
            if (sum > 1) breakdown.Add(("Lucky Clover", sum / 2));
        }
        if (Has(FirstAid)) CombatRules.HealPlayer(6);
    }
}
