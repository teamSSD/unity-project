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
    [SerializeField] private Transform recipeListTransform;
    [SerializeField] private Transform ingredientListTransform;
    [SerializeField] private TextMeshProUGUI textPrefab;

    private FoodData foodData;
    private RecipeData recipeData;
    private Color enableButton, enableText, disableButton, disableText;
    private Image ingredientButton, recipeButton;
    private TextMeshProUGUI ingredientLabel, recipeLabel;

    private List<TextMeshProUGUI> ingredientTextList = new List<TextMeshProUGUI>();
    private List<TextMeshProUGUI> recipeTextList = new List<TextMeshProUGUI>();
    private List<RecipeData> recipeList = new List<RecipeData>();
    private HashSet<RecipeData> recipeCheck = new HashSet<RecipeData>();

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

        foreach (RecipeIngredient item in recipeData.inputs)
            AddIngredient(item.food.id);
        foreach (RecipeData recipe in recipeList)
            AddRecipe(recipe);

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
        ingredientTextList.ForEach(i => Destroy(i.gameObject));
        ingredientTextList.Clear();
        
        recipeTextList.ForEach(r => Destroy(r.gameObject));
        recipeTextList.Clear();
        recipeList.Clear();
        recipeCheck.Clear();
    }

    public void CloseMenuCard()
    {
        isMenuCardActive = false;

        Destroy(this.gameObject);
    }

    private void AddIngredient(string id)
    {
        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(id);

        if (recipe != null)
        {
            if (recipeCheck.Add(recipe))
            {
                recipeList.Add(recipe);
            }

            foreach (RecipeIngredient item in recipe.inputs)
            {
                AddIngredient(item.food.id);
            }
        }
        else
        {
            TextMeshProUGUI ingredient = Instantiate(textPrefab, ingredientListTransform).GetComponent<TextMeshProUGUI>();
            ingredient.text = $"- {SearchDataUtil.GetFoodDataById(id).ingredientName}";
            ingredientTextList.Add(ingredient);
        }
    }

    private void AddRecipe(RecipeData recipe)
    {
        TextMeshProUGUI recipeText = Instantiate(textPrefab, recipeListTransform).GetComponent<TextMeshProUGUI>();
        for (int i = 0; i < recipe.inputs.Count; i++)
        {
            if (i == recipe.inputs.Count - 1) recipeText.text += $"{recipe.inputs[i].food.ingredientName} -({GetCookingProcess(recipe.minigameId)})-> {recipe.outputFood.ingredientName}";
            else recipeText.text += $"{recipe.inputs[i].food.ingredientName} + ";
        }
        recipeTextList.Add(recipeText);
    }

    private string GetCookingProcess(string id)
    {
        switch (id)
        {
            case "M001":
                return "±Á±â";
            case "M002":
                return "»î±â";
            case "M003":
                return "?";
            case "M004":
                return "ºñºñ±â";
            case "M005":
                return "¼Ò½º »Ñ¸®±â";
            case "M006":
                return "½ä±â";
            case "M007":
                return "±Á±â";
            default:
                return "";

        }
    }
}
