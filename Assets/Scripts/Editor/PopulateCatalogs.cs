#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Game.Schema.Catalog;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 일회성 도구: catalog SO 에셋 자동 생성 + 모든 해당 자산을 인스펙터 등록.
/// Tools/Catalog/Populate All Catalogs 메뉴로 실행.
/// Phase 2-D 마이그레이션 완료 후 이 파일은 삭제 가능 (또는 신규 자산 추가 시 재실행).
/// </summary>
public static class PopulateCatalogs
{
    private const string CatalogFolder = "Assets/Bundles/Catalogs";

    [MenuItem("Tools/Catalog/Populate Crop Sprites")]
    public static void PopulateCropSprites()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CropSpriteCatalogSO>($"{CatalogFolder}/CropSpriteCatalog.asset");
        if (catalog == null) { Debug.LogError("[PopulateCatalogs] CropSpriteCatalog not found"); return; }
        var csv = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Bundles/driveAssets/dataTables/crop_data.csv");
        if (csv == null) { Debug.LogError("[PopulateCatalogs] crop_data.csv not found"); return; }

        var entries = new List<(string key, Sprite sprite)>();
        var lines = csv.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            var parts = line.Split(',');
            if (parts.Length < 4) continue;
            var key = parts[3].Trim();
            // Resources.Load 우선 시도 (이전 위치), 실패 시 Bundles AssetDatabase 시도
            var sprite = Resources.Load<Sprite>(key);
            if (sprite == null)
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Bundles/{key}.png");
            if (sprite == null) Debug.LogWarning($"[PopulateCatalogs] Crop sprite not found: {key}");
            entries.Add((key, sprite));
        }

        var so = new SerializedObject(catalog);
        var entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();
        entriesProp.arraySize = entries.Count;
        for (int i = 0; i < entries.Count; i++)
        {
            var el = entriesProp.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("key").stringValue = entries[i].key;
            el.FindPropertyRelative("sprite").objectReferenceValue = entries[i].sprite;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PopulateCatalogs] CropSpriteCatalog populated with {entries.Count} entries");
    }

    [MenuItem("Tools/Catalog/Populate All Catalogs")]
    public static void PopulateAll()
    {
        EnsureFolder(CatalogFolder);

        var food            = EnsureCatalog<FoodCatalogSO>("FoodCatalog");
        var recipe          = EnsureCatalog<RecipeCatalogSO>("RecipeCatalog");
        var ingredient      = EnsureCatalog<IngredientCatalogSO>("IngredientCatalog");
        var cookingTool     = EnsureCatalog<CookingToolCatalogSO>("CookingToolCatalog");
        var deliveryNpc     = EnsureCatalog<DeliveryNpcCatalogSO>("DeliveryNpcCatalog");
        var dialogueConfig  = EnsureCatalog<DialogueConfigCatalogSO>("DialogueConfigCatalog");

        Populate(food,        FindAll<FoodData>());
        Populate(recipe,      FindAll<RecipeData>());
        Populate(ingredient,  FindAll<IngredientData>());
        Populate(cookingTool, FindAll<CookingToolData>());
        Populate(deliveryNpc, FindAll<DeliveryNpcData>());
        PopulateDialogueConfig(dialogueConfig, FindAll<DeliveryDialogueConfig>());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PopulateCatalogs] Done. " +
                  $"Food={food.Count}, Recipe={recipe.Count}, Ingredient={ingredient.Count}, " +
                  $"CookingTool={cookingTool.Count}, DeliveryNpc={deliveryNpc.Count}, " +
                  $"DialogueConfig={dialogueConfig.Count}");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static T EnsureCatalog<T>(string assetName) where T : ScriptableObject
    {
        var assetPath = $"{CatalogFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null) return existing;

        var inst = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(inst, assetPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[PopulateCatalogs] Created {assetPath}");
        return inst;
    }

    private static List<T> FindAll<T>() where T : Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        var list = new List<T>(guids.Length);
        foreach (var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            var a = AssetDatabase.LoadAssetAtPath<T>(p);
            if (a != null) list.Add(a);
        }
        return list;
    }

    private static void Populate<T>(CatalogSO<T> catalog, List<T> assets) where T : Object
    {
        var so = new SerializedObject(catalog);
        var itemsProp = so.FindProperty("items");
        itemsProp.ClearArray();
        itemsProp.arraySize = assets.Count;
        for (int i = 0; i < assets.Count; i++)
        {
            itemsProp.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
    }

    private static void PopulateDialogueConfig(DialogueConfigCatalogSO catalog, List<DeliveryDialogueConfig> configs)
    {
        // 각 DeliveryDialogueConfig 자산의 경로에서 groupId 추출.
        // 기존 Resources/ScriptableObjects/Dialogue/{groupId}/Config.asset 패턴
        var so = new SerializedObject(catalog);
        var entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();

        var added = 0;
        foreach (var cfg in configs)
        {
            var path = AssetDatabase.GetAssetPath(cfg);
            // 폴더 이름 = groupId 추출
            var parts = path.Split('/');
            string groupId = null;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (parts[i] == "Dialogue" && i + 1 < parts.Length - 1)
                {
                    groupId = parts[i + 1];
                    break;
                }
            }
            if (string.IsNullOrEmpty(groupId))
            {
                Debug.LogWarning($"[PopulateCatalogs] DialogueConfig at {path} has no detectable groupId; skipped.");
                continue;
            }

            entriesProp.arraySize = added + 1;
            var el = entriesProp.GetArrayElementAtIndex(added);
            el.FindPropertyRelative("groupId").stringValue = groupId;
            el.FindPropertyRelative("config").objectReferenceValue = cfg;
            added++;
        }
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
    }
}
#endif
