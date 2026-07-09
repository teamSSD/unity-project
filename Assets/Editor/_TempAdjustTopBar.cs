using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>일회성. StatUI 프리팹의 TopBar 높이 조정 (배경 테이블과 gap 메움).</summary>
public static class _TempAdjustTopBar
{
    private const string PrefabPath = "Assets/Bundles/Prefabs/ui/StatUI.prefab";
    private const float NewHeight = 85f;

    [MenuItem("Tools/Diagnostics/Adjust StatUI TopBar Height")]
    public static void Run()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform topBar = null;
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name == "TopBar") { topBar = rt; break; }
            }
            if (topBar == null) { Debug.LogError("[TopBar] StatUI 프리팹에서 TopBar 미발견"); return; }

            var trt = (RectTransform)topBar;
            Vector2 size = trt.sizeDelta;
            float oldY = size.y;
            size.y = NewHeight;
            trt.sizeDelta = size;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"[TopBar] SizeDelta.y {oldY} → {NewHeight}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
