using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
    public float raycastDistance = 32;
    public Color rayColor = Color.red;
    public static List<string> Tags = new List<string>() { "Enemy", "Card" };
    public void GetPath()
    {
        var direction = transform.up;

        Debug.DrawRay(transform.position, direction * raycastDistance, rayColor);

        var card = GetComponentInChildren<Card>().GetComponent<RectTransform>();

        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (!Tags.Contains(hit.collider.tag))
        {
            GetCard(hit.transform, card);
            return;
        }
        Debug.Log("Объект перед картой не является следующим местом: " + hit.collider.tag);
    }
    private void GetCard(Transform transform, RectTransform card)
    {
        card.transform.SetParent(transform);
        card.DOMove(transform.GetComponent<RectTransform>().position, 1f).SetEase(Ease.OutQuad);
        if (GetComponent<DropCard>())
        {
            GetComponent<DropCard>().canDrop = true;
        }
    }
}
