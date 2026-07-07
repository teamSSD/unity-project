using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Managers 씬 TutorialOverlayCanvas에 여러 방향/fraction 조합의 bubble preview 배치.</summary>
public static class _TempPreviewTutorialBubble
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/tutorial/TutorialBubble.prefab";
    private const string ScenePath = "Assets/Scenes/ForReal/Managers.unity";

    [MenuItem("Tools/Tutorial/Preview Bubble Directions in Managers Scene")]
    public static void Preview()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RectTransform canvasRt = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { canvasRt = (RectTransform)r.transform; break; }
        if (canvasRt == null) { Debug.LogError("[Tutorial] TutorialOverlayCanvas 미발견"); return; }

        // 기존 preview 제거
        for (int i = canvasRt.childCount - 1; i >= 0; i--)
            if (canvasRt.GetChild(i).name.StartsWith("[Preview]"))
                Object.DestroyImmediate(canvasRt.GetChild(i).gameObject);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError($"[Tutorial] Prefab 미발견: {PrefabPath}"); return; }

        // 6개 preview: Down/Up x fraction {-0.5, 0, +0.5}
        // 1920x1080 canvas — 그리드 배치
        Spawn(prefab, canvasRt, "[Preview] Down_L",  new Vector2(320, 800),  TutorialBubble.TailDirection.Down, -0.5f, "Down / f=-0.5");
        Spawn(prefab, canvasRt, "[Preview] Down_C",  new Vector2(960, 800),  TutorialBubble.TailDirection.Down,  0.0f, "Down / f=0");
        Spawn(prefab, canvasRt, "[Preview] Down_R",  new Vector2(1600, 800), TutorialBubble.TailDirection.Down, +0.5f, "Down / f=+0.5");
        Spawn(prefab, canvasRt, "[Preview] Up_L",    new Vector2(320, 280),  TutorialBubble.TailDirection.Up,   -0.5f, "Up / f=-0.5");
        Spawn(prefab, canvasRt, "[Preview] Up_C",    new Vector2(960, 280),  TutorialBubble.TailDirection.Up,    0.0f, "Up / f=0");
        Spawn(prefab, canvasRt, "[Preview] Up_R",    new Vector2(1600, 280), TutorialBubble.TailDirection.Up,   +0.5f, "Up / f=+0.5");

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Tutorial] Preview 6개 배치. Scene 뷰에서 확인. 완료 후 Remove Preview로 정리.");
    }

    private static void Spawn(GameObject prefab, RectTransform canvas, string name, Vector2 tipScreen,
                              TutorialBubble.TailDirection dir, float fraction, string label)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        inst.name = name;
        var bubble = inst.GetComponent<TutorialBubble>();
        if (bubble == null) return;

        var tmp = inst.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null) tmp.text = label;

        // fraction 세팅 (SerializeField에 리플렉션)
        var so = new SerializedObject(bubble);
        so.FindProperty("tailHorizontalFraction").floatValue = fraction;
        so.ApplyModifiedPropertiesWithoutUndo();

        bubble.PlaceAtScreenPoint(tipScreen, dir);
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
