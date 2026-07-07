using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>일회성. TutorialBubble 프리팹 (원본 SpeechBubble 스프라이트 매핑 정확히) + Overlay canvas 배치.
///
/// 정확한 스프라이트 매핑 (SpeechBubble.prefab 분석 결과):
///   Body sprite = ui_speechBubble_0 (fileID 7860725457570909701), Type=Sliced
///   Tail sprite = ui_speechBubble_1 (fileID -3876946149166117198), Type=Simple
///
/// 구조:
///   Root (RectTransform + TutorialBubble, pivot 0.5 0)
///     Body (Image body sprite + VLG + CSF, pivot 0.5 0, anchoredPos (0, tailH-overlap))
///       OptionalImage (Image, 비활성)
///       Contents (TMP)
///     Tail (Image tail sprite, pivot 0.5 0, anchoredPos (0, 0))
/// </summary>
public static class _TempCreateTutorialInfra
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/tutorial/TutorialBubble.prefab";
    private const string ScenePath = "Assets/Scenes/ForReal/Managers.unity";
    private const string AtlasPath = "Assets/Bundles/driveAssets/art/ui/cooking/ui_speechBubble.png";
    private const string SpeechBubblePrefab = "Assets/Bundles/Prefabs/cooking/SpeechBubble.prefab";

    // 튜닝 값 — Overlay canvas 1920x1080 기준.
    // 원본 SpeechBubble tail = 126x184, overlap = 52. 그것보다 훨씬 작게.
    private const float TailWidth = 60f;
    private const float TailHeight = 85f;
    private const float TailOverlapWithBubble = 40f; // body 안쪽으로 40px 겹침 — seamless
    private const float BodyMinWidth = 260f;
    private const int PadLR = 40;
    private const int PadTB = 28;
    private const int FontSize = 32; // 프로젝트 dominant sizes: 36/32/24. 튜토리얼은 32.

    [MenuItem("Tools/Tutorial/Build Bubble Prefab + Overlay Canvas")]
    public static void Build()
    {
        BuildPrefab();
        PlaceOverlayInManagers();
    }

    [MenuItem("Tools/Tutorial/Place TutorialController + Wire Refs")]
    public static void PlaceController()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject canvasGO = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { canvasGO = r; break; }
        if (canvasGO == null) { Debug.LogError("[Tutorial] TutorialOverlayCanvas 미발견. 먼저 Build 실행."); return; }

        GameObject ctrlGO = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialController") { ctrlGO = r; break; }
        if (ctrlGO == null)
        {
            ctrlGO = new GameObject("TutorialController");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(ctrlGO, scene);
        }

        var ctrl = ctrlGO.GetComponent<TutorialController>();
        if (ctrl == null) ctrl = ctrlGO.AddComponent<TutorialController>();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        var bubbleComp = prefab != null ? prefab.GetComponent<TutorialBubble>() : null;

        var so = new SerializedObject(ctrl);
        so.FindProperty("bubblePrefab").objectReferenceValue = bubbleComp;
        so.FindProperty("overlayCanvas").objectReferenceValue = canvasGO.GetComponent<RectTransform>();
        // catalog는 나중에 SO 생성 후 wire (Step 3의 다음 단계)
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Tutorial] TutorialController 배치 + wire 완료 (catalog는 별도 assign 필요)");
    }

    private static void BuildPrefab()
    {
        Sprite bodySprite = null, tailSprite = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
        {
            if (a is Sprite s)
            {
                // 원본 매핑: _0 = body (sliced), _1 = tail (simple)
                if (s.name == "ui_speechBubble_0") bodySprite = s;
                else if (s.name == "ui_speechBubble_1") tailSprite = s;
            }
        }
        if (bodySprite == null || tailSprite == null)
        {
            Debug.LogError($"[Tutorial] 스프라이트 로드 실패: {AtlasPath}");
            return;
        }

        TMP_FontAsset font = null;
        var speechPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpeechBubblePrefab);
        if (speechPrefab != null)
        {
            var speechTmp = speechPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (speechTmp != null) font = speechTmp.font;
        }

        // Root
        var root = new GameObject("TutorialBubble", typeof(RectTransform), typeof(TutorialBubble));
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = new Vector2(0, 0);
        rootRt.anchorMax = new Vector2(0, 0);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.sizeDelta = Vector2.zero;

        // Body: Image (sliced) + VLG + CSF
        var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        bodyGO.transform.SetParent(root.transform, false);
        var bodyRt = (RectTransform)bodyGO.transform;
        bodyRt.anchorMin = new Vector2(0, 0);
        bodyRt.anchorMax = new Vector2(0, 0);
        bodyRt.pivot = new Vector2(0.5f, 0f);
        // Body 하단이 tail top 근처에 오도록 anchor 위치: tail 안의 base 부분과 overlap.
        bodyRt.anchoredPosition = new Vector2(0, TailHeight - TailOverlapWithBubble);

        var bodyImg = bodyGO.GetComponent<Image>();
        bodyImg.sprite = bodySprite;
        bodyImg.type = Image.Type.Sliced;
        bodyImg.raycastTarget = false;
        bodyImg.color = Color.white;

        var vlg = bodyGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(PadLR, PadLR, PadTB, PadTB);
        vlg.spacing = 10;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var csf = bodyGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Body 최소 너비 보장 (짧은 텍스트에도 아주 좁아지진 않게)
        var bodyLE = bodyGO.AddComponent<LayoutElement>();
        bodyLE.minWidth = BodyMinWidth;

        // OptionalImage
        var imgGO = new GameObject("OptionalImage", typeof(RectTransform), typeof(CanvasRenderer),
                                    typeof(Image), typeof(LayoutElement));
        imgGO.transform.SetParent(bodyGO.transform, false);
        var img = imgGO.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = true;
        var imgLE = imgGO.GetComponent<LayoutElement>();
        imgLE.preferredWidth = 360;
        imgLE.preferredHeight = 200;
        imgGO.SetActive(false);

        // Contents (TMP)
        var textGO = new GameObject("Contents", typeof(RectTransform), typeof(CanvasRenderer),
                                     typeof(TextMeshProUGUI));
        textGO.transform.SetParent(bodyGO.transform, false);
        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "튜토리얼 안내 텍스트";
        tmp.fontSize = FontSize;
        tmp.fontStyle = FontStyles.Normal;
        tmp.fontWeight = FontWeight.Regular;
        tmp.color = new Color(0.19f, 0.13f, 0.09f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        // Tail (Body와 sibling)
        var tailGO = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tailGO.transform.SetParent(root.transform, false);
        var tailRt = (RectTransform)tailGO.transform;
        tailRt.anchorMin = new Vector2(0, 0);
        tailRt.anchorMax = new Vector2(0, 0);
        tailRt.pivot = new Vector2(0.5f, 0f); // tail 하단 중앙 = tip
        tailRt.sizeDelta = new Vector2(TailWidth, TailHeight);
        tailRt.anchoredPosition = Vector2.zero; // tail 하단이 root origin

        var tailImg = tailGO.GetComponent<Image>();
        tailImg.sprite = tailSprite;
        tailImg.type = Image.Type.Simple;
        tailImg.raycastTarget = false;

        // Wire refs
        var bubble = root.GetComponent<TutorialBubble>();
        var so = new SerializedObject(bubble);
        so.FindProperty("body").objectReferenceValue = bodyRt;
        so.FindProperty("tail").objectReferenceValue = tailRt;
        so.FindProperty("contents").objectReferenceValue = tmp;
        so.FindProperty("optionalImage").objectReferenceValue = img;
        so.FindProperty("tailBodyOverlap").floatValue = TailOverlapWithBubble;
        so.FindProperty("tailTipVisualPadding").floatValue = 8f;
        so.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[Tutorial] Prefab 생성 완료: {PrefabPath}");
    }

    private static void PlaceOverlayInManagers()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject existing = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { existing = r; break; }

        GameObject go;
        if (existing != null) go = existing;
        else
        {
            go = new GameObject("TutorialOverlayCanvas", typeof(Canvas), typeof(CanvasScaler),
                                 typeof(GraphicRaycaster));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        }

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Tutorial] TutorialOverlayCanvas placed in Managers scene");
    }
}
