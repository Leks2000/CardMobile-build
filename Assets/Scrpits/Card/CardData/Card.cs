using Assets.Utility;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [SerializeField] private CardData cardData;
    public CardData CardData { get; private set; }

    private DropCard dropCard;
    private TMP_Text cardHp;
    private TMP_Text cardDmg;
    private TMP_Text speedAttack;
    private Image typeAttack;
    private void Awake()
    {
        cardHp = transform.Find("HP").GetComponent<TMP_Text>();
        cardDmg = transform.Find("Damage").GetComponent<TMP_Text>();
        speedAttack = transform.Find("speedAttack").GetComponent<TMP_Text>();
        typeAttack = transform.Find("TypeAttack").GetComponent<Image>();
        CardData = cardData.Clone();
    }
    public void UpdateCardDisplay()
    {
        cardHp.text = CardData.HP.ToString();
        cardDmg.text = CardData.Damage.ToString();
        speedAttack.text = CardData.SpeedAttack.ToString();
        Debug.Log($"HP: {CardData.HP}, Damage: {CardData.Damage}, Range: {CardData.distanceToAttack}, SpeedAttack: {CardData.SpeedAttack}");
    }

    public void TakeDamage(int damage)
    {
        CardData.ApplyDamage(damage);
        UpdateCardDisplay();
        if (CardData.HP <= 0)
        {
            dropCard = GetComponentInParent<DropCard>();
            Assets.Utility.DebugUtility.HandleErrorIfNullGetComponent<DropCard, Card>(dropCard, this, gameObject);
            if (dropCard != null)
            {
                dropCard.canDrop = true;
            }
            Destroy(gameObject);
        }
    }
}
