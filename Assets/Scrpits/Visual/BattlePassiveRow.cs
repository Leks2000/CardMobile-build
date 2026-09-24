using System.Collections.Generic;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Items] Ряд пассивных предметов (реликвий) в бою: маленькие иконки в левой колонке HUD,
/// тап - подсказка с названием и эффектом. Строится кодом (<see cref="Create"/>).
/// </summary>
public class BattlePassiveRow : MonoBehaviour
{
    private const float IconSize = 58f;

    private RectTransform row;
    private RectTransform tip;
    private TMP_Text tipText;
    private Image tipBorder;
    private string tipFor;
    private int shownCount = -1;

    public static BattlePassiveRow Create(RectTransform canvasRoot, Vector2 anchoredPos, float width)
    {
        var rt = VisualTheme.Node("V_Passives", canvasRoot, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.pivot = Vector2.zero;
        rt.sizeDelta = new Vector2(width, IconSize + 34f);
        rt.anchoredPosition = anchoredPos;
        var v = VisualTheme.Ensure<BattlePassiveRow>(rt.gameObject);
        v.Build(rt);
        return v;
    }

    private void Build(RectTransform rt)
    {
        var cap = VisualTheme.Txt(VisualTheme.Node("Caption", rt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(4, -28), Vector2.zero), "PASSIVES", 22, UiTheme.TextDim);
        cap.alignment = TextAlignmentOptions.Left;
        cap.characterSpacing = 3;

        row = VisualTheme.Node("Row", rt, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, IconSize));
        var hl = VisualTheme.Ensure<HorizontalLayoutGroup>(row.gameObject);
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.spacing = 8;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;

        tip = VisualTheme.Node("Tip", rt, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
        tip.pivot = new Vector2(0, 0);
        tip.sizeDelta = new Vector2(420, 130);
        tip.anchoredPosition = new Vector2(0, 8);
        VisualTheme.Img(VisualTheme.Node("Shadow", tip, Vector2.zero, Vector2.one, new Vector2(-20, -30), new Vector2(20, 10)), ProcSprites.SoftShadow, new Color(0, 0, 0, 0.75f), true);
        tipBorder = VisualTheme.Img(VisualTheme.Stretch("Border", tip), ProcSprites.RoundRect, UiTheme.Accent, true);
        var bg = VisualTheme.Img(VisualTheme.Stretch("Bg", tip, 4f), ProcSprites.RoundRect, UiTheme.Panel, true);
        bg.raycastTarget = true;
        VisualTheme.Ensure<Button>(bg.gameObject).onClick.AddListener(HideTip);
        tipText = VisualTheme.Txt(VisualTheme.Stretch("Text", tip, 16f), "", 24, UiTheme.Text, false);
        tipText.alignment = TextAlignmentOptions.TopLeft;
        tipText.textWrappingMode = TextWrappingModes.Normal;
        tipText.enableAutoSizing = true; tipText.fontSizeMin = 16; tipText.fontSizeMax = 26;
        tip.gameObject.SetActive(false);

        Rebuild();
    }

    private void Update()
    {
        // реликвия может прийти посреди боя (награда элиты/босса)
        if (RunState.Relics.Count != shownCount) Rebuild();
    }

    private void Rebuild()
    {
        shownCount = RunState.Relics.Count;
        for (int i = row.childCount - 1; i >= 0; i--) Destroy(row.GetChild(i).gameObject);
        var owned = new List<ItemDef>();
        foreach (var id in RunState.Relics)
        {
            var item = ItemDatabase.Get(id);
            if (item != null) owned.Add(item);
        }
        gameObject.SetActive(true);
        foreach (Transform c in transform) if (c != tip) c.gameObject.SetActive(owned.Count > 0);
        foreach (var item in owned) MakeIcon(item);
    }

    private void MakeIcon(ItemDef item)
    {
        var rc = RarityColors.Get(item.rarity);
        var rt = VisualTheme.Centered("P_" + item.id, row, new Vector2(0, 0.5f), new Vector2(IconSize, IconSize));
        var le = VisualTheme.Ensure<LayoutElement>(rt.gameObject);
        le.preferredWidth = IconSize; le.preferredHeight = IconSize;
        VisualTheme.Img(VisualTheme.Stretch("Border", rt), ProcSprites.Circle, rc);
        var face = VisualTheme.Img(VisualTheme.Stretch("Face", rt, 3f), ProcSprites.Circle, Color.Lerp(UiTheme.Background, rc, 0.2f));
        face.raycastTarget = true;
        var icon = VisualTheme.Img(VisualTheme.Stretch("Icon", rt, 9f), null, Color.white);
        icon.sprite = ItemIcons.Get(item);
        icon.preserveAspect = true;
        var btn = VisualTheme.Ensure<Button>(face.gameObject);
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(() => ShowTip(item, rt));
        CombatFx.PopIn(rt, 0.3f);
    }

    private void ShowTip(ItemDef item, RectTransform icon)
    {
        if (tip.gameObject.activeSelf && tipFor == item.id) { HideTip(); return; }
        tipFor = item.id;
        var rc = RarityColors.Get(item.rarity);
        tipBorder.color = rc;
        tipText.text = $"<size=120%><color=#{ColorUtility.ToHtmlStringRGB(rc)}>{item.name}</color></size>\n{item.description}";
        tip.gameObject.SetActive(true);
        tip.SetAsLastSibling();
        tip.DOKill(true);
        tip.localScale = new Vector3(0.9f, 0.9f, 1f);
        tip.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetLink(tip.gameObject);
        CombatFx.Punch(icon, 0.15f, 0.25f);
    }

    private void HideTip()
    {
        tipFor = null;
        tip.gameObject.SetActive(false);
    }
}
