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
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnStaminaExhausted += OnStaminaExhausted;
        var time = TimeManager.Instance;
        if (time != null) time.OnTimeEnd += OnTimeEnd;
    }

    void OnDisable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnStaminaExhausted -= OnStaminaExhausted;
        var time = TimeManager.Instance;
        if (time != null) time.OnTimeEnd -= OnTimeEnd;
    }



    private void OnStaminaExhausted()
    {
        ProgressSystem.Instance?.Die();
    }

    private void OnTimeEnd()
    {
        onTimeEnd.Invoke();
    }
}
