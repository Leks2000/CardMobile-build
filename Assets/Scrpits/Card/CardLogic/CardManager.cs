using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private List<GameObject> deck;
    [SerializeField] private int currentCardsInHand = 5;

    private int currentCardsPerRound;

    public RectTransform playerDeck;
    public GridLayoutGroup layoutGroup;
    public float duration;
    public RectTransform CDPOS;

    private void Awake()
    {
        CDPOS = cardDeck.GetComponent<RectTransform>();
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
                var newCard = Instantiate(randomCard, CDPOS.localPosition, Quaternion.identity);
                var getcard = newCard.GetComponent<RectTransform>();
                getcard.SetParent(cardDeck.transform);
                getcard.localScale = Vector3.one;

                var cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.UpdateCardDisplay();
                }

                var newCards = NewMethod();
                newCards.Add(getcard);

                MoveCardsToPlayerDeck(newCards);

                currentCardsInHand--;
                cardDeck.RemoveCard(1);
            }
        }
    }

    private static List<RectTransform> NewMethod() => new();

    public void MoveCardsToPlayerDeck(List<RectTransform> newCards)
    {
        layoutGroup.enabled = false;

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < newCards.Count; i++)
        {
            var newCard = newCards[i];
            float delay = i * duration; // Задержка для каждой карты

            sequence.AppendCallback(() =>
            {
                newCard.position = CDPOS.position;
                newCard.localScale = Vector3.one;
            })
            .Append(newCard.DOMove(playerDeck.position, duration)
                .SetEase(Ease.OutQuad)
                .SetDelay(delay)
                .OnUpdate(() =>
                {
                    // Optional: Update card visuals or effects during movement
                }));
        }

        sequence.AppendInterval(duration)
            .OnComplete(() =>
            {
                foreach (var newCard in newCards)
                {
                    newCard.SetParent(playerDeck, false);
                    newCard.localPosition = Vector3.zero;
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
                Canvas.ForceUpdateCanvases();
                layoutGroup.enabled = true;
            });
    }
}
