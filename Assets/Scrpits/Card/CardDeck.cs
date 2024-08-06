
using TMPro;
using UnityEngine;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private int totalCard;
    private CardManager cardManager;
    private TextMeshProUGUI deck;

    private void Start()
    {
        deck = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        deck.text = totalCard.ToString();
        cardManager = FindObjectOfType<CardManager>();
        GetComponent<Button_UI>().ClickFunc = () => cardManager.DrawCard();
    }
    public int GetTotalCards()
    {
        return totalCard;
    }
    public void RemoveCard(int count)
    {
        totalCard = Mathf.Max(totalCard - count);
        deck.text = totalCard.ToString();
    }
}
