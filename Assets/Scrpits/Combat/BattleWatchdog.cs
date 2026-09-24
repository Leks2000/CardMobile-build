using Assets.Scrpits.Run;
using UnityEngine;

/// <summary>
/// Авто-поражение, когда воевать нечем: нет карт на поле и в руке, колода пуста и нет предметов,
/// которые могут добить босса (урон/яд). Проверяет только в твой ход; добавляется BattleSceneDresser.
/// </summary>
public class BattleWatchdog : MonoBehaviour
{
    private const float StartGrace = 2.5f;
    private const float Interval = 0.5f;

    private float started;
    private float next;
    private int hits;
    private bool fired;

    private void Start() => started = Time.time;

    private void Update()
    {
        if (fired || Time.time - started < StartGrace || Time.time < next) return;
        next = Time.time + Interval;
        // условие должно держаться ~1.5 c подряд (карты могут быть в полёте из колоды)
        hits = IsOutOfOptions() ? hits + 1 : 0;
        if (hits < 3) return;
        fired = true;
        Debug.Log("[RULES] No cards, empty deck, no finishing items -> defeat");
        CombatFx.TurnBanner("NO FORCES LEFT", UiTheme.Damage, 1.1f);
        CombatRules.EndBattleLost();
    }

    public static bool IsOutOfOptions()
    {
        var over = FindAnyObjectByType<GameManagerOver>();
        if (over != null && over.IsShown) return false;
        var boss = FindAnyObjectByType<Boss>();
        if (boss == null || boss.IsDefeated()) return false;
        if (Player.Instance == null || Player.Instance.IsDefeated()) return false;
        if (ItemSystem.IsEnemyTurn() || CardDrag.IsDraggingAnyCard) return false;

        var gcm = FindAnyObjectByType<GameControlManager>();
        if (gcm == null || gcm.DrawPileCount > 0) return false;
        if (ItemSystem.PlayerCards().Count > 0) return false; // в руке или на поле

        // яд/кровотечение на боссе ещё может его добить
        var h = StatusHolder.Of(boss);
        if (h != null && h.Get(StatusType.Poison) + h.Get(StatusType.Bleed) >= boss.HP) return false;

        foreach (var kv in RunState.Items)
        {
            if (kv.Value <= 0) continue;
            var item = ItemDatabase.Get(kv.Key);
            if (item != null && (item.effect == ItemEffect.BossDamage || item.effect == ItemEffect.Poison)) return false;
        }
        return true;
    }
}
