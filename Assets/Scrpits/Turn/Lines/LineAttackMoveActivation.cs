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

    private int finishedAttacks = 0;
    private int totalAttackers = 0;
    /// <summary>Карты, уже атаковавшие в этом ходу (каждая атакует максимум 1 раз)</summary>
    private readonly HashSet<CardForwardAttack> attackedThisTurn = new HashSet<CardForwardAttack>();
    /// <summary>Страховка от зависания хода, если атака так и не сообщила о завершении</summary>
    [SerializeField] private float attackTimeout = 5f;

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
        totalAttackers = 0;
        finishedAttacks = 0;
        attackedThisTurn.Clear();
        moveForwardLines.RemoveAll(m => m == null);

        // [D] начало раунда: яд / кровотечение
        if (CombatRules.TickRound())
        {
            yield return new WaitForSeconds(0.6f);
        }

        mesh.enabled = true;
        rectTransform.localPosition = new Vector3(0, 0, 5);
        rectTransform.DOScale(new Vector3(125f, 900f, 1), 0.25f);

        GameObject initialLine = lines.FirstOrDefault(line =>
            line.GetComponentsInChildren<Transform>().Any(child => Tags.Contains(child.tag)));

        if (initialLine != null)
        {
            transform.SetParent(initialLine.transform);
            rectTransform.anchoredPosition = Vector2.zero;
            // Коллайдер включаем только на первой линии, иначе срабатывают карты линии, где он остался с прошлого хода
            coll.enabled = true;
            yield return rectTransform.DOAnchorPos(Vector2.zero, 1f)
                .SetEase(Ease.OutElastic, 0.6f, 1f)
                .WaitForCompletion();
            yield return WaitForAttacks();

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
                    yield return WaitForAttacks();
                }
            }
        }

        rectTransform.DOLocalMoveZ(rectTransform.localPosition.z + 100, 0.1f).SetEase(Ease.Linear);
        yield return new WaitForSeconds(0.1f);

        mesh.enabled = false;
        coll.enabled = false;
        rectTransform.localScale = new Vector3(25f, 900f, 1);

        yield return WaitForAttacks();

        // [D] конец раунда: босс бьёт игрока (телеграф - подпись под HP босса)
        yield return CombatRules.BossAttack(boss);

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

    /// <summary>
    /// Ждём завершения всех начатых атак (с таймаутом, чтобы ход никогда не завис)
    /// </summary>
    private IEnumerator WaitForAttacks()
    {
        yield return new WaitForFixedUpdate();
        var elapsed = 0f;
        while (finishedAttacks < totalAttackers && elapsed < attackTimeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (finishedAttacks < totalAttackers)
        {
            Debug.LogWarning($"LineAttackMoveActivation: attack timeout ({finishedAttacks}/{totalAttackers})");
            finishedAttacks = totalAttackers;
        }
    }

    public void OnTriggerEnter(Collider hit)
    {
        if (Tags.Contains(hit.tag))
        {
            var cardForwardAttack = hit.gameObject.GetComponentInParent<CardForwardAttack>();

            cardForwardAttack.HandleErrorIfNullGetComponent<CardForwardAttack, LineAttackMoveActivation>(this, gameObject);

            if (cardForwardAttack != null && attackedThisTurn.Add(cardForwardAttack))
            {
                totalAttackers++;

                CardForwardAttack.AttackCompleteHandler handler = null;
                handler = () =>
                {
                    cardForwardAttack.OnAttackComplete -= handler;
                    finishedAttacks++;
                };
                cardForwardAttack.OnAttackComplete += handler;

                StartCoroutine(cardForwardAttack.PerformAttack());
            }
        }
    }

    public void OnTriggerExit(Collider hit)
    {
        if (Tags.Contains(hit.tag))
        {
            var attacker = hit.gameObject.GetComponentInParent<CardForwardAttack>();
            var moveForward = attacker != null ? attacker.GetComponentInParent<MoveForward>() : null;
            if (moveForward != null && !moveForward.isMovingBackLine && !moveForwardLines.Contains(moveForward))
            {
                moveForwardLines.Add(moveForward);
            }
        }
    }
}
