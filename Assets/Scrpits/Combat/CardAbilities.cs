using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [Board] Свойства карт (<see cref="CardAbility"/>) + продвижение вперёд после гибели карты.
/// Поле игрока: столбцы (родитель слота) x 2 ряда: передний (DropCard без MoveForward) и задний (DropCard + MoveForward).
/// </summary>
public static class CardAbilities
{
    public static string Label(CardAbility a) => a switch
    {
        CardAbility.Ranged => "RANGED",
        CardAbility.BuffFront => "INSPIRE",
        CardAbility.BuffDiagonal => "FLANK",
        CardAbility.ShieldEachRound => "GUARD",
        CardAbility.HealAdjacent => "HEAL",
        _ => "",
    };

    public static string Describe(CardData d)
    {
        if (d == null) return "";
        int v = d.abilityValue;
        return d.ability switch
        {
            CardAbility.Ranged => "Ranged: attacks from any row over your cards.",
            CardAbility.BuffFront => $"Inspire: when played, the ally in front (or behind) gets +{v} ATK.",
            CardAbility.BuffDiagonal => $"Flank: when played, allies diagonally get +{v} ATK.",
            CardAbility.ShieldEachRound => $"Guard: gains {v} Shield at the start of each of your turns.",
            CardAbility.HealAdjacent => $"Heal: when played, adjacent allies get +{v} HP.",
            _ => "",
        };
    }

    public static Color LabelColor(CardAbility a) => a switch
    {
        CardAbility.Ranged => new Color32(0x9A, 0xD8, 0xFF, 0xFF),
        CardAbility.BuffFront => new Color32(0xFF, 0xB8, 0x3D, 0xFF),
        CardAbility.BuffDiagonal => new Color32(0xFF, 0x8A, 0x3D, 0xFF),
        CardAbility.ShieldEachRound => new Color32(0x7F, 0xD1, 0xFF, 0xFF),
        CardAbility.HealAdjacent => new Color32(0x5E, 0xE0, 0x7A, 0xFF),
        _ => Color.white,
    };

    // ---------------- board geometry ----------------

    private class Slot
    {
        public Transform t;
        public int col;
        public bool back;
    }

    /// <summary>Клетки игрока (DropCard), с номером столбца слева направо и рядом.</summary>
    private static List<Slot> PlayerSlots()
    {
        var drops = Object.FindObjectsByType<DropCard>(FindObjectsSortMode.None);
        var columns = new List<Transform>();
        foreach (var d in drops)
            if (d.transform.parent != null && !columns.Contains(d.transform.parent)) columns.Add(d.transform.parent);
        columns.Sort((a, b) => a.localPosition.x.CompareTo(b.localPosition.x));
        var list = new List<Slot>();
        foreach (var d in drops)
        {
            list.Add(new Slot { t = d.transform, col = columns.IndexOf(d.transform.parent), back = d.GetComponent<MoveForward>() != null });
        }
        return list;
    }

    private static Card CardIn(Transform slot)
    {
        if (slot == null) return null;
        foreach (Transform c in slot)
        {
            if (c.TryGetComponent<Card>(out var card) && card.CardData != null && card.CardData.HP > 0) return card;
        }
        return null;
    }

    private static Card AllyAt(List<Slot> slots, int col, bool back)
    {
        var s = slots.Find(x => x.col == col && x.back == back);
        var c = s != null ? CardIn(s.t) : null;
        return c != null && !c.CardData.isEnemy ? c : null;
    }

    // ---------------- hooks ----------------

    /// <summary>Карта игрока положена на поле (CardDrag).</summary>
    public static void OnPlayed(Card card)
    {
        if (card == null || card.CardData == null || card.CardData.ability == CardAbility.None) return;
        var slots = PlayerSlots();
        var me = slots.Find(s => s.t == card.transform.parent);
        if (me == null) return;
        int v = Mathf.Max(1, card.CardData.abilityValue);
        var targets = new List<Card>();
        switch (card.CardData.ability)
        {
            case CardAbility.BuffFront:
                Add(targets, AllyAt(slots, me.col, !me.back));
                foreach (var t in targets) Buff(t, v, 0, card);
                break;
            case CardAbility.BuffDiagonal:
                Add(targets, AllyAt(slots, me.col - 1, !me.back));
                Add(targets, AllyAt(slots, me.col + 1, !me.back));
                foreach (var t in targets) Buff(t, v, 0, card);
                break;
            case CardAbility.HealAdjacent:
                Add(targets, AllyAt(slots, me.col, !me.back));
                Add(targets, AllyAt(slots, me.col - 1, me.back));
                Add(targets, AllyAt(slots, me.col + 1, me.back));
                foreach (var t in targets) Buff(t, 0, v, card);
                break;
        }
        if (targets.Count > 0) CombatFx.Punch(card.transform, 0.15f, 0.3f);
    }

    /// <summary>Начало твоего хода: Guard-карты на поле получают щит.</summary>
    public static void OnRoundStart()
    {
        foreach (var s in PlayerSlots())
        {
            var c = CardIn(s.t);
            if (c == null || c.CardData.isEnemy || c.CardData.ability != CardAbility.ShieldEachRound || c.Statuses == null) continue;
            c.Statuses.Add(StatusType.Shield, Mathf.Max(1, c.CardData.abilityValue));
            ImpactFx.Ring(c.transform, UiTheme.Shield, 0.8f);
        }
    }

    private static void Add(List<Card> list, Card c)
    {
        if (c != null && !list.Contains(c)) list.Add(c);
    }

    private static void Buff(Card target, int atk, int hp, Card source)
    {
        target.CardData.Damage += atk;
        target.CardData.HP += hp;
        target.UpdateCardDisplay();
        CombatFx.Punch(target.transform, 0.2f, 0.3f);
        ImpactFx.Sparkle(target.transform, atk > 0 ? LabelColor(CardAbility.BuffFront) : UiTheme.Heal, 6, 0.8f);
    }

    // ---------------- ranged ----------------

    /// <summary>Дальний бой: первый коллайдер-цель по лучу вперёд, пропуская своих и пустые клетки.</summary>
    public static bool FindRangedTarget(Transform from, System.Func<Collider, bool> isEnemy, System.Func<Collider, bool> isBoss, out Collider target)
    {
        target = null;
        var hits = Physics.RaycastAll(from.position, from.up, 140f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider.transform.IsChildOf(from)) continue;
            if (isEnemy(h.collider) || isBoss(h.collider))
            {
                target = h.collider;
                return true;
            }
        }
        return false;
    }

    // ---------------- advance after death ----------------

    /// <summary>Клетка освободилась (карта погибла): карта позади сразу выходит вперёд (и дальше по цепочке).</summary>
    public static void OnSlotFreed(Transform slot)
    {
        if (slot == null || !Application.isPlaying) return;
        CombatFx.StartRoutine(Advance(slot, 0.35f));
    }

    private static IEnumerator Advance(Transform slot, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (slot == null || CardIn(slot) != null) yield break;
        foreach (var mf in Object.FindObjectsByType<MoveForward>(FindObjectsSortMode.None))
        {
            if (mf.transform == slot || CardIn(mf.transform) == null) continue;
            if (!Physics.Raycast(mf.transform.position, mf.transform.up, out var hit, 32)) continue;
            if (hit.transform != slot) continue;
            mf.GetPath();
            // освободилась клетка сзади - по цепочке
            CombatFx.StartRoutine(Advance(mf.transform, 0.6f));
            yield break;
        }
    }
}
