using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DataManager
{
    string path = Application.persistentDataPath + "/";

    public void SaveData<T>(T data)
    {
        File.WriteAllText(path + nameof(data), JsonUtility.ToJson(data));
    }

    public T LoadData<T>(T data)
    {
        try
        {
            return JsonUtility.FromJson<T>(File.ReadAllText(path + nameof(data)));
        }
        catch (IOException e) {
            Debug.LogException(e);
            return default(T);
        }
    }
}
