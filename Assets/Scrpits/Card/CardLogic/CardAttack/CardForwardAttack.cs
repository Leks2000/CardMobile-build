using UnityEngine;
using DG.Tweening;
using System;

public abstract class CardForwardAttack : MonoBehaviour
{
    [SerializeField] protected Card card;
    [SerializeField] protected Camera mainCamera;
    public float moveDistance = 1f;
    private void Awake()
    {
        mainCamera = Camera.main;
    }
    public void PerformAttack()
    {
        var direction = transform.up;
        var raycastDistance = 32;
        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (IsEnemy(hit.collider))
        {
            Debug.Log("Объект перед картой является врагом: " + hit.collider.tag);
            var enemyCard = hit.collider.GetComponentInParent<Card>();
            Attack(enemyCard);
            return;
        }
        if (IsBoss(hit.collider))
        {
            Debug.Log("Враг БОСС " + hit.collider.tag);
            OnBossHit();
        }
    }
    protected abstract bool IsBoss(Collider collider);
    protected abstract bool IsEnemy(Collider collider);
    protected virtual void OnBossHit()
    {
        Attack(null);
    }
    protected void ShakeCamera()
    {
        mainCamera.transform.DOShakePosition(0.5f, strength: new Vector3(3, 3, 0), vibrato: 10, randomness: 90, snapping: false, fadeOut: true);
    }

    protected void Attack(Card enemyData)
    {
        var transDef = transform.GetComponent<RectTransform>().position;
        Vector3 forwardPosition = transDef + transform.up * moveDistance;

        Sequence bossAttackSequence = DOTween.Sequence();

        bossAttackSequence.Append(transform.DOMove(forwardPosition, 0.2f).SetEase(Ease.OutQuad));

        bossAttackSequence.AppendCallback(() =>
        {
            if (enemyData != null)
            {
                enemyData.TakeDamage(card.CardData.Damage);
            }
            else
            {
                Debug.Log("Наносим урон боссу.");
            }
        });
        if (card.CardData.HP > 0)
        {
            bossAttackSequence.Append(transform.DOMove(transDef, 0.2f).SetEase(Ease.OutQuad));
        }
        bossAttackSequence.Play();
    }
}
