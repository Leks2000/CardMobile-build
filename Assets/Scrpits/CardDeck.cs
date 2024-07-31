
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;

public class CardDeck : MonoBehaviour
{
    [SerializeField] private int totalCard;
    private TextMeshProUGUI Deck;

    private void Start()
    {
        Deck = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        Deck.text = totalCard.ToString();
    }
    public int GetTotalCards()
    {
        return totalCard;
    }
    public void RemoveCard(int count)
    {
        totalCard = Mathf.Max(0, totalCard - count);
        Deck.text = totalCard.ToString();
    }
}