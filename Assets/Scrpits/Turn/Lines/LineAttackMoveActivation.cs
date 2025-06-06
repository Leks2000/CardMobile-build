using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Utility;
using DG.Tweening;
using DG.Tweening.Core.Easing;
using UnityEngine;

public class LineAttackMoveActivation : MonoBehaviour
{
    [SerializeField] private float delayCam;
    [SerializeField] private Canvas uiControlCV;
    public GameObject[] lines;

    private RectTransform rectTransform;
    private EndTurnCamera turnCamera;
    private MoveActivation moveActivation;
    private MeshRenderer mesh;
    private Collider coll;
    public static List<string> Tags = new List<string>() { "Card", "Enemy" };
    public List<MoveForward> moveForwardLines;

    private bool attackCompleted = false;
    private int finishedAttacks = 0;
    private int totalAttackers = 0;

    [SerializeField] private Boss boss;
    [SerializeField] private Player player;
    [SerializeField] private GameManagerOver gameManager;

    private void Awake()
    {
        turnCamera = FindObjectOfType<EndTurnCamera>();
        moveActivation = new MoveActivation();
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

        totalAttackers = 0;
        finishedAttacks = 0;

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

        rectTransform.DOLocalMoveZ(rectTransform.localPosition.z + 100, 0.1f).SetEase(Ease.Linear);
        yield return new WaitForSeconds(0.1f);

        mesh.enabled = false;
        coll.enabled = false;
        rectTransform.localScale = new Vector3(25f, 900f, 1);

        yield return new WaitUntil(() => finishedAttacks >= totalAttackers);

        if (boss.IsDefeated())
        {
            yield return StartCoroutine(turnCamera.ReturnToInitialPosition());
            uiControlCV.renderMode = RenderMode.WorldSpace;
            gameManager.GameOver(true);
            yield break;
        }
        else if (player.IsDefeated())
        {
            yield return StartCoroutine(turnCamera.ReturnToInitialPosition());
            gameManager.GameOver(false);
            yield break;
        }

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
                totalAttackers++;
                attackCompleted = false;

                cardForwardAttack.OnAttackComplete += () =>
                {
                    finishedAttacks++;
                    attackCompleted = true;
                };

                StartCoroutine(cardForwardAttack.PerformAttack());
            }
        }
    }

    public void OnTriggerExit(Collider hit)
    {
        if (Tags.Contains(hit.tag))
        {
            var moveForward = hit.gameObject.GetComponentInParent<CardForwardAttack>().GetComponentInParent<MoveForward>();
            if (moveForward != null && !moveForward.isMovingBackLine)
            {
                moveForwardLines.Add(moveForward);
            }
        }
    }
}
