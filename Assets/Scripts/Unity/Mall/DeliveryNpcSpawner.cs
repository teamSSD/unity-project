using UnityEngine;
using System.Collections.Generic;

public class DeliveryNpcSpawner : MonoBehaviour
{
    [SerializeField] private DeliveryNpcView npcPrefab;

    private readonly List<GameObject> spawnedNpcs = new();

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        Clear();

        var npcDatas = CatalogProvider.DeliveryNpc?.All;
        if (npcDatas == null) return;

        foreach (var data in npcDatas)
        {
            if (data.state == DeliveryNpcState.Completed)
                continue;

            DeliveryNpcView npc = Instantiate(npcPrefab, transform);
            npc.Init(data);
            spawnedNpcs.Add(npc.gameObject);
        }
    }

    private void Clear()
    {
        foreach (var npc in spawnedNpcs)
            Destroy(npc);

        spawnedNpcs.Clear();
    }
}
