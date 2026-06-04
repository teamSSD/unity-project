namespace Game.Domain.Common
{
    /// <summary>
    /// 골드 보관/차감 추상화. Domain 레이어가 Unity StatsService에 직접 의존하지 않도록 분리.
    /// Game.Unity의 어댑터(StatsMoneyAdapter)가 GameSessionRoot.Instance.Stats로 위임.
    /// </summary>
    public interface IMoneyService
    {
        int Current { get; }
        bool TrySpend(int amount);
        void Add(int amount);
    }
}
