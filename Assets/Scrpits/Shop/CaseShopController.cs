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
    /// Магазин (CardShopScene). Строит весь UI из кода внутри своего RectTransform (Canvas).
    /// Вкладки: CARD CASES (3 кейса карт), ITEM CASES (3 кейса предметов), MERCHANT (прямая покупка предметов).
    /// Открытие кейса: Wallet.TrySpend -> ролл (карта/предмет) -> рулетка (CS:GO-стиль) -> reveal -> колода / инвентарь.
    /// Цвет рамки/свечения/подписи - по редкости (RarityColors).
    /// Eval: Assets.Scrpits.Shop.CaseShopController.Open("wood"|"iron"|"royal"|"pouch"|"satchel"|"relic_chest"),
    ///       .Buy(0..5), .Tab(0..2), .Continue(), .Leave(), .State()
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

        // tabs
        private static readonly string[] TabNames = { "CARD CASES", "ITEM CASES", "MERCHANT" };
        private static readonly string[] TabSubtitles =
        {
            "Open a case - win a card for your deck",
            "Open a case - win a usable item or a passive relic",
            "Buy items directly. Usable items go to your battle bag",
        };
        private readonly List<(Image face, TMP_Text label)> tabUis = new List<(Image, TMP_Text)>();
        private readonly RectTransform[] tabRoots = new RectTransform[3];
        private int currentTab;
        private TMP_Text subtitleText;

        // merchant
        private readonly List<StockSlot> stock = new List<StockSlot>();

        private class StockSlot
        {
            public ItemDef item;
            public bool sold;
            public RectTransform tile;
            public Button btn;
            public Image btnImg;
            public TMP_Text label;
            public TMP_Text price;
            public TMP_Text warn;
        }

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

        private readonly List<CaseDrop> wonThisVisit = new List<CaseDrop>();
        private readonly List<ItemDef> boughtThisVisit = new List<ItemDef>();
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

        public static bool Buy(int slot) => Instance != null && Instance.TryBuy(slot);
        public static void Tab(int index) { if (Instance != null) Instance.ShowTab(index); }
        public static void Continue() { if (Instance != null) Instance.CloseOverlay(); }
        public static void Leave() { if (Instance != null) Instance.LeaveShop(); }

        public static string State() =>
            Instance == null ? "no shop" :
            $"phase={Instance.Phase} tab={Instance.currentTab} busy={Instance.IsBusy} coins={Wallet.Coins} deck={RunState.Deck.Count} items={RunState.TotalItems} " +
            $"won=[{string.Join(",", Instance.wonThisVisit.ConvertAll(d => $"{d.Id}:{d.Rarity}"))}] " +
            $"stock=[{string.Join(",", Instance.stock.ConvertAll(st => $"{st.item.id}{(st.sold ? "(sold)" : "")}"))}]";

        // ---------------- logic ----------------

        private bool TryOpen(CaseDef def)
        {
            if (IsBusy) return false;
            var ui = caseUis.Find(u => u.def == def);
            int price = def.Price;
            if (!Wallet.TrySpend(price))
            {
                // нет денег - встряхнуть панель и подсветить цену
                if (ui.panel != null)
                {
                    ui.panel.DOKill(true);
                    ui.panel.DOShakeAnchorPos(0.4f, new Vector2(18, 0), 20, 0).SetLink(ui.panel.gameObject);
                    ui.warn.text = $"Need {price - Wallet.Coins} more coins";
                    ui.warn.DOKill();
                    ui.warn.alpha = 1f;
                    ui.warn.DOFade(0f, 0.6f).SetDelay(1.4f).SetLink(ui.warn.gameObject);
                }
                Debug.Log($"[SHOP] Not enough coins for {def.id} ({Wallet.Coins}/{price})");
                return false;
            }

            var drop = def.Roll();
            if (drop.IsEmpty)
            {
                Wallet.Add(price); // вернуть - дропать нечего
                Debug.LogWarning("[SHOP] Nothing to drop, refunded");
                return false;
            }
            // награда выдаётся сразу (даже если сцену закроют во время анимации)
            if (drop.card != null) RunState.AddCard(drop.card);
            else ItemDatabase.Grant(drop.item);
            wonThisVisit.Add(drop);
            Debug.Log($"[SHOP] Opened {def.id} for {price}: {drop.Id} ({drop.Rarity}). Deck={RunState.Deck.Count} Items={RunState.TotalItems}");
            StartSpin(def, drop);
            return true;
        }

        private void StartSpin(CaseDef def, CaseDrop won)
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
            string prev = null, prev2 = null;
            for (int i = 0; i < stripLength; i++)
            {
                // рядом с выигрышем иногда самая редкая плитка кейса - «чуть-чуть не хватило»
                bool tease = (i == winIndex + 1 || i == winIndex - 1) && Random.value < 0.45f;
                var c = i == winIndex ? won : Filler(def, prev, prev2, tease);
                prev2 = prev;
                prev = c.Id;
                var t = c.BuildTile(strip, tileScale);
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
                    SoundFx.Play(SoundFx.Clip.Tick, 0.8f, 0.03f); // щелчок на каждой карте ленты
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

        /// <summary>Плитка-«пустышка» для ленты: без побочных эффектов (пассивки не выдаются).</summary>
        /// <summary>
        /// Плитка-«пустышка» для ленты (без побочных эффектов): случайная карта/предмет из всего пула кейса,
        /// редкость по шансам кейса, но редкие мелькают чаще (для азарта) и без повторов с двумя предыдущими.
        /// </summary>
        private static CaseDrop Filler(CaseDef def, string avoidA = null, string avoidB = null, bool tease = false)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var r = tease ? def.odds[def.odds.Length - 1].rarity : def.RollRarity();
                // визуально поднять редкость в ~25% плиток
                if (!tease && Random.value < 0.25f && (int)r < (int)CardRarity.Legendary) r = (CardRarity)((int)r + 1);
                CaseDrop drop;
                if (def.items)
                {
                    var pool = new List<ItemDef>();
                    foreach (var i in ItemDatabase.All) if (i.rarity == r) pool.Add(i);
                    if (pool.Count == 0) foreach (var i in ItemDatabase.All) pool.Add(i);
                    drop = new CaseDrop(pool[Random.Range(0, pool.Count)]);
                }
                else
                {
                    var pool = new List<CardData>();
                    foreach (var c in CardDatabase.PlayerCards)
                        if (c.inShopPool && MetaProgress.IsCardUnlocked(c.Id) && c.rarity == r) pool.Add(c);
                    if (pool.Count == 0)
                        foreach (var c in CardDatabase.PlayerCards)
                            if (c.inShopPool && MetaProgress.IsCardUnlocked(c.Id)) pool.Add(c);
                    drop = pool.Count > 0 ? new CaseDrop(pool[Random.Range(0, pool.Count)]) : new CaseDrop(def.RollCard());
                }
                if (drop.Id != avoidA && drop.Id != avoidB) return drop;
            }
            return def.items ? new CaseDrop(ItemDatabase.Roll(def.RollRarity())) : new CaseDrop(def.RollCard());
        }

        private void Reveal(CaseDrop drop, RectTransform fromTile)
        {
            Phase = "reveal";
            var rarity = drop.Rarity;
            Color rc = RarityColors.Get(rarity);
            revealRoot.SetActive(true);
            stripViewport.gameObject.SetActive(false);
            overlayTitle.gameObject.SetActive(false);

            for (int i = revealCardHolder.childCount - 1; i >= 0; i--) Destroy(revealCardHolder.GetChild(i).gameObject);
            var big = drop.BuildTile(revealCardHolder, 1f, true);
            big.anchoredPosition = Vector2.zero;
            big.localScale = Vector3.one * tileScale;
            big.DOScale(1.75f, 0.55f).SetEase(Ease.OutBack).SetTarget(this);
            big.localRotation = Quaternion.Euler(0, 0, -8);
            big.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutBack).SetTarget(this);

            revealRays.color = new Color(rc.r, rc.g, rc.b, 0f);
            revealRays.DOFade(rarity >= CardRarity.Epic ? 0.95f : 0.6f, 0.4f).SetTarget(this);
            revealRays.rectTransform.localScale = Vector3.one * 0.3f;
            revealRays.rectTransform.DOScale(1f, 0.6f).SetEase(Ease.OutBack).SetTarget(this);
            revealRays.rectTransform.DORotate(new Vector3(0, 0, -360), 14f, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1).SetTarget(this);

            revealGlow.color = new Color(rc.r, rc.g, rc.b, 0f);
            revealGlow.DOFade(0.9f, 0.15f).SetTarget(this);
            revealGlow.rectTransform.localScale = Vector3.one * 0.4f;
            revealGlow.rectTransform.DOScale(2.2f, 0.7f).SetEase(Ease.OutCubic).SetTarget(this);
            revealGlow.DOFade(0.35f, 0.8f).SetDelay(0.3f).SetTarget(this);

            string exclaim = rarity == CardRarity.Legendary ? "LEGENDARY!" : rarity == CardRarity.Epic ? "EPIC!" : RarityColors.Name(rarity).ToUpper();
            revealRarity.text = exclaim;
            revealRarity.color = rc;
            revealRarity.transform.localScale = Vector3.zero;
            revealRarity.transform.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.25f).SetTarget(this);

            revealTitle.text = drop.Title;
            if (rarity >= CardRarity.Epic) { SoundFx.Play(SoundFx.Clip.RevealEpic); SoundFx.Vibrate(); }
            else SoundFx.Play(SoundFx.Clip.Reveal);
            if (drop.card != null)
            {
                var card = drop.card;
                revealStats.text = $"<color=#{UiKit.Hex(UiTheme.Mana)}>Cost {card.Cost}</color>    <color=#{UiKit.Hex(UiTheme.Damage)}>Dmg {card.Damage}</color>    <color=#{UiKit.Hex(UiTheme.Heal)}>HP {card.HP}</color>";
                revealNote.text = $"Added to your deck  ({RunState.Deck.Count} cards)";
            }
            else
            {
                var item = drop.item;
                revealStats.text = item.description;
                revealNote.text = item.IsPassive
                    ? "Passive unlocked - works automatically"
                    : $"Added to your bag  (x{RunState.ItemCount(item.id)}) - use it in battle";
            }
            foreach (var t in new[] { revealTitle, revealStats, revealNote })
            {
                t.alpha = 0f;
                t.DOFade(1f, 0.3f).SetDelay(0.45f).SetTarget(this);
            }
            continueBtn.transform.localScale = Vector3.zero;
            continueBtn.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetDelay(0.6f).SetTarget(this)
                .OnComplete(() => Phase = "revealed");
            if (rarity >= CardRarity.Epic) Camera.main?.DOShakePosition(0.35f, 0.15f, 12).SetTarget(this);
            RefreshDeck();
        }

        private void CloseOverlay()
        {
            if (!overlay.activeSelf) return;
            DOTween.Kill(this);
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
                int cards = wonThisVisit.FindAll(d => d.card != null).Count;
                int items = wonThisVisit.Count - cards + boughtThisVisit.Count;
                var lastCard = wonThisVisit.FindLast(d => d.card != null).card;
                if (cards + items > 0)
                {
                    var parts = new List<string>();
                    if (cards > 0) parts.Add($"+{cards} card{(cards > 1 ? "s" : "")}");
                    if (items > 0) parts.Add($"+{items} item{(items > 1 ? "s" : "")}");
                    RunState.QueueMapToast("Shop: " + string.Join(", ", parts), lastCard);
                }
                else
                {
                    RunState.QueueMapToast("You leave the merchant empty-handed");
                }
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
                int price = u.def.Price;
                bool can = Wallet.Coins >= price;
                u.btnImg.color = can ? UiTheme.Accent : new Color(0.38f, 0.35f, 0.42f, 1f);
                u.price.text = price.ToString();
                u.price.color = can ? UiTheme.Background : UiTheme.Damage;
                var label = u.btn.transform.Find("Text").GetComponent<TMP_Text>();
                label.color = can ? UiTheme.Background : UiTheme.TextDim;
            }
            foreach (var st in stock)
            {
                bool owned = st.item.IsPassive && RunState.Relics.Contains(st.item.id);
                bool closed = st.sold || owned;
                int price = RelicSystem.ShopPrice(st.item.price);
                bool can = !closed && Wallet.Coins >= price;
                st.btn.interactable = !closed;
                st.btnImg.color = closed ? new Color(0.25f, 0.23f, 0.3f, 1f) : can ? UiTheme.Accent : new Color(0.38f, 0.35f, 0.42f, 1f);
                st.label.text = st.sold ? "SOLD" : owned ? "OWNED" : "BUY";
                st.label.color = can ? UiTheme.Background : UiTheme.TextDim;
                st.price.gameObject.SetActive(!closed);
                st.price.transform.parent.Find("Coin")?.gameObject.SetActive(!closed);
                st.price.text = price.ToString();
                st.price.color = can ? UiTheme.Background : UiTheme.Damage;
                var cg = st.tile.GetComponent<CanvasGroup>();
                if (cg == null) cg = st.tile.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = closed ? 0.45f : 1f;
            }
            if (promoteBtn != null)
            {
                int pp = RelicSystem.ShopPrice(CardUpgrade.ShopPrice);
                bool canP = Wallet.Coins >= pp && CardUpgrade.AnyUpgradable();
                ((Image)promoteBtn.targetGraphic).color = canP ? CardUpgrade.Gold : new Color(0.38f, 0.35f, 0.42f, 1f);
                promotePrice.text = pp.ToString();
                promotePrice.color = canP ? UiTheme.Background : UiTheme.Damage;
            }
            RefreshDeck();
        }

        private void RefreshDeck()
        {
            if (deckText != null) deckText.text = $"Deck: {RunState.Deck.Count}   Items: {RunState.TotalItems}   Passives: {RunState.Relics.Count}";
        }

        // ---------------- tabs ----------------

        private void ShowTab(int index)
        {
            if (IsBusy || index < 0 || index >= tabRoots.Length) return;
            bool changed = index != currentTab;
            currentTab = index;
            for (int i = 0; i < tabRoots.Length; i++)
            {
                if (tabRoots[i] != null) tabRoots[i].gameObject.SetActive(i == index);
                if (i < tabUis.Count)
                {
                    tabUis[i].face.color = i == index ? UiTheme.Accent : UiTheme.PanelLight;
                    tabUis[i].label.color = i == index ? UiTheme.Background : UiTheme.TextDim;
                }
            }
            if (subtitleText != null) subtitleText.text = TabSubtitles[index];
            if (changed && tabRoots[index] != null)
            {
                var cg = tabRoots[index].GetComponent<CanvasGroup>();
                if (cg == null) cg = tabRoots[index].gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
                cg.DOFade(1f, 0.25f).SetLink(cg.gameObject);
                tabRoots[index].anchoredPosition = new Vector2(0, -30);
                tabRoots[index].DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutCubic).SetLink(cg.gameObject);
            }
        }

        // ---------------- merchant ----------------

        /// <summary>Ассортимент на визит: 3 разных расходника + до 3 ещё не собранных пассивок.</summary>
        private void RollStock()
        {
            stock.Clear();
            var cons = new List<ItemDef>(ItemDatabase.Consumables);
            for (int i = 0; i < 3 && cons.Count > 0; i++)
            {
                var pick = ItemDatabase.RollConsumable(CardRarity.Legendary);
                if (!cons.Contains(pick)) pick = cons[Random.Range(0, cons.Count)];
                cons.Remove(pick);
                stock.Add(new StockSlot { item = pick });
            }
            var pas = ItemDatabase.Passives.FindAll(p => !RunState.Relics.Contains(p.id));
            for (int i = 0; i < 3 && pas.Count > 0; i++)
            {
                var pick = pas[Random.Range(0, pas.Count)];
                pas.Remove(pick);
                stock.Add(new StockSlot { item = pick });
            }
            // пассивки кончились - добить расходниками
            while (stock.Count < 6 && cons.Count > 0)
            {
                var pick = cons[Random.Range(0, cons.Count)];
                cons.Remove(pick);
                stock.Add(new StockSlot { item = pick });
            }
        }

        private bool TryBuy(int index)
        {
            if (IsBusy || index < 0 || index >= stock.Count) return false;
            var st = stock[index];
            bool owned = st.item.IsPassive && RunState.Relics.Contains(st.item.id);
            if (st.sold || owned) return false;
            int price = RelicSystem.ShopPrice(st.item.price);
            if (!Wallet.TrySpend(price))
            {
                st.tile.DOKill(true);
                st.tile.DOShakeAnchorPos(0.4f, new Vector2(14, 0), 20, 0).SetLink(st.tile.gameObject);
                st.warn.text = $"Need {price - Wallet.Coins} more";
                st.warn.DOKill();
                st.warn.alpha = 1f;
                st.warn.DOFade(0f, 0.6f).SetDelay(1.2f).SetLink(st.warn.gameObject);
                return false;
            }
            ItemDatabase.Grant(st.item);
            SoundFx.Play(SoundFx.Clip.Coin);
            st.sold = true;
            boughtThisVisit.Add(st.item);
            Debug.Log($"[SHOP] Bought {st.item.id} for {price}. Items={RunState.TotalItems} Relics={RunState.Relics.Count}");

            // «покупка»: вспышка редкости + подпрыгивание плитки
            var rc = RarityColors.Get(st.item.rarity);
            var flash = UiKit.Img("BuyFlash", st.tile, new Color(rc.r, rc.g, rc.b, 0.8f), UiKit.Glow);
            UiKit.Place(flash.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, CardTile.BaseSize * 1.6f);
            flash.transform.SetAsFirstSibling();
            flash.DOFade(0f, 0.7f).SetLink(flash.gameObject).OnComplete(() => Destroy(flash.gameObject));
            st.tile.DOKill(true);
            st.tile.DOPunchScale(Vector3.one * 0.12f, 0.4f, 6, 0.6f).SetLink(st.tile.gameObject);
            RefreshAffordability();
            return true;
        }

        private Button promoteBtn;
        private TMP_Text promotePrice;

        /// <summary>Прокачка карты за монеты: выбор карты -> оплата -> Senior-версия.</summary>
        private void Promote()
        {
            if (IsBusy) return;
            int price = RelicSystem.ShopPrice(CardUpgrade.ShopPrice);
            if (Wallet.Coins < price || !CardUpgrade.AnyUpgradable())
            {
                var prt = (RectTransform)promoteBtn.transform;
                prt.DOKill(true);
                prt.DOShakeAnchorPos(0.4f, new Vector2(14, 0), 20, 0).SetLink(prt.gameObject);
                return;
            }
            DeckPicker.Show(root, "Promote a card", $"Pay {price} coins: +1 ATK, +1 HP, golden frame", CardUpgrade.CanUpgrade, idx =>
            {
                if (!Wallet.TrySpend(price)) return;
                var card = CardUpgrade.UpgradeDeckCard(idx);
                if (card == null) { Wallet.Add(price); return; }
                RunState.QueueMapToast($"{card.Title} promoted!", card);
                RefreshAffordability();
            }, null);
        }

        private void BuildMerchant(RectTransform parent)
        {
            RollStock();
            promoteBtn = UiKit.Button("Promote", parent, "PROMOTE A CARD", UiTheme.Accent, new Vector2(560, 84), Promote, 36);
            UiKit.Place((RectTransform)promoteBtn.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -335), new Vector2(560, 84));
            var plabel = promoteBtn.transform.Find("Text").GetComponent<TMP_Text>();
            UiKit.Stretch(plabel.rectTransform, 24, 130, 0, 12);
            plabel.alignment = TextAlignmentOptions.Left;
            var pcoin = UiKit.Coin("Coin", promoteBtn.transform);
            UiKit.Place(pcoin.rectTransform, new Vector2(1, 0.5f), new Vector2(-100, 4), new Vector2(34, 34));
            promotePrice = UiKit.Text("Price", promoteBtn.transform, "", 36, UiTheme.Background, TextAlignmentOptions.Left);
            UiKit.Place(promotePrice.rectTransform, new Vector2(1, 0.5f), new Vector2(-44, 4), new Vector2(80, 70));
            float scale = 1.05f;
            float w = CardTile.BaseSize.x * scale, gap = 42f;
            for (int i = 0; i < stock.Count; i++)
            {
                var st = stock[i];
                float x = (i - (stock.Count - 1) * 0.5f) * (w + gap);
                var holder = UiKit.Rect("Stock_" + i, parent);
                UiKit.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(x, 20), CardTile.BaseSize * scale);
                st.tile = ItemTile.Build(holder, st.item, scale);
                st.tile.anchorMin = st.tile.anchorMax = new Vector2(0.5f, 0.5f);
                st.tile.anchoredPosition = Vector2.zero;

                int idx = i;
                var btn = UiKit.Button("Buy", parent, "BUY", UiTheme.Accent, new Vector2(w, 80), () => TryBuy(idx), 36);
                UiKit.Place((RectTransform)btn.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -200), new Vector2(w, 80));
                var label = btn.transform.Find("Text").GetComponent<TMP_Text>();
                UiKit.Stretch(label.rectTransform, 16, 100, 0, 0);
                label.alignment = TextAlignmentOptions.Left;
                var coin = UiKit.Coin("Coin", btn.transform);
                UiKit.Place(coin.rectTransform, new Vector2(1, 0.5f), new Vector2(-88, 0), new Vector2(32, 32));
                if (!UiKit.HasCoinArt) { var ring = UiKit.Img("Ring", coin.rectTransform, new Color(0.75f, 0.45f, 0.05f), UiKit.Ring); UiKit.Stretch(ring.rectTransform, 4, 4, 4, 4); }
                var price = UiKit.Text("Price", btn.transform, "", 36, UiTheme.Background, TextAlignmentOptions.Left);
                UiKit.Place(price.rectTransform, new Vector2(1, 0.5f), new Vector2(-38, 0), new Vector2(76, 70));
                var warn = UiKit.Text("Warn", parent, "", 26, UiTheme.Damage);
                UiKit.Place(warn.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -265), new Vector2(w + 30, 40));
                warn.alpha = 0f;

                st.btn = btn;
                st.btnImg = (Image)btn.targetGraphic;
                st.label = label;
                st.price = price;
                st.warn = warn;
            }
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
            var sub = UiKit.Text("Subtitle", root, TabSubtitles[0], 40, UiTheme.TextDim);
            UiKit.Place(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -170), new Vector2(1400, 60));
            subtitleText = sub;

            // coins (top-right)
            var coinBox = UiKit.Img("Coins", root, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(coinBox.rectTransform, new Vector2(1, 1), new Vector2(-190, -70), new Vector2(280, 90));
            var coin = UiKit.Coin("CoinIcon", coinBox.rectTransform);
            UiKit.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(56, 56));
            if (!UiKit.HasCoinArt) { var coinIn = UiKit.Img("In", coin.rectTransform, new Color(0.85f, 0.55f, 0.1f, 1f), UiKit.Ring); UiKit.Stretch(coinIn.rectTransform, 8, 8, 8, 8); }
            coinsIcon = coin.rectTransform;
            coinsText = UiKit.Text("Value", coinBox.rectTransform, "0", 54, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Stretch(coinsText.rectTransform, 92, 10, 0, 0);

            deckText = UiKit.Text("Deck", root, "", 32, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Place(deckText.rectTransform, new Vector2(0, 1), new Vector2(330, -70), new Vector2(600, 70));

            // tabs
            float tabW = 330f;
            for (int i = 0; i < TabNames.Length; i++)
            {
                int idx = i;
                var tb = UiKit.Button("Tab_" + i, root, TabNames[i], UiTheme.PanelLight, new Vector2(tabW, 72), () => ShowTab(idx), 34);
                UiKit.Place((RectTransform)tb.transform, new Vector2(0.5f, 1f), new Vector2((i - 1) * (tabW + 24f), -245), new Vector2(tabW, 72));
                tabUis.Add(((Image)tb.targetGraphic, tb.transform.Find("Text").GetComponent<TMP_Text>()));
                tabRoots[i] = UiKit.Rect("TabRoot_" + i, root);
                UiKit.Stretch(tabRoots[i]);
            }

            // cases
            float w = 470f, gap = 60f;
            for (int i = 0; i < CaseDefs.All.Length; i++)
            {
                float x = (i - (CaseDefs.All.Length - 1) * 0.5f) * (w + gap);
                BuildCase(CaseDefs.All[i], tabRoots[0], new Vector2(x, -75), new Vector2(w, 640), i);
            }
            for (int i = 0; i < CaseDefs.Items.Length; i++)
            {
                float x = (i - (CaseDefs.Items.Length - 1) * 0.5f) * (w + gap);
                BuildCase(CaseDefs.Items[i], tabRoots[1], new Vector2(x, -75), new Vector2(w, 640), i);
            }
            BuildMerchant(tabRoots[2]);

            var leave = UiKit.Button("Leave", root, "Leave shop", UiTheme.PanelLight, new Vector2(380, 100), LeaveShop, 46);
            leave.transform.Find("Text").GetComponent<TMP_Text>().color = UiTheme.Text;
            UiKit.Place((RectTransform)leave.transform, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(380, 100));

            BuildOverlay();
            currentTab = -1;
            ShowTab(0);
        }

        private void BuildCase(CaseDef def, RectTransform parent, Vector2 pos, Vector2 size, int index)
        {
            var panel = UiKit.Img("Case_" + def.id, parent, UiTheme.Panel, UiKit.RoundedRect, true);
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
            var coin = UiKit.Coin("Coin", btn.transform);
            UiKit.Place(coin.rectTransform, new Vector2(1, 0.5f), new Vector2(-135, 0), new Vector2(40, 40));
            if (!UiKit.HasCoinArt) { var coinRing = UiKit.Img("Ring", coin.rectTransform, new Color(0.75f, 0.45f, 0.05f), UiKit.Ring); UiKit.Stretch(coinRing.rectTransform, 5, 5, 5, 5); }
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
