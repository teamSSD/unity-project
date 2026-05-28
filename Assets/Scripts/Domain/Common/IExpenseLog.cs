namespace Game.Domain.Common
{
    /// <summary>
    /// 정산용 지출 기록 추상화. SettlementManager 직접 참조 회피.
    /// </summary>
    public interface IExpenseLog
    {
        void Add(string category, int amount);
    }
}
