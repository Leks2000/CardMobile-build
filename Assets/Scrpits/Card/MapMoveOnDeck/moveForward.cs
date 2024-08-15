using DG.Tweening;
using UnityEngine;

public class MoveForward : MonoBehaviour
{
    public float raycastDistance = 32; // Расстояние луча
    public Color rayColor = Color.red;  // Цвет луча

    public void GetPath()
    {
        var direction = transform.up;

        Debug.DrawRay(transform.position, direction * raycastDistance, rayColor);

        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (!hit.collider.CompareTag("Board"))
        {
            Debug.Log("Объект перед картой не является следующим местом: " + hit.collider.tag);
            return;
        }
        GetCard(hit.transform);
    }
    private void GetCard(Transform transform)
    {
        var card = GetComponentInChildren<Card>().GetComponent<RectTransform>();
        card.transform.SetParent(transform);
        card.DOMove(transform.GetComponent<RectTransform>().position, 1f).SetEase(Ease.OutQuad);
        if (GetComponent<DropCard>())
        {
            GetComponent<DropCard>().canDrop = true;
        }
    }
}
