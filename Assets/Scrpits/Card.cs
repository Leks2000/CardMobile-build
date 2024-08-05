using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    RectTransform rectTransform;
    Canvas canvas;
    Vector2 initialPosition;
    public Transform defaultParent;
    CanvasGroup canvasGroup;
    Bounds bounds;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        initialPosition = rectTransform.anchoredPosition;
        defaultParent = transform.parent;
        transform.SetParent(defaultParent.parent);
        GetComponent<CanvasGroup>().blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out localPoint);
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(defaultParent);
        RectTransform parentRectTransform = defaultParent.GetComponent<RectTransform>();
        if (parentRectTransform != null)
        {
            Vector2 parentSize = parentRectTransform.rect.size;
            Vector2 cardSize = rectTransform.rect.size;

            Vector2 centeredPosition = new Vector2(
                (parentSize.x - cardSize.x) / 2,
                (parentSize.y - cardSize.y) / 2
            );
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = centeredPosition;

            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            transform.localRotation = Quaternion.identity;
        }

    }
}
