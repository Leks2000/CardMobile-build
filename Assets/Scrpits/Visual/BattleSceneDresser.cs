using System;
using System.Reflection;
using Assets.Scrpits.Run;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// [V] Оформление боя кодом (сцену EnemyScene не трогаем - работает и со старой, и с уже оформленной сценой):
///  - HUD: панель босса (аура, парение, HP-бар с «хвостом» урона, статусы), панель игрока (HP-бар, монеты,
///    мана-кристаллы, End Turn), единый шрифт и палитра;
///  - атмосфера: виньетка, туман, угольки поверх стола, затемнение стола;
///  - цветокоррекция URP (Volume: Color Adjustments / Bloom / Vignette);
///  - панели предметов (<see cref="BattleItemBar"/>) и пассивок (<see cref="BattlePassiveRow"/>).
/// Каждая часть ставится, только если её ещё нет. Вызывается из RunBattleSetup при загрузке боя.
/// </summary>
public class BattleSceneDresser : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly Color PlayerHpColor = new Color32(0x46, 0xC3, 0x5A, 0xFF);
    private static readonly Color BossHpColor = new Color32(0xE0, 0x3A, 0x45, 0xFF);
    private static readonly Color Track = new Color(0.06f, 0.05f, 0.08f, 0.95f);

    public static void Install()
    {
        if (FindAnyObjectByType<BattleSceneDresser>() != null) return;
        var go = new GameObject("[BattleSceneDresser]");
        var scene = SceneManager.GetSceneByName(RunState.BattleSceneName);
        if (scene.IsValid() && scene.isLoaded && go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
        go.AddComponent<BattleSceneDresser>();
    }

    // Start: после всех sceneLoaded-обработчиков (Encounters уже подставил BossData).
    private void Start()
    {
        ItemSystem.OnBattleStart();
        var gameUi = FindCanvas("GameUI");
        var uiRoot = gameUi != null ? (RectTransform)gameUi.transform : null;

        Safe("boss", DressBoss);
        Safe("player", () => DressPlayer(uiRoot));
        Safe("pause", DressPause);
        Safe("atmosphere", DressAtmosphere);
        Safe("post", DressPostFx);
        if (uiRoot != null)
        {
            Safe("items", () => { if (FindAnyObjectByType<BattleItemBar>() == null) BattleItemBar.Create(uiRoot); });
            Safe("passives", () => { if (FindAnyObjectByType<BattlePassiveRow>() == null) BattlePassiveRow.Create(uiRoot, new Vector2(25f, 345f), 330f); });
        }
    }

    private static void Safe(string what, Action a)
    {
        try { a(); }
        catch (Exception e) { Debug.LogWarning($"[V] BattleSceneDresser.{what} failed: {e}"); }
    }

    private static Canvas FindCanvas(string name)
    {
        foreach (var c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c.name == name) return c;
        return null;
    }

    // ---------------- boss ----------------

    private static void DressBoss()
    {
        var boss = FindAnyObjectByType<Boss>();
        if (boss == null || boss.GetComponent<BossView>() != null || boss.bossImage == null) return;
        var panel = (RectTransform)boss.transform;

        // подложка всей колонки босса
        var plate = VisualTheme.Stretch("V_BossPlate", panel, -8f);
        var plateImg = VisualTheme.Img(plate, ProcSprites.RoundRect, VisualTheme.WithA(UiTheme.Panel, 0.82f), true);
        VisualTheme.Ensure<Outline>(plate.gameObject).effectColor = new Color(0, 0, 0, 0.6f);
        plate.SetAsFirstSibling();
        plateImg.raycastTarget = false;

        // имя
        var nameBox = panel.Find("BossName") as RectTransform;
        TMP_Text nameText = null;
        if (nameBox != null)
        {
            PanelImg(nameBox, UiTheme.PanelLight);
            nameText = nameBox.GetComponentInChildren<TMP_Text>(true);
            if (nameText != null)
            {
                VisualTheme.Style(nameText, 42, UiTheme.Text);
                nameText.enableAutoSizing = true; nameText.fontSizeMin = 22; nameText.fontSizeMax = 42;
            }
        }

        // «риг» для парения: портрет переезжает внутрь, чтобы парение и удары не спорили за одну позицию
        var img = boss.bossImage;
        var imgRt = img.rectTransform;
        var rig = VisualTheme.Node("V_BossRig", panel, imgRt.anchorMin, imgRt.anchorMax, imgRt.offsetMin, imgRt.offsetMax);
        rig.SetSiblingIndex(imgRt.GetSiblingIndex());
        imgRt.SetParent(rig, false);
        imgRt.anchorMin = Vector2.zero; imgRt.anchorMax = Vector2.one;
        imgRt.pivot = new Vector2(0.5f, 0.5f);
        imgRt.offsetMin = imgRt.offsetMax = Vector2.zero;
        img.preserveAspect = true;

        var shadow = VisualTheme.Node("V_BossShadow", panel, new Vector2(rig.anchorMin.x, rig.anchorMin.y), new Vector2(rig.anchorMax.x, rig.anchorMin.y),
            new Vector2(40f, -10f), new Vector2(-40f, 26f));
        var shadowImg = VisualTheme.Img(shadow, ProcSprites.Glow, new Color(0, 0, 0, 0.75f));
        var auraOuter = VisualTheme.Img(VisualTheme.Node("V_AuraOuter", panel, rig.anchorMin, rig.anchorMax, new Vector2(-50, -30), new Vector2(50, 50)), ProcSprites.Ring, Color.clear);
        var aura = VisualTheme.Img(VisualTheme.Node("V_Aura", panel, rig.anchorMin, rig.anchorMax, new Vector2(-40, -20), new Vector2(40, 30)), ProcSprites.Glow, Color.clear);
        auraOuter.preserveAspect = true;
        int idx = rig.GetSiblingIndex();
        shadow.SetSiblingIndex(idx);
        auraOuter.transform.SetSiblingIndex(idx + 1);
        aura.transform.SetSiblingIndex(idx + 2);

        // подпись типа боя (BOSS / ELITE / GUARDIAN)
        var subRt = VisualTheme.Node("V_Subtitle", panel, new Vector2(0, 0.77f), new Vector2(1, 0.82f), new Vector2(8, 0), new Vector2(-8, 0));
        var subtitle = VisualTheme.Txt(subRt, "", 26, UiTheme.Accent);
        subtitle.characterSpacing = 6;

        // HP
        HpBarView hpBar = null;
        var hpBox = panel.Find("BossHP") as RectTransform;
        if (hpBox != null)
        {
            var hpText = hpBox.Find("HP") != null ? hpBox.Find("HP").GetComponent<TMP_Text>() : null;
            hpBar = BuildHpBar(hpBox, hpText, BossHpColor, "{0}/{1}", 40);
            var intent = hpBox.Find("D_BossIntent") as RectTransform;
            if (intent != null && intent.TryGetComponent<TMP_Text>(out var it))
            {
                VisualTheme.Style(it, 22, new Color32(0xFF, 0x8A, 0x80, 0xFF), false);
                it.textWrappingMode = TextWrappingModes.Normal;
                it.enableAutoSizing = true; it.fontSizeMin = 14; it.fontSizeMax = 22;
                intent.sizeDelta = new Vector2(0f, 54f);
                intent.anchoredPosition = new Vector2(0f, -6f);
            }
        }

        // статусы босса (яд/кровотечение/щит) над HP
        var statusRt = VisualTheme.Node("V_BossStatus", panel, new Vector2(0, 0.18f), new Vector2(1, 0.18f), new Vector2(10, 4), new Vector2(-10, 42));
        var statusRow = StatusRowView.Setup(statusRt, 34, 20, TextAnchor.MiddleCenter);

        var view = boss.gameObject.AddComponent<BossView>();
        view.rig = rig;
        view.portrait = img;
        view.aura = aura;
        view.auraOuter = auraOuter;
        view.groundShadow = shadowImg;
        view.nameText = nameText;
        view.subtitleText = subtitle;
        view.hpBar = hpBar;
        view.statusRow = statusRow;
        typeof(Boss).GetField("view", Flags)?.SetValue(boss, view);
        boss.SendMessage("OnBossDataChanged", SendMessageOptions.DontRequireReceiver);
    }

    // ---------------- player / HUD ----------------

    private static void DressPlayer(RectTransform uiRoot)
    {
        var player = Player.Instance != null ? Player.Instance : FindAnyObjectByType<Player>();
        if (player == null || player.GetComponentInChildren<HpBarView>(true) != null) return;
        var panel = player.transform;

        // HP
        HpBarView hpBar = null;
        var hpBox = panel.Find("PlayerHP") as RectTransform;
        if (hpBox != null && player.playerHpText != null)
        {
            hpBar = BuildHpBar(hpBox, player.playerHpText, PlayerHpColor, "HP {0}/{1}", 36);
            if (player.takeDamageText != null) player.takeDamageText.transform.SetAsLastSibling();
            typeof(Player).GetField("hpBar", Flags)?.SetValue(player, hpBar);
            player.UpdatePlayerDisplay();

            // статусы игрока (щит и т.п.) - над HP-баром справа
            var srt = VisualTheme.Node("V_PlayerStatus", hpBox, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 2), new Vector2(0, 34));
            StatusRowView.Setup(srt, 30, 18, TextAnchor.MiddleRight).Track(player);
        }

        // монеты
        TMP_Text coinsText = null;
        RectTransform coinIcon = null;
        var money = panel.Find("Money") as RectTransform;
        if (money != null)
        {
            PanelImg(money, UiTheme.Panel);
            coinIcon = VisualTheme.Centered("V_Coin", money, new Vector2(0, 0.5f), new Vector2(46, 46), new Vector2(34, 0));
            VisualTheme.Img(coinIcon, ProcSprites.Circle, UiTheme.Accent);
            VisualTheme.Ensure<Outline>(coinIcon.gameObject).effectColor = new Color(0.45f, 0.25f, 0.05f, 1f);
            VisualTheme.Img(VisualTheme.Stretch("Ring", coinIcon, 8f), ProcSprites.Ring, new Color(0.85f, 0.55f, 0.1f, 1f));
            coinsText = money.GetComponentInChildren<TMP_Text>(true);
            if (coinsText != null)
            {
                VisualTheme.Style(coinsText, 40, UiTheme.Accent, true, TextAlignmentOptions.Left);
                var trt = coinsText.rectTransform;
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(66, 0); trt.offsetMax = new Vector2(-8, 0);
            }
        }

        // End Turn
        var end = panel.Find("EndTurn") as RectTransform;
        Image endFace = null, endGlow = null;
        TMP_Text endLabel = null;
        if (end != null)
        {
            var glowRt = VisualTheme.Node("V_EndTurnGlow", panel, end.anchorMin, end.anchorMax, new Vector2(-26, -26), new Vector2(26, 26));
            glowRt.SetSiblingIndex(end.GetSiblingIndex());
            endGlow = VisualTheme.Img(glowRt, ProcSprites.Glow, Color.clear);
            endFace = PanelImg(end, BattleHud.EndTurnOn);
            VisualTheme.Img(VisualTheme.Node("V_Gloss", end, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(8, 0), new Vector2(-8, -5)), ProcSprites.Gloss, Color.white, true);
            endLabel = end.GetComponentInChildren<TMP_Text>(true);
            if (endLabel != null)
            {
                VisualTheme.Style(endLabel, 38, new Color32(0x2A, 0x16, 0x08, 0xFF), false);
                endLabel.transform.SetAsLastSibling();
            }
        }

        // мана
        CardCostStatus mana = FindAnyObjectByType<CardCostStatus>();
        RectTransform pipRow = null;
        Image manaGem = null;
        var magic = uiRoot != null ? uiRoot.Find("UI/Magic") as RectTransform : null;
        if (magic != null)
        {
            var drop = magic.Find("Magic");
            if (drop != null) manaGem = drop.GetComponent<Image>();
            var power = magic.Find("MagicPower");
            if (power != null && power.TryGetComponent<TMP_Text>(out var pt)) VisualTheme.Style(pt, 54, Color.white);
            pipRow = VisualTheme.Node("V_ManaPips", magic, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 2), new Vector2(0, 32));
            var hl = VisualTheme.Ensure<HorizontalLayoutGroup>(pipRow.gameObject);
            hl.childAlignment = TextAnchor.MiddleCenter; hl.spacing = 6;
            hl.childControlWidth = hl.childControlHeight = false;
            hl.childForceExpandWidth = hl.childForceExpandHeight = false;
        }

        // колода
        TMP_Text deckCount = null;
        var gcm = FindAnyObjectByType<GameControlManager>();
        var deck = FindAnyObjectByType<CardDeck>();
        if (deck != null)
        {
            deckCount = deck.GetComponentInChildren<TMP_Text>(true);
            if (deckCount != null) VisualTheme.Style(deckCount, 26, Color.white);
        }

        if (uiRoot == null || uiRoot.GetComponentInChildren<BattleHud>(true) != null) return;
        var hud = uiRoot.gameObject.AddComponent<BattleHud>();
        hud.coinsText = coinsText;
        hud.coinIcon = coinIcon;
        hud.mana = mana;
        hud.manaPipRow = pipRow;
        hud.manaGem = manaGem;
        hud.endTurn = end != null ? end.GetComponent<EndTurnCamera>() : null;
        hud.endTurnFace = endFace;
        hud.endTurnLabel = endLabel;
        hud.endTurnGlow = endGlow;
        hud.deckCount = deckCount;
        hud.Bind();
        if (gcm == null) Debug.LogWarning("[V] No GameControlManager in battle scene");
    }

    private static void DressPause()
    {
        var c = FindCanvas("CanvasSetBTN");
        var pause = c != null ? c.transform.Find("Pause") as RectTransform : null;
        if (pause == null || pause.Find("V_Gloss") != null) return;
        PanelImg(pause, UiTheme.Panel);
        VisualTheme.Img(VisualTheme.Node("V_Gloss", pause, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(6, 0), new Vector2(-6, -4)), ProcSprites.Gloss, new Color(1, 1, 1, 0.5f), true);
        var t = pause.GetComponentInChildren<TMP_Text>(true);
        if (t != null)
        {
            VisualTheme.Style(t, 30, UiTheme.Text);
            t.transform.SetAsLastSibling();
        }
    }

    // ---------------- atmosphere / post ----------------

    private static void DressAtmosphere()
    {
        if (GameObject.Find("V_Atmosphere") != null || FindAnyObjectByType<BattleBackground>() != null) return;
        var go = new GameObject("V_Atmosphere", typeof(RectTransform));
        MoveToScene(go, gameObjectScene: FindAnyObjectByType<BattleSceneDresser>());
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1; // над столом, под HUD (GameUI = 6) и результатом боя (7)
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = (RectTransform)go.transform;

        var vig = VisualTheme.Img(VisualTheme.Stretch("Vignette", root, -40f), ProcSprites.Vignette, new Color(1, 1, 1, 0.55f));
        vig.raycastTarget = false;

        var bg = go.AddComponent<BattleBackground>();
        var fogTex = ProcSprites.Get(ProcSprites.Fog);
        for (int i = 0; i < 2; i++)
        {
            var rt = VisualTheme.Node("Fog" + i, root, new Vector2(0, 0), new Vector2(1, i == 0 ? 0.45f : 0.3f), new Vector2(-60, -40), new Vector2(60, 0));
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = fogTex != null ? fogTex.texture : null;
            raw.color = i == 0 ? new Color(0.65f, 0.6f, 0.85f, 0.07f) : new Color(0.9f, 0.6f, 0.45f, 0.05f);
            raw.raycastTarget = false;
            raw.uvRect = new Rect(0, 0, i == 0 ? 1.5f : 2.2f, 1);
            bg.fog.Add(new BattleBackground.FogLayer { image = raw, speed = i == 0 ? 0.008f : -0.013f, bob = 0.02f });
        }
        var embers = VisualTheme.Stretch("Embers", root);
        bg.emberRoot = embers;
        bg.emberCount = 14;
        bg.emberColor = new Color(1f, 0.55f, 0.25f, 0.45f);

        // стол темнее и холоднее - карты и HUD читаются лучше
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if (r.name != "Plane" || r.sharedMaterial == null || !r.enabled) continue;
            var m = r.material;
            m.color = m.color * new Color(0.62f, 0.56f, 0.66f, 1f);
        }
    }

    private static void DressPostFx()
    {
        if (GameObject.Find("V_PostFX") != null || FindAnyObjectByType<Volume>() != null) return;
        var go = new GameObject("V_PostFX");
        MoveToScene(go, FindAnyObjectByType<BattleSceneDresser>());
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "V_BattleGrade (runtime)";

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.1f);
        color.contrast.Override(14f);
        color.saturation.Override(8f);
        color.colorFilter.Override(new Color(1f, 0.95f, 0.9f, 1f));

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.65f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.3f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.05f, 0.02f, 0.06f, 1f));

        volume.profile = profile;

        foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) data.renderPostProcessing = true;
        }
    }

    // ---------------- helpers ----------------

    /// <summary>Новые объекты - в сцену боя (чтобы выгружались вместе с ней).</summary>
    private static void MoveToScene(GameObject go, Component gameObjectScene)
    {
        if (gameObjectScene == null) return;
        var scene = gameObjectScene.gameObject.scene;
        if (scene.IsValid() && go.scene != scene) SceneManager.MoveGameObjectToScene(go, scene);
    }

    /// <summary>Перекрасить существующую плашку: скруглённый 9-slice + контур.</summary>
    private static Image PanelImg(RectTransform rt, Color color)
    {
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.sprite = ProcSprites.Get(ProcSprites.RoundRect);
        img.type = Image.Type.Sliced;
        img.color = color;
        var ol = VisualTheme.Ensure<Outline>(rt.gameObject);
        ol.effectColor = VisualTheme.Outline;
        ol.effectDistance = new Vector2(2f, -2f);
        return img;
    }

    /// <summary>HP-бар на месте старой плашки «HP: N»: трек, скруглённая маска, хвост урона, заливка, блик, число.</summary>
    private static HpBarView BuildHpBar(RectTransform box, TMP_Text label, Color fillColor, string format, float fontSize)
    {
        PanelImg(box, Track);
        var clip = VisualTheme.Stretch("V_Clip", box, 6f);
        var clipImg = VisualTheme.Img(clip, ProcSprites.RoundRect, Color.white, true);
        var mask = VisualTheme.Ensure<Mask>(clip.gameObject);
        mask.showMaskGraphic = false;
        clipImg.raycastTarget = false;

        var trail = VisualTheme.Img(VisualTheme.Stretch("Trail", clip), null, new Color(1f, 0.92f, 0.75f, 0.9f));
        trail.type = Image.Type.Filled;
        trail.fillMethod = Image.FillMethod.Horizontal;
        trail.fillOrigin = (int)Image.OriginHorizontal.Left;
        var fill = VisualTheme.Img(VisualTheme.Stretch("Fill", clip), null, fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        var shade = VisualTheme.Img(VisualTheme.Node("Shade", clip, Vector2.zero, new Vector2(1, 0.45f), Vector2.zero, Vector2.zero), null, new Color(0, 0, 0, 0.18f));
        shade.raycastTarget = false;
        VisualTheme.Img(VisualTheme.Node("Gloss", clip, new Vector2(0, 0.55f), new Vector2(1, 1), new Vector2(4, 0), new Vector2(-4, -3)), ProcSprites.Gloss, new Color(1, 1, 1, 0.55f), true);

        var bar = VisualTheme.Ensure<HpBarView>(box.gameObject);
        bar.fill = fill;
        bar.trail = trail;
        bar.fillColor = fillColor;
        bar.format = format;
        if (label != null)
        {
            VisualTheme.Style(label, fontSize, Color.white);
            label.enableAutoSizing = true; label.fontSizeMin = fontSize * 0.55f; label.fontSizeMax = fontSize;
            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 0); lrt.offsetMax = new Vector2(-8, 0);
            label.transform.SetAsLastSibling();
            bar.label = label;
        }
        return bar;
    }
}
