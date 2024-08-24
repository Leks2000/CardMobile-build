using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Utility;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

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
    public IEnumerator changeLine()
    {
        mesh.enabled = true;
        coll.enabled = true;
        rectTransform.localPosition = new Vector3(0, 0, 5);
        rectTransform.DOScale(new Vector3(125f, 900f, 1), 0.25f);
        transform.SetParent(lines[0].transform);
        rectTransform.anchoredPosition = Vector2.zero;

        for (var index = 0; index < lines.Length; index++)
        {
            transform.SetParent(lines[index].transform);
            yield return rectTransform.DOAnchorPos(Vector2.zero, 1f)
                .SetEase(Ease.OutElastic, 0.6f, 1f)
                .WaitForCompletion();
            yield return new WaitUntil(() => attackCompleted);
        }
        mesh.enabled = false;
        coll.enabled = false;
        rectTransform.localScale = new Vector3(25f, 900f, 1);

        yield return StartCoroutine(moveCards(moveForwardLines, 1f));
        StartCoroutine(turnCamera.ChangeRotation());
    }

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
            yield return new WaitForSeconds(0.5f);
            moveForward.GetPath();
        }
        yield return new WaitForSeconds(escapeTime);
    }
    public static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
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
            //moveForward.HandleErrorIfNullGetComponent<MoveForward, LineAttack>(this, gameObject);
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
