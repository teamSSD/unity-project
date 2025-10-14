using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

[RequireComponent(typeof(RecipeSystem))]
[DisallowMultipleComponent]
public class CuisineManager : MonoBehaviour
{
    [SerializeField] private GameObject cookingToolParent;
    [SerializeField] private GameObject cookingToolPrefab;
    [SerializeField] private List<AnimatorController> animatorControllers;
    PlayMinigameUsecase playMinigameUsecase;
    SearchRecipeUsecase searchRecipeUsecase;

    void Start()
    {
        MakeCookingTools();
        playMinigameUsecase = new tempPlayMinigameUsecase(); 
        searchRecipeUsecase = new tempSearchRecipeUsecase();
    }

    private void MakeCookingTools()
    {
        /*
        LoadCookingTools().ForEach(schema =>
        {
            GameObject go = Instantiate(cookingToolPrefab, cookingToolParent.transform);
            go.name = schema.Item1.cookingToolData.name;
            go.GetComponent<CookingToolBehavior>().defaultPosition = schema.Item2;
            go.GetComponent<CookingToolModel>().Inject(playMinigameUsecase, searchRecipeUsecase, schema.Item1, animatorControllers[0]);
        });
        */
    }

    private List<(CookingToolSchema, Vector2)> LoadCookingTools()
    {
        return new List<(CookingToolSchema, Vector2)>{
            (
                new CookingToolSchema(
                    GenerateCookingToolData(
                        Resources.Load<Sprite>("driveAssets/art/item/item_cuttingSet_default"),
                        "C000",
                        "cookerName"
                    )
                ),
                new Vector2(1, 2)
            )
        };
    }
    
    private CookingToolData GenerateCookingToolData(Sprite defaultImage, string id, string cookerName)
    {
        var data = ScriptableObject.CreateInstance<CookingToolData>();
        data.Init(defaultImage, id, cookerName);
        return data;
    }
}

class tempPlayMinigameUsecase : PlayMinigameUsecase
{
    public IEnumerator<float> PlayCoroutine(Vector2 position, List<FoodData> ingredients, Action<float> onCompleted)
    {
        float duration = 3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return elapsed / duration;
        }

        onCompleted?.Invoke(1f);
    }
}

class tempSearchRecipeUsecase : SearchRecipeUsecase
{
    public (FoodData, RecipeData) Search(List<FoodData> ingredients)
    {
        return (new FoodData(), new RecipeData());
    }
}