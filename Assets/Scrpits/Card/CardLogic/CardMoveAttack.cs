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
        if (!hit.collider.CompareTag("Enemy"))
        {
            Debug.Log("Объект перед картой является врагом: " + hit.collider.tag);
            return;
        }
        Debug.Log("Враг БОСС " + hit.collider.tag);
        var enemyCard = hit.collider.GetComponentInParent<Card>();
        Attack(enemyCard);
    }
    private void Attack(Card enemyData)
    {
        Debug.Log("DD");
        Sequence shakeSequence = DOTween.Sequence();

        // Тряска по позиции
        shakeSequence.Append(transform.DOShakePosition(1f, 1f, 10, 90f)
            .SetEase(Ease.Linear));

        // Тряска по вращению
        shakeSequence.Join(transform.DOShakeRotation(1f, 10f, 10, 90f)
            .SetEase(Ease.Linear));

        // Тряска по масштабу
        shakeSequence.Join(transform.DOShakeScale(1f, 0.1f, 10, 90f)
            .SetEase(Ease.Linear));

        // Запуск последовательности
        shakeSequence.Play();
        enemyData.TakeDamage(card.CardData.Damage);
    }
}
