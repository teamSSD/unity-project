using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MenuCard 내 Input/Tool/Result 아이템 렌더링. MenuCardController에서 추출.
/// 모두 static — 인스턴스 상태 의존 없음.
/// </summary>
public static class MenuCardItemHelper
{
    /// <summary>Input 섹션 (재료들 + 도구) 동기화 후 layout adjust.</summary>
    public static void PopulateInputSection(Transform inputContainer, RecipeData recipe, CookingToolData toolData)
    {
        int existingCount = inputContainer.childCount;
        int ingredientCount = recipe.inputs.Count;
        int neededCount = ingredientCount + (toolData != null ? 1 : 0);

        // 부족하면 첫 자식 복제
        if (existingCount > 0)
        {
            GameObject template = inputContainer.GetChild(0).gameObject;
            for (int i = existingCount; i < neededCount; i++)
            {
                GameObject newItem = Object.Instantiate(template, inputContainer);
                newItem.SetActive(true);
            }
        }

        // 초과분 비활성화
        for (int i = neededCount; i < inputContainer.childCount; i++)
            inputContainer.GetChild(i).gameObject.SetActive(false);

        // 재료들
        for (int i = 0; i < ingredientCount; i++)
        {
            Transform item = inputContainer.GetChild(i);
            item.gameObject.SetActive(true);
            PopulateInputItem(item, recipe.inputs[i].food);
        }

        // 도구
        if (toolData != null)
        {
            Transform toolItem = inputContainer.GetChild(ingredientCount);
            toolItem.gameObject.SetActive(true);
            PopulateToolItem(toolItem, toolData);
        }

    }

    public static void PopulateInputItem(Transform item, FoodData food)
    {
        Image toolImage = item.Find("Tool")?.GetComponent<Image>();
        Image ingredientImage = item.Find("Ingredient")?.GetComponent<Image>();
        if (ingredientImage == null) return;

        if (food.type == FoodType.INGREDIENT)
        {
            if (toolImage != null) toolImage.gameObject.SetActive(false);
            ingredientImage.sprite = food.image;
            return;
        }

        // 중간재료: 해당 재료를 만든 도구 찾아서 표시
        RecipeData sourceRecipe = SearchDataUtil.GetRecipeDataByFoodId(food.id);
        string sourceToolId = sourceRecipe != null
            ? GameSessionRoot.Instance?.RecipeLookup?.GetToolIdForMinigame(sourceRecipe.minigameId)
            : null;

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
            if (toolImage != null) toolImage.gameObject.SetActive(false);
            ingredientImage.sprite = food.image;
        }
    }

    public static void PopulateToolItem(Transform item, CookingToolData toolData)
    {
        Image toolImage = item.Find("Tool")?.GetComponent<Image>();
        Image ingredientImage = item.Find("Ingredient")?.GetComponent<Image>();
        if (toolImage != null) toolImage.gameObject.SetActive(false);
        if (ingredientImage != null)
        {
            ingredientImage.gameObject.SetActive(true);
            ingredientImage.sprite = toolData.defaultImage;
        }
    }

    public static void PopulateResultItem(Transform item, FoodData food, string toolId, CookingToolData toolData)
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
}
