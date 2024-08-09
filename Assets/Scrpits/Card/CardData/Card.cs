using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CardData;

public class Card : MonoBehaviour
{
    private CardData cardData;
    private DropCard dropCard;
    private TMP_Text cardDmg;
    private TMP_Text cardHp;
    private Image typeAttack;
    private void Awake()
    {
        cardHp = transform.Find("HP").GetComponent<TMP_Text>();
        cardDmg = transform.Find("Damage").GetComponent<TMP_Text>();
        typeAttack = transform.Find("TypeAttack").GetComponent<Image>();
    }

    public void Initialize()
    {
        var hp = int.Parse(cardHp.text);
        var damage = int.Parse(cardDmg.text);
        var attackType = GetAttackTypeFromImage(typeAttack.sprite);
        cardData = new CardData(hp, damage, attackType);

    }
    public void UpdateCardDisplay()
    {
        cardHp.text = cardData.HP.ToString();
        cardDmg.text = cardData.Damage.ToString();
        Debug.Log($"HP: {cardData.HP}, Damage: {cardData.Damage}, Range: {cardData.Range}");
    }

    private AttackType GetAttackTypeFromImage(Sprite sprite)
    {
        if (sprite.name == "CloseAttack")
        {
            return AttackType.CloseRange;
        }
        else
        {
            return AttackType.LongRange;
        }
    }
    public CardData GetCardData()
    {
        return cardData;
    }
    public void SetHP(int newHP)
    {
        cardData.ApplyDamage(newHP);
        UpdateCardDisplay();
        if (cardData.HP <= 0)
        {
            dropCard = GetComponentInParent<DropCard>();
            dropCard.canDrop = true;
            Destroy(gameObject);
        }
    }
}
