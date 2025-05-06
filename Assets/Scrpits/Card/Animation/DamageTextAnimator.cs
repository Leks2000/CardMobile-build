using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Assets.Scrpits.Card.Animation
{

    public class DamageTextAnimator : MonoBehaviour
    {
        public float moveDistance = 30f;
        public float duration = 1f;

        /// <summary>
        /// Анимация получения урона
        /// </summary>
        /// <param name="onComplete">Вызов по завершении анимации</param>
        public void Animate(TMP_Text damageText, int damage, Action onMidAnimation = null, Action onComplete = null)
        {
            damageText.text = "-" + damage.ToString();
            damageText.enabled = true;
            Vector3 initialPosition = damageText.transform.localPosition;

            Sequence sequence = DOTween.Sequence();

            sequence.Append(damageText.transform.DOLocalMoveY(initialPosition.y + moveDistance, duration).SetEase(Ease.OutQuad));
            sequence.Join(damageText.DOFade(0, duration * 0.5f).SetDelay(0.25f));

            sequence.OnStart(() =>
            {
                onMidAnimation?.Invoke();
            });

            sequence.OnComplete(() =>
            {
                damageText.enabled = false;
                damageText.alpha = 1;
                damageText.transform.localScale = Vector3.one;
                damageText.transform.localPosition = initialPosition;

                onComplete?.Invoke();
            });
        }
    }
}
