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
    [SerializeField] private float delayCam;
    public GameObject[] lines;

    private RectTransform rectTransform;
    private EndTurnCamera turnCamera;
    private MoveActivation moveActivation;
    private MeshRenderer mesh;
    private Collider coll;
    public static List<string> Tags = new List<string>() { "Card", "Enemy" };
    public List<MoveForward> moveForwardLines;

    private bool attackCompleted = false;

    private void Awake()
    {
        turnCamera = FindObjectOfType<EndTurnCamera>().GetComponent<EndTurnCamera>();
        moveActivation = new MoveActivation();
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

        yield return StartCoroutine(moveActivation.moveCards(moveForwardLines, 0.25f));
        yield return new WaitForSeconds(delayCam);
        yield return StartCoroutine(turnCamera.ChangeRotation());
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
        }
    }
}
