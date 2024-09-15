using DG.Tweening;
using TMPro;
using UnityEngine;

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
    public CardData CardData { get; private set; }

    public TMP_Text damageText;
    public TMP_Text cardHp;
    public TMP_Text cardDmg;
    public TMP_Text cardCost;
    public float duration;
    public float moveDistance;

    private void Awake()
    {
        if (cardData != null)
        {
            CardData = cardData.Clone();
            UpdateCardDisplay();
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
        damageText.text = "-" + damage.ToString();
        AnimateDamageText(() =>
        {
            CardData.ApplyDamage(damage);

            UpdateCardDisplay();

            if (CardData.HP <= 0)
            {
                Destroy(gameObject);
                var dropCard = GetComponentInParent<DropCard>();
                if (dropCard != null)
                {
                    dropCard.canDrop = true;
                }
            }
        });
    }

    private void AnimateDamageText(TweenCallback onCompleteCallback)
    {
        damageText.enabled = true;

        Vector3 initialPosition = damageText.transform.localPosition;

        Sequence damageSequence = DOTween.Sequence();

        damageSequence.Append(damageText.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack));
        damageSequence.Join(damageText.transform.DOMoveY(transform.position.y + moveDistance, duration).SetEase(Ease.OutQuad));
        damageSequence.Join(damageText.DOFade(0, duration).SetDelay(0.5f));

        damageSequence.OnComplete(() =>
        {
            damageText.enabled = false;
            damageText.alpha = 1;
            damageText.transform.localScale = Vector3.one;
            damageText.transform.localPosition = initialPosition;
            onCompleteCallback?.Invoke();
        });
    }
}
