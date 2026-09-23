using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scrpits.Map
{
    public enum MapNodeType { Battle, Elite, Event, Shop, Rest, Boss }

    public class MapNode
    {
        public int id;
        public MapNodeType type;
        /// <summary>Позиция в области карты (0..1, y=0 низ).</summary>
        public Vector2 pos;
        public int[] next;

        public MapNode(int id, MapNodeType type, float x, float y, params int[] next)
        {
            this.id = id; this.type = type; pos = new Vector2(x, y); this.next = next;
        }
    }

    /// <summary>
    /// Ручная карта (без процедурной генерации). Снизу вверх:
    /// 0 Battle -> {1 Battle, 2 Event} -> {3 Elite, 4 Shop, 5 Rest} -> 6 Battle -> 7 Boss
    /// </summary>
    public static class MapGraph
    {
        public static readonly MapNode[] Nodes =
        {
            new MapNode(0, MapNodeType.Battle, 0.50f, 0.00f, 1, 2),
            new MapNode(1, MapNodeType.Battle, 0.25f, 0.25f, 3, 4),
            new MapNode(2, MapNodeType.Event,  0.75f, 0.25f, 4, 5),
            new MapNode(3, MapNodeType.Elite,  0.12f, 0.50f, 6),
            new MapNode(4, MapNodeType.Shop,   0.50f, 0.50f, 6),
            new MapNode(5, MapNodeType.Rest,   0.88f, 0.50f, 6),
            new MapNode(6, MapNodeType.Battle, 0.50f, 0.75f, 7),
            new MapNode(7, MapNodeType.Boss,   0.50f, 1.00f),
        };

        public static readonly int[] StartNodes = { 0 };

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
    }
}
