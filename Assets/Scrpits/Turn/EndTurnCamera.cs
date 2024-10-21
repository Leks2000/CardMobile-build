using System.Collections;
using UnityEngine;

/// <summary>
/// Передвижение камеры с различными этапами
/// </summary>
/// <remarks> 3 этапа передвжиения в конце раунда </remarks>
public class EndTurnCamera : MonoBehaviour
{
    [SerializeField] private GameControlManager cardManager;
    [SerializeField] private Camera mainCam;
    [SerializeField] private float duration;
    [SerializeField] private float returnDuration;
    [SerializeField] private float delay;
    [SerializeField] private float rotationDuration;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Quaternion intermediateRotation;

    private LineAttackMoveActivation attackLine;
    private LineBackMove lineBackMove;
    private Coroutine currentCoroutine;
    private bool hasClicked;

    private void Start()
    {
        initialPosition = mainCam.transform.position;
        initialRotation = mainCam.transform.rotation;

        targetPosition = new Vector3(0, 0, -5);
        targetRotation = Quaternion.Euler(86, 0, 0);
        intermediateRotation = Quaternion.Euler(75, 0, 0);
        GetComponent<Button_UI>().ClickFunc = () => OnClickFunc();
        attackLine = FindAnyObjectByType<LineAttackMoveActivation>().GetComponent<LineAttackMoveActivation>();
        lineBackMove = FindAnyObjectByType<LineBackMove>().GetComponent<LineBackMove>();
    }
    private void OnClickFunc()
    {
        if (!hasClicked)
        {
            hasClicked = true;
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(MoveCamera());
        }
    }

    private void Update()
    {
        if (!hasClicked)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (currentCoroutine != null)
                {
                    StopCoroutine(currentCoroutine);
                }
                currentCoroutine = StartCoroutine(MoveCamera());
            }
            else if (Input.GetKeyUp(KeyCode.Space))
            {
                if (currentCoroutine != null)
                {
                    StopCoroutine(currentCoroutine);
                }
                currentCoroutine = StartCoroutine(ReturnToInitialPosition());
            }
        }
    }

    /// <summary>
    /// Движение к игровому полю
    /// </summary>
    private IEnumerator MoveCamera()
    {
        var timeElapsed = 0f;
        mainCam.transform.GetPositionAndRotation(out var startPosition, out var startRotation);

        while (timeElapsed < duration)
        {
            mainCam.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            mainCam.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        mainCam.transform.SetPositionAndRotation(targetPosition, targetRotation);

        yield return new WaitForSeconds(delay + 0.1f);
        if (hasClicked)
        {
            StartCoroutine(attackLine.changeLine());
        }
    }

    /// <summary>
    /// Движение к вражескому полю где он выкладывает карты
    /// </summary>
    public IEnumerator ChangeRotation()
    {
        var timeElapsed = 0f;
        var startRotation = mainCam.transform.rotation;

        while (timeElapsed < rotationDuration)
        {
            mainCam.transform.rotation = Quaternion.Lerp(startRotation, intermediateRotation, timeElapsed / rotationDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        mainCam.transform.rotation = intermediateRotation;

        yield return StartCoroutine(lineBackMove.moveBackCards());
        StartCoroutine(ReturnToInitialPosition());
    }
    /// <summary>
    /// Возвращение в дефолтную позицию
    /// </summary>
    /// <remarks> + Активация возможности кликнуть на конец раунда</remarks>
    private IEnumerator ReturnToInitialPosition()
    {
        var timeElapsed = 0f;
        mainCam.transform.GetPositionAndRotation(out var startPosition, out var startRotation);

        while (timeElapsed < returnDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(timeElapsed / returnDuration);
            mainCam.transform.position = Vector3.Lerp(startPosition, initialPosition, t);
            mainCam.transform.rotation = Quaternion.Lerp(startRotation, initialRotation, t);
            yield return null;
        }

        mainCam.transform.SetPositionAndRotation(initialPosition, initialRotation);

        if (hasClicked)
        {
            cardManager.TurnRound();
            hasClicked = false;
            yield return new WaitForSeconds(0.25f);
        }
    }
}
