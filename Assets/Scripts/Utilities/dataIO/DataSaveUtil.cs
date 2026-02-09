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
            // ���丮 ����
            string filePath = Path.GetDirectoryName(path);
            if (!Directory.Exists(filePath))
                Directory.CreateDirectory(filePath);

            // json ���Ϸ� ����
            string savePath = Path.Combine(filePath, nameof(data) + ".json");
            File.WriteAllText(savePath, JsonUtility.ToJson(data));

            Debug.Log($"{nameof(data)}�����͸� ���̺��߽��ϴ�. : {savePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"{nameof(data)}������ ���̺꿡 �����߽��ϴ�. : {e.Message}");
        }
    }

    public static T LoadData<T>(T data, string path)
    {
        try
        {
            string filePath = Path.GetDirectoryName(path);
            string loadPath = Path.Combine(filePath, nameof(data) + ".json");
            // ���� ���� ���� Ȯ��
            if (!File.Exists(loadPath))
            {
                Debug.LogWarning($"{loadPath}�̶�� ������ �������� �ʽ��ϴ�.");
                return data;
            }

            // ������ �ҷ�����
            Debug.Log($"{nameof(data)}�����͸� �ҷ��Խ��ϴ�. : {loadPath}");
            return JsonUtility.FromJson<T>(File.ReadAllText(loadPath));
        }
        catch (Exception e)
        {
            Debug.LogError($"{path} ������ �ҷ����⿡ �����߽��ϴ�. : {e.Message}");
            return data;
        }
    }

    public static bool HasFile<T>(string path)
{
    try
    {
        string filePath = Path.GetDirectoryName(path);
        string targetPath = Path.Combine(filePath, "data.json"); 

        return File.Exists(targetPath);
    }
    catch (Exception e)
    {
        Debug.LogError($"파일 확인 중 오류 발생: {e.Message}");
        return false;
    }
}
}
