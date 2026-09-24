using System;
using System.Collections.Generic;
using Assets.Scrpits.Run;
using Assets.Scrpits.Shop;
using UnityEngine;

namespace Assets.Scrpits.Map
{
    /// <summary>Итог выбора на узле: текст для попапа + (опционально) полученная карта.</summary>
    public struct NodeResult
    {
        public string title;
        public string text;
        public CardData card;
        public NodeResult(string title, string text, CardData card = null) { this.title = title; this.text = text; this.card = card; }
    }

    public class NodeChoice
    {
        public string label;
        public string detail;
        public Func<bool> enabled;
        public Func<NodeResult> apply;
        /// <summary>Выбор требует карты из колоды: MapController открывает DeckPicker и передаёт индекс карты.</summary>
        public Func<int, NodeResult> pick;
        public Func<CardData, bool> pickFilter;

        public NodeChoice(string label, string detail, Func<NodeResult> apply, Func<bool> enabled = null)
        {
            this.label = label; this.detail = detail; this.apply = apply; this.enabled = enabled;
        }

        public bool IsEnabled => enabled == null || enabled();
    }

    public class NodeScenario
    {
        public string title;
        public string body;
        public List<NodeChoice> choices = new List<NodeChoice>();
    }

    /// <summary>
    /// Содержимое небоевых узлов: случайные события (3 шт.) и привал.
    /// Награды - через Wallet / RelicDatabase.Grant / RunState.AddCard / RunState.Heal.
    /// </summary>
    public static class MapEvents
    {
        public const int RestHealPercent = 30;

        private static int Pct(int percent) => RunState.PlayerHPMax > 0 ? Mathf.Max(1, Mathf.CeilToInt(RunState.PlayerHPMax * percent / 100f)) : 0;
        private static bool HpFull => RunState.PlayerHP >= RunState.PlayerHPMax;

        private static string C(Color c, string s) => $"<color=#{UiKit.Hex(c)}>{s}</color>";

        public static NodeResult Leave(string title) => new NodeResult(title, "You move on.");

        public static NodeScenario Rest()
        {
            int heal = Pct(RestHealPercent);
            var s = new NodeScenario
            {
                title = "Campfire",
                body = "A quiet fire crackles in the dark.\nTake a moment before the next fight."
            };
            s.choices.Add(new NodeChoice("Rest", C(UiTheme.Heal, $"Heal {heal} HP") + $" ({RestHealPercent}%)", () =>
            {
                int healed = RunState.Heal(heal);
                return new NodeResult("Rested", healed > 0 ? $"You feel better.\n{C(UiTheme.Heal, $"+{healed} HP")}" : "You were already at full health.");
            }, () => !HpFull));
            s.choices.Add(new NodeChoice("Train", "Promote a card: " + C(CardUpgrade.Gold, "Senior +1 ATK +1 HP"), null, CardUpgrade.AnyUpgradable)
            {
                pickFilter = CardUpgrade.CanUpgrade,
                pick = idx =>
                {
                    var card = CardUpgrade.UpgradeDeckCard(idx);
                    return card != null
                        ? new NodeResult("Promoted!", $"{C(CardUpgrade.Gold, card.Title)} is stronger now.", card)
                        : new NodeResult("Campfire", "Nothing to train.");
                }
            });
            s.choices.Add(new NodeChoice("Search the camp", "Free card from a " + C(new Color32(0xFF, 0x9E, 0x5A, 0xFF), "Wooden Case"), () =>
            {
                var card = CaseDefs.Get("wood").RollCard();
                RunState.AddCard(card);
                return new NodeResult("Found something!", card != null
                    ? $"{C(RarityColors.Get(card.rarity), card.Title)} joins your deck."
                    : "Nothing but ashes.", card);
            }));
            return s;
        }

        /// <summary>Случайное событие.</summary>
        public static NodeScenario RandomEvent()
        {
            var all = new Func<NodeScenario>[] { BloodAltar, Wagon, WishingWell, Peddler };
            return all[UnityEngine.Random.Range(0, all.Length)]();
        }

        public static NodeScenario ByName(string name) => name switch
        {
            "altar" => BloodAltar(),
            "wagon" => Wagon(),
            "well" => WishingWell(),
            "peddler" => Peddler(),
            _ => RandomEvent(),
        };

        private static NodeScenario BloodAltar()
        {
            int cost = Mathf.Max(2, Pct(30));
            const int fallbackCoins = 45;
            var s = new NodeScenario
            {
                title = "Blood Altar",
                body = "A cracked altar hums with dark power.\nIt thirsts for blood..."
            };
            bool hasRelic = RelicDatabase.RandomNotOwned() != null;
            s.choices.Add(new NodeChoice("Offer blood", C(UiTheme.Damage, $"Pay {cost} HP") + "  ->  " + (hasRelic ? C(UiTheme.Accent, "gain a relic") : C(UiTheme.Accent, $"{fallbackCoins} coins")), () =>
            {
                int lost = RunState.Damage(cost);
                var relic = RelicDatabase.RandomNotOwned();
                if (relic != null && RelicDatabase.Grant(relic.id))
                    return new NodeResult("The altar accepts", $"{C(UiTheme.Damage, $"-{lost} HP")}\nRelic: {C(RarityColors.Get(relic.rarity), relic.name)}\n<size=80%>{relic.description}</size>");
                Wallet.Add(fallbackCoins);
                RunState.CoinsEarned += fallbackCoins;
                return new NodeResult("The altar accepts", $"{C(UiTheme.Damage, $"-{lost} HP")}\nGold spills from the cracks: {C(UiTheme.Accent, $"+{fallbackCoins} coins")}");
            }, () => RunState.PlayerHP > cost));
            s.choices.Add(new NodeChoice("Leave", "Walk away", () => Leave("Blood Altar")));
            return s;
        }

