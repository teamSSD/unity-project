using System.Collections.Generic;

namespace Game.Editor.Simulation
{
    /// <summary>Sim 게임 룰 튜닝용 config. SimContext.Build에 전달해 in-memory override.
    /// 실제 SO/CSV 파일 안 건드림 (프로덕션 안전).</summary>
    public class SimGameConfig
    {
        /// <summary>모든 업그레이드(Tool/Storage/Farm) 비용에 곱함. 1.0=원본, 0.5=반값.</summary>
        public float upgradeCostMultiplier = 1.0f;

        /// <summary>L3+ (endgame) 업그레이드 비용에만 추가로 곱함. 초기 upgrade 부담 없이 endgame만 늦춤.
        /// upgradeCostMultiplier와 곱해짐 (예: base=1.0, late=3.0 → L1/L2는 원본, L3+는 ×3).</summary>
        public float lateUpgradeCostMultiplier = 1.0f;

        /// <summary>사이드 하나당 보상 계수. 프로덕션 MenuValidator (2026-07 튜닝: 5% → 15%).</summary>
        public float sideCoefficient = 0.15f;

        /// <summary>초기 자산 G. 프로덕션 GameStart와 동기화 (2026-07 튜닝: 8000 → 12000).</summary>
        public int startingMoney = 12000;

        /// <summary>true면 6개 MAIN + 4개 SIDE 전부 unlock (quest 진행 대체용).
        /// 다양성/음식별 분석 시 초기 2 메뉴 제한 우회.</summary>
        public bool unlockAllMenus = false;

        /// <summary>점진 unlock 스케줄 — (day, foodId) 튜플. quest 완료 시점 근사 (하드코딩).</summary>
        public List<(int day, string foodId)> ProgressiveUnlock = new();

        /// <summary>자산 임계치 기반 동적 unlock — (moneyThreshold, foodId).
        /// 실 유저의 "quest 감당 가능하면 accept" 판단 모사.
        /// </summary>
        public List<(int moneyThreshold, string foodId)> AssetTriggeredUnlock = new();

        /// <summary>Quest 완료 보상 — foodId → 자산 지급. Unlock 트리거 시점에 지급.</summary>
        public Dictionary<string, int> QuestUnlockReward = new();

        /// <summary>Quest 매일 배달 수입 — foodId → 하루 수입. Unlock된 quest는 매일 이만큼 지급.
        /// Lump sum reward 대안 — 자산 급증 방지, 자연스러운 daily flow.</summary>
        public Dictionary<string, int> QuestDailyIncome = new();

        /// <summary>Farm crop들 판매가 배율 (재고에 추가된 crop이 요리에 쓰일 때 원가로 반영).
        /// 실제로는 IngredientData.defaultPrice가 crop 판매가와 같음 (crop 수확 → ingredient).
        /// 이 배율은 크롭용 재료들(I004,I005,I012,I013,I017,I018,I025,I026,I027,I028,I029,I065,I066)에만 적용.</summary>
        public float farmCropPriceMultiplier = 1.0f;

        /// <summary>Farm crop id 목록 — 위 배율 적용 대상. cropData.csv 기준.</summary>
        public static readonly HashSet<string> FarmCropIds = new()
        {
            "I004", "I005", "I012", "I013", "I017", "I018",
            "I025", "I026", "I027", "I028", "I029", "I065", "I066",
        };

        public override string ToString() =>
            $"upCost={upgradeCostMultiplier:F2} sideCoef={sideCoefficient:F2} startM={startingMoney} cropPx={farmCropPriceMultiplier:F2}";
    }
}
