using UnityEngine;
using UnityEngine.EventSystems;

public class DropCard : MonoBehaviour, IDropHandler
{
    public bool canDrop = true;
    public void OnDrop(PointerEventData eventData)
    {
        var card = eventData.pointerDrag.GetComponent<CardDrag>();
        if (card && canDrop)
        {
            card.GetParent = transform;
            canDrop = false;
        }
    }
}
