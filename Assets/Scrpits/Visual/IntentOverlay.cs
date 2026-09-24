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

    private void Update()
    {
        if (Time.unscaledTime < next) return;
        next = Time.unscaledTime + 0.2f;
        Refresh();
    }

    private void Refresh()
    {
        incoming.Clear();
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
            }
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
