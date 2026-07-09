using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor.Simulation.Events;
using Game.Editor.Simulation.Policies;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Simulation
{
    /// <summary>어느 phase에 요리하는가 (영업 시간대) → 다른 지표 상관관계 실험.
    /// 조합: Morning-only (Baseline 기본) / MA / ME / MN / MAE / MEN / ALL 등.</summary>
    public static class PhaseTimingExperiment
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Phase Timing Experiment")]
        public static void Run() => RunCore(lateGame: false);

        [MenuItem("Tools/Simulation/Phase Timing Experiment (late-game buffer)")]
        public static void RunLateGame() => RunCore(lateGame: true);

        private static void RunCore(bool lateGame)
        {
            var configs = new List<PhaseTimingPolicy>
            {
                new PhaseTimingPolicy("M"),                                                           // Morning 강제만
                new PhaseTimingPolicy("+A", PhaseType.Afternoon) { ScalePurchase = true },            // M + A
                new PhaseTimingPolicy("+E", PhaseType.Evening) { ScalePurchase = true },              // M + E (E 손님 최다)
                new PhaseTimingPolicy("+N", PhaseType.Night) { ScalePurchase = true },                // M + N
                new PhaseTimingPolicy("+AE", PhaseType.Afternoon, PhaseType.Evening) { ScalePurchase = true },
                new PhaseTimingPolicy("+EN", PhaseType.Evening, PhaseType.Night) { ScalePurchase = true },
                new PhaseTimingPolicy("+AEN", PhaseType.Afternoon, PhaseType.Evening, PhaseType.Night) { ScalePurchase = true },
            };

            int[] seeds = Enumerable.Range(42, 15).ToArray();
            int days = 90;

            string tag = $"phase_timing_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            var results = new List<Row>();

            foreach (var policy in configs)
            {
                foreach (var seed in seeds)
                {
                    SimGameConfig cfg = null;
                    if (lateGame)
                    {
                        // 후반부 시뮬레이션: 초기 자산 500k + upgrade cost 0 (기 완료)
                        cfg = new SimGameConfig { startingMoney = 500_000, upgradeCostMultiplier = 0.001f };
                    }
                    var ctx = cfg != null ? SimContext.Build(seed, cfg) : SimContext.Build(seed);
                    ctx.Policy = policy;
                    new SimHarness(ctx).Run(days);

                    int served = ctx.Log.OfType<CustomerServedEvent>().Count();
                    int timedOut = ctx.Log.OfType<CustomerTimedOutEvent>().Count();
                    int upgrades = ctx.Log.OfType<UpgradeMadeEvent>().Count();
                    int cookRev = ctx.Log.OfType<PhaseCashFlowEvent>()
                        .Where(e => e.category == "cook").Sum(e => e.amount);
                    int purchaseSpend = -ctx.Log.OfType<PhaseCashFlowEvent>()
                        .Where(e => e.category == "purchase").Sum(e => e.amount);

                    results.Add(new Row
                    {
                        Label = policy.Name,
                        Seed = seed,
                        Bankrupt = ctx.Bankrupt,
                        EndMoney = ctx.Stats.GetMoney(),
                        Served = served,
                        TimedOut = timedOut,
                        Upgrades = upgrades,
                        CookRev = cookRev,
                        PurchaseSpend = purchaseSpend,
                    });
                }
            }

            WriteReport(batchDir, configs, results);
            Debug.Log($"[PhaseTiming] Done → {batchDir}");
        }

        private static void WriteReport(string dir, List<PhaseTimingPolicy> configs, List<Row> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Phase Timing Experiment — 영업 시간대 vs 지표 상관관계");
            sb.AppendLine();
            sb.AppendLine("15 seeds × 90일. 다른 로직은 Baseline과 동일 (SelectMenu/Upgrade/Purchase/Serving).");
            sb.AppendLine();
            sb.AppendLine("| Timing | 요리 phase | Survival | Median endMoney | Serve rate | Total cook rev | Purchase spend | Upgrades |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            foreach (var policy in configs)
            {
                var runs = results.Where(r => r.Label == policy.Name).ToList();
                float survival = (float)runs.Count(r => !r.Bankrupt) / runs.Count * 100f;
                var monies = runs.Select(r => r.EndMoney).OrderBy(x => x).ToList();
                int median = monies[monies.Count / 2];
                float serveRate = runs.Sum(r => r.Served + r.TimedOut) > 0
                    ? (float)runs.Sum(r => r.Served) / runs.Sum(r => r.Served + r.TimedOut) * 100f
                    : 0f;
                var revs = runs.Select(r => r.CookRev).OrderBy(x => x).ToList();
                int medRev = revs[revs.Count / 2];
                var spends = runs.Select(r => r.PurchaseSpend).OrderBy(x => x).ToList();
                int medSpend = spends[spends.Count / 2];
                float avgUpg = (float)runs.Average(r => r.Upgrades);

                string phaseStr = policy.Name.Substring(2);
                sb.AppendLine($"| {policy.Name} | {phaseStr} | {survival:F0}% | {median:N0}G | {serveRate:F1}% | " +
                              $"{medRev:N0}G | {medSpend:N0}G | {avgUpg:F1} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 상관관계 관찰");
            sb.AppendLine("- 요리 페이즈 수 vs endMoney/서빙율");
            sb.AppendLine("- 저녁(E) 손님 최다 → E 포함 조합의 서빙율/매출 상승?");
            sb.AppendLine("- 요리 페이즈 증가 vs Shopping 페이즈 감소 → 재료 매수 부족 or 여유?");

            File.WriteAllText(Path.Combine(dir, "PHASE_TIMING.md"), sb.ToString());

            // CSV
            using var w = new StreamWriter(Path.Combine(dir, "phase_timing.csv"), false);
            w.WriteLine("timing,seed,bankrupt,endMoney,served,timedOut,cookRev,purchase,upgrades");
            foreach (var r in results)
            {
                w.WriteLine($"{r.Label},{r.Seed},{(r.Bankrupt ? 1 : 0)},{r.EndMoney}," +
                            $"{r.Served},{r.TimedOut},{r.CookRev},{r.PurchaseSpend},{r.Upgrades}");
            }
        }

        private class Row
        {
            public string Label;
            public int Seed, EndMoney, Served, TimedOut, Upgrades, CookRev, PurchaseSpend;
            public bool Bankrupt;
        }
    }
}
