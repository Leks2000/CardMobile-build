using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTextAbility;
    [SerializeField] private TextMeshProUGUI tooltipTextInfo;
    [SerializeField] private TextMeshProUGUI tooltipTextStatus;

    private RectTransform tooltipRectTransform;
    private Canvas mainCanvas;
    private RectTransform canvasRectTransform;

    private void Awake()
    {
        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        mainCanvas = FindObjectOfType<Canvas>();
        canvasRectTransform = mainCanvas.GetComponent<RectTransform>();

        tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Показывает тултип с информацией о карте.
    /// </summary>
    public void ShowTooltip(CardData cardData, Transform targetTransform)
    {
        tooltipTextAbility.text = cardData.cardInfo;
        tooltipTextInfo.text = cardData.name;
        tooltipTextStatus.text = $"Dmg {cardData.Damage} / Hp {cardData.Cost}";

        UpdateTooltipSize();
        PositionTooltip(targetTransform);

        tooltipPanel.SetActive(true);
    }

    /// <summary>
    /// Скрывает тултип.
    /// </summary>
    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Обновляет размер тултипа в зависимости от содержания.
    /// </summary>
    private void UpdateTooltipSize()
    {
        tooltipTextAbility.margin = new Vector4(0, 20, 0, 10);
        tooltipTextInfo.margin = new Vector4(0, 20, 0, 20);
        tooltipTextStatus.margin = new Vector4(0, 10, 0, 0);

        float totalHeight = tooltipTextAbility.preferredHeight + tooltipTextInfo.preferredHeight + tooltipTextStatus.preferredHeight + 40;
        float maxWidth = Mathf.Max(tooltipTextAbility.preferredWidth, tooltipTextInfo.preferredWidth, tooltipTextStatus.preferredWidth) + 20;

        tooltipRectTransform.sizeDelta = new Vector2(maxWidth, totalHeight);
    }

    /// <summary>
    /// Позиционирует тултип рядом с картой, корректируя позицию, если тултип выходит за пределы экрана.
    /// </summary>
    private void PositionTooltip(Transform targetTransform)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, targetTransform.position);
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPoint, mainCanvas.worldCamera, out localPoint);

        tooltipRectTransform.localPosition = localPoint;

        Vector3[] corners = new Vector3[4];
        tooltipRectTransform.GetWorldCorners(corners);

        Vector2 tooltipPosition = tooltipRectTransform.localPosition;

        if (corners[2].x > canvasRectTransform.rect.width / 2)
        {
            tooltipPosition.x -= corners[2].x - canvasRectTransform.rect.width / 2;
        }
        if (corners[0].x < -canvasRectTransform.rect.width / 2)
        {
            tooltipPosition.x -= corners[0].x + canvasRectTransform.rect.width / 2;
        }
        if (corners[2].y > canvasRectTransform.rect.height / 2)
        {
            tooltipPosition.y -= corners[2].y - canvasRectTransform.rect.height / 2;
        }
        if (corners[0].y < -canvasRectTransform.rect.height / 2)
        {
            tooltipPosition.y -= corners[0].y + canvasRectTransform.rect.height / 2;
        }

        tooltipRectTransform.localPosition = tooltipPosition;
    }
}
