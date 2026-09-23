using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Helpers for the battle-screen look: outlined font material, text styling, simple UI node factory.
/// Colours come from <see cref="UiTheme"/> / <see cref="RarityColors"/>; this only adds a few derived tones.
/// </summary>
public static class VisualTheme
{
    public static readonly Color EnemyPanel = new Color32(0x3A, 0x14, 0x1E, 0xFF);
    public static readonly Color EnemyAccent = new Color32(0xE0, 0x3A, 0x3A, 0xFF);
    public static readonly Color PlayerPanel = new Color32(0x2B, 0x23, 0x3B, 0xFF);
    public static readonly Color AttackBadge = new Color32(0xF0, 0x9A, 0x2E, 0xFF);
    public static readonly Color HpBadge = new Color32(0xD9, 0x34, 0x45, 0xFF);
    public static readonly Color Shadow = new Color(0f, 0f, 0f, 0.55f);
    public static readonly Color Outline = new Color32(0x0C, 0x08, 0x12, 0xFF);

    private static Material outlineMat;

    /// <summary>Chewy SDF material with a dark outline + soft underlay (baked asset, runtime fallback).</summary>
    public static Material OutlineMaterial
    {
        get
        {
            if (outlineMat != null) return outlineMat;
            outlineMat = Resources.Load<Material>(ProcSprites.Folder + "/Chewy-Outline");
            if (outlineMat == null && UiTheme.Font != null)
            {
                outlineMat = new Material(UiTheme.Font.material) { name = "Chewy-Outline (runtime)" };
                ConfigureOutline(outlineMat);
            }
            return outlineMat;
        }
    }

    public static void ConfigureOutline(Material m)
    {
        m.EnableKeyword("OUTLINE_ON");
        m.EnableKeyword("UNDERLAY_ON");
        m.SetFloat("_OutlineWidth", 0.22f);
        m.SetColor("_OutlineColor", Outline);
        m.SetFloat("_FaceDilate", 0.15f);
        m.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.6f));
        m.SetFloat("_UnderlayOffsetX", 0.4f);
        m.SetFloat("_UnderlayOffsetY", -0.5f);
        m.SetFloat("_UnderlaySoftness", 0.35f);
    }

    /// <summary>Unified font + colour; outlined=true uses the outlined material (numbers, titles).</summary>
    public static void Style(TMP_Text t, float size, Color color, bool outlined = true, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        if (t == null) return;
        if (UiTheme.Font != null) t.font = UiTheme.Font;
        if (outlined && OutlineMaterial != null) t.fontSharedMaterial = OutlineMaterial;
        else if (UiTheme.Font != null) t.fontSharedMaterial = UiTheme.Font.material;
        t.enableAutoSizing = false;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = FontStyles.Normal;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
    }

    // ---------------- small node factory ----------------
    public static RectTransform Node(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var existing = parent.Find(name);
        var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one; rt.localRotation = Quaternion.identity;
        return rt;
    }

    public static RectTransform Stretch(string name, Transform parent, float inset = 0f)
        => Node(name, parent, Vector2.zero, Vector2.one, new Vector2(inset, inset), new Vector2(-inset, -inset));

    public static RectTransform Centered(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 offset = default)
    {
        var rt = Node(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
        rt.sizeDelta = size;
        rt.anchoredPosition = offset;
        return rt;
    }

    public static Image Img(RectTransform rt, string sprite, Color color, bool sliced = false)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite != null ? ProcSprites.Get(sprite) : null;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Txt(RectTransform rt, string text, float size, Color color, bool outlined = true)
    {
        var t = rt.GetComponent<TextMeshProUGUI>();
        if (t == null) t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        Style(t, size, color, outlined);
        return t;
    }

    /// <summary>GetComponent-or-add (no ?? on Unity objects: editor fake-null).</summary>
    public static T Ensure<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    public static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
    public static Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);
}
