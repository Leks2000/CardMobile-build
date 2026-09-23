using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Pooled uGUI particle effects (impact shards burst, glow flash, expanding ring, trail dots).
/// One pool + updater per canvas; particles live in the target canvas plane (works for the tilted
/// world-space board and for screen-space HUD). No DOTween, no allocations after warm-up.
/// </summary>
public static class ImpactFx
{
    private class P
    {
        public RectTransform rt;
        public Image img;
        public Vector2 pos, vel;
        public float life, age, size0, size1, rot, spin, drag;
        public Color color;
        public bool stretch;
    }

    private class Pool : MonoBehaviour
    {
        public readonly List<P> active = new List<P>();
        public readonly Stack<P> free = new Stack<P>();

        public P Get(string sprite)
        {
            var p = free.Count > 0 ? free.Pop() : null;
            if (p == null)
            {
                var go = new GameObject("fx", typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                p = new P { rt = (RectTransform)go.transform, img = go.AddComponent<Image>() };
                p.img.raycastTarget = false;
                p.rt.anchorMin = p.rt.anchorMax = new Vector2(0.5f, 0.5f);
            }
            p.img.sprite = ProcSprites.Get(sprite);
            p.rt.gameObject.SetActive(true);
            p.age = 0f;
            active.Add(p);
            return p;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var p = active[i];
                p.age += dt;
                float k = p.age / p.life;
                if (k >= 1f || p.rt == null)
                {
                    if (p.rt != null) p.rt.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    if (p.rt != null) free.Push(p);
                    continue;
                }
                p.vel *= Mathf.Max(0f, 1f - p.drag * dt);
                p.pos += p.vel * dt;
                p.rot += p.spin * dt;
                float s = Mathf.Lerp(p.size0, p.size1, 1f - (1f - k) * (1f - k));
                p.rt.anchoredPosition = p.pos;
                if (p.stretch)
                {
                    float speed = p.vel.magnitude;
                    p.rt.sizeDelta = new Vector2(s * 0.5f, s * (1f + speed * 0.004f));
                    p.rt.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(p.vel.y, p.vel.x) * Mathf.Rad2Deg - 90f);
                }
                else
                {
                    p.rt.sizeDelta = new Vector2(s, s);
                    p.rt.localEulerAngles = new Vector3(0, 0, p.rot);
                }
                var c = p.color;
                c.a *= 1f - k * k;
                p.img.color = c;
            }
        }
    }

    private static readonly Dictionary<int, Pool> pools = new Dictionary<int, Pool>();

    private static Pool PoolFor(Transform target, out Vector2 local, out float unit)
    {
        local = Vector2.zero;
        unit = 1f;
        if (!Application.isPlaying || target == null) return null;
        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        canvas = canvas.rootCanvas;
        int id = canvas.GetInstanceID();
        if (!pools.TryGetValue(id, out var pool) || pool == null)
        {
            var go = new GameObject("[ImpactFx]", typeof(RectTransform));
            go.layer = canvas.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(canvas.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            // own sorting so particles draw above cards on the same canvas
            var sub = go.AddComponent<Canvas>();
            sub.overrideSorting = true;
            sub.sortingLayerID = canvas.sortingLayerID;
            sub.sortingOrder = canvas.sortingOrder + 20;
            pool = go.AddComponent<Pool>();
            pools[id] = pool;
        }
        pool.transform.SetAsLastSibling();
        var prt = (RectTransform)pool.transform;
        local = prt.InverseTransformPoint(target.position);
        var trt = target as RectTransform;
        if (trt != null)
        {
            // size of the target measured in the pool's space (handles nested scales)
            var sz = trt.rect.size;
            var w = prt.InverseTransformVector(trt.TransformVector(new Vector3(sz.x, 0, 0))).magnitude;
            var h = prt.InverseTransformVector(trt.TransformVector(new Vector3(0, sz.y, 0))).magnitude;
            unit = Mathf.Max(0.3f, Mathf.Max(w, h) / 145f);
        }
        return pool;
    }

    /// <summary>Impact: flash + shards flying out from the target centre.</summary>
    public static void Burst(Transform target, Color color, int shards = 8, float scale = 1f)
    {
        var pool = PoolFor(target, out var at, out var unit);
        if (pool == null) return;
        unit *= scale;
        var flash = pool.Get(ProcSprites.Glow);
        flash.pos = at; flash.vel = Vector2.zero; flash.life = 0.28f;
        flash.size0 = 60f * unit; flash.size1 = 190f * unit; flash.color = new Color(1f, 0.95f, 0.85f, 0.9f);
        flash.stretch = false; flash.drag = 0; flash.spin = 0;

        var tintGlow = pool.Get(ProcSprites.Glow);
        tintGlow.pos = at; tintGlow.vel = Vector2.zero; tintGlow.life = 0.45f;
        tintGlow.size0 = 90f * unit; tintGlow.size1 = 230f * unit; tintGlow.color = new Color(color.r, color.g, color.b, 0.6f);
        tintGlow.stretch = false; tintGlow.drag = 0; tintGlow.spin = 0;

        for (int i = 0; i < shards; i++)
        {
            var p = pool.Get(ProcSprites.Shard);
            float a = (i + Random.value * 0.6f) / shards * Mathf.PI * 2f;
            float sp = Random.Range(380f, 620f) * unit;
            p.pos = at; p.vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * sp;
            p.life = Random.Range(0.3f, 0.45f); p.drag = 5f;
            p.size0 = Random.Range(20f, 30f) * unit; p.size1 = 6f * unit;
            p.color = i % 3 == 0 ? Color.white : Color.Lerp(color, Color.white, 0.25f);
            p.stretch = true;
        }
    }

    /// <summary>Expanding ring (card landing, slot confirm).</summary>
    public static void Ring(Transform target, Color color, float scale = 1f, float life = 0.45f)
    {
        var pool = PoolFor(target, out var at, out var unit);
        if (pool == null) return;
        unit *= scale;
        var p = pool.Get(ProcSprites.Ring);
        p.pos = at; p.vel = Vector2.zero; p.life = life; p.drag = 0; p.spin = 0; p.stretch = false;
        p.size0 = 70f * unit; p.size1 = 260f * unit; p.color = color;
    }

    /// <summary>Soft dots scattered upward (e.g. play landing dust, heal sparkle).</summary>
    public static void Sparkle(Transform target, Color color, int count = 6, float scale = 1f)
    {
        var pool = PoolFor(target, out var at, out var unit);
        if (pool == null) return;
        unit *= scale;
        for (int i = 0; i < count; i++)
        {
            var p = pool.Get(ProcSprites.Glow);
            p.pos = at + Random.insideUnitCircle * 50f * unit;
            p.vel = new Vector2(Random.Range(-60f, 60f), Random.Range(80f, 200f)) * unit;
            p.life = Random.Range(0.4f, 0.7f); p.drag = 2f; p.spin = 0; p.stretch = false;
            p.size0 = Random.Range(18f, 30f) * unit; p.size1 = 4f * unit; p.color = color;
        }
    }

    /// <summary>Single trail dot at the target's current position (called every frame by a follower).</summary>
    public static void TrailDot(Transform target, Color color, float size = 34f)
    {
        var pool = PoolFor(target, out var at, out var unit);
        if (pool == null) return;
        var p = pool.Get(ProcSprites.Glow);
        p.pos = at; p.vel = Vector2.zero; p.life = 0.3f; p.drag = 0; p.spin = 0; p.stretch = false;
        p.size0 = size * unit; p.size1 = size * 0.2f * unit; p.color = color;
    }
}
