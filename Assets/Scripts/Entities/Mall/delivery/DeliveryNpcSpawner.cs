using UnityEngine;
using System.Collections.Generic;

public class DeliveryNpcSpawner : MonoBehaviour
{
    [SerializeField] private DeliveryNpcView npcPrefab;
    [SerializeField]private string csvPath = "driveAssets/dataTables/deliveryNPC";


    private readonly List<GameObject> spawnedNpcs = new();

    private void Start()
    {
        SpawnAll();
    }

    public void SpawnAll()
    {
        Clear();

        List<DeliveryNpcCsvData> npcDatas =
            DeliveryNpcCsvLoader.Load(csvPath);

        foreach (var data in npcDatas)
        {
            // 수령 완료는 안 보여줄 수도 있음
            if (data.state == DeliveryNpcState.Completed)
                continue;

            DeliveryNpcView npc =
                Instantiate(npcPrefab, transform);

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
