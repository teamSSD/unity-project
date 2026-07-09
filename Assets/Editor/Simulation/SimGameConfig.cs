using System.Collections.Generic;

namespace Game.Editor.Simulation
{
    /// <summary>Sim 게임 룰 튜닝용 config. SimContext.Build에 전달해 in-memory override.
    /// 실제 SO/CSV 파일 안 건드림 (프로덕션 안전).</summary>
    public class SimGameConfig
    {
        /// <summary>모든 업그레이드(Tool/Storage/Farm) 비용에 곱함. 1.0=원본, 0.5=반값.</summary>
        public float upgradeCostMultiplier = 1.0f;

        /// <summary>사이드 하나당 보상 계수. 프로덕션 MenuValidator (2026-07 튜닝: 5% → 15%).</summary>
        public float sideCoefficient = 0.15f;

        /// <summary>초기 자산 G. 프로덕션 GameStart와 동기화 (2026-07 튜닝: 8000 → 12000).</summary>
        public int startingMoney = 12000;

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
