using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Reports
{
    /// <summary>손님 timeout 원인 집계 리포트.
    ///
    /// 컬럼:
    /// - reason (INSUFFICIENT_BUY / MID_DAY_RUN_OUT / RECIPE_UNKNOWN)
    /// - count
    /// - pct (전체 timeout 대비 비율)
    /// - byMenu (요약: mainId → count)
    /// - byMissingIngredient (요약: ingredientId → count)
    ///
    /// 튜닝 방향 자동 지목:
    /// - INSUFFICIENT_BUY 많음 → 정책 buy 계산 상향 or 예산 부족 (upgrade/reserve 줄이기)
    /// - MID_DAY_RUN_OUT 많음 → 정상 (재고 관리 스트레스) / 살짝 더 사면 개선
    /// - RECIPE_UNKNOWN 발생 → 게임 데이터 이슈 (레시피/카탈로그)</summary>
    public static class TimeoutCauseReport
    {
        public static void WriteCsv(SimEventLog log, string filePath)
        {
            var timeouts = log.OfType<CustomerTimedOutEvent>().ToList();
            var byReason = new Dictionary<string, int>();
            var byMenu = new Dictionary<string, Dictionary<string, int>>();       // reason → menu → count
            var byIngredient = new Dictionary<string, Dictionary<string, int>>(); // reason → ingredient → count

            foreach (var t in timeouts)
            {
                var reason = string.IsNullOrEmpty(t.reason) ? "OTHER" : t.reason;
                byReason.TryGetValue(reason, out int c);
                byReason[reason] = c + 1;

                if (!byMenu.TryGetValue(reason, out var mm)) { mm = new Dictionary<string, int>(); byMenu[reason] = mm; }
                mm.TryGetValue(t.menuMainId ?? "?", out int mc);
                mm[t.menuMainId ?? "?"] = mc + 1;

                if (!byIngredient.TryGetValue(reason, out var ii)) { ii = new Dictionary<string, int>(); byIngredient[reason] = ii; }
                string ing = string.IsNullOrEmpty(t.missingIngredientId) ? "?" : t.missingIngredientId;
                ii.TryGetValue(ing, out int ic);
                ii[ing] = ic + 1;
            }

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            int total = timeouts.Count;
            using var w = new StreamWriter(filePath, false);
            w.WriteLine("reason,count,pct,topMenu(count),topIngredient(count)");
            foreach (var kv in byReason.OrderByDescending(x => x.Value))
            {
                float pct = total > 0 ? (float)kv.Value / total * 100f : 0f;
                string topMenu = byMenu.TryGetValue(kv.Key, out var mm)
                    ? mm.OrderByDescending(x => x.Value).Take(3)
                        .Select(x => $"{x.Key}({x.Value})").Aggregate((a, b) => a + " " + b)
                    : "";
                string topIng = byIngredient.TryGetValue(kv.Key, out var ii)
                    ? ii.OrderByDescending(x => x.Value).Take(3)
                        .Select(x => $"{x.Key}({x.Value})").Aggregate((a, b) => a + " " + b)
                    : "";
                w.WriteLine($"{kv.Key},{kv.Value},{pct:F1}%,{topMenu},{topIng}");
            }
        }
    }
}
