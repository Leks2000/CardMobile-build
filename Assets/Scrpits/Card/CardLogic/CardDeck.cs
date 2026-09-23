using TMPro;
using UnityEngine;

/// <summary>
/// Колода игрока
/// </summary>
public class CardDeck : MonoBehaviour
{
    [SerializeField] private int totalCard;
    private TextMeshProUGUI deck;

    private void Start()
    {
        deck = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        deck.text = $"x{totalCard}";
    }
    public int GetTotalCards()
    {
        return totalCard;
    }
    public void RemoveCard(int count)
    {
        totalCard = Mathf.Max(0, totalCard - count);
        if (deck != null)
        {
            deck.text = $"x{totalCard}";
        }
    }
}
