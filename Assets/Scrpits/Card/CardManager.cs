using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

    public void DrawCard()
    {
        if (cardDeck.GetTotalCards() > 0 && currentCardsPerRound <= 1 && currentCardsInHand > 0)
        {
            currentCardsPerRound += 1;
            currentCardsInHand -= 1;
            cardDeck.RemoveCard(1);
            GameObject newCard = Instantiate(cardPrefab, transform);
            Card cardComponent = newCard.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.Initialize();
                cardComponent.UpdateCardDisplay();
            }
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
