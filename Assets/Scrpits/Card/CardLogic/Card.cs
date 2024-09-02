using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;

    public CardData CardData { get; private set; }

    private TMP_Text cardHp;
    private TMP_Text cardDmg;
    private TMP_Text cardCost;

    private void Awake()
    {
        cardHp = transform.Find("HP/HpText").GetComponent<TMP_Text>();
        cardDmg = transform.Find("TypeAttack/DmgText").GetComponent<TMP_Text>();
        cardCost = transform.Find("Cost/CostText").GetComponent<TMP_Text>();

        if (cardData != null)
        {
            CardData = cardData.Clone();
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
        CardData.ApplyDamage(damage);
        if (CardData.HP <= 0)
        {
            var dropCard = GetComponentInParent<DropCard>();
            if (dropCard != null)
            {
                dropCard.canDrop = true;
            }
        }
        UpdateCardDisplay();
    }
}
