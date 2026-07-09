using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>씬/프리팹의 missing script 참조를 찾는 진단 툴.
/// 결과 콘솔 출력 + 옵션으로 자동 제거.</summary>
public static class _TempFindMissingScripts
{
    [MenuItem("Tools/Diagnostics/Find Missing Scripts (Open Scenes)")]
    public static void FindInOpenScenes()
    {
        int total = 0;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;
            total += ScanScene(scene);
        }
        Debug.Log($"[MissingScripts] 열린 씬 총 {total} GameObject에 missing script 존재.");
    }

    [MenuItem("Tools/Diagnostics/Find Missing Scripts (All Prefabs)")]
    public static void FindInAllPrefabs()
    {
        var guids = AssetDatabase.FindAssets("t:Prefab");
        int hits = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            int count = CountMissingOnGO(prefab);
            if (count > 0)
            {
                Debug.LogWarning($"[MissingScripts] {path} — {count}개");
                hits++;
            }
        }
        Debug.Log($"[MissingScripts] 스캔한 프리팹 {guids.Length}개, missing 있는 프리팹 {hits}개.");
    }

    [MenuItem("Tools/Diagnostics/Find Missing Scripts (All Scenes in Assets)")]
    public static void FindInAllScenes()
    {
        var guids = AssetDatabase.FindAssets("t:Scene");
        int hits = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int count = ScanScene(scene);
            EditorSceneManager.CloseScene(scene, true);
            if (count > 0)
            {
                Debug.LogWarning($"[MissingScripts] {path} — {count} GameObject");
                hits++;
            }
        }
        Debug.Log($"[MissingScripts] 스캔한 씬 {guids.Length}개, missing 있는 씬 {hits}개.");
    }

    private static int ScanScene(Scene scene)
    {
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            var stack = new Stack<GameObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var go = stack.Pop();
                int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
                if (missing > 0)
                {
                    Debug.LogWarning($"[MissingScripts] {scene.name} :: {GetPath(go)} — {missing}개 missing", go);
                    count++;
                }
                for (int i = 0; i < go.transform.childCount; i++)
                    stack.Push(go.transform.GetChild(i).gameObject);
            }
        }
        return count;
    }

    private static int CountMissingOnGO(GameObject root)
    {
        int total = 0;
        var stack = new Stack<GameObject>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var go = stack.Pop();
            total += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            for (int i = 0; i < go.transform.childCount; i++)
                stack.Push(go.transform.GetChild(i).gameObject);
        }
        return total;
    }

    private static string GetPath(GameObject go)
    {
        var t = go.transform;
        string path = go.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }

    [MenuItem("Tools/Diagnostics/Remove Missing Scripts (All Scenes)")]
    public static void RemoveInAllScenes()
    {
        var guids = AssetDatabase.FindAssets("t:Scene");
        int totalRemoved = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.StartsWith("Assets/")) continue;

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            int removed = 0;
            foreach (var root in scene.GetRootGameObjects())
                removed += RemoveMissingRecursive(root);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[MissingScripts:REMOVED] {path} — {removed}개 제거");
                totalRemoved += removed;
            }
            EditorSceneManager.CloseScene(scene, true);
        }
        Debug.Log($"[MissingScripts:REMOVED] 총 {totalRemoved}개 제거");
    }

    private static int RemoveMissingRecursive(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        for (int i = 0; i < go.transform.childCount; i++)
            removed += RemoveMissingRecursive(go.transform.GetChild(i).gameObject);
        return removed;
    }

    [MenuItem("Tools/Diagnostics/Install TextureDiagnostics in Managers scene")]
    public static void InstallTextureDiagnostics()
    {
        const string path = "Assets/Scenes/ForReal/Managers.unity";
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        GameObject existing = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<TextureDiagnostics>(true) != null)
            {
                existing = root;
                break;
            }
        }
        if (existing != null)
        {
            Debug.Log("[TextureDiag] 이미 Managers 씬에 존재.");
            return;
        }
        var go = new GameObject("TextureDiagnostics");
        go.AddComponent<TextureDiagnostics>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TextureDiag] Managers 씬에 설치 완료.");
    }
}
