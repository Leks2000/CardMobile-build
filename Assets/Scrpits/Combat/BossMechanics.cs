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

/// <summary>Что босс делает в свой ход (цикл по Encounters.Pattern, следующее действие показано под HP).</summary>
public enum BossAction
{
    /// <summary>Бьёт тебя на силу атаки.</summary>
    Attack,
    /// <summary>Тяжёлый удар: двойная сила атаки.</summary>
    HeavyAttack,
    /// <summary>Получает щит (сила атаки + 2).</summary>
    Guard,
    /// <summary>Все вражеские карты +1 ATK.</summary>
    Rally,
    /// <summary>Передышка: ничего не делает.</summary>
    Rest,
}

/// <summary>[Boss] Логика механик босса. Хуки: старт боя, ход босса (конец раунда), старт хода игрока, удар по боссу.</summary>
public static class BossMechanics
{
    public static string DescribeAction(BossAction a, int power) => a switch
    {
        BossAction.Attack => $"Next: attacks you for {power}",
        BossAction.HeavyAttack => $"Next: HEAVY attack for {power * 2}!",
        BossAction.Guard => $"Next: raises a shield ({power + 2})",
        BossAction.Rally => "Next: rallies minions (+1 ATK)",
        _ => "Next: catches breath",
    };

    /// <summary>Выполнить действие босса этого раунда (конец раунда).</summary>
    public static IEnumerator DoAction(Boss boss, BossAction action, int power)
    {
        switch (action)
        {
            case BossAction.Attack:
            case BossAction.HeavyAttack:
            {
                bool heavy = action == BossAction.HeavyAttack;
                CombatFx.Punch(boss.transform, heavy ? 0.2f : 0.12f, 0.3f);
                yield return new WaitForSeconds(0.3f);
                CombatFx.ShakeCamera(heavy ? 1f : 0.6f, 0.3f);
                CombatRules.DamagePlayer(heavy ? power * 2 : power);
                yield return new WaitForSeconds(0.5f);
                break;
            }
            case BossAction.Guard:
                StatusHolder.Of(boss).Add(StatusType.Shield, power + 2);
                if (boss.bossImage != null) ImpactFx.Ring(boss.bossImage.transform, UiTheme.Shield, 1.4f);
                SoundFx.Play(SoundFx.Clip.Block);
                yield return new WaitForSeconds(0.5f);
                break;
            case BossAction.Rally:
                foreach (var c in ItemSystem.EnemyCards())
                {
                    c.CardData.Damage += 1;
                    c.UpdateCardDisplay();
                    CombatFx.Punch(c.transform, 0.2f, 0.3f);
                }
                CombatFx.TurnBanner("ENEMIES RALLY!", VisualTheme.EnemyAccent, 0.4f);
                yield return new WaitForSeconds(0.6f);
                break;
            default:
                yield return new WaitForSeconds(0.2f);
                break;
        }
    }

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
