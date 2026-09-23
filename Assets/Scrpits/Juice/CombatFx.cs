using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static facade for lightweight combat "juice" (punch, flash, shake, death, banners, screen flash).
/// Purely visual: never changes gameplay state or timing.
/// </summary>
/// <remarks>
/// Animations run as coroutines on a hidden runner instead of DOTween tweens on purpose:
/// CardDrag.OnBeginDrag calls DOTween.PauseAll(), which would freeze a flash (card stuck red),
/// a banner (stuck on screen) or a death ghost. Coroutines are unaffected.
/// </remarks>
public static class CombatFx
{
    public static readonly Color DamageRed = new Color(1f, 0.25f, 0.2f, 1f);
    public static readonly Color ValidGreen = new Color(0.45f, 1f, 0.5f, 1f);
    public static readonly Color InvalidRed = new Color(1f, 0.35f, 0.35f, 1f);
    public static readonly Color VictoryGold = new Color(1f, 0.85f, 0.3f, 1f);

    // ---------- runner ----------
    private class Runner : MonoBehaviour { }
    private static Runner runner;

    private static Runner R
    {
        get
        {
            if (runner == null)
            {
                var go = new GameObject("[CombatFx]");
                go.hideFlags = HideFlags.DontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            return runner;
        }
    }

    private static readonly Dictionary<(int, string), Coroutine> running = new Dictionary<(int, string), Coroutine>();
    private static readonly Dictionary<(int, string), Action> restore = new Dictionary<(int, string), Action>();

    /// <summary>Starts a channel-exclusive animation on a target; a previous one on the same channel is stopped and restored.</summary>
    private static void Run(UnityEngine.Object target, string channel, IEnumerator routine, Action restoreFn)
    {
        if (!Application.isPlaying || target == null)
        {
            return;
        }
        var key = (target.GetInstanceID(), channel);
        if (running.TryGetValue(key, out var old) && old != null)
        {
            R.StopCoroutine(old);
            if (restore.TryGetValue(key, out var r))
            {
                r?.Invoke();
            }
        }
        restore[key] = restoreFn;
        running[key] = R.StartCoroutine(Wrap(key, routine));
    }

    private static IEnumerator Wrap((int, string) key, IEnumerator routine)
    {
        yield return routine;
        running.Remove(key);
        restore.Remove(key);
    }

    private static IEnumerator Tween(float duration, Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            step(t / duration);
            yield return null;
            t += Time.deltaTime;
        }
        step(1f);
    }

    // ---------- punch ----------
    /// <summary>Quick scale punch (overshoot then settle back to the original scale).</summary>
    public static void Punch(Transform target, float amount = 0.18f, float duration = 0.3f)
    {
        if (target == null)
        {
            return;
        }
        var key = (target.GetInstanceID(), "punch");
        Vector3 baseScale = target.localScale;
        // If a punch is already running, restore() below resets to its base first.
        if (restore.ContainsKey(key) && running.ContainsKey(key))
        {
            restore[key]?.Invoke();
            baseScale = target.localScale;
        }
        Run(target, "punch", PunchRoutine(target, baseScale, amount, duration), () => { if (target) target.localScale = baseScale; });
    }

    private static IEnumerator PunchRoutine(Transform target, Vector3 baseScale, float amount, float duration)
    {
        yield return Tween(duration, p =>
        {
            if (!target) return;
            // damped sine: fast up, small undershoot, settle
            float k = Mathf.Sin(p * Mathf.PI * 2.5f) * (1f - p) * amount;
            target.localScale = baseScale * (1f + k);
        });
        if (target) target.localScale = baseScale;
    }

    /// <summary>Scales a freshly spawned object up from zero (with a little overshoot).</summary>
    public static void PopIn(Transform target, float duration = 0.25f)
    {
        if (target == null)
        {
            return;
        }
        Vector3 baseScale = target.localScale;
        Run(target, "punch", PopRoutine(target, baseScale, duration), () => { if (target) target.localScale = baseScale; });
    }

    private static IEnumerator PopRoutine(Transform target, Vector3 baseScale, float duration)
    {
        yield return Tween(duration, p =>
        {
            if (!target) return;
            float s = 1f + 2.2f * Mathf.Pow(p - 1f, 3) + 1.2f * Mathf.Pow(p - 1f, 2); // easeOutBack
            target.localScale = baseScale * s;
        });
        if (target) target.localScale = baseScale;
    }

