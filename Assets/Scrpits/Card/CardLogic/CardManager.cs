using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private List<GameObject> deck;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

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

                var cardAnimation = newCard.GetComponent<CardAnimation>();
                var index = transform.childCount;
                /// јнимаци€ выт€гивани€ карт из колоды
                /// ѕиздец полный просто нету сука сил
                //if (cardAnimation != null)
                //{
                //    StartCoroutine(cardAnimation.AnimateCardCoroutine(cardDeck.transform));
                //}

                var cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.UpdateCardDisplay();
                }
                currentCardsInHand--;
                cardDeck.RemoveCard(1);
            }
        }
    }
}
