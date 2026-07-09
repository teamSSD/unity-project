using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>이벤트 로그에서 일자별 현금흐름 분해 CSV 파생.
    /// 컬럼: day, cook, delivery, purchase, upgrade, mgmtFee, net, endMoney.</summary>
    public static class CashFlowReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            var byDay = new Dictionary<int, Dictionary<string, int>>();

            foreach (var e in log.OfType<PhaseCashFlowEvent>())
            {
                if (!byDay.TryGetValue(e.day, out var cat))
                {
                    cat = new Dictionary<string, int>();
                    byDay[e.day] = cat;
                }
                cat.TryGetValue(e.category, out int cur);
                cat[e.category] = cur + e.amount;
            }

            var endMoneyByDay = new Dictionary<int, int>();
            foreach (var e in log.OfType<DayEndSnapshotEvent>())
                endMoneyByDay[e.day] = e.money;

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath, false);
            w.WriteLine("day,cook,delivery,purchase,upgrade,mgmtFee,net,endMoney");
            var days = new List<int>(byDay.Keys);
            days.Sort();
            foreach (var d in days)
            {
                var c = byDay[d];
                int cook = Get(c, "cook");
                int delivery = Get(c, "delivery");
                int purchase = Get(c, "purchase");
                int upgrade = Get(c, "upgrade");
                int mgmt = Get(c, "mgmtFee");
                int net = cook + delivery + purchase + upgrade + mgmt;
                int endM = endMoneyByDay.TryGetValue(d, out int m) ? m : 0;
                w.WriteLine($"{d},{cook},{delivery},{purchase},{upgrade},{mgmt},{net},{endM}");
            }
        }

        private static int Get(Dictionary<string, int> d, string k) =>
            d.TryGetValue(k, out int v) ? v : 0;
    }
}
