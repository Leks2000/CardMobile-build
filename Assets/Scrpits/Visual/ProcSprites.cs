using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [V] Procedural UI sprites (no downloaded / AI art). Each sprite is generated from math
/// (rounded rects, discs, rings, radial glows, fog noise). The editor builder bakes them into
/// Assets/Resources/Generated/*.png so prefabs/scenes can reference them; at runtime
/// <see cref="Get"/> loads the baked PNG and falls back to generating the texture in memory.
/// </summary>
public static class ProcSprites
{
    public const string Folder = "Generated";

    public struct Spec
    {
        public string name;
        public int w, h;
        public Vector4 border; // 9-slice border (L,B,R,T) in pixels
        public System.Func<int, int, int, int, Color> pixel;
    }

    private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

    // ---- shared names ----
    public const string RoundRect = "ui_roundrect";
    public const string RoundRectSmall = "ui_roundrect_s";
    public const string RoundOutline = "ui_roundoutline";
    public const string SoftShadow = "ui_softshadow";
    public const string Circle = "ui_circle";
    public const string Ring = "ui_ring";
    public const string Glow = "ui_glow";
    public const string Gem = "ui_gem";
    public const string Shard = "ui_shard";
    public const string Vignette = "ui_vignette";
    public const string Fog = "ui_fog";
    public const string BarFill = "ui_barfill";
    public const string Gloss = "ui_gloss";

    public static IEnumerable<Spec> All()
    {
        yield return new Spec { name = RoundRect, w = 64, h = 64, border = new Vector4(22, 22, 22, 22), pixel = (x, y, w, h) => RoundRectPx(x, y, w, h, 20f, 0f) };
        yield return new Spec { name = RoundRectSmall, w = 32, h = 32, border = new Vector4(11, 11, 11, 11), pixel = (x, y, w, h) => RoundRectPx(x, y, w, h, 9f, 0f) };
        yield return new Spec { name = RoundOutline, w = 64, h = 64, border = new Vector4(22, 22, 22, 22), pixel = (x, y, w, h) => RoundRectPx(x, y, w, h, 20f, 5f) };
        yield return new Spec { name = SoftShadow, w = 96, h = 96, border = new Vector4(40, 40, 40, 40), pixel = SoftShadowPx };
        yield return new Spec { name = Circle, w = 64, h = 64, pixel = (x, y, w, h) => DiscPx(x, y, w, 0f) };
        yield return new Spec { name = Ring, w = 128, h = 128, pixel = (x, y, w, h) => DiscPx(x, y, w, 7f) };
        yield return new Spec { name = Glow, w = 128, h = 128, pixel = GlowPx };
        yield return new Spec { name = Gem, w = 64, h = 64, pixel = GemPx };
        yield return new Spec { name = Shard, w = 32, h = 32, pixel = ShardPx };
        yield return new Spec { name = Vignette, w = 256, h = 256, pixel = VignettePx };
        yield return new Spec { name = Fog, w = 256, h = 128, pixel = FogPx };
        yield return new Spec { name = BarFill, w = 32, h = 32, border = new Vector4(12, 12, 12, 12), pixel = BarFillPx };
        yield return new Spec { name = Gloss, w = 32, h = 64, border = new Vector4(8, 8, 8, 8), pixel = GlossPx };
    }

