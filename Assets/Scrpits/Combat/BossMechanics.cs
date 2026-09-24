using System.Collections;
using UnityEngine;

/// <summary>Особая механика босса/элиты (Encounters.Mechanic).</summary>
public enum BossMechanic
{
    None,
    /// <summary>Каждые 3 раунда призывает value подчинённых.</summary>
    Summoner,
    /// <summary>Щит value: пока он есть, ближний бой не проходит - щит пробивают только дальнобойные карты (и предметы).
    /// Каждые 2 раунда щит восстанавливается.</summary>
    RangedShield,
    /// <summary>В начале каждого твоего хода крадёт value маны.</summary>
    ManaThief,
}

/// <summary>[Boss] Логика механик босса. Хуки: старт боя, ход босса (конец раунда), старт хода игрока, удар по боссу.</summary>
public static class BossMechanics
{
    public static string Describe(BossMechanic m, int v) => m switch
    {
        BossMechanic.Summoner => $"Summons {v} minion{(v > 1 ? "s" : "")} every 3 turns",
        BossMechanic.RangedShield => $"Shield {v}: only RANGED cards can break it",
        BossMechanic.ManaThief => $"Steals {v} mana every turn",
        _ => "",
    };

    private static Boss FindBoss() => Object.FindAnyObjectByType<Boss>();

    /// <summary>После Encounters.Setup.</summary>
    public static void OnBattleStart()
    {
        if (Encounters.Mechanic == BossMechanic.RangedShield) RestoreShield(false);
    }

    /// <summary>Ход босса (конец раунда, перед его атакой).</summary>
    public static IEnumerator OnBossTurn(Boss boss)
    {
        if (boss == null || boss.IsDefeated()) yield break;
        int round = CombatRules.Round;
        switch (Encounters.Mechanic)
        {
            case BossMechanic.Summoner:
                if (round > 0 && round % 3 == 0)
                {
                    var spawner = Object.FindAnyObjectByType<EnemySpawnCardLogic>();
                    int n = spawner != null ? spawner.Summon(Encounters.MechanicValue) : 0;
                    if (n > 0)
                    {
                        CombatFx.TurnBanner("REINFORCEMENTS!", VisualTheme.EnemyAccent, 0.5f);
                        SoundFx.Play(SoundFx.Clip.BossHit);
                        yield return new WaitForSeconds(0.8f);
                    }
                }
                break;
            case BossMechanic.RangedShield:
                if (round > 0 && round % 2 == 0 && StatusHolder.Of(boss).Get(StatusType.Shield) < Encounters.MechanicValue)
                {
                    RestoreShield(true);
                    yield return new WaitForSeconds(0.5f);
                }
                break;
        }
    }

    /// <summary>Начало хода игрока (после восстановления маны).</summary>
    public static void OnPlayerTurnStart()
    {
        if (Encounters.Mechanic != BossMechanic.ManaThief) return;
        var boss = FindBoss();
        if (boss == null || boss.IsDefeated()) return;
        var mana = Object.FindAnyObjectByType<CardCostStatus>();
        if (mana == null) return;
        int steal = Mathf.Min(Encounters.MechanicValue, mana.totalMana - 1);
        if (steal <= 0) return;
        mana.totalMana -= steal;
        mana.updateText();
        mana.setStatusPreparedness();
        CombatFx.TurnBanner($"MANA STOLEN -{steal}", UiTheme.Mana, 0.4f);
        if (mana.manaValue != null) ImpactFx.Burst(mana.manaValue.transform, UiTheme.Mana, 8, 1f);
    }

    /// <summary>Удар картой по боссу заблокирован (щит, а карта не дальнобойная).</summary>
    public static bool BlocksHit(Card attacker, Boss boss)
    {
        if (Encounters.Mechanic != BossMechanic.RangedShield || boss == null || attacker == null || attacker.CardData == null) return false;
        if (attacker.CardData.ability == CardAbility.Ranged) return false;
        if (StatusHolder.Of(boss).Get(StatusType.Shield) <= 0) return false;
        if (boss.bossImage != null) ImpactFx.Ring(boss.bossImage.transform, UiTheme.Shield, 1.3f);
        CombatFx.Punch(attacker.transform, 0.15f, 0.25f);
        SoundFx.Play(SoundFx.Clip.Block);
        return true;
    }

    private static void RestoreShield(bool announce)
    {
        var boss = FindBoss();
        if (boss == null) return;
        var h = StatusHolder.Of(boss);
        int need = Encounters.MechanicValue - h.Get(StatusType.Shield);
        if (need > 0) h.Add(StatusType.Shield, need);
        if (announce)
        {
            CombatFx.TurnBanner("SHIELD RESTORED", UiTheme.Shield, 0.4f);
            if (boss.bossImage != null) ImpactFx.Ring(boss.bossImage.transform, UiTheme.Shield, 1.6f);
        }
    }
}
