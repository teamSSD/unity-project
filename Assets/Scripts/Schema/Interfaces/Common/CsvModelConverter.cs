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
    public static List<T> Parse<T>(string resourcePath) where T : CsvParsable, new()
    {
        List<T> result = new List<T>();

        TextAsset csvFile = Resources.Load<TextAsset>(resourcePath);
        if (csvFile == null)
        {
            Debug.LogError($"[CsvModelConverter] CSV file not found: {resourcePath}");
            return result;
        }
        string[] lines = csvFile.text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

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
                Debug.LogError($"Exception occured during parsing csv file \"{resourcePath}\" - {line}\n{e.Message}");
            }
        }
        return result;
    }
}