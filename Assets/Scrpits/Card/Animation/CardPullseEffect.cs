using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CardPulseEffect : MonoBehaviour
{
    [Header("Параметры эффекта")]
    public float pulseScale = 1.1f;
    public float pulseDuration = 1.25f;

    private RectTransform rectTransform;
    private Tween scaleTween;
    private Tween moveTween;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        PulseEffectManager.RegisterEffect(this);

        SetVisible(false);
        StartPulse();
    }

    public void StartPulse()
    {
        scaleTween = rectTransform.DOScale(pulseScale, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);

        moveTween = rectTransform.DOLocalMoveY(0 + 5f, pulseDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetLink(gameObject);
    }

    public void ContinuePulse()
    {
        scaleTween.Play();
        moveTween.Play();
    }

    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
    }
}
