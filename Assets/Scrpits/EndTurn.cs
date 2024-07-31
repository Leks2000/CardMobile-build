using System.Collections;
using UnityEngine;

public class EndTurn : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float delay = 1f;
    [SerializeField] private float rotationDuration = 0.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Quaternion intermediateRotation;

    private void Start()
    {
        // Сохраняем начальное положение и поворот камеры
        initialPosition = _camera.transform.position;
        initialRotation = _camera.transform.rotation;

        // Определяем целевое положение и вращение
        targetPosition = new Vector3(0, 0, -10);
        targetRotation = Quaternion.Euler(86, 0, 0);
        intermediateRotation = Quaternion.Euler(75, 0, 0);
    }

    public void EndRound()
    {
        StartCoroutine(MoveCamera());
    }

    private IEnumerator MoveCamera()
    {
        float timeElapsed = 0;
        Vector3 startPosition = _camera.transform.position;
        Quaternion startRotation = _camera.transform.rotation;

        // Двигаем камеру к целевому положению и вращению
        while (timeElapsed < duration)
        {
            _camera.transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / duration);
            _camera.transform.rotation = Quaternion.Lerp(startRotation, targetRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _camera.transform.position = targetPosition;
        _camera.transform.rotation = targetRotation;

        // Ждём указанное время перед сменой вращения
        yield return new WaitForSeconds(delay);

        StartCoroutine(ChangeRotation());
    }

    private IEnumerator ChangeRotation()
    {
        float timeElapsed = 0;
        Quaternion startRotation = _camera.transform.rotation;

        // Меняем вращение камеры
        while (timeElapsed < rotationDuration)
        {
            _camera.transform.rotation = Quaternion.Lerp(startRotation, intermediateRotation, timeElapsed / rotationDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _camera.transform.rotation = intermediateRotation;

        // Ждём немного перед возвратом в начальное состояние
        yield return new WaitForSeconds(delay);

        StartCoroutine(ReturnToInitialPosition());
    }

    private IEnumerator ReturnToInitialPosition()
    {
        float timeElapsed = 0;
        Vector3 startPosition = _camera.transform.position;
        Quaternion startRotation = _camera.transform.rotation;

        // Возвращаем камеру в начальное положение и вращение
        while (timeElapsed < duration)
        {
            _camera.transform.position = Vector3.Lerp(startPosition, initialPosition, timeElapsed / duration);
            _camera.transform.rotation = Quaternion.Lerp(startRotation, initialRotation, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        _camera.transform.position = initialPosition;
        _camera.transform.rotation = initialRotation;
    }
}