        private static NodeScenario Wagon()
        {
            const int coins = 30;
            int trap = Mathf.Max(1, Pct(20));
            var s = new NodeScenario
            {
                title = "Abandoned Wagon",
                body = "A merchant's wagon lies overturned on the road.\nNobody is around."
            };
            s.choices.Add(new NodeChoice("Take the purse", C(UiTheme.Accent, $"+{coins} coins"), () =>
            {
                Wallet.Add(coins);
                RunState.CoinsEarned += coins;
                return new NodeResult("Lucky find", $"{C(UiTheme.Accent, $"+{coins} coins")}");
            }));
            s.choices.Add(new NodeChoice("Search the crates", "50%: " + C(RarityColors.Get(CardRarity.Rare), "Rare card") + "   50%: " + C(UiTheme.Damage, $"-{trap} HP"), () =>
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    var card = CaseDefs.PickCard(CardRarity.Rare);
                    RunState.AddCard(card);
                    return new NodeResult("Treasure!", card != null ? $"{C(RarityColors.Get(card.rarity), card.Title)} joins your deck." : "The crates are empty.", card);
                }
                int lost = RunState.Damage(trap);
                return new NodeResult("It's a trap!", $"A rat bites you.\n{C(UiTheme.Damage, $"-{lost} HP")}");
            }, () => RunState.PlayerHP > trap));
            s.choices.Add(new NodeChoice("Leave", "Walk away", () => Leave("Abandoned Wagon")));
            return s;
        }

        /// <summary>[Items] Бродячий торговец: предметы за монеты или за кровь.</summary>
        private static NodeScenario Peddler()
        {
            const int price = 15;
            int blood = Mathf.Max(2, Pct(15));
            var s = new NodeScenario
            {
                title = "Wandering Peddler",
                body = "A hooded figure opens a coat full of vials.\n\"Something for the road, friend?\""
            };
            s.choices.Add(new NodeChoice("Mystery vial", C(UiTheme.Accent, $"Pay {price} coins") + "  ->  random " + C(RarityColors.Get(CardRarity.Rare), "item"), () =>
            {
                if (!Wallet.TrySpend(price)) return new NodeResult("Wandering Peddler", "Not enough coins.");
                var item = ItemDatabase.RollConsumable(CardRarity.Rare);
                ItemDatabase.Grant(item);
                return new NodeResult("A fine choice", $"{C(RarityColors.Get(item.rarity), item.name)} goes into your bag.\n<size=80%>{item.description}</size>");
            }, () => Wallet.Coins >= price));
            s.choices.Add(new NodeChoice("Pay in blood", C(UiTheme.Damage, $"Pay {blood} HP") + "  ->  " + C(RarityColors.Get(CardRarity.Epic), "rare item or relic"), () =>
            {
                int lost = RunState.Damage(blood);
                var item = ItemDatabase.Roll(UnityEngine.Random.value < 0.3f ? CardRarity.Epic : CardRarity.Rare, 0.4f);
                ItemDatabase.Grant(item);
                return new NodeResult("The deal is done", $"{C(UiTheme.Damage, $"-{lost} HP")}\n{C(RarityColors.Get(item.rarity), item.name)}" +
                    (item.IsPassive ? " (passive)" : " goes into your bag.") + $"\n<size=80%>{item.description}</size>");
            }, () => RunState.PlayerHP > blood));
            s.choices.Add(new NodeChoice("Leave", "Walk away", () => Leave("Wandering Peddler")));
            return s;
        }

        private static NodeScenario WishingWell()
        {
            const int toss = 20;
            int heal = Pct(35);
            var s = new NodeScenario
            {
                title = "Wishing Well",
                body = "Coins glitter at the bottom of an old well.\nThe water smells of magic."
            };
            s.choices.Add(new NodeChoice("Drink", C(UiTheme.Heal, $"Heal {heal} HP"), () =>
            {
                int healed = RunState.Heal(heal);
                return new NodeResult("Refreshing", healed > 0 ? C(UiTheme.Heal, $"+{healed} HP") : "You were already at full health.");
            }, () => !HpFull));
            s.choices.Add(new NodeChoice("Make a wish", C(UiTheme.Accent, $"Pay {toss} coins") + "  ->  " + C(RarityColors.Get(CardRarity.Rare), "Iron Case card"), () =>
            {
                if (!Wallet.TrySpend(toss)) return new NodeResult("Wishing Well", "Not enough coins.");
                var card = CaseDefs.Get("iron").RollCard();
                RunState.AddCard(card);
                return new NodeResult("Wish granted!", card != null ? $"{C(RarityColors.Get(card.rarity), card.Title)} joins your deck." : "Nothing happens.", card);
            }, () => Wallet.Coins >= toss));
            s.choices.Add(new NodeChoice("Leave", "Walk away", () => Leave("Wishing Well")));
            return s;
        }
    }
}
