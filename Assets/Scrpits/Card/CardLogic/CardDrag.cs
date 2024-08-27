using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Transform mapTrans;
    private Transform defaultParent;
    public bool isPlaced = false;
    private RectTransform rectTransform;
    private CardManager cardManag;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    public static List<string> Tags = new List<string>() { "Board", "Player" };


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        cardManag = FindObjectOfType<CardManager>().GetComponent<CardManager>();
        mapTrans = GameObject.FindGameObjectWithTag("Deck").GetComponent<Transform>();
        defaultParent = cardManag.gameObject.transform.parent;
        ResetCard();
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

        rectTransform.localPosition = mapTrans.localPosition;
        rectTransform.localRotation = Quaternion.identity;
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

        if (Tags.Contains(defaultParent.tag))
        {
            var curCarInHand = cardManag.GetCardInHand;
            cardManag.GetCardInHand = curCarInHand + 1;
            isPlaced = true;
        }
        ResetCard();
    }
    private void ResetCard()
    {
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0);
        rectTransform.localRotation = Quaternion.identity;
    }
}
