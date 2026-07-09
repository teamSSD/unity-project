using UnityEditor;
using UnityEngine;

/// <summary>일회성. MallCorridor (11) + Closing (16) SO 생성 + Catalog 추가.
/// 위치는 message content 기반 (targetKey 비움). 유저가 필요 시 나중에 조정.</summary>
public static class _TempCreateMallSteps
{
    private const string MallCorridorPath = "Assets/Bundles/TutorialSteps/11_MallCorridor.asset";
    private const string ClosingPath = "Assets/Bundles/TutorialSteps/16_Closing.asset";
    private const string CatalogPath = "Assets/Bundles/TutorialSteps/TutorialStepCatalog.asset";

    [MenuItem("Tools/Tutorial/Create Mall + Closing Steps")]
    public static void Run()
    {
        var corridor = CreateMallCorridor();
        var closing = CreateClosing();
        AddToCatalog(corridor);
        AddToCatalog(closing);
    }

    private static TutorialStepData CreateMallCorridor()
    {
        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(MallCorridorPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, MallCorridorPath);
        }
        step.stepId = TutorialStepId.MallCorridor;
        step.parts.Clear();

        // dialogue.md "영업 선택 가이드" 후반부 5문단.
        var msgs = new string[]
        {
            "재료 수급을 선택하게 되면 상가 복도로 이동하게 됩니다.",
            "옥상 층에는 특정 재료를 기르고 수확할 수 있는 텃밭이 존재하며,\n가게의 좌측편에는 재료를 재화로 구매하거나 텃밭, 도구, 창고 업그레이드가 가능한 상점이 존재합니다.",
            "또한 영업 준비시간이나 재료 수급 시간에는 상가를 돌아다니며 만나게 되는 인물로 부터\n밤 시간에 진행될 배달 주문들을 받을 수 있습니다.",
            "영업 준비를 마친 후 다시 가게로 돌아가면 다음 영업 페이즈를 선택 할 수 있습니다.",
        };

        foreach (var msg in msgs)
        {
            step.parts.Add(new TutorialStepPart
            {
                message = msg,
                targetKey = "",
                tailDirection = TutorialBubble.TailDirection.Down,
                tailHorizontalFraction = 0f,
                screenOffset = Vector2.zero,
                dismissKey = KeyCode.Space,
                allowSceneInteraction = false,
            });
        }

        EditorUtility.SetDirty(step);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Tutorial] MallCorridor SO ({step.parts.Count} 파트) 생성");
        return step;
    }

    private static TutorialStepData CreateClosing()
    {
        var step = AssetDatabase.LoadAssetAtPath<TutorialStepData>(ClosingPath);
        if (step == null)
        {
            step = ScriptableObject.CreateInstance<TutorialStepData>();
            AssetDatabase.CreateAsset(step, ClosingPath);
        }
        step.stepId = TutorialStepId.Closing;
        step.parts.Clear();

        // 단일 마지막 메시지 → dismiss 시 onDone에서 tc.Complete() + PassPhase().
        step.parts.Add(new TutorialStepPart
        {
            message = "이제 진짜 시작이에요.",
            targetKey = "",
            tailDirection = TutorialBubble.TailDirection.Down,
            tailHorizontalFraction = 0f,
            screenOffset = Vector2.zero,
            dismissKey = KeyCode.Space,
            allowSceneInteraction = false,
        });

        EditorUtility.SetDirty(step);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Tutorial] Closing SO ({step.parts.Count} 파트) 생성");
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
            Debug.Log($"[Tutorial] Catalog에 {step.name} 추가");
        }
    }
}
