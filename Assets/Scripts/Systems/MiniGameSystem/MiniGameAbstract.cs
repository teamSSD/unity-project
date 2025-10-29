using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    protected bool isPlaying;
    protected float duration = 5f;  // 게임 진행 시간 (초)
    protected float elapsedTime = 0f;

    protected GameObject miniGameBgPrefab;
    public void StartGame()
    {
        isPlaying = true;
        elapsedTime = 0f;
        Debug.Log($"{GetType().Name} 시작!");
        ShowBG();
        this.transform.position = miniGameBgPrefab.transform.position;
    }

    protected virtual void Update()
    {
        if (!isPlaying) return;

        elapsedTime += Time.deltaTime;

        OnUpdate();

        if (elapsedTime >= duration)
        {
            EndGame();
        }
    }

    public void EndGame()
    {
        if (!isPlaying) return;

        isPlaying = false;

        RemoveBG();

        float score = CalculateScore();
        Debug.Log($"{GetType().Name} 종료! 점수: {score:F2}");
        Destroy(this.gameObject);
    }

    private void ShowBG()
    { 
        miniGameBgPrefab = Resources.Load<GameObject>("miniGameBG");

        Vector3 pos = GetBGPosition();
        miniGameBgPrefab = Instantiate(miniGameBgPrefab, pos, Quaternion.identity);
    }
    private void RemoveBG()
    {
        if (miniGameBgPrefab != null) Destroy(miniGameBgPrefab);
    }
    public abstract Vector3 GetBGPosition();
    public abstract void OnUpdate();
    public abstract float CalculateScore();
}
