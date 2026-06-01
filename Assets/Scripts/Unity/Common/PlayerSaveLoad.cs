using UnityEngine;
using System.IO;

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int score;
}

public class PlayerSaveLoad : MonoBehaviour
{
    public GameObject GameManager; // Inspector에서 연결
    string path;

    void Start()
    {
        path = Application.persistentDataPath + "/save.json";
        LoadPlayerData();
    }

    public void SavePlayerData()
    {
        // GameManager에서 점수 가져오기
        //int currentScore = GameManager.GetComponent<GameManager>().finalScore;
        
        int currentScore = 1234; //임시코드
        string currentName = "jay";

        PlayerData data = new PlayerData
        {
            playerName = currentName,
            score = currentScore
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public void LoadPlayerData()
    {
        if (File.Exists(path))
        {
            string loadedJson = File.ReadAllText(path);
            PlayerData loaded = JsonUtility.FromJson<PlayerData>(loadedJson);

        }
        else
        {
        }
    }
}
