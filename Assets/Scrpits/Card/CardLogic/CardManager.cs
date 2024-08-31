using System.Collections.Generic;
using DG.Tweening;
using UnityEditor.UIElements;
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
    public float duration = 2.5f;
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

        foreach (var newCard in newCards)
        {
            newCard.position = CDPOS.position;

            newCard.DOMove(playerDeck.position, duration).SetEase(Ease.OutQuad);
        }

        // Перемещаем все новые карты в колоду игрока по завершении анимации
        DOTween.Sequence().AppendInterval(duration).OnComplete(() =>
        {
            layoutGroup.enabled = true;
            foreach (var newCard in newCards)
            {
                newCard.SetParent(playerDeck, false);
                newCard.localPosition = new Vector3(0, 0, 0);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
        });
    }

}
