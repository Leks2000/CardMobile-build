using System.Collections.Generic;
using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>
/// [D] Эффекты реликвий (каталог - <see cref="RelicDatabase"/>). Хуки: старт боя, старт раунда,
/// карта сыграна, удар картой игрока, победа.
/// </summary>
public static class RelicSystem
{
    public const string Espresso = "espresso";
    public const string HardHat = "hard_hat";
    public const string Whetstone = "whetstone";
    public const string FirstAid = "first_aid";
    public const string PiggyBank = "piggy_bank";
    public const string VenomVial = "venom_vial";

    private static bool firstCardPlayed;

    public static bool Has(string id) => RunState.Relics.Contains(id);

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
        if (firstCardPlayed || card == null || card.CardData == null) return;
        firstCardPlayed = true;
        if (Has(Whetstone))
        {
            card.CardData.Damage += 2;
            card.UpdateCardDisplay();
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
        if (Has(FirstAid)) CombatRules.HealPlayer(6);
    }
}
