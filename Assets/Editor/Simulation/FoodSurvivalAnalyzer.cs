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
    /// <summary>Baseline 정책 seed 30개 × 90일 → 각 음식별 서빙 카운트 + 생존율 상관.
    /// 결과: food_survival.csv (음식 × seed matrix) + FOOD_SURVIVAL.md 요약.</summary>
    public static class FoodSurvivalAnalyzer
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Analyze Food Survival Correlation")]
        public static void RunAnalysis() => Run(mode: "default");

        [MenuItem("Tools/Simulation/Analyze Food Survival (all menus unlocked)")]
        public static void RunAnalysisUnlockAll() => Run(mode: "allunlocked");

        [MenuItem("Tools/Simulation/Analyze Food Survival (progressive unlock)")]
        public static void RunAnalysisProgressive() => Run(mode: "progressive");

        [MenuItem("Tools/Simulation/Analyze Food Survival (dynamic unlock)")]
        public static void RunAnalysisDynamic() => Run(mode: "dynamic");

        private static void Run(string mode)
        {
            int[] seeds = Enumerable.Range(42, 30).ToArray();
            int days = 90;

            // 정책들 — MenuDiversify는 랜덤 3픽이라 all-unlocked 시 다양성 실측 가능
            IPlayerPolicy[] MakePolicies() => new IPlayerPolicy[] {
                new BaselinePolicy(),
                new MenuDiversifyPolicy(),
                new AdaptiveSidePolicy(),
                new FarmAwarePolicy(),
                new AdaptiveFarmSidePolicy(),
            };

            string tag = $"food_survival_{mode}_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            var perPolicy = new Dictionary<string, List<RunData>>();

            foreach (var seed in seeds)
            {
                foreach (var policy in MakePolicies())
                {
                    var cfg = new SimGameConfig
                    {
                        unlockAllMenus = mode == "allunlocked",
                        ProgressiveUnlock = mode == "progressive"
                            ? new System.Collections.Generic.List<(int, string)>
                            {
                                (10, "I049"), (20, "I034"), (30, "I039"), (45, "I053")
                            }
                            : new System.Collections.Generic.List<(int, string)>(),
                        AssetTriggeredUnlock = mode == "dynamic"
                            ? new System.Collections.Generic.List<(int, string)>
                            {
                                // 임계치 = 새 메뉴 unlock 후 3 메뉴 × 4 서빙 × 원가 × 3일 버퍼
                                (160_000, "I049"),  // 스테이크: 3 메뉴 [I060+I044+I049] daily 54k → 3일 162k
                                (160_000, "I034"),  // 새벽국: 4 메뉴 top-3 daily 65k → 200k. 4개 unlock 후에도 안전
                                (230_000, "I039"),  // 구룡면: 5 메뉴 top-3 daily 76k → 3일 228k
                                (280_000, "I053"),  // 삼각밥: 6 메뉴 top-3 daily 92k → 3일 276k
                            }
                            : new System.Collections.Generic.List<(int, string)>(),
                    };
                    var ctx = SimContext.Build(seed, cfg);
                    ctx.Policy = policy;
                    new SimHarness(ctx).Run(days);

                    var counts = new Dictionary<string, int>();
                    foreach (var e in ctx.Log.OfType<CustomerServedEvent>())
                    {
                        counts.TryGetValue(e.mainId, out int cur);
                        counts[e.mainId] = cur + 1;
                    }

                    if (!perPolicy.TryGetValue(policy.Name, out var list))
                    {
                        list = new List<RunData>();
                        perPolicy[policy.Name] = list;
                    }
                    list.Add(new RunData
                    {
                        seed = seed,
                        bankrupt = ctx.Bankrupt,
                        endMoney = ctx.Stats.GetMoney(),
                        servedByFood = counts,
                    });
                }
            }

            WriteReports(batchDir, perPolicy);
            Debug.Log($"[FoodSurvival] Done → {batchDir}");
        }

        private static void WriteReports(string dir, Dictionary<string, List<RunData>> perPolicy)
        {
            // Aggregate: for each policy, average food serve count for SURVIVED vs BANKRUPT
            var sb = new StringBuilder();
            sb.AppendLine("# Food Survival Correlation");
            sb.AppendLine();
            sb.AppendLine($"30 seeds × 90 days × {perPolicy.Count} policies.");
            sb.AppendLine();

            // 모든 정책에 걸친 총 서빙 카운트 (음식별) — Baseline 기준
            if (perPolicy.TryGetValue("Baseline", out var baselineRuns))
            {
                sb.AppendLine("## Baseline 정책: 음식별 총 서빙 (30 seeds × 90일)");
                sb.AppendLine();
                sb.AppendLine("| mainId | totalServed | avgPerSeed | survivedAvg | bankruptAvg |");
                sb.AppendLine("|---|---|---|---|---|");

                var allFoods = baselineRuns.SelectMany(r => r.servedByFood.Keys).Distinct().ToList();
                foreach (var foodId in allFoods.OrderByDescending(f =>
                    baselineRuns.Sum(r => r.servedByFood.TryGetValue(f, out int c) ? c : 0)))
                {
                    int total = baselineRuns.Sum(r => r.servedByFood.TryGetValue(foodId, out int c) ? c : 0);
                    float avgAll = (float)total / baselineRuns.Count;
                    var survived = baselineRuns.Where(r => !r.bankrupt).ToList();
                    var bankrupt = baselineRuns.Where(r => r.bankrupt).ToList();
                    float avgSurvived = survived.Count > 0
                        ? (float)survived.Sum(r => r.servedByFood.TryGetValue(foodId, out int c) ? c : 0) / survived.Count
                        : 0f;
                    float avgBankrupt = bankrupt.Count > 0
                        ? (float)bankrupt.Sum(r => r.servedByFood.TryGetValue(foodId, out int c) ? c : 0) / bankrupt.Count
                        : 0f;
                    sb.AppendLine($"| {foodId} | {total} | {avgAll:F1} | {avgSurvived:F1} | {avgBankrupt:F1} |");
                }
                sb.AppendLine();
            }

            // 정책별 요약 (survival + avg money)
            sb.AppendLine("## 정책별 요약");
            sb.AppendLine();
            sb.AppendLine("| Policy | Survival | Avg servings/seed | Top menu |");
            sb.AppendLine("|---|---|---|---|");
            foreach (var kv in perPolicy)
            {
                float survival = (float)kv.Value.Count(r => !r.bankrupt) / kv.Value.Count * 100f;
                float avgServings = (float)kv.Value.Sum(r => r.servedByFood.Sum(x => x.Value)) / kv.Value.Count;
                var topFood = kv.Value
                    .SelectMany(r => r.servedByFood)
                    .GroupBy(x => x.Key)
                    .OrderByDescending(g => g.Sum(x => x.Value))
                    .FirstOrDefault();
                string top = topFood != null ? $"{topFood.Key} ({topFood.Sum(x => x.Value)})" : "-";
                sb.AppendLine($"| {kv.Key} | {survival:F0}% | {avgServings:F0} | {top} |");
            }

            File.WriteAllText(Path.Combine(dir, "FOOD_SURVIVAL.md"), sb.ToString());

            // CSV: seed × food matrix (Baseline only)
            if (perPolicy.TryGetValue("Baseline", out var runs))
            {
                var allFoods = runs.SelectMany(r => r.servedByFood.Keys).Distinct().OrderBy(x => x).ToList();
                using var w = new StreamWriter(Path.Combine(dir, "baseline_food_matrix.csv"), false);
                w.WriteLine("seed,bankrupt,endMoney," + string.Join(",", allFoods));
                foreach (var run in runs.OrderBy(r => r.seed))
                {
                    var row = new List<string>
                    {
                        run.seed.ToString(),
                        run.bankrupt ? "1" : "0",
                        run.endMoney.ToString(),
                    };
                    foreach (var f in allFoods)
                        row.Add(run.servedByFood.TryGetValue(f, out int c) ? c.ToString() : "0");
                    w.WriteLine(string.Join(",", row));
                }
            }
        }

        private class RunData
        {
            public int seed;
            public bool bankrupt;
            public int endMoney;
            public Dictionary<string, int> servedByFood;
        }
    }
}
