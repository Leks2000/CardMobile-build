using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Раскладка руки игрока: считает позиции слотов для N карт (ряд по центру)
/// и плавно перемещает карты в их слоты. Заменяет GridLayoutGroup, который конфликтовал с твинами.
/// Карты руки = дочерние объекты <see cref="hand"/> (в порядке sibling index).
/// </summary>
public class HandLayoutController : MonoBehaviour
{
    [SerializeField] private RectTransform hand;
    [SerializeField] private Vector2 cellSize = new Vector2(90f, 140f);
    [SerializeField] private float spacing = 4f;
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float drawDuration = 0.35f;

    /// <summary>Текущий твин позиции каждой карты (масштаб не трогаем, чтобы не обрывать анимацию добора)</summary>
    private readonly Dictionary<RectTransform, Tween> moveTweens = new Dictionary<RectTransform, Tween>();

    public RectTransform Hand => hand;
    public int Count => hand.childCount;

    /// <summary>
    /// Инициализация. Берёт размеры ячейки из GridLayoutGroup (если есть) и выключает его.
    /// </summary>
    public void Init(RectTransform handRoot, GridLayoutGroup grid)
    {
        hand = handRoot;
        if (grid != null)
        {
            cellSize = grid.cellSize;
            spacing = grid.spacing.x;
            grid.enabled = false;
        }
        foreach (Transform child in hand)
        {
            PrepareCard((RectTransform)child);
        }
        Relayout(true);
    }

    /// <summary>
    /// Локальная позиция слота с индексом index при count картах в руке.
    /// </summary>
    public Vector3 GetSlotPosition(int index, int count)
    {
        var step = cellSize.x + spacing;
        var x = (index - (count - 1) * 0.5f) * step;
        return new Vector3(x, 0f, 0f);
    }

    /// <summary>
    /// Текущая позиция слота карты (карта должна быть дочерней для руки).
    /// </summary>
    public Vector3 GetSlotPosition(RectTransform card)
    {
        if (card.parent != hand)
        {
            return card.localPosition;
        }
        return GetSlotPosition(card.GetSiblingIndex(), hand.childCount);
    }

    /// <summary>
    /// Добавить новую карту (уже дочернюю для руки) с анимацией вылета из колоды.
    /// </summary>
    /// <param name="card">Карта</param>
    /// <param name="fromWorldPosition">Откуда вылетает (позиция колоды)</param>
    /// <param name="delay">Задержка для последовательной выдачи нескольких карт</param>
    public void AddCardFromDeck(RectTransform card, Vector3 fromWorldPosition, float delay)
    {
        if (card.parent != hand)
        {
            card.SetParent(hand, false);
        }
        PrepareCard(card);
        card.position = fromWorldPosition;
        card.localPosition = new Vector3(card.localPosition.x, card.localPosition.y, 0f);
        card.localScale = Vector3.one * 0.6f;

        // Сначала сдвигаем остальные карты, затем новая карта летит в свой слот
        Relayout(false, card);
        var target = GetSlotPosition(card);
        MoveTo(card, target, drawDuration, Ease.OutCubic, delay);
        card.DOScale(Vector3.one, drawDuration).SetEase(Ease.OutBack).SetDelay(delay);
    }

    /// <summary>
    /// Вернуть карту в руку на заданную позицию (sibling index) и перестроить руку.
    /// </summary>
    public void ReturnCard(RectTransform card, int siblingIndex)
    {
        card.SetParent(hand, true);
        card.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, hand.childCount - 1));
        PrepareCard(card);
        Relayout(false);
    }

    /// <summary>
    /// Перестроить руку: каждая карта двигается в свой слот.
    /// </summary>
    /// <param name="instant">Без анимации</param>
    /// <param name="skip">Карта, которую не трогать (например, ещё летит из колоды)</param>
    public void Relayout(bool instant, RectTransform skip = null)
    {
        var count = hand.childCount;
        for (var i = 0; i < count; i++)
        {
            var card = (RectTransform)hand.GetChild(i);
            if (card == skip)
            {
                continue;
            }
            var target = GetSlotPosition(i, count);
            if (instant)
            {
                KillMove(card);
                card.localPosition = target;
            }
            else
            {
                MoveTo(card, target, moveDuration, Ease.OutQuad, 0f);
            }
        }
    }

    /// <summary>
    /// Приподнять карту руки при наведении (или вернуть на место)
    /// </summary>
    public void SetHover(RectTransform card, bool hovered, float lift, float duration)
    {
        if (card.parent != hand)
        {
            return;
        }
        var target = GetSlotPosition(card) + (hovered ? Vector3.up * lift : Vector3.zero);
        MoveTo(card, target, duration, hovered ? Ease.OutQuad : Ease.InQuad, 0f);
    }

    /// <summary>
    /// Остановить движение карты (перед перетаскиванием)
    /// </summary>
    public void KillMove(RectTransform card)
    {
        if (moveTweens.TryGetValue(card, out var tween))
        {
            tween.Kill();
            moveTweens.Remove(card);
        }
    }

    private void MoveTo(RectTransform card, Vector3 target, float time, Ease ease, float delay)
    {
        KillMove(card);
        var tween = card.DOLocalMove(target, time).SetEase(ease).SetDelay(delay);
        tween.OnKill(() =>
        {
            if (moveTweens.TryGetValue(card, out var current) && current == tween)
            {
                moveTweens.Remove(card);
            }
        });
        moveTweens[card] = tween;
    }

    /// <summary>
    /// Якоря/пивот по центру и размер ячейки руки.
    /// </summary>
    private void PrepareCard(RectTransform card)
    {
        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.sizeDelta = cellSize;
        card.localRotation = Quaternion.identity;
    }
}
