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
            Debug.LogError("대표 TMP Sprite Asset을 선택하세요.");
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
            Debug.LogWarning("등록할 TMP Sprite Asset이 없습니다.");
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

        Debug.Log($"등록 완료: {allSpriteAssets.Count}개 Sprite Asset");
    }
}
#endif
