using System;
using System.Collections.Generic;
using UnityEngine;
/*
MiniGameManager.cs
==================
+) ***주의: 현재는 임시로 Start()에서 ClickMiniGame 바로 실행하고 있음***

설명:
- 미니게임 시스템의 중앙 관리 클래스
- 외부에서 미니게임을 시작하도록 인터페이스 제공
- MiniGameAbstract를 상속받는 모든 미니게임과 호환

사용법:
- MiniGameAbstract를 상속한 미니게임을 생성 후 StartMiniGame() 호출
*/
public class MiniGameManager : MonoBehaviour, PlayMinigameUsecase
{
    private MiniGameAbstract currentGame;

    [SerializeField] private GameObject BakeMinigamePrefab;
    [SerializeField] private GameObject BoilMinigamePrefab;
    [SerializeField] private GameObject MixMinigamePrefab;
    [SerializeField] private GameObject SauseMinigamePrefab;
    [SerializeField] private GameObject CutMinigamePrefab;
    [SerializeField] private GameObject GrillMinigamePrefab;

    private static Vector2 offset = new Vector2(0, 2);

    public IEnumerator<float> PlayCoroutine(string toolId, RecipeData recipeData, Vector2 position, List<FoodData> ingredients, Action<RecipeData, float> onCompleted)
    {
        GameObject prefab = GetPrefab(toolId, recipeData);
        if (prefab == null) yield break;

        bool isFinished = false;
        float finalScore = 0f;

        GameObject go = Instantiate(prefab);
        currentGame = go.GetComponent<MiniGameAbstract>();
        currentGame.OnGameFinished += (score) =>
        {
            finalScore = score;
            isFinished = true;
        };
        currentGame.StartGame();
        while (!isFinished)
        {
            yield return 0f;
        }
        onCompleted?.Invoke(recipeData, finalScore);
    }

    private GameObject GetPrefab(string toolId, RecipeData recipeData)
    {
        if (recipeData.minigameId == "M001") return BakeMinigamePrefab;
        if (recipeData.minigameId == "M002") return BoilMinigamePrefab;
        if (recipeData.minigameId == "M004") return MixMinigamePrefab;
        if (recipeData.minigameId == "M005") return SauseMinigamePrefab;
        if (recipeData.minigameId == "M006") return CutMinigamePrefab;
        if (recipeData.minigameId == "M007") return GrillMinigamePrefab;

        if (toolId == "T001") return BakeMinigamePrefab;
        if (toolId == "T002") return BoilMinigamePrefab;
        if (toolId == "T003") return MixMinigamePrefab;
        if (toolId == "T004") return CutMinigamePrefab;
        if (toolId == "T005") return GrillMinigamePrefab;

        return null;
    }
}
