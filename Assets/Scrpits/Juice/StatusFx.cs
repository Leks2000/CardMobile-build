using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Визуальные эффекты баффов/дебаффов: над целью всплывает иконка статуса из набора проекта (Resources/Art/UI)
/// с числом (+2 / Poison 3), вспышка кольцом и искры цвета статуса. Вешается на StatusHolder.Added
/// (любое наложение статуса) и вызывается напрямую для +ATK / +HP.
/// </summary>
public static class StatusFx
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        StatusHolder.Added -= OnAdded;
        StatusHolder.Added += OnAdded;
    }

    private static void OnAdded(StatusHolder holder, StatusType type, int amount)
    {
        if (holder == null || amount <= 0 || !Application.isPlaying) return;
        string label = type switch
        {
            StatusType.Shield => $"+{amount}",
            StatusType.Poison => $"Poison {amount}",
            StatusType.Bleed => $"Bleed {amount}",
            StatusType.Lifesteal => $"Lifesteal {amount}",
            StatusType.Thorns => $"Thorns {amount}",
            _ => $"+{amount}",
        };
        Pop(holder.transform, StatusRowView.StatusSprite(type), label, UiTheme.StatusColor(type));
    }

    /// <summary>+ATK (баннер, кузнец, бафф миньонов, реликвии).</summary>
    public static void Atk(Transform target, int amount)
    {
        if (amount == 0) return;
        Pop(target, ArtLib.UI("status_atk"), (amount > 0 ? "+" : "") + amount + " ATK", new Color32(0xFF, 0xB8, 0x3D, 0xFF));
    }

    /// <summary>+HP (лечение карт).</summary>
    public static void Hp(Transform target, int amount)
    {
        if (amount <= 0) return;
        Pop(target, ArtLib.UI("status_heal"), $"+{amount} HP", UiTheme.Heal);
    }

    /// <summary>Тик яда/кровотечения в начале раунда: иконка вспыхивает на цели.</summary>
    public static void Tick(Transform target, StatusType type)
    {
        Pop(target, StatusRowView.StatusSprite(type), type == StatusType.Poison ? "Poison!" : "Bleed!", UiTheme.StatusColor(type));
    }

    private static void Pop(Transform target, Sprite icon, string text, Color color)
    {
        if (target == null || !Application.isPlaying) return;
        if (!(target is RectTransform)) return;
        var root = VisualTheme.Node("V_StatusPop", target, new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f), Vector2.zero, Vector2.zero);
        root.sizeDelta = new Vector2(140, 60);
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas != null) root.rotation = canvas.transform.rotation; // не вверх ногами на картах врага
        root.SetAsLastSibling();

        var ic = VisualTheme.Node("Icon", root, new Vector2(0f, 0f), new Vector2(0.4f, 1f), Vector2.zero, Vector2.zero);
        var img = VisualTheme.Img(ic, null, Color.white);
        img.sprite = icon;
        img.preserveAspect = true;
        img.enabled = icon != null;
        var t = VisualTheme.Txt(VisualTheme.Node("Text", root, new Vector2(0.38f, 0f), new Vector2(1.6f, 1f), Vector2.zero, Vector2.zero), text, 26, color);
        t.alignment = TextAlignmentOptions.Left;

        ImpactFx.Ring(target, color, 0.9f);
        ImpactFx.Sparkle(target, color, 6, 0.9f);

        var cg = VisualTheme.Ensure<CanvasGroup>(root.gameObject);
        cg.blocksRaycasts = false;
        root.localScale = Vector3.one * 0.4f;
        var seq = DOTween.Sequence().SetLink(root.gameObject);
        seq.Append(root.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
        seq.Join(root.DOAnchorPosY(70f, 1.1f).SetEase(Ease.OutCubic));
        seq.Insert(0.75f, cg.DOFade(0f, 0.4f));
        seq.OnComplete(() => { if (root != null) Object.Destroy(root.gameObject); });
    }
}
