using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Presentation of a card (player or enemy). Gameplay stays on <see cref="Card"/> / CardDrag / attack
/// components; this only draws: rarity frame, art window (CardData.art, fallback = prefab sprite),
/// name plate (CardData.Title), cost gem, attack / HP badges, status icon row, drop shadow.
/// Usage: <c>GetComponent&lt;CardView&gt;().Bind(data)</c> after spawning, <c>Refresh()</c> after stats change.
/// </summary>
[DisallowMultipleComponent]
public class CardView : MonoBehaviour
{
    [SerializeField] private bool isEnemy;
    [SerializeField] private Card card;

    [Header("Built parts (BuildVisuals fills these)")]
    [SerializeField] private Image glow;
    [SerializeField] private Image shadow;
    [SerializeField] private Image frame;
    [SerializeField] private Image inner;
    [SerializeField] private RectTransform artWindow;
    [SerializeField] private Image art;
    [SerializeField] private AspectRatioFitter artFitter;
    [SerializeField] private Image namePlate;
    [SerializeField] private TMP_Text title;
    [SerializeField] private RectTransform statusRow;
    [SerializeField] private Image costGem;
    [SerializeField] private Image atkBadge;
    [SerializeField] private Image hpBadge;
    [SerializeField] private Sprite fallbackArt;

    private CardData bound;

    public bool IsEnemy => isEnemy;
    /// <summary>The data currently shown (bound data, else the Card's runtime clone).</summary>
    public CardData Data => bound != null ? bound : (card != null ? card.CardData : null);

    [SerializeField] private Image rarityDot;

    private void Awake()
    {
        if (card == null) card = GetComponent<Card>();
        // Раскладка задаётся кодом при каждом спавне (идемпотентно): префабы могли быть «запечены» старой версией.
        BuildVisuals();
    }

    private void Start() => Refresh();

    /// <summary>Show this CardData (title, art, rarity frame, stats, statuses).</summary>
    public void Bind(CardData data)
    {
        bound = data;
        Refresh();
    }

    // Палитра карты: тёмная «сталь» + редкость в рамке, контуре таблички и точке снизу.
    private static readonly Color PlayerInner = new Color32(0x1B, 0x18, 0x22, 0xFF);
    private static readonly Color EnemyInner = new Color32(0x26, 0x12, 0x17, 0xFF);
    private static readonly Color PlayerPlate = new Color32(0x3A, 0x36, 0x45, 0xFF);
    private static readonly Color EnemyPlate = new Color32(0x4A, 0x1E, 0x26, 0xFF);
    private static readonly Color CommonFrame = new Color32(0x8E, 0x8A, 0x99, 0xFF);

    /// <summary>Re-read the data and redraw everything (call after HP/damage/status changes).</summary>
    public void Refresh()
    {
        var d = Data;
        if (d == null || frame == null) return;

        var rarity = RarityColors.Get(d.rarity);
        var frameColor = d.rarity == CardRarity.Common ? CommonFrame : rarity;
        frame.color = isEnemy ? Color.Lerp(frameColor, VisualTheme.EnemyAccent, 0.55f) : frameColor;
        if (inner != null) inner.color = isEnemy ? EnemyInner : PlayerInner;
        if (glow != null)
        {
            float a = isEnemy ? 0.28f : d.rarity switch
            {
                CardRarity.Legendary => 0.55f,
                CardRarity.Epic => 0.4f,
                CardRarity.Rare => 0.22f,
                _ => 0f,
            };
            glow.color = VisualTheme.WithA(isEnemy ? VisualTheme.EnemyAccent : rarity, a);
        }
        if (namePlate != null)
        {
            namePlate.color = isEnemy ? EnemyPlate : PlayerPlate;
            var ol = namePlate.GetComponent<Outline>();
            if (ol != null) ol.effectColor = VisualTheme.WithA(d.rarity == CardRarity.Common && !isEnemy ? Color.black : frame.color, 0.9f);
        }
        if (rarityDot != null) rarityDot.color = frame.color;
        if (title != null) title.text = d.Title;

        if (art != null)
        {
            art.sprite = d.art != null ? d.art : fallbackArt;
            if (artFitter != null && art.sprite != null)
            {
                artFitter.aspectRatio = art.sprite.rect.width / Mathf.Max(1f, art.sprite.rect.height);
            }
        }

        if (card != null)
        {
            if (card.cardDmg != null) card.cardDmg.text = d.Damage.ToString();
            if (card.cardHp != null) card.cardHp.text = d.HP.ToString();
            if (card.cardCost != null) card.cardCost.text = d.Cost.ToString();
        }
        if (costGem != null) costGem.gameObject.SetActive(!isEnemy);

        ShowStatuses(d.statuses);
    }

