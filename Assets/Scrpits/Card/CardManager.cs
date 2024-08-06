using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private GameObject card;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

    public void DrawCard()
    {
        if (cardDeck.GetTotalCards() > 0 && currentCardsPerRound <= 1 && currentCardsInHand > 0)
        {
            currentCardsPerRound += 1;
            currentCardsInHand -= 1;
            cardDeck.RemoveCard(1);
            Instantiate(card, transform);
        }
    }
    public int GetCardInHand
    {
        get { return currentCardsInHand; }
        set { currentCardsInHand = value; }
    }
    public void TurnRound()
    {
        currentCardsPerRound = 0;
    }
}
