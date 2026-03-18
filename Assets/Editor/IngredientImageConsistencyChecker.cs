using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class IngredientImageConsistencyChecker : EditorWindow
{
    [MenuItem("Tools/Check Ingredient Image Consistency")]
    public static void CheckConsistency()
    {
        var toolMapping = new Dictionary<string, string>
        {
            { "T001", "pan" },
            { "T002", "pot" },
            { "T003", "bowl" },
            { "T004", "cut" },
            { "T005", "plate" }
        };

        // Load all FoodData assets
        string[] guids = AssetDatabase.FindAssets("t:FoodData");
        var missingVariants = new List<string>();
        var successCount = 0;
        int totalIngredients = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FoodData food = AssetDatabase.LoadAssetAtPath<FoodData>(path);

            if (food == null || food.image == null) continue;

            totalIngredients++;
            string baseName = food.image.name.Replace("_raw", "");

            // Check each available tool has a variant image
            foreach (string toolId in food.availableTools)
            {
                if (string.IsNullOrEmpty(toolId)) continue;

                if (!toolMapping.TryGetValue(toolId, out string toolType))
                {
                    Debug.LogWarning($"[ConsistencyCheck] Unknown tool ID '{toolId}' in {food.ingredientName} ({food.id})");
                    continue;
                }

                string variantName = $"{baseName}_{toolType}";
                string variantPath = ResourcePaths.Art.FOOD + variantName;
                Sprite variant = Resources.Load<Sprite>(variantPath);

                if (variant == null)
                {
                    missingVariants.Add($"[{food.id}] {food.ingredientName} → {variantPath}.png (Tool: {toolType}/{toolId})");
                }
                else
                {
                    successCount++;
                }
            }
        }

        // Report results
        Debug.Log("========================================");
        Debug.Log("  Ingredient Image Consistency Check");
        Debug.Log("========================================");

        if (missingVariants.Count == 0)
        {
            Debug.Log($"✅ <color=green><b>ALL VARIANT IMAGES PRESENT!</b></color>");
            Debug.Log($"   Checked {successCount} variants across {totalIngredients} ingredients.");
            Debug.Log("========================================");
        }
        else
        {
            Debug.LogError($"❌ <color=red><b>FOUND {missingVariants.Count} MISSING VARIANT IMAGES:</b></color>");
            Debug.Log($"   ({successCount} variants found, {totalIngredients} total ingredients)");
            Debug.Log("========================================");

            foreach (var missing in missingVariants)
            {
                Debug.LogError($"  - {missing}");
            }

            Debug.Log("========================================");
            Debug.LogWarning("⚠️  Please add the missing variant images or update the ingredient's availableTools list.");
        }
    }
}
