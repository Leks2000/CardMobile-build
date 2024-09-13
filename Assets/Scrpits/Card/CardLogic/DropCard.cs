using DG.Tweening;
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

            RectTransform targetRect = transform as RectTransform;
            card.transform.DOLocalMove(targetRect.rect.center, card.moveDuration)
                .SetEase(Ease.InOutCubic);
        }
    }
}
