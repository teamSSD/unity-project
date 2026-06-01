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
        public event Action<int>      OnDayChanged;
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
        public void ApplySaveData(BasicStats _) { /* GameState 라이브 참조이므로 별도 동작 불필요 */ }
        public void Reset()
        {
            var s = Stats;
            if (s == null) return;
            s.day = 0; s.time = 0; s.stamina = 0; s.money = 0;
        }

        // ── Day ──
        public int GetDay() => Stats?.day ?? 0;
        public void AddDay(int value)
        {
            var s = Stats; if (s == null) return;
            s.day += value;
            OnDayChanged?.Invoke(s.day);
        }

        // ── Time ──
        public int GetHour()   => (Stats?.time ?? 0) / 60;
        public int GetMinute() => (Stats?.time ?? 0) % 60;

        public void SetTime(int hour, int minute)
        {
            var s = Stats; if (s == null) return;
            s.time = Mathf.Clamp(hour * 60 + minute, 0, 1439);
            OnTimeChanged?.Invoke(GetHour(), GetMinute());
        }

        public void AddTime(int hour, int minute)
        {
            var s = Stats; if (s == null) return;
            s.time += hour * 60 + minute;
            if (s.time >= 1440) { s.time %= 1440; AddDay(1); }
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