    // ---------- flash ----------
    /// <summary>Tints a Graphic to a color and fades back to its original color.</summary>
    public static void Flash(Graphic graphic, Color color, float duration = 0.25f)
    {
        if (graphic == null)
        {
            return;
        }
        var key = (graphic.GetInstanceID(), "flash");
        if (running.ContainsKey(key) && restore.TryGetValue(key, out var r))
        {
            r?.Invoke(); // restore the real base colour before capturing it
        }
        Color baseColor = graphic.color;
        Color target = new Color(color.r, color.g, color.b, baseColor.a);
        Run(graphic, "flash", Tween(duration, p =>
        {
            if (graphic) graphic.color = Color.Lerp(target, baseColor, p * p);
        }), () => { if (graphic) graphic.color = baseColor; });
    }

    /// <summary>Standard "got hit" feedback: red flash + punch on the target.</summary>
    public static void Hit(Transform target, Graphic flashGraphic)
    {
        Flash(flashGraphic, DamageRed, 0.3f);
        Punch(target, 0.15f, 0.3f);
    }

    // ---------- camera shake ----------
    /// <summary>
    /// Small additive camera shake that cooperates with scripts moving the camera
    /// (if someone else writes the position mid-shake, their value becomes the new base).
    /// </summary>
    public static void ShakeCamera(float strength = 0.6f, float duration = 0.25f, Camera cam = null)
    {
        cam = cam != null ? cam : Camera.main;
        if (cam == null)
        {
            return;
        }
        Run(cam, "shake", ShakeRoutine(cam.transform, strength, duration), null);
    }

    private static IEnumerator ShakeRoutine(Transform t, float strength, float duration)
    {
        Vector3 lastOffset = Vector3.zero;
        Vector3 lastWritten = t.position;
        float time = 0f;
        while (time < duration && t)
        {
            Vector3 basePos = t.position == lastWritten ? t.position - lastOffset : t.position;
            float falloff = 1f - time / duration;
            Vector2 rnd = UnityEngine.Random.insideUnitCircle * strength * falloff;
            lastOffset = t.right * rnd.x + t.up * rnd.y;
            t.position = basePos + lastOffset;
            lastWritten = t.position;
            yield return null;
            time += Time.deltaTime;
        }
        if (t && t.position == lastWritten)
        {
            t.position -= lastOffset;
        }
    }

    // ---------- death ----------
    /// <summary>
    /// Plays a shrink + fade "death" on a visual-only ghost copy of <paramref name="source"/>.
    /// The caller still destroys the original immediately, so gameplay timing is unchanged.
    /// </summary>
    public static void PlayDeath(GameObject source, Action onDone = null)
    {
        if (!Application.isPlaying || source == null)
        {
            onDone?.Invoke();
            return;
        }
        GameObject ghost = null;
        try
        {
            ghost = CreateGhost(source);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[CombatFx] death ghost failed: " + e.Message);
        }
        if (ghost == null)
        {
            onDone?.Invoke();
            return;
        }
        R.StartCoroutine(DeathRoutine(ghost, onDone));
    }

    private static readonly Type[] keepTypes =
    {
        typeof(Transform), typeof(CanvasRenderer), typeof(Graphic), typeof(CanvasGroup),
        typeof(BaseMeshEffect), typeof(Mask), typeof(RectMask2D), typeof(LayoutElement)
    };

    private static bool Keep(Component c)
    {
        if (c == null) return true;
        var t = c.GetType();
        foreach (var k in keepTypes)
        {
            if (k.IsAssignableFrom(t)) return true;
        }
        return false;
    }

