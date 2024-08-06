using UnityEngine;

public class CardAttack : MonoBehaviour
{
    public void PerformAttack()
    {
        var direction = transform.up;
        var raycastDistance = 100f;

        if (!Physics.Raycast(transform.position, direction, out var hit, raycastDistance))
        {
            Debug.Log("Впереди ничего нет.");
            return;
        }
        if (!hit.collider.CompareTag("Enemy"))
        {
            Debug.Log("Объект перед картой не является врагом: " + hit.collider.tag);
            return;
        }
        Debug.Log("Враг найден: " + hit.collider.tag);
    }
}
