using System.Collections.Generic;
using Game.Editor.Simulation.Cooking;
using Game.Editor.Simulation.Events;
using Game.Editor.Simulation.Policies;

namespace Game.Editor.Simulation
{
    /// <summary>메인 sim 루프. 지정 일수만큼 phase 순환.
    /// 파산 감지: 자산 &lt;= 0 도달 시 stop + Bankrupt=true 세팅.
    ///
    /// 페이즈 흐름:
    ///  - Preparation: 정책이 3 메뉴 선택
    ///  - Morning: 쿠킹 (자동 Work)
    ///  - Afternoon: 정책 결정 (Shopping/Rest/Work)
    ///     - Shopping이면 업그레이드 우선 + 재료 매수
    ///     - Work이면 쿠킹
    ///  - Evening/Night: 동일
    ///
    /// 페이즈 종료마다 PhaseCashFlow 이벤트 발화.
    /// 일자 종료마다 DayEndSnapshot 발화 + 관리비 차감(Progress.PassDay).</summary>
    public class SimHarness
    {
        private readonly SimContext _ctx;
        private readonly CookingSimulator _cook;

        public SimHarness(SimContext ctx)
        {
            _ctx = ctx;
            _cook = new CookingSimulator(ctx);
        }

        /// <summary>days 일수 완주. 파산이면 조기 종료.</summary>
        public void Run(int days)
        {
            string[] selectedMenus = null;

            for (int d = 0; d < days; d++)
            {
                if (CheckBankrupt()) return;

                selectedMenus = RunPreparation();
                if (CheckBankrupt()) return;

                _ctx.Progress.PassPhase(); // → Morning

                RunCookingPhase(PhaseType.Morning, selectedMenus);
                if (CheckBankrupt()) return;
                _ctx.Progress.PassPhase(); // → Afternoon

                RunChoicePhase(PhaseType.Afternoon, selectedMenus);
                if (CheckBankrupt()) return;
                _ctx.Progress.PassPhase(); // → Evening

                RunChoicePhase(PhaseType.Evening, selectedMenus);
                if (CheckBankrupt()) return;
                _ctx.Progress.PassPhase(); // → Night

                RunChoicePhase(PhaseType.Night, selectedMenus);
                if (CheckBankrupt()) return;

                // Night PassPhase가 PassDay 호출 → 관리비 차감 + Weather 갱신 + 재고 만료.
                int moneyBeforePassDay = _ctx.Stats.GetMoney();
                _ctx.Progress.PassPhase();
                int mgmt = Game.Domain.Mall.SettlementService.ManagementFee;
                _ctx.Log.Add(new PhaseCashFlowEvent
                {
                    day = _ctx.State.phase.Day - 1, // 방금 지난 날
                    phase = (int)PhaseType.Night,
                    category = "mgmtFee",
                    amount = -mgmt,
                });

                EmitDaySnapshot(d);

                if (CheckBankrupt()) return;
            }
        }

        // ── Preparation ──
        private string[] RunPreparation()
        {
            EmitPhaseEnter(PhaseType.Preparation);
            var menus = _ctx.Policy.SelectMenusForDay(_ctx);
            _ctx.SelectedMenuIds = menus ?? new string[0];
            _ctx.Log.Add(new MenuSelectedEvent
            {
                day = _ctx.State.phase.Day,
                phase = (int)PhaseType.Preparation,
                mainIds = _ctx.SelectedMenuIds,
            });
            return _ctx.SelectedMenuIds;
        }

        // ── Morning (자동 Work) ──
        private void RunCookingPhase(PhaseType phase, string[] menus)
        {
            EmitPhaseEnter(phase);
            int moneyBefore = _ctx.Stats.GetMoney();
            _cook.SimulatePhase(phase, menus);
            int cookIncome = _ctx.Stats.GetMoney() - moneyBefore;
            if (cookIncome != 0)
            {
                _ctx.Log.Add(new PhaseCashFlowEvent
                {
                    day = _ctx.State.phase.Day,
                    phase = (int)phase,
                    category = "cook",
                    amount = cookIncome,
                });
            }
        }

        // ── Afternoon/Evening/Night (정책 결정) ──
        private void RunChoicePhase(PhaseType phase, string[] menus)
        {
            EmitPhaseEnter(phase);
            var action = _ctx.Policy.DecidePhaseAction(_ctx, (int)phase);
            switch (action)
            {
                case PhaseAction.Work:
                    RunCookingPhase(phase, menus);
                    break;
                case PhaseAction.Shopping:
                    RunShoppingPhase(phase);
                    break;
                case PhaseAction.Rest:
                    // 별다른 처리 없음 (스태미너는 PassDay가 리셋)
                    break;
            }
        }

