using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DataManager
{
    public static void SaveData<T>(T data, string path) where T : class
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

            Debug.Log($"{nameof(data)}데이터를 세이브 했습니다. : {savePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"{nameof(data)}데이터 세이브에 실패했습니다. : {e.Message}");
        }
    }

    public static T LoadData<T>(string path) where T : class
    {
        try
        {
            // 파일 존재 여부 확인
            if (!File.Exists(path))
            {
                Debug.LogWarning($"{path}이라는 파일이 존재하지 않습니다.");
                return null;
            }

            // 데이터 불러오기
            return JsonUtility.FromJson<T>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogError($"{path} 데이터 불러오기에 실패했습니다. : {e.Message}");
            return null;
        }
    }
}
