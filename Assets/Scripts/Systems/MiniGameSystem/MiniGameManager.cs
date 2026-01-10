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

    public IEnumerator<float> PlayCoroutine(RecipeData recipeData, Vector2 position, List<FoodData> ingredients, Action<RecipeData, float> onCompleted)
    {
        if (recipeData.minigameId == "M001") executeMinigame(BakeMinigamePrefab);
        else if (recipeData.minigameId == "M002") executeMinigame(BoilMinigamePrefab);
        else if (recipeData.minigameId == "M004") executeMinigame(MixMinigamePrefab);
        else if (recipeData.minigameId == "M005") executeMinigame(SauseMinigamePrefab);
        else if (recipeData.minigameId == "M006") executeMinigame(CutMinigamePrefab);
        else if (recipeData.minigameId == "M007") executeMinigame(GrillMinigamePrefab);
        else
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

    private void executeMinigame(GameObject prefab)
    {
        GameObject go = Instantiate(prefab);
        currentGame = go.GetComponent<MiniGameAbstract>();
        currentGame.StartGame();
    }
}
