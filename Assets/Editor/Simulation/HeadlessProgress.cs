using Game.Domain.Common;
using Game.Domain.Mall;

namespace Game.Editor.Simulation
{
    /// <summary>
    /// ProgressService의 headless 대체. Unity 커플링(SceneLoader/SaveManager/GameSessionRoot.Instance) 제거.
    /// PassPhase/PassDay 로직은 ProgressService.cs 참조 (2026-07 기준).
    ///
    /// TimePhaseProvider 구현하지 않음 — 필요 시 확장.
    /// Night PassPhase는 Settlement 씬 로드 대신 그냥 flag 세팅 → 다음 호출에서 PassDay가 자동 처리.
    /// </summary>
    public class HeadlessProgress
    {
        private readonly PhaseData _phaseData;
        private readonly StatsService _stats;
        private readonly InventoryService _inventory;
        private readonly WeatherService _weather;
        private readonly SettlementService _settlement;

        public PhaseData PhaseData => _phaseData;
        public int CurrentPhaseIndex { get; private set; }

        public HeadlessProgress(PhaseData phaseData, StatsService stats, InventoryService inventory,
            WeatherService weather, SettlementService settlement)
        {
            _phaseData = phaseData;
            _stats = stats;
            _inventory = inventory;
            _weather = weather;
            _settlement = settlement;
        }

        /// <summary>페이즈 진행. Night → 다음 호출 시 PassDay 유도.
        /// (프로덕션은 Night PassPhase에서 Settlement 씬 로드 후 유저가 확인 → PassDay.
        /// Headless는 즉시 넘어감.)</summary>
        public void PassPhase()
        {
            if (_phaseData.Phase == PhaseType.Night)
            {
                PassDay();
                return;
            }
            _phaseData.Phase++;
            CurrentPhaseIndex++;
            SetPhaseTime(_phaseData.Phase);
        }

        /// <summary>일자 종료 — 관리비 차감 + 다음날 리셋. ProgressService.PassDay 로직 복제.
        /// SceneLoader/SaveManager 제외.</summary>
        public void PassDay()
        {
            _stats.SubMoney(SettlementService.ManagementFee);

            _phaseData.Day++;
            _phaseData.Phase = PhaseType.Preparation;
            CurrentPhaseIndex++;
            SetPhaseTime(_phaseData.Phase);

            _stats.SetStamina(100);
            GameRandom.InitDay(_phaseData.Day);
            _inventory.AdvanceDay();
            _weather?.UpdateWeather(_phaseData.Day);
            _settlement?.Reset(_stats.GetMoney());
            // SaveManager.SaveAll 생략.
        }

        private void SetPhaseTime(PhaseType phase)
        {
            switch (phase)
            {
                case PhaseType.Preparation: _stats.SetTime(5, 0);  break;
                case PhaseType.Morning:     _stats.SetTime(7, 0);  break;
                case PhaseType.Afternoon:   _stats.SetTime(12, 0); break;
                case PhaseType.Evening:     _stats.SetTime(17, 0); break;
                case PhaseType.Night:       _stats.SetTime(22, 0); break;
            }
        }
    }
}
