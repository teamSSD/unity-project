using System;
using System.Collections.Generic;

using UnityEngine;

public interface PlayMinigameUsecase
{
    IEnumerator<float> PlayCoroutine(Vector2 position, List<FoodData> ingredients, Action<float> onCompleted);
}