        private void RunShoppingPhase(PhaseType phase)
        {
            // 업그레이드 우선
            var up = _ctx.Policy.DecideUpgrade(_ctx);
            if (up.doUpgrade)
            {
                int moneyBefore = _ctx.Stats.GetMoney();
                bool ok = TryUpgrade(up);
                int spent = moneyBefore - _ctx.Stats.GetMoney();
                if (ok && spent > 0)
                {
                    _ctx.Log.Add(new PhaseCashFlowEvent
                    {
                        day = _ctx.State.phase.Day,
                        phase = (int)phase,
                        category = "upgrade",
                        amount = -spent,
                    });
                    int newLevel = GetCurrentLevel(up.category, up.trackId);
                    _ctx.Log.Add(new UpgradeMadeEvent
                    {
                        day = _ctx.State.phase.Day,
                        phase = (int)phase,
                        category = up.category,
                        trackId = up.trackId,
                        newLevel = newLevel,
                        cost = spent,
                    });
                }
            }

            // 재료 매수
            var decisions = _ctx.Policy.DecidePurchases(_ctx);
            if (decisions == null) return;

            int purchaseSpentTotal = 0;
            foreach (var dec in decisions)
            {
                if (dec.qty <= 0 || string.IsNullOrEmpty(dec.foodId)) continue;
                if (!_ctx.FoodById.TryGetValue(dec.foodId, out var food) || food == null) continue;

                int unitPrice = food.ingredient != null ? food.ingredient.defaultPrice : 0;
                if (unitPrice <= 0) continue;

                int invBefore = _ctx.Inventory.CheckStockAmount(food);
                bool ok = _ctx.Purchase.TryBuy(food, dec.qty, unitPrice);
                if (!ok) continue;

                int total = dec.qty * unitPrice;
                purchaseSpentTotal += total;

                _ctx.Log.Add(new IngredientPurchasedEvent
                {
                    day = _ctx.State.phase.Day,
                    phase = (int)phase,
                    itemId = dec.foodId,
                    qty = dec.qty,
                    unitPrice = unitPrice,
                    totalCost = total,
                    invCountBefore = invBefore,
                    invCountAfter = _ctx.Inventory.CheckStockAmount(food),
                });
            }

            if (purchaseSpentTotal > 0)
            {
                _ctx.Log.Add(new PhaseCashFlowEvent
                {
                    day = _ctx.State.phase.Day,
                    phase = (int)phase,
                    category = "purchase",
                    amount = -purchaseSpentTotal,
                });
            }
        }

        // ── 업그레이드 dispatch ──
        private bool TryUpgrade(UpgradeDecision up)
        {
            return up.category switch
            {
                "tool"    => _ctx.ToolUpgrade.TryUpgrade(up.trackId),
                "storage" => _ctx.StorageUpgrade.TryUpgrade(up.trackId),
                "farm"    => _ctx.FarmUpgrade.TryUpgrade(up.trackId),
                _         => false,
            };
        }

        private int GetCurrentLevel(string category, string trackId)
        {
            switch (category)
            {
                case "tool":    return _ctx.ToolUpgrade.GetCurrentData(trackId)?.level ?? 0;
                case "storage": return _ctx.StorageUpgrade.GetCurrentData(trackId)?.level ?? 0;
                case "farm":    return _ctx.FarmUpgrade.GetCurrentData(trackId)?.level ?? 0;
                default: return 0;
            }
        }

        // ── 이벤트 ──
        private void EmitPhaseEnter(PhaseType phase)
        {
            _ctx.Log.Add(new PhaseEnterEvent { day = _ctx.State.phase.Day, phase = (int)phase });
            // 매 페이즈 진입마다 farm tick — 수확/심기 자동 진행.
            FarmSim.Tick(_ctx);
        }

        private void EmitDaySnapshot(int dayIdx)
        {
            var invByFood = new Dictionary<string, int>();
            foreach (var kv in _ctx.FoodById)
            {
                int cnt = _ctx.Inventory.CheckStockAmount(kv.Value);
                if (cnt > 0) invByFood[kv.Key] = cnt;
            }

            var upgrades = new Dictionary<string, int>();
            foreach (var t in new[] { "T001", "T002", "T003", "T004", "T005" })
                upgrades["tool." + t] = _ctx.ToolUpgrade.GetCurrentData(t)?.level ?? 0;
            foreach (var s in new[] { "refrigerator", "upperShelf", "lowerShelf" })
                upgrades["storage." + s] = _ctx.StorageUpgrade.GetCurrentData(s)?.level ?? 0;
            foreach (var f in new[] { "tile", "harvestCount", "timeReduction" })
                upgrades["farm." + f] = _ctx.FarmUpgrade.GetCurrentData(f)?.level ?? 0;

            _ctx.Log.Add(new DayEndSnapshotEvent
            {
                day = dayIdx,
                phase = (int)PhaseType.Night,
                money = _ctx.Stats.GetMoney(),
                stamina = _ctx.Stats.GetStamina(),
                inventoryByFood = invByFood,
                upgradeLevels = upgrades,
                badWeather = _ctx.Weather.IsBadWeather,
                // 스냅샷 시점에서 자산 &lt;= 0이면 파산 예정. _ctx.Bankrupt는 CheckBankrupt 호출 후 세팅되므로 별도 판정.
                bankrupt = _ctx.Stats.GetMoney() <= 0,
            });
        }

        private bool CheckBankrupt()
        {
            if (_ctx.Stats.GetMoney() <= 0)
            {
                _ctx.Bankrupt = true;
                return true;
            }
            return false;
        }
    }
}
