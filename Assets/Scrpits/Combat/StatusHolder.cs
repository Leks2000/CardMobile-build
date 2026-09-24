using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [D] Статусы на карте / боссе / игроке (стаки по типу).
/// Bleed  - урон = стаки в начале раунда, потом стаки делятся пополам (быстро сгорает).
/// Poison - урон = стаки в начале раунда, потом стаки -1 (долгий яд).
/// Shield - поглощает входящий урон, расходуется. Lifesteal - лечит игрока при ударе.
/// Thorns - атакующая карта получает урон = стаки.
/// Визуал подписывается на <see cref="Changed"/> и читает <see cref="Stacks"/>.
/// </summary>
[DisallowMultipleComponent]
public class StatusHolder : MonoBehaviour
{
    private readonly Dictionary<StatusType, int> stacks = new Dictionary<StatusType, int>();

    public IReadOnlyDictionary<StatusType, int> Stacks => stacks;
    public event System.Action Changed;
    /// <summary>Статус наложен (для эффектов StatusFx): holder, тип, сколько добавлено.</summary>
    public static event System.Action<StatusHolder, StatusType, int> Added;

    public int Get(StatusType type) => stacks.TryGetValue(type, out var v) ? v : 0;

    public void Add(StatusType type, int amount)
    {
        if (amount == 0) return;
        var value = Mathf.Max(0, Get(type) + amount);
        if (value == 0) stacks.Remove(type);
        else stacks[type] = value;
        Changed?.Invoke();
        if (amount > 0) Added?.Invoke(this, type, amount);
    }

    public void Clear()
    {
        stacks.Clear();
        Changed?.Invoke();
    }

    /// <summary>Снять урон щитом. Возвращает урон, прошедший сквозь щит.</summary>
    public int AbsorbWithShield(int damage)
    {
        var shield = Get(StatusType.Shield);
        if (shield <= 0 || damage <= 0) return damage;
        var used = Mathf.Min(shield, damage);
        Add(StatusType.Shield, -used);
        return damage - used;
    }

    /// <summary>Урон от Bleed/Poison в начале раунда (игнорирует щит) + затухание стаков.</summary>
    public int TickDamageOverTime()
    {
        var damage = 0;
        var poison = Get(StatusType.Poison);
        if (poison > 0)
        {
            damage += poison;
            Add(StatusType.Poison, -1);
        }
        var bleed = Get(StatusType.Bleed);
        if (bleed > 0)
        {
            damage += bleed;
            Add(StatusType.Bleed, -(bleed - bleed / 2));
        }
        return damage;
    }

    /// <summary>StatusHolder объекта (создаётся при первом обращении).</summary>
    public static StatusHolder Of(Component owner)
    {
        if (owner == null) return null;
        return owner.TryGetComponent<StatusHolder>(out var holder) ? holder : owner.gameObject.AddComponent<StatusHolder>();
    }

    public override string ToString()
    {
        var parts = new List<string>();
        foreach (var kv in stacks) parts.Add(kv.Key + ":" + kv.Value);
        return string.Join(",", parts);
    }
}
