using System.Collections;
using UnityEngine;
using static UnityEngine.UI.Image;

public class CardMoveAttack : MonoBehaviour
{
    [SerializeField] private Card card;
    public void PerformAttack()
    {
        var direction = transform.up;
        var raycastDistance = card.CardData.distanceToAttack;
        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (!hit.collider.CompareTag("enemyCard"))
        {
            Debug.Log("Объект перед картой не является врагом: " + hit.collider.tag);
            return;
        }
        Debug.Log("Враг найден: " + hit.collider.tag);
        var enemyCard = hit.collider.GetComponentInParent<Card>();
        Attack(enemyCard);
    }
    private void Attack(Card enemyData)
    {
        enemyData.TakeDamage(card.CardData.Damage);
    }
}
