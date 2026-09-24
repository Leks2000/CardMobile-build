using System;
using System.Collections.Generic;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Victory / Defeat overlay (presentation of GameManagerOver's result panel).
/// Victory: gold title on a ribbon, rotating rays, rewards on the ornate dark panel (coins, overkill, relic, item
/// with icons), coin sparkles, CONTINUE button.
/// Defeat: red title, what killed you, Renown earned, RESTART (new run right away) and UNLOCKS buttons -
/// no trip through the main menu.
/// </summary>
public class ResultOverlayView : MonoBehaviour
{
    private RectTransform panel;
    private Image dim;
    private TMP_Text title;
    private RectTransform built;
    private RectTransform rays;
    private Image glow;
    private Image ribbon;
    private RectTransform card;
    private RectTransform rows;
    private RectTransform buttons;
    private Button primary, secondary;
    private TMP_Text primaryLabel, secondaryLabel;
    private TMP_Text subtitle;
    private bool victory;

    private static readonly Color VictoryTop = new Color32(0xFF, 0xF1, 0xA8, 0xFF);
    private static readonly Color VictoryBottom = new Color32(0xFF, 0xA8, 0x2E, 0xFF);
    private static readonly Color DefeatTop = new Color32(0xFF, 0x9A, 0x8A, 0xFF);
    private static readonly Color DefeatBottom = new Color32(0xB0, 0x1E, 0x2A, 0xFF);

    /// <summary>Plays the overlay. <paramref name="onReady"/> fires when the player may continue.</summary>
    public void Play(bool isVictory, GameObject resPanel, TMP_Text titleText, IEnumerable<TMP_Text> legacyTexts, Action onReady)
    {
        victory = isVictory;
        panel = (RectTransform)resPanel.transform;
        title = titleText;
        foreach (var t in legacyTexts) if (t != null) t.gameObject.SetActive(false);
        Build();

        panel.localScale = Vector3.one;
        var accent = victory ? UiTheme.Accent : UiTheme.Damage;

        dim.color = new Color(0.03f, 0.02f, 0.05f, 0f);
        dim.DOFade(victory ? 0.8f : 0.88f, 0.4f).SetLink(gameObject);
        glow.color = new Color(accent.r, accent.g, accent.b, 0f);
        glow.DOFade(victory ? 0.5f : 0.35f, 0.8f).SetLink(gameObject);
        rays.gameObject.SetActive(victory);
        foreach (var img in rays.GetComponentsInChildren<Image>(true)) img.color = new Color(1f, 0.85f, 0.4f, 0.12f);
        rays.DOLocalRotate(new Vector3(0, 0, -360f), 40f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear).SetLink(gameObject);

        // title on a ribbon
        ribbon.color = victory ? Color.white : new Color(0.55f, 0.45f, 0.45f, 1f);
        ribbon.rectTransform.localScale = new Vector3(0f, 1f, 1f);
        VisualTheme.Style(title, 140, Color.white);
        title.enableVertexGradient = true;
        title.colorGradient = victory ? new VertexGradient(VictoryTop, VictoryTop, VictoryBottom, VictoryBottom)
                                      : new VertexGradient(DefeatTop, DefeatTop, DefeatBottom, DefeatBottom);
        title.characterSpacing = 6;
        var trt = title.rectTransform;
        trt.anchorMin = new Vector2(0.1f, 0.74f); trt.anchorMax = new Vector2(0.9f, 0.92f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        title.transform.SetAsLastSibling();
        title.gameObject.SetActive(true);
        trt.localScale = Vector3.one * 2.2f;
        title.alpha = 0f;
        var seq = DOTween.Sequence().SetLink(gameObject);
        seq.Append(ribbon.rectTransform.DOScaleX(1f, 0.35f).SetEase(Ease.OutBack));
        seq.Join(trt.DOScale(1f, 0.45f).SetEase(Ease.OutBack));
        seq.Join(DOTween.To(() => title.alpha, a => title.alpha = a, 1f, 0.25f));
        seq.AppendCallback(() =>
        {
            CombatFx.ShakeCamera(victory ? 0.4f : 0.8f, 0.25f);
            CombatFx.ScreenFlash(victory ? CombatFx.VictoryGold : CombatFx.DamageRed, 0.3f, 0.4f);
            if (victory)
            {
                ImpactFx.Burst(trt, UiTheme.Accent, 14, 1.3f);
                ImpactFx.Sparkle(trt, UiTheme.Accent, 12, 1.6f);
            }
        });

        subtitle.text = victory ? "The way forward is open" : "Your run ends here... but not your legend";
        subtitle.color = victory ? UiTheme.Text : new Color32(0xE8, 0xB0, 0xB0, 0xFF);
        subtitle.alpha = 0f;
        seq.Append(DOTween.To(() => subtitle.alpha, a => subtitle.alpha = a, 1f, 0.3f));

        FillRows();
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
            var rowT = row;
            seq.AppendCallback(() => { if (victory && rowT.name == "RowCoins") ImpactFx.Sparkle(rowT, UiTheme.Accent, 5, 0.8f); });
            seq.AppendInterval(0.06f);
        }

        // buttons
        primaryLabel.text = victory ? "CONTINUE" : "RESTART";
        secondary.gameObject.SetActive(!victory);
        buttons.gameObject.SetActive(true);
        buttons.localScale = Vector3.zero;
        primary.interactable = false;
        seq.Append(buttons.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
        seq.OnComplete(() =>
        {
            primary.interactable = true;
            primary.transform.DOScale(1.05f, 0.7f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(primary.gameObject);
            onReady?.Invoke();
        });
    }

    private void OnPrimary()
    {
        var over = GetComponent<GameManagerOver>();
        if (over != null) over.OnTapToContinue();
    }

    private void OnUnlocks()
    {
        Assets.Scrpits.Map.MetaScreen.Show(panel);
    }

    private void FillRows()
    {
        for (int i = rows.childCount - 1; i >= 0; i--) Destroy(rows.GetChild(i).gameObject);
        rows.DetachChildren();

        if (victory)
        {
            AddRow("REWARDS", null, UiTheme.TextDim, 30, true);
            foreach (var (label, coins) in BattleRewards.LastBreakdown) AddRow(label, "+" + coins, UiTheme.Text, 36, false);
            AddDivider();
            AddRow("Total", "+" + BattleRewards.LastCoins, UiTheme.Accent, 46, false).name = "RowCoins";
            if (BattleRewards.LastRelic != null)
            {
                var item = ItemDatabase.Get(BattleRewards.LastRelic.id);
                AddDivider();
                AddIconRow(item != null ? ItemIcons.Get(item) : null, "Relic: " + BattleRewards.LastRelic.name, BattleRewards.LastRelic.description, RarityColors.Get(BattleRewards.LastRelic.rarity));
            }
            if (BattleRewards.LastItem != null)
            {
                AddDivider();
                AddIconRow(ItemIcons.Get(BattleRewards.LastItem), "Item: " + BattleRewards.LastItem.name, BattleRewards.LastItem.description, RarityColors.Get(BattleRewards.LastItem.rarity));
            }
            AddRow("Wallet  " + Wallet.Coins, null, UiTheme.TextDim, 26, true);
        }
        else
        {
            var boss = FindAnyObjectByType<Boss>();
            var name = boss != null && boss.Data != null && !string.IsNullOrEmpty(boss.Data.displayName) ? boss.Data.displayName : "the dungeon";
            AddRow("Defeated by " + name, null, UiTheme.Text, 38, true);
            AddDivider();
            AddRow($"Act {RunState.Act}   Nodes cleared {RunState.NodesCleared}", null, UiTheme.TextDim, 28, true);
            AddRow("Renown earned", "+" + RunState.RunScore, UiTheme.Accent, 40, false, false);
            AddRow($"Total Renown  {MetaProgress.Points}  - spend it in UNLOCKS", null, UiTheme.TextDim, 26, true);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    private RectTransform AddRow(string label, string value, Color color, float size, bool centered, bool coin = true)
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
            float right = 0f;
            if (coin)
            {
                var c = VisualTheme.Centered("Coin", row, new Vector2(1f, 0.5f), new Vector2(size * 0.9f, size * 0.9f), new Vector2(-size * 0.45f, 0));
                var ci = VisualTheme.Img(c, ProcSprites.Circle, UiTheme.Accent);
                var art = ArtLib.UI("coin");
                if (art != null) { ci.sprite = art; ci.color = Color.white; ci.preserveAspect = true; }
                right = size;
            }
            var v = VisualTheme.Txt(VisualTheme.Node("Value", row, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-150 - right, 0), new Vector2(-right - 6, 0)), value, size, color);
            v.alignment = TextAlignmentOptions.Right;
        }
        return row;
    }

    private void AddIconRow(Sprite icon, string label, string desc, Color color)
    {
        var row = new GameObject("RowIcon", typeof(RectTransform)).GetComponent<RectTransform>();
        row.SetParent(rows, false);
        var le = row.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 86;
        le.flexibleWidth = 1;
        var ic = VisualTheme.Node("Icon", row, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(80, 0));
        var img = VisualTheme.Img(ic, null, Color.white);
        img.sprite = icon;
        img.preserveAspect = true;
        img.enabled = icon != null;
        var t = VisualTheme.Txt(VisualTheme.Node("Label", row, new Vector2(0, 0), new Vector2(1, 1), new Vector2(96, 0), Vector2.zero),
            $"{label}\n<size=70%><color=#{ColorUtility.ToHtmlStringRGB(UiTheme.TextDim)}>{desc}</color></size>", 34, color);
        t.alignment = TextAlignmentOptions.Left;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.enableAutoSizing = true; t.fontSizeMin = 18; t.fontSizeMax = 34;
    }

    private void AddDivider()
    {
        var d = new GameObject("Divider", typeof(RectTransform)).GetComponent<RectTransform>();
        d.SetParent(rows, false);
        var le = d.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 4; le.flexibleWidth = 1;
        VisualTheme.Img(d, ProcSprites.RoundRectSmall, new Color(1f, 0.85f, 0.6f, 0.15f), true);
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
        glow = VisualTheme.Img(VisualTheme.Centered("Glow", built, new Vector2(0.5f, 0.8f), new Vector2(1600, 1000)), ProcSprites.Glow, Color.clear);
        rays = VisualTheme.Centered("Rays", built, new Vector2(0.5f, 0.8f), new Vector2(10, 10));
        for (int i = 0; i < 12; i++)
        {
            var r = VisualTheme.Centered("Ray" + i, rays, new Vector2(0.5f, 0.5f), new Vector2(160, 1500));
            r.localEulerAngles = new Vector3(0, 0, i * 15f);
            VisualTheme.Img(r, ProcSprites.Shard, new Color(1, 1, 1, 0.1f));
        }

        // лента под заголовком (арт проекта)
        var rb = VisualTheme.Node("Ribbon", built, new Vector2(0.18f, 0.745f), new Vector2(0.82f, 0.915f), Vector2.zero, Vector2.zero);
        ribbon = VisualTheme.Img(rb, null, Color.white);
        ribbon.sprite = ArtLib.UI("ribbon_red");
        if (ribbon.sprite != null && ribbon.sprite.border != Vector4.zero) ribbon.type = Image.Type.Sliced;
        ribbon.enabled = ribbon.sprite != null;

        subtitle = VisualTheme.Txt(VisualTheme.Node("Subtitle", built, new Vector2(0.15f, 0.67f), new Vector2(0.85f, 0.73f), Vector2.zero, Vector2.zero), "", 40, UiTheme.Text);

        // карточка наград на тёмной панели с орнаментом (без чёрной тени-прямоугольника)
        card = VisualTheme.Node("RewardCard", built, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.64f), Vector2.zero, Vector2.zero);
        card.pivot = new Vector2(0.5f, 1f);
        card.sizeDelta = new Vector2(760, 300);
        var bg = VisualTheme.Img(card, ProcSprites.RoundRect, UiTheme.Panel, true);
        var art = ArtLib.UI("panel_frame_dark");
        if (art != null)
        {
            bg.sprite = art;
            bg.type = art.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            bg.color = Color.white;
        }
        var vl = VisualTheme.Ensure<VerticalLayoutGroup>(card.gameObject);
        vl.padding = new RectOffset(70, 70, 70, 60); vl.spacing = 6;
        vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
        var fit = VisualTheme.Ensure<ContentSizeFitter>(card.gameObject);
        fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        rows = card;

        // кнопки (объёмные игровые, как в магазине / UNLOCKS)
        buttons = VisualTheme.Node("V_Buttons", panel, new Vector2(0.5f, 0.1f), new Vector2(0.5f, 0.1f), Vector2.zero, Vector2.zero);
        buttons.sizeDelta = new Vector2(900, 110);
        primary = MakeButton("Primary", new Vector2(0, 0), UiTheme.Accent, new Color32(0x2A, 0x16, 0x08, 0xFF), out primaryLabel, OnPrimary);
        secondary = MakeButton("Unlocks", new Vector2(0, 0), UiTheme.PanelLight, UiTheme.Text, out secondaryLabel, OnUnlocks);
        secondaryLabel.text = "UNLOCKS";
        ((RectTransform)primary.transform).anchoredPosition = new Vector2(-210, 0);
        ((RectTransform)secondary.transform).anchoredPosition = new Vector2(210, 0);
        buttons.gameObject.SetActive(false);
        buttons.SetAsLastSibling();
    }

    private Button MakeButton(string name, Vector2 pos, Color color, Color textColor, out TMP_Text label, Action onClick)
    {
        var rt = VisualTheme.Centered(name, buttons, new Vector2(0.5f, 0.5f), new Vector2(380, 104), pos);
        var img = VisualTheme.Img(rt, null, color);
        img.sprite = Assets.Scrpits.Map.UiKit.ButtonShape;
        img.type = Image.Type.Sliced;
        img.raycastTarget = true;
        var btn = VisualTheme.Ensure<Button>(rt.gameObject);
        btn.onClick.AddListener(() => { SoundFx.Play(SoundFx.Clip.Click); onClick(); });
        label = VisualTheme.Txt(VisualTheme.Node("Label", rt, Vector2.zero, Vector2.one, new Vector2(10, 12), new Vector2(-10, -4)), "", 46, textColor, false);
        return btn;
    }

    private void LateUpdate()
    {
        // наши кнопки всегда поверх старой кнопки сцены (ButtonNextScene), иначе она перехватит нажатия
        if (buttons != null && buttons.gameObject.activeSelf && buttons.GetSiblingIndex() != buttons.parent.childCount - 1) buttons.SetAsLastSibling();
    }
}
