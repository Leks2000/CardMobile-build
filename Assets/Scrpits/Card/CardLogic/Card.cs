using Assets.Scrpits.Card.Animation;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// Класс, представляющий карточку в игре. Управляет отображением данных карточки и взаимодействиями.
/// </summary>
public class Card : MonoBehaviour
{
    /// <summary>
    /// Данные карточки, содержащие информацию о характеристиках (атака, здоровье, стоимость и т.д.).
    /// <see cref="CardData"/> для получения более подробной информации о структуре данных карточки.
    /// </summary>
    [SerializeField] private CardData cardData;
    [SerializeField] public CardCostStatus status;
    [SerializeField] private DamageTextAnimator damageAnimator;
    public CardData CardData { get; private set; }

    public TMP_Text damageText;
    public TMP_Text cardDmg;
    public TMP_Text cardHp;
    public TMP_Text cardCost;
    public float moveDistance;
    private int damageCard;


    /// <summary>[D] Статусы этой карты (Shield/Thorns/Lifesteal на себе, Bleed/Poison от врагов).</summary>
    public StatusHolder Statuses { get; private set; }

    /// <summary>
    /// [D] Назначить исходные данные. Вызывать ДО Awake (см. <see cref="CardFactory"/>);
    /// если карта уже инициализирована - пересобирает клон.
    /// </summary>
    public void SetSourceData(CardData data)
    {
        cardData = data;
        if (CardData != null && data != null)
        {
            CardData = data.Clone();
            UpdateCardDisplay();
        }
    }

    private void Awake()
    {
        if (cardData != null)
        {
            CardData = cardData.Clone();
            Statuses = StatusHolder.Of(this);
            foreach (var spec in CardData.statuses)
            {
                if (spec.self) Statuses.Add(spec.type, spec.value);
            }
            UpdateCardDisplay();
            if (gameObject.CompareTag("Card"))
            {
                status = FindAnyObjectByType<CardCostStatus>().GetComponent<CardCostStatus>();
                status.getStatus(gameObject);
            }
        }
    }

    public void UpdateCardDisplay()
    {
        cardHp.text = CardData.HP.ToString();
        cardDmg.text = CardData.Damage.ToString();
        cardCost.text = CardData.Cost.ToString();
        if (TryGetComponent<CardView>(out var view)) view.Refresh(); // [D] V's view redraws stats
    }

    /// <summary>
    /// Получить урон. Щит (StatusHolder) поглощает урон, если не <paramref name="ignoreShield"/> (яд/кровотечение).
    /// Возвращает урон, реально прошедший по HP.
    /// </summary>
    public int TakeDamage(int damage, bool ignoreShield = false)
    {
        if (!ignoreShield && Statuses != null)
        {
            damage = Statuses.AbsorbWithShield(damage);
        }
        if (damage <= 0)
        {
            CombatFx.Punch(transform, 0.1f, 0.2f);
            return 0;
        }
        damageCard = damage;
        damageText.text = "-" + damage.ToString();
        damageAnimator.Animate(damageText, damage,
        () =>
        {
            CardData.ApplyDamage(damage);
            UpdateCardDisplay();
        },
        () =>
        {
            if (CardData.HP <= 0)
            {
                var slot = transform.parent;
                var dropCard = GetComponentInParent<DropCard>();
                if (dropCard != null)
                {
                    dropCard.canDrop = true;
                }
                CombatFx.PlayDeath(gameObject); // [FX] visual-only ghost; original is still destroyed right away
                Destroy(gameObject);
                CardAbilities.OnSlotFreed(slot); // [Board] карта сзади сразу выходит вперёд
            }
        });
        return damage;
    }
}