    /// <summary>Draw a fixed status list (CardData specs). Live stacks come from the Card's StatusHolder automatically.</summary>
    public void ShowStatuses(IList<StatusSpec> statuses)
    {
        if (statusRow == null) return;
        var row = statusRow.GetComponent<StatusRowView>();
        if (row == null) row = StatusRowView.Setup(statusRow, 17, 11);
        if (card != null && Application.isPlaying) row.Track(card);
        row.ShowSpecs(statuses);
    }

    /// <summary>
    /// Builds / re-styles the visual hierarchy on this card. Idempotent (re-uses existing nodes);
    /// runs on every spawn so the look is defined here, not by what was baked into the prefab.
    /// Never touches gameplay components, colliders, tags or the Card text references.
    /// Layout (100x145): art window on top, name plate, cost gem on the top-left corner,
    /// ATK / HP discs on the bottom corners, rarity dot between them.
    /// </summary>
    public void BuildVisuals()
    {
        if (card == null) card = GetComponent<Card>();
        var root = (RectTransform)transform;

        // Root Image stays as the (invisible) raycast target for drag / drop / tooltip.
        var rootImg = GetComponent<Image>();
        if (rootImg != null) { rootImg.color = new Color(1, 1, 1, 0f); rootImg.raycastTarget = true; }
        foreach (var fx in GetComponents<Shadow>()) fx.enabled = false; // Outline derives from Shadow

        // Art: existing "ImageCard" child (keep the object, just move it into a masked window).
        var artImg = FindDeep(transform, "ImageCard")?.GetComponent<Image>();
        if (artImg != null && fallbackArt == null) fallbackArt = artImg.sprite;

        glow = VisualTheme.Img(VisualTheme.Stretch("V_Glow", root, -14f), ProcSprites.Glow, new Color(1, 1, 1, 0));
        var shadowRt = VisualTheme.Node("V_Shadow", root, Vector2.zero, Vector2.one, new Vector2(-10, -16), new Vector2(10, 4));
        shadow = VisualTheme.Img(shadowRt, ProcSprites.SoftShadow, new Color(0, 0, 0, 0.7f), true);
        frame = VisualTheme.Img(VisualTheme.Stretch("V_Frame", root), ProcSprites.RoundRectSmall, Color.white, true);
        var frameOl = VisualTheme.Ensure<Outline>(frame.gameObject);
        frameOl.effectColor = VisualTheme.Outline; frameOl.effectDistance = new Vector2(1.5f, -1.5f);
        var dta = GetComponent<Assets.Scrpits.Card.Animation.DamageTextAnimator>();
        if (dta != null) { dta.flashTarget = frame; dta.hitTarget = transform; }
        inner = VisualTheme.Img(VisualTheme.Stretch("V_Inner", root, 3f), ProcSprites.RoundRectSmall, isEnemy ? EnemyInner : PlayerInner, true);

        // Art window (top ~70%): dark backdrop + mask + art (envelope fit) + light vignette.
        artWindow = VisualTheme.Node("V_ArtWindow", root, new Vector2(0, 0.3f), new Vector2(1, 1), new Vector2(6, 0), new Vector2(-6, -6));
        VisualTheme.Img(artWindow, ProcSprites.RoundRectSmall, new Color32(0x0E, 0x0C, 0x13, 0xFF), true);
        if (artWindow.GetComponent<Mask>() == null) artWindow.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        if (artImg != null)
        {
            var art_rt = (RectTransform)artImg.transform;
            art_rt.SetParent(artWindow, false);
            art_rt.anchorMin = art_rt.anchorMax = new Vector2(0.5f, 0.5f);
            art_rt.anchoredPosition = Vector2.zero;
            art_rt.localScale = Vector3.one;
            artImg.raycastTarget = false;
            artImg.maskable = true;
            artImg.preserveAspect = false;
            artImg.color = Color.white;
            art = artImg;
            artFitter = VisualTheme.Ensure<AspectRatioFitter>(artImg.gameObject);
            artFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            if (artImg.sprite != null) artFitter.aspectRatio = artImg.sprite.rect.width / Mathf.Max(1f, artImg.sprite.rect.height);
        }
        var vig = VisualTheme.Img(VisualTheme.Stretch("V_ArtVignette", artWindow), ProcSprites.Vignette, new Color(1, 1, 1, 0.35f));
        vig.maskable = true;
        vig.transform.SetAsLastSibling();

        // Name plate: dark steel pill, rarity-coloured rim (Outline).
        var plate = VisualTheme.Node("V_NamePlate", root, new Vector2(0, 0.19f), new Vector2(1, 0.33f), new Vector2(4, 0), new Vector2(-4, 0));
        namePlate = VisualTheme.Img(plate, ProcSprites.RoundRectSmall, PlayerPlate, true);
        var plateOl = VisualTheme.Ensure<Outline>(plate.gameObject);
        plateOl.effectColor = new Color(0, 0, 0, 0.9f); plateOl.effectDistance = new Vector2(1.2f, -1.2f);
        var titleRt = VisualTheme.Stretch("V_Title", plate, 3f);
        title = VisualTheme.Txt(titleRt, name, 14, UiTheme.Text);
        title.enableAutoSizing = true; title.fontSizeMin = 8; title.fontSizeMax = 15;
        title.overflowMode = TextOverflowModes.Ellipsis;

        // Status row sits on the bottom edge of the art window.
        statusRow = VisualTheme.Node("V_StatusRow", root, new Vector2(0, 0.33f), new Vector2(1, 0.33f), new Vector2(8, 2), new Vector2(-8, 20));
        StatusRowView.Setup(statusRow, 17, 11);

        // Stat badges: reuse the existing containers/text (Card.cs keeps writing into the same TMP objects).
        costGem = RestyleBadge("Magic", "MagicCost", card != null ? card.cardCost : null, new Vector2(0.11f, 0.93f), ProcSprites.Gem, UiTheme.Mana, new Vector2(34, 34), 20);
        atkBadge = RestyleBadge("DmgCard", "SwordImage", card != null ? card.cardDmg : null, new Vector2(0.13f, 0.09f), ProcSprites.Circle, VisualTheme.AttackBadge, new Vector2(31, 31), 19);
        hpBadge = RestyleBadge("HP", "HPImage", card != null ? card.cardHp : null, new Vector2(0.87f, 0.09f), ProcSprites.Circle, VisualTheme.HpBadge, new Vector2(31, 31), 19);

        // Old baked divider is gone; a small rarity dot sits between the stat discs instead.
        var oldBar = root.Find("V_BottomBar");
        if (oldBar != null) oldBar.gameObject.SetActive(false);
        var dot = VisualTheme.Centered("V_RarityDot", root, new Vector2(0.5f, 0.09f), new Vector2(9, 9));
        rarityDot = VisualTheme.Img(dot, ProcSprites.Circle, Color.white);
        VisualTheme.Ensure<Outline>(dot.gameObject).effectColor = VisualTheme.Outline;

        // Z-order: glow, shadow, frame, inner, art, plate, status, dot, badges, damage text (top).
        int i = 0;
        glow.transform.SetSiblingIndex(i++);
        shadow.transform.SetSiblingIndex(i++);
        frame.transform.SetSiblingIndex(i++);
        inner.transform.SetSiblingIndex(i++);
        artWindow.SetSiblingIndex(i++);
        plate.SetSiblingIndex(i++);
        statusRow.SetSiblingIndex(i++);
        dot.SetSiblingIndex(i++);
        // RatCard keeps its badges under a plain "Visual" transform: lift that above the new layers.
        var visual = transform.Find("Visual");
        if (visual != null) visual.SetSiblingIndex(i++);
        if (costGem != null) costGem.transform.parent.SetAsLastSibling();
        if (card != null && card.damageText != null)
        {
            VisualTheme.Style(card.damageText, 44, UiTheme.Damage);
            card.damageText.transform.SetAsLastSibling();
        }
        // Keep physics helpers last (no graphics, order irrelevant) - untouched otherwise.
    }

