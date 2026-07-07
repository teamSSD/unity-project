using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>일회성. Welcome 스텝 SO + Catalog SO 생성. Controller에 catalog wire.
/// Mall 씬에 TutorialTarget 컴포넌트 (player, playerStore) 배치.</summary>
public static class _TempCreateWelcomeStep
{
    private const string StepsDir = "Assets/Bundles/TutorialSteps";
    private const string WelcomeStepPath = "Assets/Bundles/TutorialSteps/01_WelcomeAtSpawn.asset";
    private const string CatalogPath = "Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset";
    private const string ManagersScene = "Assets/Scenes/ForReal/Managers.unity";
    private const string MallScene = "Assets/Scenes/ForReal/Mall.unity";

    [MenuItem("Tools/Tutorial/Create Welcome Step + Wire Everything")]
    public static void Run()
    {
        CreateWelcomeSO();
        WireCatalogIntoController();
        AddTargetsInMall();
    }

    private static void CreateWelcomeSO()
    {
        System.IO.Directory.CreateDirectory(StepsDir);

        // Welcome 6줄 각각 한 파트씩
        var lines = new List<(string message, string targetKey)>
        {
            ("Aftertaste 상가에 오신걸 환영합니다!",                             "player"),
            ("게임 시작에 앞서 게임 방식에 대해 간단히 안내드리겠습니다.",       "player"),
            ("이동은 A/D 또는 ←/→ 키로 할 수 있습니다.",                          "player"),
            ("ESC를 누르면 띄워둔 창을 닫거나 설정을 열 수 있습니다.",           "player"),
            ("스페이스바를 통해 상호작용이 가능합니다.",                          "player"),
            ("가게 문쪽에서 스페이스바를 눌러 하루 영업을 시작해봐요.",           "playerStore"),
        };

        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(WelcomeStepPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, WelcomeStepPath);
        }
        step.stepId = TutorialStepId.WelcomeAtSpawn;
        step.parts.Clear();
        foreach (var (msg, key) in lines)
        {
            step.parts.Add(new TutorialStepPart
            {
                message = msg,
                targetKey = key,
                tailDirection = TutorialBubble.TailDirection.Down,
            });
        }
        EditorUtility.SetDirty(step);

        // Catalog
        var catalog = AssetDatabase.LoadAssetAtPath<TutorialStepCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<TutorialStepCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }
        catalog.steps.Clear();
        catalog.steps.Add(step);
        EditorUtility.SetDirty(catalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Tutorial] Welcome SO (6 파트) + Catalog 생성 완료");
    }

    private static void WireCatalogIntoController()
    {
        var scene = EditorSceneManager.OpenScene(ManagersScene, OpenSceneMode.Single);
        TutorialController ctrl = null;
        foreach (var r in scene.GetRootGameObjects())
        {
            ctrl = r.GetComponentInChildren<TutorialController>(true);
            if (ctrl != null) break;
        }
        if (ctrl == null) { Debug.LogError("[Tutorial] TutorialController 미배치"); return; }

        var catalog = AssetDatabase.LoadAssetAtPath<TutorialStepCatalog>(CatalogPath);
        var so = new SerializedObject(ctrl);
        so.FindProperty("catalog").objectReferenceValue = catalog;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Tutorial] Catalog → TutorialController wire 완료");
    }

    private static void AddTargetsInMall()
    {
        var scene = EditorSceneManager.OpenScene(MallScene, OpenSceneMode.Single);

        // Player 찾기 (Tag)
        GameObject player = null;
        GameObject playerStore = null;
        foreach (var r in scene.GetRootGameObjects())
        {
            foreach (var t in r.GetComponentsInChildren<Transform>(true))
            {
                if (player == null && t.CompareTag("Player")) player = t.gameObject;
                if (playerStore == null && t.gameObject.name == "playerStore") playerStore = t.gameObject;
            }
        }
        if (player == null) Debug.LogError("[Tutorial] Player 미발견 (Tag=Player)");
        if (playerStore == null) Debug.LogError("[Tutorial] playerStore 미발견");

        // Player: 머리 바로 위가 아니라 조금 띄운 위치. child anchor + screen offset 미세조정.
        Transform headAnchor = EnsureChildAnchor(player, "TutorialAnchor_Head", new Vector3(0.3f, 0.9f, 0));
        AddTutorialTarget(player, "player", 1f, headAnchor, new Vector2(42, 65));

        // playerStore: 문 위쪽 anchor (Scene 뷰에서 드래그로 조정 가능).
        Transform doorAnchor = EnsureChildAnchor(playerStore, "TutorialAnchor_Door", new Vector3(0, -1.0f, 0));
        AddTutorialTarget(playerStore, "playerStore", 0.3f, doorAnchor, Vector2.zero);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Tutorial] Mall 씬에 TutorialTarget 배치 완료");
    }

    private static void AddTutorialTarget(GameObject go, string key, float verticalFraction, Transform anchorOverride, Vector2 screenOffset)
    {
        if (go == null) return;
        var existing = go.GetComponent<TutorialTarget>();
        if (existing == null) existing = go.AddComponent<TutorialTarget>();
        var so = new SerializedObject(existing);
        so.FindProperty("key").stringValue = key;
        so.FindProperty("verticalAnchorFraction").floatValue = verticalFraction;
        so.FindProperty("anchorOverride").objectReferenceValue = anchorOverride;
        so.FindProperty("screenOffset").vector2Value = screenOffset;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"  · TutorialTarget on '{go.name}' key='{key}' vAnchor={verticalFraction} override={(anchorOverride != null ? anchorOverride.name : "none")} screenOffset={screenOffset}");
    }

    private static Transform EnsureChildAnchor(GameObject parent, string name, Vector3 localPos)
    {
        if (parent == null) return null;
        var existing = parent.transform.Find(name);
        Transform t;
        if (existing == null)
        {
            var childGO = new GameObject(name);
            childGO.transform.SetParent(parent.transform, false);
            t = childGO.transform;
        }
        else t = existing;
        t.localPosition = localPos; // 항상 재설정 (재실행 시 최신값)
        return t;
    }
}
