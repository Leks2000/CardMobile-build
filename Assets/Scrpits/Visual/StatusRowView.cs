using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Row of small status icons (coloured disc + glyph + stacks) built from uGUI primitives.
/// Shows a live <see cref="StatusHolder"/> (runtime stacks) when <see cref="Track"/> is set,
/// otherwise whatever was passed to <see cref="ShowSpecs"/>.
/// </summary>
public class StatusRowView : MonoBehaviour
{
    public float iconSize = 17f;
    public float fontSize = 11f;

    private readonly List<GameObject> icons = new List<GameObject>();
    private StatusHolder holder;
    private Component owner;
    private IList<StatusSpec> specs;
    private float nextPoll;

    /// <summary>Follow the StatusHolder of this owner (created lazily by combat code).</summary>
    public void Track(Component statusOwner)
    {
        owner = statusOwner;
        TryAttach();
    }

    public void ShowSpecs(IList<StatusSpec> list)
    {
        specs = list;
        Redraw();
    }

    private void TryAttach()
    {
        if (holder != null || owner == null) return;
        if (owner.TryGetComponent<StatusHolder>(out var h))
        {
            holder = h;
            holder.Changed += Redraw;
            Redraw();
        }
    }

    private void Update()
    {
        // StatusHolder is added on first use: poll a few times per second until it exists.
        if (holder == null && owner != null && Time.unscaledTime >= nextPoll)
        {
            nextPoll = Time.unscaledTime + 0.25f;
            TryAttach();
        }
    }

    private void OnDestroy()
    {
        if (holder != null) holder.Changed -= Redraw;
    }

    public void Redraw()
    {
        var items = new List<(StatusType type, int value)>();
        if (holder != null && holder.Stacks.Count > 0)
        {
            foreach (var kv in holder.Stacks) items.Add((kv.Key, kv.Value));
        }
        else if (holder == null && specs != null)
        {
            foreach (var s in specs) items.Add((s.type, s.value));
        }

        while (icons.Count < items.Count) icons.Add(MakeIcon(icons.Count));
        for (int i = 0; i < icons.Count; i++)
        {
            bool on = i < items.Count;
            if (icons[i].activeSelf != on) icons[i].SetActive(on);
            if (!on) continue;
            var (type, value) = items[i];
            icons[i].GetComponent<Image>().color = UiTheme.StatusColor(type);
            icons[i].GetComponentInChildren<TMP_Text>(true).text = UiTheme.StatusGlyph(type) + (value > 0 ? "<size=65%>" + value + "</size>" : "");
        }
    }

    private GameObject MakeIcon(int i)
    {
        var rt = VisualTheme.Centered("Status" + i, transform, new Vector2(0, 0.5f), new Vector2(iconSize, iconSize));
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = iconSize; le.preferredHeight = iconSize;
        VisualTheme.Img(rt, ProcSprites.Circle, Color.white);
        var ol = rt.gameObject.AddComponent<Outline>();
        ol.effectColor = VisualTheme.Outline; ol.effectDistance = new Vector2(1, -1);
        VisualTheme.Txt(VisualTheme.Stretch("Glyph", rt), "", fontSize, Color.white);
        return rt.gameObject;
    }

    /// <summary>Adds a left-aligned horizontal layout to <paramref name="rt"/> and a StatusRowView.</summary>
    public static StatusRowView Setup(RectTransform rt, float iconSize, float fontSize, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var hl = VisualTheme.Ensure<HorizontalLayoutGroup>(rt.gameObject);
        hl.childAlignment = align; hl.spacing = 2;
        hl.childControlWidth = hl.childControlHeight = false;
        hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        var v = VisualTheme.Ensure<StatusRowView>(rt.gameObject);
        v.iconSize = iconSize; v.fontSize = fontSize;
        return v;
    }
}
