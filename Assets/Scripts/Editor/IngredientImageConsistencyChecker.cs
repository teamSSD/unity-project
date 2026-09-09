using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

public class IngredientImageConsistencyChecker : EditorWindow
{
    private const string FoodDataFolder = "Assets/Bundles/ScriptableObjects/FoodData";
    private const string RecipeDataFolder = "Assets/Bundles/ScriptableObjects/RecipeData";
    private const string FoodCsvPath = "Assets/Bundles/driveAssets/dataTables/food.csv";
    private const float ExpectedPixelsPerUnit = 250f;

    private static readonly HashSet<string> BowlOnlyIngredientIds = new()
    {
        "I023", "I024", "I025"
    };

    private static readonly Dictionary<string, string> ApprovedCriticalSpriteHashes = new()
    {
        {
            "Assets/Bundles/driveAssets/art/item/cooking/food/item_blackSesame_bowl.png",
            "41dbd5f8257a7fdce929b5d6d9a49c66da209ae4bea85cd70e627dcfa781d828"
        },
        {
            "Assets/Bundles/driveAssets/art/item/cooking/food/item_cheongyangLeaf_bowl.png",
            "a77a638d9121df5626e33e59990575050af7d412b2572cbc1652df8e9fe8c0d6"
        },
        {
            "Assets/Bundles/driveAssets/art/item/cooking/food/item_caramelTopping_plate.png",
            "18bdeb49a14c0b321858a8b988b3487fc78fd029218a16bdb20c8f0f570c5c4c"
        }
    };

    private static readonly Dictionary<string, (int index, string suffix)> ToolVariants = new()
    {
        { "T001", (0, "_pan") },
        { "T002", (1, "_pot") },
        { "T003", (2, "_bowl") },
        { "T004", (3, "_cut") },
        { "T005", (4, "_plate") }
    };

    private static readonly Dictionary<string, string> MinigameTools = new()
    {
        { "M001", "T001" },
        { "M002", "T002" },
        { "M004", "T003" },
        { "M005", "T003" },
        { "M006", "T004" },
        { "M007", "T005" }
    };

    [MenuItem("Tools/Check Ingredient Image Consistency")]
    public static void CheckConsistency()
    {
        var violations = CollectViolations();

        Debug.Log("========================================");
        Debug.Log("  Cooking Asset Consistency Check");
        Debug.Log("========================================");

        if (violations.Count == 0)
        {
            Debug.Log("✅ <color=green><b>ALL COOKING ASSET CONTRACTS PASSED.</b></color>");
        }
        else
        {
            Debug.LogError($"❌ <color=red><b>FOUND {violations.Count} COOKING ASSET VIOLATION(S):</b></color>");
            foreach (var violation in violations)
                Debug.LogError($"  - {violation}");
        }

        Debug.Log("========================================");
    }

    public static IReadOnlyList<string> CollectViolations()
    {
        var violations = new List<string>();
        var foods = LoadAssets<FoodData>(FoodDataFolder);
        var foodsById = foods
            .Where(food => food != null && !string.IsNullOrWhiteSpace(food.id))
            .GroupBy(food => food.id)
            .ToDictionary(group => group.Key, group => group.First());

        ValidateCsvTools(foodsById, violations);
        ValidateBowlOnlyIngredients(foodsById, violations);
        ValidateIngredientVariants(foods, violations);
        ValidateRecipeInputs(violations);
        ValidateCriticalSpriteFingerprints(violations);
        return violations;
    }

    private static void ValidateBowlOnlyIngredients(
        IReadOnlyDictionary<string, FoodData> foodsById,
        ICollection<string> violations)
    {
        foreach (string foodId in BowlOnlyIngredientIds)
        {
            if (!foodsById.TryGetValue(foodId, out var food))
            {
                violations.Add($"[{foodId}] Bowl-only FoodData asset is missing.");
                continue;
            }

            var tools = NormalizeTools(food.availableTools);
            if (!tools.SequenceEqual(new[] { "T003" }))
            {
                violations.Add(
                    $"[{foodId}] Must remain bowl-only (T003), actual=[{string.Join(",", tools)}].");
            }
        }
    }

    private static void ValidateCsvTools(
        IReadOnlyDictionary<string, FoodData> foodsById,
        ICollection<string> violations)
    {
        string foodCsvFile = Path.Combine(
            Application.dataPath,
            "Bundles",
            "driveAssets",
            "dataTables",
            "food.csv");

        if (!File.Exists(foodCsvFile))
        {
            violations.Add($"Food CSV not found: {FoodCsvPath}");
            return;
        }

        foreach (var line in File.ReadLines(foodCsvFile).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var fields = line.Split(',');
            if (fields.Length != 6)
            {
                violations.Add($"Malformed food.csv row: {line}");
                continue;
            }

            string foodId = fields[0].Trim();
            if (!foodsById.TryGetValue(foodId, out var food))
            {
                violations.Add($"[{foodId}] FoodData asset is missing.");
                continue;
            }

            var csvTools = SplitTools(fields[4]);
            var assetTools = NormalizeTools(food.availableTools);
            if (!csvTools.SequenceEqual(assetTools))
            {
                violations.Add(
                    $"[{foodId}] availableTools drift: CSV=[{string.Join(",", csvTools)}], " +
                    $"FoodData=[{string.Join(",", assetTools)}]");
            }
        }
    }

