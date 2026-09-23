using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Фишка игрока на карте: золотая пешка с ореолом и тенью.
    /// MoveTo - перемещение по линии пути прыжками (изинг + подпрыгивание + следы), приземление с отскоком, затем колбэк.
    /// Позиции - в локальных координатах родителя (как у узлов: anchor (0,0) области карты).
    /// </summary>
    public class MapPlayerToken : MonoBehaviour
    {
        private RectTransform rt;
        private RectTransform body;    // пешка (подпрыгивает)
        private RectTransform shadow;
        private Image halo;
        private RectTransform fxRoot;  // следы/кольца (в родителе, под фишкой)
        private float size = 100f;

        public bool IsMoving { get; private set; }
        public Vector2 Position => rt.anchoredPosition;

        public static MapPlayerToken Create(RectTransform parent, RectTransform fxParent)
        {
            var root = UiKit.Rect("PlayerToken", parent);
            root.anchorMin = root.anchorMax = Vector2.zero;
            root.pivot = new Vector2(0.5f, 0.5f);
            var t = root.gameObject.AddComponent<MapPlayerToken>();
            t.rt = root;
            t.fxRoot = fxParent;

            t.halo = UiKit.Img("Halo", root, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.55f), UiKit.Glow);
            t.shadow = UiKit.Img("Shadow", root, new Color(0, 0, 0, 0.55f), UiKit.Glow).rectTransform;
            var pawn = UiKit.Img("Pawn", root, Color.white, UiKit.Pawn);
            t.body = pawn.rectTransform;
            t.body.pivot = new Vector2(0.5f, 0f);
            // блик-звёздочка на голове
            var shine = UiKit.Img("Shine", t.body, new Color(1f, 1f, 0.9f, 0.9f), UiKit.Glow);
            shine.rectTransform.anchorMin = shine.rectTransform.anchorMax = new Vector2(0.4f, 0.84f);
            shine.rectTransform.sizeDelta = new Vector2(26, 26);
            t.Resize(100f);

            t.halo.DOFade(0.2f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(root.gameObject);
            t.halo.rectTransform.DOScale(1.12f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(root.gameObject);
            return t;
        }

        /// <summary>Размер фишки подстраивается под размер узла.</summary>
        public void Resize(float nodeSize)
        {
            size = nodeSize;
            float h = nodeSize * 0.95f;
            body.sizeDelta = new Vector2(h * 0.75f, h);
            body.anchoredPosition = new Vector2(0, -nodeSize * 0.12f);
            shadow.sizeDelta = new Vector2(h * 0.9f, h * 0.28f);
            shadow.anchoredPosition = new Vector2(0, -nodeSize * 0.12f);
            halo.rectTransform.sizeDelta = new Vector2(h * 1.8f, h * 1.8f);
            halo.rectTransform.anchoredPosition = new Vector2(0, h * 0.3f);
            var shine = body.Find("Shine") as RectTransform;
            if (shine != null) shine.sizeDelta = Vector2.one * h * 0.28f;
        }

        public void Place(Vector2 pos)
        {
            if (IsMoving) return;
            rt.anchoredPosition = pos;
        }

        /// <summary>"Прибытие" при возврате на карту: падение сверху с отскоком + кольцо.</summary>
        public void ArrivePulse(Color ringColor)
        {
            body.DOKill(true);
            float baseY = body.anchoredPosition.y;
            body.anchoredPosition = new Vector2(0, baseY + size * 0.9f);
            body.DOAnchorPosY(baseY, 0.55f).SetEase(Ease.OutBounce).SetLink(gameObject);
            shadow.localScale = Vector3.one * 0.4f;
            shadow.DOScale(1f, 0.55f).SetEase(Ease.OutQuad).SetLink(gameObject);
            DOVirtual.DelayedCall(0.25f, () => Ring(rt.anchoredPosition, ringColor)).SetLink(gameObject);
        }

        /// <summary>
        /// Путь от текущей позиции к target. onStep(t 0..1) - прогресс (для подсветки линии), onArrive - после приземления.
        /// </summary>
        public void MoveTo(Vector2 target, Action<float> onStep, Action onArrive)
        {
            if (IsMoving) return;
            IsMoving = true;
            Vector2 from = rt.anchoredPosition;
            float dist = Vector2.Distance(from, target);
            float duration = Mathf.Clamp(dist / 380f, 0.75f, 1.4f);
            int hops = Mathf.Max(2, Mathf.RoundToInt(dist / (size * 1.1f)));
            float hopH = size * 0.32f;
            float baseY = -size * 0.12f;
            float nextStep = 0f;
            int landed = 0;

            // замах перед стартом
            var seq = DOTween.Sequence().SetLink(gameObject);
            seq.Append(body.DOScale(new Vector3(1.12f, 0.85f, 1f), 0.12f).SetEase(Ease.OutQuad));
            seq.Append(DOVirtual.Float(0f, 1f, duration, t =>
            {
                float u = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutSine);
                rt.anchoredPosition = Vector2.Lerp(from, target, u);
                float ph = t * hops;
                float hop = Mathf.Sin(Mathf.PI * Mathf.Repeat(ph, 1f));
                body.anchoredPosition = new Vector2(0, baseY + hop * hopH);
                // растяжение в полёте, сплющивание у земли
                float stretch = 1f + (hop - 0.5f) * 0.12f;
                body.localScale = new Vector3(2f - stretch, stretch, 1f);
                body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(ph * Mathf.PI * 2f) * 6f * Mathf.Sign(from.x - target.x + 0.001f));
                shadow.localScale = Vector3.one * (1f - hop * 0.45f);
                int l = Mathf.FloorToInt(ph);
                if (l > landed) { landed = l; Footstep(rt.anchoredPosition); }
                if (u >= nextStep) { nextStep += 0.04f; Dot(rt.anchoredPosition); }
                onStep?.Invoke(u);
            }).SetEase(Ease.Linear));
            seq.AppendCallback(() =>
            {
                rt.anchoredPosition = target;
                body.anchoredPosition = new Vector2(0, baseY);
                body.localRotation = Quaternion.identity;
                shadow.localScale = Vector3.one;
                body.localScale = new Vector3(1.25f, 0.75f, 1f);
                onStep?.Invoke(1f);
                Ring(target, UiTheme.Accent);
                Footstep(target);
            });
            seq.Append(body.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutElastic));
            seq.Join(body.DOAnchorPosY(baseY + size * 0.12f, 0.12f).SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo));
            seq.AppendInterval(0.12f);
            seq.OnComplete(() =>
            {
                IsMoving = false;
                onArrive?.Invoke();
            });
        }

        private readonly List<Image> pool = new List<Image>();

        private Image Fx(Sprite sprite)
        {
            Image img = pool.Find(i => i != null && !i.gameObject.activeSelf);
            if (img == null)
            {
                img = UiKit.Img("Fx", fxRoot, Color.white, sprite);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = Vector2.zero;
                pool.Add(img);
            }
            img.sprite = sprite;
            img.DOKill();
            img.rectTransform.DOKill();
            img.gameObject.SetActive(true);
            img.transform.SetAsLastSibling();
            img.rectTransform.localScale = Vector3.one;
            return img;
        }

        /// <summary>Маленькая золотая точка-след, тает.</summary>
        private void Dot(Vector2 pos)
        {
            var img = Fx(UiKit.Circle);
            img.rectTransform.anchoredPosition = pos + new Vector2(0, -size * 0.1f);
            img.rectTransform.sizeDelta = Vector2.one * size * 0.13f;
            img.color = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.9f);
            img.DOKill();
            img.DOFade(0f, 0.9f).SetEase(Ease.InQuad).OnComplete(() => img.gameObject.SetActive(false)).SetLink(img.gameObject);
            img.rectTransform.DOScale(0.4f, 0.9f).SetLink(img.gameObject);
        }

        /// <summary>Пыль при приземлении.</summary>
        private void Footstep(Vector2 pos)
        {
            var img = Fx(UiKit.Glow);
            img.rectTransform.anchoredPosition = pos + new Vector2(0, -size * 0.12f);
            img.rectTransform.sizeDelta = new Vector2(size * 0.9f, size * 0.3f);
            img.color = new Color(1f, 0.92f, 0.75f, 0.6f);
            img.rectTransform.localScale = Vector3.one * 0.5f;
            img.DOKill();
            img.rectTransform.DOScale(1.3f, 0.45f).SetEase(Ease.OutQuad).SetLink(img.gameObject);
            img.DOFade(0f, 0.45f).OnComplete(() => img.gameObject.SetActive(false)).SetLink(img.gameObject);
        }

        /// <summary>Расходящееся кольцо.</summary>
        public void Ring(Vector2 pos, Color color)
        {
            var img = Fx(UiKit.Ring);
            img.rectTransform.anchoredPosition = pos;
            img.rectTransform.sizeDelta = Vector2.one * size * 1.2f;
            img.color = color;
            img.rectTransform.localScale = Vector3.one * 0.5f;
            img.DOKill();
            img.rectTransform.DOScale(1.9f, 0.6f).SetEase(Ease.OutCubic).SetLink(img.gameObject);
            img.DOFade(0f, 0.6f).SetEase(Ease.InQuad).OnComplete(() => img.gameObject.SetActive(false)).SetLink(img.gameObject);
        }
    }
}
