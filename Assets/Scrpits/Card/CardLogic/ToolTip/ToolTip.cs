using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Linq;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipTextAbility;
    [SerializeField] private TextMeshProUGUI tooltipTextInfo;
    [SerializeField] private TextMeshProUGUI tooltipTextStatus;

    private RectTransform tooltipRectTransform;
    private Canvas mainCanvas;
    private RectTransform canvasRectTransform;
    private Coroutine hideCoroutine;
    private Vector3 toolScale;
    private Tween currentTween;



    private void Awake()
    {
        tooltipRectTransform = tooltipPanel.GetComponent<RectTransform>();
        mainCanvas = FindObjectsOfType<Canvas>().FirstOrDefault(c => c.sortingLayerName == "Game");
        canvasRectTransform = mainCanvas.GetComponent<RectTransform>();

        toolScale = tooltipRectTransform.localScale;

        tooltipPanel.SetActive(false);
    }

    public void ShowTooltip(CardData cardData, Transform targetTransform)
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        currentTween?.Kill();

        tooltipTextAbility.text = cardData.cardInfo;
        tooltipTextInfo.text = cardData.name;
        tooltipTextStatus.text = $"Dmg {cardData.Damage} / Hp {cardData.Cost}";

        PositionTooltip(targetTransform);

        tooltipPanel.SetActive(true);
        tooltipRectTransform.localScale = Vector3.zero;

        currentTween = tooltipRectTransform.DOScale(toolScale, 0.25f).SetEase(Ease.OutBack);
    }

    public void HideTooltip()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine = StartCoroutine(HideTooltipDelayed());
    }

    private IEnumerator HideTooltipDelayed()
    {
        yield return new WaitForSeconds(0.1f);

        currentTween?.Kill();

        currentTween = tooltipRectTransform.DOScale(Vector3.zero, 0.15f).OnComplete(() =>
        {
            tooltipPanel.SetActive(false);
        });
    }


    /// <summary>
    /// Позиционирует тултип рядом с картой, корректируя позицию, если тултип выходит за пределы экрана.
    /// </summary>
    private void PositionTooltip(Transform targetTransform)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, targetTransform.position);
        Vector2 tooltipScreenPosition;
        Vector2 pivot;

        Vector2 tooltipSize = tooltipRectTransform.sizeDelta * tooltipRectTransform.lossyScale;

        if (screenPoint.y > Screen.height * 0.9f)
        {
            tooltipScreenPosition = screenPoint - new Vector2(0f, 100f);
            pivot = new Vector2(1.5f, 1f);
        }
        else if (screenPoint.y < Screen.height * 0.2f)
        {
            tooltipScreenPosition = screenPoint - new Vector2(50f, 0f);
            pivot = new Vector2(1f, 0.25f);
        }
        else if (screenPoint.y > Screen.height * 0.8f)
        {
            tooltipScreenPosition = screenPoint - new Vector2(50f, 0f);
            pivot = new Vector2(1f, 0.75f);
        }
        else
        {
            tooltipScreenPosition = screenPoint - new Vector2(50f, 0f);
            pivot = new Vector2(1f, 0.5f);
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, tooltipScreenPosition, mainCanvas.worldCamera, out Vector2 localPoint);

        tooltipRectTransform.pivot = pivot;
        tooltipRectTransform.localPosition = localPoint;
        tooltipRectTransform.localRotation = Quaternion.identity;
    }

}
