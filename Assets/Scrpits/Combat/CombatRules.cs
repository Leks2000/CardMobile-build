using System.Collections;
using System.Reflection;
using UnityEngine;

/// <summary>
/// [D] Правила удара и раунда: щит, статусы при ударе, вампиризм, шипы, яд/кровотечение, атака босса.
/// Атакующий код (CardForwardAttack) и ход (LineAttackMoveActivation) вызывают только эти методы.
/// </summary>
public static class CombatRules
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Номер раунда текущего боя (растёт при каждом End Turn).</summary>
    public static int Round;
    /// <summary>Урон, полученный игроком за бой (для бонуса "No damage taken").</summary>
    public static int DamageTaken;

    public static void ResetBattle()
    {
        Round = 0;
        DamageTaken = 0;
    }

    // ---------- удары ----------

    public static void CardHitsCard(Card attacker, Card target)
    {
        if (attacker == null || target == null) return;
        var dealt = target.TakeDamage(attacker.CardData.Damage);
        SoundFx.Play(SoundFx.Clip.Hit);
        AfterHit(attacker, target.Statuses, dealt);

        var thorns = target.Statuses != null ? target.Statuses.Get(StatusType.Thorns) : 0;
        if (thorns > 0 && attacker != null)
        {
            attacker.TakeDamage(thorns);
        }
    }

    public static void CardHitsBoss(Card attacker, Boss boss)
    {
        if (attacker == null || boss == null) return;
        if (BossMechanics.BlocksHit(attacker, boss)) return; // [Boss] щит держит ближний бой
        var holder = StatusHolder.Of(boss);
        var dealt = holder.AbsorbWithShield(attacker.CardData.Damage);
        // число урона сначала на клетке ударившей карты, потом летит к боссу - босс реагирует по прилёту
        var bossTarget = boss.bossImage != null ? boss.bossImage.transform : boss.transform;
        float fxDelay = BoardDamagePop.Fly(attacker.transform, dealt, bossTarget, dealt > 0 ? (Color?)null : UiTheme.Shield);
        if (dealt > 0) boss.TakeDamage(dealt, fxDelay);
        SoundFx.Play(dealt > 0 ? SoundFx.Clip.BossHit : SoundFx.Clip.Block);
        AfterHit(attacker, holder, dealt);
    }

    public static void CardHitsPlayer(Card attacker)
    {
        if (attacker == null || Player.Instance == null) return;
        var dealt = DamagePlayer(attacker.CardData.Damage);
        AfterHit(attacker, StatusHolder.Of(Player.Instance), dealt);
    }

    /// <summary>Урон игроку через его щит. Возвращает урон по HP.</summary>
    public static int DamagePlayer(int damage, bool ignoreShield = false)
    {
        var player = Player.Instance;
        if (player == null || damage <= 0 || player.IsDefeated()) return 0;
        // [Rules] босс уже повержен - игрок больше не получает урон (добивать босса ради сверхурона можно)
        var boss = Object.FindAnyObjectByType<Boss>();
        if (boss != null && boss.IsDefeated()) return 0;
        if (!ignoreShield) damage = StatusHolder.Of(player).AbsorbWithShield(damage);
        if (damage > 0)
        {
            player.TakeDamage(damage);
            DamageTaken += damage;
            SoundFx.Play(SoundFx.Clip.PlayerHit);
            SoundFx.Vibrate();
            if (player.IsDefeated()) EndBattleLost();
        }
        return damage;
    }

    private static void AfterHit(Card attacker, StatusHolder target, int dealt)
    {
        var data = attacker.CardData;
        if (target != null)
        {
            foreach (var spec in data.statuses)
            {
                if (!spec.self) target.Add(spec.type, spec.value);
            }
        }
        if (data.isEnemy) return;

        RelicSystem.OnPlayerCardHit(attacker, target);
        var lifesteal = attacker.Statuses != null ? attacker.Statuses.Get(StatusType.Lifesteal) : 0;
        if (lifesteal > 0 && dealt > 0)
        {
            HealPlayer(Mathf.Min(lifesteal, dealt));
        }
    }

    // ---------- раунд ----------

    /// <summary>Начало раунда (нажат End Turn): яд/кровотечение на всех. true - кто-то получил урон.</summary>
    public static bool TickRound()
    {
        Round++;
        var any = false;
        foreach (var card in Object.FindObjectsByType<Card>(FindObjectsSortMode.None))
        {
            if (card.Statuses == null) continue;
            bool poisoned = card.Statuses.Get(StatusType.Poison) > 0;
            var dot = card.Statuses.TickDamageOverTime();
            if (dot > 0)
            {
                StatusFx.Tick(card.transform, poisoned ? StatusType.Poison : StatusType.Bleed);
                card.TakeDamage(dot, true);
                any = true;
            }
        }

        var boss = Object.FindAnyObjectByType<Boss>();
        if (boss != null && boss.TryGetComponent<StatusHolder>(out var bossStatus))
        {
            var dot = bossStatus.TickDamageOverTime();
            if (dot > 0)
            {
                boss.TakeDamage(dot);
                any = true;
            }
        }

        if (Player.Instance != null && Player.Instance.TryGetComponent<StatusHolder>(out var playerStatus))
        {
            var dot = playerStatus.TickDamageOverTime();
            if (dot > 0)
            {
                DamagePlayer(dot, true);
                any = true;
            }
        }
        return any;
    }

    /// <summary>Конец раунда: босс бьёт игрока на attackPower (показано в BossIntent).</summary>
    public static IEnumerator BossAttack(Boss boss)
    {
        yield return BossMechanics.OnBossTurn(boss); // [Boss] призыв / восстановление щита
        var power = Encounters.BossAttack;
        if (boss == null || boss.IsDefeated() || Player.Instance == null || Player.Instance.IsDefeated())
        {
            yield break;
        }
        // [Balance] босс не бьёт каждый раунд: действие по циклу (атака / щит / тяжёлый удар / передышка)
        yield return BossMechanics.DoAction(boss, Encounters.ActionForRound(Round), Mathf.Max(0, power));
        Encounters.RefreshIntent();
    }

    /// <summary>
    /// [Rules] HP игрока = 0: бой заканчивается сразу, не дожидаясь атак остальных линий.
    /// Останавливает ход (атаки/движение), возвращает камеру и показывает поражение.
    /// </summary>
    public static void EndBattleLost()
    {
        var over = Object.FindAnyObjectByType<GameManagerOver>();
        if (over == null || over.IsShown) return;
        var line = Object.FindAnyObjectByType<LineAttackMoveActivation>();
        if (line != null) line.StopAllCoroutines();
        foreach (var atk in Object.FindObjectsByType<CardForwardAttack>(FindObjectsSortMode.None)) atk.StopAllCoroutines();
        CombatFx.StartRoutine(LostRoutine(over));
    }

    private static IEnumerator LostRoutine(GameManagerOver over)
    {
        yield return new WaitForSeconds(0.6f);
        var cam = Object.FindAnyObjectByType<EndTurnCamera>();
        if (cam != null) yield return cam.StartCoroutine(cam.ReturnToInitialPosition());
        if (over != null && !over.IsShown) over.GameOver(false);
    }

    // ---------- игрок ----------

    public static PlayerData PlayerDataOf(Player player) =>
        player == null ? null : typeof(Player).GetField("playerData", Flags)?.GetValue(player) as PlayerData;

    /// <summary>Лечит игрока в бою (до максимума). Возвращает фактическое лечение.</summary>
    public static int HealPlayer(int amount)
    {
        var player = Player.Instance;
        var data = PlayerDataOf(player);
        if (data == null || amount <= 0) return 0;
        var before = data.playerHP;
        data.playerHP = Mathf.Min(data.playerHPMAX, data.playerHP + amount);
        player.UpdatePlayerDisplay();
        if (data.playerHP > before && player.playerHpText != null)
        {
            CombatFx.Punch(player.playerHpText.transform, 0.15f, 0.25f);
        }
        return data.playerHP - before;
    }
}
