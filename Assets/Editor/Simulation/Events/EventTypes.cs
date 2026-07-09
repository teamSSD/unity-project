using System.Collections.Generic;
using System.Text;

namespace Game.Editor.Simulation.Events
{
    // 13개 이벤트 타입 정의. plan.md 스키마 반영.

    /// <summary>페이즈 진입.</summary>
    public sealed class PhaseEnterEvent : SimEvent
    {
        public override string ToJsonLine() => "{" + BaseFields("PhaseEnter") + "}";
    }

    /// <summary>Afternoon/Evening/Night에서 정책이 결정한 액션 로그.</summary>
    public sealed class PhaseActionEvent : SimEvent
    {
        public string action; // "Work" / "Shopping" / "Rest"
        public override string ToJsonLine() =>
            "{" + BaseFields("PhaseAction") + $",\"action\":{J(action)}" + "}";
    }

    /// <summary>Preparation에서 도시락 3슬롯 선택.</summary>
    public sealed class MenuSelectedEvent : SimEvent
    {
        public string[] mainIds; // 3 slot
        public override string ToJsonLine()
        {
            var mains = new StringBuilder("[");
            for (int i = 0; i < (mainIds?.Length ?? 0); i++)
            {
                if (i > 0) mains.Append(',');
                mains.Append(J(mainIds[i]));
            }
            mains.Append(']');
            return "{" + BaseFields("MenuSelected") + $",\"mainIds\":{mains}" + "}";
        }
    }

    /// <summary>손님 서빙 완료. 매출 정산.</summary>
    public sealed class CustomerServedEvent : SimEvent
    {
        public string mainId;
        public string[] sideIds;
        public float accuracy;
        public int reward;
        public override string ToJsonLine()
        {
            var sides = new StringBuilder("[");
            for (int i = 0; i < (sideIds?.Length ?? 0); i++)
            {
                if (i > 0) sides.Append(',');
                sides.Append(J(sideIds[i]));
            }
            sides.Append(']');
            return "{" + BaseFields("CustomerServed") +
                   $",\"mainId\":{J(mainId)},\"sideIds\":{sides},\"accuracy\":{accuracy:F3},\"reward\":{reward}" + "}";
        }
    }

    /// <summary>손님 인내심 소진 나감 (놓친 매출). 원인 태깅으로 정책/규칙 튜닝 방향 지목.</summary>
    public sealed class CustomerTimedOutEvent : SimEvent
    {
        public string menuMainId;
        /// <summary>실패 원인 카테고리. INSUFFICIENT_BUY / MID_DAY_RUN_OUT / RECIPE_UNKNOWN / OTHER.</summary>
        public string reason;
        /// <summary>부족했던 leaf ingredient id (여러 개 중 첫 발견).</summary>
        public string missingIngredientId;
        public override string ToJsonLine() =>
            "{" + BaseFields("CustomerTimedOut") +
            $",\"menuMainId\":{J(menuMainId)},\"reason\":{J(reason)},\"missingIngredientId\":{J(missingIngredientId)}" + "}";
    }

    /// <summary>재료 구매.</summary>
    public sealed class IngredientPurchasedEvent : SimEvent
    {
        public string itemId;
        public int qty;
        public int unitPrice;
        public int totalCost;
        public int invCountBefore;
        public int invCountAfter;
        public override string ToJsonLine() =>
            "{" + BaseFields("IngredientPurchased") +
            $",\"itemId\":{J(itemId)},\"qty\":{qty},\"unitPrice\":{unitPrice},\"totalCost\":{totalCost}" +
            $",\"invBefore\":{invCountBefore},\"invAfter\":{invCountAfter}" + "}";
    }

    /// <summary>재료 소비 — 요리용/유통기한 만료용.</summary>
    public sealed class IngredientConsumedEvent : SimEvent
    {
        public string itemId;
        public int qty;
        public string purpose; // "cook" | "expired" | "wasted"
        public int valueAtCost; // 원가 (소비된 재고의 자산가치)
        public override string ToJsonLine() =>
            "{" + BaseFields("IngredientConsumed") +
            $",\"itemId\":{J(itemId)},\"qty\":{qty},\"purpose\":{J(purpose)},\"valueAtCost\":{valueAtCost}" + "}";
    }

