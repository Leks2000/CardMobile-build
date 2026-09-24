using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Процедурные иконки предметов (без внешнего арта): SDF-фигура + вертикальный градиент + тёмный контур + блик.
/// Цвет задаётся при запросе (<see cref="Get"/>), спрайты кешируются по (форма, цвет).
/// </summary>
public static class ItemIcons
{
    public const string Potion = "potion";
    public const string Shield = "shield";
    public const string Crystal = "crystal";
    public const string Scroll = "scroll";
    public const string Dagger = "dagger";
    public const string Bomb = "bomb";
    public const string Flask = "flask";
    public const string Star = "star";
    public const string Drop = "drop";
    public const string Heart = "heart";
    public const string Coin = "coin";
    public const string Ring = "ring";
    public const string Cup = "cup";
    public const string Helmet = "helmet";
    public const string Fang = "fang";
    public const string Clover = "clover";
    public const string Cross = "cross";

    private const int Size = 128;
    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string shape, Color color)
    {
        string key = shape + "_" + ColorUtility.ToHtmlStringRGB(color);
        if (cache.TryGetValue(key, out var s) && s != null) return s;
        var sd = Shape(shape);
        Color light = Color.Lerp(color, Color.white, 0.35f);
        Color dark = Color.Lerp(color, Color.black, 0.45f);
        s = MakeShaded(sd, light, dark, new Color(0.07f, 0.05f, 0.09f, 1f));
        s.name = "item_" + key;
        cache[key] = s;
        return s;
    }

    /// <summary>Нарисованные иконки из набора проекта (Resources/Art/UI) для предметов; остальные - процедурные.</summary>
    private static readonly Dictionary<string, string> Art = new Dictionary<string, string>
    {
        ["heal_potion"] = "status_heal",
        ["iron_skin"] = "status_shield",
        ["insight_scroll"] = "loc_map",
        ["holy_water"] = "relic_heal",
        ["powder_bomb"] = "status_stun",
        ["venom_flask"] = "status_poison",
        ["battle_cry"] = "status_atk",
        ["phoenix_elixir"] = "status_fire",
        ["hard_hat"] = "relic_shield",
        ["whetstone"] = "relic_atk",
        ["first_aid"] = "relic_heal",
        ["piggy_bank"] = "relic_moneybag",
        ["venom_vial"] = "relic_poison",
        ["deck_pouch"] = "relic_banner",
        ["lucky_clover"] = "relic_skullcoin",
        ["heart_amulet"] = "relic_amulet",
        ["vampire_fang"] = "relic_fang",
        ["golden_ring"] = "relic_ring",
    };

    public static Sprite Get(ItemDef item)
    {
        if (item == null) return null;
        if (Art.TryGetValue(item.id, out var art))
        {
            var s = ArtLib.UI(art);
            if (s != null) return s;
        }
        return Get(item.icon, item.color);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => cache.Clear();

    // ---------------- shapes (128x128, y вверх, d < 0 внутри) ----------------

    private static Func<Vector2, float> Shape(string shape)
    {
        switch (shape)
        {
            case Potion:
                return p => Min(Circle(p, 64, 50, 40), Box(p, 64, 92, 13, 14, 3), Box(p, 64, 106, 19, 6, 3), Box(p, 64, 117, 11, 6, 2));
            case Shield:
                return p => Polygon(p, new[] { V(20, 112), V(108, 112), V(106, 64), V(64, 10), V(22, 64) }) - 2f;
            case Crystal:
                return p => Polygon(p, new[] { V(64, 122), V(98, 84), V(64, 6), V(30, 84) });
            case Scroll:
                return p => Min(Box(p, 64, 64, 32, 40, 4), Box(p, 64, 104, 42, 10, 9), Box(p, 64, 24, 42, 10, 9));
            case Dagger:
                return p => Min(Polygon(p, new[] { V(50, 46), V(78, 46), V(78, 98), V(64, 124), V(50, 98) }),
                    Box(p, 64, 40, 34, 7, 3), Box(p, 64, 24, 8, 12, 2), Circle(p, 64, 9, 9));
            case Bomb:
                return p => Min(Circle(p, 58, 52, 42), Box(p, 84, 94, 10, 10, 2), Capsule(p, V(88, 102), V(104, 118), 4));
            case Flask:
                return p => Min(Box(p, 64, 100, 11, 18, 3), Box(p, 64, 118, 17, 5, 3),
                    Polygon(p, new[] { V(20, 12), V(108, 12), V(76, 86), V(52, 86) }) - 3f);
            case Star:
                return p => StarSd(p, V(64, 62), 58, 26);
            case Drop:
                return p => Min(Circle(p, 64, 46, 38), Polygon(p, new[] { V(31, 64), V(97, 64), V(64, 124) }));
            case Heart:
                return p => Min(Circle(p, 42, 80, 30), Circle(p, 86, 80, 30), Polygon(p, new[] { V(16, 72), V(112, 72), V(64, 10) }));
            case Coin:
                return p => Mathf.Max(Circle(p, 64, 64, 56), -(Mathf.Abs(Circle(p, 64, 64, 40)) - 3f));
            case Ring:
                return p => Min(Mathf.Abs(Circle(p, 64, 50, 36)) - 10f, Polygon(p, new[] { V(64, 124), V(84, 100), V(64, 80), V(44, 100) }));
            case Cup:
                return p => Min(Box(p, 56, 58, 34, 38, 10), Mathf.Abs(Circle(p, 94, 62, 18)) - 6f, Box(p, 64, 14, 52, 6, 6));
            case Helmet:
                return p => Min(Mathf.Max(Circle(p, 64, 44, 50), 44f - p.y), Box(p, 64, 44, 60, 8, 6), Box(p, 64, 96, 8, 10, 3));
            case Fang:
                return p => Polygon(p, new[] { V(38, 118), V(90, 118), V(86, 82), V(64, 8), V(42, 82) }) - 2f;
            case Clover:
                return p => Min(Min(Circle(p, 64, 94, 22), Circle(p, 64, 50, 22)), Min(Circle(p, 42, 72, 22), Circle(p, 86, 72, 22)), Capsule(p, V(64, 60), V(80, 10), 5));
            case Cross:
                return p => Min(Box(p, 64, 64, 18, 52, 5), Box(p, 64, 64, 52, 18, 5));
            default:
                return p => Circle(p, 64, 64, 50);
        }
    }

    private static Vector2 V(float x, float y) => new Vector2(x, y);
    private static float Min(float a, float b) => Mathf.Min(a, b);
    private static float Min(float a, float b, float c) => Mathf.Min(a, Mathf.Min(b, c));
    private static float Min(float a, float b, float c, float d) => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));

    private static float Circle(Vector2 p, float cx, float cy, float r) => Vector2.Distance(p, new Vector2(cx, cy)) - r;

    private static float Box(Vector2 p, float cx, float cy, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(p.x - cx) - hw + r, qy = Mathf.Abs(p.y - cy) - hh + r;
        return new Vector2(Mathf.Max(qx, 0), Mathf.Max(qy, 0)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    private static float Capsule(Vector2 p, Vector2 a, Vector2 b, float r)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude - r;
    }

    private static float StarSd(Vector2 p, Vector2 c, float outer, float inner)
    {
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? outer : inner;
            pts[i] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
        }
        return Polygon(p, pts);
    }

    private static float Polygon(Vector2 p, Vector2[] v)
    {
        float d = Vector2.Dot(p - v[0], p - v[0]);
        float s = 1f;
        for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
        {
            Vector2 e = v[j] - v[i];
            Vector2 w = p - v[i];
            Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Vector2.Dot(e, e));
            d = Mathf.Min(d, Vector2.Dot(b, b));
            bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
            if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s *= -1f;
        }
        return s * Mathf.Sqrt(d);
    }

    // ---------------- raster ----------------

    private static Sprite MakeShaded(Func<Vector2, float> sd, Color light, Color dark, Color outline)
    {
        const float ow = 5f;
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
        var px = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d = sd(p);
                float a = Mathf.Clamp01(0.5f - d);
                if (a <= 0f) { px[y * Size + x] = new Color32(0, 0, 0, 0); continue; }
                float t = p.y / Size;
                Color fill = Color.Lerp(dark, light, t);
                float rim = Mathf.Clamp01((-d - ow) / 7f);
                fill = Color.Lerp(fill * 0.78f, fill, rim);
                // блик слева сверху
                float spec = Mathf.Clamp01(1f - Vector2.Distance(p, new Vector2(Size * 0.36f, Size * 0.72f)) / (Size * 0.28f));
                fill = Color.Lerp(fill, Color.white, spec * spec * 0.45f * rim);
                float inner = Mathf.Clamp01(-d - ow + 0.5f);
                Color c = Color.Lerp(outline, fill, inner);
                c.a = a;
                px[y * Size + x] = c;
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        var s = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }
}
