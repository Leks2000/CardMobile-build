using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Visual feedback for a board slot. [V] explicit states:
///  Idle (base look) / Valid (a card is being dragged and this slot accepts it: gold pulsing outline) /
///  Hover-valid (green) / Invalid (hovered but occupied or blocked: red) / Occupied (dimmed frame).
///  Landing: punch + white flash + expanding ring + sparkle. Spawn mode (enemy slots): pop-in.
/// Pure visuals, no gameplay changes.
/// </summary>
public class SlotFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum Mode { Drop, Spawn }
    public enum State { Idle, Valid, HoverValid, Invalid, Occupied }

    public Mode mode = Mode.Drop;
    [Tooltip("Tinted by state. Defaults to the Image on this object.")]
    public Image slotImage;
    public float tintStrength = 0.75f;

    public static readonly Color ValidGold = new Color(1f, 0.78f, 0.3f, 1f);

    // Cards that already got their "landed"/"spawned" punch (so moving between slots doesn't re-punch).
    private static readonly HashSet<int> punched = new HashSet<int>();

    private DropCard drop;
    private Outline outline;
    private Color baseColor;
    private Color baseOutline;
    private Vector2 baseOutlineDist;
    private bool hovered;
    private bool occupied;
    private readonly HashSet<int> knownChildren = new HashSet<int>();

    public State Current { get; private set; }

    private void Awake()
    {
        drop = GetComponent<DropCard>();
        if (slotImage == null) slotImage = GetComponent<Image>();
        outline = GetComponent<Outline>();
        if (slotImage != null) baseColor = slotImage.color;
        if (outline != null) { baseOutline = outline.effectColor; baseOutlineDist = outline.effectDistance; }
        foreach (Transform c in transform) knownChildren.Add(c.GetInstanceID());
        occupied = GetComponentInChildren<Card>() != null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mode != Mode.Drop || drop == null || !GlobalDragTracker.IsDraggingCard) return;
        if (eventData.pointerDrag == null || eventData.pointerDrag.GetComponent<CardDrag>() == null) return;
        hovered = true;
        CombatFx.Punch(transform, 0.05f, 0.18f);
    }

    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void Update()
    {
        if (mode != Mode.Drop || slotImage == null) return;
        bool dragging = GlobalDragTracker.IsDraggingCard;
        if (!dragging) hovered = false;
        bool accepts = drop != null && drop.canDrop && !occupied;

        State s;
        if (dragging && hovered) s = accepts ? State.HoverValid : State.Invalid;
        else if (dragging && accepts) s = State.Valid;
        else if (occupied) s = State.Occupied;
        else s = State.Idle;
        Current = s;

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
        Color fill = baseColor, edge = baseOutline;
        switch (s)
        {
            case State.Valid:
                fill = Color.Lerp(baseColor, WithA(ValidGold, baseColor.a), 0.18f + 0.1f * pulse);
                edge = WithA(ValidGold, 0.55f + 0.4f * pulse);
                break;
            case State.HoverValid:
                fill = Color.Lerp(baseColor, WithA(CombatFx.ValidGreen, baseColor.a), tintStrength * 0.6f);
                edge = WithA(CombatFx.ValidGreen, 1f);
                break;
            case State.Invalid:
                fill = Color.Lerp(baseColor, WithA(CombatFx.InvalidRed, baseColor.a), tintStrength * 0.6f);
                edge = WithA(CombatFx.InvalidRed, 1f);
                break;
            case State.Occupied:
                fill = Color.Lerp(baseColor, WithA(Color.black, baseColor.a), 0.35f);
                edge = WithA(baseOutline, baseOutline.a * 0.4f);
                break;
        }
        float k = 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f);
        slotImage.color = Color.Lerp(slotImage.color, fill, k);
        if (outline != null)
        {
            outline.effectColor = Color.Lerp(outline.effectColor, edge, k);
            outline.effectDistance = s == State.Idle || s == State.Occupied ? baseOutlineDist : baseOutlineDist * 1.8f;
        }
    }

    private static Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);

    private void OnDisable()
    {
        hovered = false;
        if (slotImage != null) slotImage.color = baseColor;
        if (outline != null) { outline.effectColor = baseOutline; outline.effectDistance = baseOutlineDist; }
    }

    private void OnTransformChildrenChanged()
    {
        occupied = GetComponentInChildren<Card>() != null;
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
            ImpactFx.Ring(transform, new Color(1f, 0.35f, 0.3f, 0.7f), 0.8f, 0.4f);
        }
    }

    private System.Collections.IEnumerator LandRoutine(Transform card, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (card == null || card.parent != transform) yield break;
        CombatFx.Punch(card, 0.2f, 0.32f);
        CombatFx.Punch(transform, 0.08f, 0.25f);
        if (slotImage != null) slotImage.color = new Color(1f, 0.95f, 0.8f, Mathf.Max(0.6f, baseColor.a)); // fades back in Update
        ImpactFx.Ring(transform, new Color(1f, 0.85f, 0.45f, 0.9f), 1f, 0.45f);
        ImpactFx.Sparkle(transform, new Color(1f, 0.9f, 0.6f, 0.9f), 7, 1f);
    }
}
