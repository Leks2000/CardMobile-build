using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform defaultParent;
    private bool isPlaced = false;
    private RectTransform rectTransform;
    private CardManager cardManag;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardManag = FindObjectOfType<CardManager>().GetComponent<CardManager>();
        defaultParent = cardManag.gameObject.transform.parent;
        NoramPos();
    }
    public Transform GetParent
    {
        set { defaultParent = value; }
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out var localPoint);
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

        if (defaultParent.CompareTag("Board"))
        {

            var parentRectTransform = defaultParent.GetComponent<RectTransform>();
            if (parentRectTransform != null)
            {
                var parentSize = parentRectTransform.rect.size;
                var cardSize = rectTransform.rect.size;

                var centeredPosition = new Vector2(
                    (parentSize.x - cardSize.x) / 2,
                    (cardSize.y - parentSize.y) / 2
                );
                rectTransform.anchoredPosition = centeredPosition;
            }

            NoramPos();
            isPlaced = true;
            var curCarInHand = cardManag.GetCardInHand;
            cardManag.GetCardInHand = curCarInHand + 1;
        }
    }
    private void NoramPos()
    {
        rectTransform.pivot = new Vector2(0, 1);
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
        transform.localRotation = Quaternion.identity;
    }
}
