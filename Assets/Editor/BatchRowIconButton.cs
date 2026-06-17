using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class BatchRowIconButton
{
    private const string PREFAB_PATH = "Assets/Bundles/Prefabs/recipebook/inventory/BatchRow.prefab";
    private const string ICON_PATH = "Assets/Bundles/driveAssets/art/obj/Cooking/obj_trashcan_default.png";

    [MenuItem("Tools/Inventory/Convert Discard To Icon")]
    public static void Convert()
    {
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(ICON_PATH);
        if (icon == null) { Debug.LogError($"[BatchRowIconButton] 아이콘 못 찾음: {ICON_PATH}"); return; }

        var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) { Debug.LogError("[BatchRowIconButton] prefab load 실패"); return; }

        var btn = root.transform.Find("DiscardButton");
        if (btn == null) { Debug.LogError("[BatchRowIconButton] DiscardButton 없음"); PrefabUtility.UnloadPrefabContents(root); return; }

        // 기존 텍스트 라벨 제거
        var label = btn.Find("Label");
        if (label != null) Object.DestroyImmediate(label.gameObject);

        // 버튼 배경 → 투명, 아이콘만 보이게
        var bgImg = btn.GetComponent<Image>();
        if (bgImg != null) bgImg.color = new Color(0f, 0f, 0f, 0f);

        // 아이콘 자식 추가
        var iconGO = new GameObject("Icon", typeof(Image));
        iconGO.transform.SetParent(btn, false);
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = icon;
        iconImg.color = Color.black;                  // silhouette
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;                // 클릭은 부모 Button만
        var iconRt = (RectTransform)iconImg.transform;
        iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(4, 2); iconRt.offsetMax = new Vector2(-4, -2);

        // 버튼 크기 정사각형에 가깝게
        var le = btn.GetComponent<LayoutElement>();
        if (le != null) { le.preferredWidth = 36; le.preferredHeight = 36; }

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[BatchRowIconButton] 아이콘 버튼으로 변환 완료");
    }
}
