using UnityEngine;
/*
ClickMiniGame.cs
==================
+) ***주의: 임시로 만들어둔 미니게임임***

설명:
- MiniGameAbstract를 상속한 구체적인 미니게임의 예시임
- 스페이스바 입력으로 클릭 수 기록
- 제한 시간 동안 클릭 수에 따라 점수 계산(0~1)
- 임시 게임이므로 점수는 로그로 출력하도록 해둠
*/
public class ClickMiniGame : MiniGameAbstract
{
    private int clickCount = 0;
    private int maxClickTarget = 20; // 20번 클릭하면 만점
    public override Vector3 GetBGPosition()
    {
        // 화면에 보이는 위치 (뷰포트 좌표)
        Vector3 viewportPos = new Vector3(0f, 0.5f, Camera.main.nearClipPlane + 5f);

        // (뷰포트 좌표 -> 월드 좌표로 역변환)
        return Camera.main.ViewportToWorldPoint(viewportPos);
    }
    public override void OnUpdate()
    {
        if (!isPlaying) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            clickCount++;
            Debug.Log($"클릭 수: {clickCount}");
        }
    }

    public override float CalculateScore()
    {
        // 클릭 횟수를 0~1 사이 점수로 변환
        return Mathf.Min(1f, (float)clickCount / maxClickTarget);
    }
}
