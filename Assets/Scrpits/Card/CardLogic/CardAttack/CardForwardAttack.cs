using UnityEngine;
using DG.Tweening;
using System;
using System.Collections;

public abstract class CardForwardAttack : MonoBehaviour
{
    [SerializeField] protected Card card;
    [SerializeField] protected Camera mainCamera;
    public float moveDistance = 1f;

    public delegate void AttackCompleteHandler();
    public event AttackCompleteHandler OnAttackComplete;

    private void Awake()
    {
        mainCamera = Camera.main;
    }
    public IEnumerator PerformAttack()
    {
        var direction = transform.up;
        var raycastDistance = 32;
        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
        }
        if (IsEnemy(hit.collider))
        {
            Debug.Log("Объект перед картой является врагом: " + hit.collider.tag);
            var enemyCard = hit.collider.GetComponentInParent<Card>();
            yield return StartCoroutine(AttackAnimation(enemyCard));
        }
        if (IsBoss(hit.collider))
        {
            Debug.Log("Враг БОСС " + hit.collider.tag);
            StartCoroutine(OnBossHit());
        }
        else
        {
            OnAttackComplete?.Invoke();
        }
    }
    protected abstract bool IsBoss(Collider collider);
    protected abstract bool IsEnemy(Collider collider);
    protected virtual IEnumerator OnBossHit()
    {
        yield return StartCoroutine(AttackAnimation(null));
    }
    protected void ShakeCamera()
    {
        mainCamera.transform.DOShakePosition(0.5f, strength: new Vector3(3, 3, 0), vibrato: 10, randomness: 90, snapping: false, fadeOut: true);
    }

    protected IEnumerator AttackAnimation(Card enemyData)
    {
        yield return new WaitForSeconds(0.5f);
        var transDef = transform.GetComponent<RectTransform>().position;
        Vector3 forwardPosition = transDef + transform.up * moveDistance;

        Sequence bossAttackSequence = DOTween.Sequence();

        bossAttackSequence.Append(transform.DOMove(forwardPosition, 0.2f).SetEase(Ease.OutQuad));

        bossAttackSequence.AppendCallback(() =>
        {
            if (enemyData != null)
            {
                enemyData.TakeDamage(card.CardData.Damage);
                if (enemyData.CardData.Damage > card.CardData.HP)
                {
                    bossAttackSequence.Kill();
                    OnAttackComplete?.Invoke();
                }
            }
        });
        if (card.CardData.HP > 0)
        {
            bossAttackSequence.Append(transform.DOMove(transDef, 0.2f).SetEase(Ease.OutQuad));
        }
        bossAttackSequence.Play();

        yield return new WaitForSeconds(0.5f);
        OnAttackComplete?.Invoke();
    }
}
