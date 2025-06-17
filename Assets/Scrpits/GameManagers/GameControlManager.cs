using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameControlManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private List<GameObject> deck;
    [SerializeField] private int currentCardsInHand = 5;
    [SerializeField] private CardCostStatus status;

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

    /// <summary>
    /// Завершение раунда игры, добавление карт в колоду игрока и вызов метода <see cref="MoveCardsToPlayerDeck(List{RectTransform})"/>
    /// </summary>
    public void TurnRound()
    {
        List<RectTransform> newCards = new List<RectTransform>();

        for (currentCardsPerRound = 0; currentCardsPerRound < 2; currentCardsPerRound++)
        {
            if (cardDeck.GetTotalCards() > 0 && currentCardsPerRound < 2 && currentCardsInHand > 0)
            {
                var randomCard = deck[Random.Range(0, deck.Count)];
                var newCard = Instantiate(randomCard, CDPOS.position, Quaternion.identity);
                var getcard = newCard.GetComponent<RectTransform>();
                getcard.SetParent(cardDeck.transform);
                getcard.localScale = Vector3.one;

                var cardComponent = newCard.GetComponent<Card>();
                if (cardComponent != null)
                {
                    cardComponent.UpdateCardDisplay();
                }

                newCards.Add(getcard);

                MoveCardsToPlayerDeck(newCards);

                currentCardsInHand--;
                cardDeck.RemoveCard(1);
            }
        }
        status.totalMana = 3;
        status.updateText();
        status.setStatusPreparedness();

        Sequence sequence = DOTween.Sequence();
        sequence.AppendInterval(0.1f)
                .OnComplete(() =>
                {
                    MoveCardsToPlayerDeck(newCards);
                });
    }



    /// <summary>
    /// Перемещение карт из колоды <see cref="CardDeck"/> в колоду игрока <see cref="GameControlManager"/>
    /// </summary>
    /// <param name="newCards">Новая карта из списка карт</param>
    public void MoveCardsToPlayerDeck(List<RectTransform> newCards)
    {
        layoutGroup.enabled = false;

        var existingCardCount = playerDeck.childCount;
        var cellSize = layoutGroup.cellSize;
        var spacing = layoutGroup.spacing;

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < newCards.Count; i++)
        {
            var newCard = newCards[i];
            var delay = i * duration;

            Vector2 targetLocal = new Vector2(
                (existingCardCount + i) * (cellSize.x + spacing.x),
                0
            );
            Vector3 targetWorld = playerDeck.TransformPoint(targetLocal);

            sequence.Join(
                newCard.DOMove(targetWorld, duration)
                    .SetEase(Ease.OutQuad)
                    .SetDelay(delay)
                    .OnComplete(() =>
                    {
                        newCard.SetParent(playerDeck, false);
                        newCard.localPosition = targetLocal;
                    })
            );

            sequence.Join(
                newCard.DOScale(Vector3.one, duration)
                    .SetEase(Ease.OutQuad)
                    .SetDelay(delay)
            );
        }

        sequence.AppendCallback(() =>
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(playerDeck);
            Canvas.ForceUpdateCanvases();
            layoutGroup.enabled = true;
        });
    }


    private Vector3 GetCardWorldPosition(int index, Vector2 cellSize, Vector2 spacing)
    {
        var localPosition = new Vector2(
            index * (cellSize.x + spacing.x),
            0
        );

        var worldPosition = playerDeck.TransformPoint(localPosition);

        return worldPosition;
    }
}
