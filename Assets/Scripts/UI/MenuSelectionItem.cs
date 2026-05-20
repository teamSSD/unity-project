using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSelectionItem : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image foodImage;
    [SerializeField] private Image toolIcon;
    [SerializeField] private Image cellImage;
    [SerializeField] private Sprite cellNormal;
    [SerializeField] private Sprite cellSelected;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private Button clickButton;

    public FoodData CurrentFood { get; private set; }
    public Action<MenuSelectionItem> OnClicked;

    private void Awake()
    {
        if (clickButton == null) clickButton = GetComponent<Button>();
        if (clickButton != null)
        {
            clickButton.onClick.AddListener(() => OnClicked?.Invoke(this));
        }
    }

    /// <summary>
    /// FoodData SO를 기반으로 음식 이미지와 이름을 설정합니다.
    /// 레시피가 있으면 도구별 조리 이미지 + 도구 아이콘을 표시합니다.
    /// </summary>
    public void SetData(FoodData foodData)
    {
        CurrentFood = foodData;
        if (foodData == null) return;

        // 레시피 조회 → 도구 정보
        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(foodData.id);
        string toolId = null;
        if (recipe != null)
            toolId = RecipeDataManager.Instance.GetToolIdForMinigame(recipe.minigameId);

        if (foodImage != null)
        {
            foodImage.sprite = toolId != null ? foodData.GetImageForTool(toolId) : foodData.image;
            foodImage.preserveAspect = true;
            foodImage.gameObject.SetActive(true);
        }

        if (nameLabel != null)
        {
            nameLabel.text = foodData.ingredientName;
        }

        // 도구 아이콘 설정
        if (toolIcon != null)
        {
            CookingToolData toolData = toolId != null ? SearchDataUtil.GetCookingToolDataById(toolId) : null;
            if (toolData != null && toolData.defaultImage != null)
            {
                toolIcon.sprite = toolData.defaultImage;
                toolIcon.preserveAspect = true;
                toolIcon.gameObject.SetActive(true);
            }
            else
            {
                toolIcon.gameObject.SetActive(false);
            }
        }

        SetSelection(false);
    }

    public void SetSelection(bool isSelected)
    {
        if (cellImage != null)
            cellImage.enabled = isSelected;
    }

    /// <summary>
    /// 필수 조리 도구 아이콘을 설정합니다. (필요 시 외부에서 호출)
    /// </summary>
    public void SetTool(Sprite toolSprite)
    {
        if (toolIcon != null)
        {
            if (toolSprite == null)
            {
                toolIcon.gameObject.SetActive(false);
            }
            else
            {
                toolIcon.gameObject.SetActive(true);
                toolIcon.sprite = toolSprite;
                toolIcon.preserveAspect = true;
            }
        }
    }
}
