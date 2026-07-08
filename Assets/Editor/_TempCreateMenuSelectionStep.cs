using UnityEditor;
using UnityEngine;

/// <summary>Menu Selection SO 생성 + Catalog 추가 + MenuSelection prefab TutorialTarget 배치.
/// target-relative offset 방식. 초기 offset (0, 0) → Play 후 iterate.</summary>
public static class _TempCreateMenuSelectionStep
{
    private const string StepPath = "Assets/Bundles/TutorialSteps/02_MenuSelection.asset";
    private const string CatalogPath = "Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset";
    private const string MenuSelectionPrefab = "Assets/Bundles/Prefabs/recipebook/MenuSelection/MenuSelection.prefab";

    [MenuItem("Tools/Tutorial/Create Menu Selection Step")]
    public static void Run()
    {
        var step = CreateStep();
        AddToCatalog(step);
        WireTargetsInPrefab();
    }

    private static TutorialStepData CreateStep()
    {
        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(StepPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, StepPath);
        }
        step.stepId = TutorialStepId.MenuSelection;
        step.parts.Clear();

        // User 지정 tail 위치를 target 기준 offset으로 계산.
        var lines = new (string msg, string key, TutorialBubble.TailDirection dir, float horizFrac, Vector2 offset)[]
        {
            ("도시락 조합은 하루에 1개에서 3개까지 가능합니다.",
                "bento-slots",     TutorialBubble.TailDirection.Down, -0.5f, new Vector2(-661.36f,  205.57f)),
            ("도시락 하나당 메인 메뉴 / 사이드 메뉴 각 1개에서 3개까지 선택이 가능합니다.",
                "bento-slots",     TutorialBubble.TailDirection.Down, -0.5f, new Vector2(-678f,     277.87f)),
            ("메뉴를 클릭하여 선택/취소 할 수 있습니다.",
                "menu-items",      TutorialBubble.TailDirection.Down, -0.5f, new Vector2(-639.3f,   102.25f)),
            ("조합이 끝났다면 확인을 눌러 넘어갈 수 있습니다.",
                "confirm-button",  TutorialBubble.TailDirection.Up,   +0.7f, new Vector2( 83.58f,  -43.64f)),
            ("다시 상가로 돌아가 영업준비를 계속 할 수 있습니다.",
                "back-button",     TutorialBubble.TailDirection.Up,   -0.7f, new Vector2(-84.78f,  -43.64f)),
        };

        foreach (var (msg, key, dir, frac, offset) in lines)
        {
            step.parts.Add(new TutorialStepPart
            {
                message = msg,
                targetKey = key,
                tailDirection = dir,
                tailHorizontalFraction = frac,
                screenOffset = offset,
            });
        }
        EditorUtility.SetDirty(step);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Tutorial] MenuSelection SO (5 파트) 생성");
        return step;
    }

    private static void AddToCatalog(TutorialStepData step)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<TutorialStepCatalog>(CatalogPath);
        if (catalog == null) { Debug.LogError("[Tutorial] Catalog 미발견"); return; }
        if (!catalog.steps.Contains(step))
        {
            catalog.steps.Add(step);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Tutorial] Catalog에 MenuSelection 추가");
        }
    }

    private static void WireTargetsInPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(MenuSelectionPrefab);
        if (root == null) { Debug.LogError($"[Tutorial] Prefab load 실패: {MenuSelectionPrefab}"); return; }
        try
        {
            AddTargetToChild(root, "Background/BentoSelectionGroup",                "bento-slots");
            AddTargetToChild(root, "Background/BentoSelectionGroup/SelectionGroup", "menu-items");
            AddTargetToChild(root, "Background/NavigationBar/ConfirmButton",        "confirm-button");
            AddTargetToChild(root, "Background/NavigationBar/BackButton",           "back-button");
            PrefabUtility.SaveAsPrefabAsset(root, MenuSelectionPrefab);
            Debug.Log($"[Tutorial] MenuSelection prefab에 4 TutorialTarget 배치");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AddTargetToChild(GameObject root, string path, string key)
    {
        var t = root.transform.Find(path);
        if (t == null) { Debug.LogError($"[Tutorial] Path 미발견: {path}"); return; }
        var existing = t.GetComponent<TutorialTarget>();
        if (existing == null) existing = t.gameObject.AddComponent<TutorialTarget>();
        var so = new SerializedObject(existing);
        so.FindProperty("key").stringValue = key;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"  · TutorialTarget on '{t.name}' key='{key}'");
    }
}
