using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>일회성. TutorialBubble.prefab의 Body Image PixelsPerUnitMultiplier +
/// TutorialBubble MonoBehaviour의 tailTipVisualPadding 조정.
/// - Body border 40px sliced가 tail 축소 스케일(60/126)과 매칭되도록 multiplier로 시각 축소
/// - Border 얇아진 만큼 tail-body 시각 gap 발생 → tailTipVisualPadding으로 tail 위치 재조정</summary>
public static class _TempAdjustBubbleBorder
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/tutorial/TutorialBubble.prefab";
    private const float NewMultiplier = 2.1f;      // 126/60, tail 축소율 역수 (border 40px → 19px)
    private const float NewTailPadding = 15.18f;   // 사용자 튜닝: tail Y = -15.18로 고정

    [MenuItem("Tools/Tutorial/Adjust Bubble Border")]
    public static void Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) { Debug.LogError($"[Bubble] Prefab 로드 실패: {PrefabPath}"); return; }

        var body = root.transform.Find("Body");
        if (body == null) { Debug.LogError("[Bubble] Body 자식 미발견"); PrefabUtility.UnloadPrefabContents(root); return; }

        var img = body.GetComponent<Image>();
        if (img == null) { Debug.LogError("[Bubble] Body Image 컴포넌트 미발견"); PrefabUtility.UnloadPrefabContents(root); return; }

        float oldMul = img.pixelsPerUnitMultiplier;
        img.pixelsPerUnitMultiplier = NewMultiplier;
        Debug.Log($"[Bubble] PixelsPerUnitMultiplier {oldMul} → {NewMultiplier}");

        var bubble = root.GetComponent<TutorialBubble>();
        if (bubble == null) { Debug.LogError("[Bubble] TutorialBubble 컴포넌트 미발견"); PrefabUtility.UnloadPrefabContents(root); return; }

        var so = new SerializedObject(bubble);
        var padProp = so.FindProperty("tailTipVisualPadding");
        float oldPad = padProp.floatValue;
        padProp.floatValue = NewTailPadding;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[Bubble] tailTipVisualPadding {oldPad} → {NewTailPadding}");

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }
}
