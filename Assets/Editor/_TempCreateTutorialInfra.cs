using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>일회성. TutorialBubble prefab + TutorialOverlayCanvas GO 생성/배치.</summary>
public static class _TempCreateTutorialInfra
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/tutorial/TutorialBubble.prefab";
    private const string ScenePath = "Assets/Scenes/ForReal/Managers.unity";
    private const string AtlasPath = "Assets/Bundles/driveAssets/art/ui/cooking/ui_speechBubble.png";

    [MenuItem("Tools/Tutorial/Build Bubble Prefab + Overlay Canvas")]
    public static void Build()
    {
        CreatePrefab();
        PlaceOverlayInManagers();
    }

    private static void CreatePrefab()
    {
        // 스프라이트 로드
        Sprite bodySprite = null, tailSprite = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(AtlasPath))
        {
            if (a is Sprite s)
            {
                if (s.name == "ui_speechBubble_1") bodySprite = s;
                else if (s.name == "ui_speechBubble_0") tailSprite = s;
            }
        }
        if (bodySprite == null || tailSprite == null)
        {
            Debug.LogError($"[Tutorial] 스프라이트 로드 실패: {AtlasPath}");
            return;
        }

        // SpeechBubble 프리팹에서 폰트 상속
        TMP_FontAsset font = null;
        var speechPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Bundles/Prefabs/cooking/SpeechBubble.prefab");
        if (speechPrefab != null)
        {
            var speechTmp = speechPrefab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (speechTmp != null) font = speechTmp.font;
        }

        // Root (visual 없음)
        var root = new GameObject("TutorialBubble", typeof(RectTransform), typeof(TutorialBubble));
        var rootRt = (RectTransform)root.transform;
        rootRt.anchorMin = new Vector2(0, 0);
        rootRt.anchorMax = new Vector2(0, 0);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.sizeDelta = Vector2.zero;

        // Body: Image + VLG + CSF
        var bodyGO = new GameObject("Body", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        bodyGO.transform.SetParent(root.transform, false);
        var bodyRt = (RectTransform)bodyGO.transform;
        bodyRt.anchorMin = new Vector2(0, 0);
        bodyRt.anchorMax = new Vector2(0, 0);
        bodyRt.pivot = new Vector2(0.5f, 0f); // pivot at bottom-center → position = 아래 중앙 좌표
        bodyRt.anchoredPosition = Vector2.zero;

        var bodyImg = bodyGO.GetComponent<Image>();
        bodyImg.sprite = bodySprite;
        bodyImg.type = Image.Type.Sliced;
        bodyImg.raycastTarget = false;
        bodyImg.color = Color.white;

        var vlg = bodyGO.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(60, 60, 50, 50);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;

        var csf = bodyGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // OptionalImage (Body의 자식)
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
        tmp.fontSize = 30;
        tmp.color = new Color(0.19f, 0.13f, 0.09f, 1f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;

        // Tail (Body와 sibling, target 좌표에 tip 꽂힘)
        var tailGO = new GameObject("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tailGO.transform.SetParent(root.transform, false);
        var tailRt = (RectTransform)tailGO.transform;
        tailRt.anchorMin = new Vector2(0, 0);
        tailRt.anchorMax = new Vector2(0, 0);
        tailRt.pivot = new Vector2(0.5f, 0f); // 스프라이트 하단 중앙 = tip 근사
        tailRt.sizeDelta = new Vector2(63, 92); // 원본 126x184의 절반
        tailRt.anchoredPosition = Vector2.zero;
        var tailImg = tailGO.GetComponent<Image>();
        tailImg.sprite = tailSprite;
        tailImg.raycastTarget = false;

        // Serialize refs wire
        var bubble = root.GetComponent<TutorialBubble>();
        var so = new SerializedObject(bubble);
        so.FindProperty("body").objectReferenceValue = bodyRt;
        so.FindProperty("tail").objectReferenceValue = tailRt;
        so.FindProperty("contents").objectReferenceValue = tmp;
        so.FindProperty("optionalImage").objectReferenceValue = img;
        so.ApplyModifiedPropertiesWithoutUndo();

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PrefabPath));
        AssetDatabase.Refresh();
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        Debug.Log($"[Tutorial] Prefab created at {PrefabPath}");
    }

    private static void PlaceOverlayInManagers()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject existing = null;
        foreach (var r in scene.GetRootGameObjects())
            if (r.name == "TutorialOverlayCanvas") { existing = r; break; }

        GameObject go;
        if (existing != null)
        {
            go = existing;
        }
        else
        {
            go = new GameObject("TutorialOverlayCanvas", typeof(Canvas), typeof(CanvasScaler),
                                 typeof(GraphicRaycaster));
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
        }

        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // 다른 Overlay UI 위에

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Tutorial] TutorialOverlayCanvas placed in Managers scene");
    }
}
