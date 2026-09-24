using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [V] Battle HUD driver: coins (Wallet), mana orbs (CardCostStatus), End Turn enabled/disabled look
/// (reads EndTurnCamera's private hasClicked read-only), deck counter punch. Presentation only.
/// </summary>
public class BattleHud : MonoBehaviour
{
    [Header("Coins")]
    public TMP_Text coinsText;
    public RectTransform coinIcon;

    [Header("Mana")]
    public CardCostStatus mana;
    public RectTransform manaPipRow;
    public Image manaGem;

    [Header("End turn")]
    public EndTurnCamera endTurn;
    public Image endTurnFace;
    public TMP_Text endTurnLabel;
    public Image endTurnGlow;

    [Header("Deck")]
    public TMP_Text deckCount;

    public static readonly Color EndTurnOn = new Color32(0xFF, 0xB8, 0x3D, 0xFF);
    public static readonly Color EndTurnOff = new Color32(0x4A, 0x43, 0x55, 0xFF);

    private readonly List<Image> pips = new List<Image>();
    private int shownMana = -1, shownMax = -1;
    private int shownCoins = int.MinValue;
    private string shownDeck;
    private FieldInfo clickedField;
    private bool? shownEnabled;
    private Boss boss;

    private void Awake()
    {
        clickedField = typeof(EndTurnCamera).GetField("hasClicked", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private void OnEnable()
    {
        Wallet.Changed += OnCoins;
        OnCoins(Wallet.Coins);
    }

    /// <summary>Поля назначены после AddComponent (BattleSceneDresser) - показать текущие значения.</summary>
    public void Bind()
    {
        shownCoins = int.MinValue;
        OnCoins(Wallet.Coins);
        shownMana = shownMax = -1;
        shownEnabled = null;
    }

    private void OnDisable() => Wallet.Changed -= OnCoins;

    private void OnCoins(int coins)
    {
        if (coinsText == null) return;
        bool changed = shownCoins != int.MinValue && coins != shownCoins;
        shownCoins = coins;
        coinsText.text = coins.ToString();
        if (changed && Application.isPlaying)
        {
            CombatFx.Punch(coinIcon != null ? coinIcon : coinsText.transform, 0.25f, 0.35f);
            if (coinIcon != null) ImpactFx.Sparkle(coinIcon, UiTheme.Accent, 5, 0.6f);
        }
    }

    private void Update()
    {
        UpdateMana();
        UpdateEndTurn();
        if (deckCount != null && deckCount.text != shownDeck)
        {
            if (shownDeck != null) CombatFx.Punch(deckCount.transform, 0.25f, 0.3f);
            shownDeck = deckCount.text;
        }
    }

    private void UpdateMana()
    {
        if (mana == null || manaPipRow == null) return;
        int cur = Mathf.Max(0, mana.totalMana), max = Mathf.Max(1, mana.maxMana);
        if (cur == shownMana && max == shownMax) return;
        bool spent = shownMana >= 0 && cur < shownMana;
        bool refilled = shownMana >= 0 && cur > shownMana;
        while (pips.Count < max) pips.Add(MakePip(pips.Count));
        for (int i = 0; i < pips.Count; i++)
        {
            pips[i].gameObject.SetActive(i < max);
            bool full = i < cur;
            pips[i].color = full ? UiTheme.Mana : new Color(0.12f, 0.14f, 0.22f, 1f);
            var glow = pips[i].transform.GetChild(0).GetComponent<Image>();
            glow.enabled = full;
            if (Application.isPlaying && spent && i >= cur && i < shownMana) CombatFx.Punch(pips[i].transform, 0.35f, 0.3f);
            if (Application.isPlaying && refilled && full && i >= shownMana) CombatFx.PopIn(pips[i].transform, 0.3f);
        }
        if (Application.isPlaying && manaGem != null && (spent || refilled)) CombatFx.Punch(manaGem.transform, 0.12f, 0.3f);
        shownMana = cur; shownMax = max;
    }

    private Image MakePip(int i)
    {
        var rt = VisualTheme.Centered("Pip" + i, manaPipRow, new Vector2(0, 0.5f), new Vector2(26, 26));
        var le = VisualTheme.Ensure<LayoutElement>(rt.gameObject);
        le.preferredWidth = 26; le.preferredHeight = 26;
        var img = VisualTheme.Img(rt, ProcSprites.Circle, UiTheme.Mana);
        var ol = VisualTheme.Ensure<Outline>(rt.gameObject);
        ol.effectColor = VisualTheme.Outline; ol.effectDistance = new Vector2(2, -2);
        var g = VisualTheme.Img(VisualTheme.Stretch("Glow", rt, -10f), ProcSprites.Glow, new Color(0.55f, 0.8f, 1f, 0.55f));
        g.transform.SetAsFirstSibling();
        var hi = VisualTheme.Img(VisualTheme.Centered("Hi", rt, new Vector2(0.38f, 0.66f), new Vector2(8, 8)), ProcSprites.Circle, new Color(1, 1, 1, 0.7f));
        hi.raycastTarget = false;
        return img;
    }

    private void UpdateEndTurn()
    {
        if (endTurn == null || endTurnFace == null) return;
        bool busy = clickedField != null && (bool)clickedField.GetValue(endTurn);
        if (boss == null) boss = FindAnyObjectByType<Boss>();
        bool over = (boss != null && boss.IsDefeated()) || (Player.Instance != null && Player.Instance.IsDefeated());
        bool enabled = !busy && !over;
        if (shownEnabled != enabled)
        {
            shownEnabled = enabled;
            endTurnFace.color = enabled ? EndTurnOn : EndTurnOff;
            if (endTurnLabel != null)
            {
                endTurnLabel.text = enabled ? "END TURN" : "ENEMY TURN";
                endTurnLabel.color = enabled ? new Color32(0x2A, 0x16, 0x08, 0xFF) : UiTheme.TextDim;
            }
            if (Application.isPlaying && enabled) CombatFx.Punch(endTurnFace.transform.parent, 0.12f, 0.35f);
        }
        if (endTurnGlow != null)
        {
            float a = enabled ? 0.35f + 0.25f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
            endTurnGlow.color = new Color(EndTurnOn.r, EndTurnOn.g, EndTurnOn.b, a);
        }
    }
}
