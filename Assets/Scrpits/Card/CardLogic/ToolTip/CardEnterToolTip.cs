using UnityEngine;
using UnityEngine.EventSystems;

public class CardEnterToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Card cardData;

    private bool isPointerOver = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        if (!GlobalDragTracker.IsDraggingCard)
        {
            FindObjectOfType<Tooltip>().ShowTooltip(cardData.CardData, transform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        FindObjectOfType<Tooltip>().HideTooltip();
    }
}
