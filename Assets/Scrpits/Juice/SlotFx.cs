using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual feedback for a board slot:
///  - Drop mode (on <see cref="DropCard"/> slots): while a card is dragged over it, tints green if free, red if occupied;
///    punches the card when it lands in the slot.
///  - Spawn mode (enemy spawn slots): pops enemy cards in when they appear.
/// Pure visuals, no gameplay changes.
/// </summary>
public class SlotFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum Mode { Drop, Spawn }

    public Mode mode = Mode.Drop;
    [Tooltip("Tinted while hovering with a card. Defaults to the Image on this object.")]
    public Image slotImage;
    public float tintStrength = 0.75f;

    // Cards that already got their "landed"/"spawned" punch (so moving between slots doesn't re-punch).
    private static readonly HashSet<int> punched = new HashSet<int>();

    private DropCard drop;
    private Outline outline;
    private Color baseColor;
    private Color baseOutline;
    private bool tinted;
    private readonly HashSet<int> knownChildren = new HashSet<int>();

    private void Awake()
    {
        drop = GetComponent<DropCard>();
        if (slotImage == null) slotImage = GetComponent<Image>();
        outline = GetComponent<Outline>();
        if (slotImage != null) baseColor = slotImage.color;
        if (outline != null) baseOutline = outline.effectColor;
        foreach (Transform c in transform) knownChildren.Add(c.GetInstanceID());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mode != Mode.Drop || drop == null || !GlobalDragTracker.IsDraggingCard) return;
        if (eventData.pointerDrag == null || eventData.pointerDrag.GetComponent<CardDrag>() == null) return;
        SetTint(drop.canDrop ? CombatFx.ValidGreen : CombatFx.InvalidRed);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ClearTint();
    }

    private void Update()
    {
        if (tinted && !GlobalDragTracker.IsDraggingCard) ClearTint();
    }

    private void OnDisable() => ClearTint();

    private void SetTint(Color c)
    {
        if (slotImage != null) slotImage.color = Color.Lerp(baseColor, new Color(c.r, c.g, c.b, baseColor.a), tintStrength);
        if (outline != null) outline.effectColor = new Color(c.r, c.g, c.b, 0.9f);
        CombatFx.Punch(transform, 0.06f, 0.18f);
        tinted = true;
    }

    private void ClearTint()
    {
        if (!tinted) return;
        if (slotImage != null) slotImage.color = baseColor;
        if (outline != null) outline.effectColor = baseOutline;
        tinted = false;
    }

    private void OnTransformChildrenChanged()
    {
        foreach (Transform child in transform)
        {
            int id = child.GetInstanceID();
            if (knownChildren.Contains(id)) continue;
            knownChildren.Add(id);
            OnChildAdded(child);
        }
        knownChildren.RemoveWhere(id => !HasChild(id));
    }

    private bool HasChild(int id)
    {
        foreach (Transform c in transform) if (c.GetInstanceID() == id) return true;
        return false;
    }

    private void OnChildAdded(Transform child)
    {
        var card = child.GetComponent<Card>();
        if (card == null) return;
        if (!punched.Add(card.GetInstanceID())) return;
        if (punched.Count > 512) punched.Clear();

        if (mode == Mode.Drop)
        {
            // Card is still flying to the slot centre: punch when it arrives.
            float delay = 0.2f;
            var drag = child.GetComponent<CardDrag>();
            if (drag != null) delay = Mathf.Clamp(drag.moveDuration, 0.05f, 0.6f);
            CombatFx.StartRoutine(LandRoutine(child, delay));
        }
        else
        {
            CombatFx.PopIn(child, 0.3f);
        }
    }

    private System.Collections.IEnumerator LandRoutine(Transform card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card == null || card.parent != transform) yield break;
        CombatFx.Punch(card, 0.2f, 0.32f);
        CombatFx.Punch(transform, 0.08f, 0.25f);
        if (slotImage != null) CombatFx.Flash(slotImage, Color.white, 0.3f);
    }
}
