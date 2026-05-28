#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class CookingDataExporter : Editor
{
    private const string BaseSOPath = "Assets/Bundles/ScriptableObjects";
    private const string BaseCSVPath = "Assets/Bundles/driveAssets/dataTables";

    [MenuItem("Tools/Export Recipes to SO")]
    public static void ExportAll()
    {
        ExportData<IngredientData>(
            $"{BaseCSVPath}/ingredient.csv",
            $"{BaseSOPath}/IngredientData"
        );

        ExportData<FoodData>(
            $"{BaseCSVPath}/food.csv",
            $"{BaseSOPath}/FoodData"
        );

        ExportData<RecipeData>(
            $"{BaseCSVPath}/recipe.csv",
            $"{BaseSOPath}/RecipeData"
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=cyan><b>[All Data Exported Successfully]</b></color>");
    }

    private static void ExportData<T>(string csvAssetPath, string savePath) where T : ScriptableObject, CsvParsable
    {
        TextAsset csvFile = AssetDatabase.LoadAssetAtPath<TextAsset>(csvAssetPath);
        if (csvFile == null)
        {
            Debug.LogError($"CSV를 찾을 수 없음: {csvAssetPath}");
            return;
        }
        
        ClearFolder(savePath);
        
        string[] lines = csvFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 1; i < lines.Length; i++)
        {
            GenerateSO<T>(savePath ,i, lines[i]);
        }
    }

    private static void ClearFolder(string path)
    {
        if (Directory.Exists(path)) AssetDatabase.DeleteAsset(path);
        Directory.CreateDirectory(path);
        AssetDatabase.Refresh();
    }

    private static T GenerateSO<T>(string savePath, int index, string line) where T : ScriptableObject, CsvParsable
    {
        string[] args = line.Split(',');
        T soInstance = ScriptableObject.CreateInstance<T>();

        try
        {
            soInstance.Init(args);
            string id = typeof(T).GetField("id").GetValue(soInstance).ToString();
            AssetDatabase.CreateAsset(soInstance, $"{savePath}/{id}.asset");
            return soInstance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{typeof(T).Name} 파싱 에러 [{index}라인]: {e.Message}");
            return null;
        }
    }
}
#endif