using UnityEngine;

/// <summary>
/// Cooking 씬 스태미너 소진 → 즉시 사망 처리만 담당.
/// (이전엔 TimeManager → CustomerManager 시간 종료 중계도 했지만,
///  구독 race를 만들어서 CustomerManager가 TimeManager에 직접 구독하도록 옮김.)
/// </summary>
[RequireComponent(typeof(CustomerManager))]
public class StatManager : MonoBehaviour
{
    void OnEnable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnStaminaExhausted += OnStaminaExhausted;
    }

    void OnDisable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnStaminaExhausted -= OnStaminaExhausted;
    }

    private void OnStaminaExhausted()
    {
        GameSessionRoot.Instance?.Progress?.Die();
    }
}
