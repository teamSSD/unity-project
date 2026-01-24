using System.Collections.Generic;
using UnityEngine;

public static class DeliveryNpcCsvLoader
{
    public static List<DeliveryNpcCsvData> Load(string csvPath)
    {
        TextAsset csv = Resources.Load<TextAsset>(csvPath);
        List<DeliveryNpcCsvData> list = new();

        string[] lines = csv.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] cols = lines[i].Split(',');

            list.Add(new DeliveryNpcCsvData
            {
                npcId = cols[0],
                characterName = cols[1],
                spritePath = cols[2],
                position = new Vector2(
                    float.Parse(cols[3]),
                    float.Parse(cols[4])
                ),
                state = System.Enum.Parse<DeliveryNpcState>(cols[5])
            });
        }

        return list;
    }
}
