using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>여러 정책 실행 결과 나란히 비교. 정책별 로그를 받아 markdown 표 생성.</summary>
    public static class PolicyComparisonReport
    {
        public class PolicyResult
        {
            public string PolicyName;
            public int Seed;
            public int DaysCompleted;
            public bool Bankrupt;
            public int EndMoney;
            public int PeakMoney;
            public int TotalRevenue;
            public int TotalPurchase;
            public int TotalUpgrade;
            public int Served;
            public int TimedOut;
            public Dictionary<string, int> TimeoutByReason;
        }

        public static PolicyResult Extract(SimEventLog log, string name, int seed)
        {
            var snaps = log.OfType<DayEndSnapshotEvent>().ToList();
            var served = log.OfType<CustomerServedEvent>().ToList();
            var timedOut = log.OfType<CustomerTimedOutEvent>().ToList();
            var cashflows = log.OfType<PhaseCashFlowEvent>().ToList();

            var reasonCount = new Dictionary<string, int>();
            foreach (var t in timedOut)
            {
                var r = string.IsNullOrEmpty(t.reason) ? "OTHER" : t.reason;
                reasonCount.TryGetValue(r, out int c);
                reasonCount[r] = c + 1;
            }

            return new PolicyResult
            {
                PolicyName = name,
                Seed = seed,
                DaysCompleted = snaps.Count > 0 ? snaps[snaps.Count - 1].day + 1 : 0,
                Bankrupt = snaps.Count > 0 && snaps[snaps.Count - 1].bankrupt,
                EndMoney = snaps.Count > 0 ? snaps[snaps.Count - 1].money : 0,
                PeakMoney = snaps.Count > 0 ? snaps.Max(s => s.money) : 0,
                TotalRevenue = served.Sum(s => s.reward),
                TotalPurchase = -cashflows.Where(c => c.category == "purchase").Sum(c => c.amount),
                TotalUpgrade = -cashflows.Where(c => c.category == "upgrade").Sum(c => c.amount),
                Served = served.Count,
                TimedOut = timedOut.Count,
                TimeoutByReason = reasonCount,
            };
        }

        public static void Write(List<PolicyResult> results, string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            var sb = new StringBuilder();
            sb.AppendLine("# Policy Comparison");
            sb.AppendLine();
            sb.AppendLine("| Metric | " + string.Join(" | ", results.Select(r => r.PolicyName)) + " |");
            sb.AppendLine("|---|" + string.Join("|", results.Select(_ => "---")) + "|");
            sb.AppendLine("| Days completed | " + string.Join(" | ", results.Select(r => r.DaysCompleted.ToString())) + " |");
            sb.AppendLine("| Status | " + string.Join(" | ", results.Select(r => r.Bankrupt ? "BANKRUPT" : "SURVIVED")) + " |");
            sb.AppendLine("| End money | " + string.Join(" | ", results.Select(r => $"{r.EndMoney:N0}G")) + " |");
            sb.AppendLine("| Peak money | " + string.Join(" | ", results.Select(r => $"{r.PeakMoney:N0}G")) + " |");
            sb.AppendLine("| Cook revenue | " + string.Join(" | ", results.Select(r => $"{r.TotalRevenue:N0}G")) + " |");
            sb.AppendLine("| Purchase spend | " + string.Join(" | ", results.Select(r => $"{r.TotalPurchase:N0}G")) + " |");
            sb.AppendLine("| Upgrade spend | " + string.Join(" | ", results.Select(r => $"{r.TotalUpgrade:N0}G")) + " |");
            sb.AppendLine("| Served | " + string.Join(" | ", results.Select(r => r.Served.ToString())) + " |");
            sb.AppendLine("| Timed out | " + string.Join(" | ", results.Select(r => r.TimedOut.ToString())) + " |");
            sb.AppendLine("| Serve rate | " + string.Join(" | ", results.Select(r =>
                (r.Served + r.TimedOut) > 0 ? $"{(float)r.Served / (r.Served + r.TimedOut) * 100f:F1}%" : "0%")) + " |");
            sb.AppendLine();

            sb.AppendLine("## Timeout causes");
            sb.AppendLine("| Cause | " + string.Join(" | ", results.Select(r => r.PolicyName)) + " |");
            sb.AppendLine("|---|" + string.Join("|", results.Select(_ => "---")) + "|");
            foreach (var cause in new[] { "INSUFFICIENT_BUY", "MID_DAY_RUN_OUT", "RECIPE_UNKNOWN", "OTHER" })
            {
                var row = new List<string> { cause };
                foreach (var r in results)
                {
                    r.TimeoutByReason.TryGetValue(cause, out int c);
                    float pct = r.TimedOut > 0 ? (float)c / r.TimedOut * 100f : 0f;
                    row.Add(c > 0 ? $"{c} ({pct:F1}%)" : "0");
                }
                sb.AppendLine("| " + string.Join(" | ", row) + " |");
            }
            sb.AppendLine();

            sb.AppendLine("## Interpretation");
            sb.AppendLine();
            // 자동 진단 (개선된 로직)
            var allBankrupt = results.All(r => r.Bankrupt);
            var anySurvived = results.Any(r => !r.Bankrupt);
            var survivors = results.Where(r => !r.Bankrupt).ToList();

            if (allBankrupt)
                sb.AppendLine("- ❌ **모든 정책 파산** → 게임 룰 밸런스 문제 가능성. 경제 순환 성립 안 되거나 초기 자원 부족.");
            else if (anySurvived)
            {
                sb.AppendLine($"- ✅ **{survivors.Count}개 정책 생존** ({string.Join(", ", survivors.Select(r => r.PolicyName))}) → 게임 룰은 성립. 나머지는 정책 튜닝 대상.");
                var bankrupts = results.Where(r => r.Bankrupt).ToList();
                if (bankrupts.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("### 파산 정책별 원인 지목:");
                    foreach (var r in bankrupts)
                    {
                        r.TimeoutByReason.TryGetValue("INSUFFICIENT_BUY", out int ib);
                        r.TimeoutByReason.TryGetValue("MID_DAY_RUN_OUT", out int mdro);
                        string diag;
                        if (ib > mdro && r.Served == 0)
                            diag = "구매 아예 못 함 → 예산 로직 문제 (안전금 너무 큼 등)";
                        else if (ib > mdro)
                            diag = "필요 재료 계산 부족 or 예산 배분 실패 → perMain 상향 or upgrade 우선순위 조정";
                        else if (r.TotalPurchase > r.TotalRevenue)
                            diag = "구매가 매출보다 많음 → 과잉 매수, target 하향";
                        else
                            diag = "혼합 이슈 (개별 파일 상세 참조)";
                        sb.AppendLine($"- **{r.PolicyName}** (Day {r.DaysCompleted}): {diag}");
                    }
                }
            }

            File.WriteAllText(filePath, sb.ToString());
        }
    }
}
