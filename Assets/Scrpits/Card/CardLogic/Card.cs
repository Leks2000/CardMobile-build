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


    private void Awake()
    {
        if (cardData != null)
        {
            CardData = cardData.Clone();
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
    }

    public void TakeDamage(int damage)
    {
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
                var dropCard = GetComponentInParent<DropCard>();
                if (dropCard != null)
                {
                    dropCard.canDrop = true;
                }
                Destroy(gameObject);
            }
        });
    }
}
