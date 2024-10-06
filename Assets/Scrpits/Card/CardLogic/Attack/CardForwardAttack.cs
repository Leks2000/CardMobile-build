using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Базовый класс Атаки
/// </summary>
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

    /// <summary>
    /// Выполняет атаку, проверяя, что находится перед картой.
    /// В зависимости от цели (враг или босс), выполняет соответствующее действие.
    /// Если впереди ничего нет, завершается без выполнения атаки.
    /// </summary>
    /// <returns>Возвращает IEnumerator для управления анимацией и логикой атаки.</returns>
    public IEnumerator PerformAttack()
    {
        var raycastDistance = 34;
        if (Physics.Raycast(transform.position, transform.up, out var hit, raycastDistance))
        {
            if (IsEnemy(hit.collider))
            {
                Debug.Log("Объект перед картой является врагом: " + hit.collider.tag);
                var enemyCard = hit.collider.GetComponentInParent<Card>();
                yield return StartCoroutine(AttackAnimation(enemyCard, null, false));
            }
            if (IsBoss(hit.collider))
            {
                Debug.Log("Враг БОСС " + hit.collider.tag);
                yield return StartCoroutine(OnBossHit(null, boss, false));
            }
            else
            {
                OnAttackComplete?.Invoke();
            }
        }
    }
    /// <summary>
    /// Выполняет только когда враг - босс
    /// </summary>
    protected abstract bool IsBoss(Collider collider);

    /// <summary>
    /// Выполняет только когда враг - обычная карта
    /// </summary>
    /// <param name="collider"></param>
    protected abstract bool IsEnemy(Collider collider);

    /// <summary>
    /// Наносит урон боссу с возможностью тряски камеры.
    /// </summary>
    /// <param name="enemyData">Будет - null, если цель — босс.</param>
    /// <param name="boss">Будет - null, если цель — карта.</param>
    /// <param name="shake">Флаг, указывающий, должна ли камера трястись при ударе.</param>
    /// <returns>Возвращает IEnumerator для выполнения анимации атаки.</returns>
    /// <remarks>
    /// Если параметр <paramref name="shake"/> равен true, камера будет трястись, что добавляет визуальный эффект удара.
    /// Используйте этот метод только когда игрок получает урон.
    /// </remarks>

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
        yield return new WaitForSeconds(0.25f);
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
        yield return new WaitForSeconds(0.25f);
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
