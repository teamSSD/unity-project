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
        // Resources 폴더에서 CSV 파일 불러오기
        TextAsset csvFile = Resources.Load<TextAsset>("data"); // "data.csv" -> "data"로 확장자 제외

        string[] lines = csvFile.text.Split('\n'); // 줄 단위로 분리

        // 첫 번째 줄은 헤더이므로 i = 1부터 시작
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue; // 빈 줄 건너뛰기

            string[] values = lines[i].Split(',');

            PlayerData pd = new PlayerData();
            pd.Name = values[0];
            pd.Score = int.Parse(values[1]);
            pd.Level = int.Parse(values[2]);

            playerList.Add(pd);
        }

        // 읽기 코드
        foreach (var p in playerList)
            Debug.Log($"읽기 코드: {p.Name} - {p.Score} - {p.Level}");

        // 검색 코드 (점수 100점 이상만)
        List<PlayerData> filteredList = playerList.FindAll(p => p.Score >= 100); 

        foreach (var p in filteredList) 
            Debug.Log($"검색 코드: {p.Name} - {p.Score} - {p.Level}"); 



    }
}
