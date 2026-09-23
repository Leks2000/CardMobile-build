using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scrpits.Shop
{
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

        public CaseDef(string id, string name, int price, Color body, Color bands, Color glow, params (CardRarity, float)[] odds)
        {
            this.id = id; this.name = name; this.price = price; this.body = body; this.bands = bands; this.glow = glow; this.odds = odds;
        }

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

        public static CaseDef Get(string id) => All.FirstOrDefault(c => c.id == id);

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
                var byRarity = CardDatabase.PlayerCards.Where(c => c.rarity == (CardRarity)r && !c.isStarter).ToList();
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
            string.Join(" | ", All.Select(c => $"{c.id} {c.name} {c.price}c [{string.Join(",", c.odds.Select(o => $"{o.rarity}:{o.weight}"))}]")) +
            $" | pools: " + string.Join(",", System.Enum.GetValues(typeof(CardRarity)).Cast<CardRarity>().Select(r => $"{r}={CardDatabase.ShopPool(r).Count()}"));
    }
}
