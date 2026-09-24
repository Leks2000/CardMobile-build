using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Урон по боссу «идёт по полю»: число появляется на клетке ударившей карты, затем летит к портрету босса.
/// Отдельный overlay-канвас поверх стола и HUD (под паузой). Возвращает время полёта - столько босс ждёт с реакцией.
/// </summary>
public static class BoardDamagePop
{
    private const float Appear = 0.25f, Hold = 0.2f, FlyTime = 0.35f;
    private static RectTransform layer;

    public static float TotalDelay => Appear + Hold + FlyTime;

    /// <summary>Число на клетке <paramref name="from"/>, потом полёт к <paramref name="to"/> (null - остаётся на клетке).</summary>
    public static float Fly(Transform from, int damage, Transform to, Color? color = null)
    {
        if (from == null) return 0f;
        var root = Layer();
        if (root == null) return 0f;
        var rt = VisualTheme.Node("DmgPop", root, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.sizeDelta = new Vector2(260, 110);
        var t = VisualTheme.Txt(rt, damage > 0 ? "-" + damage : "BLOCK", damage > 0 ? 76 : 48, color ?? UiTheme.Damage);
        t.raycastTarget = false;

        Vector3 start = ScreenPos(from) + new Vector3(0f, 30f, 0f);
        rt.position = start;
        rt.localScale = Vector3.zero;
        var seq = DOTween.Sequence().SetLink(rt.gameObject);
        seq.Append(rt.DOScale(1.25f, Appear * 0.6f).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(1f, Appear * 0.4f));
        seq.Join(rt.DOMove(start + new Vector3(0f, 36f, 0f), Appear + Hold).SetEase(Ease.OutQuad));
        if (to != null && damage > 0)
        {
            seq.Append(rt.DOMove(ScreenPos(to), FlyTime).SetEase(Ease.InQuad));
            seq.Join(rt.DOScale(0.7f, FlyTime).SetEase(Ease.InQuad));
            seq.OnComplete(() => { if (rt != null) Object.Destroy(rt.gameObject); });
            return TotalDelay;
        }
        seq.AppendInterval(0.2f);
        seq.Append(t.DOFade(0f, 0.3f));
        seq.OnComplete(() => { if (rt != null) Object.Destroy(rt.gameObject); });
        return 0f;
    }

    /// <summary>Экранная позиция UI-элемента из любого канваса (overlay / camera / world).</summary>
    public static Vector3 ScreenPos(Transform t)
    {
        var canvas = t.GetComponentInParent<Canvas>();
        if (canvas != null) canvas = canvas.rootCanvas;
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return t.position;
        var cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        return RectTransformUtility.WorldToScreenPoint(cam, t.position);
    }

    private static RectTransform Layer()
    {
        if (layer != null) return layer;
        var go = new GameObject("V_DamagePops", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 7; // над столом и HUD (6), под паузой (30)
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        layer = (RectTransform)go.transform;
        return layer;
    }
}
