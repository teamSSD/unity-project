using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>요리별 매출 랭킹. CustomerServedEvent 순회.
    /// 컬럼: mainId, servedCount, totalReward, avgReward, avgAccuracy, avgSideCount.</summary>
    public static class MenuRevenueReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            var stats = new Dictionary<string, Agg>();
            foreach (var e in log.OfType<CustomerServedEvent>())
            {
                if (!stats.TryGetValue(e.mainId, out var a))
                {
                    a = new Agg();
                    stats[e.mainId] = a;
                }
                a.Count++;
                a.TotalReward += e.reward;
                a.AccuracySum += e.accuracy;
                a.SideCountSum += (e.sideIds?.Length ?? 0);
            }

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath, false);
            w.WriteLine("mainId,servedCount,totalReward,avgReward,avgAccuracy,avgSideCount");
            var ranked = stats.OrderByDescending(kv => kv.Value.TotalReward);
            foreach (var kv in ranked)
            {
                var a = kv.Value;
                int avgReward = a.Count > 0 ? a.TotalReward / a.Count : 0;
                float avgAcc = a.Count > 0 ? a.AccuracySum / a.Count : 0f;
                float avgSides = a.Count > 0 ? (float)a.SideCountSum / a.Count : 0f;
                w.WriteLine($"{kv.Key},{a.Count},{a.TotalReward},{avgReward},{avgAcc:F3},{avgSides:F2}");
            }
        }

        private class Agg
        {
            public int Count;
            public int TotalReward;
            public float AccuracySum;
            public int SideCountSum;
        }
    }
}
