using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MixMiniGame : MiniGameAbstract
{
    [SerializeField] private SpriteStackRenderer stackRenderer;
    [Header("게임 설정")]
    public GameObject mixingRod;
    public float width = 1f;
    public float height = 0.5f;
    public int pressRequiringCount = 20;
    public float idleClearTime = 2f;

    private Vector3 mixingRodDefaultPosition;
    private float timer = 0f;
    private float beforeTime = 0f;
    private int pressCount = 0;

    void Awake()
    {
        RodMovement.mixingRod = mixingRod;
        RodMovement.width = width;
        RodMovement.height = height;
    }

    void Start()
    {
        duration = 15; // 15초안에도 못누르면 이건 그냥 할 마음이 없는거다
        mixingRodDefaultPosition = mixingRod.transform.localPosition;
        RodMovement.mixingRodDefaultPosition = mixingRodDefaultPosition;
        RodMovement.SetDefaultPosition();
    }
    
    public override void OnUpdate()
    {
        if (!isPlaying) return;
        timer += Time.deltaTime;

        RodMovement.Animating();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            pressCount++;
            RodMovement.sweep(timer - beforeTime);
            beforeTime = timer;
        }

        checkEnd();
    }

    public override float CalculateScore()
    {
        return Mathf.SmoothStep(1, 0, (timer - idleClearTime) / (duration - idleClearTime));
    }

    private void checkEnd()
    {
        if (pressCount >= pressRequiringCount)
        {
            EndGame();
        }
    }

    

    public static class RodMovement
    {
        public static GameObject mixingRod;
        public static Vector3 mixingRodDefaultPosition;
        public static float width = 0;
        public static float height = 0;

        private static bool sweeping = false;
        private static float lapDuration = 0f;
        private static float angle = 0f;

        public static void SetDefaultPosition()
        {
            mixingRod.transform.localPosition = mixingRodDefaultPosition + calculatePosition(0);
        }

        public static void Animating()
        {
            if (!sweeping) return;
            
            float angularVelocity = (Mathf.PI * 2f) / lapDuration;
            angle += Time.deltaTime * angularVelocity;
            mixingRod.transform.localPosition = mixingRodDefaultPosition + calculatePosition(angle);
            if (angle >= Mathf.PI * 2f)
            {
                sweeping = false;
                SetDefaultPosition();
            }
        }

        public static void sweep(float period)
        {
            sweeping = true;
            angle = 0;
            lapDuration = period * 0.9f;
        }

        private static Vector3 calculatePosition(float ang)
        {
            ang -= Mathf.Sin(ang) * 0.5f; // 0.5는 임의의 값
            float x = Mathf.Cos(ang) * width;
            float y = Mathf.Sin(ang) * height;
            return new Vector3(x, y, 0);
        }
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
