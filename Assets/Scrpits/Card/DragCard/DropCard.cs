using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Место куда можно скидывать карты игрока
/// </summary>
public class DropCard : MonoBehaviour, IDropHandler
{
    public bool canDrop = true;

    /// <summary>
    /// Метод проверяющий возможность скинуть карту
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }
        var card = eventData.pointerDrag.GetComponent<CardDrag>();
        // Слот свободен и карта реально перетаскивается из руки (не уже выложенная)
        if (card && card.IsDragging && !card.isPlaced && canDrop && GetComponentInChildren<Card>() == null)
        {
            // Перемещение в слот выполняет CardDrag.OnEndDrag (в правильном пространстве координат)
            card.GetParent = transform;
            canDrop = false;
        }
    }
}
