using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// Спавн вражеских карт в заднюю линию. [D] Состав задаёт Encounters через <see cref="Configure"/>
/// (пул CardData + общее число врагов за бой); карты создаёт CardFactory.
/// </summary>
public class EnemySpawnCardLogic : MonoBehaviour
{
    [SerializeField] private LineBackMove lineback;
    [SerializeField] private int cardSpawn = 6;
    [SerializeField] private int maxPerRound = 3;
    private Transform slotsParent;
    private List<Transform> imageSlots = new List<Transform>();
    private readonly List<CardData> pool = new List<CardData>();
    private int cardsLeftToSpawn;

    public int CardsLeftToSpawn => cardsLeftToSpawn;

    public void Awake()
    {
        slotsParent = transform;
        foreach (Transform child in slotsParent)
        {
            if (child.name == "Image")
            {
                imageSlots.Add(child);
            }
        }

        cardsLeftToSpawn = cardSpawn;
    }

    /// <summary>Пул врагов (повторы = чаще) и сколько всего врагов выйдет за бой.</summary>
    public void Configure(IEnumerable<CardData> enemies, int total)
    {
        pool.Clear();
        pool.AddRange(enemies);
        cardsLeftToSpawn = total;
    }

    public void SpawnEnemyCardsThisRound()
    {
        int maxCardsThisRound = Mathf.Min(maxPerRound, cardsLeftToSpawn, imageSlots.Count);
        SpawnWave(Random.Range(1, maxCardsThisRound + 1));
    }

    public void SpawnWave(int count)
    {
        if (pool.Count == 0)
        {
            var rat = CardDatabase.Get("RatCard");
            if (rat != null) pool.Add(rat);
        }
        count = Mathf.Min(count, cardsLeftToSpawn);
        if (count <= 0 || pool.Count == 0)
        {
            return;
        }

        int spawned = 0;
        foreach (Transform slot in imageSlots)
        {
            if (slot.childCount == 0 && spawned < count)
            {
                var card = CardFactory.Spawn(pool[Random.Range(0, pool.Count)], slot);
                card.transform.localPosition = Vector3.zero;
                lineback.moveBackLines.Add(slot.gameObject.GetComponent<MoveForward>());

                spawned++;
                cardsLeftToSpawn--;
            }
        }
    }
}
