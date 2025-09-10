using UnityEngine;
using UnityEngine.UI;

/*
SauceMiniGame.cs
====================================
설명:
- 스페이스바를 누를 때마다 게이지가 위로 채워짐 (UI Image Fill 사용)
- 게임 종료 시 게이지가 목표구간(예: 80%)에 가까울수록 점수가 높음
- SauceMiniGame 이라는 프리팹은 게이지바를 위한 Canvas와 UI Image를 자식으로 포함하도록 통째로 프리팹으로 제작해두었음.

사용법:
- SauceMiniGame 이라는 프리팹을 만들어두었으니 아래 3줄로 미니게임실행 하면됨
- GameObject go = Instantiate(sliceMiniGamePrefab);// Prefab에서 오브젝트 생성
  currentGame = go.GetComponent<MiniGameAbstract>();// 미니게임 스크립트 가져오기
  currentGame.StartGame();// 게임 시작
*/
public class SauceMiniGame : MiniGameAbstract
{
    [Header("UI 오브젝트")]
    public Image gaugeBar;               // UI Image (Fill 방식)

    [Header("게임 설정")]
    public float increasePerPress = 0.1f;  // 스페이스바 당 게이지 증가량 (0~1)
    public float targetGauge = 0.8f;        // 목표 게이지 (80%)

    private float currentGauge = 0f;        // 현재 게이지 값 (0~1)

    public override void OnUpdate()
    {
        if (!isPlaying) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentGauge += increasePerPress;
            currentGauge = Mathf.Clamp01(currentGauge); // 0~1 범위 제한
        }

        if (gaugeBar != null)
        {
            gaugeBar.fillAmount = currentGauge;
        }
    }

    public override float CalculateScore()
    {
        float diff = Mathf.Abs(currentGauge - targetGauge);
        float score = 1f - diff / targetGauge;
        return Mathf.Clamp01(score);
    }
}
