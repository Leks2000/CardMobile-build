using System;
using Assets.Scrpits.Run;
using Assets.Scrpits.Shop;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Окно выбора карты из колоды забега (прокачка «Senior» в отдыхе и магазине).
    /// Сетка плиток CardTile с прокруткой; неподходящие карты затемнены. Возвращает индекс карты в RunState.Deck.
    /// </summary>
    public static class DeckPicker
    {
        public static GameObject Show(RectTransform parent, string title, string subtitle, Func<CardData, bool> filter,
            Action<int> onPick, Action onCancel)
        {
            var dim = UiKit.Img("DeckPicker", parent, new Color(0.02f, 0.01f, 0.04f, 0.92f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);
            var root = dim.rectTransform;
            var go = dim.gameObject;
            var group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.DOFade(1f, 0.2f).SetLink(go);

            var t = UiKit.Text("Title", root, title, 72, UiTheme.Accent);
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1400, 100));
            var st = UiKit.Text("Subtitle", root, subtitle, 36, UiTheme.TextDim);
            UiKit.Place(st.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1400, 60));

            // прокручиваемая сетка
            var view = UiKit.Img("View", root, new Color(0, 0, 0, 0.25f), UiKit.RoundedRect, true);
            UiKit.Place(view.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1640, 640));
            view.raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Rect("Content", view.rectTransform);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            float scale = 0.8f;
            grid.cellSize = CardTile.BaseSize * scale;
            grid.spacing = new Vector2(26, 26);
            grid.padding = new RectOffset(30, 30, 24, 24);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 8;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            for (int i = 0; i < RunState.Deck.Count; i++)
            {
                var card = RunState.Deck[i];
                bool ok = filter == null || filter(card);
                var cell = UiKit.Rect("Cell" + i, content);
                var tile = CardTile.Build(cell, card, scale);
                tile.anchorMin = tile.anchorMax = new Vector2(0.5f, 0.5f);
                tile.anchoredPosition = Vector2.zero;
                var cg = tile.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = ok ? 1f : 0.35f;
                if (!ok) continue;

                var hit = UiKit.Img("Hit", cell, new Color(1, 1, 1, 0f));
                UiKit.Stretch(hit.rectTransform);
                hit.raycastTarget = true;
                var btn = hit.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() =>
                {
                    UnityEngine.Object.Destroy(go);
                    onPick?.Invoke(idx);
                });
                var hover = hit.gameObject.AddComponent<TileHover>();
                hover.target = tile;
            }

            var cancel = UiKit.Button("Cancel", root, "Cancel", UiTheme.PanelLight, new Vector2(340, 100), () =>
            {
                UnityEngine.Object.Destroy(go);
                onCancel?.Invoke();
            }, 44);
            cancel.transform.Find("Text").GetComponent<TMP_Text>().color = UiTheme.Text;
            UiKit.Place((RectTransform)cancel.transform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(340, 100));
            return go;
        }
    }
}
