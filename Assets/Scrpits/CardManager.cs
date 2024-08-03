using Unity.VisualScripting;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private GameObject card;
    [SerializeField] private int maxCardsInHand = 5;

    private int currentCardsInHand;

    public void DrawCard()
    {
        if (cardDeck.GetTotalCards() > 0 && currentCardsInHand <= 4)
        {
            currentCardsInHand += 1;
            maxCardsInHand -= 1;
            cardDeck.RemoveCard(1);
            Instantiate(card, transform);
        }
    }
}
