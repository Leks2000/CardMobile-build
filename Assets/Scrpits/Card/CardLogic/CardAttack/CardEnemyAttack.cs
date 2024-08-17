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

    protected override void OnBossHit()
    {
        ShakeCamera();
        base.OnBossHit();
    }
}
