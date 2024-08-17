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
}
