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
        deck = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        deck.text = $"x{totalCard}";
    }
    public int GetTotalCards()
    {
        return totalCard;
    }
    public void RemoveCard(int count)
    {
        totalCard = Mathf.Max(totalCard - count);
        deck.text = $"x{totalCard}";
    }
}
