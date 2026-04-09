using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DataSaveUtil
{
    public static void SaveData<T>(T data, string path)
    {
        try
        {
            // 전체 경로를 파일명으로 사용
            string savePath = path + ".json";

            // 디렉토리 생성
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // json 파일로 저장
            File.WriteAllText(savePath, JsonUtility.ToJson(data));
        }
        catch (IOException e)
        {
            Debug.LogError($"{typeof(T).Name} 저장 실패: {e.Message}");
        }
    }

    public static T LoadData<T>(T data, string path)
    {
        try
        {
            // 전체 경로를 파일명으로 사용
            string loadPath = path + ".json";

            // 파일 존재 여부 확인
            if (!File.Exists(loadPath))
            {
                return data;
            }

            // 데이터 불러오기
            return JsonUtility.FromJson<T>(File.ReadAllText(loadPath));
        }
        catch (Exception e)
        {
            Debug.LogError($"{typeof(T).Name} 로드 실패 ({path}): {e.Message}");
            return data;
        }
    }

    public static bool HasFile<T>(string path)
    {
        try
        {
            // 전체 경로를 파일명으로 사용
            string targetPath = path + ".json";

            return File.Exists(targetPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 확인 중 오류 발생: {e.Message}");
            return false;
        }
    }
}
