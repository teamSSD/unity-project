using System.Collections.Generic;
using System.IO;
using Game.Editor.Simulation.Policies;
using Game.Editor.Simulation.Reports;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Simulation
{
    /// <summary>[MenuItem] sim 실행 진입점.</summary>
    public static class SimRunner
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Run Baseline (seed=42, days=30)")]
        public static void RunBaseline() => RunOne(new BaselinePolicy(), 42, 30);

        [MenuItem("Tools/Simulation/Run SideStuffed (seed=42, days=30)")]
        public static void RunSideStuffed() => RunOne(new SideStuffedPolicy(), 42, 30);

        [MenuItem("Tools/Simulation/Run Baseline (seed=42, days=60)")]
        public static void RunBaseline60() => RunOne(new BaselinePolicy(), 42, 60);

        [MenuItem("Tools/Simulation/Run AdaptiveSide (seed=42, days=60)")]
        public static void RunAdaptive60() => RunOne(new AdaptiveSidePolicy(), 42, 60);

        [MenuItem("Tools/Simulation/Run Baseline dynamic-unlock (seed=42, days=90)")]
        public static void RunBaselineDynamic90()
        {
            var cfg = new SimGameConfig
            {
                AssetTriggeredUnlock = new System.Collections.Generic.List<(int, string)>
                {
                    (160_000, "I049"), (160_000, "I034"),
                    (230_000, "I039"), (280_000, "I053"),
                }
            };
            var ctx = SimContext.Build(42, cfg);
            ctx.Policy = new BaselinePolicy();
            new SimHarness(ctx).Run(90);
            string tag = $"BaselineDyn_seed42_90d_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string runDir = System.IO.Path.Combine("tmp/simulation/runs", tag);
            System.IO.Directory.CreateDirectory(runDir);
            ctx.Log.Flush(System.IO.Path.Combine(runDir, "events.jsonl"));
            Game.Editor.Simulation.Reports.CashFlowReport.WriteCsv(ctx.Log, System.IO.Path.Combine(runDir, "cashflow.csv"));
            Game.Editor.Simulation.Reports.SummaryReport.Write(ctx.Log, System.IO.Path.Combine(runDir, "SUMMARY.md"), "Baseline", 42, 90);
            Game.Editor.Simulation.Reports.DailyTrajectoryReport.WriteCsv(ctx.Log, System.IO.Path.Combine(runDir, "daily_trajectory.csv"));
            UnityEngine.Debug.Log($"[Sim] Baseline dynamic 90d seed=42 → {runDir}");
        }

        [MenuItem("Tools/Simulation/Run Baseline (seed=48, days=30)")]
        public static void RunBaselineSeed48() => RunOne(new BaselinePolicy(), 48, 30);

        [MenuItem("Tools/Simulation/Run Baseline (seed=46, days=30)")]
        public static void RunBaselineSeed46() => RunOne(new BaselinePolicy(), 46, 30);

        [MenuItem("Tools/Simulation/Run AdaptiveSide (seed=48, days=30)")]
        public static void RunAdaptiveSeed48() => RunOne(new AdaptiveSidePolicy(), 48, 30);

        [MenuItem("Tools/Simulation/Run All Policies (seed=42, days=30)")]
        public static void RunAllPolicies()
        {
            IPlayerPolicy[] policies = {
                new NovicePolicy(),
                new BaselinePolicy(),
                new AggressivePolicy(),
                new MinMaxPolicy(),
                new FarmMaxPolicy(),
                new NoFarmPolicy(),
                new SideStuffedPolicy(),
            };
            string batchTag = $"compare_seed42_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, batchTag);
            Directory.CreateDirectory(batchDir);

            var results = new List<PolicyComparisonReport.PolicyResult>();
            foreach (var p in policies)
            {
                var ctx = SimContext.Build(seed: 42);
                ctx.Policy = p;
                new SimHarness(ctx).Run(days: 30);

                // 각 정책별 상세 리포트도 저장
                string policyDir = Path.Combine(batchDir, p.Name);
                WriteAllReports(ctx, policyDir, days: 30);

                results.Add(PolicyComparisonReport.Extract(ctx.Log, p.Name, 42));
            }

            string cmpPath = Path.Combine(batchDir, "COMPARISON.md");
            PolicyComparisonReport.Write(results, cmpPath);
            Debug.Log($"[Sim] Compared {policies.Length} policies → {batchDir}");
        }

        private static void RunOne(IPlayerPolicy policy, int seed, int days)
        {
            var ctx = SimContext.Build(seed);
            ctx.Policy = policy;
            new SimHarness(ctx).Run(days);

            string tag = $"{policy.Name}_seed{seed}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string runDir = Path.Combine(OutputDir, tag);
            WriteAllReports(ctx, runDir, days);

            int finalMoney = ctx.Stats.GetMoney();
            int day = ctx.State.phase.Day;
            string status = ctx.Bankrupt ? "BANKRUPT" : "SURVIVED";
            Debug.Log($"[Sim/{policy.Name}] {status} — Day={day}, Money={finalMoney}G, Events={ctx.Log.Count}\n  → {runDir}");
        }

        private static void WriteAllReports(SimContext ctx, string runDir, int days)
        {
            Directory.CreateDirectory(runDir);
            ctx.Log.Flush(Path.Combine(runDir, "events.jsonl"));
            CashFlowReport.WriteCsv(ctx.Log, Path.Combine(runDir, "cashflow.csv"));
            MenuRevenueReport.WriteCsv(ctx.Log, Path.Combine(runDir, "menu_revenue.csv"));
            InventoryReport.WriteCsv(ctx.Log, Path.Combine(runDir, "inventory.csv"));
            CustomerReport.WriteCsv(ctx.Log, Path.Combine(runDir, "customers.csv"));
            TimeoutCauseReport.WriteCsv(ctx.Log, Path.Combine(runDir, "timeout_causes.csv"));
            SummaryReport.Write(ctx.Log, Path.Combine(runDir, "SUMMARY.md"), ctx.Policy.Name, ctx.Seed, days);
            DailyTrajectoryReport.WriteCsv(ctx.Log, Path.Combine(runDir, "daily_trajectory.csv"));
        }
    }
}
