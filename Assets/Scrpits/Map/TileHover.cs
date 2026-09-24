using Assets.Scrpits.Shop;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>Подсветка плитки под курсором: золотое свечение позади (без масштабирования).</summary>
    public class TileHover : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        public RectTransform target;
        private Image glow;

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e)
        {
            if (target == null) return;
            if (glow == null)
            {
                glow = UiKit.Img("HoverGlow", target, new Color(CardUpgrade.Gold.r, CardUpgrade.Gold.g, CardUpgrade.Gold.b, 0f), UiKit.Glow);
                UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, CardTile.BaseSize * 1.7f);
                glow.transform.SetAsFirstSibling();
            }
            glow.DOKill();
            glow.DOFade(0.8f, 0.12f).SetLink(glow.gameObject);
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)
        {
            if (glow == null) return;
            glow.DOKill();
            glow.DOFade(0f, 0.12f).SetLink(glow.gameObject);
        }
    }
}
