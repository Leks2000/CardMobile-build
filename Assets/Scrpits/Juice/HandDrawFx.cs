using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [V] Card draw trail: sits on the hand root and listens for new children (HandLayoutController /
/// CardFactory parent drawn cards here). Each card seen for the first time gets a glowing trail while
/// it flies from the deck, and a small sparkle when it arrives. No gameplay hooks needed.
/// </summary>
public class HandDrawFx : MonoBehaviour
{
    public float trailTime = 0.6f;
    public Color trailColor = new Color(1f, 0.8f, 0.35f, 0.55f);

    private readonly HashSet<int> seen = new HashSet<int>();

    private void Awake()
    {
        foreach (Transform c in transform) seen.Add(c.GetInstanceID());
    }

    private void OnTransformChildrenChanged()
    {
        if (!isActiveAndEnabled) return;
        foreach (Transform c in transform)
        {
            if (!seen.Add(c.GetInstanceID())) continue;
            if (c.GetComponent<Card>() == null) continue;
            StartCoroutine(Trail(c));
        }
    }

    private IEnumerator Trail(Transform card)
    {
        yield return null; // HandLayoutController places the card at the deck this frame
        if (card == null) yield break;
        var rarity = RarityColors.Get(card.GetComponent<CardView>() is CardView v && v.Data != null ? v.Data.rarity : CardRarity.Common);
        var col = Color.Lerp(trailColor, new Color(rarity.r, rarity.g, rarity.b, trailColor.a), 0.5f);
        Vector3 last = card.position;
        float t = 0f, emit = 0f;
        while (t < trailTime && card != null)
        {
            t += Time.deltaTime;
            emit += Time.deltaTime;
            if ((card.position - last).sqrMagnitude > 0.0001f && emit >= 0.016f)
            {
                emit = 0f;
                ImpactFx.TrailDot(card, col, 70f);
            }
            last = card.position;
            yield return null;
        }
        if (card != null && card.parent == transform) ImpactFx.Sparkle(card, new Color(1f, 0.9f, 0.6f, 0.8f), 5, 0.8f);
    }
}
