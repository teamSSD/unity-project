using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Simulation
{
    /// <summary>각 MAIN 음식의 원가 / 예상 판매가 / 마진율 진단.
    /// Baseline 정책의 EstimateMarginPerCraft가 실제 게임 pricing과 맞는지 검증.</summary>
    public static class MenuEconomicsAnalyzer
    {
        [MenuItem("Tools/Simulation/Analyze Menu Economics")]
        public static void Run()
        {
            var cfg = new SimGameConfig { unlockAllMenus = true };
            var ctx = SimContext.Build(42, cfg);

            var sb = new StringBuilder();
            sb.AppendLine("# Menu Economics — 각 MAIN 메뉴의 원가/마진/자본 요구량");
            sb.AppendLine();
            sb.AppendLine("모든 메뉴 unlock 상태. 서빙 accuracy 0.85 (Baseline 기본).");
            sb.AppendLine();
            sb.AppendLine("| id | 이름 | leafCost | est.Price (0.85) | est.Margin | margin% | 12k로 4서빙 원가 |");
            sb.AppendLine("|---|---|---|---|---|---|---|");

            var mains = ctx.FoodById.Values
                .Where(f => f?.type == FoodType.MAIN)
                .OrderBy(f => ctx.Recipes.EstimateCostBasis(f.id, ctx.IngredientPriceById))
                .ToList();

            foreach (var main in mains)
            {
                int cost = ctx.Recipes.EstimateCostBasis(main.id, ctx.IngredientPriceById);
                // EstimatePrice 공식 (CookingSimulator.EstimatePrice와 동일):
                //   price = cost × (0.85 + accuracy × 1.0) × 1.2
                float chainBonus = 1.2f;
                float scoreFactor = 0.85f + 0.85f * 1.0f; // = 1.70
                int estPrice = Mathf.RoundToInt(cost * scoreFactor * chainBonus);
                int estMargin = estPrice - cost;
                float marginPct = cost > 0 ? (float)estMargin / cost * 100f : 0f;
                int fourServeCost = cost * 4;

                sb.AppendLine($"| {main.id} | {main.ingredientName ?? main.id} | " +
                              $"{cost:N0}G | {estPrice:N0}G | {estMargin:N0}G | " +
                              $"{marginPct:F1}% | {fourServeCost:N0}G |");
            }

            sb.AppendLine();
            sb.AppendLine("## Baseline의 EstimateMarginPerCraft 수식 문제");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine("int estimatedPrice = (int)(cost * 1.5);  // ❌ 실제 판매가와 다름");
            sb.AppendLine("return estimatedPrice - cost;             // = 0.5 × cost");
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("실제 공식 = `cost × (0.85 + acc) × 1.2 = cost × 2.04` (acc=0.85 기준).");
            sb.AppendLine("즉 실 마진 = cost × 1.04 (104%). Baseline의 추정치 0.5 × cost의 2배.");
            sb.AppendLine();
            sb.AppendLine("**절대값 order → 원가 desc = 마진 desc.** 결과: 가장 비싼 메뉴가 top-pick.");
            sb.AppendLine("**의도된 마진율 기반 랭킹이 아님** (모든 메뉴 마진율 100% 근방으로 비슷).");
            sb.AppendLine();
            sb.AppendLine("## 자본 감당 가능 메뉴");
            sb.AppendLine();
            sb.AppendLine("초기 12k, mgmt 1k 유예 → 11k 예산. 3 메뉴 × 4 서빙 = 12 서빙 나눔.");
            sb.AppendLine("서빙당 원가 상한 ≈ 11k / 12 ≈ **900G**.");
            sb.AppendLine();

            var affordable = mains.Where(m =>
                ctx.Recipes.EstimateCostBasis(m.id, ctx.IngredientPriceById) <= 900).ToList();
            sb.AppendLine($"12k로 감당 가능한 메뉴: {affordable.Count}개 → " +
                          string.Join(", ", affordable.Select(m => m.id)));

            sb.AppendLine();
            sb.AppendLine("## 프로덕션 유저의 정보");
            sb.AppendLine();
            sb.AppendLine("실 게임 MenuCardController: 각 메뉴 카드에 원가/재료/툴 표시. 유저가 자산 대비 판단.");
            sb.AppendLine();

            string outPath = "tmp/simulation/runs/menu_economics.md";
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllText(outPath, sb.ToString());
            Debug.Log($"[MenuEconomics] → {outPath}");
        }
    }
}
