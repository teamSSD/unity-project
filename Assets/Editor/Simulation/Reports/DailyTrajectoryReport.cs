using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>Day별 종합 KPI 리포트 (표준 플레이 궤적 시각화용).
    /// 컬럼: day, endMoney, cookRev, purchase, upgrade, mgmt, net, served, timedOut, serveRate,
    ///       upgradesAcquired (당일 획득 upgrade list), inventoryUnique, farmHarvests.
    /// </summary>
    public static class DailyTrajectoryReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            // Cash flow by day
            var cashByDay = new Dictionary<int, Dictionary<string, int>>();
            foreach (var e in log.OfType<PhaseCashFlowEvent>())
            {
                if (!cashByDay.TryGetValue(e.day, out var cat))
                {
                    cat = new Dictionary<string, int>();
                    cashByDay[e.day] = cat;
                }
                cat.TryGetValue(e.category, out int cur);
                cat[e.category] = cur + e.amount;
            }

            // End-of-day snapshots
            var snapshotByDay = new Dictionary<int, DayEndSnapshotEvent>();
            foreach (var e in log.OfType<DayEndSnapshotEvent>()) snapshotByDay[e.day] = e;

            // Served/timedOut by day
            var servedByDay = new Dictionary<int, int>();
            var timedOutByDay = new Dictionary<int, int>();
            foreach (var e in log.OfType<CustomerServedEvent>())
            {
                servedByDay.TryGetValue(e.day, out int cur);
                servedByDay[e.day] = cur + 1;
            }
            foreach (var e in log.OfType<CustomerTimedOutEvent>())
            {
                timedOutByDay.TryGetValue(e.day, out int cur);
                timedOutByDay[e.day] = cur + 1;
            }

            // Upgrades by day
            var upgradesByDay = new Dictionary<int, List<string>>();
            foreach (var e in log.OfType<UpgradeMadeEvent>())
            {
                if (!upgradesByDay.TryGetValue(e.day, out var list))
                {
                    list = new List<string>();
                    upgradesByDay[e.day] = list;
                }
                list.Add($"{e.category}.{e.trackId}(L{e.newLevel})");
            }

            // Farm harvests by day
            var farmHarvestsByDay = new Dictionary<int, int>();
            foreach (var e in log.OfType<FarmEvent>())
            {
                if (e.action != "harvest") continue;
                farmHarvestsByDay.TryGetValue(e.day, out int cur);
                farmHarvestsByDay[e.day] = cur + e.yieldQty;
            }

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath, false);
            w.WriteLine("day,endMoney,cookRev,purchase,upgrade,mgmt,net,served,timedOut,serveRate,upgradesAcquired,invUnique,farmHarvest");

            var allDays = new HashSet<int>(cashByDay.Keys);
            foreach (var d in snapshotByDay.Keys) allDays.Add(d);
            var days = allDays.OrderBy(x => x).ToList();

            foreach (var d in days)
            {
                cashByDay.TryGetValue(d, out var c);
                int cook = Get(c, "cook");
                int purchase = Get(c, "purchase");
                int upgrade = Get(c, "upgrade");
                int mgmt = Get(c, "mgmtFee");
                int net = cook + purchase + upgrade + mgmt;

                int endM = snapshotByDay.TryGetValue(d, out var snap) ? snap.money : 0;
                int served = servedByDay.TryGetValue(d, out int s) ? s : 0;
                int timedOut = timedOutByDay.TryGetValue(d, out int t) ? t : 0;
                float serveRate = (served + timedOut) > 0
                    ? (float)served / (served + timedOut) * 100f : 0f;

                string upList = upgradesByDay.TryGetValue(d, out var ups)
                    ? string.Join(";", ups) : "";
                int invUnique = snap?.inventoryByFood?.Count(kv => kv.Value > 0) ?? 0;
                int farmH = farmHarvestsByDay.TryGetValue(d, out int fh) ? fh : 0;

                w.WriteLine($"{d},{endM},{cook},{purchase},{upgrade},{mgmt},{net}," +
                            $"{served},{timedOut},{serveRate:F1},{upList},{invUnique},{farmH}");
            }
        }

        private static int Get(Dictionary<string, int> d, string k) =>
            d != null && d.TryGetValue(k, out int v) ? v : 0;
    }
}