    /// <summary>Sprite by name: baked PNG from Resources/Generated, else generated in memory.</summary>
    public static Sprite Get(string name)
    {
        if (cache.TryGetValue(name, out var s) && s != null) return s;
        s = Resources.Load<Sprite>(Folder + "/" + name);
        if (s == null)
        {
            foreach (var spec in All())
            {
                if (spec.name != name) continue;
                var tex = Generate(spec);
                s = Sprite.Create(tex, new Rect(0, 0, spec.w, spec.h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, spec.border);
                s.name = name;
                break;
            }
        }
        cache[name] = s;
        return s;
    }

    public static Texture2D Generate(Spec spec)
    {
        var tex = new Texture2D(spec.w, spec.h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = spec.name };
        var px = new Color[spec.w * spec.h];
        for (int y = 0; y < spec.h; y++)
            for (int x = 0; x < spec.w; x++)
                px[y * spec.w + x] = spec.pixel(x, y, spec.w, spec.h);
        tex.SetPixels(px);
        tex.Apply(false, false);
        return tex;
    }

    // ---------------- pixel functions (white shapes, tinted by Image.color) ----------------
    private static float SdRoundBox(float px, float py, float hw, float hh, float r)
    {
        float qx = Mathf.Abs(px) - hw + r, qy = Mathf.Abs(py) - hh + r;
        float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    private static Color RoundRectPx(int x, int y, int w, int h, float r, float ring)
    {
        float cx = x + 0.5f - w * 0.5f, cy = y + 0.5f - h * 0.5f;
        float d = SdRoundBox(cx, cy, w * 0.5f - 1f, h * 0.5f - 1f, r);
        float a = Mathf.Clamp01(0.5f - d);
        if (ring > 0) a *= Mathf.Clamp01(d + ring + 0.5f);
        return new Color(1, 1, 1, a);
    }

    private static Color SoftShadowPx(int x, int y, int w, int h)
    {
        float cx = x + 0.5f - w * 0.5f, cy = y + 0.5f - h * 0.5f;
        float d = SdRoundBox(cx, cy, w * 0.5f - 22f, h * 0.5f - 22f, 12f);
        float a = 1f - Mathf.SmoothStep(-6f, 20f, d);
        return new Color(0, 0, 0, a * a);
    }

    private static Color DiscPx(int x, int y, int w, float ring)
    {
        float c = w * 0.5f;
        float d = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) - (c - 1f);
        float a = Mathf.Clamp01(0.5f - d);
        if (ring > 0) a *= Mathf.Clamp01(d + ring + 0.5f);
        return new Color(1, 1, 1, a);
    }

    private static Color GlowPx(int x, int y, int w, int h)
    {
        float c = w * 0.5f;
        float r = Mathf.Sqrt((x + 0.5f - c) * (x + 0.5f - c) + (y + 0.5f - c) * (y + 0.5f - c)) / c;
        float a = Mathf.Clamp01(1f - r);
        return new Color(1, 1, 1, a * a * (3 - 2 * a));
    }

    private static Color GemPx(int x, int y, int w, int h)
    {
        // faceted hexagon-ish gem: diamond distance with a lighter top facet
        float c = w * 0.5f;
        float dx = Mathf.Abs(x + 0.5f - c) / (c - 2f), dy = Mathf.Abs(y + 0.5f - c) / (c - 2f);
        float d = Mathf.Max(dx * 0.87f + dy * 0.5f, dy);
        float a = Mathf.Clamp01((1f - d) * c * 0.9f);
        float shade = (y > c ? 1f : 0.78f) * (x < c ? 1f : 0.9f);
        if (d < 0.55f) shade = Mathf.Lerp(shade, 1f, 0.35f);
        return new Color(shade, shade, shade, a);
    }

    private static Color ShardPx(int x, int y, int w, int h)
    {
        float c = w * 0.5f;
        float dx = Mathf.Abs(x + 0.5f - c) / (c * 0.35f), dy = Mathf.Abs(y + 0.5f - c) / (c - 1f);
        float d = dx + dy;
        return new Color(1, 1, 1, Mathf.Clamp01((1f - d) * 6f));
    }

    private static Color VignettePx(int x, int y, int w, int h)
    {
        float u = (x + 0.5f) / w * 2f - 1f, v = (y + 0.5f) / h * 2f - 1f;
        float r = Mathf.Sqrt(u * u * 0.8f + v * v);
        float a = Mathf.SmoothStep(0.45f, 1.35f, r);
        return new Color(0, 0, 0, a);
    }

    private static Color FogPx(int x, int y, int w, int h)
    {
        // tileable in X: sample Perlin on a circle-free wrap by blending two offsets
        float fx = (float)x / w, fy = (float)y / h;
        float n = 0f, amp = 0.6f, freq = 3f;
        for (int o = 0; o < 4; o++)
        {
            float a = Mathf.PerlinNoise(fx * freq + 11.3f, fy * freq * 0.7f + 3.1f);
            float b = Mathf.PerlinNoise((fx - 1f) * freq + 11.3f, fy * freq * 0.7f + 3.1f);
            n += Mathf.Lerp(a, b, fx) * amp;
            amp *= 0.5f; freq *= 2f;
        }
        float edge = Mathf.Sin(fy * Mathf.PI); // fade top/bottom
        float alpha = Mathf.Clamp01((n - 0.35f) * 1.6f) * edge;
        return new Color(1, 1, 1, alpha);
    }

    private static Color BarFillPx(int x, int y, int w, int h)
    {
        var c = RoundRectPx(x, y, w, h, 10f, 0f);
        float t = (y + 0.5f) / h;
        float shade = Mathf.Lerp(0.72f, 1f, t) + (t > 0.62f && t < 0.8f ? 0.12f : 0f);
        return new Color(Mathf.Min(1, shade), Mathf.Min(1, shade), Mathf.Min(1, shade), c.a);
    }

    private static Color GlossPx(int x, int y, int w, int h)
    {
        var c = RoundRectPx(x, y, w, h, 7f, 0f);
        float t = (y + 0.5f) / h;
        return new Color(1, 1, 1, c.a * Mathf.SmoothStep(0.45f, 1f, t) * 0.55f);
    }
}
