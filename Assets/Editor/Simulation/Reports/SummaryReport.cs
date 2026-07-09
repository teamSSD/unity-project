using System.IO;
using System.Linq;
using System.Text;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>Sim 실행 한 판 전체 KPI 요약. 사람이 바로 읽기 좋은 markdown/text.
    /// 파산 여부, 최종/최대 자산, 총 매출 · 지출, 서빙율, 업그레이드 도달, 파산 원인 근사 등.</summary>
    public static class SummaryReport
    {
        public static void Write(SimEventLog log, string filePath, string policyName, int seed, int daysRequested)
        {
            var snapshots = log.OfType<DayEndSnapshotEvent>().ToList();
            var served = log.OfType<CustomerServedEvent>().ToList();
            var timedOut = log.OfType<CustomerTimedOutEvent>().ToList();
            var purchases = log.OfType<IngredientPurchasedEvent>().ToList();
            var upgrades = log.OfType<UpgradeMadeEvent>().ToList();
            var cashflows = log.OfType<PhaseCashFlowEvent>().ToList();

            var last = snapshots.LastOrDefault();
            bool bankrupt = last != null && last.bankrupt;
            int lastDay = last?.day ?? 0;
            int endMoney = last?.money ?? 0;
            int maxMoney = snapshots.Count > 0 ? snapshots.Max(s => s.money) : 0;
            int totalServed = served.Count;
            int totalTimedOut = timedOut.Count;
            int totalRevenue = served.Sum(s => s.reward);
            int totalPurchases = -cashflows.Where(c => c.category == "purchase").Sum(c => c.amount); // 양수화
            int totalUpgradeCost = -cashflows.Where(c => c.category == "upgrade").Sum(c => c.amount);
            int totalMgmt = -cashflows.Where(c => c.category == "mgmtFee").Sum(c => c.amount);
            float serveRate = (totalServed + totalTimedOut) > 0
                ? (float)totalServed / (totalServed + totalTimedOut) : 0f;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("# Sim Run Summary");
            sb.AppendLine();
            sb.AppendLine($"- Policy: **{policyName}**");
            sb.AppendLine($"- Seed: {seed}");
            sb.AppendLine($"- Days requested: {daysRequested}");
            sb.AppendLine($"- Days completed: **{lastDay + 1}**");
            sb.AppendLine($"- Status: **{(bankrupt ? "BANKRUPT" : "SURVIVED")}**");
            sb.AppendLine();
            sb.AppendLine("## Money");
            sb.AppendLine($"- End money: **{endMoney:N0}G**");
            sb.AppendLine($"- Peak money: {maxMoney:N0}G");
            sb.AppendLine();
            sb.AppendLine("## Cash Flow Totals");
            sb.AppendLine($"- Cook revenue: {totalRevenue:N0}G");
            sb.AppendLine($"- Purchase spend: {totalPurchases:N0}G");
            sb.AppendLine($"- Upgrade spend: {totalUpgradeCost:N0}G");
            sb.AppendLine($"- Management fee: {totalMgmt:N0}G");
            sb.AppendLine();
            sb.AppendLine("## Customers");
            sb.AppendLine($"- Served: **{totalServed}**");
            sb.AppendLine($"- Timed out: {totalTimedOut}");
            sb.AppendLine($"- Serve rate: **{serveRate * 100:F1}%**");
            sb.AppendLine();
            sb.AppendLine("## Upgrades acquired");
            if (upgrades.Count == 0) sb.AppendLine("- (none)");
            else
                foreach (var u in upgrades)
                    sb.AppendLine($"- Day {u.day} {u.category}.{u.trackId} → L{u.newLevel} (cost {u.cost:N0}G)");
            sb.AppendLine();
            if (bankrupt) DiagnoseBankruptcy(sb, snapshots, cashflows, served);

            File.WriteAllText(filePath, sb.ToString());
        }

        private static void DiagnoseBankruptcy(StringBuilder sb, System.Collections.Generic.List<DayEndSnapshotEvent> snaps,
            System.Collections.Generic.List<PhaseCashFlowEvent> cashflows, System.Collections.Generic.List<CustomerServedEvent> served)
        {
            sb.AppendLine("## Bankruptcy diagnosis (heuristic)");
            // 매출 0 연속 일수, 재고 empty 지속 여부, reserve 걸림 등을 대략 지목
            int lastDayWithCook = -1;
            foreach (var e in cashflows)
                if (e.category == "cook" && e.amount > 0) lastDayWithCook = System.Math.Max(lastDayWithCook, e.day);
            int lastDay = snaps.Count > 0 ? snaps[snaps.Count - 1].day : -1;
            int deadDays = lastDay - lastDayWithCook;
            sb.AppendLine($"- 마지막 요리 매출 발생 day: {lastDayWithCook}");
            sb.AppendLine($"- 이후 매출 0 지속: **{deadDays} 일**");
            if (deadDays >= 2)
                sb.AppendLine("- ↑ 이 기간 동안 재고 부족 or 정책이 매수를 못한 것으로 추정 (자세한 원인은 InventoryReport 참조).");
        }
    }
}
