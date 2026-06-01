using UnityEngine;

/// <summary>
/// StatsService.OnMoneyChanged 이벤트 구독 → cashDrawer SFX 재생.
/// Managers 씬에 배치 (별도 GameObject 또는 GameSessionRoot 자식).
/// 부수효과(audio)를 POCO StatsService 밖으로 분리한 어댑터.
/// </summary>
public class StatsAudioAdapter : MonoBehaviour
{
    [SerializeField] private AudioClip cashDrawerSfx;

    private int _lastMoney = -1;

    private void OnEnable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats == null) return;
        _lastMoney = stats.GetMoney();
        stats.OnMoneyChanged += OnMoneyChanged;
    }

    private void OnDisable()
    {
        var stats = GameSessionRoot.Instance?.Stats;
        if (stats != null) stats.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int newAmount)
    {
        if (newAmount != _lastMoney && cashDrawerSfx != null)
            SoundManager.Instance?.Play2DSFX(cashDrawerSfx);
        _lastMoney = newAmount;
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
