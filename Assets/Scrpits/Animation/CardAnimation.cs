using System.Collections;
using DG.Tweening;
using UnityEngine;

public class CardAnimation : MonoBehaviour
{
    public void MoveCardToPlayerDeck(GameObject card, Transform playerDeck)
    {
        if (playerDeck.childCount < 5)
        {
            Vector3 targetPosition = playerDeck.position;

            // Сохраняем глобальные координаты карты
            Vector3 originalPosition = card.transform.position;

            card.transform.SetParent(null); // Отсоединяем карту от текущего родителя
            card.transform.position = originalPosition; // Восстанавливаем глобальные координаты

            // Анимация перемещения карты
            card.transform.DOJump(targetPosition, 2f, 1, 0.5f).OnComplete(() =>
            {
                card.transform.SetParent(playerDeck); // Назначаем карту новым ребенком
                card.transform.localPosition = Vector3.zero; // Центрируем внутри нового родителя
            });
        }
        else
        {
            Debug.Log("Колода игрока заполнена.");
        }
    }
}
