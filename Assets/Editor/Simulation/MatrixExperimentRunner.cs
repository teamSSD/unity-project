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
    /// <summary>4축 × 2^4 = 16 조합의 정책 매트릭스 실험.
    /// Farm × Side × UpgradeAggressive × SkillHigh, 각 5 seeds × 30일 → 80 runs.
    /// 결과: 조합별 median 자산 궤적, 생존율, 최종 자산.</summary>
    public static class MatrixExperimentRunner
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Matrix Experiment (16 combos × 5 seeds × 30 days)")]
        public static void RunMatrix()
        {
            int[] seeds = Enumerable.Range(42, 5).ToArray(); // 42~46
            int days = 30;

            var combos = new List<MatrixPolicy>();
            foreach (bool useFarm in new[] { false, true })
                foreach (bool useSide in new[] { false, true })
                    foreach (bool aggUp in new[] { false, true })
                        foreach (bool skillHigh in new[] { false, true })
                            combos.Add(new MatrixPolicy(useFarm, useSide, aggUp, skillHigh));

            string tag = $"matrix_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            var perCombo = new Dictionary<string, List<Result>>();

            foreach (var combo in combos)
            {
                foreach (var seed in seeds)
                {
                    var ctx = SimContext.Build(seed);
                    // NOTE: MatrixPolicy 재사용 시 latch 상태 leak 방지 — new 인스턴스로 재생성
                    var freshPolicy = new MatrixPolicy(combo.useFarm, combo.useSide, combo.aggressiveUpgrade, combo.skillHigh);
                    ctx.Policy = freshPolicy;
                    new SimHarness(ctx).Run(days);

                    if (!perCombo.TryGetValue(combo.Name, out var list))
                    {
                        list = new List<Result>();
                        perCombo[combo.Name] = list;
                    }
                    list.Add(new Result
                    {
                        seed = seed,
                        bankrupt = ctx.Bankrupt,
                        endMoney = ctx.Stats.GetMoney(),
                        served = ctx.Log.OfType<CustomerServedEvent>().Count(),
                        useFarm = combo.useFarm,
                        useSide = combo.useSide,
                        aggUp = combo.aggressiveUpgrade,
                        skillHigh = combo.skillHigh,
                    });
                }
            }

            WriteReport(batchDir, perCombo);
            Debug.Log($"[Matrix] Done → {batchDir}");
        }

        private static void WriteReport(string dir, Dictionary<string, List<Result>> perCombo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Matrix Experiment — 16 조합 × 5 seeds × 30일");
            sb.AppendLine();
            sb.AppendLine("**축**: Farm 활용 (F/-) × Side 채택 (S/-) × Upgrade 적극 (U/u) × Skill 상 (K/k)");
            sb.AppendLine();
            sb.AppendLine("| Combo | Farm | Side | Upgrade | Skill | Survival | Median | Avg served |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            var ranked = perCombo
                .Select(kv => new
                {
                    Name = kv.Key,
                    Results = kv.Value,
                    Median = Median(kv.Value.Select(r => r.endMoney).OrderBy(x => x).ToList()),
                })
                .OrderByDescending(x => x.Median);

            foreach (var row in ranked)
            {
                var r = row.Results[0];
                float survival = (float)row.Results.Count(x => !x.bankrupt) / row.Results.Count * 100f;
                float avgServed = (float)row.Results.Sum(x => x.served) / row.Results.Count;
                sb.AppendLine($"| {row.Name} | {(r.useFarm ? "O" : "X")} | {(r.useSide ? "O" : "X")} " +
                              $"| {(r.aggUp ? "적극" : "소극")} | {(r.skillHigh ? "상" : "하")} " +
                              $"| {survival:F0}% | {row.Median:N0}G | {avgServed:F0} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 축별 영향 분석 (marginal effect)");
            sb.AppendLine();
            var allResults = perCombo.SelectMany(kv => kv.Value).ToList();

            sb.AppendLine("| 축 | ON 평균 (median) | OFF 평균 (median) | Δ |");
            sb.AppendLine("|---|---|---|---|");
            LineFor(sb, "Farm", allResults, r => r.useFarm);
            LineFor(sb, "Side", allResults, r => r.useSide);
            LineFor(sb, "Upgrade 적극", allResults, r => r.aggUp);
            LineFor(sb, "Skill 상", allResults, r => r.skillHigh);

            File.WriteAllText(Path.Combine(dir, "MATRIX.md"), sb.ToString());

            // CSV: full raw
            using var w = new StreamWriter(Path.Combine(dir, "matrix_raw.csv"), false);
            w.WriteLine("combo,seed,bankrupt,endMoney,served,useFarm,useSide,aggUp,skillHigh");
            foreach (var kv in perCombo)
            {
                foreach (var r in kv.Value)
                {
                    w.WriteLine($"{kv.Key},{r.seed},{(r.bankrupt ? 1 : 0)},{r.endMoney},{r.served}," +
                                $"{(r.useFarm ? 1 : 0)},{(r.useSide ? 1 : 0)},{(r.aggUp ? 1 : 0)},{(r.skillHigh ? 1 : 0)}");
                }
            }
        }

        private static void LineFor(StringBuilder sb, string label, List<Result> all, System.Func<Result, bool> pred)
        {
            var on = all.Where(pred).Select(r => r.endMoney).OrderBy(x => x).ToList();
            var off = all.Where(r => !pred(r)).Select(r => r.endMoney).OrderBy(x => x).ToList();
            int mOn = Median(on), mOff = Median(off);
            sb.AppendLine($"| {label} | {mOn:N0}G | {mOff:N0}G | {mOn - mOff:+#,##0;-#,##0}G |");
        }

        private static int Median(List<int> sorted) => sorted.Count == 0 ? 0 : sorted[sorted.Count / 2];

        private class Result
        {
            public int seed, endMoney, served;
            public bool bankrupt, useFarm, useSide, aggUp, skillHigh;
        }
    }
}
