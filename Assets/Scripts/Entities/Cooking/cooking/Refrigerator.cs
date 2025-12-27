using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Refrigerator : MonoBehaviour
{
    [SerializeField] float xOffset = 0f;
    [SerializeField] float yOffset = 0f;
    [SerializeField] float xInterval = 1f;
    [SerializeField] float yInterval = 1f;
    [SerializeField] int linePerEntity = 3;
    private List<FoodModel> foodModels = new List<FoodModel>();

    public void AddIngredients(FoodModel ingredient)
    {
        if (ingredient == null || foodModels.Contains(ingredient)) return;

        ingredient.BehaviorInstance.defaultPosition = this.transform.position + CalculatePositionForIndex(foodModels.Count);
        foodModels.Add(ingredient);
        ingredient.onDestroy += HandleFoodDestroyed;
    }

    public void AddIngredients(List<FoodModel> ingredients)
    {
        ingredients.ForEach(ingredient => AddIngredients(ingredients));
    }

    public void HandleFoodDestroyed(FoodModel destroyedFood)
    {
        foodModels.Remove(destroyedFood);
        destroyedFood.onDestroy -= HandleFoodDestroyed;
        RefreshPosition();
    }

    public void RefreshPosition()
    {
        for (int i = 0; i < foodModels.Count; i++)
        {
            foodModels[i].BehaviorInstance.defaultPosition = CalculatePositionForIndex(i);
        }
    }
    
    private Vector3 CalculatePositionForIndex(int index)
    {
        return new Vector3(
            xOffset + (index % linePerEntity) * xInterval,
            yOffset + (index / linePerEntity) * yInterval,
            0f);
    }
}
