using System.Collections.Generic;
using Assets.Scrpits.Map;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scrpits.Shop
{
    /// <summary>
    /// Магазин кейсов (CardShopScene). Строит весь UI из кода внутри своего RectTransform (Canvas).
    /// Открытие кейса: Wallet.TrySpend -> ролл карты -> рулетка (CS:GO-стиль) -> reveal -> RunState.AddCard.
    /// Eval: Assets.Scrpits.Shop.CaseShopController.Open("wood"|"iron"|"royal"), .Continue(), .Leave(), .State()
    /// </summary>
    public class CaseShopController : MonoBehaviour
    {
        public static CaseShopController Instance { get; private set; }

        [SerializeField] private float spinDuration = 5.2f;
        [SerializeField] private int stripLength = 46;
        [SerializeField] private int winIndex = 39;
        [SerializeField] private float tileScale = 0.8f;
        [SerializeField] private float tileSpacing = 16f;

        private RectTransform root;
        private TMP_Text coinsText;
        private TMP_Text deckText;
        private RectTransform coinsIcon;
        private readonly List<(CaseDef def, Button btn, Image btnImg, TMP_Text price, RectTransform panel, TMP_Text warn)> caseUis =
            new List<(CaseDef, Button, Image, TMP_Text, RectTransform, TMP_Text)>();

        // roulette / reveal
        private GameObject overlay;
        private CanvasGroup overlayGroup;
        private RectTransform stripViewport;
        private RectTransform strip;
        private RectTransform marker;
        private TMP_Text overlayTitle;
        private GameObject revealRoot;
        private RectTransform revealCardHolder;
        private Image revealRays, revealGlow;
        private TMP_Text revealRarity, revealTitle, revealStats, revealNote;
        private Button continueBtn;

        private readonly List<CardData> wonThisVisit = new List<CardData>();
        private CardData pendingCard;
        public bool IsBusy { get; private set; }
        public string Phase { get; private set; } = "idle";

        private float Step => CardTile.BaseSize.x * tileScale + tileSpacing;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            root = (RectTransform)transform;
            Build();
            RefreshAffordability();
        }

        private void OnEnable() => Wallet.Changed += OnCoinsChanged;
        private void OnDisable() => Wallet.Changed -= OnCoinsChanged;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            DOTween.Kill(this);
        }

        // ---------------- public API (eval) ----------------

        public static bool Open(string caseId)
        {
            if (Instance == null) { Debug.LogWarning("[SHOP] No shop in scene"); return false; }
            var def = CaseDefs.Get(caseId);
            return def != null && Instance.TryOpen(def);
        }

        public static void Continue() { if (Instance != null) Instance.CloseOverlay(); }
        public static void Leave() { if (Instance != null) Instance.LeaveShop(); }

        public static string State() =>
            Instance == null ? "no shop" :
            $"phase={Instance.Phase} busy={Instance.IsBusy} coins={Wallet.Coins} deck={RunState.Deck.Count} won=[{string.Join(",", Instance.wonThisVisit.ConvertAll(c => c != null ? $"{c.Id}:{c.rarity}" : "null"))}]";

        // ---------------- logic ----------------

        private bool TryOpen(CaseDef def)
        {
            if (IsBusy) return false;
            var ui = caseUis.Find(u => u.def == def);
            if (!Wallet.TrySpend(def.price))
            {
                // нет денег - встряхнуть панель и подсветить цену
                if (ui.panel != null)
                {
                    ui.panel.DOKill(true);
                    ui.panel.DOShakeAnchorPos(0.4f, new Vector2(18, 0), 20, 0).SetLink(ui.panel.gameObject);
                    ui.warn.text = $"Need {def.price - Wallet.Coins} more coins";
                    ui.warn.DOKill();
                    ui.warn.alpha = 1f;
                    ui.warn.DOFade(0f, 0.6f).SetDelay(1.4f).SetLink(ui.warn.gameObject);
                }
                Debug.Log($"[SHOP] Not enough coins for {def.id} ({Wallet.Coins}/{def.price})");
                return false;
            }

            var card = def.RollCard();
            if (card == null)
            {
                Wallet.Add(def.price); // вернуть - карт нет вообще
                Debug.LogWarning("[SHOP] No cards to drop, refunded");
                return false;
            }
            // карта выдаётся сразу (даже если сцену закроют во время анимации)
            RunState.AddCard(card);
            wonThisVisit.Add(card);
            pendingCard = card;
            Debug.Log($"[SHOP] Opened {def.id} for {def.price}: {card.Id} ({card.rarity}). Deck={RunState.Deck.Count}");
            StartSpin(def, card);
            return true;
        }

        private void StartSpin(CaseDef def, CardData won)
        {
            IsBusy = true;
            Phase = "spin";
            overlay.SetActive(true);
            revealRoot.SetActive(false);
            stripViewport.gameObject.SetActive(true);
            overlayGroup.alpha = 0f;
            overlayGroup.DOFade(1f, 0.25f).SetTarget(this);
            overlayTitle.text = $"<color=#{UiKit.Hex(def.glow)}>{def.name}</color>";
            overlayTitle.gameObject.SetActive(true);

            for (int i = strip.childCount - 1; i >= 0; i--) Destroy(strip.GetChild(i).gameObject);
            var tiles = new List<RectTransform>();
            for (int i = 0; i < stripLength; i++)
            {
                var c = i == winIndex ? won : def.RollCard();
                var t = CardTile.Build(strip, c, tileScale);
                t.anchorMin = t.anchorMax = new Vector2(0, 0.5f);
                t.anchoredPosition = new Vector2(i * Step + Step * 0.5f, 0);
                tiles.Add(t);
            }

            float start = -(2 * Step + Step * 0.5f);
            float jitter = Random.Range(-0.38f, 0.38f) * CardTile.BaseSize.x * tileScale;
            float target = -(winIndex * Step + Step * 0.5f) + jitter;
            strip.anchoredPosition = new Vector2(start, 0);
            int lastIdx = -1;
            var seq = DOTween.Sequence().SetTarget(this);
            seq.Append(strip.DOAnchorPosX(target, spinDuration).SetEase(Ease.OutQuart).OnUpdate(() =>
            {
                int idx = Mathf.FloorToInt(-strip.anchoredPosition.x / Step);
                if (idx != lastIdx)
                {
                    lastIdx = idx;
                    marker.DOKill(true);
                    marker.DOPunchScale(new Vector3(0.25f, 0.08f, 0), 0.12f, 1, 0).SetLink(marker.gameObject);
                }
            }));
            // досадка точно в центр
            seq.Append(strip.DOAnchorPosX(-(winIndex * Step + Step * 0.5f), 0.35f).SetEase(Ease.InOutSine));
            seq.AppendCallback(() => tiles[winIndex].DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.6f));
            seq.AppendInterval(0.35f);
            seq.OnComplete(() => Reveal(won, tiles[winIndex]));
        }

        private void Reveal(CardData card, RectTransform fromTile)
        {
            Phase = "reveal";
            Color rc = RarityColors.Get(card.rarity);
            revealRoot.SetActive(true);
            stripViewport.gameObject.SetActive(false);
            overlayTitle.gameObject.SetActive(false);

            for (int i = revealCardHolder.childCount - 1; i >= 0; i--) Destroy(revealCardHolder.GetChild(i).gameObject);
            var big = CardTile.Build(revealCardHolder, card, 1f, true);
            big.anchoredPosition = Vector2.zero;
            big.localScale = Vector3.one * tileScale;
            big.DOScale(1.75f, 0.55f).SetEase(Ease.OutBack).SetTarget(this);
            big.localRotation = Quaternion.Euler(0, 0, -8);
            big.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack).SetTarget(this);

            revealRays.color = new Color(rc.r, rc.g, rc.b, 0f);
            revealRays.DOFade(card.rarity >= CardRarity.Epic ? 0.95f : 0.6f, 0.4f).SetTarget(this);
            revealRays.rectTransform.localScale = Vector3.one * 0.3f;
            revealRays.rectTransform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetTarget(this);
            revealRays.rectTransform.DORotate(new Vector3(0, 0, -360), 14f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetTarget(this);

            revealGlow.color = new Color(rc.r, rc.g, rc.b, 0f);
            revealGlow.DOFade(0.9f, 0.15f).SetTarget(this);
            revealGlow.rectTransform.localScale = Vector3.one * 0.4f;
            revealGlow.rectTransform.DOScale(2.2f, 0.7f).SetEase(Ease.OutCubic).SetTarget(this);
            revealGlow.DOFade(0.35f, 0.8f).SetDelay(0.3f).SetTarget(this);

            string exclaim = card.rarity == CardRarity.Legendary ? "LEGENDARY!" : card.rarity == CardRarity.Epic ? "EPIC!" : RarityColors.Name(card.rarity).ToUpper();
            revealRarity.text = exclaim;
            revealRarity.color = rc;
            revealRarity.transform.localScale = Vector3.zero;
            revealRarity.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.25f).SetTarget(this);

            revealTitle.text = card.Title;
            revealStats.text = $"<color=#{UiKit.Hex(UiTheme.Mana)}>Cost {card.Cost}</color>    <color=#{UiKit.Hex(UiTheme.Damage)}>Dmg {card.Damage}</color>    <color=#{UiKit.Hex(UiTheme.Heal)}>HP {card.HP}</color>";
            revealNote.text = $"Added to your deck  ({RunState.Deck.Count} cards)";
            foreach (var t in new[] { revealTitle, revealStats, revealNote })
            {
                t.alpha = 0f;
                t.DOFade(1f, 0.3f).SetDelay(0.45f).SetTarget(this);
            }
            continueBtn.transform.localScale = Vector3.zero;
            continueBtn.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.6f).SetTarget(this)
                .OnComplete(() => Phase = "revealed");
            RefreshDeck();
        }

        private void CloseOverlay()
        {
            if (!overlay.activeSelf) return;
            DOTween.Kill(this);
            pendingCard = null;
            overlay.SetActive(false);
            IsBusy = false;
            Phase = "idle";
            RefreshAffordability();
        }

        private void LeaveShop()
        {
            DOTween.Kill(this);
            if (RunState.IsActive)
            {
                if (RunState.CurrentNodeType == MapNodeType.Shop && !RunState.IsCompleted(RunState.CurrentNodeId))
                    RunState.CompleteCurrentNode();
                if (wonThisVisit.Count > 0)
                    RunState.QueueMapToast($"Shop: +{wonThisVisit.Count} card{(wonThisVisit.Count > 1 ? "s" : "")} added to your deck", wonThisVisit[wonThisVisit.Count - 1]);
                else
                    RunState.QueueMapToast("You leave the merchant empty-handed");
                RunState.LoadMap();
            }
            else
            {
                SceneManager.LoadScene(RunState.MainMenuSceneName);
            }
        }

        private void OnCoinsChanged(int coins)
        {
            if (coinsText == null) return;
            coinsText.text = coins.ToString();
            coinsIcon.DOKill(true);
            coinsIcon.DOPunchScale(Vector3.one * 0.3f, 0.3f, 6, 0.5f).SetLink(coinsIcon.gameObject);
            RefreshAffordability();
        }

        private void RefreshAffordability()
        {
            if (coinsText != null) coinsText.text = Wallet.Coins.ToString();
            foreach (var u in caseUis)
            {
                bool can = Wallet.Coins >= u.def.price;
                u.btnImg.color = can ? UiTheme.Accent : new Color(0.38f, 0.35f, 0.42f, 1f);
                u.price.color = can ? UiTheme.Background : UiTheme.Damage;
                var label = u.btn.transform.Find("Text").GetComponent<TMP_Text>();
                label.color = can ? UiTheme.Background : UiTheme.TextDim;
            }
            RefreshDeck();
        }

        private void RefreshDeck()
        {
            if (deckText != null) deckText.text = $"Deck: {RunState.Deck.Count} cards";
        }

        // ---------------- UI ----------------

        private void Build()
        {
            for (int i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);

            var bg = UiKit.Img("Background", root, UiTheme.Background);
            bg.raycastTarget = true;
            UiKit.Stretch(bg.rectTransform);
            var topGlow = UiKit.Img("TopGlow", root, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.12f), UiKit.Glow);
            UiKit.Place(topGlow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1900, 900));
            var floor = UiKit.Img("Floor", root, new Color(0, 0, 0, 0.45f), UiKit.VGradient);
            floor.rectTransform.localScale = new Vector3(1, -1, 1);
            UiKit.Place(floor.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 160), new Vector2(4000, 320));

            var title = UiKit.Text("Title", root, "CASE SHOP", 96, UiTheme.Accent);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -90), new Vector2(1000, 120));
            var tsh = title.gameObject.AddComponent<Shadow>();
            tsh.effectColor = new Color(0, 0, 0, 0.6f); tsh.effectDistance = new Vector2(0, -5);
            var sub = UiKit.Text("Subtitle", root, "Open a case - win a card for your deck", 40, UiTheme.TextDim);
            UiKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1200, 60));

            // coins (top-right)
            var coinBox = UiKit.Img("Coins", root, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(coinBox.rectTransform, new Vector2(1, 1), new Vector2(-190, -70), new Vector2(280, 90));
            var coin = UiKit.Img("CoinIcon", coinBox.rectTransform, UiTheme.Accent, UiKit.Circle);
            UiKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(56, 56));
            var coinIn = UiKit.Img("In", coin.rectTransform, new Color(0.85f, 0.55f, 0.1f, 1f), UiKit.Ring);
            UiKit.Stretch(coinIn.rectTransform, 8, 8, 8, 8);
            coinsIcon = coin.rectTransform;
            coinsText = UiKit.Text("Value", coinBox.rectTransform, "0", 54, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Stretch(coinsText.rectTransform, 92, 10, 0, 0);

            deckText = UiKit.Text("Deck", root, "", 40, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Place(deckText.rectTransform, new Vector2(0, 1), new Vector2(230, -70), new Vector2(400, 70));

            // cases
            float w = 470f, gap = 60f;
            for (int i = 0; i < CaseDefs.All.Length; i++)
            {
                float x = (i - (CaseDefs.All.Length - 1) * 0.5f) * (w + gap);
                BuildCase(CaseDefs.All[i], new Vector2(x, -10), new Vector2(w, 640), i);
            }

            var leave = UiKit.Button("Leave", root, "Leave shop", UiTheme.PanelLight, new Vector2(380, 100), LeaveShop, 46);
            leave.transform.Find("Text").GetComponent<TMP_Text>().color = UiTheme.Text;
            UiKit.Place((RectTransform)leave.transform, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(380, 100));

            BuildOverlay();
        }

        private void BuildCase(CaseDef def, Vector2 pos, Vector2 size, int index)
        {
            var panel = UiKit.Img("Case_" + def.id, root, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), pos, size);
            var psh = panel.gameObject.AddComponent<Shadow>();
            psh.effectColor = new Color(0, 0, 0, 0.5f); psh.effectDistance = new Vector2(0, -8);
            var frame = UiKit.Img("Frame", panel.rectTransform, new Color(def.glow.r, def.glow.g, def.glow.b, 0.55f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frame.rectTransform);
            var prt = panel.rectTransform;

            var glow = UiKit.Img("Glow", prt, new Color(def.glow.r, def.glow.g, def.glow.b, 0.45f), UiKit.Glow);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -175), new Vector2(430, 360));
            glow.DOFade(0.2f, 1.6f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(index * 0.4f).SetLink(glow.gameObject);

            var shadow = UiKit.Img("ChestShadow", prt, new Color(0, 0, 0, 0.5f), UiKit.Glow);
            UiKit.Place(shadow.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -290), new Vector2(300, 50));

            var chest = UiKit.Img("Chest", prt, Color.white, UiKit.Chest(def.id, def.body, def.bands));
            UiKit.Place(chest.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -175), new Vector2(270, 216));
            chest.rectTransform.DOAnchorPosY(-160f, 1.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetDelay(index * 0.3f).SetLink(chest.gameObject);

            var name = UiKit.Text("Name", prt, def.name, 56, UiTheme.Text);
            UiKit.Place(name.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -345), new Vector2(440, 70));

            var odds = UiKit.Text("Odds", prt, CaseDefs.OddsText(def), 32, UiTheme.TextDim);
            odds.lineSpacing = -8;
            UiKit.Place(odds.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -455), new Vector2(420, 150));

            var btn = UiKit.Button("Open", prt, "OPEN", UiTheme.Accent, new Vector2(360, 96), () => TryOpen(def), 48);
            UiKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 0f), new Vector2(0, 72), new Vector2(360, 96));
            var label = btn.transform.Find("Text").GetComponent<TMP_Text>();
            UiKit.Stretch(label.rectTransform, 24, 170, 0, 0);
            label.alignment = TextAlignmentOptions.Left;
            var coin = UiKit.Img("Coin", btn.transform, new Color(1f, 0.9f, 0.4f), UiKit.Circle);
            UiKit.Place(coin.rectTransform, new Vector2(1, 0.5f), new Vector2(-135, 0), new Vector2(40, 40));
            var coinRing = UiKit.Img("Ring", coin.rectTransform, new Color(0.75f, 0.45f, 0.05f), UiKit.Ring);
            UiKit.Stretch(coinRing.rectTransform, 5, 5, 5, 5);
            var price = UiKit.Text("Price", btn.transform, def.price.ToString(), 48, UiTheme.Background, TextAlignmentOptions.Left);
            UiKit.Place(price.rectTransform, new Vector2(1, 0.5f), new Vector2(-55, 0), new Vector2(110, 90));

            var warn = UiKit.Text("Warn", prt, "", 30, UiTheme.Damage);
            UiKit.Place(warn.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(440, 40));
            warn.alpha = 0f;

            caseUis.Add((def, btn, (Image)btn.targetGraphic, price, prt, warn));
        }

        private void BuildOverlay()
        {
            var dim = UiKit.Img("Overlay", root, new Color(0.03f, 0.02f, 0.05f, 0.92f));
            dim.raycastTarget = true;
            UiKit.Stretch(dim.rectTransform);
            overlay = dim.gameObject;
            overlayGroup = overlay.AddComponent<CanvasGroup>();
            var ort = dim.rectTransform;

            overlayTitle = UiKit.Text("CaseName", ort, "", 72, UiTheme.Text);
            UiKit.Place(overlayTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 280), new Vector2(1200, 100));

            // strip
            var vp = UiKit.Img("StripViewport", ort, new Color(0, 0, 0, 0.55f), UiKit.RoundedRect, true);
            UiKit.Place(vp.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1720, 300));
            vp.gameObject.AddComponent<RectMask2D>();
            stripViewport = vp.rectTransform;
            strip = UiKit.Rect("Strip", stripViewport);
            strip.anchorMin = strip.anchorMax = new Vector2(0.5f, 0.5f);
            strip.pivot = new Vector2(0f, 0.5f);
            strip.sizeDelta = new Vector2(10, 280);
            // затемнение краёв
            for (int s = 0; s < 2; s++)
            {
                var fade = UiKit.Img(s == 0 ? "FadeL" : "FadeR", stripViewport, new Color(0.03f, 0.02f, 0.05f, 1f), UiKit.VGradient);
                var frt = fade.rectTransform;
                frt.pivot = new Vector2(0.5f, 0.5f);
                frt.anchorMin = frt.anchorMax = new Vector2(s == 0 ? 0f : 1f, 0.5f);
                frt.sizeDelta = new Vector2(300, 260); // поворот: высота градиента -> ширина
                frt.localRotation = Quaternion.Euler(0, 0, s == 0 ? 90 : -90);
                frt.anchoredPosition = new Vector2(s == 0 ? 130 : -130, 0);
                frt.sizeDelta = new Vector2(300, 260);
            }
            // центр-маркер
            marker = UiKit.Rect("Marker", stripViewport);
            UiKit.Place(marker, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8, 300));
            var line = UiKit.Img("Line", marker, UiTheme.Accent);
            UiKit.Stretch(line.rectTransform);
            var lglow = UiKit.Img("LineGlow", marker, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.35f), UiKit.Glow);
            UiKit.Place(lglow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 340));
            var frameV = UiKit.Img("Frame", stripViewport, new Color(UiTheme.Accent.r, UiTheme.Accent.g, UiTheme.Accent.b, 0.5f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frameV.rectTransform, -6, -6, -6, -6);
            // указатели сверху/снизу (ромбы на краю ленты)
            for (int s = 0; s < 2; s++)
            {
                var tri = UiKit.Img(s == 0 ? "PointerTop" : "PointerBottom", ort, UiTheme.Accent, UiKit.RoundedRect);
                UiKit.Place(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, s == 0 ? 146 : -166), new Vector2(46, 46));
                tri.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var tsh = tri.gameObject.AddComponent<Shadow>();
                tsh.effectColor = new Color(0, 0, 0, 0.6f); tsh.effectDistance = new Vector2(0, -4);
            }

            // reveal
            revealRoot = UiKit.Rect("Reveal", ort).gameObject;
            var rrt = (RectTransform)revealRoot.transform;
            UiKit.Stretch(rrt);
            revealRays = UiKit.Img("Rays", rrt, Color.white, UiKit.Rays);
            UiKit.Place(revealRays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1100, 1100));
            revealGlow = UiKit.Img("Burst", rrt, Color.white, UiKit.Glow);
            UiKit.Place(revealGlow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(520, 520));
            revealCardHolder = UiKit.Rect("CardHolder", rrt);
            UiKit.Place(revealCardHolder, new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(10, 10));
            revealRarity = UiKit.Text("Rarity", rrt, "", 84, Color.white);
            UiKit.Place(revealRarity.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 430), new Vector2(1000, 110));
            var rsh = revealRarity.gameObject.AddComponent<Shadow>();
            rsh.effectColor = new Color(0, 0, 0, 0.7f); rsh.effectDistance = new Vector2(0, -5);
            revealTitle = UiKit.Text("Title", rrt, "", 60, UiTheme.Text);
            UiKit.Place(revealTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(1000, 80));
            revealStats = UiKit.Text("Stats", rrt, "", 40, UiTheme.Text);
            UiKit.Place(revealStats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -235), new Vector2(1000, 60));
            revealNote = UiKit.Text("Note", rrt, "", 34, UiTheme.TextDim);
            UiKit.Place(revealNote.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -290), new Vector2(1000, 50));
            continueBtn = UiKit.Button("Continue", rrt, "Continue", UiTheme.Accent, new Vector2(360, 100), CloseOverlay, 48);
            UiKit.Place((RectTransform)continueBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -390), new Vector2(360, 100));

            overlay.SetActive(false);
        }
    }
}
