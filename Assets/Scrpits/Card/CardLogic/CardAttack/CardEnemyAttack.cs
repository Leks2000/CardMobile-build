using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CardEnemyAttack : CardForwardAttack
{
    protected override bool IsBoss(Collider collider)
    {
        return collider.CompareTag("Player");
    }
    protected override bool IsEnemy(Collider collider)
    {
        return collider.CompareTag("Card");
    }

    protected override IEnumerator OnBossHit(Card enemyData, Boss boss, bool shake)
    {
        yield return base.OnBossHit(null, null, true);
    }
}
