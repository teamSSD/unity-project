using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>일회성 미리보기. 5가지 케이스를 화면 여러 곳에 배치.</summary>
public static class _TempPreviewTutorialBubble
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/tutorial/TutorialBubble.prefab";
    private const string ScenePath = "Assets/Scenes/ForReal/Managers.unity";

    [MenuItem("Tools/Tutorial/Preview Bubble in Managers Scene")]
    public static void Preview()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RectTransform canvasRt = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { canvasRt = (RectTransform)r.transform; break; }
        if (canvasRt == null) { Debug.LogError("[Tutorial] TutorialOverlayCanvas 미발견"); return; }

        for (int i = canvasRt.childCount - 1; i >= 0; i--)
            if (canvasRt.GetChild(i).name.StartsWith("[Preview]"))
                Object.DestroyImmediate(canvasRt.GetChild(i).gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError($"[Tutorial] Prefab 미발견: {PrefabPath}"); return; }

        // 1920x1080 기준 배치. 각 preview는 tail tip이 지정 좌표에 꽂힘.
        SpawnPreview(prefab, canvasRt, "[Preview] Down_Short",  new Vector2(960, 300),  TutorialBubble.TailDirection.Down,
                     "한 줄 안내입니다.");
        SpawnPreview(prefab, canvasRt, "[Preview] Down_Long",   new Vector2(400, 250),  TutorialBubble.TailDirection.Down,
                     "여러 줄 안내가 들어갈 때\n어떻게 보이는지 확인하는\n케이스입니다.");
        SpawnPreview(prefab, canvasRt, "[Preview] Up_Short",    new Vector2(1500, 900), TutorialBubble.TailDirection.Up,
                     "타겟이 위에 있어요.");

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Tutorial] 5개 preview 배치. Scene 뷰에서 확인.");
    }

    private static void SpawnPreview(GameObject prefab, RectTransform canvas, string name,
                                     Vector2 tipScreen, TutorialBubble.TailDirection dir, string text)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        inst.name = name;
        var bubble = inst.GetComponent<TutorialBubble>();
        if (bubble != null)
        {
            var tmp = inst.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (tmp != null) tmp.text = text;
            bubble.PlaceAtScreenPoint(tipScreen, dir);
        }
    }

    [MenuItem("Tools/Tutorial/Remove Preview")]
    public static void Remove()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RectTransform canvasRt = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { canvasRt = (RectTransform)r.transform; break; }
        if (canvasRt == null) return;
        int removed = 0;
        for (int i = canvasRt.childCount - 1; i >= 0; i--)
            if (canvasRt.GetChild(i).name.StartsWith("[Preview]"))
            {
                Object.DestroyImmediate(canvasRt.GetChild(i).gameObject);
                removed++;
            }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Tutorial] Preview {removed}개 제거");
    }
}
