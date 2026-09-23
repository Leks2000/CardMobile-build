using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] HP bar: instant fill + delayed "damage lag" trail + optional number. Built by the scene builder
/// (fill / trail are horizontal-filled Images). Call <see cref="Set"/> whenever HP changes.
/// </summary>
public class HpBarView : MonoBehaviour
{
    public Image fill;
    public Image trail;
    public TMP_Text label;
    public string format = "{0}/{1}";
    public Color fillColor = new Color32(0xE0, 0x3A, 0x45, 0xFF);
    public Color lowColor = new Color32(0xFF, 0x6A, 0x2A, 0xFF);
    public Color healColor = new Color32(0x5E, 0xE0, 0x7A, 0xFF);

    private float shown = -1f;
    private Tween trailTween;

    public void Set(float current, float max, bool animate = true)
    {
        float t = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        if (label != null) label.text = string.Format(format, Mathf.Max(0, Mathf.RoundToInt(current)), Mathf.RoundToInt(max));
        if (fill == null) return;
        fill.color = t <= 0.3f ? lowColor : fillColor;

        bool first = shown < 0f || !Application.isPlaying || !animate;
        float prev = first ? t : shown;
        shown = t;
        fill.fillAmount = t;
        if (trail == null) return;

        trailTween?.Kill();
        if (first)
        {
            trail.fillAmount = t;
            return;
        }
        if (t < prev)
        {
            // damage: trail stays, then catches up
            trail.color = new Color(1f, 0.92f, 0.75f, 0.9f);
            trail.fillAmount = Mathf.Max(trail.fillAmount, prev);
            trailTween = trail.DOFillAmount(t, 0.45f).SetDelay(0.35f).SetEase(Ease.InOutQuad).SetLink(gameObject);
            CombatFx.Punch(transform, 0.06f, 0.25f);
        }
        else if (t > prev)
        {
            // heal: trail leads in green, fill grows into it
            trail.color = healColor;
            trail.fillAmount = t;
            fill.fillAmount = prev;
            trailTween = fill.DOFillAmount(t, 0.4f).SetDelay(0.15f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }
    }
}
