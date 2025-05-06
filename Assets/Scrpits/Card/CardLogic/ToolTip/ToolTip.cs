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

        PositionTooltip(targetTransform, 0);

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

    /// <summary>
    /// Позиционирует тултип рядом с картой, корректируя позицию, если тултип выходит за пределы экрана.
    /// </summary>
    private void PositionTooltip(Transform targetTransform, float angle)
    {
        // Смещение тултипа относительно карты (сдвиг влево)
        Vector2 offset = new Vector2(0, 0);  // Сдвиг влево на 50 пикселей

        // Преобразуем позицию целевого объекта в экранные координаты
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, targetTransform.position);

        // Переводим экранные координаты в локальные координаты холста
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPoint, mainCanvas.worldCamera, out var localPoint);

        tooltipRectTransform.localPosition = localPoint;

        // Применяем угол наклона (вращение тултипа)
        tooltipRectTransform.localRotation = Quaternion.Euler(angle, 0, 0);

        // Корректируем позицию, если тултип выходит за границы экрана
        var corners = new Vector3[4];
        tooltipRectTransform.GetWorldCorners(corners);

        Vector2 tooltipPosition = tooltipRectTransform.localPosition;

        // Корректировка по границам экрана
        if (corners[2].x > canvasRectTransform.rect.width / 2) // Правая граница
        {
            tooltipPosition.x -= corners[2].x - canvasRectTransform.rect.width / 2;
        }
        if (corners[0].x < -canvasRectTransform.rect.width / 2) // Левая граница
        {
            tooltipPosition.x -= corners[0].x + canvasRectTransform.rect.width / 2;
        }
        if (corners[2].y > canvasRectTransform.rect.height / 2) // Верхняя граница
        {
            tooltipPosition.y -= corners[2].y - canvasRectTransform.rect.height / 2;
        }
        if (corners[0].y < -canvasRectTransform.rect.height / 2) // Нижняя граница
        {
            tooltipPosition.y -= corners[0].y + canvasRectTransform.rect.height / 2;
        }

        tooltipRectTransform.localPosition = tooltipPosition;
    }
}
