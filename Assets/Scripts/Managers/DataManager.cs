using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlayerData
{
    public string name;
    public int gold;
    public int stamina;
}

public class DataManager : MonoBehaviour
{
    // Singleton
    public static DataManager instance;
    PlayerData nowPlayer = new PlayerData();
    string path;
    string filename = "Save";

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else if (instance != this) { Destroy(instance.gameObject); }
        DontDestroyOnLoad(this.gameObject);

        // Application.persistentDataPath = C:\Users\"UserName"\AppData\LocalLow\DefaultCompany\"Project Name"
        path = Application.persistentDataPath + "/";
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void SaveData()
    {
        string data = JsonUtility.ToJson(nowPlayer);
        File.WriteAllText(path + filename, data);
    }

    public void LoadData()
    {
        string data = File.ReadAllText(path + filename);
        nowPlayer = JsonUtility.FromJson<PlayerData>(data);
    }
}
