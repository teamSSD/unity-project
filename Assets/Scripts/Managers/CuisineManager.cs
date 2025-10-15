using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RecipeSystem))]
[DisallowMultipleComponent]
public class CuisineManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> cookingTools;
    [SerializeField] private GameObject refrigeratorGameObject;
    [SerializeField] private GameObject upperShelfGameObject;
    [SerializeField] private GameObject lowerShelfGameObject;
    [SerializeField] private GameObject foodPrefab;
    private PlayMinigameUsecase playMinigameUsecase;
    private SearchRecipeUsecase searchRecipeUsecase;
    private SearchFoodUsecase searchFoodUsecase;
    private LoadInventoryUsecase loadInventoryUsecase;
    private Refrigerator refrigerator;
    private UpperShelf upperShelf;
    private LowerShelf lowerShelf;

    void Start()
    {
        playMinigameUsecase = new tempPlayMinigameUsecase();
        searchRecipeUsecase = new tempSearchRecipeUsecase();
        searchFoodUsecase = new tempSearchFoodUsecase();
        loadInventoryUsecase = new tempLoadInventoryUsecase(searchFoodUsecase);

        cookingTools.ForEach(tool =>
                tool.GetComponent<CookingToolModel>()
                        .Inject(playMinigameUsecase, searchRecipeUsecase, searchFoodUsecase));

        refrigerator = refrigeratorGameObject.GetComponent<Refrigerator>();
        upperShelf = upperShelfGameObject.GetComponent<UpperShelf>();
        lowerShelf = lowerShelfGameObject.GetComponent<LowerShelf>();

        FillRefrigerator(refrigeratorGameObject);
        FillUpperShelf(upperShelfGameObject);
        FillLowerShelf(lowerShelfGameObject);
    }

    private void FillRefrigerator(GameObject parent)
    {
        loadInventoryUsecase.LoadRefrigeratorIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                refrigerator.AddIngredients(foodModel);
            });
    }

    public void FillUpperShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadUpperShelfIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                upperShelf.AddIngredients(foodModel);
            });
    }
    
    public void FillLowerShelf(GameObject parent)
    {
        loadInventoryUsecase.LoadLowerShelfIngredient().ForEach(data =>
            {
                GameObject ingredientInstance = Instantiate(foodPrefab, parent.transform);
                ingredientInstance.name = data.Item1.ingredientName;
                FoodModel foodModel = ingredientInstance.GetComponent<FoodModel>();
                foodModel.Inject(loadInventoryUsecase, data.Item1, data.Item2.defaultPrice);
                lowerShelf.AddIngredients(foodModel);
            });
    }
}

class tempPlayMinigameUsecase : PlayMinigameUsecase
{
    public IEnumerator<float> PlayCoroutine(RecipeData recipeData, Vector2 position, List<FoodData> ingredients, Action<RecipeData, float> onCompleted)
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return elapsed / duration;
        }

        onCompleted?.Invoke(recipeData, 0.8f);
    }
}

class tempSearchRecipeUsecase : SearchRecipeUsecase
{
    List<RecipeData> recipes;

    public tempSearchRecipeUsecase()
    {
        recipes = CsvModelConverter.Parse<RecipeData>("driveAssets/dataTables/recipe");
    }

    public RecipeData Search(List<FoodData> ingredients)
    {
        ISet<string> ingredientIdSet = new HashSet<string>(ingredients.Select(i => i.id));
        return recipes.FirstOrDefault(recipe =>
                recipe.inputInfoSet.Count == ingredientIdSet.Count
                && ingredientIdSet.SetEquals(recipe.inputInfoSet.Select(info => info.foodId)))
                ?? new RecipeData("R000", "I000", "NONE", new HashSet<RecipeIngredient>());
    }
}

class tempSearchFoodUsecase : SearchFoodUsecase
{
    List<FoodData> foodDatas;

    public tempSearchFoodUsecase()
    {
        foodDatas = CsvModelConverter.Parse<FoodData>("driveAssets/dataTables/food");
    }

    /* null을 반환할 수 있음*/
    public FoodData Search(string id)
    {
        return foodDatas.Find(foodData => foodData.id == id);
    }
}

class tempLoadInventoryUsecase : LoadInventoryUsecase
{
    private SearchFoodUsecase searchFoodUsecase;
    private List<(IngredientData, int)> ingredients;

    public tempLoadInventoryUsecase(SearchFoodUsecase searchFoodUsecase)
    {
        this.searchFoodUsecase = searchFoodUsecase;

        ingredients = CsvModelConverter.Parse<IngredientData>("driveAssets/dataTables/ingredient")
                .ConvertAll(ingredient => (ingredient, 10));
    }

    public void ConsumeFood(string foodId, int amount)
    {
        (IngredientData, int) found = ingredients.Find(ingredient => ingredient.Item1.id == foodId);
        found.Item2 = found.Item2 - amount;
    }
    public int CheckStockAmount(string foodId)
    {
        return ingredients.Find(ingredient => ingredient.Item1.id == foodId).Item2;
    }
    public List<(FoodData, IngredientData)> LoadRefrigeratorIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.Refrigerator)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
    public List<(FoodData, IngredientData)> LoadUpperShelfIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.UpperShelf)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
    public List<(FoodData, IngredientData)> LoadLowerShelfIngredient()
    {
        return ingredients
                .FindAll(ingredient => ingredient.Item1.display == IngredientDisplayCategory.LowerShelf)
                .ConvertAll(ingredient => (searchFoodUsecase.Search(ingredient.Item1.id), ingredient.Item1))
                .ToList();
    }
}