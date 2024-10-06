using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPanel;

    public TextMeshProUGUI tooltipText;
    private RectTransform tooltipRectTransform;
    private Canvas mainCanvas;

    public string tooltipContent = "Описание этого объекта";

    private void Awake()
    {
        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        mainCanvas = FindObjectOfType<Canvas>();

        // Установите фиксированный размер для тултипа
        tooltipRectTransform.sizeDelta = new Vector2(100, 50); // Пример фиксированного размера
    }

    private void Start()
    {
        tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(string content, Transform newParent)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);

        UpdateTooltipSize();

        // Позиционируем тултип рядом с объектом
        Vector3 tooltipPosition = newParent.position; // Получаем позицию объекта
        tooltipPosition.y += newParent.GetComponent<RectTransform>().rect.height / 2 + tooltipRectTransform.rect.height / 2 + 10; // Отступ
        tooltipPosition.x += 0; // Если нужно, можно добавить отступ по оси X

        tooltipPanel.transform.position = tooltipPosition;
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void UpdateTooltipSize()
    {
        // Здесь можно дополнительно настраивать размер в зависимости от содержимого
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Card card = GetComponentInChildren<Card>();
        tooltipContent = card.GetTooltipContent();
        ShowTooltip(tooltipContent, transform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }
}
