using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GriddleMinigame : MiniGameAbstract
{
    [SerializeField] private SpriteStackRenderer stackRenderer;

    [Header("Settings")]
    [SerializeField] private int totalArrowCount = 10;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private Transform arrowSpawnParent;
    [SerializeField] private Transform effectParent;
    [SerializeField] private float spacing = 150f; // 양수값 권장 (코드 내부에서 계산)

    private Queue<Vector2Int> _directionQueue = new Queue<Vector2Int>();
    private List<ArrowButton> _activeArrows = new List<ArrowButton>();
    private List<Vector2Int> _activeDirections = new List<Vector2Int>();
    
    private int _processedCount = 0;
    private int _successCount = 0;
    private const int MaxVisibleCount = 3;

    private readonly List<Vector2Int> _directionPool = new List<Vector2Int> {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    void Start()
    {
        InitializeQueue();
        for (int i = 0; i < MaxVisibleCount; i++)
        {
            SpawnNextArrow();
        }
    }

    private void InitializeQueue()
    {
        for (int i = 0; i < totalArrowCount; i++)
            _directionQueue.Enqueue(GameRandom.Pick(GameRandom.Variable, _directionPool));
    }

    private void SpawnNextArrow()
    {
        if (_directionQueue.Count == 0) return;

        Vector2Int dir = _directionQueue.Dequeue();
        GameObject go = Instantiate(arrowPrefab, arrowSpawnParent);
        
        if (go.TryGetComponent(out ArrowButton arrowScript))
        {
            float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0, 0, -angle);
            
            _activeArrows.Add(arrowScript);
            _activeDirections.Add(dir);
            UpdateVisualPositions(); // 생성 시 초기 위치 설정
        }
    }

    private void UpdateVisualPositions()
    {
        for (int i = 0; i < _activeArrows.Count; i++)
        {
            _activeArrows[i].transform.localPosition = new Vector3(0, i * spacing, 0);

            float factor = 1.0f - (i * 0.3f); 

            SpriteRenderer sr = _activeArrows[i].GetComponentInChildren<SpriteRenderer>();

            if (sr != null)
            {
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, factor);
                sr.sortingOrder = 10 - i;
            }
        }
    }

    public override void OnUpdate()
    {
        if (_processedCount >= totalArrowCount) return;

        Vector2Int inputDir = GetInput();
        if (inputDir != Vector2Int.zero && _activeArrows.Count > 0)
        {
            CheckAnswer(inputDir);
        }
    }

    private Vector2Int GetInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))    return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow))  return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow))  return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    private void CheckAnswer(Vector2Int input)
    {
        ArrowButton targetArrow = _activeArrows[0];
        Vector2Int targetDir = _activeDirections[0];

        if (input == targetDir)
        {
            targetArrow.Success();
            _successCount++;
        }
        else
        {
            targetArrow.Fail();
        }

        // 레이어 최상단으로 (이펙트가 잘 보이게)
        if (targetArrow.TryGetComponent(out SpriteRenderer sr))
            sr.sortingOrder = 100;

        // 논리적 데이터는 즉시 제거하여 다음 입력을 준비
        _activeArrows.RemoveAt(0);
        _activeDirections.RemoveAt(0);
        _processedCount++;

        // 시각적 처리를 위한 코루틴 실행
        StartCoroutine(ProcessArrowEffect(targetArrow));

        if (_processedCount >= totalArrowCount)
            EndGame();
    }

    private IEnumerator ProcessArrowEffect(ArrowButton target)
    {
        // 1. 이펙트용 부모로 이동 (대기열 위치 계산에서 제외됨)
        target.transform.SetParent(effectParent);

        // 2. 애니메이션이 진행되는 동안 대기 (0.25초)
        yield return new WaitForSeconds(0.25f);

        // 3. 이펙트 오브젝트 파괴
        Destroy(target.gameObject);

        // 4. 이펙트가 끝난 시점에 다음 화살표를 채우고 위치를 내림
        SpawnNextArrow();
        UpdateVisualPositions();
    }

    public override float CalculateScore()
    {
        if (totalArrowCount == 0) return 0f;
        // 정수 나눗셈 방지를 위해 float 캐스팅
        float score = (float)_successCount / totalArrowCount;
        return score <= 0 ? 0.01f : score;
    }

    public override void ApplyUpgrade(float m, int s) { base.ApplyUpgrade(m, s); totalArrowCount = Mathf.Max(3, (int)(totalArrowCount * m)); }

    public override void SetIngredients(List<FoodData> ingredients, string toolId = null)
    {
        if (stackRenderer != null && ingredients != null)
        {
            var sprites = toolId != null
                ? ingredients.Select(x => x.GetImageForTool(toolId))
                : ingredients.Select(x => x.image);
            stackRenderer.DrawMany(sprites);
        }
    }
}