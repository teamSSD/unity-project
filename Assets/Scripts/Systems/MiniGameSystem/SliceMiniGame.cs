using UnityEngine;

/*
SliceMiniGame.cs
==================
설명:
- 미니게임 시작 시 칼 스프라이트 생성
- 칼 드래그 제어 및 기준선 통과 시 슬라이스 생성
- 슬라이스 10개 = 만점

사용법:
- SliceMiniGame 이라는 프리팹을 만들어두었으니 아래 3줄로 미니게임실행 하면됨
- GameObject go = Instantiate(sliceMiniGamePrefab);// Prefab에서 오브젝트 생성
  currentGame = go.GetComponent<MiniGameAbstract>();// 미니게임 스크립트 가져오기
  currentGame.StartGame();// 게임 시작
*/
public class SliceMiniGame : MiniGameAbstract
{
    [Header("프리팹")]
    public GameObject knifePrefab;      // 칼 프리팹
    public GameObject slicePrefab;      // 토마토 조각 프리팹

    [Header("게임 설정")]
    public float yThreshold = 0f;       // 기준선 Y좌표
    public int maxSliceTarget = 10;

    private Transform knifeTransform;    // 미니게임이 생성한 칼
    private bool isDragging = false;
    private Vector3 offset;
    private int sliceCount = 0;
    private bool wasAboveThreshold = true;
    public override void OnUpdate()
    {
        if (!isPlaying) return;

        // 칼이 없으면 생성
        if (knifeTransform == null)
        {
            SpawnKnife();
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // 드래그 시작
        if (Input.GetMouseButtonDown(0))
        {
            Collider2D col = knifeTransform.GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(mouseWorld))
            {
                isDragging = true;
                offset = knifeTransform.position - mouseWorld;
            }

            // 기준선 상태 초기화
            wasAboveThreshold = knifeTransform.position.y > yThreshold;
        }

        // 드래그 중
        if (isDragging && Input.GetMouseButton(0))
        {
            knifeTransform.position = mouseWorld + offset;

            // 기준선 통과 체크
            if (wasAboveThreshold && knifeTransform.position.y < yThreshold)
            {
                SpawnSlice();
                wasAboveThreshold = false;
            }
            else if (knifeTransform.position.y > yThreshold)
            {
                wasAboveThreshold = true;
            }
        }

        // 드래그 종료
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    private void SpawnKnife()
    {
        GameObject knife = Instantiate(knifePrefab, new Vector3(0, 3, 0), Quaternion.identity);
        knifeTransform = knife.transform;
    }
    private void SpawnSlice()
    {
        sliceCount++;

        Vector3 spawnPos = knifeTransform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.2f, 0.2f), 0);
        Instantiate(slicePrefab, spawnPos, Quaternion.identity);
        Debug.Log($"슬라이스 생성! 현재 개수: {sliceCount}");
    }

    public override float CalculateScore()
    {
        return Mathf.Min(1f, (float)sliceCount / maxSliceTarget);
    }
}
