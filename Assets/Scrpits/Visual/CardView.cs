using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Presentation of a card (player or enemy), in the style of the project's first card design:
/// the art fills the whole card inside a thin frame, and each number sits next to the project's
/// hand-drawn icon (Images/CardUi: mana drop, sword, heart) so it is obvious what it means:
///   top    - name strip, mana cost [drop 2] on the right (player cards only);
///   bottom - [sword 2] attack on the left, [2 heart] HP on the right.
/// The frame colour shows rarity (enemies: red). Everything stays inside the card rect.
/// Gameplay stays on <see cref="Card"/> / CardDrag / attack components; this only draws.
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

    // Иконки проекта (рисованные) - те же, что в первой версии карт и в HUD маны.
    private const string ManaIcon = "Images/CardUi/Manna";
    private const string AttackIcon = "Images/CardUi/Sword";
    private const string HpIcon = "Images/CardUi/Hurt";
    private static readonly Color ManaTint = new Color(0.24f, 0.55f, 1f, 1f); // как капля маны в HUD

    private static readonly Color PlayerInner = new Color32(0x2A, 0x24, 0x30, 0xFF);
    private static readonly Color EnemyInner = new Color32(0x30, 0x18, 0x1C, 0xFF);
    private static readonly Color CommonFrame = new Color32(0x1C, 0x16, 0x22, 0xFF);
    private static readonly Color PillColor = new Color(0.05f, 0.04f, 0.07f, 0.8f);

    private static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();

    private static Sprite Icon(string path)
    {
        if (!icons.TryGetValue(path, out var s) || s == null)
        {
            s = Resources.Load<Sprite>(path);
            icons[path] = s;
        }
        return s;
    }

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

    /// <summary>Re-read the data and redraw everything (call after HP/damage/status changes).</summary>
    public void Refresh()
    {
        var d = Data;
        if (d == null || frame == null) return;

        var rarity = RarityColors.Get(d.rarity);
        if (isEnemy) frame.color = Color.Lerp(CommonFrame, VisualTheme.EnemyAccent, 0.75f);
        else frame.color = d.rarity == CardRarity.Common ? CommonFrame : rarity;
        if (inner != null) inner.color = isEnemy ? EnemyInner : PlayerInner;
        if (glow != null)
        {
            float a = isEnemy ? 0f : d.rarity == CardRarity.Legendary ? 0.45f : d.rarity == CardRarity.Epic ? 0.3f : 0f;
            glow.color = VisualTheme.WithA(rarity, a);
        }
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
        // у врагов нет цены
        if (costGem != null && costGem.transform.parent != null) costGem.transform.parent.gameObject.SetActive(!isEnemy);

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
    /// </summary>
    public void BuildVisuals()
    {
        if (card == null) card = GetComponent<Card>();
        var root = (RectTransform)transform;

        // Root Image stays as the (invisible) raycast target for drag / drop / tooltip.
        var rootImg = GetComponent<Image>();
        if (rootImg != null) { rootImg.color = new Color(1, 1, 1, 0f); rootImg.raycastTarget = true; }
        foreach (var fx in GetComponents<Shadow>()) fx.enabled = false; // Outline derives from Shadow

        var artImg = FindDeep(transform, "ImageCard")?.GetComponent<Image>();
        if (artImg != null && fallbackArt == null) fallbackArt = artImg.sprite;

        glow = VisualTheme.Img(VisualTheme.Stretch("V_Glow", root, -6f), ProcSprites.Glow, new Color(1, 1, 1, 0));
        var shadowRt = VisualTheme.Node("V_Shadow", root, Vector2.zero, Vector2.one, new Vector2(-3, -7), new Vector2(3, 1));
        shadow = VisualTheme.Img(shadowRt, ProcSprites.SoftShadow, new Color(0, 0, 0, 0.55f), true);
        // thin frame (rarity colour) + dark card body
        frame = VisualTheme.Img(VisualTheme.Stretch("V_Frame", root), ProcSprites.RoundRectSmall, CommonFrame, true);
        VisualTheme.Ensure<Outline>(frame.gameObject).enabled = false;
        var dta = GetComponent<Assets.Scrpits.Card.Animation.DamageTextAnimator>();
        if (dta != null) { dta.flashTarget = frame; dta.hitTarget = transform; }
        inner = VisualTheme.Img(VisualTheme.Stretch("V_Inner", root, 3f), ProcSprites.RoundRectSmall, isEnemy ? EnemyInner : PlayerInner, true);

        // Art fills the whole card (as in the first design).
        artWindow = VisualTheme.Stretch("V_ArtWindow", root, 4f);
        VisualTheme.Img(artWindow, ProcSprites.RoundRectSmall, isEnemy ? EnemyInner : PlayerInner, true);
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
        var oldVig = artWindow.Find("V_ArtVignette");
        if (oldVig != null) oldVig.gameObject.SetActive(false);

        // Name strip at the top (dark band over the art), leaves room for the mana pill on the right.
        var plate = VisualTheme.Node("V_NamePlate", root, new Vector2(0, 0.86f), new Vector2(1, 1), new Vector2(4, 0), new Vector2(-4, -4));
        namePlate = VisualTheme.Img(plate, ProcSprites.RoundRectSmall, PillColor, true);
        var plateOl = plate.GetComponent<Outline>();
        if (plateOl != null) plateOl.enabled = false;
        var titleRt = VisualTheme.Node("V_Title", plate, Vector2.zero, Vector2.one, new Vector2(5, 1), new Vector2(isEnemy ? -5 : -34, -1));
        title = VisualTheme.Txt(titleRt, name, 12, UiTheme.Text);
        title.alignment = TextAlignmentOptions.Left;
        title.enableAutoSizing = true; title.fontSizeMin = 7; title.fontSizeMax = 12;
        title.overflowMode = TextOverflowModes.Ellipsis;

        // Status icons under the name strip.
        statusRow = VisualTheme.Node("V_StatusRow", root, new Vector2(0, 0.86f), new Vector2(1, 0.86f), new Vector2(6, -20), new Vector2(-6, -2));
        StatusRowView.Setup(statusRow, 17, 11);

        // Stat pills: icon + number. Existing text objects are reused (Card.cs keeps writing into them).
        costGem = StatPill("Magic", "MagicCost", card != null ? card.cardCost : null,
            new Vector2(0.64f, 0.86f), new Vector2(1f, 1f), new Vector2(0, 0), new Vector2(-4, -4), ManaIcon, ManaTint, true);
        atkBadge = StatPill("DmgCard", "SwordImage", card != null ? card.cardDmg : null,
            new Vector2(0f, 0f), new Vector2(0.46f, 0.17f), new Vector2(4, 4), Vector2.zero, AttackIcon, Color.white, true);
        hpBadge = StatPill("HP", "HPImage", card != null ? card.cardHp : null,
            new Vector2(0.54f, 0f), new Vector2(1f, 0.17f), Vector2.zero, new Vector2(-4, 4), HpIcon, Color.white, false);

        // Old baked parts from previous layouts.
        foreach (var old in new[] { "V_BottomBar", "V_RarityDot" })
        {
            var t = root.Find(old);
            if (t != null) t.gameObject.SetActive(false);
        }

        // Z-order: glow, shadow, frame, inner, art, name, status, pills, damage text (top).
        int i = 0;
        glow.transform.SetSiblingIndex(i++);
        shadow.transform.SetSiblingIndex(i++);
        frame.transform.SetSiblingIndex(i++);
        inner.transform.SetSiblingIndex(i++);
        artWindow.SetSiblingIndex(i++);
        plate.SetSiblingIndex(i++);
        statusRow.SetSiblingIndex(i++);
        // RatCard keeps its badges under a plain "Visual" transform: lift that above the new layers.
        var visual = transform.Find("Visual");
        if (visual != null) visual.SetSiblingIndex(i++);
        foreach (var b in new[] { costGem, atkBadge, hpBadge })
        {
            if (b != null && b.transform.parent != null) b.transform.parent.SetAsLastSibling();
        }
        if (card != null && card.damageText != null)
        {
            VisualTheme.Style(card.damageText, 44, UiTheme.Damage);
            card.damageText.transform.SetAsLastSibling();
        }
    }

    /// <summary>
    /// Dark pill (anchored rect) with the stat icon on one side and the number on the other.
    /// The holder / icon / text objects already exist on the card prefabs and are only re-arranged.
    /// </summary>
    private Image StatPill(string holderName, string imageName, TMP_Text text, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax,
        string iconPath, Color iconColor, bool iconLeft)
    {
        var holder = FindDeep(transform, holderName) as RectTransform;
        var imgTr = FindDeep(transform, imageName) as RectTransform;
        if (holder == null || imgTr == null) return null;
        // Holder may live under "Visual" (plain Transform) - re-anchor relative to the card root rect.
        if (holder.parent != transform && holder.parent.GetComponent<RectTransform>() == null)
        {
            holder.SetParent(transform, false);
        }
        holder.anchorMin = aMin; holder.anchorMax = aMax;
        holder.pivot = new Vector2(0.5f, 0.5f);
        holder.offsetMin = oMin; holder.offsetMax = oMax;
        holder.localScale = Vector3.one;
        var pill = VisualTheme.Img(holder, ProcSprites.RoundRectSmall, PillColor, true);
        pill.raycastTarget = false;

        // icon: half of the pill, keeps its drawn proportions
        imgTr.SetParent(holder, false);
        imgTr.anchorMin = new Vector2(iconLeft ? 0f : 0.5f, 0f);
        imgTr.anchorMax = new Vector2(iconLeft ? 0.5f : 1f, 1f);
        imgTr.pivot = new Vector2(0.5f, 0.5f);
        imgTr.offsetMin = new Vector2(1, 1); imgTr.offsetMax = new Vector2(-1, -1);
        imgTr.localScale = Vector3.one;
        imgTr.localRotation = Quaternion.identity;
        var img = imgTr.GetComponent<Image>();
        var sprite = Icon(iconPath);
        if (sprite != null) img.sprite = sprite;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        img.color = iconColor;
        img.raycastTarget = false;
        foreach (var s in img.GetComponents<Shadow>()) s.enabled = false;

        // number: the other half
        if (text != null)
        {
            var trt = (RectTransform)text.transform;
            trt.SetParent(holder, false);
            trt.anchorMin = new Vector2(iconLeft ? 0.45f : 0f, 0f);
            trt.anchorMax = new Vector2(iconLeft ? 1f : 0.55f, 1f);
            trt.pivot = new Vector2(0.5f, 0.5f);
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            trt.localScale = Vector3.one;
            VisualTheme.Style(text, 16, Color.white);
            text.enableAutoSizing = true; text.fontSizeMin = 9; text.fontSizeMax = 16;
            text.transform.SetAsLastSibling();
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
