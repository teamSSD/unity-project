using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>재료별 통계 리포트. 구매·소비·만료·거절 이벤트 aggregation.
    /// 컬럼: itemId, boughtQty, boughtCost, consumedQty, consumedCost, expiredQty, expiredCost,
    ///        rejectedQty, wasteRate(=expired/bought), holdTimeAvgDays(대략).</summary>
    public static class InventoryReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            var stats = new Dictionary<string, Agg>();

            foreach (var e in log.OfType<IngredientPurchasedEvent>())
            {
                var a = Get(stats, e.itemId);
                a.BoughtQty += e.qty;
                a.BoughtCost += e.totalCost;
                // hold time 추정: 첫 구매 day 기록
                if (a.FirstBuyDay < 0) a.FirstBuyDay = e.day;
                a.LastBuyDay = e.day;
            }
            foreach (var e in log.OfType<IngredientConsumedEvent>())
            {
                var a = Get(stats, e.itemId);
                if (e.purpose == "cook") { a.ConsumedQty += e.qty; a.ConsumedCost += e.valueAtCost; }
                else if (e.purpose == "wasted") { a.WastedQty += e.qty; a.WastedCost += e.valueAtCost; }
                if (a.FirstConsumeDay < 0) a.FirstConsumeDay = e.day;
                a.LastConsumeDay = e.day;
            }
            foreach (var e in log.OfType<IngredientExpiredEvent>())
            {
                var a = Get(stats, e.itemId);
                a.ExpiredQty += e.qty;
                a.ExpiredCost += e.valueAtCost;
            }
            foreach (var e in log.OfType<StorageRejectedEvent>())
            {
                var a = Get(stats, e.itemId);
                a.RejectedQty += e.qty;
            }

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath, false);
            w.WriteLine("itemId,boughtQty,boughtCost,consumedQty,consumedCost,expiredQty,expiredCost," +
                        "rejectedQty,wasteRate,avgHoldDays");
            var ranked = stats.OrderByDescending(kv => kv.Value.BoughtCost);
            foreach (var kv in ranked)
            {
                var a = kv.Value;
                float wasteRate = a.BoughtCost > 0 ? (float)a.ExpiredCost / a.BoughtCost : 0f;
                int avgHold = a.FirstBuyDay >= 0 && a.LastConsumeDay >= 0
                    ? System.Math.Max(0, a.LastConsumeDay - a.FirstBuyDay) : 0;
                w.WriteLine($"{kv.Key},{a.BoughtQty},{a.BoughtCost},{a.ConsumedQty},{a.ConsumedCost}," +
                            $"{a.ExpiredQty},{a.ExpiredCost},{a.RejectedQty},{wasteRate:F3},{avgHold}");
            }
        }

        private static Agg Get(Dictionary<string, Agg> d, string key)
        {
            if (!d.TryGetValue(key, out var a)) { a = new Agg(); d[key] = a; }
            return a;
        }

        private class Agg
        {
            public int BoughtQty, BoughtCost;
            public int ConsumedQty, ConsumedCost;
            public int WastedQty, WastedCost;
            public int ExpiredQty, ExpiredCost;
            public int RejectedQty;
            public int FirstBuyDay = -1, LastBuyDay = -1;
            public int FirstConsumeDay = -1, LastConsumeDay = -1;
        }
    }
}
