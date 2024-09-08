using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    private RectTransform tooltipRectTransform;

    public string tooltipContent = "Описание этого объекта";

    private void Awake()
    {
        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
    }

    private void Start()
    {
        tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(string content, Vector3 position)
    {
        tooltipText.text = content;
        tooltipPanel.SetActive(true);

        UpdateTooltipSize();

        tooltipPanel.transform.position = position;
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void UpdateTooltipSize()
    {
        tooltipRectTransform.sizeDelta = new Vector2(tooltipText.preferredWidth + 20, tooltipText.preferredHeight + 20);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Vector3 tooltipPosition = transform.position + new Vector3(0, 50, 0);  // Смещение над объектом
        ShowTooltip(tooltipContent, tooltipPosition);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }
}
