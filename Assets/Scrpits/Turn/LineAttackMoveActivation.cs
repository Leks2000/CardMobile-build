using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Utility;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Класс для проверки спорикосается ли <see cref="LineAttackMoveActivation"/> с другими обьектами
/// </summary>
public class LineAttackMoveActivation : MonoBehaviour
{
    public GameObject[] lines;

    private RectTransform rectTransform;
    private EndTurnCamera turnCamera;
    private MeshRenderer mesh;
    private Collider coll;
    public static List<string> Tags = new List<string>() { "Card", "Enemy" };
    public List<MoveForward> moveForwardLines;
    public List<MoveForward> moveBaclkLines;

    private bool attackCompleted = false;

    private void Awake()
    {
        turnCamera = FindObjectOfType<EndTurnCamera>().GetComponent<EndTurnCamera>();
        rectTransform = GetComponent<RectTransform>();
        mesh = GetComponent<MeshRenderer>();
        coll = GetComponent<Collider>();
    }

    /// <summary>
    /// Смена линии по линиям от <see cref="lines"/>
    /// </summary>
    /// <returns></returns>
    public IEnumerator changeLine()
    {
        mesh.enabled = true;
        coll.enabled = true;
        rectTransform.localPosition = new Vector3(0, 0, 5);
        rectTransform.DOScale(new Vector3(125f, 900f, 1), 0.25f);

        GameObject initialLine = lines.FirstOrDefault(line =>
                  line.GetComponentsInChildren<Transform>().Any(child => Tags.Contains(child.tag)));

        if (initialLine != null)
        {
            transform.SetParent(initialLine.transform);
            rectTransform.anchoredPosition = Vector2.zero;
            yield return rectTransform.DOAnchorPos(Vector2.zero, 1f)
                .SetEase(Ease.OutElastic, 0.6f, 1f)
                .WaitForCompletion();
            yield return new WaitUntil(() => attackCompleted);

            foreach (var line in lines)
            {
                if (line == initialLine)
                {
                    continue;
                }

                var hasObjects = line.GetComponentsInChildren<Transform>()
                    .Any(child => Tags.Contains(child.tag));

                if (hasObjects)
                {
                    transform.SetParent(line.transform);
                    yield return rectTransform.DOAnchorPos(Vector2.zero, 1f)
                        .SetEase(Ease.OutElastic, 0.6f, 1f)
                        .WaitForCompletion();
                    yield return new WaitUntil(() => attackCompleted);
                }
            }
        }

        mesh.enabled = false;
        coll.enabled = false;
        rectTransform.localScale = new Vector3(25f, 900f, 1);

        yield return StartCoroutine(moveCards(moveForwardLines, 1f));
        moveForwardLines.Clear();
        StartCoroutine(turnCamera.ChangeRotation());
    }

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
        }
        yield return new WaitForSeconds(escapeTime);
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
    public void OnTriggerEnter(Collider hit)
    {
        if (Tags.Contains(hit.tag))
        {
            var cardForwardAttack = hit.gameObject.GetComponentInParent<CardForwardAttack>();

            cardForwardAttack.HandleErrorIfNullGetComponent<CardForwardAttack, LineAttackMoveActivation>(this, gameObject);
            if (cardForwardAttack != null)
            {
                attackCompleted = false;
                cardForwardAttack.OnAttackComplete += () => attackCompleted = true;
                StartCoroutine(cardForwardAttack.PerformAttack());
            }
            var moveForward = cardForwardAttack.GetComponentInParent<MoveForward>();
            if (moveForward != null && moveForward.isMovingBackLine == false)
            {
                moveForwardLines.Add(moveForward);
            }
            else if (moveForward != null && moveForward.isMovingBackLine == true)
            {
                moveBaclkLines.Add(moveForward);
            }
        }
    }
}
