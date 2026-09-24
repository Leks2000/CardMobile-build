using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scrpits.Map
{
    public enum MapNodeType { Battle, Elite, Event, Shop, Rest, Boss }

    public class MapNode
    {
        public int id;
        public MapNodeType type;
        /// <summary>Позиция в области карты (0..1, y=0 начало пути, y=1 босс).</summary>
        public Vector2 pos;
        public int[] next;
        public int row;

        public MapNode(int id, MapNodeType type, float x, float y, params int[] next)
        {
            this.id = id; this.type = type; pos = new Vector2(x, y); this.next = next;
        }
    }

    /// <summary>Акт забега: название локации и фон карты (арт из Resources/Images/LocationImages).</summary>
    public class ActInfo
    {
        public string name;
        public string background;
        public Color tint;

        public ActInfo(string name, string background, Color tint)
        {
            this.name = name; this.background = background; this.tint = tint;
        }
    }

    /// <summary>
    /// Карта акта, генерируется по seed (одинаковый seed - одинаковая карта, переживает загрузку сцен).
    /// 7 рядов: старт (2 узла) -> 4 ряда развилок (2-3 узла) -> привал/магазин -> босс.
    /// Из каждого узла 1-2 пути в следующий ряд, в каждый узел входит хотя бы один путь.
    /// </summary>
    public static class MapGraph
    {
        public const int Acts = 3;
        public const int Rows = 7;

        public static readonly ActInfo[] ActInfos =
        {
            new ActInfo("Dark Forest", "Images/LocationImages/Forest/fantasy-enemy-lair-background--dark-forest-clearin", new Color(0.55f, 0.62f, 0.55f)),
            new ActInfo("Hollow Caves", "Images/LocationImages/Forest/-a-dark-fantasy-cave-background-in-hand-drawn-cart", new Color(0.55f, 0.55f, 0.68f)),
            new ActInfo("Haunted Office", "Images/LocationImages/BackBoss/-dark-wooden-cabin-interior-lit-by-candlelight--my", new Color(0.7f, 0.58f, 0.5f)),
        };

        public static MapNode[] Nodes { get; private set; } = new MapNode[0];
        public static int[] StartNodes { get; private set; } = new int[0];
        /// <summary>Максимум узлов в ряду (для размера узлов на экране).</summary>
        public static int MaxPerRow { get; private set; } = 1;
        public static int GeneratedAct { get; private set; }
        public static int GeneratedSeed { get; private set; }

        public static ActInfo Info(int act) => ActInfos[Mathf.Clamp(act - 1, 0, ActInfos.Length - 1)];

        public static bool TryGet(int id, out MapNode node)
        {
            node = (id >= 0 && id < Nodes.Length) ? Nodes[id] : null;
            return node != null;
        }

        public static bool IsBattle(MapNodeType t) =>
            t == MapNodeType.Battle || t == MapNodeType.Elite || t == MapNodeType.Boss;

        public static IEnumerable<(int from, int to)> Edges()
        {
            foreach (var n in Nodes)
                foreach (var t in n.next)
                    yield return (n.id, t);
        }

        /// <summary>Сгенерировать карту акта (если уже сгенерирована с тем же act/seed - ничего не делает).</summary>
        public static void Ensure(int act, int seed)
        {
            if (Nodes.Length > 0 && GeneratedAct == act && GeneratedSeed == seed) return;
            Generate(act, seed);
        }

        public static void Generate(int act, int seed)
        {
            var rng = new System.Random(seed * 31 + act * 7919);
            var rows = new List<List<MapNodeType>>();

            rows.Add(new List<MapNodeType> { MapNodeType.Battle, MapNodeType.Battle });
            for (int r = 1; r <= 4; r++)
            {
                int count = rng.NextDouble() < 0.5 ? 2 : 3;
                var row = new List<MapNodeType>();
                for (int i = 0; i < count; i++) row.Add(RollType(rng, r));
                rows.Add(row);
            }
            rows.Add(new List<MapNodeType> { MapNodeType.Rest, rng.NextDouble() < 0.5 ? MapNodeType.Shop : MapNodeType.Rest });
            rows.Add(new List<MapNodeType> { MapNodeType.Boss });

            // гарантии: есть магазин в середине, есть элита в рядах 3-4, первый ряд развилок - хотя бы один бой
            if (!rows.Skip(1).Take(4).Any(r => r.Contains(MapNodeType.Shop))) rows[2][rng.Next(rows[2].Count)] = MapNodeType.Shop;
            if (!rows[3].Contains(MapNodeType.Elite) && !rows[4].Contains(MapNodeType.Elite)) rows[4][rng.Next(rows[4].Count)] = MapNodeType.Elite;
            if (!rows[1].Contains(MapNodeType.Battle)) rows[1][0] = MapNodeType.Battle;

            // узлы
            var nodes = new List<MapNode>();
            var ids = new List<List<int>>();
            for (int r = 0; r < rows.Count; r++)
            {
                var rowIds = new List<int>();
                int count = rows[r].Count;
                for (int i = 0; i < count; i++)
                {
                    float x = (i + 1f) / (count + 1f);
                    if (count > 1) x += ((float)rng.NextDouble() - 0.5f) * 0.08f;
                    float y = r / (float)(rows.Count - 1);
                    var n = new MapNode(nodes.Count, rows[r][i], x, y) { row = r };
                    rowIds.Add(n.id);
                    nodes.Add(n);
                }
                ids.Add(rowIds);
            }

            // пути
            var next = nodes.ToDictionary(n => n.id, n => new List<int>());
            for (int r = 0; r < rows.Count - 1; r++)
            {
                var from = ids[r];
                var to = ids[r + 1];
                for (int i = 0; i < from.Count; i++)
                {
                    int j = from.Count == 1 ? 0 : Mathf.RoundToInt(i * (to.Count - 1) / (float)(from.Count - 1));
                    if (from.Count == 1)
                    {
                        foreach (var t in to) next[from[i]].Add(t); // из одного узла - во все следующие
                        continue;
                    }
                    next[from[i]].Add(to[j]);
                    // развилка к соседу (без пересечений: только «наружу» от ближайшего)
                    if (rng.NextDouble() < 0.45)
                    {
                        int k = i < from.Count / 2f ? j - 1 : j + 1;
                        if (k >= 0 && k < to.Count && !next[from[i]].Contains(to[k])) next[from[i]].Add(to[k]);
                    }
                }
                // в каждый узел следующего ряда что-то входит
                for (int k = 0; k < to.Count; k++)
                {
                    if (from.Any(f => next[f].Contains(to[k]))) continue;
                    float tx = nodes[to[k]].pos.x;
                    int best = from.OrderBy(f => Mathf.Abs(nodes[f].pos.x - tx)).First();
                    next[best].Add(to[k]);
                }
            }
            foreach (var n in nodes) n.next = next[n.id].OrderBy(t => nodes[t].pos.x).ToArray();

            Nodes = nodes.ToArray();
            StartNodes = ids[0].ToArray();
            MaxPerRow = rows.Max(r => r.Count);
            GeneratedAct = act;
            GeneratedSeed = seed;
            Debug.Log($"[MAP] Act {act} generated (seed {seed}): {string.Join(" | ", rows.Select(r => string.Join(",", r)))}");
        }

        private static MapNodeType RollType(System.Random rng, int row)
        {
            // веса: бой 44, событие 22, элита 14 (не в первом ряду развилок), магазин 10, привал 10
            double x = rng.NextDouble() * 100;
            if (x < 44) return MapNodeType.Battle;
            if (x < 66) return MapNodeType.Event;
            if (x < 80) return row >= 2 ? MapNodeType.Elite : MapNodeType.Battle;
            if (x < 90) return MapNodeType.Shop;
            return MapNodeType.Rest;
        }
    }
}
