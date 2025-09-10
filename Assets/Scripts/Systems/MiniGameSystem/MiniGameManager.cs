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
public class MiniGameManager : MonoBehaviour
{
    private MiniGameAbstract currentGame;
    public GameObject MiniGamePrefab;

    private void Start() //***테스트용 임시코드 - 이후 Start()함수 삭제할 것***
    {
        //클릭미니게임 실행코드
        //GameObject miniGameObject = new GameObject("ClickMiniGame");
        //currentGame = miniGameObject.AddComponent<ClickMiniGame>();

        //슬라이스/게이지미니게임 실행코드
        GameObject go = Instantiate(MiniGamePrefab);
        currentGame = go.GetComponent<MiniGameAbstract>();
        currentGame.StartGame();
    }
    public void StartMiniGame(MiniGameAbstract game)
    {
        currentGame = game;
        currentGame.StartGame();
    }
}
