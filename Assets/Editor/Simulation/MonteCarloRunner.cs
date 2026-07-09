using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor.Simulation.Events;
using Game.Editor.Simulation.Policies;
using Game.Editor.Simulation.Reports;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Simulation
{
    /// <summary>여러 시드 × 여러 정책 실행. RNG 분산을 통계로 요약.
    /// 결과: 정책별 median/p10/p90 자산 + 파산율.</summary>
    public static class MonteCarloRunner
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Monte Carlo (30 seeds × all policies, 30 days)")]
        public static void RunMonteCarlo30() => RunMonteCarlo(days: 30);

        [MenuItem("Tools/Simulation/Monte Carlo (30 seeds × all policies, 60 days)")]
        public static void RunMonteCarlo60() => RunMonteCarlo(days: 60);

        [MenuItem("Tools/Simulation/Monte Carlo (30 seeds × all policies, 90 days)")]
        public static void RunMonteCarlo90() => RunMonteCarlo(days: 90);

        public static void RunMonteCarlo(int days)
        {
            IPlayerPolicy[] MakePolicies() => new IPlayerPolicy[] {
                new NovicePolicy(), new CautiousPolicy(), new BaselinePolicy(),
                new AggressivePolicy(), new MinMaxPolicy(), new FarmMaxPolicy(),
                new NoFarmPolicy(), new NoUpgradePolicy(), new SideStuffedPolicy(),
                new MenuDiversifyPolicy(), new AdaptiveSidePolicy(),
                new FarmAwarePolicy(), new AdaptiveFarmSidePolicy(),
            };

            int[] seeds = Enumerable.Range(42, 30).ToArray();

            string tag = $"mc_{days}d_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            // 정책별로 결과 누적
            var perPolicy = new Dictionary<string, List<RunResult>>();
            foreach (var pName in MakePolicies().Select(p => p.Name))
                perPolicy[pName] = new List<RunResult>();

            int totalRuns = seeds.Length * MakePolicies().Length;
            int done = 0;
            var progressBar = false; // GUI progress

            foreach (var seed in seeds)
            {
                foreach (var policy in MakePolicies())
                {
                    var ctx = SimContext.Build(seed);
                    ctx.Policy = policy;
                    new SimHarness(ctx).Run(days: days);

                    perPolicy[policy.Name].Add(new RunResult
                    {
                        Seed = seed,
                        DaysCompleted = ctx.State.phase.Day,
                        Bankrupt = ctx.Bankrupt,
                        EndMoney = ctx.Stats.GetMoney(),
                        Served = ctx.Log.OfType<CustomerServedEvent>().Count(),
                        TimedOut = ctx.Log.OfType<CustomerTimedOutEvent>().Count(),
                    });
                    done++;
                    if (done % 20 == 0) Debug.Log($"[MC] {done}/{totalRuns} 완료");
                }
            }

            WriteReport(batchDir, perPolicy);
            Debug.Log($"[MC] Done → {batchDir}");
        }

        private static void WriteReport(string dir, Dictionary<string, List<RunResult>> perPolicy)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Monte Carlo Results (30 seeds × {perPolicy.First().Value.FirstOrDefault()?.DaysCompleted ?? 0}~ days)");
            sb.AppendLine();
            sb.AppendLine("| Policy | Survival | End money (median) | p10 | p90 | Serve rate |");
            sb.AppendLine("|---|---|---|---|---|---|");

            // 정책 순: median 자산 desc
            var ranked = perPolicy.OrderByDescending(kv => Median(kv.Value.Select(r => r.EndMoney).ToList())).ToList();

            foreach (var kv in ranked)
            {
                var results = kv.Value;
                if (results.Count == 0) continue;
                float survivalRate = (float)results.Count(r => !r.Bankrupt) / results.Count * 100f;
                var moneys = results.Select(r => r.EndMoney).OrderBy(x => x).ToList();
                int median = Median(moneys);
                int p10 = Percentile(moneys, 10);
                int p90 = Percentile(moneys, 90);
                int totalServed = results.Sum(r => r.Served);
                int totalTimedOut = results.Sum(r => r.TimedOut);
                float serveRate = (totalServed + totalTimedOut) > 0
                    ? (float)totalServed / (totalServed + totalTimedOut) * 100f : 0f;
                sb.AppendLine($"| {kv.Key} | {survivalRate:F0}% | {median:N0}G | {p10:N0}G | {p90:N0}G | {serveRate:F1}% |");
            }

            sb.AppendLine();
            sb.AppendLine("## 해석");
            var alwaysSurvive = ranked.Where(kv => kv.Value.All(r => !r.Bankrupt)).Select(kv => kv.Key).ToList();
            var alwaysBankrupt = ranked.Where(kv => kv.Value.All(r => r.Bankrupt)).Select(kv => kv.Key).ToList();
            var mixed = ranked.Where(kv => kv.Value.Any(r => !r.Bankrupt) && kv.Value.Any(r => r.Bankrupt)).Select(kv => kv.Key).ToList();
            sb.AppendLine();
            if (alwaysSurvive.Count > 0)
                sb.AppendLine($"- ✅ **항상 생존** (30/30 seeds): {string.Join(", ", alwaysSurvive)} → seed에 강함, 안정 정책");
            if (mixed.Count > 0)
                sb.AppendLine($"- ⚠️ **seed 의존적** (일부만 생존): {string.Join(", ", mixed)} → RNG 편차에 취약, 경계선 정책");
            if (alwaysBankrupt.Count > 0)
                sb.AppendLine($"- ❌ **항상 파산** (0/30 seeds): {string.Join(", ", alwaysBankrupt)} → 구조적 실패, 규모 관계없이 안 됨");

            File.WriteAllText(Path.Combine(dir, "SUMMARY.md"), sb.ToString());

            // CSV: seed × policy 매트릭스 (endMoney)
            using (var w = new StreamWriter(Path.Combine(dir, "matrix_end_money.csv"), false))
            {
                var policies = perPolicy.Keys.ToList();
                w.WriteLine("seed," + string.Join(",", policies));
                var seeds = perPolicy.First().Value.Select(r => r.Seed).OrderBy(s => s).ToList();
                foreach (var seed in seeds)
                {
                    var row = new List<string> { seed.ToString() };
                    foreach (var p in policies)
                    {
                        var r = perPolicy[p].FirstOrDefault(x => x.Seed == seed);
                        row.Add(r?.EndMoney.ToString() ?? "0");
                    }
                    w.WriteLine(string.Join(",", row));
                }
            }
        }

        private static int Median(List<int> sorted) => sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
        private static int Percentile(List<int> sorted, int p)
        {
            if (sorted.Count == 0) return 0;
            int idx = System.Math.Min(sorted.Count - 1, sorted.Count * p / 100);
            return sorted[idx];
        }

        private class RunResult
        {
            public int Seed, DaysCompleted, EndMoney, Served, TimedOut;
            public bool Bankrupt;
        }
    }
}
