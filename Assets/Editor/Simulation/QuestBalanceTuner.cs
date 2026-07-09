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
    /// <summary>Quest reward 파라미터 튜닝 sweep.
    /// 목표: Day 15에 6 메뉴 다 unlock + Day 60~90 마지막 upgrade 완료.
    /// 파라미터: 4개 quest reward (초기 자산 30k 기준으로 sweep).
    /// Upgrade cost는 ×1.0 (CSV 원본, 사용자 요청).</summary>
    public static class QuestBalanceTuner
    {
        private const string OutputDir = "tmp/simulation/runs";

        [MenuItem("Tools/Simulation/Quest Reward Tuning Sweep")]
        public static void RunSweep()
        {
            // 프로덕션 quest 구조 반영. 3 quest 그룹으로 4 mains unlock.
            // night_market은 I039+I049 동시 unlock — 임계치 하나로 처리.
            var thresholds = new List<(int, string)>
            {
                (20_000, "I034"),   // nimo_solo
                (40_000, "I039"),   // night_market first main
                (40_000, "I049"),   // night_market second main (same threshold)
                (80_000, "I053"),   // lede_solo
            };

            // Delivery 보상 (MenuValidator fix 반영) — quest 완료 시 자연 매출.
            // Q1 nimo_solo (I034 단품): 11k. Q2 night_market (I039 단품): 17k. Q3 lede_solo (I053 단품): 18k.
            // I049는 night_market에 포함이라 reward 0 (unlock만).
            var lumpTiers = new List<(string label, Dictionary<string, int> lumpsum)>
            {
                // Natural: 실 배달 매출 근사
                ("Nat", new() { {"I034",11000}, {"I039",17000}, {"I049",0}, {"I053",18000} }),
                // Nat + margin (더 후한 quest 보상 시나리오)
                ("Gen", new() { {"I034",25000}, {"I039",35000}, {"I049",0}, {"I053",40000} }),
                // Nat + big boost (예: quest 3회 완료 = 100k+ total)
                ("Big", new() { {"I034",40000}, {"I039",60000}, {"I049",0}, {"I053",70000} }),
            };
            // 초기 부담 없음, L3-L4 (endgame gate)만 상향.
            var lateMults = new[] { 1.0f, 2.0f, 3.0f };
            var rewardSets = new List<(string label, Dictionary<string, int> rewards, float lateMult)>();
            foreach (var (lbl, lump) in lumpTiers)
                foreach (var m in lateMults)
                    rewardSets.Add(($"{lbl}_Late{m:F1}", lump, m));

            int[] seeds = Enumerable.Range(42, 10).ToArray();
            int days = 90;

            string tag = $"quest_tune_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            string batchDir = Path.Combine(OutputDir, tag);
            Directory.CreateDirectory(batchDir);

            var results = new List<Row>();

            foreach (var (label, rewards, lateMult) in rewardSets)
            {
                foreach (var seed in seeds)
                {
                    var cfg = new SimGameConfig
                    {
                        AssetTriggeredUnlock = thresholds,
                        QuestUnlockReward = rewards,
                        upgradeCostMultiplier = 1.0f,
                        lateUpgradeCostMultiplier = lateMult,
                    };
                    var ctx = SimContext.Build(seed, cfg);
                    ctx.Policy = new BaselinePolicy();
                    new SimHarness(ctx).Run(days);

                    // Day 15 unlock 카운트
                    int day15UnlockCount = 2 + CountUnlockAtDay(ctx.Log, 15);
                    // Day별 upgrade 마지막 완료
                    int lastUpgradeDay = LastUpgradeDay(ctx.Log);
                    // 총 upgrade 카운트 (Baseline: Tool 10 + Storage 9 + Farm 12 = 31 총)
                    int upgradeCount = ctx.Log.OfType<UpgradeMadeEvent>().Count();

                    results.Add(new Row
                    {
                        Label = label,
                        Seed = seed,
                        Bankrupt = ctx.Bankrupt,
                        EndMoney = ctx.Stats.GetMoney(),
                        Day15UnlockCount = day15UnlockCount,
                        LastUpgradeDay = lastUpgradeDay,
                        UpgradeCount = upgradeCount,
                    });
                }
            }

            WriteReport(batchDir, rewardSets, results);
            Debug.Log($"[QuestTune] Done → {batchDir}");
        }

        private static int CountUnlockAtDay(SimEventLog log, int targetDay)
        {
            // DayEndSnapshot의 upgradeLevels 대신 자산 궤적으로 threshold 통과 계산.
            // AssetTriggeredUnlock 임계치: I049=25k, I034=40k, I039=60k, I053=80k.
            var moneyByDay = log.OfType<DayEndSnapshotEvent>()
                .Where(e => e.day <= targetDay)
                .ToDictionary(e => e.day, e => e.money);
            int maxMoney = moneyByDay.Values.DefaultIfEmpty(0).Max();
            int[] thresholds = { 25_000, 40_000, 60_000, 80_000 };
            return thresholds.Count(t => maxMoney >= t);
        }

        private static int LastUpgradeDay(SimEventLog log)
        {
            var upgrades = log.OfType<UpgradeMadeEvent>().ToList();
            return upgrades.Count > 0 ? upgrades.Max(u => u.day) : -1;
        }

        private static void WriteReport(string dir,
            List<(string label, Dictionary<string, int> rewards, float lateMult)> rewardSets,
            List<Row> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# Quest Reward Tuning Sweep — 목표: Day 15 all unlock + Day 60~90 last upgrade");
            sb.AppendLine();
            sb.AppendLine("Baseline 정책, 10 seeds × 90일. Upgrade cost ×1.0 (CSV 원본).");
            sb.AppendLine();
            sb.AppendLine("Unlock 임계치: I049=25k, I034=40k, I039=60k, I053=80k.");
            sb.AppendLine();
            sb.AppendLine("## 조합별 결과");
            sb.AppendLine();
            sb.AppendLine("| Combo | Reward I049/I034/I039/I053 | Survival | Day15 unlock | Last upgrade day | Median endMoney |");
            sb.AppendLine("|---|---|---|---|---|---|");

            foreach (var (label, rewards, lateMult) in rewardSets)
            {
                var runs = results.Where(r => r.Label == label).ToList();
                _ = lateMult;
                float survival = (float)runs.Count(r => !r.Bankrupt) / runs.Count * 100f;
                float avgDay15 = (float)runs.Average(r => r.Day15UnlockCount);
                float avgLastUpg = runs.Count(r => r.LastUpgradeDay > 0) > 0
                    ? (float)runs.Where(r => r.LastUpgradeDay > 0).Average(r => r.LastUpgradeDay)
                    : -1f;
                var monies = runs.Select(r => r.EndMoney).OrderBy(x => x).ToList();
                int median = monies[monies.Count / 2];

                string rewardStr = rewards.Count == 0 ? "none"
                    : $"{Get(rewards, "I049")}/{Get(rewards, "I034")}/{Get(rewards, "I039")}/{Get(rewards, "I053")}";

                sb.AppendLine($"| {label} | {rewardStr} | {survival:F0}% | {avgDay15:F1}/6 | {avgLastUpg:F0} | {median:N0}G |");
            }

            sb.AppendLine();
            sb.AppendLine("## 해석");
            sb.AppendLine("- Day15 unlock 6/6이면 목표 달성");
            sb.AppendLine("- Last upgrade day 60~90이 목표 (너무 이르면 뒷심 부족, 너무 늦으면 진행 불가)");
            sb.AppendLine("- Survival 90%+ 유지");
            sb.AppendLine();

            // 가장 목표 근접한 combo 추천
            var scored = rewardSets.Select(rs =>
            {
                var runs = results.Where(r => r.Label == rs.label).ToList();
                float survival = (float)runs.Count(r => !r.Bankrupt) / runs.Count * 100f;
                float avgDay15 = (float)runs.Average(r => r.Day15UnlockCount);
                float avgLastUpg = runs.Count(r => r.LastUpgradeDay > 0) > 0
                    ? (float)runs.Where(r => r.LastUpgradeDay > 0).Average(r => r.LastUpgradeDay)
                    : 0f;
                // Score: unlock 근접도 (6까지) + upgrade 60~90 근접도 + survival 페널티
                float unlockScore = Mathf.Abs(6f - avgDay15) * 100f;
                float upgScore = avgLastUpg < 60 ? (60 - avgLastUpg) * 5f
                              : avgLastUpg > 90 ? (avgLastUpg - 90) * 5f : 0f;
                float survivalPenalty = survival < 80 ? (80 - survival) * 10f : 0f;
                return (label: rs.label, score: unlockScore + upgScore + survivalPenalty);
            }).OrderBy(x => x.score).ToList();

            sb.AppendLine("## 목표 근접 순 (score 낮을수록 좋음)");
            sb.AppendLine();
            sb.AppendLine("| Rank | Combo | Score |");
            sb.AppendLine("|---|---|---|");
            for (int i = 0; i < scored.Count; i++)
                sb.AppendLine($"| {i + 1} | {scored[i].label} | {scored[i].score:F1} |");

            File.WriteAllText(Path.Combine(dir, "QUEST_TUNING.md"), sb.ToString());
        }

        private static int Get(Dictionary<string, int> d, string k) => d.TryGetValue(k, out int v) ? v : 0;

        private class Row
        {
            public string Label;
            public int Seed, EndMoney, Day15UnlockCount, LastUpgradeDay, UpgradeCount;
            public bool Bankrupt;
        }
    }
}
