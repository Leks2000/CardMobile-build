using System.Collections;
using UnityEngine;

public class EndTurnCamera : MonoBehaviour
{
    CardManager cardManager;

    [SerializeField] private Camera mainCam;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float delay = 1f;
    [SerializeField] private float rotationDuration = 0.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Quaternion intermediateRotation;

    AttackChecker attackLine;

    private void Start()
    {
        initialPosition = mainCam.transform.position;
        initialRotation = mainCam.transform.rotation;

        targetPosition = new Vector3(0, 0, -10);
        targetRotation = Quaternion.Euler(86, 0, 0);
        intermediateRotation = Quaternion.Euler(75, 0, 0);
        cardManager = FindObjectOfType<CardManager>().GetComponent<CardManager>();
        GetComponent<Button_UI>().ClickFunc = () => StartCoroutine(MoveCamera());
        attackLine = FindObjectOfType<AttackChecker>().GetComponent<AttackChecker>();
    }


    private IEnumerator MoveCamera()
    {
        cardManager.TurnRound();
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

        yield return new WaitForSeconds(delay);

        StartCoroutine(ReturnToInitialPosition());
    }

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
    }
}