    private static void ValidateIngredientVariants(
        IEnumerable<FoodData> foods,
        ICollection<string> violations)
    {
        foreach (var food in foods.Where(food => food != null && food.type == FoodType.INGREDIENT))
        {
            if (food.image == null)
            {
                violations.Add($"[{food.id}] Base image is missing.");
                continue;
            }

            string baseName = food.image.name.EndsWith("_raw", StringComparison.Ordinal)
                ? food.image.name[..^4]
                : food.image.name;

            foreach (string toolId in NormalizeTools(food.availableTools))
            {
                if (!ToolVariants.TryGetValue(toolId, out var definition))
                {
                    violations.Add($"[{food.id}] Unknown tool id: {toolId}");
                    continue;
                }

                if (food.toolVariants == null || food.toolVariants.Length <= definition.index)
                {
                    violations.Add($"[{food.id}] toolVariants has no slot for {toolId}.");
                    continue;
                }

                var sprite = food.toolVariants[definition.index];
                if (sprite == null)
                {
                    violations.Add($"[{food.id}] Missing assigned sprite for {toolId} ({definition.suffix}).");
                    continue;
                }

                string spritePath = AssetDatabase.GetAssetPath(sprite);
                string expectedName = baseName + definition.suffix;
                string actualName = Path.GetFileNameWithoutExtension(spritePath);
                if (!string.Equals(expectedName, actualName, StringComparison.Ordinal))
                {
                    violations.Add(
                        $"[{food.id}] {toolId} points to '{actualName}', expected '{expectedName}'.");
                }

                if (AssetImporter.GetAtPath(spritePath) is TextureImporter importer &&
                    !Mathf.Approximately(importer.spritePixelsPerUnit, ExpectedPixelsPerUnit))
                {
                    violations.Add(
                        $"[{food.id}] '{actualName}' PPU is {importer.spritePixelsPerUnit}, " +
                        $"expected {ExpectedPixelsPerUnit}.");
                }
            }
        }
    }

    private static void ValidateRecipeInputs(ICollection<string> violations)
    {
        foreach (var recipe in LoadAssets<RecipeData>(RecipeDataFolder))
        {
            if (recipe == null || string.IsNullOrWhiteSpace(recipe.minigameId)) continue;
            if (!MinigameTools.TryGetValue(recipe.minigameId, out string toolId))
            {
                violations.Add($"[{recipe.id}] Unknown minigame id: {recipe.minigameId}");
                continue;
            }

            ValidateRecipeToolVariant(recipe, recipe.outputFood, toolId, "output", violations);

            foreach (var input in recipe.inputs ?? new List<RecipeIngredient>())
            {
                var food = input.food;
                if (food == null)
                {
                    violations.Add($"[{recipe.id}] Recipe input is missing a FoodData reference.");
                    continue;
                }

                if (food.availableTools == null || !food.availableTools.Contains(toolId))
                {
                    violations.Add(
                        $"[{recipe.id}] Input {food.id} does not allow recipe tool {toolId}.");
                }

                if (BowlOnlyIngredientIds.Contains(food.id) && toolId != "T003")
                {
                    violations.Add(
                        $"[{recipe.id}] Bowl-only input {food.id} is used with {toolId}.");
                }

                ValidateRecipeToolVariant(recipe, food, toolId, "input", violations);
            }
        }
    }

    private static void ValidateRecipeToolVariant(
        RecipeData recipe,
        FoodData food,
        string toolId,
        string role,
        ICollection<string> violations)
    {
        if (food == null || !ToolVariants.TryGetValue(toolId, out var definition)) return;

        if (food.toolVariants == null ||
            food.toolVariants.Length <= definition.index ||
            food.toolVariants[definition.index] == null)
        {
            violations.Add(
                $"[{recipe.id}] {role} {food.id} is missing {toolId} ({definition.suffix}); " +
                "runtime would fall back to the raw image.");
        }
    }

    private static void ValidateCriticalSpriteFingerprints(ICollection<string> violations)
    {
        foreach (var contract in ApprovedCriticalSpriteHashes)
        {
            string filePath = Path.Combine(
                Application.dataPath,
                contract.Key["Assets/".Length..].Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(filePath))
            {
                violations.Add($"Critical sprite is missing: {contract.Key}");
                continue;
            }

            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            string actualHash = BitConverter.ToString(sha256.ComputeHash(stream))
                .Replace("-", string.Empty)
                .ToLowerInvariant();

            if (!string.Equals(actualHash, contract.Value, StringComparison.Ordinal))
            {
                violations.Add(
                    $"Critical sprite content changed: {contract.Key} " +
                    $"(expected {contract.Value}, actual {actualHash}).");
            }
        }
    }

    private static List<T> LoadAssets<T>(string folder) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToList();
    }

    private static string[] SplitTools(string value)
    {
        return value.Split('/')
            .Select(tool => tool.Trim())
            .Where(tool => !string.IsNullOrWhiteSpace(tool))
            .Distinct()
            .OrderBy(tool => tool, StringComparer.Ordinal)
            .ToArray();
    }

    private static string[] NormalizeTools(IEnumerable<string> tools)
    {
        return (tools ?? Enumerable.Empty<string>())
            .Select(tool => tool?.Trim())
            .Where(tool => !string.IsNullOrWhiteSpace(tool))
            .Distinct()
            .OrderBy(tool => tool, StringComparer.Ordinal)
            .ToArray();
    }
}
