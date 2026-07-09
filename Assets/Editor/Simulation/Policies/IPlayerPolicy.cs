using System.Collections.Generic;

namespace Game.Editor.Simulation.Policies
{
    /// <summary>정책이 반환하는 페이즈별 액션.</summary>
    public enum PhaseAction { Work, Rest, Shopping }

    /// <summary>Shopping 페이즈에서 구매하려는 항목.</summary>
    public struct PurchaseDecision
    {
        public string foodId; // ingredient FoodData.id
        public int qty;
    }

    /// <summary>Shopping 중 업그레이드 결정 (재료 구매 전에 결정 우선순위 통과 시).</summary>
    public struct UpgradeDecision
    {
        public bool doUpgrade;
        public string category; // "tool" | "storage" | "farm"
        public string trackId;  // e.g. "T001", "refrigerator", "harvestCount"
    }

    /// <summary>손님 서빙 결정 — 어떤 사이드 붙일지, 정확도 얼마나 낼지.</summary>
    public struct ServingDecision
    {
        public List<string> sideFoodIds; // 담을 사이드 요리 id들 (0~3개)
        public float accuracy; // 0..1, 미니게임 실력 근사
    }

    /// <summary>정책 인터페이스. mock 플레이어의 성향/전략을 캡슐화.
    /// SimContext는 순환 참조 피하려 <c>object</c>로 받고 각 구현에서 캐스팅.</summary>
    public interface IPlayerPolicy
    {
        string Name { get; }

        /// <summary>Preparation에서 3 도시락 슬롯 선택 (main dish FoodData.id 3개).</summary>
        string[] SelectMenusForDay(object simContext);

        /// <summary>페이즈 시작 시 액션 결정 (Morning은 자동 Work — 이 훅에선 Afternoon/Evening/Night만 호출).</summary>
        PhaseAction DecidePhaseAction(object simContext, int phase);

        /// <summary>Shopping에서 업그레이드 우선 여부.</summary>
        UpgradeDecision DecideUpgrade(object simContext);

        /// <summary>Shopping에서 재료 구매 결정. 여러 항목 리스트.</summary>
        List<PurchaseDecision> DecidePurchases(object simContext);

        /// <summary>손님 한 명 서빙 방식 결정.</summary>
        ServingDecision DecideServing(object simContext, string mainFoodId);
    }
}
