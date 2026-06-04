namespace Game.Domain.Cooking
{
    /// <summary>
    /// 쿠킹 세션 내 누적 통계 (POCO). CustomerManager에서 추출.
    /// 주문수/완벽주문/정확도누계/총수익 + 등급 산출.
    /// </summary>
    public class CustomerSessionStats
    {
        public int TotalOrders { get; private set; }
        public int PerfectOrders { get; private set; }
        public float TotalAccuracyScore { get; private set; }
        public int TotalEarnings { get; private set; }

        public float AverageAccuracy =>
            TotalOrders > 0 ? TotalAccuracyScore / TotalOrders : 0f;

        public float PerfectRatePercent =>
            TotalOrders > 0 ? (float)PerfectOrders / TotalOrders * 100f : 0f;

        public void RecordOrderServed(float accuracyScore, int reward)
        {
            TotalOrders++;
            TotalAccuracyScore += accuracyScore;
            if (accuracyScore >= 1.0f) PerfectOrders++;
            TotalEarnings += reward;
        }

        public (int total, int perfect, float avgScore, int earnings) Snapshot()
            => (TotalOrders, PerfectOrders, AverageAccuracy, TotalEarnings);
    }
}
