using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSlot : MonoBehaviour
{
    [SerializeField] private Image MenuImage;
    [SerializeField] private Image toolImage;
    [SerializeField] private TextMeshProUGUI NameLabel;
    [Tooltip("MenuImage 컨테이너. InitEmpty/InitSlot이 SetActive로 토글.")]
    [SerializeField] private GameObject imageContainer;
    [Tooltip("NameLabel 컨테이너. InitEmpty/InitSlot이 SetActive로 토글.")]
    [SerializeField] private GameObject labelContainer;
    [Header("Menu ID")]
    [SerializeField] public string Id;

    private FoodData foodData;

    public void InitEmpty()
    {
        if (imageContainer != null) imageContainer.SetActive(false);
        if (labelContainer != null) labelContainer.SetActive(false);
    }

    public void InitSlot()
    {
        if (imageContainer != null) imageContainer.SetActive(true);
        if (labelContainer != null) labelContainer.SetActive(true);

        foodData = SearchDataUtil.GetFoodDataById(Id);
        if (foodData == null) return;

        NameLabel.text = foodData.ingredientName;

        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(Id);
        if (recipe != null && GameSessionRoot.Instance?.MenuSelection != null)
        {
            string toolId = GameSessionRoot.Instance?.RecipeLookup.GetToolIdForMinigame(recipe.minigameId);
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
