using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Передвижение карт вперёд
/// </summary>
public class MoveForward : MonoBehaviour
{
    public bool isMovingBackLine = false;
    public static List<string> Tags = new List<string>() { "Enemy", "Card" };

    /// <summary>
    /// Проверка что находиться впереди
    /// </summary>
    public void GetPath()
    {
        var card = GetComponentInChildren<Card>().GetComponent<RectTransform>();

        if (!Physics.Raycast(transform.position, transform.up, out var hit, 32))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (!Tags.Contains(hit.collider.tag))
        {
            StartCoroutine(GetCard(hit.transform, card));
            return;
        }
        Debug.Log("Объект перед картой не является следующим местом: " + hit.collider.tag);
    }

    /// <summary>
    /// Узнать размещён ли обьект впереди у <see cref="DropCard"/> если нету то анимация перемещения
    /// </summary>
    private IEnumerator GetCard(Transform targetTransform, RectTransform card)
    {
        card.transform.SetParent(targetTransform);
        card.DOMove(targetTransform.GetComponent<RectTransform>().position, 0.5f)
                   .SetEase(Ease.OutQuad)
                   .WaitForCompletion();

        if (GetComponent<DropCard>())
        {
            GetComponent<DropCard>().canDrop = true;
        }
        yield return null;
    }
}
