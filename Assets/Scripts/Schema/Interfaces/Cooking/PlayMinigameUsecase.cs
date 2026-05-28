using System;
using System.Collections.Generic;

using UnityEngine;

public interface PlayMinigameUsecase
{
    IEnumerator<float> PlayCoroutine(string toolId, RecipeData recipeData, Vector2 position, List<FoodData> ingredients, Action<RecipeData, float> onCompleted);
}