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
        // 도구 안내는 tool name prefix ("철판:", "냄비/팬:" 등) 제거. 소스뿌리기/섞기 라벨은 유지.
        var parts = new (string msg, string key, TutorialBubble.TailDirection dir, float frac, Vector2 offset, KeyCode dismiss)[]
        {
            ("원하는 재료들을 조리도구에 드래그 드랍 한 후,\n조리도구를 클릭하면 요리 미니게임이 시작됩니다!",
                "refrigerator",  TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("보여지는 방향에 맞추어 방향키를 입력하세요!",
                "tool-T005",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            // 팬 (T001) — Fire minigame
            ("스페이스바로 화력을 조절하세요.\n삼각형이 초록 게이지에 유지 될수록 점수가 올라갑니다!",
                "tool-T001",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            // 냄비 (T002) — Fire minigame (팬과 동일 내용, 별도 bubble)
            ("스페이스바로 화력을 조절하세요.\n삼각형이 초록 게이지에 유지 될수록 점수가 올라갑니다!",
                "tool-T002",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            // 볼 (T003) — 소스 뿌리기 + 섞기 통합. 소스 도구가 볼 옆에 있어서 하나로 묶음.
            ("소스 뿌리기: 위 아래 방향키를 연타해 정확한 양의 소스를 넣으세요!\n섞기: 스페이스바를 연타해 재료들을 섞으세요!",
                "tool-T003",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            ("안내선을 따라 재료를 정확하게 자르세요!",
                "tool-T004",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Space),
            // Tab 눌러야 dismiss (→ RecipeBookManager가 Tab 감지해서 자동으로 열림)
            ("탭(Tab)을 눌러 레시피북을 열어보세요.",
                "tool-T004",     TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.Tab),

            // 레시피북 열림 → dialogue.md 원문. offset (-406.5, +316.2) → 화면 (553.5, 856.2) 근처.
            ("메뉴를 클릭하여 상세 레시피를 확인 할 수 있습니다.",
                "",              TutorialBubble.TailDirection.Down, -0.5f, new Vector2(-406.5f, 316.2f), KeyCode.Space),
            // ESC/X 닫기 안내. tail 오른쪽 위(Up + +0.5). ESC로 dismiss (동시에 책 닫힘).
            // offset (+590.7, +315) → 화면 (1550.7, 855) 근처 (X 버튼 우측 상단).
            ("ESC나 X 버튼으로 레시피북을 닫을 수 있어요.",
                "",              TutorialBubble.TailDirection.Up,   0.5f,  new Vector2(590.7f, 315f), KeyCode.Escape),

            // 손님 mock — TutorialCookingController가 message prefix로 라우팅해 스폰/dismiss 처리.
            ("손님이 오면 클릭해서 주문을 받아요.",
                "customer",      TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.None),
            // 도시락 무더기 가리킴.
            ("도시락을 원하는 자리에 놓아주세요.",
                "bento-pile",    TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.None),
            // 유저가 방금 놓은 도시락 위치 가리킴 (HandleBentoPlaced에서 dynamic target 부여).
            ("완성한 요리를 도시락에 담아주세요.",
                "placed-bento",  TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.None),
            // 영수증 — tail 왼쪽 위 (Up + -0.5).
            ("영수증을 도시락에 붙이면 손님이 가져갑니다.",
                "receipt",       TutorialBubble.TailDirection.Up,   -0.5f, Vector2.zero, KeyCode.None),
            // 시간 안내 — target = taking 손님 (dynamic).
            ("시간 안에 처리하지 못하면 손님이 그냥 나갈 수 있어요.",
                "taking-customer", TutorialBubble.TailDirection.Down, -0.5f, Vector2.zero, KeyCode.None),
            // 스킵 버튼 — tail 오른쪽 위 (Up + +0.5).
            ("오늘은 손님이 더 오지 않을 것 같네요.\n이 버튼을 눌러 다음으로 넘어가요.",
                "skip",          TutorialBubble.TailDirection.Up,   0.5f,  Vector2.zero, KeyCode.None),
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
                autoFlipByScreenSide = key == "refrigerator" || key.StartsWith("tool-") || key == "customer" || key == "receipt",
                // "ESC나 X 버튼으로..." 파트는 실제 책 닫힘까지 대기 (카드 ESC 오작동 방지).
                dismissOnRecipeBookClose = msg.StartsWith("ESC나 X 버튼으로"),
                // "메뉴 클릭" 파트는 책이 강제로 열린 상태 유지 (Space로만 진행).
                forceRecipeBookOpen = msg.StartsWith("메뉴를 클릭"),
                // Tab hint 파트만 유일하게 Tab 오픈 허용. 그 외 모든 파트에서 Tab 오픈 차단.
                blockRecipeBookOpen = !msg.StartsWith("탭(Tab)"),
                // 손님/도시락/영수증/시간/스킵 파트는 게임 상호작용 필요 → 락 해제.
                allowSceneInteraction =
                    msg.StartsWith("손님이 오면") ||
                    msg.StartsWith("도시락을 원하는") ||
                    msg.StartsWith("완성한 요리") ||
                    msg.StartsWith("영수증") ||
                    msg.StartsWith("시간 안에") ||
                    msg.StartsWith("오늘은 손님이"),
                // 요리하는 파트(도시락 놓기 이후)는 유저가 카메라 자유롭게 움직일 수 있어야 함.
                // 초기 도구 안내 + 손님 받기까지는 튜토리얼이 카메라 강제.
                freeCamera =
                    msg.StartsWith("도시락을 원하는") ||
                    msg.StartsWith("완성한 요리") ||
                    msg.StartsWith("영수증") ||
                    msg.StartsWith("시간 안에") ||
                    msg.StartsWith("오늘은 손님이"),
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

        // 스킵 버튼 — EarlyEndButton에 target key="skip".
        EarlyEndButton skip = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            skip = root.GetComponentInChildren<EarlyEndButton>(true);
            if (skip != null) break;
        }
        if (skip != null) { AddKey(skip.gameObject, "skip"); placed++; }
        else Debug.LogWarning("[Tutorial] EarlyEndButton 미발견 (CookingTutorial 씬에 배치 필요).");

        // 도시락 무더기 — BentoSetModel (첫 발견본).
        BentoSetModel bentoSet = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            bentoSet = root.GetComponentInChildren<BentoSetModel>(true);
            if (bentoSet != null) break;
        }
        if (bentoSet != null) { AddKey(bentoSet.gameObject, "bento-pile"); placed++; }
        else Debug.LogWarning("[Tutorial] BentoSetModel 미발견 (CookingTutorial 씬).");

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
