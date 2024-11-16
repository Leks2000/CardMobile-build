using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipTextAbility;
    public TextMeshProUGUI tooltipTextInfo;
    public TextMeshProUGUI tooltipTextStatus;
    private bool isDragging = false;

    private RectTransform tooltipRectTransform;
    private Canvas mainCanvas;
    private RectTransform playerUITransform;

    private void Awake()
    {
        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        mainCanvas = FindObjectOfType<Canvas>();

        tooltipPanel.SetActive(false);

        // НайдитеRectTransform UI игрока
        playerUITransform = GameObject.FindWithTag("PlayerUI").GetComponent<RectTransform>();
    }

    public void ShowTooltip(CardData cardData, Transform targetTransform)
    {
        tooltipTextAbility.text = cardData.cardInfo;
        tooltipTextInfo.text = cardData.name;
        tooltipTextStatus.text = $"Dmg {cardData.Damage} / Hp {cardData.Cost}";

        UpdateTooltipSize();

        tooltipPanel.SetActive(true);

        Vector2 localPosition = playerUITransform.InverseTransformPoint(targetTransform.position);
        localPosition.x -= targetTransform.GetComponent<RectTransform>().rect.width / 2;
        localPosition.y += targetTransform.GetComponent<RectTransform>().rect.height / 2;

        tooltipRectTransform.localPosition = localPosition;
    }
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
        tooltipPanel.transform.SetParent(mainCanvas.transform, false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging)
        {
            return;
        }

        var card = eventData.pointerEnter.transform;
        if (card.CompareTag("Card") || card.CompareTag("Enemy"))
        {
            UpdateCardSize(eventData);
            CardData cardData = card.GetComponentInChildren<Card>().CardData;
            ShowTooltip(cardData, eventData.pointerEnter.transform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    public void UpdateCardSize(PointerEventData eventData)
    {
        Debug.Log(eventData.pointerEnter.transform.parent.parent.name);
        if (eventData.pointerEnter.transform.parent.parent.name.Contains("Line"))
        {
        }
        else
        {
            tooltipPanel.transform.rotation = eventData.pointerEnter.transform.rotation;
        }
    }

    private void SetCardSize(Vector2 size)
    {
        tooltipRectTransform.sizeDelta = size;
    }

    private void UpdateTooltipSize()
    {
        tooltipTextAbility.margin = new Vector4(0, 30, 0, 10);
        tooltipTextInfo.margin = new Vector4(0, 30, 0, 30);
        tooltipTextStatus.margin = new Vector4(0, 10, 0, 0);

        float totalHeight = tooltipTextAbility.preferredHeight + tooltipTextInfo.preferredHeight + tooltipTextStatus.preferredHeight + 30; // отступы между текстами
        tooltipRectTransform.sizeDelta = new Vector2(tooltipTextAbility.preferredWidth + 20, totalHeight);

        TMP_Text[] texts = { tooltipTextAbility, tooltipTextInfo, tooltipTextStatus };
        foreach (TMP_Text text in texts)
        {
            text.fontSizeMin = 12;
            text.fontSizeMax = 24;
            text.autoSizeTextContainer = true;
        }
    }

    public void StartDragging()
    {
        isDragging = true;
        HideTooltip();
    }

    public void StopDragging()
    {
        isDragging = false;
    }
}
