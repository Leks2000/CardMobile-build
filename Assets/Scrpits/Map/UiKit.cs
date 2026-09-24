using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Мини-набор для постройки uGUI из кода (карта, магазин кейсов) +
    /// процедурные спрайты (круги, свечение, скруглённые панели, иконки) - без внешнего арта.
    /// Текстуры генерируются один раз в рантайме и кешируются.
    /// </summary>
    public static class UiKit
    {
        // ---------------- builders ----------------

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Img(string name, Transform parent, Color color, Sprite sprite = null, bool sliced = false)
        {
            var img = Rect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            if (sliced) img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var t = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (UiTheme.Font != null) t.font = UiTheme.Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Кнопка: скруглённая панель + текст. Возвращает Button; текст - child "Text".</summary>
        public static Button Button(string name, Transform parent, string label, Color color, Vector2 size, Action onClick, float fontSize = 44)
        {
            var img = Img(name, parent, color, ButtonShape, true);
            img.raycastTarget = true;
            img.rectTransform.sizeDelta = size;
            var sh = img.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.45f);
            sh.effectDistance = new Vector2(0, -6);
            var btn = img.gameObject.AddComponent<Button>();
            var cb = btn.colors;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.disabledColor = new Color(0.45f, 0.45f, 0.5f, 0.8f);
            btn.colors = cb;
            btn.onClick.AddListener(() => SoundFx.Play(SoundFx.Clip.Click, 0.6f));
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            var t = Text("Text", img.rectTransform, label, fontSize, UiTheme.Background);
            Stretch(t.rectTransform, 10, 10, 4, 12); // над нижней кромкой кнопки
            return btn;
        }

        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        /// <summary>Есть нарисованная монета проекта (Resources/Art/UI/coin).</summary>
        public static bool HasCoinArt => ArtLib.UI("coin") != null;

        /// <summary>Иконка монеты: арт проекта, иначе процедурный золотой круг.</summary>
        public static Image Coin(string name, Transform parent)
        {
            var art = ArtLib.UI("coin");
            var img = Img(name, parent, art != null ? Color.white : new Color(1f, 0.9f, 0.4f), art != null ? art : Circle);
            img.preserveAspect = true;
            return img;
        }

        // ---------------- procedural sprites ----------------

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Circle => Get("circle", () => Make(128, 128, p => SdCircle(p, new Vector2(64, 64), 62), null));
        public static Sprite Ring => Get("ring", () => Make(128, 128, p => Mathf.Abs(SdCircle(p, new Vector2(64, 64), 56)) - 6f, null));
        /// <summary>Мягкое радиальное свечение (центр ярче).</summary>
        public static Sprite Glow => Get("glow", () => MakeAlpha(128, 128, p =>
        {
            float d = Vector2.Distance(p, new Vector2(64, 64)) / 64f;
            float a = Mathf.Clamp01(1f - d);
            return a * a * a;
        }));
        /// <summary>Лучи для вспышки (reveal).</summary>
        public static Sprite Rays => Get("rays", () => MakeAlpha(256, 256, p =>
        {
            Vector2 v = p - new Vector2(128, 128);
            float d = v.magnitude / 128f;
            if (d > 1f) return 0f;
            float ang = Mathf.Atan2(v.y, v.x);
            float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 6f)), 6f);
            return ray * Mathf.Clamp01(1f - d) * Mathf.Clamp01(d * 4f);
        }));
        /// <summary>Скруглённый прямоугольник под 9-slice (border 24).</summary>
        public static Sprite RoundedRect => Get("rrect", () => Make(64, 64, p => SdRoundBox(p - new Vector2(32, 32), new Vector2(31, 31), 22), null, 24));
        /// <summary>
        /// Игровая кнопка под 9-slice (border 24): тёмный контур, светлая верхняя грань, основной цвет,
        /// тёмная «губа» снизу (объём). Белая - тинтуется Image.color.
        /// </summary>
        public static Sprite ButtonShape => Get("button", () => MakeColored(64, 64, p =>
        {
            float d = SdRoundBox(p - new Vector2(32, 32), new Vector2(31, 31), 16);
            float a = Mathf.Clamp01(0.5f - d);
            if (a <= 0f) return new Color(0, 0, 0, 0);
            Color outline = new Color(0.09f, 0.06f, 0.1f, a);
            if (d > -3.5f) return outline;
            float shade;
            if (p.y < 13f) shade = 0.58f;                 // нижняя кромка
            else if (p.y < 15f) shade = 0.45f;            // линия перелома
            else if (p.y > 52f && d < -5f) shade = 1f;    // блик сверху
            else shade = 0.86f;                           // лицевая часть
            float edge = Mathf.Clamp01(-d - 3.5f);        // мягкий переход к контуру
            var c = Color.Lerp(new Color(0.09f, 0.06f, 0.1f), new Color(shade, shade, shade), edge);
            c.a = a;
            return c;
        }, 24));

        /// <summary>Рамка скруглённого прямоугольника под 9-slice.</summary>
        public static Sprite RoundedFrame => Get("rframe", () => Make(64, 64, p => Mathf.Abs(SdRoundBox(p - new Vector2(32, 32), new Vector2(28, 28), 20)) - 3f, null, 24));
        /// <summary>Вертикальный градиент (белый сверху -> прозрачный снизу), тинтуется.</summary>
        public static Sprite VGradient => Get("vgrad", () => MakeAlpha(4, 64, p => Mathf.Clamp01(p.y / 63f)));

        /// <summary>Пешка игрока: основание + тело + голова, золотое с тёмным контуром и бликом.</summary>
        public static Sprite Pawn => Get("pawn", () => MakeShaded(96, 128, p =>
        {
            float baseD = SdRoundBox(p - new Vector2(48, 16), new Vector2(38, 10), 8);
            float neck = SdRoundBox(p - new Vector2(48, 32), new Vector2(26, 6), 5);
            float body = SdTrapezoid(p - new Vector2(48, 58), 24, 11, 26);
            float collar = SdRoundBox(p - new Vector2(48, 82), new Vector2(18, 5), 4);
            float head = SdCircle(p, new Vector2(48, 100), 21);
            return Mathf.Min(Mathf.Min(Mathf.Min(baseD, neck), Mathf.Min(body, collar)), head);
        }, new Color32(0xFF, 0xE0, 0x8A, 255), new Color32(0xC8, 0x82, 0x1E, 255), new Color32(0x3A, 0x22, 0x08, 255)));

        /// <summary>Корона (элита/босс).</summary>
        public static Sprite Crown => Get("crown", () => MakeShaded(128, 96, p =>
        {
            float band = SdRoundBox(p - new Vector2(64, 20), new Vector2(48, 12), 4);
            float tri = SdPolygon(p, new[]
            {
                new Vector2(16, 26), new Vector2(10, 78), new Vector2(38, 50), new Vector2(64, 88),
                new Vector2(90, 50), new Vector2(118, 78), new Vector2(112, 26)
            });
            float gems = Mathf.Min(Mathf.Min(SdCircle(p, new Vector2(10, 80), 7), SdCircle(p, new Vector2(64, 90), 8)), SdCircle(p, new Vector2(118, 80), 7));
            return Mathf.Min(Mathf.Min(band, tri), gems);
        }, new Color32(0xFF, 0xE8, 0x9A, 255), new Color32(0xD8, 0x8E, 0x1A, 255), new Color32(0x3A, 0x22, 0x08, 255)));

        /// <summary>Череп (босс).</summary>
        public static Sprite Skull => Get("skull", () => MakeShaded(112, 128, p =>
        {
            float cranium = SdCircle(p, new Vector2(56, 76), 44);
            float jaw = SdRoundBox(p - new Vector2(56, 30), new Vector2(26, 20), 8);
            float shape = Mathf.Min(cranium, jaw);
            float eyeL = SdCircle(p, new Vector2(38, 72), 12);
            float eyeR = SdCircle(p, new Vector2(74, 72), 12);
            float nose = SdPolygon(p, new[] { new Vector2(56, 58), new Vector2(49, 44), new Vector2(63, 44) });
            float teeth = Mathf.Min(SdRoundBox(p - new Vector2(46, 18), new Vector2(2.5f, 10), 1), SdRoundBox(p - new Vector2(66, 18), new Vector2(2.5f, 10), 1));
            float holes = Mathf.Min(Mathf.Min(eyeL, eyeR), Mathf.Min(nose, teeth));
            return Mathf.Max(shape, -holes);
        }, new Color32(0xFA, 0xF2, 0xE4, 255), new Color32(0xB8, 0xA8, 0x94, 255), new Color32(0x2A, 0x10, 0x10, 255)));

        /// <summary>Меч остриём вверх (обычный бой). Поворачивать через Image.</summary>
        public static Sprite Sword => Get("sword", () => MakeShaded(64, 128, p =>
        {
            float blade = SdPolygon(p, new[] { new Vector2(25, 40), new Vector2(39, 40), new Vector2(39, 104), new Vector2(32, 122), new Vector2(25, 104) });
            float guard = SdRoundBox(p - new Vector2(32, 36), new Vector2(22, 5), 3);
            float grip = SdRoundBox(p - new Vector2(32, 20), new Vector2(5, 13), 2);
            float pommel = SdCircle(p, new Vector2(32, 8), 7);
            return Mathf.Min(Mathf.Min(blade, guard), Mathf.Min(grip, pommel));
        }, new Color32(0xF2, 0xF4, 0xF8, 255), new Color32(0x9A, 0xA4, 0xB4, 255), new Color32(0x1E, 0x18, 0x24, 255)));

        /// <summary>Пламя (привал).</summary>
        public static Sprite Flame => Get("flame", () => MakeShaded(96, 128, p =>
        {
            float outer = Mathf.Min(SdCircle(p, new Vector2(48, 44), 34), SdPolygon(p, new[] { new Vector2(16, 52), new Vector2(46, 124), new Vector2(80, 52) }));
            float tongueL = SdPolygon(p, new[] { new Vector2(14, 40), new Vector2(8, 88), new Vector2(34, 60) });
            float shape = Mathf.Min(outer, tongueL);
            float logs = Mathf.Min(SdRoundBox(p - new Vector2(48, 8), new Vector2(40, 6), 5), shape);
            return logs;
        }, new Color32(0xFF, 0xE6, 0x6A, 255), new Color32(0xE8, 0x4A, 0x1A, 255), new Color32(0x3A, 0x12, 0x06, 255)));

        /// <summary>Сундук (кейс) - используется в магазине; тинтуется Image.color не нужно - цвета задаются тут.</summary>
        public static Sprite Chest(string key, Color body, Color bands) => Get("chest_" + key, () =>
        {
            Color outline = new Color(0.08f, 0.05f, 0.04f, 1f);
            return MakeColored(160, 128, p =>
            {
                float boxD = SdRoundBox(p - new Vector2(80, 44), new Vector2(66, 38), 8);
                float lidD = SdRoundBox(p - new Vector2(80, 94), new Vector2(66, 24), 18);
                float all = Mathf.Min(boxD, lidD);
                if (all > 2.5f) return new Color(0, 0, 0, 0);
                if (all > -3.5f) return outline * new Color(1, 1, 1, Mathf.Clamp01(2.5f - all));
                // бэнды и замок
                bool band = Mathf.Abs(p.x - 30) < 7 || Mathf.Abs(p.x - 130) < 7 || Mathf.Abs(p.y - 74) < 5;
                float lockD = SdRoundBox(p - new Vector2(80, 70), new Vector2(13, 16), 4);
                float keyhole = Mathf.Min(SdCircle(p, new Vector2(80, 74), 4), SdRoundBox(p - new Vector2(80, 64), new Vector2(1.8f, 6), 1));
                if (keyhole < 0) return outline;
                if (lockD < 0) return Color.Lerp(bands, Color.white, 0.35f);
                if (lockD < 2.5f) return outline;
                Color c = band ? bands : body;
                // объём: сверху светлее, внизу темнее
                float shade = Mathf.Lerp(0.72f, 1.12f, p.y / 128f);
                // планки дерева / листы металла
                if (!band && (Mathf.Repeat(p.y, 16f) < 1.5f)) shade *= 0.82f;
                c = new Color(c.r * shade, c.g * shade, c.b * shade, 1f);
                return c;
            });
        });

        private static Sprite Get(string key, Func<Sprite> make)
        {
            if (cache.TryGetValue(key, out var s) && s != null) return s;
            s = make();
            cache[key] = s;
            return s;
        }

        private static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            return tex;
        }

        private static Sprite ToSprite(Texture2D tex, float border)
        {
            tex.Apply(false, true);
            var s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            s.hideFlags = HideFlags.DontSave;
            return s;
        }

        /// <summary>Белая фигура по SDF (d&lt;0 внутри), с AA краем.</summary>
        private static Sprite Make(int w, int h, Func<Vector2, float> sd, Func<Vector2, Color> color, float border = 0)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - sd(p));
                    Color c = color != null ? color(p) : Color.white;
                    c.a *= a;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            return ToSprite(tex, border);
        }

        private static Sprite MakeAlpha(int w, int h, Func<Vector2, float> alpha)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color(1, 1, 1, alpha(new Vector2(x + 0.5f, y + 0.5f)));
            tex.SetPixels32(px);
            return ToSprite(tex, 0);
        }

        private static Sprite MakeColored(int w, int h, Func<Vector2, Color> color, float border = 0)
        {
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = color(new Vector2(x + 0.5f, y + 0.5f));
            tex.SetPixels32(px);
            return ToSprite(tex, border);
        }

        /// <summary>Фигура с контуром, вертикальным градиентом (light сверху, dark снизу) и бликом слева.</summary>
        private static Sprite MakeShaded(int w, int h, Func<Vector2, float> sd, Color light, Color dark, Color outline)
        {
            const float ow = 4f;
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = sd(p);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f) { px[y * w + x] = new Color(0, 0, 0, 0); continue; }
                    float t = p.y / h;
                    Color fill = Color.Lerp(dark, light, t);
                    // блик: левая сторона светлее, внутренняя тень у края
                    float rim = Mathf.Clamp01((-d - ow) / 6f);
                    fill = Color.Lerp(fill * 0.8f, fill, rim);
                    float side = Mathf.Clamp01(1f - Mathf.Abs(p.x / w - 0.35f) * 5f);
                    fill = Color.Lerp(fill, Color.white, side * 0.25f * rim);
                    float inner = Mathf.Clamp01(-d - ow + 0.5f);
                    Color c = Color.Lerp(outline, fill, inner);
                    c.a = a;
                    px[y * w + x] = c;
                }
            tex.SetPixels32(px);
            return ToSprite(tex, 0);
        }

        // ---------------- SDF primitives ----------------

        private static float SdCircle(Vector2 p, Vector2 c, float r) => Vector2.Distance(p, c) - r;

        private static float SdRoundBox(Vector2 p, Vector2 halfSize, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - halfSize + new Vector2(r, r);
            return new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - r;
        }

        /// <summary>Равнобокая трапеция: r1 - полуширина низа, r2 - верха, he - полувысота.</summary>
        private static float SdTrapezoid(Vector2 p, float r1, float r2, float he)
        {
            Vector2 k1 = new Vector2(r2, he);
            Vector2 k2 = new Vector2(r2 - r1, 2f * he);
            p.x = Mathf.Abs(p.x);
            Vector2 ca = new Vector2(p.x - Mathf.Min(p.x, p.y < 0 ? r1 : r2), Mathf.Abs(p.y) - he);
            Vector2 cb = p - k1 + k2 * Mathf.Clamp01(Vector2.Dot(k1 - p, k2) / Vector2.Dot(k2, k2));
            float s = (cb.x < 0 && ca.y < 0) ? -1f : 1f;
            return s * Mathf.Sqrt(Mathf.Min(Vector2.Dot(ca, ca), Vector2.Dot(cb, cb)));
        }

        private static float SdPolygon(Vector2 p, Vector2[] v)
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
    }
}
