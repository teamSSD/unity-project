using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuCardController : MonoBehaviour
{
    private static MenuCardController instance;
    public static MenuCardController Instance => instance;

    private static bool isMenuCardActive = false;
    public static bool IsMenuCardActive => isMenuCardActive;

    // 세션 내 마지막 탭 기억 (static이라 Destroy 후에도 유지)
    private static bool lastTabWasIngredient = false;

    // 자동 바인딩 (Awake에서 계층구조 기반으로 찾음)
    private TextMeshProUGUI nameLabel;
    private Image foodToolImage;
    private Image foodImage;
    private Image recipeTab;
    private TextMeshProUGUI recipeTabLabel;
    private Image ingredientTab;
    private TextMeshProUGUI ingredientTabLabel;
    private Transform recipeContainer;
    private Transform ingredientContainer;

    private static readonly Color EnableButton = new Color(222f / 255f, 215f / 255f, 207f / 255f, 1f); // #DED7CF
    private static readonly Color EnableText = new Color(0.286f, 0.259f, 0.259f, 1f);
    private static readonly Color DisableButton = new Color(139f / 255f, 125f / 255f, 109f / 255f, 1f); // #8B7D6D
    private static readonly Color DisableText = new Color(0.388f, 0.353f, 0.333f, 1f);


    private static readonly Dictionary<string, string> MinigameLabels = new()
    {
        { "M001", "Grill" },
        { "M002", "Boil" },
        { "M004", "Mix" },
        { "M005", "Sauce" },
        { "M006", "Slice" },
        { "M007", "Roast" },
    };

    private GameObject recipeLineTemplate;
    private GameObject itemTemplate;
    private GameObject ingredientLineTemplate;
    private List<GameObject> spawnedLines = new List<GameObject>();
    private List<GameObject> spawnedIngredientLines = new List<GameObject>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        isMenuCardActive = false;

        BindReferences();
        CacheTemplates();
    }

    private void BindReferences()
    {
        // Header > Image > Text (TMP)
        nameLabel = transform.Find("Header/Image/Text (TMP)")?.GetComponent<TextMeshProUGUI>();

        // FoodImage > Tool (요리도구 배경, sibling 0) + Image (음식 오버레이)
        foodToolImage = transform.Find("FoodImage/Tool")?.GetComponent<Image>();
        foodImage = transform.Find("FoodImage/Image")?.GetComponent<Image>();

        // RecipeAndIngredient > Header > RecipeTab / IngredientTab
        Transform riHeader = transform.Find("RecipeAndIngredient/Header");
        if (riHeader != null)
        {
            recipeTab = riHeader.Find("RecipeTab")?.GetComponent<Image>();
            recipeTabLabel = riHeader.Find("RecipeTab/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            ingredientTab = riHeader.Find("IngredientTab")?.GetComponent<Image>();
            ingredientTabLabel = riHeader.Find("IngredientTab/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        }

        // RecipeAndIngredient > Recipe / Ingredient
        recipeContainer = transform.Find("RecipeAndIngredient/Recipe");
        ingredientContainer = transform.Find("RecipeAndIngredient/Ingredient");
    }

    private void CacheTemplates()
    {
        if (recipeContainer == null || recipeContainer.childCount == 0) return;

        // RecipeLine 템플릿: Recipe 컨테이너의 첫 번째 자식
        recipeLineTemplate = recipeContainer.GetChild(0).gameObject;

        // Item 템플릿: RecipeLine > Input > 첫 번째 Item
        Transform input = recipeLineTemplate.transform.Find("Input");
        if (input != null && input.childCount > 0)
            itemTemplate = input.GetChild(0).gameObject;

        // 모든 기존 RecipeLine 비활성화
        for (int i = 0; i < recipeContainer.childCount; i++)
            recipeContainer.GetChild(i).gameObject.SetActive(false);

        // IngredientLine 템플릿: Ingredient > Viewport > Content > 첫 번째 IngredientLine (자식이 있는 것 위주)
        if (ingredientContainer != null)
        {
            Transform content = ingredientContainer.Find("Viewport/Content");
            if (content != null && content.childCount > 0)
            {
                // 빈 자식은 템플릿으로 쓰지 않도록 검사
                for (int i = 0; i < content.childCount; i++)
                {
                    Transform child = content.GetChild(i);
                    if (child.childCount > 0 && ingredientLineTemplate == null)
                    {
                        ingredientLineTemplate = child.gameObject;
                    }
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

        // 헤더 설정
        if (nameLabel != null) nameLabel.text = foodData.ingredientName;

        // 레시피 체인 구축
        List<RecipeData> chain = BuildRecipeChain(foodId);

        // 상단 요리 이미지: 최종 레시피의 도구 + 음식 표시
        if (chain.Count > 0)
        {
            var lastRecipe = chain[chain.Count - 1];
            string lastToolId = RecipeDataManager.Instance?.GetToolIdForMinigame(lastRecipe.minigameId);
            CookingToolData lastToolData = !string.IsNullOrEmpty(lastToolId)
                ? SearchDataUtil.GetCookingToolDataById(lastToolId) : null;

            if (foodToolImage != null)
            {
                if (lastToolData != null)
                {
                    foodToolImage.gameObject.SetActive(true);
                    foodToolImage.sprite = lastToolData.defaultImage;
                }
                else
                {
                    foodToolImage.gameObject.SetActive(false);
                }
            }
            if (foodImage != null)
                foodImage.sprite = !string.IsNullOrEmpty(lastToolId)
                    ? foodData.GetImageForTool(lastToolId) : foodData.GetRepresentativeBentoImage();
        }
        else
        {
            if (foodToolImage != null) foodToolImage.gameObject.SetActive(false);
            if (foodImage != null) foodImage.sprite = foodData.GetRepresentativeBentoImage();
        }

        // 레시피 라인 표시
        PopulateRecipeLines(chain);
        // 재료 리스트 표시
        PopulateIngredients(foodId);

        if (lastTabWasIngredient)
            OpenIngredient();
        else
            OpenRecipe();
        isMenuCardActive = true;
    }

    /// <summary>
    /// DFS 바텀업으로 레시피 체인 구축.
    /// 가장 기본 단계부터 최종 요리까지 순서대로 반환.
    /// </summary>
    private List<RecipeData> BuildRecipeChain(string foodId)
    {
        var result = new List<RecipeData>();
        var visited = new HashSet<string>();
        CollectRecipes(foodId, result, visited);
        return result;
    }

    private void CollectRecipes(string foodId, List<RecipeData> result, HashSet<string> visited)
    {
        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(foodId);
        if (recipe == null) return;
        if (!visited.Add(recipe.id)) return;

        foreach (var input in recipe.inputs)
            CollectRecipes(input.food.id, result, visited);

        result.Add(recipe);
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
    }

    private void PopulateRecipeLine(Transform line, RecipeData recipe)
    {
        string toolId = null;
        if (RecipeDataManager.Instance != null)
            toolId = RecipeDataManager.Instance.GetToolIdForMinigame(recipe.minigameId);

        CookingToolData toolData = null;
        if (!string.IsNullOrEmpty(toolId))
            toolData = SearchDataUtil.GetCookingToolDataById(toolId);

        // === Input 섹션 ===
        Transform inputContainer = line.Find("Input");
        if (inputContainer != null)
            PopulateInputSection(inputContainer, recipe);

        // === Minigame 섹션 ===
        Transform minigame = line.Find("Minigame");
        if (minigame != null)
        {
            // 배경 이미지 비활성화
            Image minigameBg = minigame.GetComponent<Image>();
            if (minigameBg != null)
                minigameBg.enabled = false;

            // 조리법 라벨 (영어, Bold, 24pt)
            TextMeshProUGUI label = minigame.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = GetMinigameLabel(recipe.minigameId);
                label.fontSize = 24;
                label.fontStyle = FontStyles.Bold;
            }

            // 화살표 (글씨 위에 배치, 색상 글씨와 동일)
            Transform arrow = minigame.Find("Arrow");
            if (arrow != null)
            {
                arrow.SetAsFirstSibling();
                TextMeshProUGUI arrowText = arrow.GetComponent<TextMeshProUGUI>();
                if (arrowText != null)
                {
                    arrowText.text = "\u2192";
                    arrowText.fontSize = 24;
                    if (label != null) arrowText.color = label.color;
                }
            }
        }

        // === Result 섹션 ===
        Transform resultContainer = line.Find("Result");
        if (resultContainer != null && resultContainer.childCount > 0)
        {
            Transform resultItem = resultContainer.GetChild(0);
            PopulateResultItem(resultItem, recipe.outputFood, toolId, toolData);
        }
    }

    private void PopulateInputSection(Transform inputContainer, RecipeData recipe)
    {
        // 기존 아이템 수와 필요한 수 비교
        int existingCount = inputContainer.childCount;
        int neededCount = recipe.inputs.Count;

        // 부족하면 첫 번째 자식을 복제
        if (existingCount > 0)
        {
            GameObject template = inputContainer.GetChild(0).gameObject;
            for (int i = existingCount; i < neededCount; i++)
            {
                GameObject newItem = Instantiate(template, inputContainer);
                newItem.SetActive(true);
            }
        }

        // 초과분 비활성화
        for (int i = neededCount; i < inputContainer.childCount; i++)
            inputContainer.GetChild(i).gameObject.SetActive(false);

        // 각 입력 재료 설정
        for (int i = 0; i < neededCount; i++)
        {
            Transform item = inputContainer.GetChild(i);
            item.gameObject.SetActive(true);
            PopulateInputItem(item, recipe.inputs[i].food);
        }

        // 동적 간격 조절 (할당된 공간에 맞춰서 줄이기)
        if (inputContainer is RectTransform rectTrans)
        {
            AdjustSpacing(rectTrans);
        }
    }

    private void AdjustSpacing(RectTransform container)
    {
        var layoutGroup = container.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup == null) return;

        // 부모(RecipeLine) 레벨에서 레이아웃 강제 갱신 (침범 방지)
        RectTransform parentRT = container.parent as RectTransform;
        if (parentRT != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRT);
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);
        }

        // 가용 너비 계산: 부모 너비에서 Minigame 및 Result 섹션이 차지할 최소 공간을 제외
        float availableWidth = 250f; // 기본 안전값
        if (parentRT != null && parentRT.rect.width > 0)
        {
            // 카드 전체 너비에서 Minigame 영역(약 120px) + Result 영역(70px) + 여유를 고려하여 약 200px 차감
            availableWidth = Mathf.Max(100f, parentRT.rect.width - 200f);
        }

        int activeCount = 0;
        float itemWidth = 0;

        foreach (Transform child in container)
        {
            if (child.gameObject.activeSelf)
            {
                activeCount++;
                if (itemWidth == 0)
                {
                    var rt = child.GetComponent<RectTransform>();
                    // 프리팹에서는 너비가 0일 수 있으므로 기본값 70을 대비책으로 사용
                    itemWidth = (rt != null && rt.rect.width > 0) ? rt.rect.width : 70f;
                }
            }
        }

        // 아이템 가로가 유효하지 않거나 1개 이하면 간격 조절 불필요
        if (activeCount <= 1 || itemWidth <= 0)
        {
            layoutGroup.spacing = 0;
            return;
        }

        float totalChildWidth = activeCount * itemWidth;

        // 실제 가용 너비를 초과할 때만 겹치도록 설정
        if (totalChildWidth > availableWidth)
        {
            // (가용 너비 - 아이템 총합) / (간격 개수)
            float neededSpacing = (availableWidth - totalChildWidth) / (activeCount - 1);
            // 0보다 커지지 않도록 (벌어지지 않도록) 제한
            layoutGroup.spacing = Mathf.Min(0, neededSpacing);
        }
        else
        {
            layoutGroup.spacing = 0;
        }
    }

    private void PopulateInputItem(Transform item, FoodData food)
    {
        Image toolImage = item.Find("Tool")?.GetComponent<Image>();
        Image ingredientImage = item.Find("Ingredient")?.GetComponent<Image>();

        if (ingredientImage == null) return;

        if (food.type == FoodType.INGREDIENT)
        {
            // 원재료: 도구 숨기고 원본 이미지만 표시
            if (toolImage != null)
                toolImage.gameObject.SetActive(false);
            ingredientImage.sprite = food.image;
        }
        else
        {
            // 중간재료: 해당 재료를 만든 도구 찾아서 표시
            RecipeData sourceRecipe = SearchDataUtil.GetRecipeDataByFoodId(food.id);
            string sourceToolId = null;
            if (sourceRecipe != null && RecipeDataManager.Instance != null)
                sourceToolId = RecipeDataManager.Instance.GetToolIdForMinigame(sourceRecipe.minigameId);

            if (!string.IsNullOrEmpty(sourceToolId))
            {
                CookingToolData sourceToolData = SearchDataUtil.GetCookingToolDataById(sourceToolId);
                if (toolImage != null)
                {
                    toolImage.gameObject.SetActive(true);
                    toolImage.sprite = sourceToolData?.defaultImage;
                }
                ingredientImage.sprite = food.GetImageForTool(sourceToolId);
            }
            else
            {
                if (toolImage != null)
                    toolImage.gameObject.SetActive(false);
                ingredientImage.sprite = food.image;
            }
        }
    }

    private void PopulateResultItem(Transform item, FoodData food, string toolId, CookingToolData toolData)
    {
        Image toolImage = item.Find("Tool")?.GetComponent<Image>();
        Image ingredientImage = item.Find("Ingredient")?.GetComponent<Image>();

        if (ingredientImage == null) return;

        if (toolImage != null)
        {
            toolImage.gameObject.SetActive(true);
            toolImage.sprite = toolData?.defaultImage;
        }

        ingredientImage.sprite = !string.IsNullOrEmpty(toolId)
            ? food.GetImageForTool(toolId)
            : food.image;
    }

    private string GetMinigameLabel(string minigameId)
    {
        return MinigameLabels.TryGetValue(minigameId, out string label) ? label : "조리";
    }

    public void OpenRecipe()
    {
        lastTabWasIngredient = false;

        if (recipeContainer != null) recipeContainer.gameObject.SetActive(true);
        if (ingredientContainer != null) ingredientContainer.gameObject.SetActive(false);

        if (recipeTab != null) recipeTab.color = EnableButton;
        if (recipeTabLabel != null) recipeTabLabel.color = EnableText;
        if (ingredientTab != null) ingredientTab.color = DisableButton;
        if (ingredientTabLabel != null) ingredientTabLabel.color = DisableText;
    }

    public void OpenIngredient()
    {
        lastTabWasIngredient = true;

        if (recipeContainer != null) recipeContainer.gameObject.SetActive(false);
        if (ingredientContainer != null) ingredientContainer.gameObject.SetActive(true);

        if (ingredientTab != null) ingredientTab.color = EnableButton;
        if (ingredientTabLabel != null) ingredientTabLabel.color = EnableText;
        if (recipeTab != null) recipeTab.color = DisableButton;
        if (recipeTabLabel != null) recipeTabLabel.color = DisableText;
    }

    public void ClearMenuCard()
    {
        ClearRecipeLines();
        ClearIngredientLines();
    }

    public void CloseMenuCard()
    {
        isMenuCardActive = false;
        instance = null;
        Destroy(gameObject);
    }

    private void ClearRecipeLines()
    {
        foreach (var line in spawnedLines)
        {
            if (line != null)
                Destroy(line);
        }
        spawnedLines.Clear();
    }

    private void ClearIngredientLines()
    {
        foreach (var line in spawnedIngredientLines)
        {
            if (line != null)
                Destroy(line);
        }
        spawnedIngredientLines.Clear();
    }

    private void PopulateIngredients(string foodId)
    {
        ClearIngredientLines();

        if (ingredientLineTemplate == null || ingredientContainer == null) return;

        Transform content = ingredientContainer.Find("Viewport/Content");
        if (content == null) return;

        List<FoodData> ingredients = GetUniqueIngredients(foodId);

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

    private List<FoodData> GetUniqueIngredients(string foodId)
    {
        var ingredients = new List<FoodData>();
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(foodId);

        while (queue.Count > 0)
        {
            string currentId = queue.Dequeue();
            if (!visited.Add(currentId)) continue;

            FoodData data = SearchDataUtil.GetFoodDataById(currentId);
            if (data == null) continue;

            if (data.type == FoodType.INGREDIENT)
            {
                ingredients.Add(data);
            }
            else
            {
                RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(currentId);
                if (recipe != null)
                {
                    foreach (var input in recipe.inputs)
                        queue.Enqueue(input.food.id);
                }
            }
        }
        return ingredients;
    }
}
