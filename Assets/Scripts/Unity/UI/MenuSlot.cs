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

    public void InitEmpty()
    {
        if (MenuImage != null) MenuImage.transform.parent.gameObject.SetActive(false);
        if (NameLabel != null) NameLabel.transform.parent.gameObject.SetActive(false);
    }

    public void InitSlot()
    {
        if (MenuImage != null) MenuImage.transform.parent.gameObject.SetActive(true);
        if (NameLabel != null) NameLabel.transform.parent.gameObject.SetActive(true);

        foodData = SearchDataUtil.GetFoodDataById(Id);
        if (foodData == null) return;

        if (toolImage == null && MenuImage != null)
            toolImage = MenuImage.transform.parent?.Find("Tool")?.GetComponent<Image>();

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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
