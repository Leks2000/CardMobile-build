using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// Централизованное управление эффектами пульсации
/// </summary>
public static class PulseEffectManager
{
    private static List<CardPulseEffect> allEffects = new List<CardPulseEffect>();

    public static void RegisterEffect(CardPulseEffect effect)
    {
        // Статический список переживает смену сцен — чистим уничтоженные эффекты
        allEffects.RemoveAll(e => e == null);
        if (!allEffects.Contains(effect))
        {
            allEffects.Add(effect);
        }
    }

    public static void UnregisterEffect(CardPulseEffect effect)
    {
        if (allEffects.Contains(effect))
        {
            allEffects.Remove(effect);
        }
    }

    /// <summary>
    /// Запускает пульсацию только если canDrop == true
    /// </summary>
    public static void ShowPlacementPulses()
    {
        foreach (var card in allEffects)
        {
            if (card == null)
            {
                continue;
            }
            var drop = card.GetComponentInParent<DropCard>();
            if (drop != null)
            {
                card.SetVisible(drop.canDrop);
                card.ContinuePulse();
            }
        }
    }

    public static void HideAllPlacementPulses()
    {
        foreach (var card in allEffects)
        {
            card.SetVisible(false);
        }
    }



    /// <summary>
    /// Визуальный эффект удара (красный) — запустить вручную
    /// </summary>
    public static void StartAttackPulse(GameObject target)
    {
        var attackEffect = target.transform.Find("AttackEffect")?.GetComponent<CardPulseEffect>();
        attackEffect?.SetVisible(true);
    }

    public static void StopAttackPulse(GameObject target)
    {
        var attackEffect = target.transform.Find("AttackEffect")?.GetComponent<CardPulseEffect>();
        attackEffect?.SetVisible(false);
    }

}
