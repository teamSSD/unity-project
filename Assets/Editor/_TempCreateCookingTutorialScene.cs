using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>일회성. Cooking.unity 복제 → CookingTutorial.unity + TutorialCookingController 배치.
/// 원 씬은 그대로. 튜토리얼 활성 시에만 라우팅 대상.</summary>
public static class _TempCreateCookingTutorialScene
{
    private const string Source = "Assets/Scenes/ForReal/Cooking.unity";
    private const string Target = "Assets/Scenes/ForReal/CookingTutorial.unity";

    [MenuItem("Tools/Tutorial/Create CookingTutorial Scene")]
    public static void Create()
    {
        // 원본 파일 복제
        if (System.IO.File.Exists(Target)) AssetDatabase.DeleteAsset(Target);
        if (!AssetDatabase.CopyAsset(Source, Target))
        {
            Debug.LogError($"[Tutorial] 씬 복제 실패: {Source} → {Target}");
            return;
        }
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(Target, OpenSceneMode.Single);

        CustomerManager cm = null;
        CookingSceneManager csm = null;
        var storages = new List<BaseStorage>();
        GameObject managersGo = null;

        foreach (var root in scene.GetRootGameObjects())
        {
            if (cm == null) cm = root.GetComponentInChildren<CustomerManager>(true);
            if (csm == null) csm = root.GetComponentInChildren<CookingSceneManager>(true);
            storages.AddRange(root.GetComponentsInChildren<BaseStorage>(true));
        }

        managersGo = (cm != null ? cm.gameObject : (csm != null ? csm.gameObject : null));
        if (managersGo == null)
        {
            Debug.LogError("[Tutorial] Cooking 씬에 CustomerManager/CookingSceneManager 미발견");
            return;
        }

        var ctrl = managersGo.GetComponent<TutorialCookingController>()
                   ?? managersGo.AddComponent<TutorialCookingController>();

        var so = new SerializedObject(ctrl);
        so.FindProperty("customerManager").objectReferenceValue = cm;
        so.FindProperty("cookingSceneManager").objectReferenceValue = csm;
        var storagesProp = so.FindProperty("storages");
        storagesProp.arraySize = storages.Count;
        for (int i = 0; i < storages.Count; i++)
            storagesProp.GetArrayElementAtIndex(i).objectReferenceValue = storages[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AddToBuildSettings(Target);

        Debug.Log($"[Tutorial] CookingTutorial 씬 생성 (customer={cm != null} sceneMgr={csm != null} storages={storages.Count})");
    }

    private static void AddToBuildSettings(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in scenes) if (s.path == path) return;
        scenes.Add(new EditorBuildSettingsScene(path, enabled: true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[Tutorial] Build settings에 {path} 추가");
    }
}
