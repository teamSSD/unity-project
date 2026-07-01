using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class _TempShrinkMenuCardItems
{
    private const string PREFAB_PATH = "Assets/Bundles/Prefabs/recipebook/MenuCardV2.prefab";
    private const int NEW_SIZE = 90; // 가장 많은 줄(Input 5개) 기준으로 작게

    // root.sizeDelta.y(980) - padding(60) - spacing 2×(20) - Header(36) - FoodImage(300) = 544
    private const int RECIPE_AREA_HEIGHT = 544;
    private const int RECIPE_LINE_HEIGHT = 110; // Item 90 + 약간 여유

    [MenuItem("Tools/MenuCard/Shrink Recipe Items")]
    public static void Shrink()
    {
        var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
        if (root == null) { Debug.LogError("prefab load 실패"); return; }

        var line = root.transform.Find("RecipeAndIngredient/Recipe/RecipeLine");
        if (line == null) { Debug.LogError("RecipeLine 없음"); PrefabUtility.UnloadPrefabContents(root); return; }

        int changed = 0;

        var input = line.Find("Input");
        if (input != null)
        {
            foreach (Transform item in input)
            {
                if (SetItemSize(item)) changed++;
            }
        }

        var resultItem = line.Find("Result/Item");
        if (resultItem != null && SetItemSize(resultItem)) changed++;

        // RecipeLine height 축소 (Item 90 + 약간 여유)
        var lineLE = line.GetComponent<LayoutElement>();
        if (lineLE != null) { lineLE.preferredHeight = RECIPE_LINE_HEIGHT; lineLE.flexibleHeight = 0; }
        var lineRt = line.GetComponent<RectTransform>();
        if (lineRt != null) lineRt.sizeDelta = new Vector2(lineRt.sizeDelta.x, RECIPE_LINE_HEIGHT);

        // RecipeAndIngredient 고정 height — min/preferred/flexible=0 완전 잠금.
        // (min을 안 잡으면 자식 min이 preferred 넘어서 RAI가 강제로 늘어남 = 문제)
        var rai = root.transform.Find("RecipeAndIngredient");
        if (rai != null)
        {
            var raiLE = rai.GetComponent<LayoutElement>();
            if (raiLE != null)
            {
                raiLE.preferredHeight = RECIPE_AREA_HEIGHT;
                raiLE.minHeight = RECIPE_AREA_HEIGHT;
                raiLE.flexibleHeight = 0;
            }
        }

        // Recipe도 고정: RAI 안 Header(25) 제외한 519 강제. min으로 자식이 밀지 못하게.
        const int RAI_HEADER = 25;
        int recipeHeight = RECIPE_AREA_HEIGHT - RAI_HEADER;
        var recipe = root.transform.Find("RecipeAndIngredient/Recipe");
        if (recipe != null)
        {
            var recipeLE = recipe.GetComponent<LayoutElement>();
            if (recipeLE != null)
            {
                recipeLE.preferredHeight = recipeHeight;
                recipeLE.minHeight = recipeHeight;
                recipeLE.flexibleHeight = 0;
            }
        }

        // root VLG가 ChildForceExpandHeight=1, ChildControlHeight=0이면 FoodImage가 stretch됨.
        // 각 자식이 자기 preferredHeight 그대로 사용하도록 강제.
        var rootVlg = root.GetComponent<VerticalLayoutGroup>();
        if (rootVlg != null)
        {
            rootVlg.childControlHeight = true;
            rootVlg.childForceExpandHeight = false;
            rootVlg.childAlignment = TextAnchor.UpperCenter;
        }

        // Recipe.VLG.cch = 1 → RecipeLine.height = LE.preferredHeight(110) 강제 (sizeDelta.y=172 무시)
        var recipeVlg = recipe != null ? recipe.GetComponent<VerticalLayoutGroup>() : null;
        if (recipeVlg != null) recipeVlg.childControlHeight = true;

        // Input.HLG.ccw = 1 → Item.width = LE.preferredWidth(90) 강제 (sizeDelta.x=150 무시)
        var inputHlg = input != null ? input.GetComponent<HorizontalLayoutGroup>() : null;
        if (inputHlg != null) inputHlg.childControlWidth = true;

        // Minigame section 정적 설정 — 코드 DecorateMinigameSection 제거의 대체.
        var minigame = line.Find("Minigame");
        if (minigame != null)
        {
            var bgImg = minigame.GetComponent<Image>();
            if (bgImg != null) bgImg.enabled = false;

            var mgLabel = minigame.Find("Text (TMP)");
            if (mgLabel != null) mgLabel.gameObject.SetActive(false);

            var arrow = minigame.Find("Arrow");
            if (arrow != null)
            {
                arrow.gameObject.SetActive(true);
                var arrowTmp = arrow.GetComponent<TextMeshProUGUI>();
                if (arrowTmp != null)
                {
                    arrowTmp.text = ">";
                    arrowTmp.fontSize = 28;
                    arrowTmp.fontStyle = FontStyles.Bold;
                    arrowTmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"[ShrinkMenuCardItems] {changed} Item {NEW_SIZE}x{NEW_SIZE}, RecipeLine.h={RECIPE_LINE_HEIGHT}, RecipeAndIngredient.h={RECIPE_AREA_HEIGHT}");
    }

    private static bool SetItemSize(Transform item)
    {
        var le = item.GetComponent<LayoutElement>();
        if (le == null) return false;
        le.preferredWidth = NEW_SIZE;
        le.preferredHeight = NEW_SIZE;
        // sizeDelta도 같이 (LayoutGroup이 min 계산 시 sizeDelta 참조 → LE와 불일치하면 강제 override 안 됨)
        var rt = item.GetComponent<RectTransform>();
        if (rt != null) rt.sizeDelta = new Vector2(NEW_SIZE, NEW_SIZE);
        return true;
    }
}
