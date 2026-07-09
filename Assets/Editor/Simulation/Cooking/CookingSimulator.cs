using System.Collections.Generic;
using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation.Cooking
{
    /// <summary>Cooking 페이즈(Morning/Afternoon/Evening) 시뮬레이션.
    /// 페이즈당 손님 K명 스폰 → 각 손님이 무작위 메뉴 주문 → 정책 서빙 결정 → 요리 → MenuValidator 보상.
    ///
    /// 실시간(Time.deltaTime) 및 미니게임 세부 로직은 추상화:
    /// - 손님 수: 페이즈별 상수 + 소량 RNG variance (실측 프로덕션 값 참조)
    /// - 미니게임 점수: policy.CookingAccuracy 파라미터
    /// - 요리 가격: 원가 × (0.85 + accuracy × 1.0) × chainBonus 근사 공식</summary>
    public class CookingSimulator
    {
        private readonly SimContext _ctx;

        public CookingSimulator(SimContext ctx) { _ctx = ctx; }

        /// <summary>페이즈별 실측 근사 손님 수. scout Morning 스폰율 debug 상수 회피해 조정.</summary>
        // 페이즈별 실측 근사 손님 수 = phase real duration / spawn interval.
        // 실제 phase 150 real sec 기준.
        // Morning 35s → 4명, Afternoon 30s → 5명, Evening 22s → 7명, Night 15s → 10명 (배달 우선이지만 sim은 요리만 계산)
        // Tool 업그레이드 반영: durationMultiplier 감소 → 조리 시간 단축 → 페이즈당 실 서빙 가능수 증가.
        // throughputBoost = 1 / avgDurationMult (Tool 5종 평균).
        private int BaseCountFor(PhaseType phase) => phase switch
        {
            PhaseType.Morning   => 4,
            PhaseType.Afternoon => 5,
            PhaseType.Evening   => 7,
            PhaseType.Night     => 3,
            _ => 0,
        };

        private int CustomerCountFor(PhaseType phase)
        {
            int baseCount = BaseCountFor(phase);
            if (baseCount == 0) return 0;
            return UnityEngine.Mathf.RoundToInt(baseCount * ToolThroughputBoost());
        }

        /// <summary>Tool 5종 durationMultiplier 평균의 역수.
        /// 모두 L0 = 1.00, 모두 L2 = 1/0.60 ≈ 1.67. 조리 스루풋 배율.</summary>
        private float ToolThroughputBoost()
        {
            string[] tools = { "T001", "T002", "T003", "T004", "T005" };
            float sumMult = 0f;
            int count = 0;
            foreach (var t in tools)
            {
                var d = _ctx.ToolUpgrade.GetCurrentData(t);
                float mult = d?.durationMultiplier ?? 1.0f;
                if (mult <= 0f) mult = 1.0f;
                sumMult += mult;
                count++;
            }
            if (count == 0) return 1.0f;
            float avg = sumMult / count;
            return 1.0f / avg;
        }

        // 페이즈 단위 서빙 카운트 — mid-day run-out 판정용
        private System.Collections.Generic.Dictionary<string, int> _phaseServedCount;

        public void SimulatePhase(PhaseType phase, string[] selectedMainIds)
        {
            if (selectedMainIds == null || selectedMainIds.Length == 0) return;

            _phaseServedCount = new System.Collections.Generic.Dictionary<string, int>();

            int count = CustomerCountFor(phase);
            for (int i = 0; i < count; i++)
            {
                // 손님이 3 슬롯 중 무작위 픽 (Variable RNG)
                int idx = GameRandom.Range(GameRandom.Variable, 0, selectedMainIds.Length);
                string mainId = selectedMainIds[idx];
                ServeOneCustomer(phase, mainId);
            }
        }

        private void ServeOneCustomer(PhaseType phase, string mainId)
        {
            var policy = _ctx.Policy;
            var serving = policy.DecideServing(_ctx, mainId);

            // 재료 소비 가능 여부: main + sides 모두 재고 충분해야
            var allRequiredLeaves = MergedLeafIngredients(mainId, serving.sideFoodIds);
            var missing = FindMissing(allRequiredLeaves);
            if (missing != null)
            {
                _ctx.Log.Add(new CustomerTimedOutEvent
                {
                    day = _ctx.State.phase.Day,
                    phase = (int)phase,
                    menuMainId = mainId,
                    reason = ClassifyReason(mainId, allRequiredLeaves),
                    missingIngredientId = missing,
                });
                return;
            }

            // 재고 소비
            foreach (var kv in allRequiredLeaves)
            {
                if (!_ctx.FoodById.TryGetValue(kv.Key, out var food) || food == null) continue;
                _ctx.Inventory.ConsumeFood(food, kv.Value);
                _ctx.Log.Add(new IngredientConsumedEvent
                {
                    day = _ctx.State.phase.Day,
                    phase = (int)phase,
                    itemId = kv.Key,
                    qty = kv.Value,
                    purpose = "cook",
                    valueAtCost = LookupPrice(kv.Key) * kv.Value,
                });
            }

            // 가격 추정 → FoodSchema 준비 → MenuValidator 보상
            var mainFood = _ctx.FoodById[mainId];
            int mainPrice = EstimatePrice(mainId, serving.accuracy);
            var mainSchema = new FoodSchema(mainFood, mainPrice);

            var sideSchemas = new List<FoodSchema>();
            var sideIds = serving.sideFoodIds ?? new List<string>();
            foreach (var sid in sideIds)
            {
                if (!_ctx.FoodById.TryGetValue(sid, out var sf) || sf == null) continue;
                int sp = EstimatePrice(sid, serving.accuracy);
                sideSchemas.Add(new FoodSchema(sf, sp));
            }

            // Reward = totalPrice × mainMultiplier × (1 + matchingSides × sideCoefficient)
            // 프로덕션 MenuValidator 공식 재현 — 단 sideCoefficient는 config로 튜닝 가능.
            float sideCoef = _ctx.GameConfig?.sideCoefficient ?? 0.05f;
            int totalPrice = mainPrice + sideSchemas.Sum(s => s.Price);
            float mainMult = 1.0f; // sim: 항상 주문 = 제공 → mainMatch true
            float sideMult = 1.0f + sideSchemas.Count * sideCoef;
            int reward = UnityEngine.Mathf.RoundToInt(totalPrice * mainMult * sideMult);

            _ctx.Stats.AddMoney(reward);
            _ctx.Settlement.AddIncome(PhaseIncomeLabel(phase), reward);

            _ctx.Log.Add(new CustomerServedEvent
            {
                day = _ctx.State.phase.Day,
                phase = (int)phase,
                mainId = mainId,
                sideIds = sideIds.ToArray(),
                accuracy = serving.accuracy,
                reward = reward,
            });
            // 이 페이즈에서 이 메뉴 서빙 성공 카운트 증가 (mid-day run-out 판정 근거).
            _phaseServedCount.TryGetValue(mainId, out int cnt);
            _phaseServedCount[mainId] = cnt + 1;
        }

        // ── 타임아웃 원인 판정 ──

        /// <summary>부족한 첫 leaf ingredient id 반환. 전부 있으면 null.</summary>
        private string FindMissing(System.Collections.Generic.Dictionary<string, int> required)
        {
            foreach (var kv in required)
            {
                if (!_ctx.FoodById.TryGetValue(kv.Key, out var food) || food == null) return kv.Key;
                if (_ctx.Inventory.CheckStockAmount(food) < kv.Value) return kv.Key;
            }
            return null;
        }

        /// <summary>타임아웃 원인 분류.
        /// - RECIPE_UNKNOWN: LeafIngredients가 없거나 알 수 없는 음식
        /// - MID_DAY_RUN_OUT: 이 페이즈에서 이 메뉴로 이미 최소 1회 서빙했음
        /// - INSUFFICIENT_BUY: 이 페이즈에서 이 메뉴 서빙 0회 = 아침 시작부터 재료 부족</summary>
        private string ClassifyReason(string mainId, System.Collections.Generic.Dictionary<string, int> required)
        {
            if (required == null || required.Count == 0) return "RECIPE_UNKNOWN";
            if (_phaseServedCount != null && _phaseServedCount.TryGetValue(mainId, out int cnt) && cnt > 0)
                return "MID_DAY_RUN_OUT";
            return "INSUFFICIENT_BUY";
        }

        // ── 헬퍼 ──

        private Dictionary<string, int> MergedLeafIngredients(string mainId, List<string> sideIds)
        {
            var acc = new Dictionary<string, int>();
            MergeInto(acc, _ctx.Recipes.LeafIngredients(mainId));
            if (sideIds != null)
                foreach (var s in sideIds)
                    MergeInto(acc, _ctx.Recipes.LeafIngredients(s));
            return acc;
        }

        private static void MergeInto(Dictionary<string, int> acc, IReadOnlyDictionary<string, int> src)
        {
            foreach (var kv in src)
            {
                acc.TryGetValue(kv.Key, out int cur);
                acc[kv.Key] = cur + kv.Value;
            }
        }

        private bool HasAllIngredients(Dictionary<string, int> leaves)
        {
            foreach (var kv in leaves)
            {
                if (!_ctx.FoodById.TryGetValue(kv.Key, out var food) || food == null) return false;
                if (_ctx.Inventory.CheckStockAmount(food) < kv.Value) return false;
            }
            return true;
        }

        private int LookupPrice(string ingredientId) =>
            _ctx.IngredientPriceById.TryGetValue(ingredientId, out int p) ? p : 0;

        /// <summary>요리 완성가 근사 공식. 실제 CookingToolSchema.Cook의 pricing에 대해 ±20% 오차.
        /// price = costBasis × (0.85 + accuracy × 1.0) × chainBonus, chainBonus ≈ 1.2 (2-3 단계 체인).</summary>
        private int EstimatePrice(string foodId, float accuracy)
        {
            int cost = _ctx.Recipes.EstimateCostBasis(foodId, _ctx.IngredientPriceById);
            float chainBonus = 1.2f;
            float score = UnityEngine.Mathf.Clamp01(accuracy);
            return UnityEngine.Mathf.RoundToInt(cost * (0.85f + score * 1.0f) * chainBonus);
        }

        private static string PhaseIncomeLabel(PhaseType phase) => phase switch
        {
            PhaseType.Morning   => "아침 영업",
            PhaseType.Afternoon => "점심 영업",
            PhaseType.Evening   => "저녁 영업",
            PhaseType.Night     => "야간 영업",
            _ => "영업",
        };
    }
}
