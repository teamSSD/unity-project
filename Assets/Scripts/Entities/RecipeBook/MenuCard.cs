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
                if (instance == null)
                {
                    Debug.LogError("MenuCard instance not found in scene.");
                }
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

    private FoodData foodData;
    private Color enableButton, enableText, disableButton, disableText;
    private Image ingredientButton, recipeButton;
    private TextMeshProUGUI ingredientLabel, recipeLabel;

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

        // Get Info
        if (SearchDataUtil.GetFoodDataById(id) is FoodData)
            foodData = SearchDataUtil.GetFoodDataById(id);
        NameLabel.text = foodData.ingredientName;
        MenuImage.sprite = foodData.image;

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

    public void CloseMenuCard()
    {
        isMenuCardActive = false;

        Destroy(this.gameObject);
    }
}
