using System.Collections.Generic;
using System.Linq;
using Assets.Scrpits.Map;
using Assets.Scrpits.Run;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Assets.Scrpits.Shop
{
    /// <summary>
    /// Слот-машина карт (вкладка CARD SLOTS магазина): 3 вертикальных барабана с картами и символом «6».
    /// Три одинаковые карты - карта в колоду; 6-6-6 - джекпот 666 монет. Запуск - рычаг (тянуть мышью / тапнуть).
    /// Результат решается до вращения, барабаны просто докручиваются до него.
    /// Eval: Assets.Scrpits.Shop.SlotMachine.PullNow()
    /// </summary>
    public class SlotMachine : MonoBehaviour
    {
        public const int BasePrice = 40;
        public const int JackpotCoins = 666;
        public const float JackpotChance = 0.03f;
        public const float CardChance = 0.30f;

        private const float SymbolScale = 0.55f;
        private const int ReelLength = 26;
        private static readonly Color Gold = new Color32(0xFF, 0xC4, 0x4D, 0xFF);
        private static readonly Color Six = new Color32(0xE8, 0x2E, 0x2E, 0xFF);

        // окна барабанов на арте Art/UI/slots_three (пиксели, левая часть листа 318x176, отсчёт сверху)
        private static readonly Rect ArtCrop = new Rect(0, 0, 318, 176);
        private static readonly Vector2 ArtSheet = new Vector2(504, 194);
        private static readonly float[] WindowX = { 39, 132, 225 };
        private const float WindowW = 74, WindowTop = 72, WindowH = 72;
        private const float ArtScale = 2.8f;

        public static SlotMachine Instance { get; private set; }
        public static int Price => RelicSystem.ShopPrice(BasePrice);

        private struct Sym
        {
            public CardData card;
            public bool six;
            public string Id => six ? "six" : card != null ? card.Id : "none";
        }

        private CaseShopController shop;
        private RectTransform root;
        private RectTransform machine;
        private readonly RectTransform[] windows = new RectTransform[3];
        private readonly RectTransform[] reels = new RectTransform[3];
        private readonly Image[] flashes = new Image[3];
        private readonly Sym[] shown = new Sym[3];
        private readonly List<RectTransform>[] tiles = { new List<RectTransform>(), new List<RectTransform>(), new List<RectTransform>() };
        private TMP_Text resultText;
        private TMP_Text priceText;
        private SlotLever lever;
        private int pendingCoins;

        public bool Spinning { get; private set; }
        private float Step => CardTile.BaseSize.y * SymbolScale + 22f;

        public static SlotMachine Build(RectTransform parent, CaseShopController shop)
        {
            var rt = UiKit.Rect("SlotMachine", parent);
            UiKit.Stretch(rt);
            var sm = rt.gameObject.AddComponent<SlotMachine>();
            sm.shop = shop;
            sm.root = rt;
            sm.BuildUi();
            Instance = sm;
            return sm;
        }

        public static bool PullNow() => Instance != null && Instance.Pull();

        private void OnDestroy()
        {
            // сцену закрыли посреди вращения - выигрыш не теряется
            if (pendingCoins > 0) { Wallet.Add(pendingCoins); pendingCoins = 0; }
            if (Instance == this) Instance = null;
            DOTween.Kill(this);
        }

        public void RefreshPrice()
        {
            if (priceText == null) return;
            int p = Price;
            priceText.text = p.ToString();
            priceText.color = Wallet.Coins >= p ? UiTheme.Text : UiTheme.Damage;
        }

        // ---------------- logic ----------------

        private bool CanPull => !Spinning && (shop == null || !shop.IsBusy);

        private bool Pull()
        {
            if (!CanPull) return false;
            int price = Price;
            if (!Wallet.TrySpend(price))
            {
                machine.DOKill(true);
                machine.DOShakeAnchorPos(0.4f, new Vector2(16, 0), 20, 0).SetTarget(this);
                ShowResult($"Need {price - Wallet.Coins} more coins", UiTheme.Damage);
                return false;
            }

            var pool = CardDatabase.Obtainable.ToList();
            Sym[] result;
            string outcome;
            float r = Random.value;
            if (r < JackpotChance || pool.Count == 0)
            {
                result = Triple(new Sym { six = true });
                outcome = "jackpot";
                pendingCoins = JackpotCoins;
            }
            else if (r < JackpotChance + CardChance)
            {
                var card = CaseDefs.PickCard(RollRarity());
                result = Triple(new Sym { card = card });
                outcome = "card";
                // карта выдаётся сразу (как у кейсов) - даже если магазин закроют во время вращения
                RunState.AddCard(card);
                shop?.RecordWin(new CaseDrop(card));
            }
            else
            {
                result = Loss(pool);
                outcome = "loss";
            }
            Debug.Log($"[SLOTS] Pull for {price}: {outcome} [{result[0].Id},{result[1].Id},{result[2].Id}] coins={Wallet.Coins}");
            Spin(result, outcome, pool);
            return true;
        }

        private static Sym[] Triple(Sym s) => new[] { s, s, s };

        private static CardRarity RollRarity()
        {
            float v = Random.value * 100f;
            if (v < 62f) return CardRarity.Common;
            if (v < 88f) return CardRarity.Rare;
            if (v < 97.5f) return CardRarity.Epic;
            return CardRarity.Legendary;
        }

        /// <summary>Проигрыш: не три одинаковых; часто «чуть-чуть» - первые два совпали (иногда 6-6-x).</summary>
        private static Sym[] Loss(List<CardData> pool)
        {
            Sym Any() => Random.value < 0.14f ? new Sym { six = true } : new Sym { card = pool[Random.Range(0, pool.Count)] };
            var res = new Sym[3];
            if (Random.value < 0.5f)
            {
                res[0] = Random.value < 0.25f ? new Sym { six = true } : new Sym { card = pool[Random.Range(0, pool.Count)] };
                res[1] = res[0];
            }
            else
            {
                res[0] = Any();
                res[1] = Any();
            }
            for (int i = 0; i < 12; i++)
            {
                res[2] = Any();
                if (res[2].Id != res[0].Id || res[0].Id != res[1].Id) break;
            }
            if (res[0].Id == res[1].Id && res[1].Id == res[2].Id)
                res[2] = res[0].six ? new Sym { card = pool[0] } : new Sym { six = true };
            return res;
        }

        private Sym Filler(List<CardData> pool)
        {
            if (pool.Count == 0 || Random.value < 0.13f) return new Sym { six = true };
            return new Sym { card = pool[Random.Range(0, pool.Count)] };
        }

        private void Spin(Sym[] result, string outcome, List<CardData> pool)
        {
            Spinning = true;
            shop?.SetSlotsBusy(true);
            ShowResult("", UiTheme.Text);
            foreach (var f in flashes) { f.DOKill(); f.color = new Color(1, 1, 1, 0); }

            bool nearMiss = outcome == "loss" && result[0].Id == result[1].Id;
            int stopped = 0;
            for (int i = 0; i < 3; i++)
            {
                int reel = i;
                var list = tiles[reel];
                foreach (var t in list) if (t != null) Destroy(t.gameObject);
                list.Clear();
                // k = -1 (под центром) ... ReelLength; центр в начале - то, что было видно
                int win = ReelLength - 2;
                for (int k = -1; k <= ReelLength; k++)
                {
                    Sym s = k == 0 && shown[reel].Id != "none" ? shown[reel] : k == win ? result[reel] : Filler(pool);
                    var t = BuildSymbol(reels[reel], s);
                    t.anchoredPosition = new Vector2(0, k * Step);
                    list.Add(t);
                }
                reels[reel].anchoredPosition = Vector2.zero;
                float dur = 2.6f + reel * 0.9f + (reel == 2 && nearMiss ? 1.4f : 0f);
                int lastIdx = 0;
                var seq = DOTween.Sequence().SetTarget(this);
                seq.Append(reels[reel].DOAnchorPosY(-win * Step - Step * 0.18f, dur).SetEase(Ease.OutCubic).OnUpdate(() =>
                {
                    int idx = Mathf.FloorToInt(-reels[reel].anchoredPosition.y / Step);
                    if (idx == lastIdx) return;
                    lastIdx = idx;
                    if (stopped == reel) SoundFx.Play(SoundFx.Clip.Tick, 0.55f, 0.04f); // щёлкает ближайший к остановке барабан
                }));
                // лёгкая «отдача» барабана при остановке
                seq.Append(reels[reel].DOAnchorPosY(-win * Step, 0.28f).SetEase(Ease.OutBack));
                seq.AppendCallback(() =>
                {
                    stopped++;
                    shown[reel] = result[reel];
                    SoundFx.Play(SoundFx.Clip.Block, 0.7f);
                    var tile = list[win + 1];
                    tile.DOPunchScale(Vector3.one * 0.12f, 0.25f, 6, 0.6f).SetTarget(this);
                    if (reel == 1 && nearMiss) ShowResult(result[0].six ? "6 - 6 - ...?" : "Two of a kind...", Gold);
                    if (stopped == 3) Finish(result, outcome, list[win + 1]);
                });
            }
        }

        private void Finish(Sym[] result, string outcome, RectTransform winTile)
        {
            var seq = DOTween.Sequence().SetTarget(this);
            switch (outcome)
            {
                case "jackpot":
                {
                    Flash(Gold, true);
                    ShowResult($"JACKPOT!  +{JackpotCoins}", Gold, true);
                    SoundFx.Play(SoundFx.Clip.RevealEpic);
                    SoundFx.Vibrate();
                    int coins = pendingCoins;
                    pendingCoins = 0;
                    Wallet.Add(coins);
                    CoinShower(40);
                    for (int i = 0; i < 6; i++) SoundFx.PlayDelayed(SoundFx.Clip.Coin, 0.25f + i * 0.18f, 0.8f);
                    seq.AppendInterval(1.6f);
                    seq.AppendCallback(EndSpin);
                    break;
                }
                case "card":
                {
                    var card = result[0].card;
                    Flash(RarityColors.Get(card.rarity), false);
                    ShowResult("THREE OF A KIND!", RarityColors.Get(card.rarity), true);
                    SoundFx.Play(SoundFx.Clip.Reveal);
                    seq.AppendInterval(1.1f);
                    seq.AppendCallback(() =>
                    {
                        Spinning = false;
                        // экран выигрыша магазина; IsBusy снимет его кнопка Continue
                        if (shop != null) shop.ShowSlotWin(new CaseDrop(card), winTile);
                        else EndSpin();
                    });
                    break;
                }
                default:
                {
                    bool near = result[0].Id == result[1].Id;
                    ShowResult(near ? "So close! Try again" : "No luck this time", near ? Gold : UiTheme.TextDim);
                    SoundFx.Play(SoundFx.Clip.Lose, 0.35f);
                    seq.AppendInterval(0.3f);
                    seq.AppendCallback(EndSpin);
                    break;
                }
            }
        }

        private void EndSpin()
        {
            Spinning = false;
            shop?.SetSlotsBusy(false);
            RefreshPrice();
        }

        // ---------------- fx ----------------

        private void ShowResult(string text, Color color, bool big = false)
        {
            resultText.transform.DOKill(true);
            resultText.text = text;
            resultText.color = color;
            resultText.fontSize = big ? 72 : 46;
            if (string.IsNullOrEmpty(text)) return;
            resultText.transform.localScale = Vector3.one * 0.6f;
            resultText.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(resultText.gameObject);
        }

        private void Flash(Color c, bool loop)
        {
            foreach (var f in flashes)
            {
                f.DOKill();
                f.color = new Color(c.r, c.g, c.b, 0f);
                var tw = f.DOFade(0.95f, 0.25f).SetLink(f.gameObject);
                if (loop) tw.SetLoops(8, LoopType.Yoyo);
            }
        }

        private void CoinShower(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var coin = UiKit.Coin("FxCoin", root);
                var rt = coin.rectTransform;
                UiKit.Place(rt, new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-700f, 700f), Random.Range(420f, 700f)), Vector2.one * Random.Range(44f, 72f));
                float delay = Random.Range(0f, 0.9f);
                float dur = Random.Range(1.2f, 1.9f);
                rt.DOAnchorPosY(-620f, dur).SetDelay(delay).SetEase(Ease.InQuad).SetTarget(this);
                rt.DORotate(new Vector3(0, 0, Random.Range(-540f, 540f)), dur, RotateMode.FastBeyond360).SetDelay(delay).SetTarget(this);
                coin.DOFade(0f, 0.3f).SetDelay(delay + dur - 0.3f).SetTarget(this).OnComplete(() => { if (rt != null) Destroy(rt.gameObject); });
            }
        }

        // ---------------- UI ----------------

        private RectTransform BuildSymbol(RectTransform reel, Sym s)
        {
            RectTransform t = s.six ? BuildSix(reel) : CardTile.Build(reel, s.card, SymbolScale);
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f);
            t.pivot = new Vector2(0.5f, 0.5f);
            return t;
        }

        /// <summary>Символ джекпота: золотая карточка с красной «6» и монетами.</summary>
        private static RectTransform BuildSix(Transform parent)
        {
            var root = UiKit.Rect("Six", parent);
            root.sizeDelta = CardTile.BaseSize;
            root.localScale = Vector3.one * SymbolScale;
            var frame = UiKit.Img("Frame", root, Gold, UiKit.RoundedRect, true);
            UiKit.Stretch(frame.rectTransform);
            var face = UiKit.Img("Face", root, new Color32(0x2A, 0x10, 0x0C, 0xFF), UiKit.RoundedRect, true);
            UiKit.Stretch(face.rectTransform, 8, 8, 8, 8);
            var glow = UiKit.Img("Glow", root, new Color(1f, 0.35f, 0.2f, 0.45f), UiKit.Glow);
            UiKit.Place(glow.rectTransform, new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(220, 220));
            var pile = ArtLib.UI("coins_pile");
            if (pile != null)
            {
                var p = UiKit.Img("Coins", root, Color.white, pile);
                p.preserveAspect = true;
                UiKit.Place(p.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(150, 80));
            }
            else
            {
                var c = UiKit.Coin("Coin", root);
                UiKit.Place(c.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 52), new Vector2(70, 70));
            }
            var six = UiKit.Text("Six", root, "6", 190, Six);
            VisualTheme.Style(six, 190, Six, true);
            UiKit.Place(six.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 34), new Vector2(200, 200));
            return root;
        }

        private void BuildUi()
        {
            // корпус: арт трёх окон; если арта нет - простая панель
            var sheet = ArtLib.UI("slots_three");
            Vector2 size = new Vector2(ArtCrop.width, ArtCrop.height) * ArtScale;
            if (sheet != null)
            {
                var raw = UiKit.Rect("Machine", root).gameObject.AddComponent<RawImage>();
                raw.texture = sheet.texture;
                raw.raycastTarget = false;
                raw.uvRect = new Rect(ArtCrop.x / ArtSheet.x, 1f - (ArtCrop.y + ArtCrop.height) / ArtSheet.y, ArtCrop.width / ArtSheet.x, ArtCrop.height / ArtSheet.y);
                machine = raw.rectTransform;
            }
            else
            {
                var panel = UiKit.Img("Machine", root, UiTheme.Panel, UiKit.RoundedRect, true);
                machine = panel.rectTransform;
            }
            UiKit.Place(machine, new Vector2(0.5f, 0.5f), new Vector2(-40, -80), size);
            var msh = machine.gameObject.AddComponent<Shadow>();
            msh.effectColor = new Color(0, 0, 0, 0.6f);
            msh.effectDistance = new Vector2(0, -10);

            for (int i = 0; i < 3; i++)
            {
                // окно в координатах машины (центр арта = 0,0; y арта идёт сверху вниз)
                float cx = (WindowX[i] + WindowW * 0.5f - ArtCrop.width * 0.5f) * ArtScale;
                float cy = (ArtCrop.height * 0.5f - (WindowTop + WindowH * 0.5f)) * ArtScale;
                var win = UiKit.Img("Window" + i, machine, new Color(0.05f, 0.03f, 0.02f, 0.85f));
                UiKit.Place(win.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(cx, cy), new Vector2(WindowW, WindowH) * ArtScale);
                win.gameObject.AddComponent<RectMask2D>();
                windows[i] = win.rectTransform;
                reels[i] = UiKit.Rect("Reel", windows[i]);
                UiKit.Place(reels[i], new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10, 10));
                // тени сверху/снизу окна - барабан «круглый»
                for (int s = 0; s < 2; s++)
                {
                    var shade = UiKit.Img(s == 0 ? "ShadeTop" : "ShadeBottom", windows[i], new Color(0, 0, 0, 0.75f), UiKit.VGradient);
                    var srt = shade.rectTransform;
                    srt.anchorMin = new Vector2(0, s == 0 ? 0.72f : 0f);
                    srt.anchorMax = new Vector2(1, s == 0 ? 1f : 0.28f);
                    srt.offsetMin = srt.offsetMax = Vector2.zero;
                    if (s == 1) srt.localScale = new Vector3(1, -1, 1);
                }
                flashes[i] = UiKit.Img("Flash", windows[i], new Color(1, 1, 1, 0), UiKit.RoundedFrame, true);
                UiKit.Stretch(flashes[i].rectTransform, -2, -2, -2, -2);
                // начальный вид - случайные карты
                var pool = CardDatabase.Obtainable.ToList();
                shown[i] = Filler(pool);
                tiles[i].Add(BuildSymbol(reels[i], shown[i]));
            }

            // стрелки линии выигрыша
            for (int s = 0; s < 2; s++)
            {
                var tri = UiKit.Img(s == 0 ? "LineL" : "LineR", machine, Gold, UiKit.RoundedRect);
                float x = s == 0 ? windows[0].anchoredPosition.x - windows[0].sizeDelta.x * 0.5f - 34f : windows[2].anchoredPosition.x + windows[2].sizeDelta.x * 0.5f + 34f;
                UiKit.Place(tri.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, windows[0].anchoredPosition.y), new Vector2(30, 30));
                tri.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            }

            var title = UiKit.Text("SlotsTitle", root, "DUNGEON SLOTS", 64, Gold);
            VisualTheme.Style(title, 64, Gold, true);
            UiKit.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40, machine.anchoredPosition.y + size.y * 0.5f + 34f), new Vector2(900, 70));

            resultText = UiKit.Text("Result", root, "", 46, UiTheme.Text);
            VisualTheme.Style(resultText, 46, UiTheme.Text, true);
            UiKit.Place(resultText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40, machine.anchoredPosition.y - size.y * 0.5f - 36f), new Vector2(1100, 64));

            BuildLever(size);
            BuildPaytable(size);
            RefreshPrice();
        }

        private void BuildLever(Vector2 machineSize)
        {
            float x = machine.anchoredPosition.x + machineSize.x * 0.5f + 70f;
            float y = machine.anchoredPosition.y;
            var area = UiKit.Img("Lever", root, new Color(0, 0, 0, 0));
            area.raycastTarget = true;
            UiKit.Place(area.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(150, 520));

            var housing = UiKit.Img("Housing", area.rectTransform, new Color32(0x3A, 0x2A, 0x1E, 0xFF), UiKit.RoundedRect, true);
            UiKit.Place(housing.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-40, 0), new Vector2(70, 150));
            var hsh = housing.gameObject.AddComponent<Shadow>();
            hsh.effectColor = new Color(0, 0, 0, 0.6f); hsh.effectDistance = new Vector2(0, -6);
            var bolt = UiKit.Img("Bolt", area.rectTransform, Gold, UiKit.Circle);
            UiKit.Place(bolt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(46, 46));

            var shaft = UiKit.Img("Shaft", area.rectTransform, new Color32(0xB8, 0xB0, 0xA4, 0xFF));
            var srt = shaft.rectTransform;
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot = new Vector2(0.5f, 0f);
            srt.anchoredPosition = Vector2.zero;
            srt.sizeDelta = new Vector2(16, 200);
            bolt.transform.SetAsLastSibling();

            var knob = UiKit.Img("Knob", area.rectTransform, new Color32(0xD8, 0x2A, 0x2A, 0xFF), UiKit.Circle);
            UiKit.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(76, 76));
            var shine = UiKit.Img("Shine", knob.rectTransform, new Color(1, 1, 1, 0.45f), UiKit.Circle);
            UiKit.Place(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-12, 12), new Vector2(24, 24));
            var ksh = knob.gameObject.AddComponent<Shadow>();
            ksh.effectColor = new Color(0, 0, 0, 0.55f); ksh.effectDistance = new Vector2(0, -5);

            var hint = UiKit.Text("Hint", area.rectTransform, "PULL", 30, UiTheme.TextDim);
            UiKit.Place(hint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 268), new Vector2(160, 40));
            hint.DOFade(0.35f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(hint.gameObject);

            // цена под рычагом
            var coin = UiKit.Coin("PriceCoin", area.rectTransform);
            UiKit.Place(coin.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-30, -240), new Vector2(40, 40));
            priceText = UiKit.Text("Price", area.rectTransform, "", 40, UiTheme.Text, TextAlignmentOptions.Left);
            UiKit.Place(priceText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(35, -240), new Vector2(90, 50));

            lever = area.gameObject.AddComponent<SlotLever>();
            lever.knob = knob.rectTransform;
            lever.shaft = srt;
            lever.travel = 400f;
            lever.canPull = () => CanPull;
            lever.onPull = () => Pull();
        }

        private void BuildPaytable(Vector2 machineSize)
        {
            float x = machine.anchoredPosition.x - machineSize.x * 0.5f - 200f;
            var panel = UiKit.Img("Paytable", root, UiTheme.Panel, UiKit.RoundedRect, true);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, machine.anchoredPosition.y), new Vector2(330, 470));
            var frame = UiKit.Img("Frame", panel.rectTransform, new Color(Gold.r, Gold.g, Gold.b, 0.5f), UiKit.RoundedFrame, true);
            UiKit.Stretch(frame.rectTransform);
            var prt = panel.rectTransform;
            string six = $"<color=#{UiKit.Hex(Six)}>6 6 6</color>";
            string gold = UiKit.Hex(Gold);
            var head = UiKit.Text("Head", prt, "PAYTABLE", 44, Gold);
            UiKit.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -44), new Vector2(300, 60));
            var body = UiKit.Text("Body", prt,
                $"<size=130%>{six}</size>\n<color=#{gold}>{JackpotCoins} coins</color>\n\n" +
                $"3 same cards\n<color=#{gold}>= the card</color>\n\n" +
                $"<size=80%><color=#{UiKit.Hex(UiTheme.TextDim)}>Card win {CardChance * 100f:0}%\nJackpot {JackpotChance * 100f:0}%</color></size>",
                34, UiTheme.Text);
            body.lineSpacing = -6;
            UiKit.Stretch(body.rectTransform, 16, 16, 90, 20);
            body.alignment = TextAlignmentOptions.Top;
        }
    }

    /// <summary>Рычаг слот-машины: тянуть вниз мышью/пальцем (дотянул - запуск) или просто тапнуть.</summary>
    public class SlotLever : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public RectTransform shaft;
        public float travel = 400f;
        public System.Func<bool> canPull;
        public System.Func<bool> onPull;

        private float pull;      // 0 - вверху, 1 - дотянут вниз
        private bool dragging;
        private bool fired;
        private float moved;
        private Vector2 top;
        private float shaftLen;
        private Canvas canvas;

        private void Start()
        {
            top = knob.anchoredPosition;
            shaftLen = shaft.sizeDelta.y;
            canvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (canPull != null && !canPull()) { Wiggle(); return; }
            dragging = true;
            fired = false;
            moved = 0f;
            DOTween.Kill(this);
        }

        public void OnDrag(PointerEventData e)
        {
            if (!dragging) return;
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            float dy = -e.delta.y / Mathf.Max(0.01f, scale);
            moved += Mathf.Abs(dy);
            pull = Mathf.Clamp01(pull + dy / travel);
            Apply();
            if (!fired && pull >= 0.92f) Fire();
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            if (!fired && moved < 12f) { AutoPull(); return; } // тап - рычаг дёргается сам
            Release();
        }

        private void Fire()
        {
            fired = true;
            SoundFx.Play(SoundFx.Clip.Click, 1f);
            SoundFx.Vibrate();
            onPull?.Invoke();
        }

        private void AutoPull()
        {
            DOTween.Kill(this);
            DOTween.To(() => pull, v => { pull = v; Apply(); }, 1f, 0.22f).SetEase(Ease.InQuad).SetTarget(this)
                .OnComplete(() => { if (!fired) Fire(); Release(); });
        }

        private void Release()
        {
            DOTween.Kill(this);
            DOTween.To(() => pull, v => { pull = v; Apply(); }, 0f, 0.7f).SetEase(Ease.OutElastic).SetTarget(this);
        }

        private void Wiggle()
        {
            if (knob == null) return;
            knob.DOKill(true);
            knob.DOPunchAnchorPos(new Vector2(10, 0), 0.3f, 12, 0.5f).SetLink(knob.gameObject);
        }

        /// <summary>Рычаг «поворачивается к игроку»: ручка идёт вниз мимо оси, штанга укорачивается и переворачивается.</summary>
        private void Apply()
        {
            if (knob == null || shaft == null) return;
            float y = Mathf.Lerp(top.y, -top.y, pull);
            knob.anchoredPosition = new Vector2(top.x, y);
            knob.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(pull * Mathf.PI));
            shaft.localScale = new Vector3(1f + 0.4f * Mathf.Sin(pull * Mathf.PI), y / Mathf.Max(1f, shaftLen), 1f);
        }

        private void OnDestroy() => DOTween.Kill(this);
    }
}
