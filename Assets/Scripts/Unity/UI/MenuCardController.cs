using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuCardController : SingletonMonoBehaviour<MenuCardController>
{
    private static bool isMenuCardActive = false;
    public static bool IsMenuCardActive => isMenuCardActive;

    // 세션 내 마지막 탭 기억 (static이라 Destroy 후에도 유지)
    private static bool lastTabWasIngredient = false;

    [Header("Header")]
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private Image foodToolImage;
    [SerializeField] private Image foodImage;
    [Header("Tabs")]
    [SerializeField] private Image recipeTab;
    [SerializeField] private TextMeshProUGUI recipeTabLabel;
    [SerializeField] private Image ingredientTab;
    [SerializeField] private TextMeshProUGUI ingredientTabLabel;
    [Header("Containers")]
    [SerializeField] private Transform recipeContainer;
    [SerializeField] private Transform ingredientContainer;

    // 탭 색상: UIColors 경유. 활성=TabActive/TextPrimary, 비활성=ButtonSecondary/TextSecondary.

    private GameObject recipeLineTemplate;
    private GameObject ingredientLineTemplate;
    private readonly List<GameObject> spawnedLines = new();
    private readonly List<GameObject> spawnedIngredientLines = new();

    protected override void OnSingletonAwake()
    {
        isMenuCardActive = false;
        CacheTemplates();
    }

    private void CacheTemplates()
    {
        if (recipeContainer == null || recipeContainer.childCount == 0) return;

        recipeLineTemplate = recipeContainer.GetChild(0).gameObject;
        for (int i = 0; i < recipeContainer.childCount; i++)
            recipeContainer.GetChild(i).gameObject.SetActive(false);

        if (ingredientContainer != null)
        {
            Transform content = ingredientContainer.Find("Viewport/Content");
            if (content != null && content.childCount > 0)
            {
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child.childCount > 0 && ingredientLineTemplate == null)
                        ingredientLineTemplate = child.gameObject;
                    child.gameObject.SetActive(false);
                }
                if (ingredientLineTemplate == null) ingredientLineTemplate = content.GetChild(0).gameObject;
            }
        }
    }

    public void InitSlot(string foodId)
    {
        FoodData foodData = SearchDataUtil.GetFoodDataById(foodId);
        if (foodData == null) return;

        if (nameLabel != null) nameLabel.text = foodData.ingredientName;

        List<RecipeData> chain = MenuCardRecipeBuilder.BuildRecipeChain(foodId);
        ApplyHeaderImage(foodData, chain);
        PopulateRecipeLines(chain);
        PopulateIngredients(foodId);

        if (lastTabWasIngredient) OpenIngredient();
        else OpenRecipe();
        isMenuCardActive = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    private void ApplyHeaderImage(FoodData foodData, List<RecipeData> chain)
    {
        if (chain.Count == 0)
        {
            if (foodToolImage != null) foodToolImage.gameObject.SetActive(false);
            if (foodImage != null) foodImage.sprite = foodData.GetRepresentativeBentoImage();
            return;
        }

        var lastRecipe = chain[chain.Count - 1];
        string lastToolId = GameSessionRoot.Instance?.RecipeLookup?.GetToolIdForMinigame(lastRecipe.minigameId);
        CookingToolData lastToolData = !string.IsNullOrEmpty(lastToolId)
            ? SearchDataUtil.GetCookingToolDataById(lastToolId) : null;

        if (foodToolImage != null)
        {
            bool hasTool = lastToolData != null;
            foodToolImage.gameObject.SetActive(hasTool);
            if (hasTool) foodToolImage.sprite = lastToolData.defaultImage;
        }
        if (foodImage != null)
            foodImage.sprite = !string.IsNullOrEmpty(lastToolId)
                ? foodData.GetImageForTool(lastToolId) : foodData.GetRepresentativeBentoImage();
    }

    private void PopulateRecipeLines(List<RecipeData> chain)
    {
        ClearRecipeLines();
        if (recipeLineTemplate == null) return;

        foreach (var recipe in chain)
        {
            GameObject lineGO = Instantiate(recipeLineTemplate, recipeContainer);
            lineGO.SetActive(true);
            spawnedLines.Add(lineGO);
            PopulateRecipeLine(lineGO.transform, recipe);
        }

        AdjustRecipeVerticalSpacing();
    }

    // 짧아도 line끼리 조금 겹치는 게 원래 의도 — 항상 음수 고정.
    // 긴 레시피(N=5+)면 계산으로 더 겹침 필요할 때만 그 값 사용.
    private const float DefaultLineOverlap = -10f;

    /// <summary>
    /// Recipe.VLG.spacing 계산 — 항상 살짝 겹침(DefaultLineOverlap).
    /// N 많아서 Recipe 영역 넘칠 위험이면 더 음수로.
    /// </summary>
    private void AdjustRecipeVerticalSpacing()
    {
        var rt = recipeContainer as RectTransform;
        if (rt == null) return;
        var vlg = rt.GetComponent<VerticalLayoutGroup>();
        if (vlg == null || spawnedLines.Count <= 1) return;

        var recipeLE = rt.GetComponent<LayoutElement>();
        float recipeH = recipeLE != null && recipeLE.preferredHeight > 0
            ? recipeLE.preferredHeight : rt.rect.height;
        float available = recipeH - vlg.padding.top - vlg.padding.bottom;

        var firstLE = spawnedLines[0].GetComponent<LayoutElement>();
        float lineH = firstLE != null && firstLE.preferredHeight > 0
            ? firstLE.preferredHeight : spawnedLines[0].GetComponent<RectTransform>().rect.height;
        if (lineH <= 0) return;

        int n = spawnedLines.Count;
        float fitSpacing = (available - n * lineH) / (n - 1);
        // 항상 DefaultLineOverlap 이하 (더 음수). fit 계산이 더 작으면 그거 사용 (더 겹침).
        vlg.spacing = Mathf.Min(DefaultLineOverlap, fitSpacing);
        vlg.childAlignment = TextAnchor.UpperCenter;
    }

    private void PopulateRecipeLine(Transform line, RecipeData recipe)
    {
        string toolId = GameSessionRoot.Instance?.RecipeLookup?.GetToolIdForMinigame(recipe.minigameId);
        CookingToolData toolData = !string.IsNullOrEmpty(toolId)
            ? SearchDataUtil.GetCookingToolDataById(toolId) : null;

        Transform inputContainer = line.Find("Input");
        if (inputContainer != null)
            MenuCardItemHelper.PopulateInputSection(inputContainer, recipe, toolData);

        // Minigame 섹션(bg disable, label 숨김, arrow ">" 등)은 prefab 정적 설정으로 이관.

        Transform resultContainer = line.Find("Result");
        if (resultContainer != null && resultContainer.childCount > 0)
            MenuCardItemHelper.PopulateResultItem(resultContainer.GetChild(0), recipe.outputFood, toolId, toolData);
    }

    public void OpenRecipe()
    {
        lastTabWasIngredient = false;
        if (recipeContainer != null) recipeContainer.gameObject.SetActive(true);
        if (ingredientContainer != null) ingredientContainer.gameObject.SetActive(false);

        if (recipeTab != null) recipeTab.color = UIColors.TabActive;
        if (recipeTabLabel != null) recipeTabLabel.color = UIColors.TextPrimary;
        if (ingredientTab != null) ingredientTab.color = UIColors.ButtonSecondary;
        if (ingredientTabLabel != null) ingredientTabLabel.color = UIColors.TextSecondary;
    }

    public void OpenIngredient()
    {
        lastTabWasIngredient = true;
        if (recipeContainer != null) recipeContainer.gameObject.SetActive(false);
        if (ingredientContainer != null) ingredientContainer.gameObject.SetActive(true);

        if (ingredientTab != null) ingredientTab.color = UIColors.TabActive;
        if (ingredientTabLabel != null) ingredientTabLabel.color = UIColors.TextPrimary;
        if (recipeTab != null) recipeTab.color = UIColors.ButtonSecondary;
        if (recipeTabLabel != null) recipeTabLabel.color = UIColors.TextSecondary;
    }

    public void ClearMenuCard()
    {
        ClearRecipeLines();
        ClearIngredientLines();
    }

    public void CloseMenuCard()
    {
        isMenuCardActive = false;
        Destroy(gameObject);
    }

    private void ClearRecipeLines()
    {
        foreach (var line in spawnedLines)
            if (line != null) Destroy(line);
        spawnedLines.Clear();
    }

    private void ClearIngredientLines()
    {
        foreach (var line in spawnedIngredientLines)
            if (line != null) Destroy(line);
        spawnedIngredientLines.Clear();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif

    private void PopulateIngredients(string foodId)
    {
        ClearIngredientLines();
        if (ingredientLineTemplate == null || ingredientContainer == null) return;

        Transform content = ingredientContainer.Find("Viewport/Content");
        if (content == null) return;

        List<FoodData> ingredients = MenuCardRecipeBuilder.GetUniqueIngredients(foodId);

        foreach (var ingredient in ingredients)
        {
            GameObject cellGO = Instantiate(ingredientLineTemplate, content);
            cellGO.SetActive(true);
            spawnedIngredientLines.Add(cellGO);

            Image icon = cellGO.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null) icon.sprite = ingredient.image;

            TextMeshProUGUI nameTMP = cellGO.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameTMP != null) nameTMP.text = ingredient.ingredientName;
        }
    }
}
