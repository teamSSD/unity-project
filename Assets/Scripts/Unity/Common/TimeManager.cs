using System;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : SingletonMonoBehaviour<TimeManager>
{
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
    [Tooltip("영업 종료 후 대기 손님 인내심 가속 배율 (퇴장 압박)")]
    [SerializeField] private float closedLocalTimerScale = 2f;

    private float localTimerScale = 1f;

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

    private void Start()
    {
        InitializeTime();
    }

    public void InitializeTime()
    {
        IsPaused = false;
        breakAction = null;

        // 현재 페이즈의 시작/종료 시각으로 override. Inspector 값은 페이즈 없을 때 fallback.
        // 페이즈 무관하게 11:00~15:00 하드코딩되어 시계가 항상 같은 위치를 표시하던 이슈 fix.
        var progress = GameSessionRoot.Instance?.Progress;
        if (progress != null && progress.PhaseData != null)
        {
            int startMin = progress.PhaseStartMinutes;
            int endMin   = progress.PhaseEndMinutes;
            startHour   = startMin / 60;
            startMinute = startMin % 60;
            endHour     = endMin / 60;
            endMinute   = endMin % 60;
        }

        breakTargetTime = endHour * 60 + endMinute;

        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null)
        {
            stats.SetTime(startHour, startMinute);
            stats.OnTimeChanged -= PlayTickingSfx;
            stats.OnTimeChanged += PlayTickingSfx;
        }
    }

    protected override void OnDestroy()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnTimeChanged -= PlayTickingSfx;
        base.OnDestroy();
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
            localTimerScale = closedLocalTimerScale; // 영업 종료 → 잔여 대기 손님 인내심 가속

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
        // 1. Global Game Time Update — IsPaused일 때 정지 (영업 종료 / 일시정지)
        if (!IsPaused)
        {
            gameTimer += Time.deltaTime;
            float secondsPerGameMinute = 60f / gameTimeScale;

            while (gameTimer >= secondsPerGameMinute)
            {
                var stats = GameSessionRoot.Instance?.Stats;
                if (stats == null) break;
                stats.AddTime(0, 1);
                gameTimer -= secondsPerGameMinute;
                CheckBreakPoint(stats.GetHour() * 60 + stats.GetMinute());
            }
        }

        // 2. Local Timers Update — 글로벌 일시정지와 무관하게 계속 진행
        //    (영업 종료 후에도 대기 손님 인내심은 흘러야 timeout → 퇴장 가능)
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var t = activeTimers[i];
            if (t.isDone)
            {
                activeTimers.RemoveAt(i);
                continue;
            }

            t.elapsed += Time.deltaTime * localTimerScale;
            t.onTick?.Invoke(t.elapsed, t.duration);

            if (t.elapsed >= t.duration)
            {
                t.isDone = true;
                t.onComplete?.Invoke();
                activeTimers.RemoveAt(i);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
