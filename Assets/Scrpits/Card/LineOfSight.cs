using UnityEngine;

public class LineOfSight : MonoBehaviour
{
    public float rayDistance; // Дистанция луча
    public Color rayColor = Color.red; // Цвет луча

    void Update()
    {
        // Определяем начальную позицию и направление луча
        Vector3 origin = transform.position;
        Vector3 direction = transform.up;

        // Рисуем луч в редакторе
        Debug.DrawRay(origin, direction * rayDistance, rayColor);

        // Отправляем луч вперед
        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        // Проверяем, что луч пересекся с каким-либо объектом
        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            // Получаем позицию объекта, с которым пересекся луч
            Vector3 hitPosition = hit.point;
            float distanceToHit = Vector3.Distance(origin, hitPosition);

            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Enemy"))
            {
                Debug.Log($"Объект с тегом 'Board' найден впереди на позиции {hitPosition} на расстоянии {distanceToHit}. Тег объекта: {hitObject.tag}");
            }
            else
            {
                Debug.Log("Объект найден, но он не имеет тег 'Board'.");
            }
        }
        else
        {
            Debug.Log("Впереди ничего нет");
        }
    }
}
