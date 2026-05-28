using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CookingToolSchema
{
    private readonly int maxIngredientSize;
    public readonly CookingToolData cookingToolData;
    public List<FoodSchema> Ingredients { get; private set; }
    private FoodSchema result;
    private bool locked = false;

    public CookingToolSchema(
        CookingToolData cookingToolData,
        int maxIngredientSize = 7)
    {
        this.cookingToolData = cookingToolData;
        this.maxIngredientSize = maxIngredientSize;
        Ingredients = new List<FoodSchema>();
    }

    public bool IsCookable()
    {
        return !locked && Ingredients.Count != 0;
    }

    public bool IsAddable(FoodSchema food)
    {
        if (locked || food == null) return false;

        // Check if incoming food supports this cooking tool
        if (!food.foodData.availableTools.Contains(cookingToolData.id))
        {
            Debug.LogWarning($"[CookingToolSchema] Cannot add {food.foodData.ingredientName} to {cookingToolData.cookerName}: not in availableTools");
            return false;
        }

        if (result == null && (Ingredients.Count == maxIngredientSize || Ingredients.Any(ingredient => ingredient.IsSameFood(food))))
        {
            return false;
        }
        if (result != null && !result.foodData.availableTools.Contains(cookingToolData.id))
        {
            Debug.Log(result.foodData.ingredientName + "\n" + String.Join(", ", result.foodData.availableTools) + "\n" + cookingToolData.id);
            return false;
        }

        return true;
    }

    public bool AddIngredient(FoodSchema food)
    {
        if (!IsAddable(food)) return false;

        if (result != null)
        {
            Ingredients.Add(result);
            result = null;
        }
        ;

        Ingredients.Add(food);
        return true;
    }

    public void ClearIngredient()
    {
        if (locked) return;
        Ingredients.Clear();
        result = null;
    }

    public void MinigameStart()
    {
        this.locked = true;
    }

    /// <summary>
    /// 요리 결과 계산. chainDepth는 호출자가 제공 (Schema 레이어는 catalog 미접근).
    /// </summary>
    public void Cook(FoodData foodData, RecipeData recipeData, float score, int chainDepth)
    {
        int newPrice = (int) Ingredients.Join(
            recipeData.inputs,
            ingredient => ingredient.foodData,
            inputInfo => inputInfo.food,
            (ingredient, inputInfo) => ingredient.Price * (0.85f + inputInfo.foodWeight * score)
        ).Sum();

        // 체인 완성 보너스: MAIN/SIDE 완성 시 체인 깊이에 비례한 보너스
        if (foodData.type == FoodType.MAIN || foodData.type == FoodType.SIDE)
        {
            float chainBonus = 1f + 0.15f * (chainDepth - 1);
            newPrice = (int)(newPrice * chainBonus);
        }

        result = new FoodSchema(foodData, newPrice);
        Ingredients.Clear();
        locked = false;
    }

    public FoodSchema GetResult()
    {
        return result;
    }
}