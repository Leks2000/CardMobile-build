using UnityEngine;

public class CardAttack : MonoBehaviour
{
    public float attackRange;
    public float raycastDistance;

    public void PerformAttack()
    {
        Vector3 direction = transform.up;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, raycastDistance))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("Враг найден: " + hit.collider.tag);
            }
            else
            {
                Debug.Log("Объект перед картой не является врагом: " + hit.collider.tag);
            }
        }
        else
        {
            Debug.Log("Впереди ничего нет.");
        }
    }
}
