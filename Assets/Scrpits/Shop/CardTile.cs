using Assets.Scrpits.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Shop
{
    /// <summary>
    /// UI-плитка карты (магазин кейсов, рулетка, награды на карте). Строится из кода, 200x280 базовый размер.
    /// Цвет рамки/подписи - по редкости (RarityColors). Арт - CardData.art, иначе цветная заглушка с буквой.
    /// Слева сверху - стоимость (синий), снизу слева - урон (красный), снизу справа - HP (зелёный).
    /// </summary>
    public static class CardTile
    {
        public static readonly Vector2 BaseSize = new Vector2(200, 280);

        public static RectTransform Build(Transform parent, CardData card, float scale = 1f, bool glow = false)
        {
            CardRarity rarity = card != null ? card.rarity : CardRarity.Common;
            Color rc = RarityColors.Get(rarity);

            var root = UiKit.Rect("Card_" + (card != null ? card.Id : "none"), parent);
            root.sizeDelta = BaseSize;
            root.localScale = Vector3.one * scale;

            if (glow)
            {
                var g = UiKit.Img("Glow", root, new Color(rc.r, rc.g, rc.b, 0.55f), UiKit.Glow);
                UiKit.Place(g.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, BaseSize * 1.9f);
            }

            var frame = UiKit.Img("Frame", root, rc, UiKit.RoundedRect, true);
            UiKit.Stretch(frame.rectTransform);
            var sh = frame.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.5f);
            sh.effectDistance = new Vector2(0, -6);

            var inner = UiKit.Img("Inner", root, Color.Lerp(UiTheme.Panel, rc, 0.12f), UiKit.RoundedRect, true);
            UiKit.Stretch(inner.rectTransform, 7, 7, 7, 7);

            // art
            var artBg = UiKit.Img("ArtBg", root, Color.Lerp(rc, Color.black, 0.55f), UiKit.RoundedRect, true);
            UiKit.Stretch(artBg.rectTransform, 16, 16, 16, 108);
            var mask = artBg.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(3, 3, 3, 3);
            if (card != null && card.art != null)
            {
                var art = UiKit.Img("Art", artBg.rectTransform, Color.white, card.art);
                art.preserveAspect = true;
                UiKit.Stretch(art.rectTransform, 3, 3, 3, 3);
            }
            else
            {
                var grad = UiKit.Img("Grad", artBg.rectTransform, new Color(rc.r, rc.g, rc.b, 0.85f), UiKit.VGradient);
                UiKit.Stretch(grad.rectTransform);
                var glowIn = UiKit.Img("Shine", artBg.rectTransform, new Color(1, 1, 1, 0.25f), UiKit.Glow);
                UiKit.Place(glowIn.rectTransform, new Vector2(0.5f, 0.6f), Vector2.zero, new Vector2(170, 170));
                string letter = card != null && card.Title.Length > 0 ? card.Title.Substring(0, 1).ToUpper() : "?";
                var l = UiKit.Text("Letter", artBg.rectTransform, letter, 96, new Color(1, 1, 1, 0.9f));
                UiKit.Stretch(l.rectTransform);
                var lsh = l.gameObject.AddComponent<Shadow>();
                lsh.effectColor = new Color(0, 0, 0, 0.5f);
                lsh.effectDistance = new Vector2(3, -3);
            }

            // title
            string title = card != null ? card.Title : "???";
            var t = UiKit.Text("Title", root, title, 26, UiTheme.Text);
            t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 28;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 86), new Vector2(176, 34));

            // rarity
            var r = UiKit.Text("Rarity", root, RarityColors.Name(rarity).ToUpper(), 20, rc);
            r.characterSpacing = 4;
            UiKit.Place(r.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(100, 28));

            if (card != null)
            {
                Chip(root, "Cost", card.Cost, UiTheme.Mana, new Vector2(0, 1), new Vector2(18, -18));
                Chip(root, "Dmg", card.Damage, UiTheme.Damage, new Vector2(0, 0), new Vector2(26, 42));
                Chip(root, "Hp", card.HP, UiTheme.Heal, new Vector2(1, 0), new Vector2(-26, 42));
            }
            return root;
        }

        private static void Chip(RectTransform root, string name, int value, Color color, Vector2 anchor, Vector2 pos)
        {
            var c = UiKit.Img(name, root, color, UiKit.Circle);
            UiKit.Place(c.rectTransform, anchor, pos, new Vector2(46, 46));
            var o = c.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.6f);
            o.effectDistance = new Vector2(2, -2);
            var t = UiKit.Text("V", c.rectTransform, value.ToString(), 28, Color.white);
            UiKit.Stretch(t.rectTransform);
        }

        /// <summary>Короткое описание статов для текста: "Cost 2   Dmg 3   HP 4".</summary>
        public static string Stats(CardData c) => c == null ? "" : $"Cost {c.Cost}   Dmg {c.Damage}   HP {c.HP}";
    }
}
