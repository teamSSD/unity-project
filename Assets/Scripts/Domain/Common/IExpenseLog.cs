namespace Game.Domain.Common
{
    /// <summary>
    /// 정산용 지출 기록 추상화. Domain 레이어가 Unity 어댑터에 위임.
    /// </summary>
    public interface IExpenseLog
    {
        void Add(string category, int amount);
    }
}
