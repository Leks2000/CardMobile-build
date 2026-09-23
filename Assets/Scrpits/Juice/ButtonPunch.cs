using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Tap feedback for any uGUI button (Button or Button_UI): squash on press, springy release.
/// Scale-only, so it never interferes with position/layout logic.
/// </summary>
public class ButtonPunch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressedScale = 0.9f;
    public float pressDuration = 0.06f;
    public float releaseDuration = 0.22f;

    private Selectable selectable;
    private Vector3 baseScale;
    private bool hasBase;
    private Coroutine routine;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
        hasBase = true;
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
        if (hasBase) transform.localScale = baseScale;
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!Interactable) return;
        Play(PressRoutine());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!hasBase) return;
        Play(ReleaseRoutine());
    }

    private void Play(IEnumerator r)
    {
        if (!isActiveAndEnabled) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(r);
    }

    private IEnumerator PressRoutine()
    {
        Vector3 from = transform.localScale;
        Vector3 to = baseScale * pressedScale;
        for (float t = 0; t < pressDuration; t += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(from, to, t / pressDuration);
            yield return null;
        }
        transform.localScale = to;
        routine = null;
    }

    private IEnumerator ReleaseRoutine()
    {
        Vector3 from = transform.localScale;
        for (float t = 0; t < releaseDuration; t += Time.unscaledDeltaTime)
        {
            float p = t / releaseDuration;
            // easeOutBack from current to base
            float s = 1f + 2.70158f * Mathf.Pow(p - 1f, 3) + 1.70158f * Mathf.Pow(p - 1f, 2);
            transform.localScale = Vector3.LerpUnclamped(from, baseScale, s);
            yield return null;
        }
        transform.localScale = baseScale;
        routine = null;
    }
}
