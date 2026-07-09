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
    /// <summary>각 정책의 페이즈별 액션 분포 (Work/Shopping/Rest %).
    /// Afternoon/Evening/Night 각각에 대해 정책이 얼마 자주 어떤 액션 선택하는지.
    /// Preparation/Morning은 자동/강제라 제외.</summary>
    public static class PolicyActionDistribution
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Policy Action Distribution")]
        public static void Run()
        {
            IPlayerPolicy[] policies = {
                new BaselinePolicy(),
                new AdaptiveSidePolicy(),
                new NoFarmPolicy(),
                new NoUpgradePolicy(),
                new MenuDiversifyPolicy(),
                new FarmAwarePolicy(),
                new AdaptiveFarmSidePolicy(),
                new NovicePolicy(),
                new CautiousPolicy(),
                new AggressivePolicy(),
                new MinMaxPolicy(),
                new FarmMaxPolicy(),
                new SideStuffedPolicy(),
            };

            int[] seeds = Enumerable.Range(42, 15).ToArray();
            int days = 90;

            string tag = $"phase_actions_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            var results = new List<Row>();

            foreach (var policy in policies)
            {
                var perPhaseCounts = new Dictionary<PhaseType, Dictionary<string, int>>
                {
                    { PhaseType.Afternoon, new Dictionary<string, int>() },
                    { PhaseType.Evening,   new Dictionary<string, int>() },
                    { PhaseType.Night,     new Dictionary<string, int>() },
                };

                foreach (var seed in seeds)
                {
                    var ctx = SimContext.Build(seed);
                    ctx.Policy = policy;
                    new SimHarness(ctx).Run(days);

                    foreach (var e in ctx.Log.OfType<PhaseActionEvent>())
                    {
                        var ph = (PhaseType)e.phase;
                        if (!perPhaseCounts.ContainsKey(ph)) continue;
                        perPhaseCounts[ph].TryGetValue(e.action, out int cur);
                        perPhaseCounts[ph][e.action] = cur + 1;
                    }
                }

                results.Add(new Row
                {
                    Policy = policy.Name,
                    AfternoonCounts = perPhaseCounts[PhaseType.Afternoon],
                    EveningCounts = perPhaseCounts[PhaseType.Evening],
                    NightCounts = perPhaseCounts[PhaseType.Night],
                });
            }

            WriteReport(batchDir, results);
            Debug.Log($"[PhaseActions] Done → {batchDir}");
        }

        private static void WriteReport(string dir, List<Row> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Policy Action Distribution — 페이즈별 액션 선택률");
            sb.AppendLine();
            sb.AppendLine("15 seeds × 90일 (파산 정책은 짧게 종료). Afternoon/Evening/Night 3 phase의 정책 결정 액션 (%).");
            sb.AppendLine();
            sb.AppendLine("| Policy | Afternoon (W/S/R) | Evening (W/S/R) | Night (W/S/R) |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var r in results)
            {
                string a = FormatDist(r.AfternoonCounts);
                string e = FormatDist(r.EveningCounts);
                string n = FormatDist(r.NightCounts);
                sb.AppendLine($"| {r.Policy} | {a} | {e} | {n} |");
            }

            sb.AppendLine();
            sb.AppendLine("## 해석");
            sb.AppendLine("- W = Work (요리), S = Shopping (재료 매수), R = Rest (휴식)");
            sb.AppendLine("- 대부분 정책이 Shopping 우세 (Baseline은 100% Shopping)");
            sb.AppendLine("- Novice/Cautious 등은 다른 액션 조합 가능성");

            File.WriteAllText(Path.Combine(dir, "PHASE_ACTIONS.md"), sb.ToString());

            using var w = new StreamWriter(Path.Combine(dir, "phase_actions.csv"), false);
            w.WriteLine("policy,phase,Work,Shopping,Rest,total");
            foreach (var r in results)
            {
                WritePhase(w, r.Policy, "Afternoon", r.AfternoonCounts);
                WritePhase(w, r.Policy, "Evening", r.EveningCounts);
                WritePhase(w, r.Policy, "Night", r.NightCounts);
            }
        }

        private static string FormatDist(Dictionary<string, int> counts)
        {
            int total = counts.Values.Sum();
            if (total == 0) return "N/A";
            float wPct = 100f * Get(counts, "Work") / total;
            float sPct = 100f * Get(counts, "Shopping") / total;
            float rPct = 100f * Get(counts, "Rest") / total;
            return $"{wPct:F0}/{sPct:F0}/{rPct:F0}";
        }

        private static int Get(Dictionary<string, int> d, string k) => d.TryGetValue(k, out int v) ? v : 0;

        private static void WritePhase(StreamWriter w, string policy, string phase, Dictionary<string, int> counts)
        {
            int work = Get(counts, "Work"), shop = Get(counts, "Shopping"), rest = Get(counts, "Rest");
            w.WriteLine($"{policy},{phase},{work},{shop},{rest},{work + shop + rest}");
        }

        private class Row
        {
            public string Policy;
            public Dictionary<string, int> AfternoonCounts;
            public Dictionary<string, int> EveningCounts;
            public Dictionary<string, int> NightCounts;
        }
    }
}
