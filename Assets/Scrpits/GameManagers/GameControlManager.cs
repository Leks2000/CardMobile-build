using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Рука и колода боя. [D] Колода = RunState.Deck (вне забега - CardDatabase.StarterDeck),
/// перемешанная стопка добора; когда стопка пуста - колода перемешивается заново (тупика нет).
/// </summary>
public class GameControlManager : MonoBehaviour
{
    [SerializeField] private CardDeck cardDeck;
    [SerializeField] private CardCostStatus status;
    [SerializeField] private int cardsPerRound = 2;
    [SerializeField] private int startingHand = 4;
    [SerializeField] private int handLimit = 7;

    private HandLayoutController handLayout;
    private readonly List<CardData> deckList = new List<CardData>();
    private readonly List<CardData> drawPile = new List<CardData>();

    public RectTransform playerDeck;
    public GridLayoutGroup layoutGroup;
    /// <summary>Задержка между выдачей карт</summary>
    public float duration;
    public RectTransform CDPOS;

    public HandLayoutController HandLayout => handLayout;
    public int DrawPileCount => drawPile.Count;

    private void Awake()
    {
        CDPOS = cardDeck.GetComponent<RectTransform>();

        handLayout = playerDeck.GetComponent<HandLayoutController>();
        if (handLayout == null)
        {
            handLayout = playerDeck.gameObject.AddComponent<HandLayoutController>();
        }
    }

    private void Start()
    {
        // Карты-заглушки из сцены убираем: рука строится из колоды забега
        for (var i = playerDeck.childCount - 1; i >= 0; i--)
        {
            var placeholder = playerDeck.GetChild(i);
            placeholder.SetParent(null, false);
            Destroy(placeholder.gameObject);
        }
        handLayout.Init(playerDeck, layoutGroup);

        deckList.Clear();
        deckList.AddRange(Assets.Scrpits.Run.RunState.IsActive && Assets.Scrpits.Run.RunState.Deck.Count > 0
            ? Assets.Scrpits.Run.RunState.Deck
            : CardDatabase.StarterDeck);
        Reshuffle();
        Debug.Log($"[D] Battle deck: {deckList.Count} cards");

        DrawCards(startingHand);
        RefillMana();
    }

    /// <summary>
    /// Свободные места в руке
    /// </summary>
    public int GetCardInHand
    {
        get { return Mathf.Max(0, handLimit - playerDeck.childCount); }
    }

    /// <summary>
    /// Начало хода игрока: добор карт из колоды в руку и восстановление маны
    /// </summary>
    public void TurnRound()
    {
        RelicSystem.OnRoundStart();
        DrawCards(cardsPerRound);
        RefillMana();
    }

    private void DrawCards(int count)
    {
        for (var drawn = 0; drawn < count && GetCardInHand > 0; drawn++)
        {
            if (drawPile.Count == 0)
            {
                Reshuffle();
                if (drawPile.Count == 0) break;
            }
            var data = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);

            var card = CardFactory.Spawn(data, playerDeck);
            handLayout.AddCardFromDeck((RectTransform)card.transform, CDPOS.position, drawn * duration);
        }
        cardDeck.SetCount(drawPile.Count);
    }

    private void Reshuffle()
    {
        drawPile.Clear();
        drawPile.AddRange(deckList);
        MoveActivation.Shuffle(drawPile);
        cardDeck.SetCount(drawPile.Count);
    }

    private void RefillMana()
    {
        status.totalMana = status.maxMana;
        status.updateText();
        status.setStatusPreparedness();
    }
}
