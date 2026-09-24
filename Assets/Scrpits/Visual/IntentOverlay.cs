using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Intents] Намерения врагов: у каждой вражеской карты, которая ударит в следующей атаке, - красный меч
/// с уроном и целью («→ YOU», если бьёт тебя). На твоей карте под ударом - красная метка с суммой входящего урона.
/// Цель считается так же, как в CardForwardAttack (луч вперёд; дальнобойные - через своих).
/// Только иконки и цифры с обводкой, без подложек. Добавляется BattleSceneDresser.
/// </summary>
public class IntentOverlay : MonoBehaviour
{
    private const string SwordIcon = "Images/CardUi/Sword";
    private static readonly Color IntentRed = new Color32(0xFF, 0x4D, 0x4D, 0xFF);

    private float next;
    private Sprite sword;
    private readonly Dictionary<Card, int> incoming = new Dictionary<Card, int>();

    private void Start() => sword = Resources.Load<Sprite>(SwordIcon);

    private readonly HashSet<Transform> danger = new HashSet<Transform>();
    private static Sprite beamSprite;

    private void Update()
    {
        UpdateDropZones();
        PulseZones();
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 0.2f;
        Refresh();
    }

    // ---------------- zones ----------------

    private static readonly Color DangerColor = new Color(1f, 0.18f, 0.15f, 1f);
    private static readonly Color DropColor = new Color(1f, 0.8f, 0.35f, 1f);

    /// <summary>Пока тащишь карту: над свободными клетками поднимается золотой «столб» света.</summary>
    private void UpdateDropZones()
    {
        bool dragging = GlobalDragTracker.IsDraggingCard;
        foreach (var slot in FindObjectsByType<DropCard>(FindObjectsSortMode.None))
        {
            bool show = dragging && slot.canDrop && slot.GetComponentInChildren<Card>() == null;
            var z = slot.transform.Find("V_DropZone");
            if (!show) { if (z != null) z.gameObject.SetActive(false); continue; }
            if (z == null) z = Zone(slot.transform, "V_DropZone", DropColor);
            z.gameObject.SetActive(true);
        }
    }

