using System.Collections.Generic;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Items] Панель расходников в бою (правый край экрана). Тап по предмету - карточка с описанием и кнопкой USE,
/// повторный тап / USE - использовать (<see cref="ItemSystem.TryUse"/>). Эффекты: полёт иконки к боссу,
/// кольца, вспышки, всплывающий текст результата. Строится кодом (<see cref="Create"/>).
/// </summary>
public class BattleItemBar : MonoBehaviour
{
    private const float SlotSize = 112f;
    private const float Spacing = 14f;
    private const int MaxSlots = 4;

    private RectTransform root;
    private RectTransform slotsRoot;
    private TMP_Text emptyText;
    private RectTransform info;
    private TMP_Text infoTitle, infoKind, infoDesc, infoHint;
    private Button useButton;
    private TMP_Text useLabel;
    private Image infoBorder;

    private readonly List<Slot> slots = new List<Slot>();
    private string selected;
    private bool busy;

    private class Slot
    {
        public string id;
        public RectTransform rt;
        public Image border, icon, glow;
        public TMP_Text count;
    }

    public static BattleItemBar Create(RectTransform canvasRoot)
    {
        var rt = VisualTheme.Node("V_ItemBar", canvasRoot, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, Vector2.zero);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(SlotSize + 24f, 620f);
        rt.anchoredPosition = new Vector2(-18f, 20f);
        var bar = VisualTheme.Ensure<BattleItemBar>(rt.gameObject);
        bar.Build(rt);
        return bar;
    }

    private void OnEnable()
    {
        RunState.ItemsChanged += Rebuild;
        ItemSystem.Used += OnUsed;
    }

    private void OnDisable()
    {
        RunState.ItemsChanged -= Rebuild;
        ItemSystem.Used -= OnUsed;
    }

    private void Build(RectTransform rt)
    {
        root = rt;
        var header = VisualTheme.Txt(VisualTheme.Node("Header", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -44), Vector2.zero), "ITEMS", 30, UiTheme.Accent);
        header.characterSpacing = 4;

