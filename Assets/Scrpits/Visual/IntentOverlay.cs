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
    private readonly Dictionary<Transform, int> dangerDmg = new Dictionary<Transform, int>();

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
        float t = Time.unscaledTime;
        float k = 0.6f + 0.4f * Mathf.Sin(t * 5f);
        foreach (var slot in FindObjectsByType<DropCard>(FindObjectsSortMode.None))
        {
            foreach (var n in ZoneNames)
            {
                var z = slot.transform.Find(n);
                if (z == null || !z.gameObject.activeSelf) continue;
                var c = n == "V_Danger" ? DangerColor : DropColor;
                foreach (var img in z.GetComponentsInChildren<Graphic>(true))
                {
                    float a = img.name switch
                    {
                        "Flat" => 0.35f * k,
                        "Fill" => 0.28f,
                        "Rim" => 0.75f + 0.25f * k,
                        _ => 1f,
                    };
                    img.color = new Color(img.name == "Icon" || img.name == "Dmg" ? 1f : c.r, img.name == "Icon" || img.name == "Dmg" ? 1f : c.g, img.name == "Icon" || img.name == "Dmg" ? 1f : c.b, a);
                }
                // «карточка» парит над клеткой (к камере)
                var lift = z.Find("Lift");
                if (lift != null) lift.localPosition = new Vector3(0f, 0f, -16f - 6f * Mathf.Sin(t * 2.5f));
            }
        }
    }

    private static readonly string[] ZoneNames = { "V_Danger", "V_DropZone" };

    /// <summary>
    /// Зона на клетке в форме карточки: мягкое свечение на столе + светящаяся карточка-силуэт (заливка + кайма),
    /// приподнятая над клеткой к камере. У красной зоны - скрещенные мечи и урон.
    /// </summary>
    private static Transform Zone(Transform slot, string name, Color color)
    {
        var root = VisualTheme.Stretch(name, slot);
        var flat = VisualTheme.Img(VisualTheme.Stretch("Flat", root, -14f), ProcSprites.Glow, color);
        flat.raycastTarget = false;
        var lift = VisualTheme.Stretch("Lift", root, 2f);
        var fill = VisualTheme.Img(VisualTheme.Stretch("Fill", lift), ProcSprites.RoundRectSmall, color, true);
        fill.raycastTarget = false;
        var rim = VisualTheme.Img(VisualTheme.Stretch("Rim", lift, -2f), ProcSprites.RoundOutline, color, true);
        rim.raycastTarget = false;
        if (name == "V_Danger")
        {
            var icon = VisualTheme.Img(VisualTheme.Node("Icon", lift, new Vector2(0.18f, 0.35f), new Vector2(0.82f, 0.8f), Vector2.zero, Vector2.zero), null, Color.white);
            icon.sprite = ArtLib.UI("loc_swords");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var dmg = VisualTheme.Txt(VisualTheme.Node("Dmg", lift, new Vector2(0f, 0.08f), new Vector2(1f, 0.36f), Vector2.zero, Vector2.zero), "", 30, Color.white);
            dmg.raycastTarget = false;
        }
        root.SetAsFirstSibling(); // под картой
        return root;
    }

    private void Refresh()
    {
        incoming.Clear();
        danger.Clear();
        dangerDmg.Clear();
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
                if (targetCard.transform.parent != null) AddDanger(targetCard.transform.parent, dmg);
            }
            else if (hitsPlayer)
            {
                var slot = EmptySlotOnPath(c);
                if (slot != null) AddDanger(slot, dmg);
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
            // на пустой клетке - парящая красная карточка с мечами и уроном (сюда придёт удар по тебе);
            // под твоей картой - только красное свечение (метка урона уже на карте)
            bool empty = slot.GetComponentInChildren<Card>() == null;
            var lift = z.Find("Lift");
            if (lift != null) lift.gameObject.SetActive(empty);
            var dmgT = z.Find("Lift/Dmg");
            if (dmgT != null && dangerDmg.TryGetValue(slot.transform, out var dd)) dmgT.GetComponent<TMP_Text>().text = $"-{dd} YOU";
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

    private void AddDanger(Transform slot, int dmg)
    {
        danger.Add(slot);
        dangerDmg.TryGetValue(slot, out var sum);
        dangerDmg[slot] = sum + dmg;
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
