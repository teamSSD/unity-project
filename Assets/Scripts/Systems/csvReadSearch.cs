using UnityEngine;
using System.Collections.Generic;

public class csvReadSearch : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public string Name;
        public int Score;
        public int Level;
    }

    public List<PlayerData> playerList = new List<PlayerData>();

    void Start()
    {
        // Resources �������� CSV ���� �ҷ�����
        TextAsset csvFile = Resources.Load<TextAsset>("data"); // "data.csv" -> "data"�� Ȯ���� ����

        string[] lines = csvFile.text.Split('\n'); // �� ������ �и�

        // ù ��° ���� ����̹Ƿ� i = 1���� ����
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue; // �� �� �ǳʶٱ�

            string[] values = lines[i].Split(',');

            PlayerData pd = new PlayerData();
            pd.Name = values[0];
            pd.Score = int.Parse(values[1]);
            pd.Level = int.Parse(values[2]);

            playerList.Add(pd);
        }

        // �б� �ڵ�
        foreach (var p in playerList)
            Debug.Log($"�б� �ڵ�: {p.Name} - {p.Score} - {p.Level}");

        // �˻� �ڵ� (���� 100�� �̻�)
        List<PlayerData> filteredList = playerList.FindAll(p => p.Score >= 100); 

        foreach (var p in filteredList) 
            Debug.Log($"�˻� �ڵ�: {p.Name} - {p.Score} - {p.Level}"); 



    }
}
