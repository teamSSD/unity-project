using UnityEngine;
using UnityEngine.UI;

/*
FireMiniGame.cs
====================================
설명:
- 스페이스바를 누를 때마다 게이지가 오른쪽으로 채워지며, 안누르는 동안은 왼쪽으로 감소함  (UI Image Fill 사용)
- 게임 종료 시 게이지가 목표구간(예: 80%)에 가까울수록 점수가 높음
- FireMiniGame 이라는 프리팹은 게이지바를 위한 Canvas와 UI Image를 자식으로 포함하도록 통째로 프리팹으로 제작해두었음.
- 녹색->노랑->빨강으로 게이지바 색상 변화

사용법:
- FireMiniGame 이라는 프리팹을 만들어두었으니 아래 3줄로 미니게임실행 하면됨
- GameObject go = Instantiate(sliceMiniGamePrefab);// Prefab에서 오브젝트 생성
  currentGame = go.GetComponent<MiniGameAbstract>();// 미니게임 스크립트 가져오기
  currentGame.StartGame();// 게임 시작
*/

public class  FireMiniGame : MiniGameAbstract
{
    [Header("게임 위치")]
    public float xPos;
    public float yPos;

    [Header("프리팹")]
    public GameObject fryingPanPrefab;

    [Header("UI 오브젝트")]
    public Image gaugeBar;           

    [Header("게임 설정")]
    public float increasePerSecond = 0.6f;  
    public float decreasePerSecond = 0.8f; 
    public float targetGauge = 0.8f;       

    private float currentGauge = 0f;

    private Vector3 bgPos;
    void Start()
    {
        GameObject firePanImage = Instantiate(fryingPanPrefab, this.transform);
        firePanImage.transform.localPosition = new Vector3(0, 0, 0);

        //게이지바 위치 맞추는 임시코드 (교체 예정)
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(new Vector3(xPos, yPos, Camera.main.nearClipPlane));
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gaugeBar.canvas.transform as RectTransform,
            screenPos,
            gaugeBar.canvas.worldCamera,
            out Vector2 localPos);
        gaugeBar.rectTransform.anchoredPosition = localPos + new Vector2(100f, 0f);//게이지바 위치
    }

    public override Vector3 GetBGPosition()
    {
        bgPos = new Vector3(xPos, yPos, Camera.main.nearClipPlane);

        // (뷰포트 좌표 -> 월드 좌표로 역변환)
        return Camera.main.ViewportToWorldPoint(bgPos);
    }
    public override void OnUpdate()
    {
        if (!isPlaying) return;

        // 스페이스바 누르는 동안 게이지 증가
        if (Input.GetKey(KeyCode.Space))
        {
            currentGauge += increasePerSecond * Time.deltaTime;
        }
        else // 스페이스바 떼면 게이지 감소
        {
            currentGauge -= decreasePerSecond * Time.deltaTime;
        }

        currentGauge = Mathf.Clamp01(currentGauge); // 0~1 범위 제한

        if (gaugeBar != null)
        {
            gaugeBar.fillAmount = currentGauge;

            // 색상 변화: 0~0.5 = 녹색 → 노랑, 0.5~1 = 노랑 → 빨강
            if (currentGauge <= 0.5f)
            {
                float t = currentGauge / 0.5f;
                gaugeBar.color = Color.Lerp(Color.green, Color.yellow, t);
            }
            else
            {
                float t = (currentGauge - 0.5f) / 0.5f;
                gaugeBar.color = Color.Lerp(Color.yellow, Color.red, t);
            }
        }
    }

    public override float CalculateScore()
    {
        float diff = Mathf.Abs(currentGauge - targetGauge);
        float score = 1f - diff / targetGauge;
        return Mathf.Clamp01(score);
    }
}
