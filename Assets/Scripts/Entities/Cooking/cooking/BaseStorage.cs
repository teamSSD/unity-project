using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for ingredient storage components (Refrigerator, Shelves)
/// Provides common functionality for managing FoodModel lists and positioning
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public abstract class BaseStorage : MonoBehaviour
{
    protected List<FoodModel> foodModels = new List<FoodModel>();

    /// <summary>
    /// Add a single ingredient to storage
    /// </summary>
    public virtual void AddIngredients(FoodModel ingredient)
    {
        if (ingredient == null || foodModels.Contains(ingredient)) return;

        ingredient.SetDefaultPosition(transform.position + CalculatePositionForIndex(foodModels.Count));
        foodModels.Add(ingredient);
        ingredient.onDestroy += HandleFoodDestroyed;
    }

    /// <summary>
    /// Add multiple ingredients to storage
    /// Fixed: properly iterates over individual ingredients
    /// </summary>
    public void AddIngredients(List<FoodModel> ingredients)
    {
        if (ingredients == null) return;

        ingredients.ForEach(ingredient => AddIngredients(ingredient)); // Fixed: was AddIngredients(ingredients)
    }

    /// <summary>
    /// Handle ingredient destruction (e.g., when consumed)
    /// </summary>
    public void HandleFoodDestroyed(FoodModel destroyedFood)
    {
        foodModels.Remove(destroyedFood);
        destroyedFood.onDestroy -= HandleFoodDestroyed;
        RefreshPosition();
    }

    /// <summary>
    /// Recalculate positions for all ingredients
    /// </summary>
    public void RefreshPosition()
    {
        for (int i = 0; i < foodModels.Count; i++)
        {
            foodModels[i].SetDefaultPosition(transform.position + CalculatePositionForIndex(i));
        }
    }

    /// <summary>
    /// Calculate position offset for ingredient at given index
    /// Each storage type implements its own layout strategy
    /// </summary>
    /// <param name="index">Index in the food list</param>
    /// <returns>Position offset from storage transform</returns>
    protected abstract Vector3 CalculatePositionForIndex(int index);

    /// <summary>
    /// Get count of stored ingredients (for debugging/UI)
    /// </summary>
    public int GetCount()
    {
        return foodModels.Count;
    }

    /// <summary>
    /// Clear all ingredients (for testing/reset)
    /// </summary>
    public void Clear()
    {
        foreach (var food in foodModels)
        {
            if (food != null)
            {
                food.onDestroy -= HandleFoodDestroyed;
            }
        }
        foodModels.Clear();
    }
}
