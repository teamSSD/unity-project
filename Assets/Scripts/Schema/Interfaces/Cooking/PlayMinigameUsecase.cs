using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface PlayMinigameUsecase
{
    UniTask PlayAsync(
        string toolId,
        RecipeData recipeData,
        Vector2 position,
        List<FoodData> ingredients,
        Action<RecipeData, float> onCompleted,
        CancellationToken ct = default);
}