    private static GameObject CreateGhost(GameObject source)
    {
        var srcRt = source.transform as RectTransform;
        var canvas = source.GetComponentInParent<Canvas>();
        if (srcRt == null || canvas == null)
        {
            return null;
        }
        Transform parent = canvas.rootCanvas.transform;

        // Instantiate under an inactive holder so no Awake/OnEnable runs on the copy.
        var holder = new GameObject("FxGhostHolder");
        holder.SetActive(false);
        var ghost = UnityEngine.Object.Instantiate(source, holder.transform, false);
        ghost.name = source.name + " (death fx)";

        // Strip all behaviour: only visuals remain. Scripts first (reverse order, so components
        // that [RequireComponent] others go before their dependencies), then colliders/renderers.
        for (int pass = 0; pass < 2; pass++)
        {
            var comps = ghost.GetComponentsInChildren<Component>(true);
            for (int i = comps.Length - 1; i >= 0; i--)
            {
                var c = comps[i];
                if (c == null || Keep(c)) continue;
                if (pass == 0 && !(c is MonoBehaviour)) continue;
                UnityEngine.Object.DestroyImmediate(c);
            }
        }
        foreach (var tr in ghost.GetComponentsInChildren<Transform>(true))
        {
            tr.gameObject.tag = "Untagged";
        }
        foreach (var g in ghost.GetComponentsInChildren<Graphic>(true))
        {
            g.raycastTarget = false;
        }

        var rt = (RectTransform)ghost.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = srcRt.pivot;
        rt.sizeDelta = srcRt.rect.size;
        rt.position = srcRt.position;
        rt.rotation = srcRt.rotation;
        Vector3 ps = parent.lossyScale;
        Vector3 ss = srcRt.lossyScale;
        rt.localScale = new Vector3(ps.x != 0 ? ss.x / ps.x : 1, ps.y != 0 ? ss.y / ps.y : 1, ps.z != 0 ? ss.z / ps.z : 1);
        rt.SetAsLastSibling();

        UnityEngine.Object.Destroy(holder);
        return ghost;
    }

    private static IEnumerator DeathRoutine(GameObject ghost, Action onDone)
    {
        var rt = ghost.transform;
        var cg = ghost.GetComponent<CanvasGroup>();
        if (cg == null) cg = ghost.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = false;
        var graphics = ghost.GetComponentsInChildren<Graphic>(true);
        var baseColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++) baseColors[i] = graphics[i].color;

        Vector3 baseScale = rt.localScale;
        Quaternion baseRot = rt.localRotation;
        float spin = UnityEngine.Random.value < 0.5f ? -12f : 12f;

