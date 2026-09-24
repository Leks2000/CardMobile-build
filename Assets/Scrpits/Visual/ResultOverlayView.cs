using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Victory / Defeat overlay (presentation of GameManagerOver's result panel).
/// Full-screen dim above everything (the result canvas is Screen Space - Overlay), gold/red title,
/// reward card listing BattleRewards.LastBreakdown / LastCoins / LastRelic, pulsing "Tap to continue".
/// The flow (what the tap does) stays in GameManagerOver.HandleTapToContinue.
/// </summary>
public class ResultOverlayView : MonoBehaviour
{
    private RectTransform panel;
    private Image dim;
    private TMP_Text title;
    private RectTransform built;
    private RectTransform rays;
    private Image glow;
    private RectTransform card;
    private RectTransform rows;
    private RectTransform continuePill;
    private TMP_Text continueText;
    private TMP_Text subtitle;

    private static readonly Color VictoryTop = new Color32(0xFF, 0xF1, 0xA8, 0xFF);
    private static readonly Color VictoryBottom = new Color32(0xFF, 0xA8, 0x2E, 0xFF);
    private static readonly Color DefeatTop = new Color32(0xFF, 0x9A, 0x8A, 0xFF);
    private static readonly Color DefeatBottom = new Color32(0xB0, 0x1E, 0x2A, 0xFF);

    /// <summary>Plays the overlay. <paramref name="onReady"/> fires when the player may tap to continue.</summary>
    public void Play(bool victory, GameObject resPanel, TMP_Text titleText, IEnumerable<TMP_Text> legacyTexts, Action onReady)
    {
        panel = (RectTransform)resPanel.transform;
        title = titleText;
        foreach (var t in legacyTexts) if (t != null) t.gameObject.SetActive(false);
        Build();

        panel.localScale = Vector3.one;
        var accent = victory ? UiTheme.Accent : UiTheme.Damage;

        // dim + glow + rays
        dim.color = new Color(0.03f, 0.02f, 0.05f, 0f);
        dim.DOFade(victory ? 0.82f : 0.88f, 0.4f).SetLink(gameObject);
        glow.color = new Color(accent.r, accent.g, accent.b, 0f);
        glow.DOFade(victory ? 0.45f : 0.35f, 0.8f).SetLink(gameObject);
        rays.gameObject.SetActive(victory);
        foreach (var img in rays.GetComponentsInChildren<Image>(true)) img.color = new Color(1f, 0.85f, 0.4f, 0.10f);
        rays.localEulerAngles = Vector3.zero;
        rays.DOLocalRotate(new Vector3(0, 0, -360f), 40f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear).SetLink(gameObject);

        // title
        VisualTheme.Style(title, 150, Color.white);
        title.enableVertexGradient = true;
        title.colorGradient = victory ? new VertexGradient(VictoryTop, VictoryTop, VictoryBottom, VictoryBottom)
                                      : new VertexGradient(DefeatTop, DefeatTop, DefeatBottom, DefeatBottom);
        title.characterSpacing = 6;
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.1f, 0.72f); trt.anchorMax = new Vector2(0.9f, 0.92f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        title.gameObject.SetActive(true);
        trt.localScale = Vector3.one * 2.2f;
        title.alpha = 0f;
        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(trt.DOScale(1f, 0.45f).SetEase(Ease.OutBack));
        seq.Join(DOTween.To(() => title.alpha, a => title.alpha = a, 1f, 0.25f));
        seq.AppendCallback(() =>
        {
            CombatFx.ShakeCamera(victory ? 0.4f : 0.8f, 0.25f);
            CombatFx.ScreenFlash(victory ? CombatFx.VictoryGold : CombatFx.DamageRed, 0.3f, 0.4f);
            if (victory) ImpactFx.Burst(trt, UiTheme.Accent, 12, 1.2f);
        });

        subtitle.text = victory ? "The way forward is open" : "Your run ends here...";
        subtitle.color = victory ? UiTheme.Text : new Color32(0xE8, 0xB0, 0xB0, 0xFF);
        subtitle.alpha = 0f;
        seq.Append(DOTween.To(() => subtitle.alpha, a => subtitle.alpha = a, 1f, 0.3f));

        // reward card
        FillRows(victory);
        card.localScale = new Vector3(1f, 0f, 1f);
        seq.Append(card.DOScaleY(1f, 0.3f).SetEase(Ease.OutBack));
        for (int i = 0; i < rows.childCount; i++)
        {
            var row = rows.GetChild(i);
            var cg = VisualTheme.Ensure<CanvasGroup>(row.gameObject);
            cg.alpha = 0f;
            row.localScale = Vector3.one * 0.8f;
            seq.Append(cg.DOFade(1f, 0.15f));
            seq.Join(row.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
            seq.AppendInterval(0.08f);
        }

        continuePill.gameObject.SetActive(true);
        continuePill.localScale = Vector3.zero;
        seq.Append(continuePill.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            continuePill.DOScale(1.06f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(continuePill.gameObject);
            onReady?.Invoke();
        });
    }

    private void FillRows(bool victory)
    {
        for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
        rows.DetachChildren();

        if (victory)
        {
            AddRow("REWARDS", null, UiTheme.TextDim, 30, true);
            foreach (var (label, coins) in BattleRewards.LastBreakdown) AddRow(label, "+" + coins, UiTheme.Text, 36, false);
            AddDivider();
            AddRow("Total", "+" + BattleRewards.LastCoins, UiTheme.Accent, 46, false);
            if (BattleRewards.LastRelic != null)
            {
                var rc = RarityColors.Get(BattleRewards.LastRelic.rarity);
                AddDivider();
                AddRow("Relic: " + BattleRewards.LastRelic.name, null, rc, 36, false);
                if (!string.IsNullOrEmpty(BattleRewards.LastRelic.description))
                    AddRow(BattleRewards.LastRelic.description, null, UiTheme.TextDim, 26, false);
            }
            if (BattleRewards.LastItem != null)
            {
                var ic = RarityColors.Get(BattleRewards.LastItem.rarity);
                AddDivider();
                AddRow("Item: " + BattleRewards.LastItem.name, null, ic, 36, false);
                AddRow(BattleRewards.LastItem.description, null, UiTheme.TextDim, 26, false);
            }
            AddRow("Wallet  " + Wallet.Coins, null, UiTheme.TextDim, 26, true);
        }
        else
        {
            var boss = FindAnyObjectByType<Boss>();
            var name = boss != null && boss.Data != null && !string.IsNullOrEmpty(boss.Data.displayName) ? boss.Data.displayName : "the dungeon";
            AddRow("Defeated by " + name, null, UiTheme.Text, 38, true);
            AddRow("Coins kept  " + Wallet.Coins, null, UiTheme.TextDim, 30, true);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    private void AddRow(string label, string value, Color color, float size, bool centered)
    {
        var row = new GameObject("Row", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(rows, false);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = size * 1.35f;
        le.flexibleWidth = 1;
        var l = VisualTheme.Txt(VisualTheme.Stretch("Label", row), label, size, color);
        l.alignment = centered ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        l.textWrappingMode = TextWrappingModes.Normal;
        l.enableAutoSizing = true; l.fontSizeMin = size * 0.6f; l.fontSizeMax = size;
        if (value != null)
        {
            ((RectTransform)l.transform).offsetMax = new Vector2(-150, 0);
            var coin = VisualTheme.Centered("Coin", row, new Vector2(1f, 0.5f), new Vector2(size * 0.8f, size * 0.8f), new Vector2(-size * 0.45f, 0));
            VisualTheme.Img(coin, ProcSprites.Circle, UiTheme.Accent);
            VisualTheme.Ensure<Outline>(coin.gameObject).effectColor = new Color(0.45f, 0.25f, 0.05f, 1f);
            var v = VisualTheme.Txt(VisualTheme.Node("Value", row, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-150 - size, 0), new Vector2(-size, 0)), value, size, color);
            v.alignment = TextAlignmentOptions.Right;
        }
    }

    private void AddDivider()
    {
        var d = new GameObject("Divider", typeof(RectTransform)).GetComponent<RectTransform>();
        d.SetParent(rows, false);
        var le = d.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 4; le.flexibleWidth = 1;
        VisualTheme.Img(d, ProcSprites.RoundRectSmall, new Color(1f, 1f, 1f, 0.12f), true);
    }

    private void Build()
    {
        if (built != null) return;
        dim = panel.GetComponent<Image>();
        if (dim == null) dim = panel.gameObject.AddComponent<Image>();
        dim.sprite = null;
        dim.raycastTarget = true;

        built = VisualTheme.Stretch("V_Result", panel);
        built.SetAsFirstSibling();
        var vig = VisualTheme.Img(VisualTheme.Stretch("Vignette", built), ProcSprites.Vignette, new Color(1, 1, 1, 0.9f));
        vig.raycastTarget = false;
        glow = VisualTheme.Img(VisualTheme.Centered("Glow", built, new Vector2(0.5f, 0.8f), new Vector2(1500, 900)), ProcSprites.Glow, Color.clear);
        rays = VisualTheme.Centered("Rays", built, new Vector2(0.5f, 0.8f), new Vector2(10, 10));
        for (int i = 0; i < 12; i++)
        {
            var r = VisualTheme.Centered("Ray" + i, rays, new Vector2(0.5f, 0.5f), new Vector2(160, 1500));
            r.localEulerAngles = new Vector3(0, 0, i * 15f);
            VisualTheme.Img(r, ProcSprites.Shard, new Color(1, 1, 1, 0.1f));
        }

        subtitle = VisualTheme.Txt(VisualTheme.Node("Subtitle", built, new Vector2(0.2f, 0.66f), new Vector2(0.8f, 0.72f), Vector2.zero, Vector2.zero), "", 40, UiTheme.Text);

        // reward card (auto-height)
        card = VisualTheme.Node("RewardCard", built, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.62f), Vector2.zero, Vector2.zero);
        card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(680, 300);
        var shadow = VisualTheme.Img(VisualTheme.Node("Shadow", card, Vector2.zero, Vector2.one, new Vector2(-30, -44), new Vector2(30, 16)), ProcSprites.SoftShadow, new Color(0, 0, 0, 0.8f), true);
        shadow.GetComponent<LayoutElement>();
        VisualTheme.Ensure<LayoutElement>(shadow.gameObject).ignoreLayout = true;
        var bg = VisualTheme.Img(card, ProcSprites.RoundRect, UiTheme.Panel, true);
        var ol = VisualTheme.Ensure<Outline>(bg.gameObject);
        ol.effectColor = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.6f); ol.effectDistance = new Vector2(3, -3);
        var vl = VisualTheme.Ensure<VerticalLayoutGroup>(card.gameObject);
        vl.padding = new RectOffset(44, 44, 26, 30); vl.spacing = 6;
        vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        var fit = VisualTheme.Ensure<ContentSizeFitter>(card.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rows = card; // rows are direct children of the card (after the shadow)

        // continue pill
        continuePill = VisualTheme.Centered("Continue", built, new Vector2(0.5f, 0.1f), new Vector2(520, 96));
        var pillShadow = VisualTheme.Img(VisualTheme.Node("Shadow", continuePill, Vector2.zero, Vector2.one, new Vector2(-24, -34), new Vector2(24, 12)), ProcSprites.SoftShadow, new Color(0, 0, 0, 0.7f), true);
        pillShadow.raycastTarget = false;
        var face = VisualTheme.Img(VisualTheme.Stretch("Face", continuePill), ProcSprites.RoundRect, UiTheme.Accent, true);
        VisualTheme.Ensure<Outline>(face.gameObject).effectColor = VisualTheme.Outline;
        VisualTheme.Img(VisualTheme.Node("Gloss", continuePill, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-8, -6)), ProcSprites.Gloss, Color.white, true);
        continueText = VisualTheme.Txt(VisualTheme.Stretch("Label", continuePill), "TAP TO CONTINUE", 46, new Color32(0x2A, 0x16, 0x08, 0xFF), false);
        continuePill.gameObject.SetActive(false);

        // Rows live directly under the card; keep the shadow out of the row list by re-parenting it.
        shadow.transform.SetParent(built, true);
        shadow.transform.SetSiblingIndex(card.GetSiblingIndex());
        FollowCard(shadow.rectTransform);
    }

    private RectTransform cardShadow;
    private void FollowCard(RectTransform s) { cardShadow = s; }

    private void LateUpdate()
    {
        if (cardShadow == null || card == null) return;
        // shadow tracks the auto-sized card
        cardShadow.anchorMin = cardShadow.anchorMax = card.anchorMin;
        cardShadow.pivot = card.pivot;
        cardShadow.sizeDelta = card.sizeDelta + new Vector2(60, 60);
        cardShadow.anchoredPosition = card.anchoredPosition + new Vector2(0, 16);
        cardShadow.localScale = card.localScale;
    }
}
