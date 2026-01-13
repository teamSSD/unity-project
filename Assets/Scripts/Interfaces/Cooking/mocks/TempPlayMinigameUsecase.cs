using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TempPlayMinigameUsecase : PlayMinigameUsecase
{
    public IEnumerator<float> PlayCoroutine(string cookingToolId, RecipeData recipeData, Vector2 position, List<FoodData> ingredients, Action<RecipeData, float> onCompleted)
    {
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return elapsed / duration;
        }

        StatsSystem.SubStamina(1);
        onCompleted?.Invoke(recipeData, 0.8f);
    }
}