#if UNITY_EDITOR
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

public class SpriteFallbackAutoRegister
{
    [MenuItem("Tools/TMP/Register All Sprite Assets As Fallback")]
    public static void RegisterAllFallbacks()
    {
        // Main Sprite Asset Select
        var main = Selection.activeObject as TMP_SpriteAsset;
        if (main == null)
        {
            Debug.LogError("��ǥ TMP Sprite Asset�� �����ϼ���.");
            return;
        }

        // Main Sprite Asset Path
        string mainPath = AssetDatabase.GetAssetPath(main);
        string folderPath = Path.GetDirectoryName(mainPath);

        // Search Sprite Asset
        var allSpriteAssets = AssetDatabase
            .FindAssets("t:TMP_SpriteAsset", new[] { folderPath })
            .Select(guid => AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(
                AssetDatabase.GUIDToAssetPath(guid)))
            .Where(a => a != null && a != main)
            .ToList();

        if (allSpriteAssets.Count == 0)
        {
            Debug.LogWarning("����� TMP Sprite Asset�� �����ϴ�.");
            return;
        }

        Undo.RecordObject(main, "Register TMP Sprite Fallbacks");

        foreach (var asset in allSpriteAssets)
        {
            if (!main.fallbackSpriteAssets.Contains(asset))
            {
                main.fallbackSpriteAssets.Add(asset);
            }
        }

        EditorUtility.SetDirty(main);
        AssetDatabase.SaveAssets();

        Debug.Log($"��� �Ϸ�: {allSpriteAssets.Count}�� Sprite Asset");
    }
}
#endif
