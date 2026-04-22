using System;
using System.Collections.Generic;
using UnityEngine;

/*
MiniGameAbstract.cs
==================
설명:
- 모든 미니게임의 공통 기본 클래스(추상 클래스)
- 게임 시작(StartGame), 종료(EndGame) 기능 제공
- 매 프레임 Update()에서 OnUpdate() 호출 및 제한 시간 관리
- MiniGameAbstract를 상속한 구체 미니게임은 OnUpdate()와 CalculateScore() 구현 필요
- 미니게임별 위치에 맞춰 배경이미지 생성
- 종료시 사용한 모든 오브젝트 자동 소멸

사용법:
- MiniGameAbstract를 상속한 미니게임 클래스 생성
- 상속한 구체 미니게임의 OnUpdate()에 매 프레임 처리할 게임 로직 작성
- 상속한 구체 미니게임의 CalculateScore()에 점수 계산 방식 정의
- StartGame()으로 게임 시작
*/
public abstract class MiniGameAbstract : MonoBehaviour
{
    public event Action<float> OnGameFinished;
    protected bool isPlaying;
    protected float duration = 5f;  // 게임 진행 시간 (초)
    protected float elapsedTime = 0f;
    public string minigameId { get; protected set; }
    protected int upgradedStaminaCost = 5;

    public GameObject scoringPrefab;
    [SerializeField] private GameObject miniGameBgSource;
    protected GameObject miniGameBgPrefab;

    public void StartGame()
    {
        isPlaying = true;
        elapsedTime = 0f;
        ShowBG();
    }

    protected virtual void Update()
    {
        if (!isPlaying)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Destroy(gameObject);
            }
            return;
        }

        elapsedTime += Time.deltaTime;

        OnUpdate();
    }

    public void EndGame()
    {
        if (!isPlaying) return;
        isPlaying = false;

        StatsSystem.Instance.SubStamina(upgradedStaminaCost);

        float score = CalculateScore();

        // 스코어링 생성
        getScoringInstance(score);

        OnGameFinished?.Invoke(score);
        Destroy(gameObject, 1f);
        RemoveBG(1f);
    }

    public void getScoringInstance(float score)
    {
        if (scoringPrefab == null) return;
        
        GameObject prefab = Instantiate(scoringPrefab);
        prefab.transform.parent = gameObject.transform;
        prefab.transform.localPosition = new Vector3(0.5f, 0.3f);
        prefab.GetComponent<MinigameResult>()?.SetScore(score);
    }

    private void ShowBG()
    {
        miniGameBgPrefab = Instantiate(miniGameBgSource);
        miniGameBgPrefab.transform.parent = this.gameObject.transform;
        miniGameBgPrefab.transform.localPosition = Vector3.zero;
    }
    private void RemoveBG(float time)
    {
        if (miniGameBgPrefab != null) Destroy(miniGameBgPrefab, time);
    }
    public abstract void OnUpdate();
    public abstract float CalculateScore();

    public virtual void ApplyUpgrade(float multiplier, int staminaCost)
    {
        upgradedStaminaCost = staminaCost;
    }

    public virtual void SetIngredients(List<FoodData> ingredients, string toolId = null) { }
}
