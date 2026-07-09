using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>일회성. 씬에서 sprite=null + BoxCollider2D 있는 GameObject의 SpriteRenderer 제거.
/// TextureDiag가 잡는 NULL_SPRITE 경고 정리. Collider 유지되므로 physics 영향 없음.
/// (예: Mall/Background/ground/layer, Stairs/Triangle 등 collider-only walkable ground.)</summary>
public static class _TempCleanDeadSpriteRenderers
{
    private static readonly string[] TargetScenes =
    {
        "Assets/Scenes/ForReal/Mall.unity",
        "Assets/Scenes/ForReal/CookingTutorial.unity",
    };

    [MenuItem("Tools/Diagnostics/Clean Dead SpriteRenderers")]
    public static void Run()
    {
        int totalRemoved = 0;
        foreach (var path in TargetScenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            int removed = 0;
            foreach (var root in scene.GetRootGameObjects())
                removed += CleanRecursive(root);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Clean SR] {System.IO.Path.GetFileName(path)} — {removed}개 제거");
                totalRemoved += removed;
            }
        }
        Debug.Log($"[Clean SR] 총 {totalRemoved}개 SpriteRenderer 제거");
    }

    private static int CleanRecursive(GameObject go)
    {
        int removed = 0;
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite == null)
        {
            // 안전 조건: BoxCollider2D 또는 다른 Collider 있으면 collider-only GO로 간주.
            bool hasCollider = go.GetComponent<Collider2D>() != null;
            if (hasCollider)
            {
                Object.DestroyImmediate(sr, allowDestroyingAssets: false);
                removed++;
            }
        }
        for (int i = 0; i < go.transform.childCount; i++)
            removed += CleanRecursive(go.transform.GetChild(i).gameObject);
        return removed;
    }
}