        slotsRoot = VisualTheme.Node("Slots", root, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, 0), new Vector2(0, -52));
        var vl = VisualTheme.Ensure<VerticalLayoutGroup>(slotsRoot.gameObject);
        vl.childAlignment = TextAnchor.UpperCenter;
        vl.spacing = Spacing;
        vl.childControlWidth = vl.childControlHeight = false;
        vl.childForceExpandWidth = vl.childForceExpandHeight = false;

        emptyText = VisualTheme.Txt(VisualTheme.Node("Empty", root, new Vector2(0, 1), new Vector2(1, 1), new Vector2(-40, -150), new Vector2(0, -56)),
            "No items\n<size=75%>buy them\nin the shop</size>", 24, UiTheme.TextDim);
        emptyText.textWrappingMode = TextWrappingModes.Normal;
        emptyText.alignment = TextAlignmentOptions.Top;

        BuildInfo();
        Rebuild();
    }

    // ---------------- slots ----------------

    private void Rebuild()
    {
        if (slotsRoot == null) return;
        foreach (var s in slots) Destroy(s.rt.gameObject);
        slots.Clear();

        int n = 0;
        foreach (var item in ItemDatabase.Consumables)
        {
            if (RunState.ItemCount(item.id) <= 0) continue;
            if (n++ >= MaxSlots) break;
            slots.Add(MakeSlot(item));
        }
        emptyText.gameObject.SetActive(slots.Count == 0);
        if (selected != null && RunState.ItemCount(selected) <= 0) CloseInfo();
        else if (selected != null) Select(selected, false);
    }

    private Slot MakeSlot(ItemDef item)
    {
        var rc = RarityColors.Get(item.rarity);
        var s = new Slot { id = item.id };
        s.rt = VisualTheme.Centered("Item_" + item.id, slotsRoot, new Vector2(0.5f, 1f), new Vector2(SlotSize, SlotSize));
        var le = VisualTheme.Ensure<LayoutElement>(s.rt.gameObject);
        le.preferredWidth = SlotSize; le.preferredHeight = SlotSize;

        s.glow = VisualTheme.Img(VisualTheme.Stretch("Glow", s.rt, -26f), ProcSprites.Glow, VisualTheme.WithA(rc, 0f));
        VisualTheme.Img(VisualTheme.Node("Shadow", s.rt, Vector2.zero, Vector2.one, new Vector2(-10, -16), new Vector2(10, 4)), ProcSprites.SoftShadow, new Color(0, 0, 0, 0.7f), true);
        s.border = VisualTheme.Img(VisualTheme.Stretch("Border", s.rt), ProcSprites.RoundRect, rc, true);
        var face = VisualTheme.Img(VisualTheme.Stretch("Face", s.rt, 5f), ProcSprites.RoundRect, Color.Lerp(UiTheme.Panel, rc, 0.18f), true);
        face.raycastTarget = true;
        VisualTheme.Img(VisualTheme.Node("Gloss", s.rt, new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(9, 0), new Vector2(-9, -8)), ProcSprites.Gloss, new Color(1, 1, 1, 0.5f), true);
        s.icon = VisualTheme.Img(VisualTheme.Centered("Icon", s.rt, new Vector2(0.5f, 0.52f), new Vector2(78, 78)), null, Color.white);
        s.icon.sprite = ItemIcons.Get(item);
        s.icon.preserveAspect = true;

        int count = RunState.ItemCount(item.id);
        var badge = VisualTheme.Centered("Count", s.rt, new Vector2(1f, 0f), new Vector2(40, 40), new Vector2(-8, 8));
        VisualTheme.Img(badge, ProcSprites.Circle, UiTheme.Background);
        VisualTheme.Ensure<Outline>(badge.gameObject).effectColor = rc;
        s.count = VisualTheme.Txt(VisualTheme.Stretch("Value", badge), count.ToString(), 24, UiTheme.Text);
        badge.gameObject.SetActive(count > 1);

        var btn = VisualTheme.Ensure<Button>(face.gameObject);
        btn.transition = Selectable.Transition.None;
        string id = item.id;
        btn.onClick.AddListener(() => OnSlotClicked(id));
        return s;
    }

    private void OnSlotClicked(string id)
    {
        if (busy) return;
        if (selected == id && info.gameObject.activeSelf)
        {
            Use(id);
            return;
        }
        Select(id, true);
    }

    // ---------------- info popover ----------------

    private void BuildInfo()
    {
        info = VisualTheme.Node("Info", root, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
        info.pivot = new Vector2(1f, 1f);
        info.sizeDelta = new Vector2(440, 250);
        VisualTheme.Img(VisualTheme.Node("Shadow", info, Vector2.zero, Vector2.one, new Vector2(-24, -34), new Vector2(24, 12)), ProcSprites.SoftShadow, new Color(0, 0, 0, 0.8f), true);
        infoBorder = VisualTheme.Img(VisualTheme.Stretch("Border", info), ProcSprites.RoundRect, UiTheme.Accent, true);
        var bg = VisualTheme.Img(VisualTheme.Stretch("Bg", info, 4f), ProcSprites.RoundRect, UiTheme.Panel, true);
        bg.raycastTarget = true; // клики по карточке не проваливаются на поле

        infoTitle = VisualTheme.Txt(VisualTheme.Node("Title", info, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -58), new Vector2(-22, -14)), "", 34, UiTheme.Text);
        infoTitle.alignment = TextAlignmentOptions.Left;
        infoTitle.enableAutoSizing = true; infoTitle.fontSizeMin = 20; infoTitle.fontSizeMax = 34;
        infoKind = VisualTheme.Txt(VisualTheme.Node("Kind", info, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -86), new Vector2(-22, -58)), "", 20, UiTheme.TextDim, false);
        infoKind.alignment = TextAlignmentOptions.Left;
        infoKind.characterSpacing = 3;
        infoDesc = VisualTheme.Txt(VisualTheme.Node("Desc", info, new Vector2(0, 0), new Vector2(1, 1), new Vector2(22, 80), new Vector2(-22, -90)), "", 25, UiTheme.Text, false);
        infoDesc.alignment = TextAlignmentOptions.TopLeft;
        infoDesc.textWrappingMode = TextWrappingModes.Normal;
        infoDesc.enableAutoSizing = true; infoDesc.fontSizeMin = 16; infoDesc.fontSizeMax = 25;

        var ubRt = VisualTheme.Node("Use", info, new Vector2(0, 0), new Vector2(0, 0), Vector2.zero, Vector2.zero);
        ubRt.pivot = new Vector2(0, 0);
        ubRt.sizeDelta = new Vector2(170, 58);
        ubRt.anchoredPosition = new Vector2(20, 16);
        var face = VisualTheme.Img(ubRt, ProcSprites.RoundRect, UiTheme.Accent, true);
        face.sprite = Assets.Scrpits.Map.UiKit.ButtonShape;
        face.raycastTarget = true;
        useButton = VisualTheme.Ensure<Button>(ubRt.gameObject);
        useButton.onClick.AddListener(() => { if (selected != null) Use(selected); });
        useLabel = VisualTheme.Txt(VisualTheme.Stretch("Label", ubRt), "USE", 32, new Color32(0x2A, 0x16, 0x08, 0xFF), false);

        infoHint = VisualTheme.Txt(VisualTheme.Node("Hint", info, new Vector2(0, 0), new Vector2(1, 0), new Vector2(200, 16), new Vector2(-18, 74)), "", 20, UiTheme.TextDim, false);
        infoHint.alignment = TextAlignmentOptions.Left;
        infoHint.textWrappingMode = TextWrappingModes.Normal;

        var close = VisualTheme.Centered("Close", info, new Vector2(1, 1), new Vector2(46, 46), new Vector2(-10, -10));
        var cimg = VisualTheme.Img(close, ProcSprites.Circle, UiTheme.PanelLight);
        cimg.raycastTarget = true;
        VisualTheme.Txt(VisualTheme.Stretch("X", close), "x", 28, UiTheme.TextDim);
        VisualTheme.Ensure<Button>(close.gameObject).onClick.AddListener(CloseInfo);

        info.gameObject.SetActive(false);
    }

    private void Select(string id, bool animate)
    {
        var item = ItemDatabase.Get(id);
        var slot = slots.Find(s => s.id == id);
        if (item == null || slot == null) { CloseInfo(); return; }
        selected = id;
        var rc = RarityColors.Get(item.rarity);
        foreach (var s in slots) s.glow.color = VisualTheme.WithA(RarityColors.Get(ItemDatabase.Get(s.id).rarity), s.id == id ? 0.7f : 0f);

        infoBorder.color = rc;
        infoTitle.text = item.name;
        infoTitle.color = rc == RarityColors.Get(CardRarity.Common) ? UiTheme.Text : rc;
        infoKind.text = $"{RarityColors.Name(item.rarity).ToUpper()}  ·  x{RunState.ItemCount(id)}";
        infoDesc.text = item.description;

        bool can = ItemSystem.CanUseNow(out var reason) && ItemSystem.HasTarget(item, out reason);
        useButton.interactable = can;
        useButton.GetComponent<Image>().color = can ? UiTheme.Accent : new Color(0.38f, 0.35f, 0.42f, 1f);
        useLabel.color = can ? (Color)new Color32(0x2A, 0x16, 0x08, 0xFF) : UiTheme.TextDim;
        infoHint.text = can ? "or tap the item again" : reason;
        infoHint.color = can ? UiTheme.TextDim : UiTheme.Damage;

        // карточка слева от слота, по его высоте
        info.gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Vector2 local = root.InverseTransformPoint(slot.rt.position);
        info.anchorMin = info.anchorMax = new Vector2(0.5f, 0.5f);
        info.anchoredPosition = local - root.rect.center + new Vector2(-SlotSize * 0.5f - 18f, SlotSize * 0.5f);
        info.SetAsLastSibling();
        if (animate)
        {
            info.DOKill(true);
            info.localScale = new Vector3(0.85f, 0.85f, 1f);
            info.DOScale(1f, 0.22f).SetEase(Ease.OutBack).SetLink(info.gameObject);
            CombatFx.Punch(slot.rt, 0.12f, 0.25f);
        }
    }

    private void CloseInfo()
    {
        selected = null;
        if (info != null) info.gameObject.SetActive(false);
        foreach (var s in slots) s.glow.color = VisualTheme.WithA(s.glow.color, 0f);
    }

    private void Update()
    {
        // состояние USE меняется при смене хода - обновляем открытую карточку
        if (selected != null && info.gameObject.activeSelf && Time.frameCount % 15 == 0) Select(selected, false);
    }

    // ---------------- use + fx ----------------

    private void Use(string id)
    {
        var item = ItemDatabase.Get(id);
        var slot = slots.Find(s => s.id == id);
        if (item == null || slot == null) return;
        if (!ItemSystem.CanUseNow(out var reason) || !ItemSystem.HasTarget(item, out reason))
        {
            slot.rt.DOKill(true);
            slot.rt.DOShakeAnchorPos(0.3f, new Vector2(10, 0), 20, 0).SetLink(slot.rt.gameObject);
            if (!string.IsNullOrEmpty(reason)) Float(slot.rt, reason, UiTheme.Damage);
            return;
        }

        var target = BossTarget();
        bool thrown = target != null && (item.effect == ItemEffect.BossDamage || item.effect == ItemEffect.Poison);
        if (!thrown)
        {
            Resolve(id, slot);
            return;
        }

        // иконка летит в босса, эффект - по прилёту
        busy = true;
        var proj = VisualTheme.Centered("Thrown_" + id, root.parent, new Vector2(0.5f, 0.5f), new Vector2(84, 84));
        var pimg = VisualTheme.Img(proj, null, Color.white);
        pimg.sprite = ItemIcons.Get(item);
        pimg.preserveAspect = true;
        proj.position = slot.icon.transform.position;
        proj.SetAsLastSibling();
        var seq = DOTween.Sequence().SetLink(proj.gameObject);
        seq.Append(proj.DOMove(target.position, 0.38f).SetEase(Ease.InQuad));
        seq.Join(proj.DORotate(new Vector3(0, 0, item.effect == ItemEffect.BossDamage ? 720f : 360f), 0.38f, RotateMode.FastBeyond360));
        seq.Join(proj.DOScale(1.3f, 0.38f));
        seq.OnComplete(() =>
        {
            ImpactFx.Burst(target, item.color, 10, 1.3f);
            ImpactFx.Ring(target, item.color, 1.6f);
            Destroy(proj.gameObject);
            busy = false;
            Resolve(id, slots.Find(s => s.id == id));
        });
    }

    private void Resolve(string id, Slot slot)
    {
        if (!ItemSystem.TryUse(id, out var result))
        {
            if (slot != null && !string.IsNullOrEmpty(result)) Float(slot.rt, result, UiTheme.Damage);
            return;
        }
        if (slot != null) Float(slot.rt, result, ItemDatabase.Get(id).color);
    }

    private void OnUsed(ItemDef item)
    {
        var slot = slots.Find(s => s.id == item.id);
        var at = slot != null ? slot.rt : root;
        ImpactFx.Ring(at, item.color, 1.2f);
        ImpactFx.Sparkle(at, item.color, 8, 1f);
        switch (item.effect)
        {
            case ItemEffect.Heal:
            case ItemEffect.FullHeal:
            case ItemEffect.Cleanse:
                CombatFx.ScreenFlash(UiTheme.Heal, 0.18f, 0.45f);
                if (Player.Instance != null && Player.Instance.playerHpText != null)
                {
                    ImpactFx.Sparkle(Player.Instance.playerHpText.transform, UiTheme.Heal, 10, 1.2f);
                }
                break;
            case ItemEffect.Shield:
                CombatFx.ScreenFlash(UiTheme.Shield, 0.15f, 0.4f);
                if (Player.Instance != null && Player.Instance.playerHpText != null)
                {
                    ImpactFx.Ring(Player.Instance.playerHpText.transform, UiTheme.Shield, 1.4f);
                }
                break;
            case ItemEffect.Mana:
                CombatFx.ScreenFlash(UiTheme.Mana, 0.15f, 0.4f);
                break;
            case ItemEffect.Rally:
                CombatFx.ScreenFlash(item.color, 0.16f, 0.4f);
                CombatFx.TurnBanner("BATTLE CRY!", item.color, 0.35f);
                break;
            case ItemEffect.Bomb:
                CombatFx.ScreenFlash(new Color(1f, 0.6f, 0.2f), 0.25f, 0.35f);
                foreach (var c in ItemSystem.EnemyCards()) ImpactFx.Burst(c.transform, new Color(1f, 0.55f, 0.2f), 8, 1f);
                break;
        }
    }

    private static RectTransform BossTarget()
    {
        var boss = FindAnyObjectByType<Boss>();
        if (boss == null || boss.bossImage == null) return null;
        return boss.bossImage.rectTransform;
    }

    /// <summary>Всплывающий текст над слотом.</summary>
    private void Float(RectTransform at, string text, Color color)
    {
        if (string.IsNullOrEmpty(text)) return;
        var rt = VisualTheme.Centered("Float", root.parent, new Vector2(0.5f, 0.5f), new Vector2(420, 60));
        rt.position = at.position;
        rt.anchoredPosition += new Vector2(-240f, 0f);
        var t = VisualTheme.Txt(rt, text, 38, color);
        t.alignment = TextAlignmentOptions.Right;
        rt.SetAsLastSibling();
        var seq = DOTween.Sequence().SetLink(rt.gameObject);
        rt.localScale = Vector3.one * 0.6f;
        seq.Append(rt.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
        seq.Join(rt.DOAnchorPosY(rt.anchoredPosition.y + 70f, 1.1f).SetEase(Ease.OutCubic));
        seq.Insert(0.7f, t.DOFade(0f, 0.4f));
        seq.OnComplete(() => Destroy(rt.gameObject));
    }
}
