using Assets.Scrpits.Map;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Shop
{
    /// <summary>
    /// UI-плитка предмета (кейсы предметов, торговец, награды) - тот же размер и язык, что и <see cref="CardTile"/>:
    /// рамка цвета редкости, большая процедурная иконка, название, тип (расходник / пассивка) и редкость.
    /// </summary>
    public static class ItemTile
    {
        public static RectTransform Build(Transform parent, ItemDef item, float scale = 1f, bool glow = false)
        {
            CardRarity rarity = item != null ? item.rarity : CardRarity.Common;
            Color rc = RarityColors.Get(rarity);
            var size = CardTile.BaseSize;

            var root = UiKit.Rect("Item_" + (item != null ? item.id : "none"), parent);
            root.sizeDelta = size;
            root.localScale = Vector3.one * scale;

            if (glow)
            {
                var g = UiKit.Img("Glow", root, new Color(rc.r, rc.g, rc.b, 0.55f), UiKit.Glow);
                UiKit.Place(g.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, size * 1.9f);
            }

            var frame = UiKit.Img("Frame", root, rc, UiKit.RoundedRect, true);
            UiKit.Stretch(frame.rectTransform);
            var sh = frame.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.5f);
            sh.effectDistance = new Vector2(0, -6);

            var inner = UiKit.Img("Inner", root, Color.Lerp(UiTheme.Panel, rc, 0.12f), UiKit.RoundedRect, true);
            UiKit.Stretch(inner.rectTransform, 7, 7, 7, 7);

            // «витрина» с иконкой: радиальный свет цвета предмета
            var artBg = UiKit.Img("ArtBg", root, Color.Lerp(rc, Color.black, 0.62f), UiKit.RoundedRect, true);
            UiKit.Stretch(artBg.rectTransform, 16, 16, 16, 108);
            if (item != null)
            {
                var halo = UiKit.Img("Halo", artBg.rectTransform, new Color(item.color.r, item.color.g, item.color.b, 0.45f), UiKit.Glow);
                UiKit.Place(halo.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 190));
                var icon = UiKit.Img("Icon", artBg.rectTransform, Color.white, ItemIcons.Get(item));
                icon.preserveAspect = true;
                UiKit.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(118, 118));
                var ish = icon.gameObject.AddComponent<Shadow>();
                ish.effectColor = new Color(0, 0, 0, 0.45f);
                ish.effectDistance = new Vector2(0, -5);

                // тип предмета - плашка сверху витрины
                var kindBg = UiKit.Img("Kind", artBg.rectTransform, item.IsPassive ? new Color32(0x7A, 0x4C, 0xC8, 0xF0) : new Color32(0x24, 0x7A, 0x52, 0xF0), UiKit.RoundedRect, true);
                UiKit.Place(kindBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(128, 28));
                var kind = UiKit.Text("Label", kindBg.rectTransform, item.IsPassive ? "PASSIVE" : "USABLE", 17, Color.white);
                kind.characterSpacing = 3;
                UiKit.Stretch(kind.rectTransform);
            }

            string title = item != null ? item.name : "???";
            var t = UiKit.Text("Title", root, title, 26, UiTheme.Text);
            t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 26;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            UiKit.Place(t.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 86), new Vector2(176, 34));

            var r = UiKit.Text("Rarity", root, RarityColors.Name(rarity).ToUpper(), 20, rc);
            r.characterSpacing = 4;
            UiKit.Place(r.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(160, 28));

            // короткое описание между названием и редкостью
            if (item != null)
            {
                var d = UiKit.Text("Desc", root, item.description, 15, UiTheme.TextDim);
                d.enableAutoSizing = true; d.fontSizeMin = 10; d.fontSizeMax = 15;
                UiKit.Place(d.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 55), new Vector2(172, 30));
            }
            return root;
        }
    }
}
