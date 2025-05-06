using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Перемещение карт
/// </summary>
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Card cardData;
    [SerializeField] private CardCostStatus status;
    private GameControlManager cardManag;

    public bool isPlaced = false;
    public static bool IsDraggingAnyCard = false;

    private Vector3 originalPosition;
    private Transform mapTrans;
    private Transform defaultParent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    public float liftHeight;
    public float liftDuration;
    public float moveDuration;

    public static List<string> Tags = new List<string>() { "Board", "Player" };

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        mapTrans = GameObject.FindGameObjectWithTag("Deck").GetComponent<Transform>();
        cardManag = FindObjectOfType<GameControlManager>().GetComponent<GameControlManager>();
        defaultParent = cardManag.gameObject.transform.parent;
        originalPosition = transform.localPosition;
        status = cardData.status;
        ResetCard();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPlaced && !IsDraggingAnyCard && transform.parent.tag == "PlayerDeck")
        {
            rectTransform.DOLocalMoveY(originalPosition.y + liftHeight, liftDuration).SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPlaced && !IsDraggingAnyCard && transform.parent.tag == "PlayerDeck")
        {
            rectTransform.DOLocalMoveY(originalPosition.y, liftDuration).SetEase(Ease.InQuad);
        }
    }

    public Transform GetParent
    {
        set { defaultParent = value; }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        DOTween.Clear();

        if (isPlaced)
        {
            return;
        }
        IsDraggingAnyCard = true;

        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        defaultParent = transform.parent;
        transform.SetParent(defaultParent.parent);

        rectTransform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InOutCubic);

        rectTransform.localPosition = mapTrans.localPosition;
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

        IsDraggingAnyCard = false;
        if (Tags.Contains(defaultParent.tag))
        {
            var targetRect = defaultParent.GetComponent<RectTransform>();
            rectTransform.DOLocalMove(targetRect.rect.center, moveDuration)
                .SetEase(Ease.InOutCubic)
                .OnComplete(() =>
                {
                    ResetCard();
                    var curCarInHand = cardManag.GetCardInHand;
                    cardManag.GetCardInHand = curCarInHand + 1;
                    isPlaced = true;
                    status.returnManaText(gameObject);
                });
        }
        else
        {
            ResetCard();
        }
    }
    /// <summary>
    /// Обнуление позиции карт по центру
    /// </summary>
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
