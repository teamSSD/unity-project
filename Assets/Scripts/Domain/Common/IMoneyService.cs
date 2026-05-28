namespace Game.Domain.Common
{
    /// <summary>
    /// 골드 보관/차감 추상화. Service 레이어가 StatsSystem(MonoBehaviour) 직접 참조 회피용.
    /// Game.Unity의 어댑터가 StatsSystem.Instance에 위임.
    /// </summary>
    public interface IMoneyService
    {
        int Current { get; }
        bool TrySpend(int amount);
    }
}
