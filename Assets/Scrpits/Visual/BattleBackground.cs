using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Animated procedural backdrop: scrolling fog layers (RawImage uv scroll on the generated,
/// X-tileable fog texture), slowly breathing centre light and a handful of rising embers.
/// All uGUI, ~20 quads, no allocations per frame.
/// </summary>
public class BattleBackground : MonoBehaviour
{
    [System.Serializable]
    public class FogLayer
    {
        public RawImage image;
        public float speed = 0.01f;
        public float bob = 0.01f;
    }

    public List<FogLayer> fog = new List<FogLayer>();
    public Image centerLight;
    public RectTransform emberRoot;
    public int emberCount = 16;
    public Color emberColor = new Color(1f, 0.55f, 0.25f, 0.6f);

    private struct Ember { public RectTransform rt; public Image img; public float x, speed, phase, size, life, age; }
    private readonly List<Ember> embers = new List<Ember>();
    private Color lightBase;

    private void Start()
    {
        if (centerLight != null) lightBase = centerLight.color;
        if (emberRoot == null) return;
        for (int i = 0; i < emberCount; i++)
        {
            var rt = VisualTheme.Centered("Ember" + i, emberRoot, new Vector2(0.5f, 0f), new Vector2(10, 10));
            var img = VisualTheme.Img(rt, ProcSprites.Glow, emberColor);
            var e = new Ember { rt = rt, img = img };
            Respawn(ref e, true);
            embers.Add(e);
        }
    }

    private void Respawn(ref Ember e, bool randomAge)
    {
        var w = emberRoot.rect.width;
        e.x = Random.Range(-0.5f, 0.5f) * w;
        e.speed = Random.Range(25f, 70f);
        e.phase = Random.value * 10f;
        e.size = Random.Range(6f, 16f);
        e.life = Random.Range(6f, 12f);
        e.age = randomAge ? Random.value * e.life : 0f;
    }

    private void Update()
    {
        float t = Time.time;
        for (int i = 0; i < fog.Count; i++)
        {
            var f = fog[i];
            if (f.image == null) continue;
            var r = f.image.uvRect;
            r.x = t * f.speed;
            r.y = Mathf.Sin(t * 0.2f + i) * f.bob;
            f.image.uvRect = r;
        }
        if (centerLight != null)
        {
            float k = 0.85f + 0.15f * Mathf.Sin(t * 0.7f);
            centerLight.color = new Color(lightBase.r, lightBase.g, lightBase.b, lightBase.a * k);
        }
        if (emberRoot == null) return;
        float h = emberRoot.rect.height;
        for (int i = 0; i < embers.Count; i++)
        {
            var e = embers[i];
            e.age += Time.deltaTime;
            if (e.age >= e.life) Respawn(ref e, false);
            float k = e.age / e.life;
            e.rt.anchoredPosition = new Vector2(e.x + Mathf.Sin(t * 0.8f + e.phase) * 30f, k * h * 0.9f);
            float a = Mathf.Sin(k * Mathf.PI) * emberColor.a * (0.6f + 0.4f * Mathf.Sin(t * 5f + e.phase));
            e.img.color = new Color(emberColor.r, emberColor.g, emberColor.b, a);
            e.rt.sizeDelta = new Vector2(e.size, e.size);
            embers[i] = e;
        }
    }
}
