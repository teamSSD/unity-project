using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

public interface CsvParsable
{
    void Init(string[] fields);
}

public class CsvParsingException : Exception
{
    public CsvParsingException(string message) : base(message) { }
    public CsvParsingException(string message, Exception innerException) : base(message, innerException) { }
}

public static class CsvModelConverter
{
    /// <summary>
    /// TextAsset CSV를 파싱해 T 리스트 반환. CatalogProvider.Csvs.X로 자산 획득 후 전달.
    /// </summary>
    public static List<T> Parse<T>(TextAsset csvAsset) where T : CsvParsable, new()
    {
        List<T> result = new List<T>();

        if (csvAsset == null)
        {
            Debug.LogError("[CsvModelConverter] csvAsset is null");
            return result;
        }
        string[] lines = csvAsset.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

        foreach (string line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                T newOne = new T();
                newOne.Init(line.Split(','));
                result.Add(newOne);
            }
            catch (CsvParsingException e)
            {
                Debug.LogError($"Exception during parsing CSV \"{csvAsset.name}\" - {line}\n{e.Message}");
            }
        }
        return result;
    }
}