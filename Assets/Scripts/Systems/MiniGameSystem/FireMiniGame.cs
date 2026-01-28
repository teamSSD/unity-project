using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FireMiniGame : MiniGameAbstract
{
    [Header("오브젝트 설정")]
    [SerializeField] private RectTransform gaugeBar;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private SpriteStackRenderer stackRenderer;
    
    [Header("게이지 설정")]
    [SerializeField] private Vector3 gaugePosition = new Vector3(-2f, 0.33f, 0);
    [SerializeField] private float safeZoneRatio = 0.25f;
    [SerializeField] private float xOffset = 0.5f;

    [Header("게임 설정")]
    public float gameDuration = 3.0f; 
    public float coldStartTime = 0.5f;
    public float acceleration = 2.0f;
    public float maxVelocity = 1.0f;

    private float timer = 0f;
    private float elapsedSinceStart = 0f;
    private float arrowValue = 0.5f;
    private float velocity = 0f;

    [Header("계산치")]
    private float gaugeHeight;
    private float safeHalf;
    private float maxPenaltyDist;
    private float normalizationDivisor;
    private float accScore;

    void Start()
    {
        if (gaugeBar != null)
        {
            // 게이지 위치 초기화
            gaugeBar.localPosition = gaugePosition;
        }

        timer = gameDuration;
        elapsedSinceStart = 0f;

        arrowValue = 0.5f;
        velocity = 0f;
        accScore = 0f;
        
        gaugeHeight = gaugeBar.rect.height;
        safeHalf = safeZoneRatio * 0.5f;
        maxPenaltyDist = 0.5f - safeHalf;
        normalizationDivisor = Mathf.Pow(maxPenaltyDist, 2);

        UpdateArrowPosition();
    }

    public override void OnUpdate()
    {
        if (!isPlaying) return;

        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            EndGame();
            return;
        }

        elapsedSinceStart += Time.deltaTime;
        float rampFactor = Mathf.Clamp01(elapsedSinceStart / coldStartTime);

        float currentAcc = acceleration * rampFactor;
        float currentMaxVel = maxVelocity * rampFactor;

        if (Input.GetKey(KeyCode.Space))
            velocity += (currentAcc + Mathf.Abs(velocity) * 1.5f) * Time.deltaTime;
        else
            velocity -= (currentAcc + Mathf.Abs(velocity) * 1.5f) * Time.deltaTime;

        velocity = Mathf.Clamp(velocity, -currentMaxVel, currentMaxVel);
        arrowValue += velocity * Time.deltaTime;

        if (arrowValue <= 0 || arrowValue >= 1)
        {
            arrowValue = Mathf.Clamp01(arrowValue);
            velocity = 0;
        }

        UpdateArrowPosition();

        float currentFrameScore = CalculateFrameScore();
        accScore += currentFrameScore * (Time.deltaTime / gameDuration);
    }

    private void UpdateArrowPosition()
    {
        if (arrowTransform != null && gaugeBar != null)
        {
            float targetY = gaugeBar.localPosition.y + (arrowValue - 0.5f) * gaugeHeight;
            
            float targetX = gaugeBar.localPosition.x + xOffset;
            
            arrowTransform.localPosition = new Vector3(targetX, targetY, gaugeBar.localPosition.z);
        }
    }

    private float CalculateFrameScore()
    {
        float distFromCenter = Mathf.Abs(arrowValue - 0.5f);
        float diff = distFromCenter - safeHalf;

        if (diff <= 0) return 1.0f;

        float penalty = Mathf.Pow(diff, 2) / normalizationDivisor;
        return Mathf.Max(0, 1.0f - penalty);
    }

    public override float CalculateScore()
    {
        return Mathf.Clamp01(accScore);
    }

    public override void SetIngredients(List<FoodData> ingredients)
    {
        if (stackRenderer != null && ingredients != null)
        {
            var sprites = ingredients.Select(x => x.image); 
            stackRenderer.DrawMany(sprites);
        }
    }
}