    private void PulseZones()
    {
        float k = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5f);
        foreach (var slot in FindObjectsByType<DropCard>(FindObjectsSortMode.None))
        {
            foreach (var n in new[] { "V_Danger", "V_DropZone" })
            {
                var z = slot.transform.Find(n);
                if (z == null || !z.gameObject.activeSelf) continue;
                var baseC = n == "V_Danger" ? DangerColor : DropColor;
                foreach (var img in z.GetComponentsInChildren<Image>(true))
                {
                    float a = img.name == "Beam" ? 0.55f : 0.45f;
                    img.color = new Color(baseC.r, baseC.g, baseC.b, a * k);
                }
            }
        }
    }

    /// <summary>
    /// Зона на клетке: плоское свечение + «стена» света, стоящая вверх из поля (повёрнута на 90° к полю,
    /// в камерном канвасе это настоящий 3D-объём).
    /// </summary>
    private static Transform Zone(Transform slot, string name, Color color)
    {
        var root = VisualTheme.Stretch(name, slot);
        var flat = VisualTheme.Img(VisualTheme.Stretch("Flat", root, -10f), ProcSprites.Glow, color);
        flat.raycastTarget = false;
        var beamRt = VisualTheme.Node("BeamRoot", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        var slotRt = (RectTransform)slot;
        beamRt.sizeDelta = new Vector2(slotRt.rect.width * 0.95f, slotRt.rect.height * 0.9f);
        beamRt.pivot = new Vector2(0.5f, 0f);
        beamRt.anchoredPosition = new Vector2(0f, -slotRt.rect.height * 0.1f);
        beamRt.localEulerAngles = new Vector3(-90f, 0f, 0f); // встаёт из плоскости поля к камере
        var beam = VisualTheme.Img(VisualTheme.Stretch("Beam", beamRt), null, color);
        beam.sprite = BeamSprite();
        beam.raycastTarget = false;
        root.SetAsFirstSibling(); // под картой
        return root;
    }

    /// <summary>Вертикальный градиент: яркий у основания, прозрачный сверху.</summary>
    private static Sprite BeamSprite()
    {
        if (beamSprite != null) return beamSprite;
        var tex = new Texture2D(8, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[8 * 64];
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 8; x++)
            {
                float t = y / 63f;
                float edge = Mathf.Sin((x + 0.5f) / 8f * Mathf.PI);
                px[y * 8 + x] = new Color(1, 1, 1, (1f - t) * (1f - t) * (0.6f + 0.4f * edge));
            }
        tex.SetPixels32(px);
        tex.Apply();
        beamSprite = Sprite.Create(tex, new Rect(0, 0, 8, 64), new Vector2(0.5f, 0f), 100f);
        return beamSprite;
    }

    private void Refresh()
    {
        incoming.Clear();
        danger.Clear();
        var cards = FindObjectsByType<Card>(FindObjectsSortMode.None);
        foreach (var c in cards)
        {
            if (c == null || c.CardData == null || !c.CardData.isEnemy) continue;
            var badge = Badge(c, "V_Intent");
            if (c.CardData.HP <= 0 || !TryTarget(c, out var targetCard, out var hitsPlayer))
            {
                badge.gameObject.SetActive(false);
                continue;
            }
            int dmg = c.CardData.Damage;
            badge.gameObject.SetActive(true);
            var text = badge.GetComponentInChildren<TMP_Text>(true);
            text.text = hitsPlayer ? $"{dmg} <size=70%>YOU</size>" : dmg.ToString();
            if (targetCard != null)
            {
                incoming.TryGetValue(targetCard, out var sum);
                incoming[targetCard] = sum + dmg;
                if (targetCard.transform.parent != null) danger.Add(targetCard.transform.parent);
            }
            else if (hitsPlayer)
            {
                var slot = EmptySlotOnPath(c);
                if (slot != null) danger.Add(slot);
            }
        }

        // красные зоны: клетки, по которым придёт удар (твоя карта или пустая клетка, через которую бьют тебя)
        foreach (var slot in FindObjectsByType<DropCard>(FindObjectsSortMode.None))
        {
            bool on = danger.Contains(slot.transform);
            var z = slot.transform.Find("V_Danger");
            if (!on) { if (z != null) z.gameObject.SetActive(false); continue; }
            if (z == null) z = Zone(slot.transform, "V_Danger", DangerColor);
            z.gameObject.SetActive(true);
        }

        foreach (var c in cards)
        {
            if (c == null || c.CardData == null || c.CardData.isEnemy) continue;
            bool onBoard = c.GetComponentInParent<DropCard>() != null;
            var t = c.transform.Find("V_Threat");
            if (!onBoard || !incoming.TryGetValue(c, out var dmg))
            {
                if (t != null) t.gameObject.SetActive(false);
                continue;
            }
            var badge = Badge(c, "V_Threat");
            badge.gameObject.SetActive(true);
            var text = badge.GetComponentInChildren<TMP_Text>(true);
            bool lethal = dmg >= c.CardData.HP;
            text.text = lethal ? $"-{dmg}!" : $"-{dmg}";
        }
    }

    /// <summary>Пустая клетка игрока, через которую враг бьёт тебя (первая клетка DropCard на луче).</summary>
    private static Transform EmptySlotOnPath(Card enemy)
    {
        var attack = enemy.GetComponent<CardForwardAttack>();
        var from = attack != null ? attack.transform : enemy.transform;
        var hits = Physics.RaycastAll(from.position, from.up, 140f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            var drop = h.collider.GetComponent<DropCard>();
            if (drop != null) return drop.transform;
        }
        return null;
    }

    /// <summary>Кого ударит карта врага: карту игрока (targetCard) или самого игрока (hitsPlayer).</summary>
    private static bool TryTarget(Card enemy, out Card targetCard, out bool hitsPlayer)
    {
        targetCard = null;
        hitsPlayer = false;
        var attack = enemy.GetComponent<CardForwardAttack>();
        var from = attack != null ? attack.transform : enemy.transform;
        Collider hit = null;
        if (enemy.CardData.ability == CardAbility.Ranged)
        {
            CardAbilities.FindRangedTarget(from, col => col.CompareTag("Card"), col => col.CompareTag("Player"), out hit);
        }
        else if (Physics.Raycast(from.position, from.up, out var h, 34))
        {
            hit = h.collider;
        }
        if (hit == null) return false;
        if (hit.CompareTag("Player")) { hitsPlayer = true; return true; }
        if (hit.CompareTag("Card"))
        {
            targetCard = hit.GetComponentInParent<Card>();
            return targetCard != null;
        }
        return false;
    }

    /// <summary>Значок (красный меч + число) на карте; всегда смотрит «вверх» относительно поля.</summary>
    private RectTransform Badge(Card card, string name)
    {
        var existing = card.transform.Find(name) as RectTransform;
        if (existing != null)
        {
            existing.SetAsLastSibling();
            var cv = card.GetComponentInParent<Canvas>();
            if (cv != null) existing.rotation = cv.transform.rotation;
            return existing;
        }
        bool threat = name == "V_Threat";
        var rt = VisualTheme.Node(name, card.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        rt.sizeDelta = new Vector2(70, 34);
        rt.anchoredPosition = new Vector2(0, threat ? 20 : 22);
        var canvas = card.GetComponentInParent<Canvas>();
        if (canvas != null) rt.rotation = canvas.transform.rotation; // текст не вверх ногами у повёрнутых вражеских карт

        var icon = VisualTheme.Node("Icon", rt, new Vector2(0, 0), new Vector2(0.38f, 1), Vector2.zero, Vector2.zero);
        var img = VisualTheme.Img(icon, null, IntentRed);
        img.sprite = sword;
        img.preserveAspect = true;
        icon.localEulerAngles = new Vector3(0, 0, threat ? 180f : 0f); // метка на своей карте - меч остриём вниз

        var t = VisualTheme.Txt(VisualTheme.Node("Value", rt, new Vector2(0.36f, 0), new Vector2(1.6f, 1), Vector2.zero, Vector2.zero), "", 22, IntentRed);
        t.alignment = TextAlignmentOptions.Left;
        rt.SetAsLastSibling();
        return rt;
    }
}
