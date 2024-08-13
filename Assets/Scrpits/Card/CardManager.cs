using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private List<GameObject> deck;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

    public void DrawCard()
    {
        if (cardDeck.GetTotalCards() > 0 && currentCardsPerRound <= 2 && currentCardsInHand > 0)
        {
            currentCardsPerRound += 1;
            currentCardsInHand -= 1;
            cardDeck.RemoveCard(1);
            var randomCard = deck[Random.Range(0, deck.Count)];
            var newCard = Instantiate(randomCard, transform);
            var cardComponent = newCard.GetComponent<Card>();
            if (cardComponent != null)
            {
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
