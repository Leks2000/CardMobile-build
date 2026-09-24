using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Наведение на узел карты: доступный узел ярко подсвечивается (белое кольцо, сильное свечение, светлее диск,
    /// белая подпись), над узлом появляется подсказка «что внутри». Без изменения масштаба.
    /// </summary>
    public class MapNodeHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image ring, glow, highlight;
        private TMP_Text label;
        private RectTransform tip;
        private CanvasGroup tipGroup;
        private bool available;
        private Color typeColor = Color.white;
        private bool hovered;
        private Color ringWas, labelWas;

        public void Init(string title, string hint)
        {
            ring = transform.Find("Ring")?.GetComponent<Image>();
            glow = transform.Find("Glow")?.GetComponent<Image>();
            label = transform.Find("Label")?.GetComponent<TMP_Text>();

            // светлая «вуаль» поверх диска (включается при наведении)
            var disc = transform.Find("Disc") as RectTransform;
            if (disc != null)
            {
                highlight = UiKit.Img("HoverLight", disc, new Color(1, 1, 1, 0f), UiKit.Circle);
                UiKit.Stretch(highlight.rectTransform);
            }

            // подсказка над узлом
            var bg = UiKit.Img("Tip", transform, new Color(0.06f, 0.05f, 0.09f, 0.94f), UiKit.RoundedRect, true);
            tip = bg.rectTransform;
            tip.anchorMin = tip.anchorMax = new Vector2(0.5f, 1f);
            tip.pivot = new Vector2(0.5f, 0f);
            tip.sizeDelta = new Vector2(360, 118);
            tip.anchoredPosition = new Vector2(0, 26);
            var frame = UiKit.Img("Frame", tip, new Color(1, 1, 1, 0.25f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frame.rectTransform);
            var t = UiKit.Text("Text", tip, $"<size=125%><b>{title}</b></size>\n<color=#{UiKit.Hex(UiTheme.TextDim)}>{hint}</color>", 26, UiTheme.Text);
            t.lineSpacing = -6;
            UiKit.Stretch(t.rectTransform, 14, 14, 8, 8);
            tipGroup = tip.gameObject.AddComponent<CanvasGroup>();
            tipGroup.blocksRaycasts = false;
            tipGroup.alpha = 0f;
            tip.gameObject.SetActive(false);
        }

        /// <summary>Вызывается из MapController.Refresh: доступен ли узел и его цвет.</summary>
        public void SetState(bool isAvailable, Color color)
        {
            available = isAvailable;
            typeColor = color;
            if (hovered) Apply(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (MapController.Busy) return;
            hovered = true;
            if (ring != null) ringWas = ring.color;
            if (label != null) labelWas = label.color;
            Apply(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!hovered) return;
            hovered = false;
            Apply(false);
        }

        private void OnDisable()
        {
            hovered = false;
            if (tip != null) tip.gameObject.SetActive(false);
        }

        private void Apply(bool on)
        {
            bool lit = on && available;
            if (ring != null) ring.color = lit ? Color.white : ringWas;
            if (label != null) label.color = lit ? Color.white : labelWas;
            if (highlight != null)
            {
                highlight.DOKill();
                highlight.DOFade(lit ? 0.22f : 0f, 0.15f).SetLink(highlight.gameObject);
            }
            if (glow != null && lit)
            {
                glow.DOKill();
                glow.color = new Color(typeColor.r, typeColor.g, typeColor.b, 1f);
            }
            else if (glow != null && !on && available)
            {
                // вернуть «дыхание» свечения доступного узла
                glow.DOKill();
                glow.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.75f);
                glow.DOFade(0.35f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(glow.gameObject);
            }

            if (tip == null) return;
            tip.DOKill();
            tipGroup.DOKill();
            if (on)
            {
                transform.SetAsLastSibling(); // подсказка поверх соседних узлов
                tip.gameObject.SetActive(true);
                tipGroup.alpha = 0f;
                tip.anchoredPosition = new Vector2(0, 14);
                tipGroup.DOFade(1f, 0.15f).SetLink(tip.gameObject);
                tip.DOAnchorPosY(26f, 0.15f).SetEase(Ease.OutQuad).SetLink(tip.gameObject);
            }
            else
            {
                tipGroup.DOFade(0f, 0.1f).SetLink(tip.gameObject).OnComplete(() => tip.gameObject.SetActive(false));
            }
        }
    }
}