        // 1) quick red flash + slight swell
        yield return Tween(0.08f, p =>
        {
            if (!ghost) return;
            for (int i = 0; i < graphics.Length; i++)
                if (graphics[i]) graphics[i].color = Color.Lerp(baseColors[i], new Color(DamageRed.r, DamageRed.g, DamageRed.b, baseColors[i].a), p);
            rt.localScale = baseScale * (1f + 0.08f * p);
        });
        // 2) shrink, tilt and fade out
        yield return Tween(0.3f, p =>
        {
            if (!ghost) return;
            float e = p * p;
            rt.localScale = baseScale * Mathf.Lerp(1.08f, 0.2f, e);
            rt.localRotation = baseRot * Quaternion.Euler(0, 0, spin * p);
            cg.alpha = 1f - e;
        });
        if (ghost) UnityEngine.Object.Destroy(ghost);
        onDone?.Invoke();
    }

    // ---------- overlay: banner + screen flash ----------
    private static Canvas overlay;
    private static TMP_FontAsset font;

    private static Canvas Overlay
    {
        get
        {
            if (overlay == null)
            {
                var go = new GameObject("[CombatFx Overlay]", typeof(RectTransform));
                go.hideFlags = HideFlags.DontSave;
                UnityEngine.Object.DontDestroyOnLoad(go);
                overlay = go.AddComponent<Canvas>();
                overlay.renderMode = RenderMode.ScreenSpaceOverlay;
                overlay.sortingOrder = 500;
                var scaler = go.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                // no GraphicRaycaster: overlay never blocks input
            }
            return overlay;
        }
    }

    /// <summary>Font used by runtime-built UI (matches the scene's TMP font when possible).</summary>
    public static TMP_FontAsset Font
    {
        get
        {
            if (font == null)
            {
                var any = UnityEngine.Object.FindAnyObjectByType<TextMeshProUGUI>();
                font = any != null ? any.font : TMP_Settings.defaultFontAsset;
            }
            return font;
        }
    }

    private static Image screenFlash;

    /// <summary>Full-screen colour flash (non-blocking), e.g. when the player is hurt.</summary>
    public static void ScreenFlash(Color color, float maxAlpha = 0.25f, float duration = 0.3f)
    {
        if (!Application.isPlaying) return;
        if (screenFlash == null)
        {
            var go = new GameObject("ScreenFlash", typeof(RectTransform));
            go.transform.SetParent(Overlay.transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            screenFlash = go.AddComponent<Image>();
            screenFlash.raycastTarget = false;
        }
        screenFlash.transform.SetAsFirstSibling();
        var img = screenFlash;
        Run(img, "screen", Tween(duration, p =>
        {
            if (img) img.color = new Color(color.r, color.g, color.b, maxAlpha * (1f - p) * (1f - p));
        }), () => { if (img) img.color = Color.clear; });
    }

    private static CanvasGroup bannerGroup;
    private static RectTransform bannerStrip;
    private static TextMeshProUGUI bannerText;

    /// <summary>Short, non-blocking centre-screen banner ("YOUR TURN", "ENEMY TURN", ...).</summary>
    public static void TurnBanner(string text, Color? textColor = null, float hold = 0.6f)
    {
        if (!Application.isPlaying) return;
        if (bannerGroup == null)
        {
            var root = new GameObject("TurnBanner", typeof(RectTransform));
            root.transform.SetParent(Overlay.transform, false);
            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = new Vector2(0, 0.5f); rrt.anchorMax = new Vector2(1, 0.5f);
            rrt.sizeDelta = new Vector2(0, 150); rrt.anchoredPosition = new Vector2(0, 120);
            bannerGroup = root.AddComponent<CanvasGroup>();
            bannerGroup.blocksRaycasts = false; bannerGroup.interactable = false;

            var strip = new GameObject("Strip", typeof(RectTransform));
            strip.transform.SetParent(root.transform, false);
            bannerStrip = (RectTransform)strip.transform;
            bannerStrip.anchorMin = Vector2.zero; bannerStrip.anchorMax = Vector2.one;
            bannerStrip.offsetMin = bannerStrip.offsetMax = Vector2.zero;
            var img = strip.AddComponent<Image>();
            img.color = new Color(0.05f, 0.03f, 0.08f, 0.7f);
            img.raycastTarget = false;

            var txt = new GameObject("Text", typeof(RectTransform));
            txt.transform.SetParent(root.transform, false);
            var trt = (RectTransform)txt.transform;
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
            bannerText = txt.AddComponent<TextMeshProUGUI>();
            bannerText.font = Font;
            bannerText.fontSize = 84;
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.alignment = TextAlignmentOptions.Center;
            bannerText.characterSpacing = 8;
            bannerText.raycastTarget = false;
            bannerText.outlineWidth = 0.2f;
            bannerText.outlineColor = new Color32(0, 0, 0, 200);
        }
        bannerGroup.transform.SetAsLastSibling();
        bannerText.text = text;
        bannerText.color = textColor ?? Color.white;
        var group = bannerGroup;
        Run(group, "banner", BannerRoutine(hold), () => { if (group) group.alpha = 0f; });
    }

    private static IEnumerator BannerRoutine(float hold)
    {
        var tr = bannerText.transform;
        bannerGroup.alpha = 0f;
        // strip opens vertically, text pops in
        yield return Tween(0.18f, p =>
        {
            bannerGroup.alpha = p;
            bannerStrip.localScale = new Vector3(1f, Mathf.Lerp(0.1f, 1f, 1f - (1f - p) * (1f - p)), 1f);
            tr.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, 1f - (1f - p) * (1f - p) * (1f - p));
        });
        yield return Tween(hold, p => tr.localScale = Vector3.one * (1f + 0.04f * p));
        yield return Tween(0.22f, p =>
        {
            bannerGroup.alpha = 1f - p;
            bannerStrip.localScale = new Vector3(1f, 1f - 0.9f * p * p, 1f);
        });
        bannerGroup.alpha = 0f;
    }

    /// <summary>Generic coroutine host for other juice components (immune to DOTween.PauseAll).</summary>
    public static Coroutine StartRoutine(IEnumerator routine) => Application.isPlaying ? R.StartCoroutine(routine) : null;
}
