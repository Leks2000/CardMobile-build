using System;
using System.Collections.Generic;
using Assets.Scrpits.Shop;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Экран мета-прогрессии: очки славы (Renown) и открытия - стартовые колоды и новые карты.
    /// Открывается из главного меню (кнопка UNLOCKS) и с финального экрана забега.
    /// </summary>
    public static class MetaScreen
    {
        public static GameObject Show(RectTransform parent, Action onClose = null)
        {
            var dim = UiKit.Img("MetaScreen", parent, new Color(0.03f, 0.02f, 0.05f, 0.96f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);
            var go = dim.gameObject;
            var root = dim.rectTransform;
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 0.2f).SetLink(go);

            var title = UiKit.Text("Title", root, "UNLOCKS", 84, UiTheme.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1200, 110));
            var points = UiKit.Text("Points", root, "", 44, UiTheme.Text);
            UiKit.Place(points.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -160), new Vector2(1400, 60));
            var hint = UiKit.Text("Hint", root, "Earn Renown every run: cleared nodes, elites, bosses and victories", 30, UiTheme.TextDim);
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -212), new Vector2(1400, 44));

            var rows = new List<Action>();

            // колонка колод
            var decksHeader = UiKit.Text("DecksHeader", root, "STARTER DECKS", 36, UiTheme.TextDim);
            UiKit.Place(decksHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(-420, -290), new Vector2(760, 50));
            for (int i = 0; i < MetaProgress.Decks.Count; i++)
            {
                var d = MetaProgress.Decks[i];
                rows.Add(Row(root, new Vector2(-420, -370 - i * 140), d.name, d.description, "deck:" + d.id, d.cost, () => MetaProgress.IsDeckUnlocked(d.id), rows, points));
            }

            // колонка карт
            var cardsHeader = UiKit.Text("CardsHeader", root, "NEW CARDS", 36, UiTheme.TextDim);
            UiKit.Place(cardsHeader.rectTransform, new Vector2(0.5f, 1f), new Vector2(420, -290), new Vector2(760, 50));
            int k = 0;
            foreach (var kv in MetaProgress.LockedCards)
            {
                var id = kv.Key;
                var data = CardDatabase.Get(id);
                string name = data != null ? data.Title : id;
                string desc = data != null ? (string.IsNullOrEmpty(data.cardInfo) ? CardAbilities.Describe(data) : data.cardInfo) : "";
                rows.Add(Row(root, new Vector2(420, -370 - k * 140), name, desc, "card:" + id, kv.Value, () => MetaProgress.IsCardUnlocked(id), rows, points));
                k++;
            }

            var close = UiKit.Button("Close", root, "Close", UiTheme.PanelLight, new Vector2(320, 96), () =>
            {
                UnityEngine.Object.Destroy(go);
                onClose?.Invoke();
            }, 44);
            close.transform.Find("Text").GetComponent<TMP_Text>().color = UiTheme.Text;
            UiKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(320, 96));

            RefreshAll(rows, points);
            return go;
        }

        private static void RefreshAll(List<Action> rows, TMP_Text points)
        {
            points.text = $"Renown: <color=#{UiKit.Hex(UiTheme.Accent)}>{MetaProgress.Points}</color>";
            foreach (var r in rows) r();
        }

        /// <summary>Строка открытия: название, описание, кнопка UNLOCK N / статус OWNED. Возвращает функцию обновления.</summary>
        private static Action Row(RectTransform root, Vector2 pos, string name, string desc, string key, int cost, Func<bool> unlocked,
            List<Action> all, TMP_Text points)
        {
            var bg = UiKit.Img("Row_" + key, root, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(bg.rectTransform, new Vector2(0.5f, 1f), pos, new Vector2(780, 124));
            var t = UiKit.Text("Name", bg.rectTransform, name, 38, UiTheme.Text, TextAlignmentOptions.TopLeft);
            UiKit.Stretch(t.rectTransform, 26, 250, 14, 60);
            var d = UiKit.Text("Desc", bg.rectTransform, desc, 24, UiTheme.TextDim, TextAlignmentOptions.TopLeft);
            d.enableAutoSizing = true; d.fontSizeMin = 16; d.fontSizeMax = 24;
            UiKit.Stretch(d.rectTransform, 26, 250, 62, 10);

            Button btn = null;
            btn = UiKit.Button("Unlock", bg.rectTransform, "", UiTheme.Accent, new Vector2(210, 80), () =>
            {
                if (MetaProgress.TryUnlock(key, cost))
                {
                    btn.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.6f).SetLink(btn.gameObject);
                    SoundFx.Play(SoundFx.Clip.Reveal);
                    RefreshAll(all, points);
                }
                else
                {
                    var rt = (RectTransform)btn.transform;
                    rt.DOKill(true);
                    rt.DOShakeAnchorPos(0.35f, new Vector2(12, 0), 20, 0).SetLink(rt.gameObject);
                }
            }, 32);
            UiKit.Place((RectTransform)btn.transform, new Vector2(1, 0.5f), new Vector2(-126, 0), new Vector2(210, 80));
            var label = btn.transform.Find("Text").GetComponent<TMP_Text>();

            return () =>
            {
                bool own = unlocked();
                bool can = !own && MetaProgress.Points >= cost;
                btn.interactable = !own;
                ((Image)btn.targetGraphic).color = own ? new Color(0.25f, 0.55f, 0.35f, 1f) : can ? UiTheme.Accent : new Color(0.38f, 0.35f, 0.42f, 1f);
                label.text = own ? "OWNED" : $"UNLOCK {cost}";
                label.color = own ? UiTheme.Text : can ? UiTheme.Background : UiTheme.TextDim;
            };
        }
    }

    /// <summary>Главное меню: кнопка UNLOCKS (строится кодом поверх сцены меню).</summary>
    public static class MainMenuMetaButton
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != Assets.Scrpits.Run.RunState.MainMenuSceneName) return;
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                SceneManager.MoveGameObjectToScene(es, scene);
            }
            var canvasGo = new GameObject("MetaCanvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            var btn = UiKit.Button("Unlocks", root, $"UNLOCKS  ({MetaProgress.Points})", UiTheme.Accent, new Vector2(420, 110), null, 38);
            UiKit.Place((RectTransform)btn.transform, new Vector2(1, 0), new Vector2(-240, 90), new Vector2(420, 110));
            var label = btn.transform.Find("Text").GetComponent<TMP_Text>();
            GameButton(btn.GetComponent<Image>(), label, false);
            label.fontSizeMax = 40;
            btn.onClick.AddListener(() => Show(root, label));

            DressStartButtons();
        }

        /// <summary>
        /// Кнопка в стиле игры - объёмная (UiKit.ButtonShape: тёмный контур, блик, нижняя кромка), как UNLOCK / OWNED.
        /// light = золотая главная кнопка, иначе тёмная.
        /// </summary>
        public static void GameButton(Image img, TMP_Text label, bool light)
        {
            if (img != null)
            {
                img.sprite = UiKit.ButtonShape;
                img.type = Image.Type.Sliced;
                img.color = light ? UiTheme.Accent : UiTheme.PanelLight;
                foreach (var sh in img.GetComponents<Shadow>()) sh.enabled = false;
                var sh2 = VisualTheme.Ensure<Shadow>(img.gameObject);
                sh2.enabled = true;
                sh2.effectColor = new Color(0, 0, 0, 0.45f);
                sh2.effectDistance = new Vector2(0, -6);
            }
            if (label != null)
            {
                VisualTheme.Style(label, 52, light ? (Color)new Color32(0x2A, 0x16, 0x08, 0xFF) : UiTheme.Text, false);
                var lrt = label.rectTransform;
                lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(12, 12); lrt.offsetMax = new Vector2(-12, -4);
                label.enableAutoSizing = true; label.fontSizeMin = 20; label.fontSizeMax = 56;
            }
        }

        /// <summary>Кнопка Start меню -> START / CONTINUE в стиле игры (рестарт - на экране поражения, не здесь).</summary>
        private static void DressStartButtons()
        {
            Assets.Scrpits.Location.MainMenuLogic start = null;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Assets.Scrpits.Location.MainMenuLogic>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (l.sceneName == Assets.Scrpits.Run.RunState.MapSceneName && l.GetComponent<Button>() != null) { start = l; break; }
            if (start == null) return;
            var label = start.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = Assets.Scrpits.Run.RunState.IsActive ? "CONTINUE" : "START";
            GameButton(start.GetComponent<Image>(), label, true);
            var rt = (RectTransform)start.transform;
            if (rt.anchorMin == rt.anchorMax && rt.rect.height < 90f) rt.sizeDelta = new Vector2(Mathf.Max(rt.sizeDelta.x, 420f), Mathf.Max(rt.sizeDelta.y, 110f));
        }

        private static void Show(RectTransform root, TMP_Text label)
        {
            MetaScreen.Show(root, () => label.text = $"UNLOCKS  ({MetaProgress.Points})");
        }
    }
}
