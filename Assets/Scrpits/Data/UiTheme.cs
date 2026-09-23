using TMPro;
using UnityEngine;

/// <summary>
/// Единый стиль UI: один шрифт + палитра. Все новые экраны берут цвета и шрифт отсюда.
/// Визуальный агент может подкрутить значения, но не переименовывать члены.
/// </summary>
public static class UiTheme
{
    private static TMP_FontAsset font;

    /// <summary>Единый шрифт игры (Chewy).</summary>
    public static TMP_FontAsset Font => font != null ? font : (font = Resources.Load<TMP_FontAsset>("Font/Chewy/Chewy-Regular SDF"));

    // Палитра: тёмное фэнтези, тёплый золотой акцент.
    public static readonly Color Background = new Color32(0x14, 0x11, 0x1C, 0xFF);
    public static readonly Color Panel = new Color32(0x22, 0x1C, 0x2E, 0xF2);
    public static readonly Color PanelLight = new Color32(0x33, 0x2A, 0x44, 0xFF);
    public static readonly Color Accent = new Color32(0xFF, 0xC4, 0x4D, 0xFF);   // золото
    public static readonly Color Text = new Color32(0xF4, 0xEC, 0xDD, 0xFF);
    public static readonly Color TextDim = new Color32(0xA8, 0x9F, 0xB5, 0xFF);
    public static readonly Color Damage = new Color32(0xFF, 0x4D, 0x4D, 0xFF);
    public static readonly Color Heal = new Color32(0x5E, 0xE0, 0x7A, 0xFF);
    public static readonly Color Mana = new Color32(0x4D, 0xA6, 0xFF, 0xFF);
    public static readonly Color Shield = new Color32(0x7F, 0xD1, 0xFF, 0xFF);
    public static readonly Color Poison = new Color32(0x8C, 0xE0, 0x3C, 0xFF);
    public static readonly Color Bleed = new Color32(0xC8, 0x1E, 0x3C, 0xFF);

    public static Color StatusColor(StatusType t) => t switch
    {
        StatusType.Bleed => Bleed,
        StatusType.Poison => Poison,
        StatusType.Shield => Shield,
        StatusType.Lifesteal => new Color32(0xE0, 0x4D, 0xA0, 0xFF),
        StatusType.Thorns => new Color32(0xB0, 0x8A, 0x50, 0xFF),
        _ => Text,
    };

    /// <summary>Короткая подпись статуса для иконки (без новой графики).</summary>
    public static string StatusGlyph(StatusType t) => t switch
    {
        StatusType.Bleed => "B",
        StatusType.Poison => "P",
        StatusType.Shield => "S",
        StatusType.Lifesteal => "L",
        StatusType.Thorns => "T",
        _ => "?",
    };

    public static void Apply(TMP_Text text, Color? color = null)
    {
        if (text == null) return;
        if (Font != null) text.font = Font;
        if (color.HasValue) text.color = color.Value;
    }
}
