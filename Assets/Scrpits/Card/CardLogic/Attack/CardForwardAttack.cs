using UnityEngine;
using DG.Tweening;
using System.Collections;
using DG.Tweening.Plugins;

public abstract class CardForwardAttack : MonoBehaviour
{
    [SerializeField] protected Card card;
    protected Camera mainCamera;
    protected Boss boss;
    public float moveDistance = 1f;

    public delegate void AttackCompleteHandler();
    public event AttackCompleteHandler OnAttackComplete;

    private void Awake()
    {
        mainCamera = Camera.main;
        boss = FindObjectOfType<Boss>();
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
            yield return StartCoroutine(AttackAnimation(enemyCard, null, false));
        }
        if (IsBoss(hit.collider))
        {
            Debug.Log("Враг БОСС " + hit.collider.tag);
            StartCoroutine(OnBossHit(null, boss, false));
        }
        else
        {
            OnAttackComplete?.Invoke();
        }
    }
    protected abstract bool IsBoss(Collider collider);
    protected abstract bool IsEnemy(Collider collider);
    protected virtual IEnumerator OnBossHit(Card enemyData, Boss boss, bool shake)
    {
        yield return StartCoroutine(AttackAnimation(enemyData, boss, shake));
    }
    protected void ShakeCamera()
    {
        mainCamera.transform.DOShakePosition(0.5f, strength: new Vector3(3, 3, 0), vibrato: 10, randomness: 90, snapping: false, fadeOut: true);
    }
    private void OnDestroy()
    {
        DOTween.Kill(transform);
    }
    protected IEnumerator AttackAnimation(Card enemyData, Boss boss, bool shake)
    {
        yield return new WaitForSeconds(0.5f);
        var transDef = transform.GetComponent<RectTransform>().position;
        var forwardPosition = transDef + transform.up * moveDistance;

        Sequence bossAttackSequence = DOTween.Sequence();

        bossAttackSequence.Append(transform.DOMove(forwardPosition, 0.2f).SetEase(Ease.OutQuad));

        bossAttackSequence.AppendCallback(() =>
        {
            if (shake == true)
            {
                ShakeCamera();
            }
            if (enemyData != null)
            {
                enemyData.TakeDamage(card.CardData.Damage);
            }
            if (boss != null)
            {
                boss.TakeDamage(card.CardData.Damage);
            }
        });
        bossAttackSequence.Append(transform.DOMove(transDef, 0.2f).SetEase(Ease.OutQuad));
        bossAttackSequence.Play();
        yield return new WaitForSeconds(0.5f);
        if (card.CardData.HP < 1)
        {
            OnAttackComplete?.Invoke();
        }
        else
        {
            OnAttackComplete?.Invoke();
        }
    }
}
