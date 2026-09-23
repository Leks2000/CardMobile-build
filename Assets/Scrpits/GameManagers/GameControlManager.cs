using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameControlManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private List<GameObject> deck;
    /// <summary>Свободных мест в руке на старте (макс. размер руки = карты в руке на старте + это значение)</summary>
    [SerializeField] private int currentCardsInHand = 5;
    [SerializeField] private CardCostStatus status;
    [SerializeField] private int cardsPerRound = 2;

    private int maxHandSize;
    private HandLayoutController handLayout;

    public RectTransform playerDeck;
    public GridLayoutGroup layoutGroup;
    /// <summary>Задержка между выдачей карт</summary>
    public float duration;
    public RectTransform CDPOS;

    public HandLayoutController HandLayout => handLayout;

    private void Awake()
    {
        CDPOS = cardDeck.GetComponent<RectTransform>();

        handLayout = playerDeck.GetComponent<HandLayoutController>();
        if (handLayout == null)
        {
            handLayout = playerDeck.gameObject.AddComponent<HandLayoutController>();
        }
        maxHandSize = playerDeck.childCount + currentCardsInHand;
    }

    private void Start()
    {
        handLayout.Init(playerDeck, layoutGroup);
    }

    /// <summary>
    /// Свободные места в руке
    /// </summary>
    public int GetCardInHand
    {
        get { return Mathf.Max(0, maxHandSize - playerDeck.childCount); }
    }

    /// <summary>
    /// Начало хода игрока: добор карт из колоды в руку и восстановление маны
    /// </summary>
    public void TurnRound()
    {
        var drawn = 0;
        while (drawn < cardsPerRound && cardDeck.GetTotalCards() > 0 && GetCardInHand > 0 && deck.Count > 0)
        {
            var randomCard = deck[Random.Range(0, deck.Count)];
            var newCard = Instantiate(randomCard, playerDeck);
            var cardRect = newCard.GetComponent<RectTransform>();

            var cardComponent = newCard.GetComponent<Card>();
            if (cardComponent != null)
            {
                cardComponent.UpdateCardDisplay();
            }

            handLayout.AddCardFromDeck(cardRect, CDPOS.position, drawn * duration);
            cardDeck.RemoveCard(1);
            drawn++;
        }
        status.totalMana = 3;
        status.updateText();
        status.setStatusPreparedness();
    }
}
