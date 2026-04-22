using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSlot : MonoBehaviour
{
    [SerializeField] private Image MenuImage;
    [SerializeField] private TextMeshProUGUI NameLabel;
    [Header("Menu ID")]
    [SerializeField] public string Id;

    private FoodData foodData;
    private Image toolImage;

    public void InitSlot()
    {
        foodData = SearchDataUtil.GetFoodDataById(Id);
        if (foodData == null) return;

        // IMAGE_Menu = 도구 배경, IMAGE_Menu/image = 음식 오버레이
        if (toolImage == null && MenuImage != null)
        {
            toolImage = MenuImage.transform.parent?.GetComponent<Image>();

            // 음식 이미지를 도구 이미지에 정확히 겹치게
            var rt = MenuImage.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.localScale = Vector3.one;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            MenuImage.preserveAspect = false;
        }

        NameLabel.text = foodData.ingredientName;

        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(Id);
        if (recipe != null && RecipeDataManager.Instance != null)
        {
            string toolId = RecipeDataManager.Instance.GetToolIdForMinigame(recipe.minigameId);
            if (!string.IsNullOrEmpty(toolId))
            {
                // 도구 배경
                if (toolImage != null)
                {
                    CookingToolData toolData = SearchDataUtil.GetCookingToolDataById(toolId);
                    if (toolData != null)
                        toolImage.sprite = toolData.defaultImage;
                }
                // 음식 오버레이 (도구별 가공 이미지)
                MenuImage.sprite = foodData.GetImageForTool(toolId);
                return;
            }
        }

        // 레시피 없으면 도구 배경 숨기고 기본 이미지
        if (toolImage != null) toolImage.color = Color.clear;
        MenuImage.sprite = foodData.GetRepresentativeBentoImage() ?? foodData.image;
    }

    public void OpenMenuCardL()
    {
        RecipeBookManager.Instance.OpenMenuCardL(Id);
    }
    public void OpenMenuCardR()
    {
        RecipeBookManager.Instance.OpenMenuCardR(Id);
    }
}
