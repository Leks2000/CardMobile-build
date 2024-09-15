using System.Collections;
using UnityEngine;

/// <summary>
/// Передвижение камеры с различными этапами
/// </summary>
/// <remarks> 3 этапа передвжиения в конце раунда </remarks>
public class EndTurnCamera : MonoBehaviour
{
    [SerializeField] CardManager cardManager;

    [SerializeField] private Camera mainCam;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float delay = 1f;
    [SerializeField] private float rotationDuration = 0.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Quaternion intermediateRotation;

    LineAttackMoveActivation attackLine;
    private bool hasClicked;

    private void Start()
    {
        initialPosition = mainCam.transform.position;
        initialRotation = mainCam.transform.rotation;

        targetPosition = new Vector3(0, 0, -5);
        targetRotation = Quaternion.Euler(86, 0, 0);
        intermediateRotation = Quaternion.Euler(75, 0, 0);
        GetComponent<Button_UI>().ClickFunc = () => OnClickFunc();
        attackLine = FindObjectOfType<LineAttackMoveActivation>().GetComponent<LineAttackMoveActivation>();
    }
    private void OnClickFunc()
    {
        if (!hasClicked)
        {
            hasClicked = true;
            StartCoroutine(MoveCamera());
        }
    }
    /// <summary>
    /// Движение к игровому полю
    /// </summary>
    private IEnumerator MoveCamera()
    {
        var timeElapsed = 0f;
        var startPosition = mainCam.transform.position;
        var startRotation = mainCam.transform.rotation;

        while (timeElapsed < duration)
        {
            mainCam.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            mainCam.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        mainCam.transform.position = targetPosition;
        mainCam.transform.rotation = targetRotation;

        yield return new WaitForSeconds(delay + 0.1f);

        StartCoroutine(attackLine.changeLine());
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

        yield return StartCoroutine(attackLine.moveCards(attackLine.moveBaclkLines, 0.25f));
        StartCoroutine(ReturnToInitialPosition());
        attackLine.moveBaclkLines.Clear();
    }
    /// <summary>
    /// Возвращение в дефолтную позицию
    /// </summary>
    /// <remarks> + Активация возможности кликнуть на конец раунда</remarks>
    private IEnumerator ReturnToInitialPosition()
    {
        var timeElapsed = 0f;
        var startPosition = mainCam.transform.position;
        var startRotation = mainCam.transform.rotation;

        while (timeElapsed < duration)
        {
            mainCam.transform.position = Vector3.Lerp(startPosition, initialPosition, timeElapsed / duration);
            mainCam.transform.rotation = Quaternion.Lerp(startRotation, initialRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        mainCam.transform.position = initialPosition;
        mainCam.transform.rotation = initialRotation;
        cardManager.TurnRound();

        yield return new WaitForSeconds(0.5f);
        hasClicked = false;
    }
}
