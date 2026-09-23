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

    private bool attackInProgress;

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
    /// <remarks>OnAttackComplete вызывается ровно один раз в любом случае (даже если впереди пусто или карта уничтожена).</remarks>
    public IEnumerator PerformAttack()
    {
        attackInProgress = true;
        var raycastDistance = 34;
        if (Physics.Raycast(transform.position, transform.up, out var hit, raycastDistance))
        {
            if (IsEnemy(hit.collider))
            {
                var enemyCard = hit.collider.GetComponentInParent<Card>();
                if (enemyCard != null)
                {
                    yield return AttackAnimation(enemyCard, null, false);
                }
            }
            else if (IsBoss(hit.collider))
            {
                yield return OnBossHit(null, boss, false);
            }
        }
        CompleteAttack();
    }

    private void CompleteAttack()
    {
        if (!attackInProgress)
        {
            return;
        }
        attackInProgress = false;
        OnAttackComplete?.Invoke();
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
    /// <param name="enemyData">Будет - null, если цель — не карта.</param>
    /// <param name="boss">Будет - null, если цель — не босс.</param>
    /// <param name="shake">Флаг, указывающий, должна ли камера трястись при ударе.</param>
    /// <returns>Возвращает IEnumerator для выполнения анимации атаки.</returns>
    /// <remarks>
    /// Если параметр <paramref name="shake"/> равен true, камера будет трястись, что добавляет визуальный эффект удара.
    /// Используйте этот метод только когда игрок получает урон.
    /// </remarks>

    protected virtual IEnumerator OnBossHit(Card enemyData, Boss boss, bool shake)
    {
        yield return AttackAnimation(enemyData, boss, shake);
    }
    protected void ShakeCamera()
    {
        mainCamera.transform.DOShakePosition(0.5f, strength: new Vector3(3, 3, 0), vibrato: 10, randomness: 90, snapping: false, fadeOut: true);
    }
    private void OnDestroy()
    {
        DOTween.Kill(transform);
        CompleteAttack();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="enemyData">вражеская карта</param>
    /// <param name="boss">Босс</param>
    /// <param name="shake">Наличие Тряски</param>
    /// <returns></returns>
    protected IEnumerator AttackAnimation(Card enemyData, Boss boss, bool shake)
    {
        yield return new WaitForSeconds(0.25f);
        if (this == null)
        {
            yield break;
        }
        var transDef = transform.GetComponent<RectTransform>().position;
        var forwardPosition = transDef + transform.up * moveDistance;

        Sequence bossAttackSequence = DOTween.Sequence().SetTarget(transform);

        bossAttackSequence.Append(transform.DOMove(forwardPosition, 0.2f).SetEase(Ease.OutQuad));

        bossAttackSequence.AppendCallback(() =>
        {
            // [D] урон идёт через CombatRules: щит, статусы при ударе, вампиризм, шипы, реликвии
            if (shake == true)
            {
                ShakeCamera();
                CombatRules.CardHitsPlayer(card);
            }
            if (enemyData != null)
            {
                CombatRules.CardHitsCard(card, enemyData);
            }
            if (boss != null)
            {
                ShakeCamera();
                CombatRules.CardHitsBoss(card, boss);
            }
        });
        bossAttackSequence.Append(transform.DOMove(transDef, 0.2f).SetEase(Ease.OutQuad));
        bossAttackSequence.Play();
        yield return bossAttackSequence.WaitForCompletion();
    }
}
