using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class TempPlayMinigameUsecase : PlayMinigameUsecase
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