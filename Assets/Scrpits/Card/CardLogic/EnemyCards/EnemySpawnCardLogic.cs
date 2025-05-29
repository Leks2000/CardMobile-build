using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class EnemySpawnCardLogic : MonoBehaviour
{
    [SerializeField] private LineBackMove lineback;
    [SerializeField] private GameObject[] cardPrefabs;
    [SerializeField] private int cardSpawn = 6;
    private Transform slotsParent;
    private List<Transform> imageSlots = new List<Transform>();
    private int cardsLeftToSpawn;
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

    public void SpawnEnemyCardsThisRound()
    {
        if (cardsLeftToSpawn <= 0)
        {
            return;
        }

        int maxCardsThisRound = Mathf.Min(3, cardsLeftToSpawn, imageSlots.Count);
        int cardsToSpawn = Random.Range(1, maxCardsThisRound + 1);

        int spawned = 0;
        foreach (Transform slot in imageSlots)
        {
            if (slot.childCount == 0 && spawned < cardsToSpawn)
            {
                GameObject prefab = cardPrefabs[Random.Range(0, cardPrefabs.Length)];

                GameObject card = Instantiate(prefab, slot);
                card.transform.localPosition = Vector3.zero;
                lineback.moveBackLines.Add(slot.gameObject.GetComponent<MoveForward>());

                spawned++;
                cardsLeftToSpawn--;
            }
        }
    }
}
