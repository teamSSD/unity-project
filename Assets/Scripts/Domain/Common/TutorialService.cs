using System;
using System.Collections.Generic;
using Game.Schema.State;

namespace Game.Domain.Common
{
    /// <summary>
    /// 튜토리얼 진행 POCO Service. Set 기반 — 각 스텝은 한 번만 표시(임의 순서 허용).
    /// completed=true면 모든 hook 비활성 (튜토리얼 종료 → 실제 게임 흐름).
    /// Save/Load 시 GameSaveData.tutorial 슬롯에 미러링.
    /// </summary>
    public class TutorialService
    {
        public event Action<int> OnStepShown;
        public event Action OnCompleted;

        private readonly Func<TutorialState> _stateAccessor;
        private HashSet<int> _shownCache;

        public TutorialService(Func<TutorialState> stateAccessor)
        {
            _stateAccessor = stateAccessor;
        }

        private TutorialState State => _stateAccessor?.Invoke();

        private HashSet<int> ShownSet
        {
            get
            {
                var s = State;
                if (s == null) return new HashSet<int>();
                if (_shownCache == null || _shownCache.Count != s.shownSteps.Count)
                    RebuildCache();
                return _shownCache;
            }
        }

        private void RebuildCache()
        {
            _shownCache = new HashSet<int>();
            var s = State;
            if (s?.shownSteps != null)
                foreach (var id in s.shownSteps) _shownCache.Add(id);
        }

        public bool IsCompleted => State?.completed ?? false;
        public bool IsActive => !IsCompleted;
        public bool HasShown(int stepId) => IsCompleted || ShownSet.Contains(stepId);

        public void MarkShown(int stepId)
        {
            var s = State;
            if (s == null || s.completed) return;
            if (ShownSet.Contains(stepId)) return;
            s.shownSteps.Add(stepId);
            _shownCache?.Add(stepId);
            OnStepShown?.Invoke(stepId);
        }

        public void Complete()
        {
            var s = State;
            if (s == null || s.completed) return;
            s.completed = true;
            OnCompleted?.Invoke();
        }

        public TutorialSaveData GetSaveData() => new TutorialSaveData
        {
            shownSteps = new List<int>(State?.shownSteps ?? new List<int>()),
            completed = State?.completed ?? false,
        };

        public void ApplySaveData(TutorialSaveData data)
        {
            var s = State;
            if (s == null || data == null) return;
            s.shownSteps = data.shownSteps != null ? new List<int>(data.shownSteps) : new List<int>();
            s.completed = data.completed;
            RebuildCache();
        }
    }

    [System.Serializable]
    public class TutorialSaveData
    {
        public List<int> shownSteps = new List<int>();
        public bool completed;
    }
}
