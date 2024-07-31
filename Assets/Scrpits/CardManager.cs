using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private GameObject Card;
    [SerializeField] private int maxCardsInHand = 5;

    private int currentCardsInHand;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.CompareTag("Deck"))
                {
                    DrawCard();
                }
            }
        }
    }

    private void DrawCard()
    {
        if (cardDeck.GetTotalCards() > 0 && currentCardsInHand <= 4)
        {
            currentCardsInHand += 1;
            maxCardsInHand -= 1;
            cardDeck.RemoveCard(1);
            Instantiate(Card, this.transform);
        }
    }
}
