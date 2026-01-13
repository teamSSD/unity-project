using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuCard : MonoBehaviour
{
    private static MenuCard instance;
    public static MenuCard Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MenuCard>();
            }
            return instance;
        }
    }

    private static bool isMenuCardActive = false;
    public static bool IsMenuCardActive
    {
        get { return isMenuCardActive; }
    }

    [SerializeField] private Image MenuImage;
    [SerializeField] private TextMeshProUGUI NameLabel;
    [SerializeField] private GameObject RecipeLorePage;
    [SerializeField] private GameObject IngredientLorePage;
    [SerializeField] private Transform instantiateListTransform;
    [SerializeField] private TextMeshProUGUI ingredientTextPrefab;

    private FoodData foodData;
    private RecipeData recipeData;
    private Color enableButton, enableText, disableButton, disableText;
    private Image ingredientButton, recipeButton;
    private TextMeshProUGUI ingredientLabel, recipeLabel;

    private List<TextMeshProUGUI> ingredientList = new List<TextMeshProUGUI>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        isMenuCardActive = false;

        ColorUtility.TryParseHtmlString("#DCD7D0", out enableButton);
        ColorUtility.TryParseHtmlString("#494242", out enableText);
        ColorUtility.TryParseHtmlString("#877D6F", out disableButton);
        ColorUtility.TryParseHtmlString("#635A55", out disableText);

        ingredientButton = IngredientLorePage.transform.Find("Button_Ingredient").GetComponentInChildren<Image>();
        ingredientLabel = ingredientButton.transform.Find("Text_Ingredient").GetComponentInChildren<TextMeshProUGUI>();
        recipeButton = RecipeLorePage.transform.Find("Button_Recipe").GetComponentInChildren<Image>();
        recipeLabel = recipeButton.transform.Find("Text_Recipe").GetComponentInChildren<TextMeshProUGUI>();
    }

    public void InitSlot(string id)
    {
        foodData = ScriptableObject.CreateInstance<FoodData>();
        recipeData = ScriptableObject.CreateInstance<RecipeData>();

        // Get Info
        if (SearchDataUtil.GetFoodDataById(id) is FoodData)
            foodData = SearchDataUtil.GetFoodDataById(id);
        if (SearchDataUtil.GetRecipeDataByFoodId(id) is RecipeData)
            recipeData = SearchDataUtil.GetRecipeDataByFoodId(id);

        NameLabel.text = foodData.ingredientName;
        MenuImage.sprite = foodData.image;

        TextMeshProUGUI ingredient;
        foreach (RecipeIngredient item in recipeData.inputs)
        {
            ingredient = Instantiate(ingredientTextPrefab, instantiateListTransform).GetComponent<TextMeshProUGUI>();
            ingredient.text = $"- {item.food.ingredientName}";
            ingredientList.Add(ingredient);
        }

        isMenuCardActive = true;
    }

    public void OpenRecipe()
    {
        RecipeLorePage.transform.SetAsLastSibling();

        recipeButton.color = enableButton;
        recipeLabel.color = enableText;
        ingredientButton.color = disableButton;
        ingredientLabel.color = disableText;
    }
    public void OpenIngredient()
    {
        IngredientLorePage.transform.SetAsLastSibling();

        ingredientButton.color = enableButton;
        ingredientLabel.color = enableText;
        recipeButton.color = disableButton;
        recipeLabel.color = disableText;
    }

    public void ClearMenuCard()
    {
        ingredientList.ForEach(i => Destroy(i.gameObject));
        ingredientList.Clear();
    }

    public void CloseMenuCard()
    {
        isMenuCardActive = false;

        Destroy(this.gameObject);
    }
}
