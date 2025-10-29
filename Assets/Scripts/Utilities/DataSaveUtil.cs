using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DataManager
{
    public static void SaveData<T>(T data, string path)
    {
        try
        {
            // 디렉토리 생성
            string filePath = Path.GetDirectoryName(path);
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            // json 파일로 저장
            string savePath = Path.Combine(filePath, nameof(data) + ".json");
            File.WriteAllText(savePath, JsonUtility.ToJson(data));

            Debug.Log($"{nameof(data)}데이터를 세이브했습니다. : {savePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"{nameof(data)}데이터 세이브에 실패했습니다. : {e.Message}");
        }
    }

    public static T LoadData<T>(T data, string path)
    {
        try
        {
            string filePath = Path.GetDirectoryName(path);
            string loadPath = Path.Combine(filePath, nameof(data) + ".json");
            // 파일 존재 여부 확인
            if (!File.Exists(loadPath))
            {
                Debug.LogWarning($"{loadPath}이라는 파일이 존재하지 않습니다.");
                return data;
            }

            // 데이터 불러오기
            Debug.Log($"{nameof(data)}데이터를 불러왔습니다. : {loadPath}");
            return JsonUtility.FromJson<T>(File.ReadAllText(loadPath));
        }
        catch (Exception e)
        {
            Debug.LogError($"{path} 데이터 불러오기에 실패했습니다. : {e.Message}");
            return data;
        }
    }
}
