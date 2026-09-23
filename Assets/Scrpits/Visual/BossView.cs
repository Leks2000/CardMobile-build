using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Boss presentation (no new art: existing boss sprite + generated glow shapes):
/// tinted aura, soft ground shadow, idle breathing / float, nameplate, HP bar with damage-lag trail,
/// hit reaction (recoil + flash + aura flare + shards). Driven by <see cref="Boss"/>.
/// </summary>
public class BossView : MonoBehaviour
{
    public RectTransform rig;        // parent of the boss image (breathing / float lives here)
    public Image portrait;           // Boss.bossImage
    public Image aura;
    public Image auraOuter;
    public Image groundShadow;
    public TMP_Text nameText;
    public TMP_Text subtitleText;
    public HpBarView hpBar;
    public StatusRowView statusRow;

    public float floatAmplitude = 8f;
    public float floatSpeed = 1.1f;
    public float breathe = 0.025f;

    private Color tint = Color.white;
    private Vector2 rigBasePos;
    private float phase;
    private float flare;

    private void Awake()
    {
        if (rig != null) rigBasePos = rig.anchoredPosition;
        phase = Random.value * 10f;
    }

    private void Start()
    {
        if (statusRow != null) statusRow.Track(GetComponent<Boss>());
    }

    /// <summary>Apply BossData look (name, sprite, tint) - called by Boss on data change.</summary>
    public void ApplyData(BossData data)
    {
        if (data == null) return;
        if (nameText != null && !string.IsNullOrEmpty(data.displayName)) nameText.text = data.displayName;
        if (portrait != null && data.sprite != null) portrait.sprite = data.sprite;
        tint = data.tint.a <= 0.01f ? Color.white : data.tint;
        if (subtitleText != null)
        {
            var type = Encounters.Type;
            subtitleText.text = type == Assets.Scrpits.Map.MapNodeType.Boss ? "BOSS" : type == Assets.Scrpits.Map.MapNodeType.Elite ? "ELITE" : "GUARDIAN";
            subtitleText.color = type == Assets.Scrpits.Map.MapNodeType.Battle ? UiTheme.TextDim : UiTheme.Accent;
        }
        // Tint the portrait lightly; the aura carries most of the colour.
        if (portrait != null) portrait.color = Color.Lerp(Color.white, tint, 0.25f);
        UpdateAura(0f);
    }

    public void SetHp(float hp, float max, bool animate = true)
    {
        if (hpBar != null) hpBar.Set(hp, max, animate);
    }

    public void PlayHit(int damage)
    {
        flare = 1f;
        if (portrait != null)
        {
            var rt = portrait.rectTransform;
            rt.DOKill(true);
            rt.DOPunchAnchorPos(new Vector2(-14f, 6f), 0.35f, 12, 0.6f).SetLink(portrait.gameObject);
            ImpactFx.Burst(rt, UiTheme.Damage, Mathf.Clamp(6 + damage, 6, 14), 1.4f);
        }
    }

    public void PlayDeath()
    {
        if (rig == null) return;
        rig.DOKill();
        var cg = VisualTheme.Ensure<CanvasGroup>(rig.gameObject);
        rig.DOScale(0.7f, 0.8f).SetEase(Ease.InBack).SetLink(rig.gameObject);
        cg.DOFade(0.15f, 0.8f).SetLink(rig.gameObject);
        if (aura != null) aura.DOFade(0f, 0.8f).SetLink(aura.gameObject);
    }

    private void Update()
    {
        float t = Time.time * floatSpeed + phase;
        if (rig != null)
        {
            rig.anchoredPosition = rigBasePos + new Vector2(0f, Mathf.Sin(t) * floatAmplitude);
            float s = 1f + Mathf.Sin(t * 2f) * breathe;
            rig.localScale = new Vector3(1f / Mathf.Sqrt(s), s, 1f);
        }
        if (groundShadow != null)
        {
            // shadow shrinks while the boss floats up
            float k = 1f - (Mathf.Sin(t) * 0.5f + 0.5f) * 0.18f;
            groundShadow.rectTransform.localScale = new Vector3(k, k, 1f);
        }
        flare = Mathf.MoveTowards(flare, 0f, Time.deltaTime * 2.5f);
        UpdateAura(t);
    }

    private void UpdateAura(float t)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 1.6f);
        if (aura != null)
        {
            var c = Color.Lerp(tint, Color.white, flare * 0.6f);
            aura.color = new Color(c.r, c.g, c.b, 0.38f + 0.12f * pulse + 0.35f * flare);
            aura.rectTransform.localScale = Vector3.one * (1f + 0.05f * pulse + 0.15f * flare);
        }
        if (auraOuter != null)
        {
            auraOuter.color = new Color(tint.r, tint.g, tint.b, 0.14f + 0.08f * (1f - pulse));
            auraOuter.rectTransform.localEulerAngles = new Vector3(0, 0, t * 6f);
        }
    }
}
