using System;
using System.Collections.Generic;
using Assets.Scrpits.Run;
using Assets.Scrpits.Shop;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scrpits.Map
{
    /// <summary>
    /// Экран карты забега. Строит граф узлов (кнопки + пунктирные пути) из MapGraph внутри своего Canvas,
    /// фишку игрока (MapPlayerToken), HUD (HP / монеты / колода / реликвии), попапы узлов и тосты наград.
    /// Выбор узла: фишка идёт по пути -> приземление -> действие узла (бой/магазин - загрузка сцены, событие/привал - попап).
    /// Eval: Assets.Scrpits.Map.MapController.SelectNode(1), .Choose(0), .Ok(), .Busy, .DescribeUi()
    /// </summary>
    public class MapController : MonoBehaviour
    {
        public static MapController Instance { get; private set; }

        // шрифт - UiTheme.Font, цвета - UiTheme; старые поля font/panelSprite в сцене больше не используются
        [SerializeField] private Vector2 nodeSize = new Vector2(200f, 200f);

        /// <summary>Для тестов: имя события для следующего Event-узла ("altar" | "wagon" | "well").</summary>
        public static string ForceEvent;

        private RectTransform root;
        private RectTransform mapArea, linesRoot, fxRoot, nodesRoot, tokenRoot;
        private TMP_Text hintText;
        private MapPlayerToken token;
        private Image fade;

        // HUD
        private TMP_Text hudHp, hudCoins, hudDeck, hudRelics, hudItems;
        private Image hudHpFill;
        private RectTransform hudCoinIcon;
        private int shownCoins = -1;

        // popup
        private GameObject popup;
        private NodeScenario popupScenario;
        private Action popupOk;

        // toasts
        private RectTransform toastRoot;
        private readonly Queue<(string title, string body, CardData card)> toasts = new Queue<(string, string, CardData)>();
        private bool toastShowing;
        private GameObject toastGo;

        private GameObject endPanel;
        private TMP_Text endTitle, endStats;

        private readonly Dictionary<int, Button> nodeButtons = new Dictionary<int, Button>();
        private readonly Dictionary<int, RectTransform> nodeRects = new Dictionary<int, RectTransform>();
        private readonly List<Edge> edges = new List<Edge>();
        private float nodeS = 110f;
        private bool landscape;
        private bool busy;
        private bool actBanner;

        private class Edge
        {
            public int from, to;
            public RectTransform root;
            public readonly List<Image> dots = new List<Image>();
            public float lit = -1f; // прогресс подсветки при движении фишки
            public bool next;
        }

        private static readonly Color ColLineDim = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color ColDone = new Color32(0x6E, 0xD8, 0x8A, 0xFF);

        public static bool Busy => Instance != null && (Instance.busy || (Instance.token != null && Instance.token.IsMoving));

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            root = (RectTransform)transform;
            if (!RunState.IsActive) RunState.StartNewRun(); // карта открыта напрямую - начинаем забег
            RunState.EnsureMap();
            // босс акта побеждён -> новая локация
            if (RunState.PendingActAdvance)
            {
                RunState.AdvanceAct();
                actBanner = true;
            }
            if (RunState.Act == 1 && RunState.CurrentNodeId < 0 && RunState.CompletedNodes.Count == 0) actBanner = true;

            // страховка: небоевой узел, в который вошли, но не завершили (например, вышли из магазина иначе)
            if (RunState.CurrentNodeId >= 0 && !RunState.IsCompleted(RunState.CurrentNodeId) && !RunState.IsCurrentNodeBattle
                && RunState.Outcome == RunOutcome.None)
                RunState.CompleteCurrentNode();

            Build();
            Refresh();
        }

        private void Start()
        {
            // прибытие на карту
            fade.color = new Color(0, 0, 0, 1f);
            fade.raycastTarget = false;
            fade.DOFade(0f, 0.4f).SetLink(fade.gameObject);
            if (RunState.CurrentNodeId >= 0) token.ArrivePulse(UiTheme.Accent);

            // тосты наград
            if (RunState.PendingBattleReward)
            {
                RunState.PendingBattleReward = false;
                QueueBattleToast();
            }
            foreach (var (text, card) in RunState.PendingToasts) toasts.Enqueue((null, text, card));
            RunState.PendingToasts.Clear();
            float delay = 0.6f;
            if (actBanner) { ShowActBanner(); delay += 2.2f; }
            if (toasts.Count > 0) DOVirtual.DelayedCall(delay, ShowNextToast).SetLink(gameObject);
            if (RunState.NeedsDeckChoice) DOVirtual.DelayedCall(delay, ShowDeckChoice).SetLink(gameObject);
        }

        private void OnEnable() => Wallet.Changed += OnCoinsChanged;
        private void OnDisable() => Wallet.Changed -= OnCoinsChanged;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // "бегущие" точки на доступных путях
            float time = Time.unscaledTime;
            foreach (var e in edges)
            {
                if (!e.next || e.lit >= 0f) continue;
                for (int i = 0; i < e.dots.Count; i++)
                {
                    float a = 0.45f + 0.5f * Mathf.Max(0f, Mathf.Sin(time * 5f - i * 0.7f));
                    e.dots[i].color = new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, a);
                }
            }
        }

        // ---------------- public API ----------------

        /// <summary>Выбрать узел (как клик игрока). false - узел недоступен / идёт анимация.</summary>
        public static bool SelectNode(int id)
        {
            if (!RunState.IsAvailable(id))
            {
                Debug.LogWarning($"[RUN] Node {id} is not available. {RunState.Describe()}");
                return false;
            }
            if (Instance != null)
            {
                if (Busy) { Debug.LogWarning("[MAP] Busy (token moving / popup open)"); return false; }
                Instance.OnNodeClicked(id);
            }
            else ResolveWithoutUi(id);
            return true;
        }

        /// <summary>Выбрать вариант в открытом попапе события/привала.</summary>
        public static bool Choose(int index)
        {
            if (Instance == null || Instance.popupScenario == null) return false;
            var ch = Instance.popupScenario.choices;
            if (index < 0 || index >= ch.Count || !ch[index].IsEnabled) return false;
            Instance.PickChoice(ch[index]);
            return true;
        }

        /// <summary>Нажать OK в попапе результата.</summary>
        public static bool Ok()
        {
            if (Instance == null || Instance.popupOk == null) return false;
            Instance.popupOk();
            return true;
        }

        public static string DescribeUi()
        {
            if (Instance == null) return "no map";
            var i = Instance;
            string pop = i.popup != null && i.popup.activeSelf
                ? (i.popupScenario != null ? $"choices[{i.popupScenario.title}: {string.Join(" | ", i.popupScenario.choices.ConvertAll(c => c.label + (c.IsEnabled ? "" : "(off)")))}]" : "result")
                : "none";
            return $"busy={Busy} moving={i.token.IsMoving} popup={pop} toast={i.toastShowing}+{i.toasts.Count} token={i.token.Position} " +
                   $"hud=[{i.hudHp.text} | {i.hudCoins.text} | {i.hudDeck.text} | {i.hudRelics.text}] {RunState.Describe()}";
        }

        public void Refresh()
        {
            var available = RunState.GetAvailableNodes();
            foreach (var node in MapGraph.Nodes)
            {
                var btn = nodeButtons[node.id];
                var rt = nodeRects[node.id];
                bool done = RunState.IsCompleted(node.id);
                bool avail = available.Contains(node.id);
                bool current = node.id == RunState.CurrentNodeId;

                rt.DOKill();
                rt.localScale = Vector3.one;
                btn.interactable = avail && !busy;

                Color c = TypeColor(node.type);
                var disc = rt.Find("Disc").GetComponent<Image>();
                var inner = rt.Find("Disc/Inner").GetComponent<Image>();
                var ring = rt.Find("Ring").GetComponent<Image>();
                var icon = rt.Find("Icon").GetComponent<Graphic>();
                var glow = rt.Find("Glow").GetComponent<Image>();
                var label = rt.Find("Label").GetComponent<TMP_Text>();
                var status = rt.Find("Status").GetComponent<TMP_Text>();

                float dim = (done || avail || current) ? 1f : 0.45f;
                Color discC = done ? Color.Lerp(c, new Color(0.22f, 0.2f, 0.26f), 0.6f) : c;
                disc.color = new Color(discC.r * dim + (1 - dim) * 0.1f, discC.g * dim + (1 - dim) * 0.09f, discC.b * dim + (1 - dim) * 0.13f, 1f);
                inner.color = Color.Lerp(disc.color, Color.black, 0.3f);
                icon.color = new Color(1f, 1f, 1f, done ? 0.55f : (avail || current ? 1f : 0.45f));
                ring.color = avail ? UiTheme.Accent : current ? Color.white : done ? ColDone : new Color(1, 1, 1, 0.12f);
                label.color = avail ? UiTheme.Accent : new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, done || current ? 0.85f : 0.45f);
                status.text = done ? "cleared" : (avail ? "GO!" : "");
                status.color = done ? ColDone : UiTheme.Accent;

                glow.DOKill();
                bool bossy = node.type == MapNodeType.Boss || node.type == MapNodeType.Elite;
                glow.color = new Color(c.r, c.g, c.b, avail ? 0.75f : (bossy && !done ? 0.35f : 0f));
                if (avail && !busy)
                {
                    glow.DOFade(0.35f, 0.8f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(glow.gameObject);
                }
                var hover = rt.GetComponent<MapNodeHover>();
                if (hover != null) hover.SetState(avail && !busy, TypeColor(node.type));
            }

            foreach (var e in edges)
            {
                bool traveled = RunState.IsCompleted(e.from) && (RunState.IsCompleted(e.to) || e.to == RunState.CurrentNodeId) && IsOnPath(e);
                e.next = !busy && e.from == RunState.CurrentNodeId && available.Contains(e.to);
                if (e.lit >= 0f) continue;
                Color col = traveled ? UiTheme.Accent : (e.next ? UiTheme.Accent : ColLineDim);
                if (!traveled && !e.next && RunState.IsCompleted(e.from) && RunState.IsCompleted(e.to)) col = ColLineDim;
                foreach (var d in e.dots) d.color = col;
            }

            RefreshHud();
            hintText.text = RunState.Outcome != RunOutcome.None ? "" :
                available.Count > 0 ? (RunState.CurrentNodeId < 0 ? "Tap a glowing node to begin" : "Choose your path") : "";

            if (RunState.Outcome != RunOutcome.None) ShowEndPanel(RunState.Outcome == RunOutcome.Won);
        }

        /// <summary>Ребро пройдено игроком (игрок был в from и потом в to). Истории нет - считаем по завершённым узлам.</summary>
        private static bool IsOnPath(Edge e)
        {
            return RunState.IsCompleted(e.from) && (RunState.IsCompleted(e.to) || RunState.CurrentNodeId == e.to);
        }

        // ---------------- node logic ----------------

        private void OnNodeClicked(int id)
        {
            if (!RunState.IsAvailable(id) || Busy) return;
            busy = true;
            int fromId = RunState.CurrentNodeId;
            Refresh();
            MapGraph.TryGet(id, out var node);
            var edge = edges.Find(e => e.from == fromId && e.to == id);
            if (edge != null) edge.lit = 0f;

            token.transform.SetAsLastSibling();
            token.MoveTo(NodePos(node), u =>
            {
                if (edge == null) return;
                edge.lit = u;
                for (int i = 0; i < edge.dots.Count; i++)
                {
                    float t = (i + 0.5f) / edge.dots.Count;
                    edge.dots[i].color = t <= u ? UiTheme.Accent : ColLineDim;
                }
            }, () => ArriveAt(id));
        }

        private void ArriveAt(int id)
        {
            MapGraph.TryGet(id, out var node);
            var rt = nodeRects[id];
            rt.DOKill(true);
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * 0.18f, 0.35f, 6, 0.6f).SetLink(rt.gameObject);
            RunState.EnterNode(id);
            foreach (var e in edges) e.lit = -1f;

            if (MapGraph.IsBattle(node.type) || node.type == MapNodeType.Shop)
            {
                bool shop = node.type == MapNodeType.Shop;
                fade.raycastTarget = true;
                fade.DOFade(1f, 0.45f).SetDelay(0.2f).SetLink(fade.gameObject).OnComplete(() =>
                {
                    if (shop) RunState.LoadShop();
                    else RunState.LoadBattle();
                });
                return;
            }

            NodeScenario sc;
            if (node.type == MapNodeType.Rest) sc = MapEvents.Rest();
            else
            {
                sc = string.IsNullOrEmpty(ForceEvent) ? MapEvents.RandomEvent() : MapEvents.ByName(ForceEvent);
                ForceEvent = null;
            }
            DOVirtual.DelayedCall(0.15f, () => ShowChoices(sc)).SetLink(gameObject);
        }

        private void PickChoice(NodeChoice choice)
        {
            string title = popupScenario != null ? popupScenario.title : "";
            var scenario = popupScenario;
            popupScenario = null;
            if (choice.pick != null)
            {
                // выбор карты из колоды: попап прячем, после выбора - результат, после отмены - снова варианты
                if (popup != null) { Destroy(popup); popup = null; }
                DeckPicker.Show(root, "Choose a card", "It becomes Senior: +1 ATK, +1 HP, golden frame", choice.pickFilter, idx =>
                {
                    NodeResult r;
                    try { r = choice.pick(idx); }
                    catch (Exception ex) { Debug.LogException(ex); r = new NodeResult(title, "Something went wrong..."); }
                    RunState.CompleteCurrentNode();
                    ShowResult(r);
                    RefreshHud();
                }, () => ShowChoices(scenario));
                return;
            }
            NodeResult res;
            try { res = choice.apply(); }
            catch (Exception ex) { Debug.LogException(ex); res = new NodeResult(title, "Something went wrong..."); }
            RunState.CompleteCurrentNode();
            Debug.Log($"[MAP] Node choice '{choice.label}' -> {res.title}: {res.text.Replace("\n", " / ")}{(res.card != null ? " card=" + res.card.Id : "")}");
            ShowResult(res);
            RefreshHud();
        }

        /// <summary>Узел без UI карты (eval вне MapScene): бой - загрузка, остальное - авто-награда.</summary>
        private static void ResolveWithoutUi(int id)
        {
            MapGraph.TryGet(id, out var node);
            RunState.EnterNode(id);
            if (MapGraph.IsBattle(node.type)) { RunState.LoadBattle(); return; }
            if (node.type == MapNodeType.Shop) { RunState.LoadShop(); return; }
            if (node.type == MapNodeType.Rest) RunState.Heal(Mathf.CeilToInt(Mathf.Max(0, RunState.PlayerHPMax) * MapEvents.RestHealPercent / 100f));
            else Wallet.Add(10);
            RunState.CompleteCurrentNode();
        }

        private void SetAllInteractable(bool value)
        {
            foreach (var b in nodeButtons.Values) b.interactable = value;
        }

        private void ShowEndPanel(bool won)
        {
            SetAllInteractable(false);
            foreach (var rt in nodeRects.Values) { rt.DOKill(); rt.localScale = Vector3.one; }
            endTitle.text = won ? "RUN COMPLETE!" : "RUN FAILED";
            endTitle.color = won ? UiTheme.Accent : UiTheme.Damage;
            int renown = RunState.RunScore;
            if (!RunState.RenownAwarded)
            {
                RunState.RenownAwarded = true;
                MetaProgress.AddPoints(renown);
            }
            endStats.text = $"Act {RunState.Act}   Coins earned: <color=#{UiKit.Hex(UiTheme.Accent)}>{RunState.CoinsEarned}</color>   Deck: {RunState.Deck.Count}   Relics: {RunState.Relics.Count}\n" +
                            $"Renown <color=#{UiKit.Hex(UiTheme.Accent)}>+{renown}</color>  (total {MetaProgress.Points}) - spend it in UNLOCKS";
            if (!endPanel.activeSelf)
            {
                endPanel.SetActive(true);
                var cg = endPanel.GetComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.DOFade(1f, 0.5f).SetLink(endPanel);
                endTitle.transform.localScale = Vector3.one * 0.5f;
                endTitle.transform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetLink(endPanel);
            }
        }

        public static void GoToMainMenu() => RunState.ReturnToMainMenu();

        /// <summary>Большая надпись «ACT N / локация» при входе в акт.</summary>
        private void ShowActBanner()
        {
            var info = MapGraph.Info(RunState.Act);
            var group = UiKit.Rect("ActBanner", root);
            UiKit.Stretch(group);
            group.SetSiblingIndex(toastRoot.GetSiblingIndex());
            var cg = group.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            var strip = UiKit.Img("Strip", group, new Color(0.03f, 0.02f, 0.05f, 0.85f), UiKit.Glow);
            UiKit.Place(strip.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2600, 420));
            var a = UiKit.Text("Act", group, $"ACT {RunState.Act}", 64, UiTheme.TextDim);
            a.characterSpacing = 12;
            UiKit.Place(a.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(1400, 90));
            var n = UiKit.Text("Name", group, info.name.ToUpper(), 120, UiTheme.Accent);
            UiKit.Place(n.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1800, 150));
            cg.alpha = 0f;
            var seq = DOTween.Sequence().SetLink(group.gameObject);
            seq.Append(cg.DOFade(1f, 0.4f));
            seq.AppendInterval(1.3f);
            seq.Append(cg.DOFade(0f, 0.5f));
            seq.OnComplete(() => Destroy(group.gameObject));
            SoundFx.Play(SoundFx.Clip.Reveal);
        }

        /// <summary>Выбор стартовой колоды (если открыто больше одной).</summary>
        private void ShowDeckChoice()
        {
            if (!RunState.NeedsDeckChoice || RunState.CurrentNodeId >= 0) return;
            busy = true;
            var decks = MetaProgress.UnlockedDecks();
            float rowH = 120f, gap = 16f;
            NewPopup(1060, 250f + decks.Count * (rowH + gap), out var box);
            var title = UiKit.Text("Title", box, "Choose your team", 64, UiTheme.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(980, 90));
            for (int i = 0; i < decks.Count; i++)
            {
                var d = decks[i];
                var btn = UiKit.Button("Deck" + i, box, "", UiTheme.PanelLight, new Vector2(940, rowH), () =>
                {
                    RunState.ChooseStarterDeck(d.id);
                    ClosePopup();
                });
                UiKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0, -160 - i * (rowH + gap) - rowH * 0.5f), new Vector2(940, rowH));
                var lbl = btn.transform.Find("Text").GetComponent<TMP_Text>();
                lbl.text = $"{d.name}\n<size=60%><color=#{UiKit.Hex(UiTheme.TextDim)}>{d.description}</color></size>";
                lbl.fontSize = 44;
                lbl.color = UiTheme.Text;
                lbl.alignment = TextAlignmentOptions.Left;
                UiKit.Stretch(lbl.rectTransform, 36, 36, 0, 12);
            }
        }

        // ---------------- HUD ----------------

        private void RefreshHud()
        {
            if (hudHp == null) return;
            bool known = RunState.PlayerHPMax > 0;
            hudHp.text = known ? $"{RunState.PlayerHP}/{RunState.PlayerHPMax}" : "--";
            hudHpFill.fillAmount = known ? Mathf.Clamp01((float)RunState.PlayerHP / RunState.PlayerHPMax) : 1f;
            hudHpFill.color = hudHpFill.fillAmount > 0.35f ? UiTheme.Heal : UiTheme.Damage;
            if (shownCoins < 0) shownCoins = Wallet.Coins;
            hudCoins.text = shownCoins.ToString();
            hudDeck.text = RunState.Deck.Count.ToString();
            hudRelics.text = RunState.Relics.Count.ToString();
            if (hudItems != null) hudItems.text = RunState.TotalItems.ToString();
        }

        private void OnCoinsChanged(int coins) => AnimateCoins(shownCoins, coins);

        private void AnimateCoins(int from, int to)
        {
            if (hudCoins == null) return;
            DOTween.Kill("mapCoins");
            DOVirtual.Int(from, to, 0.9f, v => { shownCoins = v; hudCoins.text = v.ToString(); })
                .SetEase(Ease.OutCubic).SetId("mapCoins").SetLink(gameObject);
            hudCoinIcon.DOKill(true);
            hudCoinIcon.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6, 0.5f).SetLink(hudCoinIcon.gameObject);
        }

        // ---------------- toasts ----------------

        private void QueueBattleToast()
        {
            var lines = new List<string>();
            foreach (var (label, coins) in BattleRewards.LastBreakdown)
                lines.Add($"{label}  <color=#{UiKit.Hex(UiTheme.Accent)}>+{coins}</color>");
            if (BattleRewards.LastCoins > 0)
                lines.Add($"<size=120%>Total  <color=#{UiKit.Hex(UiTheme.Accent)}>+{BattleRewards.LastCoins} coins</color></size>");
            if (BattleRewards.LastRelic != null)
            {
                var r = BattleRewards.LastRelic;
                lines.Add($"Relic: <color=#{UiKit.Hex(RarityColors.Get(r.rarity))}>{r.name}</color>");
            }
            if (BattleRewards.LastItem != null)
            {
                var it = BattleRewards.LastItem;
                lines.Add($"Item: <color=#{UiKit.Hex(RarityColors.Get(it.rarity))}>{it.name}</color>");
            }
            if (lines.Count == 0) lines.Add("The enemy is defeated.");
            toasts.Enqueue(("VICTORY REWARDS", string.Join("\n", lines), null));
            // монеты уже начислены оверлеем - показать счётчик "с нуля"
            if (BattleRewards.LastCoins > 0)
            {
                shownCoins = Mathf.Max(0, Wallet.Coins - BattleRewards.LastCoins);
                RefreshHud();
                DOVirtual.DelayedCall(0.9f, () => AnimateCoins(shownCoins, Wallet.Coins)).SetLink(gameObject);
            }
        }

        private void ShowNextToast()
        {
            if (toastShowing || toasts.Count == 0) return;
            toastShowing = true;
            var (title, body, card) = toasts.Dequeue();

            if (toastGo != null) Destroy(toastGo);
            var panel = UiKit.Img("Toast", toastRoot, UiTheme.Panel, UiKit.RoundedRect, true);
            panel.raycastTarget = true;
            toastGo = panel.gameObject;
            int lineCount = body.Split('\n').Length + (title != null ? 1 : 0);
            float h = Mathf.Max(card != null ? 250f : 120f, 60f + lineCount * 50f);
            var prt = panel.rectTransform;
            UiKit.Place(prt, new Vector2(1f, 1f), new Vector2(-330, -140 - h * 0.5f), new Vector2(600, h));
            var frame = UiKit.Img("Frame", prt, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.8f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frame.rectTransform);
            var sh = panel.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.5f); sh.effectDistance = new Vector2(0, -8);

            float left = 30f;
            if (card != null)
            {
                var tile = CardTile.Build(prt, card, 0.75f);
                tile.anchorMin = tile.anchorMax = new Vector2(0, 0.5f);
                tile.anchoredPosition = new Vector2(20 + CardTile.BaseSize.x * 0.75f * 0.5f, 0);
                left = 20 + CardTile.BaseSize.x * 0.75f + 20;
            }
            string text = (title != null ? $"<color=#{UiKit.Hex(UiTheme.Accent)}><size=115%>{title}</size></color>\n" : "") + body;
            var t = UiKit.Text("Text", prt, text, 36, UiTheme.Text, card != null ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
            t.lineSpacing = -6;
            UiKit.Stretch(t.rectTransform, left, 24, 14, 14);

            var btn = panel.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(HideToast);

            prt.anchoredPosition += new Vector2(700, 0);
            prt.DOAnchorPosX(-330, 0.45f).SetEase(Ease.OutBack).SetLink(toastGo);
            float stay = 3.2f + lineCount * 0.35f;
            DOVirtual.DelayedCall(stay, HideToast).SetLink(toastGo).SetId("toastHide");
        }

        private void HideToast()
        {
            if (!toastShowing || toastGo == null) return;
            DOTween.Kill("toastHide");
            var go = toastGo;
            toastGo = null;
            var prt = (RectTransform)go.transform;
            prt.DOAnchorPosX(-330 + 700, 0.35f).SetEase(Ease.InBack).SetLink(go).OnComplete(() =>
            {
                Destroy(go);
                toastShowing = false;
                ShowNextToast();
            });
        }

        // ---------------- popups ----------------

        private GameObject NewPopup(float width, float height, out RectTransform box)
        {
            if (popup != null) Destroy(popup);
            var dim = UiKit.Img("Popup", root, new Color(0.02f, 0.01f, 0.04f, 0.7f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);
            popup = dim.gameObject;
            popup.transform.SetSiblingIndex(toastRoot.GetSiblingIndex());

            var b = UiKit.Img("Box", dim.rectTransform, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(b.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            var sh = b.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(0, -10);
            var frame = UiKit.Img("Frame", b.rectTransform, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.6f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frame.rectTransform);
            box = b.rectTransform;

            dim.color = new Color(0.02f, 0.01f, 0.04f, 0f);
            dim.DOFade(0.7f, 0.2f).SetLink(popup);
            box.localScale = Vector3.one * 0.7f;
            box.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetLink(popup);
            return popup;
        }

        private void ShowChoices(NodeScenario sc)
        {
            popupScenario = sc;
            popupOk = null;
            float rowH = 104f, gap = 16f;
            float height = 330f + sc.choices.Count * (rowH + gap);
            NewPopup(1060, height, out var box);

            var title = UiKit.Text("Title", box, sc.title, 66, UiTheme.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(980, 90));
            var body = UiKit.Text("Body", box, sc.body, 36, UiTheme.TextDim);
            body.lineSpacing = -4;
            UiKit.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(960, 110));

            for (int i = 0; i < sc.choices.Count; i++)
            {
                var ch = sc.choices[i];
                bool on = ch.IsEnabled;
                var btn = UiKit.Button("Choice" + i, box, "", on ? UiTheme.PanelLight : new Color(0.2f, 0.18f, 0.24f), new Vector2(940, rowH), null);
                UiKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 1f), new Vector2(0, -280 - i * (rowH + gap) - rowH * 0.5f), new Vector2(940, rowH));
                btn.interactable = on;
                var captured = ch;
                btn.onClick.AddListener(() => { if (popupScenario != null) PickChoice(captured); });
                var lbl = btn.transform.Find("Text").GetComponent<TMP_Text>();
                lbl.text = ch.label;
                lbl.fontSize = 44;
                lbl.color = on ? UiTheme.Text : UiTheme.TextDim;
                lbl.alignment = TextAlignmentOptions.Left;
                UiKit.Stretch(lbl.rectTransform, 36, 470, 0, 0);
                var det = UiKit.Text("Detail", btn.transform, on ? ch.detail : $"<color=#{UiKit.Hex(UiTheme.TextDim)}>{StripTags(ch.detail)}</color>", 32, UiTheme.Text, TextAlignmentOptions.Right);
                UiKit.Stretch(det.rectTransform, 400, 36, 0, 0);
                var accent = UiKit.Img("Accent", btn.transform, on ? UiTheme.Accent : UiTheme.TextDim, UiKit.RoundedRect, true);
                UiKit.Place(accent.rectTransform, new Vector2(0, 0.5f), new Vector2(10, 0), new Vector2(12, rowH - 30));
            }
        }

        private void ShowResult(NodeResult res)
        {
            popupScenario = null;
            bool hasCard = res.card != null;
            NewPopup(hasCard ? 1060 : 860, hasCard ? 560 : 440, out var box);

            float textLeft = 40f;
            if (hasCard)
            {
                var tile = CardTile.Build(box, res.card, 1.3f, true);
                tile.anchorMin = tile.anchorMax = new Vector2(0, 0.5f);
                tile.anchoredPosition = new Vector2(200, 20);
                tile.localRotation = Quaternion.Euler(0, 0, 6);
                tile.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack).SetLink(tile.gameObject);
                textLeft = 380f;
            }
            var title = UiKit.Text("Title", box, res.title, 62, UiTheme.Accent, hasCard ? TextAlignmentOptions.Left : TextAlignmentOptions.Center);
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1f);
            trt.offsetMin = new Vector2(textLeft, -140); trt.offsetMax = new Vector2(-40, -40);
            string txt = res.text + (hasCard ? $"\n<size=80%><color=#{UiKit.Hex(UiTheme.TextDim)}>{CardTile.Stats(res.card)}</color></size>" : "");
            var body = UiKit.Text("Body", box, txt, 42, UiTheme.Text, hasCard ? TextAlignmentOptions.TopLeft : TextAlignmentOptions.Top);
            body.lineSpacing = -4;
            var brt = body.rectTransform;
            brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 1);
            brt.offsetMin = new Vector2(textLeft, 150); brt.offsetMax = new Vector2(-40, -150);

            popupOk = ClosePopup;
            var ok = UiKit.Button("OK", box, "OK", UiTheme.Accent, new Vector2(320, 100), ClosePopup, 50);
            UiKit.Place((RectTransform)ok.transform, new Vector2(hasCard ? 0.66f : 0.5f, 0f), new Vector2(0, 85), new Vector2(320, 100));
        }

        private void ClosePopup()
        {
            popupOk = null;
            popupScenario = null;
            if (popup != null)
            {
                var go = popup;
                popup = null;
                go.GetComponent<Image>().raycastTarget = false;
                go.transform.GetChild(0).DOScale(0.8f, 0.15f).SetEase(Ease.InBack).SetLink(go)
                    .OnComplete(() => Destroy(go));
            }
            busy = false;
            Refresh();
        }

        private static string StripTags(string s) => System.Text.RegularExpressions.Regex.Replace(s ?? "", "<.*?>", "");

        // ---------------- UI building ----------------

        private static Color TypeColor(MapNodeType t)
        {
            switch (t)
            {
                case MapNodeType.Battle: return new Color32(0xB8, 0x4A, 0x3E, 0xFF);
                case MapNodeType.Elite: return new Color32(0x8E, 0x3F, 0xC8, 0xFF);
                case MapNodeType.Event: return new Color32(0x3E, 0x7F, 0xC8, 0xFF);
                case MapNodeType.Shop: return new Color32(0xD9, 0x9A, 0x2B, 0xFF);
                case MapNodeType.Rest: return new Color32(0x3E, 0xA8, 0x62, 0xFF);
                default: return new Color32(0xD8, 0x24, 0x24, 0xFF);
            }
        }

        private static string TypeName(MapNodeType t)
        {
            switch (t)
            {
                case MapNodeType.Battle: return "Fight";
                case MapNodeType.Elite: return "Elite";
                case MapNodeType.Event: return "Event";
                case MapNodeType.Shop: return "Shop";
                case MapNodeType.Rest: return "Rest";
                default: return "BOSS";
            }
        }

        /// <summary>Подсказка при наведении на узел.</summary>
        private static string NodeHint(MapNodeType t) => t switch
        {
            MapNodeType.Battle => "Regular enemies\nReward: coins, maybe an item",
            MapNodeType.Elite => "Strong enemy\nReward: more coins, relic, item",
            MapNodeType.Event => "Something unexpected\nChoices with risks and rewards",
            MapNodeType.Shop => "Card cases, item cases\nand the merchant",
            MapNodeType.Rest => "Heal or search the camp",
            _ => "The final fight",
        };

        private static float SizeMul(MapNodeType t) => t == MapNodeType.Boss ? 1.5f : t == MapNodeType.Elite ? 1.2f : 1f;

        private void Build()
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            var bg = UiKit.Img("Background", root, UiTheme.Background);
            UiKit.Stretch(bg.rectTransform);
            // фон локации акта (арт проекта), приглушённый, чтобы узлы читались
            var act = MapGraph.Info(RunState.Act);
            var art = Resources.Load<Sprite>(act.background);
            if (art != null)
            {
                var artImg = UiKit.Img("LocationArt", root, act.tint * 0.55f, art);
                artImg.color = new Color(act.tint.r * 0.55f, act.tint.g * 0.55f, act.tint.b * 0.55f, 1f);
                UiKit.Stretch(artImg.rectTransform);
                var fitter = artImg.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = art.rect.width / Mathf.Max(1f, art.rect.height);
            }
            var glow = UiKit.Img("BgGlow", root, new Color(UiTheme.PanelLight.r, UiTheme.PanelLight.g, UiTheme.PanelLight.b, art != null ? 0.35f : 0.8f), UiKit.Glow);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.45f), Vector2.zero, new Vector2(2600, 1500));
            var bottom = UiKit.Img("BgShade", root, new Color(0, 0, 0, 0.5f), UiKit.VGradient);
            bottom.rectTransform.localScale = new Vector3(1, -1, 1);
            UiKit.Place(bottom.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(4000, 220));

            var title = UiKit.Text("Title", root, $"ACT {RunState.Act}: {act.name.ToUpper()}", 76, UiTheme.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(1400, 110));
            var tsh = title.gameObject.AddComponent<Shadow>();
            tsh.effectColor = new Color(0, 0, 0, 0.6f); tsh.effectDistance = new Vector2(0, -5);

            BuildHud();

            hintText = UiKit.Text("Hint", root, "", 38, UiTheme.TextDim);
            UiKit.Place(hintText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1000, 60));
            hintText.DOFade(0.5f, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(hintText.gameObject);

            mapArea = UiKit.Rect("MapArea", root);
            UiKit.Stretch(mapArea, 150, 150, 400, 300);
            linesRoot = UiKit.Rect("Lines", mapArea); UiKit.Stretch(linesRoot);
            fxRoot = UiKit.Rect("Fx", mapArea); UiKit.Stretch(fxRoot);
            nodesRoot = UiKit.Rect("Nodes", mapArea); UiKit.Stretch(nodesRoot);
            tokenRoot = UiKit.Rect("Token", mapArea); UiKit.Stretch(tokenRoot);

            foreach (var node in MapGraph.Nodes) BuildNode(node);
            foreach (var (from, to) in MapGraph.Edges())
            {
                var er = UiKit.Rect($"Path_{from}_{to}", linesRoot);
                UiKit.Stretch(er); // точки считаются от левого нижнего угла области карты (как и узлы)
                edges.Add(new Edge { from = from, to = to, root = er });
            }
            token = MapPlayerToken.Create(tokenRoot, fxRoot);

            BuildEndPanel();
            toastRoot = UiKit.Rect("Toasts", root); UiKit.Stretch(toastRoot);

            fade = UiKit.Img("Fade", root, new Color(0, 0, 0, 0));
            UiKit.Stretch(fade.rectTransform);

            Canvas.ForceUpdateCanvases();
            LayoutMap();
        }

        private void BuildHud()
        {
            var bar = UiKit.Rect("Hud", root);
            UiKit.Place(bar, new Vector2(0.5f, 1f), new Vector2(0, -175), new Vector2(1240, 76));
            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 22; layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;

            hudHp = HudChip(bar, "HP", 260, UiTheme.Damage, UiKit.Circle, "+", out var hpIcon);
            var track = UiKit.Img("Track", hudHp.transform.parent, new Color(0, 0, 0, 0.5f), UiKit.RoundedRect, true);
            UiKit.Place(track.rectTransform, new Vector2(0, 0), new Vector2(0, 14), new Vector2(150, 10));
            track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0, 0);
            track.rectTransform.anchoredPosition = new Vector2(84 + 75, 14);
            hudHpFill = UiKit.Img("Fill", track.rectTransform, UiTheme.Heal, UiKit.Circle);
            UiKit.Stretch(hudHpFill.rectTransform);
            hudHpFill.sprite = null;
            hudHpFill.type = Image.Type.Filled;
            hudHpFill.fillMethod = Image.FillMethod.Horizontal;

            hudCoins = HudChip(bar, "Coins", 200, UiTheme.Accent, UiKit.Circle, "", out hudCoinIcon);
            var coinRing = UiKit.Img("Ring", hudCoinIcon, new Color(0.8f, 0.5f, 0.08f), UiKit.Ring);
            UiKit.Stretch(coinRing.rectTransform, 8, 8, 8, 8);
            hudDeck = HudChip(bar, "Deck", 200, UiTheme.Mana, UiKit.RoundedRect, "", out var deckIcon);
            deckIcon.sizeDelta = new Vector2(36, 48);
            var deckBack = UiKit.Img("Back", deckIcon, new Color(0.2f, 0.4f, 0.7f), UiKit.RoundedRect);
            deckBack.transform.SetAsFirstSibling();
            UiKit.Place(deckBack.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-7, -5), new Vector2(36, 48));
            hudRelics = HudChip(bar, "Relics", 200, new Color32(0xB0, 0x6C, 0xFF, 0xFF), UiKit.RoundedRect, "", out var relicIcon);
            relicIcon.sizeDelta = new Vector2(38, 38);
            relicIcon.localRotation = Quaternion.Euler(0, 0, 45);
            // [Items] расходники в сумке (иконка зелья)
            hudItems = HudChip(bar, "Items", 200, Color.white, ItemIcons.Get(ItemIcons.Potion, new Color32(0xE8, 0x3A, 0x4A, 0xFF)), "", out var itemIcon);
            itemIcon.GetComponent<Image>().preserveAspect = true;

            RefreshHud();
        }

        private TMP_Text HudChip(RectTransform parent, string name, float width, Color iconColor, Sprite iconSprite, string glyph, out RectTransform icon)
        {
            var chip = UiKit.Img("Chip_" + name, parent, UiTheme.Panel, UiKit.RoundedRect, true);
            chip.rectTransform.sizeDelta = new Vector2(width, 76);
            var ic = UiKit.Img("Icon", chip.rectTransform, iconColor, iconSprite);
            UiKit.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(42, 0), new Vector2(50, 50));
            icon = ic.rectTransform;
            if (!string.IsNullOrEmpty(glyph))
            {
                var g = UiKit.Text("Glyph", ic.rectTransform, glyph, 44, Color.white);
                UiKit.Stretch(g.rectTransform, 0, 0, 0, 4);
            }
            var cap = UiKit.Text("Caption", chip.rectTransform, name.ToUpper(), 18, UiTheme.TextDim, TextAlignmentOptions.TopLeft);
            cap.characterSpacing = 3;
            UiKit.Stretch(cap.rectTransform, 84, 8, 6, 44);
            var val = UiKit.Text("Value", chip.rectTransform, "", 40, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Stretch(val.rectTransform, 84, 8, 18, 0);
            return val;
        }

        private void BuildNode(MapNode node)
        {
            var rt = UiKit.Rect($"Node_{node.id}_{node.type}", nodesRoot);
            var c = TypeColor(node.type);

            var glow = UiKit.Img("Glow", rt, new Color(c.r, c.g, c.b, 0f), UiKit.Glow);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            glow.rectTransform.anchorMin = new Vector2(-0.45f, -0.45f); glow.rectTransform.anchorMax = new Vector2(1.45f, 1.45f);
            glow.rectTransform.sizeDelta = Vector2.zero;

            var shadow = UiKit.Img("Shadow", rt, new Color(0, 0, 0, 0.5f), UiKit.Circle);
            UiKit.Stretch(shadow.rectTransform, 0, 0, 8, -8);

            var disc = UiKit.Img("Disc", rt, c, UiKit.Circle);
            disc.raycastTarget = true;
            UiKit.Stretch(disc.rectTransform);
            var inner = UiKit.Img("Inner", disc.rectTransform, Color.black, UiKit.Circle);
            inner.rectTransform.anchorMin = new Vector2(0.12f, 0.12f); inner.rectTransform.anchorMax = new Vector2(0.88f, 0.88f);
            inner.rectTransform.sizeDelta = Vector2.zero;

            var ring = UiKit.Img("Ring", rt, Color.white, UiKit.Ring);
            ring.rectTransform.anchorMin = new Vector2(-0.06f, -0.06f); ring.rectTransform.anchorMax = new Vector2(1.06f, 1.06f);
            ring.rectTransform.sizeDelta = Vector2.zero;

            Graphic icon;
            Sprite sp = node.type switch
            {
                MapNodeType.Battle => UiKit.Sword,
                MapNodeType.Elite => UiKit.Crown,
                MapNodeType.Boss => UiKit.Skull,
                MapNodeType.Rest => UiKit.Flame,
                _ => null
            };
            if (sp != null)
            {
                var img = UiKit.Img("Icon", rt, Color.white, sp);
                img.preserveAspect = true;
                img.rectTransform.anchorMin = new Vector2(0.2f, 0.2f); img.rectTransform.anchorMax = new Vector2(0.8f, 0.8f);
                img.rectTransform.sizeDelta = Vector2.zero;
                if (node.type == MapNodeType.Battle) img.rectTransform.localRotation = Quaternion.Euler(0, 0, -35);
                icon = img;
            }
            else
            {
                var t = UiKit.Text("Icon", rt, node.type == MapNodeType.Event ? "?" : "$", 60, Color.white);
                UiKit.Stretch(t.rectTransform);
                t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 200;
                t.rectTransform.anchorMin = new Vector2(0.15f, 0.12f); t.rectTransform.anchorMax = new Vector2(0.85f, 0.9f);
                icon = t;
            }

            if (node.type == MapNodeType.Boss)
            {
                // корона над боссом
                var crown = UiKit.Img("Crown", rt, Color.white, UiKit.Crown);
                crown.preserveAspect = true;
                crown.rectTransform.anchorMin = new Vector2(0.28f, 0.86f); crown.rectTransform.anchorMax = new Vector2(0.72f, 1.2f);
                crown.rectTransform.sizeDelta = Vector2.zero;
                crown.rectTransform.localRotation = Quaternion.Euler(0, 0, 8);
            }

            var label = UiKit.Text("Label", rt, TypeName(node.type), 32, Color.white);
            label.rectTransform.anchorMin = new Vector2(-0.5f, 0f); label.rectTransform.anchorMax = new Vector2(1.5f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.anchoredPosition = new Vector2(0, -8);
            label.rectTransform.sizeDelta = new Vector2(0, 44);

            var status = UiKit.Text("Status", rt, "", 24, UiTheme.Accent);
            status.rectTransform.anchorMin = new Vector2(-0.5f, 0f); status.rectTransform.anchorMax = new Vector2(1.5f, 0f);
            status.rectTransform.pivot = new Vector2(0.5f, 1f);
            status.rectTransform.anchoredPosition = new Vector2(0, -48);
            status.rectTransform.sizeDelta = new Vector2(0, 32);

            rt.gameObject.AddComponent<MapNodeHover>().Init(TypeName(node.type), NodeHint(node.type));
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = disc;
            btn.transition = Selectable.Transition.None;
            int id = node.id;
            btn.onClick.AddListener(() => OnNodeClicked(id));

            nodeButtons[node.id] = btn;
            nodeRects[node.id] = rt;
        }

        /// <summary>Портрет: путь снизу вверх. Ландшафт: слева направо.</summary>
        private Vector2 MapPos(MapNode n) => landscape ? new Vector2(n.pos.y, n.pos.x) : n.pos;

        private Vector2 NodePos(MapNode n) => Vector2.Scale(MapPos(n), mapArea.rect.size);

        private Vector2 StartPos()
        {
            var n0 = MapGraph.Nodes[MapGraph.StartNodes[0]];
            return NodePos(n0) + (landscape ? new Vector2(-nodeS * 1.05f, 0) : new Vector2(0, -nodeS * 1.05f));
        }

        private void LayoutMap()
        {
            Vector2 rootSize = root.rect.size;
            landscape = rootSize.x > rootSize.y;
            if (landscape) Stretch(mapArea, 230, 190, 330, 190);
            else Stretch(mapArea, 150, 150, 420, 320);

            Vector2 size = mapArea.rect.size;
            float rowGap = (landscape ? size.x : size.y) / Mathf.Max(1, MapGraph.Rows - 1);
            float colGap = (landscape ? size.y : size.x) / (MapGraph.MaxPerRow + 1f);
            nodeS = Mathf.Max(56f, Mathf.Min(nodeSize.x * 0.65f, rowGap * 0.62f, colGap * 0.7f));
            foreach (var node in MapGraph.Nodes)
            {
                if (!nodeRects.TryGetValue(node.id, out var nrt)) continue;
                nrt.anchorMin = nrt.anchorMax = MapPos(node);
                nrt.pivot = new Vector2(0.5f, 0.5f);
                nrt.anchoredPosition = Vector2.zero;
                nrt.sizeDelta = Vector2.one * nodeS * SizeMul(node.type);
                var label = nrt.Find("Label").GetComponent<TMP_Text>();
                label.fontSize = Mathf.Clamp(nodeS * 0.3f, 22, 40) * (node.type == MapNodeType.Boss ? 1.25f : 1f);
            }

            float spacing = 24f, dotSize = Mathf.Clamp(nodeS * 0.1f, 8, 14);
            foreach (var e in edges)
            {
                UiKit.Stretch(e.root);
                foreach (var d in e.dots) if (d != null) Destroy(d.gameObject);
                e.dots.Clear();
                var na = MapGraph.Nodes[e.from];
                var nb = MapGraph.Nodes[e.to];
                Vector2 a = NodePos(na), b = NodePos(nb);
                float ra = nodeS * SizeMul(na.type) * 0.5f + 12f, rb = nodeS * SizeMul(nb.type) * 0.5f + 12f;
                float dist = Vector2.Distance(a, b);
                Vector2 dir = (b - a) / Mathf.Max(1f, dist);
                float len = dist - ra - rb;
                int count = Mathf.Max(2, Mathf.FloorToInt(len / spacing) + 1);
                for (int i = 0; i < count; i++)
                {
                    float t = count == 1 ? 0.5f : i / (float)(count - 1);
                    var dot = UiKit.Img("Dot", e.root, ColLineDim, UiKit.Circle);
                    var drt = dot.rectTransform;
                    drt.anchorMin = drt.anchorMax = Vector2.zero;
                    drt.sizeDelta = Vector2.one * dotSize;
                    drt.anchoredPosition = a + dir * (ra + len * t);
                    e.dots.Add(dot);
                }
            }

            token.Resize(nodeS);
            token.Place(RunState.CurrentNodeId >= 0 && MapGraph.TryGet(RunState.CurrentNodeId, out var cur) ? NodePos(cur) : StartPos());
        }

        private static void Stretch(RectTransform rt, float left, float right, float top, float bottom) => UiKit.Stretch(rt, left, right, top, bottom);

        private void OnRectTransformDimensionsChange()
        {
            if (mapArea != null && token != null)
            {
                LayoutMap();
                Refresh();
            }
        }

        private void BuildEndPanel()
        {
            var dim = UiKit.Img("EndPanel", root, new Color(0.02f, 0.01f, 0.04f, 0.88f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);
            endPanel = dim.gameObject;
            endPanel.AddComponent<CanvasGroup>();

            var glow = UiKit.Img("Glow", dim.rectTransform, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.18f), UiKit.Glow);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(1400, 700));

            endTitle = UiKit.Text("Title", dim.rectTransform, "", 120, Color.white);
            UiKit.Place(endTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(1400, 180));
            var sh = endTitle.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.7f); sh.effectDistance = new Vector2(0, -6);

            endStats = UiKit.Text("Stats", dim.rectTransform, "", 40, UiTheme.Text);
            UiKit.Place(endStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1500, 120));

            var btn = UiKit.Button("MainMenu", dim.rectTransform, "Main Menu", UiTheme.Accent, new Vector2(420, 120), GoToMainMenu, 54);
            UiKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(-240, -150), new Vector2(420, 120));
            var unlocks = UiKit.Button("Unlocks", dim.rectTransform, "Unlocks", UiTheme.PanelLight, new Vector2(420, 120), () => MetaScreen.Show(root), 54);
            unlocks.transform.Find("Text").GetComponent<TMP_Text>().color = UiTheme.Text;
            UiKit.Place((RectTransform)unlocks.transform, new Vector2(0.5f, 0.5f), new Vector2(240, -150), new Vector2(420, 120));
            endPanel.SetActive(false);
        }
    }
}