    private Image RestyleBadge(string holderName, string imageName, TMP_Text text, Vector2 anchor, string sprite, Color color, Vector2 size, float fontSize)
    {
        var holder = FindDeep(transform, holderName) as RectTransform;
        var imgTr = FindDeep(transform, imageName) as RectTransform;
        if (holder == null || imgTr == null) return null;
        // Holder may live under "Visual" (plain Transform) - re-anchor relative to the card root rect.
        if (holder.parent != transform && holder.parent.GetComponent<RectTransform>() == null)
        {
            holder.SetParent(transform, false);
        }
        holder.anchorMin = holder.anchorMax = anchor;
        holder.anchoredPosition = Vector2.zero;
        holder.sizeDelta = Vector2.zero;
        imgTr.anchorMin = imgTr.anchorMax = new Vector2(0.5f, 0.5f);
        imgTr.pivot = new Vector2(0.5f, 0.5f);
        imgTr.anchoredPosition = Vector2.zero;
        imgTr.sizeDelta = size;
        var img = imgTr.GetComponent<Image>();
        img.sprite = ProcSprites.Get(sprite);
        img.type = Image.Type.Simple;
        img.preserveAspect = false;
        img.color = color;
        img.raycastTarget = false;
        var ol = VisualTheme.Ensure<Outline>(img.gameObject);
        ol.effectColor = VisualTheme.Outline; ol.effectDistance = new Vector2(1.5f, -1.5f);
        var sh = img.GetComponents<Shadow>();
        if (sh.Length < 2) { var s2 = img.gameObject.AddComponent<Shadow>(); s2.effectColor = new Color(0, 0, 0, 0.5f); s2.effectDistance = new Vector2(0, -3); }
        if (text != null)
        {
            var trt = (RectTransform)text.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            VisualTheme.Style(text, fontSize, Color.white);
        }
        return img;
    }

    private static Transform FindDeep(Transform t, string n)
    {
        if (t.name == n) return t;
        foreach (Transform c in t)
        {
            var r = FindDeep(c, n);
            if (r != null) return r;
        }
        return null;
    }
}
