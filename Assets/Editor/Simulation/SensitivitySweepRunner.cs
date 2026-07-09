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
    /// <summary>게임 룰 파라미터 스윕. Baseline 정책 기준 사이드 계수 × upgrade 비용 × 초기 자산 그리드 실행.
    /// 각 조합 5 seeds → median 자산 + 생존율 리포트.
    /// 결과: heatmap CSV + sweet spot 자동 지목.</summary>
    public static class SensitivitySweepRunner
    {
        private const string OutputDir = "tmp/simulation/runs";
        // rev: side coef sweep menu item

        [MenuItem("Tools/Simulation/Side Coef Sweep")]
        public static void RunSideSweep()
        {
            // 사이드 계수만 sweep. SideStuffed 정책이 사이드 3개 붙임.
            float[] coefs = { 0.05f, 0.10f, 0.15f, 0.20f, 0.25f, 0.30f, 0.40f };
            int[] seeds = Enumerable.Range(42, 10).ToArray();
            int days = 60;

            string tag = $"sidesweep_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string dir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(dir);

            var rows = new List<(float coef, float survival, int median, float avgServed, int avgReward)>();
            foreach (var coef in coefs)
            {
                var cfg = new SimGameConfig
                {
                    upgradeCostMultiplier = 0.75f, // 이미 튜닝된 값
                    sideCoefficient = coef,
                    startingMoney = 12000,        // 이미 튜닝된 값
                };
                var moneys = new List<int>();
                int bankrupt = 0;
                int totalServed = 0, totalReward = 0;
                foreach (var seed in seeds)
                {
                    var ctx = SimContext.Build(seed, cfg);
                    ctx.Policy = new SideStuffedPolicy();
                    new SimHarness(ctx).Run(days);
                    moneys.Add(ctx.Stats.GetMoney());
                    if (ctx.Bankrupt) bankrupt++;
                    var served = ctx.Log.OfType<Events.CustomerServedEvent>().ToList();
                    totalServed += served.Count;
                    totalReward += served.Sum(s => s.reward);
                }
                moneys.Sort();
                rows.Add((
                    coef,
                    (float)(seeds.Length - bankrupt) / seeds.Length,
                    moneys[moneys.Count / 2],
                    (float)totalServed / seeds.Length,
                    totalServed > 0 ? totalReward / totalServed : 0
                ));
                Debug.Log($"[SideSweep] coef={coef:F2} 완료");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"# Side Coefficient Sweep — SideStuffed × {days}d × 10 seeds");
            sb.AppendLine();
            sb.AppendLine("(Upgrade cost 0.75×, Starting money 12,000G, 60일 기준)");
            sb.AppendLine();
            sb.AppendLine("| Side coef | 생존율 | Median 자산 | 평균 서빙수 | 서빙당 avg 보상 |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var r in rows)
                sb.AppendLine($"| {r.coef:F2} | {r.survival * 100:F0}% | {r.median:N0}G | {r.avgServed:F0} | {r.avgReward:N0}G |");

            sb.AppendLine();
            sb.AppendLine("## Break-even 지목");
            var breakEven = rows.FirstOrDefault(r => r.survival >= 0.5f);
            if (breakEven.coef > 0)
                sb.AppendLine($"- **최소 생존 side coef ≥ {breakEven.coef:F2}** (50% 이상 seed 생존)");
            else
                sb.AppendLine("- **모든 side coef에서 SideStuffed 파산** — 사이드는 근본적으로 순손실");

            var best = rows.OrderByDescending(r => r.median).First();
            sb.AppendLine($"- **Sweet spot: side coef = {best.coef:F2}** (median {best.median:N0}G, 생존율 {best.survival * 100:F0}%)");

            // Baseline과 비교 (사이드 안 붙임): sideStuffed sweet spot이 Baseline 60d(815k)를 넘는지
            sb.AppendLine();
            sb.AppendLine("## 해석");
            if (best.median >= 815000)
                sb.AppendLine($"- SideStuffed({best.coef:F2}) 자산 {best.median:N0}G ≥ Baseline 60d(815k) → **사이드 붙이는 게 유리한 계수 존재**. side coef를 {best.coef:F2}로 조정 권장.");
            else
                sb.AppendLine($"- SideStuffed 최고 {best.median:N0}G < Baseline 60d(815k) → **어떤 계수에도 사이드 안 붙이는 게 여전히 우세**. Baseline 유지.");

            File.WriteAllText(Path.Combine(dir, "SIDE_SWEEP.md"), sb.ToString());

            using (var w = new StreamWriter(Path.Combine(dir, "grid.csv"), false))
            {
                w.WriteLine("sideCoef,survivalRate,medianEndMoney,avgServed,avgRewardPerServe");
                foreach (var r in rows)
                    w.WriteLine($"{r.coef},{r.survival:F2},{r.median},{r.avgServed:F1},{r.avgReward}");
            }
            Debug.Log($"[SideSweep] Done → {dir}");
        }

        [MenuItem("Tools/Simulation/Sensitivity Sweep (Baseline × 27 configs × 5 seeds)")]
        public static void RunSweep()
        {
            // 파라미터 그리드
            float[] upgradeCosts = { 0.5f, 0.75f, 1.0f };   // 원본, 25%↓, 50%↓
            float[] sideCoefs    = { 0.05f, 0.15f, 0.25f }; // 원본, 3배, 5배
            int[] startMoneys    = { 8000, 12000, 16000 };  // 원본, 1.5x, 2x
            int[] seeds          = Enumerable.Range(42, 5).ToArray();

            string tag = $"sweep_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string dir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(dir);

            var results = new List<Config>();

            int totalConfigs = upgradeCosts.Length * sideCoefs.Length * startMoneys.Length;
            int cfgIdx = 0;
            foreach (var upCost in upgradeCosts)
            foreach (var sideCoef in sideCoefs)
            foreach (var startM in startMoneys)
            {
                cfgIdx++;
                var cfg = new SimGameConfig
                {
                    upgradeCostMultiplier = upCost,
                    sideCoefficient = sideCoef,
                    startingMoney = startM,
                };

                var runs = new List<(int seed, bool bankrupt, int endMoney, int served)>();
                foreach (var seed in seeds)
                {
                    var ctx = SimContext.Build(seed, cfg);
                    ctx.Policy = new BaselinePolicy();
                    new SimHarness(ctx).Run(days: 30);
                    runs.Add((seed, ctx.Bankrupt, ctx.Stats.GetMoney(), ctx.Log.OfType<CustomerServedEvent>().Count()));
                }

                results.Add(new Config
                {
                    UpgradeCostMul = upCost,
                    SideCoef = sideCoef,
                    StartMoney = startM,
                    SurvivalRate = (float)runs.Count(r => !r.bankrupt) / runs.Count,
                    MedianEndMoney = Median(runs.Select(r => r.endMoney).ToList()),
                    AvgServed = (float)runs.Sum(r => r.served) / runs.Count,
                });
                if (cfgIdx % 5 == 0) Debug.Log($"[Sweep] {cfgIdx}/{totalConfigs} 완료");
            }

            WriteReport(dir, results);
            Debug.Log($"[Sweep] Done → {dir}");
        }

        private static void WriteReport(string dir, List<Config> results)
        {
            // 1. Full grid CSV
            using (var w = new StreamWriter(Path.Combine(dir, "grid.csv"), false))
            {
                w.WriteLine("upgradeCostMul,sideCoef,startMoney,survivalRate,medianEndMoney,avgServed");
                foreach (var r in results)
                    w.WriteLine($"{r.UpgradeCostMul},{r.SideCoef},{r.StartMoney},{r.SurvivalRate:F2},{r.MedianEndMoney},{r.AvgServed:F1}");
            }

            // 2. Sweet spot summary
            var sb = new StringBuilder();
            sb.AppendLine("# Sensitivity Sweep Results (Baseline × 27 configs × 5 seeds)");
            sb.AppendLine();
            sb.AppendLine("파라미터별 결과 요약. 축은 각 3단계.");
            sb.AppendLine();

            // Best config (survival 우선, tie면 median money)
            var best = results.OrderByDescending(r => r.SurvivalRate)
                              .ThenByDescending(r => r.MedianEndMoney).First();
            sb.AppendLine("## 🎯 Sweet Spot");
            sb.AppendLine($"- **Upgrade cost multiplier**: {best.UpgradeCostMul:F2}");
            sb.AppendLine($"- **Side coefficient**: {best.SideCoef:F2}");
            sb.AppendLine($"- **Starting money**: {best.StartMoney:N0}G");
            sb.AppendLine($"- **결과**: {best.SurvivalRate * 100:F0}% survival, median {best.MedianEndMoney:N0}G, avg {best.AvgServed:F0} served");
            sb.AppendLine();

            // 파라미터별 marginal effect
            sb.AppendLine("## 파라미터별 영향 (marginal — 다른 파라미터 평균)");
            sb.AppendLine();
            sb.AppendLine("### Upgrade cost multiplier");
            sb.AppendLine("| Value | Avg survival | Avg median money |");
            sb.AppendLine("|---|---|---|");
            foreach (var v in results.Select(r => r.UpgradeCostMul).Distinct().OrderBy(x => x))
            {
                var subset = results.Where(r => System.Math.Abs(r.UpgradeCostMul - v) < 0.001f).ToList();
                sb.AppendLine($"| {v:F2} | {subset.Average(r => r.SurvivalRate) * 100:F0}% | {subset.Average(r => r.MedianEndMoney):N0}G |");
            }
            sb.AppendLine();
            sb.AppendLine("### Side coefficient");
            sb.AppendLine("| Value | Avg survival | Avg median money |");
            sb.AppendLine("|---|---|---|");
            foreach (var v in results.Select(r => r.SideCoef).Distinct().OrderBy(x => x))
            {
                var subset = results.Where(r => System.Math.Abs(r.SideCoef - v) < 0.001f).ToList();
                sb.AppendLine($"| {v:F2} | {subset.Average(r => r.SurvivalRate) * 100:F0}% | {subset.Average(r => r.MedianEndMoney):N0}G |");
            }
            sb.AppendLine();
            sb.AppendLine("### Starting money");
            sb.AppendLine("| Value | Avg survival | Avg median money |");
            sb.AppendLine("|---|---|---|");
            foreach (var v in results.Select(r => r.StartMoney).Distinct().OrderBy(x => x))
            {
                var subset = results.Where(r => r.StartMoney == v).ToList();
                sb.AppendLine($"| {v:N0}G | {subset.Average(r => r.SurvivalRate) * 100:F0}% | {subset.Average(r => r.MedianEndMoney):N0}G |");
            }
            sb.AppendLine();

            // 원본(1.0, 0.05, 8000)과 sweet spot 비교
            var original = results.FirstOrDefault(r =>
                System.Math.Abs(r.UpgradeCostMul - 1.0f) < 0.001f &&
                System.Math.Abs(r.SideCoef - 0.05f) < 0.001f &&
                r.StartMoney == 8000);
            if (original != null)
            {
                sb.AppendLine("## 원본 vs Sweet Spot");
                sb.AppendLine($"- **원본** (upCost=1.0, side=0.05, start=8000): {original.SurvivalRate * 100:F0}% 생존, median {original.MedianEndMoney:N0}G");
                sb.AppendLine($"- **Sweet spot**: {best.SurvivalRate * 100:F0}% 생존, median {best.MedianEndMoney:N0}G");
                sb.AppendLine($"- **개선폭**: 생존율 +{(best.SurvivalRate - original.SurvivalRate) * 100:F0}%p, 자산 +{best.MedianEndMoney - original.MedianEndMoney:N0}G");
            }

            File.WriteAllText(Path.Combine(dir, "SWEEP_SUMMARY.md"), sb.ToString());
        }

        private static int Median(List<int> vals)
        {
            var sorted = vals.OrderBy(x => x).ToList();
            return sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];
        }

        private class Config
        {
            public float UpgradeCostMul, SideCoef;
            public int StartMoney, MedianEndMoney;
            public float SurvivalRate, AvgServed;
        }
    }
}
