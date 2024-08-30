using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    private Transform playerDeck;
    [SerializeField] private List<GameObject> deck;
    private CardAnimation cardAnim;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

    private void Awake()
    {
        playerDeck = GetComponent<Transform>();
    }
    public int GetCardInHand
    {
        get { return currentCardsInHand; }
        set { currentCardsInHand = value; }
    }

    public void TurnRound()
    {
        for (currentCardsPerRound = 0; currentCardsPerRound < 2; currentCardsPerRound++)
        {
            if (cardDeck.GetTotalCards() > 0 && currentCardsPerRound < 2 && currentCardsInHand > 0)
            {
                var randomCard = deck[Random.Range(0, deck.Count)];
                var newCard = Instantiate(randomCard, transform);


                var cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.UpdateCardDisplay();
                }
                cardAnim = newCard.GetComponent<CardAnimation>();
                cardAnim.MoveCardToPlayerDeck(newCard, playerDeck);
                currentCardsInHand--;
                cardDeck.RemoveCard(1);
            }
        }
    }
}
