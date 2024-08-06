using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform defaultParent;
    private bool isPlaced = false;
    RectTransform rectTransform;
    CardManager cardManag;
    CanvasGroup canvasGroup;
    Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardManag = FindObjectOfType<CardManager>().GetComponent<CardManager>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            return;
        }
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;
        defaultParent = transform.parent;
        transform.SetParent(defaultParent.parent);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            return;
        }
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out localPoint);
        rectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (isPlaced)
        {
            return;
        }
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        transform.SetParent(defaultParent);

        rectTransform.pivot = new Vector2(0, 1);

        RectTransform parentRectTransform = defaultParent.GetComponent<RectTransform>();
        if (parentRectTransform != null)
        {
            Vector2 parentSize = parentRectTransform.rect.size;
            Vector2 cardSize = rectTransform.rect.size;

            Vector2 centeredPosition = new Vector2(
                (parentSize.x - cardSize.x) / 2,
                (cardSize.y - parentSize.y) / 2
            );
            rectTransform.anchoredPosition = centeredPosition;
        }
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        transform.localRotation = Quaternion.identity;
        if (defaultParent.CompareTag("Board"))
        {
            isPlaced = true;
            int curCarInHand = cardManag.GetCardInHand;
            cardManag.GetCardInHand = curCarInHand + 1;
        }
    }
}
