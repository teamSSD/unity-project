using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>일자·페이즈별 손님 서빙율 리포트.
    /// 컬럼: day, phase, served, timedOut, serveRate, revenue.</summary>
    public static class CustomerReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            var stats = new Dictionary<(int day, int phase), Agg>();
            foreach (var e in log.OfType<CustomerServedEvent>())
            {
                var a = Get(stats, (e.day, e.phase));
                a.Served++;
                a.Revenue += e.reward;
            }
            foreach (var e in log.OfType<CustomerTimedOutEvent>())
            {
                var a = Get(stats, (e.day, e.phase));
                a.TimedOut++;
            }

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using var w = new StreamWriter(filePath, false);
            w.WriteLine("day,phase,served,timedOut,serveRate,revenue");
            var keys = stats.Keys.OrderBy(k => k.day).ThenBy(k => k.phase);
            foreach (var k in keys)
            {
                var a = stats[k];
                int total = a.Served + a.TimedOut;
                float serveRate = total > 0 ? (float)a.Served / total : 0f;
                w.WriteLine($"{k.day},{k.phase},{a.Served},{a.TimedOut},{serveRate:F3},{a.Revenue}");
            }
        }

        private static Agg Get(Dictionary<(int, int), Agg> d, (int, int) key)
        {
            if (!d.TryGetValue(key, out var a)) { a = new Agg(); d[key] = a; }
            return a;
        }

        private class Agg
        {
            public int Served;
            public int TimedOut;
            public int Revenue;
        }
    }
}
