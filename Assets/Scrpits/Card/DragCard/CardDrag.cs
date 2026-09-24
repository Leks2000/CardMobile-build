using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Перетаскивание карт из руки на поле
/// </summary>
public class CardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Card cardData;
    [SerializeField] private CardCostStatus status;
    private GameControlManager cardManag;

    public bool isPlaced = false;
    public static bool IsDraggingAnyCard = false;

    private bool isDragging;
    private int handIndex;
    private Transform defaultParent;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;

    public float liftHeight;
    public float liftDuration;
    public float moveDuration;

    public static List<string> Tags = new List<string>() { "Board", "Player" };

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        canvas = GetComponentInParent<Canvas>();
        cardManag = FindAnyObjectByType<GameControlManager>();
        defaultParent = transform.parent;
        status = cardData.status;
    }

    private bool IsInHand => cardManag != null && transform.parent == cardManag.playerDeck;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isPlaced && !IsDraggingAnyCard && IsInHand)
        {
            cardManag.HandLayout.SetHover(rectTransform, true, liftHeight, liftDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPlaced && !IsDraggingAnyCard && IsInHand)
        {
            cardManag.HandLayout.SetHover(rectTransform, false, liftHeight, liftDuration);
        }
    }

    public Transform GetParent
    {
        set { defaultParent = value; }
    }

    public bool IsDragging => isDragging;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isPlaced || !IsInHand)
        {
            return;
        }
        GlobalDragTracker.BeginDrag();
        var tooltip = FindAnyObjectByType<Tooltip>();
        if (tooltip != null)
        {
            tooltip.HideTooltip();
        }
        isDragging = true;
        IsDraggingAnyCard = true;
        canvasGroup.alpha = 0.8f;
        canvasGroup.blocksRaycasts = false;

        cardManag.HandLayout.KillMove(rectTransform);
        rectTransform.DOKill();
        rectTransform.localScale = Vector3.one;
        handIndex = transform.GetSiblingIndex();
        defaultParent = transform.parent;
        transform.SetParent(defaultParent.parent, true);
        cardManag.HandLayout.Relayout(false);

        rectTransform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InOutCubic);
        rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y, 0f);
        FollowPointer(eventData);
        PulseEffectManager.ShowPlacementPulses();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }
        FollowPointer(eventData);
    }

    private void FollowPointer(PointerEventData eventData)
    {
        // пальцем: карта чуть выше точки касания, чтобы палец не закрывал её (цель броска - под пальцем)
        var pos = eventData.position;
        if (eventData.pointerId >= 0) pos.y += Screen.height * 0.06f;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, pos, canvas.worldCamera, out var localPoint);
        rectTransform.anchoredPosition = localPoint;
    }

    private int releasedFrames;

    /// <summary>
    /// Страховка: касание пропало без OnEndDrag (свернули приложение, системный жест) - карта не должна
    /// «залипнуть» в перетаскивании (иначе блокируются предметы и подсветка зон не гаснет).
    /// </summary>
    private void Update()
    {
        if (!isDragging) { releasedFrames = 0; return; }
        bool pressed = Input.touchCount > 0 || Input.GetMouseButton(0);
        releasedFrames = pressed ? 0 : releasedFrames + 1;
        if (releasedFrames >= 3) OnEndDrag(null);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && isDragging) OnEndDrag(null);
    }

    private void OnDisable()
    {
        if (isDragging) { isDragging = false; IsDraggingAnyCard = false; GlobalDragTracker.EndDrag(); PulseEffectManager.HideAllPlacementPulses(); }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
        {
            return;
        }
        GlobalDragTracker.EndDrag();
        isDragging = false;
        IsDraggingAnyCard = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        PulseEffectManager.HideAllPlacementPulses();

        if (defaultParent != null && defaultParent != cardManag.playerDeck && Tags.Contains(defaultParent.tag))
        {
            // Карта положена в слот поля (DropCard назначил defaultParent)
            transform.SetParent(defaultParent, true);
            isPlaced = true;
            status.returnManaText(gameObject);
            RelicSystem.OnCardPlayed(cardData);
            CardAbilities.OnPlayed(cardData);
            SoundFx.Play(SoundFx.Clip.PlayCard);

            var targetRect = defaultParent.GetComponent<RectTransform>();
            rectTransform.DOKill();
            rectTransform.DOLocalMove(targetRect.rect.center, moveDuration)
                .SetEase(Ease.InOutCubic)
                .OnComplete(ResetCard);
        }
        else
        {
            // Некуда положить - возвращаем в руку на своё место
            defaultParent = cardManag.playerDeck;
            cardManag.HandLayout.ReturnCard(rectTransform, handIndex);
        }
    }

    /// <summary>
    /// Центрирует карту в слоте поля
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
