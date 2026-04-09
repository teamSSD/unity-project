using System;
using UnityEngine;

/// <summary>
/// Cooking 씬의 통계 및 결과(종료) 이벤트 관리.
/// 시간 진행은 이제 TimeManager가 주도합니다.
/// </summary>
[RequireComponent(typeof(CustomerManager))]
[RequireComponent(typeof(TimeManager))]
public class StatManager : MonoBehaviour
{
    public event Action onTimeEnd = () => {};

    void OnEnable()
    {
        StatsSystem.Instance.Initialize();

        StatsSystem.Instance.OnStaminaExhausted += OnStaminaExhausted;
        
        // TimeManager의 마감 이벤트 구독
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeEnd += OnTimeEnd;
    }

    void OnDisable()
    {
        StatsSystem.Instance.OnStaminaExhausted -= OnStaminaExhausted;
        if (TimeManager.Instance != null)
            TimeManager.Instance.OnTimeEnd -= OnTimeEnd;
    }



    private void OnStaminaExhausted()
    {
        Debug.Log("[StatManager] Stamina exhausted");
    }

    private void OnTimeEnd()
    {
        Debug.Log("[StatManager] Time ended");
        onTimeEnd.Invoke();
    }
}
