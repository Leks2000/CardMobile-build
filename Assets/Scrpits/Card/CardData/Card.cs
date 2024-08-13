using Assets.Utility;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
    public CardData CardData { get; private set; }

    private TMP_Text cardHp;
    private TMP_Text cardDmg;
    private TMP_Text cardCost;
    private Image typeAttack;
    private void Awake()
    {
        cardHp = transform.Find("HP/HpText").GetComponent<TMP_Text>();
        cardDmg = transform.Find("TypeAttack/DmgText").GetComponent<TMP_Text>();
        cardCost = transform.Find("Cost/CostText").GetComponent<TMP_Text>();
        //typeAttack = transform.Find("TypeAttack").GetComponent<Image>();

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
        Debug.Log($"HP: {CardData.HP}, Damage: {CardData.Damage}, Range: {CardData.distanceToAttack}, Cost: {CardData.Cost}");
    }

    public void TakeDamage(int damage)
    {
        CardData.ApplyDamage(damage);
        UpdateCardDisplay();
        if (CardData.HP <= 0)
        {
            var dropCard = GetComponentInParent<DropCard>();
            if (dropCard != null)
            {
                Assets.Utility.DebugUtility.HandleErrorIfNullGetComponent<DropCard, Card>(dropCard, this, gameObject);
                dropCard.canDrop = true;
            }
            Destroy(gameObject);
        }
    }
}
