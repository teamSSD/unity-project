using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private SpriteStackRenderer stackRenderer;
    [Header("SFX")]
    [SerializeField] private AudioClip interactionSfx;

    [Header("게임 설정")]
    public float decreasePerPress = 2.5f;  // 스페이스바 당 게이지 증가량
    [Tooltip("Awake에서 targetMin~targetMax 범위 정규분포로 덮어씀. Inspector 값은 실전엔 미사용.")]
    public float targetGauge = 63;
    [SerializeField, Tooltip("동적 목표 범위 최소값 (%). 하한.")] private float targetMin = 20f;
    [SerializeField, Tooltip("동적 목표 범위 최대값 (%). 상한.")] private float targetMax = 70f;
    [SerializeField, Tooltip("정규분포 평균 (%). 범위의 중심이 아닌 별도 지정 — 예: [20,70]에서 평균 50 가능.")] private float targetMean = 50f;
    [SerializeField, Tooltip("정규분포 표준편차 (%). 클수록 target이 mean에서 더 자주 벗어남.")] private float targetStdDev = 10f;
    [SerializeField, Tooltip("목표 위치 마커 (Sauce 자식). anchor y가 target/100으로 갱신됨.")]
    private RectTransform stopMarker;
    [SerializeField, Tooltip("마커 x offset — 양수면 게이지 안쪽(오른쪽)으로 이동. Sauce local 좌표.")]
    private float stopMarkerXOffset = 0.35f;
    [SerializeField, Tooltip("마커 y ratio 보정 — 시각적 fill top과 마커가 안 맞을 때 이 값으로 조정. target/100에 더해짐. Play test 결과 sprite 시각적 fill 위치가 anchor보다 약 8% 높게 보여 -0.08 기본값.")]
    private float stopMarkerYRatioOffset = -0.08f;
    public GameObject upperArrow;
    public GameObject lowerArrow;
    private GuidedButtonAnimator upperArrowAnim;
    private GuidedButtonAnimator lowerArrowAnim;
    private bool isUpperTurn = true;
    private float currentGauge = 100;
    private bool waitingForFirstInput = true;

    private float waitingTime = 0f;
    private float waitingThreshold = 0.7f;
    [SerializeField, Tooltip("정답 허용치 (%). |current-target| 값이 이 이하면 감점 없음(perfect). 1틱=decreasePerPress=2.5. 5 = 2틱 여유(반응 지연 흡수).")]
    private float tolerance = 5f;
    [SerializeField, Tooltip("score 0이 되는 diff (%). tolerance ~ 이 값 사이는 linear.")]
    private float zeroScoreDiff = 20f;

    private void Awake()
    {
        upperArrowAnim = upperArrow.GetComponent<GuidedButtonAnimator>();
        lowerArrowAnim = lowerArrow.GetComponent<GuidedButtonAnimator>();

        upperArrowAnim.Guide();

        // 매 게임 target 랜덤화 — mean 중심 정규분포, [min, max] 클램프.
        // GameRandom.Variable 사용 → 상점(PhaseRandom, ImmutableSeed 기반) 등 영향 없음.
        // decreasePerPress(2.5) 배수로 스냅 — 플레이어가 실제 도달 가능한 값으로 맞춤.
        float raw = Mathf.Clamp(
            GameRandom.Normal(GameRandom.Variable, targetMean, targetStdDev),
            targetMin, targetMax);
        targetGauge = Mathf.Round(raw / decreasePerPress) * decreasePerPress;
        Debug.Log($"[SauceMiniGame] target {targetGauge:F1}% (raw {raw:F1}, snapped to ×{decreasePerPress})");
        UpdateStopMarkerPosition();
    }

    protected override void OnGameEnded()
    {
        float diff = Mathf.Abs(currentGauge - targetGauge);
        Debug.Log($"[SauceMiniGame] finished — current {currentGauge:F1}% / target {targetGauge:F1}% / diff {diff:F1} / tolerance {tolerance}");
    }

    private void UpdateStopMarkerPosition()
    {
        if (stopMarker == null) return;
        float ratio = Mathf.Clamp01(targetGauge / 100f + stopMarkerYRatioOffset);
        stopMarker.anchorMin = new Vector2(0f, ratio);
        stopMarker.anchorMax = new Vector2(0f, ratio);
        stopMarker.anchoredPosition = new Vector2(stopMarkerXOffset, 0f);
    }

    public override void OnUpdate()
    {
        if (!isPlaying) return;

        if (currentGauge <= 0)
        {
            EndGame();
        }

        gaugeBar.fillAmount = currentGauge / 100;

        if (isUpperTurn && Input.GetKeyDown(KeyCode.UpArrow))
        {
            waitingForFirstInput = false;
            upperArrowAnim.Unguide();
            lowerArrowAnim.Guide();
            waitingTime = 0;
            currentGauge -= decreasePerPress;
            isUpperTurn = false;
            SoundManager.Instance?.Play2DSFX(interactionSfx);
            return;
        }
        if (!isUpperTurn && Input.GetKeyDown(KeyCode.DownArrow))
        {
            waitingForFirstInput = false;
            lowerArrowAnim.Unguide();
            upperArrowAnim.Guide();
            waitingTime = 0;
            currentGauge -= decreasePerPress;
            isUpperTurn = true;
            SoundManager.Instance?.Play2DSFX(interactionSfx);
            return;
        }

        if (waitingForFirstInput) return;

        waitingTime += Time.deltaTime;
        if (waitingTime >= waitingThreshold)
        {
            EndGame();
        }
    }

    public override float CalculateScore()
    {
        // diff ≤ tolerance → 1.0. diff = zeroScoreDiff → 0.0. 그 사이 linear.
        // target 위치와 무관하게 대칭. (기존 공식은 maxPossibleDiff로 나눠서 극단 target 유리)
        float diff = Mathf.Abs(currentGauge - targetGauge);
        float t = Mathf.Clamp01((diff - tolerance) / (zeroScoreDiff - tolerance));
        return 1f - t;
    }

    public override void ApplyUpgrade(float m, int s) { base.ApplyUpgrade(m, s); tolerance = Mathf.Ceil(tolerance / m); }

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

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