    /// <summary>유통기한 만료로 폐기 (Consumed로 대체 가능하지만 별개 유지해 필터 편함).</summary>
    public sealed class IngredientExpiredEvent : SimEvent
    {
        public string itemId;
        public int qty;
        public int valueAtCost;
        public override string ToJsonLine() =>
            "{" + BaseFields("IngredientExpired") +
            $",\"itemId\":{J(itemId)},\"qty\":{qty},\"valueAtCost\":{valueAtCost}" + "}";
    }

    /// <summary>창고 가득참/타입 불일치로 재료 못 받음.</summary>
    public sealed class StorageRejectedEvent : SimEvent
    {
        public string itemId;
        public int qty;
        public string reason; // "full" | "wrongType"
        public override string ToJsonLine() =>
            "{" + BaseFields("StorageRejected") +
            $",\"itemId\":{J(itemId)},\"qty\":{qty},\"reason\":{J(reason)}" + "}";
    }

    /// <summary>업그레이드 실행.</summary>
    public sealed class UpgradeMadeEvent : SimEvent
    {
        public string category; // "tool" | "storage" | "farm"
        public string trackId;
        public int newLevel;
        public int cost;
        public override string ToJsonLine() =>
            "{" + BaseFields("UpgradeMade") +
            $",\"category\":{J(category)},\"trackId\":{J(trackId)},\"newLevel\":{newLevel},\"cost\":{cost}" + "}";
    }

    /// <summary>배달 주문 상태 전환 (accepted/cooked/delivered).</summary>
    public sealed class DeliveryEvent : SimEvent
    {
        public string questId;
        public string state; // "accepted" | "cooked" | "delivered"
        public int reward;
        public int latencyPhases; // delivered 시 accepted → delivered 지연
        public override string ToJsonLine() =>
            "{" + BaseFields("Delivery") +
            $",\"questId\":{J(questId)},\"state\":{J(state)},\"reward\":{reward},\"latencyPhases\":{latencyPhases}" + "}";
    }

    /// <summary>텃밭 심기/수확.</summary>
    public sealed class FarmEvent : SimEvent
    {
        public string action; // "plant" | "harvest"
        public string cropId;
        public int tileIndex;
        public int yieldQty; // harvest 시 수확량
        public override string ToJsonLine() =>
            "{" + BaseFields("Farm") +
            $",\"action\":{J(action)},\"cropId\":{J(cropId)},\"tile\":{tileIndex},\"yield\":{yieldQty}" + "}";
    }

    /// <summary>페이즈별 수입/지출 항목별 집계.</summary>
    public sealed class PhaseCashFlowEvent : SimEvent
    {
        public string category; // "cook" | "delivery" | "purchase" | "upgrade" | "mgmtFee"
        public int amount; // 양수=수입, 음수=지출
        public override string ToJsonLine() =>
            "{" + BaseFields("PhaseCashFlow") +
            $",\"category\":{J(category)},\"amount\":{amount}" + "}";
    }

    /// <summary>일자 종료 스냅샷 — 상태 시계열.</summary>
    public sealed class DayEndSnapshotEvent : SimEvent
    {
        public int money;
        public int stamina;
        public Dictionary<string, int> inventoryByFood; // foodId → total qty
        public Dictionary<string, int> upgradeLevels;   // "tool.T001" → level 등
        public bool badWeather;
        public bool bankrupt; // sim harness 판정
        public override string ToJsonLine()
        {
            var inv = new StringBuilder("{");
            bool first = true;
            if (inventoryByFood != null)
                foreach (var kv in inventoryByFood)
                {
                    if (!first) inv.Append(',');
                    first = false;
                    inv.Append(J(kv.Key)).Append(':').Append(kv.Value);
                }
            inv.Append('}');

            var up = new StringBuilder("{");
            first = true;
            if (upgradeLevels != null)
                foreach (var kv in upgradeLevels)
                {
                    if (!first) up.Append(',');
                    first = false;
                    up.Append(J(kv.Key)).Append(':').Append(kv.Value);
                }
            up.Append('}');

            return "{" + BaseFields("DayEndSnapshot") +
                   $",\"money\":{money},\"stamina\":{stamina}" +
                   $",\"inventory\":{inv},\"upgrades\":{up}" +
                   $",\"badWeather\":{(badWeather ? "true" : "false")},\"bankrupt\":{(bankrupt ? "true" : "false")}" +
                   "}";
        }
    }
}
