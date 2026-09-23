using UnityEngine;

/// <summary>
/// Редкость карты. Цвет редкости используется рамкой карты, магазином и кейсами.
/// </summary>
public enum CardRarity { Common, Rare, Epic, Legendary }

public static class RarityColors
{
    public static Color Get(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare => new Color32(0x3D, 0x8B, 0xFF, 0xFF),
        CardRarity.Epic => new Color32(0xA3, 0x4B, 0xFF, 0xFF),
        CardRarity.Legendary => new Color32(0xFF, 0xB3, 0x1F, 0xFF),
        _ => new Color32(0x9A, 0xA3, 0xAD, 0xFF),
    };

    public static string Name(CardRarity rarity) => rarity switch
    {
        CardRarity.Rare => "Rare",
        CardRarity.Epic => "Epic",
        CardRarity.Legendary => "Legendary",
        _ => "Common",
    };
}
