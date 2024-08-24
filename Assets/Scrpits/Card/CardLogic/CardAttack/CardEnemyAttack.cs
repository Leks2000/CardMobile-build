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

    protected override IEnumerator OnBossHit()
    {
        yield return base.OnBossHit();
        ShakeCamera();
    }
}
