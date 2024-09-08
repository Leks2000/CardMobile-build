using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CardPlayerAttack : CardForwardAttack
{
    protected override bool IsBoss(Collider collider)
    {
        return collider.CompareTag("EnemyBoss");
    }
    protected override bool IsEnemy(Collider collider)
    {
        return collider.CompareTag("Enemy");
    }
    protected override IEnumerator OnBossHit(Card enemyData, Boss boss, bool shake)
    {
        yield return base.OnBossHit(null, FindObjectOfType<Boss>(), false);
    }
}
