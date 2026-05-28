using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Global Time Settings")]
    [SerializeField] private float gameTimeScale = 120f;
    [SerializeField] private int startHour = 11;
    [SerializeField] private int startMinute = 0;
    [SerializeField] private int endHour = 15;
    [SerializeField] private int endMinute = 0;
    
    [Header("Audio")]
    [SerializeField] private AudioClip tickingSfx;

    [Header("Balancing - Local Timers")]
    [Tooltip("손님이 기다리는 기본 인내심 시간(초)")]
    [SerializeField] private float defaultCustomerWaitTime = 90f;

    public event Action OnTimePaused;
    public event Action OnTimeResumed;
    public event Action OnTimeEnd;

    public bool IsPaused { get; private set; } = false;

    public int StartTimeMinutes => startHour * 60 + startMinute;
    public int EndTimeMinutes => endHour * 60 + endMinute;

    private float gameTimer = 0f;
    private int breakTargetTime = -1;
    private Action breakAction;

    // 내부 타이머 클래스
    public class CustomTimer
    {
        public int id;
        public float duration;
        public float elapsed;
        public Action<float, float> onTick;
        public Action onComplete;
        public bool isDone;
    }

    private List<CustomTimer> activeTimers = new List<CustomTimer>();
    private int timerIdCounter = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeTime();
    }

    public void InitializeTime()
    {
        IsPaused = false;
        breakAction = null;
        breakTargetTime = endHour * 60 + endMinute;

        StatsSystem.Instance.SetTime(startHour, startMinute);
        
        // Ticking SFX 구독
        StatsSystem.Instance.OnTimeChanged -= PlayTickingSfx;
        StatsSystem.Instance.OnTimeChanged += PlayTickingSfx;
    }

    private void OnDestroy()
    {
        StatsSystem.Instance.OnTimeChanged -= PlayTickingSfx;
    }

    // --- Global Time Controls ---
    public void PauseTime()
    {
        if (!IsPaused)
        {
            IsPaused = true;
            OnTimePaused?.Invoke();
        }
    }

    public void ResumeTime()
    {
        if (IsPaused)
        {
            IsPaused = false;
            OnTimeResumed?.Invoke();
        }
    }

    public void RegisterBreakPoint(int hour, int minute, Action action)
    {
        breakTargetTime = hour * 60 + minute;
        breakAction = action;
    }

    public void ClearBreakPoint()
    {
        breakAction = null;
        breakTargetTime = -1;
    }

    private void CheckBreakPoint(int nowMinutes)
    {
        if (nowMinutes >= breakTargetTime && breakTargetTime >= 0)
        {
            PauseTime();
            breakTargetTime = -1; // 한 번 울리면 비활성화
            
            Debug.Log("[TimeManager] Time ended");
            OnTimeEnd?.Invoke();
            breakAction?.Invoke();
        }
    }

    private void PlayTickingSfx(int hour, int minute)
    {
        if (tickingSfx != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.Play2DSFX(tickingSfx, 0.5f);
        }
    }

    // --- Local Timer Controls ---
    
    /// <summary>
    /// 손님 대기 전용 타이머를 시작합니다.
    /// </summary>
    public int StartCustomerTimer(Action<float, float> onTick, Action onComplete)
    {
        return StartLocalTimer(defaultCustomerWaitTime, onTick, onComplete);
    }

    /// <summary>
    /// 범용 로컬 타이머를 등록합니다.
    /// </summary>
    public int StartLocalTimer(float duration, Action<float, float> onTick, Action onComplete)
    {
        var t = new CustomTimer
        {
            id = ++timerIdCounter,
            duration = duration,
            elapsed = 0f,
            onTick = onTick,
            onComplete = onComplete,
            isDone = false
        };
        activeTimers.Add(t);
        return t.id;
    }

    /// <summary>
    /// 발급받은 타이머 아이디를 정지/취소합니다.
    /// </summary>
    public void CancelTimer(int id)
    {
        for (int i = 0; i < activeTimers.Count; i++)
        {
            if (activeTimers[i].id == id)
            {
                activeTimers[i].isDone = true; // 다음 Update에서 치워짐
                break;
            }
        }
    }

    private void Update()
    {
        if (IsPaused) return;

        // 1. Global Game Time Update
        gameTimer += Time.deltaTime;
        float secondsPerGameMinute = 60f / gameTimeScale;

        while (gameTimer >= secondsPerGameMinute)
        {
            StatsSystem.Instance.AddTime(0, 1);
            gameTimer -= secondsPerGameMinute;
            
            // 틱이 흐를 때 마감시간 도달 여부 체크
            CheckBreakPoint(StatsSystem.Instance.GetHour() * 60 + StatsSystem.Instance.GetMinute());
        }

        // 2. Local Timers Update
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var t = activeTimers[i];
            if (t.isDone)
            {
                activeTimers.RemoveAt(i);
                continue;
            }

            t.elapsed += Time.deltaTime;
            t.onTick?.Invoke(t.elapsed, t.duration);

            if (t.elapsed >= t.duration)
            {
                t.isDone = true;
                t.onComplete?.Invoke();
                activeTimers.RemoveAt(i);
            }
        }
    }
}
