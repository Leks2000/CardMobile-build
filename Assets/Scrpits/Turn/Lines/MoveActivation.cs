using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Класс для активации движения карт <see cref="MoveActivation"/>
/// </summary>
public class MoveActivation
{
    /// <summary>
    /// Передвижение всех карт в рандомном порядке
    /// </summary>
    /// <param name="moveCards">Рандомная карта</param>
    /// <param name="escapeTime">Время выхода из <see cref="moveCards"/></param>
    /// <returns></returns>
    public IEnumerator moveCards(List<MoveForward> moveCards, float escapeTime)
    {
        Shuffle(moveCards);
        foreach (var moveForward in moveCards.ToList())
        {
            if (moveForward.GetComponentInChildren<Card>() == null)
            {
                moveCards.Remove(moveForward);
                continue;
            }
            yield return new WaitForSeconds(0.1f);
            moveForward.GetPath();

            yield return new WaitForSeconds(escapeTime);
        }
        moveCards.Clear();
    }

    /// <summary>
    /// Перемешивает элементы в списке случайным образом.
    /// </summary>
    /// <typeparam name="T">Тип элементов в списке.</typeparam>
    /// <param name="list">Список, который нужно перемешать.</param>
    /// <remarks>
    /// Алгоритм перемешивания основан на алгоритме Фишера-Йетса.
    /// Каждый элемент списка меняется местами с произвольным элементом,
    /// расположенным перед ним или на его месте.
    /// </remarks>
    public static void Shuffle<T>(IList<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
