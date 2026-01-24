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
            if (data.state == DeliveryNpcState.Completed)// 수령 완료인 npc는 생성안하게 해둠
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
