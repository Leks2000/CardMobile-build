using System.Collections.Generic;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Экран карты забега. Строит граф узлов (кнопки + линии) из MapGraph внутри своего Canvas.
    /// Eval: Assets.Scrpits.Map.MapController.SelectNode(1)
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public static MapController Instance { get; private set; }

        [SerializeField] private TMP_FontAsset font;
        [SerializeField] private Sprite panelSprite;
        [SerializeField] private Vector2 nodeSize = new Vector2(200f, 200f);
        [SerializeField] private float lineWidth = 10f;
        [SerializeField] private int restHealPercent = 30;
        [SerializeField] private int eventCoins = 10;

        private RectTransform mapArea;
        private RectTransform linesRoot;
        private RectTransform nodesRoot;
        private TMP_Text infoText;
        private GameObject popup;
        private TMP_Text popupText;
        private GameObject endPanel;
        private TMP_Text endTitle;

        private readonly Dictionary<int, Button> nodeButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, RectTransform> nodeRects = new Dictionary<int, RectTransform>();
        private readonly List<(int from, int to, Image img)> lines = new List<(int, int, Image)>();

        private static readonly Color ColBg = new Color(0.09f, 0.08f, 0.12f, 1f);
        private static readonly Color ColLineDim = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color ColLineDone = new Color(0.55f, 0.85f, 0.55f, 0.9f);
        private static readonly Color ColLineNext = new Color(1f, 0.85f, 0.3f, 0.95f);
        private static readonly Color ColHighlight = new Color(1f, 0.85f, 0.3f, 1f);
        private static readonly Color ColDone = new Color(0.55f, 0.85f, 0.55f, 1f);

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            if (font == null) font = Resources.Load<TMP_FontAsset>("Font/Gloria/GloriaHallelujah-Regular SDF");
            if (!RunState.IsActive) RunState.StartNewRun(); // карта открыта напрямую - начинаем забег
            Build();
            Refresh();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------------- public API ----------------

        /// <summary>Выбрать узел (как клик игрока). false - узел недоступен.</summary>
        public static bool SelectNode(int id)
        {
            if (!RunState.IsAvailable(id))
            {
                Debug.LogWarning($"[RUN] Node {id} is not available. {RunState.Describe()}");
                return false;
            }
            if (Instance != null) Instance.OnNodeClicked(id);
            else ResolveNode(id, out _);
            return true;
        }

        public void Refresh()
        {
            var available = RunState.GetAvailableNodes();
            foreach (var node in MapGraph.Nodes)
            {
                var btn = nodeButtons[node.id];
                var rt = nodeRects[node.id];
                var img = btn.GetComponent<Image>();
                var outline = btn.GetComponent<Outline>();
                bool done = RunState.IsCompleted(node.id);
                bool avail = available.Contains(node.id);
                bool current = node.id == RunState.CurrentNodeId;

                rt.DOKill();
                rt.localScale = Vector3.one;
                btn.interactable = avail;

                Color c = TypeColor(node.type);
                if (done) c = Color.Lerp(c, new Color(0.25f, 0.25f, 0.28f), 0.65f);
                else if (!avail) c.a = 0.35f;
                img.color = c;

                outline.enabled = done || avail || current;
                outline.effectColor = avail ? ColHighlight : (current ? Color.white : ColDone);

                var label = rt.Find("Label").GetComponent<TMP_Text>();
                label.alpha = (done || avail || current) ? 1f : 0.45f;
                var status = rt.Find("Status").GetComponent<TMP_Text>();
                status.text = done ? "CLEARED" : (avail ? "GO!" : "");
                status.color = done ? ColDone : ColHighlight;

                if (avail)
                {
                    rt.DOScale(1.12f, 0.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(rt.gameObject);
                }
            }

            foreach (var (from, to, img) in lines)
            {
                if (RunState.IsCompleted(from) && RunState.IsCompleted(to)) img.color = ColLineDone;
                else if (from == RunState.CurrentNodeId && available.Contains(to)) img.color = ColLineNext;
                else img.color = ColLineDim;
            }

            string hp = RunState.PlayerHP >= 0 ? $"HP {RunState.PlayerHP}/{RunState.PlayerHPMax}" : "HP full";
            infoText.text = $"{hp}    Coins {PlayerPrefs.GetInt("Coins", 0)}    Cleared {RunState.CompletedNodes.Count}/{MapGraph.Nodes.Length}";

            if (RunState.Outcome != RunOutcome.None) ShowEndPanel(RunState.Outcome == RunOutcome.Won);
        }

        // ---------------- node logic ----------------

        private void OnNodeClicked(int id)
        {
            if (!RunState.IsAvailable(id)) return;
            SetAllInteractable(false);
            if (ResolveNode(id, out string message))
            {
                // бой - сцена грузится внутри ResolveNode
                return;
            }
            ShowPopup(message);
            Refresh();
        }

        /// <summary>true - загружен бой; иначе узел завершён и message содержит текст для попапа.</summary>
        private static bool ResolveNode(int id, out string message)
        {
            message = null;
            MapGraph.TryGet(id, out var node);
            RunState.EnterNode(id);

            if (MapGraph.IsBattle(node.type))
            {
                RunState.LoadBattle();
                return true;
            }

            var ctrl = Instance;
            switch (node.type)
            {
                case MapNodeType.Rest:
                    int percent = ctrl != null ? ctrl.restHealPercent : 30;
                    int amount = RunState.PlayerHPMax > 0 ? Mathf.CeilToInt(RunState.PlayerHPMax * percent / 100f) : 0;
                    int healed = RunState.Heal(amount);
                    message = healed > 0 ? $"Rested at the campfire\n+{healed} HP" : "Rested at the campfire\nHP already full";
                    break;
                case MapNodeType.Shop:
                    message = "The merchant is away...\nShop coming soon";
                    break;
                default: // Event
                    int coins = ctrl != null ? ctrl.eventCoins : 10;
                    PlayerPrefs.SetInt("Coins", PlayerPrefs.GetInt("Coins", 0) + coins);
                    message = $"You search an abandoned camp\n+{coins} coins";
                    break;
            }
            RunState.CompleteCurrentNode();
            return false;
        }

        private void SetAllInteractable(bool value)
        {
            foreach (var b in nodeButtons.Values) b.interactable = value;
        }

        private void ShowPopup(string text)
        {
            popupText.text = text;
            popup.SetActive(true);
            popup.transform.GetChild(0).localScale = Vector3.one * 0.6f;
            popup.transform.GetChild(0).DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetLink(popup);
        }

        private void ClosePopup()
        {
            popup.SetActive(false);
            Refresh();
        }

        private void ShowEndPanel(bool won)
        {
            SetAllInteractable(false);
            foreach (var rt in nodeRects.Values) { rt.DOKill(); rt.localScale = Vector3.one; }
            endTitle.text = won ? "RUN COMPLETE!" : "RUN FAILED";
            endTitle.color = won ? ColHighlight : new Color(1f, 0.4f, 0.35f);
            endPanel.SetActive(true);
        }

        public static void GoToMainMenu() => RunState.ReturnToMainMenu();

        // ---------------- UI building ----------------

        private static Color TypeColor(MapNodeType t)
        {
            switch (t)
            {
                case MapNodeType.Battle: return new Color(0.75f, 0.30f, 0.28f);
                case MapNodeType.Elite: return new Color(0.62f, 0.25f, 0.70f);
                case MapNodeType.Event: return new Color(0.28f, 0.52f, 0.80f);
                case MapNodeType.Shop: return new Color(0.85f, 0.65f, 0.20f);
                case MapNodeType.Rest: return new Color(0.28f, 0.68f, 0.42f);
                default: return new Color(0.90f, 0.15f, 0.15f);
            }
        }

        private static string TypeName(MapNodeType t)
        {
            switch (t)
            {
                case MapNodeType.Battle: return "Fight";
                case MapNodeType.Elite: return "Elite";
                case MapNodeType.Event: return "?";
                case MapNodeType.Shop: return "Shop";
                case MapNodeType.Rest: return "Rest";
                default: return "BOSS";
            }
        }

        private void Build()
        {
            var root = (RectTransform)transform;
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            var bg = NewImage("Background", root, ColBg);
            Stretch(bg.rectTransform, 0, 0, 0, 0);

            var title = NewText("Title", root, "DUNGEON MAP", 80, Color.white);
            Anchor(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -150), new Vector2(0, 140));

            infoText = NewText("Info", root, "", 40, new Color(1f, 1f, 1f, 0.8f));
            Anchor(infoText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -250), new Vector2(0, 60));

            var hint = NewText("Hint", root, "Choose your path", 38, new Color(1f, 1f, 1f, 0.5f));
            Anchor(hint.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 110), new Vector2(0, 60));

            mapArea = NewRect("MapArea", root);
            Stretch(mapArea, 150, 150, 400, 300);
            linesRoot = NewRect("Lines", mapArea);
            Stretch(linesRoot, 0, 0, 0, 0);
            nodesRoot = NewRect("Nodes", mapArea);
            Stretch(nodesRoot, 0, 0, 0, 0);

            foreach (var node in MapGraph.Nodes) BuildNode(node);
            foreach (var (from, to) in MapGraph.Edges()) BuildLine(from, to);

            BuildPopup(root);
            BuildEndPanel(root);
            Canvas.ForceUpdateCanvases();
            LayoutLines();
        }

        private void BuildNode(MapNode node)
        {
            var img = NewImage($"Node_{node.id}_{node.type}", nodesRoot, TypeColor(node.type));
            img.sprite = panelSprite;
            img.type = Image.Type.Sliced;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = node.pos;
            rt.sizeDelta = node.type == MapNodeType.Boss ? nodeSize * 1.3f : nodeSize;
            rt.anchoredPosition = Vector2.zero;

            var outline = img.gameObject.AddComponent<Outline>();
            outline.effectDistance = new Vector2(6, -6);

            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.disabledColor = Color.white; // цвет задаём сами
            btn.colors = colors;
            int id = node.id;
            btn.onClick.AddListener(() => OnNodeClicked(id));

            var label = NewText("Label", rt, TypeName(node.type), node.type == MapNodeType.Boss ? 64 : 50, Color.white);
            Stretch(label.rectTransform, 0, 0, 0, 0);
            label.raycastTarget = false;

            var status = NewText("Status", rt, "", 34, ColHighlight);
            Anchor(status.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, -30), new Vector2(80, 50));
            status.raycastTarget = false;

            nodeButtons[node.id] = btn;
            nodeRects[node.id] = rt;
        }

        private void BuildLine(int from, int to)
        {
            var img = NewImage($"Line_{from}_{to}", linesRoot, ColLineDim);
            img.raycastTarget = false;
            lines.Add((from, to, img));
        }

        private bool landscape;

        /// <summary>Портрет: путь снизу вверх. Ландшафт: слева направо.</summary>
        private Vector2 MapPos(MapNode n) => landscape ? new Vector2(n.pos.y, n.pos.x) : n.pos;

        private void LayoutLines()
        {
            Vector2 rootSize = ((RectTransform)transform).rect.size;
            landscape = rootSize.x > rootSize.y;
            if (landscape) Stretch(mapArea, 170, 170, 330, 170);
            else Stretch(mapArea, 150, 150, 400, 300);

            Vector2 size = mapArea.rect.size;
            // размер узла - чтобы соседние узлы не перекрывались
            float rowGap = (landscape ? size.x : size.y) / 4f;
            float colGap = (landscape ? size.y : size.x) * 0.25f;
            float s = Mathf.Max(60f, Mathf.Min(nodeSize.x, rowGap * 0.7f, colGap * 0.8f));
            foreach (var node in MapGraph.Nodes)
            {
                if (!nodeRects.TryGetValue(node.id, out var nrt)) continue;
                nrt.anchorMin = nrt.anchorMax = MapPos(node);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = Vector2.one * (node.type == MapNodeType.Boss ? s * 1.25f : s);
                var label = nrt.Find("Label")?.GetComponent<TMP_Text>();
                if (label != null) label.fontSize = (node.type == MapNodeType.Boss ? 0.32f : 0.25f) * s;
            }

            foreach (var (from, to, img) in lines)
            {
                Vector2 a = Vector2.Scale(MapPos(MapGraph.Nodes[from]), size);
                Vector2 b = Vector2.Scale(MapPos(MapGraph.Nodes[to]), size);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = (a + b) * 0.5f;
                rt.sizeDelta = new Vector2(Vector2.Distance(a, b), lineWidth);
                rt.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (mapArea != null) LayoutLines();
        }

        private void BuildPopup(RectTransform root)
        {
            var dim = NewImage("Popup", root, new Color(0, 0, 0, 0.6f));
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            popup = dim.gameObject;

            var box = NewImage("Box", dim.rectTransform, new Color(0.18f, 0.16f, 0.22f, 1f));
            box.sprite = panelSprite; box.type = Image.Type.Sliced;
            Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820, 520));

            popupText = NewText("Text", box.rectTransform, "", 46, Color.white);
            popupText.lineSpacing = -25f;
            Anchor(popupText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -170), new Vector2(-60, 280));

            NewButton("OK", box.rectTransform, "OK", new Vector2(0, 90), ClosePopup);
            popup.SetActive(false);
        }

        private void BuildEndPanel(RectTransform root)
        {
            var dim = NewImage("EndPanel", root, new Color(0, 0, 0, 0.8f));
            Stretch(dim.rectTransform, 0, 0, 0, 0);
            endPanel = dim.gameObject;

            endTitle = NewText("Title", dim.rectTransform, "", 110, Color.white);
            Anchor(endTitle.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0, 150), new Vector2(0, 200));

            var btn = NewButton("MainMenu", dim.rectTransform, "Main Menu", Vector2.zero, GoToMainMenu);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, -120);
            endPanel.SetActive(false);
        }

        private Button NewButton(string name, RectTransform parent, string text, Vector2 bottomOffset, UnityEngine.Events.UnityAction onClick)
        {
            var img = NewImage(name, parent, new Color(0.85f, 0.65f, 0.2f, 1f));
            img.sprite = panelSprite; img.type = Image.Type.Sliced;
            Anchor(img.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), bottomOffset, new Vector2(420, 130));
            var btn = img.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(onClick);
            var t = NewText("Text", img.rectTransform, text, 54, new Color(0.12f, 0.1f, 0.14f));
            Stretch(t.rectTransform, 0, 0, 0, 0);
            t.raycastTarget = false;
            return btn;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image NewImage(string name, Transform parent, Color color)
        {
            var img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private TMP_Text NewText(string name, Transform parent, string text, float size, Color color)
        {
            var t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>pos - anchoredPosition, size - sizeDelta.</summary>
        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
