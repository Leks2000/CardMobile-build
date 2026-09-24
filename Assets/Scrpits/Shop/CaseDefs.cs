using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scrpits.Shop
{
    /// <summary>Что выпало из кейса: карта ИЛИ предмет (+ редкость для цвета).</summary>
    public struct CaseDrop
    {
        public CardData card;
        public ItemDef item;

        public CaseDrop(CardData card) { this.card = card; item = null; }
        public CaseDrop(ItemDef item) { card = null; this.item = item; }

        public bool IsEmpty => card == null && item == null;
        public CardRarity Rarity => card != null ? card.rarity : item != null ? item.rarity : CardRarity.Common;
        public string Title => card != null ? card.Title : item != null ? item.name : "???";
        public string Id => card != null ? card.Id : item != null ? item.id : "none";

        /// <summary>Плитка для ленты / показа (карта или предмет).</summary>
        public RectTransform BuildTile(Transform parent, float scale, bool glow = false) =>
            item != null ? ItemTile.Build(parent, item, scale, glow) : CardTile.Build(parent, card, scale, glow);
    }

    /// <summary>Кейс магазина: цена в монетах (Wallet) + таблица дропа по редкостям (веса в %).</summary>
    public class CaseDef
    {
        public string id;
        public string name;
        public int price;
        public Color body;
        public Color bands;
        public Color glow;
        public (CardRarity rarity, float weight)[] odds;
        /// <summary>true - кейс с предметами (расходники + пассивки), false - с картами.</summary>
        public bool items;
        /// <summary>Для кейса предметов: шанс, что выпадет пассивка (если есть неполученные этой редкости).</summary>
        public float passiveChance = 0.35f;

        public CaseDef(string id, string name, int price, Color body, Color bands, Color glow, params (CardRarity, float)[] odds)
        {
            this.id = id; this.name = name; this.price = price; this.body = body; this.bands = bands; this.glow = glow; this.odds = odds;
        }

        /// <summary>Цена с учётом скидок (Golden Ring).</summary>
        public int Price => RelicSystem.ShopPrice(price);

        /// <summary>Случайный дроп этого кейса (карта или предмет).</summary>
        public CaseDrop Roll() => items ? new CaseDrop(ItemDatabase.Roll(RollRarity(), passiveChance)) : new CaseDrop(RollCard());

        public float TotalWeight => odds.Sum(o => o.weight);

        public CardRarity RollRarity()
        {
            float r = Random.value * TotalWeight;
            foreach (var (rarity, w) in odds)
            {
                if (r < w) return rarity;
                r -= w;
            }
            return odds[odds.Length - 1].rarity;
        }

        /// <summary>Случайная карта из этого кейса (редкость по шансам -> карта из пула).</summary>
        public CardData RollCard() => CaseDefs.PickCard(RollRarity());
    }

    /// <summary>
    /// Каталог кейсов и выбор карты по редкости.
    /// Eval: Assets.Scrpits.Shop.CaseDefs.Describe()
    /// </summary>
    public static class CaseDefs
    {
        public static readonly CaseDef[] All =
        {
            new CaseDef("wood", "Wooden Case", 30, new Color32(0x9A, 0x62, 0x34, 0xFF), new Color32(0x5A, 0x5E, 0x66, 0xFF), new Color32(0xFF, 0x9E, 0x5A, 0xFF),
                (CardRarity.Common, 75f), (CardRarity.Rare, 20f), (CardRarity.Epic, 4.5f), (CardRarity.Legendary, 0.5f)),
            new CaseDef("iron", "Iron Case", 75, new Color32(0x7A, 0x86, 0x94, 0xFF), new Color32(0x38, 0x3E, 0x4A, 0xFF), new Color32(0x3D, 0x8B, 0xFF, 0xFF),
                (CardRarity.Rare, 70f), (CardRarity.Epic, 25f), (CardRarity.Legendary, 5f)),
            new CaseDef("royal", "Royal Case", 150, new Color32(0x7A, 0x2C, 0xB8, 0xFF), new Color32(0xFF, 0xC4, 0x4D, 0xFF), new Color32(0xFF, 0xB3, 0x1F, 0xFF),
                (CardRarity.Epic, 70f), (CardRarity.Legendary, 30f)),
        };

        /// <summary>Кейсы с предметами (вкладка ITEMS).</summary>
        public static readonly CaseDef[] Items =
        {
            new CaseDef("pouch", "Traveler's Pouch", 25, new Color32(0x6E, 0x4A, 0x2E, 0xFF), new Color32(0xC8, 0x9A, 0x5A, 0xFF), new Color32(0x9A, 0xE0, 0x6A, 0xFF),
                (CardRarity.Common, 70f), (CardRarity.Rare, 25f), (CardRarity.Epic, 4.5f), (CardRarity.Legendary, 0.5f)) { items = true, passiveChance = 0.25f },
            new CaseDef("satchel", "Alchemist Satchel", 60, new Color32(0x2E, 0x6A, 0x5A, 0xFF), new Color32(0xB8, 0xC4, 0xD0, 0xFF), new Color32(0x3D, 0xE0, 0xC8, 0xFF),
                (CardRarity.Rare, 65f), (CardRarity.Epic, 28f), (CardRarity.Legendary, 7f)) { items = true, passiveChance = 0.4f },
            new CaseDef("relic_chest", "Relic Chest", 130, new Color32(0x2A, 0x24, 0x3A, 0xFF), new Color32(0xFF, 0x7A, 0x3D, 0xFF), new Color32(0xFF, 0x5A, 0x8A, 0xFF),
                (CardRarity.Epic, 65f), (CardRarity.Legendary, 35f)) { items = true, passiveChance = 0.6f },
        };

        public static CaseDef Get(string id) => All.FirstOrDefault(c => c.id == id) ?? Items.FirstOrDefault(c => c.id == id);

        /// <summary>
        /// Карта нужной редкости: ShopPool(rarity) -> любая карта игрока этой редкости ->
        /// ближайшая более низкая редкость -> любая карта игрока (пока ассетов мало).
        /// </summary>
        public static CardData PickCard(CardRarity rarity)
        {
            for (int r = (int)rarity; r >= 0; r--)
            {
                var pool = CardDatabase.ShopPool((CardRarity)r).ToList();
                if (pool.Count > 0) return pool[Random.Range(0, pool.Count)];
                var byRarity = CardDatabase.PlayerCards.Where(c => c.rarity == (CardRarity)r && !c.isStarter && MetaProgress.IsCardUnlocked(c.Id)).ToList();
                if (byRarity.Count > 0) return byRarity[Random.Range(0, byRarity.Count)];
            }
            var any = CardDatabase.PlayerCards.ToList();
            if (any.Count == 0) any = CardDatabase.All.ToList();
            return any.Count > 0 ? any[Random.Range(0, any.Count)] : null;
        }

        public static string OddsText(CaseDef c)
        {
            var lines = new List<string>();
            float total = c.TotalWeight;
            foreach (var (rarity, w) in c.odds)
                lines.Add($"<color=#{ColorUtility.ToHtmlStringRGB(RarityColors.Get(rarity))}>{RarityColors.Name(rarity)}</color>  {w / total * 100f:0.#}%");
            return string.Join("\n", lines);
        }

        public static string Describe() =>
            string.Join(" | ", All.Concat(Items).Select(c => $"{c.id} {c.name} {c.price}c [{string.Join(",", c.odds.Select(o => $"{o.rarity}:{o.weight}"))}]")) +
            $" | pools: " + string.Join(",", System.Enum.GetValues(typeof(CardRarity)).Cast<CardRarity>().Select(r => $"{r}={CardDatabase.ShopPool(r).Count()}"));
    }
}
