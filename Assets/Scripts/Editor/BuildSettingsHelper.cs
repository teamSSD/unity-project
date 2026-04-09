#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Adds missing scenes to Build Settings and fixes corrupted scene .meta files
/// </summary>
public class BuildSettingsHelper
{
    [MenuItem("Tools/Add Missing Scenes to Build Settings")]
    public static void AddMissingScenes()
    {
        EditorUtility.DisplayProgressBar("Build Settings", "Fixing scene metadata...", 0.2f);

        // Step 1: Fix .meta files
        string[] scenesToFix = {
            "Assets/Scenes/ForReal/Scene_Delivery.unity",
            "Assets/Scenes/ForReal/Scene_Shop.unity"
        };

        foreach (var scenePath in scenesToFix)
        {
            FixSceneMetaFile(scenePath);
        }

        EditorUtility.DisplayProgressBar("Build Settings", "Adding scenes...", 0.6f);

        // Step 2: Add to build settings
        var originalScenes = EditorBuildSettings.scenes.ToList();
        int addedCount = 0;

        foreach (var scenePath in scenesToFix)
        {
            if (!originalScenes.Any(s => s.path == scenePath))
            {
                originalScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                addedCount++;
                Debug.Log($"[BuildSettingsHelper] Added: {scenePath}");
            }
            else
            {
                Debug.Log($"[BuildSettingsHelper] Already in build settings: {scenePath}");
            }
        }

        EditorBuildSettings.scenes = originalScenes.ToArray();

        EditorUtility.DisplayProgressBar("Build Settings", "Validating...", 0.9f);

        // Step 3: Validate
        ValidateBuildSettings();

        EditorUtility.ClearProgressBar();

        // Step 4: User feedback
        string message = addedCount > 0
            ? $"Successfully added {addedCount} scene(s) to Build Settings"
            : "All scenes already in Build Settings";

        EditorUtility.DisplayDialog("Build Settings Updated", message, "OK");
    }

    private static void FixSceneMetaFile(string scenePath)
    {
        string metaPath = scenePath + ".meta";

        if (!File.Exists(metaPath))
        {
            Debug.LogWarning($"[BuildSettingsHelper] Meta file missing: {metaPath}");
            return;
        }

        string metaContent = File.ReadAllText(metaPath);

        if (metaContent.Contains("folderAsset: yes"))
        {
            Debug.Log($"[BuildSettingsHelper] Fixing corrupted meta file: {metaPath}");

            // Delete the corrupted .meta file
            File.Delete(metaPath);

            // Reimport to regenerate correct .meta
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();

            Debug.Log($"[BuildSettingsHelper] ✓ Fixed: {Path.GetFileName(scenePath)}");
        }
        else
        {
            Debug.Log($"[BuildSettingsHelper] Meta file OK: {Path.GetFileName(scenePath)}");
        }
    }

    private static void ValidateBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        int enabledCount = scenes.Count(s => s.enabled);

        Debug.Log($"[BuildSettingsHelper] Total scenes in build: {scenes.Length}");
        Debug.Log($"[BuildSettingsHelper] Enabled scenes: {enabledCount}");

        // Verify required scenes
        string[] requiredScenes = {
            "Assets/Scenes/ForReal/GameStart.unity",
            "Assets/Scenes/ForReal/Idle.unity",
            "Assets/Scenes/ForReal/Cooking.unity",
            "Assets/Scenes/ForReal/Scene_Delivery.unity",
            "Assets/Scenes/ForReal/Scene_Shop.unity"
        };

        foreach (var requiredScene in requiredScenes)
        {
            bool found = scenes.Any(s => s.path == requiredScene && s.enabled);

            if (found)
            {
                Debug.Log($"[BuildSettingsHelper] ✓ {Path.GetFileName(requiredScene)}");
            }
            else
            {
                Debug.LogError($"[BuildSettingsHelper] ✗ Missing: {requiredScene}");
            }
        }
    }
}
#endif
