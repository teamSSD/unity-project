using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>일회성. PhaseSelectAfternoon 스텝 SO + Catalog + shoppingButton에 TutorialTarget key="phase-shopping".</summary>
public static class _TempCreatePhaseSelectSteps
{
    private const string StepPath = "Assets/Bundles/TutorialSteps/10_PhaseSelectAfternoon.asset";
    private const string CatalogPath = "Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset";
    private const string PhaseUIPrefabPath = "Assets/Bundles/Prefabs/mall/PhaseSelectionUI.prefab";

    [MenuItem("Tools/Tutorial/Create PhaseSelect Steps + Wire")]
    public static void Run()
    {
        var step = CreateStep();
        AddToCatalog(step);
        WireShoppingTargetOnPrefab();
    }

    private static TutorialStepData CreateStep()
    {
        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(StepPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, StepPath);
        }
        step.stepId = TutorialStepId.PhaseSelectAfternoon;
        step.parts.Clear();

        // Part 0 — 정보 안내 (Space 진행). 화면 중앙, target 없음.
        step.parts.Add(new TutorialStepPart
        {
            message = "점심, 저녁, 밤 각 페이즈 마다 영업 여부를 선택할 수 있습니다.",
            targetKey = "",
            tailDirection = TutorialBubble.TailDirection.Down,
            tailHorizontalFraction = 0f,
            screenOffset = Vector2.zero,
            dismissKey = KeyCode.Space,
            allowSceneInteraction = false,
        });

        // Part 1 — Shopping 버튼 지시. dismissKey 없음, Shopping 클릭 시 MallSceneController가 dismiss.
        step.parts.Add(new TutorialStepPart
        {
            message = "이번 점심에는 상가 이동을 선택해 봅시다.",
            targetKey = "phase-shopping",
            tailDirection = TutorialBubble.TailDirection.Down,
            tailHorizontalFraction = -0.5f,
            screenOffset = Vector2.zero,
            dismissKey = KeyCode.None,
            allowSceneInteraction = true,
        });

        EditorUtility.SetDirty(step);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Tutorial] PhaseSelectAfternoon SO ({step.parts.Count} 파트) 생성");
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
            Debug.Log("[Tutorial] Catalog에 PhaseSelectAfternoon 추가");
        }
    }

    private static void WireShoppingTargetOnPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhaseUIPrefabPath);
        if (prefab == null) { Debug.LogError("[Tutorial] PhaseSelectionUI prefab 미발견"); return; }

        var root = PrefabUtility.LoadPrefabContents(PhaseUIPrefabPath);
        try
        {
            var selector = root.GetComponentInChildren<PhaseActionSelector>(true);
            if (selector == null) { Debug.LogError("[Tutorial] PhaseActionSelector 미발견"); return; }

            var so = new SerializedObject(selector);
            var shoppingBtn = so.FindProperty("shoppingButton")?.objectReferenceValue as Button;
            if (shoppingBtn == null) { Debug.LogError("[Tutorial] shoppingButton 미할당"); return; }

            var target = shoppingBtn.GetComponent<TutorialTarget>();
            if (target == null) target = shoppingBtn.gameObject.AddComponent<TutorialTarget>();
            var tso = new SerializedObject(target);
            tso.FindProperty("key").stringValue = "phase-shopping";
            tso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PhaseUIPrefabPath);
            Debug.Log("[Tutorial] shoppingButton에 TutorialTarget key='phase-shopping' 설정");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
