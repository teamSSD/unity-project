using Game.Schema.State.Mall;

namespace Game.Domain.Mall
{
    /// <summary>
    /// NPC 일반 대사 사이클 진행도 (POCO Service).
    /// MallPersistent에 parallel List로 저장.
    /// 호출자는 인터랙션 시점에 GetIndex(npcId)로 현재 재생할 section 인덱스를 받고,
    /// 재생 완료 후 Advance(npcId, sectionCount)로 다음 회차로 넘김.
    /// </summary>
    public class NpcNormalDialogueService
    {
        private readonly MallPersistent _state;

        public NpcNormalDialogueService(MallPersistent state)
        {
            _state = state;
        }

        public int GetIndex(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return 0;
            int idx = _state.normalCycleNpcIds.IndexOf(npcId);
            return idx >= 0 ? _state.normalCycleIndices[idx] : 0;
        }

        public void Advance(string npcId, int sectionCount)
        {
            if (string.IsNullOrEmpty(npcId) || sectionCount <= 0) return;
            int idx = _state.normalCycleNpcIds.IndexOf(npcId);
            int cur = idx >= 0 ? _state.normalCycleIndices[idx] : 0;
            int next = (cur + 1) % sectionCount;
            if (idx >= 0)
                _state.normalCycleIndices[idx] = next;
            else
            {
                _state.normalCycleNpcIds.Add(npcId);
                _state.normalCycleIndices.Add(next);
            }
        }

        public void Clear()
        {
            _state.normalCycleNpcIds.Clear();
            _state.normalCycleIndices.Clear();
        }
    }
}
