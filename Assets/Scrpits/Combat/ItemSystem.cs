using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>
/// [Items] Использование расходников в бою (панель предметов BattleItemBar вызывает <see cref="TryUse"/>).
/// Предмет можно использовать только в свой ход (не во время атаки/хода врага) и пока бой не закончен.
/// Eval: ItemSystem.TryUse("heal_potion", out var r)
/// </summary>
public static class ItemSystem
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>Предмет использован (id) - панель предметов проигрывает эффект.</summary>
    public static event System.Action<ItemDef> Used;

    private static bool battleEndedByItem;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Used = null;
        battleEndedByItem = false;
    }

    public static void OnBattleStart() => battleEndedByItem = false;

    /// <summary>Можно ли сейчас использовать предметы (и почему нет).</summary>
    public static bool CanUseNow(out string reason)
    {
        reason = null;
        var boss = Object.FindAnyObjectByType<Boss>();
        if (battleEndedByItem || (boss != null && boss.IsDefeated()) || Player.Instance == null || Player.Instance.IsDefeated())
        {
            reason = "The battle is over";
            return false;
        }
        if (IsEnemyTurn())
        {
            reason = "Wait for your turn";
            return false;
        }
        if (CardDrag.IsDraggingAnyCard)
        {
            reason = "";
            return false;
        }
        return true;
    }

    /// <summary>Идёт ход врага (нажат End Turn и камера/атаки ещё не вернулись).</summary>
    public static bool IsEnemyTurn()
    {
        var end = Object.FindAnyObjectByType<EndTurnCamera>();
        if (end == null) return false;
        var f = typeof(EndTurnCamera).GetField("hasClicked", Flags);
        return f != null && (bool)f.GetValue(end);
    }

    /// <summary>Нужна ли предмету цель, которой сейчас нет (например, бомба без вражеских карт).</summary>
    public static bool HasTarget(ItemDef item, out string reason)
    {
        reason = null;
        switch (item.effect)
        {
            case ItemEffect.Bomb:
                if (EnemyCards().Count == 0) { reason = "No enemy cards on the board"; return false; }
                break;
            case ItemEffect.Rally:
                if (PlayerCards().Count == 0) { reason = "You have no cards"; return false; }
                break;
            case ItemEffect.Heal:
            case ItemEffect.FullHeal:
                if (Player.Instance != null && Player.Instance.HP >= Player.Instance.MaxHP && item.effect == ItemEffect.Heal)
                {
                    reason = "Your HP is already full"; return false;
                }
                break;
            case ItemEffect.Draw:
                var gcm = Object.FindAnyObjectByType<GameControlManager>();
                if (gcm != null && gcm.GetCardInHand <= 0) { reason = "Your hand is full"; return false; }
                if (gcm != null && gcm.DrawPileCount <= 0) { reason = "Your deck is empty"; return false; }
                break;
        }
        return true;
    }

    /// <summary>Использовать расходник из инвентаря забега. result - короткий текст для всплывашки.</summary>
    public static bool TryUse(string id, out string result)
    {
        result = null;
        var item = ItemDatabase.Get(id);
        if (item == null || item.IsPassive) { result = "Unknown item"; return false; }
        if (RunState.ItemCount(id) <= 0) { result = "You don't have it"; return false; }
        if (!CanUseNow(out result)) return false;
        if (!HasTarget(item, out result)) return false;

        RunState.ConsumeItem(id);
        result = Apply(item);
        Debug.Log($"[ITEM] Used {id}: {result}. Left {RunState.ItemCount(id)}");
        Used?.Invoke(item);
        SoundFx.Play(SoundFx.Clip.Item);
        CheckBossDefeated();
        return true;
    }

    private static string Apply(ItemDef item)
    {
        var player = Player.Instance;
        var boss = Object.FindAnyObjectByType<Boss>();
        switch (item.effect)
        {
            case ItemEffect.Heal:
            {
                int healed = CombatRules.HealPlayer(item.value);
                return $"+{healed} HP";
            }
            case ItemEffect.Shield:
                StatusHolder.Of(player).Add(StatusType.Shield, item.value);
                return $"+{item.value} Shield";
            case ItemEffect.Mana:
            {
                var mana = Object.FindAnyObjectByType<CardCostStatus>();
                if (mana != null)
                {
                    mana.totalMana += item.value;
                    mana.updateText();
                    mana.setStatusPreparedness();
                }
                return $"+{item.value} Mana";
            }
            case ItemEffect.Draw:
            {
                var gcm = Object.FindAnyObjectByType<GameControlManager>();
                int drawn = gcm != null ? gcm.DrawExtra(item.value) : 0;
                var mana = Object.FindAnyObjectByType<CardCostStatus>();
                if (mana != null) mana.setStatusPreparedness();
                return drawn > 0 ? $"Drew {drawn}" : "Hand is full";
            }
            case ItemEffect.BossDamage:
            {
                if (boss == null || boss.IsDefeated()) return "No target";
                int dealt = StatusHolder.Of(boss).AbsorbWithShield(item.value);
                if (dealt > 0) boss.TakeDamage(dealt);
                return $"-{dealt} to boss";
            }
            case ItemEffect.Bomb:
            {
                var targets = EnemyCards();
                foreach (var c in targets) c.TakeDamage(item.value);
                CombatFx.ShakeCamera(0.5f, 0.25f);
                return $"{targets.Count} enemies hit";
            }
            case ItemEffect.Poison:
            {
                if (boss != null && !boss.IsDefeated()) StatusHolder.Of(boss).Add(StatusType.Poison, item.value);
                foreach (var c in EnemyCards()) StatusHolder.Of(c).Add(StatusType.Poison, 2);
                return $"Poison {item.value}";
            }
            case ItemEffect.Rally:
            {
                var cards = PlayerCards();
                foreach (var c in cards)
                {
                    c.CardData.Damage += item.value;
                    c.UpdateCardDisplay();
                    StatusFx.Atk(c.transform, item.value);
                    CombatFx.Punch(c.transform, 0.2f, 0.3f);
                }
                return $"+{item.value} ATK x{cards.Count}";
            }
            case ItemEffect.Cleanse:
            {
                Cleanse(StatusHolder.Of(player));
                foreach (var c in PlayerCards()) Cleanse(c.Statuses);
                int healed = CombatRules.HealPlayer(item.value);
                return healed > 0 ? $"Cleansed, +{healed} HP" : "Cleansed";
            }
            case ItemEffect.FullHeal:
            {
                int healed = CombatRules.HealPlayer(Mathf.Max(1, player != null ? player.MaxHP : 999));
                StatusHolder.Of(player).Add(StatusType.Shield, item.value);
                return $"+{healed} HP, +{item.value} Shield";
            }
        }
        return "";
    }

    private static void Cleanse(StatusHolder h)
    {
        if (h == null) return;
        h.Add(StatusType.Poison, -h.Get(StatusType.Poison));
        h.Add(StatusType.Bleed, -h.Get(StatusType.Bleed));
    }

    /// <summary>Вражеские карты на поле (живые).</summary>
    public static List<Card> EnemyCards()
    {
        var list = new List<Card>();
        foreach (var c in Object.FindObjectsByType<Card>(FindObjectsSortMode.None))
        {
            if (c.CardData != null && c.CardData.isEnemy && c.CardData.HP > 0 && c.gameObject.activeInHierarchy) list.Add(c);
        }
        return list;
    }

    /// <summary>Карты игрока (в руке и на поле).</summary>
    public static List<Card> PlayerCards()
    {
        var list = new List<Card>();
        foreach (var c in Object.FindObjectsByType<Card>(FindObjectsSortMode.None))
        {
            if (c.CardData != null && !c.CardData.isEnemy && c.CardData.HP > 0 && c.gameObject.activeInHierarchy) list.Add(c);
        }
        return list;
    }

    /// <summary>
    /// Босс убит предметом посреди хода - завершаем бой так же, как LineAttackMoveActivation в конце раунда.
    /// </summary>
    private static void CheckBossDefeated()
    {
        var boss = Object.FindAnyObjectByType<Boss>();
        if (boss == null || !boss.IsDefeated() || battleEndedByItem) return;
        battleEndedByItem = true;
        // End Turn больше не нажимается
        var end = Object.FindAnyObjectByType<EndTurnCamera>();
        if (end != null) typeof(EndTurnCamera).GetField("hasClicked", Flags)?.SetValue(end, true);
        CombatFx.StartRoutine(EndBattleWon());
    }

    private static IEnumerator EndBattleWon()
    {
        yield return new WaitForSeconds(0.9f);
        var line = Object.FindAnyObjectByType<LineAttackMoveActivation>();
        if (line != null && typeof(LineAttackMoveActivation).GetField("uiControlCV", Flags)?.GetValue(line) is Canvas cv && cv != null)
        {
            cv.renderMode = RenderMode.WorldSpace;
        }
        var over = Object.FindAnyObjectByType<GameManagerOver>();
        if (over != null) over.GameOver(true);
    }
}
