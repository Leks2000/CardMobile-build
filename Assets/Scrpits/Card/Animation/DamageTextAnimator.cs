using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Card.Animation
{

    public class DamageTextAnimator : MonoBehaviour
    {
        public float moveDistance = 30f;
        public float duration = 1f;

        [Header("Juice")]
        [Tooltip("Colour of the damage number.")]
        public Color damageColor = new Color(1f, 0.25f, 0.2f, 1f);
        [Tooltip("Peak scale of the damage number pop.")]
        public float popScale = 1.6f;
        [Tooltip("Punched on hit. Defaults to this object when it has a Graphic (cards).")]
        public Transform hitTarget;
        [Tooltip("Flashed red on hit. Defaults to the Graphic on this object (cards).")]
        public Graphic flashTarget;
        public bool playHitFx = true;

        private void Awake()
        {
            if (flashTarget == null) flashTarget = GetComponent<Graphic>();
            if (hitTarget == null && flashTarget != null) hitTarget = flashTarget.transform;
        }

        /// <summary>Lets Boss/Player point the hit feedback at their portrait / HP plate.</summary>
        public void SetHitTargets(Transform target, Graphic flash)
        {
            hitTarget = target;
            flashTarget = flash;
        }

        /// <summary>
        /// Анимация получения урона
        /// </summary>
        /// <param name="onComplete">Вызов по завершении анимации</param>
        public void Animate(TMP_Text damageText, int damage, Action onMidAnimation = null, Action onComplete = null)
        {
            damageText.text = "-" + damage.ToString();
            damageText.enabled = true;
            // [V] unified font + outlined material, drawn above siblings
            if (UiTheme.Font != null && damageText.font != UiTheme.Font) damageText.font = UiTheme.Font;
            if (VisualTheme.OutlineMaterial != null) damageText.fontSharedMaterial = VisualTheme.OutlineMaterial;
            damageText.transform.SetAsLastSibling();
            Vector3 initialPosition = damageText.transform.localPosition;
            Vector3 initialScale = damageText.transform.localScale;
            Color initialColor = damageText.color;
            damageText.color = new Color(damageColor.r, damageColor.g, damageColor.b, initialColor.a);
            damageText.transform.localScale = initialScale * 0.3f;

            // Same total duration as before: onMidAnimation/onComplete timing is unchanged (Card.cs relies on it).
            Sequence sequence = DOTween.Sequence().SetLink(damageText.gameObject);

            sequence.Append(damageText.transform.DOLocalMoveY(initialPosition.y + moveDistance, duration).SetEase(Ease.OutQuad));
            sequence.Join(damageText.DOFade(0, duration * 0.5f).SetDelay(0.25f));
            sequence.Insert(0f, damageText.transform.DOScale(initialScale * popScale, duration * 0.12f).SetEase(Ease.OutQuad));
            sequence.Insert(duration * 0.12f, damageText.transform.DOScale(initialScale, duration * 0.2f).SetEase(Ease.OutBack));
            // warm-up the colour (RGB only, so it doesn't fight the alpha fade)
            Color hot = new Color(1f, 0.85f, 0.75f);
            sequence.Insert(duration * 0.1f, DOTween.To(() => 0f, v =>
            {
                Color c = Color.Lerp(damageColor, hot, v);
                damageText.color = new Color(c.r, c.g, c.b, damageText.color.a);
            }, 1f, duration * 0.3f));

            sequence.OnStart(() =>
            {
                if (playHitFx && hitTarget != null)
                {
                    CombatFx.Hit(hitTarget, flashTarget);
                    ImpactFx.Burst(hitTarget, damageColor, Mathf.Clamp(5 + damage, 6, 12), 0.9f); // [V]
                }
                onMidAnimation?.Invoke();
            });

            sequence.OnComplete(() =>
            {
                damageText.enabled = false;
                damageText.color = initialColor;
                damageText.alpha = 1;
                damageText.transform.localScale = Vector3.one;
                damageText.transform.localPosition = initialPosition;

                onComplete?.Invoke();
            });
        }
    }
}
