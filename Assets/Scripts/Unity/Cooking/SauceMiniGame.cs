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
    public float targetGauge = 63;        // 목표 게이지
    public GameObject upperArrow;
    public GameObject lowerArrow;
    private GuidedButtonAnimator upperArrowAnim;
    private GuidedButtonAnimator lowerArrowAnim;
    private bool isUpperTurn = true;
    private float currentGauge = 100;
    private bool waitingForFirstInput = true;

    private float waitingTime = 0f;
    private float waitingThreshold = 0.7f;
    private int tolerance = 2;

    private void Awake()
    {
        upperArrowAnim = upperArrow.GetComponent<GuidedButtonAnimator>();
        lowerArrowAnim = lowerArrow.GetComponent<GuidedButtonAnimator>();
        
        upperArrowAnim.Guide();
        
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
        float maxPossibleDiff = Mathf.Max(targetGauge, 100f - targetGauge);;
        float diff = Mathf.Abs(currentGauge - targetGauge);
        float penaltyDiff = Mathf.Max(0, diff - tolerance);
        float score = 1.0f - (penaltyDiff / (maxPossibleDiff - tolerance));
        return Mathf.Clamp01(score);
    }

    public override void ApplyUpgrade(float m, int s) { base.ApplyUpgrade(m, s); tolerance = (int)Mathf.Ceil(tolerance / m); }

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
