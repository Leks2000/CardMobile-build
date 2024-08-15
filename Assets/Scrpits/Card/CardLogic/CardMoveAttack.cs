using UnityEngine;
using DG.Tweening;

public class CardMoveAttack : MonoBehaviour
{
    [SerializeField] private Card card;
    public void PerformAttack()
    {
        var direction = transform.up;
        var raycastDistance = 32;
        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (LineAttack.Tags.Contains(hit.collider.tag))
        {
            Debug.Log("Объект перед картой является врагом: " + hit.collider.tag);
            var enemyCard = hit.collider.GetComponentInParent<Card>();
            Attack(enemyCard);
            return;
        }
        Debug.Log("Враг БОСС " + hit.collider.tag);
        var enemyBoss = hit.collider.GetComponentInParent<Card>();
        Attack(enemyBoss);
    }
    private void Attack(Card enemyData)
    {
        /// Анимировать атаку
        /*
        var transEnemy = enemyData.GetComponent<RectTransform>().localPosition;
        var transDef = transform.GetComponent<RectTransform>().localPosition;
        transform.DOMove(transEnemy, 2f).SetEase(Ease.OutQuad);
        transform.DOMove(transDef, 2f).SetEase(Ease.OutQuad);
        */
        enemyData.TakeDamage(card.CardData.Damage);
    }
}
