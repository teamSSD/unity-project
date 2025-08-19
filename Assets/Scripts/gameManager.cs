using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public float gameTime = 30f; // 총 게임 시간 (초)
    private float timer;

    public Bean bean; // 씬에 있는 Bean 오브젝트 연결
    public TMP_Text scoreText; // TMP Text 연결 (Canvas에 크게 배치)

    private bool gameEnded = false;

    void Start()
    {
        timer = gameTime;
        scoreText.text = "";
    }

    void Update()
    {
        if (gameEnded) return;

        // 타이머 감소
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        gameEnded = true;

        // Bean 점수 가져오기
        int finalScore = bean.playerScore;

        // 화면에 크게 출력
        scoreText.text = "finalScore: " + finalScore;

        Debug.Log("게임 종료! 최종 점수: " + finalScore);
    }
}
