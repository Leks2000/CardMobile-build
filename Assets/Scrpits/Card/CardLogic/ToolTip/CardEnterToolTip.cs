using UnityEngine;
using UnityEngine.EventSystems;

public class CardEnterToolTip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Card cardData;

    public void OnPointerEnter(PointerEventData eventData)
    {
        FindObjectOfType<Tooltip>().ShowTooltip(cardData.CardData, transform);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        FindObjectOfType<Tooltip>().HideTooltip();
    }
}
