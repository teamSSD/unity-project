using System;
using UnityEngine;

namespace Game.Domain.Common
{
    /// <summary>
    /// 글로벌 통계(money/stamina/day/time) POCO Service. StatsSystem facade 폐기 후 직접 사용.
    /// 상태: BasicStats (GameState.stats 라이브 참조).
    /// 이벤트: C# event 발행 — UI/Sound 등 구독자가 onChange로 반응.
    /// 부수효과(SFX 등) 없음 — 별도 어댑터가 event 구독해서 처리.
    /// </summary>
    public class StatsService
    {
        public event Action<int, int> OnTimeChanged;   // (hour, minute)
        public event Action<int>      OnStaminaChanged;
        public event Action<int>      OnMoneyChanged;
        public event Action           OnStaminaExhausted;

        private readonly Func<BasicStats> _stateAccessor;

        public StatsService(Func<BasicStats> stateAccessor)
        {
            _stateAccessor = stateAccessor;
        }

        private BasicStats Stats => _stateAccessor?.Invoke();

        public BasicStats GetSaveData() => Stats;

        /// <summary>
        /// 디스크에서 로드된 BasicStats를 현재 GameState.stats 인스턴스에 복사.
        /// (saved data는 JsonUtility로 deserialize된 NEW 인스턴스라 필드 복사 필요.)
        /// 모든 이벤트 broadcast하여 UI 즉시 갱신.
        /// </summary>
        public void ApplySaveData(BasicStats data)
        {
            var s = Stats;
            if (s == null || data == null) return;
            s.time = data.time;
            s.stamina = data.stamina;
            s.money = data.money;
            s.immutableSeed = data.immutableSeed;
            s.sessionSeed = data.sessionSeed;
            // UI 갱신을 위해 모든 이벤트 broadcast
            OnMoneyChanged?.Invoke(s.money);
            OnStaminaChanged?.Invoke(s.stamina);
            OnTimeChanged?.Invoke(GetHour(), GetMinute());
        }

        public void Reset()
        {
            var s = Stats;
            if (s == null) return;
            s.time = 0; s.stamina = 0; s.money = 0;
            // Reset 후 UI도 갱신
            OnMoneyChanged?.Invoke(0);
            OnStaminaChanged?.Invoke(0);
            OnTimeChanged?.Invoke(0, 0);
        }

        // ── Time ──
        public int GetHour()   => (Stats?.time ?? 0) / 60;
        public int GetMinute() => (Stats?.time ?? 0) % 60;

        // Night 페이즈가 22:00~익일 03:00(=27:00=1620분)까지 이어져 하루 경계를 넘음.
        // 상한 1740(=29:00)은 이전 Night endTime 여유 마진 겸 미래 페이즈 확장 대비.
        // Day 카운터는 PhaseData.Day가 SSOT — Day 전환은 ProgressService.PassDay 경로로.
        private const int TimeUpperBound = 1740;

        public void SetTime(int hour, int minute)
        {
            var s = Stats; if (s == null) return;
            s.time = Mathf.Clamp(hour * 60 + minute, 0, TimeUpperBound);
            OnTimeChanged?.Invoke(GetHour(), GetMinute());
        }

        public void AddTime(int hour, int minute)
        {
            var s = Stats; if (s == null) return;
            s.time = Mathf.Min(s.time + hour * 60 + minute, TimeUpperBound);
            OnTimeChanged?.Invoke(GetHour(), GetMinute());
        }

        // ── Stamina ──
        public int GetStamina() => Stats?.stamina ?? 0;
        public void SetStamina(int value)
        {
            var s = Stats; if (s == null) return;
            s.stamina = value;
            OnStaminaChanged?.Invoke(s.stamina);
            if (value <= 0) OnStaminaExhausted?.Invoke();
        }
        public void SubStamina(int value) => SetStamina((Stats?.stamina ?? 0) - value);

        // ── Money ──
        public int GetMoney() => Stats?.money ?? 0;
        public void SetMoney(int value)
        {
            var s = Stats; if (s == null) return;
            s.money = Mathf.Max(0, value);
            OnMoneyChanged?.Invoke(s.money);
        }
        public void AddMoney(int value) => SetMoney((Stats?.money ?? 0) + value);
        public void SubMoney(int value) => SetMoney((Stats?.money ?? 0) - value);
    }
}
