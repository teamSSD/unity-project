using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>일회성. Cooking 스텝 SO (7 파트) + Catalog + TutorialTarget들을 CookingTutorial 씬에 배치.</summary>
public static class _TempCreateCookingSteps
{
    private const string StepPath = "Assets/Bundles/TutorialSteps/03_CookingSequence.asset";
    private const string CatalogPath = "Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset";
    private const string CookingScene = "Assets/Scenes/ForReal/CookingTutorial.unity";

    [MenuItem("Tools/Tutorial/Create Cooking Steps + Wire")]
    public static void Run()
    {
        var step = CreateStep();
        AddToCatalog(step);
        WireTargetsInMockScene();
    }

    private static TutorialStepData CreateStep()
    {
        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(StepPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, StepPath);
        }
        step.stepId = TutorialStepId.CookingIntro; // umbrella step (intro + 5 tools + recipe tab 통합)
        step.parts.Clear();

        // (message, targetKey, direction, fraction, offset, dismissKey)
        var parts = new (string msg, string key, TutorialBubble.TailDirection dir, float frac, Vector2 offset, KeyCode dismiss)[]
        {
            ("원하는 재료들을 조리도구에 드래그 드랍 한 후,\n조리도구를 클릭하면 요리 미니게임이 시작됩니다!",
                "refrigerator",  TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("철판: 보여지는 방향에 맞추어 방향키를 입력하세요!",
                "tool-T005",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("냄비/팬: 스페이스바로 화력을 조절하세요.\n삼각형이 초록 게이지에 유지 될수록 점수가 올라갑니다!",
                "tool-T001",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("소스: 위 아래 방향키를 연타해 정확한 양의 소스를 넣으세요!",
                "tool-T002",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("볼: 스페이스바를 연타해 재료들을 섞으세요!",
                "tool-T003",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("도마: 안내선을 따라 재료를 정확하게 자르세요!",
                "tool-T004",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            // 마지막: Tab 눌러야 dismiss (→ RecipeBookManager가 Tab 감지해서 자동으로 열림)
            // 카메라 유지를 위해 직전 도구(도마 T004)와 같은 target.
            ("탭(Tab)을 눌러 레시피북을 열어보세요.",
                "tool-T004",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Tab),
        };

        foreach (var (msg, key, dir, frac, offset, dismiss) in parts)
        {
            step.parts.Add(new TutorialStepPart
            {
                message = msg,
                targetKey = key,
                tailDirection = dir,
                tailHorizontalFraction = frac,
                screenOffset = offset,
                dismissKey = dismiss,
                // Cooking 파트는 카메라가 target으로 이동하는데 target 위치에 따라 body 방향 자동 반전.
                autoFlipByScreenSide = key == "refrigerator" || key.StartsWith("tool-"),
            });
        }
        EditorUtility.SetDirty(step);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Tutorial] Cooking Sequence SO ({step.parts.Count} 파트) 생성");
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
            Debug.Log($"[Tutorial] Catalog에 Cooking Sequence 추가");
        }
    }

    private static void WireTargetsInMockScene()
    {
        var scene = EditorSceneManager.OpenScene(CookingScene, OpenSceneMode.Single);

        int placed = 0;

        // 조리도구 5개
        var toolModels = new List<CookingToolModel>();
        foreach (var root in scene.GetRootGameObjects())
            toolModels.AddRange(root.GetComponentsInChildren<CookingToolModel>(true));
        foreach (var m in toolModels)
        {
            var so = new SerializedObject(m);
            var toolIdProp = so.FindProperty("toolId");
            string toolId = toolIdProp?.stringValue;
            if (string.IsNullOrEmpty(toolId)) continue;
            AddKey(m.gameObject, $"tool-{toolId}");
            placed++;
        }

        // 냉장고 — CookingSceneManager.refrigeratorGameObject SerializeField에서 얻음.
        CookingSceneManager csm = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            csm = root.GetComponentInChildren<CookingSceneManager>(true);
            if (csm != null) break;
        }
        if (csm != null)
        {
            var so = new SerializedObject(csm);
            var fridgeGO = so.FindProperty("refrigeratorGameObject")?.objectReferenceValue as GameObject;
            if (fridgeGO != null)
            {
                AddKey(fridgeGO, "refrigerator");
                placed++;
            }
            else Debug.LogWarning("[Tutorial] refrigerator GO 미발견 (CookingSceneManager.refrigeratorGameObject 미할당?)");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Tutorial] CookingTutorial 씬에 {placed} TutorialTarget 배치");
    }

    private static void AddKey(GameObject go, string key)
    {
        var target = go.GetComponent<TutorialTarget>();
        if (target == null) target = go.AddComponent<TutorialTarget>();
        var tso = new SerializedObject(target);
        tso.FindProperty("key").stringValue = key;
        tso.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"  · TutorialTarget on '{go.name}' key='{key}'");
    }
}
